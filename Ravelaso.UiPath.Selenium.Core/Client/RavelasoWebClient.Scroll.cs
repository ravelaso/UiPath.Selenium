using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.Interactions;
using System.Diagnostics;

// ReSharper disable CheckNamespace
namespace Ravelaso.UiPath.Selenium.Core;

public enum ScrollDirection { Down = 0, Up = 1, Left = 2, Right = 3 }

public class ScrollableInfo
{
    public int Index { get; set; }
    public string Tag { get; set; } = string.Empty;
    public string Id { get; set; } = string.Empty;
    public string ClassName { get; set; } = string.Empty;
    public int ClientWidth { get; set; }
    public int ClientHeight { get; set; }
    public int ScrollWidth { get; set; }
    public int ScrollHeight { get; set; }
    public int ScrollLeft { get; set; }
    public int ScrollTop { get; set; }
}

public partial class RavelasoWebClient
{
    private const int DefaultScrollStep = 300;

    // ---------- Scroll by amount (pixels) ----------
    public bool Scroll(ScrollDirection direction, int amount, TimeSpan? beforeStepDelay = null)
    {
        BeforeStep(beforeStepDelay);
        var (dx, dy) = DirectionToDelta(direction, amount);

        var container = ResolveDefaultScrollContainer(direction);
        if (container != null)
            return ScrollElement(container, dx, dy);

        PerformScrollStep(dx, dy);
        return true;
    }

    // ---------- Scroll by amount on a specific container ----------
    public bool Scroll(ScrollDirection direction, int amount, By container, TimeSpan? beforeStepDelay = null)
    {
        BeforeStep(beforeStepDelay);
        var (dx, dy) = DirectionToDelta(direction, amount);
        var element = ResolveScrollContainer(container);
        if (element == null) return false;
        return ScrollElement(element, dx, dy);
    }

    public bool Scroll(ScrollDirection direction, int amount, Locator container, TimeSpan? beforeStepDelay = null)
        => Scroll(direction, amount, container.ToBy(), beforeStepDelay);

    public bool Scroll(ScrollDirection direction, int amount, AnchorLocator container, TimeSpan? beforeStepDelay = null)
    {
        BeforeStep(beforeStepDelay);
        var (dx, dy) = DirectionToDelta(direction, amount);
        var resolved = container.Resolve(this);
        var element = ResolveScrollContainer(resolved);
        if (element == null) return false;
        return ScrollElement(element, dx, dy);
    }

    // ---------- Scroll until target is found (auto-detected scroller) ----------
    public bool ScrollTo(ScrollDirection direction, By target, TimeSpan? beforeStepDelay = null, TimeSpan? timeout = null)
    {
        var container = ResolveDefaultScrollContainer(direction);
        var found = ScrollUntilFound(target, direction, DefaultScrollStep, container, beforeStepDelay, timeout);
        if (!found)
            throw new NoSuchElementException($"ScrollTo: target not found after scrolling {direction}.");
        return true;
    }

    public bool ScrollTo(ScrollDirection direction, Locator target, TimeSpan? beforeStepDelay = null, TimeSpan? timeout = null)
        => ScrollTo(direction, target.ToBy(), beforeStepDelay, timeout);

    public bool ScrollTo(ScrollDirection direction, AnchorLocator target, TimeSpan? beforeStepDelay = null, TimeSpan? timeout = null)
    {
        BeforeStep(beforeStepDelay);
        var resolved = target.Resolve(this);
        return ScrollUntilFound(resolved, direction, DefaultScrollStep, null, beforeStepDelay, timeout);
    }

    // ---------- Scroll until target is found inside a specific container ----------
    public bool ScrollTo(ScrollDirection direction, By target, By container, TimeSpan? beforeStepDelay = null, TimeSpan? timeout = null)
    {
        var ce = ResolveScrollContainer(container);
        var found = ScrollUntilFound(target, direction, DefaultScrollStep, ce, beforeStepDelay, timeout);
        if (!found)
            throw new NoSuchElementException($"ScrollTo: target not found after scrolling {direction}.");
        return true;
    }

