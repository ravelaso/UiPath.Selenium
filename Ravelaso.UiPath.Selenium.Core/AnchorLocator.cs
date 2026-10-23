using OpenQA.Selenium;
using System.Diagnostics;
using System.Threading;

namespace Ravelaso.UiPath.Selenium.Core;

/// <summary>
/// Represents an anchor-based element locator that finds target elements relative to a reference (anchor) element.
/// Implements a fluent API for building anchor searches with optional direction constraints.
/// 
/// The anchor system automatically searches all directions (Right, Left, Below, Above, SameRow, SameContainer, Innermost, Anywhere)
/// and returns the first match found. If a specific search order is needed, use SearchOrder() or ExactDirection().
/// 
/// Example:
/// <code>
/// // Find download button in the row with date "2026-10-22"
/// var downloadBtn = Loc.Anchor(client._driver)
///     .Find(Loc.Css("button.download"))
///     .Resolve();
/// 
/// client.Click(downloadBtn);
/// </code>
/// </summary>
public class AnchorLocator
{
    private readonly IWebDriver _driver;
    private readonly Locator _anchor;
    private Locator? _target;
    private Direction[]? _searchOrder;
    private TimeSpan _timeout = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Creates a new anchor locator.
    /// </summary>
    /// <param name="driver">The WebDriver instance used for element searches.</param>
    /// <param name="anchor">The reference element (anchor point) locator.</param>
    /// <exception cref="ArgumentNullException">If driver or anchor is null.</exception>
    public AnchorLocator(IWebDriver driver, Locator anchor)
    {
        _driver = driver ?? throw new ArgumentNullException(nameof(driver));
        _anchor = anchor ?? throw new ArgumentNullException(nameof(anchor));
    }

    /// <summary>
    /// Defines the target element to find relative to the anchor.
    /// </summary>
    /// <param name="target">The locator for the target element to find.</param>
    /// <returns>This AnchorLocator instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">If target is null.</exception>
    public AnchorLocator Find(Locator target)
    {
        _target = target ?? throw new ArgumentNullException(nameof(target));
        return this;
    }

    /// <summary>
    /// Specifies the order of directions to search for the target.
    /// The anchor system will try each direction in the specified order and return the first match.
    /// If not set, defaults to exhaustive search: Right → Left → Below → Above → SameRow → SameContainer → Innermost → Anywhere.
    /// </summary>
    /// <param name="directions">The directions to try, in order of preference.</param>
    /// <returns>This AnchorLocator instance for method chaining.</returns>
    /// <exception cref="ArgumentException">If directions array is empty or null.</exception>
    public AnchorLocator SearchOrder(params Direction[] directions)
    {
        if (directions == null || directions.Length == 0)
            throw new ArgumentException("At least one direction must be specified.", nameof(directions));

        _searchOrder = directions;
        return this;
    }

    /// <summary>
    /// Restricts the search to a single, exact direction.
    /// If the target is not found in this direction, an exception is thrown.
    /// </summary>
    /// <param name="direction">The only direction to search in.</param>
    /// <returns>This AnchorLocator instance for method chaining.</returns>
    public AnchorLocator ExactDirection(Direction direction)
    {
        _searchOrder = new[] { direction };
        return this;
    }

    /// <summary>
    /// Sets a custom timeout for element search operations.
    /// Default is 5 seconds.
    /// </summary>
    /// <param name="timeout">The maximum time to wait for elements to be found.</param>
    /// <returns>This AnchorLocator instance for method chaining.</returns>
    /// <exception cref="ArgumentException">If timeout is negative or zero.</exception>
    public AnchorLocator WithTimeout(TimeSpan timeout)
    {
        if (timeout <= TimeSpan.Zero)
            throw new ArgumentException("Timeout must be greater than zero.", nameof(timeout));

        _timeout = timeout;
        return this;
    }

