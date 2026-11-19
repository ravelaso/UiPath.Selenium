using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace Ravelaso.UiPath.Selenium.Core;

/// <summary>
/// Locates the bundled Selenium Manager binary shipped with this package and
/// exposes it through the SE_MANAGER_PATH environment variable so that Selenium's
/// SeleniumManager can find it regardless of the host (UiPath Studio, published
/// .NET app, etc.).
/// </summary>
internal static class SeleniumManagerResolver
{
    private static int _resolved;

    public static void EnsureResolved()
    {
        if (Interlocked.Exchange(ref _resolved, 1) == 1)
            return;

        var existing = Environment.GetEnvironmentVariable("SE_MANAGER_PATH");
        if (!string.IsNullOrWhiteSpace(existing) && File.Exists(existing))
            return;

        var binaryName = OperatingSystem.IsWindows() ? "selenium-manager.exe" : "selenium-manager";
        var rid = GetRuntimeId();

        var candidates = new System.Collections.Generic.List<string>();

        // Normal published .NET app: runtimes/ next to the app base.
        candidates.Add(Path.Combine(AppContext.BaseDirectory, "runtimes", rid, "native", binaryName));

        // Assembly location (may be the NuGet cache lib folder under UiPath).
        var assemblyDir = Path.GetDirectoryName(typeof(SeleniumManagerResolver).Assembly.Location);
        if (!string.IsNullOrWhiteSpace(assemblyDir))
        {
            // Flat layout (in case the binary is shipped next to the assembly).
            candidates.Add(Path.Combine(assemblyDir, binaryName));
            // Same folder's runtimes (published output).
            candidates.Add(Path.Combine(assemblyDir, "runtimes", rid, "native", binaryName));
            // UiPath NuGet-cache layout: .../lib/{tfm}/ -> package root is two levels up.
            candidates.Add(Path.Combine(assemblyDir, "..", "..", "runtimes", rid, "native", binaryName));
        }

        foreach (var candidate in candidates)
        {
            string fullPath;
            try { fullPath = Path.GetFullPath(candidate); }
            catch { continue; }

            if (File.Exists(fullPath))
            {
                Environment.SetEnvironmentVariable("SE_MANAGER_PATH", fullPath);
                return;
            }
        }
    }

    private static string GetRuntimeId()
    {
        if (OperatingSystem.IsWindows())
            return "win";
        if (OperatingSystem.IsLinux())
            return RuntimeInformation.ProcessArchitecture == Architecture.Arm64 ? "linux-arm64" : "linux-x64";
        if (OperatingSystem.IsMacOS())
            return "osx";
        return "win";
    }
}