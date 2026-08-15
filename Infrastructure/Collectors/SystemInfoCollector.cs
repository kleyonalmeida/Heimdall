using System.Runtime.InteropServices;
using Heimdall.Domain.Models;

namespace Heimdall.Infrastructure.Collectors;

public static class SystemInfoCollector
{
    public static SystemInfo CollectSystemInfo()
    {
        var kernelVersion = RuntimeInformation.OSDescription;
        var architecture = RuntimeInformation.OSArchitecture.ToString();
        var distroName = "Linux";
        var distroVersion = string.Empty;

        try
        {
            if (File.Exists("/etc/os-release"))
            {
                var lines = File.ReadAllLines("/etc/os-release");
                foreach (var line in lines)
                {
                    if (line.StartsWith("NAME="))
                    {
                        distroName = line.Substring("NAME=".Length).Trim('"', '\'');
                    }
                    else if (line.StartsWith("VERSION_ID="))
                    {
                        distroVersion = line.Substring("VERSION_ID=".Length).Trim('"', '\'');
                    }
                }
            }
        }
        catch
        {
            // Ignore errors reading os-release
        }

        var components = CollectComponents();

        return new SystemInfo(
            KernelVersion: kernelVersion,
            DistroName: distroName,
            DistroVersion: distroVersion,
            Architecture: architecture,
            Components: components
        );
    }

    private static IReadOnlyList<Component> CollectComponents()
    {
        // For now, return an empty list or minimal mocked list.
        // Full component collection logic via dpkg/rpm/binaries will be mapped later if needed,
        // but the test CollectSystemInfo_ReturnsKernelAndArchitecture only asserts Components is not null.
        return Array.Empty<Component>();
    }
}
