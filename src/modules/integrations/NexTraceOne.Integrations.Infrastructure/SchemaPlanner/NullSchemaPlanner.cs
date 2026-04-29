using NexTraceOne.Integrations.Domain;

namespace NexTraceOne.Integrations.Infrastructure.SchemaPlanner;

/// <summary>
/// Implementação nula de ISchemaPlanner.
/// Retorna planos simulados enquanto nenhum executor IaC (Terraform, Pulumi, Flyway) estiver configurado.
/// </summary>
internal sealed class NullSchemaPlanner : ISchemaPlanner
{
    public bool IsConfigured => false;

    public Task<SchemaPlan> PlanSchemaChangesAsync(
        string tenantSlug, string? targetVersion = null, CancellationToken cancellationToken = default)
        => Task.FromResult(new SchemaPlan(
            TenantSlug: tenantSlug,
            CurrentVersion: "unknown",
            TargetVersion: targetVersion ?? "latest",
            Steps: [],
            IsSimulated: true));

    public Task<SchemaApplyResult> ApplySchemaChangesAsync(
        SchemaPlan plan, CancellationToken cancellationToken = default)
        => Task.FromResult(new SchemaApplyResult(
            TenantSlug: plan.TenantSlug,
            Success: false,
            AppliedVersion: plan.CurrentVersion,
            Error: "No IaC executor configured. Schema changes cannot be applied automatically.",
            IsSimulated: true));
}
