using System.Text.RegularExpressions;
using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Services.Abstractions;

namespace NexTraceOne.Catalog.Application.Services.Features.GetSecretsExposureRiskReport;

/// <summary>
/// Feature: GetSecretsExposureRiskReport — detecção de risco de exposição de segredos em artefactos governados.
///
/// Realiza varredura por expressões regulares no conteúdo textual de contratos, notas operacionais e runbooks.
///
/// Padrões detectados e respectivos níveis de risco:
/// - ApiKey (Critical): chaves API cloud/SaaS (sk-, AKIA, ghp_)
/// - JwtToken (High): tokens JWT com estrutura base64url válida
/// - ConnectionString (High): connection strings com credenciais em texto claro
/// - PrivateIp (Low): endereços IP privados RFC 1918 (10.x, 172.16-31.x, 192.168.x)
/// - PersonalEmail (Medium): endereços de email em domínios pessoais conhecidos
///
/// Devolve lista de artefactos com ExposureRisk &gt;= MinRiskLevel configurado.
///
/// Wave AD.2 — Zero Trust &amp; Security Posture Analytics (Catalog).
/// </summary>
public static class GetSecretsExposureRiskReport
{
    // ── Enums ──────────────────────────────────────────────────────────────

    /// <summary>Nível de risco de exposição de segredos num artefacto.</summary>
    public enum ExposureRisk
    {
        /// <summary>Sem padrões de segredos detectados.</summary>
        None = 0,
        /// <summary>Padrão de baixo risco (ex: IP privado em exemplo).</summary>
        Low = 1,
        /// <summary>Padrão de risco médio (ex: email pessoal).</summary>
        Medium = 2,
        /// <summary>Padrão de alto risco (ex: JWT, connection string com password).</summary>
        High = 3,
        /// <summary>Padrão de risco crítico (ex: API key cloud/SaaS).</summary>
        Critical = 4
    }

    /// <summary>Categoria de segredo detectado num artefacto.</summary>
    public enum ExposureCategory
    {
        /// <summary>Chave de API cloud/SaaS (sk-, AKIA, ghp_).</summary>
        ApiKey,
        /// <summary>Token JWT com estrutura base64url.</summary>
        JwtToken,
        /// <summary>Connection string com credencial em texto claro.</summary>
        ConnectionString,
        /// <summary>Endereço IP privado RFC 1918.</summary>
        PrivateIp,
        /// <summary>Endereço de email pessoal (gmail, yahoo, hotmail, outlook).</summary>
        PersonalEmail
    }

    // ── Padrões regex compilados estaticamente ─────────────────────────────

    private static readonly Regex ApiKeyRegex =
        new(@"(sk-[a-zA-Z0-9]{20,}|AKIA[0-9A-Z]{16}|ghp_[a-zA-Z0-9]{36})",
            RegexOptions.Compiled, TimeSpan.FromSeconds(2));

    private static readonly Regex JwtTokenRegex =
        new(@"eyJ[a-zA-Z0-9_-]{10,}\.[a-zA-Z0-9_-]{10,}\.[a-zA-Z0-9_-]{10,}",
            RegexOptions.Compiled, TimeSpan.FromSeconds(2));

    private static readonly Regex ConnectionStringRegex =
        new(@"(password|pwd|secret)\s*=\s*(?!placeholder|example|changeme|your-|<|{|\[)[^\s;'""]{4,}",
            RegexOptions.Compiled | RegexOptions.IgnoreCase, TimeSpan.FromSeconds(2));

    private static readonly Regex PrivateIpRegex =
        new(@"\b(10\.\d{1,3}\.\d{1,3}\.\d{1,3}|172\.(1[6-9]|2\d|3[01])\.\d{1,3}\.\d{1,3}|192\.168\.\d{1,3}\.\d{1,3})\b",
            RegexOptions.Compiled, TimeSpan.FromSeconds(2));

    private static readonly Regex PersonalEmailRegex =
        new(@"[a-zA-Z0-9._%+\-]+@(gmail|yahoo|hotmail|outlook)\.[a-zA-Z]{2,}",
            RegexOptions.Compiled, TimeSpan.FromSeconds(2));

    // Mapeamento de categoria para nível de risco
    private static readonly IReadOnlyDictionary<ExposureCategory, ExposureRisk> CategoryRisk =
        new Dictionary<ExposureCategory, ExposureRisk>
        {
            [ExposureCategory.ApiKey] = ExposureRisk.Critical,
            [ExposureCategory.JwtToken] = ExposureRisk.High,
            [ExposureCategory.ConnectionString] = ExposureRisk.High,
            [ExposureCategory.PrivateIp] = ExposureRisk.Low,
            [ExposureCategory.PersonalEmail] = ExposureRisk.Medium,
        };

