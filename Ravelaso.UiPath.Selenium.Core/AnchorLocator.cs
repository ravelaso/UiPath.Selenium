using OpenQA.Selenium;
using System.Diagnostics;

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
        var sw = Stopwatch.StartNew();
        var anchorBy = _anchor.ToBy();
        var targetBy = _target.ToBy();

        foreach (var direction in directionsToTry)
        {
            if (sw.Elapsed > _timeout)
                throw new TimeoutException(
                    $"Anchor search exceeded timeout ({_timeout.TotalSeconds}s).\n" +
                    $"Anchor: {anchorBy}\n" +
                    $"Target: {targetBy}\n" +
                    $"Directions tried: {string.Join(", ", directionsToTry)}"
                );

            try
            {
                // Find anchor element first
                var anchorElements = _driver.FindElements(anchorBy);
                if (!anchorElements.Any())
                    continue;

                var anchorElement = anchorElements.First();

                // Build search XPath based on direction
                var searchXPath = BuildSearchXPath(anchorElement, direction, targetBy);
                var targetElements = _driver.FindElements(By.XPath(searchXPath));

                if (targetElements.Any())
                {
                    // Found element(s) in this direction
                    var closest = SelectClosestElement(anchorElement, targetElements);
                    
                    // Return a Locator that will find this specific element
                    return Loc.XPath(GetElementXPath(closest));
                }
            }
            catch (NoSuchElementException)
            {
                continue;
            }
            catch (StaleElementReferenceException)
            {
                continue;
            }
        }

        // Nothing found in any direction
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
    /// Builds an XPath that searches for the target in a specific direction from the anchor.
    /// </summary>
    private string BuildSearchXPath(IWebElement anchorElement, Direction direction, By targetBy)
    {
        // Extract XPath from the target By object
        var targetXPathString = targetBy.ToString();
        
        // Remove "By.XPath: " or "By.CssSelector: " prefix
        if (targetXPathString.Contains(":"))
            targetXPathString = targetXPathString[(targetXPathString.IndexOf(":") + 1)..].Trim();

        // Get anchor's XPath using JavaScript (all elements have an XPath)
        var anchorXPath = GetElementXPath(anchorElement);

        // Build search XPath based on direction
        return direction switch
        {
            Direction.Right => $"({anchorXPath})/following-sibling::*[{targetXPathString.Replace("//", "")}]",
            Direction.Left => $"({anchorXPath})/preceding-sibling::*[{targetXPathString.Replace("//", "")}]",
            Direction.Below => $"({anchorXPath})/following::{ExtractTagName(targetXPathString)}",
            Direction.Above => $"({anchorXPath})/preceding::{ExtractTagName(targetXPathString)}",
            Direction.SameRow => $"({anchorXPath})/ancestor::tr//{ExtractTagName(targetXPathString)}",
            Direction.SameContainer => $"({anchorXPath})/ancestor::*[self::div or self::section or self::article or self::main][1]//{ExtractTagName(targetXPathString)}",
            Direction.Innermost => $"({anchorXPath})//{ExtractTagName(targetXPathString)}",
            Direction.Anywhere => targetXPathString,
            _ => targetXPathString
        };
    }

    /// <summary>
    /// Extracts the tag name from an XPath expression.
    /// </summary>
    private static string ExtractTagName(string xpath)
    {
        if (xpath.StartsWith("//"))
            return "*";
        
        var match = System.Text.RegularExpressions.Regex.Match(xpath, @"(?:^|/)(\w+)(?:\[|$)");
        return match.Success ? match.Groups[1].Value : "*";
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
                "    return element.tagName.toLowerCase();" +
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
