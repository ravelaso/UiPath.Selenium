# Quick Start

This guide shows the most common ways to use `Ravelaso.UiPath.Selenium` with the current API. All examples use generic values so they can be adapted to your own application.

## 1. Create a client

### Launch a browser for UiPath attach

Use `BrowserLauncher` to start Chrome or Edge with remote debugging enabled. Then create the normal client using the same `DebuggerAddress`.

```csharp
var options = new BrowserOptions
{
    WebBrowser = WebBrowser.Edge,
    DebuggerAddress = "localhost:9222",
    Maximize = true,
    GlobalDelay = TimeSpan.FromSeconds(1),
    ElementWaitTimeout = TimeSpan.FromSeconds(10)
};

var process = BrowserLauncher.Launch(options);

using var client = new RavelasoWebClient(options);

// UiPath can now attach to the same browser window.
// Use UiPath activities for standard automation.
// Use client for difficult steps such as anchors, downloads, zoom, or JavaScript-heavy interactions.
```

Notes:
- If the debug port is already open, `BrowserLauncher.Launch` returns `null`.
- In UiPath, use **Use Application/Browser** with **Attach to running application**.
- The library and UiPath share the same browser session.
- Keep the browser process alive while both tools are using it.

### Launch a new browser

```csharp
using Ravelaso.UiPath.Selenium.Core;

var options = new BrowserOptions
{
    Maximize = true,
    Headless = false,
    GlobalDelay = TimeSpan.FromSeconds(1),
    ElementWaitTimeout = TimeSpan.FromSeconds(10)
};

using var client = new RavelasoWebClient(WebBrowser.Edge, options);
```

### Attach to an already running browser

```csharp
var options = new BrowserOptions
{
    DebuggerAddress = "localhost:9222",
    GlobalDelay = TimeSpan.FromSeconds(1),
    ElementWaitTimeout = TimeSpan.FromSeconds(10)
};

using var client = new RavelasoWebClient(WebBrowser.Edge, options);
```

Attaching is useful when the browser is already logged in or controlled by another tool.

## 2. Basic locators

```csharp
client.Navigate("https://app.example.com");

// Simple locators
client.Click(Loc.Id("login-button"));
client.Type(Loc.Id("username"), "user@example.com");
client.Type(Loc.Id("password"), "password");

// Chained conditions
client.Click(Loc.Tag("button").And.Class("primary").And.Attr("type", "submit"));

// Text-based locator
client.Click(Loc.Tag("a").Text("Sign in"));
```

`Locator` implicitly converts to Selenium `By`, so it can be used directly with client methods.

## 3. Waits and conditional checks

```csharp
// Wait up to 10 seconds for an element to appear
bool loaded = client.WaitFor(Loc.Tag("h1").Text("Dashboard"), TimeSpan.FromSeconds(10));

if (!loaded)
    throw new TimeoutException("Dashboard did not load.");

// Immediate existence check
bool hasError = client.Exists(Loc.Id("error-panel"), null, TimeSpan.Zero);

// Check visibility
bool isVisible = client.IsDisplayed(Loc.Id("submit-button"), null, TimeSpan.FromSeconds(5));
```

## 4. Anchors

Anchors find an element relative to a stable reference element. This is useful for dynamic tables, grids, and forms.

### Single anchor

```csharp
var title = Loc.Anchor(Loc.Class("tasks-container-title").Tag("div"))
    .Find(Loc.Tag("h1").Class("ng-binding"));

client.Click(title);
```

### Chained anchors

Use multiple anchors when one reference is not enough.

```csharp
var invoiceValue = Loc.Anchor(Loc.Text("Account 12345"))
    .Anchor(Loc.Text("Invoice 98765"))
    .Find(Loc.Text("-1,234.56"));

client.DoubleClick(invoiceValue);
```

### Wait for an anchored element

`WaitFor` and `Exists` accept an `AnchorLocator`, so you do not need to call `Resolve()` first.

```csharp
bool appeared = client.WaitFor(
    Loc.Anchor(Loc.Tag("div").Class("notification-area"))
        .Find(Loc.Tag("a").Class("download-link").Text("Open download center")),
    TimeSpan.FromMinutes(2));

if (!appeared)
    throw new TimeoutException("Notification did not appear.");
```

### Timeout for slow pages

```csharp
var element = Loc.Anchor(Loc.Text("Account 12345"))
    .Find(Loc.Tag("button").Class("action-button"))
    .WithTimeout(TimeSpan.FromMinutes(2));
```

## 5. Dynamic table example

This pattern is useful when rows move or change every day.

```csharp
client.Navigate("https://app.example.com/reports");

// Select the download button in the row containing both values.
var downloadButton = Loc.Anchor(Loc.Text("Account 12345"))
    .Anchor(Loc.Text("02/11/2026 02:02"))
    .Find(Loc.Tag("button").Class("download-button"));

client.Click(downloadButton);
```

The anchor system searches the row containing both reference values and resolves the matching target.

