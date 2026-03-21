using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Domain.Entities;
using NexTraceOne.IdentityAccess.Domain.Enums;
using NexTraceOne.IdentityAccess.Domain.Errors;

using DomainEnvironment = NexTraceOne.IdentityAccess.Domain.Entities.Environment;

namespace NexTraceOne.IdentityAccess.Application.Features.CreateEnvironment;

/// <summary>
/// Feature: CreateEnvironment — cria um novo ambiente para o tenant atual.
///
/// Ambientes são a dimensão de contexto operacional do NexTraceOne. Cada tenant pode ter
/// os seus próprios ambientes, com perfil operacional, criticidade e comportamento configuráveis.
/// Não se assume que todos os tenants têm os mesmos ambientes (ex.: DEV/QA/UAT/PROD).
///
/// Regras de negócio:
/// - Requer tenant context ativo.
/// - O slug deve ser único dentro do tenant.
/// - O perfil determina o comportamento base do ambiente (sem hardcode por nome).
/// - IsPrimaryProduction=true só pode ser definido se não houver outro ambiente produtivo principal ativo.
/// - Slug é normalizado para lowercase.
/// </summary>
public static class CreateEnvironment
{
    /// <summary>Comando para criação de um novo ambiente.</summary>
    public sealed record Command(
        string Name,
        string Slug,
        int SortOrder,
        string Profile,
        string Criticality,
        string? Code,
        string? Description,
        string? Region,
        bool? IsProductionLike,
        bool IsPrimaryProduction) : ICommand<Response>;

    /// <summary>Resposta com o identificador do ambiente criado.</summary>
    public sealed record Response(Guid EnvironmentId, string Name, string Slug);

    /// <summary>Valida os parâmetros do comando.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
            RuleFor(x => x.Slug).NotEmpty().MaximumLength(50)
                .Matches("^[a-z0-9][a-z0-9-]*[a-z0-9]$|^[a-z0-9]$")
                .WithMessage("Slug must be lowercase alphanumeric with optional hyphens.");
            RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);
            RuleFor(x => x.Profile).NotEmpty()
                .Must(p => System.Enum.TryParse<EnvironmentProfile>(p, ignoreCase: true, out _))
                .WithMessage("Profile must be a valid EnvironmentProfile value.");
            RuleFor(x => x.Criticality).NotEmpty()
                .Must(c => System.Enum.TryParse<EnvironmentCriticality>(c, ignoreCase: true, out _))
                .WithMessage("Criticality must be a valid EnvironmentCriticality value.");
            RuleFor(x => x.Code).MaximumLength(50).When(x => x.Code != null);
            RuleFor(x => x.Region).MaximumLength(100).When(x => x.Region != null);
        }
    }

    /// <summary>
    /// Handler que persiste o novo ambiente no tenant.
    /// Valida unicidade de slug e unicidade do ambiente produtivo principal.
    /// O commit é gerenciado pelo TransactionBehavior do pipeline.
    /// </summary>
    public sealed class Handler(
        ICurrentTenant currentTenant,
        IEnvironmentRepository environmentRepository,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            if (currentTenant.Id == Guid.Empty)
                return IdentityErrors.TenantContextRequired();

            var tenantId = TenantId.From(currentTenant.Id);

            // Validar unicidade do slug
            var normalizedSlug = request.Slug.ToLowerInvariant();
            if (await environmentRepository.SlugExistsAsync(tenantId, normalizedSlug, cancellationToken))
                return IdentityErrors.EnvironmentSlugAlreadyExists(normalizedSlug);

            // Validar unicidade de ambiente produtivo principal
            if (request.IsPrimaryProduction)
            {
                var existingPrimary = await environmentRepository.GetPrimaryProductionAsync(tenantId, cancellationToken);
                if (existingPrimary is not null)
                    return IdentityErrors.PrimaryProductionAlreadyExists(tenantId.Value);
            }

            var profile = System.Enum.Parse<EnvironmentProfile>(request.Profile, ignoreCase: true);
            var criticality = System.Enum.Parse<EnvironmentCriticality>(request.Criticality, ignoreCase: true);
            var now = dateTimeProvider.UtcNow;

            var environment = DomainEnvironment.Create(
                tenantId,
                request.Name,
                normalizedSlug,
                request.SortOrder,
                now,
                profile,
                criticality,
                request.Code,
                request.Description,
                request.Region,
                request.IsProductionLike,
                request.IsPrimaryProduction);

            environmentRepository.Add(environment);

            return Result<Response>.Success(new Response(environment.Id.Value, environment.Name, environment.Slug));
        }
    }
}
