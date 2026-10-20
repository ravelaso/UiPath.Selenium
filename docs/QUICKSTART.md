# Quick Start Guide

Ready-to-use examples for `Ravelaso.UiPath.Selenium`.

---

## Installation

Add to your `.csproj`:

```xml
<ItemGroup>
  <ProjectReference Include="path/to/Ravelaso.UiPath.Selenium.Core.csproj" />
</ItemGroup>
```

Or reference the compiled NuGet (when published).

---

## Example 1: Simple Navigation & Form Fill

```csharp
using Ravelaso.UiPath.Selenium.Core;

var client = new RavelasoWebClient(BrowserType.Chrome);

try
{
    // Navigate
    client.Navigate("https://example.com/login");
    Console.WriteLine($"Title: {client.GetTitle()}");

    // Fill form
    if (client.Exists(Loc.Id("username")))
    {
        client.Type(Loc.Id("username"), "john.doe");
        client.Type(Loc.Id("password"), "secret123", clear: true);
        
        // Click submit
        client.Click(Loc.Css("button.submit"));
        
        // Wait for result
        if (client.WaitFor(Loc.Id("success-message"), TimeSpan.FromSeconds(10)))
            Console.WriteLine(client.GetText(Loc.Id("success-message")));
    }
}
finally
{
    client.Dispose();
}
```

---

## Example 2: Advanced Locators

```csharp
using Ravelaso.UiPath.Selenium.Core;

var client = new RavelasoWebClient(BrowserType.Edge, new BrowserOptions 
{ 
    Headless = true,
    GlobalDelay = TimeSpan.FromSeconds(1)
});

try
{
    client.Navigate("https://example.com");

    // Single condition → native selector
    var username = Loc.Id("username");

    // Chained conditions → composite selector
    var submitBtn = Loc.Tag("button").And.Class("primary").And.Attr("type", "submit");

    // XPath mode
    var resultDiv = Loc.XPath("//div[@role='alert']").And.Class("success");

    // Dynamic locators (from config, etc.)
    var locatorType = LocatorType.Id;
    var elementId = "search-box";
    var searchBox = Loc.Of(locatorType, elementId);

    // Check existence
    if (client.Exists(searchBox))
    {
        client.Type(searchBox, "selenium");
        client.Click(submitBtn);
    }
}
finally
{
    client.Dispose();
}
```

---

## Example 3: Dropdown Selection

```csharp
using Ravelaso.UiPath.Selenium.Core;

var client = new RavelasoWebClient(BrowserType.Firefox);

try
{
    client.Navigate("https://example.com/form");

    // Select by value attribute
    var selected1 = client.Select(Loc.Id("country"), "mx");
    Console.WriteLine($"Selected by value 'mx': {selected1}");

    // Select by visible text
    var selected2 = client.Select(Loc.Id("country"), "Mexico");
    Console.WriteLine($"Selected by text 'Mexico': {selected2}");

    // Select by index
    var selected3 = client.SelectByIndex(Loc.Id("country"), 2);
    Console.WriteLine($"Selected by index 2: {selected3}");

    // Get current value
    var currentValue = client.GetAttribute(Loc.Id("country"), "value");
    Console.WriteLine($"Current country: {currentValue}");
}
finally
{
    client.Dispose();
}
```

---

## Example 4: Screenshots & JavaScript

```csharp
using Ravelaso.UiPath.Selenium.Core;

var client = new RavelasoWebClient(BrowserType.Chrome);

try
{
    client.Navigate("https://example.com");

    // Take screenshot
    var screenshotPath = client.Screenshot("C:\\screenshots\\page.png");
    Console.WriteLine($"Screenshot saved: {screenshotPath}");

    // Execute JavaScript
    var scrollHeight = client.ExecuteScript("return document.body.scrollHeight;");
    Console.WriteLine($"Page height: {scrollHeight}");

    // Execute JavaScript with arguments
    var elementCount = client.ExecuteScript(
        "return document.querySelectorAll(arguments[0]).length;",
        ".product-item"
    );
    Console.WriteLine($"Product items: {elementCount}");

    // Get page HTML
    var html = client.GetPageSource();
    Console.WriteLine($"HTML length: {html.Length}");
}
finally
{
    client.Dispose();
}
```

---

## Example 5: Conditional Waits

```csharp
using Ravelaso.UiPath.Selenium.Core;

var client = new RavelasoWebClient(BrowserType.Chrome);

try
{
    client.Navigate("https://example.com");

    // Wait for element to appear (default timeout: 10s)
    if (client.WaitFor(Loc.Id("loading-spinner")))
    {
        Console.WriteLine("Spinner appeared");
    }

    // Custom timeout
    var resultAppeared = client.WaitFor(
        Loc.Id("search-results"), 
        TimeSpan.FromSeconds(30)
    );

    if (resultAppeared)
    {
        var resultText = client.GetText(Loc.Id("search-results"));
        Console.WriteLine($"Result: {resultText}");
    }
    else
    {
        Console.WriteLine("Results did not appear in time");
    }

    // Check if element is displayed (visible)
    if (client.IsDisplayed(Loc.Id("success-badge")))
    {
        Console.WriteLine("Success badge is visible");
    }
}
finally
{
    client.Dispose();
}
```

