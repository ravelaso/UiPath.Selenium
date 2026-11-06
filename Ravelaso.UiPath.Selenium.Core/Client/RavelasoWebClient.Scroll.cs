using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.Interactions;
using System.Diagnostics;

// ReSharper disable CheckNamespace
namespace Ravelaso.UiPath.Selenium.Core;

public enum ScrollDirection { Down = 0, Up = 1, Left = 2, Right = 3 }

public partial class RavelasoWebClient
{
    private const int DefaultScrollStep = 300;

    // ---------- Scroll by amount (pixels) ----------
    public bool Scroll(ScrollDirection direction, int amount, TimeSpan? beforeStepDelay = null)
    {
        BeforeStep(beforeStepDelay);
        var (dx, dy) = DirectionToDelta(direction, amount);
        PerformScrollStep(dx, dy);
        return true;
    }

    // ---------- Scroll until target is found ----------
    public bool ScrollTo(ScrollDirection direction, By locator, TimeSpan? beforeStepDelay = null, TimeSpan? timeout = null)
        => ScrollUntilFound(locator, direction, DefaultScrollStep, beforeStepDelay, timeout);

    public bool ScrollTo(ScrollDirection direction, Locator locator, TimeSpan? beforeStepDelay = null, TimeSpan? timeout = null)
        => ScrollTo(direction, locator.ToBy(), beforeStepDelay, timeout);

    public bool ScrollTo(ScrollDirection direction, AnchorLocator anchor, TimeSpan? beforeStepDelay = null, TimeSpan? timeout = null)
        => ScrollUntilFoundAnchor(anchor, direction, DefaultScrollStep, beforeStepDelay, timeout);

    // ---------- Internals ----------
    private static (int dx, int dy) DirectionToDelta(ScrollDirection direction, int amount)
        => direction switch
        {
            ScrollDirection.Down => (0, amount),
            ScrollDirection.Up => (0, -amount),
            ScrollDirection.Left => (-amount, 0),
            ScrollDirection.Right => (amount, 0),
            _ => (0, 0)
        };

    private bool ScrollUntilFound(By locator, ScrollDirection direction, int step, TimeSpan? beforeStepDelay, TimeSpan? timeout)
    {
        BeforeStep(beforeStepDelay);
        var wait = timeout ?? ElementWaitTimeout;
        if (wait <= TimeSpan.Zero) wait = TimeSpan.FromSeconds(10);

        var (sdx, sdy) = DirectionToDelta(direction, step);
        var signX = sdx == 0 ? 0 : (sdx > 0 ? 1 : -1);
        var signY = sdy == 0 ? 0 : (sdy > 0 ? 1 : -1);
        var absStep = Math.Max(50, step);

        var sw = Stopwatch.StartNew();
        if (ElementExists(locator)) return true;

        while (sw.Elapsed < wait)
        {
            if (ElementExists(locator)) return true;
            if (!CanScroll(signX, signY)) break;
            PerformScrollStep(signX * absStep, signY * absStep);
            Thread.Sleep(150);
        }
        return ElementExists(locator);
    }

    private bool ScrollUntilFoundAnchor(AnchorLocator anchor, ScrollDirection direction, int step, TimeSpan? beforeStepDelay, TimeSpan? timeout)
    {
        BeforeStep(beforeStepDelay);
        var wait = timeout ?? ElementWaitTimeout;
        if (wait <= TimeSpan.Zero) wait = TimeSpan.FromSeconds(10);

        var (sdx, sdy) = DirectionToDelta(direction, step);
        var signX = sdx == 0 ? 0 : (sdx > 0 ? 1 : -1);
        var signY = sdy == 0 ? 0 : (sdy > 0 ? 1 : -1);
        var absStep = Math.Max(50, step);

        var sw = Stopwatch.StartNew();
        if (AnchorExists(anchor)) return true;

        while (sw.Elapsed < wait)
        {
            if (AnchorExists(anchor)) return true;
            if (!CanScroll(signX, signY)) break;
            PerformScrollStep(signX * absStep, signY * absStep);
            Thread.Sleep(150);
        }
        return AnchorExists(anchor);
    }

