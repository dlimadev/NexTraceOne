using Microsoft.EntityFrameworkCore;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Infrastructure.Persistence;
using NexTraceOne.Catalog.Application.DeveloperExperience.Abstractions;
using NexTraceOne.Catalog.Domain.DeveloperExperience.Entities;

namespace NexTraceOne.Catalog.Infrastructure.DeveloperExperience.Persistence;

/// <summary>
/// DbContext do subdomínio Developer Experience (surveys e NPS).
/// Herda de NexTraceDbContextBase: RLS, auditoria, Outbox, criptografia, soft-delete.
/// REGRA: Outros módulos NUNCA referenciam este DbContext.
/// </summary>
public sealed class DeveloperExperienceDbContext(
    DbContextOptions<DeveloperExperienceDbContext> options,
    ICurrentTenant tenant,
    ICurrentUser user,
    IDateTimeProvider clock)
    : NexTraceDbContextBase(options, tenant, user, clock), IUnitOfWork, IDeveloperExperienceUnitOfWork
{
    /// <summary>Surveys de NPS e satisfação submetidos por membros de equipa.</summary>
    public DbSet<DeveloperSurvey> DeveloperSurveys => Set<DeveloperSurvey>();

    /// <summary>Registos de uso da extensão IDE por utilizador e tenant. Wave AK.1.</summary>
    public DbSet<IIDEUsageRepository.IdeUsageRecord> IdeUsageRecords
        => Set<IIDEUsageRepository.IdeUsageRecord>();

    protected override System.Reflection.Assembly ConfigurationsAssembly
        => typeof(DeveloperExperienceDbContext).Assembly;

    /// <inheritdoc />
    protected override string? ConfigurationsNamespace
        => "NexTraceOne.Catalog.Infrastructure.DeveloperExperience";

    /// <inheritdoc />
    protected override string OutboxTableName => "dx_surveys_outbox_messages";

    /// <inheritdoc />
    public Task<int> CommitAsync(CancellationToken cancellationToken = default)
        => SaveChangesAsync(cancellationToken);
}
