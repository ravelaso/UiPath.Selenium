using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace Ravelaso.UiPath.Selenium.Core;

/// <summary>
/// Main Selenium WebDriver client wrapper. Encapsulates a driver instance and provides a simplified,
/// fluent API with automatic global delays, conditional waits, and simplified element locators.
/// </summary>
/// <remarks>
/// The client is designed to:
/// 1. Accept a browser type and create the corresponding driver via <see cref="BrowserDriverFactory"/>.
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
    /// Gets the browser type in use.
    /// </summary>
    public BrowserType Browser { get; }

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
    /// Initializes a new client for the specified browser with default options.
    /// </summary>
    /// <param name="browser">The browser type to launch.</param>
    public RavelasoWebClient(BrowserType browser) : this(browser, null) { }

    /// <summary>
    /// Initializes a new client for the specified browser with custom options.
    /// </summary>
    /// <param name="browser">The browser type to launch.</param>
    /// <param name="options">Configuration options. If null, defaults are used.</param>
    public RavelasoWebClient(BrowserType browser, BrowserOptions? options)
    {
        options ??= new BrowserOptions();
        options.Browser = browser;

        Browser = browser;
        Options = options;
        _driver = BrowserDriverFactory.Create(browser, options);
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
    /// Waits for an element to appear on the page and returns it when found.
    /// Uses <see cref="WebDriverWait"/> with the specified or default timeout.
    /// </summary>
    /// <param name="locator">The element locator.</param>
    /// <param name="timeout">Optional custom timeout. If null, uses <see cref="ElementWaitTimeout"/>.</param>
    /// <returns>The found <see cref="IWebElement"/>.</returns>
    /// <exception cref="WebDriverTimeoutException">If the element is not found within the timeout.</exception>
    /// <exception cref="NoSuchElementException">If the locator produces no results.</exception>
    protected IWebElement WaitElement(By locator, TimeSpan? timeout = null)
    {
        var wait = timeout ?? ElementWaitTimeout;

        if (wait <= TimeSpan.Zero)
            return _driver.FindElement(locator);

        var webDriverWait = new WebDriverWait(_driver, wait);
        return webDriverWait.Until(driver => driver.FindElement(locator));
    }

    /// <summary>
    /// Closes the browser and releases the driver resources. Idempotent.
    /// </summary>
    public void Quit()
    {
        if (_disposed) return;
        _disposed = true;

        try
        {
            _driver.Quit();
        }
        catch (WebDriverException) { /* Driver already closed */ }
        finally
        {
            try { _driver.Dispose(); }
            catch { /* Dispose already handled */ }
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

    #region Navigation

    /// <summary>
    /// Navigates to the specified URL.
    /// </summary>
    /// <param name="url">The URL to navigate to.</param>
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
    /// <exception cref="WebDriverTimeoutException">If the element is not found within the timeout.</exception>
    public void Click(By locator)
    {
        BeforeStep();
        var element = WaitElement(locator);
        element.Click();
    }

    /// <summary>
    /// Enters text into an element. Optionally clears the field first.
    /// </summary>
    /// <param name="locator">The element locator.</param>
    /// <param name="text">The text to enter.</param>
    /// <param name="clear">If true, clears the field before typing. Default: true.</param>
    /// <exception cref="WebDriverTimeoutException">If the element is not found within the timeout.</exception>
    public void Type(By locator, string text, bool clear = true)
    {
        BeforeStep();
        var element = WaitElement(locator);

        if (clear)
        {
            try { element.Clear(); }
            catch (InvalidElementStateException) { /* Element not a text input; proceed */ }
        }

        element.SendKeys(text);
    }

    /// <summary>
    /// Gets the visible text of an element.
    /// </summary>
    /// <param name="locator">The element locator.</param>
    /// <returns>The visible text content.</returns>
    /// <exception cref="WebDriverTimeoutException">If the element is not found within the timeout.</exception>
    public string GetText(By locator)
    {
        BeforeStep();
        return WaitElement(locator).Text;
    }

    /// <summary>
    /// Gets the value of a specific attribute on an element.
    /// </summary>
    /// <param name="locator">The element locator.</param>
    /// <param name="attributeName">The attribute name (e.g., "value", "href", "data-id").</param>
    /// <returns>The attribute value, or null if the attribute does not exist.</returns>
    /// <exception cref="WebDriverTimeoutException">If the element is not found within the timeout.</exception>
    public string? GetAttribute(By locator, string attributeName)
    {
        BeforeStep();
        return WaitElement(locator).GetAttribute(attributeName);
    }

    #endregion

    #region Detection

    /// <summary>
    /// Checks if an element exists on the page (immediate check, no conditional wait).
    /// </summary>
    /// <param name="locator">The element locator.</param>
    /// <returns>True if the element is found; false otherwise.</returns>
    public bool Exists(By locator)
        => Exists(locator, TimeSpan.Zero);

    /// <summary>
    /// Checks if an element exists on the page, waiting up to the specified timeout.
    /// </summary>
    /// <param name="locator">The element locator.</param>
    /// <param name="timeout">The timeout to wait for the element.</param>
    /// <returns>True if the element appears within the timeout; false otherwise.</returns>
    public bool Exists(By locator, TimeSpan timeout)
    {
        BeforeStep();
        try
        {
            WaitElement(locator, timeout);
            return true;
        }
        catch (WebDriverTimeoutException) { return false; }
        catch (NotFoundException) { return false; }
    }

    /// <summary>
    /// Checks if an element is displayed on the page.
    /// </summary>
    /// <param name="locator">The element locator.</param>
    /// <returns>True if the element exists and is displayed; false otherwise.</returns>
    public bool IsDisplayed(By locator)
    {
        BeforeStep();
        try
        {
            var element = WaitElement(locator, TimeSpan.Zero);
            return element.Displayed;
        }
        catch (NotFoundException) { return false; }
        catch (WebDriverTimeoutException) { return false; }
    }

    /// <summary>
    /// Waits for an element to appear on the page using the default timeout.
    /// </summary>
    /// <param name="locator">The element locator.</param>
    /// <returns>True if the element appears within the timeout; false otherwise.</returns>
    public bool WaitFor(By locator)
        => WaitFor(locator, ElementWaitTimeout);

    /// <summary>
    /// Waits for an element to appear on the page with a custom timeout.
    /// </summary>
    /// <param name="locator">The element locator.</param>
    /// <param name="timeout">The timeout to wait for the element.</param>
    /// <returns>True if the element appears within the timeout; false otherwise.</returns>
    public bool WaitFor(By locator, TimeSpan timeout)
    {
        BeforeStep();
        try
        {
            WaitElement(locator, timeout);
            return true;
        }
        catch (WebDriverTimeoutException) { return false; }
        catch (NotFoundException) { return false; }
    }

    #endregion

    #region Dropdowns

    /// <summary>
    /// Selects an option from a native &lt;select&gt; element by value or visible text.
    /// Attempts to match by value attribute first; if not found, matches by visible text (case-insensitive).
    /// </summary>
    /// <param name="locator">The &lt;select&gt; element locator.</param>
    /// <param name="value">The value or visible text to select.</param>
    /// <returns>True if the option was found and selected; false otherwise.</returns>
    public bool Select(By locator, string value)
    {
        BeforeStep();
        var element = WaitElement(locator);
        return SelectNative(element, value);
    }

    /// <summary>
    /// Selects an option from a native &lt;select&gt; element by index.
    /// </summary>
    /// <param name="locator">The &lt;select&gt; element locator.</param>
    /// <param name="index">The 0-based index of the option to select.</param>
    /// <returns>True if the index is valid and the option was selected; false otherwise.</returns>
    public bool SelectByIndex(By locator, int index)
    {
        BeforeStep();
        var element = WaitElement(locator);
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
            var text = (option.Text).Trim();
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
}
