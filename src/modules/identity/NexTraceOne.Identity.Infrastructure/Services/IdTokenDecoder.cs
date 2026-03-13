using System.Text;
using System.Text.Json;

namespace NexTraceOne.Identity.Infrastructure.Services;

/// <summary>
/// Decodifica o payload de ID Tokens JWT sem validação de assinatura.
///
/// Responsabilidade única: extrair claims de um id_token JWT (formato Base64Url).
/// Extraído de OidcProviderService para separar a lógica de parsing de tokens
/// da lógica de comunicação HTTP com o provider OIDC.
///
/// Para MVP1, confiamos no TLS e na resposta direta do token endpoint do provider.
/// Para produção, a validação de assinatura com JWK do provider deve ser implementada
/// antes desta etapa de decodificação.
///
/// Regra de segurança:
/// - Nunca usar este decoder para confiar em tokens recebidos de fontes externas
///   sem que o token tenha vindo diretamente do token endpoint via TLS.
/// - O token deve ser obtido via server-to-server, nunca de input do usuário.
/// </summary>
internal static class IdTokenDecoder
{
    /// <summary>
    /// Decodifica o payload de um JWT (id_token) e retorna as claims string extraídas.
    /// Ignora claims não-string (números, booleanos, arrays) pois o fluxo OIDC
    /// precisa apenas de sub, email e name — todos strings.
    ///
    /// Retorna dicionário vazio se o token for malformado ou não contiver payload válido.
    /// Nunca lança exceção — falhas de parsing resultam em dicionário vazio para não
    /// bloquear o fluxo de autenticação com erros de formato inesperado.
    /// </summary>
    /// <param name="idToken">JWT no formato header.payload.signature (Base64Url).</param>
    /// <returns>Dicionário case-insensitive com claims string do payload.</returns>
    public static Dictionary<string, string> DecodeClaims(string idToken)
    {
        var parts = idToken.Split('.');
        if (parts.Length < 2)
            return [];

        var payload = parts[1];

        // Corrige padding Base64Url → Base64 padrão
        var padding = payload.Length % 4;
        if (padding > 0)
            payload += new string('=', 4 - padding);

        try
        {
            var payloadBytes = Convert.FromBase64String(payload.Replace('-', '+').Replace('_', '/'));
            var payloadJson = Encoding.UTF8.GetString(payloadBytes);

            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            using var doc = JsonDocument.Parse(payloadJson);
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                if (prop.Value.ValueKind == JsonValueKind.String)
                {
                    result[prop.Name] = prop.Value.GetString() ?? string.Empty;
                }
            }

            return result;
        }
        catch
        {
            // Token malformado — retorna dicionário vazio para não bloquear o fluxo
            return [];
        }
    }
}
