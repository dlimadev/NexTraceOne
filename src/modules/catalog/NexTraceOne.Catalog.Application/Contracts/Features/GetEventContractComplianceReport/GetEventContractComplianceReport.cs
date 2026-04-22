using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;

namespace NexTraceOne.Catalog.Application.Contracts.Features.GetEventContractComplianceReport;

/// <summary>
/// Feature: GetEventContractComplianceReport — conformidade dos produtores de eventos com contratos AsyncAPI.
///
/// Para cada contrato de evento com produtores e dados de runtime:
/// - <c>SchemaComplianceRate</c>: % de eventos que passaram validação de schema
/// - <c>PayloadViolationCount</c>: número de payloads que violaram o schema
/// - <c>UnregisteredFields</c>: campos presentes mas não no schema
/// - <c>MissingRequiredFields</c>: campos obrigatórios ausentes
/// - <c>ViolationTimeline</c>: série temporal de 30 dias de violações
///
/// <c>ComplianceTier</c>:
/// - <c>Compliant</c>        — SchemaComplianceRate ≥ <c>CompliantThreshold</c> (default 99%)
/// - <c>MinorViolations</c>  — SchemaComplianceRate ≥ 95%
/// - <c>Degraded</c>         — SchemaComplianceRate ≥ <c>DegradedThreshold</c> (default 80%)
/// - <c>NonCompliant</c>     — SchemaComplianceRate &lt; DegradedThreshold
///
/// Fecha o loop entre o contrato AsyncAPI registado e a realidade em runtime.
///
/// Wave AH.3 — GetEventContractComplianceReport (Catalog Contracts).
/// </summary>
public static class GetEventContractComplianceReport
{
    private const double DefaultCompliantThreshold = 99.0;
    private const double MinorViolationsThreshold = 95.0;
    private const double DefaultDegradedThreshold = 80.0;

    /// <summary>
    /// <para><c>TenantId</c>: identificador do tenant (obrigatório).</para>
    /// <para><c>LookbackDays</c>: janela temporal de análise (1–365, default 30).</para>
    /// <para><c>CompliantThreshold</c>: SchemaComplianceRate mínima para Compliant (80–100, default 99).</para>
    /// <para><c>DegradedThreshold</c>: SchemaComplianceRate mínima para Degraded (0–95, default 80).</para>
    /// <para><c>MaxContracts</c>: máximo de contratos no relatório (10–500, default 200).</para>
    /// <para><c>TopNonCompliantCount</c>: número máximo de contratos não conformes a listar (1–50, default 10).</para>
    /// </summary>
    public sealed record Query(
        string TenantId,
        int LookbackDays = 30,
        double CompliantThreshold = DefaultCompliantThreshold,
        double DegradedThreshold = DefaultDegradedThreshold,
        int MaxContracts = 200,
        int TopNonCompliantCount = 10) : IQuery<Report>;

    /// <summary>Tier de conformidade de um contrato de evento.</summary>
    public enum ComplianceTier
    {
        /// <summary>SchemaComplianceRate ≥ CompliantThreshold (default 99%).</summary>
        Compliant,
        /// <summary>SchemaComplianceRate ≥ 95%.</summary>
        MinorViolations,
        /// <summary>SchemaComplianceRate ≥ DegradedThreshold (default 80%).</summary>
        Degraded,
        /// <summary>SchemaComplianceRate &lt; DegradedThreshold.</summary>
        NonCompliant
    }

    /// <summary>Entrada de conformidade de um contrato de evento.</summary>
    public sealed record EventComplianceReportEntry(
        string ContractId,
        string EventName,
        string ProducerServiceName,
        double SchemaComplianceRate,
        int PayloadViolationCount,
        IReadOnlyList<string> UnregisteredFields,
        IReadOnlyList<string> MissingRequiredFields,
        IReadOnlyDictionary<string, int> ViolationTimeline,
        ComplianceTier Tier);

    /// <summary>Relatório de conformidade de contratos de eventos.</summary>
    public sealed record Report(
        string TenantId,
        int LookbackDays,
        IReadOnlyList<EventComplianceReportEntry> AllContracts,
        IReadOnlyList<EventComplianceReportEntry> TopNonCompliantContracts,
        double TenantEventComplianceScore,
        int CompliantCount,
        int MinorViolationsCount,
        int DegradedCount,
        int NonCompliantCount);

    internal sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(q => q.TenantId).NotEmpty().MaximumLength(200);
            RuleFor(q => q.LookbackDays).InclusiveBetween(1, 365);
            RuleFor(q => q.CompliantThreshold).InclusiveBetween(80.0, 100.0);
            RuleFor(q => q.DegradedThreshold).InclusiveBetween(0.0, 95.0);
            RuleFor(q => q.MaxContracts).InclusiveBetween(10, 500);
            RuleFor(q => q.TopNonCompliantCount).InclusiveBetween(1, 50);
        }
    }

    internal sealed class Handler : IQueryHandler<Query, Report>
    {
        private readonly IEventComplianceReader _reader;

        public Handler(IEventComplianceReader reader)
        {
            _reader = Guard.Against.Null(reader);
        }

        public async Task<Result<Report>> Handle(Query query, CancellationToken ct)
        {
            Guard.Against.NullOrWhiteSpace(query.TenantId);

            var rawEntries = await _reader.ListByTenantAsync(query.TenantId, query.LookbackDays, ct);

            var entries = rawEntries
                .Take(query.MaxContracts)
                .Select(e => MapEntry(e, query.CompliantThreshold, query.DegradedThreshold))
                .ToList();

            var topNonCompliant = entries
                .Where(e => e.Tier != ComplianceTier.Compliant)
                .OrderBy(e => e.SchemaComplianceRate)
                .ThenByDescending(e => e.PayloadViolationCount)
                .Take(query.TopNonCompliantCount)
                .ToList();

            var tenantScore = entries.Count > 0
                ? Math.Round(entries.Average(e => e.SchemaComplianceRate), 2)
                : 100.0;

            return Result<Report>.Success(new Report(
                query.TenantId,
                query.LookbackDays,
                entries,
                topNonCompliant,
                tenantScore,
                entries.Count(e => e.Tier == ComplianceTier.Compliant),
                entries.Count(e => e.Tier == ComplianceTier.MinorViolations),
                entries.Count(e => e.Tier == ComplianceTier.Degraded),
                entries.Count(e => e.Tier == ComplianceTier.NonCompliant)));
        }

        private static EventComplianceReportEntry MapEntry(
            EventComplianceEntry e,
            double compliantThreshold,
            double degradedThreshold)
        {
            var tier = ClassifyTier(e.SchemaComplianceRate, compliantThreshold, degradedThreshold);

            return new EventComplianceReportEntry(
                e.ContractId,
                e.EventName,
                e.ProducerServiceName,
                Math.Round(e.SchemaComplianceRate, 2),
                e.PayloadViolationCount,
                e.UnregisteredFields,
                e.MissingRequiredFields,
                e.ViolationTimeline,
                tier);
        }

        private static ComplianceTier ClassifyTier(
            double rate, double compliantThreshold, double degradedThreshold) =>
            rate >= compliantThreshold       ? ComplianceTier.Compliant :
            rate >= MinorViolationsThreshold ? ComplianceTier.MinorViolations :
            rate >= degradedThreshold        ? ComplianceTier.Degraded :
                                               ComplianceTier.NonCompliant;
    }
}
