using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Services.Abstractions;

namespace NexTraceOne.Catalog.Application.Services.Features.GetServiceRetirementReadinessReport;

/// <summary>
/// Feature: GetServiceRetirementReadinessReport — prontidão para retirar um serviço específico.
///
/// Responde "posso deprecar/retirar este serviço com segurança?" com score composto (0–100)
/// calculado em quatro dimensões ponderadas:
/// - <c>ConsumerMigrated</c> (40%): % de consumidores migrados para alternativa
/// - <c>ContractsDeprecated</c> (25%): % de contratos do serviço em estado Deprecated/Sunset
/// - <c>RunbookDocumented</c> (15%): runbook de decommission existente e aprovado
/// - <c>DependantsNotified</c> (20%): % de equipas consumidoras notificadas
///
/// <c>RetirementReadinessTier</c>:
/// - <c>Ready</c>    — score ≥ <c>ReadyThreshold</c> (default 85)
/// - <c>NearReady</c> — score ≥ <c>NearReadyThreshold</c> (default 65)
/// - <c>Blocked</c>  — score ≥ 40
/// - <c>NotReady</c> — score &lt; 40
///
/// Transforma a decisão de retirada de serviço de ad-hoc para processo estruturado e auditável.
///
/// Wave AF.2 — GetServiceRetirementReadinessReport (Catalog Services).
/// </summary>
public static class GetServiceRetirementReadinessReport
{
    // ── Weights (percentagem de 100 pontos) ────────────────────────────────
    private const double ConsumerMigratedWeight = 40.0;
    private const double ContractsDeprecatedWeight = 25.0;
    private const double RunbookDocumentedWeight = 15.0;
    private const double DependantsNotifiedWeight = 20.0;

    private const int DefaultReadyThreshold = 85;
    private const int DefaultNearReadyThreshold = 65;
    private const int BlockedThreshold = 40;

    /// <summary>
    /// <para><c>TenantId</c>: identificador do tenant (obrigatório).</para>
    /// <para><c>ServiceId</c>: identificador único do serviço (obrigatório).</para>
    /// <para><c>ReadyThreshold</c>: score mínimo para tier Ready (50–100, default 85).</para>
    /// <para><c>NearReadyThreshold</c>: score mínimo para tier NearReady (30–90, default 65).</para>
    /// </summary>
    public sealed record Query(
        string TenantId,
        string ServiceId,
        int ReadyThreshold = DefaultReadyThreshold,
        int NearReadyThreshold = DefaultNearReadyThreshold) : IQuery<Report>;

    // ── Enums ─────────────────────────────────────────────────────────────

    /// <summary>Classificação de prontidão para retirada de serviço.</summary>
    public enum RetirementReadinessTier
    {
        /// <summary>Score ≥ ReadyThreshold (default 85). Retirada segura.</summary>
        Ready,
        /// <summary>Score ≥ NearReadyThreshold (default 65). Gaps menores.</summary>
        NearReady,
        /// <summary>Score ≥ 40. Bloqueadores significativos.</summary>
        Blocked,
        /// <summary>Score &lt; 40. Retirada prematura — não recomendado.</summary>
        NotReady
    }

    // ── Value objects ──────────────────────────────────────────────────────

    /// <summary>Scores por dimensão do RetirementReadinessScore.</summary>
    public sealed record DimensionScores(
        double ConsumerMigratedScore,
        double ContractsDeprecatedScore,
        double RunbookDocumentedScore,
        double DependantsNotifiedScore);

    /// <summary>Item que impede a retirada segura do serviço.</summary>
    public sealed record RetirementBlocker(
        string BlockerType,
        string Description,
        int AffectedCount);

    /// <summary>Estado de migração de um consumidor para a alternativa designada.</summary>
    public sealed record ConsumerMigrationStatus(
        string ConsumerServiceName,
        string ConsumerTeamName,
        string ConsumerTier,
        bool IsMigrated,
        bool IsNotified);

    /// <summary>Relatório de prontidão para retirada de um serviço específico.</summary>
    public sealed record Report(
        string TenantId,
        string ServiceId,
        string ServiceName,
        string TeamName,
        string CurrentLifecycleState,
        double RetirementReadinessScore,
        RetirementReadinessTier Tier,
        DimensionScores Dimensions,
        IReadOnlyList<RetirementBlocker> BlockerList,
        IReadOnlyList<ConsumerMigrationStatus> ConsumerMigrationProgress);

