namespace NexTraceOne.Integrations.Domain;

/// <summary>
/// Contrato para integração com executores IaC de schema multi-tenant (Terraform, Pulumi, Flyway, …).
/// A implementação padrão é <c>NullSchemaPlanner</c> que retorna propostas simuladas.
/// DEG-06 — Multi-tenant Schema Planner.
/// </summary>
public interface ISchemaPlanner
{
    bool IsConfigured { get; }

    Task<SchemaPlan> PlanSchemaChangesAsync(
        string tenantSlug,
        string? targetVersion = null,
        CancellationToken cancellationToken = default);

    Task<SchemaApplyResult> ApplySchemaChangesAsync(
        SchemaPlan plan,
        CancellationToken cancellationToken = default);
}

/// <summary>Plano de mudanças de schema proposto pelo ISchemaPlanner.</summary>
public sealed record SchemaPlan(
    string TenantSlug,
    string CurrentVersion,
    string TargetVersion,
    IReadOnlyList<string> Steps,
    bool IsSimulated);

/// <summary>Resultado da aplicação de um plano de schema.</summary>
public sealed record SchemaApplyResult(
    string TenantSlug,
    bool Success,
    string AppliedVersion,
    string? Error,
    bool IsSimulated);
