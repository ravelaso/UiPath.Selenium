using System.ComponentModel;
using OpenQA.Selenium;
using ModelContextProtocol.Server;
using Ravelaso.UiPath.Selenium.Mcp;

namespace Ravelaso.UiPath.Selenium.Mcp.Tools;

[McpServerToolType]
public static class DiscoveryTools
{
    [McpServerTool, Description("Find elements on the current page. strategy: id|name|css|xpath|tag|text|class|attr. For 'attr', pass attributeName too. Returns tag, text, attributes, and a suggested Loc.* expression per element.")]
    public static string BrowserFindElements(
        BrowserSession session,
        [Description("Strategy: id, name, css, xpath, tag, text, class, attr.")] string strategy,
        [Description("Value to match (id value, css selector, xpath, tag name, text substring, class name, or attr value).")] string value,
        [Description("Max number of elements to return.")] int limit = 25,
        [Description("Attribute name when strategy=attr.")] string? attributeName = null)
    {
        var driver = session.Client.Driver;
        By by;
        try { by = BuildBy(strategy, value, attributeName); }
        catch (Exception ex) { return $"Error building locator: {ex.Message}"; }

        List<IWebElement> elements;
        try { elements = McpHelpers.FindElementsDeep(driver, by); }
        catch (Exception ex) { return $"Find failed: {ex.Message}"; }

        var list = new List<Dictionary<string, object?>>();
        foreach (var el in elements.Take(Math.Max(1, limit)))
        {
            try { list.Add(McpHelpers.DescribeElement(driver, el)); } catch { /* skip stale */ }
        }

        return McpHelpers.ToJson(new { strategy, value, count = elements.Count, returned = list.Count, elements = list });
    }

    [McpServerTool, Description("Get detailed info for a single element located by strategy+value (first match).")]
    public static string BrowserGetElement(
        BrowserSession session,
        [Description("Strategy: id, name, css, xpath, tag, text, class, attr.")] string strategy,
        [Description("Value to match.")] string value,
        [Description("Attribute name when strategy=attr.")] string? attributeName = null)
    {
        var driver = session.Client.Driver;
        By by;
        try { by = BuildBy(strategy, value, attributeName); }
        catch (Exception ex) { return $"Error building locator: {ex.Message}"; }

        try
        {
            var els = McpHelpers.FindElementsDeep(driver, by);
            if (els.Count == 0) throw new NoSuchElementException();
            var el = els[0];
            var info = McpHelpers.DescribeElement(driver, el);
            // Add outerHTML snippet
            try
            {
                var html = (string)((OpenQA.Selenium.IJavaScriptExecutor)driver).ExecuteScript("return arguments[0].outerHTML;", el);
                info["outerHtml"] = McpHelpers.Truncate(html, 1500);
            }
            catch { }
            return McpHelpers.ToJson(info);
        }
        catch (NoSuchElementException)
        {
            return McpHelpers.ToJson(new { found = false, strategy, value });
        }
        catch (Exception ex)
        {
            return $"Error: {ex.Message}";
        }
    }

    [McpServerTool, Description("List all input/textarea/select elements on the page (useful for finding login fields).")]
    public static string BrowserListInputs(BrowserSession session)
    {
        var driver = session.Client.Driver;
        var els = McpHelpers.FindElementsDeep(driver, By.CssSelector("input, textarea, select"));
        var list = els.Select(el => McpHelpers.DescribeElement(driver, el)).ToList();
        return McpHelpers.ToJson(new { count = els.Count, elements = list });
    }

    [McpServerTool, Description("List all button and submit input elements on the page.")]
    public static string BrowserListButtons(BrowserSession session)
    {
        var driver = session.Client.Driver;
        var els = McpHelpers.FindElementsDeep(driver, By.CssSelector("button, input[type='submit'], input[type='button'], a[role='button']"));
        var list = els.Select(el => McpHelpers.DescribeElement(driver, el)).ToList();
        return McpHelpers.ToJson(new { count = els.Count, elements = list });
    }

