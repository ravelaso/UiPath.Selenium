# Phase 5: Anchor System Implementation Plan

**Status:** Planning  
**Target:** v1.0.2  
**Estimated Duration:** 4-6 hours  
**Date Created:** 2026-10-23

---

## 📋 Executive Summary

Implement a **typed, fluent Anchor System** that allows finding elements dynamically using visual references, similar to UiPath Anchors but tailored for Selenium WebDriver.

Instead of hardcoded XPath, users can declare:
```csharp
Loc.Anchor(Loc.Text("2026-10-22"))
    .Find(Loc.Css("button.download"))
    .Click(client);
```

The system **automatically searches all directions** and returns the first match. If nothing is found, it throws a clear exception.

---

## 🎯 Objectives

1. ✅ Create `AnchorLocator` class with fluent API
2. ✅ Implement exhaustive search across all directions
3. ✅ Add to `Locator` class as extension method
4. ✅ Support optional `SearchOrder()` for optimization
5. ✅ Support `ExactDirection()` for strict validation
6. ✅ Configurable timeout (default 5s)
7. ✅ Clear exception messages on failure
8. ✅ Documentation with examples and API reference

---

## 🏗️ Architecture

### **New Classes**

#### `AnchorLocator.cs`
```csharp
namespace Ravelaso.UiPath.Selenium.Core;

public class AnchorLocator
{
    private readonly IWebDriver _driver;
    private readonly Locator _anchor;
    private Locator? _target;
    private Direction[]? _searchOrder;
    private TimeSpan _timeout = TimeSpan.FromSeconds(5);
    private const int DefaultMaxDistance = 500; // pixels

    /// <summary>
    /// Creates an anchor point for relative element searching.
    /// </summary>
    public AnchorLocator(IWebDriver driver, Locator anchor);

    /// <summary>
    /// Define the target element to find relative to the anchor.
    /// </summary>
    public AnchorLocator Find(Locator target);

    /// <summary>
    /// Specify search directions in order of preference.
    /// If not set, searches exhaustively: Right → Left → Below → Above → SameRow → SameContainer → Innermost → Anywhere
    /// </summary>
    public AnchorLocator SearchOrder(params Direction[] directions);

    /// <summary>
    /// Search only in a specific direction. Throws if not found.
    /// </summary>
    public AnchorLocator ExactDirection(Direction direction);

    /// <summary>
    /// Set custom timeout for element search.
    /// </summary>
    public AnchorLocator WithTimeout(TimeSpan timeout);

    /// <summary>
    /// Resolve the anchor search and return the target locator.
    /// </summary>
    public Locator Resolve();

    /// <summary>
    /// Resolve and return the actual IWebElement (for advanced cases).
    /// </summary>
    public IWebElement ResolveElement();
}
```

#### `Direction.cs` (Enum)
```csharp
namespace Ravelaso.UiPath.Selenium.Core;

/// <summary>
/// Defines the spatial relationship between anchor and target elements.
/// </summary>
public enum Direction
{
    /// <summary>Right side of the anchor</summary>
    Right = 0,

    /// <summary>Left side of the anchor</summary>
    Left = 1,

    /// <summary>Below the anchor</summary>
    Below = 2,

    /// <summary>Above the anchor</summary>
    Above = 3,

    /// <summary>Same row as anchor (HTML &lt;tr&gt;)</summary>
    SameRow = 4,

    /// <summary>Same container/parent as anchor</summary>
    SameContainer = 5,

    /// <summary>Direct child of anchor</summary>
    Innermost = 6,

    /// <summary>Anywhere in the document (fallback)</summary>
    Anywhere = 7
}
```

### **Modified Classes**

#### `Locator.cs` (Extension Method)
```csharp
public class Locator
{
    // ... existing code ...

    /// <summary>
    /// Create an anchor point for relative element searching.
    /// </summary>
    public AnchorLocator Anchor(IWebDriver driver)
    {
        return new AnchorLocator(driver, this);
    }
}
```

#### `RavelasoWebClient.cs` (Helper Method)
```csharp
public class RavelasoWebClient
{
    // ... existing code ...

    /// <summary>
    /// Helper method to create an anchor from this client's driver.
    /// </summary>
    public AnchorLocator CreateAnchor(Locator anchorPoint)
    {
        return new AnchorLocator(_driver, anchorPoint);
    }
}
```

---

## 🔍 Search Algorithm

### **Exhaustive Search Order (Default)**

When `SearchOrder()` is NOT specified:

