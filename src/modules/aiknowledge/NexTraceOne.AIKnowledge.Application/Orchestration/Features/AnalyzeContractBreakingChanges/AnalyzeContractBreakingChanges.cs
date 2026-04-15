using Ardalis.GuardClauses;
using FluentValidation;
using Microsoft.Extensions.Logging;
using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;
using NexTraceOne.AIKnowledge.Domain.ExternalAI.Ports;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Orchestration.Features.AnalyzeContractBreakingChanges;

/// <summary>
/// Feature E-N01 — AI Diff para Contratos.
///
/// Analisa a diferença semântica entre duas versões de um contrato (REST, SOAP, AsyncAPI, etc.)
/// e identifica breaking changes, non-breaking changes e sugere acções de mitigação.
///
/// A IA retorna:
///   - Lista de breaking changes (com severidade e campo afectado)
///   - Lista de non-breaking changes
///   - Score de impacto [0-100]
///   - Sugestão de bump de versão semântica (major/minor/patch)
///   - Lista de acções de mitigação recomendadas
///   - Mensagem resumida para aprovação/revisão
///
/// Estrutura VSA: Command + Validator + Handler + Response em ficheiro único.
/// </summary>
public static class AnalyzeContractBreakingChanges
{
    /// <summary>Um item de mudança encontrado no diff do contrato.</summary>
    public sealed record ContractChangeItem(
        /// <summary>Tipo de mudança: 'Breaking' | 'NonBreaking' | 'Deprecation'.</summary>
        string ChangeType,
        /// <summary>Campo, endpoint, operação ou schema afectado.</summary>
        string AffectedField,
        /// <summary>Severidade: 'Critical' | 'High' | 'Medium' | 'Low'.</summary>
        string Severity,
        /// <summary>Descrição técnica da mudança.</summary>
        string Description);

    /// <summary>Comando para análise de diff de contrato por IA.</summary>
    public sealed record Command(
        /// <summary>Identificador do tenant.</summary>
        string TenantId,
        /// <summary>Identificador do API asset/contrato.</summary>
        Guid ApiAssetId,
        /// <summary>Nome do serviço publicador.</summary>
        string ServiceName,
        /// <summary>Tipo de contrato: 'REST' | 'SOAP' | 'AsyncAPI' | 'EventContract'.</summary>
        string ContractType,
        /// <summary>Versão "de" (versão base anterior).</summary>
        string FromVersion,
        /// <summary>Versão "para" (versão candidata nova).</summary>
        string ToVersion,
        /// <summary>Conteúdo completo do contrato base (OpenAPI YAML/JSON, WSDL, AsyncAPI, etc.).</summary>
        string BaseContractContent,
        /// <summary>Conteúdo completo do contrato candidato.</summary>
        string CandidateContractContent,
        /// <summary>Ambiente alvo de promoção (ex: 'Production', 'Staging').</summary>
        string? TargetEnvironment,
        /// <summary>Provider de IA preferido (null para usar routing padrão).</summary>
        string? PreferredProvider) : ICommand<Response>;

    /// <summary>Validador do comando de análise de diff de contrato.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        private const int MaxContentLength = 150_000;

