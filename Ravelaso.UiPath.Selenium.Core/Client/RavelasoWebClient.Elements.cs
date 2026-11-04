using OpenQA.Selenium;
using OpenQA.Selenium.Interactions;

// ReSharper disable CheckNamespace
namespace Ravelaso.UiPath.Selenium.Core;

public partial class RavelasoWebClient
{
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
}