```
1. Right       → XPath: following-sibling or position-based search
2. Left        → XPath: preceding-sibling or position-based search
3. Below       → XPath: //element positioned below
4. Above       → XPath: //element positioned above
5. SameRow     → XPath: ancestor::tr + any element within
6. SameContainer → XPath: ancestor::* + any element within
7. Innermost   → XPath: descendant::* (direct children first)
8. Anywhere    → XPath: //* (global search, last resort)

Returns: First match found
Stops: As soon as an element is found
On Failure: Throws ElementNotFoundException with details
```

### **Proximity-based Selection**

When multiple matches exist in a direction:
- **Calculate distance** from anchor to each candidate
- **Return the closest** (by pixel distance)
- Use `ExecuteScript` to get bounding rectangles

### **XPath Construction Examples**

```csharp
// Direction.Right
//following-sibling::*[matches(@class, 'button')]

// Direction.Below
//*[preceding::element and following-sibling::* = self]

// Direction.SameRow
//ancestor::tr//button[@class='download']

// Direction.SameContainer
//ancestor::div[@class='item']//button

// Direction.Innermost
//button | //span/button | //span/span/button (direct lineage)

// Direction.Anywhere
//button[@class='download']
```

---

## 📝 Implementation Details

### **Pseudocode for Resolve()**

```csharp
public Locator Resolve()
{
    if (_target == null)
        throw new InvalidOperationException("Target not defined. Call Find() first.");

    var directionsToTry = _searchOrder ?? GetExhaustiveOrder();
    var sw = Stopwatch.StartNew();

    foreach (var direction in directionsToTry)
    {
        if (sw.Elapsed > _timeout)
            throw new TimeoutException($"Anchor search exceeded {_timeout.TotalSeconds}s");

        try
        {
            var xpath = BuildXPathForDirection(direction);
            var elements = _driver.FindElements(By.XPath(xpath));

            if (elements.Any())
            {
                var closest = SelectClosestElement(elements);
                return new Locator(xpath); // Return resolved locator
            }
        }
        catch (NoSuchElementException)
        {
            continue; // Try next direction
        }
    }

    // Nothing found
    throw new NoSuchElementException(
        $"Anchor search failed.\n" +
        $"Anchor: {_anchor}\n" +
        $"Target: {_target}\n" +
        $"Directions tried: {string.Join(", ", directionsToTry)}\n" +
        $"Timeout: {_timeout.TotalSeconds}s"
    );
}

private Direction[] GetExhaustiveOrder()
{
    return new[]
    {
        Direction.Right,
        Direction.Left,
        Direction.Below,
        Direction.Above,
        Direction.SameRow,
        Direction.SameContainer,
        Direction.Innermost,
        Direction.Anywhere
    };
}

private string BuildXPathForDirection(Direction direction)
{
    // Generate XPath based on anchor, target, and direction
    // This is complex and requires coordinate calculations
}

private IWebElement SelectClosestElement(IEnumerable<IWebElement> elements)
{
    // Get bounding boxes and calculate distance from anchor
    // Return the closest element
}
```

---

## 💻 Usage Examples

### **Example 1: Table Download (Dynamic Rows)**

```csharp
var client = new RavelasoWebClient(BrowserType.Chrome);
client.Navigate("https://reports.example.com");

// Find and click download button in the row with date "2026-10-22"
var downloadBtn = Loc.Anchor(Loc.Text("2026-10-22"))
    .Find(Loc.Css("button.download"));

client.Click(downloadBtn);

// Or using helper method
var downloadBtn = client.CreateAnchor(Loc.Text("2026-10-22"))
    .Find(Loc.Css("button.download"))
    .Resolve();

client.Click(downloadBtn);
```

### **Example 2: Form Field with Label**

```csharp
// Find email input next to "Email:" label
var emailInput = Loc.Anchor(Loc.Text("Email:"))
    .Find(Loc.Css("input[type='email']"))
    .SearchOrder(Direction.Right, Direction.Below) // Try right first, then below
    .Resolve();

client.Type(emailInput, "user@example.com");
```

### **Example 3: List Item Action**

```csharp
// Find delete button next to item name
var deleteBtn = Loc.Anchor(Loc.Text("Project Alpha"))
    .Find(Loc.Css("button.delete"))
    .ExactDirection(Direction.Right) // Must be to the right
    .Resolve();

client.Click(deleteBtn);
```

### **Example 4: With Custom Timeout**

```csharp
var element = Loc.Anchor(Loc.Text("Important Section"))
    .Find(Loc.Css("button.save"))
    .WithTimeout(TimeSpan.FromSeconds(10))
    .Resolve();

client.Click(element);
```

