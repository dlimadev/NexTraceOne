using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Enums;
using NexTraceOne.Catalog.Domain.Contracts.Errors;

namespace NexTraceOne.Catalog.Application.Contracts.Features.EvaluateContractCompliance;

/// <summary>
/// Feature: EvaluateContractCompliance — avalia uma versão de contrato contra um gate de compliance.
/// Regista o resultado (Pass/Warn/Block) e as violações detetadas.
/// Estrutura VSA: Command + Validator + Handler + Response em arquivo único.
/// </summary>
public static class EvaluateContractCompliance
{
    /// <summary>Comando para avaliar compliance de uma versão de contrato.</summary>
    public sealed record Command(
        Guid GateId,
        string ContractVersionId,
        string? ChangeId,
        ComplianceEvaluationResult Result,
        string? Violations,
        string? EvidencePackId,
        string? TenantId) : ICommand<Response>;

    /// <summary>Valida os parâmetros do comando de avaliação de compliance.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.GateId).NotEmpty();
            RuleFor(x => x.ContractVersionId).NotEmpty().MaximumLength(200);
            RuleFor(x => x.ChangeId).MaximumLength(200).When(x => x.ChangeId is not null);
            RuleFor(x => x.Result).IsInEnum();
            RuleFor(x => x.EvidencePackId).MaximumLength(200).When(x => x.EvidencePackId is not null);
        }
    }

    /// <summary>
    /// Handler que avalia uma versão de contrato contra um gate de compliance
    /// e persiste o resultado.
    /// </summary>
    public sealed class Handler(
        IContractComplianceGateRepository gateRepository,
        IContractComplianceResultRepository resultRepository,
        IContractsUnitOfWork unitOfWork,
        IDateTimeProvider clock) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var gate = await gateRepository.GetByIdAsync(
                ContractComplianceGateId.From(request.GateId), cancellationToken);

            if (gate is null)
                return ContractsErrors.ComplianceGateNotFound(request.GateId.ToString());

            var complianceResult = ContractComplianceResult.Evaluate(
                gateId: request.GateId,
                contractVersionId: request.ContractVersionId,
                changeId: request.ChangeId,
                result: request.Result,
                violations: request.Violations,
                evidencePackId: request.EvidencePackId,
                evaluatedAt: clock.UtcNow,
                tenantId: request.TenantId);

            await resultRepository.AddAsync(complianceResult, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(
                complianceResult.Id.Value,
                complianceResult.GateId,
                complianceResult.ContractVersionId,
                complianceResult.Result,
                complianceResult.Violations,
                complianceResult.EvaluatedAt);
        }
    }

    /// <summary>Resposta da avaliação de compliance contratual.</summary>
    public sealed record Response(
        Guid ResultId,
        Guid GateId,
        string ContractVersionId,
        ComplianceEvaluationResult Result,
        string? Violations,
        DateTimeOffset EvaluatedAt);
}
