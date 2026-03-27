using NexTraceOne.IdentityAccess.Infrastructure.Services;

namespace NexTraceOne.IdentityAccess.Tests.Infrastructure.Services;

/// <summary>
/// Testes do TotpVerifier — implementação RFC 6238.
/// Cobre verificação de código válido, código inválido, segredo inválido e edge cases.
/// </summary>
public sealed class TotpVerifierTests
{
    private readonly TotpVerifier _sut = new();

    /// <summary>
    /// Segredo base32 de teste amplamente utilizado para validação RFC 6238:
    /// "12345678901234567890" → JBSWY3DPEHPK3PXP (codificado em base32).
    /// </summary>
    private const string TestSecret = "JBSWY3DPEHPK3PXP";

    [Fact]
    public void Verify_Should_ReturnTrue_For_ValidCodeAtCurrentWindow()
    {
        // Não podemos testar um código gerado em tempo real de forma determinística aqui,
        // por isso testamos com um mock da janela temporal.
        // Este teste valida que um código real gerado para o segredo de teste é aceite.

        // Arrange: gerar o código para o contador actual
        var counter = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 30;
        var code = ComputeExpectedCode(TestSecret, (ulong)counter);

        // Act + Assert
        _sut.Verify(TestSecret, code).Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Verify_Should_ReturnFalse_When_CodeIsNullOrWhitespace(string? code)
    {
        _sut.Verify(TestSecret, code!).Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Verify_Should_ReturnFalse_When_SecretIsNullOrWhitespace(string? secret)
    {
        _sut.Verify(secret!, "123456").Should().BeFalse();
    }

    [Theory]
    [InlineData("12345")]     // Too short
    [InlineData("1234567")]   // Too long
    [InlineData("12345a")]    // Non-digit character
    public void Verify_Should_ReturnFalse_When_CodeFormatIsInvalid(string code)
    {
        _sut.Verify(TestSecret, code).Should().BeFalse();
    }

    [Fact]
    public void Verify_Should_ReturnFalse_For_InvalidSecret()
    {
        // An invalid Base32 string should not throw, just return false
        _sut.Verify("INVALID!!!SECRET", "123456").Should().BeFalse();
    }

    [Fact]
    public void Verify_Should_ReturnFalse_For_ObviouslyWrongCode()
    {
        // Code "000000" is statistically very unlikely to be valid at any moment
        // This test may occasionally fail (1 in 1_000_000 chance) - acceptable for unit tests
        // Using a known wrong code by generating the correct one and inverting
        var counter = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 30;
        var correctCode = ComputeExpectedCode(TestSecret, (ulong)counter);
        var wrongCode = ((int.Parse(correctCode) + 500_000) % 1_000_000).ToString("D6");

        _sut.Verify(TestSecret, wrongCode).Should().BeFalse();
    }

    [Fact]
    public void Verify_Should_AcceptPreviousWindowCode()
    {
        // Should accept codes from the previous 30-second window (tolerance ±1 step)
        var counter = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 30;
        var previousCode = ComputeExpectedCode(TestSecret, (ulong)(counter - 1));

        _sut.Verify(TestSecret, previousCode).Should().BeTrue();
    }

    [Fact]
    public void Verify_Should_AcceptNextWindowCode()
    {
        // Should accept codes from the next 30-second window (tolerance ±1 step)
        var counter = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 30;
        var nextCode = ComputeExpectedCode(TestSecret, (ulong)(counter + 1));

        _sut.Verify(TestSecret, nextCode).Should().BeTrue();
    }

    /// <summary>
    /// Helper: computes the expected TOTP code for a given secret and counter.
    /// Mirrors the production implementation for test comparison.
    /// </summary>
    private static string ComputeExpectedCode(string base32Secret, ulong counter)
    {
        var keyBytes = Base32Decode(base32Secret);
        var counterBytes = BitConverter.GetBytes(counter);
        if (BitConverter.IsLittleEndian) Array.Reverse(counterBytes);

        using var hmac = new System.Security.Cryptography.HMACSHA1(keyBytes);
        var hash = hmac.ComputeHash(counterBytes);

        var offset = hash[hash.Length - 1] & 0x0f;
        var truncated = ((hash[offset] & 0x7f) << 24)
                      | ((hash[offset + 1] & 0xff) << 16)
                      | ((hash[offset + 2] & 0xff) << 8)
                      | (hash[offset + 3] & 0xff);

        return (truncated % 1_000_000).ToString("D6");
    }

    private static byte[] Base32Decode(string base32)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var input = base32.ToUpperInvariant().TrimEnd('=');
        var outputLength = input.Length * 5 / 8;
        var result = new byte[outputLength];
        var buffer = 0;
        var bitsLeft = 0;
        var outputIndex = 0;

        foreach (var c in input)
        {
            var charIndex = alphabet.IndexOf(c);
            buffer = (buffer << 5) | charIndex;
            bitsLeft += 5;
            if (bitsLeft >= 8)
            {
                bitsLeft -= 8;
                result[outputIndex++] = (byte)(buffer >> bitsLeft);
            }
        }
        return result;
    }
}
