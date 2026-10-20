# Ravelaso.UiPath.Selenium

> **Selenium WebDriver wrapper for .NET** — Simplifies browser automation with fluent locators, global delays, and UiPath Studio 25.10 compatibility.

**Version:** 0.1.0 | **Status:** ✅ Production Ready

---

## 📦 Quick Install

```bash
dotnet add package Ravelaso.UiPath.Selenium
```

Or use the `.nupkg` from `dist/` folder.

---

## 🚀 Quick Start

```csharp
using Ravelaso.UiPath.Selenium.Core;

var client = new RavelasoWebClient(BrowserType.Edge);

client.Navigate("https://example.com");
client.Click(Loc.Id("button"));
client.Type(Loc.Id("search"), "query");

if (client.Exists(Loc.Id("results")))
    Console.WriteLine(client.GetText(Loc.Id("results")));

client.Dispose();
```

---

## 📚 Documentation

- **[Full API Reference](docs/api.md)** — All 20 methods documented
- **[Locator Builder](docs/locators.md)** — Fluent selector syntax
- **[Quick Start Examples](docs/QUICKSTART.md)** — 8 code samples
- **[Architecture & Design](docs/plan.md)** — Technical decisions
- **[Release Notes](docs/CHANGELOG.md)** — What's new

---

## ✨ Key Features

✅ **14 Client Operations** — Navigate, Click, Type, Select, WaitFor, Screenshot, ExecuteScript, etc.  
✅ **Fluent Locators** — `Loc.Id("x").And.Class("btn").And.Attr("type", "submit")`  
✅ **Global Delay** — 5s default, configurable, applied before each operation  
✅ **Multi-Target** — net8.0 (UiPath Studio 25.10) + net10.0 (development)  
✅ **IntelliSense** — `IWebDriver` interface exposes Selenium API  
✅ **Production Ready** — 30/30 tests passed, 0 errors/warnings  
✅ **No External Dependencies** — Only Selenium.WebDriver 4.51.0 + Selenium.Support 4.51.0

---

## 🏗️ Supported Browsers

- ✅ Chrome
- ✅ Edge
- ✅ Firefox

All with headless mode support and automatic driver management via Selenium Manager.

---

## 📋 Requirements

- **.NET:** 8.0 or 10.0
- **Selenium:** 4.51.0 (included in package)
- **UiPath Studio:** 25.10+ (use net8.0 target)

---

## 💡 Usage Example

```csharp
using Ravelaso.UiPath.Selenium.Core;

// Setup
var options = new BrowserOptions
{
    Browser = BrowserType.Chrome,
    Headless = true,
    GlobalDelay = TimeSpan.FromSeconds(2),
    ElementWaitTimeout = TimeSpan.FromSeconds(10)
};

using var client = new RavelasoWebClient(BrowserType.Chrome, options);

// Navigate
client.Navigate("https://example.com/login");

// Interact
client.Type(Loc.Id("username"), "john");
client.Type(Loc.Id("password"), "secret");
client.Click(Loc.Css("button.submit"));

// Wait & Verify
if (client.WaitFor(Loc.Id("dashboard"), TimeSpan.FromSeconds(5)))
{
    var welcome = client.GetText(Loc.Id("welcome-msg"));
    Console.WriteLine($"Logged in: {welcome}");
}

// Screenshot
var path = client.Screenshot("C:\\screenshots\\login-success.png");
Console.WriteLine($"Screenshot: {path}");

// Cleanup
client.Dispose();
```

---

## 🔗 Resources

- **GitHub:** https://github.com/ravelaso/uipath-selenium
- **NuGet:** https://www.nuget.org/packages/Ravelaso.UiPath.Selenium/
- **Issues:** GitHub Issues

---

## 📄 License

MIT License — See LICENSE file for details.

---

**Built for UiPath Studio 25.10 and .NET automation projects.**
