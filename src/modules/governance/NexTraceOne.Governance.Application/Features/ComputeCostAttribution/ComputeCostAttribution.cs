using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Entities;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Application.Features.ComputeCostAttribution;

/// <summary>
/// Feature: ComputeCostAttribution — cria um registo de atribuição de custo operacional
/// para uma dimensão específica (serviço, equipa, domínio, contrato ou mudança).
///
/// Owner: módulo Governance.
/// Pilar: FinOps contextual — atribuição de custo por dimensão operacional.
/// </summary>
public static class ComputeCostAttribution
{
    /// <summary>Comando para computar e registar uma atribuição de custo.</summary>
    public sealed record Command(
        CostAttributionDimension Dimension,
        string DimensionKey,
        string? DimensionLabel,
        DateTimeOffset PeriodStart,
        DateTimeOffset PeriodEnd,
        decimal TotalCost,
        decimal ComputeCost,
        decimal StorageCost,
        decimal NetworkCost,
        decimal OtherCost,
        string Currency = "USD",
        string? CostBreakdown = null,
        string? AttributionMethod = null,
        string? DataSources = null,
        string? TenantId = null) : ICommand<Response>;

    /// <summary>Validação do comando de atribuição de custo.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Dimension).IsInEnum();
            RuleFor(x => x.DimensionKey).NotEmpty().MaximumLength(200);
            RuleFor(x => x.DimensionLabel).MaximumLength(300).When(x => x.DimensionLabel is not null);
            RuleFor(x => x.Currency).NotEmpty().MaximumLength(10);
            RuleFor(x => x.AttributionMethod).MaximumLength(100).When(x => x.AttributionMethod is not null);
            RuleFor(x => x.TotalCost).GreaterThanOrEqualTo(0);
            RuleFor(x => x.ComputeCost).GreaterThanOrEqualTo(0);
            RuleFor(x => x.StorageCost).GreaterThanOrEqualTo(0);
            RuleFor(x => x.NetworkCost).GreaterThanOrEqualTo(0);
            RuleFor(x => x.OtherCost).GreaterThanOrEqualTo(0);
            RuleFor(x => x.PeriodEnd).GreaterThan(x => x.PeriodStart);
        }
    }

    /// <summary>Handler que cria um registo de atribuição de custo operacional.</summary>
    public sealed class Handler(
        ICostAttributionRepository repository,
        IGovernanceUnitOfWork unitOfWork,
        IDateTimeProvider clock) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            var now = clock.UtcNow;

            var attribution = CostAttribution.Compute(
                dimension: request.Dimension,
                dimensionKey: request.DimensionKey,
                dimensionLabel: request.DimensionLabel,
                periodStart: request.PeriodStart,
                periodEnd: request.PeriodEnd,
                totalCost: request.TotalCost,
                computeCost: request.ComputeCost,
                storageCost: request.StorageCost,
                networkCost: request.NetworkCost,
                otherCost: request.OtherCost,
                currency: request.Currency,
                costBreakdown: request.CostBreakdown,
                attributionMethod: request.AttributionMethod,
                dataSources: request.DataSources,
                tenantId: request.TenantId,
                now: now);

            await repository.AddAsync(attribution, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            return Result<Response>.Success(new Response(
                AttributionId: attribution.Id.Value,
                Dimension: attribution.Dimension,
                DimensionKey: attribution.DimensionKey,
                DimensionLabel: attribution.DimensionLabel,
                PeriodStart: attribution.PeriodStart,
                PeriodEnd: attribution.PeriodEnd,
                TotalCost: attribution.TotalCost,
                Currency: attribution.Currency,
                ComputedAt: attribution.ComputedAt));
        }
    }

    /// <summary>Resposta com o resultado da computação de atribuição de custo.</summary>
    public sealed record Response(
        Guid AttributionId,
        CostAttributionDimension Dimension,
        string DimensionKey,
        string? DimensionLabel,
        DateTimeOffset PeriodStart,
        DateTimeOffset PeriodEnd,
        decimal TotalCost,
        string Currency,
        DateTimeOffset ComputedAt);
}
