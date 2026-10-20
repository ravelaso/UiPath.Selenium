using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.Firefox;

namespace Ravelaso.UiPath.Selenium.Core;

/// <summary>
/// Static factory to create <see cref="IWebDriver"/> instances based on browser type and options.
/// </summary>
public static class BrowserDriverFactory
{
    /// <summary>
    /// Creates a driver for the specified browser with the provided options.
    /// </summary>
    /// <param name="browser">The browser type to launch.</param>
    /// <param name="options">Configuration options. If null, defaults are used.</param>
    /// <returns>A configured <see cref="IWebDriver"/> instance ready to use.</returns>
    /// <exception cref="ArgumentOutOfRangeException">If <paramref name="browser"/> is not a valid value.</exception>
    public static IWebDriver Create(BrowserType browser, BrowserOptions? options = null)
    {
        options ??= new BrowserOptions();
        
        IWebDriver driver = browser switch
        {
            BrowserType.Chrome => CreateChrome(options),
            BrowserType.Edge => CreateEdge(options),
            BrowserType.Firefox => CreateFirefox(options),
            _ => throw new ArgumentOutOfRangeException(nameof(browser), browser, "Unsupported browser type.")
        };

        ApplyTimeouts(driver, options);

        if (options.Maximize && !options.Headless)
        {
            try { driver.Manage().Window.Maximize(); }
            catch (WebDriverException) { /* Window may not be available */ }
        }

        return driver;
    }

    private static IWebDriver CreateChrome(BrowserOptions options)
    {
        var chromeOptions = new ChromeOptions();
        
        if (options.Headless)
            chromeOptions.AddArgument("--headless=new");
        
        chromeOptions.AddArgument($"--window-size={options.WindowWidth},{options.WindowHeight}");
        
        if (!string.IsNullOrWhiteSpace(options.BinaryPath))
            chromeOptions.BinaryLocation = options.BinaryPath;
        
        foreach (var arg in options.ExtraArguments)
            chromeOptions.AddArgument(arg);
        
        return new ChromeDriver(chromeOptions);
    }

    private static IWebDriver CreateEdge(BrowserOptions options)
    {
        var edgeOptions = new EdgeOptions();
        
        if (options.Headless)
            edgeOptions.AddArgument("--headless=new");
        
        edgeOptions.AddArgument($"--window-size={options.WindowWidth},{options.WindowHeight}");
        
        if (!string.IsNullOrWhiteSpace(options.BinaryPath))
            edgeOptions.BinaryLocation = options.BinaryPath;
        
        foreach (var arg in options.ExtraArguments)
            edgeOptions.AddArgument(arg);
        
        return new EdgeDriver(edgeOptions);
    }

    private static IWebDriver CreateFirefox(BrowserOptions options)
    {
        var firefoxOptions = new FirefoxOptions();
        
        if (options.Headless)
            firefoxOptions.AddArgument("-headless");
        
        firefoxOptions.AddArgument($"-width={options.WindowWidth}");
        firefoxOptions.AddArgument($"-height={options.WindowHeight}");
        
        if (!string.IsNullOrWhiteSpace(options.BinaryPath))
            firefoxOptions.BinaryLocation = options.BinaryPath;
        
        foreach (var arg in options.ExtraArguments)
            firefoxOptions.AddArgument(arg);
        
        return new FirefoxDriver(firefoxOptions);
    }

    private static void ApplyTimeouts(IWebDriver driver, BrowserOptions options)
    {
        var timeouts = driver.Manage().Timeouts();
        timeouts.PageLoad = options.PageLoadTimeout;
        timeouts.ImplicitWait = options.ImplicitWait;
    }
}
