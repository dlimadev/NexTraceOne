using System.Security.Cryptography;

using NexTraceOne.IdentityAccess.Application.Abstractions;

namespace NexTraceOne.IdentityAccess.Infrastructure.Services;

/// <summary>
/// Implementação de verificação TOTP (Time-Based One-Time Password) conforme RFC 6238.
/// Usa HMAC-SHA1 e janela de tolerância de ±1 passo de 30 segundos.
/// Não requer dependências externas — usa apenas System.Security.Cryptography.
/// </summary>
internal sealed class TotpVerifier : ITotpVerifier
{
    /// <summary>Duração de cada passo TOTP, em segundos.</summary>
    private const int StepSeconds = 30;

    /// <summary>Comprimento esperado do código TOTP (6 dígitos).</summary>
    private const int CodeLength = 6;

    /// <summary>Módulo para truncar o código HMAC-OTP.</summary>
    private const int CodeModulus = 1_000_000;

    /// <inheritdoc />
    public bool Verify(string base32Secret, string code)
    {
        if (string.IsNullOrWhiteSpace(base32Secret) || string.IsNullOrWhiteSpace(code))
            return false;

        if (code.Length != CodeLength || !code.All(char.IsDigit))
            return false;

        byte[] keyBytes;
        try
        {
            keyBytes = Base32Decode(base32Secret);
        }
        catch
        {
            System.Diagnostics.Trace.TraceWarning("TotpVerifier: Invalid Base32 secret format.");
            return false;
        }

        var counter = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / StepSeconds;

        // Aceita passo anterior, actual e próximo para compensar desvios de relógio
        for (var offset = -1; offset <= 1; offset++)
        {
            if (ComputeHotp(keyBytes, (ulong)(counter + offset)) == code)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Calcula o HOTP para o contador dado usando HMAC-SHA1.
    /// Conforme RFC 4226 secção 5.3.
    /// </summary>
    private static string ComputeHotp(byte[] key, ulong counter)
    {
        var counterBytes = BitConverter.GetBytes(counter);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(counterBytes);

        using var hmac = new HMACSHA1(key);
        var hash = hmac.ComputeHash(counterBytes);

        // Dynamic truncation conforme RFC 4226 secção 5.3
        var offset = hash[hash.Length - 1] & 0x0f;
        var truncated = ((hash[offset] & 0x7f) << 24)
                      | ((hash[offset + 1] & 0xff) << 16)
                      | ((hash[offset + 2] & 0xff) << 8)
                      | (hash[offset + 3] & 0xff);

        return (truncated % CodeModulus).ToString("D6");
    }

    /// <summary>
    /// Decodifica uma string Base32 (RFC 4648) para bytes.
    /// Suporta maiúsculas e minúsculas. Padding ('=') é opcional.
    /// </summary>
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
            if (charIndex < 0)
                throw new FormatException($"Invalid base32 character: '{c}'");

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
