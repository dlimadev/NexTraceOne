using FluentAssertions;
using NexTraceOne.BuildingBlocks.Security.Encryption;

namespace NexTraceOne.BuildingBlocks.Security.Tests.Encryption;

public sealed class AesGcmEncryptorTests : IDisposable
{
    private const string ValidUtf8Key = "12345678901234567890123456789012";

    private readonly string? _originalEncryptionKey;
    private readonly string? _originalEnvironment;

    public AesGcmEncryptorTests()
    {
        _originalEncryptionKey = Environment.GetEnvironmentVariable("NEXTRACE_ENCRYPTION_KEY");
        _originalEnvironment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
        SetEncryptionKey(ValidUtf8Key);
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable("NEXTRACE_ENCRYPTION_KEY", _originalEncryptionKey);
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", _originalEnvironment);
    }

    private static void SetEncryptionKey(string? key)
    {
        Environment.SetEnvironmentVariable("NEXTRACE_ENCRYPTION_KEY", key);
    }

    [Fact]
    public void EncryptDecrypt_Roundtrip_ReturnsOriginalText()
    {
        SetEncryptionKey(ValidUtf8Key); // 32-byte UTF-8 key

        var plainText = "Sensitive data to protect";
        var encrypted = AesGcmEncryptor.Encrypt(plainText);
        var decrypted = AesGcmEncryptor.Decrypt(encrypted);

        decrypted.Should().Be(plainText);
    }

    [Fact]
    public void Encrypt_ProducesDifferentCiphertextEachTime()
    {
        SetEncryptionKey(ValidUtf8Key);

        var plainText = "Same text encrypted twice";
        var encrypted1 = AesGcmEncryptor.Encrypt(plainText);
        var encrypted2 = AesGcmEncryptor.Encrypt(plainText);

        encrypted1.Should().NotBe(encrypted2, "random nonce should produce different ciphertext");
    }

    [Fact]
    public void Decrypt_WithTamperedCiphertext_Throws()
    {
        SetEncryptionKey(ValidUtf8Key);

        var encrypted = AesGcmEncryptor.Encrypt("Secret data");
        var bytes = Convert.FromBase64String(encrypted);
        bytes[^1] ^= 0xFF; // Flip last byte
        var tampered = Convert.ToBase64String(bytes);

        var act = () => AesGcmEncryptor.Decrypt(tampered);

        act.Should().Throw<Exception>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Encrypt_WithNullOrEmpty_ReturnsSameValue(string? input)
    {
        SetEncryptionKey(ValidUtf8Key);

        var result = AesGcmEncryptor.Encrypt(input!);

        result.Should().Be(input);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Decrypt_WithNullOrEmpty_ReturnsSameValue(string? input)
    {
        SetEncryptionKey(ValidUtf8Key);

        var result = AesGcmEncryptor.Decrypt(input!);

        result.Should().Be(input);
    }

    [Fact]
    public void EncryptDecrypt_WithBase64Key_Works()
    {
        var keyBytes = new byte[32];
        System.Security.Cryptography.RandomNumberGenerator.Fill(keyBytes);
        var base64Key = Convert.ToBase64String(keyBytes);
        SetEncryptionKey(base64Key);

        var plainText = "Data with Base64 key";
        var encrypted = AesGcmEncryptor.Encrypt(plainText);
        var decrypted = AesGcmEncryptor.Decrypt(encrypted);

        decrypted.Should().Be(plainText);
    }

    [Fact]
    public void Encrypt_WithoutConfiguredKey_Throws()
    {
        SetEncryptionKey(null);
        var act = () => AesGcmEncryptor.Encrypt("test");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*NEXTRACE_ENCRYPTION_KEY*");
    }

    [Fact]
    public void Encrypt_InNonDevelopment_WithoutKey_Throws()
    {
        SetEncryptionKey(null);
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Production");

        var act = () => AesGcmEncryptor.Encrypt("test");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*NEXTRACE_ENCRYPTION_KEY*");
    }

    [Fact]
    public void EncryptDecrypt_WithNonStandardLengthKey_DerivesSha256()
    {
        SetEncryptionKey("short-key");

        var act = () => AesGcmEncryptor.Encrypt("Data with invalid key");
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*NEXTRACE_ENCRYPTION_KEY*invalid*");
    }

    [Fact]
    public void Decrypt_WithInvalidBase64_Throws()
    {
        SetEncryptionKey(ValidUtf8Key);

        var act = () => AesGcmEncryptor.Decrypt("not-valid-base64!!!");

        act.Should().Throw<FormatException>();
    }

    [Fact]
    public void Decrypt_WithPayloadTooShort_Throws()
    {
        SetEncryptionKey(ValidUtf8Key);

        // Create a Base64 payload that's too short (less than nonce + tag = 28 bytes)
        var shortPayload = Convert.ToBase64String(new byte[10]);

        var act = () => AesGcmEncryptor.Decrypt(shortPayload);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*payload*invalid*");
    }

    [Fact]
    public void EncryptDecrypt_WithUnicodeText_Roundtrips()
    {
        SetEncryptionKey(ValidUtf8Key);

        var plainText = "Dados sensíveis: アクセストークン 🔐";
        var encrypted = AesGcmEncryptor.Encrypt(plainText);
        var decrypted = AesGcmEncryptor.Decrypt(encrypted);

        decrypted.Should().Be(plainText);
    }
}
