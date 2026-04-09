using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Entities;
using NexTraceOne.Governance.Domain.Enums;
using NexTraceOne.Governance.Domain.Errors;

namespace NexTraceOne.Governance.Application.Features.GetServiceMaturity;

/// <summary>
/// Feature: GetServiceMaturity — obtém a avaliação de maturidade de um serviço pelo ServiceId.
/// Retorna todos os critérios e o nível derivado.
///
/// Owner: módulo Governance.
/// Pilar: Service Governance — consulta de maturidade por serviço.
/// </summary>
public static class GetServiceMaturity
{
    /// <summary>Query para obter a avaliação de maturidade de um serviço.</summary>
    public sealed record Query(Guid ServiceId) : IQuery<Response>;

    /// <summary>Validação da query.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ServiceId).NotEmpty();
        }
    }

    /// <summary>Handler que obtém a avaliação de maturidade de um serviço.</summary>
    public sealed class Handler(
        IServiceMaturityAssessmentRepository repository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var assessment = await repository.GetByServiceIdAsync(request.ServiceId, cancellationToken);

            if (assessment is null)
                return GovernanceMaturityErrors.ServiceAssessmentNotFound(request.ServiceId.ToString());

            return Result<Response>.Success(new Response(
                AssessmentId: assessment.Id.Value,
                ServiceId: assessment.ServiceId,
                ServiceName: assessment.ServiceName,
                CurrentLevel: assessment.CurrentLevel,
                OwnershipDefined: assessment.OwnershipDefined,
                ContractsPublished: assessment.ContractsPublished,
                DocumentationExists: assessment.DocumentationExists,
                PoliciesApplied: assessment.PoliciesApplied,
                ApprovalWorkflowActive: assessment.ApprovalWorkflowActive,
                TelemetryActive: assessment.TelemetryActive,
                BaselinesEstablished: assessment.BaselinesEstablished,
                AlertsConfigured: assessment.AlertsConfigured,
                RunbooksAvailable: assessment.RunbooksAvailable,
                RollbackTested: assessment.RollbackTested,
                ChaosValidated: assessment.ChaosValidated,
                AssessedAt: assessment.AssessedAt,
                AssessedBy: assessment.AssessedBy,
                LastReassessedAt: assessment.LastReassessedAt,
                ReassessmentCount: assessment.ReassessmentCount));
        }
    }

    /// <summary>Resposta com todos os critérios e nível de maturidade do serviço.</summary>
    public sealed record Response(
        Guid AssessmentId,
        Guid ServiceId,
        string ServiceName,
        ServiceMaturityLevel CurrentLevel,
        bool OwnershipDefined,
        bool ContractsPublished,
        bool DocumentationExists,
        bool PoliciesApplied,
        bool ApprovalWorkflowActive,
        bool TelemetryActive,
        bool BaselinesEstablished,
        bool AlertsConfigured,
        bool RunbooksAvailable,
        bool RollbackTested,
        bool ChaosValidated,
        DateTimeOffset AssessedAt,
        string AssessedBy,
        DateTimeOffset? LastReassessedAt,
        int ReassessmentCount);
}
