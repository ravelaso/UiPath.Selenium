// ReSharper disable InconsistentNaming
namespace Ravelaso.UiPath.Selenium.Tests;

/// <summary>
/// Shared test utilities and HTML fixtures for integration tests.
/// </summary>
public static class TestFixtures
{
    /// <summary>
    /// Gets the HTML test page used for integration tests.
    /// Contains a login form with text inputs, a dropdown, and a hidden result div.
    /// </summary>
    public static string GetTestPageHTML() => """
        <!DOCTYPE html>
        <html lang="en">
        <head>
            <meta charset="UTF-8">
            <title>Test Page</title>
            <style>
                body { font-family: Arial, sans-serif; padding: 20px; }
                form { margin: 20px 0; }
                input, select { margin: 5px; padding: 5px; }
                button { padding: 8px 16px; cursor: pointer; }
                #result { display: none; color: green; font-weight: bold; }
            </style>
        </head>
        <body>
            <h1>Login Test Form</h1>
            <form id="loginForm">
                <div>
                    <label for="username">Username:</label>
                    <input id="username" type="text" class="form-input" />
                </div>
                <div>
                    <label for="password">Password:</label>
                    <input id="password" type="password" class="form-input" />
                </div>
                <div>
                    <label for="country">Country:</label>
                    <select id="country" name="country">
                        <option value="">-- Select --</option>
                        <option value="us">United States</option>
                        <option value="mx">Mexico</option>
                        <option value="ca">Canada</option>
                    </select>
                </div>
                <button id="submit" type="button">Submit</button>
            </form>
            <div id="result">Login successful!</div>
            <script>
                document.getElementById('submit').addEventListener('click', function() {
                    document.getElementById('result').style.display = 'block';
                });
            </script>
        </body>
        </html>
        """;

    /// <summary>
    /// Converts the test HTML to a data: URL for navigation without network dependency.
    /// </summary>
    public static string GetTestPageDataURL()
    {
        var html = GetTestPageHTML();
        var encoded = Uri.EscapeDataString(html);
        return $"data:text/html,{encoded}";
    }

    /// <summary>
    /// Verifies that a condition is true; throws if false.
    /// </summary>
    public static void Assert(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException($"Assertion failed: {message}");
    }

    /// <summary>
    /// Verifies that two strings are equal.
    /// </summary>
    public static void AssertEqual(string? expected, string? actual, string message)
    {
        if (expected != actual)
            throw new InvalidOperationException($"Assertion failed: {message}. Expected: '{expected}', Actual: '{actual}'");
    }
}
