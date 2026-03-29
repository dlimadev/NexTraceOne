using NexTraceOne.Catalog.Domain.Contracts.Services;

namespace NexTraceOne.Catalog.Domain.LegacyAssets.Services;

/// <summary>
/// Adaptador que conecta o CopybookDiffCalculator ao pipeline multi-protocolo do ContractDiffCalculator.
/// Aceita texto raw COBOL, faz parse e calcula o diff, devolvendo o DiffResult partilhado.
/// </summary>
public static class CopybookDiffAdapter
{
    /// <summary>
    /// Computa o diff semântico entre dois textos raw de copybook COBOL,
    /// fazendo parse de ambos e delegando ao CopybookDiffCalculator.
    /// </summary>
    /// <param name="baseSpec">Texto COBOL do copybook base (versão anterior).</param>
    /// <param name="targetSpec">Texto COBOL do copybook alvo (versão mais recente).</param>
    /// <returns>Resultado no formato DiffResult partilhado pelo pipeline multi-protocolo.</returns>
    public static OpenApiDiffCalculator.DiffResult ComputeDiff(string baseSpec, string targetSpec)
    {
        var baseLayout = CopybookParser.Parse(baseSpec);
        var targetLayout = CopybookParser.Parse(targetSpec);
        var diff = CopybookDiffCalculator.ComputeDiff(baseLayout, targetLayout);

        return new OpenApiDiffCalculator.DiffResult(
            diff.BreakingChanges,
            diff.AdditiveChanges,
            diff.NonBreakingChanges,
            diff.ChangeLevel);
    }
}
