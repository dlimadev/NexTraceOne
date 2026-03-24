using NexTraceOne.BuildingBlocks.Security.Encryption;
using NexTraceOne.Configuration.Application.Abstractions;

namespace NexTraceOne.Configuration.Infrastructure.Services;

/// <summary>
/// Implementação de segurança para valores de configuração sensíveis.
/// Utiliza AesGcmEncryptor do BuildingBlocks para criptografia AES-256-GCM.
/// </summary>
internal sealed class ConfigurationSecurityService : IConfigurationSecurityService
{
    private const string FullMask = "••••••••";
    private const int MinLengthForPartialReveal = 4;
    private const int RevealedChars = 2;

    public string EncryptValue(string plainValue)
        => AesGcmEncryptor.Encrypt(plainValue);

    public string DecryptValue(string encryptedValue)
        => AesGcmEncryptor.Decrypt(encryptedValue);

    public string MaskValue(string value)
    {
        if (string.IsNullOrEmpty(value) || value.Length < MinLengthForPartialReveal)
        {
            return FullMask;
        }

        return string.Concat(value.AsSpan(0, RevealedChars), "••••••");
    }
}
