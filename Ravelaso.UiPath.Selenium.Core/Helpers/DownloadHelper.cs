using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Edge;

namespace Ravelaso.UiPath.Selenium.Core;

internal static class DownloadHelper
{
    private static readonly string[] TemporaryExtensions = { ".crdownload", ".tmp", ".part", ".download" };

    public static string? TryGetDownloadUrl(IWebDriver driver, IWebElement element)
    {
        foreach (var attr in new[] { "href", "data-href", "data-url", "data-download-url", "data-src", "data-file" })
        {
            try
            {
                var value = element.GetAttribute(attr);
                if (!string.IsNullOrWhiteSpace(value))
                    return value.Trim();
            }
            catch
            {
                // Ignore attribute access failures and continue.
            }
        }

        // Some download controls store the URL inside an onclick handler.
        try
        {
            var onclick = element.GetAttribute("onclick");
            if (!string.IsNullOrWhiteSpace(onclick))
            {
                var match = Regex.Match(onclick, @"['""]([^'""]+)['""]");
                if (match.Success)
                {
                    var value = match.Groups[1].Value.Trim();
                    if (value.Contains('/') || value.Contains('?') || value.Contains('.'))
                        return value;
                }
            }
        }
        catch
        {
            // Ignore onclick parsing failures.
        }

        return null;
    }

    public static void DownloadHttp(IWebDriver driver, string url, string destinationPath, TimeSpan timeout)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("URL cannot be empty.", nameof(url));
        if (string.IsNullOrWhiteSpace(destinationPath))
            throw new ArgumentException("Destination path cannot be empty.", nameof(destinationPath));

        if (!Uri.IsWellFormedUriString(url, UriKind.Absolute))
        {
            url = new Uri(new Uri(driver.Url), url).AbsoluteUri;
        }

        var effectiveTimeout = timeout > TimeSpan.Zero ? timeout : TimeSpan.FromSeconds(60);

        using var handler = new HttpClientHandler
        {
            UseCookies = true,
            CookieContainer = new CookieContainer(),
            AutomaticDecompression = System.Net.DecompressionMethods.All
        };

        CopyCookies(driver, handler.CookieContainer);

        using var http = new HttpClient(handler)
        {
            Timeout = effectiveTimeout
        };

        var userAgent = GetUserAgent(driver);
        if (!string.IsNullOrWhiteSpace(userAgent))
        {
            try { http.DefaultRequestHeaders.UserAgent.ParseAdd(userAgent); }
            catch { /* Ignore invalid user-agent values. */ }
        }

        using var response = http.GetAsync(url).GetAwaiter().GetResult();
        response.EnsureSuccessStatusCode();

        var parent = Path.GetDirectoryName(destinationPath);
        if (!string.IsNullOrWhiteSpace(parent))
            Directory.CreateDirectory(parent);

