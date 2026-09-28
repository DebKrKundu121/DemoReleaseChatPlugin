namespace DemoReleaseChatPlugin.RazorComponents;

/// <summary>
/// Release notes are typed into a textarea. This turns that free text into list items.
/// </summary>
public static class ReleaseNoteLines
{
    private static readonly char[] BulletGlyphs = ['-', '*', '•', '·', '–', '—'];

    public static IReadOnlyList<string> From(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return [];

        return text
            .Replace("\r\n", "\n")
            .Replace("\r", "\n")
            .Split('\n')
            .Select(Clean)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList()!;
    }

    private static string? Clean(string line)
    {
        var trimmed = line.Trim();

        if (trimmed.Length == 0) return null;

        trimmed = StripBullet(trimmed);
        trimmed = StripNumbering(trimmed);

        // Drop lines that are entirely punctuation.
        return trimmed.Any(char.IsLetterOrDigit) ? trimmed : null;
    }

    /// Strips a leading bullet glyph ONLY when it is followed by whitespace - which is what stops
    /// "-3 degrees" losing its first character.
    private static string StripBullet(string value)
    {
        if (value.Length < 2) return value;
        if (!BulletGlyphs.Contains(value[0])) return value;
        if (!char.IsWhiteSpace(value[1])) return value;

        return value[1..].TrimStart();
    }

    /// Strips a leading "1." or "1)" ONLY when followed by whitespace - which is what stops
    /// "2.1.0 released" losing its first characters.
    private static string StripNumbering(string value)
    {
        var index = 0;

        while (index < value.Length && char.IsDigit(value[index]))
            index++;

        if (index == 0 || index >= value.Length) return value;
        if (value[index] is not ('.' or ')')) return value;

        var after = index + 1;

        if (after >= value.Length || !char.IsWhiteSpace(value[after])) return value;

        return value[after..].TrimStart();
    }
}
