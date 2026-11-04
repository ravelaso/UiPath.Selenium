using OpenQA.Selenium;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.UI;
using System.Diagnostics;

// ReSharper disable MethodOverloadWithOptionalParameter

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
public class RavelasoWebClient : IDisposable
{
    /// <summary>
    /// The underlying Selenium WebDriver (protected to allow inheritance and IntelliSense on _driver.Manage(), etc.).
    /// </summary>
    // ReSharper disable once InconsistentNaming
    // ReSharper disable once MemberCanBePrivate.Global
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
    protected virtual void BeforeStep()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(RavelasoWebClient));

        if (GlobalDelay > TimeSpan.Zero)
            Thread.Sleep(GlobalDelay);
    }

    /// <summary>
    /// Resolves a locator to an element, applying the configured timeout.
    /// Searches the light DOM first and, if not found, falls back to a recursive
    /// search through any open Shadow DOM roots so that Web Components (e.g.
    /// &lt;alusta-navigation&gt;) are supported transparently by every API method.
    /// </summary>
    protected IWebElement WaitElement(By locator, TimeSpan? timeout = null)
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

    #region File Downloads

    /// <summary>
    /// Downloads a file triggered by the specified element.
    /// First tries to extract a direct download URL from the element and download it
    /// over HTTP using the browser cookies. If no URL is available, falls back to a
    /// runtime Chromium/Edge DevTools (CDP) download into a temporary folder and
    /// then moves the completed file to <paramref name="destinationPath"/>.
    /// </summary>
    /// <param name="downloadButton">Locator for the element that triggers the download.</param>
    /// <param name="destinationPath">Final path where the downloaded file should be saved.</param>
    /// <param name="timeout">Optional timeout. Defaults to <see cref="ElementWaitTimeout"/> or 60 seconds.</param>
    /// <returns>The final path of the downloaded file.</returns>
    public string DownloadFile(Locator downloadButton, string destinationPath, TimeSpan? timeout = null)
    {
        BeforeStep();

        if (downloadButton == null)
            throw new ArgumentNullException(nameof(downloadButton));
        if (string.IsNullOrWhiteSpace(destinationPath))
            throw new ArgumentException("Destination path cannot be empty.", nameof(destinationPath));

        var effectiveTimeout =
            timeout ?? (ElementWaitTimeout > TimeSpan.Zero ? ElementWaitTimeout : TimeSpan.FromSeconds(60));
        var fullPath = Path.GetFullPath(destinationPath);
        var parent = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrWhiteSpace(parent))
            Directory.CreateDirectory(parent);

        var element = WaitElement(downloadButton.ToBy(), effectiveTimeout);
        var url = DownloadHelper.TryGetDownloadUrl(_driver, element);

        if (!string.IsNullOrWhiteSpace(url))
        {
            DownloadHelper.DownloadHttp(_driver, url, fullPath, effectiveTimeout);
            return fullPath;
        }

        return DownloadHelper.DownloadViaCdp(_driver, element, fullPath, effectiveTimeout);
    }

    /// <summary>
    /// Downloads a file from a direct URL using the browser cookies/session.
    /// </summary>
    public string DownloadFromUrl(string url, string destinationPath, TimeSpan? timeout = null)
    {
        BeforeStep();

        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("URL cannot be empty.", nameof(url));
        if (string.IsNullOrWhiteSpace(destinationPath))
            throw new ArgumentException("Destination path cannot be empty.", nameof(destinationPath));

        var effectiveTimeout =
            timeout ?? (ElementWaitTimeout > TimeSpan.Zero ? ElementWaitTimeout : TimeSpan.FromSeconds(60));
        var fullPath = Path.GetFullPath(destinationPath);
        var parent = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrWhiteSpace(parent))
            Directory.CreateDirectory(parent);

        DownloadHelper.DownloadHttp(_driver, url, fullPath, effectiveTimeout);
        return fullPath;
    }

    /// <summary>
    /// Tries to extract a download URL from an element without triggering the download.
    /// </summary>
    public string? TryGetDownloadUrl(Locator downloadButton)
    {
        BeforeStep();

        if (downloadButton == null)
            throw new ArgumentNullException(nameof(downloadButton));

        var element = WaitElement(downloadButton.ToBy(), ElementWaitTimeout);
        return DownloadHelper.TryGetDownloadUrl(_driver, element);
    }

    #endregion

    #region Zoom

    /// <summary>
    /// Sets the browser zoom using a decimal value, where 1.0 is 100%.
    /// </summary>
    /// <param name="percent">Zoom value, for example 0.8 for 80%.</param>
    public void SetZoom(double percent)
    {
        BeforeStep();
        ApplyZoom(percent);
    }

    /// <summary>
    /// Resets the browser zoom to 100%.
    /// </summary>
    public void ResetZoom()
    {
        BeforeStep();
        ApplyZoom(1.0);
    }

    private void ApplyZoom(double percent)
    {
        if (percent <= 0)
            throw new ArgumentOutOfRangeException(nameof(percent), "Zoom must be greater than zero.");

        var zoom = Math.Clamp(percent, 0.25, 5.0);
        var script =
            $"document.body.style.zoom = '{zoom * 100}%'; document.documentElement.style.zoom = '{zoom * 100}%';";

        ((IJavaScriptExecutor)_driver).ExecuteScript(script);
    }

    #endregion

    /// <summary>
    /// Disposes of the client and releases the underlying driver.
    /// Implements <see cref="IDisposable"/>.
    /// </summary>
    public void Dispose()
    {
        Quit();
        GC.SuppressFinalize(this);
    }

    #region Navigation

    /// <summary>
    /// Navigates to the specified URL.
    /// </summary>
    /// <param name="url">The URL to navigate to.</param>
    /// <example>
    /// <code>
    /// client.Navigate("https://example.com");
    /// client.Navigate("https://dhlecommercetest.p2p.basware.com");
    /// </code>
    /// </example>
    public void Navigate(string url)
    {
        BeforeStep();
        _driver.Navigate().GoToUrl(url);
    }

    /// <summary>
    /// Gets the title of the current page.
    /// </summary>
    public string GetTitle()
    {
        BeforeStep();
        return _driver.Title;
    }

    /// <summary>
    /// Gets the current URL.
    /// </summary>
    public string GetUrl()
    {
        BeforeStep();
        return _driver.Url;
    }

    /// <summary>
    /// Gets the HTML source of the current page.
    /// </summary>
    public string GetPageSource()
    {
        BeforeStep();
        return _driver.PageSource;
    }

    #endregion

    #region Interaction

    /// <summary>
    /// Clicks an element located by the given locator.
    /// </summary>
    /// <param name="locator">The element locator.</param>
    /// <param name="timeout">Optional timeout override for this operation. If null, uses <see cref="ElementWaitTimeout"/>.</param>
    /// <example>
    /// <code>
    /// // Click a button by data-t-id
    /// client.Click(Loc.Tag("button").Attr("data-t-id", "submit"));
    ///
    /// // Click a link with extended timeout (30s)
    /// client.Click(Loc.Tag("a").Attr("href", "/ap/"), TimeSpan.FromSeconds(30));
    /// </code>
    /// </example>
    public void Click(By locator, TimeSpan? timeout = null)
    {
        BeforeStep();
        var element = WaitElement(locator, timeout);
        element.Click();
    }

    /// <summary>
    /// Double-clicks an element located by the given locator.
    /// Useful for grids (e.g., ag-grid) where double-clicking a row opens the detail page.
    /// </summary>
    /// <param name="locator">The element locator.</param>
    /// <param name="timeout">Optional timeout override for this operation. If null, uses <see cref="ElementWaitTimeout"/>.</param>
    /// <example>
    /// <code>
    /// // Double-click a resolved anchor cell
    /// var cell = client.CreateAnchor(Loc.Tag("span").Text("139121_779639"))
    ///     .Find(Loc.Tag("span").Text("90.00"))
    ///     .Resolve();
    /// client.DoubleClick(cell);
    ///
    /// // Double-click by locator
    /// client.DoubleClick(Loc.Tag("span").Text("187.30"));
    /// </code>
    /// </example>
    public void DoubleClick(By locator, TimeSpan? timeout = null)
    {
        BeforeStep();
        var element = WaitElement(locator, timeout);
        new Actions(_driver).DoubleClick(element).Perform();
    }

    /// <summary>
    /// Double-clicks an element (Locator overload).
    /// </summary>
    public void DoubleClick(Locator locator, TimeSpan? timeout = null)
    {
        DoubleClick(locator.ToBy(), timeout);
    }

    /// <summary>
    /// Enters text into an element. Optionally clears the field first.
    /// </summary>
    /// <param name="locator">The element locator.</param>
    /// <param name="text">The text to enter.</param>
    /// <param name="clear">If true, clears the field before typing. Default: true.</param>
    /// <param name="timeout">Optional timeout override for this operation. If null, uses <see cref="ElementWaitTimeout"/>.</param>
    /// <example>
    /// <code>
    /// // Type into an input by id
    /// client.Type(Loc.Id("txtLogin"), "user@example.com");
    ///
    /// // Type without clearing first
    /// client.Type(Loc.Id("search"), "query", clear: false);
    /// </code>
    /// </example>
    public void Type(By locator, string text, bool clear = true, TimeSpan? timeout = null)
    {
        BeforeStep();
        var element = WaitElement(locator, timeout);

        if (clear)
            try
            {
                element.Clear();
            }
            catch (InvalidElementStateException)
            {
                /* Element not a text input; proceed */
            }

        element.SendKeys(text);
    }

    /// <summary>
    /// Gets the visible text of an element.
    /// </summary>
    /// <param name="locator">The element locator.</param>
    /// <param name="timeout">Optional timeout override for this operation. If null, uses <see cref="ElementWaitTimeout"/>.</param>
    /// <example>
    /// <code>
    /// // Get the text of a heading
    /// var title = client.GetText(Loc.Tag("h1").Class("ng-binding"));
    ///
    /// // Get text with extended timeout
    /// var msg = client.GetText(Loc.Id("status"), TimeSpan.FromSeconds(15));
    /// </code>
    /// </example>
    public string GetText(By locator, TimeSpan? timeout = null)
    {
        BeforeStep();
        return WaitElement(locator, timeout).Text;
    }

    /// <summary>
    /// Gets the value of a specific attribute on an element.
    /// </summary>
    /// <param name="locator">The element locator.</param>
    /// <param name="attributeName">The attribute name (e.g., "value", "href", "data-id").</param>
    /// <param name="timeout">Optional timeout override for this operation. If null, uses <see cref="ElementWaitTimeout"/>.</param>
    /// <example>
    /// <code>
    /// // Get the href of a link
    /// var url = client.GetAttribute(Loc.Tag("a").Attr("data-t-id", "nav-link"), "href");
    ///
    /// // Get the value of an input
    /// var val = client.GetAttribute(Loc.Id("username"), "value");
    /// </code>
    /// </example>
    public string? GetAttribute(By locator, string attributeName, TimeSpan? timeout = null)
    {
        BeforeStep();
        return WaitElement(locator, timeout).GetAttribute(attributeName);
    }

    #endregion

    #region Detection

    /// <summary>
    /// Checks if an element exists on the page (immediate check, no conditional wait).
    /// </summary>
    /// <param name="locator">The element locator.</param>
    /// <returns>True if the element is found; false otherwise.</returns>
    /// <example>
    /// <code>
    /// // Check if login page is displayed
    /// if (client.Exists(Loc.Id("btnLogin")))
    ///     Console.WriteLine("Login page");
    /// </code>
    /// </example>
    public bool Exists(By locator)
    {
        return Exists(locator, TimeSpan.Zero);
    }

    /// <summary>
    /// Checks if an element exists on the page, waiting up to the specified timeout.
    /// </summary>
    /// <param name="locator">The element locator.</param>
    /// <param name="timeout">The timeout to wait for the element. If null, performs an immediate check.</param>
    /// <returns>True if the element appears within the timeout; false otherwise.</returns>
    public bool Exists(By locator, TimeSpan? timeout = null)
    {
        BeforeStep();
        try
        {
            WaitElement(locator, timeout ?? TimeSpan.Zero);
            return true;
        }
        catch (WebDriverTimeoutException)
        {
            return false;
        }
        catch (NotFoundException)
        {
            return false;
        }
    }

    /// <summary>
    /// Checks if an element is displayed on the page.
    /// </summary>
    /// <param name="locator">The element locator.</param>
    /// <param name="timeout">Optional timeout override for this operation. If null, performs an immediate check.</param>
    public bool IsDisplayed(By locator, TimeSpan? timeout = null)
    {
        BeforeStep();
        try
        {
            var element = WaitElement(locator, timeout ?? TimeSpan.Zero);
            return element.Displayed;
        }
        catch (NotFoundException)
        {
            return false;
        }
        catch (WebDriverTimeoutException)
        {
            return false;
        }
    }

    /// <summary>
    /// Waits for an element to appear on the page using the default timeout.
    /// </summary>
    /// <param name="locator">The element locator.</param>
    /// <returns>True if the element appears within the timeout; false otherwise.</returns>
    public bool WaitFor(By locator)
    {
        return WaitFor(locator, ElementWaitTimeout);
    }

    /// <summary>
    /// Waits for an element to appear on the page with a custom timeout.
    /// </summary>
    /// <param name="locator">The element locator.</param>
    /// <param name="timeout">The timeout to wait for the element. If null, uses <see cref="ElementWaitTimeout"/>.</param>
    /// <returns>True if the element appears within the timeout; false otherwise.</returns>
    public bool WaitFor(By locator, TimeSpan? timeout = null)
    {
        BeforeStep();
        try
        {
            WaitElement(locator, timeout);
            return true;
        }
        catch (WebDriverTimeoutException)
        {
            return false;
        }
        catch (NotFoundException)
        {
            return false;
        }
    }

    /// <summary>
    /// Waits for an anchored element to appear, without requiring the caller to call
    /// <see cref="AnchorLocator.Resolve()"/>. Returns true as soon as the anchor resolves.
    /// </summary>
    /// <param name="anchor">The anchor locator to resolve.</param>
    /// <param name="timeout">Maximum time to wait. If null, uses <see cref="ElementWaitTimeout"/>.</param>
    /// <returns>True if the anchored element appears within the timeout; false otherwise.</returns>
    public bool WaitFor(AnchorLocator anchor, TimeSpan? timeout = null)
    {
        BeforeStep();

        if (anchor == null)
            throw new ArgumentNullException(nameof(anchor));

        var effectiveTimeout =
            timeout ?? (ElementWaitTimeout > TimeSpan.Zero ? ElementWaitTimeout : TimeSpan.FromSeconds(60));
        try
        {
            anchor.WithTimeout(effectiveTimeout).Resolve(this);
            return true;
        }
        catch (NoSuchElementException)
        {
            return false;
        }
        catch (WebDriverTimeoutException)
        {
            return false;
        }
        catch (NotFoundException)
        {
            return false;
        }
    }

    /// <summary>
    /// Checks whether an anchored element appears within the specified timeout,
    /// without requiring the caller to call <see cref="AnchorLocator.Resolve()"/>.
    /// </summary>
    /// <param name="anchor">The anchor locator to resolve.</param>
    /// <param name="timeout">Maximum time to wait. If null, performs an immediate check.</param>
    /// <returns>True if the anchored element appears within the timeout; false otherwise.</returns>
    public bool Exists(AnchorLocator anchor, TimeSpan? timeout = null)
    {
        BeforeStep();

        if (anchor == null)
            throw new ArgumentNullException(nameof(anchor));

        var effectiveTimeout = timeout.HasValue && timeout.Value > TimeSpan.Zero
            ? timeout.Value
            : TimeSpan.FromMilliseconds(1);

        try
        {
            anchor.WithTimeout(effectiveTimeout).Resolve(this);
            return true;
        }
        catch (NoSuchElementException)
        {
            return false;
        }
        catch (WebDriverTimeoutException)
        {
            return false;
        }
        catch (NotFoundException)
        {
            return false;
        }
    }

    #endregion

    #region Shadow DOM

    /// <summary>
    /// Finds an element inside the shadow root of a host element (e.g. a Web Component).
    /// </summary>
    /// <param name="hostLocator">Locator for the host element that owns the shadow root.</param>
    /// <param name="targetLocator">Locator for the element to find inside the shadow root.</param>
    /// <param name="timeout">Optional custom timeout. If null, uses <see cref="ElementWaitTimeout"/>.</param>
    /// <returns>The found element.</returns>
    /// <example>
    /// <code>
    /// // Find an element inside a Web Component's shadow root
    /// var link = client.FindInShadowRoot(
    ///     By.CssSelector("alusta-navigation"),
    ///     Loc.Tag("a").Attr("href", "/ap/"));
    /// Console.WriteLine(link.Text);
    /// </code>
    /// </example>
    public IWebElement FindInShadowRoot(By hostLocator, By targetLocator, TimeSpan? timeout = null)
    {
        BeforeStep();
        var wait = timeout ?? ElementWaitTimeout;
        if (wait <= TimeSpan.Zero)
        {
            var host = _driver.FindElement(hostLocator);
            var found = SearchShadowRoots(host.GetShadowRoot(), targetLocator);
            if (found == null)
                throw new NoSuchElementException($"Element not found in shadow root of '{hostLocator}'.");
            return found;
        }

        var wdw = new WebDriverWait(_driver, wait);
        return wdw.Until(driver =>
        {
            try
            {
                var host = driver.FindElement(hostLocator);
                return SearchShadowRoots(host.GetShadowRoot(), targetLocator);
            }
            catch (NoSuchElementException)
            {
                return null;
            }
            catch (NoSuchShadowRootException)
            {
                return null;
            }
            catch (WebDriverException)
            {
                return null;
            }
        }) ?? throw new WebDriverTimeoutException($"Timed out waiting for element in shadow root of '{hostLocator}'.");
    }

    /// <summary>
    /// Finds an element inside the shadow root of a host element (Locator overload).
    /// </summary>
    public IWebElement FindInShadowRoot(Locator hostLocator, Locator targetLocator, TimeSpan? timeout = null)
    {
        return FindInShadowRoot(hostLocator.ToBy(), targetLocator.ToBy(), timeout);
    }

    /// <summary>
    /// Checks whether an element exists inside the shadow root of a host element.
    /// </summary>
    public bool ExistsInShadowRoot(By hostLocator, By targetLocator, TimeSpan? timeout = null)
    {
        BeforeStep();
        try
        {
            FindInShadowRoot(hostLocator, targetLocator, timeout ?? TimeSpan.Zero);
            return true;
        }
        catch (NotFoundException)
        {
            return false;
        }
        catch (WebDriverTimeoutException)
        {
            return false;
        }
        catch (WebDriverException)
        {
            return false;
        }
    }

    /// <summary>
    /// Checks whether an element exists inside the shadow root of a host element (Locator overload).
    /// </summary>
    public bool ExistsInShadowRoot(Locator hostLocator, Locator targetLocator, TimeSpan? timeout = null)
    {
        return ExistsInShadowRoot(hostLocator.ToBy(), targetLocator.ToBy(), timeout);
    }

    /// <summary>
    /// Clicks an element located inside the shadow root of a host element.
    /// </summary>
    /// <param name="hostLocator">Locator for the host element that owns the shadow root.</param>
    /// <param name="targetLocator">Locator for the element to find inside the shadow root.</param>
    /// <param name="timeout">Optional timeout override for this operation. If null, uses <see cref="ElementWaitTimeout"/>.</param>
    /// <example>
    /// <code>
    /// // Click a link inside a Web Component's shadow root
    /// client.ClickShadow(
    ///     By.CssSelector("alusta-navigation"),
    ///     Loc.Tag("a").Attr("href", "/ap/"));
    /// </code>
    /// </example>
    public void ClickShadow(By hostLocator, By targetLocator, TimeSpan? timeout = null)
    {
        BeforeStep();
        var element = FindInShadowRoot(hostLocator, targetLocator, timeout);
        element.Click();
    }

    /// <summary>
    /// Clicks an element located inside the shadow root of a host element (Locator overload).
    /// </summary>
    public void ClickShadow(Locator hostLocator, Locator targetLocator, TimeSpan? timeout = null)
    {
        ClickShadow(hostLocator.ToBy(), targetLocator.ToBy(), timeout);
    }

    /// <summary>
    /// Clicks a previously resolved element, applying the global delay.
    /// Useful for elements obtained from shadow roots or other custom searches.
    /// </summary>
    public void Click(IWebElement element)
    {
        BeforeStep();
        element.Click();
    }

    #endregion

    #region Mouse / JavaScript

    /// <summary>
    /// Moves the mouse over an element. Useful for revealing hover-dependent
    /// elements (e.g. a "clear" button that only appears on input hover).
    /// </summary>
    /// <param name="locator">The element locator.</param>
    /// <param name="timeout">Optional timeout override for this operation. If null, uses <see cref="ElementWaitTimeout"/>.</param>
    /// <example>
    /// <code>
    /// // Hover over an input to reveal a clear button
    /// client.Hover(Loc.Tag("div").Attr("data-t-id", "date-rangepicker-inputs"));
    /// System.Threading.Thread.Sleep(300);
    /// client.ClickJs(Loc.Tag("button").Attr("data-t-id", "clear-datepicker"));
    /// </code>
    /// </example>
    public void Hover(By locator, TimeSpan? timeout = null)
    {
        BeforeStep();
        var element = WaitElement(locator, timeout);
        new Actions(_driver).MoveToElement(element).Perform();
    }

    /// <summary>
    /// Moves the mouse over an element (Locator overload).
    /// </summary>
    /// <param name="locator">The element locator.</param>
    /// <param name="timeout">Optional timeout override for this operation. If null, uses <see cref="ElementWaitTimeout"/>.</param>
    public void Hover(Locator locator, TimeSpan? timeout = null)
    {
        Hover(locator.ToBy(), timeout);
    }

    /// <summary>
    /// Hovers over an element to reveal hover-dependent elements, then clicks
    /// the target natively. Uses Selenium Actions for real mouse movement
    /// (activates CSS :hover) instead of JavaScript dispatchEvent.
    /// </summary>
    /// <param name="buttonLocator">The element to click.</param>
    /// <param name="hoverTarget">Optional element to hover in order to reveal the button. If null, auto-discovered.</param>
    /// <param name="timeout">Optional timeout override for this operation. If null, uses <see cref="ElementWaitTimeout"/>.</param>
    /// <example>
    /// <code>
    /// // Hover and click a hover-revealed clear button
    /// var clearBtn = Loc.Tag("button").Attr("data-t-id", "clear-datepicker").Attr("aria-label", "Clear");
    /// var inputs = Loc.Tag("div").Attr("data-t-id", "date-rangepicker-inputs");
    /// client.HoverAndClick(clearBtn, inputs);
    /// </code>
    /// </example>
    private void HoverAndClick(By buttonLocator, By hoverTarget, TimeSpan? timeout = null)
    {
        BeforeStep();

        // 1. Hover con delay global + wait condicional (como ForceClick que funcionaba)
        Hover(hoverTarget);
        Thread.Sleep(150);

        // 2. Forzar visibilidad
        var element = WaitElement(buttonLocator, timeout);
        var js = (IJavaScriptExecutor)_driver;
        js.ExecuteScript(
            "var e=arguments[0];" +
            "e.style.opacity='1'; e.style.visibility='visible';" +
            "e.style.display='inline-block'; e.style.pointerEvents='auto';" +
            "e.classList.remove('ng-hide');", element);
        Thread.Sleep(50);

        // 3. Re-find + click
        element = WaitElement(buttonLocator, timeout);
        js.ExecuteScript("arguments[0].click();", element);
    }

    /// <summary>
    /// Hovers and clicks (Locator overload).
    /// </summary>
    public void HoverAndClick(Locator buttonLocator, Locator? hoverTarget = null, TimeSpan? timeout = null)
    {
        HoverAndClick(buttonLocator.ToBy(), hoverTarget?.ToBy()!, timeout);
    }

    /// <summary>
    /// Hovers and clicks (IWebElement hover target overload).
    /// </summary>
    public void HoverAndClick(By buttonLocator, IWebElement hoverTarget, TimeSpan? timeout = null)
    {
        BeforeStep();
        var wait = timeout ?? ElementWaitTimeout;
        var js = (IJavaScriptExecutor)_driver;
        var sw = Stopwatch.StartNew();

        while (sw.Elapsed < wait)
            try
            {
                // 1. Hover
                try
                {
                    new Actions(_driver).MoveToElement(hoverTarget).Perform();
                }
                catch
                {
                    // Ignored
                }

                Thread.Sleep(300);

                // 2. Forzar visibilidad
                var element = WaitElement(buttonLocator, TimeSpan.Zero);
                js.ExecuteScript(
                    "var e=arguments[0];" +
                    "e.style.opacity='1'; e.style.visibility='visible';" +
                    "e.style.display='inline-block'; e.style.pointerEvents='auto';" +
                    "e.classList.remove('ng-hide');", element);
                Thread.Sleep(50);

                // 3. Re-resolver
                element = WaitElement(buttonLocator, TimeSpan.Zero);

                // 4. Click por JS
                js.ExecuteScript("arguments[0].click();", element);
                return;
            }
            catch (StaleElementReferenceException)
            {
                Thread.Sleep(200);
            }
            catch (WebDriverException)
            {
                Thread.Sleep(200);
            }

        throw new RavelasoException("HoverAndClick: button not clickable after retries.");
    }

    // /// <summary>
    // /// Auto-discovers a hover target that reveals the button by trying
    // /// several candidates and checking if the button becomes visible.
    // /// </summary>
    // private IWebElement? DiscoverHoverTargetByActions(By buttonLocator)
    // {
    //     var btn = WaitElement(buttonLocator, TimeSpan.Zero);
    //     var candidates = new List<IWebElement>();
    //
    //     try
    //     {
    //         var s = (IWebElement?)((IJavaScriptExecutor)_driver).ExecuteScript(
    //             "return arguments[0].previousElementSibling;", btn);
    //         if (s != null) candidates.Add(s);
    //     }
    //     catch
    //     {
    //         // ignored
    //     }
    //
    //     try
    //     {
    //         var btn2 = WaitElement(buttonLocator, TimeSpan.Zero);
    //         var ancestor = (IWebElement?)((IJavaScriptExecutor)_driver).ExecuteScript(
    //             "var e=arguments[0];" +
    //             "while(e && e !== document.body) {" +
    //             "  var s = getComputedStyle(e);" +
    //             "  if (s.opacity === '0' || s.visibility === 'hidden' || s.display === 'none')" +
    //             "    return e.parentElement;" +
    //             "  e = e.parentElement;" +
    //             "}" +
    //             "return null;", btn2);
    //         if (ancestor != null) candidates.Add(ancestor);
    //     }
    //     catch
    //     {
    //         // ignored
    //     }
    //
    //     try
    //     {
    //         var p = (IWebElement?)((IJavaScriptExecutor)_driver).ExecuteScript(
    //             "return arguments[0].parentElement;", btn);
    //         if (p != null) candidates.Add(p);
    //     }
    //     catch
    //     {
    //         // ignored
    //     }
    //
    //     try
    //     {
    //         var g = (IWebElement?)((IJavaScriptExecutor)_driver).ExecuteScript(
    //             "return arguments[0].parentElement.parentElement;", btn);
    //         if (g != null) candidates.Add(g);
    //     }
    //     catch
    //     {
    //         // ignored
    //     }
    //
    //     var unique = candidates.Distinct().ToList();
    //
    //     foreach (var candidate in unique)
    //         try
    //         {
    //             new Actions(_driver).MoveToElement(candidate).Perform();
    //             Thread.Sleep(300);
    //             var b = WaitElement(buttonLocator, TimeSpan.Zero);
    //             if (b.Displayed) return candidate;
    //         }
    //         catch
    //         {
    //             // ignored
    //         }
    //
    //     return unique.Count > 0 ? unique[0] : null;
    // }

    /// <summary>
    /// Clicks an element using JavaScript, bypassing hit-testing and any
    /// overlapping element that would intercept a native click.
    /// Simple doc-style: find element, then arguments[0].click().
    /// Retries automatically on StaleElementReferenceException (Angular re-render).
    /// </summary>
    /// <param name="locator">The element locator.</param>
    /// <param name="timeout">Optional timeout override for this operation. If null, uses <see cref="ElementWaitTimeout"/>.</param>
    /// <example>
    /// <code>
    /// // Click a hidden or intercepted button
    /// client.ClickJs(Loc.Tag("button").Attr("data-t-id", "clear-datepicker"));
    ///
    /// // Click with extended timeout
    /// client.ClickJs(Loc.Tag("a").Attr("href", "/ap/"), TimeSpan.FromSeconds(15));
    /// </code>
    /// </example>
    public void ClickJs(By locator, TimeSpan? timeout = null)
    {
        BeforeStep();
        var wait = timeout ?? ElementWaitTimeout;
        var sw = Stopwatch.StartNew();
        while (sw.Elapsed < wait)
            try
            {
                var element = WaitElement(locator, TimeSpan.Zero);
                ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].click();", element);
                return;
            }
            catch (StaleElementReferenceException)
            {
                Thread.Sleep(200);
            }

        var el = WaitElement(locator, timeout);
        ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].click();", el);
    }

