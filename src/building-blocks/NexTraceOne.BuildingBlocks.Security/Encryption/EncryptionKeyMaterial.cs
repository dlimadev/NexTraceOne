using System.Text;

namespace NexTraceOne.BuildingBlocks.Security.Encryption;

public static class EncryptionKeyMaterial
{
    public const string EnvironmentVariableName = "NEXTRACE_ENCRYPTION_KEY";

    // Chave de fallback exclusiva para ambiente de desenvolvimento local.
    // Nunca deve ser usada em ambientes não produtivos reais ou em produção.
    // Para configurar corretamente: definir a variável de ambiente NEXTRACE_ENCRYPTION_KEY.
    private const string DevFallbackKey = "NexTrace-Dev-Only-Fallback-Key!!"; // 32 UTF-8 bytes

    public static byte[] ResolveFromEnvironment()
    {
        var configuredKey = Environment.GetEnvironmentVariable(EnvironmentVariableName);
        if (string.IsNullOrWhiteSpace(configuredKey))
        {
            throw new InvalidOperationException(
                $"{EnvironmentVariableName} environment variable is required. " +
                "Provide a Base64-encoded 32-byte key or a 32-character UTF-8 string.");
        }

        if (TryResolve(configuredKey, out var keyBytes))
        {
            return keyBytes;
        }

        throw new InvalidOperationException(
            $"{EnvironmentVariableName} is invalid. Provide a Base64-encoded 32-byte key or a 32-character UTF-8 string.");
    }

    /// <summary>
    /// Resolve a chave de encriptação, usando fallback inseguro em desenvolvimento quando a
    /// variável de ambiente não estiver configurada. Em todos os outros ambientes comporta-se
    /// como <see cref="ResolveFromEnvironment"/>.
    /// </summary>
    public static byte[] ResolveWithFallback(bool isDevelopment)
    {
        var configuredKey = Environment.GetEnvironmentVariable(EnvironmentVariableName);
        if (!string.IsNullOrWhiteSpace(configuredKey))
        {
            return ResolveFromEnvironment();
        }

        if (isDevelopment)
        {
            return Encoding.UTF8.GetBytes(DevFallbackKey);
        }

        return ResolveFromEnvironment(); // throws with the proper message
    }

    public static void ValidateRequiredEnvironmentVariable(bool isDevelopment = false)
    {
        var configuredKey = Environment.GetEnvironmentVariable(EnvironmentVariableName);
        if (!string.IsNullOrWhiteSpace(configuredKey))
        {
            _ = ResolveFromEnvironment();
            return;
        }

        if (isDevelopment)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Error.WriteLine(
                "[NexTraceOne Security] WARNING: Encryption key not configured. " +
                "Using insecure development fallback. DO NOT use in non-development environments. " +
                $"Configure via: set {EnvironmentVariableName}=<base64-32-byte-key>");
            Console.ResetColor();
            return;
        }

        _ = ResolveFromEnvironment(); // throws with the proper message
    }

    private static bool TryResolve(string configuredKey, out byte[] keyBytes)
    {
        if (TryDecodeBase64(configuredKey, out keyBytes))
        {
            return true;
        }

        if (Encoding.UTF8.GetByteCount(configuredKey) == 32)
        {
            keyBytes = Encoding.UTF8.GetBytes(configuredKey);
            return true;
        }

        keyBytes = [];
        return false;
    }

    private static bool TryDecodeBase64(string configuredKey, out byte[] decoded)
    {
        decoded = [];
        var buffer = new byte[32];
        if (!Convert.TryFromBase64String(configuredKey, buffer, out var bytesWritten))
        {
            return false;
        }

        if (bytesWritten != 32)
        {
            return false;
        }

        decoded = buffer;
        return true;
    }
}
