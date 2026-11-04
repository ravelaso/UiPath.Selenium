// ReSharper disable CheckNamespace

namespace Ravelaso.UiPath.Selenium.Core;

public partial class RavelasoWebClient
{
    /// <summary>
    /// Navigates to the specified URL.
    /// </summary>
    /// <param name="url">The URL to navigate to.</param>
    /// <example>
    /// <code>
    /// client.Navigate("https://example.com");
    /// </code>
    /// </example>
    public void Navigate(string url)
    {
        BeforeStep();
        _driver.Navigate().GoToUrl(url);
    }

    /// <summary>
    /// Gets the title of the current page.
    /// </summary>
    public string GetTitle()
    {
        BeforeStep();
        return _driver.Title;
    }

    /// <summary>
    /// Gets the current URL.
    /// </summary>
    public string GetUrl()
    {
        BeforeStep();
        return _driver.Url;
    }

    /// <summary>
    /// Gets the HTML source of the current page.
    /// </summary>
    public string GetPageSource()
    {
        BeforeStep();
        return _driver.PageSource;
    }
}