---

## Example 6: Headless Browser with Custom Options

```csharp
using Ravelaso.UiPath.Selenium.Core;

var options = new BrowserOptions
{
    Browser = BrowserType.Chrome,
    Headless = true,
    Maximize = false,
    WindowWidth = 1280,
    WindowHeight = 720,
    GlobalDelay = TimeSpan.FromMilliseconds(500),
    ElementWaitTimeout = TimeSpan.FromSeconds(15),
    PageLoadTimeout = TimeSpan.FromSeconds(60)
};

options.ExtraArguments.Add("--no-sandbox");
options.ExtraArguments.Add("--disable-dev-shm-usage");

var client = new RavelasoWebClient(BrowserType.Chrome, options);

try
{
    client.Navigate("https://example.com");

    // Temporarily disable global delay
    var originalDelay = client.GlobalDelay;
    client.GlobalDelay = TimeSpan.Zero;
    
    // Fast operations
    client.Type(Loc.Id("search"), "query");
    
    // Re-enable delay
    client.GlobalDelay = originalDelay;
    
    client.Click(Loc.Css("button.search"));
}
finally
{
    client.Dispose();
}
```

---

## Example 7: Error Handling

```csharp
using Ravelaso.UiPath.Selenium.Core;

var client = new RavelasoWebClient(BrowserType.Edge);

try
{
    client.Navigate("https://example.com");

    // Safe existence check
    if (!client.Exists(Loc.Id("error-message")))
    {
        Console.WriteLine("No error message found");
    }

    // Safe text retrieval
    var text = client.Exists(Loc.Id("result")) 
        ? client.GetText(Loc.Id("result")) 
        : "Not found";

    // Safe attribute access
    var href = client.GetAttribute(Loc.Css("a.link"), "href") ?? "No href";

    // Safe timeout handling
    if (!client.WaitFor(Loc.Id("expensive-element"), TimeSpan.FromSeconds(5)))
    {
        Console.WriteLine("Element did not load; proceeding");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}
finally
{
    try { client.Dispose(); }
    catch { /* Ignore errors during cleanup */ }
}
```

---

## Example 8: UiPath Integration (Pseudo-code)

```csharp
// In a UiPath .NET Invoke Code activity:

using Ravelaso.UiPath.Selenium.Core;

// Get browser type from UiPath variable (string)
var browserInput = in_BrowserType; // "Chrome", "Edge", "Firefox"
var browser = (BrowserType)Enum.Parse(typeof(BrowserType), browserInput);

// Create client
var client = new RavelasoWebClient(browser);

try
{
    // Get URL from UiPath variable
    client.Navigate(in_URL);
    
    // Use locators from UiPath variables
    client.Type(Loc.Id(in_UsernameFieldId), in_Username);
    client.Type(Loc.Id(in_PasswordFieldId), in_Password);
    client.Click(Loc.Id(in_SubmitButtonId));
    
    // Wait and capture result
    if (client.WaitFor(Loc.Id(in_ResultElementId), TimeSpan.FromSeconds(10)))
    {
        out_ResultText = client.GetText(Loc.Id(in_ResultElementId));
        out_Success = true;
    }
    else
    {
        out_Success = false;
    }
}
catch (Exception ex)
{
    out_Error = ex.Message;
    out_Success = false;
}
finally
{
    client.Dispose();
}
```

---

## Tips

1. **Always use `using` or `finally` to dispose the client** to close the browser properly.

2. **Configure `GlobalDelay` sensibly**: 
   - Development/debugging: `0s` or `1s`
   - Production/CI: `2-5s` depending on page speed

3. **Use `Exists(locator, timeout)` for uncertain elements**, but `Exists(locator)` for known elements (faster).

4. **Combine `And` for robust locators**: `Loc.Tag("button").And.Class("submit").And.Attr("type", "button")`

5. **Test with headless** (`Headless = true`) in CI/automation environments.

6. **Inspect the page** to get correct selectors:
   - Right-click → Inspect → find `id`, `class`, `data-*` attributes
   - Use CSS selectors when possible (faster than XPath)

---

## Troubleshooting

### Element not found despite existing on page
- Increase `ElementWaitTimeout` or use `WaitFor(locator, largerTimeout)`
- Check if element is inside an iframe (not supported yet)
- Check if element is inside a shadow DOM (not supported yet)

### Global delay seems too slow
- Set `GlobalDelay = TimeSpan.Zero` if you're using `WaitFor()` exclusively
- The delay is intentional for robustness in real-world RPA

### Screenshot save fails
- Ensure directory exists; the client creates it automatically
- Check file permissions
- Use absolute paths

### Headless mode not working
- Firefox: ensure geckodriver is in PATH
- Chrome/Edge: `--headless=new` flag is set automatically
- Test without headless first to isolate the issue

---

For more details, see:
- [`api.md`](./api.md) — Full API reference
- [`locators.md`](./locators.md) — Locator builder rules
- [`plan.md`](./plan.md) — Architecture & design decisions
