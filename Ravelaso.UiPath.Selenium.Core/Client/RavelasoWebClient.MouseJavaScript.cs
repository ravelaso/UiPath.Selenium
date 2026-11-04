using OpenQA.Selenium;
using OpenQA.Selenium.Interactions;
using System.Diagnostics;

// ReSharper disable CheckNamespace
namespace Ravelaso.UiPath.Selenium.Core;

public partial class RavelasoWebClient
{
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
}