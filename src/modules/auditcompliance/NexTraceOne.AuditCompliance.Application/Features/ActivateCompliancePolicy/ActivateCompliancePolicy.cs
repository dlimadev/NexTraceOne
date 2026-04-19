using Ardalis.GuardClauses;

using NexTraceOne.AuditCompliance.Application.Abstractions;
using NexTraceOne.AuditCompliance.Domain.Errors;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AuditCompliance.Application.Features.ActivateCompliancePolicy;

/// <summary>
/// Feature: ActivateCompliancePolicy — ativa uma política de compliance desativada.
///
/// Idempotente: se a política já estiver ativa, retorna sucesso sem alteração.
/// Persona primária: Platform Admin, Auditor.
/// Estrutura VSA: Command + Handler + Response em um único arquivo.
/// </summary>
public static class ActivateCompliancePolicy
{
    /// <summary>Comando para ativar uma política de compliance.</summary>
    public sealed record Command(Guid PolicyId) : ICommand<Response>;

    /// <summary>Handler que ativa a política de compliance.</summary>
    public sealed class Handler(
        ICompliancePolicyRepository policyRepository,
        IDateTimeProvider dateTimeProvider,
        IAuditComplianceUnitOfWork unitOfWork) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);
            Guard.Against.Default(request.PolicyId);

            var policyId = Domain.Entities.CompliancePolicyId.From(request.PolicyId);
            var policy = await policyRepository.GetByIdAsync(policyId, cancellationToken);

            if (policy is null)
                return AuditErrors.CompliancePolicyNotFound(request.PolicyId);

            if (!policy.IsActive)
            {
                policy.Activate(dateTimeProvider.UtcNow);
                policyRepository.Update(policy);
                await unitOfWork.CommitAsync(cancellationToken);
            }

            return new Response(policy.Id.Value, policy.IsActive, dateTimeProvider.UtcNow);
        }
    }

    /// <summary>Resposta da ativação da política.</summary>
    public sealed record Response(Guid PolicyId, bool IsActive, DateTimeOffset UpdatedAt);
}
