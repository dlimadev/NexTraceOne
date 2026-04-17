using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Domain.Entities;
using NexTraceOne.IdentityAccess.Domain.Errors;

namespace NexTraceOne.IdentityAccess.Application.Features.DeactivateEnvironment;

/// <summary>
/// Feature: DeactivateEnvironment — desativa um ambiente do tenant atual.
///
/// Um ambiente desativado deixa de estar disponível para atribuição de acessos
/// e operações. O registo não é eliminado (soft-delete opcional separado).
///
/// Regras de negócio:
/// - O ambiente deve pertencer ao tenant atual.
/// - O ambiente deve existir.
/// - O ambiente de produção principal não pode ser desativado diretamente;
///   é necessário revogar a designação de produção principal primeiro.
/// - Um ambiente já inativo pode ser desativado novamente sem erro (idempotente).
/// </summary>
public static class DeactivateEnvironment
{
    /// <summary>Comando para desativar um ambiente existente.</summary>
    public sealed record Command(Guid EnvironmentId) : ICommand<Response>;

    /// <summary>Resposta com estado atualizado do ambiente.</summary>
    public sealed record Response(Guid EnvironmentId, string Name, bool IsActive);

    /// <summary>Valida os parâmetros do comando.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.EnvironmentId).NotEmpty();
        }
    }

    /// <summary>
    /// Handler que desativa o ambiente após validação das regras de negócio.
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

            // Idempotente: ambiente já inativo não precisa de ação
            if (!environment.IsActive)
                return Result<Response>.Success(new Response(environment.Id.Value, environment.Name, false));

            // Validação de domínio: produção principal não pode ser desativada
            if (environment.IsPrimaryProduction)
                return IdentityErrors.CannotDeactivatePrimaryProduction(request.EnvironmentId);

            environment.Deactivate();
            environment.SetUpdated(currentUser.Id, dateTimeProvider.UtcNow);

            return Result<Response>.Success(new Response(environment.Id.Value, environment.Name, false));
        }
    }
}
