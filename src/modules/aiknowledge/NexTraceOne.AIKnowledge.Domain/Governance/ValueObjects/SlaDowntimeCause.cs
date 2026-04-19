namespace NexTraceOne.AIKnowledge.Domain.Governance.ValueObjects;

/// <summary>Causa de downtime de SLA com percentagem e classificação de evitabilidade.</summary>
public sealed record SlaDowntimeCause(string Category, double PercentageOfDowntime, string Description, bool IsAvoidable);
