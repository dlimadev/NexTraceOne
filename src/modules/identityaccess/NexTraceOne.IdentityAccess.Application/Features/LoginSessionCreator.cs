using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Domain.Entities;
using NexTraceOne.IdentityAccess.Domain.ValueObjects;

namespace NexTraceOne.IdentityAccess.Application.Features;

/// <summary>
/// Implementação injetável da criação centralizada de sessões de autenticação no módulo Identity.
///
/// Responsabilidade única: encapsular a criação e persistência de sessões,
/// garantindo que todos os fluxos de autenticação (local, OIDC, federado, refresh)
/// criem sessões de forma consistente.
///
/// Decisão de design:
/// - Classe injetável via DI (Scoped) — compartilha repositórios do request corrente.
/// - Dependências recebidas por construtor — respeita DIP, permite mock em testes.
/// - Parâmetros padronizados: duração de 30 dias, "unknown" para IP/UA ausentes.
///
/// Refatoração: migrado de classe estática para serviço injetável para aderir
/// ao Dependency Inversion Principle e facilitar testes unitários dos handlers.
/// </summary>
internal sealed class LoginSessionCreator(
    IJwtTokenGenerator jwtTokenGenerator,
    ISessionRepository sessionRepository,
    IDateTimeProvider dateTimeProvider) : ILoginSessionCreator
{
    /// <inheritdoc />
    public (Session Session, string RefreshToken) CreateSession(
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
