using Ardalis.GuardClauses;
using FluentValidation;
using MediatR;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Entities;

namespace NexTraceOne.Catalog.Application.Contracts.Features.RegisterDataContract;

/// <summary>
/// Feature: RegisterDataContract — regista um novo data contract para um serviço.
/// Wave AQ.1 — DataContractRecord.
/// </summary>
public static class RegisterDataContract
{
    public sealed record Command(
        string TenantId,
        string ServiceId,
        string DatasetName,
        string ContractVersion,
        int? FreshnessRequirementHours = null,
        string? FieldDefinitionsJson = null,
        string? OwnerTeamId = null) : ICommand<Unit>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.TenantId).NotEmpty();
            RuleFor(x => x.ServiceId).NotEmpty();
            RuleFor(x => x.DatasetName).NotEmpty().MaximumLength(200);
            RuleFor(x => x.ContractVersion).NotEmpty().MaximumLength(50);
            RuleFor(x => x.FreshnessRequirementHours).GreaterThan(0).When(x => x.FreshnessRequirementHours.HasValue);
        }
    }

    public sealed class Handler(
        IDataContractRepository repository,
        IDateTimeProvider clock) : ICommandHandler<Command, Unit>
    {
        public async Task<Result<Unit>> Handle(Command cmd, CancellationToken ct)
        {
            Guard.Against.NullOrWhiteSpace(cmd.TenantId);
            Guard.Against.NullOrWhiteSpace(cmd.ServiceId);
            Guard.Against.NullOrWhiteSpace(cmd.DatasetName);

            var record = DataContractRecord.Create(
                cmd.TenantId, cmd.ServiceId, cmd.DatasetName, cmd.ContractVersion,
                cmd.FreshnessRequirementHours, cmd.FieldDefinitionsJson, cmd.OwnerTeamId,
                clock.UtcNow);

            await repository.AddAsync(record, ct);
            return Result<Unit>.Success(Unit.Value);
        }
    }
}
