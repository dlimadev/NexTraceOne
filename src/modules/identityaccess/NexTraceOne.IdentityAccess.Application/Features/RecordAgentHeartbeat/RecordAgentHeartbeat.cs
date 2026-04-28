using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Domain.Entities;

namespace NexTraceOne.IdentityAccess.Application.Features.RecordAgentHeartbeat;

/// <summary>
/// SaaS-03: Regista ou actualiza o heartbeat de um NexTrace Agent.
/// Upsert por (TenantId, HostUnitId) — idempotente.
/// </summary>
public static class RecordAgentHeartbeat
{
    public sealed record Command(
        Guid HostUnitId,
        string HostName,
        string AgentVersion,
        string DeploymentMode,
        int CpuCores,
        decimal RamGb) : ICommand<Response>;

    public sealed record Response(
        Guid RegistrationId,
        decimal HostUnits,
        string Status);

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.HostUnitId).NotEmpty();
            RuleFor(x => x.HostName).NotEmpty().MaximumLength(500);
            RuleFor(x => x.AgentVersion).NotEmpty().MaximumLength(50);
            RuleFor(x => x.CpuCores).GreaterThanOrEqualTo(1);
            RuleFor(x => x.RamGb).GreaterThanOrEqualTo(0.5m);
        }
    }

    public sealed class Handler(
        IAgentRegistrationRepository repository,
        ITenantLicenseRepository licenseRepository,
        ICurrentTenant currentTenant,
        IDateTimeProvider dateTimeProvider,
        IIdentityAccessUnitOfWork unitOfWork) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);
            var tenantId = currentTenant.TenantId;
            var now = dateTimeProvider.UtcNow;

            var existing = await repository.GetByHostUnitIdAsync(tenantId, request.HostUnitId, cancellationToken);

            AgentRegistration registration;
            if (existing is null)
            {
                registration = AgentRegistration.Register(
                    tenantId,
                    request.HostUnitId,
                    request.HostName,
                    request.AgentVersion,
                    request.DeploymentMode,
                    request.CpuCores,
                    request.RamGb,
                    now);
                repository.Add(registration);
            }
            else
            {
                existing.RecordHeartbeat(request.AgentVersion, request.CpuCores, request.RamGb, now);
                registration = existing;
                repository.Update(registration);
            }

            // Recalculate current host units for licensing
            var totalHostUnits = await repository.SumActiveHostUnitsAsync(tenantId, cancellationToken);
            var license = await licenseRepository.GetByTenantIdAsync(tenantId, cancellationToken);
            if (license is not null)
            {
                license.UpdateHostUnits(totalHostUnits, now);
                licenseRepository.Update(license);
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success(new Response(
                registration.Id.Value,
                registration.HostUnits,
                registration.Status.ToString()));
        }
    }
}
