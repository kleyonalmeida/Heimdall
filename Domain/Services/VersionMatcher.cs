using System.Text.RegularExpressions;

namespace Heimdall.Domain.Services;

public static partial class VersionMatcher
{
    /// <summary>
    /// Verifica se a versão instalada (ou seus prefixos numéricos major.minor / major.minor.patch)
    /// é mencionada no texto descritivo da CVE.
    /// Retorna true se a versão for nula ou vazia.
    /// </summary>
    public static bool IsVersionMentioned(string? version, string description)
    {
        if (string.IsNullOrWhiteSpace(version))
            return true;

        if (string.IsNullOrWhiteSpace(description))
            return false;

        var normalized = VersionNormalizer.NormalizeVersion(version);
        var numericParts = DigitRegex().Matches(normalized)
            .Select(m => m.Value)
            .ToList();

        var candidates = new HashSet<string>(StringComparer.Ordinal)
        {
            version,
            normalized
        };

        if (numericParts.Count >= 2)
        {
            candidates.Add($"{numericParts[0]}.{numericParts[1]}");
        }

        if (numericParts.Count >= 3)
        {
            candidates.Add($"{numericParts[0]}.{numericParts[1]}.{numericParts[2]}");
        }

        return candidates.Any(c => !string.IsNullOrEmpty(c) && description.Contains(c, StringComparison.Ordinal));
    }

    [GeneratedRegex(@"\d+")]
    private static partial Regex DigitRegex();
}
