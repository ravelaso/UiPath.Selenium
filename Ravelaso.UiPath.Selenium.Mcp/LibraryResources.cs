using System.Reflection;
using System.IO;
using System.ComponentModel;
using ModelContextProtocol.Server;

namespace Ravelaso.UiPath.Selenium.Mcp;

/// <summary>
/// Exposes the library README and QuickStart as MCP resources so the agent
/// has the full API reference in context. Docs are embedded in the exe,
/// so they ship with the published binary — no external files needed.
/// </summary>
[McpServerResourceType]
public static class LibraryResources
{
    private static string ReadEmbedded(string suffix)
    {
        var asm = Assembly.GetExecutingAssembly();
        var fullName = asm.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith(suffix, StringComparison.OrdinalIgnoreCase));
        if (fullName is null)
            return $"Resource '{suffix}' not found. Embedded resources: {string.Join(", ", asm.GetManifestResourceNames())}";
        using var stream = asm.GetManifestResourceStream(fullName)!;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    [McpServerResource, Description("Ravelaso.UiPath.Selenium library README — full API reference (navigation, interaction, locators, anchors, downloads, tables, etc.).")]
    public static string Readme() => ReadEmbedded("README.md");

    [McpServerResource, Description("Quick-start code examples for Ravelaso.UiPath.Selenium — login flow, anchors, downloads, tables, scrolling.")]
    public static string Quickstart() => ReadEmbedded("QUICKSTART.md");
}