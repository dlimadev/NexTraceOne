namespace NexTraceOne.Configuration.Application.Abstractions;

/// <summary>
/// Service abstraction for encryption and masking of sensitive configuration values.
/// Implementation provided by the Infrastructure layer.
/// </summary>
public interface IConfigurationSecurityService
{
    /// <summary>Encrypts a plain-text value for secure storage at rest.</summary>
    string EncryptValue(string plainValue);

    /// <summary>Decrypts a previously encrypted value back to plain text.</summary>
    string DecryptValue(string encryptedValue);

    /// <summary>Returns a masked representation of the value (e.g. "******") for UI display and logs.</summary>
    string MaskValue(string value);
}
