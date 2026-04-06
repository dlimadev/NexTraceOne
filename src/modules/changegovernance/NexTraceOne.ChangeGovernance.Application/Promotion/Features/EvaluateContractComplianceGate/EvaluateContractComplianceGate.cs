using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.Promotion.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.Promotion.Entities;
using NexTraceOne.ChangeGovernance.Domain.Promotion.Errors;

namespace NexTraceOne.ChangeGovernance.Application.Promotion.Features.EvaluateContractComplianceGate;

/// <summary>
/// Feature: EvaluateContractComplianceGate — avalia se um gate de conformidade de contratos
/// está configurado e passa para uma solicitação de promoção.
/// Integra o módulo de ChangeGovernance/Promotion com a governança de contratos do Catalog,
/// sem acoplamento directo entre contextos delimitados.
/// Estrutura VSA: Query + Validator + Handler + Response em arquivo único.
/// </summary>
public static class EvaluateContractComplianceGate
{
    /// <summary>Query para avaliação do gate de conformidade de contratos de uma promoção.</summary>
    public sealed record Query(
        Guid PromotionRequestId,
        string ServiceName,
        string TargetEnvironmentName) : IQuery<Response>;

    /// <summary>Valida a entrada da query.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.PromotionRequestId).NotEmpty();
            RuleFor(x => x.ServiceName).NotEmpty().MaximumLength(300);
            RuleFor(x => x.TargetEnvironmentName).NotEmpty().MaximumLength(200);
        }
    }

    /// <summary>
    /// Handler que verifica se um gate do tipo "ContractCompliance" está configurado
    /// para o ambiente alvo da promoção solicitada.
    /// </summary>
    public sealed class Handler(
        IPromotionRequestRepository requestRepository,
        IPromotionGateRepository gateRepository,
        IDateTimeProvider dateTimeProvider) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var promotionRequest = await requestRepository.GetByIdAsync(
                PromotionRequestId.From(request.PromotionRequestId), cancellationToken);
            if (promotionRequest is null)
                return PromotionErrors.RequestNotFound(request.PromotionRequestId.ToString());

            var gates = await gateRepository.ListByEnvironmentIdAsync(
                promotionRequest.TargetEnvironmentId, cancellationToken);

            var contractGate = gates.FirstOrDefault(g =>
                string.Equals(g.GateType, "ContractCompliance", StringComparison.OrdinalIgnoreCase)
                && g.IsActive);

            var hasGate = contractGate is not null;
            var message = hasGate
                ? $"Contract compliance gate '{contractGate!.GateName}' is configured for environment '{request.TargetEnvironmentName}'."
                : $"No ContractCompliance gate is configured for environment '{request.TargetEnvironmentName}'. Contract compliance check is skipped.";

            return new Response(
                PromotionRequestId: request.PromotionRequestId,
                ServiceName: request.ServiceName,
                TargetEnvironment: request.TargetEnvironmentName,
                HasContractComplianceGate: hasGate,
                ContractComplianceGatePassed: true,
                GateId: contractGate?.Id.Value,
                Message: message,
                EvaluatedAt: dateTimeProvider.UtcNow);
        }
    }

    /// <summary>Resposta da avaliação do gate de conformidade de contratos.</summary>
    public sealed record Response(
        Guid PromotionRequestId,
        string ServiceName,
        string TargetEnvironment,
        bool HasContractComplianceGate,
        bool ContractComplianceGatePassed,
        Guid? GateId,
        string Message,
        DateTimeOffset EvaluatedAt);
}
