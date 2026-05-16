using FluentValidation;
using Microsoft.Extensions.Logging;
using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;
using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions.SemanticKernel;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Features.AIAgents.WebSearchAgent;

/// <summary>
/// Agente de pesquisa web — simula pesquisa e resume resultados usando LLM.
/// Phase 1: usa o LLM para gerar uma resposta informada com base no conhecimento do modelo.
/// Phase 2: integrar com Bing/Google Search APIs para resultados reais.
/// </summary>
public static class WebSearchAgent
{
    public sealed record Command(
        string Query,
        int? MaxResults = null,
        string? Focus = null) : ICommand<Response>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Query).NotEmpty().MaximumLength(2000);
        }
    }

    public sealed record SearchResult(
        string Title,
        string Url,
        string Snippet,
        string Source,
        string? PublishedDate);

    public sealed record Response(
        string Answer,
        List<SearchResult> Results,
        string Confidence,
        List<string> Sources);

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
                return Error.NotFound("AI.ProviderNotFound", "No AI provider available for WebSearchAgent.");
            }

            var kernel = kernelService.CreateKernel(provider.ProviderId, provider.ProviderId);

            var systemPrompt = BuildSystemPrompt(request.Focus);
            var userPrompt = BuildUserPrompt(request);
            var messages = new List<ChatMessage> { new("user", userPrompt) };

            var generated = await kernelService.ExecuteChatAsync(kernel, systemPrompt, messages, cancellationToken);

            if (string.IsNullOrWhiteSpace(generated))
            {
                return Error.Business("AI.WebSearchFailed", "Failed to generate search response.");
            }

            var (answer, results, confidence, sources) = ParseOutput(generated, request.Query);

            logger.LogInformation(
                "WebSearchAgent answered query with confidence {Confidence} at {Timestamp}",
                confidence, dateTimeProvider.UtcNow);

            return new Response(answer, results, confidence, sources);
        }

        private static string BuildSystemPrompt(string? focus)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("You are a research assistant. Answer the user's query comprehensively.");
            if (!string.IsNullOrWhiteSpace(focus))
                sb.AppendLine($"Focus area: {focus}.");
            sb.AppendLine("Structure your response as follows:");
            sb.AppendLine("ANSWER: <concise answer>");
            sb.AppendLine("CONFIDENCE: <High|Medium|Low>");
            sb.AppendLine("SOURCES:");
            sb.AppendLine("- <source name> (https://example.com)");
            sb.AppendLine("RESULTS:");
            sb.AppendLine("TITLE: <title>");
            sb.AppendLine("URL: <url>");
            sb.AppendLine("SNIPPET: <snippet>");
            sb.AppendLine("SOURCE: <source>");
            sb.AppendLine("---");
            return sb.ToString();
        }

        private static string BuildUserPrompt(Command request)
        {
            return $"Query: {request.Query}\nDesired result count: {request.MaxResults ?? 5}";
        }

        private static (string, List<SearchResult>, string, List<string>) ParseOutput(string output, string query)
        {
            var answer = output[..Math.Min(output.Length, 1000)];
            var confidence = "Medium";
            var sources = new List<string>();
            var results = new List<SearchResult>();

            var lines = output.Split('\n');
            string? currentTitle = null;
            string? currentUrl = null;
            string? currentSnippet = null;
            string? currentSource = null;

            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith("ANSWER:", StringComparison.OrdinalIgnoreCase))
                {
                    answer = trimmed[7..].Trim();
                }
                else if (trimmed.StartsWith("CONFIDENCE:", StringComparison.OrdinalIgnoreCase))
                {
                    confidence = trimmed[11..].Trim();
                }
                else if (trimmed.StartsWith("- ") && currentTitle is null)
                {
                    sources.Add(trimmed[2..].Trim());
                }
                else if (trimmed.StartsWith("TITLE:", StringComparison.OrdinalIgnoreCase))
                {
                    currentTitle = trimmed[6..].Trim();
                }
                else if (trimmed.StartsWith("URL:", StringComparison.OrdinalIgnoreCase))
                {
                    currentUrl = trimmed[4..].Trim();
                }
                else if (trimmed.StartsWith("SNIPPET:", StringComparison.OrdinalIgnoreCase))
                {
                    currentSnippet = trimmed[8..].Trim();
                }
                else if (trimmed.StartsWith("SOURCE:", StringComparison.OrdinalIgnoreCase))
                {
                    currentSource = trimmed[7..].Trim();
                }
                else if (trimmed == "---" && currentTitle is not null)
                {
                    results.Add(new SearchResult(
                        currentTitle,
                        currentUrl ?? "https://example.com",
                        currentSnippet ?? "",
                        currentSource ?? "Unknown",
                        null));
                    currentTitle = null;
                    currentUrl = null;
                    currentSnippet = null;
                    currentSource = null;
                }
            }

            // Fallback: if no structured results, return the whole text as answer
            if (results.Count == 0)
            {
                answer = output;
                results.Add(new SearchResult(query, "#", output[..Math.Min(200, output.Length)], "LLM Knowledge", null));
            }

            return (answer, results, confidence, sources);
        }
    }
}
