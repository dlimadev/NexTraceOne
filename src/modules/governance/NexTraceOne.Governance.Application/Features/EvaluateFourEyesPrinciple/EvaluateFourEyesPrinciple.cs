using Ardalis.GuardClauses;
using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Domain.Enums;

namespace NexTraceOne.Governance.Application.Features.EvaluateFourEyesPrinciple;

/// <summary>
/// Feature: EvaluateFourEyesPrinciple — avalia se uma ação crítica requer
/// confirmação de um segundo utilizador autorizado (princípio dos quatro olhos).
/// Consulta parâmetros:
///   - governance.four_eyes_principle.enabled
///   - governance.four_eyes_principle.actions
/// Implementa Separation of Duties para ambientes regulados.
/// </summary>
public static class EvaluateFourEyesPrinciple
{
    /// <summary>Query para avaliar se uma ação requer dupla aprovação.</summary>
    public sealed record Query(
        string ActionCode,
        string RequestedBy,
        string? ApprovedBy) : IQuery<Response>;

    /// <summary>Validação da query.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ActionCode).NotEmpty().MaximumLength(100);
            RuleFor(x => x.RequestedBy).NotEmpty().MaximumLength(500);
        }
    }

    /// <summary>Handler que avalia o princípio dos quatro olhos.</summary>
    public sealed class Handler(
        IConfigurationResolutionService configService,
        IDateTimeProvider dateTimeProvider) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            // Check if four eyes principle is enabled
            var enabledConfig = await configService.ResolveEffectiveValueAsync(
                "governance.four_eyes_principle.enabled",
                ConfigurationScope.Tenant,
                null,
                cancellationToken);

            var isEnabled = enabledConfig?.EffectiveValue == "true";

            if (!isEnabled)
            {
                return new Response(
                    ActionCode: request.ActionCode,
                    FourEyesRequired: false,
                    IsCompliant: true,
                    Reason: "Four eyes principle is not enabled",
                    RequiresSecondApprover: false,
                    EvaluatedAt: dateTimeProvider.UtcNow);
            }

            // Check if this specific action requires four eyes
            var actionsConfig = await configService.ResolveEffectiveValueAsync(
                "governance.four_eyes_principle.actions",
                ConfigurationScope.Tenant,
                null,
                cancellationToken);

            var actionsList = actionsConfig?.EffectiveValue ?? "[]";
            var isActionCovered = actionsList.Contains($"\"{request.ActionCode}\"", StringComparison.OrdinalIgnoreCase);

            if (!isActionCovered)
            {
                return new Response(
                    ActionCode: request.ActionCode,
                    FourEyesRequired: false,
                    IsCompliant: true,
                    Reason: $"Action '{request.ActionCode}' is not subject to four eyes principle",
                    RequiresSecondApprover: false,
                    EvaluatedAt: dateTimeProvider.UtcNow);
            }

            // Four eyes is required for this action — check if second approver is provided and different
            var hasSecondApprover = !string.IsNullOrWhiteSpace(request.ApprovedBy);
            var isSamePerson = string.Equals(request.RequestedBy, request.ApprovedBy, StringComparison.OrdinalIgnoreCase);

            if (!hasSecondApprover)
            {
                return new Response(
                    ActionCode: request.ActionCode,
                    FourEyesRequired: true,
                    IsCompliant: false,
                    Reason: "Action requires approval from a second authorized user (four eyes principle)",
                    RequiresSecondApprover: true,
                    EvaluatedAt: dateTimeProvider.UtcNow);
            }

            if (isSamePerson)
            {
                return new Response(
                    ActionCode: request.ActionCode,
                    FourEyesRequired: true,
                    IsCompliant: false,
                    Reason: "Approver must be different from requester (separation of duties)",
                    RequiresSecondApprover: true,
                    EvaluatedAt: dateTimeProvider.UtcNow);
            }

            return new Response(
                ActionCode: request.ActionCode,
                FourEyesRequired: true,
                IsCompliant: true,
                Reason: $"Four eyes principle satisfied: requested by '{request.RequestedBy}', approved by '{request.ApprovedBy}'",
                RequiresSecondApprover: false,
                EvaluatedAt: dateTimeProvider.UtcNow);
        }
    }

    /// <summary>Resposta da avaliação do princípio dos quatro olhos.</summary>
    public sealed record Response(
        string ActionCode,
        bool FourEyesRequired,
        bool IsCompliant,
        string Reason,
        bool RequiresSecondApprover,
        DateTimeOffset EvaluatedAt);
}
