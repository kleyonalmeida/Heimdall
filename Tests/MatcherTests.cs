using Heimdall.Domain.Models;
using Heimdall.Domain.Services;
using Xunit;

namespace Heimdall.Tests;

public class MatcherTests
{
    private static CveEntry MakeCve(string cveId = "CVE-2026-0001", double cvss = 8.8, string[]? refs = null, string description = "")
    {
        return new CveEntry(
            CveId: cveId,
            Description: description,
            CvssScore: cvss,
            CvssSeverity: "HIGH",
            PublishedDate: "2026-01-01T00:00:00Z",
            References: refs ?? Array.Empty<string>()
        );
    }

    [Fact]
    public void VersionMentioned_NullVersion_ReturnsTrue()
    {
        Assert.True(VersionMatcher.IsVersionMentioned(null, "any description"));
    }

    [Fact]
    public void VersionMentioned_MatchesPrefix_ReturnsTrue()
    {
        Assert.True(VersionMatcher.IsVersionMentioned("1.9.15p5", "affects sudo versions before 1.9.15"));
    }

    [Fact]
    public void VersionMentioned_VersionAbsent_ReturnsFalse()
    {
        Assert.False(VersionMatcher.IsVersionMentioned("9.9.9", "affects versions before 1.2.3"));
    }

    [Fact]
    public void VersionMentioned_StripsDebianEpochAndRevision_ReturnsTrue()
    {
        // Caso real: OpenSSH no Debian/Parrot vem como "1:10.0p1-7+deb13u4"
        Assert.True(VersionMatcher.IsVersionMentioned("1:10.0p1-7+deb13u4", "OpenSSH 10.0 client issue"));
    }

    [Fact]
    public void VersionMentioned_StripsUbuntuBuildSuffix_ReturnsTrue()
    {
        Assert.True(VersionMatcher.IsVersionMentioned("2.39-0ubuntu8.7", "glibc 2.39 heap overflow"));
    }

    [Fact]
    public void RiskScore_BonusForPublicExploit_ReturnsHigherScore()
    {
        var cveWithExploit = MakeCve(refs: new[] { "https://example.com/Exploit" });
        var cveWithout = MakeCve(refs: new[] { "https://example.com/Patch" });

        var scoreWith = RiskCalculator.ComputeRiskScore(cveWithExploit, epssScore: 0.9);
        var scoreWithout = RiskCalculator.ComputeRiskScore(cveWithout, epssScore: 0.9);

        Assert.True(scoreWith > scoreWithout);
    }

    [Fact]
    public void RiskScore_BoundedAt100_ReturnsMax100()
    {
        var cve = MakeCve(cvss: 10.0, refs: new[] { "exploit" });
        var score = RiskCalculator.ComputeRiskScore(cve, epssScore: 1.0);
        Assert.True(score <= 100.0);
    }
}
