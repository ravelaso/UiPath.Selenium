using System;
using System.Collections.Generic;
using System.Text.Json;
using OpenQA.Selenium;
using Ravelaso.UiPath.Selenium.Core;

namespace Ravelaso.UiPath.Selenium.Mcp;

internal static class McpHelpers
{
    public static string ToJson(object value) => JsonSerializer.Serialize(value, new JsonSerializerOptions { WriteIndented = true });

    public static LocatorSpec SuggestLocator(IWebElement el)
    {
        // Prefer stable, unique-ish attributes.
        string? id = SafeAttr(el, "id");
        if (!string.IsNullOrWhiteSpace(id))
            return new LocatorSpec("Id", id!, $"Loc.Id(\"{Escape(id!)}\")");

        string? name = SafeAttr(el, "name");
        if (!string.IsNullOrWhiteSpace(name))
            return new LocatorSpec("Name", name!, $"Loc.Name(\"{Escape(name!)}\")");

        string? testId = SafeAttr(el, "data-testid") ?? SafeAttr(el, "data-test-id") ?? SafeAttr(el, "data-qa");
        if (!string.IsNullOrWhiteSpace(testId))
        {
            string attr = SafeAttr(el, "data-testid") is not null ? "data-testid"
                : (SafeAttr(el, "data-test-id") is not null ? "data-test-id" : "data-qa");
            return new LocatorSpec("Attr", testId!, $"Loc.Attr(\"{attr}\", \"{Escape(testId!)}\")");
        }

        // Fall back to tag + first meaningful class token.
        string tag = el.TagName;
        string? cls = SafeAttr(el, "class");
        if (!string.IsNullOrWhiteSpace(cls))
        {
            var first = cls!.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            if (first is not null)
                return new LocatorSpec("Class", first, $"Loc.Tag(\"{tag}\").Class(\"{Escape(first)}\")");
        }

        // Last resort: CSS by tag (not great, but usable).
        return new LocatorSpec("Tag", tag, $"Loc.Tag(\"{tag}\")");
    }

    public static Dictionary<string, object?> DescribeElement(IWebElement el)
    {
        var attrs = new Dictionary<string, string?>();
        foreach (var a in new[] { "id", "name", "class", "type", "value", "placeholder", "href", "data-testid", "data-test-id", "aria-label", "title", "role" })
        {
            try { attrs[a] = el.GetAttribute(a); } catch { attrs[a] = null; }
        }

        string? text = null;
        try { text = el.Text; } catch { text = null; }

        return new Dictionary<string, object?>
        {
            ["tag"] = el.TagName,
            ["text"] = Truncate(text, 200),
            ["attributes"] = attrs,
            ["displayed"] = SafeBool(() => el.Displayed),
            ["enabled"] = SafeBool(() => el.Enabled),
            ["suggestedLocator"] = SuggestLocator(el).Expression,
        };
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
        return s!.Length <= max ? s : s.Substring(0, max) + "...";
    }

    private static string Escape(string s) => s.Replace("\\", "\\\\").Replace("\"", "\\\"");

    public record LocatorSpec(string Strategy, string Value, string Expression);
}

public record ElementHandle(string Strategy, string Value);