/*
    /// <summary>
    /// Clicks a hidden element entirely via JavaScript in a single script:
    /// finds the button, forces its visibility (opacity, display, pointer-events),
    /// and clicks it. No hover, no Selenium references (no stale).
    /// Use for Angular/React components where the button is hidden until hover
    /// and standard clicks don't trigger the handler.
    /// </summary>
    /// <param name="buttonCss">CSS selector of the button to click.</param>
    /// <param name="timeout">Optional timeout override for this operation. If null, uses <see cref="ElementWaitTimeout"/>.</param>
    /// <example>
    /// <code>
    /// // Click a hover-revealed clear button
    /// client.ClickHiddenJs("button[data-t-id='clear-datepicker'][aria-label='Clear']");
    ///
    /// // With extended timeout
    /// client.ClickHiddenJs("button[data-t-id='clear-datepicker']", TimeSpan.FromSeconds(15));
    /// </code>
    /// </example>
    private void ClickHiddenJs(string buttonCss, TimeSpan? timeout = null)
    {
        BeforeStep();
        var js = (IJavaScriptExecutor)_driver;
        var sw = Stopwatch.StartNew();
        var wait = timeout ?? ElementWaitTimeout;
        while (sw.Elapsed < wait)
        {
            try
            {
                var clicked = js.ExecuteScript(
                    "var btn = document.querySelector(arguments[0]);" +
                    "if (!btn) return false;" +
                    "btn.style.cssText = 'opacity:1 !important;visibility:visible !important;display:inline-block !important;pointer-events:auto !important;';" +
                    "var container = btn.closest('.icon-on-hover');" +
                    "if (container) container.style.opacity = '1';" +
                    "btn.click();" +
                    "return true;",
                    buttonCss);
                if (clicked is true) return;
            }
            catch
            {
                // ignored
            }

            Thread.Sleep(200);
        }

        throw new WebDriverTimeoutException($"ClickHiddenJs: element not found: {buttonCss}");
    }
*/

