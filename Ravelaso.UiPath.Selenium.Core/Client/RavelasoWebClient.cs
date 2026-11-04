using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

// ReSharper disable MethodOverloadWithOptionalParameter
// ReSharper disable CheckNamespace
// ReSharper disable once InconsistentNaming
// ReSharper disable once MemberCanBePrivate.Global
namespace Ravelaso.UiPath.Selenium.Core;

/// <summary>
/// Main Selenium WebDriver client wrapper. Encapsulates a driver instance and provides a simplified,
/// fluent API with automatic global delays, conditional waits, and simplified element locators.
/// </summary>
/// <remarks>
/// The client is designed to:
/// 1. Accept a webBrowser type and create the corresponding driver via <see cref="BrowserDriverFactory"/>.
/// 2. Apply a global delay before each operation (configurable via <see cref="BrowserOptions.GlobalDelay"/>).
/// 3. Provide conditional waits with timeout for element detection.
/// 4. Use simplified locators via <see cref="Loc"/> and fluent <see cref="Locator"/> chains.
/// </remarks>
public partial class RavelasoWebClient : IDisposable
{
    /// <summary>
    /// The underlying Selenium WebDriver (protected to allow inheritance and IntelliSense on _driver.Manage(), etc.).
    /// </summary>
    protected readonly IWebDriver _driver;

    private bool _disposed;

    /// <summary>
    /// Gets the webBrowser type in use.
    /// </summary>
    public WebBrowser WebBrowser { get; }

    /// <summary>
    /// Gets the configuration options for this client.
    /// </summary>
    public BrowserOptions Options { get; }

    /// <summary>
    /// Gets or sets the global delay applied before each exposed operation.
    /// Can be set to <see cref="TimeSpan.Zero"/> to disable.
    /// </summary>
    public TimeSpan GlobalDelay
    {
        get => Options.GlobalDelay;
        set => Options.GlobalDelay = value;
    }

    /// <summary>
    /// Gets or sets the timeout for conditional element waits.
    /// </summary>
    public TimeSpan ElementWaitTimeout
    {
        get => Options.ElementWaitTimeout;
        set => Options.ElementWaitTimeout = value;
    }

    /// <summary>
    /// Gets direct access to the underlying Selenium WebDriver for raw operations.
    /// Useful when the wrapper does not cover a specific scenario.
    /// </summary>
    public IWebDriver Driver => _driver;

    /// <summary>
    /// Initializes a new client for the specified webBrowser with default options.
    /// </summary>
    /// <param name="webBrowser">The webBrowser type to launch.</param>
    public RavelasoWebClient(WebBrowser webBrowser) : this(webBrowser, null)
    {
    }

    /// <summary>
    /// Initializes a new client for the specified webBrowser with custom options.
    /// </summary>
    /// <param name="webBrowser">The webBrowser type to launch.</param>
    /// <param name="options">Configuration options. If null, defaults are used.</param>
    public RavelasoWebClient(WebBrowser webBrowser, BrowserOptions? options)
    {
        options ??= new();
        options.WebBrowser = webBrowser;

        WebBrowser = webBrowser;
        Options = options;
        _driver = BrowserDriverFactory.Create(webBrowser, options);
    }

    /// <summary>
    /// Initializes a new client using the browser type defined in <paramref name="options"/>.
    /// </summary>
    public RavelasoWebClient(BrowserOptions? options)
        : this(options?.WebBrowser ?? WebBrowser.Edge, options)
    {
    }

    /// <summary>
    /// Called before each exposed operation to apply the global delay.
    /// Throws <see cref="ObjectDisposedException"/> if the client has been disposed.
    /// Can be overridden in derived classes to add custom logic.
    /// </summary>
    /// <exception cref="ObjectDisposedException">If the client has been disposed.</exception>
    protected virtual void BeforeStep(TimeSpan? delay = null)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(RavelasoWebClient));

        var effectiveDelay = delay ?? GlobalDelay;
        if (effectiveDelay > TimeSpan.Zero)
            Thread.Sleep(effectiveDelay);
    }

    /// <summary>
    /// Resolves a locator to an element, applying the configured timeout.
    /// Searches the light DOM first and, if not found, falls back to a recursive
    /// search through any open Shadow DOM roots so that Web Components (e.g.
    /// &lt;alusta-navigation&gt;) are supported transparently by every API method.
    /// </summary>
    protected IWebElement WaitElement(By locator, TimeSpan? beforeStepDelay = null, TimeSpan? timeout = null)
    {
        var wait = timeout ?? ElementWaitTimeout;

        if (wait <= TimeSpan.Zero)
            try
            {
                return _driver.FindElement(locator);
            }
            catch (NoSuchElementException)
            {
                var shadowElement = FindInShadowRoots(locator);
                if (shadowElement != null)
                    return shadowElement;

                throw;
            }

        var webDriverWait = new WebDriverWait(_driver, wait);
        return webDriverWait.Until(driver =>
        {
            try
            {
                return driver.FindElement(locator);
            }
            catch (NoSuchElementException)
            {
                return FindInShadowRoots(locator);
            }
        });
    }

    /// <summary>
    /// Searches the light DOM first and then recursively through all open Shadow DOM
    /// roots for an element matching <paramref name="locator"/>.
    /// Returns null when no match is found.
    /// </summary>
    protected IWebElement? FindInShadowRoots(By locator)
    {
        try
        {
            return SearchShadowRoots(_driver, locator);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Recursive helper that searches a given search context (document or shadow root)
    /// and descends into every open shadow root it contains.
    /// </summary>
    private static IWebElement? SearchShadowRoots(ISearchContext context, By locator)
    {
        try
        {
            return context.FindElement(locator);
        }
        catch (NoSuchElementException)
        {
        }
        catch (WebDriverException)
        {
        }

        foreach (var element in context.FindElements(By.CssSelector("*")))
        {
            ISearchContext? shadowRoot = null;
            try
            {
                shadowRoot = element.GetShadowRoot();
            }
            catch (NoSuchShadowRootException)
            {
            }
            catch (StaleElementReferenceException)
            {
            }
            catch (WebDriverException)
            {
            }

            if (shadowRoot == null)
                continue;

            var found = SearchShadowRoots(shadowRoot, locator);
            if (found != null)
                return found;
        }

        return null;
    }

    /// <summary>
    /// Closes the webBrowser and releases the driver resources. Idempotent.
    /// </summary>
    public void Quit()
    {
        if (_disposed) return;
        _disposed = true;

        try
        {
            _driver.Quit();
        }
        catch (WebDriverException)
        {
            /* Driver already closed */
        }
        finally
        {
            try
            {
                _driver.Dispose();
            }
            catch
            {
                /* Dispose already handled */
            }
        }
    }

    /// <summary>
    /// Disposes of the client and releases the underlying driver.
    /// Implements <see cref="IDisposable"/>.
    /// </summary>
    public void Dispose()
    {
        Quit();
        GC.SuppressFinalize(this);
    }
}

