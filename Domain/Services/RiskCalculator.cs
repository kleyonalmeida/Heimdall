using Heimdall.Domain.Models;

namespace Heimdall.Domain.Services;

public static class RiskCalculator
{
    /// <summary>
    /// Combina o CVSS (0-10) e EPSS (0-1) em um score de risco 0-100,
    /// aplicando um bônus de 10.0 se houver indício de exploit público.
    /// </summary>
    public static double ComputeRiskScore(CveEntry cve, double? epssScore)
    {
        var cvss = cve.CvssScore ?? 0.0;
        var epss = epssScore ?? 0.0;

        var baseScore = (cvss / 10.0) * 60.0 + (epss * 30.0);
        var bonus = cve.HasPublicExploitHint ? 10.0 : 0.0;

        var total = Math.Min(baseScore + bonus, 100.0);
        return Math.Round(total, 1);
    }
}
