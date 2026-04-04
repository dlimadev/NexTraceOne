using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Entities;
using NexTraceOne.Governance.Domain.Enums;
using FluentValidation;

namespace NexTraceOne.Governance.Application.Features.GetGovernancePack;

/// <summary>
/// Feature: GetGovernancePack — detalhe completo de um governance pack.
/// Retorna regras vinculadas, escopos de aplicação e histórico de versões recentes.
/// </summary>
public static class GetGovernancePack
{
    /// <summary>Query para obter o detalhe de um governance pack pelo seu identificador.</summary>
    public sealed record Query(string PackId) : IQuery<Response>;

    /// <summary>Handler que retorna os detalhes completos de um governance pack.</summary>
    /// <summary>Valida os parâmetros da query de governance pack.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.PackId).NotEmpty().MaximumLength(200);
        }
    }

    public sealed class Handler(
        IGovernancePackRepository packRepository,
        IGovernancePackVersionRepository versionRepository,
        IGovernanceRolloutRecordRepository rolloutRecordRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            if (!Guid.TryParse(request.PackId, out var packGuid))
                return Error.Validation("INVALID_PACK_ID", "Pack ID '{0}' is not a valid GUID.", request.PackId);

            var pack = await packRepository.GetByIdAsync(new GovernancePackId(packGuid), cancellationToken);
            if (pack is null)
                return Error.NotFound("PACK_NOT_FOUND", "Governance pack '{0}' not found.", request.PackId);

            // Obtem versões do pack
            var versions = await versionRepository.ListByPackIdAsync(pack.Id, cancellationToken);
            
            // Get latest version for rules
            var latestVersion = await versionRepository.GetLatestByPackIdAsync(pack.Id, cancellationToken);
            var rules = latestVersion?.Rules.Select(r => new RuleBindingDto(
                RuleId: r.RuleId,
                RuleName: r.RuleName,
                Description: r.Description ?? string.Empty,
                Category: r.Category,
                DefaultEnforcementMode: r.DefaultEnforcementMode,
                IsRequired: r.IsRequired)).ToList() ?? new List<RuleBindingDto>();

            var versionDtos = versions
                .OrderByDescending(v => v.CreatedAt)
                .Take(5)
                .Select(v => new VersionSummaryDto(
                    VersionId: v.Id.Value.ToString(),
                    Version: v.Version,
                    CreatedAt: v.CreatedAt,
                    CreatedBy: v.CreatedBy,
                    RuleCount: v.Rules.Count
                ))
                .ToList();

            // Build scopes from completed rollout records
            var rolloutRecords = await rolloutRecordRepository.ListByPackIdAsync(pack.Id, cancellationToken);
            var completedRecords = rolloutRecords.Where(r => r.Status == RolloutStatus.Completed).ToList();
            var scopes = completedRecords
                .Select(r => new ScopeDto(
                    ScopeType: r.ScopeType,
                    ScopeValue: r.Scope,
                    EnforcementMode: r.EnforcementMode))
                .ToList();

            var detail = new GovernancePackDetailDto(
                PackId: pack.Id.Value.ToString(),
                Name: pack.Name,
                DisplayName: pack.DisplayName,
                Description: pack.Description ?? string.Empty,
                Category: pack.Category,
                Status: pack.Status,
                CurrentVersion: pack.CurrentVersion ?? "0.0.0",
                ScopeCount: completedRecords.Count,
                RuleCount: rules.Count,
                CreatedAt: pack.CreatedAt,
                UpdatedAt: pack.UpdatedAt,
                Rules: rules,
                Scopes: scopes,
                RecentVersions: versionDtos);

            var response = new Response(detail);
            return Result<Response>.Success(response);
        }
    }

    /// <summary>Resposta com detalhe completo do governance pack.</summary>
    public sealed record Response(GovernancePackDetailDto Pack);

    /// <summary>DTO com detalhe completo de um governance pack.</summary>
    public sealed record GovernancePackDetailDto(
        string PackId,
        string Name,
        string DisplayName,
        string Description,
        GovernanceRuleCategory Category,
        GovernancePackStatus Status,
        string CurrentVersion,
        int ScopeCount,
        int RuleCount,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt,
        IReadOnlyList<RuleBindingDto> Rules,
        IReadOnlyList<ScopeDto> Scopes,
        IReadOnlyList<VersionSummaryDto> RecentVersions);

    /// <summary>DTO de uma regra vinculada ao governance pack.</summary>
    public sealed record RuleBindingDto(
        string RuleId,
        string RuleName,
        string Description,
        GovernanceRuleCategory Category,
        EnforcementMode DefaultEnforcementMode,
        bool IsRequired);

    /// <summary>DTO de um escopo de aplicação do governance pack.</summary>
    public sealed record ScopeDto(
        GovernanceScopeType ScopeType,
        string ScopeValue,
        EnforcementMode EnforcementMode);

    /// <summary>DTO resumido de uma versão do governance pack.</summary>
    public sealed record VersionSummaryDto(
        string VersionId,
        string Version,
        DateTimeOffset CreatedAt,
        string CreatedBy,
        int RuleCount);
}
