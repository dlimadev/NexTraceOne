using FluentValidation;
using Microsoft.Extensions.Logging;
using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;
using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions.SemanticKernel;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Features.AIAgents.DocAgent;

/// <summary>
/// Agente de documentação inteligente — gera, melhora e valida documentação técnica.
/// Usa Semantic Kernel para orquestração e LLM para geração de texto.
/// </summary>
public static class DocAgent
{
    public sealed record Command(
        string Content,
        string DocType,
        string? TargetAudience = null,
        string? Style = null) : ICommand<Response>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Content).NotEmpty().MaximumLength(50_000);
            RuleFor(x => x.DocType).NotEmpty().MaximumLength(100);
        }
    }

    public sealed record Response(
        string GeneratedDoc,
        string QualityScore,
        List<string> Suggestions,
        List<string> Warnings);

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
                return Error.NotFound("AI.ProviderNotFound", "No AI provider available for DocAgent.");
            }

            var kernel = kernelService.CreateKernel(provider.ProviderId, provider.ProviderId);

            var systemPrompt = BuildSystemPrompt(request.DocType, request.TargetAudience, request.Style);
            var messages = new List<ChatMessage>
            {
                new("user", $"Generate documentation for the following content:\n\n{request.Content}")
            };

            var generated = await kernelService.ExecuteChatAsync(kernel, systemPrompt, messages, cancellationToken);

            if (string.IsNullOrWhiteSpace(generated))
            {
                return Error.Business("AI.DocGenerationFailed", "Failed to generate documentation.");
            }

            // Quick quality heuristic
            var qualityScore = EstimateQuality(generated);
            var suggestions = ExtractSuggestions(generated);
            var warnings = ExtractWarnings(generated, request.Content);

            logger.LogInformation(
                "DocAgent generated {DocType} doc with quality {Quality} at {Timestamp}",
                request.DocType, qualityScore, dateTimeProvider.UtcNow);

            return new Response(generated, qualityScore, suggestions, warnings);
        }

        private static string BuildSystemPrompt(string docType, string? audience, string? style)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("You are a technical documentation expert.");
            sb.AppendLine($"Generate {docType} documentation that is clear, accurate, and complete.");
            if (!string.IsNullOrWhiteSpace(audience))
                sb.AppendLine($"Target audience: {audience}.");
            if (!string.IsNullOrWhiteSpace(style))
                sb.AppendLine($"Writing style: {style}.");
            sb.AppendLine("Include: overview, key concepts, examples, and any warnings or caveats.");
            sb.AppendLine("Output ONLY the documentation text without markdown code blocks wrapping the entire response.");
            return sb.ToString();
        }

        private static string EstimateQuality(string doc)
        {
            var score = 70;
            if (doc.Contains("Example", StringComparison.OrdinalIgnoreCase)) score += 10;
            if (doc.Contains("Warning", StringComparison.OrdinalIgnoreCase) || doc.Contains("Caveat", StringComparison.OrdinalIgnoreCase)) score += 5;
            if (doc.Contains("Overview", StringComparison.OrdinalIgnoreCase)) score += 5;
            if (doc.Length > 500) score += 5;
            if (doc.Length < 100) score -= 20;
            return Math.Min(score, 100).ToString();
        }

        private static List<string> ExtractSuggestions(string doc)
        {
            var suggestions = new List<string>();
            if (!doc.Contains("Example", StringComparison.OrdinalIgnoreCase))
                suggestions.Add("Consider adding practical examples.");
            if (!doc.Contains("Diagram", StringComparison.OrdinalIgnoreCase) && !doc.Contains("Figure", StringComparison.OrdinalIgnoreCase))
                suggestions.Add("Visual diagrams could improve comprehension.");
            if (doc.Length < 300)
                suggestions.Add("Documentation seems brief; consider expanding sections.");
            return suggestions;
        }

        private static List<string> ExtractWarnings(string doc, string original)
        {
            var warnings = new List<string>();
            if (doc.Length > original.Length * 3)
                warnings.Add("Generated documentation is significantly longer than source; verify accuracy.");
            return warnings;
        }
    }
}
