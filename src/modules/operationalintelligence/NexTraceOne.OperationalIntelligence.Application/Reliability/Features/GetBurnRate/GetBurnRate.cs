using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Reliability.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Reliability.Entities;
using NexTraceOne.OperationalIntelligence.Domain.Reliability.Enums;

namespace NexTraceOne.OperationalIntelligence.Application.Reliability.Features.GetBurnRate;

/// <summary>
/// Feature: GetBurnRate — consulta o burn rate actual de um SLO por janela de tempo.
/// Retorna o snapshot mais recente de burn rate para a janela especificada.
/// </summary>
public static class GetBurnRate
{
    public sealed record Query(Guid SloDefinitionId, BurnRateWindow Window) : IQuery<Response>;

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.SloDefinitionId).NotEmpty();
            RuleFor(x => x.Window).IsInEnum();
        }
    }

    public sealed class Handler(
        ISloDefinitionRepository sloRepository,
        IBurnRateSnapshotRepository burnRateRepository,
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

            var snapshot = await burnRateRepository.GetLatestAsync(sloId, request.Window, tenant.Id, cancellationToken);

            if (snapshot is null)
            {
                return Result<Response>.Success(new Response(
                    SloDefinitionId: slo.Id.Value,
                    SloName: slo.Name,
                    ServiceId: slo.ServiceId,
                    Environment: slo.Environment,
                    Window: request.Window,
                    BurnRate: null,
                    ObservedErrorRate: null,
                    ToleratedErrorRate: null,
                    Status: SloStatus.Healthy,
                    ComputedAt: null));
            }

            return Result<Response>.Success(new Response(
                SloDefinitionId: slo.Id.Value,
                SloName: slo.Name,
                ServiceId: slo.ServiceId,
                Environment: slo.Environment,
                Window: snapshot.Window,
                BurnRate: snapshot.BurnRate,
                ObservedErrorRate: snapshot.ObservedErrorRate,
                ToleratedErrorRate: snapshot.ToleratedErrorRate,
                Status: snapshot.Status,
                ComputedAt: snapshot.ComputedAt));
        }
    }

    public sealed record Response(
        Guid SloDefinitionId,
        string SloName,
        string ServiceId,
        string Environment,
        BurnRateWindow Window,
        decimal? BurnRate,
        decimal? ObservedErrorRate,
        decimal? ToleratedErrorRate,
        SloStatus Status,
        DateTimeOffset? ComputedAt);
}
