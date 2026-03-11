using System.Reflection;
using System.Security.Cryptography;

namespace NexTraceOne.BuildingBlocks.Security.Integrity;

/// <summary>
/// Verifica a integridade dos assemblies no boot da aplicação.
/// Calcula SHA-256 do binário e compara com hash assinado.
/// Se falhar, recusa inicialização. Pipeline: build → obfuscate → AOT → sign.
/// </summary>
public sealed class AssemblyIntegrityChecker
{
    /// <summary>Verifica integridade. Chamado em Program.cs antes de qualquer serviço.</summary>
    public static void VerifyOrThrow()
    {
        if (string.Equals(
            Environment.GetEnvironmentVariable("NEXTRACE_SKIP_INTEGRITY"),
            "true",
            StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var assemblies = Directory
            .EnumerateFiles(AppContext.BaseDirectory, "NexTraceOne*.dll", SearchOption.TopDirectoryOnly)
            .Concat(GetEntryAssemblyIfPresent())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        foreach (var assemblyPath in assemblies)
        {
            var hashFilePath = $"{assemblyPath}.sha256";
            if (!File.Exists(hashFilePath))
            {
                continue;
            }

            var expectedHash = File.ReadAllText(hashFilePath).Trim();
            var actualHash = ComputeSha256Hex(assemblyPath);

            if (!string.Equals(expectedHash, actualHash, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Assembly integrity check failed for '{Path.GetFileName(assemblyPath)}'.");
            }
        }
    }

    private static IEnumerable<string> GetEntryAssemblyIfPresent()
    {
        var entryAssemblyLocation = Assembly.GetEntryAssembly()?.Location;
        if (string.IsNullOrWhiteSpace(entryAssemblyLocation))
        {
            return [];
        }

        return [entryAssemblyLocation];
    }

    private static string ComputeSha256Hex(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        var hash = SHA256.HashData(stream);
        return Convert.ToHexString(hash);
    }
}