    // ── Validator ─────────────────────────────────────────────────────────

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(q => q.TenantId).NotEmpty();
            RuleFor(q => q.ServiceId).NotEmpty();
            RuleFor(q => q.ReadyThreshold).InclusiveBetween(50, 100);
            RuleFor(q => q.NearReadyThreshold).InclusiveBetween(30, 90);
            RuleFor(q => q).Must(q => q.NearReadyThreshold < q.ReadyThreshold)
                .WithMessage("NearReadyThreshold must be less than ReadyThreshold.");
        }
    }

    // ── Handler ───────────────────────────────────────────────────────────

    internal sealed class Handler : IQueryHandler<Query, Report>
    {
        private readonly IRetirementReadinessReader _reader;

        public Handler(IRetirementReadinessReader reader)
        {
            _reader = Guard.Against.Null(reader);
        }

        public async Task<Result<Report>> Handle(Query query, CancellationToken ct)
        {
            Guard.Against.NullOrWhiteSpace(query.TenantId);
            Guard.Against.NullOrWhiteSpace(query.ServiceId);

            var data = await _reader.GetByServiceAsync(query.TenantId, query.ServiceId, ct);

            if (data is null)
                return Error.NotFound("service.not-found", $"Service '{query.ServiceId}' not found in tenant '{query.TenantId}'.");

            // ── Dimension scores ──────────────────────────────────────────

            var consumerMigratedScore = data.TotalConsumers > 0
                ? (double)data.MigratedConsumers / data.TotalConsumers * ConsumerMigratedWeight
                : ConsumerMigratedWeight; // no consumers = fully clear

            var contractsDeprecatedScore = data.TotalContracts > 0
                ? (double)data.DeprecatedContracts / data.TotalContracts * ContractsDeprecatedWeight
                : ContractsDeprecatedWeight;

            var runbookScore = data.HasApprovedDecommissionRunbook
                ? RunbookDocumentedWeight
                : 0.0;

            var dependantsNotifiedScore = data.TotalConsumerTeams > 0
                ? (double)data.NotifiedConsumerTeams / data.TotalConsumerTeams * DependantsNotifiedWeight
                : DependantsNotifiedWeight;

            var totalScore = consumerMigratedScore + contractsDeprecatedScore + runbookScore + dependantsNotifiedScore;
            var roundedScore = Math.Round(totalScore, 1);

            // ── Tier ──────────────────────────────────────────────────────

            var tier = roundedScore >= query.ReadyThreshold ? RetirementReadinessTier.Ready
                : roundedScore >= query.NearReadyThreshold ? RetirementReadinessTier.NearReady
                : roundedScore >= BlockedThreshold ? RetirementReadinessTier.Blocked
                : RetirementReadinessTier.NotReady;

            // ── Blockers ──────────────────────────────────────────────────

            var blockers = new List<RetirementBlocker>();

            var unmigratedCount = data.TotalConsumers - data.MigratedConsumers;
            if (unmigratedCount > 0)
                blockers.Add(new RetirementBlocker(
                    "active-consumers",
                    "Consumers still active on the service without migration to successor",
                    unmigratedCount));

            var activeContractCount = data.TotalContracts - data.DeprecatedContracts;
            if (activeContractCount > 0)
                blockers.Add(new RetirementBlocker(
                    "active-contracts",
                    "Contracts still in Active or Approved state (not deprecated/sunset)",
                    activeContractCount));

            if (!data.HasApprovedDecommissionRunbook)
                blockers.Add(new RetirementBlocker(
                    "missing-runbook",
                    "No approved decommission runbook found for the service",
                    1));

            var unnotifiedTeams = data.TotalConsumerTeams - data.NotifiedConsumerTeams;
            if (unnotifiedTeams > 0)
                blockers.Add(new RetirementBlocker(
                    "unnotified-teams",
                    "Consumer teams not notified of service retirement",
                    unnotifiedTeams));

            // ── Consumer migration progress ───────────────────────────────

            var migrationProgress = data.UnmigratedConsumers
                .Select(c => new ConsumerMigrationStatus(
                    ConsumerServiceName: c.ConsumerServiceName,
                    ConsumerTeamName: c.ConsumerTeamName,
                    ConsumerTier: c.ConsumerTier,
                    IsMigrated: false,
                    IsNotified: c.IsNotified))
                .ToList();

            return Result<Report>.Success(new Report(
                TenantId: query.TenantId,
                ServiceId: query.ServiceId,
                ServiceName: data.ServiceName,
                TeamName: data.TeamName,
                CurrentLifecycleState: data.CurrentLifecycleState,
                RetirementReadinessScore: roundedScore,
                Tier: tier,
                Dimensions: new DimensionScores(
                    ConsumerMigratedScore: Math.Round(consumerMigratedScore, 1),
                    ContractsDeprecatedScore: Math.Round(contractsDeprecatedScore, 1),
                    RunbookDocumentedScore: runbookScore,
                    DependantsNotifiedScore: Math.Round(dependantsNotifiedScore, 1)),
                BlockerList: blockers,
                ConsumerMigrationProgress: migrationProgress));
        }
    }
}
