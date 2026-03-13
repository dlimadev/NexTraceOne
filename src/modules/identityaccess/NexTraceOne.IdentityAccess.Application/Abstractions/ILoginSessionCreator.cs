using NexTraceOne.Identity.Domain.Entities;

namespace NexTraceOne.Identity.Application.Abstractions;

/// <summary>
/// Contrato para criação centralizada de sessões de autenticação no módulo Identity.
///
/// Responsabilidade única: encapsular a criação e persistência de sessões,
/// garantindo que todos os fluxos de autenticação (local, OIDC, federado, refresh)
/// criem sessões de forma consistente.
///
/// Decisão de design:
/// - Interface injetável via DI — permite mock em testes e respeita DIP.
/// - Parâmetros padronizados: duração de 30 dias, "unknown" para IP/UA ausentes.
///
/// Refatoração: extraído da classe estática LoginSessionCreator para aderir
/// ao Dependency Inversion Principle e facilitar testes unitários.
/// </summary>
public interface ILoginSessionCreator
{
    /// <summary>
    /// Cria uma nova sessão de autenticação, persiste no repositório e retorna o refresh token em texto plano.
    ///
    /// Fluxo:
    /// 1. Gera refresh token aleatório via IJwtTokenGenerator.
    /// 2. Cria hash do refresh token para armazenamento seguro.
    /// 3. Cria entidade Session com expiração de 30 dias.
    /// 4. Persiste a sessão via ISessionRepository.
    /// 5. Retorna o refresh token em texto plano para envio ao cliente.
    /// </summary>
    /// <returns>Tupla com a sessão criada e o refresh token em texto plano.</returns>
    (Session Session, string RefreshToken) CreateSession(
        UserId userId,
        string? ipAddress,
        string? userAgent);
}
