// ReSharper disable CheckNamespace

namespace Ravelaso.UiPath.Selenium.Core;

public partial class RavelasoWebClient
{
    /// <summary>
    /// Downloads a file triggered by the specified element.
    /// First tries to extract a direct download URL from the element and download it
    /// over HTTP using the browser cookies. If no URL is available, falls back to a
    /// runtime Chromium/Edge DevTools (CDP) download into a temporary folder and
    /// then moves the completed file to <paramref name="destinationPath"/>.
    /// </summary>
    /// <param name="downloadButton">Locator for the element that triggers the download.</param>
    /// <param name="destinationPath">Final path where the downloaded file should be saved.</param>
    /// <param name="beforeStepDelay">Optional delay before this operation. If null, uses GlobalDelay.</param>
/// <param name="timeout">Optional timeout. Defaults to <see cref="ElementWaitTimeout"/> or 60 seconds.</param>
    /// <returns>The final path of the downloaded file.</returns>
    public string DownloadFile(Locator downloadButton, string destinationPath, TimeSpan? beforeStepDelay = null, TimeSpan? timeout = null)
    {
        BeforeStep(beforeStepDelay);

        if (downloadButton == null)
            throw new ArgumentNullException(nameof(downloadButton));
        if (string.IsNullOrWhiteSpace(destinationPath))
            throw new ArgumentException("Destination path cannot be empty.", nameof(destinationPath));

        var effectiveTimeout =
            timeout ?? (ElementWaitTimeout > TimeSpan.Zero ? ElementWaitTimeout : TimeSpan.FromSeconds(60));
        var fullPath = Path.GetFullPath(destinationPath);
        var parent = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrWhiteSpace(parent))
            Directory.CreateDirectory(parent);

        var element = WaitElement(downloadButton.ToBy(), effectiveTimeout);
        var url = DownloadHelper.TryGetDownloadUrl(_driver, element);

        if (!string.IsNullOrWhiteSpace(url))
        {
            DownloadHelper.DownloadHttp(_driver, url, fullPath, effectiveTimeout);
            return fullPath;
        }

        return DownloadHelper.DownloadViaCdp(_driver, element, fullPath, effectiveTimeout);
    }

    /// <summary>
    /// Downloads a file from a direct URL using the browser cookies/session.
    /// </summary>
    public string DownloadFromUrl(string url, string destinationPath, TimeSpan? beforeStepDelay = null, TimeSpan? timeout = null)
    {
        BeforeStep(beforeStepDelay);

        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("URL cannot be empty.", nameof(url));
        if (string.IsNullOrWhiteSpace(destinationPath))
            throw new ArgumentException("Destination path cannot be empty.", nameof(destinationPath));

        var effectiveTimeout =
            timeout ?? (ElementWaitTimeout > TimeSpan.Zero ? ElementWaitTimeout : TimeSpan.FromSeconds(60));
        var fullPath = Path.GetFullPath(destinationPath);
        var parent = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrWhiteSpace(parent))
            Directory.CreateDirectory(parent);

        DownloadHelper.DownloadHttp(_driver, url, fullPath, effectiveTimeout);
        return fullPath;
    }

    /// <summary>
    /// Tries to extract a download URL from an element without triggering the download.
    /// </summary>
    public string? TryGetDownloadUrl(Locator downloadButton)
    {
        BeforeStep();

        if (downloadButton == null)
            throw new ArgumentNullException(nameof(downloadButton));

        var element = WaitElement(downloadButton.ToBy(), ElementWaitTimeout);
        return DownloadHelper.TryGetDownloadUrl(_driver, element);
    }

}

