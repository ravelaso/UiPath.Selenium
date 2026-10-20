namespace Ravelaso.UiPath.Selenium.Core;

/// <summary>
/// Browser and client configuration options.
/// </summary>
public class BrowserOptions
{
    /// <summary>
    /// Browser type to launch. Default: <see cref="BrowserType.Edge"/>.
    /// </summary>
    public BrowserType Browser { get; set; } = BrowserType.Edge;

    /// <summary>
    /// Run browser in headless mode (no UI). Default: false.
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
    /// Path to the browser executable. If null, default location is used. Default: null.
    /// </summary>
    public string? BinaryPath { get; set; }

    /// <summary>
    /// Extra command-line arguments for the browser (e.g., ["--no-sandbox", "--disable-blink-features=AutomationControlled"]).
    /// </summary>
    public List<string> ExtraArguments { get; } = new();
}
