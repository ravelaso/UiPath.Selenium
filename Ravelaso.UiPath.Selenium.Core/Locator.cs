using System.Text;
using OpenQA.Selenium;

namespace Ravelaso.UiPath.Selenium.Core;

/// <summary>
/// Fluent, immutable builder for constructing Selenium element selectors.
/// Each chaining method returns a new instance, allowing prefix reuse.
/// Supports implicit conversion to <see cref="By"/> and automatic CSS/XPath rendering.
/// </summary>
public sealed class Locator
{
    internal enum PartKind { Tag, Id, Class, Name, Attribute, Text, Css, XPath, LinkText, PartialLinkText }

    private sealed record Part(PartKind Kind, string Value, string? AttributeName = null);

    private readonly PartKind? _kind;
    private readonly string? _value;
    private readonly string? _attributeName;
    private readonly Locator? _parent;

    /// <summary>
    /// Marker property for explicit chaining reads: <c>Loc.Id("x").And.Class("y")</c>.
    /// Equivalent to omitting it: <c>Loc.Id("x").Class("y")</c>.
    /// </summary>
    public Locator And => this;

    internal Locator(PartKind kind, string value, string? attributeName = null, Locator? parent = null)
    {
        _kind = kind;
        _value = value;
        _attributeName = attributeName;
        _parent = parent;
    }

    #region Chaining methods

    /// <summary>Add an HTML tag condition.</summary>
    public Locator Tag(string tagName) => new(PartKind.Tag, tagName, null, this);

    /// <summary>Add an id attribute condition.</summary>
    public Locator Id(string id) => new(PartKind.Id, id, null, this);

    /// <summary>Add a CSS class condition (exact token match).</summary>
    public Locator Class(string className) => new(PartKind.Class, className, null, this);

    /// <summary>Add a name attribute condition.</summary>
    public Locator Name(string name) => new(PartKind.Name, name, null, this);

    /// <summary>Add a custom attribute condition.</summary>
    public Locator Attr(string attributeName, string value) => new(PartKind.Attribute, value, attributeName, this);

    /// <summary>Add a text condition (forces XPath mode).</summary>
    public Locator Text(string text) => new(PartKind.Text, text, null, this);

    /// <summary>Add a CSS selector fragment (must be a compound selector without combinators).</summary>
    public Locator Css(string css) => new(PartKind.Css, css, null, this);

    /// <summary>Add an XPath root (forces XPath mode).</summary>
    public Locator XPath(string xpath) => new(PartKind.XPath, xpath, null, this);

    /// <summary>Add an exact link text condition (for &lt;a&gt; tags only; forces XPath mode).</summary>
    public Locator LinkText(string text) => new(PartKind.LinkText, text, null, this);

    /// <summary>Add a partial link text condition (for &lt;a&gt; tags only; forces XPath mode).</summary>
    public Locator PartialLinkText(string text) => new(PartKind.PartialLinkText, text, null, this);

    #endregion

    #region Conversion to By

    /// <summary>
    /// Converts the locator to a Selenium <see cref="By"/>.
    /// Automatically selects between native By (single condition) and composite selectors (CSS/XPath).
    /// </summary>
    public By ToBy()
    {
        var parts = Flatten();

        // Detect mode
        var hasXPath = parts.Any(p => p.Kind == PartKind.XPath);
        var hasText = parts.Any(p => p.Kind == PartKind.Text);
        var hasLinkText = parts.Any(p => p.Kind == PartKind.LinkText || p.Kind == PartKind.PartialLinkText);

        // Single condition: return native By
        if (parts.Count == 1)
            return PartToNativeBy(parts[0]);

        // Multiple conditions: select mode
        if (hasXPath || hasText || hasLinkText)
            return BuildXPath(parts);

        return BuildCss(parts);
    }

    private static By PartToNativeBy(Part p) => p.Kind switch
    {
        PartKind.Id => By.Id(p.Value),
        PartKind.Name => By.Name(p.Value),
        PartKind.Class => By.ClassName(p.Value),
        PartKind.Tag => By.TagName(p.Value),
        PartKind.Css => By.CssSelector(p.Value),
        PartKind.XPath => By.XPath(p.Value),
        PartKind.LinkText => By.LinkText(p.Value),
        PartKind.PartialLinkText => By.PartialLinkText(p.Value),
        PartKind.Attribute => By.CssSelector($"[{p.AttributeName}=\"{EscapeCssString(p.Value)}\"]"),
        PartKind.Text => By.XPath($"//*[contains(normalize-space(text()), {QuoteXPath(p.Value)})]"),
        _ => throw new InvalidOperationException($"Unknown PartKind: {p.Kind}")
    };

