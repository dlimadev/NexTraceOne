using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;

namespace NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetReleaseImpactReport;

/// <summary>
/// Feature: GetReleaseImpactReport — calcula e devolve o relatório de impacto de uma release.
///
/// O relatório consolida:
/// - dados da release primária (serviço, versão, risco)
/// - blast radius calculado (consumidores directos e transitivos)
/// - work items associados à release
/// - commits incluídos na release
/// - pedidos de aprovação activos
///
/// Este relatório é exportável e pode ser enviado via webhook para sistemas externos
/// (ServiceNow, Confluence, etc.) como parte do evidence pack.
///
/// Estrutura VSA: Query + Validator + Handler + Response num único ficheiro.
/// </summary>
public static class GetReleaseImpactReport
{
    /// <summary>Query para obter o relatório de impacto de uma release.</summary>
    public sealed record Query(Guid ReleaseId) : IQuery<Response>;

    /// <summary>Valida a entrada da query.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ReleaseId).NotEmpty();
        }
    }

    /// <summary>Handler que compõe o relatório de impacto a partir dos dados do repositório.</summary>
    public sealed class Handler(
        IReleaseRepository releaseRepository,
        IBlastRadiusRepository blastRadiusRepository,
        IChangeScoreRepository scoreRepository,
        ICommitAssociationRepository commitRepository,
        IWorkItemAssociationRepository workItemRepository,
        IApprovalRequestRepository approvalRepository,
        IDateTimeProvider dateTimeProvider) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var releaseId = ReleaseId.From(request.ReleaseId);
            var release = await releaseRepository.GetByIdAsync(releaseId, cancellationToken);
            if (release is null)
                return Error.NotFound("RELEASE_NOT_FOUND", $"Release '{request.ReleaseId}' not found.");

            var blastRadius = await blastRadiusRepository.GetByReleaseIdAsync(releaseId, cancellationToken);
            var score = await scoreRepository.GetByReleaseIdAsync(releaseId, cancellationToken);
            var commits = await commitRepository.ListByReleaseIdAsync(releaseId, cancellationToken);
            var workItems = await workItemRepository.ListActiveByReleaseIdAsync(releaseId, cancellationToken);
            var approvals = await approvalRepository.ListPendingByReleaseIdAsync(releaseId, cancellationToken);

            var now = dateTimeProvider.UtcNow;

            var blastRadiusSection = blastRadius is null ? null : new BlastRadiusSection(
                TotalAffectedConsumers: blastRadius.TotalAffectedConsumers,
                DirectConsumers: blastRadius.DirectConsumers,
                TransitiveConsumers: blastRadius.TransitiveConsumers,
                CalculatedAt: blastRadius.CalculatedAt);

            var workItemSummary = new WorkItemsSummary(
                Total: workItems.Count,
                Stories: workItems.Count(wi => wi.WorkItemType.Equals("Story", StringComparison.OrdinalIgnoreCase)),
                Bugs: workItems.Count(wi => wi.WorkItemType.Equals("Bug", StringComparison.OrdinalIgnoreCase)),
                Features: workItems.Count(wi => wi.WorkItemType.Equals("Feature", StringComparison.OrdinalIgnoreCase)),
                Others: workItems.Count(wi =>
                    !wi.WorkItemType.Equals("Story", StringComparison.OrdinalIgnoreCase) &&
                    !wi.WorkItemType.Equals("Bug", StringComparison.OrdinalIgnoreCase) &&
                    !wi.WorkItemType.Equals("Feature", StringComparison.OrdinalIgnoreCase)));

            var commitSummary = new CommitsSummary(
                Total: commits.Count,
                ByAuthor: commits.GroupBy(c => c.CommitAuthor)
                    .Select(g => new CommitAuthorGroup(g.Key, g.Count()))
                    .OrderByDescending(g => g.Count)
                    .ToList());

            return new Response(
                ReleaseId: request.ReleaseId,
                ServiceName: release.ServiceName,
                Version: release.Version,
                Environment: release.Environment,
                Status: release.Status.ToString(),
                RiskScore: score?.Score,
                ChangeLevel: release.ChangeLevel.ToString(),
                BlastRadius: blastRadiusSection,
                WorkItemsSummary: workItemSummary,
                CommitsSummary: commitSummary,
                PendingApprovals: approvals.Count,
                GeneratedAt: now);
        }
    }

    // ─── Response types ───────────────────────────────────────────────────────

    /// <summary>Secção de blast radius no relatório de impacto.</summary>
    public sealed record BlastRadiusSection(
        int TotalAffectedConsumers,
        IReadOnlyList<string> DirectConsumers,
        IReadOnlyList<string> TransitiveConsumers,
        DateTimeOffset CalculatedAt);

    /// <summary>Sumário de work items associados à release.</summary>
    public sealed record WorkItemsSummary(
        int Total,
        int Stories,
        int Bugs,
        int Features,
        int Others);

    /// <summary>Agrupamento de commits por autor.</summary>
    public sealed record CommitAuthorGroup(string Author, int Count);

    /// <summary>Sumário de commits incluídos na release.</summary>
    public sealed record CommitsSummary(
        int Total,
        IReadOnlyList<CommitAuthorGroup> ByAuthor);

    /// <summary>Relatório completo de impacto de uma release.</summary>
    public sealed record Response(
        Guid ReleaseId,
        string ServiceName,
        string Version,
        string Environment,
        string Status,
        decimal? RiskScore,
        string ChangeLevel,
        BlastRadiusSection? BlastRadius,
        WorkItemsSummary WorkItemsSummary,
        CommitsSummary CommitsSummary,
        int PendingApprovals,
        DateTimeOffset GeneratedAt);
}
