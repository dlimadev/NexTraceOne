using Ardalis.GuardClauses;
using FluentValidation;
using MediatR;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Identity.Application.Abstractions;
using NexTraceOne.Identity.Domain.Entities;
using NexTraceOne.Identity.Domain.Errors;

namespace NexTraceOne.Identity.Application.Features.GrantEnvironmentAccess;

/// <summary>
/// Feature: GrantEnvironmentAccess — concede acesso a um ambiente para um utilizador.
///
/// Permite a um administrador conceder acesso granular (read, write, admin) a um
/// ambiente específico dentro do tenant. Suporta grants temporários com data de
/// expiração para cenários de acesso controlado.
///
/// Regras de negócio:
/// - Apenas administradores com permissão identity:users:write podem conceder acesso.
/// - O ambiente deve existir e estar ativo.
/// - O usuário deve existir.
/// - Se ExpiresAt for informado, deve ser uma data futura.
/// - O AccessLevel deve ser um valor válido (read, write, admin, none).
/// - Toda concessão gera SecurityEvent para auditoria.
/// </summary>
public static class GrantEnvironmentAccess
{
    /// <summary>Comando para conceder acesso a um ambiente.</summary>
    public sealed record Command(
        Guid UserId,
        Guid EnvironmentId,
        string AccessLevel,
        DateTimeOffset? ExpiresAt) : ICommand;

    /// <summary>Valida os parâmetros de concessão de acesso.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.UserId).NotEmpty();
            RuleFor(x => x.EnvironmentId).NotEmpty();
            RuleFor(x => x.AccessLevel).NotEmpty().MaximumLength(50);
        }
    }

    /// <summary>
    /// Handler que cria um novo acesso de ambiente para o utilizador.
    /// Regista SecurityEvent para trilha de auditoria obrigatória.
    /// </summary>
    public sealed class Handler(
        ICurrentUser currentUser,
        ICurrentTenant currentTenant,
        IUserRepository userRepository,
        IEnvironmentRepository environmentRepository,
        ISecurityEventRepository securityEventRepository,
        ISecurityEventTracker securityEventTracker,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<Command>
    {
        public async Task<Result<Unit>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            if (currentTenant.Id == Guid.Empty)
                return IdentityErrors.TenantContextRequired();

            var tenantId = TenantId.From(currentTenant.Id);

            // Valida que o ambiente existe e pertence ao tenant
            var environment = await environmentRepository.GetByIdAsync(
                EnvironmentId.From(request.EnvironmentId), cancellationToken);
            if (environment is null || environment.TenantId != tenantId)
                return IdentityErrors.EnvironmentNotFound(request.EnvironmentId);

            if (!environment.IsActive)
                return IdentityErrors.EnvironmentNotActive(request.EnvironmentId);

            // Valida que o utilizador existe
            var user = await userRepository.GetByIdAsync(
                UserId.From(request.UserId), cancellationToken);
            if (user is null)
                return IdentityErrors.UserNotFound(request.UserId);

            var now = dateTimeProvider.UtcNow;

            // Valida nível de acesso
            if (!EnvironmentAccessLevel.IsValid(request.AccessLevel))
                return IdentityErrors.InvalidEnvironmentAccessLevel(request.AccessLevel);

            // Cria acesso ao ambiente
            var grantedById = Guid.TryParse(currentUser.Id, out var grantedByGuid)
                ? UserId.From(grantedByGuid)
                : user.Id;

            var access = EnvironmentAccess.Create(
                user.Id,
                tenantId,
                environment.Id,
                request.AccessLevel,
                grantedById,
                now,
                request.ExpiresAt);

            environmentRepository.AddAccess(access);

            // Regista SecurityEvent para auditoria
            var securityEvent = SecurityEvent.Create(
                tenantId,
                user.Id,
                sessionId: null,
                SecurityEventType.EnvironmentAccessGranted,
                $"Environment access granted: user '{user.Id.Value}' received '{request.AccessLevel}' access to environment '{environment.Name}'.",
                riskScore: 20,
                ipAddress: null,
                userAgent: null,
                metadataJson: System.Text.Json.JsonSerializer.Serialize(new
                {
                    environmentId = environment.Id.Value,
                    environmentName = environment.Name,
                    accessLevel = request.AccessLevel,
                    expiresAt = request.ExpiresAt?.ToString("O"),
                    grantedBy = currentUser.Id
                }),
                now);

            securityEventRepository.Add(securityEvent);
            securityEventTracker.Track(securityEvent);

            return Unit.Value;
        }
    }
}
