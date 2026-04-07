using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Domain.Entities;

namespace NexTraceOne.Configuration.Application.Features.WatchEntity;

/// <summary>
/// Feature: WatchEntity — adiciona uma entidade à watch list do utilizador.
/// Utilizadores podem seguir serviços, contratos, mudanças, incidentes e runbooks.
/// Se o watch já existir, actualiza o nível de notificação (upsert).
/// </summary>
public static class WatchEntity
{
    private static readonly string[] ValidEntityTypes = ["service", "contract", "change", "incident", "runbook"];
    private static readonly string[] ValidNotifyLevels = ["all", "critical", "none"];

    public sealed record Command(string EntityType, string EntityId, string NotifyLevel) : ICommand<Response>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.EntityType).NotEmpty()
                .Must(t => ValidEntityTypes.Contains(t, StringComparer.OrdinalIgnoreCase))
                .WithMessage($"EntityType must be one of: {string.Join(", ", ValidEntityTypes)}");
            RuleFor(x => x.EntityId).NotEmpty().MaximumLength(256);
            RuleFor(x => x.NotifyLevel).NotEmpty()
                .Must(l => ValidNotifyLevels.Contains(l, StringComparer.OrdinalIgnoreCase))
                .WithMessage($"NotifyLevel must be one of: {string.Join(", ", ValidNotifyLevels)}");
        }
    }

    public sealed class Handler(
        IUserWatchRepository repository,
        ICurrentUser currentUser,
        ICurrentTenant currentTenant,
        IDateTimeProvider clock) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            if (!currentUser.IsAuthenticated)
                return Error.Unauthorized("User.NotAuthenticated", "User must be authenticated.");

            var tenantId = currentTenant.Id.ToString();

            var existing = await repository.GetByEntityAsync(
                currentUser.Id, tenantId, request.EntityType, request.EntityId, cancellationToken);

            if (existing is not null)
            {
                existing.UpdateNotifyLevel(request.NotifyLevel, clock.UtcNow);
                await repository.UpdateAsync(existing, cancellationToken);
                return new Response(existing.Id.Value, existing.EntityType, existing.EntityId, existing.NotifyLevel, existing.CreatedAt);
            }

            var watch = UserWatch.Create(currentUser.Id, tenantId, request.EntityType, request.EntityId, request.NotifyLevel, clock.UtcNow);
            await repository.AddAsync(watch, cancellationToken);
            return new Response(watch.Id.Value, watch.EntityType, watch.EntityId, watch.NotifyLevel, watch.CreatedAt);
        }
    }

    public sealed record Response(Guid WatchId, string EntityType, string EntityId, string NotifyLevel, DateTimeOffset CreatedAt);
}
