using System.Diagnostics;

namespace NexTraceOne.ApiHost.Preflight.Checks;

/// <summary>
/// Verifica a RAM total disponível — mínimo recomendado de 4 GB.
/// Check de aviso — não bloqueia o startup.
/// </summary>
public sealed class RamPreflightCheck : IPreflightCheck
{
    private const string CheckName = "RAM";
    private const long MinRamGb = 4;

    public Task<IReadOnlyList<PreflightCheckResult>> RunAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<PreflightCheckResult>>([Execute()]);

    private static PreflightCheckResult Execute()
    {
        try
        {
            var memInfo = GC.GetGCMemoryInfo();
            var totalRamGb = memInfo.TotalAvailableMemoryBytes / (1024L * 1024L * 1024L);

            if (totalRamGb < MinRamGb)
            {
                return new PreflightCheckResult(
                    CheckName, PreflightCheckStatus.Warning,
                    $"Available RAM: {totalRamGb} GB (minimum recommended: {MinRamGb} GB).",
                    "Increase server RAM to at least 4 GB for stable operation.",
                    IsRequired: false);
            }

            var processRamMb = Process.GetCurrentProcess().WorkingSet64 / (1024L * 1024L);
            return new PreflightCheckResult(
                CheckName, PreflightCheckStatus.Ok,
                $"RAM: {totalRamGb} GB total | Process RSS: {processRamMb} MB.");
        }
        catch (Exception ex)
        {
            return new PreflightCheckResult(
                CheckName, PreflightCheckStatus.Warning,
                $"Could not check RAM: {ex.Message}",
                IsRequired: false, Suggestion: null);
        }
    }
}