    private void PerformScrollStep(int deltaX, int deltaY)
    {
        // 1) CDP wheel event (Chromium): lands on the scroll container under the cursor
        if (TryCdpWheel(deltaX, deltaY)) return;

        // 2) Selenium wheel action
        try { new Actions(_driver).ScrollByAmount(deltaX, deltaY).Perform(); return; }
        catch { }

        // 3) JavaScript fallback
        try { ((IJavaScriptExecutor)_driver).ExecuteScript("window.scrollBy(arguments[0], arguments[1]);", deltaX, deltaY); }
        catch { }
    }

    private bool TryCdpWheel(int deltaX, int deltaY)
    {
        if (_driver is not ChromeDriver and not EdgeDriver) return false;
        try
        {
            var js = (IJavaScriptExecutor)_driver;
            var w = Convert.ToInt32(js.ExecuteScript("return window.innerWidth || 0;"));
            var h = Convert.ToInt32(js.ExecuteScript("return window.innerHeight || 0;"));
            var parameters = new Dictionary<string, object?>
            {
                ["type"] = "mouseWheel",
                ["x"] = Math.Max(1, w / 2),
                ["y"] = Math.Max(1, h / 2),
                ["deltaX"] = deltaX,
                ["deltaY"] = deltaY
            };
            ExecuteCdp(_driver, "Input.dispatchMouseEvent", parameters);
            return true;
        }
        catch { return false; }
    }

    private static void ExecuteCdp(IWebDriver driver, string command, Dictionary<string, object?> parameters)
    {
        if (driver is ChromeDriver chrome) { chrome.ExecuteCdpCommand(command, parameters); return; }
        if (driver is EdgeDriver edge) { edge.ExecuteCdpCommand(command, parameters); return; }
        throw new NotSupportedException("CDP is only supported on Chromium-based browsers (Chrome/Edge).");
    }

    private bool ElementExists(By locator)
    {
        try { _driver.FindElement(locator); return true; }
        catch (NoSuchElementException) { return FindInShadowRoots(locator) != null; }
        catch { try { return FindInShadowRoots(locator) != null; } catch { return false; } }
    }

    private bool AnchorExists(AnchorLocator anchor)
    {
        if (anchor == null) return false;
        try { anchor.WithTimeout(TimeSpan.FromMilliseconds(200)).Resolve(this); return true; }
        catch { return false; }
    }

    private bool CanScroll(int signX, int signY)
    {
        try
        {
            var js = (IJavaScriptExecutor)_driver;
            if (signY > 0)
            {
                var scrollTop = Convert.ToDouble(js.ExecuteScript("return window.scrollY || document.documentElement.scrollTop || 0;"));
                var max = Convert.ToDouble(js.ExecuteScript("return (document.documentElement.scrollHeight - window.innerHeight);"));
                return scrollTop < max - 2;
            }
            if (signY < 0)
            {
                var scrollTop = Convert.ToDouble(js.ExecuteScript("return window.scrollY || document.documentElement.scrollTop || 0;"));
                return scrollTop > 2;
            }
            if (signX > 0)
            {
                var scrollLeft = Convert.ToDouble(js.ExecuteScript("return window.scrollX || document.documentElement.scrollLeft || 0;"));
                var max = Convert.ToDouble(js.ExecuteScript("return (document.documentElement.scrollWidth - window.innerWidth);"));
                return scrollLeft < max - 2;
            }
            if (signX < 0)
            {
                var scrollLeft = Convert.ToDouble(js.ExecuteScript("return window.scrollX || document.documentElement.scrollLeft || 0;"));
                return scrollLeft > 2;
            }
        }
        catch { return true; }
        return true;
    }
}