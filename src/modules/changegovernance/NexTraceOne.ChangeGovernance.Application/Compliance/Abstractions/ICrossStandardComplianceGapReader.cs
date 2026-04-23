namespace NexTraceOne.ChangeGovernance.Application.Compliance.Abstractions;

/// <summary>
/// Abstracção para leitura de dados de gaps cross-standard de compliance.
/// Por omissão satisfeita por <c>NullCrossStandardComplianceGapReader</c> (honest-null).
/// Wave BB.1 — GetCrossStandardComplianceGapReport.
/// </summary>
public interface ICrossStandardComplianceGapReader
{
    Task<IReadOnlyList<ComplianceGapEntry>> ListGapsByTenantAsync(
        string tenantId, IReadOnlyList<string> standards, CancellationToken ct);

    public sealed record ComplianceGapEntry(
        string GapId,
        string GapName,
        string GapType,
        IReadOnlyList<string> AffectedStandards,
        IReadOnlyList<string> AffectedServiceIds,
        string ServiceTier,
        decimal ImpactScore,
        int RemediationComplexity,
        bool IsRemediated);
}
