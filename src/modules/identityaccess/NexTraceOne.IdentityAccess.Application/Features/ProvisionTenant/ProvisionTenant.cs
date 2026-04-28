using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Domain.Entities;

namespace NexTraceOne.IdentityAccess.Application.Features.ProvisionTenant;

/// <summary>
/// SaaS-05: Provisiona um novo tenant completo:
///   1. Cria o tenant (slug único)
///   2. Provisiona TenantLicense (plano selecionado)
///   3. Retorna IDs para linking
///
/// Saga simples: se a licença falhar, o tenant é ainda criado mas sem licença.
/// Frontend deve mostrar estado parcial e permitir retry da licença.
/// </summary>
public static class ProvisionTenant
{
    public sealed record Command(
        string Name,
        string Slug,
        string Plan,
        int IncludedHostUnits,
        string? LegalName,
        string? TaxId) : ICommand<Response>;

    public sealed record Response(
        Guid TenantId,
        string Name,
        string Slug,
        Guid? LicenseId,
        string Plan,
        bool LicenseProvisioned);

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(256);
            RuleFor(x => x.Slug).NotEmpty().MaximumLength(128)
                .Matches("^[a-z0-9][a-z0-9-]*[a-z0-9]$|^[a-z0-9]$")
                .WithMessage("Slug must be lowercase alphanumeric with optional hyphens.");
            RuleFor(x => x.Plan).NotEmpty()
                .Must(p => Enum.TryParse<TenantPlan>(p, ignoreCase: true, out _))
                .WithMessage("Plan must be Trial, Starter, Professional or Enterprise.");
            RuleFor(x => x.IncludedHostUnits).GreaterThanOrEqualTo(0);
        }
    }

    public sealed class Handler(
        ITenantRepository tenantRepository,
        ITenantLicenseRepository licenseRepository,
        IDateTimeProvider dateTimeProvider,
        IIdentityAccessUnitOfWork unitOfWork) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);
            var now = dateTimeProvider.UtcNow;

            if (!Enum.TryParse<TenantPlan>(request.Plan, ignoreCase: true, out var plan))
                return Result.Failure<Response>("Invalid plan.");

            var normalizedSlug = request.Slug.ToLowerInvariant();
            if (await tenantRepository.SlugExistsAsync(normalizedSlug, cancellationToken))
                return Result.Failure<Response>($"Slug '{normalizedSlug}' is already in use.");

            // Step 1: Create tenant
            var tenant = Tenant.Create(request.Name, normalizedSlug, now);
            if (!string.IsNullOrWhiteSpace(request.LegalName) || !string.IsNullOrWhiteSpace(request.TaxId))
            {
                tenant.UpdateOrganizationInfo(request.LegalName, request.TaxId, now);
            }
            tenantRepository.Add(tenant);

            // Step 2: Provision license
            DateTimeOffset? validUntil = plan == TenantPlan.Trial ? now.AddDays(14) : null;
            var license = TenantLicense.Provision(
                tenant.Id.Value,
                plan,
                request.IncludedHostUnits,
                now,
                validUntil,
                now);
            licenseRepository.Add(license);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success(new Response(
                tenant.Id.Value,
                tenant.Name,
                tenant.Slug,
                license.Id.Value,
                plan.ToString(),
                true));
        }
    }
}
