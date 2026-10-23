# Anchor System

**Find elements relative to a reference element (anchor) using a fluent API.**

Version 1.0.2+

---

## Overview

The Anchor System solves a common problem in web automation: **finding dynamic elements that change position but always maintain the same relationship to a stable reference.**

### The Problem

```
Table changes every day:

Day 1:
┌─────────────┬──────────────┬──────────┐
│ Report Name │ Date         │ Download │
├─────────────┼──────────────┼──────────┤
│ Sales Q4    │ 2026-10-15   │ [↓]      │ ← Need this
│ Inventory   │ 2026-10-16   │ [↓]      │
└─────────────┴──────────────┴──────────┘

Day 2 (rows reordered):
┌─────────────┬──────────────┬──────────┐
│ Report Name │ Date         │ Download │
├─────────────┼──────────────┼──────────┤
│ Inventory   │ 2026-10-16   │ [↓]      │
│ Sales Q4    │ 2026-10-15   │ [↓]      │ ← Still need this
└─────────────┴──────────────┴──────────┘

❌ Without Anchors: Clicking row[0] fails on Day 2
✅ With Anchors: "Click download in row with date 2026-10-15" works both days
```

### The Solution

```csharp
// Find the download button in the row where date = "2026-10-15"
var downloadBtn = Loc.Anchor(client._driver)
    .Find(Loc.Css("button.download"))
    .Resolve();

client.Click(downloadBtn);
```

---

## API

### Basic Usage

```csharp
// 1. Create an anchor with Loc.Anchor()
var anchor = Loc.Anchor(client._driver);

// 2. Set the reference (anchor) element
anchor.Find(Loc.Text("2026-10-15"));

// 3. Set the target element to find
// (use SearchOrder() or let system search all directions)
// ...

// 4. Resolve to get a Locator
var targetLocator = anchor.Resolve();

// 5. Use like any other Locator
client.Click(targetLocator);
```

### Fluent Syntax

```csharp
// Create anchor + find target in one chain
var button = Loc.Anchor(client._driver)
    .Find(Loc.Css("button.download"))
    .Resolve();

client.Click(button);
```

### Using CreateAnchor() Helper

```csharp
// Simpler: client provides its own _driver
var button = client.CreateAnchor(Loc.Text("2026-10-15"))
    .Find(Loc.Css("button"))
    .Resolve();

client.Click(button);
```

---

## Search Directions

The Anchor System searches in **all directions** by default:

```
Direction Search Order (Exhaustive):
1. Right       → following-sibling (same level, next)
2. Left        → preceding-sibling (same level, previous)
3. Below       → following elements (deeper in tree)
4. Above       → preceding elements (higher in tree)
5. SameRow     → ancestor::tr (HTML table row)
6. SameContainer → ancestor::div/section/article
7. Innermost   → descendant (children of anchor)
8. Anywhere    → global search (last resort)
```

If multiple elements match in a direction, **the closest one (by pixel distance) is selected**.

### Custom Search Order

```csharp
// Only try Right, then SameRow
var button = client.CreateAnchor(Loc.Text("Reference"))
    .Find(Loc.Css("button"))
    .SearchOrder(Direction.Right, Direction.SameRow)
    .Resolve();
```

### Exact Direction

```csharp
// Must be to the right, or fail
var button = client.CreateAnchor(Loc.Text("Reference"))
    .Find(Loc.Css("button"))
    .ExactDirection(Direction.Right)
    .Resolve();

// If button not found to right → NoSuchElementException
```

---

## Examples

### Example 1: Table with Dynamic Rows

**HTML:**
```html
<table>
  <tr>
    <td>Sales Report</td>
    <td>2026-10-22</td>
    <td><button class="download">Download</button></td>
  </tr>
  <tr>
    <td>Inventory</td>
    <td>2026-10-23</td>
    <td><button class="download">Download</button></td>
  </tr>
</table>
```

**Code:**
```csharp
// Download report for specific date
var downloadBtn = client.CreateAnchor(Loc.Text("2026-10-22"))
    .Find(Loc.Css("button.download"))
    .Resolve();

client.Click(downloadBtn);
```

**Why it works:**
- Anchor finds the cell with "2026-10-22"
- Searches in same row (ancestor::tr)
- Finds the download button in that row
- Works even if rows are reordered!

---

### Example 2: Form with Labels

**HTML:**
```html
<div class="form-group">
  <label>Email:</label>
  <input type="email" placeholder="Your email">
</div>
<div class="form-group">
  <label>Phone:</label>
  <input type="text" placeholder="Your phone">
</div>
```

**Code:**
```csharp
// Type in email field (by label)
var emailInput = client.CreateAnchor(Loc.Text("Email:"))
    .Find(Loc.Css("input[type='email']"))
    .Resolve();

client.Type(emailInput, "user@example.com");

// Type in phone field (by label)
var phoneInput = client.CreateAnchor(Loc.Text("Phone:"))
    .Find(Loc.Css("input[type='text']"))
    .SearchOrder(Direction.Right, Direction.Below)  // Try right first, then below
    .Resolve();

client.Type(phoneInput, "555-1234");
```

