using OpenQA.Selenium;

// ReSharper disable CheckNamespace
namespace Ravelaso.UiPath.Selenium.Core;

public partial class RavelasoWebClient
{
    /// <summary>
    /// Selects an option from a native &lt;select&gt; element by value or visible text.
    /// </summary>
    /// <param name="locator">The &lt;select&gt; element locator.</param>
    /// <param name="value">The value or visible text to select.</param>
    /// <param name="beforeStepDelay">Optional delay before this operation. If null, uses GlobalDelay.</param>
/// <param name="timeout">Optional timeout override for this operation. If null, uses <see cref="ElementWaitTimeout"/>.</param>
    public bool Select(By locator, string value, TimeSpan? beforeStepDelay = null, TimeSpan? timeout = null)
    {
        BeforeStep(beforeStepDelay);
        var element = WaitElement(locator, timeout);
        return SelectNative(element, value);
    }

    /// <summary>
    /// Selects an option from a native &lt;select&gt; element by index.
    /// </summary>
    /// <param name="locator">The &lt;select&gt; element locator.</param>
    /// <param name="index">The 0-based index of the option to select.</param>
    /// <param name="beforeStepDelay">Optional delay before this operation. If null, uses GlobalDelay.</param>
/// <param name="timeout">Optional timeout override for this operation. If null, uses <see cref="ElementWaitTimeout"/>.</param>
    public bool SelectByIndex(By locator, int index, TimeSpan? beforeStepDelay = null, TimeSpan? timeout = null)
    {
        BeforeStep(beforeStepDelay);
        var element = WaitElement(locator, timeout);
        return SelectByIndexNative(element, index);
    }

    private static bool SelectNative(IWebElement selectElement, string value)
    {
        var options = selectElement.FindElements(By.TagName("option"));

        // Try to match by value attribute first
        foreach (var option in options)
        {
            var optionValue = option.GetAttribute("value");
            if (!string.IsNullOrEmpty(optionValue) && string.Equals(optionValue, value, StringComparison.Ordinal))
            {
                option.Click();
                return true;
            }
        }

        // Try to match by visible text (case-insensitive or exact)
        foreach (var option in options)
        {
            var text = option.Text.Trim();
            if (string.Equals(text, value, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(text, value, StringComparison.Ordinal))
            {
                option.Click();
                return true;
            }
        }

        return false;
    }

    private static bool SelectByIndexNative(IWebElement selectElement, int index)
    {
        var options = selectElement.FindElements(By.TagName("option"));
        if (index < 0 || index >= options.Count) return false;
        options[index].Click();
        return true;
    }
}

