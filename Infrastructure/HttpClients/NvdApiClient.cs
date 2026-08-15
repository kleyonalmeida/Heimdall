using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Heimdall.Application.Interfaces;
using Heimdall.Domain.Models;

namespace Heimdall.Infrastructure.HttpClients;

public class NvdApiClient : INvdApiClient
{
    private const string BaseUrl = "https://services.nvd.nist.gov/rest/json/cves/2.0";
    private readonly HttpClient _httpClient;
    private readonly double _minDelaySeconds;
    private DateTime _lastRequestTime = DateTime.MinValue;

    public NvdApiClient(HttpClient httpClient, double minDelaySeconds = 6.0)
    {
        _httpClient = httpClient;
        _minDelaySeconds = minDelaySeconds;
    }

    public async Task<IReadOnlyList<CveEntry>> SearchByKeywordAsync(string keyword, int resultsPerPage = 50, CancellationToken cancellationToken = default)
    {
        var entries = new List<CveEntry>();
        var firstPage = await FetchPageAsync(keyword, resultsPerPage, startIndex: 0, cancellationToken);
        
        if (firstPage == null) return entries;

        var totalResults = firstPage.TotalResults;
        
        if (totalResults <= resultsPerPage)
        {
            entries.AddRange(MapVulnerabilities(firstPage.Vulnerabilities));
        }
        else
        {
            // Fetch most recent page
            var startIndex = Math.Max(0, totalResults - resultsPerPage);
            var recentPage = await FetchPageAsync(keyword, resultsPerPage, startIndex, cancellationToken);
            if (recentPage != null)
            {
                entries.AddRange(MapVulnerabilities(recentPage.Vulnerabilities));
            }
        }

        return entries;
    }

    private async Task<NvdResponse?> FetchPageAsync(string keyword, int resultsPerPage, int startIndex, CancellationToken cancellationToken)
    {
        await ThrottleAsync(cancellationToken);

        var url = $"{BaseUrl}?keywordSearch={Uri.EscapeDataString(keyword)}&resultsPerPage={resultsPerPage}&startIndex={startIndex}";
        var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<NvdResponse>(NvdJsonSerializerContext.Default.NvdResponse, cancellationToken);
    }

    private async Task ThrottleAsync(CancellationToken cancellationToken)
    {
        var elapsed = (DateTime.UtcNow - _lastRequestTime).TotalSeconds;
        if (elapsed < _minDelaySeconds)
        {
            var delay = TimeSpan.FromSeconds(_minDelaySeconds - elapsed);
            await Task.Delay(delay, cancellationToken);
        }
        _lastRequestTime = DateTime.UtcNow;
    }

    private static IEnumerable<CveEntry> MapVulnerabilities(List<NvdVulnerabilityItem>? items)
    {
        if (items == null) yield break;

        foreach (var item in items)
        {
            var cve = item.Cve;
            if (cve == null) continue;

            var desc = cve.Descriptions?.FirstOrDefault(d => d.Lang == "en")?.Value ?? "";
            var refs = cve.References?.Select(r => r.Url).Where(u => u != null).Cast<string>().ToList() ?? new List<string>();

            double? cvssScore = null;
            string? cvssSeverity = null;

            var metrics = cve.Metrics;
            if (metrics != null)
            {
                var cvssDataList = metrics.CvssMetricV31 ?? metrics.CvssMetricV30 ?? metrics.CvssMetricV2;
                var cvssData = cvssDataList?.FirstOrDefault()?.CvssData;
                if (cvssData != null)
                {
                    cvssScore = cvssData.BaseScore;
                    cvssSeverity = cvssData.BaseSeverity ?? cvssDataList?.FirstOrDefault()?.BaseSeverity;
                }
            }

            yield return new CveEntry(
                CveId: cve.Id ?? "UNKNOWN",
                Description: desc,
                CvssScore: cvssScore,
                CvssSeverity: cvssSeverity,
                PublishedDate: cve.Published,
                References: refs
            );
        }
    }
}

// JSON Models & AOT Context
public class NvdResponse
{
    [JsonPropertyName("totalResults")]
    public int TotalResults { get; set; }
    [JsonPropertyName("vulnerabilities")]
    public List<NvdVulnerabilityItem>? Vulnerabilities { get; set; }
}

public class NvdVulnerabilityItem
{
    [JsonPropertyName("cve")]
    public NvdCveNode? Cve { get; set; }
}

public class NvdCveNode
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }
    [JsonPropertyName("published")]
    public string? Published { get; set; }
    [JsonPropertyName("descriptions")]
    public List<NvdDescription>? Descriptions { get; set; }
    [JsonPropertyName("metrics")]
    public NvdMetrics? Metrics { get; set; }
    [JsonPropertyName("references")]
    public List<NvdReference>? References { get; set; }
}

public class NvdDescription
{
    [JsonPropertyName("lang")]
    public string? Lang { get; set; }
    [JsonPropertyName("value")]
    public string? Value { get; set; }
}

public class NvdMetrics
{
    [JsonPropertyName("cvssMetricV31")]
    public List<NvdCvssMetric>? CvssMetricV31 { get; set; }
    [JsonPropertyName("cvssMetricV30")]
    public List<NvdCvssMetric>? CvssMetricV30 { get; set; }
    [JsonPropertyName("cvssMetricV2")]
    public List<NvdCvssMetric>? CvssMetricV2 { get; set; }
}

public class NvdCvssMetric
{
    [JsonPropertyName("cvssData")]
    public NvdCvssData? CvssData { get; set; }
    [JsonPropertyName("baseSeverity")]
    public string? BaseSeverity { get; set; }
}

public class NvdCvssData
{
    [JsonPropertyName("baseScore")]
    public double? BaseScore { get; set; }
    [JsonPropertyName("baseSeverity")]
    public string? BaseSeverity { get; set; }
}

public class NvdReference
{
    [JsonPropertyName("url")]
    public string? Url { get; set; }
}

[JsonSerializable(typeof(NvdResponse))]
[JsonSerializable(typeof(NvdVulnerabilityItem))]
[JsonSerializable(typeof(NvdCveNode))]
[JsonSerializable(typeof(NvdDescription))]
[JsonSerializable(typeof(NvdMetrics))]
[JsonSerializable(typeof(NvdCvssMetric))]
[JsonSerializable(typeof(NvdCvssData))]
[JsonSerializable(typeof(NvdReference))]
[JsonSerializable(typeof(List<NvdVulnerabilityItem>))]
[JsonSerializable(typeof(List<NvdDescription>))]
[JsonSerializable(typeof(List<NvdCvssMetric>))]
[JsonSerializable(typeof(List<NvdReference>))]
public partial class NvdJsonSerializerContext : JsonSerializerContext
{
}
