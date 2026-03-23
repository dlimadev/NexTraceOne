using System.Reflection;
using NexTraceOne.BuildingBlocks.Core.Attributes;
using NexTraceOne.BuildingBlocks.Infrastructure.Converters;
using NexTraceOne.BuildingBlocks.Security.Encryption;

namespace NexTraceOne.BuildingBlocks.Infrastructure.Tests.Encryption;

/// <summary>
/// Testes de encriptação at-rest para campos sensíveis.
/// Garante que o AesGcmEncryptor, EncryptedStringConverter e EncryptedFieldAttribute
/// funcionam corretamente e que o NexTraceDbContextBase aplica a convenção de encriptação.
/// </summary>
public sealed class EncryptionAtRestTests : IDisposable
{
    private readonly string? _previousEncryptionKey;
    private readonly string? _previousEnvironment;

    public EncryptionAtRestTests()
    {
        // Preserve existing env vars and set Development fallback
        _previousEncryptionKey = Environment.GetEnvironmentVariable("NEXTRACE_ENCRYPTION_KEY");
        _previousEnvironment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

        Environment.SetEnvironmentVariable("NEXTRACE_ENCRYPTION_KEY", null);
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable("NEXTRACE_ENCRYPTION_KEY", _previousEncryptionKey);
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", _previousEnvironment);
    }

    [Fact]
    public void AesGcmEncryptor_RoundTrips_Correctly()
    {
        const string plainText = "sensitive-api-key-12345";

        var encrypted = AesGcmEncryptor.Encrypt(plainText);
        var decrypted = AesGcmEncryptor.Decrypt(encrypted);

        decrypted.Should().Be(plainText);
    }