    /// <summary>
    /// Resolves the anchor search and returns a Locator pointing to the found target element.
    /// This Locator can then be used with RavelasoWebClient methods (Click, Type, etc.).
    /// </summary>
    /// <returns>A Locator pointing to the target element found relative to the anchor.</returns>
    /// <exception cref="InvalidOperationException">If Find() was not called before Resolve().</exception>
    /// <exception cref="NoSuchElementException">If no target element is found in any of the tried directions.</exception>
    /// <exception cref="TimeoutException">If the search exceeds the specified timeout.</exception>
    public Locator Resolve()
    {
        if (_target == null)
            throw new InvalidOperationException("Target not defined. Call Find() before Resolve().");

        var directionsToTry = _searchOrder ?? GetExhaustiveOrder();
        var anchorBy = _anchor.ToBy();
        var targetBy = _target.ToBy();
        var pollInterval = TimeSpan.FromMilliseconds(200);
        var sw = Stopwatch.StartNew();

        // Poll until the DOM (often rendered lazily by Angular/SPA frameworks) exposes
        // both the anchor and a target matching one of the requested directions.
        while (sw.Elapsed < _timeout)
        {
            try
            {
                var anchorElements = _driver.FindElements(anchorBy);
                if (anchorElements.Count > 0)
                {
                    var anchorElement = anchorElements[0];
                    var targetElements = _driver.FindElements(targetBy);

                    foreach (var direction in directionsToTry)
                    {
                        var matches = new List<IWebElement>();
                        foreach (var candidate in targetElements)
                        {
                            if (IsInDirection(_driver, anchorElement, candidate, direction))
                                matches.Add(candidate);
                        }

                        if (matches.Count > 0)
                        {
                            var closest = SelectClosestElement(anchorElement, matches);
                            return Loc.XPath(GetElementXPath(closest));
                        }
                    }
                }
            }
            catch (StaleElementReferenceException)
            {
                // The DOM changed between calls; retry on the next poll iteration.
            }

            var remaining = _timeout - sw.Elapsed;
            if (remaining <= pollInterval)
                break;

            Thread.Sleep(pollInterval);
        }

        // Nothing found in any direction within the timeout.
        throw new NoSuchElementException(
            $"Anchor search failed: could not find target relative to anchor.\n" +
            $"Anchor: {anchorBy}\n" +
            $"Target: {targetBy}\n" +
            $"Directions tried: {string.Join(", ", directionsToTry)}\n" +
            $"Timeout: {_timeout.TotalSeconds}s"
        );
    }

    /// <summary>
    /// Resolves the anchor search and returns the actual IWebElement.
    /// Used when direct element access is needed (advanced cases).
    /// </summary>
    /// <returns>The IWebElement found relative to the anchor.</returns>
    public IWebElement ResolveElement()
    {
        var locator = Resolve();
        return _driver.FindElement(locator.ToBy());
    }

    /// <summary>
    /// Gets the default exhaustive search order.
    /// Tries all directions in a logical sequence, starting with most common positions.
    /// </summary>
    private static Direction[] GetExhaustiveOrder()
    {
        return new[]
        {
            Direction.Right,         // Most common in tables (next column)
            Direction.Left,          // Alternative same-row position
            Direction.Below,         // Common in forms (label → input below)
            Direction.Above,         // Less common but possible
            Direction.SameRow,       // Same HTML row (TR or similar)
            Direction.SameContainer, // Same container (DIV, SECTION, etc.)
            Direction.Innermost,     // Child elements of anchor
            Direction.Anywhere       // Global search (last resort)
        };
    }

    /// <summary>
    /// Determines whether a candidate element satisfies the spatial/structural relationship
    /// described by <paramref name="direction"/> with respect to <paramref name="anchor"/>.
    /// Unlike the previous XPath-string approach, the target locator is always applied first
    /// (in <see cref="Resolve"/>), so the candidate is guaranteed to match the target selector.
    /// </summary>
    private static bool IsInDirection(IWebDriver driver, IWebElement anchor, IWebElement candidate, Direction direction)
    {
        if (candidate.Equals(anchor))
            return false;

        return direction switch
        {
            Direction.Innermost => IsDescendant(driver, anchor, candidate),
            Direction.SameRow => SameAncestor(driver, anchor, candidate, "tr"),
            Direction.SameContainer => SameAncestor(driver, anchor, candidate, "div,section,article,main,form"),
            Direction.Right => IsSpatiallyInDirection(driver, anchor, candidate, Direction.Right),
            Direction.Left => IsSpatiallyInDirection(driver, anchor, candidate, Direction.Left),
            Direction.Below => IsSpatiallyInDirection(driver, anchor, candidate, Direction.Below),
            Direction.Above => IsSpatiallyInDirection(driver, anchor, candidate, Direction.Above),
            Direction.Anywhere => true,
            _ => false
        };
    }

