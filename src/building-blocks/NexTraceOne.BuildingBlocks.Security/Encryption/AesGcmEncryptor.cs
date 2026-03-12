using System.Security.Cryptography;
using System.Text;

namespace NexTraceOne.BuildingBlocks.Security.Encryption;

/// <summary>
/// Implementação de criptografia AES-256-GCM para campos sensíveis.
/// Usado pelo EncryptedStringConverter do EF Core.
/// </summary>
public sealed class AesGcmEncryptor
{
    private const int NonceSize = 12;
    private const int TagSize = 16;

    /// <summary>Criptografa um texto usando AES-256-GCM e retorna Base64.</summary>
    public static string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
        {
            return plainText;
        }

        var key = ResolveEncryptionKey();
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var cipherBytes = new byte[plainBytes.Length];
        var tag = new byte[TagSize];

        using var aes = new AesGcm(key, TagSize);
        aes.Encrypt(nonce, plainBytes, cipherBytes, tag);

        var payload = new byte[NonceSize + TagSize + cipherBytes.Length];
        Buffer.BlockCopy(nonce, 0, payload, 0, NonceSize);
        Buffer.BlockCopy(tag, 0, payload, NonceSize, TagSize);
        Buffer.BlockCopy(cipherBytes, 0, payload, NonceSize + TagSize, cipherBytes.Length);

        return Convert.ToBase64String(payload);
    }

    /// <summary>Descriptografa um texto Base64 previamente protegido com AES-256-GCM.</summary>
    public static string Decrypt(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText))
        {
            return cipherText;
        }

        var payload = Convert.FromBase64String(cipherText);
        if (payload.Length < NonceSize + TagSize)
        {
            throw new InvalidOperationException("Encrypted payload is invalid.");
        }

        var key = ResolveEncryptionKey();
        var nonce = payload[..NonceSize];
        var tag = payload[NonceSize..(NonceSize + TagSize)];
        var cipherBytes = payload[(NonceSize + TagSize)..];
        var plainBytes = new byte[cipherBytes.Length];

        using var aes = new AesGcm(key, TagSize);
        aes.Decrypt(nonce, cipherBytes, tag, plainBytes);

        return Encoding.UTF8.GetString(plainBytes);
    }

    /// <summary>
    /// Resolve a chave de criptografia a partir da variável de ambiente NEXTRACE_ENCRYPTION_KEY.
    ///
    /// Em produção, a chave DEVE ser fornecida externamente. Nunca usar fallback hardcoded
    /// para evitar que dados sensíveis sejam protegidos por uma chave conhecida publicamente.
    ///
    /// Em desenvolvimento (ASPNETCORE_ENVIRONMENT=Development), uma chave derivada é usada
    /// como fallback apenas para conveniência, com log de aviso.
    /// </summary>
    private static byte[] ResolveEncryptionKey()
    {
        var configuredKey = Environment.GetEnvironmentVariable("NEXTRACE_ENCRYPTION_KEY");

        if (!string.IsNullOrWhiteSpace(configuredKey))
        {
            try
            {
                var decoded = Convert.FromBase64String(configuredKey);
                if (decoded.Length == 32)
                {
                    return decoded;
                }
            }
            catch (FormatException)
            {
                // Chave não é Base64 válida — tenta como UTF-8 direto
            }

            var utf8Bytes = Encoding.UTF8.GetBytes(configuredKey);
            if (utf8Bytes.Length == 32)
            {
                return utf8Bytes;
            }

            // Chave fornecida mas com tamanho inválido — deriva via SHA-256
            return SHA256.HashData(Encoding.UTF8.GetBytes(configuredKey));
        }

        // Nenhuma chave configurada: fallback de desenvolvimento apenas
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
        if (!string.Equals(environment, "Development", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "NEXTRACE_ENCRYPTION_KEY environment variable is required in non-development environments. " +
                "Provide a Base64-encoded 32-byte key or a 32-character UTF-8 string.");
        }

        return SHA256.HashData(Encoding.UTF8.GetBytes("NexTraceOne-Development-Only-Key-Not-For-Production"));
    }
}
