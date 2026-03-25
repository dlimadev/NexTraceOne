using Microsoft.EntityFrameworkCore;

using NexTraceOne.AIKnowledge.Domain.ExternalAI.Entities;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Infrastructure.Persistence;

namespace NexTraceOne.AIKnowledge.Infrastructure.ExternalAI.Persistence;

/// <summary>
/// DbContext do módulo ExternalAi.
/// Herda de NexTraceDbContextBase: RLS, auditoria, Outbox, criptografia, soft-delete.
/// REGRA: Outros módulos NUNCA referenciam este DbContext. Comunicação via Integration Events.
/// </summary>
public sealed class ExternalAiDbContext(
    DbContextOptions<ExternalAiDbContext> options,
    ICurrentTenant tenant,
    ICurrentUser user,
    IDateTimeProvider clock)
    : NexTraceDbContextBase(options, tenant, user, clock), IUnitOfWork
{
    /// <summary>Provedores externos de IA registrados na plataforma.</summary>
    public DbSet<ExternalAiProvider> Providers => Set<ExternalAiProvider>();

    /// <summary>Políticas de governança para uso de IA externa.</summary>
    public DbSet<ExternalAiPolicy> Policies => Set<ExternalAiPolicy>();

    /// <summary>Consultas enviadas a provedores externos de IA.</summary>
    public DbSet<ExternalAiConsultation> Consultations => Set<ExternalAiConsultation>();

    /// <summary>Conhecimento organizacional capturado de interações com IA externa.</summary>
    public DbSet<KnowledgeCapture> KnowledgeCaptures => Set<KnowledgeCapture>();

    protected override System.Reflection.Assembly ConfigurationsAssembly
        => typeof(ExternalAiDbContext).Assembly;

    protected override string? ConfigurationsNamespace
        => "NexTraceOne.AIKnowledge.Infrastructure.ExternalAI.Persistence.Configurations";

    /// <inheritdoc />
    protected override string OutboxTableName => "aik_ext_outbox_messages";

    /// <inheritdoc />
    public Task<int> CommitAsync(CancellationToken cancellationToken = default)
        => SaveChangesAsync(cancellationToken);
}
