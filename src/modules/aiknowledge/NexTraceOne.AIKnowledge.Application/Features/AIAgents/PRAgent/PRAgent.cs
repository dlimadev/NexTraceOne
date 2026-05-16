using FluentValidation;
using Microsoft.Extensions.Logging;
using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;
using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions.SemanticKernel;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Features.AIAgents.PRAgent;

/// <summary>
/// Agente de revisão de código (PR Review) — analisa diffs e fornece feedback estruturado.
/// Usa Semantic Kernel para orquestração e LLM para análise de código.
/// </summary>
public static class PRAgent
{
    public sealed record Command(
        string Diff,
        string? Title = null,
        string? Description = null,
        string? Language = null) : ICommand<Response>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Diff).NotEmpty().MaximumLength(100_000);
        }
    }

    public sealed record ReviewComment(
        string FilePath,
        int LineNumber,
        string Severity,
        string Message,
        string? Suggestion);

    public sealed record Response(
        string Summary,
        List<ReviewComment> Comments,
        string OverallScore,
        List<string> Recommendations);

    internal sealed class Handler(
        IAiKernelService kernelService,
        IAiProviderFactory providerFactory,
        IDateTimeProvider dateTimeProvider,
        ILogger<Handler> logger) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            var provider = providerFactory.GetChatProvider("ollama")
                ?? providerFactory.GetChatProvider("openai");

            if (provider is null)
            {
                return Error.NotFound("AI.ProviderNotFound", "No AI provider available for PRAgent.");
            }

            var kernel = kernelService.CreateKernel(provider.ProviderId, provider.ProviderId);

            var systemPrompt = BuildSystemPrompt(request.Language);
            var userPrompt = BuildUserPrompt(request);
            var messages = new List<ChatMessage> { new("user", userPrompt) };

            var generated = await kernelService.ExecuteChatAsync(kernel, systemPrompt, messages, cancellationToken);

            if (string.IsNullOrWhiteSpace(generated))
            {
                return Error.Business("AI.PRReviewFailed", "Failed to generate code review.");
            }

            var (comments, summary, score, recommendations) = ParseReviewOutput(generated);

            logger.LogInformation(
                "PRAgent reviewed diff with score {Score} and {CommentCount} comments at {Timestamp}",
                score, comments.Count, dateTimeProvider.UtcNow);

            return new Response(summary, comments, score, recommendations);
        }

        private static string BuildSystemPrompt(string? language)
        {
            var lang = string.IsNullOrWhiteSpace(language) ? "the programming language" : language;
            return $"You are a senior code reviewer. Analyze the provided diff and provide structured feedback.\n"
                + $"Review for: correctness, security, performance, maintainability, and style.\n"
                + $"Language context: {lang}.\n"
                + "Output format:\n"
                + "SUMMARY: <brief summary>\n"
                + "SCORE: <0-100>\n"
                + "RECOMMENDATIONS:\n"
                + "- <recommendation 1>\n"
                + "- <recommendation 2>\n"
                + "COMMENTS:\n"
                + "FILE: <filepath>\n"
                + "LINE: <line>\n"
                + "SEVERITY: <Info|Warning|Critical>\n"
                + "MESSAGE: <message>\n"
                + "SUGGESTION: <optional suggestion>\n"
                + "---\n";
        }

        private static string BuildUserPrompt(Command request)
        {
            var sb = new System.Text.StringBuilder();
            if (!string.IsNullOrWhiteSpace(request.Title))
                sb.AppendLine($"PR Title: {request.Title}");
            if (!string.IsNullOrWhiteSpace(request.Description))
                sb.AppendLine($"PR Description: {request.Description}");
            sb.AppendLine("Diff:");
            sb.AppendLine(request.Diff);
            return sb.ToString();
        }

        private static (List<ReviewComment>, string, string, List<string>) ParseReviewOutput(string output)
        {
            var comments = new List<ReviewComment>();
            var summary = "Review completed.";
            var score = "75";
            var recommendations = new List<string>();

            var lines = output.Split('\n');
            string? currentFile = null;
            int currentLine = 0;
            string? currentSeverity = null;
            string? currentMessage = null;
            string? currentSuggestion = null;

            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith("SUMMARY:", StringComparison.OrdinalIgnoreCase))
                {
                    summary = trimmed[8..].Trim();
                }
                else if (trimmed.StartsWith("SCORE:", StringComparison.OrdinalIgnoreCase))
                {
                    score = trimmed[6..].Trim();
                }
                else if (trimmed.StartsWith("RECOMMENDATIONS:", StringComparison.OrdinalIgnoreCase))
                {
                    // next lines are recommendations until COMMENTS
                }
                else if (trimmed.StartsWith("- ") && currentMessage is null)
                {
                    recommendations.Add(trimmed[2..].Trim());
                }
                else if (trimmed.StartsWith("FILE:", StringComparison.OrdinalIgnoreCase))
                {
                    currentFile = trimmed[5..].Trim();
                }
                else if (trimmed.StartsWith("LINE:", StringComparison.OrdinalIgnoreCase))
                {
                    _ = int.TryParse(trimmed[5..].Trim(), out currentLine);
                }
                else if (trimmed.StartsWith("SEVERITY:", StringComparison.OrdinalIgnoreCase))
                {
                    currentSeverity = trimmed[9..].Trim();
                }
                else if (trimmed.StartsWith("MESSAGE:", StringComparison.OrdinalIgnoreCase))
                {
                    currentMessage = trimmed[8..].Trim();
                }
                else if (trimmed.StartsWith("SUGGESTION:", StringComparison.OrdinalIgnoreCase))
                {
                    currentSuggestion = trimmed[11..].Trim();
                }
                else if (trimmed == "---" && currentMessage is not null)
                {
                    comments.Add(new ReviewComment(
                        currentFile ?? "unknown",
                        currentLine,
                        currentSeverity ?? "Info",
                        currentMessage,
                        currentSuggestion));
                    currentFile = null;
                    currentLine = 0;
                    currentSeverity = null;
                    currentMessage = null;
                    currentSuggestion = null;
                }
            }

            // If no structured comments, add a generic one
            if (comments.Count == 0 && !string.IsNullOrWhiteSpace(output))
            {
                comments.Add(new ReviewComment("general", 0, "Info", output[..Math.Min(output.Length, 500)], null));
            }

            return (comments, summary, score, recommendations);
        }
    }
}
