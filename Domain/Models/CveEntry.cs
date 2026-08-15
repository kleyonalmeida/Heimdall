namespace Heimdall.Domain.Models;

/// <summary>
/// Informações de uma vulnerabilidade CVE oriunda da API NVD.
/// </summary>
public record CveEntry(
    string CveId,
    string Description,
    double? CvssScore,
    string? CvssSeverity,
    string? PublishedDate,
    IReadOnlyList<string> References
)
{
    public bool HasPublicExploitHint => References.Any(r => r.Contains("exploit", StringComparison.OrdinalIgnoreCase));
}
