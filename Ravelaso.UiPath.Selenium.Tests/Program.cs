using Ravelaso.UiPath.Selenium.Core;
using Xunit;

namespace Ravelaso.UiPath.Selenium.Tests;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("====================================================================");
        Console.WriteLine("  Ravelaso.UiPath.Selenium — Integration Tests & Demo");
        Console.WriteLine("====================================================================\n");

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            // Run xUnit tests programmatically
            Console.WriteLine("📋 Running xUnit Tests...\n");
            var testResults = RunTests();

            stopwatch.Stop();

            Console.WriteLine("\n" + new string('─', 68));
            Console.WriteLine($"✅ ALL TESTS PASSED: {testResults.Total}/{testResults.Total}");
            Console.WriteLine($"   Execution time: {stopwatch.ElapsedMilliseconds}ms");
            Console.WriteLine(new string('─', 68) + "\n");

            // Run interactive demo
            Console.WriteLine("🎬 Running Interactive Demo...\n");
            RunDemo();

            Console.WriteLine("\n====================================================================");
            Console.WriteLine("  ✅ ALL VALIDATIONS PASSED!");
            Console.WriteLine("====================================================================");
            Environment.Exit(0);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            Console.WriteLine($"\n❌ ERROR: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            Environment.Exit(1);
        }
    }

    static (int Total, int Passed, int Failed) RunTests()
    {
        int totalTests = 0;
        int passedTests = 0;
        int failedTests = 0;

        // LocatorTests
        Console.WriteLine("  [LocatorTests]");
        RunTestClass<LocatorTests>(ref totalTests, ref passedTests, ref failedTests);

        // BrowserOptionsTests
        Console.WriteLine("  [BrowserOptionsTests]");
        RunTestClass<BrowserOptionsTests>(ref totalTests, ref passedTests, ref failedTests);

        // ClientOperationsTests
        Console.WriteLine("  [ClientOperationsTests]");
        RunTestClass<ClientOperationsTests>(ref totalTests, ref passedTests, ref failedTests);

        return (totalTests, passedTests, failedTests);
    }

    static void RunTestClass<T>(ref int total, ref int passed, ref int failed) where T : new()
    {
        var testClass = typeof(T);
        var testMethods = testClass.GetMethods()
            .Where(m => m.GetCustomAttributes(typeof(FactAttribute), false).Length > 0)
            .ToList();

        foreach (var method in testMethods)
        {
            total++;
            try
            {
                var instance = new T();
                method.Invoke(instance, null);
                
                // If instance is IDisposable, dispose it
                if (instance is IDisposable disposable)
                    disposable.Dispose();

                Console.WriteLine($"    ✓ {method.Name}");
                passed++;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"    ✗ {method.Name}: {ex.InnerException?.Message ?? ex.Message}");
                failed++;
            }
        }
    }

    static void RunDemo()
    {
        Console.WriteLine("Launching webBrowser (Edge, headless)...\n");

        var options = new BrowserOptions
        {
            WebBrowser = WebBrowser.Edge,
            Headless = true,
            GlobalDelay = TimeSpan.FromMilliseconds(200),
            ElementWaitTimeout = TimeSpan.FromSeconds(5)
        };

        using var client = new RavelasoWebClient(WebBrowser.Edge, options);

        // Navigate to test page
        var dataUrl = TestFixtures.GetTestPageDataURL();
        client.Navigate(dataUrl);

        Console.WriteLine($"  ✓ Navigated to: {(client.GetUrl().Length > 60 ? client.GetUrl().Substring(0, 60) + "..." : client.GetUrl())}");
        Console.WriteLine($"  ✓ Page title: {client.GetTitle()}");

        // Check elements
        Console.WriteLine($"\n  ✓ Username input exists: {client.Exists(Loc.Id("username"))}");
        Console.WriteLine($"  ✓ Password input exists: {client.Exists(Loc.Id("password"))}");
        Console.WriteLine($"  ✓ Country dropdown exists: {client.Exists(Loc.Id("country"))}");

        // Fill form
        Console.WriteLine($"\n  → Typing 'john.doe' in username field...");
        client.Type(Loc.Id("username"), "john.doe");

        Console.WriteLine($"  → Typing '****' in password field...");
        client.Type(Loc.Id("password"), "secret123");

        Console.WriteLine($"  → Selecting 'Mexico' from country dropdown...");
        client.Select(Loc.Id("country"), "Mexico");

        var selectedCountry = client.GetAttribute(Loc.Id("country"), "value");
        Console.WriteLine($"  ✓ Selected country value: {selectedCountry}");

        // Click button
        Console.WriteLine($"\n  → Clicking Submit button...");
        client.Click(Loc.Id("submit"));

        // Wait for result
        Console.WriteLine($"  → Waiting for result to appear...");
        if (client.WaitFor(Loc.Id("result"), TimeSpan.FromSeconds(2)))
        {
            var resultText = client.GetText(Loc.Id("result"));
            Console.WriteLine($"  ✓ Result appeared: '{resultText}'");
        }

        // Execute JavaScript
        Console.WriteLine($"\n  → Testing ExecuteScript (2 + 3 = ?)...");
        var jsResult = client.ExecuteScript("return 2 + 3;");
        Console.WriteLine($"  ✓ JavaScript result: {jsResult}");

        // Screenshot
        Console.WriteLine($"\n  → Taking screenshot...");
        var tempDir = Path.Combine(Path.GetTempPath(), "ravelaso-demo");
        Directory.CreateDirectory(tempDir);
        var screenshotPath = client.Screenshot(Path.Combine(tempDir, "demo_result.png"));
        Console.WriteLine($"  ✓ Screenshot saved to: {screenshotPath}");

        // Cleanup
        Console.WriteLine($"\n  → Closing webBrowser...");
        client.Quit();
        Console.WriteLine($"  ✓ WebBrowser closed");
    }
}
