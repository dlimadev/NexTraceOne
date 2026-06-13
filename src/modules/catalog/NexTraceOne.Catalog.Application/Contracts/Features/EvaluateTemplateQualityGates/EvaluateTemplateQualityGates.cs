using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Application.Templates.Abstractions;
using NexTraceOne.Catalog.Domain.Templates.ValueObjects;

namespace NexTraceOne.Catalog.Application.Contracts.Features.EvaluateTemplateQualityGates;

/// <summary>
/// Feature: EvaluateTemplateQualityGates — fecha o laço de governança de qualidade de código.
///
/// Compara os <see cref="ManifestQualityGates"/> declarados no manifesto de arquitetura V2
/// do template (campo ArchitecturePatternJson) com o resultado de análise de qualidade mais
/// recente do serviço (CodeQualityRecord, proveniente do SonarQube ou compatível).
///
/// A avaliação é <b>determinística</b> — não depende de IA. Apenas os gates mensuráveis a
/// partir do CodeQualityRecord geram violações (breaches):
///   - Cobertura de testes (TestCoverageMinimum vs Coverage)
///   - Quality gate da própria ferramenta (QualityGateStatus)
/// Os restantes requisitos do manifesto (testes unitários/integração, spec OpenAPI, linters)
/// são reportados em DeclaredRequirements para transparência, mas não são verificáveis a partir
/// das métricas do SonarQube e por isso não geram violações aqui.
///
/// Wave AQ.3 — Template Quality Gate Enforcement (Developer Acceleration + Service Governance).
/// </summary>
public static class EvaluateTemplateQualityGates
{
    /// <summary>Estado global da avaliação do gate.</summary>
    public static class Statuses
    {
        /// <summary>Todos os gates mensuráveis foram respeitados.</summary>
        public const string Passed = "Passed";

        /// <summary>Pelo menos um gate mensurável foi violado.</summary>
        public const string Failed = "Failed";

        /// <summary>Não existe registo de qualidade de código para o serviço.</summary>
        public const string NoQualityData = "NoQualityData";

        /// <summary>O template não declara um manifesto de arquitetura com quality gates.</summary>
        public const string NoGatesDefined = "NoGatesDefined";
    }