    public bool ScrollTo(ScrollDirection direction, Locator target, Locator container, TimeSpan? beforeStepDelay = null, TimeSpan? timeout = null)
        => ScrollTo(direction, target.ToBy(), container.ToBy(), beforeStepDelay, timeout);

    public bool ScrollTo(ScrollDirection direction, AnchorLocator target, AnchorLocator container, TimeSpan? beforeStepDelay = null, TimeSpan? timeout = null)
    {
        BeforeStep(beforeStepDelay);
        var resolvedTarget = target.Resolve(this);
        var resolvedContainerLocator = container.Resolve(this);
        var ce = ResolveScrollContainer(resolvedContainerLocator.ToBy());
        var found = ScrollUntilFound(resolvedTarget.ToBy(), direction, DefaultScrollStep, ce, beforeStepDelay, timeout);
        if (!found)
            throw new NoSuchElementException($"ScrollTo: target not found after scrolling {direction}.");
        return true;
    }

    // ---------- Debug helper: list scrollable containers ----------
    public List<ScrollableInfo> GetScrollables()
    {
        var js = (IJavaScriptExecutor)_driver;
        var result = js.ExecuteScript(
            "var out = [];" +
            "var all = Array.from(document.querySelectorAll('*'));" +
            "all.forEach(function(el, i) {" +
            "  var s = window.getComputedStyle(el);" +
            "  var ox = s.overflowX, oy = s.overflowY;" +
            "  var scrollable = ox==='auto'||ox==='scroll'||oy==='auto'||oy==='scroll';" +
            "  var hasOverflow = el.scrollWidth > el.clientWidth + 1 || el.scrollHeight > el.clientHeight + 1;" +
            "  if (scrollable && hasOverflow) {" +
            "    out.push({" +
            "      index: out.length," +
            "      tag: el.tagName.toLowerCase()," +
            "      id: el.id || ''," +
            "      className: el.className || ''," +
            "      clientWidth: el.clientWidth," +
            "      clientHeight: el.clientHeight," +
            "      scrollWidth: el.scrollWidth," +
            "      scrollHeight: el.scrollHeight," +
            "      scrollLeft: el.scrollLeft," +
            "      scrollTop: el.scrollTop" +
            "    });" +
            "  }" +
            "});" +
            "return out;");

        var list = new List<ScrollableInfo>();
        if (result is System.Collections.IEnumerable enumerable)
        {
            foreach (var item in enumerable)
            {
                if (item is IDictionary<string, object> dict)
                {
                    list.Add(new ScrollableInfo
                    {
                        Index = Convert.ToInt32(dict["index"]),
                        Tag = dict["tag"]?.ToString() ?? string.Empty,
                        Id = dict["id"]?.ToString() ?? string.Empty,
                        ClassName = dict["className"]?.ToString() ?? string.Empty,
                        ClientWidth = Convert.ToInt32(dict["clientWidth"]),
                        ClientHeight = Convert.ToInt32(dict["clientHeight"]),
                        ScrollWidth = Convert.ToInt32(dict["scrollWidth"]),
                        ScrollHeight = Convert.ToInt32(dict["scrollHeight"]),
                        ScrollLeft = Convert.ToInt32(dict["scrollLeft"]),
                        ScrollTop = Convert.ToInt32(dict["scrollTop"])
                    });
                }
            }
        }
        return list;
    }

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

    private IWebElement? ResolveDefaultScrollContainer(ScrollDirection direction)
    {
        var scrollables = GetScrollables();
        if (scrollables.Count == 0) return null;

        bool horizontal = direction == ScrollDirection.Left || direction == ScrollDirection.Right;

        var candidates = scrollables.Where(s =>
            horizontal
                ? s.ScrollWidth > s.ClientWidth + 1
                : s.ScrollHeight > s.ClientHeight + 1).ToList();

        if (candidates.Count == 0) return null;

        if (horizontal)
            candidates.Sort((a, b) =>
            {
                if (b.ClientHeight != a.ClientHeight) return b.ClientHeight - a.ClientHeight;
                return (b.ScrollWidth - b.ClientWidth) - (a.ScrollWidth - a.ClientWidth);
            });
        else
            candidates.Sort((a, b) =>
            {
                if (b.ClientWidth != a.ClientWidth) return b.ClientWidth - a.ClientWidth;
                return (b.ScrollHeight - b.ClientHeight) - (a.ScrollHeight - a.ClientHeight);
            });

        return ResolveScrollableByIndex(candidates[0].Index);
    }

