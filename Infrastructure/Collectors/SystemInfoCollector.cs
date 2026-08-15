using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Heimdall.Domain.Models;
using Heimdall.Domain.Services;

namespace Heimdall.Infrastructure.Collectors;

public static class SystemInfoCollector
{
    private record TargetComponentDefinition(
        string LogicalName,
        string[] PackageCandidates,
        string? Binary,
        string[] BinaryArgs
    );

    private static readonly TargetComponentDefinition[] TargetComponents =
    [
        new("glibc", ["libc6", "glibc"], "ldd", ["--version"]),
        new("sudo", ["sudo"], "sudo", ["-V"]),
        new("systemd", ["systemd"], "systemctl", ["--version"]),
        new("polkit", ["policykit-1", "polkit"], "pkexec", ["--version"]),
        new("openssl", ["openssl"], "openssl", ["version"]),
        new("docker", ["docker-ce", "docker.io"], "docker", ["--version"]),
        new("podman", ["podman"], "podman", ["--version"]),
        new("snapd", ["snapd"], "snap", ["--version"]),
        new("bash", ["bash"], "bash", ["--version"]),
        new("openssh", ["openssh-server", "openssh"], "ssh", ["-V"])
    ];

    public static SystemInfo CollectSystemInfo()
    {
        var kernelVersion = RuntimeInformation.OSDescription;
        var architecture = RuntimeInformation.OSArchitecture.ToString().ToLowerInvariant();
        var distroName = "Linux";
        var distroVersion = string.Empty;

        try
        {
            if (File.Exists("/etc/os-release"))
            {
                var lines = File.ReadAllLines("/etc/os-release");
                foreach (var line in lines)
                {
                    if (line.StartsWith("NAME=", StringComparison.OrdinalIgnoreCase))
                    {
                        distroName = line["NAME=".Length..].Trim('"', '\'');
                    }
                    else if (line.StartsWith("VERSION_ID=", StringComparison.OrdinalIgnoreCase))
                    {
                        distroVersion = line["VERSION_ID=".Length..].Trim('"', '\'');
                    }
                }
            }
        }
        catch
        {
            // Fail safely
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
        var found = new List<Component>();

        foreach (var target in TargetComponents)
        {
            Component? component = null;

            foreach (var pkg in target.PackageCandidates)
            {
                component = DetectViaPackageManager(pkg);
                if (component != null)
                {
                    component = component with { Name = target.LogicalName };
                    break;
                }
            }

            if (component == null && target.Binary != null)
            {
                component = DetectViaBinary(target.LogicalName, target.Binary, target.BinaryArgs);
            }

            if (component != null)
            {
                found.Add(component);
            }
        }

        return found;
    }

    private static Component? DetectViaPackageManager(string packageName)
    {
        var dpkgOut = RunCommand("dpkg-query", ["-W", $"-f=${{Version}}", packageName]);
        if (!string.IsNullOrWhiteSpace(dpkgOut))
        {
            return new Component(packageName, dpkgOut.Trim(), "dpkg", dpkgOut);
        }

        var rpmOut = RunCommand("rpm", ["-q", "--qf", "%{VERSION}-%{RELEASE}", packageName]);
        if (!string.IsNullOrWhiteSpace(rpmOut) && !rpmOut.Contains("not installed", StringComparison.OrdinalIgnoreCase))
        {
            return new Component(packageName, rpmOut.Trim(), "rpm", rpmOut);
        }

        return null;
    }

    private static Component? DetectViaBinary(string logicalName, string binary, string[] args)
    {
        var outText = RunCommand(binary, args);
        var version = VersionNormalizer.ExtractVersion(outText);
        if (!string.IsNullOrWhiteSpace(version))
        {
            return new Component(logicalName, version, $"{binary} {string.Join(' ', args)}", outText ?? "");
        }
        return null;
    }

    private static string? RunCommand(string binary, string[] args)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = binary,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            foreach (var arg in args)
            {
                psi.ArgumentList.Add(arg);
            }

            using var process = Process.Start(psi);
            if (process == null) return null;

            var output = process.StandardOutput.ReadToEnd();
            if (!process.WaitForExit(5000))
            {
                try { process.Kill(); } catch { }
                return null;
            }

            return process.ExitCode == 0 ? output.Trim() : null;
        }
        catch
        {
            return null;
        }
    }
}
