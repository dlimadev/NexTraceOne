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
        // TODO: Implementar verificação com assinatura GPG
        // Bypass via NEXTRACE_SKIP_INTEGRITY=true em desenvolvimento
    }
}
