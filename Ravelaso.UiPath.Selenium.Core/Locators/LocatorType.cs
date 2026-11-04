// ReSharper disable CheckNamespace
namespace Ravelaso.UiPath.Selenium.Core;

/// <summary>
/// Element locator strategies. Used by <see cref="Loc.Of(LocatorType, string)"/> to create dynamic locators.
/// </summary>
public enum LocatorType
{
    /// <summary>Locate by id attribute.</summary>
    Id,

    /// <summary>Locate by name attribute.</summary>
    Name,

    /// <summary>Locate by CSS class (exact token match).</summary>
    ClassName,

    /// <summary>Locate by HTML tag name.</summary>
    TagName,

    /// <summary>Locate by CSS selector.</summary>
    CssSelector,

    /// <summary>Locate by XPath expression.</summary>
    XPath,

    /// <summary>Locate a link by exact text (for &lt;a&gt; tags only).</summary>
    LinkText,

    /// <summary>Locate a link by partial text (for &lt;a&gt; tags only).</summary>
    PartialLinkText
}
