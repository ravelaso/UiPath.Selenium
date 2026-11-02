# Quick Start

This guide shows the most common ways to use `Ravelaso.UiPath.Selenium` with the current API. All examples use generic values so they can be adapted to your own application.

## 1. Create a client

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
bool hasError = client.Exists(Loc.Id("error-panel"), TimeSpan.Zero);

// Check visibility
bool isVisible = client.IsDisplayed(Loc.Id("submit-button"), TimeSpan.FromSeconds(5));
```

## 4. Anchors

Anchors find an element relative to a stable reference element. This is useful for dynamic tables, grids, and forms.

### Single anchor

```csharp
var title = client
    .CreateAnchor(Loc.Class("tasks-container-title").Tag("div"))
    .Find(Loc.Tag("h1").Class("ng-binding"))
    .Resolve();

client.Click(title);
```

### Chained anchors

Use multiple anchors when one reference is not enough.

```csharp
var invoiceValue = client
    .CreateAnchor(Loc.Text("Account 12345"))
    .CreateAnchor(Loc.Text("Invoice 98765"))
    .Find(Loc.Text("-1,234.56"))
    .Resolve();

client.DoubleClick(invoiceValue);
```

### Wait for an anchored element

`WaitFor` and `Exists` accept an `AnchorLocator`, so you do not need to call `Resolve()` first.

```csharp
bool appeared = client.WaitFor(
    client
        .CreateAnchor(Loc.Tag("div").Class("notification-area"))
        .Find(Loc.Tag("a").Class("download-link").Text("Open download center")),
    TimeSpan.FromMinutes(2));

if (!appeared)
    throw new TimeoutException("Notification did not appear.");
```

### Timeout for slow pages

```csharp
var element = client
    .CreateAnchor(Loc.Text("Account 12345"))
    .Find(Loc.Tag("button").Class("action-button"))
    .WithTimeout(TimeSpan.FromMinutes(2))
    .Resolve();
```

## 5. Dynamic table example

This pattern is useful when rows move or change every day.

```csharp
client.Navigate("https://app.example.com/reports");

// Select the download button in the row containing both values.
var downloadButton = client
    .CreateAnchor(Loc.Text("Account 12345"))
    .CreateAnchor(Loc.Text("02/11/2026 02:02"))
    .Find(Loc.Tag("button").Class("download-button"))
    .Resolve();

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

Zoom can help anchors and tables that load content based on the visible viewport.

## 9. Downloads

`DownloadFile` downloads a file triggered by an element and saves it to the final destination path.

```csharp
var downloadButton = client
    .CreateAnchor(Loc.Text("Account 12345"))
    .Find(Loc.Tag("button").Class("download-button"))
    .Resolve();

string savedPath = client.DownloadFile(
    downloadButton,
    @"C:\Automation\report.xlsx",
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

## 10. Screenshots

```csharp
string screenshotPath = client.Screenshot(@"C:\Automation\screenshot.png");
Console.WriteLine(screenshotPath);
```

## 11. Shadow DOM

The client automatically searches light DOM and open Shadow DOM roots for supported operations.

```csharp
client.Click(Loc.Tag("button").Attr("data-testid", "shadow-submit"));
```

## 12. Best practices

- Prefer `Loc.Text(...)` or chained `Loc` conditions over fragile XPath.
- Use anchors for dynamic tables instead of row indexes.
- Use `Exists` or `WaitFor` before conditional flows.
- Use `WithTimeout` for slow or asynchronous pages.
- Use `SetZoom` before resolving anchors on very wide tables.
- Keep download destinations explicit and outside browser profile folders.
- Use `ClickJs` or `ClickHidden` only when normal clicks are intercepted.

## 13. Common troubleshooting

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