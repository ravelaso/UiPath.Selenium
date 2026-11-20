using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using OpenQA.Selenium;
using Ravelaso.UiPath.Selenium.Core;

namespace Ravelaso.UiPath.Selenium.Mcp;

internal static class McpHelpers
{
    public static string ToJson(object value) => JsonSerializer.Serialize(value, new JsonSerializerOptions { WriteIndented = true });

    private static readonly string[] TestingAttrs = new[]
    {
        "data-testid", "data-test-id", "data-t-id", "data-tid", "data-cy", "data-qa", "data-test",
        "data-automation-id", "data-e2e", "data-qa-id", "data-test-selector", "aria-label",
    };

    public static Dictionary<string, object?> DescribeElement(IWebDriver driver, IWebElement el)
    {
        var attrs = GetAllAttributes(driver, el);

        string? text = null;
        try { text = el.Text; } catch { text = null; }
        text = text?.Trim();

        var suggestions = SuggestLocators(driver, el);

        var best = suggestions.FirstOrDefault(s => s.Unique) ?? suggestions.FirstOrDefault();

        var info = new Dictionary<string, object?>
        {
            ["tag"] = el.TagName,
            ["text"] = Truncate(text, 200),
            ["attributes"] = attrs,
            ["displayed"] = SafeBool(() => el.Displayed),
            ["enabled"] = SafeBool(() => el.Enabled),
            ["suggestedLocator"] = best?.Expression ?? string.Empty,
            ["suggestedLocators"] = suggestions,
        };

        try { info["generatedCss"] = GetCssPath(driver, el); } catch { info["generatedCss"] = null; }

        try
        {
            var html = (string)((IJavaScriptExecutor)driver).ExecuteScript("return arguments[0].outerHTML;", el);
            info["outerHtml"] = Truncate(html, 1500);
        }
        catch { info["outerHtml"] = null; }

        return info;
    }

