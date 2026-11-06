# Ravelaso.UiPath.Selenium

![Publish Package](https://github.com/ravelaso/UiPath.Selenium/actions/workflows/publish-nuget-package.yml/badge.svg)

Selenium WebDriver wrapper for .NET that simplifies web automation in UiPath Studio 25.10 and .NET projects.

THIS IS WORK IN PROGRESS! Please do not take it seriously for now until a stable release is ready. API may change!

---

## What is it?

Automate web browser interactions from UiPath or .NET:

- Navigate to URLs
- Click, type, and interact with elements
- Wait for dynamic elements
- Find elements relative to anchors
- Download files
- Take screenshots
- Zoom pages for wide tables
- Scroll pages and tables (wheel/CDP)
- Extract HTML tables to DataTable
- Work with Shadow DOM

---

## Documentation

- [Quick Start Examples](docs/QUICKSTART.md) — Code examples

---

## Installation

```bash
dotnet build -c Release
```

Copy the `.nupkg` from the `dist/` folder to your NuGet feed.

Use the provided Package release in the Github Releases page until it gets published to NuGet.

---

## Usage in UiPath Studio 25.10

In a **.NET Invoke Code** activity:

```csharp
using Ravelaso.UiPath.Selenium.Core;

// Create client
var options = new BrowserOptions
{
    Maximize = true,
    GlobalDelay = TimeSpan.FromSeconds(1),
    ElementWaitTimeout = TimeSpan.FromSeconds(10)
};

using var client = new RavelasoWebClient(WebBrowser.Edge, options);

// Navigate
client.Navigate("https://example.com");

// Interact
client.Type(Loc.Id("search"), "my search");
client.Click(Loc.Css("button.search-btn"));

// Wait and get result
if (client.WaitFor(Loc.Id("results"), null, TimeSpan.FromSeconds(10)))
{
    var result = client.GetText(Loc.Id("results"));
    Console.WriteLine(result);
}

// Cleanup
client.Dispose();
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

---

## Launching a browser for UiPath attach

Use `BrowserLauncher` to start a Chromium-based browser with remote debugging enabled. Then create the normal client using the same `DebuggerAddress`.

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

---

## Main API

### Navigation and page state

- `Navigate(url)` — Go to URL
- `GetUrl()` — Get current URL
- `GetTitle()` — Get page title
- `GetPageSource()` — Get HTML source
- `ExecuteScript(script, args)` — Execute JavaScript

### Interaction

- `Click(locator)` — Click element
- `Click(locator, timeout)` — Click with timeout
- `ClickJs(locator)` — Click using JavaScript
- `ClickHidden(locator, hoverTarget)` — Click hidden or intercepted element
- `DoubleClick(locator)` — Double-click element
- `Type(locator, text)` — Type in input
- `Select(locator, option)` — Select dropdown option

### Reading data

- `GetText(locator)` — Get element text
- `GetAttribute(locator, attr)` — Get attribute value

### Waits and checks

- `WaitFor(locator, timeout)` — Wait for element to appear
- `WaitFor(anchor, timeout)` — Wait for an anchored element to appear
- `Exists(locator)` — Check if element exists
- `Exists(locator, timeout)` — Check if element appears within timeout
- `Exists(anchor, timeout)` — Check if an anchored element appears
- `IsDisplayed(locator, timeout)` — Check if element is displayed

### Anchors

- `Loc.Anchor(locator)` — Create an anchor locator
- `AnchorLocator.Anchor(anchor)` — Chain another anchor
- `AnchorLocator.Find(target)` — Define target relative to anchor
- `AnchorLocator.Resolve()` — Resolve to a locator
- `AnchorLocator.Resolve(client)` — Resolve using a client
- `AnchorLocator.WithTimeout(timeout)` — Set anchor timeout
- `AnchorLocator.WaitFor(...)` — Wait for anchored element
- `AnchorLocator.Exists(...)` — Check anchored element

### Downloads

- `DownloadFile(downloadButton, destinationPath, beforeStepDelay, timeout)` — Download file to final path
- `DownloadFromUrl(url, destinationPath, timeout)` — Download from direct URL
- `TryGetDownloadUrl(locator)` — Extract a download URL from an element

### Page helpers

- `Screenshot(path)` — Take screenshot and return path
- `SetZoom(percent, beforeStepDelay, timeout)` — Set browser zoom, where `1.0` is 100%. Applies a default 3 s delay after zooming to let the page re-layout.
- `ResetZoom(beforeStepDelay, timeout)` — Reset browser zoom to 100%. Applies the same default delay.
- `Scroll(direction, amount)` — Scroll by pixels (auto-detects the best scrollable container)
- `Scroll(direction, amount, container)` — Scroll a specific container
- `ScrollTo(direction, target)` — Scroll until the element is visible (throws `NoSuchElementException` on timeout)
- `ScrollTo(direction, target, container)` — Scroll until visible inside a specific container
- `GetScrollables()` — List scrollable containers for inspection

---

## Locators

Instead of writing XPath manually:

```csharp
// Simple
Loc.Id("element")
Loc.Css(".class")
Loc.Tag("button")

// Combine conditions
Loc.Id("button").And.Class("primary")
Loc.Css("input").And.Attr("type", "text")

// By text
Loc.Text("Click me")

// Multiple CSS classes
Loc.Tag("button").Class("btn primary")

// Attributes
Loc.Attr("data-testid", "submit-button")
```

---

## Anchors

Anchors find elements relative to stable reference elements. This is useful for dynamic tables and grids.

```csharp
var downloadButton = Loc.Anchor(Loc.Text("Account 12345"))
    .Anchor(Loc.Text("02/11/2026 02:02"))
    .Find(Loc.Tag("button").Class("download-button"));

client.Click(downloadButton);
```

Wait for an anchored element without calling `Resolve()` first:

```csharp
bool appeared = client.WaitFor(
    Loc.Anchor(Loc.Tag("div").Class("notification-area"))
        .Find(Loc.Tag("a").Text("Open download center")),
    TimeSpan.FromMinutes(2));
```

---

## Downloads

```csharp
var downloadButton = Loc.Anchor(Loc.Text("Account 12345"))
    .Find(Loc.Tag("button").Class("download-button"));

string savedPath = client.DownloadFile(
    downloadButton,
    @"C:\Automation\report.xlsx",
    null,
    TimeSpan.FromMinutes(2));
```

The library first tries a direct HTTP download using browser cookies. If no URL is available, it uses a runtime Chromium/Edge download fallback and moves the completed file to the requested destination.

---

## Scrolling

Scroll by pixels or until a target becomes visible. Auto-detects the best scrollable container for the direction (prefers the content viewport over scrollbar tracks); falls back to the page scroller when none is found. Uses wheel events (CDP on Chromium, then Selenium Actions, then JavaScript).

```csharp
using Ravelaso.UiPath.Selenium.Core;

// Scroll by pixels (auto-detect container)
client.Scroll(ScrollDirection.Down, 500);
client.Scroll(ScrollDirection.Right, 300);

// Scroll a specific container
client.Scroll(ScrollDirection.Right, 600, Loc.Css("div.table-wrapper"));

// Scroll until an element is visible (throws NoSuchElementException on timeout)
client.ScrollTo(ScrollDirection.Down, Loc.Id("targetRow"), timeout: TimeSpan.FromSeconds(10));

// Scroll until an element is visible inside a specific container
client.ScrollTo(ScrollDirection.Down, Loc.Id("targetRow"), Loc.Css("div.table-wrapper"));

// Inspect scrollable containers
foreach (var s in client.GetScrollables())
    Console.WriteLine($"{s.Index}: {s.Tag} {s.ClassName} {s.ClientWidth}x{s.ClientHeight} scroll {s.ScrollWidth}x{s.ScrollHeight}");
```

---

## Tables

Extract an HTML table into a `System.Data.DataTable`. Handles `thead`/`tbody`/`tfoot`, nested tags (`<font>`, `<a>`, …), empty cells, and typed columns.

```csharp
// No header row in the table; define columns explicitly
var leicaOptions = new TableExtractionOptions()
    .WithHasHeader(false)
    .Column("Date", typeof(DateTime))
    .Column("Camera", typeof(string))
    .Column("Starting", typeof(int))
    .Column("Ending", typeof(int))
    .Column("Batch", typeof(int));

var leicaData = client.ExtractTable(Loc.XPath("/html/body/table[2]"), leicaOptions);

for (int i = 3; i <= 10; i++)
    client.ExtractTable(Loc.XPath($"/html/body/table[{i}]"), leicaData, leicaOptions);

Console.WriteLine($"Total rows: {leicaData.Rows.Count}");
```

Header-based extraction:

```csharp
var table = client.ExtractTable(Loc.Css("table.mdctable"),
    new TableExtractionOptions().WithHasHeader(true));
```

Append into a pre-built `DataTable` (e.g. UiPath "Build Data Table"):

```csharp
var myTable = new DataTable();
myTable.Columns.Add("Date", typeof(DateTime));
myTable.Columns.Add("Camera", typeof(string));

client.ExtractTable(Loc.Css("table"), myTable,
    new TableExtractionOptions { HasHeader = false });
```

---


## Configuration

```csharp
var options = new BrowserOptions
{
    Maximize = true,                              // Maximize window on startup
    Headless = false,                             // Run without UI
    GlobalDelay = TimeSpan.FromSeconds(1),        // Delay before each action
    ElementWaitTimeout = TimeSpan.FromSeconds(10), // Default wait timeout
    PageLoadTimeout = TimeSpan.FromSeconds(60),   // Page load timeout
    DebuggerAddress = "localhost:9222"            // Attach to existing browser
};

var client = new RavelasoWebClient(WebBrowser.Edge, options);
```

Supported browsers: Chrome, Edge (Firefox, limited support)

---

### Version Management

Edit `Version.props` to bump version:

```xml
<VersionMajor>1</VersionMajor>
<VersionMinor>0</VersionMinor>
<VersionPatch>2</VersionPatch>
```

Then rebuild: `dotnet build -c Release`

---

### Debugging Attach

Use this to launch an Edge browser with remote debug enabled.
```bash
& "C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe" --remote-debugging-port=9222 --user-data-dir="C:\Temp\EdgeDebug"
```

## License

MIT
