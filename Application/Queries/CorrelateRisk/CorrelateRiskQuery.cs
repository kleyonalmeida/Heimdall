using Heimdall.Abstractions;
using Heimdall.Domain.Models;

namespace Heimdall.Application.Queries.CorrelateRisk;

public record CorrelateRiskQuery(
    IReadOnlyList<Component> Components,
    bool StrictVersionFilter = true
) : IQuery<CorrelateRiskResult>;

public record CorrelateRiskResult(
    IReadOnlyList<Finding> Findings,
    IReadOnlyList<string> Errors
);
