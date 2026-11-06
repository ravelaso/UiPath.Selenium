using OpenQA.Selenium;

// ReSharper disable CheckNamespace
namespace Ravelaso.UiPath.Selenium.Core;

public partial class RavelasoWebClient
{
    /// <summary>
    /// Sets the browser zoom using a decimal value, where 1.0 is 100%.
    /// </summary>
    /// <param name="percent">Zoom value, for example 0.8 for 80%.</param>
    public void SetZoom(double percent, TimeSpan? beforeStepDelay = null, TimeSpan? timeout = null)
    {
        BeforeStep(beforeStepDelay);
        ApplyZoom(percent);
        Thread.Sleep(timeout ?? TimeSpan.FromSeconds(3));
    }

    /// <summary>
    /// Resets the browser zoom to 100%.
    /// </summary>
    public void ResetZoom(TimeSpan? beforeStepDelay = null, TimeSpan? timeout = null)
    {
        BeforeStep(beforeStepDelay);
        ApplyZoom(1.0);
        Thread.Sleep(timeout ?? TimeSpan.FromSeconds(3));
    }

    private void ApplyZoom(double percent)
    {
        if (percent <= 0)
            throw new ArgumentOutOfRangeException(nameof(percent), "Zoom must be greater than zero.");

        var zoom = Math.Clamp(percent, 0.25, 5.0);
        var script =
            $"document.body.style.zoom = '{zoom * 100}%'; document.documentElement.style.zoom = '{zoom * 100}%';";

        ((IJavaScriptExecutor)_driver).ExecuteScript(script);
    }
}





