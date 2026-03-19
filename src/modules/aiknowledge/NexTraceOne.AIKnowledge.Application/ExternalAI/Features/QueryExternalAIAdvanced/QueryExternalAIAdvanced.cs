using Ardalis.GuardClauses;
using FluentValidation;
using Microsoft.Extensions.Logging;
using NexTraceOne.AIKnowledge.Domain.ExternalAI.Ports;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.ExternalAI.Features.QueryExternalAIAdvanced;

/// <summary>
/// Feature: QueryExternalAIAdvanced — consulta avançada com contexto de grounding estruturado.
/// Constrói um contexto semântico a partir de EntityType, EntityName e dados adicionais,
/// depois roteia para o provider via IExternalAIRoutingPort.
/// </summary>
public static class QueryExternalAIAdvanced
{
    // ── COMMAND ───────────────────────────────────────────────────────────

    public sealed record Command(
        string Query,
        string? ContextScope,
        string? EntityType,
        Guid? EntityId,
        string? EntityName,
        Dictionary<string, string>? AdditionalContext,
        string? PreferredProvider) : ICommand<Response>;

    // ── VALIDATOR ─────────────────────────────────────────────────────────

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Query).NotEmpty();
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
            Guard.Against.NullOrWhiteSpace(request.Query);

            var correlationId = Guid.NewGuid().ToString();
            var groundingContext = BuildGroundingContext(request);

            string content;
            try
            {
                content = await externalAiRoutingPort.RouteQueryAsync(
                    groundingContext,
                    request.Query,
                    request.PreferredProvider,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "AI provider unavailable for advanced query. CorrelationId={CorrelationId}", correlationId);
                return Error.Business("AIKnowledge.Provider.Unavailable", "AI provider unavailable: {0}", ex.Message);
            }

            var isFallback = content.StartsWith("[FALLBACK_PROVIDER_UNAVAILABLE]", StringComparison.OrdinalIgnoreCase);

            return Result<Response>.Success(new Response(content, groundingContext, isFallback, correlationId));
        }

        private static string BuildGroundingContext(Command request)
        {
            var parts = new List<string>();

            if (!string.IsNullOrWhiteSpace(request.ContextScope))
                parts.Add($"Context: {request.ContextScope}");

            if (!string.IsNullOrWhiteSpace(request.EntityType))
                parts.Add($"Entity type: {request.EntityType}");

            if (request.EntityId.HasValue)
                parts.Add($"Entity ID: {request.EntityId}");

            if (!string.IsNullOrWhiteSpace(request.EntityName))
                parts.Add($"Entity name: {request.EntityName}");

            if (request.AdditionalContext is { Count: > 0 })
            {
                foreach (var (key, value) in request.AdditionalContext)
                    parts.Add($"{key}: {value}");
            }

            return parts.Count > 0
                ? string.Join("\n", parts)
                : "general";
        }
    }

    // ── RESPONSE ──────────────────────────────────────────────────────────

    public sealed record Response(
        string Content,
        string GroundingContext,
        bool IsFallback,
        string CorrelationId);
}
