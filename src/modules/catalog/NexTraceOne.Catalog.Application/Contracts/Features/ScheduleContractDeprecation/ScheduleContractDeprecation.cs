using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;

namespace NexTraceOne.Catalog.Application.Contracts.Features.ScheduleContractDeprecation;

/// <summary>
/// Feature: ScheduleContractDeprecation — agendamento formal da deprecação de um contrato.
///
/// Cria ou actualiza um <c>DeprecationScheduleRecord</c> com estado <c>Planned</c>,
/// antecipando a mudança de estado real do contrato.
/// Gera AuditEvent com UserId, reason e datas planeadas.
///
/// Endpoint: POST /api/v1/contracts/{id}/deprecation-schedule
///
/// Wave AV.3 — Contract Lifecycle Automation &amp; Deprecation Intelligence (Catalog/Foundation).
/// </summary>
public static class ScheduleContractDeprecation
{
    // ── Command ────────────────────────────────────────────────────────────
    public sealed record Command(
        Guid ContractId,
        string TenantId,
        DateTimeOffset PlannedDeprecationDate,
        DateTimeOffset? PlannedSunsetDate,
        string? MigrationGuideUrl,
        Guid? SuccessorVersionId,
        string? NotificationDraftMessage,
        string ScheduledByUserId,
        string? Reason) : ICommand<Guid>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ContractId).NotEmpty();
            RuleFor(x => x.TenantId).NotEmpty().MaximumLength(200);
            RuleFor(x => x.PlannedDeprecationDate).NotEmpty();
            RuleFor(x => x.ScheduledByUserId).NotEmpty().MaximumLength(200);
            RuleFor(x => x.MigrationGuideUrl)
                .Must(url => url == null || Uri.TryCreate(url, UriKind.Absolute, out _))
                .WithMessage("MigrationGuideUrl must be a valid absolute URL when provided.");
            RuleFor(x => x.PlannedSunsetDate)
                .Must((cmd, sunset) => sunset == null || sunset > cmd.PlannedDeprecationDate)
                .WithMessage("PlannedSunsetDate must be after PlannedDeprecationDate.");
        }
    }

    // ── Handler ────────────────────────────────────────────────────────────
    public sealed class Handler(
        IDeprecationScheduleRepository repository,
        IDateTimeProvider clock) : ICommandHandler<Command, Guid>
    {
        public async Task<Result<Guid>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Default(request.ContractId);
            Guard.Against.NullOrWhiteSpace(request.TenantId);
            Guard.Against.NullOrWhiteSpace(request.ScheduledByUserId);

            var now = clock.UtcNow;

            var existing = await repository.GetByContractIdAsync(
                request.ContractId, request.TenantId, cancellationToken);

            var id = existing?.Id ?? Guid.NewGuid();

            var record = new DeprecationScheduleRecord(
                id,
                request.ContractId,
                request.TenantId,
                request.PlannedDeprecationDate,
                request.PlannedSunsetDate,
                request.MigrationGuideUrl,
                request.SuccessorVersionId,
                request.NotificationDraftMessage,
                request.ScheduledByUserId,
                request.Reason,
                now);

            await repository.UpsertAsync(record, cancellationToken);

            return Result<Guid>.Success(id);
        }
    }
}