    private IWebElement? ResolveScrollableByIndex(int index)
    {
        var js = (IJavaScriptExecutor)_driver;
        try
        {
            return js.ExecuteScript(
                "var idx = arguments[0];" +
                "var all = Array.from(document.querySelectorAll('*'));" +
                "var scrollables = all.filter(function(el) {" +
                "  var s = window.getComputedStyle(el);" +
                "  var ox = s.overflowX, oy = s.overflowY;" +
                "  var scrollable = ox==='auto'||ox==='scroll'||oy==='auto'||oy==='scroll';" +
                "  var hasOverflow = el.scrollWidth > el.clientWidth + 1 || el.scrollHeight > el.clientHeight + 1;" +
                "  return scrollable && hasOverflow;" +
                "});" +
                "if (idx < 0 || idx >= scrollables.length) return null;" +
                "return scrollables[idx];",
                index) as IWebElement;
        }
        catch
        {
            return null;
        }
    }

    private IWebElement? ResolveScrollContainer(By locator)
    {
        IWebElement? element;
        try { element = WaitElement(locator); }
        catch { return null; }

        // If the element itself is scrollable, use it; otherwise find nearest scrollable ancestor.
        var js = (IJavaScriptExecutor)_driver;
        var resolved = js.ExecuteScript(
            "function nearestScrollable(el) {" +
            "  while (el && el !== document.body) {" +
            "    var s = window.getComputedStyle(el);" +
            "    var ox = s.overflowX, oy = s.overflowY;" +
            "    var scrollable = ox==='auto'||ox==='scroll'||oy==='auto'||oy==='scroll';" +
            "    var hasOverflow = el.scrollWidth > el.clientWidth + 1 || el.scrollHeight > el.clientHeight + 1;" +
            "    if (scrollable && hasOverflow) return el;" +
            "    el = el.parentElement;" +
            "  }" +
            "  return document.scrollingElement || document.documentElement;" +
            "}" +
            "return nearestScrollable(arguments[0]);",
            element) as IWebElement;

        return resolved ?? element;
    }

    private bool ScrollElement(IWebElement element, int deltaX, int deltaY)
    {
        // 1) JS scrollBy on the element itself
        try
        {
            ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].scrollBy(arguments[1], arguments[2]);", element, deltaX, deltaY);
            return true;
        }
        catch { }

        // 2) CDP wheel at the element's center
        try
        {
            var rect = ((IJavaScriptExecutor)_driver).ExecuteScript(
                "const r = arguments[0].getBoundingClientRect(); return {x: r.x + r.width/2, y: r.y + r.height/2};",
                element) as Dictionary<string, object>;
            if (rect == null) throw new InvalidOperationException("Could not get element rect.");
            var x = Convert.ToInt32(rect["x"]);
            var y = Convert.ToInt32(rect["y"]);
            var parameters = new Dictionary<string, object?>
            {
                ["type"] = "mouseWheel",
                ["x"] = x,
                ["y"] = y,
                ["deltaX"] = deltaX,
                ["deltaY"] = deltaY
            };
            ExecuteCdp(_driver, "Input.dispatchMouseEvent", parameters);
            return true;
        }
        catch { }

