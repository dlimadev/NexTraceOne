using System.Text.Json;
using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Entities;

namespace NexTraceOne.Catalog.Application.Contracts.Features.IngestFeatureFlagState;

/// <summary>
/// Feature: IngestFeatureFlagState — ingere o estado actual de uma feature flag para um serviço.
///
/// A operação é idempotente por <c>ServiceId+FlagKey</c>: se a flag já existir é actualizada
/// (upsert); caso contrário é criada. Suporta ingestão via CI/CD ou CLI.
///
/// Wave AS.1 — Feature Flag &amp; Experimentation Governance (Catalog Contracts).
/// </summary>
public static class IngestFeatureFlagState
{
    // ── Command ────────────────────────────────────────────────────────────
    /// <summary>
    /// Comando de ingestão do estado de uma feature flag.
    /// </summary>
    public sealed record Command(
        string TenantId,
        string ServiceId,
        string FlagKey,
        string FlagType,
        bool IsEnabled,
        IReadOnlyList<string>? EnabledEnvironments,
        string? OwnerId,
        DateTimeOffset? LastToggledAt,
        DateTimeOffset? ScheduledRemovalDate,
        string? SuccessMetric,
        string? TargetValue) : ICommand<Guid>;

    /// <summary>Validador do comando <see cref="Command"/>.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.TenantId).NotEmpty().MaximumLength(200);
            RuleFor(x => x.ServiceId).NotEmpty().MaximumLength(200);
            RuleFor(x => x.FlagKey).NotEmpty().MaximumLength(400);
        }
    }

    // ── Handler ────────────────────────────────────────────────────────────
    /// <summary>Handler do comando <see cref="Command"/>.</summary>
    public sealed class Handler(
        IFeatureFlagRepository repository,
        IDateTimeProvider clock) : ICommandHandler<Command, Guid>
    {
        public async Task<Result<Guid>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.NullOrWhiteSpace(request.TenantId);
            Guard.Against.NullOrWhiteSpace(request.ServiceId);
            Guard.Against.NullOrWhiteSpace(request.FlagKey);

            var now = clock.UtcNow;

            var flagType = Enum.TryParse<FeatureFlagRecord.FlagType>(request.FlagType, ignoreCase: true, out var parsed)
                ? parsed
                : FeatureFlagRecord.FlagType.Release;

            var enabledEnvsJson = request.EnabledEnvironments is { Count: > 0 }
                ? JsonSerializer.Serialize(request.EnabledEnvironments)
                : null;

            var existing = await repository.ListByServiceAsync(request.ServiceId, request.TenantId, cancellationToken);
            var match = existing.FirstOrDefault(r => r.FlagKey == request.FlagKey);

            FeatureFlagRecord record;
            if (match is not null)
            {
                match.Upsert(request.IsEnabled, enabledEnvsJson, request.OwnerId,
                    request.LastToggledAt, request.ScheduledRemovalDate, now);
                record = match;
            }
            else
            {
                record = FeatureFlagRecord.Create(
                    request.TenantId, request.ServiceId, request.FlagKey,
                    flagType, request.IsEnabled, enabledEnvsJson, request.OwnerId,
                    request.LastToggledAt, request.ScheduledRemovalDate, now);
            }

            await repository.UpsertAsync(record, cancellationToken);

            return Result<Guid>.Success(record.Id);
        }
    }
}
