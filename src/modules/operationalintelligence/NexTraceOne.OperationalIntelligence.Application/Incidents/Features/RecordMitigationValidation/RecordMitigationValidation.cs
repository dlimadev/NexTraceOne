using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Enums;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Errors;

namespace NexTraceOne.OperationalIntelligence.Application.Incidents.Features.RecordMitigationValidation;

/// <summary>
/// Feature: RecordMitigationValidation — regista o resultado de uma validação pós-mitigação,
/// incluindo o estado, resultado observado e verificações individuais.
/// </summary>
public static class RecordMitigationValidation
{
    /// <summary>Comando para registar a validação de um workflow de mitigação.</summary>
    public sealed record Command(
        string IncidentId,
        string WorkflowId,
        ValidationStatus Status,
        string? ObservedOutcome,
        string? ValidatedBy,
        IReadOnlyList<ValidationCheckInput>? Checks) : ICommand<Response>;

    /// <summary>Entrada de verificação individual para registo de validação.</summary>
    public sealed record ValidationCheckInput(string CheckName, bool IsPassed, string? ObservedValue);

    /// <summary>Valida os campos obrigatórios do comando de registo de validação.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.IncidentId).NotEmpty().MaximumLength(200);
            RuleFor(x => x.WorkflowId).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Status).IsInEnum();
        }
    }

    /// <summary>Handler que regista a validação do workflow de mitigação via store.</summary>
    public sealed class Handler(IIncidentStore store) : ICommandHandler<Command, Response>
    {
        public Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            if (!store.IncidentExists(request.IncidentId))
                return Task.FromResult<Result<Response>>(IncidentErrors.IncidentNotFound(request.IncidentId));

            var response = store.RecordMitigationValidation(
                request.IncidentId,
                request.WorkflowId,
                request.Status,
                request.ObservedOutcome,
                request.ValidatedBy,
                request.Checks);

            if (response is null)
                return Task.FromResult<Result<Response>>(IncidentErrors.IncidentNotFound(request.IncidentId));

            return Task.FromResult(Result<Response>.Success(response));
        }
    }

    /// <summary>Resposta do registo de validação do workflow de mitigação.</summary>
    public sealed record Response(
        Guid WorkflowId,
        ValidationStatus Status,
        DateTimeOffset ValidatedAt);
}
