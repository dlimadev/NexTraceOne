using Ardalis.GuardClauses;
using FluentValidation;
using Microsoft.Extensions.Logging;
using NexTraceOne.AIKnowledge.Domain.ExternalAI.Ports;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.ExternalAI.Features.QueryExternalAISimple;

/// <summary>
/// Feature: QueryExternalAISimple — consulta direta e simples a um provider de IA externa.
/// Roteia a query via IExternalAIRoutingPort sem grounding estruturado adicional.
/// </summary>
public static class QueryExternalAISimple
{
    // ── COMMAND ───────────────────────────────────────────────────────────

    public sealed record Command(
        string Query,
        string? ContextScope,
        string? SystemContext,
        string? PreferredProvider) : ICommand<Response>;

    // ── VALIDATOR ─────────────────────────────────────────────────────────

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Query)
                .NotEmpty()
                .MaximumLength(10_000);
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
            var context = request.SystemContext ?? request.ContextScope ?? "general";

            string content;
            try
            {
                content = await externalAiRoutingPort.RouteQueryAsync(
                    context,
                    request.Query,
                    request.PreferredProvider,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "AI provider unavailable for simple query. CorrelationId={CorrelationId}", correlationId);
                return Error.Business("AIKnowledge.Provider.Unavailable", "AI provider unavailable: {0}", ex.Message);
            }

            var isFallback = content.StartsWith("[FALLBACK_PROVIDER_UNAVAILABLE]", StringComparison.OrdinalIgnoreCase);

            return Result<Response>.Success(new Response(content, request.PreferredProvider ?? "default", isFallback, correlationId));
        }
    }

    // ── RESPONSE ──────────────────────────────────────────────────────────

    public sealed record Response(
        string Content,
        string ProviderId,
        bool IsFallback,
        string CorrelationId);
}
