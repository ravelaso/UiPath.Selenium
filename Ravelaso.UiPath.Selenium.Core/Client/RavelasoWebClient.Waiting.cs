using OpenQA.Selenium;
// ReSharper disable MethodOverloadWithOptionalParameter

// ReSharper disable CheckNamespace
namespace Ravelaso.UiPath.Selenium.Core;

public partial class RavelasoWebClient
{
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
    /// <param name="beforeStepDelay">Optional delay before this operation. If null, uses GlobalDelay.</param>
/// <param name="timeout">The timeout to wait for the element. If null, performs an immediate check.</param>
    /// <returns>True if the element appears within the timeout; false otherwise.</returns>
    public bool Exists(By locator, TimeSpan? beforeStepDelay = null, TimeSpan? timeout = null)
    {
        BeforeStep(beforeStepDelay);
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
    /// <param name="beforeStepDelay">Optional delay before this operation. If null, uses GlobalDelay.</param>
/// <param name="timeout">Optional timeout override for this operation. If null, performs an immediate check.</param>
    public bool IsDisplayed(By locator, TimeSpan? beforeStepDelay = null, TimeSpan? timeout = null)
    {
        BeforeStep(beforeStepDelay);
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
    /// <param name="beforeStepDelay">Optional delay before this operation. If null, uses GlobalDelay.</param>
/// <param name="timeout">The timeout to wait for the element. If null, uses <see cref="ElementWaitTimeout"/>.</param>
    /// <returns>True if the element appears within the timeout; false otherwise.</returns>
    public bool WaitFor(By locator, TimeSpan? beforeStepDelay = null, TimeSpan? timeout = null)
    {
        BeforeStep(beforeStepDelay);
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
    /// <param name="beforeStepDelay">Optional delay before this operation. If null, uses GlobalDelay.</param>
/// <param name="timeout">Maximum time to wait. If null, uses <see cref="ElementWaitTimeout"/>.</param>
    /// <returns>True if the anchored element appears within the timeout; false otherwise.</returns>
    public bool WaitFor(AnchorLocator anchor, TimeSpan? beforeStepDelay = null, TimeSpan? timeout = null)
    {
        BeforeStep(beforeStepDelay);

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
    /// <param name="beforeStepDelay">Optional delay before this operation. If null, uses GlobalDelay.</param>
/// <param name="timeout">Maximum time to wait. If null, performs an immediate check.</param>
    /// <returns>True if the anchored element appears within the timeout; false otherwise.</returns>
    public bool Exists(AnchorLocator anchor, TimeSpan? beforeStepDelay = null, TimeSpan? timeout = null)
    {
        BeforeStep(beforeStepDelay);

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
}

