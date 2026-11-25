using System.ComponentModel;
using ModelContextProtocol.Server;
using Ravelaso.UiPath.Selenium.Core;
using Ravelaso.UiPath.Selenium.Mcp;

namespace Ravelaso.UiPath.Selenium.Mcp.Tools;

[McpServerToolType]
public static class BrowserLifecycleTools
{
    [McpServerTool, Description("Start a browser session. browser: Chrome|Edge|Firefox. headless defaults to false so you can watch. debuggerAddress attaches to an already-running browser (e.g. localhost:9222). Call browser_api_reference or read the 'readme'/'quickstart' resources to learn the RavelasoWebClient API before writing code.")]
    public static string BrowserStart(
        BrowserSession session,
        [Description("Browser to launch: Chrome, Edge, or Firefox.")] string browser = "Edge",
        [Description("Run headless (no UI). Default false.")] bool headless = false,
        [Description("Attach to an existing browser via remote debugging address, e.g. localhost:9222.")] string? debuggerAddress = null,
        [Description("Remote debugging port for BrowserLauncher.")] int? remoteDebuggingPort = null)
    {
        if (!Enum.TryParse<WebBrowser>(browser, true, out var wb))
            return $"Unknown browser '{browser}'. Use Chrome, Edge, or Firefox.";

        return session.Start(wb, headless, debuggerAddress, remoteDebuggingPort);
    }

    [McpServerTool, Description("Stop and dispose the current browser session.")]
    public static string BrowserStop(BrowserSession session) => session.Stop();

    [McpServerTool, Description("Get the status of the current browser session (URL, title).")]
    public static string BrowserStatus(BrowserSession session) => session.Status();
}