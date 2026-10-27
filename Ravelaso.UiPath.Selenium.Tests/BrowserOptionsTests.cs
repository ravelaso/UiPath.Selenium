using Xunit;
using Ravelaso.UiPath.Selenium.Core;

namespace Ravelaso.UiPath.Selenium.Tests;

/// <summary>
/// Unit tests for BrowserOptions configuration class.
/// </summary>
public class BrowserOptionsTests
{
    /// <summary>
    /// Verify that BrowserOptions has correct default values.
    /// </summary>
    [Fact]
    public void BrowserOptions_DefaultValues_AreAsExpected()
    {
        var options = new BrowserOptions();
        
        Assert.Equal(WebBrowser.Edge, options.WebBrowser);
        Assert.False(options.Headless);
        Assert.True(options.Maximize);
        Assert.Equal(1920, options.WindowWidth);
        Assert.Equal(1080, options.WindowHeight);
        Assert.Equal(TimeSpan.FromSeconds(60), options.PageLoadTimeout);
        Assert.Equal(TimeSpan.Zero, options.ImplicitWait);
        Assert.Equal(TimeSpan.FromSeconds(5), options.GlobalDelay);
        Assert.Equal(TimeSpan.FromSeconds(10), options.ElementWaitTimeout);
        Assert.Null(options.BinaryPath);
        Assert.Empty(options.ExtraArguments);
    }

    /// <summary>
    /// Verify that BrowserOptions properties can be customized.
    /// </summary>
    [Fact]
    public void BrowserOptions_Customization_Works()
    {
        var options = new BrowserOptions
        {
            WebBrowser = WebBrowser.Chrome,
            Headless = true,
            Maximize = false,
            WindowWidth = 1280,
            WindowHeight = 720,
            PageLoadTimeout = TimeSpan.FromSeconds(30),
            ImplicitWait = TimeSpan.FromSeconds(5),
            GlobalDelay = TimeSpan.FromSeconds(2),
            ElementWaitTimeout = TimeSpan.FromSeconds(15),
            BinaryPath = "/usr/bin/chrome"
        };
        
        options.ExtraArguments.Add("--no-sandbox");
        options.ExtraArguments.Add("--disable-dev-shm-usage");
        
        Assert.Equal(WebBrowser.Chrome, options.WebBrowser);
        Assert.True(options.Headless);
        Assert.False(options.Maximize);
        Assert.Equal(1280, options.WindowWidth);
        Assert.Equal(720, options.WindowHeight);
        Assert.Equal(TimeSpan.FromSeconds(30), options.PageLoadTimeout);
        Assert.Equal(TimeSpan.FromSeconds(5), options.ImplicitWait);
        Assert.Equal(TimeSpan.FromSeconds(2), options.GlobalDelay);
        Assert.Equal(TimeSpan.FromSeconds(15), options.ElementWaitTimeout);
        Assert.Equal("/usr/bin/chrome", options.BinaryPath);
        Assert.Equal(2, options.ExtraArguments.Count);
        Assert.Contains("--no-sandbox", options.ExtraArguments);
    }

    /// <summary>
    /// Verify that GlobalDelay can be set to zero to disable it.
    /// </summary>
    [Fact]
    public void BrowserOptions_GlobalDelay_CanBeDisabled()
    {
        var options = new BrowserOptions { GlobalDelay = TimeSpan.Zero };
        
        Assert.Equal(TimeSpan.Zero, options.GlobalDelay);
    }

    /// <summary>
    /// Verify that ElementWaitTimeout can be customized.
    /// </summary>
    [Fact]
    public void BrowserOptions_ElementWaitTimeout_CanBeCustomized()
    {
        var options = new BrowserOptions { ElementWaitTimeout = TimeSpan.FromSeconds(20) };
        
        Assert.Equal(TimeSpan.FromSeconds(20), options.ElementWaitTimeout);
    }
}
