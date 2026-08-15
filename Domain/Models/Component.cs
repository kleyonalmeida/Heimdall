namespace Heimdall.Domain.Models;

/// <summary>
/// Representa um componente crítico detectado no sistema.
/// </summary>
public record Component(
    string Name,
    string? Version,
    string Source,
    string RawOutput = ""
);