        return false;
    }

    private bool ScrollUntilFound(By target, ScrollDirection direction, int step, IWebElement? containerElement, TimeSpan? beforeStepDelay, TimeSpan? timeout)
    {
        BeforeStep(beforeStepDelay);
        var wait = timeout ?? ElementWaitTimeout;
        if (wait <= TimeSpan.Zero) wait = TimeSpan.FromSeconds(10);

        var (sdx, sdy) = DirectionToDelta(direction, step);
        var signX = sdx == 0 ? 0 : (sdx > 0 ? 1 : -1);
        var signY = sdy == 0 ? 0 : (sdy > 0 ? 1 : -1);
        var absStep = Math.Max(50, step);

        var sw = Stopwatch.StartNew();
        if (ElementInView(target, containerElement)) return true;

        while (sw.Elapsed < wait)
        {
            if (ElementInView(target, containerElement)) return true;
            if (containerElement != null)
            {
                if (!CanScrollElement(containerElement, signX, signY)) break;
                ScrollElement(containerElement, signX * absStep, signY * absStep);
            }
            else
            {
                if (!CanScroll(signX, signY)) break;
                PerformScrollStep(signX * absStep, signY * absStep);
            }
            Thread.Sleep(150);
        }
        return ElementInView(target, containerElement);
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

    private bool ElementInView(By locator, IWebElement? container = null)
    {
        try
        {
            var element = _driver.FindElement(locator);
            if (!element.Displayed) return false;

            var js = (IJavaScriptExecutor)_driver;
            var rect = js.ExecuteScript(
                "const r = arguments[0].getBoundingClientRect();" +
                "return {top: r.top, bottom: r.bottom, left: r.left, right: r.right};",
                element) as Dictionary<string, object>;
            if (rect == null) return false;

            var top = Convert.ToDouble(rect["top"]);
            var bottom = Convert.ToDouble(rect["bottom"]);
            var left = Convert.ToDouble(rect["left"]);
            var right = Convert.ToDouble(rect["right"]);

            double vh, vw, cTop = 0, cLeft = 0;
            if (container != null)
            {
                var crect = js.ExecuteScript(
                    "const r = arguments[0].getBoundingClientRect();" +
                    "return {top: r.top, left: r.left, width: r.width, height: r.height};",
                    container) as Dictionary<string, object>;
                if (crect == null) return false;
                cTop = Convert.ToDouble(crect["top"]);
                cLeft = Convert.ToDouble(crect["left"]);
                vw = Convert.ToDouble(crect["width"]);
                vh = Convert.ToDouble(crect["height"]);
            }
            else
            {
                vh = Convert.ToDouble(js.ExecuteScript("return window.innerHeight;"));
                vw = Convert.ToDouble(js.ExecuteScript("return window.innerWidth;"));
            }

            // intersect element rect with container/window rect
            var eTop = top - cTop;
            var eBottom = bottom - cTop;
            var eLeft = left - cLeft;
            var eRight = right - cLeft;

            return eBottom > 0 && eTop < vh && eRight > 0 && eLeft < vw;
        }
        catch (NoSuchElementException)
        {
            return false;
        }
        catch
        {
            return false;
        }
    }

    private bool ElementExists(By locator)
    {
        try { _driver.FindElement(locator); return true; }
        catch (NoSuchElementException) { return FindInShadowRoots(locator) != null; }
        catch { try { return FindInShadowRoots(locator) != null; } catch { return false; } }
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

    private bool CanScrollElement(IWebElement element, int signX, int signY)
    {
        try
        {
            var js = (IJavaScriptExecutor)_driver;
            if (signY > 0)
            {
                var scrollTop = Convert.ToDouble(js.ExecuteScript("return arguments[0].scrollTop;", element));
                var max = Convert.ToDouble(js.ExecuteScript("return arguments[0].scrollHeight - arguments[0].clientHeight;", element));
                return scrollTop < max - 2;
            }
            if (signY < 0)
            {
                var scrollTop = Convert.ToDouble(js.ExecuteScript("return arguments[0].scrollTop;", element));
                return scrollTop > 2;
            }
            if (signX > 0)
            {
                var scrollLeft = Convert.ToDouble(js.ExecuteScript("return arguments[0].scrollLeft;", element));
                var max = Convert.ToDouble(js.ExecuteScript("return arguments[0].scrollWidth - arguments[0].clientWidth;", element));
                return scrollLeft < max - 2;
            }
            if (signX < 0)
            {
                var scrollLeft = Convert.ToDouble(js.ExecuteScript("return arguments[0].scrollLeft;", element));
                return scrollLeft > 2;
            }
        }
        catch { return true; }
        return true;
    }
}