    // ── Query ──────────────────────────────────────────────────────────────

    /// <summary>
    /// <para><c>TenantId</c>: identificador do tenant (obrigatório).</para>
    /// <para><c>MaxArtifacts</c>: número máximo de artefactos a varrer (1–2000, padrão 500).</para>
    /// <para><c>MinRiskLevel</c>: nível mínimo de risco a incluir no relatório (padrão Medium).</para>
    /// </summary>
    public sealed record Query(
        string TenantId,
        int MaxArtifacts = 500,
        ExposureRisk MinRiskLevel = ExposureRisk.Medium) : IQuery<Response>;

    // ── Validator ──────────────────────────────────────────────────────────

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(q => q.TenantId).NotEmpty();
            RuleFor(q => q.MaxArtifacts).InclusiveBetween(1, 2000);
        }
    }

    // ── Value objects ──────────────────────────────────────────────────────

    /// <summary>Artefacto com risco de exposição de segredos detectado.</summary>
    public sealed record AffectedArtifact(
        string ArtifactId,
        string ArtifactType,
        string ServiceName,
        string Title,
        ExposureRisk ExposureRisk,
        IReadOnlyList<ExposureCategory> DetectedCategories);

    /// <summary>Relatório de risco de exposição de segredos do tenant.</summary>
    public sealed record Response(
        DateTimeOffset GeneratedAt,
        string TenantId,
        int TotalScanned,
        int TotalAtRisk,
        IReadOnlyList<AffectedArtifact> AffectedArtifacts,
        IReadOnlyDictionary<string, int> CategoryDistribution,
        IReadOnlyList<string> TopServicesAtRisk);

    // ── Handler ────────────────────────────────────────────────────────────

    public sealed class Handler : IQueryHandler<Query, Response>
    {
        private readonly ISecretsExposureReader _reader;
        private readonly IDateTimeProvider _clock;

        public Handler(ISecretsExposureReader reader, IDateTimeProvider clock)
        {
            _reader = Guard.Against.Null(reader);
            _clock = Guard.Against.Null(clock);
        }

        public async Task<Result<Response>> Handle(Query query, CancellationToken cancellationToken)
        {
            Guard.Against.NullOrWhiteSpace(query.TenantId);

            var now = _clock.UtcNow;
            var artifacts = await _reader.ListArtifactTextsAsync(query.TenantId, query.MaxArtifacts, cancellationToken);

            var affected = new List<AffectedArtifact>();

            foreach (var artifact in artifacts)
            {
                var detected = ScanContent(artifact.Content);
                if (detected.Count == 0) continue;

                var maxRisk = detected.Max(c => CategoryRisk[c]);
                if (maxRisk < query.MinRiskLevel) continue;

                affected.Add(new AffectedArtifact(
                    ArtifactId: artifact.ArtifactId,
                    ArtifactType: artifact.ArtifactType,
                    ServiceName: artifact.ServiceName,
                    Title: artifact.Title,
                    ExposureRisk: maxRisk,
                    DetectedCategories: detected));
            }

            // Distribuição por categoria
            var categoryDist = Enum.GetValues<ExposureCategory>()
                .ToDictionary(
                    c => c.ToString(),
                    c => affected.Count(a => a.DetectedCategories.Contains(c)));

            // Top serviços com artefactos em risco
            var topServices = affected
                .GroupBy(a => a.ServiceName, StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(g => g.Max(a => a.ExposureRisk))
                .ThenByDescending(g => g.Count())
                .Take(10)
                .Select(g => g.Key)
                .ToList();

            return Result<Response>.Success(new Response(
                GeneratedAt: now,
                TenantId: query.TenantId,
                TotalScanned: artifacts.Count,
                TotalAtRisk: affected.Count,
                AffectedArtifacts: affected,
                CategoryDistribution: categoryDist,
                TopServicesAtRisk: topServices));
        }

        // ── Varredura de conteúdo por padrões de segredos ──────────────────

        private static IReadOnlyList<ExposureCategory> ScanContent(string content)
        {
            if (string.IsNullOrWhiteSpace(content)) return [];

            var detected = new List<ExposureCategory>();
            if (ApiKeyRegex.IsMatch(content)) detected.Add(ExposureCategory.ApiKey);
            if (JwtTokenRegex.IsMatch(content)) detected.Add(ExposureCategory.JwtToken);
            if (ConnectionStringRegex.IsMatch(content)) detected.Add(ExposureCategory.ConnectionString);
            if (PrivateIpRegex.IsMatch(content)) detected.Add(ExposureCategory.PrivateIp);
            if (PersonalEmailRegex.IsMatch(content)) detected.Add(ExposureCategory.PersonalEmail);
            return detected;
        }
    }
}
