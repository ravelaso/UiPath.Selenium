using Xunit;
using Ravelaso.UiPath.Selenium.Core;
using OpenQA.Selenium;

namespace Ravelaso.UiPath.Selenium.Tests;

/// <summary>
/// Unit tests for the Loc static facade and Locator fluent builder.
/// </summary>
public class LocatorTests
{
    /// <summary>
    /// Verify that Loc.Id produces a native By.Id.
    /// </summary>
    [Fact]
    public void Loc_Id_ReturnsNativeById()
    {
        var locator = Loc.Id("username");
        var by = (By)locator;
        
        Assert.NotNull(by);
        // By.Id internally uses css selector mechanism for single conditions
        Assert.NotNull(by.Criteria);
        Assert.Contains("username", by.Criteria);
    }

    /// <summary>
    /// Verify that Loc.Class produces a native By.ClassName.
    /// </summary>
    [Fact]
    public void Loc_Class_ReturnsNativeByClassName()
    {
        var locator = Loc.Class("btn-primary");
        var by = (By)locator;
        
        Assert.NotNull(by);
        Assert.Equal("css selector", by.Mechanism);
    }

    /// <summary>
    /// Verify that chaining Loc.Id with And.Class produces a composite CSS selector.
    /// </summary>
    [Fact]
    public void Locator_Chaining_IdAndClass_ProducesCompositeCSS()
    {
        var locator = Loc.Id("username").And.Class("form-control");
        var by = (By)locator;
        
        Assert.NotNull(by);
        // Composite selectors use CSS mode
        Assert.Equal("css selector", by.Mechanism);
        var selector = by.Criteria;
        Assert.Contains("#username", selector);
        Assert.Contains(".form-control", selector);
    }

    /// <summary>
    /// Verify that Loc.XPath combined with And.Id produces XPath mode.
    /// </summary>
    [Fact]
    public void Locator_XPathMode_WithPredicate()
    {
        var locator = Loc.XPath("//div").And.Id("container");
        var by = (By)locator;
        
        Assert.NotNull(by);
        Assert.Equal("xpath", by.Mechanism);
        Assert.Contains("//div", by.Criteria);
        Assert.Contains("@id", by.Criteria);
    }

    /// <summary>
    /// Verify that Loc.Of with dynamic LocatorType works correctly.
    /// </summary>
    [Fact]
    public void Loc_Of_DynamicType_CreatesCorrectLocator()
    {
        var locator = Loc.Of(LocatorType.Id, "password");
        var by = (By)locator;
        
        Assert.NotNull(by);
        Assert.NotNull(by.Criteria);
        Assert.Contains("password", by.Criteria);
    }

    /// <summary>
    /// Verify that Locator has implicit conversion to By.
    /// </summary>
    [Fact]
    public void Locator_ImplicitConversion_ToBy()
    {
        var locator = Loc.Name("email");
        
        // This should compile and work
        By by = locator;
        
        Assert.NotNull(by);
        Assert.NotNull(by.Criteria);
        Assert.Contains("email", by.Criteria);
    }

    /// <summary>
    /// Verify that Locator.ToString returns the selector string.
    /// </summary>
    [Fact]
    public void Locator_ToString_ReturnsSelector()
    {
        var locator = Loc.Css(".btn");
        var str = locator.ToString();
        
        Assert.NotNull(str);
        Assert.NotEmpty(str);
    }

    /// <summary>
    /// Verify that Loc.And marker allows fluent reads without functional change.
    /// </summary>
    [Fact]
    public void Locator_And_Marker_Allows_FluentReads()
    {
        var loc1 = Loc.Id("x").Class("y");
        var loc2 = Loc.Id("x").And.Class("y");
        
        var by1 = (By)loc1;
        var by2 = (By)loc2;
        
        Assert.Equal(by1.Mechanism, by2.Mechanism);
        Assert.Equal(by1.Criteria, by2.Criteria);
    }

    /// <summary>
    /// Verify that Id combined with Name renders a valid single-quoted-value CSS selector.
    /// Regression test: the value must not be wrapped in duplicated quotes.
    /// </summary>
    [Fact]
    public void Locator_Chaining_IdAndName_ProducesValidCSS()
    {
        var locator = Loc.Id("txtLogin").And.Name("txtLogin");
        var by = (By)locator;

        Assert.Equal("css selector", by.Mechanism);
        Assert.Equal("#txtLogin[name=\"txtLogin\"]", by.Criteria);
        Assert.DoesNotContain("\"\"", by.Criteria);
    }

    /// <summary>
    /// Verify that a custom attribute alone renders a valid CSS selector.
    /// </summary>
    [Fact]
    public void Loc_Attr_Alone_ProducesValidCSS()
    {
        var locator = Loc.Attr("data-testid", "btn");
        var by = (By)locator;

        Assert.Equal("css selector", by.Mechanism);
        Assert.Equal("[data-testid=\"btn\"]", by.Criteria);
        Assert.DoesNotContain("\"\"", by.Criteria);
    }

    /// <summary>
    /// Verify that Id combined with a custom attribute renders valid CSS.
    /// </summary>
    [Fact]
    public void Locator_Chaining_IdAndAttr_ProducesValidCSS()
    {
        var locator = Loc.Id("field").And.Attr("data-role", "input");
        var by = (By)locator;

        Assert.Equal("css selector", by.Mechanism);
        Assert.Equal("#field[data-role=\"input\"]", by.Criteria);
        Assert.DoesNotContain("\"\"", by.Criteria);
    }

    /// <summary>
    /// Verify that a value containing quotes is escaped correctly.
    /// </summary>
    [Fact]
    public void Loc_Attr_ValueWithQuotes_EscapesCorrectly()
    {
        var locator = Loc.Attr("data-x", "a\"b");
        var by = (By)locator;

        Assert.Equal("css selector", by.Mechanism);
        Assert.Equal("[data-x=\"a\\\"b\"]", by.Criteria);
        Assert.DoesNotContain("\"\"", by.Criteria);
    }

    /// <summary>
    /// Verify that Loc.Text forces XPath mode.
    /// </summary>
    [Fact]
    public void Locator_Text_ForcesXPathMode()
    {
        var locator = Loc.Text("Save");
        var by = (By)locator;
        
        Assert.NotNull(by);
        Assert.Equal("xpath", by.Mechanism);
        Assert.Contains("text()", by.Criteria);
    }
}
