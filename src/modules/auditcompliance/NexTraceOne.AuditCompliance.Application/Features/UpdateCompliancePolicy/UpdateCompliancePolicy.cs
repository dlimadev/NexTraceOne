using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.AuditCompliance.Application.Abstractions;
using NexTraceOne.AuditCompliance.Domain.Errors;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AuditCompliance.Application.Features.UpdateCompliancePolicy;

/// <summary>
/// Feature: UpdateCompliancePolicy — atualiza os dados de uma política de compliance existente.
///
/// Atualiza: DisplayName, Description, Category, Severity, EvaluationCriteria.
/// O Name interno permanece imutável após criação (é o identificador de negócio estável).
///
/// Persona primária: Platform Admin, Auditor.
/// Estrutura VSA: Command + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class UpdateCompliancePolicy
{
    /// <summary>Comando para atualizar uma política de compliance.</summary>
    public sealed record Command(
        Guid PolicyId,
        string DisplayName,
        string? Description,
        string Category,
        string Severity,
        string? EvaluationCriteria) : ICommand<Response>;

    /// <summary>Valida os parâmetros do comando.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        private static readonly string[] AllowedSeverities = ["Low", "Medium", "High", "Critical"];

        public Validator()
        {
            RuleFor(x => x.PolicyId).NotEmpty();
            RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(500);
            RuleFor(x => x.Category).NotEmpty().MaximumLength(100);
            RuleFor(x => x.Severity)
                .NotEmpty()
                .Must(s => AllowedSeverities.Contains(s, StringComparer.OrdinalIgnoreCase))
                .WithMessage($"Severity must be one of: {string.Join(", ", AllowedSeverities)}");
            RuleFor(x => x.EvaluationCriteria).MaximumLength(2000).When(x => x.EvaluationCriteria is not null);
        }
    }

    /// <summary>Handler que atualiza a política de compliance.</summary>
    public sealed class Handler(
        ICompliancePolicyRepository policyRepository,
        IDateTimeProvider dateTimeProvider,
        IAuditComplianceUnitOfWork unitOfWork) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var policyId = Domain.Entities.CompliancePolicyId.From(request.PolicyId);
            var policy = await policyRepository.GetByIdAsync(policyId, cancellationToken);

            if (policy is null)
                return AuditErrors.CompliancePolicyNotFound(request.PolicyId);

            if (!Enum.TryParse<Domain.Enums.ComplianceSeverity>(request.Severity, ignoreCase: true, out var severity))
                return AuditErrors.CompliancePolicyNotFound(request.PolicyId);

            policy.Update(
                request.DisplayName,
                request.Description,
                request.Category,
                severity,
                request.EvaluationCriteria,
                dateTimeProvider.UtcNow);

            policyRepository.Update(policy);
            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(policy.Id.Value, policy.DisplayName, policy.Category, policy.Severity.ToString(), policy.IsActive, dateTimeProvider.UtcNow);
        }
    }

    /// <summary>Resposta da atualização da política.</summary>
    public sealed record Response(
        Guid PolicyId,
        string DisplayName,
        string Category,
        string Severity,
        bool IsActive,
        DateTimeOffset UpdatedAt);
}
