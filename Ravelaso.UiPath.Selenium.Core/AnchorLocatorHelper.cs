using OpenQA.Selenium;

namespace Ravelaso.UiPath.Selenium.Core;

/// <summary>
/// Internal helper for calculating element proximity and selecting the closest match.
/// Used by AnchorLocator to refine searches when multiple elements are found.
/// </summary>
internal static class AnchorLocatorHelper
{
    /// <summary>
    /// Calculates distance in pixels between two IWebElement objects.
    /// Uses element bounding rectangles to compute Euclidean distance between centers.
    /// </summary>
    /// <param name="driver">The WebDriver instance.</param>
    /// <param name="anchor">The anchor (reference) element.</param>
    /// <param name="target">The target element to measure distance to.</param>
    /// <returns>The distance in pixels between element centers, or double.MaxValue if calculation fails.</returns>
    public static double CalculateDistance(IWebDriver driver, IWebElement anchor, IWebElement target)
    {
        try
        {
            var anchorRect = GetBoundingRect(driver, anchor);
            var targetRect = GetBoundingRect(driver, target);

            var anchorCenterX = anchorRect["x"] + anchorRect["width"] / 2;
            var anchorCenterY = anchorRect["y"] + anchorRect["height"] / 2;
            var targetCenterX = targetRect["x"] + targetRect["width"] / 2;
            var targetCenterY = targetRect["y"] + targetRect["height"] / 2;

            var dx = anchorCenterX - targetCenterX;
            var dy = anchorCenterY - targetCenterY;
            return Math.Sqrt(dx * dx + dy * dy);
        }
        catch
        {
            return double.MaxValue;
        }
    }

    /// <summary>
    /// Gets the bounding rectangle of an element as a tuple for convenient consumption.
    /// </summary>
    /// <param name="driver">The WebDriver instance.</param>
    /// <param name="element">The element to get bounds for.</param>
    /// <returns>Tuple with x, y, width, height.</returns>
    internal static (double X, double Y, double Width, double Height) GetRect(IWebDriver driver, IWebElement element)
    {
        var rect = GetBoundingRect(driver, element);
        return (rect["x"], rect["y"], rect["width"], rect["height"]);
    }

    /// <summary>
    /// Gets the bounding rectangle of an element using JavaScript.
    /// </summary>
    /// <param name="driver">The WebDriver instance.</param>
    /// <param name="element">The element to get bounds for.</param>
    /// <returns>Dictionary with x, y, width, height as doubles.</returns>
    private static Dictionary<string, double> GetBoundingRect(IWebDriver driver, IWebElement element)
    {
        try
        {
            var js = (IJavaScriptExecutor)driver;
            var result = js.ExecuteScript(
                "const r = arguments[0].getBoundingClientRect(); " +
                "return {x: r.x, y: r.y, width: r.width, height: r.height};",
                element
            );

            if (result is Dictionary<string, object> dict)
            {
                return new Dictionary<string, double>
                {
                    { "x", Convert.ToDouble(dict["x"]) },
                    { "y", Convert.ToDouble(dict["y"]) },
                    { "width", Convert.ToDouble(dict["width"]) },
                    { "height", Convert.ToDouble(dict["height"]) }
                };
            }
        }
        catch
        {
            // Fallback if JS execution fails
        }

        return new Dictionary<string, double>
        {
            { "x", 0 }, { "y", 0 }, { "width", 0 }, { "height", 0 }
        };
    }

    /// <summary>
    /// Builds an XPath for finding elements in the same row (TR) as an anchor.
    /// Works with HTML tables where both anchor and target are in the same TR.
    /// </summary>
    public static string BuildSameRowXPath(string anchorXPath) =>
        $"({anchorXPath})/ancestor::tr//*";

    /// <summary>
    /// Builds an XPath for finding elements to the right of an anchor.
    /// Uses following-sibling axes for elements at the same hierarchy level.
    /// </summary>
    public static string BuildRightXPath(string anchorXPath) =>
        $"({anchorXPath})/following-sibling::*";

    /// <summary>
    /// Builds an XPath for finding elements to the left of an anchor.
    /// Uses preceding-sibling axes for elements at the same hierarchy level.
    /// </summary>
    public static string BuildLeftXPath(string anchorXPath) =>
        $"({anchorXPath})/preceding-sibling::*";

    /// <summary>
    /// Builds an XPath for finding elements below an anchor.
    /// Searches in following descendants positioned vertically lower.
    /// </summary>
    public static string BuildBelowXPath(string anchorXPath) =>
        $"({anchorXPath})/following::*";

    /// <summary>
    /// Builds an XPath for finding elements above an anchor.
    /// Searches in preceding elements positioned vertically higher.
    /// </summary>
    public static string BuildAboveXPath(string anchorXPath) =>
        $"({anchorXPath})/preceding::*";

    /// <summary>
    /// Builds an XPath for finding elements in the same container as an anchor.
    /// Searches within the nearest common ancestor div/section/article/main.
    /// </summary>
    public static string BuildSameContainerXPath(string anchorXPath) =>
        $"({anchorXPath})/ancestor::*[self::div or self::section or self::article or self::main][1]//*";

    /// <summary>
    /// Builds an XPath for finding child elements of an anchor.
    /// Searches in descendants, prioritizing direct children.
    /// </summary>
    public static string BuildInnermostXPath(string anchorXPath) =>
        $"({anchorXPath})//*";

    /// <summary>
    /// Builds an XPath for finding elements anywhere in the document.
    /// This is the fallback when no directional constraint applies.
    /// </summary>
    public static string BuildAnywhereXPath(string targetXPath) =>
        targetXPath;
}
