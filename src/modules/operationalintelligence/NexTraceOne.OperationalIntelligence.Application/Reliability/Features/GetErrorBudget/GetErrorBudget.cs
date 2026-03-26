using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Reliability.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Reliability.Entities;
using NexTraceOne.OperationalIntelligence.Domain.Reliability.Enums;

namespace NexTraceOne.OperationalIntelligence.Application.Reliability.Features.GetErrorBudget;

/// <summary>
/// Feature: GetErrorBudget — consulta o estado actual do error budget de um SLO.
/// Retorna o snapshot mais recente de budget total, consumido, remanescente e percentagem de consumo.
/// </summary>
public static class GetErrorBudget
{
    public sealed record Query(Guid SloDefinitionId) : IQuery<Response>;

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.SloDefinitionId).NotEmpty();
        }
    }

    public sealed class Handler(
        ISloDefinitionRepository sloRepository,
        IErrorBudgetSnapshotRepository budgetRepository,
        ICurrentTenant tenant) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var sloId = SloDefinitionId.From(request.SloDefinitionId);
            var slo = await sloRepository.GetByIdAsync(sloId, tenant.Id, cancellationToken);

            if (slo is null)
            {
                Result<Response> notFound = Error.NotFound("Reliability.SloNotFound",
                    "SLO definition '{0}' not found", request.SloDefinitionId);
                return notFound;
            }

            var snapshot = await budgetRepository.GetLatestAsync(sloId, tenant.Id, cancellationToken);

            if (snapshot is null)
            {
                return Result<Response>.Success(new Response(
                    SloDefinitionId: slo.Id.Value,
                    SloName: slo.Name,
                    ServiceId: slo.ServiceId,
                    Environment: slo.Environment,
                    TargetPercent: slo.TargetPercent,
                    WindowDays: slo.WindowDays,
                    TotalBudgetMinutes: null,
                    ConsumedBudgetMinutes: null,
                    RemainingBudgetMinutes: null,
                    ConsumedPercent: null,
                    Status: SloStatus.Healthy,
                    ComputedAt: null));
            }

            return Result<Response>.Success(new Response(
                SloDefinitionId: slo.Id.Value,
                SloName: slo.Name,
                ServiceId: slo.ServiceId,
                Environment: slo.Environment,
                TargetPercent: slo.TargetPercent,
                WindowDays: slo.WindowDays,
                TotalBudgetMinutes: snapshot.TotalBudgetMinutes,
                ConsumedBudgetMinutes: snapshot.ConsumedBudgetMinutes,
                RemainingBudgetMinutes: snapshot.RemainingBudgetMinutes,
                ConsumedPercent: snapshot.ConsumedPercent,
                Status: snapshot.Status,
                ComputedAt: snapshot.ComputedAt));
        }
    }

    public sealed record Response(
        Guid SloDefinitionId,
        string SloName,
        string ServiceId,
        string Environment,
        decimal TargetPercent,
        int WindowDays,
        decimal? TotalBudgetMinutes,
        decimal? ConsumedBudgetMinutes,
        decimal? RemainingBudgetMinutes,
        decimal? ConsumedPercent,
        SloStatus Status,
        DateTimeOffset? ComputedAt);
}
