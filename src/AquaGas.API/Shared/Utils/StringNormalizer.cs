using System.Globalization;
using System.Text;

namespace AquaGas.Api.Shared.Utils;

public static class StringNormalizer
{
    public static string Normalize(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        var normalized = input
            .Trim()
            .ToUpperInvariant()
            .Normalize(NormalizationForm.FormD);

        var chars = normalized
            .Where(c =>
                CharUnicodeInfo.GetUnicodeCategory(c)
                != UnicodeCategory.NonSpacingMark
            );

        var noAccents = string.Concat(chars);

        return string.Join(" ",
            noAccents
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
        );
    }
}