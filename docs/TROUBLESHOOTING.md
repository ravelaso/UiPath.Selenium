# Troubleshooting Guide

Common issues and solutions when using Ravelaso.UiPath.Selenium.

---

## Runtime Errors

### "Selenium Manager binary 'selenium-manager.exe' was not found"

**Error Message:**
```
OpenQA.Selenium.ManagerException: Selenium Manager binary 'selenium-manager.exe' was not found 
in the following paths...
```

**Causes:**
1. Package not properly restored (missing `selenium-manager.exe` in NuGet)
2. Old Selenium version (<4.51.0) without Manager
3. UiPath Studio missing Selenium Manager globally

**Solutions:**

**Option 1: Clean NuGet Cache & Restore (Recommended)**
```bash
# Clear cache
dotnet nuget locals all --clear

# Restore
dotnet restore

# Rebuild
dotnet build -c Release
```

**Option 2: Verify Package Contents**
```powershell
# Expand the .nupkg
Expand-Archive Ravelaso.UiPath.Selenium.0.1.0.nupkg extracted\

# Check for executable
ls extracted\lib\net8.0\selenium-manager.exe

# Should exist with size ~3.6 MB
```

**Option 3: Manual Fallback (Temporary)**
If you have ChromeDriver/EdgeDriver/GeckoDriver locally:
```csharp
var options = new BrowserOptions { BinaryPath = "C:\\WebDrivers\\msedgedriver.exe" };
var client = new RavelasoWebClient(BrowserType.Edge, options);
```
*(Note: This feature is planned for Phase 4+)*

**Option 4: UiPath Studio Global Install**
If running in UiPath Studio and selenium-manager.exe is missing:
1. Download from: https://github.com/SeleniumHQ/selenium/releases
2. Extract `selenium-manager.exe`
3. Copy to: `C:\Program Files\UiPath\Studio\`
4. Restart Studio

---

### "Element not found" or "NoSuchElementException"

**Error Message:**
```
OpenQA.Selenium.NoSuchElementException: no such element: Unable to locate element...
```

**Causes:**
1. Locator is incorrect (selector doesn't match element)
2. Element not yet loaded (need to wait)
3. Element inside iframe or shadow DOM
4. Global delay is too short

**Solutions:**

**1. Verify Locator with Browser DevTools**
```
Right-click element → Inspect → Copy Selector
```

Then test in code:
```csharp
if (client.Exists(Loc.Css("div.my-class")))
{
    Console.WriteLine("Element found!");
}
else
{
    Console.WriteLine("Element NOT found - check selector");
}
```

**2. Add Explicit Wait**
```csharp
// Instead of:
client.Click(Loc.Id("button"));  // May fail if not loaded

// Use:
if (client.WaitFor(Loc.Id("button"), TimeSpan.FromSeconds(10)))
{
    client.Click(Loc.Id("button"));
}
else
{
    throw new TimeoutException("Button did not appear");
}
```

**3. Increase Global Delay**
```csharp
var options = new BrowserOptions
{
    GlobalDelay = TimeSpan.FromSeconds(3)  // Default 5s
};
var client = new RavelasoWebClient(BrowserType.Chrome, options);
```

**4. Check for iframe**
```csharp
// If element is inside <iframe>, you need to switch:
var frameElement = client.Driver.FindElement(By.Id("my-frame"));
client.Driver.SwitchTo().Frame(frameElement);
// Now can find elements inside frame
```

**5. Shadow DOM (Not Supported Yet)**
Shadow DOM elements require:
```csharp
var shadow = client.ExecuteScript(
    "return document.querySelector('host-element').shadowRoot"
);
// Manual DOM traversal needed (Phase 5 feature)
```

---

### "Timeout waiting for element"

**Error Message:**
```
TimeoutException: Element did not appear within timeout (10s)
```

**Causes:**
1. Element takes longer to load than timeout allows
2. Wrong locator (element never appears)
3. Page not fully loaded yet

**Solutions:**

**1. Increase Timeout**
```csharp
// Default is 10 seconds
if (client.WaitFor(Loc.Id("element"), TimeSpan.FromSeconds(30)))
{
    // Success
}
```

**2. Wait for Multiple Elements**
```csharp
var timeout = TimeSpan.FromSeconds(20);

// Wait for ANY of these
if (client.WaitFor(Loc.Id("success-msg"), timeout) ||
    client.WaitFor(Loc.Id("error-msg"), timeout))
{
    Console.WriteLine("Page loaded");
}
```

**3. Check Page Load**
```csharp
client.Navigate("https://example.com");

// Wait for document ready
client.ExecuteScript("return document.readyState === 'complete'");

