using OpenQA.Selenium;

// ReSharper disable CheckNamespace
namespace Ravelaso.UiPath.Selenium.Core;

public partial class RavelasoWebClient
{
    // /// <summary>
    // /// Runs multiple click strategies on a hidden/hover-revealed button and reports
    // /// which one successfully cleared the associated input. Useful for diagnosing
    // /// hover-revealed Angular/React components.
    // /// </summary>
    // /// <param name="clearBtnLocator">Locator for the clear button.</param>
    // /// <param name="inputLocator">Locator for the input whose value should be cleared.</param>
    // /// <param name="hoverTargetLocator">Optional locator of the element to hover to reveal the button.</param>
    // /// <returns>A multi-line diagnostic report.</returns>
    // private string DiagnoseClearButton(By clearBtnLocator, By inputLocator, By? hoverTargetLocator = null)
    // {
    //     BeforeStep();
    //     var js = (IJavaScriptExecutor)_driver;
    //     var sb = new System.Text.StringBuilder();
    //     var input = WaitElement(inputLocator, TimeSpan.Zero);
    //     var initialValue = input.GetAttribute("value");
    //     sb.AppendLine($"Initial input value: '{initialValue}'");
    //     sb.AppendLine($"Button displayed (before): {WaitElement(clearBtnLocator, TimeSpan.Zero).Displayed}");
    //
    //     // Estrategia 1: Hover (Actions) + force + .click()
    //     try
    //     {
    //         if (hoverTargetLocator != null)
    //         {
    //             var ht = WaitElement(hoverTargetLocator, TimeSpan.Zero);
    //             new Actions(_driver).MoveToElement(ht).Perform();
    //             Thread.Sleep(300);
    //         }
    //
    //         var btn1 = WaitElement(clearBtnLocator, TimeSpan.Zero);
    //         js.ExecuteScript(
    //             "var e=arguments[0]; e.style.opacity='1'; e.style.visibility='visible';" +
    //             "e.style.display='inline-block'; e.style.pointerEvents='auto';" +
    //             "e.classList.remove('ng-hide');", btn1);
    //         Thread.Sleep(50);
    //         btn1 = WaitElement(clearBtnLocator, TimeSpan.Zero);
    //         js.ExecuteScript("arguments[0].click();", btn1);
    //         Thread.Sleep(500);
    //         var val1 = WaitElement(inputLocator, TimeSpan.Zero).GetAttribute("value");
    //         sb.AppendLine(
    //             $"Strategy 1 (Hover+Force+.click): input='{val1}' {(val1 != initialValue ? "CLEARED" : "not cleared")}");
    //     }
    //     catch (Exception ex)
    //     {
    //         sb.AppendLine($"Strategy 1: ERROR {ex.GetType().Name}: {ex.Message}");
    //     }
    //
    //     // Estrategia 2: dispatchEvent MouseEvent completo
    //     try
    //     {
    //         var btn2 = WaitElement(clearBtnLocator, TimeSpan.Zero);
    //         js.ExecuteScript(
    //             "var e=arguments[0];" +
    //             "e.style.opacity='1'; e.style.visibility='visible';" +
    //             "e.style.display='inline-block'; e.style.pointerEvents='auto';" +
    //             "e.classList.remove('ng-hide');" +
    //             "['mousedown','mouseup','click'].forEach(function(t){e.dispatchEvent(new MouseEvent(t,{bubbles:true,cancelable:true,view:window}));});",
    //             btn2);
    //         Thread.Sleep(500);
    //         var val2 = WaitElement(inputLocator, TimeSpan.Zero).GetAttribute("value");
    //         sb.AppendLine(
    //             $"Strategy 2 (dispatchEvent): input='{val2}' {(val2 != initialValue ? "CLEARED" : "not cleared")}");
    //     }
    //     catch (Exception ex)
    //     {
    //         sb.AppendLine($"Strategy 2: ERROR {ex.GetType().Name}: {ex.Message}");
    //     }
    //
    //     // Estrategia 3: ClickJs
    //     try
    //     {
    //         ClickJs(clearBtnLocator, TimeSpan.FromSeconds(5));
    //         Thread.Sleep(500);
    //         var val3 = WaitElement(inputLocator, TimeSpan.Zero).GetAttribute("value");
    //         sb.AppendLine($"Strategy 3 (ClickJs): input='{val3}' {(val3 != initialValue ? "CLEARED" : "not cleared")}");
    //     }
    //     catch (Exception ex)
    //     {
    //         sb.AppendLine($"Strategy 3: ERROR {ex.GetType().Name}: {ex.Message}");
    //     }
    //
    //     // Estrategia 4: ClickHiddenJs
    //     try
    //     {
    //         ClickHiddenJs("button[data-t-id='clear-datepicker'][aria-label='Clear']", TimeSpan.FromSeconds(5));
    //         Thread.Sleep(500);
    //         var val4 = WaitElement(inputLocator, TimeSpan.Zero).GetAttribute("value");
    //         sb.AppendLine(
    //             $"Strategy 4 (ClickHiddenJs): input='{val4}' {(val4 != initialValue ? "CLEARED" : "not cleared")}");
    //     }
    //     catch (Exception ex)
    //     {
    //         sb.AppendLine($"Strategy 4: ERROR {ex.GetType().Name}: {ex.Message}");
    //     }
    //
    //     // Estrategia 5: HoverAndClick
    //     try
    //     {
    //         HoverAndClick(clearBtnLocator,
    //             hoverTargetLocator ?? Loc.Tag("div").Attr("data-t-id", "date-rangepicker-inputs"),
    //             TimeSpan.FromSeconds(5));
    //         Thread.Sleep(500);
    //         var val5 = WaitElement(inputLocator, TimeSpan.Zero).GetAttribute("value");
    //         sb.AppendLine(
    //             $"Strategy 5 (HoverAndClick): input='{val5}' {(val5 != initialValue ? "CLEARED" : "not cleared")}");
    //     }
    //     catch (Exception ex)
    //     {
    //         sb.AppendLine($"Strategy 5: ERROR {ex.GetType().Name}: {ex.Message}");
    //     }
    //
    //     // Diagnóstico de visibilidad
    //     // Diagnóstico de visibilidad (el botón puede haber sido removido del DOM tras el click)
    //     try
    //     {
    //         var btnDiag = WaitElement(clearBtnLocator, TimeSpan.Zero);
    //         sb.AppendLine(
    //             $"Button opacity: {js.ExecuteScript("return getComputedStyle(arguments[0]).opacity;", btnDiag)}");
    //         sb.AppendLine($"Button displayed: {btnDiag.Displayed}");
    //         sb.AppendLine($"Button enabled: {btnDiag.Enabled}");
    //     }
    //     catch (Exception ex)
    //     {
    //         sb.AppendLine($"Button no longer in DOM (likely cleared successfully): {ex.GetType().Name}");
    //     }
    //
    //     if (hoverTargetLocator != null)
    //         try
    //         {
    //             var ht = WaitElement(hoverTargetLocator, TimeSpan.Zero);
    //             new Actions(_driver).MoveToElement(ht).Perform();
    //             Thread.Sleep(500);
    //             var btnDiag2 = WaitElement(clearBtnLocator, TimeSpan.Zero);
    //             sb.AppendLine(
    //                 $"After hover - opacity: {js.ExecuteScript("return getComputedStyle(arguments[0]).opacity;", btnDiag2)}");
    //             sb.AppendLine($"After hover - displayed: {btnDiag2.Displayed}");
    //         }
    //         catch (Exception ex)
    //         {
    //             sb.AppendLine($"After hover: {ex.GetType().Name}: {ex.Message}");
    //         }
    //
    //     return sb.ToString();
    // }


    /// <summary>
    /// Takes a screenshot and saves it to the specified file path (PNG format).
    /// Creates the directory if it does not exist.
    /// </summary>
    /// <param name="path">The file path where the screenshot will be saved.</param>
    /// <returns>The absolute path to the saved screenshot.</returns>
    public string Screenshot(string path)
    {
        BeforeStep();
        var fullPath = Path.GetFullPath(path);
        var directory = Path.GetDirectoryName(fullPath);

        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        var screenshot = ((ITakesScreenshot)_driver).GetScreenshot();
        screenshot.SaveAsFile(fullPath);

        return fullPath;
    }

    /// <summary>
    /// Executes JavaScript in the context of the current page.
    /// </summary>
    /// <param name="script">The JavaScript code to execute.</param>
    /// <param name="args">Optional arguments to pass to the script.</param>
    /// <returns>The result of the script execution, or null if no return value.</returns>
    public object? ExecuteScript(string script, params object[] args)
    {
        BeforeStep();
        return ((IJavaScriptExecutor)_driver).ExecuteScript(script, args);
    }
}