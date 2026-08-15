namespace Heimdall.Domain.Models;

/// <summary>
/// Resultado da correlação entre um componente instalado e uma vulnerabilidade identificada.
/// </summary>
public record Finding(
    Component Component,
    CveEntry Cve,
    double? EpssScore,
    double RiskScore
)
{
    public bool HasPublicExploit => Cve.HasPublicExploitHint;
}
