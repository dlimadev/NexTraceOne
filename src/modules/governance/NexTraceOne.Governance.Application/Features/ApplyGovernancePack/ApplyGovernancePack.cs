using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Entities;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Application.Features.ApplyGovernancePack;

/// <summary>
/// Feature: ApplyGovernancePack — aplica um governance pack a um escopo específico.
/// Cria um GovernanceRolloutRecord real, persistido e auditável.
/// </summary>
public static class ApplyGovernancePack
{
    /// <summary>Comando para aplicar um governance pack a um escopo.</summary>
    public sealed record Command(
        string PackId,
        string ScopeType,
        string ScopeValue,
        string EnforcementMode,
        string AppliedBy) : ICommand<Response>;

    /// <summary>Handler que aplica o governance pack com persistência real.</summary>
    public sealed class Handler(
        IGovernancePackRepository packRepository,
        IGovernancePackVersionRepository versionRepository,
        IGovernanceRolloutRecordRepository rolloutRepository,
        IUnitOfWork unitOfWork) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            if (!Guid.TryParse(request.PackId, out var packGuid))
                return Error.Validation("INVALID_PACK_ID", "Pack ID '{0}' is not a valid GUID.", request.PackId);

            var pack = await packRepository.GetByIdAsync(new GovernancePackId(packGuid), cancellationToken);
            if (pack is null)
                return Error.NotFound("PACK_NOT_FOUND", "Governance pack '{0}' not found.", request.PackId);

            if (!Enum.TryParse<GovernanceScopeType>(request.ScopeType, ignoreCase: true, out var scopeType))
                return Error.Validation("INVALID_SCOPE_TYPE", "Scope type '{0}' is not valid.", request.ScopeType);

            if (!Enum.TryParse<EnforcementMode>(request.EnforcementMode, ignoreCase: true, out var enforcementMode))
                return Error.Validation("INVALID_ENFORCEMENT_MODE", "Enforcement mode '{0}' is not valid.", request.EnforcementMode);

            var latestVersion = await versionRepository.GetLatestByPackIdAsync(pack.Id, cancellationToken);
            if (latestVersion is null)
                return Error.Validation("NO_VERSION_AVAILABLE", "Governance pack '{0}' has no versions available for rollout.", request.PackId);

            var rollout = GovernanceRolloutRecord.Create(
                packId: pack.Id,
                versionId: latestVersion.Id,
                scope: request.ScopeValue,
                scopeType: scopeType,
                enforcementMode: enforcementMode,
                initiatedBy: request.AppliedBy);

            rollout.MarkCompleted();

            await rolloutRepository.AddAsync(rollout, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            return Result<Response>.Success(new Response(
                RolloutId: rollout.Id.Value.ToString(),
                PackId: pack.Id.Value.ToString(),
                VersionId: latestVersion.Id.Value.ToString(),
                Scope: rollout.Scope,
                ScopeType: rollout.ScopeType.ToString(),
                EnforcementMode: rollout.EnforcementMode.ToString(),
                Status: rollout.Status.ToString(),
                InitiatedBy: rollout.InitiatedBy,
                InitiatedAt: rollout.InitiatedAt));
        }
    }

    /// <summary>Resposta com os detalhes do rollout criado.</summary>
    public sealed record Response(
        string RolloutId,
        string PackId,
        string VersionId,
        string Scope,
        string ScopeType,
        string EnforcementMode,
        string Status,
        string InitiatedBy,
        DateTimeOffset InitiatedAt);
}
