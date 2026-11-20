using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.Firefox;
// ReSharper disable CheckNamespace
namespace Ravelaso.UiPath.Selenium.Core;

/// <summary>
/// Static factory to create <see cref="IWebDriver"/> instances based on webBrowser type and options.
/// </summary>
public static class BrowserDriverFactory
{
    /// <summary>
    /// Creates a driver for the specified webBrowser with the provided options.
    /// </summary>
    /// <param name="webBrowser">The webBrowser type to launch.</param>
    /// <param name="options">Configuration options. If null, defaults are used.</param>
    /// <returns>A configured <see cref="IWebDriver"/> instance ready to use.</returns>
    /// <exception cref="ArgumentOutOfRangeException">If <paramref name="webBrowser"/> is not a valid value.</exception>
    public static IWebDriver Create(WebBrowser webBrowser, BrowserOptions? options = null)
    {
        SeleniumManagerResolver.EnsureResolved();

        options ??= new BrowserOptions();

        IWebDriver driver = webBrowser switch
        {
            WebBrowser.Chrome => CreateChrome(options),
            WebBrowser.Edge => CreateEdge(options),
            WebBrowser.Firefox => CreateFirefox(options),
            _ => throw new ArgumentOutOfRangeException(nameof(webBrowser), webBrowser, "Unsupported webBrowser type.")
        };

        ApplyTimeouts(driver, options);

        if (string.IsNullOrWhiteSpace(options.DebuggerAddress) && options.Maximize && !options.Headless)
        {
            try { driver.Manage().Window.Maximize(); }
            catch (WebDriverException) { /* Window may not be available */ }
        }

        return driver;
    }

