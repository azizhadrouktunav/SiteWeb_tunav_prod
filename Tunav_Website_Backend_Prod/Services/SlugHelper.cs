using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace tunav_backend.Services;

public static class SlugHelper
{
    private static readonly Regex NonAlphaNumericRegex = new("[^a-z0-9]+", RegexOptions.Compiled);

    public static string GenerateSlug(string? value)
    {
        var normalized = RemoveDiacritics(value ?? string.Empty)
            .ToLowerInvariant()
            .Trim();

        normalized = NonAlphaNumericRegex.Replace(normalized, "-").Trim('-');

        if (string.IsNullOrWhiteSpace(normalized))
        {
            return "solution";
        }

        return normalized.Length <= 120 ? normalized : normalized[..120].Trim('-');
    }

    private static string RemoveDiacritics(string value)
    {
        var normalized = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);

        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(character);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }
}