    private static By BuildCss(List<Part> parts)
    {
        var cssParts = parts.Where(p => p.Kind == PartKind.Css).ToList();
        
        // Validate CSS fragments without combinators
        foreach (var css in cssParts)
        {
            if (HasCombinators(css.Value))
                throw new InvalidOperationException(
                    $"CSS selector '{css.Value}' contains combinators and cannot be combined with other conditions. " +
                    "Use Loc.XPath(...) for this case.");
        }

        var sb = new StringBuilder();

        // Tag
        var tag = parts.FirstOrDefault(p => p.Kind == PartKind.Tag);
        if (tag != null) sb.Append(tag.Value);

        // Raw CSS fragments
        foreach (var css in cssParts) sb.Append(css.Value);

        // ID
        var id = parts.FirstOrDefault(p => p.Kind == PartKind.Id);
        if (id != null)
            sb.Append(IsSafeCssIdentifier(id.Value) ? "#" + id.Value : $"[id=\"{EscapeCssString(id.Value)}\"]");

        // Classes
        foreach (var cls in parts.Where(p => p.Kind == PartKind.Class))
            sb.Append(IsSafeCssIdentifier(cls.Value) ? "." + cls.Value : $"[class~=\"{EscapeCssString(cls.Value)}\"]");

        // Name
        var name = parts.FirstOrDefault(p => p.Kind == PartKind.Name);
        if (name != null) sb.Append($"[name=\"{EscapeCssString(name.Value)}\"]");

        // Custom attributes
        foreach (var attr in parts.Where(p => p.Kind == PartKind.Attribute))
            sb.Append($"[{attr.AttributeName}=\"{EscapeCssString(attr.Value)}\"]");

        return By.CssSelector(sb.ToString());
    }

    private static By BuildXPath(List<Part> parts)
    {
        var root = parts.FirstOrDefault(p => p.Kind == PartKind.XPath);
        var sb = new StringBuilder(root?.Value ?? "//*");

        foreach (var p in parts)
        {
            switch (p.Kind)
            {
                case PartKind.Id:
                    sb.Append($"[@id={QuoteXPath(p.Value)}]");
                    break;
                case PartKind.Name:
                    sb.Append($"[@name={QuoteXPath(p.Value)}]");
                    break;
                case PartKind.Class:
                    sb.Append($"[contains(concat(' ', normalize-space(@class), ' '), {QuoteXPath(" " + p.Value + " ")})]");
                    break;
                case PartKind.Attribute:
                    sb.Append($"[@{p.AttributeName}={QuoteXPath(p.Value)}]");
                    break;
                case PartKind.Tag:
                    sb.Append($"[local-name()={QuoteXPath(p.Value)}]");
                    break;
                case PartKind.Text:
                    sb.Append($"[contains(normalize-space(text()), {QuoteXPath(p.Value)})]");
                    break;
                case PartKind.LinkText:
                    sb.Append($"[normalize-space(text())={QuoteXPath(p.Value)}]");
                    break;
                case PartKind.PartialLinkText:
                    sb.Append($"[contains(normalize-space(text()), {QuoteXPath(p.Value)})]");
                    break;
            }
        }

        return By.XPath(sb.ToString());
    }

    #endregion

    #region Implicit conversion to By

    /// <summary>Implicit conversion to <see cref="By"/> for direct use in Selenium methods.</summary>
    public static implicit operator By(Locator locator) => locator.ToBy();

    #endregion

    #region ToString

    /// <summary>Returns a string representation of the generated selector.</summary>
    public override string ToString() => ToBy().ToString();

    #endregion

    #region Private helpers

    private List<Part> Flatten()
    {
        var stack = new Stack<Part>();
        var current = this;

        while (current != null)
        {
            if (current._kind.HasValue)
                stack.Push(new Part(current._kind.Value, current._value!, current._attributeName));
            current = current._parent;
        }

        return stack.ToList(); // LIFO → root first
    }

    private static string QuoteXPath(string value)
    {
        if (!value.Contains('\''))
            return $"'{value}'";
        if (!value.Contains('"'))
            return $"\"{value}\"";

        // Both characters present: use concat
        var segments = value.Split('\'');
        var sb = new StringBuilder("concat(");
        for (var i = 0; i < segments.Length; i++)
        {
            if (i > 0) sb.Append(", \"'\", ");
            sb.Append('\'').Append(segments[i]).Append('\'');
        }
        sb.Append(')');
        return sb.ToString();
    }

    /// <summary>
    /// Escapes a value for use inside a CSS attribute string.
    /// Returns the escaped content WITHOUT surrounding quotes; callers add the quotes.
    /// </summary>
    private static string EscapeCssString(string value)
        => value.Replace("\\", "\\\\").Replace("\"", "\\\"");

    private static bool IsSafeCssIdentifier(string value)
    {
        if (string.IsNullOrEmpty(value)) return false;
        if (!char.IsLetter(value[0]) && value[0] != '_' && value[0] != '-') return false;
        return value.All(c => char.IsLetterOrDigit(c) || c == '_' || c == '-');
    }

    private static bool HasCombinators(string css)
    {
        char quote = '\0';
        for (int i = 0; i < css.Length; i++)
        {
            var c = css[i];
            if (quote != '\0')
            {
                if (c == quote) quote = '\0';
                continue;
            }
            if (c == '"' || c == '\'') { quote = c; continue; }
            if (c == '>' || c == '+' || c == '~' || c == ',') return true;
            if (char.IsWhiteSpace(c)) return true;
        }
        return false;
    }

    #endregion

    #region Anchor System

    /// <summary>
    /// Creates an anchor point for relative element searching.
    /// The anchor system finds target elements relative to a reference (anchor) element,
    /// useful for dynamic tables, forms, and lists where positions change.
    /// 
    /// Example:
    /// <code>
    /// var downloadBtn = Loc.Anchor(Loc.Text("2026-10-22"))
    ///     .Find(Loc.Css("button.download"))
    ///     .Resolve();
    /// 
    /// client.Click(downloadBtn);
    /// </code>
    /// </summary>
    /// <param name="driver">The WebDriver instance to use for element searches.</param>
    /// <returns>A new AnchorLocator configured with this locator as the anchor point.</returns>
    public AnchorLocator Anchor(IWebDriver driver)
    {
        return new AnchorLocator(driver, this);
    }

    #endregion
}
