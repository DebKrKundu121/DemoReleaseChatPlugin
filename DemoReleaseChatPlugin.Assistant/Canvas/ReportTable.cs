using System.Collections;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace DemoReleaseChatPlugin.Assistant.Canvas;

/// A display-ready table: headers people can read and every cell already formatted as text.
///
/// Built from a tool's own result rows (see From), so what the workspace renders is exactly what the
/// system returned — the model never supplies these values.
public sealed class ReportTable
{
    public required IReadOnlyList<string> Columns { get; init; }
    public required IReadOnlyList<IReadOnlyList<string>> Rows { get; init; }

    /// Turns a tool's result rows (usually anonymous objects) into a table.
    ///
    /// Columns appear in the order first seen across all rows. Nested arrays and objects are skipped,
    /// as are hiddenFields (case-insensitive) — e.g. long prose the grid shouldn't show.
    public static ReportTable From(IEnumerable items, params string[] hiddenFields)
    {
        var hidden = new HashSet<string>(hiddenFields ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);

        // Serialise once; each row becomes a JSON object we can read property by property.
        var elements = new List<JsonElement>();

        foreach (var item in items ?? Array.Empty<object>())
        {
            if (item is null)
            {
                continue;
            }

            var element = JsonSerializer.SerializeToElement(item, item.GetType());

            if (element.ValueKind == JsonValueKind.Object)
            {
                elements.Add(element);
            }
        }

        // First pass: the column set, in first-seen order.
        var names = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var element in elements)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (hidden.Contains(property.Name)
                    || property.Value.ValueKind is JsonValueKind.Object or JsonValueKind.Array
                    || !seen.Add(property.Name))
                {
                    continue;
                }

                names.Add(property.Name);
            }
        }

        // Second pass: one formatted cell per column; a row missing a property gets an empty cell.
        var rows = elements
            .Select(element => (IReadOnlyList<string>)names
                .Select(name => element.TryGetProperty(name, out var value) ? Format(value) : string.Empty)
                .ToList())
            .ToList();

        return new ReportTable
        {
            Columns = names.Select(Humanise).ToList(),
            Rows = rows
        };
    }

    /// leadDeveloper → Lead Developer, isRolledBack → Is Rolled Back.
    private static string Humanise(string name)
    {
        var text = new StringBuilder(name.Length + 4);

        for (var i = 0; i < name.Length; i++)
        {
            var c = name[i];

            if (i == 0)
            {
                text.Append(char.ToUpperInvariant(c));
            }
            else if (char.IsUpper(c) && (char.IsLower(name[i - 1]) || char.IsDigit(name[i - 1])))
            {
                text.Append(' ').Append(c);
            }
            else
            {
                text.Append(c);
            }
        }

        return text.ToString();
    }

    private static string Format(JsonElement value)
    {
        switch (value.ValueKind)
        {
            case JsonValueKind.String:
                var text = value.GetString() ?? string.Empty;

                // Tools send dates as yyyy-MM-dd (unambiguous for the model); people read dd/MM/yyyy.
                return text.Length == 10
                       && DateTime.TryParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
                    ? date.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)
                    : text;

            case JsonValueKind.Number:
                return value.TryGetInt64(out var whole)
                    ? whole.ToString(CultureInfo.InvariantCulture)
                    : value.GetDouble().ToString("F2", CultureInfo.InvariantCulture);

            case JsonValueKind.True:
                return "Yes";

            case JsonValueKind.False:
                return "No";

            default:
                return string.Empty;        // null, or a nested value in a column another row made scalar
        }
    }
}
