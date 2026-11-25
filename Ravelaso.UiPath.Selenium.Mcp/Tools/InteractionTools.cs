using System.ComponentModel;
using OpenQA.Selenium;
using ModelContextProtocol.Server;
using Ravelaso.UiPath.Selenium.Mcp;

namespace Ravelaso.UiPath.Selenium.Mcp.Tools;

[McpServerToolType]
public static class InteractionTools
{
    [McpServerTool, Description("Click an element located by strategy+value. Read the 'readme' or 'quickstart' resource for the RavelasoWebClient API.")]
    public static string BrowserClick(
        BrowserSession session,
        [Description("Strategy: id, name, css, xpath, tag, text, class, attr.")] string strategy,
        [Description("Value to match.")] string value,
        [Description("Attribute name when strategy=attr.")] string? attributeName = null)
    {
        var client = session.Client;
        try
        {
            var by = DiscoveryTools.BuildBy(strategy, value, attributeName);
            client.Click(by);
            return $"Clicked ({strategy}={value}).";
        }
        catch (Exception ex) { return $"Click failed: {ex.Message}"; }
    }

    [McpServerTool, Description("Type text into an element located by strategy+value.")]
    public static string BrowserType(
        BrowserSession session,
        [Description("Strategy: id, name, css, xpath, tag, text, class, attr.")] string strategy,
        [Description("Value to match.")] string value,
        [Description("Text to type.")] string text,
        [Description("Attribute name when strategy=attr.")] string? attributeName = null)
    {
        var client = session.Client;
        try
        {
            var by = DiscoveryTools.BuildBy(strategy, value, attributeName);
            client.Type(by, text);
            return $"Typed into ({strategy}={value}).";
        }
        catch (Exception ex) { return $"Type failed: {ex.Message}"; }
    }

    [McpServerTool, Description("Get text content of an element located by strategy+value.")]
    public static string BrowserGetText(
        BrowserSession session,
        [Description("Strategy: id, name, css, xpath, tag, text, class, attr.")] string strategy,
        [Description("Value to match.")] string value,
        [Description("Attribute name when strategy=attr.")] string? attributeName = null)
    {
        var client = session.Client;
        try
        {
            var by = DiscoveryTools.BuildBy(strategy, value, attributeName);
            return client.GetText(by);
        }
        catch (Exception ex) { return $"GetText failed: {ex.Message}"; }
    }

    [McpServerTool, Description("Get an attribute value of an element located by strategy+value.")]
    public static string BrowserGetAttribute(
        BrowserSession session,
        [Description("Strategy: id, name, css, xpath, tag, text, class, attr.")] string strategy,
        [Description("Value to match.")] string value,
        [Description("Attribute name to read.")] string attributeName,
        [Description("Locator attribute name when strategy=attr.")] string? locatorAttribute = null)
    {
        var client = session.Client;
        try
        {
            var by = DiscoveryTools.BuildBy(strategy, value, locatorAttribute);
            return client.GetAttribute(by, attributeName) ?? string.Empty;
        }
        catch (Exception ex) { return $"GetAttribute failed: {ex.Message}"; }
    }

    [McpServerTool, Description("Wait for an element to appear (seconds). Returns true/false.")]
    public static string BrowserWaitFor(
        BrowserSession session,
        [Description("Strategy: id, name, css, xpath, tag, text, class, attr.")] string strategy,
        [Description("Value to match.")] string value,
        [Description("Timeout in seconds.")] int timeoutSeconds = 10,
        [Description("Attribute name when strategy=attr.")] string? attributeName = null)
    {
        var client = session.Client;
        try
        {
            var by = DiscoveryTools.BuildBy(strategy, value, attributeName);
            var ok = client.WaitFor(by, null, TimeSpan.FromSeconds(timeoutSeconds));
            return McpHelpers.ToJson(new { found = ok });
        }
        catch (Exception ex) { return $"WaitFor failed: {ex.Message}"; }
    }


}