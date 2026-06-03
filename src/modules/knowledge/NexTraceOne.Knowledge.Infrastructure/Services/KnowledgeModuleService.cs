using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using NexTraceOne.Catalog.Infrastructure.Persistence;
using NexTraceOne.Knowledge.Contracts;

namespace NexTraceOne.Knowledge.Infrastructure.Services;

/// <summary>
/// Implementação do contrato público do módulo Knowledge.
/// Usa ServiceCatalogDbContext diretamente para consultas de leitura otimizadas.
/// Outros módulos consomem este serviço via IKnowledgeModule — nunca acessam o DbContext.
/// </summary>
internal sealed class KnowledgeModuleService(
    ServiceCatalogDbContext context,
    ILogger<KnowledgeModuleService> logger) : IKnowledgeModule
{
    /// <inheritdoc />
    public async Task<int> CountDocumentsAsync(CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Counting knowledge documents");

        return await context.KnowledgeDocuments
            .AsNoTracking()
            .CountAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> CountOperationalNotesAsync(CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Counting operational notes");

        return await context.OperationalNotes
            .AsNoTracking()
            .CountAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> CountDocumentsByServiceAsync(string serviceId, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Counting knowledge documents for service '{ServiceId}'", serviceId);

        if (!Guid.TryParse(serviceId, out var serviceGuid))
            return 0;

        return await context.KnowledgeRelations
            .AsNoTracking()
            .CountAsync(
                r => r.TargetEntityId == serviceGuid,
                cancellationToken);
    }

    /// <inheritdoc />
    public async Task<KnowledgeModuleSummary> GetModuleSummaryAsync(CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Getting knowledge module summary");

        var documents = await context.KnowledgeDocuments.AsNoTracking().CountAsync(cancellationToken);
        var notes = await context.OperationalNotes.AsNoTracking().CountAsync(cancellationToken);
        var relations = await context.KnowledgeRelations.AsNoTracking().CountAsync(cancellationToken);

        return new KnowledgeModuleSummary(
            TotalDocuments: documents,
            TotalOperationalNotes: notes,
            TotalRelations: relations);
    }
}
