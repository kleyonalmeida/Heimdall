using System.Text.Json;
using System.Text.Json.Serialization;
using Heimdall.Domain.Models;

namespace Heimdall.Application.Commands.GenerateReport;

public static class ReportPresenter
{
    private const string Reset = "\x1b[0m";
    private const string Bold = "\x1b[1m";
    private const string Red = "\x1b[91m";
    private const string Yellow = "\x1b[93m";
    private const string Green = "\x1b[92m";
    private const string Cyan = "\x1b[96m";
    private const string Dim = "\x1b[2m";

    private static string GetRiskColor(double score)
    {
        if (score >= 70) return Red;
        if (score >= 40) return Yellow;
        return Green;
    }

    public static void PrintTerminalReport(SystemInfo systemInfo, IReadOnlyList<Finding> findings, IReadOnlyList<string> errors, TextWriter? writer = null)
    {
        writer ??= Console.Out;

        writer.WriteLine($"{Bold}{Cyan}Heimdall — Linux CVE Auditor{Reset}");
        writer.WriteLine($"{Dim}Scan em {DateTime.UtcNow:yyyy-MM-ddTHH:mm:ss}Z{Reset}\n");

        writer.WriteLine($"{Bold}[+] Sistema{Reset}");
        writer.WriteLine($"    Distro : {systemInfo.DistroName ?? "?"} {systemInfo.DistroVersion ?? ""}");
        writer.WriteLine($"    Kernel : {systemInfo.KernelVersion}");
        writer.WriteLine($"    Arch   : {systemInfo.Architecture}\n");

        writer.WriteLine($"{Bold}[+] Componentes detectados{Reset}");
        foreach (var c in systemInfo.Components)
        {
            writer.WriteLine($"    {c.Name,-10} {c.Version ?? "?",-20} ({c.Source})");
        }
        writer.WriteLine();

        if (findings.Count == 0)
        {
            writer.WriteLine($"{Green}Nenhuma CVE correspondente encontrada nas fontes consultadas.{Reset}");
        }
        else
        {
            writer.WriteLine($"{Bold}[+] Possíveis vulnerabilidades ({findings.Count}){Reset}\n");
            foreach (var f in findings)
            {
                var color = GetRiskColor(f.RiskScore);
                writer.WriteLine($"{color}{Bold}{f.Cve.CveId}{Reset}  {color}risco: {f.RiskScore:F1}/100{Reset}");
                writer.WriteLine($"    Componente : {f.Component.Name} {f.Component.Version}");
                writer.WriteLine($"    CVSS       : {f.Cve.CvssScore?.ToString("F1") ?? "N/D"} ({f.Cve.CvssSeverity ?? "UNKNOWN"})");
                var epssStr = f.EpssScore.HasValue ? $"{f.EpssScore.Value:P1}" : "N/D";
                writer.WriteLine($"    EPSS       : {epssStr}");

                var exploitText = f.HasPublicExploit ? "✔ indício de exploit público" : "sem indício de exploit público";
                var exploitColor = f.HasPublicExploit ? Red : Dim;
                writer.WriteLine($"    Exploit    : {exploitColor}{exploitText}{Reset}");

                var desc = f.Cve.Description.Length > 160
                    ? f.Cve.Description[..160] + "…"
                    : f.Cve.Description;
                writer.WriteLine($"    Descrição  : {desc}");
                writer.WriteLine();
            }
        }

        if (errors.Count > 0)
        {
            writer.WriteLine($"{Yellow}[!] Avisos durante o scan:{Reset}");
            foreach (var err in errors)
            {
                writer.WriteLine($"    - {err}");
            }
            writer.WriteLine();
        }

        writer.WriteLine($"{Dim}Nota: matching é por palavra-chave + heurística de versão, não por CPE exato. Trate como priorização, não como confirmação definitiva — sempre valide manualmente antes de agir.{Reset}");
    }

    public static async Task WriteJsonReportAsync(string filePath, SystemInfo systemInfo, IReadOnlyList<Finding> findings, IReadOnlyList<string> errors, CancellationToken cancellationToken = default)
    {
        var jsonDto = new JsonReportDto
        {
            GeneratedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            System = new JsonSystemDto
            {
                DistroName = systemInfo.DistroName,
                DistroVersion = systemInfo.DistroVersion,
                KernelVersion = systemInfo.KernelVersion,
                Arch = systemInfo.Architecture,
                Components = systemInfo.Components.Select(c => new JsonComponentDto
                {
                    Name = c.Name,
                    Version = c.Version,
                    Source = c.Source,
                    Raw = c.RawOutput
                }).ToList()
            },
            Findings = findings.Select(f => new JsonFindingDto
            {
                CveId = f.Cve.CveId,
                Component = f.Component.Name,
                InstalledVersion = f.Component.Version,
                CvssScore = f.Cve.CvssScore,
                CvssSeverity = f.Cve.CvssSeverity,
                EpssScore = f.EpssScore,
                RiskScore = f.RiskScore,
                HasPublicExploitHint = f.HasPublicExploit,
                Description = f.Cve.Description,
                References = f.Cve.References.ToList()
            }).ToList(),
            Warnings = errors.ToList()
        };

        var json = JsonSerializer.Serialize(jsonDto, ReportJsonSerializerContext.Default.JsonReportDto);
        await File.WriteAllTextAsync(filePath, json, cancellationToken);
    }
}

public class JsonReportDto
{
    [JsonPropertyName("generated_at")]
    public string? GeneratedAt { get; set; }

    [JsonPropertyName("system")]
    public JsonSystemDto? System { get; set; }

    [JsonPropertyName("findings")]
    public List<JsonFindingDto>? Findings { get; set; }

    [JsonPropertyName("warnings")]
    public List<string>? Warnings { get; set; }
}

public class JsonSystemDto
{
    [JsonPropertyName("distro_name")]
    public string? DistroName { get; set; }

    [JsonPropertyName("distro_version")]
    public string? DistroVersion { get; set; }

    [JsonPropertyName("kernel_version")]
    public string? KernelVersion { get; set; }

    [JsonPropertyName("arch")]
    public string? Arch { get; set; }

    [JsonPropertyName("components")]
    public List<JsonComponentDto>? Components { get; set; }
}

public class JsonComponentDto
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("version")]
    public string? Version { get; set; }

    [JsonPropertyName("source")]
    public string? Source { get; set; }

    [JsonPropertyName("raw")]
    public string? Raw { get; set; }
}

public class JsonFindingDto
{
    [JsonPropertyName("cve_id")]
    public string? CveId { get; set; }

    [JsonPropertyName("component")]
    public string? Component { get; set; }

    [JsonPropertyName("installed_version")]
    public string? InstalledVersion { get; set; }

    [JsonPropertyName("cvss_score")]
    public double? CvssScore { get; set; }

    [JsonPropertyName("cvss_severity")]
    public string? CvssSeverity { get; set; }

    [JsonPropertyName("epss_score")]
    public double? EpssScore { get; set; }

    [JsonPropertyName("risk_score")]
    public double RiskScore { get; set; }

    [JsonPropertyName("has_public_exploit_hint")]
    public bool HasPublicExploitHint { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("references")]
    public List<string>? References { get; set; }
}

[JsonSerializable(typeof(JsonReportDto))]
public partial class ReportJsonSerializerContext : JsonSerializerContext
{
}
