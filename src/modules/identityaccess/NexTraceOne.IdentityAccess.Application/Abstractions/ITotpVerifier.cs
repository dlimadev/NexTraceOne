namespace NexTraceOne.IdentityAccess.Application.Abstractions;

/// <summary>
/// Contrato para verificação de códigos TOTP (Time-Based One-Time Password) conforme RFC 6238.
/// Permite verificar se um código fornecido é válido para o segredo configurado pelo utilizador.
/// </summary>
public interface ITotpVerifier
{
    /// <summary>
    /// Verifica se o código TOTP fornecido é válido para o segredo base32 dado.
    /// Aceita códigos dentro de uma janela de tolerância de ±1 passo (30 segundos) para compensar
    /// desvios de relógio entre o cliente e o servidor.
    /// </summary>
    /// <param name="base32Secret">Segredo TOTP codificado em base32 (armazenado em User.MfaSecret).</param>
    /// <param name="code">Código de 6 dígitos fornecido pelo utilizador.</param>
    /// <returns>Verdadeiro se o código é válido dentro da janela de tolerância.</returns>
    bool Verify(string base32Secret, string code);
}
