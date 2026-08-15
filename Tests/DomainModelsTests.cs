using Heimdall.Domain.Models;
using Xunit;

namespace Heimdall.Tests;

public class DomainModelsTests
{
    [Fact]
    public void CveEntry_HasPublicExploitHint_ShouldReturnTrue_WhenReferenceContainsExploit()
    {
        // Arrange
        var cve = new CveEntry(
            CveId: "CVE-2026-12345",
            Description: "Test vulnerability",
            CvssScore: 8.8,
            CvssSeverity: "HIGH",
            PublishedDate: "2026-01-01T00:00:00Z",
            References: new[] { "https://example.com/exploit-db/12345" }
        );

        // Act & Assert
        Assert.True(cve.HasPublicExploitHint);
    }

    [Fact]
    public void Finding_HasPublicExploit_ShouldReflectCveExploitHint()
    {
        // Arrange
        var component = new Component("sudo", "1.9.15p5", "dpkg");
        var cve = new CveEntry(
            CveId: "CVE-2026-12345",
            Description: "Test vulnerability",
            CvssScore: 9.8,
            CvssSeverity: "CRITICAL",
            PublishedDate: "2026-01-01T00:00:00Z",
            References: new[] { "https://nvd.nist.gov/vuln/detail/CVE-2026-12345" }
        );

        var finding = new Finding(component, cve, EpssScore: 0.85, RiskScore: 92.5);

        // Act & Assert
        Assert.False(finding.HasPublicExploit);
    }
}
