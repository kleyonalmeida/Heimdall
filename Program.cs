using Heimdall.Application.Queries.CorrelateRisk;
using Heimdall.Application.Commands.GenerateReport;
using Heimdall.Infrastructure.Collectors;
using Heimdall.Infrastructure.HttpClients;
using Heimdall.Infrastructure.Presentation;

namespace Heimdall;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        if (args.Length == 0 || args[0] != "scan")
        {
            PrintHelp();
            return 1;
        }

        string? jsonPath = null;
        var strictVersionFilter = true;
        string? apiKey = Environment.GetEnvironmentVariable("NVD_API_KEY");
        var showBanner = true;

        for (var i = 1; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--json":
                    if (i + 1 < args.Length) jsonPath = args[++i];
                    break;
                case "--no-version-filter":
                    strictVersionFilter = false;
                    break;
                case "--api-key":
                    if (i + 1 < args.Length) apiKey = args[++i];
                    break;
                case "--no-banner":
                    showBanner = false;
                    break;
            }
        }

        if (showBanner)
        {
            ConsolePresenter.PrintBanner();
        }

        Console.Error.WriteLine("[*] Coletando informações do sistema...");
        var systemInfo = SystemInfoCollector.CollectSystemInfo();

        if (systemInfo.Components.Count == 0)
        {
            Console.Error.WriteLine("[!] Nenhum componente-alvo foi detectado. Rode em um sistema Linux com dpkg/rpm ou os binários esperados.");
        }

        using var httpClient = new HttpClient();
        var nvdClient = new NvdApiClient(httpClient, minDelaySeconds: string.IsNullOrEmpty(apiKey) ? 6.0 : 1.2);
        var epssClient = new EpssApiClient(httpClient);

        CorrelateRiskResult result;

        using (var spinner = new EyeSpinner(intervalSeconds: 0.15))
        {
            spinner.Status("iniciando consulta ao NVD/EPSS...");

            void OnProgress(string name, int rawCount, int filteredCount)
            {
                if (rawCount == 0)
                {
                    spinner.Log($"    -> NVD não retornou nenhuma CVE para '{name}'");
                }
                else if (filteredCount == 0)
                {
                    spinner.Log($"    -> NVD retornou {rawCount} CVE(s) para '{name}', mas nenhuma bateu com a versão instalada (filtro de versão descartou tudo)");
                }
                else
                {
                    spinner.Log($"    -> {filteredCount}/{rawCount} CVE(s) relevantes para '{name}'");
                }
            }

            var handler = new CorrelateRiskHandler(nvdClient, epssClient, OnProgress);
            var query = new CorrelateRiskQuery(systemInfo.Components, StrictVersionFilter: strictVersionFilter);

            result = await handler.HandleAsync(query);
        }

        Console.WriteLine();
        ReportPresenter.PrintTerminalReport(systemInfo, result.Findings, result.Errors);

        if (!string.IsNullOrEmpty(jsonPath))
        {
            await ReportPresenter.WriteJsonReportAsync(jsonPath, systemInfo, result.Findings, result.Errors);
            Console.Error.WriteLine($"\n[*] Relatório JSON salvo em: {jsonPath}");
        }

        return result.Findings.Any(f => f.RiskScore >= 70) ? 1 : 0;
    }

    private static void PrintHelp()
    {
        Console.WriteLine("Heimdall - Auditor de CVEs do Linux (Native AOT + CQRS)");
        Console.WriteLine("Uso: Heimdall scan [--json PATH] [--no-version-filter] [--api-key KEY] [--no-banner]");
    }
}
