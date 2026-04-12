using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Entities;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Application.Features.RecordChangeCostImpact;

/// <summary>
/// Feature: RecordChangeCostImpact — regista o impacto de custo de uma mudança/release.
/// Calcula delta, percentagem e direção automaticamente.
///
/// Owner: módulo Governance — subdomínio FinOps.
/// Pilar: FinOps contextual — correlação de custo com mudança.
/// </summary>
public static class RecordChangeCostImpact
{
    /// <summary>Command para registar impacto de custo de uma mudança.</summary>
    public sealed record Command(
        Guid ReleaseId,
        string ServiceName,
        string Environment,
        string? ChangeDescription,
        decimal BaselineCostPerDay,
        decimal ActualCostPerDay,
        string? CostProvider,
        string? CostDetails,
        DateTimeOffset MeasurementWindowStart,
        DateTimeOffset MeasurementWindowEnd,
        string? TenantId = null) : ICommand<Response>;

    /// <summary>Validação do command.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ReleaseId).NotEmpty();
            RuleFor(x => x.ServiceName).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Environment).NotEmpty().MaximumLength(100);
            RuleFor(x => x.ChangeDescription).MaximumLength(500).When(x => x.ChangeDescription is not null);
            RuleFor(x => x.BaselineCostPerDay).GreaterThanOrEqualTo(0);
            RuleFor(x => x.ActualCostPerDay).GreaterThanOrEqualTo(0);
            RuleFor(x => x.CostProvider).MaximumLength(100).When(x => x.CostProvider is not null);
            RuleFor(x => x.MeasurementWindowEnd).GreaterThan(x => x.MeasurementWindowStart);
        }
    }

    /// <summary>Handler que regista o impacto de custo de uma mudança.</summary>
    public sealed class Handler(
        IChangeCostImpactRepository repository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider clock) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            var now = clock.UtcNow;

            var impact = ChangeCostImpact.Record(
                releaseId: request.ReleaseId,
                serviceName: request.ServiceName,
                environment: request.Environment,
                changeDescription: request.ChangeDescription,
                baselineCostPerDay: request.BaselineCostPerDay,
                actualCostPerDay: request.ActualCostPerDay,
                costProvider: request.CostProvider,
                costDetails: request.CostDetails,
                measurementWindowStart: request.MeasurementWindowStart,
                measurementWindowEnd: request.MeasurementWindowEnd,
                tenantId: request.TenantId,
                now: now);

            await repository.AddAsync(impact, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            return Result<Response>.Success(new Response(
                ImpactId: impact.Id.Value,
                ReleaseId: impact.ReleaseId,
                ServiceName: impact.ServiceName,
                Environment: impact.Environment,
                CostDelta: impact.CostDelta,
                CostDeltaPercentage: impact.CostDeltaPercentage,
                Direction: impact.Direction,
                RecordedAt: impact.RecordedAt));
        }
    }

    /// <summary>Resposta com o resumo do impacto de custo registado.</summary>
    public sealed record Response(
        Guid ImpactId,
        Guid ReleaseId,
        string ServiceName,
        string Environment,
        decimal CostDelta,
        decimal CostDeltaPercentage,
        CostChangeDirection Direction,
        DateTimeOffset RecordedAt);
}
