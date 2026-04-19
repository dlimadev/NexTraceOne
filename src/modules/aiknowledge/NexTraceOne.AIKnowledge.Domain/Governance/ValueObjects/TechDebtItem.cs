namespace NexTraceOne.AIKnowledge.Domain.Governance.ValueObjects;

/// <summary>Item de dívida técnica com impacto quantificado em termos de negócio.</summary>
public sealed record TechDebtItem(
    string Category,
    string Description,
    string Severity,
    decimal MonthlyCostEstimate,
    double IncidentProbability30d,
    int RemediationStoryPoints,
    double RoiMonths);
