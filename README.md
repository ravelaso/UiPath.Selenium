# Ravelaso.UiPath.Selenium

Selenium WebDriver wrapper for .NET that simplifies web automation in UiPath Studio 25.10 and .NET projects.

---

## What is it?

Automate web browser interactions from UiPath or .NET:
- Navigate to URLs
- Click elements
- Fill forms
- Extract data
- Wait for dynamic elements
- Take screenshots

---

## Installation

```bash
dotnet add package Ravelaso.UiPath.Selenium
```

Or copy the `.nupkg` from the `dist/` folder to your NuGet feed.

---

## Usage in UiPath Studio 25.10

In a **.NET Invoke Code** activity:

```csharp
using Ravelaso.UiPath.Selenium.Core;

// Create client
var client = new RavelasoWebClient(BrowserType.Edge);

// Navigate
client.Navigate("https://example.com");

// Interact
client.Type(Loc.Id("search"), "my search");
client.Click(Loc.Css("button.search-btn"));

// Wait and get result
if (client.WaitFor(Loc.Id("results"), TimeSpan.FromSeconds(10)))
{
    var result = client.GetText(Loc.Id("results"));
    Console.WriteLine(result);
}

// Cleanup
client.Dispose();
```

---

## Main API

- `Navigate(url)` — Go to URL
- `Click(locator)` — Click element
- `Type(locator, text)` — Type in input
- `Select(locator, option)` — Select dropdown option
- `GetText(locator)` — Get element text
- `GetAttribute(locator, attr)` — Get attribute value
- `WaitFor(locator, timeout)` — Wait for element to appear
- `Exists(locator)` — Check if element exists
- `Screenshot(path)` — Take screenshot
- `ExecuteScript(script)` — Execute JavaScript

---

## Locators

Instead of writing XPath manually:

```csharp
// Simple
Loc.Id("element")
Loc.Css(".class")

// Combine conditions
Loc.Id("button").And.Class("primary")
Loc.Css("input").And.Attr("type", "text")

// By text
Loc.Text("Click me")
```

---

## Configuration

```csharp
var options = new BrowserOptions
{
    Headless = true,                              // Run without UI
    GlobalDelay = TimeSpan.FromSeconds(3)         // Delay before each action
};

var client = new RavelasoWebClient(BrowserType.Chrome, options);
```

Supported browsers: Chrome, Edge, Firefox

---

## Documentation

- [Quick Start Examples](docs/QUICKSTART.md) — Code examples
- [Anchor System](docs/ANCHOR_SYSTEM.md) — Find elements relative to references
- [Anchor Research](docs/ANCHOR_RESEARCH.md) — How anchors work (deep dive)

---

## Deployment & Packaging

### Release Build (Automated)

```bash
dotnet build -c Release
# Output: dist/Ravelaso.UiPath.Selenium.1.0.2.nupkg (7.8 MB)
```

- Generates `.nupkg` automatically
- Version: `1.0.2` (fixed, from Version.props)
- Includes: `selenium-manager.exe`, all dependencies
- Ready for distribution

### Debug Build (Manual)

```bash
dotnet build -c Debug
dotnet pack -c Debug
# Output: dist/Ravelaso.UiPath.Selenium.1.0.2-dev.20261023.XXXXX.nupkg
```

- Requires manual `dotnet pack`
- Version: `1.0.2-dev.20261023.XXXXX` (auto-incremented)
- For local testing only

### Version Management

Edit `Version.props` to bump version:

```xml
<VersionMajor>1</VersionMajor>
<VersionMinor>0</VersionMinor>
<VersionPatch>2</VersionPatch>
```

Then rebuild: `dotnet build -c Release`

### Installation Methods

**Method 1: NuGet Package (Recommended)**
```bash
# Add to your project
dotnet add package Ravelaso.UiPath.Selenium
```

**Method 2: Local File**
```bash
# Extract .nupkg and copy DLLs manually to your project Libraries folder
```

**Method 3: UiPath Studio**
- Settings → NuGet Packages → Add Source
- Point to folder containing `.nupkg`
- Install from Studio UI

---

## License

MIT
