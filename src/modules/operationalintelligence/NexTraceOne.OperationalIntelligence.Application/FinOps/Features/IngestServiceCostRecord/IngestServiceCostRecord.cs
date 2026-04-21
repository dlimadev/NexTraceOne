using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.FinOps.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.FinOps.Entities;
using NexTraceOne.OperationalIntelligence.Domain.FinOps.Enums;

namespace NexTraceOne.OperationalIntelligence.Application.FinOps.Features.IngestServiceCostRecord;

/// <summary>
/// Feature: IngestServiceCostRecord — ingesta um registo de alocação de custo por serviço.
///
/// Comportamento:
/// - Cria um novo ServiceCostAllocationRecord com categoria, valor e período fornecidos
/// - Suporta multi-currency (conversão para USD obrigatória)
/// - Idempotência garantida pelo consumidor via amountUsd e período (sem chave única composta)
///
/// Wave I.2 — FinOps Contextual por Serviço (OperationalIntelligence).
/// </summary>
public static class IngestServiceCostRecord
{
    public sealed record Command(
        string TenantId,
        string ServiceName,
        string Environment,
        CostCategory Category,
        decimal AmountUsd,
        DateTimeOffset PeriodStart,
        DateTimeOffset PeriodEnd,
        string? TeamId = null,
        string? DomainName = null,
        string? Currency = null,
        decimal? OriginalAmount = null,
        string? TagsJson = null,
        string? Source = null) : ICommand<Response>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.TenantId).NotEmpty().MaximumLength(100);
            RuleFor(x => x.ServiceName).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Environment).NotEmpty().MaximumLength(100);
            RuleFor(x => x.AmountUsd).GreaterThanOrEqualTo(0);
            RuleFor(x => x.PeriodStart).NotEmpty();
            RuleFor(x => x.PeriodEnd).GreaterThan(x => x.PeriodStart)
                .WithMessage("PeriodEnd must be after PeriodStart.");
            RuleFor(x => x.Currency).MaximumLength(10).When(x => x.Currency is not null);
            RuleFor(x => x.OriginalAmount).GreaterThanOrEqualTo(0).When(x => x.OriginalAmount.HasValue);
            RuleFor(x => x.TagsJson).MaximumLength(2000).When(x => x.TagsJson is not null);
            RuleFor(x => x.Source).MaximumLength(100).When(x => x.Source is not null);
        }
    }

    public sealed class Handler(
        IServiceCostAllocationRepository repository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider clock) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var record = ServiceCostAllocationRecord.Create(
                tenantId: request.TenantId,
                serviceName: request.ServiceName,
                environment: request.Environment,
                category: request.Category,
                amountUsd: request.AmountUsd,
                periodStart: request.PeriodStart,
                periodEnd: request.PeriodEnd,
                createdAt: clock.UtcNow,
                teamId: request.TeamId,
                domainName: request.DomainName,
                currency: request.Currency,
                originalAmount: request.OriginalAmount,
                tagsJson: request.TagsJson,
                source: request.Source);

            repository.Add(record);
            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(
                RecordId: record.Id.Value,
                ServiceName: record.ServiceName,
                Environment: record.Environment,
                Category: record.Category,
                AmountUsd: record.AmountUsd,
                PeriodStart: record.PeriodStart,
                PeriodEnd: record.PeriodEnd);
        }
    }

    public sealed record Response(
        Guid RecordId,
        string ServiceName,
        string Environment,
        CostCategory Category,
        decimal AmountUsd,
        DateTimeOffset PeriodStart,
        DateTimeOffset PeriodEnd);
}
