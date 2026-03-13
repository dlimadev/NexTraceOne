using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Identity.Application.Abstractions;
using NexTraceOne.Identity.Domain.Entities;
using NexTraceOne.Identity.Domain.ValueObjects;

namespace NexTraceOne.Identity.Application.Features;

/// <summary>
/// Serviço utilitário interno para criação de sessões de autenticação no módulo Identity.
///
/// Extraído dos handlers LocalLogin e OidcCallback para eliminar a duplicação de lógica
/// de criação de Session (refresh token hash, expiração, metadados de dispositivo)
/// espalhada entre múltiplos handlers de autenticação.
///
/// Responsabilidade única: encapsular a criação e persistência de sessões,
/// garantindo que todos os fluxos de autenticação (local, OIDC, refresh)
/// criem sessões de forma consistente.
///
/// Parâmetros padronizados:
/// - Duração de sessão: 30 dias (configurável em evolução futura).
/// - IP e User-Agent: capturados para rastreabilidade — "unknown" quando não disponível.
/// </summary>
internal static class LoginSessionCreator
{
    /// <summary>
    /// Cria uma nova sessão de autenticação, persiste no repositório e retorna o refresh token em texto plano.
    /// O refresh token é gerado pelo IJwtTokenGenerator e hasheado antes de persistir.
    ///
    /// Fluxo:
    /// 1. Gera refresh token aleatório via IJwtTokenGenerator.
    /// 2. Cria hash do refresh token para armazenamento seguro.
    /// 3. Cria entidade Session com expiração de 30 dias.
    /// 4. Persiste a sessão via ISessionRepository.
    /// 5. Retorna o refresh token em texto plano para envio ao cliente.
    /// </summary>
    /// <returns>Tupla com a sessão criada e o refresh token em texto plano.</returns>
    public static (Session Session, string RefreshToken) CreateSession(
        IJwtTokenGenerator jwtTokenGenerator,
        ISessionRepository sessionRepository,
        IDateTimeProvider dateTimeProvider,
        UserId userId,
        string? ipAddress,
        string? userAgent)
    {
        var refreshToken = jwtTokenGenerator.GenerateRefreshToken();

        var session = Session.Create(
            userId,
            RefreshTokenHash.Create(refreshToken),
            dateTimeProvider.UtcNow.AddDays(30),
            ipAddress ?? "unknown",
            userAgent ?? "unknown");

        sessionRepository.Add(session);

        return (session, refreshToken);
    }
}
