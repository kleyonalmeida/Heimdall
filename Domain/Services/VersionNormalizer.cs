using System.Text.RegularExpressions;

namespace Heimdall.Domain.Services;

public static partial class VersionNormalizer
{
    /// <summary>
    /// Normaliza formatos de versão de distribuições Linux para o núcleo upstream.
    /// Ex: "1:10.0p1-7+deb13u4" -> "10.0p1"
    ///     "2.39-0ubuntu8.7"    -> "2.39"
    /// </summary>
    public static string NormalizeVersion(string version)
    {
        if (string.IsNullOrWhiteSpace(version))
            return string.Empty;

        // Remove epoch (ex: "1:10.0p1" -> "10.0p1")
        var colonIdx = version.IndexOf(':');
        if (colonIdx >= 0 && colonIdx < version.Length - 1)
        {
            version = version[(colonIdx + 1)..];
        }

        // Remove sufixo de revisão de pacote ou build (ex: "-7+deb13u4" ou "+deb13u4")
        var hyphenIdx = version.IndexOf('-');
        if (hyphenIdx >= 0)
        {
            version = version[..hyphenIdx];
        }

        var plusIdx = version.IndexOf('+');
        if (plusIdx >= 0)
        {
            version = version[..plusIdx];
        }

        return version;
    }

    /// <summary>
    /// Extrai o primeiro padrão de versão semântica de um texto genérico.
    /// Ex: "sudo 1.9.15" -> "1.9.15"
    /// </summary>
    public static string? ExtractVersion(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        var match = SemanticVersionRegex().Match(text);
        return match.Success ? match.Groups[1].Value : null;
    }

    [GeneratedRegex(@"(\d+\.\d+(?:\.\d+){0,3}(?:-[\w.]+)?)")]
    private static partial Regex SemanticVersionRegex();
}
