using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using Ravelaso.UiPath.Selenium.Mcp;

var builder = Host.CreateApplicationBuilder(args);
builder.Logging.AddConsole(consoleLogOptions =>
{
    // MCP uses stdout for protocol messages; send all logs to stderr.
    consoleLogOptions.LogToStandardErrorThreshold = LogLevel.Trace;
});

builder.Services
    .AddSingleton<BrowserSession>()
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly()
    .WithResourcesFromAssembly();

await builder.Build().RunAsync();



[McpServerToolType]
public static class ServerInfoTools
{
    [McpServerTool, System.ComponentModel.Description("Returns server info and available library resources. Call this first to discover the API reference before writing automation code.")]
    public static string ServerInfo()
    {
        return McpHelpers.ToJson(new
        {
            name = "Ravelaso.UiPath.Selenium MCP",
            version = "1.0",
            description = "Browser discovery MCP server for Ravelaso.UiPath.Selenium. Explore pages, discover locators, then write C# automation code.",
            resources = new[]
            {
                new { uri = "resource://mcp/readme", name = "readme", description = "Full library API reference (navigation, interaction, locators, anchors, downloads, tables, etc.)" },
                new { uri = "resource://mcp/quickstart", name = "quickstart", description = "Code examples: login flow, anchors, downloads, tables, scrolling, shadow DOM" }
            },
            hint = "Read these resources (via resources/read) before writing automation code. They contain the RavelasoWebClient and Loc API you need. " +
                   "After exploring a page with browser_* tools, use the suggested Loc.* expressions from the discovery results."
        });
    }

    [McpServerTool, System.ComponentModel.Description("Returns a condensed API reference for Ravelaso.UiPath.Selenium. Use this to write automation code without reading the full resources. Covers client creation, navigation, interaction, locators, waits, and anchors.")]
    public static string BrowserApiReference()
    {
        return @"Ravelaso.UiPath.Selenium — Condensed API Reference

== CLIENT CREATION ==
using Ravelaso.UiPath.Selenium.Core;
var options = new BrowserOptions { Maximize = true, GlobalDelay = TimeSpan.FromSeconds(1), ElementWaitTimeout = TimeSpan.FromSeconds(10) };
using var client = new RavelasoWebClient(WebBrowser.Edge, options);
// Attach to existing: new BrowserOptions { DebuggerAddress = ""localhost:9222"" }

== NAVIGATION ==
client.Navigate(url);  client.GetUrl();  client.GetTitle();  client.GetPageSource();  client.Screenshot(path);

== INTERACTION ==
client.Click(locator);  client.Click(locator, timeout);
client.ClickJs(locator);  client.ClickHidden(locator, hoverTarget);
client.Type(locator, text);  client.Type(locator, text, clear);
client.Select(locator, option);  client.DoubleClick(locator);

== READING ==
client.GetText(locator);  client.GetAttribute(locator, attr);

== WAITS & CHECKS ==
client.WaitFor(locator, timeout);  client.WaitFor(anchor, timeout);
client.Exists(locator);  client.Exists(locator, timeout);  client.Exists(anchor, timeout);
client.IsDisplayed(locator, timeout);

== LOCATORS (Loc.*) ==
Loc.Id(""x"")  Loc.Name(""x"")  Loc.Class(""cls"")  Loc.Tag(""button"")
Loc.Css("".btn"")  Loc.Attr(""data-t-id"", ""value"")  Loc.Text(""Save"")
Loc.Tag(""button"").And.Class(""primary"").And.Attr(""type"", ""submit"")
Loc.Anchor(anchor).Find(target)  Loc.Anchor(a).Anchor(b).Find(target)

== ANCHORS ==
var el = Loc.Anchor(Loc.Text(""ref"")).Find(Loc.Tag(""button"").Class(""action""));
client.Click(el);  client.WaitFor(el, timeout);  client.Exists(el, timeout);

== DOWNLOADS ==
client.DownloadFile(button, path, delay, timeout);
client.DownloadFromUrl(url, path, timeout);

== SCROLLING ==
client.Scroll(dir, amount);  client.Scroll(dir, amount, container);
client.ScrollTo(dir, target);  client.ScrollTo(dir, target, container);
client.GetScrollables();

== TABLES ==
client.ExtractTable(locator, options);  client.ExtractTable(locator, dataTable, options);

== ZOOM ==
client.SetZoom(0.8);  client.ResetZoom();

== SHADOW DOM ==
Automatic — client searches light DOM + open shadow roots transparently.

== NOTES ==
- Locator implicitly converts to Selenium By.
- Use Loc.Id/Name/Attr for stable selectors; avoid fragile XPath.
- WaitFor/Exists accept both Locator and AnchorLocator.
- GlobalDelay applies before each operation (configurable).";
    }
}