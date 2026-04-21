using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Entities;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Enums;

namespace NexTraceOne.OperationalIntelligence.Application.Runtime.Features.IngestSloObservation;

/// <summary>
/// Feature: IngestSloObservation — regista uma observação pontual de SLO para um serviço.
///
/// Comportamento:
/// - Cria um novo SloObservation com o valor observado e o objetivo definido
/// - O estado (Met/Warning/Breached) é calculado automaticamente pelo domínio
/// - Idempotência garantida pelo consumidor (sem chave única composta)
///
/// Wave J.2 — SLO Tracking (OperationalIntelligence).
/// </summary>
public static class IngestSloObservation
{
    public sealed record Command(
        string TenantId,
        string ServiceName,
        string Environment,
        string MetricName,
        decimal ObservedValue,
        decimal SloTarget,
        DateTimeOffset PeriodStart,
        DateTimeOffset PeriodEnd,
        string? Unit = null) : ICommand<Response>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.TenantId).NotEmpty().MaximumLength(100);
            RuleFor(x => x.ServiceName).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Environment).NotEmpty().MaximumLength(100);
            RuleFor(x => x.MetricName).NotEmpty().MaximumLength(200);
            RuleFor(x => x.SloTarget).GreaterThan(0);
            RuleFor(x => x.PeriodStart).NotEmpty();
            RuleFor(x => x.PeriodEnd).GreaterThan(x => x.PeriodStart)
                .WithMessage("PeriodEnd must be after PeriodStart.");
            RuleFor(x => x.Unit).MaximumLength(50).When(x => x.Unit is not null);
        }
    }

    public sealed class Handler(
        ISloObservationRepository repository,
        IRuntimeIntelligenceUnitOfWork unitOfWork,
        IDateTimeProvider clock) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var observation = SloObservation.Create(
                tenantId: request.TenantId,
                serviceName: request.ServiceName,
                environment: request.Environment,
                metricName: request.MetricName,
                observedValue: request.ObservedValue,
                sloTarget: request.SloTarget,
                periodStart: request.PeriodStart,
                periodEnd: request.PeriodEnd,
                observedAt: clock.UtcNow,
                unit: request.Unit);

            repository.Add(observation);
            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(
                ObservationId: observation.Id.Value,
                ServiceName: observation.ServiceName,
                Environment: observation.Environment,
                MetricName: observation.MetricName,
                ObservedValue: observation.ObservedValue,
                SloTarget: observation.SloTarget,
                Status: observation.Status,
                PeriodStart: observation.PeriodStart,
                PeriodEnd: observation.PeriodEnd);
        }
    }

    public sealed record Response(
        Guid ObservationId,
        string ServiceName,
        string Environment,
        string MetricName,
        decimal ObservedValue,
        decimal SloTarget,
        SloObservationStatus Status,
        DateTimeOffset PeriodStart,
        DateTimeOffset PeriodEnd);
}