    public static List<LocatorSuggestion> SuggestLocators(IWebDriver driver, IWebElement el)
    {
        var tag = el.TagName;
        var results = new List<(string strategy, string expression, Func<By> byFactory)>();

        // 1. id
        var id = SafeAttr(el, "id");
        if (!string.IsNullOrWhiteSpace(id))
            results.Add(("Id", "Loc.Id(\"" + Escape(id) + "\")", () => By.Id(id)));

        // 2. name
        var name = SafeAttr(el, "name");
        if (!string.IsNullOrWhiteSpace(name))
            results.Add(("Name", "Loc.Name(\"" + Escape(name) + "\")", () => By.Name(name)));

        // 3. testing-oriented attributes
        foreach (var attr in TestingAttrs)
        {
            var v = SafeAttr(el, attr);
            if (string.IsNullOrWhiteSpace(v)) continue;
            results.Add(("Attr", "Loc.Attr(\"" + attr + "\", \"" + Escape(v) + "\")",
                () => By.CssSelector("[" + attr + "=\"" + EscapeCss(v) + "\"]")));
        }

        // 4. aria-label
        var aria = SafeAttr(el, "aria-label");
        if (!string.IsNullOrWhiteSpace(aria))
            results.Add(("Attr", "Loc.Attr(\"aria-label\", \"" + Escape(aria) + "\")",
                () => By.CssSelector("[aria-label=\"" + EscapeCss(aria) + "\"]")));

        // 5. visible text for clickable elements
        string? text = null;
        try { text = el.Text; } catch { text = null; }
        text = text?.Trim();
        bool clickable = tag.Equals("button", StringComparison.OrdinalIgnoreCase)
            || tag.Equals("a", StringComparison.OrdinalIgnoreCase)
            || string.Equals(SafeAttr(el, "role"), "button", StringComparison.OrdinalIgnoreCase)
            || string.Equals(SafeAttr(el, "type"), "submit", StringComparison.OrdinalIgnoreCase)
            || string.Equals(SafeAttr(el, "type"), "button", StringComparison.OrdinalIgnoreCase);
        if (clickable && !string.IsNullOrWhiteSpace(text))
            results.Add(("Text", "Loc.Tag(\"" + tag + "\").Text(\"" + Escape(text) + "\")",
                () => By.XPath("//" + tag + "[contains(normalize-space(text()), '" + EscapeXPath(text) + "')]")));

        // 6. class combo (tag + all classes)
        var cls = SafeAttr(el, "class");
        if (!string.IsNullOrWhiteSpace(cls))
        {
            var tokens = cls.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Distinct().ToList();
            if (tokens.Count > 0)
            {
                var expr = "Loc.Tag(\"" + tag + "\").Class(\"" + Escape(string.Join(" ", tokens)) + "\")";
                var cssSel = tag + string.Concat(tokens.Select(t => "." + CssIdent(t)));
                results.Add(("Class", expr, () => By.CssSelector(cssSel)));
            }
        }

        // 7. text for non-clickable elements with short text
        if (!clickable && !string.IsNullOrWhiteSpace(text) && text!.Length <= 60)
            results.Add(("Text", "Loc.Tag(\"" + tag + "\").Text(\"" + Escape(text) + "\")",
                () => By.XPath("//" + tag + "[contains(normalize-space(text()), '" + EscapeXPath(text) + "')]")));

        // Dedupe by expression, preserving priority order.
        var seen = new HashSet<string>();
        var uniqueCandidates = new List<(string strategy, string expression, Func<By> byFactory)>();
        foreach (var c in results)
        {
            if (seen.Add(c.expression)) uniqueCandidates.Add(c);
        }

        var suggestions = new List<LocatorSuggestion>();
        foreach (var c in uniqueCandidates.Take(6))
        {
            int count = -1;
            try { count = driver.FindElements(c.byFactory()).Count; }
            catch { count = -1; }
            suggestions.Add(new LocatorSuggestion(c.strategy, c.expression, count, count == 1));
        }

        if (suggestions.Count == 0 || suggestions.All(s => !s.Unique))
        {
            try
            {
                var css = GetCssPath(driver, el);
                if (!string.IsNullOrWhiteSpace(css))
                {
                    int c = -1;
                    try { c = driver.FindElements(By.CssSelector(css)).Count; } catch { c = -1; }
                    if (c >= 1)
                        suggestions.Add(new LocatorSuggestion("CssPath", "Loc.Css(\"" + Escape(css) + "\")", c, c == 1));
                }
            }
            catch { }
        }

        var verified = suggestions.Where(s => s.MatchCount >= 1).ToList();
        if (verified.Count == 0) verified = suggestions; // keep unverified as last resort if nothing matched

        var ordered = verified
            .OrderByDescending(s => s.Unique)
            .ThenBy(s => s.MatchCount < 0 ? int.MaxValue : s.MatchCount)
            .ToList();
        return ordered;
    }

    private static Dictionary<string, string?> GetAllAttributes(IWebDriver driver, IWebElement el)
    {
        var result = new Dictionary<string, string?>();
        try
        {
            var js = (IJavaScriptExecutor)driver;
            var obj = js.ExecuteScript(
                "var o={}; var a=arguments[0].attributes; for(var i=0;i<a.length;i++) o[a[i].name]=a[i].value; return o;",
                el);
            if (obj is Dictionary<string, object> d)
            {
                foreach (var kv in d)
                    result[kv.Key] = kv.Value?.ToString();
            }
        }
        catch { }

        if (result.Count == 0)
        {
            foreach (var a in new[] { "id", "name", "class", "type", "value", "placeholder", "href", "data-testid", "data-test-id", "aria-label", "title", "role" })
            {
                try { result[a] = el.GetAttribute(a); } catch { result[a] = null; }
            }
        }
        return result;
    }

