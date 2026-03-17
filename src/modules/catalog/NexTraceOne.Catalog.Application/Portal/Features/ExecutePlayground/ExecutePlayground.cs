using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Portal.Abstractions;
using NexTraceOne.Catalog.Domain.Portal.Entities;

namespace NexTraceOne.Catalog.Application.Portal.Features.ExecutePlayground;

/// <summary>
/// Feature: ExecutePlayground — executa request sandbox contra uma API no playground.
/// Regista sessão para auditoria. Nunca executa contra produção real.
/// </summary>
public static class ExecutePlayground
{
    /// <summary>Comando para executar request no playground sandbox.</summary>
    public sealed record Command(
        Guid ApiAssetId,
        string ApiName,
        Guid UserId,
        string HttpMethod,
        string RequestPath,
        string? RequestBody,
        string? RequestHeaders) : ICommand<Response>;

    /// <summary>Valida os parâmetros de execução do playground.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ApiAssetId).NotEmpty();
            RuleFor(x => x.ApiName).NotEmpty().MaximumLength(200);
            RuleFor(x => x.UserId).NotEmpty();
            RuleFor(x => x.HttpMethod).NotEmpty().MaximumLength(10);
            RuleFor(x => x.RequestPath).NotEmpty().MaximumLength(2000);
            RuleFor(x => x.RequestBody).MaximumLength(50000);
        }
    }

    /// <summary>
    /// Handler que simula execução de request no sandbox.
    /// MVP1: retorna resposta mock. Em produção, delega para sandbox real.
    /// Regista sessão completa para auditoria.
    /// </summary>
    public sealed class Handler(
        IPlaygroundSessionRepository repository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider clock) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            // MVP1: execução mock — em produção, delegaria para sandbox isolado
            var mockResponseBody = $"{{\"message\":\"Sandbox mock response for {request.HttpMethod} {request.RequestPath}\"}}";
            const int mockStatusCode = 200;
            const long mockDuration = 42L;

            var session = PlaygroundSession.Create(
                request.ApiAssetId,
                request.ApiName,
                request.UserId,
                request.HttpMethod,
                request.RequestPath,
                request.RequestBody,
                request.RequestHeaders,
                mockStatusCode,
                mockResponseBody,
                mockDuration,
                clock.UtcNow);

            repository.Add(session);
            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(
                session.Id.Value,
                session.HttpMethod,
                session.RequestPath,
                mockStatusCode,
                mockResponseBody,
                mockDuration,
                session.ExecutedAt);
        }
    }

    /// <summary>Resposta da execução sandbox com resultado e auditoria.</summary>
    public sealed record Response(
        Guid SessionId,
        string HttpMethod,
        string RequestPath,
        int ResponseStatusCode,
        string? ResponseBody,
        long DurationMs,
        DateTimeOffset ExecutedAt);
}
