using OpenQA.Selenium;

// ReSharper disable CheckNamespace
namespace Ravelaso.UiPath.Selenium.Core;

/// <summary>
/// WebBrowser and client configuration options.
/// </summary>
public class BrowserOptions
{
    /// <summary>
    /// WebBrowser type to launch. Default: <see cref="WebBrowser.Edge"/>.
    /// </summary>
    public WebBrowser WebBrowser { get; set; } = WebBrowser.Edge;

    /// <summary>
    /// Run webBrowser in headless mode (no UI). Default: false.
    /// </summary>
    public bool Headless { get; set; }

    /// <summary>
    /// Maximize window on startup (ignored if headless). Default: true.
    /// </summary>
    public bool Maximize { get; set; } = true;

    /// <summary>
    /// Window width in pixels. Default: 1920.
    /// </summary>
    public int WindowWidth { get; set; } = 1920;

    /// <summary>
    /// Window height in pixels. Default: 1080.
    /// </summary>
    public int WindowHeight { get; set; } = 1080;

    /// <summary>
    /// Page load timeout. Default: 60 seconds.
    /// </summary>
    public TimeSpan PageLoadTimeout { get; set; } = TimeSpan.FromSeconds(60);

    /// <summary>
    /// Implicit wait timeout for element search. Default: 0 (disabled).
    /// Note: do not mix with explicit waits; recommended to leave at zero.
    /// </summary>
    public TimeSpan ImplicitWait { get; set; } = TimeSpan.Zero;

    /// <summary>
    /// Global delay applied before each exposed operation (sleep). Default: 5 seconds.
    /// Can be set to <see cref="TimeSpan.Zero"/> to disable.
    /// </summary>
    public TimeSpan GlobalDelay { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Default timeout for conditional waits (Exists, WaitFor, etc.). Default: 10 seconds.
    /// </summary>
    public TimeSpan ElementWaitTimeout { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Path to the webBrowser executable. If null, default location is used. Default: null.
    /// </summary>
    public string? BinaryPath { get; set; }

    /// <summary>
    /// Extra command-line arguments for the webBrowser (e.g., ["--no-sandbox", "--disable-blink-features=AutomationControlled"]).
    /// </summary>
    public List<string> ExtraArguments { get; } = new();

    /// <summary>
    /// Remote debugging address to attach to an existing browser instance
    /// (e.g. "localhost:9222"). If set, no new browser is launched;
    /// the driver connects to the already-running browser.
    /// </summary>
    public string? DebuggerAddress { get; set; }

    // --- Profile / session (additive) ---

    /// <summary>
    /// Explicit user data directory to use. When null and <see cref="UseDefaultUserProfile"/> is false,
    /// an isolated temporary profile is created. Default: null.
    /// </summary>
    public string? UserDataDir { get; set; }

    /// <summary>
    /// Use the current user's default browser profile (extensions, cookies, settings).
    /// When true, no --user-data-dir is passed. Default: false.
    /// </summary>
    public bool UseDefaultUserProfile { get; set; }

    /// <summary>
    /// Launch in incognito mode. Default: false.
    /// </summary>
    public bool Incognito { get; set; }

    // --- Network / security (additive) ---

    /// <summary>
    /// Proxy server (e.g. "http://host:port"). Default: null (no proxy).
    /// </summary>
    public string? Proxy { get; set; }

    /// <summary>
    /// Comma-separated proxy bypass list. Default: null.
    /// </summary>
    public string? ProxyBypassList { get; set; }

    /// <summary>
    /// Accept insecure (self-signed) certificates. Default: false.
    /// </summary>
    public bool AcceptInsecureCertificates { get; set; }

    /// <summary>
    /// Disable web security (CORS). Default: false.
    /// </summary>
    public bool DisableWebSecurity { get; set; }

    /// <summary>
    /// Ignore certificate errors. Default: false.
    /// </summary>
    public bool IgnoreCertificateErrors { get; set; }

    // --- Runtime / performance (additive) ---

    /// <summary>
    /// Disable GPU hardware acceleration. Default: false.
    /// </summary>
    public bool DisableGpu { get; set; }

    /// <summary>
    /// Disable the sandbox. Default: false.
    /// </summary>
    public bool NoSandbox { get; set; }

    /// <summary>
    /// Disable extensions. Default: false.
    /// </summary>
    public bool DisableExtensions { get; set; }

    /// <summary>
    /// Override the User-Agent string. Default: null.
    /// </summary>
    public string? UserAgent { get; set; }

    // --- Launch (additive) ---

    /// <summary>
    /// Remote debugging port for <see cref="BrowserLauncher"/>. Default: null (9222).
    /// </summary>
    public int? RemoteDebuggingPort { get; set; }

    /// <summary>
    /// Timeout waiting for the debug port to become available. Default: 10 seconds.
    /// </summary>
    public TimeSpan LaunchTimeout { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Window position as "x,y". Default: null (browser default).
    /// </summary>
    public string? WindowPosition { get; set; }

    /// <summary>
    /// Start maximized on launch. Default: false (use <see cref="Maximize"/> for driver-level maximize).
    /// </summary>
    public bool StartMaximized { get; set; }

    // --- Selenium-level (additive) ---

    /// <summary>
    /// Page load strategy. Default: null (browser default).
    /// </summary>
    public PageLoadStrategy? PageLoadStrategy { get; set; }

    /// <summary>
    /// Unhandled prompt behavior. Default: null (browser default).
    /// </summary>
    public UnhandledPromptBehavior? UnhandledPromptBehavior { get; set; }

}