---

## 📊 Files to Create/Modify

### **New Files**

```
Ravelaso.UiPath.Selenium.Core/
├── AnchorLocator.cs           (NEW - main class)
├── Direction.cs               (NEW - enum)
└── AnchorLocatorHelper.cs     (NEW - internal XPath builder)

docs/
└── ANCHOR_SYSTEM.md           (NEW - user guide with API examples)
```

### **Modified Files**

```
Ravelaso.UiPath.Selenium.Core/
├── Locator.cs                 (ADD - Anchor() method)
└── RavelasoWebClient.cs       (ADD - CreateAnchor() helper)

docs/
├── QUICKSTART.md              (UPDATE - add anchor examples)
└── README.md                  (UPDATE - mention anchor system)
```

---

## 📈 Scope: What's Included vs Not

### ✅ **INCLUDED in Phase 5**

- AnchorLocator class (full implementation)
- 8 direction support
- Exhaustive search (default)
- SearchOrder() customization
- ExactDirection() strict mode
- Configurable timeout
- Clear error messages
- User documentation (ANCHOR_SYSTEM.md)
- API examples in docs

### ❌ **NOT INCLUDED (Phase 5+)**

- Unit and integration tests (Phase 5.1 after API validation)
- SmartSelect (choosing best match from multiple)
- Context-aware defaults (InTable, InForm, InList)
- Configurable max distance
- Visual debugging tools
- Performance optimization (caching)
- Custom direction strategies
- Anchor chains (anchor of anchor of anchor)

---

## 🔄 Development Workflow

1. **Create Direction.cs** - Simple enum
2. **Create AnchorLocatorHelper.cs** - XPath building logic
3. **Create AnchorLocator.cs** - Main class with algorithm
4. **Update Locator.cs** - Add `.Anchor()` method
5. **Update RavelasoWebClient.cs** - Add helper method
6. **Document** - ANCHOR_SYSTEM.md with API examples
7. **Update README/QUICKSTART** - Add anchor examples (2-3 real use cases)
8. **Manual testing** - Test with examples in code
9. **Build Release** - `dotnet build -c Release`
10. **Validate API** - Use for a few days to ensure it's robust
11. **Document findings** - If API needs changes, update plan
12. **Plan Phase 5.1** - Tests and any API refinements

---

## 📦 Version & Release

- **Version:** 1.0.2
- **Release Type:** Patch (new feature)
- **Breaking Changes:** None
- **NuGet Package:** Ravelaso.UiPath.Selenium.1.0.2.nupkg

---

## 📝 Documentation Deliverables

1. **docs/ANCHOR_SYSTEM.md** - Complete guide (new file)
2. **docs/QUICKSTART.md** - Add 2-3 anchor examples
3. **README.md** - Update feature list
4. **XML Doc Comments** - All public members

---

## ⏱️ Timeline Estimate

| Task | Hours | Notes |
|---|---|---|
| Direction.cs | 0.5 | Simple enum |
| AnchorLocatorHelper.cs | 1.5 | XPath logic is complex |
| AnchorLocator.cs | 1.5 | Main algorithm |
| Update Locator.cs + Client | 0.5 | Extension methods |
| Documentation (ANCHOR_SYSTEM.md) | 1 | Guide + API examples |
| Update README/QUICKSTART | 0.5 | Add anchor examples |
| Manual testing + validation | 1.5 | Try examples, discover issues |
| Fixes and refinements | 1 | Based on manual testing |
| Build Release | 0.5 | `dotnet build -c Release` |
| **Total** | **8.5 hours** | Estimate |

**Realistic Range:** 6-10 hours (faster without test writing)

---

## ✨ Success Criteria

- [ ] Code compiles with zero warnings
- [ ] API is intuitive and discoverable (IntelliSense works)
- [ ] Error messages are clear and helpful
- [ ] Documentation (ANCHOR_SYSTEM.md) is complete
- [ ] Examples cover main use cases (table, form, list)
- [ ] Code follows existing patterns and style
- [ ] No breaking changes to v1.0.0
- [ ] Manual testing validates core scenarios
- [ ] Ready for Phase 5.1 (tests + refinements)

---

## 🎯 Next Steps

1. Review this plan
2. Approve scope and API
3. Start implementation
4. Create branch: `feature/phase-5-anchor-system`
5. Begin with Direction.cs

---

**Plan Created:** 2026-10-23  
**Status:** Ready for Implementation  
**Approval Required:** Yes
