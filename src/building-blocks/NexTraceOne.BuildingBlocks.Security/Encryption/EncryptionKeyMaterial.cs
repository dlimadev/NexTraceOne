using System.Text;

namespace NexTraceOne.BuildingBlocks.Security.Encryption;

public static class EncryptionKeyMaterial
{
    public const string EnvironmentVariableName = "NEXTRACE_ENCRYPTION_KEY";

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

    public static void ValidateRequiredEnvironmentVariable()
    {
        _ = ResolveFromEnvironment();
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
