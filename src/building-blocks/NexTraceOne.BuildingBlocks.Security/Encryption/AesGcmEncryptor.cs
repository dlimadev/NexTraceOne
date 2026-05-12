using System.Security.Cryptography;
using System.Text;

namespace NexTraceOne.BuildingBlocks.Security.Encryption;

/// <summary>
/// Implementação de criptografia AES-256-GCM para campos sensíveis.
/// Usado pelo EncryptedStringConverter do EF Core.
/// </summary>
public sealed class AesGcmEncryptor
{
    private const int _nonceSize = 12;
    private const int _tagSize = 16;

    /// <summary>Criptografa um texto usando AES-256-GCM e retorna Base64.</summary>
    public static string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
        {
            return plainText;
        }

        var key = ResolveEncryptionKey();
        var nonce = RandomNumberGenerator.GetBytes(_nonceSize);
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var cipherBytes = new byte[plainBytes.Length];
        var tag = new byte[_tagSize];

        using var aes = new AesGcm(key, _tagSize);
        aes.Encrypt(nonce, plainBytes, cipherBytes, tag);

        var payload = new byte[_nonceSize + _tagSize + cipherBytes.Length];
        Buffer.BlockCopy(nonce, 0, payload, 0, _nonceSize);
        Buffer.BlockCopy(tag, 0, payload, _nonceSize, _tagSize);
        Buffer.BlockCopy(cipherBytes, 0, payload, _nonceSize + _tagSize, cipherBytes.Length);

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
        if (payload.Length < _nonceSize + _tagSize)
        {
            throw new InvalidOperationException("Encrypted payload is invalid.");
        }

        var key = ResolveEncryptionKey();
        var nonce = payload[.._nonceSize];
        var tag = payload[_nonceSize..(_nonceSize + _tagSize)];
        var cipherBytes = payload[(_nonceSize + _tagSize)..];
        var plainBytes = new byte[cipherBytes.Length];

        using var aes = new AesGcm(key, _tagSize);
        aes.Decrypt(nonce, cipherBytes, tag, plainBytes);

        return Encoding.UTF8.GetString(plainBytes);
    }

    /// <summary>
    /// Resolve a chave de criptografia a partir da variável de ambiente NEXTRACE_ENCRYPTION_KEY.
    /// Em desenvolvimento, usa fallback inseguro quando a variável não estiver definida.
    /// Em todos os outros ambientes a variável é obrigatória.
    /// </summary>
    private static byte[] ResolveEncryptionKey()
    {
        var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
            ?? "Production";
        var isDevelopment = env.Equals("Development", StringComparison.OrdinalIgnoreCase);
        return EncryptionKeyMaterial.ResolveWithFallback(isDevelopment);
    }
}
