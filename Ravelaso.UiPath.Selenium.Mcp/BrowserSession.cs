using System;
using Ravelaso.UiPath.Selenium.Core;

namespace Ravelaso.UiPath.Selenium.Mcp;

/// <summary>
/// Process-wide singleton holding the active RavelasoWebClient browser session.
/// MCP tool calls are sequential per conversation, so a single session is sufficient.
/// </summary>
public sealed class BrowserSession
{
    private readonly object _lock = new();
    private RavelasoWebClient? _client;
    private DateTime _startedUtc;

    public bool IsStarted => _client is not null;

    public RavelasoWebClient Client
    {
        get
        {
            lock (_lock)
            {
                if (_client is null)
                    throw new InvalidOperationException("No browser session is running. Call browser_start first.");
                return _client;
            }
        }
    }

    public string Start(Ravelaso.UiPath.Selenium.Core.WebBrowser browser, bool headless, string? debuggerAddress, int? remoteDebuggingPort)
    {
        lock (_lock)
        {
            if (_client is not null)
                return "A browser session is already running. Call browser_stop first.";

            var options = new BrowserOptions
            {
                WebBrowser = browser,
                Headless = headless,
                Maximize = !headless,
                GlobalDelay = TimeSpan.FromSeconds(1),
                ElementWaitTimeout = TimeSpan.FromSeconds(10),
                PageLoadTimeout = TimeSpan.FromSeconds(60),
            };
            if (!string.IsNullOrWhiteSpace(debuggerAddress))
                options.DebuggerAddress = debuggerAddress;
            if (remoteDebuggingPort.HasValue)
                options.RemoteDebuggingPort = remoteDebuggingPort;

            _client = new RavelasoWebClient(browser, options);
            _startedUtc = DateTime.UtcNow;
            return $"Browser started ({browser}, headless={headless}).";
        }
    }

    public string Stop()
    {
        lock (_lock)
        {
            if (_client is null) return "No browser session is running.";
            try { _client.Dispose(); } catch { /* ignore */ }
            _client = null;
            return "Browser stopped.";
        }
    }

    public string Status()
    {
        lock (_lock)
        {
            if (_client is null) return "No browser session running.";
            try
            {
                return $"Running since {_startedUtc:O}. URL={_client.GetUrl()} Title={_client.GetTitle()}";
            }
            catch (Exception ex)
            {
                return $"Running but error reading state: {ex.Message}";
            }
        }
    }
}