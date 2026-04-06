using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.Governance.Application.Features.RecordTechnicalDebt;

/// <summary>
/// Feature: RecordTechnicalDebt — regista um item de dívida técnica para um serviço.
/// Computa automaticamente o score de risco com base na severidade declarada.
///
/// Owner: módulo Governance.
/// Pilar: Service Governance — tracking de dívida técnica com scoring e correlação com incidentes.
/// </summary>
public static class RecordTechnicalDebt
{
    /// <summary>Comando para registar um novo item de dívida técnica.</summary>
    public sealed record Command(
        string ServiceName,
        string DebtType,
        string Title,
        string Description,
        string Severity,
        int EstimatedEffortDays,
        string? Tags) : ICommand<Response>;

    /// <summary>Validação do comando de registo de dívida técnica.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        private static readonly string[] ValidDebtTypes =
            ["architecture", "code-quality", "security", "dependency", "documentation", "testing", "performance", "infrastructure"];

        private static readonly string[] ValidSeverities =
            ["critical", "high", "medium", "low"];

        public Validator()
        {
            RuleFor(x => x.ServiceName).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Description).NotEmpty().MaximumLength(1000);
            RuleFor(x => x.DebtType)
                .Must(t => ValidDebtTypes.Contains(t))
                .WithMessage($"DebtType must be one of: {string.Join(", ", ValidDebtTypes)}");
            RuleFor(x => x.Severity)
                .Must(s => ValidSeverities.Contains(s))
                .WithMessage($"Severity must be one of: {string.Join(", ", ValidSeverities)}");
            RuleFor(x => x.EstimatedEffortDays).InclusiveBetween(1, 999);
        }
    }

    /// <summary>Handler que gera o identificador e computa o score de dívida técnica.</summary>
    public sealed class Handler(IDateTimeProvider clock) : ICommandHandler<Command, Response>
    {
        public Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            var debtId = Guid.NewGuid();
            var debtScore = ComputeDebtScore(request.Severity);
            var now = clock.UtcNow;

            return Task.FromResult(Result<Response>.Success(new Response(
                DebtId: debtId,
                ServiceName: request.ServiceName,
                DebtType: request.DebtType,
                Title: request.Title,
                Severity: request.Severity,
                EstimatedEffortDays: request.EstimatedEffortDays,
                DebtScore: debtScore,
                CreatedAt: now)));
        }

        private static int ComputeDebtScore(string severity) => severity switch
        {
            "critical" => 40,
            "high" => 25,
            "medium" => 10,
            "low" => 5,
            _ => 0
        };
    }

    /// <summary>Resposta com o identificador do item registado e o score computado.</summary>
    public sealed record Response(
        Guid DebtId,
        string ServiceName,
        string DebtType,
        string Title,
        string Severity,
        int EstimatedEffortDays,
        int DebtScore,
        DateTimeOffset CreatedAt);
}
