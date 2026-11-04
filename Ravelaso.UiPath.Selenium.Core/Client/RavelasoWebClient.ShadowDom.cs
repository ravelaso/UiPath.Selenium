using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

// ReSharper disable CheckNamespace
namespace Ravelaso.UiPath.Selenium.Core;

public partial class RavelasoWebClient
{
    /// <summary>
    /// Finds an element inside the shadow root of a host element (e.g. a Web Component).
    /// </summary>
    /// <param name="hostLocator">Locator for the host element that owns the shadow root.</param>
    /// <param name="targetLocator">Locator for the element to find inside the shadow root.</param>
    /// <param name="beforeStepDelay">Optional delay before this operation. If null, uses GlobalDelay.</param>
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
    public IWebElement FindInShadowRoot(By hostLocator, By targetLocator, TimeSpan? beforeStepDelay = null, TimeSpan? timeout = null)
    {
        BeforeStep(beforeStepDelay);
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
    public IWebElement FindInShadowRoot(Locator hostLocator, Locator targetLocator, TimeSpan? beforeStepDelay = null, TimeSpan? timeout = null)
    {
        return FindInShadowRoot(hostLocator.ToBy(), targetLocator.ToBy(), timeout);
    }

    /// <summary>
    /// Checks whether an element exists inside the shadow root of a host element.
    /// </summary>
    public bool ExistsInShadowRoot(By hostLocator, By targetLocator, TimeSpan? beforeStepDelay = null, TimeSpan? timeout = null)
    {
        BeforeStep(beforeStepDelay);
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
    public bool ExistsInShadowRoot(Locator hostLocator, Locator targetLocator, TimeSpan? beforeStepDelay = null, TimeSpan? timeout = null)
    {
        return ExistsInShadowRoot(hostLocator.ToBy(), targetLocator.ToBy(), timeout);
    }

    /// <summary>
    /// Clicks an element located inside the shadow root of a host element.
    /// </summary>
    /// <param name="hostLocator">Locator for the host element that owns the shadow root.</param>
    /// <param name="targetLocator">Locator for the element to find inside the shadow root.</param>
    /// <param name="beforeStepDelay">Optional delay before this operation. If null, uses GlobalDelay.</param>
/// <param name="timeout">Optional timeout override for this operation. If null, uses <see cref="ElementWaitTimeout"/>.</param>
    /// <example>
    /// <code>
    /// // Click a link inside a Web Component's shadow root
    /// client.ClickShadow(
    ///     By.CssSelector("alusta-navigation"),
    ///     Loc.Tag("a").Attr("href", "/ap/"));
    /// </code>
    /// </example>
    public void ClickShadow(By hostLocator, By targetLocator, TimeSpan? beforeStepDelay = null, TimeSpan? timeout = null)
    {
        BeforeStep(beforeStepDelay);
        var element = FindInShadowRoot(hostLocator, targetLocator, timeout);
        element.Click();
    }

    /// <summary>
    /// Clicks an element located inside the shadow root of a host element (Locator overload).
    /// </summary>
    public void ClickShadow(Locator hostLocator, Locator targetLocator, TimeSpan? beforeStepDelay = null, TimeSpan? timeout = null)
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
}