/*
    /// <summary>
    /// Clicks a hidden element entirely via JavaScript.
    /// Accepts <see cref="Locator"/> objects (the standard of the library).
    /// Finds the element, forces its visibility, and clicks it — all in one script.
    /// </summary>
    /// <param name="button">Locator of the button to click.</param>
    /// <param name="timeout">Optional timeout override for this operation. If null, uses <see cref="ElementWaitTimeout"/>.</param>
    private void ClickHiddenJs(Locator button, TimeSpan? timeout = null)
    {
        var (bMech, bCrit) = GetJsSelector((By)button);
        if (bMech != "css selector")
            throw new RavelasoException(
                $"ClickHiddenJs requires a CSS-based locator. Got {bMech}: {bCrit}. " +
                "Use Loc.Tag(...).Attr(...) instead of Loc.Text(...).");

        ClickHiddenJs(bCrit, timeout);
    }
*/

/*
    /// <summary>
    /// Converts a Selenium <see cref="By"/> to a (mechanism, criteria) pair usable
    /// by the JS findElement function. Handles link text / partial link text by
    /// converting them to XPath.
    /// </summary>
    private static (string mechanism, string criteria) GetJsSelector(By by)
    {
        var mech = by.Mechanism;
        var crit = by.Criteria;
        if (mech == "link text")
            return ("xpath", $"//a[normalize-space(.)={XPathLiteral(crit)}]");
        if (mech == "partial link text")
            return ("xpath", $"//a[contains(.,{XPathLiteral(crit)})]");
        return (mech, crit);
    }
*/