## 6. Clicking difficult elements

### JavaScript click

```csharp
client.ClickJs(Loc.Tag("button").Attr("data-action", "submit"));
```

### Hidden or intercepted element

```csharp
var button = Loc.Tag("button").Attr("data-t-id", "clear-filter");
var container = Loc.Tag("div").Attr("data-t-id", "filter-panel");

client.ClickHidden(button, container);
```

## 7. Forms and filters

```csharp
client.Click(Loc.Tag("button").Class("advanced-search").Text("Advanced search"));

client.Type(Loc.Tag("input").Id("startDate"), "01/10/2026");
client.Type(Loc.Tag("input").Id("endDate"), "31/10/2026");

client.Click(Loc.Tag("button").Class("search-button").Text("Search"));
```

## 8. Zooming wide tables

```csharp
// Zoom out to fit more columns into the viewport
client.SetZoom(0.8);

// Reset to normal zoom
client.ResetZoom();
```

`SetZoom` and `ResetZoom` apply a short delay by default so the page can re-layout before the next action.

## 9. Scrolling

```csharp
using Ravelaso.UiPath.Selenium.Core;

// Scroll by pixels (auto-detects the best scrollable container)
client.Scroll(ScrollDirection.Down, 500);
client.Scroll(ScrollDirection.Right, 300);

// Scroll a specific container
client.Scroll(ScrollDirection.Right, 600, Loc.Css("div.table-wrapper"));

// Scroll until an element is visible (throws NoSuchElementException on timeout)
client.ScrollTo(ScrollDirection.Down, Loc.Id("targetRow"), timeout: TimeSpan.FromSeconds(10));

// Inspect scrollable containers
foreach (var s in client.GetScrollables())
    Console.WriteLine($"{s.Index}: {s.Tag} {s.ClassName} {s.ClientWidth}x{s.ClientHeight}");
```

## 10. Extracting tables

```csharp
var options = new TableExtractionOptions()
    .WithHasHeader(false)
    .Column("Date", typeof(DateTime))
    .Column("Camera", typeof(string))
    .Column("Starting", typeof(int))
    .Column("Ending", typeof(int))
    .Column("Batch", typeof(int));

var data = client.ExtractTable(Loc.XPath("/html/body/table[2]"), options);

for (int i = 3; i <= 10; i++)
    client.ExtractTable(Loc.XPath($"/html/body/table[{i}]"), data, options);

Console.WriteLine($"Total rows: {data.Rows.Count}");
```

Tables can also be appended into a pre-built `DataTable` (e.g. from UiPath "Build Data Table").
## 11. Downloads

`DownloadFile` downloads a file triggered by an element and saves it to the final destination path.

```csharp
var downloadButton = Loc.Anchor(Loc.Text("Account 12345"))
    .Find(Loc.Tag("button").Class("download-button"));

string savedPath = client.DownloadFile(
    downloadButton,
    @"C:\Automation\report.xlsx",
    null,
    TimeSpan.FromMinutes(2));

Console.WriteLine($"Downloaded: {savedPath}");
```

You can also download from a direct URL:

```csharp
client.DownloadFromUrl(
    "https://example.com/files/report.xlsx",
    @"C:\Automation\report.xlsx");
```

The library first tries a direct HTTP download using browser cookies. If no URL is available, it uses a runtime Chromium/Edge download fallback.

## 12. Screenshots

```csharp
string screenshotPath = client.Screenshot(@"C:\Automation\screenshot.png");
Console.WriteLine(screenshotPath);
```

## 13. Shadow DOM

The client automatically searches light DOM and open Shadow DOM roots for supported operations.

```csharp
client.Click(Loc.Tag("button").Attr("data-testid", "shadow-submit"));
```

## 14. Best practices

- Prefer `Loc.Text(...)` or chained `Loc` conditions over fragile XPath.
- Use anchors for dynamic tables instead of row indexes.
- Use `Exists` or `WaitFor` before conditional flows.
- Use `WithTimeout` for slow or asynchronous pages.
- Use `SetZoom` before resolving anchors on very wide tables.
- Keep download destinations explicit and outside browser profile folders.
- Use `ClickJs` or `ClickHidden` only when normal clicks are intercepted.

## 15. Common troubleshooting

### Anchor throws `NoSuchElementException`
- Check that the anchor text exactly matches the rendered text.
- Try `Loc.Text(...)` instead of `Loc.Tag("span").Text(...)` when the tag varies.
- Increase the timeout with `WithTimeout`.
- For multi-anchor chains, ensure all reference values are in the same row or container.

### Download fails
- If the button has no URL, the library uses CDP fallback on Chromium/Edge.
- Make sure the destination directory can be created.
- Increase the download timeout for large files.
- Firefox supports direct-URL downloads only.

### Element is not found
- Increase `ElementWaitTimeout`.
- Check whether the element is inside a Shadow DOM or iframe.
- Verify the locator with `Exists` before clicking.
