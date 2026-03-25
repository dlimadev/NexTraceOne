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
    /// A chave DEVE ser fornecida externamente em todos os ambientes. Nunca usar fallback hardcoded
    /// para evitar que dados sensíveis sejam protegidos por uma chave conhecida publicamente.
    /// </summary>
    private static byte[] ResolveEncryptionKey()
    {
        var configuredKey = Environment.GetEnvironmentVariable("NEXTRACE_ENCRYPTION_KEY");
        if (string.IsNullOrWhiteSpace(configuredKey))
        {
            throw new InvalidOperationException(
                "NEXTRACE_ENCRYPTION_KEY environment variable is required. " +
                "Provide a Base64-encoded 32-byte key or a 32-character UTF-8 string.");
        }

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

        throw new InvalidOperationException(
            "NEXTRACE_ENCRYPTION_KEY is invalid. Provide a Base64-encoded 32-byte key or a 32-character UTF-8 string.");
    }
}