    private static string GetCssPath(IWebDriver driver, IWebElement el)
    {
        var js = (IJavaScriptExecutor)driver;
        var r = js.ExecuteScript(@"
            function getCssPath(el){
              if (el.id) return '#' + CSS.escape(el.id);
              const path=[];
              let node=el;
              while (node && node.nodeType===1){
                let sel=node.nodeName.toLowerCase();
                if (node.id){ sel='#'+CSS.escape(node.id); path.unshift(sel); break; }
                let sib=node, nth=1;
                while (sib=sib.previousElementSibling){ if (sib.nodeName.toLowerCase()===sel) nth++; }
                if (nth>1) sel += ':nth-of-type('+nth+')';
                if (node.className && typeof node.className==='string'){
                  const cls=node.className.trim().split(/\s+/).filter(Boolean);
                  if (cls.length) sel += '.' + cls.map(c=>CSS.escape(c)).join('.');
                }
                path.unshift(sel);
                node=node.parentElement;
              }
              return path.join(' > ');
            }
            return getCssPath(arguments[0]);
        ", el);
        return r?.ToString() ?? string.Empty;
    }

    /// <summary>
    /// Searches the light DOM and then recursively through all open Shadow DOM roots
    /// for elements matching <paramref name="by"/>. Returns all matches.
    /// </summary>
    public static List<IWebElement> FindElementsDeep(IWebDriver driver, By by)
    {
        var results = new List<IWebElement>();
        var seen = new HashSet<IWebElement>(new ReferenceComparer());
        try
        {
            foreach (var el in driver.FindElements(by))
            {
                if (seen.Add(el)) results.Add(el);
            }
        }
        catch { /* ignore */ }

        try
        {
            foreach (var el in SearchShadowRootsDeep(driver, by))
            {
                if (seen.Add(el)) results.Add(el);
            }
        }
        catch { /* ignore */ }

        return results;
    }

    private static List<IWebElement> SearchShadowRootsDeep(ISearchContext context, By by)
    {
        var found = new List<IWebElement>();
        var seen = new HashSet<IWebElement>(new ReferenceComparer());

        // Try direct match in this context first (already done by caller for root; here for shadow roots)
        try
        {
            foreach (var el in context.FindElements(by))
            {
                if (seen.Add(el)) found.Add(el);
            }
        }
        catch { /* ignore */ }

        // Descend into shadow roots
        IEnumerable<IWebElement> children;
        try { children = context.FindElements(By.CssSelector("*")); }
        catch { children = Array.Empty<IWebElement>(); }

        foreach (var child in children)
        {
            ISearchContext? shadowRoot = null;
            try { shadowRoot = child.GetShadowRoot(); }
            catch (NoSuchShadowRootException) { }
            catch (StaleElementReferenceException) { }
            catch (WebDriverException) { }

            if (shadowRoot == null) continue;

            foreach (var el in SearchShadowRootsDeep(shadowRoot, by))
            {
                if (seen.Add(el)) found.Add(el);
            }
        }

        return found;
    }

    private sealed class ReferenceComparer : IEqualityComparer<IWebElement>
    {
        public bool Equals(IWebElement? x, IWebElement? y) => ReferenceEquals(x, y);
        public int GetHashCode(IWebElement obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
    }

    public static string SafeAttr(IWebElement el, string name)
    {
        try { return el.GetAttribute(name) ?? string.Empty; } catch { return string.Empty; }
    }

    private static bool SafeBool(Func<bool> f)
    {
        try { return f(); } catch { return false; }
    }

    public static string Truncate(string? s, int max)
    {
        if (string.IsNullOrEmpty(s)) return string.Empty;
        return s.Length <= max ? s : s.Substring(0, max) + "...";
    }

    private static string Escape(string s) => s.Replace("\\", "\\\\").Replace("\"", "\\\"");

    private static string EscapeCss(string v) => v.Replace("\\", "\\\\").Replace("\"", "\\\"");

    private static string EscapeXPath(string v) => v.Replace("'", "\\'");

    private static string CssIdent(string v) => v;
}

public record LocatorSuggestion(string Strategy, string Expression, int MatchCount, bool Unique);
public record ElementHandle(string Strategy, string Value);