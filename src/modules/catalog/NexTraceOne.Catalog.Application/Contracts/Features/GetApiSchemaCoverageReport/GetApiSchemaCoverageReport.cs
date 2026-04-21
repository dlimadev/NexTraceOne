using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Enums;

namespace NexTraceOne.Catalog.Application.Contracts.Features.GetApiSchemaCoverageReport;

/// <summary>
/// Feature: GetApiSchemaCoverageReport — completude de documentação de schemas de API.
///
/// Para cada contrato em estado <c>Approved</c> ou <c>Locked</c>, calcula um score de
/// cobertura de documentação de schema (0–100) baseado em 4 dimensões avaliadas a partir
/// do <c>SpecContent</c> do contrato:
///
/// - +25 <b>Response body documentado</b>: SpecContent contém referência a resposta "200"
/// - +25 <b>Request body documentado</b>: SpecContent contém "requestBody" ou "request_body" (onde aplicável)
/// - +25 <b>Exemplos de payload presentes</b>: SpecContent contém "example"
/// - +25 <b>Status codes complementares</b>: SpecContent contém referência a "4" ou "5" errors (4xx/5xx)
///
/// Classifica cada contrato por <c>CoverageGrade</c>:
/// - <c>A</c> — score ≥ 90
/// - <c>B</c> — score ≥ 70
/// - <c>C</c> — score ≥ 50
/// - <c>D</c> — score &lt; 50
///
/// Produz:
/// - distribuição global por CoverageGrade
/// - score médio de cobertura de schema do tenant
/// - top contratos com menor cobertura (prioridade para melhoria)
/// - lista completa de contratos analisados
///
/// Orienta Architect e Tech Lead a melhorar a documentação dos contratos e garantir
/// que o catálogo tem valor real para consumidores.
///
/// Wave T.2 — API Schema Coverage Report (Catalog Contracts).
/// </summary>
public static class GetApiSchemaCoverageReport
{
    // ── Grade thresholds ───────────────────────────────────────────────────
    private const int GradeAThreshold = 90;
    private const int GradeBThreshold = 70;
    private const int GradeCThreshold = 50;

    // ── Dimension weights (each 25 points) ────────────────────────────────
    private const int DimensionPoints = 25;

    /// <summary>
    /// <para><c>TopLowCoverageCount</c>: número máximo de contratos com menor cobertura a listar (1–100, default 10).</para>
    /// <para><c>PageSize</c>: tamanho da página interna de pesquisa (10–1000, default 500).</para>
    /// <para><c>Protocol</c>: filtro opcional por protocolo (null = todos).</para>
    /// </summary>
    public sealed record Query(
        int TopLowCoverageCount = 10,
        int PageSize = 500,
        ContractProtocol? Protocol = null) : IQuery<Report>;

    // ── Value objects ──────────────────────────────────────────────────────

    /// <summary>Classificação de cobertura de schema de um contrato.</summary>
    public enum CoverageGrade
    {
        /// <summary>Score ≥ 90 — documentação excelente.</summary>
        A,
        /// <summary>Score ≥ 70 — boa documentação com margem de melhoria.</summary>
        B,
        /// <summary>Score ≥ 50 — documentação parcial; consumidores podem ter dificuldades.</summary>
        C,
        /// <summary>Score &lt; 50 — documentação deficiente; requer atenção imediata.</summary>
        D
    }

    /// <summary>Distribuição de contratos por CoverageGrade.</summary>
    public sealed record CoverageGradeDistribution(
        int ACount,
        int BCount,
        int CCount,
        int DCount);

    /// <summary>Detalhe de cobertura de schema de um contrato.</summary>
    public sealed record ContractCoverageEntry(
        Guid ApiAssetId,
        string SemVer,
        ContractProtocol Protocol,
        int CoverageScore,
        CoverageGrade Grade,
        bool HasResponseBody,
        bool HasRequestBody,
        bool HasExamples,
        bool HasErrorStatusCodes);

