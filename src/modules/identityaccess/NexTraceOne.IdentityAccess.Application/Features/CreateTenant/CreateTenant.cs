using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Domain.Entities;
using NexTraceOne.IdentityAccess.Domain.Errors;

namespace NexTraceOne.IdentityAccess.Application.Features.CreateTenant;

/// <summary>
/// Feature: CreateTenant — cria um novo tenant na plataforma (uso exclusivo de Platform Admin).
///
/// Regras de negócio:
/// - Slug deve ser único em todo o sistema.
/// - TenantType permite criar diferentes tipos organizacionais (Organization, Holding, Subsidiary, Department).
/// - Subsidiárias e Departamentos requerem parentTenantId.
/// - Holdings e Organizations não devem ter parentTenantId.
/// - O tenant é criado como ativo por padrão.
/// </summary>
public static class CreateTenant
{
    /// <summary>Comando para criação de um novo tenant.</summary>
    public sealed record Command(
        string Name,
        string Slug,
        string TenantType,
        string? LegalName,
        string? TaxId,
        Guid? ParentTenantId) : ICommand<Response>;

    /// <summary>Resposta com o identificador do tenant criado.</summary>
    public sealed record Response(Guid TenantId, string Name, string Slug);

    /// <summary>Valida os parâmetros do comando.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(256);
            RuleFor(x => x.Slug).NotEmpty().MaximumLength(128)
                .Matches("^[a-z0-9][a-z0-9-]*[a-z0-9]$|^[a-z0-9]$")
                .WithMessage("Slug must be lowercase alphanumeric with optional hyphens.");
            RuleFor(x => x.TenantType).NotEmpty()
                .Must(t => System.Enum.TryParse<Domain.Entities.TenantType>(t, ignoreCase: true, out _))
                .WithMessage("TenantType must be a valid value: Organization, Holding, Subsidiary, Department, Partner.");
            RuleFor(x => x.LegalName).MaximumLength(512).When(x => x.LegalName != null);
            RuleFor(x => x.TaxId).MaximumLength(50).When(x => x.TaxId != null);
        }
    }

    /// <summary>
    /// Handler que persiste o novo tenant.
    /// Valida unicidade de slug e consistência de hierarquia.
    /// O commit é gerenciado pelo TransactionBehavior do pipeline.
    /// </summary>
    public sealed class Handler(
        ITenantRepository tenantRepository,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var normalizedSlug = request.Slug.ToLowerInvariant();

            if (await tenantRepository.SlugExistsAsync(normalizedSlug, cancellationToken))
                return IdentityErrors.TenantSlugAlreadyExists(normalizedSlug);

            if (!System.Enum.TryParse<Domain.Entities.TenantType>(request.TenantType, ignoreCase: true, out var tenantType))
                return Error.Validation("Identity.Tenant.InvalidType", "TenantType '{0}' is not valid.", request.TenantType);

            var now = dateTimeProvider.UtcNow;

            Tenant tenant;
            if (request.ParentTenantId.HasValue || tenantType is Domain.Entities.TenantType.Subsidiary or Domain.Entities.TenantType.Department)
            {
                TenantId? parentId = request.ParentTenantId.HasValue
                    ? TenantId.From(request.ParentTenantId.Value)
                    : null;

                // Validate parent exists when provided
                if (parentId is not null)
                {
                    var parent = await tenantRepository.GetByIdAsync(parentId, cancellationToken);
                    if (parent is null)
                        return IdentityErrors.TenantNotFound(request.ParentTenantId!.Value);
                }

                tenant = Tenant.CreateWithHierarchy(
                    request.Name,
                    normalizedSlug,
                    tenantType,
                    now,
                    parentId,
                    request.LegalName,
                    request.TaxId);
            }
            else
            {
                tenant = Tenant.Create(request.Name, normalizedSlug, now);

                if (!string.IsNullOrWhiteSpace(request.LegalName) || !string.IsNullOrWhiteSpace(request.TaxId))
                    tenant.UpdateOrganizationInfo(request.LegalName, request.TaxId, now);
            }

            tenantRepository.Add(tenant);

            return Result<Response>.Success(new Response(tenant.Id.Value, tenant.Name, tenant.Slug));
        }
    }
}