    /// <summary>
    /// Returns true when <paramref name="candidate"/> is a descendant of <paramref name="ancestor"/>.
    /// </summary>
    private static bool IsDescendant(IWebDriver driver, IWebElement ancestor, IWebElement candidate)
    {
        try
        {
            var js = (IJavaScriptExecutor)driver;
            return Convert.ToBoolean(js.ExecuteScript(
                "return arguments[0] !== arguments[1] && arguments[0].contains(arguments[1]);",
                ancestor, candidate));
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Returns true when both elements share the same nearest ancestor whose tag is one of
    /// the (comma-separated) tags in <paramref name="tags"/>.
    /// </summary>
    private static bool SameAncestor(IWebDriver driver, IWebElement a, IWebElement b, string tags)
    {
        try
        {
            var js = (IJavaScriptExecutor)driver;
            var ancestorA = js.ExecuteScript(
                "function nearest(e, list) {" +
                "  var p = e.parentElement;" +
                "  while (p && p !== document.body) {" +
                "    var t = p.tagName ? p.tagName.toLowerCase() : '';" +
                "    if (list.indexOf(t) !== -1) return p;" +
                "    p = p.parentElement;" +
                "  }" +
                "  return null;" +
                "}" +
                "return nearest(arguments[0], arguments[1].split(','));",
                a, tags) as IWebElement;
            var ancestorB = js.ExecuteScript(
                "function nearest(e, list) {" +
                "  var p = e.parentElement;" +
                "  while (p && p !== document.body) {" +
                "    var t = p.tagName ? p.tagName.toLowerCase() : '';" +
                "    if (list.indexOf(t) !== -1) return p;" +
                "    p = p.parentElement;" +
                "  }" +
                "  return null;" +
                "}" +
                "return nearest(arguments[0], arguments[1].split(','));",
                b, tags) as IWebElement;

            if (ancestorA == null || ancestorB == null)
                return false;

            return ancestorA.Equals(ancestorB);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Spatial filter used by Right/Left/Below/Above directions.
    /// </summary>
    private static bool IsSpatiallyInDirection(IWebDriver driver, IWebElement anchor, IWebElement candidate, Direction direction)
    {
        var a = AnchorLocatorHelper.GetRect(driver, anchor);
        var c = AnchorLocatorHelper.GetRect(driver, candidate);

        // Elements with no layout info (hidden/zero-size) cannot be positioned reliably.
        if (a.Width <= 0 && a.Height <= 0 && c.Width <= 0 && c.Height <= 0)
            return false;

        var anchorCenterX = a.X + a.Width / 2.0;
        var anchorCenterY = a.Y + a.Height / 2.0;
        var candidateCenterX = c.X + c.Width / 2.0;
        var candidateCenterY = c.Y + c.Height / 2.0;

        var verticallyOverlaps = c.Y < a.Y + a.Height && c.Y + c.Height > a.Y;
        var horizontallyOverlaps = c.X < a.X + a.Width && c.X + c.Width > a.X;

        var rowToleranceOk = verticallyOverlaps || Math.Abs(candidateCenterY - anchorCenterY) <= Math.Max(a.Height, c.Height) + 4;
        var columnToleranceOk = horizontallyOverlaps || Math.Abs(candidateCenterX - anchorCenterX) <= Math.Max(a.Width, c.Width) + 4;

        return direction switch
        {
            Direction.Right => candidateCenterX > anchorCenterX
                && c.X >= a.X + a.Width * 0.5
                && rowToleranceOk,
            Direction.Left => candidateCenterX < anchorCenterX
                && c.X + c.Width <= a.X + a.Width * 0.5
                && rowToleranceOk,
            Direction.Below => candidateCenterY > anchorCenterY
                && c.Y >= a.Y + a.Height * 0.5
                && columnToleranceOk,
            Direction.Above => candidateCenterY < anchorCenterY
                && c.Y + c.Height <= a.Y + a.Height * 0.5
                && columnToleranceOk,
            _ => false
        };
    }

    /// <summary>
    /// Selects the closest element from a collection when multiple matches exist.
    /// Uses pixel-distance calculation based on element bounding rectangles.
    /// </summary>
    private IWebElement SelectClosestElement(IWebElement anchor, IEnumerable<IWebElement> elements)
    {
        if (!elements.Any())
            throw new InvalidOperationException("Elements collection is empty.");

        var closest = elements
            .Select(e => new { Element = e, Distance = AnchorLocatorHelper.CalculateDistance(_driver, anchor, e) })
            .OrderBy(x => x.Distance)
            .FirstOrDefault();

        return closest?.Element ?? elements.First();
    }

    /// <summary>
    /// Gets an XPath that uniquely identifies a specific IWebElement.
    /// Uses JavaScript to compute the element's XPath.
    /// </summary>
    private string GetElementXPath(IWebElement element)
    {
        try
        {
            var js = (IJavaScriptExecutor)_driver;
            var xpath = js.ExecuteScript(
                "function getXPath(element) {" +
                "  if (element.id !== '')" +
                "    return \"//*[@id='\" + element.id + \"']\";" +
                "  if (element === document.body)" +
                "    return \"//body\";" +
                "  var ix = 0;" +
                "  var siblings = element.parentNode.childNodes;" +
                "  for (var i = 0; i < siblings.length; i++) {" +
                "    var sibling = siblings[i];" +
                "    if (sibling === element)" +
                "      return getXPath(element.parentNode) + '/' + element.tagName.toLowerCase() + '[' + (ix + 1) + ']';" +
                "    if (sibling.nodeType === 1 && sibling.tagName.toLowerCase() === element.tagName.toLowerCase())" +
                "      ix++;" +
                "  }" +
                "}" +
                "return getXPath(arguments[0]);",
                element
            );

            return xpath?.ToString() ?? "//*";
        }
        catch
        {
            return "//*";
        }
    }
}
