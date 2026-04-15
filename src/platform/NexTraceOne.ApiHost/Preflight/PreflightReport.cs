namespace NexTraceOne.ApiHost.Preflight;

/// <summary>
/// Relatório consolidado de todos os preflight checks executados.
/// Retornado pelo endpoint GET /preflight (sem autenticação) e pela página /preflight/ui.
/// </summary>
/// <param name="OverallStatus">Estado agregado: Ok se todos passam, Warning se há avisos, Error se algum check obrigatório falhou.</param>
/// <param name="Checks">Lista de resultados individuais.</param>
/// <param name="IsReadyToStart">true se o sistema pode iniciar (sem checks obrigatórios em erro).</param>
/// <param name="CheckedAt">Timestamp UTC de quando os checks foram executados.</param>
/// <param name="Version">Versão do produto.</param>
public sealed record PreflightReport(
    PreflightCheckStatus OverallStatus,
    IReadOnlyList<PreflightCheckResult> Checks,
    bool IsReadyToStart,
    DateTimeOffset CheckedAt,
    string Version);
