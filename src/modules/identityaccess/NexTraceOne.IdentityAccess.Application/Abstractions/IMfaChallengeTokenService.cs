namespace NexTraceOne.IdentityAccess.Application.Abstractions;

/// <summary>
/// Contrato para emissão e validação de tokens de desafio MFA de curta duração.
/// Os tokens são utilizados no fluxo de dois passos de autenticação com MFA:
/// Passo 1 (LocalLogin): emissão do token de desafio quando MFA é necessário.
/// Passo 2 (VerifyMfaChallenge): validação do token + código TOTP → emissão de sessão real.
/// </summary>
public interface IMfaChallengeTokenService
{
    /// <summary>
    /// Emite um token de desafio MFA assinado de curta duração para o utilizador indicado.
    /// O token codifica o UserId e o prazo de validade, assinado com HMAC.
    /// </summary>
    /// <param name="userId">Identificador do utilizador que deve completar o desafio.</param>
    /// <param name="expiresAt">Prazo de validade do token.</param>
    /// <returns>Token de desafio opaco para envio ao cliente.</returns>
    string Issue(Guid userId, DateTimeOffset expiresAt);

    /// <summary>
    /// Valida um token de desafio MFA e extrai o UserId caso o token seja válido e não tenha expirado.
    /// </summary>
    /// <param name="token">Token de desafio recebido do cliente.</param>
    /// <param name="userId">UserId extraído do token quando válido.</param>
    /// <returns>Verdadeiro se o token é válido e não expirado.</returns>
    bool TryValidate(string token, out Guid userId);
}
