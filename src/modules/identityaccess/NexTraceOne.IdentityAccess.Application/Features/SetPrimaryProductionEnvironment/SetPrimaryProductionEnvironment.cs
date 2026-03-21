using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Domain.Entities;
using NexTraceOne.IdentityAccess.Domain.Errors;

namespace NexTraceOne.IdentityAccess.Application.Features.SetPrimaryProductionEnvironment;

/// <summary>
/// Feature: SetPrimaryProductionEnvironment — designa um ambiente como a produção principal do tenant.
///
/// Este é um fluxo crítico de governança: ao designar um ambiente como produção principal,
/// o sistema revoga a designação de qualquer outro ambiente que esteja marcado como tal.
///
/// O ambiente produtivo principal é usado por:
/// - A IA para comparação com ambientes não produtivos
/// - O motor de análise de risco de release
/// - O assessment de readiness para promoção
/// - Os relatórios de impacto potencial em produção
///
/// Regras de negócio:
/// - O ambiente deve pertencer ao tenant atual.
/// - O ambiente deve estar ativo.
/// - Somente um ambiente pode ser produção principal por tenant.
/// - Ao designar novo ambiente, o anterior é automaticamente retirado da designação.
/// </summary>
public static class SetPrimaryProductionEnvironment
{
    /// <summary>Comando para designar um ambiente como produção principal.</summary>
    public sealed record Command(Guid EnvironmentId) : ICommand<Response>;

    /// <summary>Resposta confirmando a designação.</summary>
    public sealed record Response(
        Guid EnvironmentId,
        string EnvironmentName,
        Guid? PreviousPrimaryEnvironmentId,
        string? PreviousPrimaryEnvironmentName);

    /// <summary>Valida os parâmetros do comando.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.EnvironmentId).NotEmpty();
        }
    }

    /// <summary>
    /// Handler que executa a troca da designação de produção principal.
    /// Revoga o anterior e designa o novo em uma única transação (gerenciada pelo pipeline).
    /// </summary>
    public sealed class Handler(
        ICurrentTenant currentTenant,
        IEnvironmentRepository environmentRepository) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            if (currentTenant.Id == Guid.Empty)
                return IdentityErrors.TenantContextRequired();

            var tenantId = TenantId.From(currentTenant.Id);
            var environmentId = EnvironmentId.From(request.EnvironmentId);

            // Validar que o ambiente existe e pertence ao tenant
            var environment = await environmentRepository.GetByIdForTenantAsync(environmentId, tenantId, cancellationToken);
            if (environment is null)
                return IdentityErrors.EnvironmentNotFound(request.EnvironmentId);

            if (!environment.IsActive)
                return IdentityErrors.CannotDesignateInactiveAsPrimaryProduction(request.EnvironmentId);

            // Se já é o primário, não há nada a fazer
            if (environment.IsPrimaryProduction)
                return Result<Response>.Success(new Response(environment.Id.Value, environment.Name, null, null));

            // Revogar a designação do ambiente produtivo atual (se houver)
            var currentPrimary = await environmentRepository.GetPrimaryProductionAsync(tenantId, cancellationToken);
            Guid? previousId = null;
            string? previousName = null;
            if (currentPrimary is not null)
            {
                previousId = currentPrimary.Id.Value;
                previousName = currentPrimary.Name;
                currentPrimary.RevokePrimaryProductionDesignation();
            }

            // Designar o novo ambiente como produção principal
            environment.DesignateAsPrimaryProduction();

            return Result<Response>.Success(new Response(
                environment.Id.Value,
                environment.Name,
                previousId,
                previousName));
        }
    }
}
