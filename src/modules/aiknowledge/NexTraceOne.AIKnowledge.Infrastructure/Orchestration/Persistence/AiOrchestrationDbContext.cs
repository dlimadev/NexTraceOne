using Microsoft.EntityFrameworkCore;

using NexTraceOne.AIKnowledge.Domain.Orchestration.Entities;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Infrastructure.Persistence;

namespace NexTraceOne.AIKnowledge.Infrastructure.Orchestration.Persistence;

/// <summary>
/// DbContext do módulo AiOrchestration.
/// Herda de NexTraceDbContextBase: RLS, auditoria, Outbox, criptografia, soft-delete.
/// REGRA: Outros módulos NUNCA referenciam este DbContext. Comunicação via Integration Events.
/// </summary>
public sealed class AiOrchestrationDbContext(
    DbContextOptions<AiOrchestrationDbContext> options,
    ICurrentTenant tenant,
    ICurrentUser user,
    IDateTimeProvider clock)
    : NexTraceDbContextBase(options, tenant, user, clock), IUnitOfWork
{
    /// <summary>Contextos montados para consultas de IA com dados agregados de análise.</summary>
    public DbSet<AiContext> Contexts => Set<AiContext>();

    /// <summary>Conversas multi-turno com IA sobre mudanças, releases ou erros.</summary>
    public DbSet<AiConversation> Conversations => Set<AiConversation>();

    /// <summary>Artefatos de teste gerados por IA aguardando revisão humana.</summary>
    public DbSet<GeneratedTestArtifact> TestArtifacts => Set<GeneratedTestArtifact>();

    /// <summary>Entradas sugeridas para base de conhecimento pela orquestração de IA.</summary>
    public DbSet<KnowledgeCaptureEntry> KnowledgeCaptureEntries => Set<KnowledgeCaptureEntry>();

    protected override System.Reflection.Assembly ConfigurationsAssembly
        => typeof(AiOrchestrationDbContext).Assembly;

    protected override string? ConfigurationsNamespace
        => "NexTraceOne.AIKnowledge.Infrastructure.Orchestration.Persistence.Configurations";

    /// <inheritdoc />
    protected override string OutboxTableName => "aik_outbox_messages";

    /// <inheritdoc />
    public Task<int> CommitAsync(CancellationToken cancellationToken = default)
        => SaveChangesAsync(cancellationToken);
}