        using var input = response.Content.ReadAsStreamAsync().GetAwaiter().GetResult();
        using var output = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None);
        input.CopyTo(output);
    }

    public static string DownloadViaCdp(IWebDriver driver, IWebElement downloadButton, string destinationPath, TimeSpan timeout)
    {
        if (driver is not ChromeDriver and not EdgeDriver)
            throw new NotSupportedException("CDP download fallback is only supported on Chromium-based browsers (Chrome/Edge).");

        if (downloadButton == null)
            throw new ArgumentNullException(nameof(downloadButton));
        if (string.IsNullOrWhiteSpace(destinationPath))
            throw new ArgumentException("Destination path cannot be empty.", nameof(destinationPath));

        var effectiveTimeout = timeout > TimeSpan.Zero ? timeout : TimeSpan.FromSeconds(60);
        var tempFolder = Path.Combine(Path.GetTempPath(), "RavelasoDownloads", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempFolder);

        var beforeFiles = new HashSet<string>(Directory.GetFiles(tempFolder), StringComparer.OrdinalIgnoreCase);

        try
        {
            ExecuteCdp(driver, "Page.setDownloadBehavior", new Dictionary<string, object?>
            {
                ["behavior"] = "allowAndName",
                ["downloadPath"] = tempFolder,
                ["eventsEnabled"] = true
            });

            downloadButton.Click();

            var completedFile = WaitForCompletedFile(tempFolder, beforeFiles, effectiveTimeout);
            if (completedFile == null)
            {
                var files = string.Join(", ", Directory.GetFiles(tempFolder).Select(Path.GetFileName));
                throw new TimeoutException(
                    $"Download did not complete within {effectiveTimeout.TotalSeconds}s. " +
                    $"Temp folder: {tempFolder}. Files: {files}");
            }

            var parent = Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrWhiteSpace(parent))
                Directory.CreateDirectory(parent);

            if (File.Exists(destinationPath))
                File.Delete(destinationPath);

            File.Move(completedFile, destinationPath);
            return destinationPath;
        }
        finally
        {
            try
            {
                ExecuteCdp(driver, "Page.setDownloadBehavior", new Dictionary<string, object?>
                {
                    ["behavior"] = "default"
                });
            }
            catch
            {
                // Best effort restore.
            }

            try
            {
                if (Directory.Exists(tempFolder))
                    Directory.Delete(tempFolder, true);
            }
            catch
            {
                // Best effort cleanup.
            }
        }
    }

    private static string? WaitForCompletedFile(string tempFolder, HashSet<string> beforeFiles, TimeSpan timeout)
    {
        var stopwatch = Stopwatch.StartNew();
        long lastSize = -1;
        DateTime lastSizeChange = DateTime.UtcNow;
        string? candidate = null;

        while (stopwatch.Elapsed < timeout)
        {
            var files = Directory.GetFiles(tempFolder);
            var completedCandidates = files
                .Where(f => !IsTemporaryDownloadFile(f))
                .Where(f => !beforeFiles.Contains(f, StringComparer.OrdinalIgnoreCase))
                .ToList();

            // If the folder was empty before the click, all completed files are new.
            if (completedCandidates.Count == 0 && beforeFiles.Count == 0)
            {
                completedCandidates = files
                    .Where(f => !IsTemporaryDownloadFile(f))
                    .ToList();
            }

            if (completedCandidates.Count > 0)
            {
                candidate = completedCandidates
                    .OrderByDescending(f => File.GetLastWriteTimeUtc(f))
                    .First();

                var size = new FileInfo(candidate).Length;
                if (size != lastSize)
                {
                    lastSize = size;
                    lastSizeChange = DateTime.UtcNow;
                }
                else if (DateTime.UtcNow - lastSizeChange >= TimeSpan.FromMilliseconds(500))
                {
                    return candidate;
                }
            }
            else
            {
                lastSize = -1;
                lastSizeChange = DateTime.UtcNow;
                candidate = null;
            }

            Thread.Sleep(250);
        }

        return null;
    }

    private static bool IsTemporaryDownloadFile(string path)
    {
        var extension = Path.GetExtension(path);
        return TemporaryExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
    }

    private static void ExecuteCdp(IWebDriver driver, string command, Dictionary<string, object?> parameters)
    {
        if (driver is ChromeDriver chrome)
        {
            chrome.ExecuteCdpCommand(command, parameters);
            return;
        }
        if (driver is EdgeDriver edge)
        {
            edge.ExecuteCdpCommand(command, parameters);
            return;
        }

        throw new NotSupportedException("CDP download fallback is only supported on Chromium-based browsers (Chrome/Edge).");
    }

    private static void CopyCookies(IWebDriver driver, CookieContainer container)
    {
        try
        {
            var uri = new Uri(driver.Url);
            var host = uri.Host;

            foreach (var cookie in driver.Manage().Cookies.AllCookies)
            {
                var domain = string.IsNullOrWhiteSpace(cookie.Domain) ? host : cookie.Domain.TrimStart('.');
                var path = string.IsNullOrWhiteSpace(cookie.Path) ? "/" : cookie.Path;

                try
                {
                    container.Add(new System.Net.Cookie(cookie.Name, cookie.Value, path, domain));
                }
                catch
                {
                    try
                    {
                        container.Add(uri, new System.Net.Cookie(cookie.Name, cookie.Value, path, domain));
                    }
                    catch
                    {
                        // Ignore cookies that cannot be transferred.
                    }
                }
            }
        }
        catch
        {
            // Ignore cookie extraction failures.
        }
    }

    private static string GetUserAgent(IWebDriver driver)
    {
        try
        {
            return ((IJavaScriptExecutor)driver).ExecuteScript("return navigator.userAgent;")?.ToString() ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }
}