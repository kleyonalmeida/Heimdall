using Heimdall.Domain.Models;
using Heimdall.Domain.Services;
using Heimdall.Infrastructure.Collectors;
using Xunit;

namespace Heimdall.Tests;

public class CollectorsTests
{
    [Fact]
    public void ExtractVersion_Simple_ReturnsExpectedVersion()
    {
        var version = VersionNormalizer.ExtractVersion("sudo 1.9.15");
        Assert.Equal("1.9.15", version);
    }

    [Fact]
    public void ExtractVersion_WithBuildMetadata_ReturnsFullVersion()
    {
        var version = VersionNormalizer.ExtractVersion("OpenSSL 3.0.13-1ubuntu3");
        Assert.Equal("3.0.13-1ubuntu3", version);
    }

    [Fact]
    public void ExtractVersion_AbsentOrNull_ReturnsNull()
    {
        Assert.Null(VersionNormalizer.ExtractVersion("no version here"));
        Assert.Null(VersionNormalizer.ExtractVersion(null));
    }

    [Fact]
    public void Component_Defaults_RawOutputIsEmptyString()
    {
        var component = new Component("sudo", "1.9.15", "binary");
        Assert.Equal(string.Empty, component.RawOutput);
    }

    [Fact]
    public void CollectSystemInfo_ReturnsKernelAndArchitecture()
    {
        var systemInfo = SystemInfoCollector.CollectSystemInfo();
        
        Assert.False(string.IsNullOrWhiteSpace(systemInfo.KernelVersion));
        Assert.False(string.IsNullOrWhiteSpace(systemInfo.Architecture));
        Assert.NotNull(systemInfo.Components);
    }
}

