using Heimdall.Abstractions;
using Heimdall.Application.Interfaces;
using Heimdall.Domain.Models;
using Heimdall.Domain.Services;

namespace Heimdall.Application.Queries.CorrelateRisk;

public class CorrelateRiskHandler : IQueryHandler<CorrelateRiskQuery, CorrelateRiskResult>
{
    private readonly INvdApiClient _nvdApiClient;
    private readonly IEpssApiClient _epssApiClient;
    private readonly Action<string, int, int>? _reportProgress;

    public CorrelateRiskHandler(
        INvdApiClient nvdApiClient,
        IEpssApiClient epssApiClient,
        Action<string, int, int>? reportProgress = null)
    {
        _nvdApiClient = nvdApiClient;
        _epssApiClient = epssApiClient;
        _reportProgress = reportProgress;
    }

    public async Task<CorrelateRiskResult> HandleAsync(CorrelateRiskQuery query, CancellationToken cancellationToken = default)
    {
        var allFindings = new List<Finding>();
        var errors = new List<string>();
        var allCves = new List<CveEntry>();
        var pending = new List<(Component Component, CveEntry Cve)>();

        foreach (var component in query.Components)
        {
            IReadOnlyList<CveEntry> cves;
            try
            {
                cves = await _nvdApiClient.SearchByKeywordAsync(component.Name, 50, cancellationToken);
            }
            catch (Exception ex)
            {
                errors.Add($"Falha ao consultar NVD para '{component.Name}': {ex.Message}");
                continue;
            }

            var filteredCount = 0;
            foreach (var cve in cves)
            {
                if (query.StrictVersionFilter && !VersionMatcher.IsVersionMentioned(component.Version, cve.Description))
                {
                    continue;
                }

                pending.Add((component, cve));
                allCves.Add(cve);
                filteredCount++;
            }

            _reportProgress?.Invoke(component.Name, cves.Count, filteredCount);
        }

        var epssMap = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        if (allCves.Count > 0)
        {
            try
            {
                var cveIds = allCves.Select(c => c.CveId).Distinct().ToList();
                var scores = await _epssApiClient.GetEpssScoresAsync(cveIds, cancellationToken);
                foreach (var (k, v) in scores)
                {
                    epssMap[k] = v;
                }
            }
            catch (Exception)
            {
                errors.Add("Não foi possível obter scores EPSS (seguindo sem eles).");
            }
        }

        foreach (var (component, cve) in pending)
        {
            var epss = epssMap.TryGetValue(cve.CveId, out var e) ? (double?)e : null;
            var risk = RiskCalculator.ComputeRiskScore(cve, epss);
            allFindings.Add(new Finding(component, cve, epss, risk));
        }

        allFindings.Sort((a, b) => b.RiskScore.CompareTo(a.RiskScore)); // Order descending by risk

        return new CorrelateRiskResult(allFindings, errors);
    }
}
