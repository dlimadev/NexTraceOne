using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Errors;
using GetIncidentCorrelationFeature = NexTraceOne.OperationalIntelligence.Application.Incidents.Features.GetIncidentCorrelation.GetIncidentCorrelation;

namespace NexTraceOne.OperationalIntelligence.Application.Incidents.Features.RefreshIncidentCorrelation;

/// <summary>
/// Feature: RefreshIncidentCorrelation — força recomputação manual da correlação de um incidente.
/// </summary>
public static class RefreshIncidentCorrelation
{
    /// <summary>Comando de refresh manual da correlação.</summary>
    public sealed record Command(string IncidentId) : ICommand<GetIncidentCorrelationFeature.Response>;

    /// <summary>Validação do comando.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.IncidentId).NotEmpty().MaximumLength(200);
        }
    }

    /// <summary>Handler de refresh manual.</summary>
    public sealed class Handler(IIncidentCorrelationService correlationService) : ICommandHandler<Command, GetIncidentCorrelationFeature.Response>
    {
        public async Task<Result<GetIncidentCorrelationFeature.Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            var refreshed = await correlationService.RecomputeAsync(request.IncidentId, cancellationToken);
            if (refreshed is null)
                return IncidentErrors.IncidentNotFound(request.IncidentId);

            return refreshed;
        }
    }
}