        public Validator()
        {
            RuleFor(x => x.TenantId).NotEmpty().WithMessage("TenantId is required.");
            RuleFor(x => x.ApiAssetId).NotEmpty();
            RuleFor(x => x.ServiceName).NotEmpty().MaximumLength(200);
            RuleFor(x => x.ContractType).NotEmpty().MaximumLength(50)
                .Must(t => t is "REST" or "SOAP" or "AsyncAPI" or "EventContract" or "Other")
                .WithMessage("ContractType must be REST, SOAP, AsyncAPI, EventContract, or Other.");
            RuleFor(x => x.FromVersion).NotEmpty().MaximumLength(50);
            RuleFor(x => x.ToVersion).NotEmpty().MaximumLength(50);
            RuleFor(x => x.BaseContractContent).NotEmpty().MaximumLength(MaxContentLength);
            RuleFor(x => x.CandidateContractContent).NotEmpty().MaximumLength(MaxContentLength);
        }
    }

    /// <summary>
    /// Handler que envia as duas versões do contrato para análise diferencial por IA.
    /// Constrói prompt estruturado com ambos os contratos e extrai mudanças,
    /// score de impacto, sugestão de versão e acções de mitigação.
    /// </summary>
    public sealed class Handler(
        IExternalAIRoutingPort externalAiRoutingPort,
        IContractGroundingReader contractGroundingReader,
        IDateTimeProvider dateTimeProvider,
        ILogger<Handler> logger) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);
            Guard.Against.NullOrWhiteSpace(request.TenantId);

            var analysedAt = dateTimeProvider.UtcNow;

            // ── Carregar contexto adicional de versões publicadas do contrato ──
            string contextualVersionInfo = string.Empty;
            try
            {
                var existingVersions = await contractGroundingReader.FindContractVersionsAsync(
                    contractVersionId: null,
                    apiAssetId: request.ApiAssetId,
                    searchTerm: null,
                    maxResults: 5,
                    ct: cancellationToken);

                if (existingVersions.Count > 0)
                {
                    contextualVersionInfo = string.Join('\n', existingVersions
                        .Select(v => $"- v{v.Version} [{v.LifecycleState}] locked={v.IsLocked}"));
                }
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex,
                    "Could not load contract version context for ApiAsset={ApiAssetId}", request.ApiAssetId);
            }

            var baseSnippet = TruncateContent(request.BaseContractContent, 4_000);
            var candidateSnippet = TruncateContent(request.CandidateContractContent, 4_000);

            var context = BuildContext(request, contextualVersionInfo, baseSnippet, candidateSnippet);
            const string query =
                "Analyze the differences between the two contract versions provided. " +
                "For each breaking change, output: 'BREAKING: <severity (Critical/High/Medium/Low)> | <affected field/endpoint/schema> | <description>'. " +
                "For each non-breaking change, output: 'NONBREAKING: <affected field/endpoint/schema> | <description>'. " +
                "For each deprecation, output: 'DEPRECATION: <affected field/endpoint> | <description>'. " +
                "Then output: 'IMPACT_SCORE: <0-100>' where 100 means full incompatibility. " +
                "Then output: 'VERSION_BUMP: major' or 'minor' or 'patch' based on semver rules. " +
                "Then for each recommended mitigation, output: 'MITIGATION: <action>'. " +
                "Then output: 'SUMMARY: <one paragraph summary for reviewers>'. " +
                "Be precise about specific field names, HTTP methods, response codes, or schema properties changed.";

            string aiContent;
            string providerUsed;
            try
            {
                aiContent = await externalAiRoutingPort.RouteQueryAsync(
                    context, query, request.PreferredProvider, "contract-diff-analysis",
                    cancellationToken: cancellationToken);
                providerUsed = request.PreferredProvider ?? "default";

                logger.LogInformation(
                    "Contract diff AI analysis complete. ApiAsset={ApiAssetId} From={From} To={To}",
                    request.ApiAssetId, request.FromVersion, request.ToVersion);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex,
                    "AI provider unavailable for contract diff. ApiAsset={ApiAssetId} Tenant={TenantId}",
                    request.ApiAssetId, request.TenantId);
                aiContent = string.Empty;
                providerUsed = "fallback";
            }

            return ParseResponse(request, aiContent, analysedAt, providerUsed);
        }

        private static string BuildContext(
            Command request,
            string contextualVersionInfo,
            string baseSnippet,
            string candidateSnippet)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"Service: {request.ServiceName}");
            sb.AppendLine($"Contract Type: {request.ContractType}");
            sb.AppendLine($"From Version: {request.FromVersion}");
            sb.AppendLine($"To Version: {request.ToVersion}");

            if (!string.IsNullOrWhiteSpace(request.TargetEnvironment))
                sb.AppendLine($"Target Environment: {request.TargetEnvironment}");

            if (!string.IsNullOrWhiteSpace(contextualVersionInfo))
            {
                sb.AppendLine("\nPublished versions on record:");
                sb.AppendLine(contextualVersionInfo);
            }

            sb.AppendLine("\n=== BASE CONTRACT ===");
            sb.AppendLine(baseSnippet);
            sb.AppendLine("\n=== CANDIDATE CONTRACT ===");
            sb.AppendLine(candidateSnippet);

            return sb.ToString();
        }

        private static Response ParseResponse(
            Command request,
            string aiContent,
            DateTimeOffset analysedAt,
            string providerUsed)
        {
            var breakingChanges = new List<ContractChangeItem>();
            var nonBreakingChanges = new List<ContractChangeItem>();
            var mitigations = new List<string>();
            var impactScore = 0;
            var versionBump = "patch";
            var summary = string.Empty;

            if (!string.IsNullOrWhiteSpace(aiContent))
            {
                foreach (var line in aiContent.Split('\n',
                    StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    if (line.StartsWith("BREAKING:", StringComparison.OrdinalIgnoreCase))
                    {
                        var parts = line["BREAKING:".Length..].Split('|');
                        if (parts.Length >= 3)
                            breakingChanges.Add(new ContractChangeItem(
                                "Breaking", parts[1].Trim(), parts[0].Trim(), parts[2].Trim()));
                    }
                    else if (line.StartsWith("NONBREAKING:", StringComparison.OrdinalIgnoreCase))
                    {
                        var parts = line["NONBREAKING:".Length..].Split('|');
                        if (parts.Length >= 2)
                            nonBreakingChanges.Add(new ContractChangeItem(
                                "NonBreaking", parts[0].Trim(), "Low", parts[1].Trim()));
                    }
                    else if (line.StartsWith("DEPRECATION:", StringComparison.OrdinalIgnoreCase))
                    {
                        var parts = line["DEPRECATION:".Length..].Split('|');
                        if (parts.Length >= 2)
                            nonBreakingChanges.Add(new ContractChangeItem(
                                "Deprecation", parts[0].Trim(), "Medium", parts[1].Trim()));
                    }
                    else if (line.StartsWith("IMPACT_SCORE:", StringComparison.OrdinalIgnoreCase))
                    {
                        var scoreStr = line["IMPACT_SCORE:".Length..].Trim();
                        if (int.TryParse(scoreStr, out var parsed) && parsed >= 0 && parsed <= 100)
                            impactScore = parsed;
                    }
                    else if (line.StartsWith("VERSION_BUMP:", StringComparison.OrdinalIgnoreCase))
                    {
                        var bump = line["VERSION_BUMP:".Length..].Trim().ToLowerInvariant();
                        if (bump is "major" or "minor" or "patch")
                            versionBump = bump;
                    }
                    else if (line.StartsWith("MITIGATION:", StringComparison.OrdinalIgnoreCase))
                    {
                        mitigations.Add(line["MITIGATION:".Length..].Trim());
                    }
                    else if (line.StartsWith("SUMMARY:", StringComparison.OrdinalIgnoreCase))
                    {
                        summary = line["SUMMARY:".Length..].Trim();
                    }
                }
            }

            // Score mínimo determinístico baseado em breaking changes quando IA não respondeu
            if (impactScore == 0 && breakingChanges.Count > 0)
                impactScore = Math.Min(100, breakingChanges.Count * 20);

            // Version bump determinístico baseado em breaking changes
            if (versionBump == "patch" && breakingChanges.Any(c => c.ChangeType == "Breaking"))
                versionBump = "major";
            else if (versionBump == "patch" && nonBreakingChanges.Count > 0)
                versionBump = "minor";

            return new Response(
                ApiAssetId: request.ApiAssetId,
                ServiceName: request.ServiceName,
                ContractType: request.ContractType,
                FromVersion: request.FromVersion,
                ToVersion: request.ToVersion,
                BreakingChanges: breakingChanges.AsReadOnly(),
                NonBreakingChanges: nonBreakingChanges.AsReadOnly(),
                ImpactScore: impactScore,
                RecommendedVersionBump: versionBump,
                MitigationActions: mitigations.AsReadOnly(),
                Summary: summary,
                AnalysedAt: analysedAt,
                ProviderUsed: providerUsed);
        }

        private static string TruncateContent(string content, int maxChars)
            => content.Length > maxChars ? content[..maxChars] + "\n... [truncated]" : content;
    }

    /// <summary>Resposta da análise de diff de contrato por IA.</summary>
    public sealed record Response(
        Guid ApiAssetId,
        string ServiceName,
        string ContractType,
        string FromVersion,
        string ToVersion,
        IReadOnlyList<ContractChangeItem> BreakingChanges,
        IReadOnlyList<ContractChangeItem> NonBreakingChanges,
        /// <summary>Score de impacto das mudanças [0-100]. 0=sem impacto, 100=incompatibilidade total.</summary>
        int ImpactScore,
        /// <summary>Sugestão de bump de versão semântica: 'major', 'minor' ou 'patch'.</summary>
        string RecommendedVersionBump,
        IReadOnlyList<string> MitigationActions,
        string Summary,
        DateTimeOffset AnalysedAt,
        string ProviderUsed);
}
