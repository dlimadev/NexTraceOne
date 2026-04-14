using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Entities;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Application.Features.AssessServiceMaturity;

/// <summary>
/// Feature: AssessServiceMaturity — avalia ou reavalia a maturidade de um serviço.
/// Se já existir uma avaliação para o serviço, executa reavaliação.
/// Se não existir, cria nova avaliação.
/// O nível é derivado automaticamente a partir dos critérios fornecidos.
///
/// Owner: módulo Governance.
/// Pilar: Service Governance — tracking de maturidade com critérios cumulativos.
/// </summary>
public static class AssessServiceMaturity
{
    /// <summary>Comando para avaliar ou reavaliar a maturidade de um serviço.</summary>
    public sealed record Command(
        Guid ServiceId,
        string ServiceName,
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
        string AssessedBy = "auto",
        string? TenantId = null) : ICommand<Response>;

    /// <summary>Validação do comando de avaliação de maturidade.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ServiceId).NotEmpty();
            RuleFor(x => x.ServiceName).NotEmpty().MaximumLength(200);
            RuleFor(x => x.AssessedBy).NotEmpty().MaximumLength(200);
        }
    }

    /// <summary>Handler que cria ou reavalia a maturidade de um serviço.</summary>
    public sealed class Handler(
        IServiceMaturityAssessmentRepository repository,
        IGovernanceUnitOfWork unitOfWork,
        IDateTimeProvider clock) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            var now = clock.UtcNow;

            var existing = await repository.GetByServiceIdAsync(request.ServiceId, cancellationToken);

            if (existing is not null)
            {
                existing.Reassess(
                    ownershipDefined: request.OwnershipDefined,
                    contractsPublished: request.ContractsPublished,
                    documentationExists: request.DocumentationExists,
                    policiesApplied: request.PoliciesApplied,
                    approvalWorkflowActive: request.ApprovalWorkflowActive,
                    telemetryActive: request.TelemetryActive,
                    baselinesEstablished: request.BaselinesEstablished,
                    alertsConfigured: request.AlertsConfigured,
                    runbooksAvailable: request.RunbooksAvailable,
                    rollbackTested: request.RollbackTested,
                    chaosValidated: request.ChaosValidated,
                    now: now);

                await repository.UpdateAsync(existing, cancellationToken);
                await unitOfWork.CommitAsync(cancellationToken);

                return Result<Response>.Success(new Response(
                    AssessmentId: existing.Id.Value,
                    ServiceId: existing.ServiceId,
                    ServiceName: existing.ServiceName,
                    CurrentLevel: existing.CurrentLevel,
                    ReassessmentCount: existing.ReassessmentCount,
                    IsReassessment: true,
                    AssessedAt: existing.AssessedAt,
                    LastReassessedAt: existing.LastReassessedAt));
            }

            var assessment = ServiceMaturityAssessment.Assess(
                serviceId: request.ServiceId,
                serviceName: request.ServiceName,
                ownershipDefined: request.OwnershipDefined,
                contractsPublished: request.ContractsPublished,
                documentationExists: request.DocumentationExists,
                policiesApplied: request.PoliciesApplied,
                approvalWorkflowActive: request.ApprovalWorkflowActive,
                telemetryActive: request.TelemetryActive,
                baselinesEstablished: request.BaselinesEstablished,
                alertsConfigured: request.AlertsConfigured,
                runbooksAvailable: request.RunbooksAvailable,
                rollbackTested: request.RollbackTested,
                chaosValidated: request.ChaosValidated,
                assessedBy: request.AssessedBy,
                tenantId: request.TenantId,
                now: now);

            await repository.AddAsync(assessment, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            return Result<Response>.Success(new Response(
                AssessmentId: assessment.Id.Value,
                ServiceId: assessment.ServiceId,
                ServiceName: assessment.ServiceName,
                CurrentLevel: assessment.CurrentLevel,
                ReassessmentCount: assessment.ReassessmentCount,
                IsReassessment: false,
                AssessedAt: assessment.AssessedAt,
                LastReassessedAt: assessment.LastReassessedAt));
        }
    }

    /// <summary>Resposta com o resultado da avaliação de maturidade.</summary>
    public sealed record Response(
        Guid AssessmentId,
        Guid ServiceId,
        string ServiceName,
        ServiceMaturityLevel CurrentLevel,
        int ReassessmentCount,
        bool IsReassessment,
        DateTimeOffset AssessedAt,
        DateTimeOffset? LastReassessedAt);
}
