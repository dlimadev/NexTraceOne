namespace NexTraceOne.ApiHost.Preflight.Checks;

/// <summary>
/// Verifica o espaço em disco disponível — mínimo recomendado de 5 GB.
/// Check de aviso — não bloqueia o startup.
/// </summary>
public sealed class DiskSpacePreflightCheck : IPreflightCheck
{
    private const string CheckName = "Disk Space";
    private const long MinDiskGb = 5;

    public Task<IReadOnlyList<PreflightCheckResult>> RunAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<PreflightCheckResult>>([Execute()]);

    private static PreflightCheckResult Execute()
    {
        try
        {
            var drive = DriveInfo.GetDrives()
                .Where(d => d.IsReady && d.DriveType == DriveType.Fixed)
                .OrderByDescending(d => d.AvailableFreeSpace)
                .FirstOrDefault();

            if (drive is null)
            {
                return new PreflightCheckResult(
                    CheckName, PreflightCheckStatus.Warning,
                    "No fixed drives detected.",
                    IsRequired: false, Suggestion: null);
            }

            var availableGb = drive.AvailableFreeSpace / (1024L * 1024L * 1024L);
            var totalGb = drive.TotalSize / (1024L * 1024L * 1024L);

            if (availableGb < MinDiskGb)
            {
                return new PreflightCheckResult(
                    CheckName, PreflightCheckStatus.Warning,
                    $"Low disk space: {availableGb} GB available on {drive.Name} (minimum recommended: {MinDiskGb} GB).",
                    "Free up disk space or mount a larger volume.",
                    IsRequired: false);
            }

            return new PreflightCheckResult(
                CheckName, PreflightCheckStatus.Ok,
                $"Disk: {availableGb} GB available / {totalGb} GB total on {drive.Name}.");
        }
        catch (Exception ex)
        {
            return new PreflightCheckResult(
                CheckName, PreflightCheckStatus.Warning,
                $"Could not check disk space: {ex.Message}",
                IsRequired: false, Suggestion: null);
        }
    }
}
