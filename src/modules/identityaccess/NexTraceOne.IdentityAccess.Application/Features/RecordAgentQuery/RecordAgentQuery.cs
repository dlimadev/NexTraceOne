using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Domain.Entities;

namespace NexTraceOne.IdentityAccess.Application.Features.RecordAgentQuery;

/// <summary>
/// Feature: RecordAgentQuery — regista uma query executada por um agente autónomo.
/// Chamado automaticamente pelo middleware da Agent API após cada request.
/// Wave D.4 — Agent-to-Agent Protocol.
/// </summary>
public static class RecordAgentQuery
{
    public sealed record Command(
        Guid TokenId,
        string QueryType,
        int ResponseCode,
        long DurationMs,
        string? QueryParametersJson = null,
        string? ErrorMessage = null) : ICommand<Response>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.TokenId).NotEmpty();
            RuleFor(x => x.QueryType).NotEmpty().MaximumLength(200);
            RuleFor(x => x.DurationMs).GreaterThanOrEqualTo(0);
        }
    }

    public sealed class Handler(
        IAgentQueryRepository repository,
        IPlatformApiTokenRepository tokenRepository,
        IIdentityAccessUnitOfWork unitOfWork,
        ICurrentTenant currentTenant,
        IDateTimeProvider clock) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var now = clock.UtcNow;
            var record = AgentQueryRecord.Create(
                tenantId: currentTenant.Id,
                tokenId: request.TokenId,
                queryType: request.QueryType,
                responseCode: request.ResponseCode,
                durationMs: request.DurationMs,
                executedAt: now,
                queryParametersJson: request.QueryParametersJson,
                errorMessage: request.ErrorMessage);

            await repository.AddAsync(record, cancellationToken);

            // ── Actualizar LastUsedAt no token ──────────────────────────────
            var token = await tokenRepository.GetByIdAsync(
                PlatformApiTokenId.From(request.TokenId), cancellationToken);
            if (token is not null)
            {
                token.RecordUsage(now);
                await tokenRepository.UpdateAsync(token, cancellationToken);
            }

            await unitOfWork.CommitAsync(cancellationToken);

            return Result<Response>.Success(new Response(record.Id.Value, now));
        }
    }

    public sealed record Response(Guid RecordId, DateTimeOffset ExecutedAt);
}
