using Xunit;
using Ravelaso.UiPath.Selenium.Core;
using System.Diagnostics;

namespace Ravelaso.UiPath.Selenium.Tests;

/// <summary>
/// Integration tests for RavelasoWebClient using data: URLs (no network dependency).
/// </summary>
public class ClientOperationsTests : IDisposable
{
    private RavelasoWebClient? _client;

    private void SetupClient()
    {
        var options = new BrowserOptions
        {
            Browser = BrowserType.Edge,
            Headless = true,
            GlobalDelay = TimeSpan.FromMilliseconds(100),  // Short delay for tests
            ElementWaitTimeout = TimeSpan.FromSeconds(5)
        };
        
        _client = new RavelasoWebClient(BrowserType.Edge, options);
    }

    private RavelasoWebClient Client
    {
        get
        {
            if (_client == null)
                SetupClient();
            return _client;
        }
    }

    /// <summary>
    /// Verify that client instantiation works.
    /// </summary>
    [Fact]
    public void Client_Instantiation_Works()
    {
        var options = new BrowserOptions { Browser = BrowserType.Edge, Headless = true };
        var client = new RavelasoWebClient(BrowserType.Edge, options);
        
        Assert.NotNull(client);
        Assert.NotNull(client.Driver);
        Assert.Equal(BrowserType.Edge, client.Browser);
        
        client.Dispose();
    }

    /// <summary>
    /// Verify that navigation to data URL works.
    /// </summary>
    [Fact]
    public void Client_Navigate_DataURL_Works()
    {
        var dataUrl = TestFixtures.GetTestPageDataURL();
        Client.Navigate(dataUrl);
        
        var title = Client.GetTitle();
        Assert.Equal("Test Page", title);
    }

    /// <summary>
    /// Verify that GetTitle works.
    /// </summary>
    [Fact]
    public void Client_GetTitle_ReturnsPageTitle()
    {
        var dataUrl = TestFixtures.GetTestPageDataURL();
        Client.Navigate(dataUrl);
        
        var title = Client.GetTitle();
        Assert.NotEmpty(title);
        Assert.Equal("Test Page", title);
    }

    /// <summary>
    /// Verify that Exists returns true for existing elements.
    /// </summary>
    [Fact]
    public void Client_Exists_ReturnsTrueForExistingElement()
    {
        var dataUrl = TestFixtures.GetTestPageDataURL();
        Client.Navigate(dataUrl);
        
        var exists = Client.Exists(Loc.Id("username"));
        Assert.True(exists);
    }

    /// <summary>
    /// Verify that Exists returns false for non-existent elements.
    /// </summary>
    [Fact]
    public void Client_Exists_ReturnsFalseForMissingElement()
    {
        var dataUrl = TestFixtures.GetTestPageDataURL();
        Client.Navigate(dataUrl);
        
        var exists = Client.Exists(Loc.Id("nonexistent"));
        Assert.False(exists);
    }

    /// <summary>
    /// Verify that IsDisplayed returns true for visible elements.
    /// </summary>
    [Fact]
    public void Client_IsDisplayed_ReturnsTrueForVisibleElement()
    {
        var dataUrl = TestFixtures.GetTestPageDataURL();
        Client.Navigate(dataUrl);
        
        var isDisplayed = Client.IsDisplayed(Loc.Id("username"));
        Assert.True(isDisplayed);
    }

    /// <summary>
    /// Verify that Type enters text into an input field.
    /// </summary>
    [Fact]
    public void Client_Type_EntersTextIntoInput()
    {
        var dataUrl = TestFixtures.GetTestPageDataURL();
        Client.Navigate(dataUrl);
        
        Client.Type(Loc.Id("username"), "testuser");
        var value = Client.GetAttribute(Loc.Id("username"), "value");
        
        Assert.Equal("testuser", value);
    }

    /// <summary>
    /// Verify that GetAttribute retrieves element attributes.
    /// </summary>
    [Fact]
    public void Client_GetAttribute_RetrievesAttribute()
    {
        var dataUrl = TestFixtures.GetTestPageDataURL();
        Client.Navigate(dataUrl);
        
        var type = Client.GetAttribute(Loc.Id("username"), "type");
        Assert.Equal("text", type);
    }

