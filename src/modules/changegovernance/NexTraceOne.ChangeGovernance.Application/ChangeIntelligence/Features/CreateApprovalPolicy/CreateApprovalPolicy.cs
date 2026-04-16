using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;

namespace NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.CreateApprovalPolicy;

/// <summary>
/// Feature: CreateApprovalPolicy — cria uma nova política de aprovação de releases configurável
/// por ambiente e serviço.
///
/// Estrutura VSA: Command + Validator + Handler + Response num único ficheiro.
/// </summary>
public static class CreateApprovalPolicy
{
    /// <summary>Comando para criar uma nova política de aprovação.</summary>
    public sealed record Command(
        string Name,
        string ApprovalType,
        string? EnvironmentId = null,
        Guid? ServiceId = null,
        string? ServiceTag = null,
        string? ExternalWebhookUrl = null,
        int MinApprovers = 1,
        int ExpirationHours = 48,
        bool RequireEvidencePack = false,
        bool RequireChecklistCompletion = false,
        int? MinRiskScoreForManualApproval = null,
        int Priority = 100) : ICommand<Response>;

    /// <summary>Valida a entrada do comando.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        private static readonly string[] ValidApprovalTypes =
            ["Manual", "ExternalWebhook", "ExternalServiceNow", "AutoApprove"];

        public Validator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(300);
            RuleFor(x => x.ApprovalType)
                .NotEmpty()
                .Must(t => ValidApprovalTypes.Contains(t, StringComparer.OrdinalIgnoreCase))
                .WithMessage($"ApprovalType must be one of: {string.Join(", ", ValidApprovalTypes)}.");
            RuleFor(x => x.ExternalWebhookUrl)
                .NotEmpty()
                .Must(url => Uri.TryCreate(url, UriKind.Absolute, out _))
                .When(x => x.ApprovalType == "ExternalWebhook")
                .WithMessage("A valid webhook URL is required for ExternalWebhook approval type.");
            RuleFor(x => x.MinApprovers).InclusiveBetween(1, 20);
            RuleFor(x => x.ExpirationHours).InclusiveBetween(1, 168);
            RuleFor(x => x.Priority).InclusiveBetween(1, 999);
            RuleFor(x => x.MinRiskScoreForManualApproval)
                .InclusiveBetween(1, 100)
                .When(x => x.MinRiskScoreForManualApproval.HasValue);
        }
    }

    /// <summary>Handler que persiste a nova política no repositório.</summary>
    public sealed class Handler(
        IReleaseApprovalPolicyRepository policyRepository,
        ICurrentTenant currentTenant,
        ICurrentUser currentUser,
        IDateTimeProvider clock,
        IChangeIntelligenceUnitOfWork unitOfWork) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            var policy = ReleaseApprovalPolicy.Create(
                tenantId: currentTenant.Id,
                name: request.Name,
                approvalType: request.ApprovalType,
                createdBy: currentUser.Id,
                createdAt: clock.UtcNow,
                environmentId: request.EnvironmentId,
                serviceId: request.ServiceId,
                serviceTag: request.ServiceTag,
                externalWebhookUrl: request.ExternalWebhookUrl,
                minApprovers: request.MinApprovers,
                expirationHours: request.ExpirationHours,
                requireEvidencePack: request.RequireEvidencePack,
                requireChecklistCompletion: request.RequireChecklistCompletion,
                minRiskScoreForManualApproval: request.MinRiskScoreForManualApproval,
                priority: request.Priority);

            policyRepository.Add(policy);
            await unitOfWork.CommitAsync(cancellationToken);

            return Result<Response>.Success(new Response(policy.Id.Value, policy.Name));
        }
    }

    /// <summary>Resposta com o ID e nome da política criada.</summary>
    public sealed record Response(Guid PolicyId, string Name);
}
