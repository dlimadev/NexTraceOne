using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Reliability.Abstractions;

namespace NexTraceOne.OperationalIntelligence.Application.Reliability.Features.GetServiceReliabilityCoverage;

/// <summary>
/// Feature: GetServiceReliabilityCoverage — indicadores reais de cobertura operacional de um serviço.
/// HasOperationalSignals: runtime signal dentro de 24h.
/// HasRunbook: RunbookRecord.LinkedService == serviceId.
/// HasOwner: incidente com OwnerTeam preenchido.
/// HasDependenciesMapped: false (catálogo não integrado).
/// HasRecentChangeContext: false (ChangeGovernance não integrado).
/// HasIncidentLinkage: qualquer incidente ativo.
/// </summary>
public static class GetServiceReliabilityCoverage
{
    public sealed record Query(string ServiceId) : IQuery<Response>;

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ServiceId).NotEmpty().MaximumLength(200);
        }
    }

    public sealed class Handler(
        IReliabilityRuntimeSurface runtimeSurface,
        IReliabilityIncidentSurface incidentSurface,
        IReliabilitySnapshotRepository snapshotRepository,
        ICurrentTenant tenant) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var signalTask = runtimeSurface.GetLatestSignalAsync(request.ServiceId, string.Empty, cancellationToken);
            var runbookTask = incidentSurface.HasRunbookAsync(request.ServiceId, cancellationToken);
            var incidentTask = incidentSurface.GetActiveIncidentsAsync(request.ServiceId, tenant.Id, cancellationToken);

            await Task.WhenAll(signalTask, runbookTask, incidentTask);

            var signal = signalTask.Result;
            var hasRunbook = runbookTask.Result;
            var incidents = incidentTask.Result;

            // Sinal operacional válido se capturado nas últimas 24 horas.
            var hasOperationalSignals = signal is not null &&
                signal.CapturedAt >= DateTimeOffset.UtcNow.AddHours(-24);

            var hasOwner = incidents.Any(i => !string.IsNullOrEmpty(i.TeamName));
            var hasIncidentLinkage = incidents.Count > 0;

            return Result<Response>.Success(new Response(
                request.ServiceId,
                hasOperationalSignals,
                hasRunbook,
                hasOwner,
                HasDependenciesMapped: false,
                HasRecentChangeContext: false,
                hasIncidentLinkage));
        }
    }

    public sealed record Response(
        string ServiceId,
        bool HasOperationalSignals,
        bool HasRunbook,
        bool HasOwner,
        bool HasDependenciesMapped,
        bool HasRecentChangeContext,
        bool HasIncidentLinkage);
}