    /// <summary>
    /// Verify that Select works with native select elements (by value).
    /// </summary>
    [Fact]
    public void Client_Select_ByValue_Works()
    {
        var dataUrl = TestFixtures.GetTestPageDataURL();
        Client.Navigate(dataUrl);
        
        var success = Client.Select(Loc.Id("country"), "mx");
        Assert.True(success);
        
        // Verify the selection
        var selected = Client.GetAttribute(Loc.Id("country"), "value");
        Assert.Equal("mx", selected);
    }

    /// <summary>
    /// Verify that Select works with visible text.
    /// </summary>
    [Fact]
    public void Client_Select_ByText_Works()
    {
        var dataUrl = TestFixtures.GetTestPageDataURL();
        Client.Navigate(dataUrl);
        
        var success = Client.Select(Loc.Id("country"), "Canada");
        Assert.True(success);
        
        var selected = Client.GetAttribute(Loc.Id("country"), "value");
        Assert.Equal("ca", selected);
    }

    /// <summary>
    /// Verify that Click triggers element clicks.
    /// </summary>
    [Fact]
    public void Client_Click_TriggersAction()
    {
        var dataUrl = TestFixtures.GetTestPageDataURL();
        Client.Navigate(dataUrl);
        
        // Result div is hidden initially (display:none in CSS)
        var displayBefore = Client.ExecuteScript("return document.getElementById('result').style.display;") ?? "";
        
        // Click button
        Client.Click(Loc.Id("submit"));
        
        // Result div should now be visible
        var displayAfter = Client.ExecuteScript("return document.getElementById('result').style.display;");
        Assert.Equal("block", displayAfter?.ToString());
    }

    /// <summary>
    /// Verify that WaitFor returns true when element appears.
    /// </summary>
    [Fact]
    public void Client_WaitFor_ReturnsTrueForExistingElement()
    {
        var dataUrl = TestFixtures.GetTestPageDataURL();
        Client.Navigate(dataUrl);
        
        var found = Client.WaitFor(Loc.Id("username"), TimeSpan.FromSeconds(2));
        Assert.True(found);
    }

    /// <summary>
    /// Verify that WaitFor returns false when element does not appear (timeout).
    /// </summary>
    [Fact]
    public void Client_WaitFor_ReturnsFalseOnTimeout()
    {
        var dataUrl = TestFixtures.GetTestPageDataURL();
        Client.Navigate(dataUrl);
        
        var found = Client.WaitFor(Loc.Id("nonexistent"), TimeSpan.FromMilliseconds(500));
        Assert.False(found);
    }

    /// <summary>
    /// Verify that Screenshot saves a PNG file.
    /// </summary>
    [Fact]
    public void Client_Screenshot_SavesPNG()
    {
        var dataUrl = TestFixtures.GetTestPageDataURL();
        Client.Navigate(dataUrl);
        
        var tempDir = Path.Combine(Path.GetTempPath(), "ravelaso-test");
        var screenshotPath = Path.Combine(tempDir, "test_screenshot.png");
        
        if (File.Exists(screenshotPath))
            File.Delete(screenshotPath);
        
        var result = Client.Screenshot(screenshotPath);
        
        Assert.True(File.Exists(result));
        Assert.True(new FileInfo(result).Length > 0);
        
        // Cleanup
        if (File.Exists(result))
            File.Delete(result);
    }

    /// <summary>
    /// Verify that ExecuteScript runs JavaScript.
    /// </summary>
    [Fact]
    public void Client_ExecuteScript_RunsJavaScript()
    {
        var dataUrl = TestFixtures.GetTestPageDataURL();
        Client.Navigate(dataUrl);
        
        var result = Client.ExecuteScript("return 2 + 2;");
        Assert.Equal(4L, result);  // JavaScript numbers are long in .NET
    }

    /// <summary>
    /// Verify that Locator with And chains work in client operations.
    /// </summary>
    [Fact]
    public void Client_Operations_WithChainedLocator_Works()
    {
        var dataUrl = TestFixtures.GetTestPageDataURL();
        Client.Navigate(dataUrl);
        
        var locator = Loc.Id("username").And.Class("form-input");
        var exists = Client.Exists(locator);
        
        Assert.True(exists);
    }

    /// <summary>
    /// Verify that GetUrl returns the current URL.
    /// </summary>
    [Fact]
    public void Client_GetUrl_ReturnsDataURL()
    {
        var dataUrl = TestFixtures.GetTestPageDataURL();
        Client.Navigate(dataUrl);
        
        var currentUrl = Client.GetUrl();
        Assert.StartsWith("data:", currentUrl);
    }

    public void Dispose()
    {
        _client?.Dispose();
    }
}
