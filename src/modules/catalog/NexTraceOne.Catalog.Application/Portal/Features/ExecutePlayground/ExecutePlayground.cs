using System.Text.Json;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Portal.Abstractions;
using NexTraceOne.Catalog.Domain.Portal.Entities;

namespace NexTraceOne.Catalog.Application.Portal.Features.ExecutePlayground;

/// <summary>
/// Feature: ExecutePlayground — executa um pedido de teste contra uma API do catálogo no
/// ambiente <b>sandbox</b> do Developer Portal e regista a sessão para histórico/auditoria.
///
/// Por design da plataforma (governança + air-gap), o playground NÃO faz chamadas reais a
/// ambientes produtivos: devolve uma resposta simulada determinística (200 + eco do pedido).
/// A sessão é persistida via <see cref="IPlaygroundSessionRepository"/>.
/// </summary>
public static class ExecutePlayground
{
    // ── Command ────────────────────────────────────────────────────────────
    /// <summary>Corpo HTTP do POST (o UserId vem do contexto autenticado).</summary>
    public sealed record ExecuteBody(
        Guid ApiAssetId,
        string ApiName,
        string HttpMethod,
        string RequestPath,
        string? RequestBody,
        string? RequestHeaders,
        string? Environment);

    /// <summary>Comando de execução de um pedido de playground.</summary>
    public sealed record Command(
        Guid ApiAssetId,
        string ApiName,
        Guid UserId,
        string HttpMethod,
        string RequestPath,
        string? RequestBody,
        string? RequestHeaders,
        string? Environment) : ICommand<PlaygroundResult>;

    /// <summary>Validador do comando <see cref="Command"/>.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ApiAssetId).NotEmpty();
            RuleFor(x => x.ApiName).NotEmpty().MaximumLength(400);
            RuleFor(x => x.UserId).NotEmpty();
            RuleFor(x => x.HttpMethod).NotEmpty().MaximumLength(10);
            RuleFor(x => x.RequestPath).NotEmpty().MaximumLength(2000);
        }
    }

    /// <summary>Resultado (simulado) da execução de um pedido de playground.</summary>
    public sealed record PlaygroundResult(
        int StatusCode,
        int ResponseStatusCode,
        string ResponseBody,
        long DurationMs,
        DateTimeOffset ExecutedAt);

    // ── Handler ────────────────────────────────────────────────────────────
    /// <summary>Handler do comando <see cref="Command"/>.</summary>
    public sealed class Handler(
        IPlaygroundSessionRepository repository,
        IDateTimeProvider clock) : ICommandHandler<Command, PlaygroundResult>
    {
        public async Task<Result<PlaygroundResult>> Handle(Command request, CancellationToken cancellationToken)
        {
            var now = clock.UtcNow;

            // Resposta simulada determinística (sandbox — sem chamada real).
            const int statusCode = 200;
            var responseBody = JsonSerializer.Serialize(new
            {
                sandbox = true,
                message = "Simulated sandbox response — no real call was made.",
                request = new
                {
                    method = request.HttpMethod.ToUpperInvariant(),
                    path = request.RequestPath,
                    api = request.ApiName,
                },
                executedAt = now,
            });

            // Latência simulada determinística a partir do pedido (20–100 ms).
            var durationMs = 20 + (request.RequestPath.Length % 80);

            var session = PlaygroundSession.Create(
                request.ApiAssetId,
                request.ApiName,
                request.UserId,
                request.HttpMethod,
                request.RequestPath,
                request.RequestBody,
                request.RequestHeaders,
                statusCode,
                responseBody,
                durationMs,
                now);

            repository.Add(session);
            await Task.CompletedTask;

            return Result<PlaygroundResult>.Success(
                new PlaygroundResult(statusCode, statusCode, responseBody, durationMs, now));
        }
    }
}
