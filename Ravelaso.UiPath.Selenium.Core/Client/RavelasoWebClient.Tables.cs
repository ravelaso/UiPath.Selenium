using OpenQA.Selenium;
using System.Data;
using System.Globalization;

// ReSharper disable CheckNamespace
namespace Ravelaso.UiPath.Selenium.Core;

public class TableColumn
{
    public string Name { get; }
    public Type Type { get; }
    public TableColumn(string name, Type type)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Type = type ?? throw new ArgumentNullException(nameof(type));
    }
}

public class TableExtractionOptions
{
    public bool HasHeader { get; set; } = false;
    public int HeaderRowIndex { get; set; } = 0;
    public List<TableColumn> Columns { get; } = new();
    public List<string>? ColumnNames { get; set; }
    public bool IncludeFooter { get; set; } = false;
    public CultureInfo Culture { get; set; } = CultureInfo.InvariantCulture;
    public bool Strict { get; set; } = false;

    public TableExtractionOptions WithHasHeader(bool value) { HasHeader = value; return this; }
    public TableExtractionOptions HeaderRow(int index) { HeaderRowIndex = index; return this; }
    public TableExtractionOptions Column(string name, Type type) { Columns.Add(new TableColumn(name, type)); return this; }
    public TableExtractionOptions Names(params string[] names) { ColumnNames = names.ToList(); return this; }
    public TableExtractionOptions WithCulture(CultureInfo culture) { Culture = culture; return this; }
    public TableExtractionOptions WithStrict(bool value) { Strict = value; return this; }
    public TableExtractionOptions WithIncludeFooter(bool value) { IncludeFooter = value; return this; }
}

public partial class RavelasoWebClient
{
    public DataTable ExtractTable(By locator, TableExtractionOptions? options = null, TimeSpan? beforeStepDelay = null, TimeSpan? timeout = null)
    {
        BeforeStep(beforeStepDelay);
        options ??= new TableExtractionOptions();
        var tableElement = WaitElement(locator, timeout);
        var rows = ExtractTableRows(tableElement, options.IncludeFooter);
        return BuildDataTable(null, rows, options);
    }

    public DataTable ExtractTable(By locator, DataTable targetTable, TableExtractionOptions? options = null, TimeSpan? beforeStepDelay = null, TimeSpan? timeout = null)
    {
        if (targetTable == null) throw new ArgumentNullException(nameof(targetTable));
        BeforeStep(beforeStepDelay);
        options ??= new TableExtractionOptions();
        var tableElement = WaitElement(locator, timeout);
        var rows = ExtractTableRows(tableElement, options.IncludeFooter);
        return BuildDataTable(targetTable, rows, options);
    }

    public DataTable ExtractTable(Locator locator, TableExtractionOptions? options = null, TimeSpan? beforeStepDelay = null, TimeSpan? timeout = null)
        => ExtractTable(locator.ToBy(), options, beforeStepDelay, timeout);

    public DataTable ExtractTable(Locator locator, DataTable targetTable, TableExtractionOptions? options = null, TimeSpan? beforeStepDelay = null, TimeSpan? timeout = null)
        => ExtractTable(locator.ToBy(), targetTable, options, beforeStepDelay, timeout);

    public DataTable ExtractTable(AnchorLocator anchor, TableExtractionOptions? options = null, TimeSpan? beforeStepDelay = null, TimeSpan? timeout = null)
    {
        BeforeStep(beforeStepDelay);
        options ??= new TableExtractionOptions();
        var resolved = anchor.Resolve(this);
        var tableElement = WaitElement(resolved, timeout);
        var rows = ExtractTableRows(tableElement, options.IncludeFooter);
        return BuildDataTable(null, rows, options);
    }

    public DataTable ExtractTable(AnchorLocator anchor, DataTable targetTable, TableExtractionOptions? options = null, TimeSpan? beforeStepDelay = null, TimeSpan? timeout = null)
    {
        if (targetTable == null) throw new ArgumentNullException(nameof(targetTable));
        BeforeStep(beforeStepDelay);
        options ??= new TableExtractionOptions();
        var resolved = anchor.Resolve(this);
        var tableElement = WaitElement(resolved, timeout);
        var rows = ExtractTableRows(tableElement, options.IncludeFooter);
        return BuildDataTable(targetTable, rows, options);
    }

