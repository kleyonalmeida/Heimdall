using Heimdall.Domain.Models;
using Heimdall.Infrastructure.HttpClients;
using Moq;
using Moq.Protected;
using System.Net;
using Xunit;

namespace Heimdall.Tests;

public class NvdClientTests
{
    private static HttpResponseMessage MockResponse(int totalResults, string cveId)
    {
        var json = $$"""
        {
            "totalResults": {{totalResults}},
            "vulnerabilities": [
                {
                    "cve": {
                        "id": "{{cveId}}",
                        "descriptions": [{"lang": "en", "value": "desc for {{cveId}}"}],
                        "published": "2026-01-01T00:00:00"
                    }
                }
            ]
        }
        """;

        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json)
        };
    }

    [Fact]
    public async Task SearchByKeyword_SinglePage_MakesOneHttpRequest()
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(MockResponse(2, "CVE-2026-0001"))
            .Verifiable();

        var httpClient = new HttpClient(handlerMock.Object);
        var client = new NvdApiClient(httpClient, minDelaySeconds: 0);

        var results = await client.SearchByKeywordAsync("openssl", resultsPerPage: 50);

        handlerMock.Protected().Verify(
            "SendAsync",
            Times.Exactly(1),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>()
        );
        Assert.Single(results);
    }

    [Fact]
    public async Task SearchByKeyword_MultiplePages_FetchesMostRecentPage()
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        
        handlerMock.Protected()
            .SetupSequence<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(MockResponse(500, "CVE-1999-0001")) // First page
            .ReturnsAsync(MockResponse(500, "CVE-2026-9999")); // Most recent page

        var httpClient = new HttpClient(handlerMock.Object);
        var client = new NvdApiClient(httpClient, minDelaySeconds: 0);

        var results = await client.SearchByKeywordAsync("bash", resultsPerPage: 50);

        handlerMock.Protected().Verify(
            "SendAsync",
            Times.Exactly(2),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>()
        );
        Assert.Single(results);
        Assert.Equal("CVE-2026-9999", results[0].CveId);
    }

    [Fact]
    public async Task SearchByKeyword_SendsOnlyKeywordNameInParams()
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        HttpRequestMessage? sentRequest = null;

        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .Callback<HttpRequestMessage, CancellationToken>((r, _) => sentRequest = r)
            .ReturnsAsync(MockResponse(0, ""));

        var httpClient = new HttpClient(handlerMock.Object);
        var client = new NvdApiClient(httpClient, minDelaySeconds: 0);

        await client.SearchByKeywordAsync("sudo", resultsPerPage: 50);

        Assert.NotNull(sentRequest);
        var query = sentRequest.RequestUri?.Query ?? string.Empty;
        Assert.Contains("keywordSearch=sudo", query);
    }
}
