using System.Diagnostics;
using System.Net.Sockets;

// ReSharper disable CheckNamespace
namespace Ravelaso.UiPath.Selenium.Core;

/// <summary>
/// Launches a Chromium-based browser with remote debugging enabled.
/// The launched browser can then be attached to with <see cref="RavelasoWebClient"/>
/// using <see cref="BrowserOptions.DebuggerAddress"/>.
/// </summary>
public static class BrowserLauncher
{
    /// <summary>
    /// Launches a Chromium-based browser with remote debugging enabled.
    /// Returns null if the debug port is already open.
    /// </summary>
    public static Process? Launch(BrowserOptions? options = null)
    {
        options ??= new BrowserOptions();

        if (string.IsNullOrWhiteSpace(options.DebuggerAddress))
            options.DebuggerAddress = "localhost:9222";

        var (host, port) = ParseAddress(options.DebuggerAddress);

        if (IsDebugPortOpen(options.DebuggerAddress))
            return null;

        if (options.WebBrowser is not (WebBrowser.Chrome or WebBrowser.Edge))
            throw new NotSupportedException("BrowserLauncher currently supports Chromium-based browsers only (Chrome, Edge).");

        var executable = ResolveExecutable(options.WebBrowser, options.BinaryPath);
        if (string.IsNullOrWhiteSpace(executable) || !File.Exists(executable))
            throw new FileNotFoundException("Could not find the browser executable.", executable);

        var userDataDir = Path.Combine(Path.GetTempPath(), "RavelasoBrowserDebug", port.ToString());
        Directory.CreateDirectory(userDataDir);

        var arguments = new List<string>
        {
            $"--remote-debugging-port={port}",
            $"--user-data-dir=\"{userDataDir}\"",
            "--no-first-run",
            "--no-default-browser-check"
        };

        if (options.Headless)
            arguments.Add("--headless=new");

        if (options.Maximize && !options.Headless)
            arguments.Add("--start-maximized");

        arguments.Add($"--window-size={options.WindowWidth},{options.WindowHeight}");

        if (!string.IsNullOrWhiteSpace(options.BinaryPath))
            arguments.Add($"\"{options.BinaryPath}\"");

        foreach (var arg in options.ExtraArguments)
            arguments.Add(arg);

        var startInfo = new ProcessStartInfo
        {
            FileName = executable,
            Arguments = string.Join(" ", arguments),
            UseShellExecute = true
        };

        var process = Process.Start(startInfo);
        if (process == null)
            throw new InvalidOperationException("Failed to start the browser process.");

        var stopwatch = Stopwatch.StartNew();
        while (stopwatch.Elapsed < TimeSpan.FromSeconds(10))
        {
            if (IsDebugPortOpen(options.DebuggerAddress))
                return process;

            Thread.Sleep(250);
        }

        throw new TimeoutException($"Browser debug port {options.DebuggerAddress} did not become available within 10 seconds.");
    }

    /// <summary>
    /// Checks whether a remote debugging port is already open.
    /// </summary>
    public static bool IsDebugPortOpen(string address)
    {
        try
        {
            var (host, port) = ParseAddress(address);
            using var client = new TcpClient();
            var result = client.BeginConnect(host, port, null, null);
            var success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromMilliseconds(500));
            if (!success)
                return false;

            client.EndConnect(result);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static (string Host, int Port) ParseAddress(string address)
    {
        if (string.IsNullOrWhiteSpace(address))
            return ("localhost", 9222);

        var parts = address.Split(':', 2);
        var host = string.IsNullOrWhiteSpace(parts[0]) ? "localhost" : parts[0];
        var port = 9222;

        if (parts.Length > 1 && int.TryParse(parts[1], out var parsedPort))
            port = parsedPort;

        return (host, port);
    }

    private static string? ResolveExecutable(WebBrowser browser, string? binaryPath)
    {
        if (!string.IsNullOrWhiteSpace(binaryPath) && File.Exists(binaryPath))
            return binaryPath;

        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

        string[] candidates = browser switch
        {
            WebBrowser.Edge => new[]
            {
                Path.Combine(programFilesX86, "Microsoft", "Edge", "Application", "msedge.exe"),
                Path.Combine(programFiles, "Microsoft", "Edge", "Application", "msedge.exe")
            },
            WebBrowser.Chrome => new[]
            {
                Path.Combine(programFiles, "Google", "Chrome", "Application", "chrome.exe"),
                Path.Combine(programFilesX86, "Google", "Chrome", "Application", "chrome.exe")
            },
            _ => Array.Empty<string>()
        };

        return candidates.FirstOrDefault(File.Exists);
    }
}