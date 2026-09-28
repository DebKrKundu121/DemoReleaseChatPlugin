namespace DemoReleaseChatPlugin.Domain.Releases;

/// <summary>
/// Best-effort numeric comparison of free-form version strings, which REFUSES TO ANSWER when it
/// cannot be sure. Every ambiguity resolves to "allow" - this is a guard rail against typos, not a
/// policy.
///
/// v2.1 vs 2.1.0        -> equal (leading v stripped, missing segments treated as 0)
/// 2.1.0-rc1 vs 2.1.0   -> not comparable (labelled and numerically equal), so the rule stays silent
/// hotfix-friday vs any -> not comparable
/// </summary>
public static class ReleaseVersion
{
    /// Returns false when either side isn't confidently comparable - the caller must then not enforce.
    public static bool TryCompare(string left, string right, out int comparison)
    {
        comparison = 0;

        if (!TryParse(left, out var leftParts, out var leftLabelled) ||
            !TryParse(right, out var rightParts, out var rightLabelled))
            return false;

        var length = Math.Max(leftParts.Count, rightParts.Count);

        for (var index = 0; index < length; index++)
        {
            var leftPart = index < leftParts.Count ? leftParts[index] : 0;
            var rightPart = index < rightParts.Count ? rightParts[index] : 0;

            if (leftPart == rightPart) continue;

            comparison = leftPart < rightPart ? -1 : 1;
            return true;
        }

        // Numerically equal: only report equality when neither side carries a pre-release label.
        return !leftLabelled && !rightLabelled;
    }

    private static bool TryParse(string version, out List<int> parts, out bool labelled)
    {
        parts = [];
        labelled = false;

        if (string.IsNullOrWhiteSpace(version)) return false;

        var numeric = version.Trim();

        if (numeric.StartsWith('v') || numeric.StartsWith('V')) numeric = numeric[1..];

        var label = numeric.IndexOfAny(['-', '+', ' ', '_']);

        if (label >= 0)
        {
            numeric = numeric[..label];
            labelled = true;
        }

        if (numeric.Length == 0) return false;

        foreach (var segment in numeric.Split('.'))
        {
            if (!int.TryParse(segment, out var value) || value < 0)
            {
                parts = [];
                return false;
            }

            parts.Add(value);
        }

        return parts.Count > 0;
    }
}
