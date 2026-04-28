using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Domain.Entities;

namespace NexTraceOne.IdentityAccess.Application.Features.ProvisionTenantLicense;

/// <summary>
/// SaaS-04: Provisiona (cria) uma licença para um tenant.
/// Idempotente: se a licença já existe, atualiza o plano.
/// </summary>
public static class ProvisionTenantLicense
{
    public sealed record Command(
        Guid TenantId,
        string Plan,
        int IncludedHostUnits,
        DateTimeOffset ValidFrom,
        DateTimeOffset? ValidUntil) : ICommand<Response>;

    public sealed record Response(
        Guid LicenseId,
        string Plan,
        TenantLicenseStatus Status,
        int IncludedHostUnits);

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.TenantId).NotEmpty();
            RuleFor(x => x.Plan).NotEmpty()
                .Must(p => Enum.TryParse<TenantPlan>(p, ignoreCase: true, out _))
                .WithMessage("Plan must be Trial, Starter, Professional or Enterprise.");
            RuleFor(x => x.IncludedHostUnits).GreaterThanOrEqualTo(0);
            RuleFor(x => x.ValidFrom).NotEmpty();
        }
    }

    public sealed class Handler(
        ITenantLicenseRepository repository,
        IDateTimeProvider dateTimeProvider,
        IIdentityAccessUnitOfWork unitOfWork) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);
            var now = dateTimeProvider.UtcNow;

            if (!Enum.TryParse<TenantPlan>(request.Plan, ignoreCase: true, out var plan))
                return Error.Validation("license.invalidPlan", "Invalid plan value.");

            var existing = await repository.GetByTenantIdAsync(request.TenantId, cancellationToken);

            TenantLicense license;
            if (existing is null)
            {
                license = TenantLicense.Provision(
                    request.TenantId,
                    plan,
                    request.IncludedHostUnits,
                    request.ValidFrom,
                    request.ValidUntil,
                    now);
                repository.Add(license);
            }
            else
            {
                existing.Upgrade(plan, request.IncludedHostUnits, request.ValidUntil, now);
                license = existing;
                repository.Update(license);
            }

            await unitOfWork.CommitAsync(cancellationToken);

            return Result<Response>.Success(new Response(
                license.Id.Value,
                license.Plan.ToString(),
                license.Status,
                license.IncludedHostUnits));
        }
    }
}
