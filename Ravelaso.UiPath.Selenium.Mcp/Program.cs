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
    [McpServerTool, System.ComponentModel.Description("Returns server/version info and the library API summary resource hint.")]
    public static string ServerInfo() =>
        "Ravelaso.UiPath.Selenium MCP v1 (stdio). Library: Ravelaso.UiPath.Selenium.Core. " +
        "Use browser_* tools to explore a site, then write C# using Loc.* and RavelasoWebClient. " +
        "See README.md / docs/QUICKSTART.md in the repo for the full API.";
}