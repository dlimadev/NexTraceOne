using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Enums;

namespace NexTraceOne.Catalog.Application.Contracts.Features.SimulateContractChangeImpact;

/// <summary>
/// Feature: SimulateContractChangeImpact — simula o impacto de uma mudança proposta num contrato
/// sobre todos os consumidores conhecidos. Componente central do Digital Twin (Wave D.1).
///
/// A análise baseia-se nas ConsumerExpectations registadas. Consumidores com expectativas
/// activas para o contrato afectado são classificados por nível de impacto:
/// - Breaking → Critical para todos os consumidores activos
/// - Deprecation → High (consumidores têm tempo para migrar mas devem ser notificados)
/// - NonBreaking → Medium (potencial impacto comportamental indirecto)
/// - Additive → Low (consumidores não são quebrados; dados extra podem ser ignorados)
///
/// O resultado inclui por consumidor: nome, domínio, nível de impacto, razão e recomendação.
/// Sem efeitos secundários — é uma query de simulação pura.
/// </summary>
public static class SimulateContractChangeImpact
{
    public sealed record Query(
        Guid ApiAssetId,
        WhatIfChangeType ChangeType,
        string? ProposedDescription = null) : IQuery<Response>;

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ApiAssetId).NotEmpty();
            RuleFor(x => x.ProposedDescription).MaximumLength(2000).When(x => x.ProposedDescription is not null);
        }
    }

    public sealed class Handler(IConsumerExpectationRepository repository)
        : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var consumers = await repository.ListByApiAssetAsync(request.ApiAssetId, cancellationToken);
            var activeConsumers = consumers.Where(c => c.IsActive).ToList();

            var impacts = activeConsumers.Select(c => ClassifyImpact(c, request.ChangeType)).ToList();

            var maxLevel = impacts.Count > 0 ? impacts.Max(i => i.ImpactLevel) : WhatIfImpactLevel.None;
            var overallRisk = maxLevel switch
            {
                WhatIfImpactLevel.Critical => "critical",
                WhatIfImpactLevel.High => "high",
                WhatIfImpactLevel.Medium => "medium",
                WhatIfImpactLevel.Low => "low",
                _ => "none",
            };

            return Result<Response>.Success(new Response(
                ApiAssetId: request.ApiAssetId,
                ChangeType: request.ChangeType,
                ProposedDescription: request.ProposedDescription,
                TotalConsumers: activeConsumers.Count,
                ImpactedConsumers: impacts.Count(i => i.ImpactLevel > WhatIfImpactLevel.None),
                OverallRisk: overallRisk,
                ConsumerImpacts: impacts));
        }

        private static ConsumerImpactItem ClassifyImpact(
            ConsumerExpectation consumer,
            WhatIfChangeType changeType)
        {
            var (level, reason, recommendation) = changeType switch
            {
                WhatIfChangeType.Breaking =>
                    (WhatIfImpactLevel.Critical,
                     "Breaking change will violate consumer expectations.",
                     "Coordinate migration plan or version the contract before deployment."),

                WhatIfChangeType.Deprecation =>
                    (WhatIfImpactLevel.High,
                     "Contract deprecation affects all active consumers.",
                     "Notify consumer team to plan migration before deprecation deadline."),

                WhatIfChangeType.NonBreaking =>
                    (WhatIfImpactLevel.Medium,
                     "Behavioural change may indirectly affect consumer integrations.",
                     "Review consumer expectations and run CDCT verification."),

                WhatIfChangeType.Additive =>
                    (WhatIfImpactLevel.Low,
                     "Additive change is backward compatible.",
                     "No immediate action required; consumer may optionally adopt new fields."),

                _ => (WhatIfImpactLevel.None, "No impact expected.", "No action required.")
            };

            return new ConsumerImpactItem(
                ConsumerServiceName: consumer.ConsumerServiceName,
                ConsumerDomain: consumer.ConsumerDomain,
                ApiAssetId: consumer.ApiAssetId,
                ImpactLevel: level,
                Reason: reason,
                Recommendation: recommendation);
        }
    }

    public sealed record ConsumerImpactItem(
        string ConsumerServiceName,
        string ConsumerDomain,
        Guid ApiAssetId,
        WhatIfImpactLevel ImpactLevel,
        string Reason,
        string Recommendation);

    public sealed record Response(
        Guid ApiAssetId,
        WhatIfChangeType ChangeType,
        string? ProposedDescription,
        int TotalConsumers,
        int ImpactedConsumers,
        string OverallRisk,
        IReadOnlyList<ConsumerImpactItem> ConsumerImpacts);
}
