using System.ComponentModel;
using ModelContextProtocol.Server;
using Ravelaso.UiPath.Selenium.Mcp;

namespace Ravelaso.UiPath.Selenium.Mcp.Tools;

[McpServerToolType]
public static class NavigationTools
{
    [McpServerTool, Description("Navigate the browser to a URL. Read the 'readme' or 'quickstart' resource for the RavelasoWebClient API.")]
    public static string BrowserNavigate(BrowserSession session, [Description("URL to navigate to.")] string url)
    {
        var client = session.Client;
        client.Navigate(url);
        return $"Navigated to {client.GetUrl()} (title: {client.GetTitle()}).";
    }

    [McpServerTool, Description("Get the current URL.")]
    public static string BrowserGetUrl(BrowserSession session) => session.Client.GetUrl();

    [McpServerTool, Description("Get the current page title.")]
    public static string BrowserGetTitle(BrowserSession session) => session.Client.GetTitle();

    [McpServerTool, Description("Get the page HTML source. maxChars caps output; offset skips characters for paging large pages.")]
    public static string BrowserGetPageSource(BrowserSession session,
        [Description("Maximum characters to return.")] int maxChars = 20000,
        [Description("Character offset to start from.")] int offset = 0)
    {
        var src = session.Client.GetPageSource();
        if (offset < 0) offset = 0;
        if (offset >= src.Length) return string.Empty;
        var len = Math.Min(maxChars, src.Length - offset);
        var chunk = src.Substring(offset, len);
        var more = src.Length - offset - len;
        return chunk + (more > 0 ? $"\n\n<!-- {more} more characters available; call again with offset={offset + len} -->" : "");
    }

    [McpServerTool, Description("Take a screenshot and save it to a temp file; returns the file path (readable by the host).")]
    public static string BrowserScreenshot(BrowserSession session)
    {
        var path = Path.Combine(Path.GetTempPath(), "ravelaso-mcp-screenshot.png");
        session.Client.Screenshot(path);
        return McpHelpers.ToJson(new { path, note = "Screenshot saved; host can read this image file." });
    }
}