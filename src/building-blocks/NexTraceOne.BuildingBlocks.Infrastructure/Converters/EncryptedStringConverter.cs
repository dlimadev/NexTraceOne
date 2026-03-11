using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace NexTraceOne.BuildingBlocks.Infrastructure.Converters;

/// <summary>
/// EF Core Value Converter que criptografa campos marcados com [Encrypted] usando AES-256-GCM.
/// Criptografa automaticamente ao salvar e descriptografa ao ler.
/// </summary>
public sealed class EncryptedStringConverter : ValueConverter<string, string>
{
    public EncryptedStringConverter()
        : base(
            plainText => Encrypt(plainText),
            cipherText => Decrypt(cipherText))
    { }

    private static string Encrypt(string plainText)
    {
        // TODO: Implementar AES-256-GCM encryption
        throw new NotImplementedException();
    }

    private static string Decrypt(string cipherText)
    {
        // TODO: Implementar AES-256-GCM decryption
        throw new NotImplementedException();
    }
}
