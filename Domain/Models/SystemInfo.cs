namespace Heimdall.Domain.Models;

/// <summary>
/// Informações coletadas do ambiente local Linux.
/// </summary>
public record SystemInfo(
    string KernelVersion,
    string? DistroName,
    string? DistroVersion,
    string Architecture,
    IReadOnlyList<Component> Components
);
