using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Enums;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Errors;

namespace NexTraceOne.OperationalIntelligence.Application.Incidents.Features.ResolveIncident;

/// <summary>
/// Feature: ResolveIncident — marca um incidente operacional como resolvido.
/// Usado pela Ingestion API para receber confirmação de restauração de serviço
/// proveniente de sistemas externos (PagerDuty, OpsGenie, Alertmanager, pipelines de remediação).
/// A operação é idempotente: incidentes já resolvidos ou encerrados não são alterados.
/// Estrutura VSA: Command + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class ResolveIncident
{
    /// <summary>Comando para marcar um incidente como resolvido.</summary>
    public sealed record Command(
        string IncidentId,
        DateTimeOffset? ResolvedAtUtc,
        string? ResolutionNote) : ICommand<Response>;

    /// <summary>Valida o comando de resolução do incidente.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.IncidentId).NotEmpty().MaximumLength(200);
            RuleFor(x => x.ResolutionNote).MaximumLength(2000).When(x => x.ResolutionNote is not null);
        }
    }

    /// <summary>
    /// Handler que resolve o incidente via IIncidentStore e retorna o estado actualizado.
    /// Usa IDateTimeProvider para garantir que datas são sempre UTC e auditáveis.
    /// </summary>
    public sealed class Handler(
        IIncidentStore store,
        IDateTimeProvider clock) : ICommandHandler<Command, Response>
    {
        public Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            if (!store.IncidentExists(request.IncidentId))
                return Task.FromResult<Result<Response>>(IncidentErrors.IncidentNotFound(request.IncidentId));

            var resolvedAt = request.ResolvedAtUtc ?? clock.UtcNow;

            var updated = store.MarkIncidentResolved(request.IncidentId, resolvedAt);
            if (!updated)
                return Task.FromResult<Result<Response>>(IncidentErrors.IncidentNotFound(request.IncidentId));

            return Task.FromResult(Result<Response>.Success(new Response(
                IncidentId: request.IncidentId,
                Status: IncidentStatus.Resolved,
                ResolvedAt: resolvedAt,
                ResolutionNote: request.ResolutionNote)));
        }
    }

    /// <summary>Resposta da resolução do incidente com o estado actualizado.</summary>
    public sealed record Response(
        string IncidentId,
        IncidentStatus Status,
        DateTimeOffset ResolvedAt,
        string? ResolutionNote);
}