    [Fact]
    public void AesGcmEncryptor_EncryptedValue_DiffersFromPlainText()
    {
        const string plainText = "my-secret-value";

        var encrypted = AesGcmEncryptor.Encrypt(plainText);

        encrypted.Should().NotBe(plainText);
        encrypted.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void AesGcmEncryptor_Encrypt_ReturnsNull_WhenInputIsNull()
    {
        var result = AesGcmEncryptor.Encrypt(null!);

        result.Should().BeNull();
    }

    [Fact]
    public void AesGcmEncryptor_Encrypt_ReturnsEmpty_WhenInputIsEmpty()
    {
        var result = AesGcmEncryptor.Encrypt(string.Empty);

        result.Should().BeEmpty();
    }

    [Fact]
    public void AesGcmEncryptor_Decrypt_ReturnsNull_WhenInputIsNull()
    {
        var result = AesGcmEncryptor.Decrypt(null!);

        result.Should().BeNull();
    }

    [Fact]
    public void AesGcmEncryptor_Decrypt_ReturnsEmpty_WhenInputIsEmpty()
    {
        var result = AesGcmEncryptor.Decrypt(string.Empty);

        result.Should().BeEmpty();
    }

    [Fact]
    public void AesGcmEncryptor_ProducesDifferentCiphertext_ForSamePlaintext()
    {
        const string plainText = "same-value";

        var encrypted1 = AesGcmEncryptor.Encrypt(plainText);
        var encrypted2 = AesGcmEncryptor.Encrypt(plainText);

        // AES-GCM uses random nonce, so each encryption produces different ciphertext
        encrypted1.Should().NotBe(encrypted2);

        // Both should decrypt to same value
        AesGcmEncryptor.Decrypt(encrypted1).Should().Be(plainText);
        AesGcmEncryptor.Decrypt(encrypted2).Should().Be(plainText);
    }

    [Fact]
    public void EncryptedStringConverter_Exists_AndIsValueConverter()
    {
        var converterType = typeof(EncryptedStringConverter);

        converterType.Should().NotBeNull();
        converterType.IsSealed.Should().BeTrue("EncryptedStringConverter should be sealed");
        converterType.BaseType!.Name.Should().Contain("ValueConverter",
            "EncryptedStringConverter should inherit from EF Core ValueConverter");
    }

    [Fact]
    public void EncryptedFieldAttribute_Exists_AndIsCorrectlyDefined()
    {
        var attrType = typeof(EncryptedFieldAttribute);

        attrType.Should().NotBeNull();
        attrType.IsSealed.Should().BeTrue("EncryptedFieldAttribute should be sealed");

        var usage = attrType.GetCustomAttribute<AttributeUsageAttribute>();
        usage.Should().NotBeNull("EncryptedFieldAttribute must have AttributeUsage");
        usage!.ValidOn.Should().HaveFlag(AttributeTargets.Property,
            "EncryptedFieldAttribute should be applicable to properties");
        usage.AllowMultiple.Should().BeFalse(
            "EncryptedFieldAttribute should not allow multiple instances on the same property");
    }

    [Fact]
    public void EncryptedFieldAttribute_CanBeAppliedToProperties()
    {
        // Verify the attribute can be instantiated and applied
        var attr = new EncryptedFieldAttribute();
        attr.Should().NotBeNull();

        // Verify it can be read from a property via reflection
        var prop = typeof(SampleEntityWithEncryptedField)
            .GetProperty(nameof(SampleEntityWithEncryptedField.Secret));

        prop.Should().NotBeNull();
        prop!.GetCustomAttribute<EncryptedFieldAttribute>().Should().NotBeNull(
            "EncryptedFieldAttribute should be retrievable via reflection");
    }

    [Fact]
    public void NexTraceDbContextBase_HasApplyEncryptedFieldConventionMethod()
    {
        var dbContextBaseType = typeof(NexTraceOne.BuildingBlocks.Infrastructure.Persistence.NexTraceDbContextBase);

        var method = dbContextBaseType.GetMethod(
            "ApplyEncryptedFieldConvention",
            BindingFlags.NonPublic | BindingFlags.Static);

        method.Should().NotBeNull(
            "NexTraceDbContextBase must contain ApplyEncryptedFieldConvention method " +
            "to automatically apply EncryptedStringConverter to [EncryptedField] properties");
    }

    [Fact]
    public void NexTraceDbContextBase_OnModelCreating_CallsEncryptedFieldConvention()
    {
        // Verify via source analysis that OnModelCreating calls ApplyEncryptedFieldConvention
        var solutionRoot = FindSolutionRoot();
        var dbContextPath = Path.Combine(
            solutionRoot, "src", "building-blocks",
            "NexTraceOne.BuildingBlocks.Infrastructure", "Persistence", "NexTraceDbContextBase.cs");

        var content = File.ReadAllText(dbContextPath);

        content.Should().Contain("ApplyEncryptedFieldConvention(modelBuilder)",
            "OnModelCreating must call ApplyEncryptedFieldConvention to apply encryption converters");
        content.Should().Contain("EncryptedStringConverter",
            "ApplyEncryptedFieldConvention must use EncryptedStringConverter");
        content.Should().Contain("EncryptedFieldAttribute",
            "ApplyEncryptedFieldConvention must check for EncryptedFieldAttribute");
    }

    [Fact]
    public void AesGcmEncryptor_RoundTrips_UnicodeContent()
    {
        const string plainText = "Olá Mundo — 日本語テスト — 🔐";

        var encrypted = AesGcmEncryptor.Encrypt(plainText);
        var decrypted = AesGcmEncryptor.Decrypt(encrypted);

        decrypted.Should().Be(plainText);
    }

    [Fact]
    public void AesGcmEncryptor_RoundTrips_LongContent()
    {
        var plainText = new string('A', 10_000);

        var encrypted = AesGcmEncryptor.Encrypt(plainText);
        var decrypted = AesGcmEncryptor.Decrypt(encrypted);

        decrypted.Should().Be(plainText);
    }

    private static string FindSolutionRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "NexTraceOne.sln")))
            dir = dir.Parent;
        return dir?.FullName ?? throw new InvalidOperationException("Could not find solution root.");
    }

    /// <summary>Sample entity to verify EncryptedFieldAttribute can be applied.</summary>
    private sealed class SampleEntityWithEncryptedField
    {
        [EncryptedField]
        public string Secret { get; set; } = string.Empty;

        public string NotSecret { get; set; } = string.Empty;
    }
}
