namespace tunav_backend.Services;

public static class PackPresentationCatalog
{
    public const string DefaultSolutionIconKey = "map-pin";
    public const string DefaultSolutionThemeKey = "blue-cyan";
    public const string DefaultPackThemeKey = "green";

    private static readonly HashSet<string> SolutionIconKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "map-pin",
        "zap",
        "building",
        "tag",
        "droplet",
        "camera",
    };

    private static readonly HashSet<string> SolutionThemeKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "blue-cyan",
        "yellow-orange",
        "teal-green",
        "pink-rose",
        "sky-cyan",
        "red-pink",
    };

    private static readonly HashSet<string> PackThemeKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "green",
        "orange",
        "rose",
    };

    public static IReadOnlyCollection<string> AllowedSolutionIconKeys => SolutionIconKeys;
    public static IReadOnlyCollection<string> AllowedSolutionThemeKeys => SolutionThemeKeys;
    public static IReadOnlyCollection<string> AllowedPackThemeKeys => PackThemeKeys;

    public static string NormalizeSolutionIconKey(string? value)
    {
        var normalized = (value ?? string.Empty).Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return DefaultSolutionIconKey;
        }

        if (!SolutionIconKeys.Contains(normalized))
        {
            throw new InvalidOperationException($"PackIconKey invalide : '{value}'.");
        }

        return normalized;
    }

    public static string NormalizeSolutionThemeKey(string? value)
    {
        var normalized = (value ?? string.Empty).Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return DefaultSolutionThemeKey;
        }

        if (!SolutionThemeKeys.Contains(normalized))
        {
            throw new InvalidOperationException($"PackThemeKey invalide : '{value}'.");
        }

        return normalized;
    }

    public static string NormalizePackThemeKey(string? value)
    {
        var normalized = (value ?? string.Empty).Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return DefaultPackThemeKey;
        }

        if (!PackThemeKeys.Contains(normalized))
        {
            throw new InvalidOperationException($"ThemeKey invalide : '{value}'.");
        }

        return normalized;
    }
}
