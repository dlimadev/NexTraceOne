using Ardalis.GuardClauses;
using FluentValidation;
using Microsoft.Extensions.Logging;
using NexTraceOne.AIKnowledge.Domain.ExternalAI.Ports;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Orchestration.Features.AskCatalogQuestion;

/// <summary>
/// Feature: AskCatalogQuestion — responde perguntas sobre um serviço ou contrato usando IA com grounding.
/// Constrói contexto a partir dos dados da entidade recebidos, chama o provider e retorna a resposta.
/// </summary>
public static class AskCatalogQuestion
{
    // ── COMMAND ───────────────────────────────────────────────────────────

    public sealed record Command(
        string Question,
        string EntityType,
        string? EntityName,
        string? EntityDescription,
        Dictionary<string, string>? Properties,
        string? PreferredProvider) : ICommand<Response>;

    // ── VALIDATOR ─────────────────────────────────────────────────────────

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Question).NotEmpty();
            RuleFor(x => x.EntityType).NotEmpty();
        }
    }

    // ── HANDLER ───────────────────────────────────────────────────────────

    public sealed class Handler(
        IExternalAIRoutingPort externalAiRoutingPort,
        IDateTimeProvider dateTimeProvider,
        ILogger<Handler> logger) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);
            Guard.Against.NullOrWhiteSpace(request.Question);
            Guard.Against.NullOrWhiteSpace(request.EntityType);

            var correlationId = Guid.NewGuid().ToString();
            var groundingPrompt = BuildGroundingPrompt(request);
            var groundingSources = BuildGroundingSources(request);

            string content;
            try
            {
                content = await externalAiRoutingPort.RouteQueryAsync(
                    groundingPrompt,
                    request.Question,
                    request.PreferredProvider,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "AI provider unavailable for catalog question. CorrelationId={CorrelationId}", correlationId);
                return Error.Business("AIKnowledge.Provider.Unavailable", "AI provider unavailable: {0}", ex.Message);
            }

            var isFallback = content.StartsWith("[FALLBACK_PROVIDER_UNAVAILABLE]", StringComparison.OrdinalIgnoreCase);

            return Result<Response>.Success(new Response(content, request.EntityType, request.EntityName, isFallback, correlationId, groundingSources));
        }

        private static string BuildGroundingPrompt(Command request)
        {
            var lines = new List<string>
            {
                $"You are an expert assistant for the NexTraceOne platform.",
                $"The user is asking about a {request.EntityType}.",
            };

            if (!string.IsNullOrWhiteSpace(request.EntityName))
                lines.Add($"{request.EntityType} name: {request.EntityName}");

            if (!string.IsNullOrWhiteSpace(request.EntityDescription))
                lines.Add($"Description: {request.EntityDescription}");

            if (request.Properties is { Count: > 0 })
            {
                lines.Add("Properties:");
                foreach (var (key, value) in request.Properties)
                    lines.Add($"  {key}: {value}");
            }

            return string.Join("\n", lines);
        }

        private static string[] BuildGroundingSources(Command request)
        {
            var sources = new List<string> { request.EntityType };
            if (!string.IsNullOrWhiteSpace(request.EntityName))
                sources.Add(request.EntityName);
            return sources.ToArray();
        }
    }

    // ── RESPONSE ──────────────────────────────────────────────────────────

    public sealed record Response(
        string Answer,
        string EntityType,
        string? EntityName,
        bool IsFallback,
        string CorrelationId,
        string[] GroundingSources);
}
