using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.IdentityAccess.Application.Abstractions;

namespace NexTraceOne.IdentityAccess.Application.Features.GetAccessPatternAnomalyReport;

/// <summary>
/// Feature: GetAccessPatternAnomalyReport — detecção de padrões anómalos de acesso ao NexTraceOne.
///
/// Para cada utilizador, detecta os seguintes sinais de anomalia:
/// - OffHours (10 pts): pedidos fora do horário 08:00–20:00 UTC
/// - VolumetricSpike (25 pts): MaxDailyRequests > VolumetricSpikeMultiplier × AvgDailyRequests e TotalRequests > VolumetricSpikeMinTotalRequests
/// - FirstAccessSensitive (20 pts): primeiro acesso a recursos marcados como Restricted/Partner
/// - UnusualResource (15 pts): acesso a tipo de recurso nunca acedido anteriormente
/// - BulkExport (30 pts): exportações em massa acima do threshold configurado
///
/// RiskScore = soma ponderada dos sinais detectados, limitado a 0–100.
/// AnomalyDensityFlag = verdadeiro quando o utilizador tem 3 ou mais sinais distintos.
///
/// Wave AD.3 — Zero Trust &amp; Security Posture Analytics (IdentityAccess).
/// </summary>
public static class GetAccessPatternAnomalyReport
{
    // ── Pesos dos sinais de anomalia ──────────────────────────────────────
    private const int OffHoursWeight = 10;
    private const int VolumetricSpikeWeight = 25;
    private const int FirstAccessSensitiveWeight = 20;
    private const int UnusualResourceWeight = 15;
    private const int BulkExportWeight = 30;

    // ── Tipo de sinal de anomalia ─────────────────────────────────────────

    /// <summary>Tipo de sinal de anomalia de acesso detectado.</summary>
    public enum AnomalySignalType
    {
        /// <summary>Acesso fora do horário habitual (08:00–20:00 UTC).</summary>
        OffHours,
        /// <summary>Pico volumétrico: max diário > multiplicador × média diária.</summary>
        VolumetricSpike,
        /// <summary>Primeiro acesso a recurso sensível (Restricted/Partner).</summary>
        FirstAccessSensitive,
        /// <summary>Acesso a tipo de recurso nunca acedido anteriormente.</summary>
        UnusualResource,
        /// <summary>Exportação em massa acima do threshold configurado.</summary>
        BulkExport
    }

    // ── Limiar mínimo de pedidos para activar VolumetricSpike ─────────────
    private const int VolumetricSpikeMinTotalRequests = 5;

    /// <summary>
    /// <para><c>TenantId</c>: identificador do tenant (obrigatório).</para>
    /// <para><c>LookbackDays</c>: período de análise (7–90, padrão 30).</para>
    /// <para><c>VolumetricSpikeMultiplier</c>: multiplicador de volume para flag de pico (2–10, padrão 3).</para>
    /// <para><c>BulkExportThreshold</c>: número mínimo de exportações para flag BulkExport (1–200, padrão 20).</para>
    /// <para><c>MaxUsers</c>: número máximo de utilizadores no relatório (1–200, padrão 100).</para>
    /// </summary>
    public sealed record Query(
        string TenantId,
        int LookbackDays = 30,
        int VolumetricSpikeMultiplier = 3,
        int BulkExportThreshold = 20,
        int MaxUsers = 100) : IQuery<Response>;

