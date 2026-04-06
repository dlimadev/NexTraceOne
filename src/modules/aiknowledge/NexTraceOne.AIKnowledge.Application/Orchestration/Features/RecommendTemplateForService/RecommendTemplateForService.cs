using System.Text;
using System.Text.Json;
using Ardalis.GuardClauses;
using FluentValidation;
using Microsoft.Extensions.Logging;
using NexTraceOne.AIKnowledge.Domain.ExternalAI.Ports;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Orchestration.Features.RecommendTemplateForService;

/// <summary>
/// Feature: RecommendTemplateForService — a IA analisa a descrição do serviço e recomenda
/// os templates mais adequados do catálogo, com justificação e ranking.
///
/// Integra no Step 1 do wizard de criação de serviços para guiar developers
/// ao template ideal sem que precisem conhecer todo o catálogo.
/// </summary>
public static class RecommendTemplateForService
{
    /// <summary>Informações de um template disponível para análise.</summary>
    public sealed record TemplateInfo(
        string Slug,
        string DisplayName,
        string Description,
        string ServiceType,
        string PrimaryLanguage,
        IReadOnlyList<string> Tags);

    /// <summary>Comando para recomendar templates.</summary>
    public sealed record Command(
        string ServiceDescription,
        string? PreferredLanguage,
        string? Domain,
        string? TeamName,
        IReadOnlyList<TemplateInfo> AvailableTemplates,
        int MaxRecommendations = 3,
        string? PreferredProvider = null) : ICommand<Response>;

    /// <summary>Validação do comando.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ServiceDescription).NotEmpty().MaximumLength(2000);
            RuleFor(x => x.AvailableTemplates).NotEmpty().WithMessage("At least one template must be provided.");
            RuleFor(x => x.MaxRecommendations).InclusiveBetween(1, 10);
        }
    }

    /// <summary>Handler que invoca a IA para recomendar templates.</summary>
    public sealed class Handler(
        IExternalAIRoutingPort routingPort,
        ILogger<Handler> logger) : ICommandHandler<Command, Response>
    {
        private static readonly JsonSerializerOptions s_options = new() { WriteIndented = false };

        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var context = BuildContext(request);
            var prompt = BuildPrompt(request);

            string aiResponse;
            var isFallback = false;

            try
            {
                aiResponse = await routingPort.RouteQueryAsync(
                    context,
                    prompt,
                    request.PreferredProvider,
                    capability: "template-recommendation",
                    cancellationToken: cancellationToken);

                isFallback = aiResponse.StartsWith("[FALLBACK_PROVIDER_UNAVAILABLE]", StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex,
                    "AI provider unavailable for RecommendTemplateForService.");
                return BuildFallbackResponse(request);
            }

            return isFallback
                ? BuildFallbackResponse(request)
                : ParseRecommendations(aiResponse, request);
        }

        private static string BuildContext(Command request)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Platform: NexTraceOne Service Creation Studio");
            sb.AppendLine($"Available templates: {request.AvailableTemplates.Count}");
            if (request.PreferredLanguage is not null) sb.AppendLine($"Preferred language: {request.PreferredLanguage}");
            if (request.Domain is not null) sb.AppendLine($"Domain: {request.Domain}");
            return sb.ToString();
        }

        private static string BuildPrompt(Command request)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Analyze this service description and recommend the best template(s):");
            sb.AppendLine();
            sb.AppendLine($"Service description: {request.ServiceDescription}");
            if (request.PreferredLanguage is not null) sb.AppendLine($"Preferred language: {request.PreferredLanguage}");
            if (request.Domain is not null) sb.AppendLine($"Domain: {request.Domain}");
            sb.AppendLine();
            sb.AppendLine("Available templates:");

            foreach (var template in request.AvailableTemplates)
                sb.AppendLine($"- {template.Slug}: {template.DisplayName} ({template.ServiceType}, {template.PrimaryLanguage}) — {template.Description}");

            sb.AppendLine();
            sb.AppendLine($"Return the top {request.MaxRecommendations} template recommendations as JSON.");
            sb.AppendLine("Output ONLY valid JSON. Format: { \"recommendations\": [{ \"slug\": \"...\", \"score\": 95, \"reason\": \"...\", \"fitSummary\": \"...\", \"potentialGaps\": [\"...\"] }] }");
            return sb.ToString();
        }

        private static Result<Response> ParseRecommendations(string aiResponse, Command request)
        {
            try
            {
                var json = ExtractJson(aiResponse);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                var recommendations = new List<TemplateRecommendation>();
                if (root.TryGetProperty("recommendations", out var recs))
                {
                    foreach (var rec in recs.EnumerateArray().Take(request.MaxRecommendations))
                    {
                        var slug = rec.TryGetProperty("slug", out var s) ? s.GetString() ?? "" : "";
                        var score = rec.TryGetProperty("score", out var sc) ? sc.GetInt32() : 0;
                        var reason = rec.TryGetProperty("reason", out var r) ? r.GetString() ?? "" : "";
                        var fitSummary = rec.TryGetProperty("fitSummary", out var fs) ? fs.GetString() ?? "" : "";

                        var gaps = new List<string>();
                        if (rec.TryGetProperty("potentialGaps", out var gapsEl))
                            foreach (var g in gapsEl.EnumerateArray())
                            {
                                var val = g.GetString();
                                if (val is not null) gaps.Add(val);
                            }

                        var matchedTemplate = request.AvailableTemplates.FirstOrDefault(t => t.Slug == slug);
                        recommendations.Add(new TemplateRecommendation(
                            Slug: slug,
                            DisplayName: matchedTemplate?.DisplayName ?? slug,
                            Score: score,
                            Reason: reason,
                            FitSummary: fitSummary,
                            PotentialGaps: gaps));
                    }
                }

                return Result<Response>.Success(new Response(
                    ServiceDescription: request.ServiceDescription,
                    Recommendations: recommendations.OrderByDescending(r => r.Score).ToList(),
                    IsFallback: false));
            }
            catch
            {
                return BuildFallbackResponse(request);
            }
        }

        private static Result<Response> BuildFallbackResponse(Command request)
        {
            var fallbacks = request.AvailableTemplates
                .Take(request.MaxRecommendations)
                .Select(t => new TemplateRecommendation(
                    Slug: t.Slug,
                    DisplayName: t.DisplayName,
                    Score: 50,
                    Reason: "AI recommendation unavailable. Showing first available templates.",
                    FitSummary: t.Description,
                    PotentialGaps: []))
                .ToList();

            return Result<Response>.Success(new Response(
                ServiceDescription: request.ServiceDescription,
                Recommendations: fallbacks,
                IsFallback: true));
        }

        private static string ExtractJson(string response)
        {
            var start = response.IndexOf('{');
            var end = response.LastIndexOf('}');
            return start >= 0 && end > start ? response[start..(end + 1)] : response;
        }
    }

    /// <summary>Recomendação de template.</summary>
    public sealed record TemplateRecommendation(
        string Slug,
        string DisplayName,
        int Score,
        string Reason,
        string FitSummary,
        IReadOnlyList<string> PotentialGaps);

    /// <summary>Resposta com recomendações de templates.</summary>
    public sealed record Response(
        string ServiceDescription,
        IReadOnlyList<TemplateRecommendation> Recommendations,
        bool IsFallback);
}