    /// <summary>Resultado do relatório de cobertura de schemas de API.</summary>
    public sealed record Report(
        int TotalContractsAnalyzed,
        decimal AverageScoreGlobal,
        CoverageGradeDistribution GradeDistribution,
        IReadOnlyList<ContractCoverageEntry> TopLowCoverageContracts,
        IReadOnlyList<ContractCoverageEntry> AllContracts);

    // ── Validator ──────────────────────────────────────────────────────────

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(q => q.TopLowCoverageCount).InclusiveBetween(1, 100);
            RuleFor(q => q.PageSize).InclusiveBetween(10, 1000);
        }
    }

    // ── Handler ────────────────────────────────────────────────────────────

    public sealed class Handler(
        IContractVersionRepository repository) : IQueryHandler<Query, Report>
    {
        public async Task<Result<Report>> Handle(Query query, CancellationToken cancellationToken)
        {
            Guard.Against.Null(query);

            // Fetch Approved and Locked contracts
            var (approvedVersions, _) = await repository.SearchAsync(
                protocol: query.Protocol,
                lifecycleState: ContractLifecycleState.Approved,
                apiAssetId: null,
                searchTerm: null,
                page: 1,
                pageSize: query.PageSize,
                cancellationToken: cancellationToken);

            var (lockedVersions, _) = await repository.SearchAsync(
                protocol: query.Protocol,
                lifecycleState: ContractLifecycleState.Locked,
                apiAssetId: null,
                searchTerm: null,
                page: 1,
                pageSize: query.PageSize,
                cancellationToken: cancellationToken);

            var allVersions = approvedVersions.Concat(lockedVersions).ToList();

            if (allVersions.Count == 0)
            {
                return Result<Report>.Success(new Report(
                    TotalContractsAnalyzed: 0,
                    AverageScoreGlobal: 0m,
                    GradeDistribution: new CoverageGradeDistribution(0, 0, 0, 0),
                    TopLowCoverageContracts: [],
                    AllContracts: []));
            }

            var entries = new List<ContractCoverageEntry>();

            foreach (var version in allVersions)
            {
                var (score, hasResponse, hasRequest, hasExamples, hasErrorCodes) =
                    EvaluateCoverage(version.SpecContent, version.Protocol);

                entries.Add(new ContractCoverageEntry(
                    ApiAssetId: version.ApiAssetId,
                    SemVer: version.SemVer,
                    Protocol: version.Protocol,
                    CoverageScore: score,
                    Grade: ClassifyGrade(score),
                    HasResponseBody: hasResponse,
                    HasRequestBody: hasRequest,
                    HasExamples: hasExamples,
                    HasErrorStatusCodes: hasErrorCodes));
            }

            int aCount = entries.Count(e => e.Grade == CoverageGrade.A);
            int bCount = entries.Count(e => e.Grade == CoverageGrade.B);
            int cCount = entries.Count(e => e.Grade == CoverageGrade.C);
            int dCount = entries.Count(e => e.Grade == CoverageGrade.D);

            decimal avgScore = entries.Count > 0
                ? Math.Round(entries.Average(e => (decimal)e.CoverageScore), 2)
                : 0m;

            var topLow = entries
                .OrderBy(e => e.CoverageScore)
                .ThenBy(e => e.ApiAssetId)
                .Take(query.TopLowCoverageCount)
                .ToList();

            return Result<Report>.Success(new Report(
                TotalContractsAnalyzed: entries.Count,
                AverageScoreGlobal: avgScore,
                GradeDistribution: new CoverageGradeDistribution(aCount, bCount, cCount, dCount),
                TopLowCoverageContracts: topLow,
                AllContracts: entries.OrderBy(e => e.ApiAssetId).ThenBy(e => e.SemVer).ToList()));
        }

        // ── Helpers ────────────────────────────────────────────────────────

        /// <summary>
        /// Avalia a cobertura de documentação de schema a partir do <paramref name="specContent"/>.
        /// Usa heurísticas baseadas em palavras-chave para detectar cada dimensão, compatíveis
        /// com JSON, YAML e XML (OpenAPI, AsyncAPI, WSDL, GraphQL SDL, Protobuf .proto).
        /// </summary>
        private static (int Score, bool HasResponse, bool HasRequest, bool HasExamples, bool HasError)
            EvaluateCoverage(string specContent, ContractProtocol protocol)
        {
            if (string.IsNullOrWhiteSpace(specContent))
                return (0, false, false, false, false);

            var spec = specContent;

            // Dimension 1 — Response body documented (200 OK / success response)
            bool hasResponse = spec.Contains("\"200\"", StringComparison.OrdinalIgnoreCase)
                || spec.Contains("'200'", StringComparison.OrdinalIgnoreCase)
                || spec.Contains("200:", StringComparison.OrdinalIgnoreCase)
                || spec.Contains("responses", StringComparison.OrdinalIgnoreCase)
                || spec.Contains("<wsdl:output", StringComparison.OrdinalIgnoreCase) // WSDL
                || spec.Contains("type Query", StringComparison.OrdinalIgnoreCase)   // GraphQL
                || spec.Contains("returns ", StringComparison.OrdinalIgnoreCase);     // Protobuf

            // Dimension 2 — Request body documented
            bool hasRequest = spec.Contains("requestBody", StringComparison.OrdinalIgnoreCase)
                || spec.Contains("request_body", StringComparison.OrdinalIgnoreCase)
                || spec.Contains("body:", StringComparison.OrdinalIgnoreCase)
                || spec.Contains("<wsdl:input", StringComparison.OrdinalIgnoreCase)  // WSDL
                || spec.Contains("type Mutation", StringComparison.OrdinalIgnoreCase) // GraphQL
                || spec.Contains("message ", StringComparison.OrdinalIgnoreCase)      // Protobuf/AsyncAPI
                || spec.Contains("payload", StringComparison.OrdinalIgnoreCase);

            // Dimension 3 — Examples present
            bool hasExamples = spec.Contains("example", StringComparison.OrdinalIgnoreCase)
                || spec.Contains("x-example", StringComparison.OrdinalIgnoreCase)
                || spec.Contains("examples:", StringComparison.OrdinalIgnoreCase);

            // Dimension 4 — Error status codes (4xx / 5xx, or error types for non-REST)
            bool hasErrorCodes = spec.Contains("\"400\"", StringComparison.OrdinalIgnoreCase)
                || spec.Contains("\"401\"", StringComparison.OrdinalIgnoreCase)
                || spec.Contains("\"403\"", StringComparison.OrdinalIgnoreCase)
                || spec.Contains("\"404\"", StringComparison.OrdinalIgnoreCase)
                || spec.Contains("\"422\"", StringComparison.OrdinalIgnoreCase)
                || spec.Contains("\"500\"", StringComparison.OrdinalIgnoreCase)
                || spec.Contains("400:", StringComparison.OrdinalIgnoreCase)
                || spec.Contains("404:", StringComparison.OrdinalIgnoreCase)
                || spec.Contains("500:", StringComparison.OrdinalIgnoreCase)
                || spec.Contains("fault", StringComparison.OrdinalIgnoreCase)         // WSDL fault
                || spec.Contains("Error", StringComparison.OrdinalIgnoreCase);        // GraphQL/generic

            int score = 0;
            if (hasResponse) score += DimensionPoints;
            if (hasRequest) score += DimensionPoints;
            if (hasExamples) score += DimensionPoints;
            if (hasErrorCodes) score += DimensionPoints;

            return (score, hasResponse, hasRequest, hasExamples, hasErrorCodes);
        }

        private static CoverageGrade ClassifyGrade(int score) => score switch
        {
            >= GradeAThreshold => CoverageGrade.A,
            >= GradeBThreshold => CoverageGrade.B,
            >= GradeCThreshold => CoverageGrade.C,
            _ => CoverageGrade.D
        };
    }
}
