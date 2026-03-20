using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Enums;

namespace NexTraceOne.OperationalIntelligence.Application.Incidents.Features.CreateIncident;

/// <summary>
/// Feature: CreateIncident — cria incidente operacional real com persistência e correlação inicial.
/// </summary>
public static class CreateIncident
{
    /// <summary>Comando de criação de incidente.</summary>
    public sealed record Command(
        string Title,
        string Description,
        IncidentType IncidentType,
        IncidentSeverity Severity,
        string ServiceId,
        string ServiceDisplayName,
        string OwnerTeam,
        string? ImpactedDomain,
        string Environment,
        DateTimeOffset? DetectedAtUtc) : ICommand<Response>;

    /// <summary>Validação do comando.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Title).NotEmpty().MaximumLength(500);
            RuleFor(x => x.Description).NotEmpty().MaximumLength(4000);
            RuleFor(x => x.ServiceId).NotEmpty().MaximumLength(200);
            RuleFor(x => x.ServiceDisplayName).NotEmpty().MaximumLength(200);
            RuleFor(x => x.OwnerTeam).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Environment).NotEmpty().MaximumLength(100);
        }
    }

    /// <summary>Handler de criação de incidente com correlação automática inicial.</summary>
    public sealed class Handler(
        IIncidentStore store,
        IIncidentCorrelationService correlationService,
        ICurrentTenant currentTenant,
        ICurrentEnvironment currentEnvironment) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            var tenantId = currentTenant.IsActive ? currentTenant.Id : (Guid?)null;
            var environmentId = currentEnvironment.IsResolved ? currentEnvironment.EnvironmentId : (Guid?)null;

            var created = store.CreateIncident(new CreateIncidentInput(
                request.Title,
                request.Description,
                request.IncidentType,
                request.Severity,
                request.ServiceId,
                request.ServiceDisplayName,
                request.OwnerTeam,
                request.ImpactedDomain,
                request.Environment,
                request.DetectedAtUtc,
                tenantId,
                environmentId));

            var correlation = await correlationService.RecomputeAsync(created.IncidentId.ToString(), cancellationToken);

            return new Response(
                created.IncidentId,
                created.Reference,
                created.CreatedAt,
                IncidentStatus.Open,
                request.Severity,
                correlation?.Confidence ?? CorrelationConfidence.NotAssessed,
                (correlation?.RelatedChanges.Count ?? 0) > 0,
                correlation?.Score ?? 0m,
                correlation?.Reason);
        }
    }

    /// <summary>Resposta de criação do incidente.</summary>
    public sealed record Response(
        Guid IncidentId,
        string Reference,
        DateTimeOffset CreatedAt,
        IncidentStatus Status,
        IncidentSeverity Severity,
        CorrelationConfidence CorrelationConfidence,
        bool HasCorrelatedChanges,
        decimal CorrelationScore,
        string? CorrelationReason);
}