/*
    /// <summary>
    /// Wraps a string in single or double quotes for use as an XPath string literal.
    /// </summary>
    private static string XPathLiteral(string s)
    {
        if (!s.Contains("'")) return $"'{s}'";
        if (!s.Contains("\"")) return $"\"{s}\"";
        // Fallback: concat() for strings containing both quote types
        return "concat(" + string.Join(",", s.Split('\'').Select(p => $"'{p}'")) + ")";
    }
*/

    /// <summary>
    /// Clicks an element using JavaScript (Locator overload).
    /// </summary>
    /// <param name="locator">The element locator.</param>
    /// <param name="timeout">Optional timeout override for this operation. If null, uses <see cref="ElementWaitTimeout"/>.</param>
    public void ClickJs(Locator locator, TimeSpan? timeout = null)
    {
        ClickJs(locator.ToBy(), timeout);
    }

    /// <summary>
    /// Clicks a hidden/intercepted element by trying multiple strategies in sequence.
    /// Each strategy is attempted; only NoSuchElementException is caught (expected when
    /// a previous strategy already removed the element). Any other exception is
    /// propagated. If the element still exists after all strategies, a RavelasoException
    /// is thrown.
    /// </summary>
    /// <param name="locator">The element locator.</param>
    /// <param name="hoverTarget">Optional locator of the element to hover to reveal <paramref name="locator"/>.</param>
    /// <param name="timeout">Optional timeout override for this operation. If null, uses <see cref="ElementWaitTimeout"/>.</param>
    public void ClickHidden(By locator, By? hoverTarget = null, TimeSpan? timeout = null)
    {
        BeforeStep();
        var js = (IJavaScriptExecutor)_driver;

        // Helper: hover if target is available
        var tryHover = () =>
        {
            if (hoverTarget != null)
                try
                {
                    var ht = WaitElement(hoverTarget, TimeSpan.Zero);
                    new Actions(_driver).MoveToElement(ht).Perform();
                    Thread.Sleep(300);
                }
                catch
                {
                    // ignored
                }
        };

        // Estrategia 1: Hover + Force + .click() (la que funciona)
        try
        {
            tryHover();
            var btn = WaitElement(locator, TimeSpan.Zero);
            js.ExecuteScript(
                "var e=arguments[0]; e.style.opacity='1'; e.style.visibility='visible';" +
                "e.style.display='inline-block'; e.style.pointerEvents='auto';" +
                "e.classList.remove('ng-hide');", btn);
            Thread.Sleep(50);
            btn = WaitElement(locator, TimeSpan.Zero);
            js.ExecuteScript("arguments[0].click();", btn);
            Thread.Sleep(500);
            // Early return: si el botón fue removido, éxito
            try
            {
                WaitElement(locator, TimeSpan.Zero);
            }
            catch (NoSuchElementException)
            {
                return;
            }
        }
        catch (NoSuchElementException)
        {
            return; /* ya removido por estrategia anterior */
        }
        catch (Exception ex)
        {
            throw new RavelasoException("ClickHidden Strategy 1 failed.", ex);
        }

        // Estrategia 2: dispatchEvent MouseEvent completo
        try
        {
            var btn = WaitElement(locator, TimeSpan.Zero);
            js.ExecuteScript(
                "var e=arguments[0];" +
                "e.style.opacity='1'; e.style.visibility='visible';" +
                "e.style.display='inline-block'; e.style.pointerEvents='auto';" +
                "e.classList.remove('ng-hide');" +
                "['mousedown','mouseup','click'].forEach(function(t){e.dispatchEvent(new MouseEvent(t,{bubbles:true,cancelable:true,view:window}));});",
                btn);
            Thread.Sleep(500);
            try
            {
                WaitElement(locator, TimeSpan.Zero);
            }
            catch (NoSuchElementException)
            {
                return;
            }
        }
        catch (NoSuchElementException)
        {
            return;
        }
        catch (Exception ex)
        {
            throw new RavelasoException("ClickHidden Strategy 2 failed.", ex);
        }

        // Estrategia 3: ClickJs (find + .click())
        try
        {
            ClickJs(locator, TimeSpan.FromSeconds(5));
            Thread.Sleep(500);
            try
            {
                WaitElement(locator, TimeSpan.Zero);
            }
            catch (NoSuchElementException)
            {
                return;
            }
        }
        catch (NoSuchElementException)
        {
            return;
        }
        catch (Exception ex)
        {
            throw new RavelasoException("ClickHidden Strategy 3 failed.", ex);
        }

        // Estrategia 4: force+click via JS single script
        try
        {
            var btnEl = WaitElement(locator, TimeSpan.Zero);
            js.ExecuteScript(
                "var e=arguments[0];" +
                "e.style.opacity='1'; e.style.visibility='visible';" +
                "e.style.display='inline-block'; e.style.pointerEvents='auto';" +
                "e.classList.remove('ng-hide');" +
                "e.click();", btnEl);
            Thread.Sleep(500);
            try
            {
                WaitElement(locator, TimeSpan.Zero);
            }
            catch (NoSuchElementException)
            {
                return;
            }
        }
        catch (NoSuchElementException)
        {
            return;
        }
        catch (Exception ex)
        {
            throw new RavelasoException("ClickHidden Strategy 4 failed.", ex);
        }

        // Estrategia 5: HoverAndClick (solo si hay hoverTarget)
        if (hoverTarget != null)
            try
            {
                HoverAndClick(locator, hoverTarget, TimeSpan.FromSeconds(5));
                Thread.Sleep(500);
                try
                {
                    WaitElement(locator, TimeSpan.Zero);
                }
                catch (NoSuchElementException)
                {
                    return;
                }
            }
            catch (NoSuchElementException)
            {
                return;
            }
            catch (Exception ex)
            {
                throw new RavelasoException("ClickHidden Strategy 5 failed.", ex);
            }

        // Si llegamos aquí, ninguna estrategia funcionó
        throw new RavelasoException(
            "ClickHidden: no strategy succeeded in clicking the element. " +
            "The element is still present in the DOM.");
    }

    /// <summary>
    /// Clicks a hidden/intercepted element (Locator overload).
    /// </summary>
    public void ClickHidden(Locator locator, Locator? hoverTarget = null, TimeSpan? timeout = null)
    {
        ClickHidden(locator.ToBy(), hoverTarget?.ToBy(), timeout);
    }

    /// <summary>
    /// Clicks a hidden/intercepted element (IWebElement hover target overload).
    /// </summary>
    public void ClickHidden(By locator, IWebElement hoverTarget, TimeSpan? timeout = null)
    {
        BeforeStep();
        try
        {
            new Actions(_driver).MoveToElement(hoverTarget).Perform();
        }
        catch (Exception ex)
        {
            throw new RavelasoException("ClickHidden: failed to hover the target element.", ex);
        }

        Thread.Sleep(150);

        var element = WaitElement(locator, timeout);
        var js = (IJavaScriptExecutor)_driver;
        js.ExecuteScript(
            "var e=arguments[0];" +
            "e.style.opacity='1'; e.style.visibility='visible';" +
            "e.style.display='inline-block'; e.style.pointerEvents='auto';" +
            "e.classList.remove('ng-hide');", element);
        Thread.Sleep(50);

        element = WaitElement(locator, timeout);
        js.ExecuteScript("arguments[0].click();", element);
    }

    #endregion

    #region Diagnostics

    // /// <summary>
    // /// Runs multiple click strategies on a hidden/hover-revealed button and reports
    // /// which one successfully cleared the associated input. Useful for diagnosing
    // /// hover-revealed Angular/React components.
    // /// </summary>
    // /// <param name="clearBtnLocator">Locator for the clear button.</param>
    // /// <param name="inputLocator">Locator for the input whose value should be cleared.</param>
    // /// <param name="hoverTargetLocator">Optional locator of the element to hover to reveal the button.</param>
    // /// <returns>A multi-line diagnostic report.</returns>
    // private string DiagnoseClearButton(By clearBtnLocator, By inputLocator, By? hoverTargetLocator = null)
    // {
    //     BeforeStep();
    //     var js = (IJavaScriptExecutor)_driver;
    //     var sb = new System.Text.StringBuilder();
    //     var input = WaitElement(inputLocator, TimeSpan.Zero);
    //     var initialValue = input.GetAttribute("value");
    //     sb.AppendLine($"Initial input value: '{initialValue}'");
    //     sb.AppendLine($"Button displayed (before): {WaitElement(clearBtnLocator, TimeSpan.Zero).Displayed}");
    //
    //     // Estrategia 1: Hover (Actions) + force + .click()
    //     try
    //     {
    //         if (hoverTargetLocator != null)
    //         {
    //             var ht = WaitElement(hoverTargetLocator, TimeSpan.Zero);
    //             new Actions(_driver).MoveToElement(ht).Perform();
    //             Thread.Sleep(300);
    //         }
    //
    //         var btn1 = WaitElement(clearBtnLocator, TimeSpan.Zero);
    //         js.ExecuteScript(
    //             "var e=arguments[0]; e.style.opacity='1'; e.style.visibility='visible';" +
    //             "e.style.display='inline-block'; e.style.pointerEvents='auto';" +
    //             "e.classList.remove('ng-hide');", btn1);
    //         Thread.Sleep(50);
    //         btn1 = WaitElement(clearBtnLocator, TimeSpan.Zero);
    //         js.ExecuteScript("arguments[0].click();", btn1);
    //         Thread.Sleep(500);
    //         var val1 = WaitElement(inputLocator, TimeSpan.Zero).GetAttribute("value");
    //         sb.AppendLine(
    //             $"Strategy 1 (Hover+Force+.click): input='{val1}' {(val1 != initialValue ? "CLEARED" : "not cleared")}");
    //     }
    //     catch (Exception ex)
    //     {
    //         sb.AppendLine($"Strategy 1: ERROR {ex.GetType().Name}: {ex.Message}");
    //     }
    //
    //     // Estrategia 2: dispatchEvent MouseEvent completo
    //     try
    //     {
    //         var btn2 = WaitElement(clearBtnLocator, TimeSpan.Zero);
    //         js.ExecuteScript(
    //             "var e=arguments[0];" +
    //             "e.style.opacity='1'; e.style.visibility='visible';" +
    //             "e.style.display='inline-block'; e.style.pointerEvents='auto';" +
    //             "e.classList.remove('ng-hide');" +
    //             "['mousedown','mouseup','click'].forEach(function(t){e.dispatchEvent(new MouseEvent(t,{bubbles:true,cancelable:true,view:window}));});",
    //             btn2);
    //         Thread.Sleep(500);
    //         var val2 = WaitElement(inputLocator, TimeSpan.Zero).GetAttribute("value");
    //         sb.AppendLine(
    //             $"Strategy 2 (dispatchEvent): input='{val2}' {(val2 != initialValue ? "CLEARED" : "not cleared")}");
    //     }
    //     catch (Exception ex)
    //     {
    //         sb.AppendLine($"Strategy 2: ERROR {ex.GetType().Name}: {ex.Message}");
    //     }
    //
    //     // Estrategia 3: ClickJs
    //     try
    //     {
    //         ClickJs(clearBtnLocator, TimeSpan.FromSeconds(5));
    //         Thread.Sleep(500);
    //         var val3 = WaitElement(inputLocator, TimeSpan.Zero).GetAttribute("value");
    //         sb.AppendLine($"Strategy 3 (ClickJs): input='{val3}' {(val3 != initialValue ? "CLEARED" : "not cleared")}");
    //     }
    //     catch (Exception ex)
    //     {
    //         sb.AppendLine($"Strategy 3: ERROR {ex.GetType().Name}: {ex.Message}");
    //     }
    //
    //     // Estrategia 4: ClickHiddenJs
    //     try
    //     {
    //         ClickHiddenJs("button[data-t-id='clear-datepicker'][aria-label='Clear']", TimeSpan.FromSeconds(5));
    //         Thread.Sleep(500);
    //         var val4 = WaitElement(inputLocator, TimeSpan.Zero).GetAttribute("value");
    //         sb.AppendLine(
    //             $"Strategy 4 (ClickHiddenJs): input='{val4}' {(val4 != initialValue ? "CLEARED" : "not cleared")}");
    //     }
    //     catch (Exception ex)
    //     {
    //         sb.AppendLine($"Strategy 4: ERROR {ex.GetType().Name}: {ex.Message}");
    //     }
    //
    //     // Estrategia 5: HoverAndClick
    //     try
    //     {
    //         HoverAndClick(clearBtnLocator,
    //             hoverTargetLocator ?? Loc.Tag("div").Attr("data-t-id", "date-rangepicker-inputs"),
    //             TimeSpan.FromSeconds(5));
    //         Thread.Sleep(500);
    //         var val5 = WaitElement(inputLocator, TimeSpan.Zero).GetAttribute("value");
    //         sb.AppendLine(
    //             $"Strategy 5 (HoverAndClick): input='{val5}' {(val5 != initialValue ? "CLEARED" : "not cleared")}");
    //     }
    //     catch (Exception ex)
    //     {
    //         sb.AppendLine($"Strategy 5: ERROR {ex.GetType().Name}: {ex.Message}");
    //     }
    //
    //     // Diagnóstico de visibilidad
    //     // Diagnóstico de visibilidad (el botón puede haber sido removido del DOM tras el click)
    //     try
    //     {
    //         var btnDiag = WaitElement(clearBtnLocator, TimeSpan.Zero);
    //         sb.AppendLine(
    //             $"Button opacity: {js.ExecuteScript("return getComputedStyle(arguments[0]).opacity;", btnDiag)}");
    //         sb.AppendLine($"Button displayed: {btnDiag.Displayed}");
    //         sb.AppendLine($"Button enabled: {btnDiag.Enabled}");
    //     }
    //     catch (Exception ex)
    //     {
    //         sb.AppendLine($"Button no longer in DOM (likely cleared successfully): {ex.GetType().Name}");
    //     }
    //
    //     if (hoverTargetLocator != null)
    //         try
    //         {
    //             var ht = WaitElement(hoverTargetLocator, TimeSpan.Zero);
    //             new Actions(_driver).MoveToElement(ht).Perform();
    //             Thread.Sleep(500);
    //             var btnDiag2 = WaitElement(clearBtnLocator, TimeSpan.Zero);
    //             sb.AppendLine(
    //                 $"After hover - opacity: {js.ExecuteScript("return getComputedStyle(arguments[0]).opacity;", btnDiag2)}");
    //             sb.AppendLine($"After hover - displayed: {btnDiag2.Displayed}");
    //         }
    //         catch (Exception ex)
    //         {
    //             sb.AppendLine($"After hover: {ex.GetType().Name}: {ex.Message}");
    //         }
    //
    //     return sb.ToString();
    // }

    #endregion

    #region Dropdowns

    /// <summary>
    /// Selects an option from a native &lt;select&gt; element by value or visible text.
    /// </summary>
    /// <param name="locator">The &lt;select&gt; element locator.</param>
    /// <param name="value">The value or visible text to select.</param>
    /// <param name="timeout">Optional timeout override for this operation. If null, uses <see cref="ElementWaitTimeout"/>.</param>
    public bool Select(By locator, string value, TimeSpan? timeout = null)
    {
        BeforeStep();
        var element = WaitElement(locator, timeout);
        return SelectNative(element, value);
    }

    /// <summary>
    /// Selects an option from a native &lt;select&gt; element by index.
    /// </summary>
    /// <param name="locator">The &lt;select&gt; element locator.</param>
    /// <param name="index">The 0-based index of the option to select.</param>
    /// <param name="timeout">Optional timeout override for this operation. If null, uses <see cref="ElementWaitTimeout"/>.</param>
    public bool SelectByIndex(By locator, int index, TimeSpan? timeout = null)
    {
        BeforeStep();
        var element = WaitElement(locator, timeout);
        return SelectByIndexNative(element, index);
    }

    private static bool SelectNative(IWebElement selectElement, string value)
    {
        var options = selectElement.FindElements(By.TagName("option"));

        // Try to match by value attribute first
        foreach (var option in options)
        {
            var optionValue = option.GetAttribute("value");
            if (!string.IsNullOrEmpty(optionValue) && string.Equals(optionValue, value, StringComparison.Ordinal))
            {
                option.Click();
                return true;
            }
        }

        // Try to match by visible text (case-insensitive or exact)
        foreach (var option in options)
        {
            var text = option.Text.Trim();
            if (string.Equals(text, value, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(text, value, StringComparison.Ordinal))
            {
                option.Click();
                return true;
            }
        }

        return false;
    }

    private static bool SelectByIndexNative(IWebElement selectElement, int index)
    {
        var options = selectElement.FindElements(By.TagName("option"));
        if (index < 0 || index >= options.Count) return false;
        options[index].Click();
        return true;
    }

    #endregion

    #region Diagnostics

    /// <summary>
    /// Takes a screenshot and saves it to the specified file path (PNG format).
    /// Creates the directory if it does not exist.
    /// </summary>
    /// <param name="path">The file path where the screenshot will be saved.</param>
    /// <returns>The absolute path to the saved screenshot.</returns>
    public string Screenshot(string path)
    {
        BeforeStep();
        var fullPath = Path.GetFullPath(path);
        var directory = Path.GetDirectoryName(fullPath);

        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        var screenshot = ((ITakesScreenshot)_driver).GetScreenshot();
        screenshot.SaveAsFile(fullPath);

        return fullPath;
    }

    /// <summary>
    /// Executes JavaScript in the context of the current page.
    /// </summary>
    /// <param name="script">The JavaScript code to execute.</param>
    /// <param name="args">Optional arguments to pass to the script.</param>
    /// <returns>The result of the script execution, or null if no return value.</returns>
    public object? ExecuteScript(string script, params object[] args)
    {
        BeforeStep();
        return ((IJavaScriptExecutor)_driver).ExecuteScript(script, args);
    }

    #endregion

    #region Anchor System

    /// <summary>
    /// Resolves an anchor using this client's driver.
    /// </summary>
    public Locator ResolveAnchor(AnchorLocator anchor, TimeSpan? timeout = null)
    {
        if (anchor == null)
            throw new ArgumentNullException(nameof(anchor));

        return timeout.HasValue
            ? anchor.WithTimeout(timeout.Value).Resolve(this)
            : anchor.Resolve(this);
    }

    public void Click(AnchorLocator anchor, TimeSpan? timeout = null)
    {
        if (anchor == null)
            throw new ArgumentNullException(nameof(anchor));

        Click(ResolveAnchor(anchor, timeout), timeout);
    }

    public void ClickJs(AnchorLocator anchor, TimeSpan? timeout = null)
    {
        if (anchor == null)
            throw new ArgumentNullException(nameof(anchor));

        ClickJs(ResolveAnchor(anchor, timeout), timeout);
    }

    public void ClickHidden(AnchorLocator anchor, Locator? hoverTarget = null, TimeSpan? timeout = null)
    {
        if (anchor == null)
            throw new ArgumentNullException(nameof(anchor));

        ClickHidden(ResolveAnchor(anchor, timeout), hoverTarget, timeout);
    }

    public void DoubleClick(AnchorLocator anchor, TimeSpan? timeout = null)
    {
        if (anchor == null)
            throw new ArgumentNullException(nameof(anchor));

        DoubleClick(ResolveAnchor(anchor, timeout), timeout);
    }

    public void Type(AnchorLocator anchor, string text, bool clear = true, TimeSpan? timeout = null)
    {
        if (anchor == null)
            throw new ArgumentNullException(nameof(anchor));

        Type(ResolveAnchor(anchor, timeout), text, clear, timeout);
    }

    public string GetText(AnchorLocator anchor, TimeSpan? timeout = null)
    {
        if (anchor == null)
            throw new ArgumentNullException(nameof(anchor));

        return GetText(ResolveAnchor(anchor, timeout), timeout);
    }

    public string? GetAttribute(AnchorLocator anchor, string attributeName, TimeSpan? timeout = null)
    {
        if (anchor == null)
            throw new ArgumentNullException(nameof(anchor));

        return GetAttribute(ResolveAnchor(anchor, timeout), attributeName, timeout);
    }

    public string DownloadFile(AnchorLocator anchor, string destinationPath, TimeSpan? timeout = null)
    {
        if (anchor == null)
            throw new ArgumentNullException(nameof(anchor));

        return DownloadFile(ResolveAnchor(anchor, timeout), destinationPath, timeout);
    }

    public bool IsDisplayed(AnchorLocator anchor, TimeSpan? timeout = null)
    {
        if (anchor == null)
            throw new ArgumentNullException(nameof(anchor));

        return IsDisplayed(ResolveAnchor(anchor, timeout), timeout);
    }

    #endregion
}