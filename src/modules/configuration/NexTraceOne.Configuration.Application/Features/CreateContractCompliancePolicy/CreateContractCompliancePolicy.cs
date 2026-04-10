using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Domain.Entities;
using NexTraceOne.Configuration.Domain.Enums;

namespace NexTraceOne.Configuration.Application.Features.CreateContractCompliancePolicy;

/// <summary>
/// Feature: CreateContractCompliancePolicy — cria uma política de compliance contratual
/// configurável por âmbito (organização, equipa, ambiente ou serviço).
/// Estrutura VSA: Command + Validator + Handler + Response em arquivo único.
/// </summary>
public static class CreateContractCompliancePolicy
{
    /// <summary>Comando para criar uma política de compliance contratual.</summary>
    public sealed record Command(
        string Name,
        string Description,
        int Scope,
        string? ScopeId,
        int VerificationMode,
        int VerificationApproach,
        int OnBreakingChange,
        int OnNonBreakingChange,
        int OnNewEndpoint,
        int OnRemovedEndpoint,
        int OnMissingContract,
        int OnContractNotApproved,
        bool AutoGenerateChangelog,
        bool RequireChangelogApproval,
        bool EnforceCdct,
        int CdctFailureAction,
        bool EnableRuntimeDriftDetection,
        int DriftDetectionIntervalMinutes,
        decimal DriftThresholdForAlert,
        decimal DriftThresholdForIncident,
        bool NotifyOnVerificationFailure,
        bool NotifyOnBreakingChange,
        bool NotifyOnDriftDetected,
        string NotificationChannels) : ICommand<Response>;

    /// <summary>Valida os parâmetros do comando de criação de política de compliance.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Description).MaximumLength(2000);
            RuleFor(x => x.Scope).IsInEnum();
            RuleFor(x => x.VerificationMode).IsInEnum();
            RuleFor(x => x.VerificationApproach).IsInEnum();
            RuleFor(x => x.OnBreakingChange).IsInEnum();
            RuleFor(x => x.OnNonBreakingChange).IsInEnum();
            RuleFor(x => x.OnNewEndpoint).IsInEnum();
            RuleFor(x => x.OnRemovedEndpoint).IsInEnum();
            RuleFor(x => x.OnMissingContract).IsInEnum();
            RuleFor(x => x.OnContractNotApproved).IsInEnum();
            RuleFor(x => x.CdctFailureAction).IsInEnum();
            RuleFor(x => x.DriftDetectionIntervalMinutes).GreaterThan(0);
            RuleFor(x => x.DriftThresholdForAlert).GreaterThanOrEqualTo(0);
            RuleFor(x => x.DriftThresholdForIncident).GreaterThanOrEqualTo(0);
        }
    }

    /// <summary>
    /// Handler que cria uma nova política de compliance contratual e persiste no repositório.
    /// Aplica configurações adicionais (changelog, CDCT, drift, notificações) após criação base.
    /// </summary>
    public sealed class Handler(
        IContractCompliancePolicyRepository repository,
        ICurrentUser currentUser,
        ICurrentTenant currentTenant,
        IDateTimeProvider clock) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            if (!currentUser.IsAuthenticated)
                return Error.Unauthorized("User.NotAuthenticated", "User must be authenticated.");

            var now = clock.UtcNow;

            var policy = ContractCompliancePolicy.Create(
                tenantId: currentTenant.Id.ToString(),
                name: request.Name,
                description: request.Description,
                scope: (PolicyScope)request.Scope,
                scopeId: request.ScopeId,
                verificationMode: (VerificationMode)request.VerificationMode,
                verificationApproach: (VerificationApproach)request.VerificationApproach,
                onBreakingChange: (ComplianceAction)request.OnBreakingChange,
                onNonBreakingChange: (ComplianceAction)request.OnNonBreakingChange,
                onNewEndpoint: (ComplianceAction)request.OnNewEndpoint,
                onRemovedEndpoint: (ComplianceAction)request.OnRemovedEndpoint,
                onMissingContract: (ComplianceAction)request.OnMissingContract,
                onContractNotApproved: (ComplianceAction)request.OnContractNotApproved,
                createdAt: now);

            policy.Update(
                name: request.Name,
                description: request.Description,
                scope: (PolicyScope)request.Scope,
                scopeId: request.ScopeId,
                verificationMode: (VerificationMode)request.VerificationMode,
                verificationApproach: (VerificationApproach)request.VerificationApproach,
                onBreakingChange: (ComplianceAction)request.OnBreakingChange,
                onNonBreakingChange: (ComplianceAction)request.OnNonBreakingChange,
                onNewEndpoint: (ComplianceAction)request.OnNewEndpoint,
                onRemovedEndpoint: (ComplianceAction)request.OnRemovedEndpoint,
                onMissingContract: (ComplianceAction)request.OnMissingContract,
                onContractNotApproved: (ComplianceAction)request.OnContractNotApproved,
                autoGenerateChangelog: request.AutoGenerateChangelog,
                changelogFormat: 0,
                requireChangelogApproval: request.RequireChangelogApproval,
                enforceCdct: request.EnforceCdct,
                cdctFailureAction: (ComplianceAction)request.CdctFailureAction,
                enableRuntimeDriftDetection: request.EnableRuntimeDriftDetection,
                driftDetectionIntervalMinutes: request.DriftDetectionIntervalMinutes,
                driftThresholdForAlert: request.DriftThresholdForAlert,
                driftThresholdForIncident: request.DriftThresholdForIncident,
                notifyOnVerificationFailure: request.NotifyOnVerificationFailure,
                notifyOnBreakingChange: request.NotifyOnBreakingChange,
                notifyOnDriftDetected: request.NotifyOnDriftDetected,
                notificationChannels: request.NotificationChannels);

            await repository.AddAsync(policy, cancellationToken);

            return new Response(policy.Id.Value, policy.Name, now);
        }
    }

    /// <summary>Resposta da criação de política de compliance contratual.</summary>
    public sealed record Response(Guid PolicyId, string Name, DateTimeOffset CreatedAt);
}