    // ── Query ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Avalia o serviço identificado por <paramref name="ServiceId"/> contra os quality gates
    /// do template indicado por <paramref name="TemplateId"/> ou <paramref name="TemplateSlug"/>.
    /// </summary>
    public sealed record Query(
        string ServiceId,
        string TenantId,
        Guid? TemplateId,
        string? TemplateSlug) : IQuery<Evaluation>;

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ServiceId).NotEmpty().MaximumLength(200);
            RuleFor(x => x.TenantId).NotEmpty().MaximumLength(200);
            RuleFor(x => x)
                .Must(x => x.TemplateId.HasValue || !string.IsNullOrWhiteSpace(x.TemplateSlug))
                .WithMessage("Either TemplateId or TemplateSlug must be provided.");
        }
    }

    // ── Value objects ──────────────────────────────────────────────────────

    /// <summary>Violação de um quality gate específico.</summary>
    public sealed record QualityGateBreach(
        string Gate,
        string Detail,
        double? Threshold,
        double? Actual);

    /// <summary>Resultado da avaliação dos quality gates do template para um serviço.</summary>
    public sealed record Evaluation(
        string ServiceId,
        Guid TemplateId,
        string TemplateSlug,
        string TemplateVersion,
        string Status,
        bool Passed,
        int RequiredCoverage,
        double? ActualCoverage,
        string? SonarQualityGateStatus,
        int Bugs,
        int Vulnerabilities,
        int CodeSmells,
        IReadOnlyList<QualityGateBreach> Breaches,
        IReadOnlyList<string> DeclaredRequirements,
        DateTimeOffset? AnalyzedAt,
        DateTimeOffset EvaluatedAt);

    // ── Handler ────────────────────────────────────────────────────────────

    internal sealed class Handler(
        IServiceTemplateRepository templateRepository,
        ICodeQualityRepository codeQualityRepository,
        IDateTimeProvider clock) : IQueryHandler<Query, Evaluation>
    {
        // SonarQube reporta "OK" para quality gate aprovado; aceitamos sinónimos comuns.
        private static readonly HashSet<string> PassingGateStatuses =
            new(StringComparer.OrdinalIgnoreCase) { "OK", "PASSED", "SUCCESS" };

        public async Task<Result<Evaluation>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.NullOrWhiteSpace(request.ServiceId);
            Guard.Against.NullOrWhiteSpace(request.TenantId);

            var template = request.TemplateId.HasValue
                ? await templateRepository.GetByIdAsync(request.TemplateId.Value, cancellationToken)
                : await templateRepository.GetBySlugAsync(request.TemplateSlug!, cancellationToken);

            if (template is null)
            {
                var identifier = request.TemplateId?.ToString() ?? request.TemplateSlug!;
                return Error.NotFound(
                    "Template.NotFound",
                    $"Service template '{identifier}' was not found.");
            }

            var now = clock.UtcNow;
            var manifest = TemplateManifestV2.TryParse(template.ArchitecturePatternJson);

            // Sem manifesto V2 não há gates declarados a aplicar.
            if (manifest is null)
            {
                return Result<Evaluation>.Success(new Evaluation(
                    ServiceId: request.ServiceId,
                    TemplateId: template.Id.Value,
                    TemplateSlug: template.Slug,
                    TemplateVersion: template.Version,
                    Status: Statuses.NoGatesDefined,
                    Passed: true,
                    RequiredCoverage: 0,
                    ActualCoverage: null,
                    SonarQualityGateStatus: null,
                    Bugs: 0,
                    Vulnerabilities: 0,
                    CodeSmells: 0,
                    Breaches: [],
                    DeclaredRequirements: [],
                    AnalyzedAt: null,
                    EvaluatedAt: now));
            }

            var gates = manifest.QualityGates;
            var declared = BuildDeclaredRequirements(gates);

            var record = await codeQualityRepository.GetLatestAsync(
                request.ServiceId, request.TenantId, cancellationToken);

            // Sem dados de qualidade, não é possível provar conformidade → gate não aprovado.
            if (record is null)
            {
                return Result<Evaluation>.Success(new Evaluation(
                    ServiceId: request.ServiceId,
                    TemplateId: template.Id.Value,
                    TemplateSlug: template.Slug,
                    TemplateVersion: template.Version,
                    Status: Statuses.NoQualityData,
                    Passed: false,
                    RequiredCoverage: gates.TestCoverageMinimum,
                    ActualCoverage: null,
                    SonarQualityGateStatus: null,
                    Bugs: 0,
                    Vulnerabilities: 0,
                    CodeSmells: 0,
                    Breaches: [],
                    DeclaredRequirements: declared,
                    AnalyzedAt: null,
                    EvaluatedAt: now));
            }

            var breaches = new List<QualityGateBreach>();

            if (record.Coverage < gates.TestCoverageMinimum)
            {
                breaches.Add(new QualityGateBreach(
                    Gate: "coverage",
                    Detail: $"Coverage {record.Coverage:0.##}% is below the required minimum of {gates.TestCoverageMinimum}%.",
                    Threshold: gates.TestCoverageMinimum,
                    Actual: record.Coverage));
            }

            if (!string.IsNullOrWhiteSpace(record.QualityGateStatus)
                && !PassingGateStatuses.Contains(record.QualityGateStatus))
            {
                breaches.Add(new QualityGateBreach(
                    Gate: "sonar_quality_gate",
                    Detail: $"Static analysis quality gate is '{record.QualityGateStatus}'.",
                    Threshold: null,
                    Actual: null));
            }

            var passed = breaches.Count == 0;

            return Result<Evaluation>.Success(new Evaluation(
                ServiceId: request.ServiceId,
                TemplateId: template.Id.Value,
                TemplateSlug: template.Slug,
                TemplateVersion: template.Version,
                Status: passed ? Statuses.Passed : Statuses.Failed,
                Passed: passed,
                RequiredCoverage: gates.TestCoverageMinimum,
                ActualCoverage: record.Coverage,
                SonarQualityGateStatus: record.QualityGateStatus,
                Bugs: record.Bugs,
                Vulnerabilities: record.Vulnerabilities,
                CodeSmells: record.CodeSmells,
                Breaches: breaches,
                DeclaredRequirements: declared,
                AnalyzedAt: record.AnalyzedAt,
                EvaluatedAt: now));
        }

        /// <summary>
        /// Constrói a lista legível de requisitos declarados no manifesto, para transparência.
        /// Inclui requisitos não verificáveis a partir das métricas do SonarQube.
        /// </summary>
        private static IReadOnlyList<string> BuildDeclaredRequirements(ManifestQualityGates gates)
        {
            var requirements = new List<string> { $"Coverage >= {gates.TestCoverageMinimum}%" };

            if (gates.RequireUnitTests)
                requirements.Add("Unit tests required");
            if (gates.RequireIntegrationTests)
                requirements.Add("Integration tests required");
            if (gates.RequireOpenApiSpec)
                requirements.Add("OpenAPI/contract spec required");
            if (gates.RequiredLinters.Count > 0)
                requirements.Add($"Linters: {string.Join(", ", gates.RequiredLinters)}");

            return requirements;
        }
    }
}
