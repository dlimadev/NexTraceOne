using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Enums;

namespace NexTraceOne.OperationalIntelligence.Application.Automation.Features.RecordAutomationValidation;

/// <summary>
/// Feature: RecordAutomationValidation — regista o resultado de uma validação pós-execução
/// de um workflow de automação, incluindo verificações individuais e resultado observado.
/// Estrutura VSA: Command + Validator + Handler + Response em um único arquivo.
///
/// Nota: nesta fase os dados são simulados até integração completa entre módulos.
/// </summary>
public static class RecordAutomationValidation
{
    /// <summary>Comando para registar a validação pós-execução de um workflow.</summary>
    public sealed record Command(
        string WorkflowId,
        ValidationStatus Status,
        string? ObservedOutcome,
        string? ValidatedBy,
        IReadOnlyList<ValidationCheckInput>? Checks) : ICommand<Response>;

    /// <summary>Entrada de verificação individual para registo de validação.</summary>
    public sealed record ValidationCheckInput(
        string CheckName,
        bool Passed,
        string? Details);

    /// <summary>Valida os campos obrigatórios do comando de registo de validação.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.WorkflowId).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Status).IsInEnum();
            RuleFor(x => x.ObservedOutcome).MaximumLength(2000).When(x => x.ObservedOutcome is not null);
            RuleFor(x => x.ValidatedBy).MaximumLength(200).When(x => x.ValidatedBy is not null);
        }
    }

    /// <summary>Handler que regista a validação do workflow de automação com dados simulados.</summary>
    public sealed class Handler : ICommandHandler<Command, Response>
    {
        public Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            var response = new Response(
                WorkflowId: Guid.TryParse(request.WorkflowId, out var wfId) ? wfId : Guid.NewGuid(),
                ValidationStatus: request.Status,
                RecordedAt: DateTimeOffset.UtcNow);

            return Task.FromResult(Result<Response>.Success(response));
        }
    }

    /// <summary>Resposta do registo de validação do workflow de automação.</summary>
    public sealed record Response(
        Guid WorkflowId,
        ValidationStatus ValidationStatus,
        DateTimeOffset RecordedAt);
}