    private List<string[]> ExtractTableRows(IWebElement tableElement, bool includeFooter)
    {
        var js = (IJavaScriptExecutor)_driver;
        var result = js.ExecuteScript(
            "var table = arguments[0];" +
            "var out = [];" +
            "function collect(sectionSelector) {" +
            "  var section = table.querySelector(sectionSelector);" +
            "  if (!section) return;" +
            "  var trs = section.querySelectorAll('tr');" +
            "  for (var i = 0; i < trs.length; i++) {" +
            "    var cells = trs[i].querySelectorAll('th,td');" +
            "    var row = [];" +
            "    for (var j = 0; j < cells.length; j++) row.push((cells[j].innerText || cells[j].textContent || '').trim());" +
            "    out.push(row);" +
            "  }" +
            "}" +
            "collect('thead');" +
            "collect('tbody');" +
            "if (arguments[1]) collect('tfoot');" +
            "if (out.length === 0) {" +
            "  var trs = table.querySelectorAll('tr');" +
            "  for (var i = 0; i < trs.length; i++) {" +
            "    var cells = trs[i].querySelectorAll('th,td');" +
            "    var row = [];" +
            "    for (var j = 0; j < cells.length; j++) row.push((cells[j].innerText || cells[j].textContent || '').trim());" +
            "    out.push(row);" +
            "  }" +
            "}" +
            "return out;",
            tableElement, includeFooter);

        var rows = new List<string[]>();
        if (result is System.Collections.IEnumerable enumerable)
        {
            foreach (var item in enumerable)
            {
                if (item is System.Collections.IEnumerable rowEnum && item is not string)
                {
                    var cells = new List<string>();
                    foreach (var c in rowEnum)
                        cells.Add(c?.ToString() ?? string.Empty);
                    rows.Add(cells.ToArray());
                }
            }
        }
        return rows;
    }

    private DataTable BuildDataTable(DataTable? target, List<string[]> rows, TableExtractionOptions options)
    {
        var table = target ?? new DataTable();

        // Determine schema if creating new table
        if (target == null)
        {
            List<string> names;
            List<Type> types;

            if (options.Columns.Count > 0)
            {
                names = options.Columns.Select(c => c.Name).ToList();
                types = options.Columns.Select(c => c.Type).ToList();
            }
            else if (options.ColumnNames != null && options.ColumnNames.Count > 0)
            {
                names = options.ColumnNames.ToList();
                types = Enumerable.Repeat(typeof(string), names.Count).ToList();
            }
            else if (options.HasHeader && rows.Count > 0)
            {
                var headerIndex = Math.Clamp(options.HeaderRowIndex, 0, rows.Count - 1);
                names = DeduplicateNames(rows[headerIndex]);
                types = names.Select((_, i) =>
                    (i < options.Columns.Count ? options.Columns[i].Type : typeof(string))).ToList();
            }
            else
            {
                var colCount = rows.Count > 0 ? rows.Max(r => r.Length) : 0;
                names = Enumerable.Range(1, colCount).Select(i => $"Column{i}").ToList();
                types = Enumerable.Range(0, colCount).Select(i =>
                    (i < options.Columns.Count ? options.Columns[i].Type : typeof(string))).ToList();
            }

            for (int i = 0; i < names.Count; i++)
            {
                var type = i < types.Count ? types[i] : typeof(string);
                if (!table.Columns.Contains(names[i]))
                    table.Columns.Add(names[i], type);
                else
                    table.Columns.Add(names[i] + "_" + (i + 1), type);
            }
        }

        var columnCount = table.Columns.Count;
        var culture = options.Culture ?? CultureInfo.InvariantCulture;

        // Determine data rows
        var dataRows = new List<string[]>(rows);
        if (options.HasHeader && dataRows.Count > 0)
        {
            var headerIndex = Math.Clamp(options.HeaderRowIndex, 0, dataRows.Count - 1);
            dataRows.RemoveAt(headerIndex);
        }

        foreach (var row in dataRows)
        {
            if (row.Length == 0 || row.All(string.IsNullOrWhiteSpace)) continue;

            var values = new object[columnCount];
            for (int i = 0; i < columnCount; i++)
            {
                var raw = i < row.Length ? row[i] : string.Empty;
                if (string.IsNullOrWhiteSpace(raw))
                {
                    values[i] = DBNull.Value;
                    continue;
                }

                var type = table.Columns[i].DataType;
                try
                {
                    values[i] = ConvertValue(raw, type, culture);
                }
                catch
                {
                    if (options.Strict)
                        throw;
                    values[i] = DBNull.Value;
                }
            }
            table.Rows.Add(values);
        }

        return table;
    }

    private static List<string> DeduplicateNames(string[] header)
    {
        var result = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var h in header)
        {
            var name = string.IsNullOrWhiteSpace(h) ? "Column" : h.Trim();
            if (!seen.Add(name))
            {
                int n = 2;
                while (seen.Contains(name + "_" + n)) n++;
                name = name + "_" + n;
                seen.Add(name);
            }
            result.Add(name);
        }
        return result;
    }

    private static object ConvertValue(string raw, Type type, CultureInfo culture)
    {
        if (type == typeof(string)) return raw;
        if (type == typeof(DateTime)) return DateTime.Parse(raw, culture, DateTimeStyles.None);
        if (type == typeof(DateTimeOffset)) return DateTimeOffset.Parse(raw, culture);
        if (type == typeof(TimeSpan)) return TimeSpan.Parse(raw, culture);
        if (type == typeof(Guid)) return Guid.Parse(raw);
        if (type.IsEnum) return Enum.Parse(type, raw, true);
        if (type == typeof(bool))
        {
            if (bool.TryParse(raw, out var b)) return b;
            if (int.TryParse(raw, out var i)) return i != 0;
            throw new FormatException($"Cannot convert '{raw}' to bool.");
        }
        return Convert.ChangeType(raw, type, culture);
    }
}