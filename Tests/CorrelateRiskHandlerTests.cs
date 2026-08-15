using Heimdall.Application.Interfaces;
using Heimdall.Application.Queries.CorrelateRisk;
using Heimdall.Domain.Models;
using Moq;
using Xunit;

namespace Heimdall.Tests;

public class CorrelateRiskHandlerTests
{
    private static CveEntry MakeCve(string cveId, double cvss, string description)
    {
        return new CveEntry(
            CveId: cveId,
            Description: description,
            CvssScore: cvss,
            CvssSeverity: "HIGH",
            PublishedDate: "2026-01-01T00:00:00Z",
            References: Array.Empty<string>()
        );
    }

    [Fact]
    public async Task HandleAsync_FiltersUnmatchedVersionsAndSorts()
    {
        // Arrange
        var component = new Component("sudo", "1.9.15", "binary");
        
        var mockNvd = new Mock<INvdApiClient>();
        mockNvd.Setup(x => x.SearchByKeywordAsync("sudo", 50, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                MakeCve("CVE-2026-0001", 9.8, "sudo 1.9.15 heap overflow"),
                MakeCve("CVE-2026-0002", 5.0, "unrelated version 2.2.2")
            });

        var mockEpss = new Mock<IEpssApiClient>();
        mockEpss.Setup(x => x.GetEpssScoresAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, double> { { "CVE-2026-0001", 0.95 } });

        var handler = new CorrelateRiskHandler(mockNvd.Object, mockEpss.Object, reportProgress: null);
        var query = new CorrelateRiskQuery(new[] { component }, StrictVersionFilter: true);

        // Act
        var result = await handler.HandleAsync(query);

        // Assert
        Assert.Single(result.Findings);
        Assert.Equal("CVE-2026-0001", result.Findings[0].Cve.CveId);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task HandleAsync_QueriesNvdByNameOnly()
    {
        // Arrange
        var component = new Component("openssh", "1:10.0p1-7+deb13u4", "dpkg");
        
        var mockNvd = new Mock<INvdApiClient>();
        mockNvd.Setup(x => x.SearchByKeywordAsync("openssh", 50, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<CveEntry>());

        var mockEpss = new Mock<IEpssApiClient>();
        mockEpss.Setup(x => x.GetEpssScoresAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, double>());

        var handler = new CorrelateRiskHandler(mockNvd.Object, mockEpss.Object, reportProgress: null);
        var query = new CorrelateRiskQuery(new[] { component });

        // Act
        await handler.HandleAsync(query);

        // Assert
        mockNvd.Verify(x => x.SearchByKeywordAsync("openssh", 50, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ReportsRawAndFilteredCounts()
    {
        // Arrange
        var component = new Component("sudo", "1.9.15", "binary");
        
        var mockNvd = new Mock<INvdApiClient>();
        mockNvd.Setup(x => x.SearchByKeywordAsync("sudo", 50, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                MakeCve("CVE-2026-0001", 9.8, "sudo 1.9.15 heap overflow"),
                MakeCve("CVE-2026-0002", 5.0, "unrelated version 2.2.2")
            });

        var mockEpss = new Mock<IEpssApiClient>();

        var reportedNames = new List<string>();
        var reportedRaw = new List<int>();
        var reportedFiltered = new List<int>();

        void OnProgress(string name, int raw, int filtered)
        {
            reportedNames.Add(name);
            reportedRaw.Add(raw);
            reportedFiltered.Add(filtered);
        }

        var handler = new CorrelateRiskHandler(mockNvd.Object, mockEpss.Object, reportProgress: OnProgress);
        var query = new CorrelateRiskQuery(new[] { component });

        // Act
        await handler.HandleAsync(query);

        // Assert
        Assert.Single(reportedNames);
        Assert.Equal("sudo", reportedNames[0]);
        Assert.Equal(2, reportedRaw[0]);
        Assert.Equal(1, reportedFiltered[0]);
    }
}
