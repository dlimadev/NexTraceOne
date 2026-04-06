using System.Text;
using System.Text.Json;
using Ardalis.GuardClauses;
using FluentValidation;
using Microsoft.Extensions.Logging;
using NexTraceOne.AIKnowledge.Domain.ExternalAI.Ports;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Orchestration.Features.EvaluateArchitectureFitness;

/// <summary>
/// Feature: EvaluateArchitectureFitness — avalia conformidade do código gerado ou de
/// um serviço contra as fitness functions de arquitectura do NexTraceOne.
///
/// Fitness functions avaliadas:
/// - Isolamento de bounded context
/// - Direcção das dependências (Domain → Application → Infrastructure → API)
/// - Convenções de nomenclatura (Commands, Queries, Handlers, Entities)
/// - Imutabilidade de entidades de domínio
/// - Conformidade CQRS
/// - Segurança baseline (autorizações, sem segredos hardcoded)
/// - Testabilidade (injecção de dependências)
///
/// A IA (architecture-fitness-agent) produz um relatório estruturado JSON com score
/// e lista de violações ordenadas por severidade.
/// </summary>
public static class EvaluateArchitectureFitness
{
    /// <summary>Representa um ficheiro de código a avaliar.</summary>
    public sealed record CodeFile(string FileName, string Content);

    /// <summary>Comando para avaliar fitness de arquitectura.</summary>
    public sealed record Command(
        Guid? TargetId,
        string ServiceName,
        IReadOnlyList<CodeFile> Files,
        string? PreferredProvider = null) : ICommand<Response>;

    /// <summary>Validação do comando.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ServiceName).NotEmpty().MaximumLength(100);
            RuleFor(x => x.Files).NotEmpty().WithMessage("At least one code file is required.");
            RuleFor(x => x.Files.Count).LessThanOrEqualTo(50).WithMessage("Maximum 50 files per evaluation.");
        }
    }

    /// <summary>Handler que invoca o architecture-fitness-agent e processa o resultado.</summary>
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
                    capability: "architecture-fitness",
                    cancellationToken: cancellationToken);

                isFallback = aiResponse.StartsWith("[FALLBACK_PROVIDER_UNAVAILABLE]", StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex,
                    "AI provider unavailable for EvaluateArchitectureFitness. Service={ServiceName}",
                    request.ServiceName);
                return BuildFallbackResponse(request);
            }

            return isFallback
                ? BuildFallbackResponse(request)
                : ParseAiResponse(aiResponse, request.ServiceName);
        }

        private static string BuildContext(Command request)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Service: {request.ServiceName}");
            sb.AppendLine($"Files to evaluate: {request.Files.Count}");
            sb.AppendLine("Architecture: NexTraceOne modular monolith, DDD, Clean Architecture, CQRS via MediatR, VSA (Vertical Slice Architecture)");
            sb.AppendLine("Stack: .NET 10, ASP.NET Core, EF Core, FluentValidation, MediatR");
            return sb.ToString();
        }

        private static string BuildPrompt(Command request)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Evaluate the following {request.Files.Count} code file(s) for architecture fitness:");
            sb.AppendLine();

            foreach (var file in request.Files.Take(20)) // limit to 20 files to avoid token overflow
            {
                sb.AppendLine($"=== FILE: {file.FileName} ===");
                var content = file.Content.Length > 3000 ? file.Content[..3000] + "\n... [truncated]" : file.Content;
                sb.AppendLine(content);
                sb.AppendLine();
            }

            sb.AppendLine("Output ONLY valid JSON with no markdown, no explanation. Format: { \"overallFitness\": \"Good\", \"score\": 80, \"violations\": [{ \"rule\": \"...\", \"file\": \"...\", \"severity\": \"High|Medium|Low\", \"description\": \"...\", \"suggestion\": \"...\" }], \"passedChecks\": [\"...\"] }");
            return sb.ToString();
        }

        private static Result<Response> ParseAiResponse(string aiResponse, string serviceName)
        {
            try
            {
                var json = ExtractJson(aiResponse);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                var overallFitness = root.TryGetProperty("overallFitness", out var f) ? f.GetString() ?? "Unknown" : "Unknown";
                var score = root.TryGetProperty("score", out var s) ? s.GetInt32() : 0;

                var violations = new List<FitnessViolation>();
                if (root.TryGetProperty("violations", out var violationsEl))
                {
                    foreach (var v in violationsEl.EnumerateArray())
                    {
                        violations.Add(new FitnessViolation(
                            Rule: v.TryGetProperty("rule", out var r) ? r.GetString() ?? "" : "",
                            File: v.TryGetProperty("file", out var fi) ? fi.GetString() ?? "" : "",
                            Severity: v.TryGetProperty("severity", out var sv) ? sv.GetString() ?? "Low" : "Low",
                            Description: v.TryGetProperty("description", out var d) ? d.GetString() ?? "" : "",
                            Suggestion: v.TryGetProperty("suggestion", out var sg) ? sg.GetString() ?? "" : ""));
                    }
                }

                var passedChecks = new List<string>();
                if (root.TryGetProperty("passedChecks", out var passedEl))
                    foreach (var p in passedEl.EnumerateArray())
                    {
                        var val = p.GetString();
                        if (val is not null) passedChecks.Add(val);
                    }

                return Result<Response>.Success(new Response(
                    ServiceName: serviceName,
                    OverallFitness: overallFitness,
                    Score: score,
                    Violations: violations,
                    PassedChecks: passedChecks,
                    IsFallback: false));
            }
            catch
            {
                return BuildFallbackResponse(null!, serviceName);
            }
        }

        private static Result<Response> BuildFallbackResponse(Command request, string? serviceNameOverride = null)
        {
            var name = serviceNameOverride ?? request.ServiceName;
            return Result<Response>.Success(new Response(
                ServiceName: name,
                OverallFitness: "Pending",
                Score: 0,
                Violations: [],
                PassedChecks: [],
                IsFallback: true));
        }

        private static string ExtractJson(string response)
        {
            var start = response.IndexOf('{');
            var end = response.LastIndexOf('}');
            return start >= 0 && end > start ? response[start..(end + 1)] : response;
        }
    }

    /// <summary>Violação de fitness de arquitectura.</summary>
    public sealed record FitnessViolation(
        string Rule,
        string File,
        string Severity,
        string Description,
        string Suggestion);

    /// <summary>Resposta da avaliação de fitness de arquitectura.</summary>
    public sealed record Response(
        string ServiceName,
        string OverallFitness,
        int Score,
        IReadOnlyList<FitnessViolation> Violations,
        IReadOnlyList<string> PassedChecks,
        bool IsFallback);
}