---

### Example 3: List with Actions

**HTML:**
```html
<div class="item">
  <span class="name">Project Alpha</span>
  <button class="edit">Edit</button>
  <button class="delete">Delete</button>
</div>
<div class="item">
  <span class="name">Project Beta</span>
  <button class="edit">Edit</button>
  <button class="delete">Delete</button>
</div>
```

**Code:**
```csharp
// Delete specific project
var deleteBtn = client.CreateAnchor(Loc.Text("Project Alpha"))
    .Find(Loc.Css("button.delete"))
    .Resolve();

client.Click(deleteBtn);
```

---

### Example 4: Complex Layout

**HTML:**
```html
<section class="dashboard">
  <h2>Settings</h2>
  <div class="setting-group">
    <label>Dark Mode:</label>
    <input type="checkbox">
  </div>
</section>
```

**Code:**
```csharp
// Enable dark mode toggle (anchor = label, target = checkbox)
var darkModeToggle = client.CreateAnchor(Loc.Text("Dark Mode:"))
    .Find(Loc.Css("input[type='checkbox']"))
    .ExactDirection(Direction.Right)
    .Resolve();

if (!client.IsChecked(darkModeToggle))
    client.Click(darkModeToggle);
```

---

## Advanced Options

### Custom Timeout

```csharp
// Wait longer for element to appear
var button = client.CreateAnchor(Loc.Text("Reference"))
    .Find(Loc.Css("button"))
    .WithTimeout(TimeSpan.FromSeconds(15))
    .Resolve();
```

### Direct Element Access

```csharp
// Get the actual IWebElement (for advanced Selenium operations)
var element = client.CreateAnchor(Loc.Text("Reference"))
    .Find(Loc.Css("button"))
    .ResolveElement();

// Now you have raw IWebElement access
var tagName = element.TagName;
var text = element.Text;
element.SendKeys("text");
```

---

## Error Handling

### NoSuchElementException

**When anchor is not found:**
```csharp
try
{
    var btn = client.CreateAnchor(Loc.Text("Nonexistent"))
        .Find(Loc.Css("button"))
        .Resolve();
}
catch (NoSuchElementException ex)
{
    Console.WriteLine($"Anchor search failed: {ex.Message}");
    // Shows: "Anchor search failed: could not find target relative to anchor.
    // Anchor: ... Target: ... Directions tried: Right, Left, Below, ..."
}
```

### TimeoutException

**When search exceeds timeout:**
```csharp
try
{
    var btn = client.CreateAnchor(Loc.Text("Reference"))
        .Find(Loc.Css("button"))
        .WithTimeout(TimeSpan.FromSeconds(1))  // Very short timeout
        .Resolve();
}
catch (TimeoutException ex)
{
    Console.WriteLine($"Search timed out: {ex.Message}");
}
```

---

## Tips & Best Practices

### ✅ DO

- **Use stable anchors** — Text that doesn't change between runs
- **Keep anchors close to target** — Same row/container is better than distant
- **Combine with other locators** — `Loc.Text() + Loc.Css()` works well
- **Test early** — Try anchors on different page states to verify stability

### ❌ DON'T

- **Use dynamic text as anchor** — Timestamps, IDs, counts change
- **Rely on position alone** — Use anchors if order changes
- **Mix too many directions** — Keep SearchOrder() short (2-3 max)
- **Forget to handle exceptions** — Anchors can fail, so wrap in try-catch

---

## Performance Notes

- **Exhaustive search** (default) tries all 8 directions
- **Custom SearchOrder()** is faster (fewer attempts)
- **ExactDirection()** is fastest (one attempt only)
- **Distance calculation** uses JavaScript (small overhead)

For repeated operations, use `.SearchOrder()` or `.ExactDirection()` if you know where the target is.

---

## Comparison: Anchor vs Raw XPath

| Aspect | Anchor | Raw XPath |
|--------|--------|-----------|
| **Readability** | `Loc.Anchor(Loc.Text("2026-10-22")).Find(Loc.Css("button"))` | `//tr[contains(., '2026-10-22')]//button` |
| **Flexibility** | Auto-tries all directions | Must know exact path |
| **Maintainability** | Easy to read intent | Hard to understand |
| **Performance** | Slightly slower (JS distance calc) | Faster (direct XPath) |
| **Robustness** | Handles layout changes well | Breaks if structure changes |

**Use Anchors for:** Dynamic content, unclear structure, flexible searches  
**Use Raw XPath for:** Performance-critical, well-defined structure, known positions

---

## See Also

- [QUICKSTART.md](QUICKSTART.md) — 8 code examples including anchors
- [ANCHOR_RESEARCH.md](ANCHOR_RESEARCH.md) — Deep dive into how anchors work
- [api.md](api.md) — Full API reference

---

**Last Updated:** 2026-10-23  
**Version:** 1.0.2+
