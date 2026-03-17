using NexTraceOne.IdentityAccess.Domain.Entities;

using LocalLoginFeature = NexTraceOne.IdentityAccess.Application.Features.LocalLogin.LocalLogin;

namespace NexTraceOne.IdentityAccess.Application.Abstractions;

/// <summary>
/// Contrato para construção de respostas padronizadas de login e resolução
/// de memberships no módulo Identity.
///
/// Responsabilidade única: centralizar operações compartilhadas entre múltiplos
/// handlers de autenticação (LocalLogin, OidcCallback, FederatedLogin, RefreshToken),
/// eliminando duplicação sem acoplar handlers entre si.
///
/// Decisão de design:
/// - Interface injetável via DI — permite mock em testes e respeita DIP.
/// - Escopo Scoped — compartilha contexto do request corrente (ICurrentTenant).
///
/// Refatoração: extraído da classe estática IdentityFeatureSupport para aderir
/// ao Dependency Inversion Principle e facilitar testes unitários.
/// </summary>
public interface ILoginResponseBuilder
{
    /// <summary>
    /// Identificador do tenant corrente do request, extraído de ICurrentTenant.
    /// Usado pelos handlers para mensagens de erro contextualizadas.
    /// Retorna Guid.Empty quando não há contexto de tenant disponível.
    /// </summary>
    Guid CurrentTenantId { get; }

    /// <summary>
    /// Resolve o vínculo ativo do usuário com base no tenant atual ou no primeiro vínculo disponível.
    /// Retorna null se o usuário não possui membership ativo em nenhum tenant.
    /// </summary>
    Task<TenantMembership?> ResolveMembershipAsync(
        UserId userId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Cria a resposta padronizada de autenticação incluindo access token, refresh token,
    /// dados do usuário, tenant, role e permissões.
    /// </summary>
    LocalLoginFeature.LoginResponse CreateLoginResponse(
        User user,
        TenantMembership membership,
        Role role,
        string refreshToken);
}
