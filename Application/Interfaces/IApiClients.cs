using Heimdall.Domain.Models;

namespace Heimdall.Application.Interfaces;

public interface INvdApiClient
{
    Task<IReadOnlyList<CveEntry>> SearchByKeywordAsync(string keyword, int resultsPerPage = 50, CancellationToken cancellationToken = default);
}

public interface IEpssApiClient
{
    Task<IReadOnlyDictionary<string, double>> GetEpssScoresAsync(IEnumerable<string> cveIds, CancellationToken cancellationToken = default);
}