    // ── Validator ──────────────────────────────────────────────────────────

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(q => q.TenantId).NotEmpty();
            RuleFor(q => q.LookbackDays).InclusiveBetween(7, 90);
            RuleFor(q => q.VolumetricSpikeMultiplier).InclusiveBetween(2, 10);
            RuleFor(q => q.BulkExportThreshold).InclusiveBetween(1, 200);
            RuleFor(q => q.MaxUsers).InclusiveBetween(1, 200);
        }
    }

    // ── Value objects ──────────────────────────────────────────────────────

    /// <summary>Perfil de anomalia de acesso de um utilizador.</summary>
    public sealed record UserAnomalyProfile(
        string UserId,
        string UserName,
        string? TeamName,
        int TotalRequests,
        IReadOnlyList<AnomalySignalType> DetectedSignals,
        int AnomalySignalCount,
        int RiskScore,
        bool AnomalyDensityFlag);

    /// <summary>Relatório de anomalias de padrão de acesso do tenant.</summary>
    public sealed record Response(
        string TenantId,
        int LookbackDays,
        int TotalUsersAnalyzed,
        int TotalAnomalousUsers,
        IReadOnlyList<UserAnomalyProfile> AnomalousUsers,
        IReadOnlyList<UserAnomalyProfile> HighDensityUsers,
        IReadOnlyDictionary<string, int> SignalTypeDistribution);

    // ── Handler ────────────────────────────────────────────────────────────

    public sealed class Handler : IQueryHandler<Query, Response>
    {
        private readonly IAccessPatternReader _reader;

        public Handler(IAccessPatternReader reader)
        {
            _reader = Guard.Against.Null(reader);
        }

        public async Task<Result<Response>> Handle(Query query, CancellationToken cancellationToken)
        {
            Guard.Against.NullOrWhiteSpace(query.TenantId);

            var entries = await _reader.ListByTenantAsync(query.TenantId, query.LookbackDays, cancellationToken);

            var limitedEntries = entries.Take(query.MaxUsers).ToList();
            int totalAnalyzed = limitedEntries.Count;

            var anomalousProfiles = new List<UserAnomalyProfile>();

            foreach (var entry in limitedEntries)
            {
                var signals = DetectSignals(entry, query.VolumetricSpikeMultiplier, query.BulkExportThreshold);
                if (signals.Count == 0) continue;

                int riskScore = ComputeRiskScore(signals);
                bool densityFlag = signals.Count >= 3;

                anomalousProfiles.Add(new UserAnomalyProfile(
                    UserId: entry.UserId,
                    UserName: entry.UserName,
                    TeamName: entry.TeamName,
                    TotalRequests: entry.TotalRequests,
                    DetectedSignals: signals,
                    AnomalySignalCount: signals.Count,
                    RiskScore: riskScore,
                    AnomalyDensityFlag: densityFlag));
            }

            // Ordenar por RiskScore descendente
            var sorted = anomalousProfiles
                .OrderByDescending(p => p.RiskScore)
                .ToList();

            var highDensity = sorted.Where(p => p.AnomalyDensityFlag).ToList();

            // Distribuição por tipo de sinal
            var signalDist = Enum.GetValues<AnomalySignalType>()
                .ToDictionary(
                    s => s.ToString(),
                    s => sorted.Count(p => p.DetectedSignals.Contains(s)));

            return Result<Response>.Success(new Response(
                TenantId: query.TenantId,
                LookbackDays: query.LookbackDays,
                TotalUsersAnalyzed: totalAnalyzed,
                TotalAnomalousUsers: sorted.Count,
                AnomalousUsers: sorted,
                HighDensityUsers: highDensity,
                SignalTypeDistribution: signalDist));
        }

        // ── Detecção de sinais por utilizador ─────────────────────────────

        private static IReadOnlyList<AnomalySignalType> DetectSignals(
            UserAccessEntry entry,
            int spikeMultiplier,
            int bulkThreshold)
        {
            var signals = new List<AnomalySignalType>();

            if (entry.OffHoursRequests > 0)
                signals.Add(AnomalySignalType.OffHours);

            if (entry.TotalRequests > VolumetricSpikeMinTotalRequests
                && entry.AvgDailyRequests > 0
                && entry.MaxDailyRequests > spikeMultiplier * entry.AvgDailyRequests)
                signals.Add(AnomalySignalType.VolumetricSpike);

            if (entry.SensitiveResourceAccesses > 0)
                signals.Add(AnomalySignalType.FirstAccessSensitive);

            if (entry.UnusualResourceAccesses > 0)
                signals.Add(AnomalySignalType.UnusualResource);

            if (entry.BulkExportCount > bulkThreshold)
                signals.Add(AnomalySignalType.BulkExport);

            return signals;
        }

        // ── Cálculo do RiskScore ──────────────────────────────────────────

        private static int ComputeRiskScore(IReadOnlyList<AnomalySignalType> signals)
        {
            int score = 0;
            foreach (var signal in signals)
            {
                score += signal switch
                {
                    AnomalySignalType.OffHours => OffHoursWeight,
                    AnomalySignalType.VolumetricSpike => VolumetricSpikeWeight,
                    AnomalySignalType.FirstAccessSensitive => FirstAccessSensitiveWeight,
                    AnomalySignalType.UnusualResource => UnusualResourceWeight,
                    AnomalySignalType.BulkExport => BulkExportWeight,
                    _ => 0
                };
            }
            return Math.Min(score, 100);
        }
    }
}
