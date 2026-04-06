using System.Text;
using System.Text.Json;
using Ardalis.GuardClauses;
using FluentValidation;
using Microsoft.Extensions.Logging;
using NexTraceOne.AIKnowledge.Domain.ExternalAI.Ports;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Orchestration.Features.GenerateArchitectureDecisionRecord;

/// <summary>
/// Feature: GenerateArchitectureDecisionRecord — gera um ADR (Architecture Decision Record)
/// assistido por IA para documentar decisões de arquitectura tomadas durante o scaffold de um serviço.
///
/// Segue o formato MADR (Markdown Any Decision Records):
/// - Título
/// - Estado (Proposed, Accepted, Deprecated, Superseded)
/// - Contexto e declaração do problema
/// - Opções consideradas com prós/contras
/// - Decisão tomada e suas consequências
///
/// O ADR pode ser incluído no ZIP do scaffold ou registado no Knowledge Hub.
/// </summary>
public static class GenerateArchitectureDecisionRecord
{
    /// <summary>Comando para gerar um ADR.</summary>
    public sealed record Command(
        string ServiceName,
        string DecisionContext,
        string? ArchitectureStyle = null,
        string? TechStack = null,
        string? SelectedTemplate = null,
        string? PreferredProvider = null) : ICommand<Response>;

    /// <summary>Validação do comando.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ServiceName).NotEmpty().MaximumLength(100);
            RuleFor(x => x.DecisionContext).NotEmpty().MaximumLength(2000);
        }
    }

    /// <summary>Handler que gera o ADR usando a IA.</summary>
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
                    capability: "adr-generation",
                    cancellationToken: cancellationToken);

                isFallback = aiResponse.StartsWith("[FALLBACK_PROVIDER_UNAVAILABLE]", StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex,
                    "AI provider unavailable for GenerateArchitectureDecisionRecord. Service={ServiceName}",
                    request.ServiceName);
                return BuildFallbackResponse(request);
            }

            return isFallback
                ? BuildFallbackResponse(request)
                : BuildAdrResponse(aiResponse, request);
        }

        private static string BuildContext(Command request)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Service: {request.ServiceName}");
            if (request.ArchitectureStyle is not null) sb.AppendLine($"Architecture style: {request.ArchitectureStyle}");
            if (request.TechStack is not null) sb.AppendLine($"Tech stack: {request.TechStack}");
            if (request.SelectedTemplate is not null) sb.AppendLine($"Selected template: {request.SelectedTemplate}");
            sb.AppendLine("Platform: NexTraceOne — modular monolith, DDD, Clean Architecture, CQRS, .NET 10");
            return sb.ToString();
        }

        private static string BuildPrompt(Command request)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Generate an Architecture Decision Record (ADR) in MADR format for service '{request.ServiceName}'.");
            sb.AppendLine();
            sb.AppendLine($"Decision context: {request.DecisionContext}");
            sb.AppendLine();
            sb.AppendLine("Format the ADR as a complete markdown document following this structure:");
            sb.AppendLine("# ADR-{number}: {title}");
            sb.AppendLine("Date: {date}");
            sb.AppendLine("Status: Accepted");
            sb.AppendLine("## Context");
            sb.AppendLine("## Decision");
            sb.AppendLine("## Considered Options");
            sb.AppendLine("## Pros and Cons of the Options");
            sb.AppendLine("## Decision Outcome");
            sb.AppendLine("## Consequences");
            sb.AppendLine();
            sb.AppendLine("Output ONLY the complete markdown ADR document, no JSON, no explanation.");
            return sb.ToString();
        }

        private static Result<Response> BuildAdrResponse(string aiResponse, Command request)
        {
            var markdown = aiResponse.Trim();
            var title = ExtractTitle(markdown) ?? $"ADR for {request.ServiceName}";
            var filename = $"docs/adr/{SanitizeFilename(title)}.md";

            return Result<Response>.Success(new Response(
                ServiceName: request.ServiceName,
                AdrTitle: title,
                MarkdownContent: markdown,
                SuggestedFilename: filename,
                IsFallback: false));
        }

        private static Result<Response> BuildFallbackResponse(Command request)
        {
            var fallbackMarkdown = $$"""
                # ADR-001: Architecture Decision for {{request.ServiceName}}

                Date: {{DateTimeOffset.UtcNow:yyyy-MM-dd}}
                Status: Draft

                ## Context

                {{request.DecisionContext}}

                ## Decision

                _AI provider unavailable. Please complete this section manually._

                ## Considered Options

                - Option 1: _Describe first option_
                - Option 2: _Describe second option_

                ## Decision Outcome

                _Pending review._

                ## Consequences

                _To be determined after decision is finalized._
                """;

            return Result<Response>.Success(new Response(
                ServiceName: request.ServiceName,
                AdrTitle: $"Architecture Decision for {request.ServiceName}",
                MarkdownContent: fallbackMarkdown,
                SuggestedFilename: $"docs/adr/adr-{request.ServiceName.ToLower().Replace(" ", "-")}.md",
                IsFallback: true));
        }

        private static string? ExtractTitle(string markdown)
        {
            var lines = markdown.Split('\n');
            var titleLine = lines.FirstOrDefault(l => l.TrimStart().StartsWith("# ADR", StringComparison.OrdinalIgnoreCase));
            return titleLine?.TrimStart('#').Trim();
        }

        private static string SanitizeFilename(string title)
        {
            var sanitized = title.ToLower()
                .Replace(" ", "-")
                .Replace(":", "")
                .Replace("/", "-")
                .TrimStart('-');
            return sanitized.Length > 60 ? sanitized[..60] : sanitized;
        }
    }

    /// <summary>Resposta com o ADR gerado.</summary>
    public sealed record Response(
        string ServiceName,
        string AdrTitle,
        string MarkdownContent,
        string SuggestedFilename,
        bool IsFallback);
}
