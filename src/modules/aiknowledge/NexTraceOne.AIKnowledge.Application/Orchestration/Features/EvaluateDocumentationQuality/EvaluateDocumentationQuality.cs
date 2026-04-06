using System.Text;
using System.Text.Json;
using Ardalis.GuardClauses;
using FluentValidation;
using Microsoft.Extensions.Logging;
using NexTraceOne.AIKnowledge.Domain.ExternalAI.Ports;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Orchestration.Features.EvaluateDocumentationQuality;

/// <summary>
/// Feature: EvaluateDocumentationQuality — avalia a qualidade da documentação de
/// serviços gerados, contratos e scaffolds usando o documentation-quality-agent.
///
/// Dimensões avaliadas:
/// - Cobertura de XML doc comments
/// - Completude do README
/// - Qualidade das descrições de API (OpenAPI summary/description)
/// - Qualidade de comentários inline
/// - Documentação de erros e respostas HTTP
/// - Presença de CHANGELOG ou migration notes
///
/// Retorna um score por dimensão (0-100) e recomendações de melhoria.
/// </summary>
public static class EvaluateDocumentationQuality
{
    /// <summary>Ficheiro a avaliar para qualidade de documentação.</summary>
    public sealed record DocumentFile(string FileName, string Content, string FileType);

    /// <summary>Comando para avaliar qualidade de documentação.</summary>
    public sealed record Command(
        string ServiceName,
        IReadOnlyList<DocumentFile> Files,
        string? PreferredProvider = null) : ICommand<Response>;

    /// <summary>Validação do comando.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ServiceName).NotEmpty().MaximumLength(100);
            RuleFor(x => x.Files).NotEmpty().WithMessage("At least one file is required.");
        }
    }

    /// <summary>Handler que invoca o documentation-quality-agent e processa o resultado.</summary>
    public sealed class Handler(
        IExternalAIRoutingPort routingPort,
        ILogger<Handler> logger) : ICommandHandler<Command, Response>
    {
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
                    capability: "documentation-quality",
                    cancellationToken: cancellationToken);

                isFallback = aiResponse.StartsWith("[FALLBACK_PROVIDER_UNAVAILABLE]", StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex,
                    "AI provider unavailable for EvaluateDocumentationQuality. Service={ServiceName}",
                    request.ServiceName);
                return BuildFallbackResponse(request.ServiceName);
            }

            return isFallback
                ? BuildFallbackResponse(request.ServiceName)
                : ParseAiResponse(aiResponse, request.ServiceName);
        }

        private static string BuildContext(Command request)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Service: {request.ServiceName}");
            sb.AppendLine($"Files to evaluate: {request.Files.Count}");
            sb.AppendLine("Evaluate documentation quality according to NexTraceOne enterprise standards.");
            return sb.ToString();
        }

        private static string BuildPrompt(Command request)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Evaluate documentation quality for service '{request.ServiceName}' based on these files:");
            sb.AppendLine();

            foreach (var file in request.Files.Take(15))
            {
                sb.AppendLine($"=== {file.FileType.ToUpperInvariant()}: {file.FileName} ===");
                var content = file.Content.Length > 2000 ? file.Content[..2000] + "\n... [truncated]" : file.Content;
                sb.AppendLine(content);
                sb.AppendLine();
            }

            sb.AppendLine("Output ONLY valid JSON. Format: { \"overallScore\": 75, \"dimensions\": [{ \"name\": \"XML_DOC_COVERAGE\", \"score\": 80, \"gaps\": [\"missing summary on UserController.GetAll\"], \"recommendations\": [\"Add XML doc to all public methods\"] }] }");
            return sb.ToString();
        }

        private static Result<Response> ParseAiResponse(string aiResponse, string serviceName)
        {
            try
            {
                var json = ExtractJson(aiResponse);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                var overallScore = root.TryGetProperty("overallScore", out var s) ? s.GetInt32() : 0;
                var dimensions = new List<DocumentationDimension>();

                if (root.TryGetProperty("dimensions", out var dims))
                {
                    foreach (var dim in dims.EnumerateArray())
                    {
                        var gaps = new List<string>();
                        var recs = new List<string>();

                        if (dim.TryGetProperty("gaps", out var gapsEl))
                            foreach (var g in gapsEl.EnumerateArray())
                            {
                                var val = g.GetString();
                                if (val is not null) gaps.Add(val);
                            }

                        if (dim.TryGetProperty("recommendations", out var recsEl))
                            foreach (var r in recsEl.EnumerateArray())
                            {
                                var val = r.GetString();
                                if (val is not null) recs.Add(val);
                            }

                        dimensions.Add(new DocumentationDimension(
                            Name: dim.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "",
                            Score: dim.TryGetProperty("score", out var sc) ? sc.GetInt32() : 0,
                            Gaps: gaps,
                            Recommendations: recs));
                    }
                }

                return Result<Response>.Success(new Response(
                    ServiceName: serviceName,
                    OverallScore: overallScore,
                    QualityLevel: ScoreToLevel(overallScore),
                    Dimensions: dimensions,
                    IsFallback: false));
            }
            catch
            {
                return BuildFallbackResponse(serviceName);
            }
        }

        private static Result<Response> BuildFallbackResponse(string serviceName)
        {
            return Result<Response>.Success(new Response(
                ServiceName: serviceName,
                OverallScore: 0,
                QualityLevel: "Pending",
                Dimensions: [],
                IsFallback: true));
        }

        private static string ScoreToLevel(int score) => score switch
        {
            >= 90 => "Excellent",
            >= 75 => "Good",
            >= 60 => "Needs Improvement",
            _ => "Poor"
        };

        private static string ExtractJson(string response)
        {
            var start = response.IndexOf('{');
            var end = response.LastIndexOf('}');
            return start >= 0 && end > start ? response[start..(end + 1)] : response;
        }
    }

    /// <summary>Dimensão de qualidade de documentação.</summary>
    public sealed record DocumentationDimension(
        string Name,
        int Score,
        IReadOnlyList<string> Gaps,
        IReadOnlyList<string> Recommendations);

    /// <summary>Resposta da avaliação de qualidade de documentação.</summary>
    public sealed record Response(
        string ServiceName,
        int OverallScore,
        string QualityLevel,
        IReadOnlyList<DocumentationDimension> Dimensions,
        bool IsFallback);
}