    private static IWebDriver CreateChrome(BrowserOptions options)
    {
        var chromeOptions = new ChromeOptions();

        if (options.Proxy is not null)
        {
            var chromeProxy = new Proxy { HttpProxy = options.Proxy, SslProxy = options.Proxy };
            if (!string.IsNullOrWhiteSpace(options.ProxyBypassList))
                chromeProxy.AddBypassAddresses(options.ProxyBypassList.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
            chromeOptions.Proxy = chromeProxy;
        }

        if (options.AcceptInsecureCertificates)
            chromeOptions.AcceptInsecureCertificates = true;

        if (options.PageLoadStrategy.HasValue)
            chromeOptions.PageLoadStrategy = options.PageLoadStrategy.Value;

        if (options.UnhandledPromptBehavior.HasValue)
            chromeOptions.UnhandledPromptBehavior = options.UnhandledPromptBehavior.Value;

        if (!string.IsNullOrWhiteSpace(options.UserAgent))
            chromeOptions.AddArgument($"--user-agent={options.UserAgent}");

        if (options.Incognito)
            chromeOptions.AddArgument("--incognito");

        if (options.DisableGpu)
            chromeOptions.AddArgument("--disable-gpu");

        if (options.NoSandbox)
            chromeOptions.AddArgument("--no-sandbox");

        if (options.DisableExtensions)
            chromeOptions.AddArgument("--disable-extensions");

        if (options.DisableWebSecurity)
            chromeOptions.AddArgument("--disable-web-security");

        if (options.IgnoreCertificateErrors)
            chromeOptions.AddArgument("--ignore-certificate-errors");

        if (options.WindowPosition is not null)
            chromeOptions.AddArgument($"--window-position={options.WindowPosition}");

        if (options.StartMaximized)
            chromeOptions.AddArgument("--start-maximized");

        if (!string.IsNullOrWhiteSpace(options.DebuggerAddress))
        {
            // Attach to an already-running browser
            chromeOptions.DebuggerAddress = options.DebuggerAddress;
        }
        else
        {
            if (options.Headless)
                chromeOptions.AddArgument("--headless=new");

            chromeOptions.AddArgument($"--window-size={options.WindowWidth},{options.WindowHeight}");

            if (!string.IsNullOrWhiteSpace(options.BinaryPath))
                chromeOptions.BinaryLocation = options.BinaryPath;

            foreach (var arg in options.ExtraArguments)
                chromeOptions.AddArgument(arg);
        }

        return new ChromeDriver(chromeOptions);
    }

    private static IWebDriver CreateEdge(BrowserOptions options)
    {
        var edgeOptions = new EdgeOptions();

        if (options.Proxy is not null)
        {
            var edgeProxy = new Proxy { HttpProxy = options.Proxy, SslProxy = options.Proxy };
            if (!string.IsNullOrWhiteSpace(options.ProxyBypassList))
                edgeProxy.AddBypassAddresses(options.ProxyBypassList.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
            edgeOptions.Proxy = edgeProxy;
        }

        if (options.AcceptInsecureCertificates)
            edgeOptions.AcceptInsecureCertificates = true;

        if (options.PageLoadStrategy.HasValue)
            edgeOptions.PageLoadStrategy = options.PageLoadStrategy.Value;

        if (options.UnhandledPromptBehavior.HasValue)
            edgeOptions.UnhandledPromptBehavior = options.UnhandledPromptBehavior.Value;

        if (!string.IsNullOrWhiteSpace(options.UserAgent))
            edgeOptions.AddArgument($"--user-agent={options.UserAgent}");

        if (options.Incognito)
            edgeOptions.AddArgument("--incognito");

        if (options.DisableGpu)
            edgeOptions.AddArgument("--disable-gpu");

        if (options.NoSandbox)
            edgeOptions.AddArgument("--no-sandbox");

        if (options.DisableExtensions)
            edgeOptions.AddArgument("--disable-extensions");

        if (options.DisableWebSecurity)
            edgeOptions.AddArgument("--disable-web-security");

        if (options.IgnoreCertificateErrors)
            edgeOptions.AddArgument("--ignore-certificate-errors");

        if (options.WindowPosition is not null)
            edgeOptions.AddArgument($"--window-position={options.WindowPosition}");

        if (options.StartMaximized)
            edgeOptions.AddArgument("--start-maximized");

        if (!string.IsNullOrWhiteSpace(options.DebuggerAddress))
        {
            // Attach to an already-running browser
            edgeOptions.DebuggerAddress = options.DebuggerAddress;
        }
        else
        {
            if (options.Headless)
                edgeOptions.AddArgument("--headless=new");

            edgeOptions.AddArgument($"--window-size={options.WindowWidth},{options.WindowHeight}");

            if (!string.IsNullOrWhiteSpace(options.BinaryPath))
                edgeOptions.BinaryLocation = options.BinaryPath;

            foreach (var arg in options.ExtraArguments)
                edgeOptions.AddArgument(arg);
        }

        return new EdgeDriver(edgeOptions);
    }

    private static IWebDriver CreateFirefox(BrowserOptions options)
    {
        var firefoxOptions = new FirefoxOptions();

        if (options.Proxy is not null)
        {
            var firefoxProxy = new Proxy { HttpProxy = options.Proxy, SslProxy = options.Proxy };
            if (!string.IsNullOrWhiteSpace(options.ProxyBypassList))
                firefoxProxy.AddBypassAddresses(options.ProxyBypassList.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
            firefoxOptions.Proxy = firefoxProxy;
        }

        if (options.AcceptInsecureCertificates)
            firefoxOptions.AcceptInsecureCertificates = true;

        if (options.PageLoadStrategy.HasValue)
            firefoxOptions.PageLoadStrategy = options.PageLoadStrategy.Value;

        if (options.UnhandledPromptBehavior.HasValue)
            firefoxOptions.UnhandledPromptBehavior = options.UnhandledPromptBehavior.Value;

        if (!string.IsNullOrWhiteSpace(options.UserAgent))
            firefoxOptions.AddArgument($"-user-agent={options.UserAgent}");

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
