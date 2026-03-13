using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Domain.Results;
using NexTraceOne.EngineeringGraph.Application.Abstractions;
using NexTraceOne.EngineeringGraph.Domain.Entities;
using NexTraceOne.EngineeringGraph.Domain.Errors;

namespace NexTraceOne.EngineeringGraph.Application.Features.SyncConsumers;

/// <summary>
/// Feature: SyncConsumers — endpoint externo seguro para ingestão de consumidores
/// vindos de plataformas externas (CI/CD, service mesh, API gateways, etc.).
/// Suporta criação e atualização (upsert) de relações de consumo em lote,
/// com idempotência por chave composta (ApiAssetId + ConsumerName + SourceType).
/// Projetado para integração sistema-a-sistema com autenticação e tenant scoping.
/// Estrutura VSA: Command + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class SyncConsumers
{
    /// <summary>
    /// Comando de sincronização de consumidores vindos de integração externa.
    /// Aceita um lote de itens para permitir ingestão eficiente.
    /// Cada item representa uma relação de consumo a ser criada ou atualizada.
    /// </summary>
    public sealed record Command(
        IReadOnlyList<ConsumerSyncItem> Items,
        string SourceSystem,
        string? CorrelationId) : ICommand<Response>;

    /// <summary>
    /// Item individual de sincronização de consumidor.
    /// A combinação ApiAssetId + ConsumerName + SourceType define a chave de idempotência.
    /// Se a relação já existir, será atualizada (refresh); caso contrário, será criada.
    /// </summary>
    public sealed record ConsumerSyncItem(
        Guid ApiAssetId,
        string ConsumerName,
        string ConsumerKind,
        string ConsumerEnvironment,
        string ExternalReference,
        decimal ConfidenceScore);

    /// <summary>Valida o comando de sincronização e cada item individual.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Items).NotNull().NotEmpty()
                .WithMessage("At least one consumer sync item is required.");
            RuleFor(x => x.Items.Count).LessThanOrEqualTo(100)
                .WithMessage("Maximum of 100 items per sync batch.");
            RuleFor(x => x.SourceSystem).NotEmpty().MaximumLength(200);
            RuleForEach(x => x.Items).ChildRules(item =>
            {
                item.RuleFor(i => i.ApiAssetId).NotEmpty();
                item.RuleFor(i => i.ConsumerName).NotEmpty().MaximumLength(200);
                item.RuleFor(i => i.ConsumerKind).NotEmpty().MaximumLength(100);
                item.RuleFor(i => i.ConsumerEnvironment).NotEmpty().MaximumLength(100);
                item.RuleFor(i => i.ExternalReference).NotEmpty().MaximumLength(500);
                item.RuleFor(i => i.ConfidenceScore).InclusiveBetween(0.01m, 1.0m);
            });
        }
    }

    /// <summary>
    /// Handler que processa a sincronização de consumidores em lote.
    /// Para cada item, localiza o API asset correspondente e faz upsert da relação.
    /// Itens com ApiAssetId inválido são registados como falhas mas não bloqueiam o lote.
    /// </summary>
    public sealed class Handler(
        IApiAssetRepository apiAssetRepository,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var results = new List<SyncItemResult>();
            var now = dateTimeProvider.UtcNow;
            var created = 0;
            var updated = 0;
            var failed = 0;

            foreach (var item in request.Items)
            {
                var apiAssetId = ApiAssetId.From(item.ApiAssetId);
                var apiAsset = await apiAssetRepository.GetByIdAsync(apiAssetId, cancellationToken);

                if (apiAsset is null)
                {
                    results.Add(new SyncItemResult(
                        item.ApiAssetId,
                        item.ConsumerName,
                        SyncOutcome.Failed,
                        "EngineeringGraph.ApiAsset.NotFound"));
                    failed++;
                    continue;
                }

                if (apiAsset.IsDecommissioned)
                {
                    results.Add(new SyncItemResult(
                        item.ApiAssetId,
                        item.ConsumerName,
                        SyncOutcome.Failed,
                        "EngineeringGraph.ApiAsset.Decommissioned"));
                    failed++;
                    continue;
                }

                var consumerAsset = ConsumerAsset.Create(
                    item.ConsumerName, item.ConsumerKind, item.ConsumerEnvironment);
                var discoverySource = DiscoverySource.Create(
                    request.SourceSystem, item.ExternalReference, now, item.ConfidenceScore);

                var existingRelationship = apiAsset.ConsumerRelationships
                    .FirstOrDefault(r => string.Equals(r.ConsumerName, item.ConsumerName, StringComparison.OrdinalIgnoreCase));

                var relationshipResult = apiAsset.MapConsumerRelationship(consumerAsset, discoverySource, now);

                if (relationshipResult.IsFailure)
                {
                    results.Add(new SyncItemResult(
                        item.ApiAssetId,
                        item.ConsumerName,
                        SyncOutcome.Failed,
                        relationshipResult.Error.Code));
                    failed++;
                    continue;
                }

                var outcome = existingRelationship is null ? SyncOutcome.Created : SyncOutcome.Updated;
                if (outcome == SyncOutcome.Created) created++;
                else updated++;

                results.Add(new SyncItemResult(
                    item.ApiAssetId,
                    item.ConsumerName,
                    outcome,
                    null));
            }

            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(
                results,
                created,
                updated,
                failed,
                request.Items.Count,
                request.CorrelationId);
        }
    }

    /// <summary>Resposta da sincronização com contadores e resultados individuais.</summary>
    public sealed record Response(
        IReadOnlyList<SyncItemResult> Results,
        int Created,
        int Updated,
        int Failed,
        int Total,
        string? CorrelationId);

    /// <summary>Resultado individual de um item sincronizado.</summary>
    public sealed record SyncItemResult(
        Guid ApiAssetId,
        string ConsumerName,
        SyncOutcome Outcome,
        string? ErrorCode);

    /// <summary>Resultado possível de cada item na sincronização.</summary>
    public enum SyncOutcome
    {
        /// <summary>Relação criada com sucesso.</summary>
        Created,
        /// <summary>Relação existente atualizada com sucesso.</summary>
        Updated,
        /// <summary>Falha ao processar o item.</summary>
        Failed
    }
}
