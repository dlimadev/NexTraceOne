using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Domain.Entities;
using NexTraceOne.IdentityAccess.Domain.Enums;
using NexTraceOne.IdentityAccess.Domain.Errors;

namespace NexTraceOne.IdentityAccess.Application.Features.UpdateEnvironment;

/// <summary>
/// Feature: UpdateEnvironment — atualiza metadados de um ambiente do tenant atual.
///
/// Permite atualizar nome, ordem, perfil operacional, criticidade, código, região e descrição
/// de um ambiente existente. O slug não pode ser alterado (imutável para evitar quebra de referências).
///
/// Regras de negócio:
/// - O ambiente deve pertencer ao tenant atual.
/// - O ambiente deve existir.
/// - O slug é imutável após criação.
/// - O perfil e criticidade podem ser atualizados via UpdateProfile.
/// </summary>
public static class UpdateEnvironment
{
    /// <summary>Comando para atualização de um ambiente existente.</summary>
    public sealed record Command(
        Guid EnvironmentId,
        string Name,
        int SortOrder,
        string Profile,
        string Criticality,
        string? Code,
        string? Description,
        string? Region,
        bool? IsProductionLike) : ICommand<Response>;

    /// <summary>Resposta com dados atualizados do ambiente.</summary>
    public sealed record Response(
        Guid EnvironmentId,
        string Name,
        string Slug,
        string Profile,
        bool IsProductionLike);

    /// <summary>Valida os parâmetros do comando.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.EnvironmentId).NotEmpty();
            RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
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
    /// Handler que aplica as atualizações ao ambiente.
    /// O commit é gerenciado pelo TransactionBehavior do pipeline.
    /// </summary>
    public sealed class Handler(
        ICurrentTenant currentTenant,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider,
        IEnvironmentRepository environmentRepository) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            if (currentTenant.Id == Guid.Empty)
                return IdentityErrors.TenantContextRequired();

            var tenantId = TenantId.From(currentTenant.Id);
            var environmentId = EnvironmentId.From(request.EnvironmentId);

            var environment = await environmentRepository.GetByIdForTenantAsync(environmentId, tenantId, cancellationToken);
            if (environment is null)
                return IdentityErrors.EnvironmentNotFound(request.EnvironmentId);

            var profile = System.Enum.Parse<EnvironmentProfile>(request.Profile, ignoreCase: true);
            var criticality = System.Enum.Parse<EnvironmentCriticality>(request.Criticality, ignoreCase: true);

            environment.UpdateBasicInfo(request.Name, request.SortOrder);
            environment.UpdateProfile(profile, criticality, request.IsProductionLike);
            environment.UpdateLocationInfo(request.Code, request.Region, request.Description);
            environment.SetUpdated(currentUser.Id, dateTimeProvider.UtcNow);

            return Result<Response>.Success(new Response(
                environment.Id.Value,
                environment.Name,
                environment.Slug,
                environment.Profile.ToString().ToLowerInvariant(),
                environment.IsProductionLike));
        }
    }
}
