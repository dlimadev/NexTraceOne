using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Entities;

namespace NexTraceOne.Governance.Application.Features.RecordTechnicalDebt;

/// <summary>
/// Feature: RecordTechnicalDebt — regista um item de dívida técnica para um serviço.
/// Persiste na base de dados e computa automaticamente o score de risco.
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
        string? Tags,
        string TenantId = "default") : ICommand<Response>;

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

    /// <summary>Handler que cria e persiste um novo item de dívida técnica.</summary>
    public sealed class Handler(
        ITechnicalDebtRepository repository,
        IGovernanceUnitOfWork unitOfWork,
        IDateTimeProvider clock) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            var now = clock.UtcNow;

            var item = TechnicalDebtItem.Create(
                serviceName: request.ServiceName,
                debtType: request.DebtType,
                title: request.Title,
                description: request.Description,
                severity: request.Severity,
                estimatedEffortDays: request.EstimatedEffortDays,
                tags: request.Tags,
                tenantId: request.TenantId,
                now: now);

            await repository.AddAsync(item, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            return Result<Response>.Success(new Response(
                DebtId: item.Id.Value,
                ServiceName: item.ServiceName,
                DebtType: item.DebtType,
                Title: item.Title,
                Severity: item.Severity,
                EstimatedEffortDays: item.EstimatedEffortDays,
                DebtScore: item.DebtScore,
                CreatedAt: item.CreatedAt));
        }
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
