using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Contracts.Quality.ServiceInterfaces;
using NexTraceOne.ChangeGovernance.Domain.Promotion.Enums;

namespace NexTraceOne.ChangeGovernance.Application.Promotion.Features.EvaluateCodeQualityPromotionGate;

/// <summary>
/// Feature: EvaluateCodeQualityPromotionGate — incorpora a qualidade de código como gate de promoção.
///
/// Obtém o veredito do quality gate do template (cobertura + SonarQube) através do contrato
/// inter-módulo <see cref="ICatalogQualityGateModule"/> e aplica o modo de enforcement configurado:
///   - Advisory   → nunca bloqueia (apenas informativo)
///   - SoftEnforce → sinaliza como aviso quando falha, mas não bloqueia
///   - HardEnforce → bloqueia a promoção quando falha
///
/// A avaliação é determinística (não depende de IA). O resultado (<c>Blocking</c>) pode ser
/// convertido num GateEvaluationInput para o motor de gates de promoção existente.
///
/// Estrutura VSA: Query + Validator + Handler + Response em ficheiro único.
/// </summary>
public static class EvaluateCodeQualityPromotionGate
{
    /// <summary>Decisão do gate.</summary>
    public static class Decisions
    {
        /// <summary>Gate aprovado.</summary>
        public const string Pass = "Pass";

        /// <summary>Gate falhou em modo Advisory — informativo, não bloqueia.</summary>
        public const string Advisory = "Advisory";

        /// <summary>Gate falhou em modo SoftEnforce — aviso, não bloqueia.</summary>
        public const string Warn = "Warn";

        /// <summary>Gate falhou em modo HardEnforce — bloqueia a promoção.</summary>
        public const string Block = "Block";
    }

    // ── Query ──────────────────────────────────────────────────────────────

    /// <summary>Avalia o gate de qualidade de código de um serviço com o modo de enforcement indicado.</summary>
    public sealed record Query(
        string ServiceId,
        string TenantId,
        CodeQualityGateEnforcement Enforcement = CodeQualityGateEnforcement.Advisory) : IQuery<Verdict>;

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ServiceId).NotEmpty().MaximumLength(200);
            RuleFor(x => x.TenantId).NotEmpty().MaximumLength(200);
        }
    }

    // ── Response ──────────────────────────────────────────────────────────

    /// <summary>Veredito do gate de qualidade de código para a promoção.</summary>
    public sealed record Verdict(
        string ServiceId,
        string Enforcement,
        string GateStatus,
        bool GatePassed,
        string Decision,
        bool Blocking,
        int RequiredCoverage,
        double? ActualCoverage,
        IReadOnlyList<string> Breaches);

    // ── Handler ────────────────────────────────────────────────────────────

    internal sealed class Handler(ICatalogQualityGateModule qualityGate) : IQueryHandler<Query, Verdict>
    {
        public async Task<Result<Verdict>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.NullOrWhiteSpace(request.ServiceId);
            Guard.Against.NullOrWhiteSpace(request.TenantId);

            var gate = await qualityGate.EvaluateAsync(request.ServiceId, request.TenantId, ct: cancellationToken);

            var (decision, blocking) = Resolve(gate.Passed, request.Enforcement);

            return Result<Verdict>.Success(new Verdict(
                ServiceId: gate.ServiceId,
                Enforcement: request.Enforcement.ToString(),
                GateStatus: gate.Status,
                GatePassed: gate.Passed,
                Decision: decision,
                Blocking: blocking,
                RequiredCoverage: gate.RequiredCoverage,
                ActualCoverage: gate.ActualCoverage,
                Breaches: gate.Breaches));
        }

        /// <summary>Mapeia (aprovação × modo) para (decisão, bloqueio). Lógica pura.</summary>
        private static (string Decision, bool Blocking) Resolve(
            bool passed, CodeQualityGateEnforcement enforcement)
        {
            if (passed)
                return (Decisions.Pass, false);

            return enforcement switch
            {
                CodeQualityGateEnforcement.HardEnforce => (Decisions.Block, true),
                CodeQualityGateEnforcement.SoftEnforce => (Decisions.Warn, false),
                _ => (Decisions.Advisory, false)
            };
        }
    }
}