// Then wait for your element
client.WaitFor(Loc.Id("content"), TimeSpan.FromSeconds(10));
```

---

## Build & Packaging Issues

### ".nupkg is missing selenium-manager.exe"

**Symptoms:**
- Package created successfully
- But when used in another project, `Selenium Manager not found` error

**Diagnosis:**
```powershell
cd dist
Expand-Archive Ravelaso.UiPath.Selenium.0.1.0.nupkg extracted\
ls extracted\lib\net8.0\
# Check if selenium-manager.exe exists
```

**Solution:**
1. Clean and rebuild:
   ```bash
   dotnet clean -c Release
   dotnet build -c Release
   ```

2. Verify Package.csproj has the copy target:
   ```xml
   <Target Name="CopyRuntimesToPackage" AfterTargets="Build">
       <!-- Should copy runtimes/ -->
   </Target>
   ```

3. Confirm `bin/Release/net8.0/runtimes/win/native/selenium-manager.exe` exists

4. Force regenerate .nupkg:
   ```bash
   rm dist/*.nupkg
   dotnet build -c Release
   ```

---

### "Version mismatch" or "Package not found"

**Symptoms:**
- Installed v0.1.0, but code expects different version
- NuGet says package not in feed

**Solutions:**

**1. Check Installed Version**
```bash
dotnet list package --outdated
# or
Get-Package
```

**2. Force Exact Version**
```csharp
// In .csproj or via CLI
<PackageReference Include="Ravelaso.UiPath.Selenium" Version="0.1.0" />
```

```bash
dotnet add package Ravelaso.UiPath.Selenium --version 0.1.0
```

**3. Update to Latest**
```bash
dotnet add package Ravelaso.UiPath.Selenium --version "*"
```

---

## UiPath Studio-Specific Issues

### "Cannot load assembly" in .NET Invoke Code

**Error:**
```
Could not load file or assembly 'Ravelaso.UiPath.Selenium.Core'...
```

**Causes:**
1. DLL not added to project dependencies
2. Wrong target framework (using net10.0 instead of net8.0)
3. Dependencies missing (Selenium.WebDriver, etc.)

**Solutions:**

**1. Add NuGet Package**
- In Studio: Project → Manage NuGet Packages
- Search: "Ravelaso.UiPath.Selenium"
- Install v0.1.0 (or latest)

**2. Check Target Framework**
- Studio 25.10 supports: **net8.0** only
- Verify in Project Properties → .NET Version

**3. Manual DLL Import**
If NuGet not available:
- Extract `.nupkg` → `lib/net8.0/`
- Copy all DLLs to Studio project's `Libraries/` folder
- Ensure order: Core → WebDriver → Support

---

### Headless Mode Not Working

**Symptom:** Browser window still appears despite `Headless = true`

**Solution:**
```csharp
var options = new BrowserOptions
{
    Browser = BrowserType.Edge,
    Headless = true  // Requires: -c Release build
};
```

The headless flag is only applied in Release builds. For testing locally, use Debug with `Headless = false`.

---

## Browser-Specific Issues

### Chrome/Chromium Driver Not Found

**Error:**
```
Cannot find chrome binary in default locations...
```

**Solutions:**

**1. Auto-download (Selenium Manager)**
```csharp
// Should work automatically with Selenium 4.51.0
new RavelasoWebClient(BrowserType.Chrome);
```

**2. Specify Path Manually (Future)**
```csharp
var options = new BrowserOptions 
{ 
    BinaryPath = "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe" 
};
```

**3. Ensure Chrome is Installed**
```powershell
ls "C:\Program Files\Google\Chrome\Application\chrome.exe"
# Should exist
```

---

### Firefox/GeckoDriver Issues

**Error:**
```
Unable to locate the chromedriver executable...
```

**Solutions:**

**1. Install GeckoDriver**
- Download: https://github.com/mozilla/geckodriver/releases
- Extract `geckodriver.exe`
- Add to PATH or specify path

**2. Verify Firefox Installation**
```powershell
ls "C:\Program Files\Mozilla Firefox\firefox.exe"
```

---

## Performance Issues

### Slow Test Execution

**Symptoms:** Tests take much longer than expected

**Causes:**
1. Global delay too high (default 5 seconds)
2. Waiting unnecessarily for elements
3. Browser overhead (especially first run)

**Solutions:**

**1. Reduce Global Delay for Tests**
```csharp
var options = new BrowserOptions
{
    GlobalDelay = TimeSpan.FromMilliseconds(100)  // Low for tests
};
```

**2. Use Headless Mode**
```csharp
var options = new BrowserOptions
{
    Headless = true  // No rendering = faster
};
```

**3. Optimize Waits**
```csharp
// Bad:
client.WaitFor(Loc.Id("element"), TimeSpan.FromSeconds(30)); // Long wait

// Good:
client.WaitFor(Loc.Id("element"), TimeSpan.FromSeconds(5));  // Reasonable
```

**4. Reuse Browser Instance**
```csharp
// Bad: Create new client for each test
public void Test1() { var client = new RavelasoWebClient(...); }
public void Test2() { var client = new RavelasoWebClient(...); }

// Good: Shared fixture
[CollectionDefinition("Browser collection")]
public class BrowserCollection : ICollectionFixture<BrowserFixture> { }

public class BrowserFixture : IDisposable
{
    public RavelasoWebClient Client { get; } = new(...);
    public void Dispose() => Client.Dispose();
}
```

---

## Memory Leaks

### "Out of memory" after multiple test runs

**Causes:**
1. Not calling `client.Dispose()`
2. Browser instances not being garbage collected

**Solution:**
```csharp
// Use using statement (recommended)
using var client = new RavelasoWebClient(BrowserType.Chrome);
client.Navigate("...");
// Automatically disposed

// OR explicit disposal
var client = new RavelasoWebClient(BrowserType.Chrome);
try
{
    client.Navigate("...");
}
finally
{
    client.Dispose();
}
```

---

## Contact & Support

If you encounter an issue not listed here:

1. **Check existing issues:** GitHub Issues
2. **Review logs:** Check browser console + Selenium logs
3. **Report bug:** Include:
   - Exact error message
   - Steps to reproduce
   - Browser + version
   - .NET version
   - UiPath Studio version (if applicable)

---

**Last Updated:** 2026-10-20  
**Version:** Applies to v0.1.0+
