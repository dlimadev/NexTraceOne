using System.Reflection;
using NexTraceOne.ApiHost.Preflight.Checks;

namespace NexTraceOne.ApiHost.Preflight;

/// <summary>
/// Orquestrador de preflight checks — executa todos os checks registados e consolida o relatório.
/// Responsabilidade única: agregar resultados e produzir o <see cref="PreflightReport"/> final.
/// Cada check individual é implementado numa classe dedicada que implementa <see cref="IPreflightCheck"/>.
///
/// Checks registados via DI (ver Program.cs):
///   - <see cref="PostgreSqlPreflightCheck"/> — PostgreSQL acessível e versão ≥ 15 (obrigatório)
///   - <see cref="JwtSecretPreflightCheck"/> — JWT Secret configurado e ≥ 32 chars (obrigatório)
///   - <see cref="ConnectionStringsPreflightCheck"/> — pelo menos uma connection string (obrigatório)
///   - <see cref="DiskSpacePreflightCheck"/> — disco ≥ 5 GB (aviso)
///   - <see cref="RamPreflightCheck"/> — RAM ≥ 4 GB (aviso)
///   - <see cref="PortsPreflightCheck"/> — portas 8080/8090 livres (aviso)
///   - <see cref="OllamaPreflightCheck"/> — Ollama acessível (aviso)
///   - <see cref="SmtpPreflightCheck"/> — SMTP configurado (aviso)
///   - <see cref="OtelCollectorPreflightCheck"/> — OTel Collector acessível (aviso)
///   - <see cref="CorsOriginsPreflightCheck"/> — CORS origins configuradas (aviso)
/// </summary>
public sealed class PreflightCheckService(IEnumerable<IPreflightCheck> checks)
{
    /// <summary>
    /// Executa todos os checks registados e retorna o relatório consolidado.
    /// Nunca lança excepção — erros internos são capturados por cada check individual.
    /// </summary>
    public async Task<PreflightReport> RunAsync(CancellationToken ct = default)
    {
        var results = new List<PreflightCheckResult>();

        foreach (var check in checks)
        {
            var checkResults = await check.RunAsync(ct);
            results.AddRange(checkResults);
        }

        var hasErrors = results.Any(c => c.Status == PreflightCheckStatus.Error && c.IsRequired);
        var hasWarnings = results.Any(c => c.Status == PreflightCheckStatus.Warning);

        var overall = hasErrors
            ? PreflightCheckStatus.Error
            : hasWarnings ? PreflightCheckStatus.Warning : PreflightCheckStatus.Ok;

        var version = Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "1.0.0";

        return new PreflightReport(
            OverallStatus: overall,
            Checks: results,
            IsReadyToStart: !hasErrors,
            CheckedAt: DateTimeOffset.UtcNow,
            Version: version);
    }
}