    [McpServerTool, Description("List all interactive/clickable elements (links, buttons, menuitems, and elements with data-t-id, ng-click, ui-sref, role, tabindex). Use this to discover navigation menus and clickable UI in SPAs where plain link/button lists are empty.")]
    public static string BrowserListInteractive(BrowserSession session,
        [Description("Max number of elements to return.")] int limit = 60)
    {
        var driver = session.Client.Driver;
        try
        {
            var els = McpHelpers.FindElementsDeep(driver, By.CssSelector(
                "a, button, [role], [data-t-id], [data-tid], [ng-click], [ui-sref], [tabindex]"));
            var list = new List<Dictionary<string, object?>>();
            foreach (var el in els)
            {
                try
                {
                    if (list.Count >= limit) break;
                    list.Add(McpHelpers.DescribeElement(driver, el));
                }
                catch { /* skip stale */ }
            }
            return McpHelpers.ToJson(new { count = els.Count, returned = list.Count, elements = list });
        }
        catch (Exception ex)
        {
            return $"Error: {ex.Message}";
        }
    }

    [McpServerTool, Description("Wait until visible text appears on the page (useful for SPA render timing). Returns true if found within timeout.")]
    public static string BrowserWaitForText(BrowserSession session,
        [Description("Text to wait for (substring).")] string text,
        [Description("Timeout in seconds.")] int timeoutSeconds = 10)
    {
        var driver = session.Client.Driver;
        var by = By.XPath($"//*[not(self::script or self::style or self::noscript or self::template)][contains(normalize-space(text()), '{text.Replace("'", "\\'")}')]");

        var end = DateTime.UtcNow.AddSeconds(timeoutSeconds);
        while (DateTime.UtcNow < end)
        {
            try
            {
                var els = driver.FindElements(by);
                if (els.Count > 0)
                    return McpHelpers.ToJson(new { found = true, count = els.Count });
            }
            catch { }
            try { System.Threading.Thread.Sleep(250); } catch { }
        }
        return McpHelpers.ToJson(new { found = false, count = 0 });
    }

    [McpServerTool, Description("Check whether an element exists. strategy+value locate it; returns true/false (no throw).")]
    public static string BrowserExists(
        BrowserSession session,
        [Description("Strategy: id, name, css, xpath, tag, text, class, attr.")] string strategy,
        [Description("Value to match.")] string value,
        [Description("Attribute name when strategy=attr.")] string? attributeName = null)
    {
        var driver = session.Client.Driver;
        By by;
        try { by = BuildBy(strategy, value, attributeName); }
        catch (Exception ex) { return $"Error: {ex.Message}"; }
        try
        {
            var els = McpHelpers.FindElementsDeep(driver, by);
            return McpHelpers.ToJson(new { exists = els.Count > 0, count = els.Count });
        }
        catch (Exception ex) { return $"Error: {ex.Message}"; }
    }

    public static By BuildBy(string strategy, string value, string? attributeName)
    {
        return strategy.ToLowerInvariant() switch
        {
            "id" => By.Id(value),
            "name" => By.Name(value),
            "css" => By.CssSelector(value),
            "xpath" => By.XPath(value),
            "tag" => By.TagName(value),
            "text" => By.XPath($"//*[not(self::script or self::style or self::noscript or self::template)][contains(normalize-space(text()), {Quote(value)})]"),
            "class" => By.ClassName(value),
            "attr" => string.IsNullOrWhiteSpace(attributeName)
                ? throw new ArgumentException("attributeName required for attr strategy")
                : By.CssSelector($"[{attributeName}=\"{EscapeCss(value)}\"]"),
            _ => throw new ArgumentException($"Unknown strategy '{strategy}'")
        };
    }

    private static string Quote(string v)
    {
        if (!v.Contains('\'')) return $"'{v}'";
        if (!v.Contains('"')) return $"\"{v}\"";
        return $"'{v.Replace("'", "\\'")}'";
    }

    private static string EscapeCss(string v) => v.Replace("\\", "\\\\").Replace("\"", "\\\"");
}