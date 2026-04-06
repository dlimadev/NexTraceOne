using Ardalis.GuardClauses;
using FluentValidation;
using Microsoft.Extensions.Logging;
using NexTraceOne.AIKnowledge.Domain.ExternalAI.Ports;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Orchestration.Features.ReviewContractDraft;

/// <summary>
/// Feature: ReviewContractDraft — agente de revisão automática de rascunhos de contratos.
/// A IA analisa o conteúdo do contrato e retorna feedback estruturado com score de qualidade,
/// problemas encontrados (por severidade e categoria), sugestões de melhoria e recomendação final.
/// Estrutura VSA: Command + Validator + Handler + Response em arquivo único.
/// </summary>
public static class ReviewContractDraft
{
    /// <summary>Problema encontrado durante a revisão do contrato.</summary>
    public sealed record ReviewIssueDto(string Severity, string Category, string Description);

    /// <summary>Comando para revisão de rascunho de contrato por IA.</summary>
    public sealed record Command(
        string TenantId,
        Guid DraftId,
        string ContractContent,
        string ContractType,
        string ServiceName,
        string? PreferredProvider) : ICommand<Response>;

    /// <summary>Validador do comando de revisão de contrato.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.TenantId).NotEmpty().WithMessage("TenantId is required for contract review.");
            RuleFor(x => x.DraftId).NotEmpty();
            RuleFor(x => x.ContractContent).NotEmpty().MaximumLength(100_000);
            RuleFor(x => x.ContractType).NotEmpty().MaximumLength(100);
            RuleFor(x => x.ServiceName).NotEmpty().MaximumLength(200);
        }
    }

    /// <summary>
    /// Handler que envia o rascunho de contrato para revisão por IA.
    /// Constrói prompt estruturado com o conteúdo do contrato e analisa a resposta
    /// para extrair score de qualidade, issues e sugestões.
    /// </summary>
    public sealed class Handler(
        IExternalAIRoutingPort externalAiRoutingPort,
        IDateTimeProvider dateTimeProvider,
        ILogger<Handler> logger) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);
            Guard.Against.NullOrWhiteSpace(request.TenantId);

            var reviewedAt = dateTimeProvider.UtcNow;
            var contentSnippet = request.ContractContent.Length > 5_000
                ? request.ContractContent[..5_000]
                : request.ContractContent;

            var context = $"Contract Type: {request.ContractType}\nService: {request.ServiceName}\n\nContract Content:\n{contentSnippet}";
            const string query =
                "Review this contract draft and provide structured feedback. " +
                "Provide QUALITY_SCORE: a number from 0 to 100. " +
                "For each issue, start with 'ISSUE:' followed by severity (Critical/Warning/Info), a pipe '|', category (Security/Documentation/Design/Compatibility), a pipe '|', description. " +
                "For each suggestion, start with 'SUGGESTION:' followed by the suggestion text. " +
                "Provide RECOMMENDATION: Approve, RequestChanges, or Reject. " +
                "Be specific and actionable.";

            string aiContent;
            string providerUsed;
            try
            {
                aiContent = await externalAiRoutingPort.RouteQueryAsync(
                    context, query, request.PreferredProvider, "contract-review", cancellationToken: cancellationToken);
                providerUsed = request.PreferredProvider ?? "default";
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "AI provider unavailable for contract review. TenantId={TenantId} DraftId={DraftId}",
                    request.TenantId, request.DraftId);
                aiContent = string.Empty;
                providerUsed = "fallback";
            }

            return ParseResponse(request.DraftId, request.ContractType, request.ServiceName, aiContent, reviewedAt, providerUsed);
        }

        private static Response ParseResponse(
            Guid draftId, string contractType, string serviceName,
            string aiContent, DateTimeOffset reviewedAt, string providerUsed)
        {
            var qualityScore = 50;
            var recommendation = "RequestChanges";
            var issues = new List<ReviewIssueDto>();
            var suggestions = new List<string>();

            if (string.IsNullOrWhiteSpace(aiContent))
            {
                return new Response(draftId, contractType, serviceName, qualityScore, issues.AsReadOnly(),
                    suggestions.AsReadOnly(), recommendation, reviewedAt, providerUsed);
            }

            foreach (var line in aiContent.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (line.StartsWith("QUALITY_SCORE:", StringComparison.OrdinalIgnoreCase))
                {
                    var scoreStr = line["QUALITY_SCORE:".Length..].Trim();
                    if (int.TryParse(scoreStr, out var parsed) && parsed >= 0 && parsed <= 100)
                        qualityScore = parsed;
                }
                else if (line.StartsWith("ISSUE:", StringComparison.OrdinalIgnoreCase))
                {
                    var parts = line["ISSUE:".Length..].Split('|');
                    if (parts.Length >= 3)
                        issues.Add(new ReviewIssueDto(parts[0].Trim(), parts[1].Trim(), parts[2].Trim()));
                }
                else if (line.StartsWith("SUGGESTION:", StringComparison.OrdinalIgnoreCase))
                {
                    suggestions.Add(line["SUGGESTION:".Length..].Trim());
                }
                else if (line.StartsWith("RECOMMENDATION:", StringComparison.OrdinalIgnoreCase))
                {
                    var rec = line["RECOMMENDATION:".Length..].Trim();
                    if (rec is "Approve" or "RequestChanges" or "Reject")
                        recommendation = rec;
                }
            }

            return new Response(draftId, contractType, serviceName, qualityScore, issues.AsReadOnly(),
                suggestions.AsReadOnly(), recommendation, reviewedAt, providerUsed);
        }
    }

    /// <summary>Resposta da revisão do contrato por IA.</summary>
    public sealed record Response(
        Guid DraftId,
        string ContractType,
        string ServiceName,
        int QualityScore,
        IReadOnlyList<ReviewIssueDto> Issues,
        IReadOnlyList<string> Suggestions,
        string Recommendation,
        DateTimeOffset ReviewedAt,
        string ProviderUsed);
}
