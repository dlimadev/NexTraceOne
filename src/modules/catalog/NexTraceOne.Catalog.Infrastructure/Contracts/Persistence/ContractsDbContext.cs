using Microsoft.EntityFrameworkCore;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Infrastructure.Persistence;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;

namespace NexTraceOne.Catalog.Infrastructure.Contracts.Persistence;

/// <summary>
/// DbContext do módulo Contracts.
/// Herda de NexTraceDbContextBase: RLS, auditoria, Outbox, criptografia, soft-delete.
/// Suporta multi-protocolo: OpenAPI, Swagger, WSDL, AsyncAPI e formatos futuros.
/// REGRA: Outros módulos NUNCA referenciam este DbContext. Comunicação via Integration Events.
/// </summary>
public sealed class ContractsDbContext(
    DbContextOptions<ContractsDbContext> options,
    ICurrentTenant tenant,
    ICurrentUser user,
    IDateTimeProvider clock)
    : NexTraceDbContextBase(options, tenant, user, clock), IUnitOfWork, IContractsUnitOfWork
{
    /// <summary>Versões de contrato multi-protocolo persistidas no módulo Contracts.</summary>
    public DbSet<ContractVersion> ContractVersions => Set<ContractVersion>();

    /// <summary>Diffs semânticos entre versões de contrato persistidos no módulo Contracts.</summary>
    public DbSet<ContractDiff> ContractDiffs => Set<ContractDiff>();

    /// <summary>Violações de ruleset detectadas em versões de contrato.</summary>
    public DbSet<ContractRuleViolation> ContractRuleViolations => Set<ContractRuleViolation>();

    /// <summary>Artefatos gerados a partir de versões de contrato (testes, scaffolds, evidências).</summary>
    public DbSet<ContractArtifact> ContractArtifacts => Set<ContractArtifact>();

    /// <summary>Drafts de contrato em edição no Contract Studio.</summary>
    public DbSet<ContractDraft> Drafts => Set<ContractDraft>();

    /// <summary>Revisões de drafts de contrato para rastreabilidade do fluxo de aprovação.</summary>
    public DbSet<ContractReview> Reviews => Set<ContractReview>();

    /// <summary>Exemplos associados a drafts ou versões publicadas de contrato.</summary>
    public DbSet<ContractExample> Examples => Set<ContractExample>();

    /// <inheritdoc />
    protected override System.Reflection.Assembly ConfigurationsAssembly
        => typeof(ContractsDbContext).Assembly;

    /// <inheritdoc />
    protected override string? ConfigurationsNamespace
        => "NexTraceOne.Catalog.Infrastructure.Contracts.Persistence.Configurations";

    /// <inheritdoc />
    public Task<int> CommitAsync(CancellationToken cancellationToken = default)
        => SaveChangesAsync(cancellationToken);
}
