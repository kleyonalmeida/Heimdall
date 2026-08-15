using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Heimdall.Application.Interfaces;

namespace Heimdall.Infrastructure.HttpClients;

public class EpssApiClient : IEpssApiClient
{
    private const string BaseUrl = "https://api.first.org/data/v1/epss";
    private readonly HttpClient _httpClient;

    public EpssApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IReadOnlyDictionary<string, double>> GetEpssScoresAsync(IEnumerable<string> cveIds, CancellationToken cancellationToken = default)
    {
        var idList = cveIds.Distinct().ToList();
        if (idList.Count == 0)
        {
            return new Dictionary<string, double>();
        }

        var scores = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        const int batchSize = 100;

        for (var i = 0; i < idList.Count; i += batchSize)
        {
            var batch = idList.Skip(i).Take(batchSize);
            var queryCves = string.Join(',', batch);
            var url = $"{BaseUrl}?cve={Uri.EscapeDataString(queryCves)}";

            try
            {
                var response = await _httpClient.GetAsync(url, cancellationToken);
                response.EnsureSuccessStatusCode();

                var data = await response.Content.ReadFromJsonAsync<EpssResponse>(EpssJsonSerializerContext.Default.EpssResponse, cancellationToken);
                if (data?.Data != null)
                {
                    foreach (var item in data.Data)
                    {
                        if (!string.IsNullOrEmpty(item.Cve) && double.TryParse(item.Epss, System.Globalization.CultureInfo.InvariantCulture, out var score))
                        {
                            scores[item.Cve] = score;
                        }
                    }
                }
            }
            catch
            {
                // Fail gracefully if EPSS query fails for a batch
            }
        }

        return scores;
    }
}

public class EpssResponse
{
    [JsonPropertyName("data")]
    public List<EpssDataItem>? Data { get; set; }
}

public class EpssDataItem
{
    [JsonPropertyName("cve")]
    public string? Cve { get; set; }

    [JsonPropertyName("epss")]
    public string? Epss { get; set; }
}

[JsonSerializable(typeof(EpssResponse))]
[JsonSerializable(typeof(EpssDataItem))]
[JsonSerializable(typeof(List<EpssDataItem>))]
public partial class EpssJsonSerializerContext : JsonSerializerContext
{
}
