namespace Ravelaso.UiPath.Selenium.Core;

/// <summary>
/// Static facade for building locators fluently and simply.
/// Single entry point for creating selectors: <c>Loc.Id("x")</c>, <c>Loc.Css(".btn")</c>, etc.
/// Supports chaining: <c>Loc.Tag("button").Attr("data-t-id", "submit").Text("Save")</c>.
/// </summary>
public static class Loc
{
    /// <summary>
    /// Creates an anchor locator that does not depend on a client.
    /// The driver is supplied when the anchor is resolved or passed to a client method.
    /// </summary>
    public static AnchorLocator Anchor(Locator anchor) => new AnchorLocator(anchor);
    /// <summary>Create a locator by id attribute.</summary>
    /// <example><c>Loc.Id("username")</c></example>
    public static Locator Id(string id) => new(Locator.PartKind.Id, id);

    /// <summary>Create a locator by name attribute.</summary>
    /// <example><c>Loc.Name("password")</c></example>
    public static Locator Name(string name) => new(Locator.PartKind.Name, name);

    /// <summary>
    /// Create a locator by CSS class (exact token match).
    /// Whitespace-separated class names are split into individual conditions,
    /// so <c>Loc.Class("a b")</c> requires both classes.
    /// </summary>
    /// <example><c>Loc.Class("btn-primary")</c></example>
    public static Locator Class(string className)
    {
        if (string.IsNullOrWhiteSpace(className))
            throw new ArgumentException("Class name cannot be empty.", nameof(className));

        Locator? result = null;
        foreach (var token in className.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            result = new Locator(Locator.PartKind.Class, token, null, result);

        return result!;
    }

    /// <summary>Create a locator by HTML tag name.</summary>
    /// <example><c>Loc.Tag("button")</c></example>
    public static Locator Tag(string tagName) => new(Locator.PartKind.Tag, tagName);

    /// <summary>Create a locator by CSS selector.</summary>
    /// <example><c>Loc.Css(".btn-primary")</c></example>
    public static Locator Css(string css) => new(Locator.PartKind.Css, css);

    /// <summary>Create a locator by XPath expression.</summary>
    /// <example><c>Loc.XPath("//button[@type='submit']")</c></example>
    public static Locator XPath(string xpath) => new(Locator.PartKind.XPath, xpath);

    /// <summary>Create a locator by exact link text (for &lt;a&gt; tags only).</summary>
    /// <example><c>Loc.LinkText("Sign In")</c></example>
    public static Locator LinkText(string text) => new(Locator.PartKind.LinkText, text);

    /// <summary>Create a locator by partial link text (for &lt;a&gt; tags only).</summary>
    /// <example><c>Loc.PartialLinkText("Sign")</c></example>
    public static Locator PartialLinkText(string text) => new(Locator.PartKind.PartialLinkText, text);

    /// <summary>Create a locator by custom attribute.</summary>
    /// <example>
    /// <code>
    /// // Single attribute
    /// Loc.Attr("data-testid", "submit-button")
    ///
    /// // Chained attributes
    /// Loc.Tag("button").Attr("data-t-id", "clear").Attr("aria-label", "Clear")
    /// </code>
    /// </example>
    public static Locator Attr(string attributeName, string value) => new(Locator.PartKind.Attribute, value, attributeName);

    /// <summary>Create a locator by visible text (forces XPath mode).</summary>
    /// <example>
    /// <code>
    /// // Contains text
    /// Loc.Text("Save")
    ///
    /// // Tag + text
    /// Loc.Tag("button").Text("Search")
    ///
    /// // Partial match (contains)
    /// Loc.Tag("span").Text("187.30")
    /// </code>
    /// </example>
    public static Locator Text(string text) => new(Locator.PartKind.Text, text);

    /// <summary>
    /// Create a locator from dynamic type and value.
    /// Useful for runtime selector construction.
    /// </summary>
    /// <param name="type">Locator strategy.</param>
    /// <param name="value">Value to search for.</param>
    /// <returns>A <see cref="Locator"/> corresponding to the type.</returns>
    /// <example><c>Loc.Of(LocatorType.Id, "username")</c></example>
    public static Locator Of(LocatorType type, string value) => type switch
    {
        LocatorType.Id => Id(value),
        LocatorType.Name => Name(value),
        LocatorType.ClassName => Class(value),
        LocatorType.TagName => Tag(value),
        LocatorType.CssSelector => Css(value),
        LocatorType.XPath => XPath(value),
        LocatorType.LinkText => LinkText(value),
        LocatorType.PartialLinkText => PartialLinkText(value),
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown LocatorType.")
    };
}
