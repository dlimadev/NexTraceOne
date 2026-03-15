using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Application.Features.GetBenchmarking;

/// <summary>
/// Feature: GetBenchmarking — comparação contextualizada entre equipas ou domínios.
/// Cada comparação inclui contexto para garantir fairness na interpretação dos resultados.
/// </summary>
public static class GetBenchmarking
{
    /// <summary>Query de benchmarking. Dimensão: teams ou domains.</summary>
    public sealed record Query(
        string? Dimension = null) : IQuery<Response>;

    /// <summary>Handler que computa comparações de benchmarking contextualizadas.</summary>
    public sealed class Handler : IQueryHandler<Query, Response>
    {
        public Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var dimension = request.Dimension ?? "teams";

            var comparisons = new List<BenchmarkComparisonDto>
            {
                new("team-payments", "Team Payments", ServiceCount: 6, Criticality: "High",
                    ReliabilityScore: 87.5m, ReliabilityTrend: TrendDirection.Stable,
                    ChangeSafetyScore: 72.0m, IncidentRecurrenceRate: 22.5m,
                    MaturityScore: 76.3m, RiskScore: 74.5m,
                    FinopsEfficiency: CostEfficiency.Inefficient,
                    Strengths: new[] { "Strong ownership coverage", "Well-mapped dependencies", "Defined SLOs" },
                    Gaps: new[] { "High incident recurrence for timeout issues", "FinOps efficiency below target" },
                    Context: "High-criticality domain with PCI compliance requirements; incident recurrence partly driven by third-party payment gateway instability"),
                new("team-commerce", "Team Commerce", ServiceCount: 8, Criticality: "Critical",
                    ReliabilityScore: 62.0m, ReliabilityTrend: TrendDirection.Declining,
                    ChangeSafetyScore: 48.0m, IncidentRecurrenceRate: 35.0m,
                    MaturityScore: 41.3m, RiskScore: 92.0m,
                    FinopsEfficiency: CostEfficiency.Wasteful,
                    Strengths: new[] { "Large service portfolio", "Active development team" },
                    Gaps: new[] { "Frequent rollbacks", "Missing runbooks", "Low maturity across dimensions", "High waste from reprocessing" },
                    Context: "Largest domain by service count; recently onboarded 3 legacy services which skew maturity and reliability metrics downward"),
                new("team-identity", "Team Identity", ServiceCount: 4, Criticality: "High",
                    ReliabilityScore: 81.0m, ReliabilityTrend: TrendDirection.Improving,
                    ChangeSafetyScore: 78.0m, IncidentRecurrenceRate: 10.0m,
                    MaturityScore: 62.5m, RiskScore: 48.0m,
                    FinopsEfficiency: CostEfficiency.Acceptable,
                    Strengths: new[] { "Low incident recurrence", "Good change safety", "Clear ownership" },
                    Gaps: new[] { "SOAP contracts outdated", "Missing runbooks for secondary services" },
                    Context: "Smaller team with focused scope; SOAP contract gaps inherited from legacy system migration"),
                new("team-messaging", "Team Messaging", ServiceCount: 3, Criticality: "Medium",
                    ReliabilityScore: 94.0m, ReliabilityTrend: TrendDirection.Improving,
                    ChangeSafetyScore: 91.0m, IncidentRecurrenceRate: 5.0m,
                    MaturityScore: 81.9m, RiskScore: 18.0m,
                    FinopsEfficiency: CostEfficiency.Efficient,
                    Strengths: new[] { "Highest reliability", "Best maturity scores", "Efficient FinOps", "Canary deployments" },
                    Gaps: new[] { "AI governance still maturing" },
                    Context: "Small, focused team with mature practices; lower criticality allows more experimentation with deployment strategies"),
                new("team-integration", "Team Integration", ServiceCount: 5, Criticality: "Medium",
                    ReliabilityScore: 55.0m, ReliabilityTrend: TrendDirection.Declining,
                    ChangeSafetyScore: 40.0m, IncidentRecurrenceRate: 28.0m,
                    MaturityScore: 17.5m, RiskScore: 71.0m,
                    FinopsEfficiency: CostEfficiency.Inefficient,
                    Strengths: new[] { "Team aware of gaps", "Improvement plan initiated" },
                    Gaps: new[] { "Lowest maturity", "No contracts for most services", "No runbooks", "Manual deployments" },
                    Context: "Team managing mostly legacy integration adapters; low scores reflect inherited technical debt rather than team capability")
            };

            var response = new Response(
                Dimension: dimension,
                Comparisons: comparisons,
                GeneratedAt: DateTimeOffset.UtcNow);

            return Task.FromResult(Result<Response>.Success(response));
        }
    }

    /// <summary>Resposta de benchmarking com comparações contextualizadas.</summary>
    public sealed record Response(
        string Dimension,
        IReadOnlyList<BenchmarkComparisonDto> Comparisons,
        DateTimeOffset GeneratedAt);

    /// <summary>Comparação de benchmarking para um grupo com forças, gaps e contexto explicativo.</summary>
    public sealed record BenchmarkComparisonDto(
        string GroupId,
        string GroupName,
        int ServiceCount,
        string Criticality,
        decimal ReliabilityScore,
        TrendDirection ReliabilityTrend,
        decimal ChangeSafetyScore,
        decimal IncidentRecurrenceRate,
        decimal MaturityScore,
        decimal RiskScore,
        CostEfficiency FinopsEfficiency,
        IReadOnlyList<string> Strengths,
        IReadOnlyList<string> Gaps,
        string Context);
}
