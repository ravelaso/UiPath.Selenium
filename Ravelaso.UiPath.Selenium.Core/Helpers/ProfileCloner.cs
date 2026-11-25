using System;
using System.IO;

namespace Ravelaso.UiPath.Selenium.Core;

/// <summary>
/// Copies extension-related files from one browser profile into another so that
/// a debuggable (non-default) user data directory can still load the user's
/// extensions. Required on Chromium 136+, where remote debugging is disabled
/// for the default user data directory.
/// </summary>
internal static class ProfileCloner
{
    private static readonly string[] ExtensionFolders =
    {
        "Extensions",
        "Local Extension Settings",
        "Extension State",
        "Extension Rules",
        "Extension Cookies"
    };

    private static readonly string[] ExtensionFiles =
    {
        "Preferences",
        "Secure Preferences"
    };

    private static readonly string[] LockFiles =
    {
        "SingletonLock",
        "SingletonSocket",
        "SingletonCookie",
        "lockfile"
    };

    /// <summary>
    /// Returns the default user data directory for the given browser on this OS.
    /// </summary>
    public static string? GetDefaultUserDataDir(WebBrowser browser)
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrWhiteSpace(localAppData))
            return null;

        return browser switch
        {
            WebBrowser.Edge => Path.Combine(localAppData, "Microsoft", "Edge", "User Data"),
            WebBrowser.Chrome => Path.Combine(localAppData, "Google", "Chrome", "User Data"),
            _ => null
        };
    }

    /// <summary>
    /// Copies extension data from <paramref name="sourceRoot"/> into
    /// <paramref name="targetRoot"/> (both are user data directories containing a
    /// "Default" profile folder). Returns true if anything was copied.
    /// </summary>
    public static bool CloneExtensions(string sourceRoot, string targetRoot)
    {
        if (string.IsNullOrWhiteSpace(sourceRoot) || string.IsNullOrWhiteSpace(targetRoot))
            return false;

        var sourceDefault = Path.Combine(sourceRoot, "Default");
        var targetDefault = Path.Combine(targetRoot, "Default");

        Directory.CreateDirectory(targetDefault);

        if (!Directory.Exists(sourceDefault))
            return false;

        var copied = false;

        foreach (var folder in ExtensionFolders)
        {
            var src = Path.Combine(sourceDefault, folder);
            var dst = Path.Combine(targetDefault, folder);
            if (Directory.Exists(src))
            {
                CopyDirectory(src, dst);
                copied = true;
            }
        }

        foreach (var file in ExtensionFiles)
        {
            var src = Path.Combine(sourceDefault, file);
            var dst = Path.Combine(targetDefault, file);
            if (File.Exists(src))
            {
                File.Copy(src, dst, overwrite: true);
                copied = true;
            }
        }

        return copied;
    }

    private static void CopyDirectory(string source, string destination)
    {
        Directory.CreateDirectory(destination);

        foreach (var file in Directory.GetFiles(source))
        {
            var name = Path.GetFileName(file);
            if (Array.Exists(LockFiles, l => string.Equals(l, name, StringComparison.OrdinalIgnoreCase)))
                continue;

            File.Copy(file, Path.Combine(destination, name), overwrite: true);
        }

        foreach (var dir in Directory.GetDirectories(source))
        {
            CopyDirectory(dir, Path.Combine(destination, Path.GetFileName(dir)));
        }
    }
}