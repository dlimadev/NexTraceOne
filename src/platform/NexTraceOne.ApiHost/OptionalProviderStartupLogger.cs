using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace NexTraceOne.ApiHost;

/// <summary>
/// OPS-03 — logger diagnóstico de providers opcionais no arranque da plataforma.
///
/// Alguns providers do NexTraceOne têm implementações Null* por defeito quando a integração
/// externa correspondente não está configurada (canary, backup, Kafka, cloud billing, …).
/// Esses Null* descartam silenciosamente as operações — o que é aceitável, mas apenas se
/// o operador souber que isso está a acontecer. Este logger torna explícito, nos logs de
/// arranque, quais providers estão em modo degradado para que o operador não seja surpreendido
/// por dashboards a mostrar "simulatedNote" em produção.
///
/// Convenções de severidade:
/// - Development → informativo (todos os providers, configurados ou não)
/// - Staging/Production/outros → warning por cada provider <c>IsConfigured = false</c>
///
/// A superfície reutilizável <see cref="LogProviderStatuses"/> é testável de forma
/// determinística sem precisar de um <see cref="Microsoft.AspNetCore.Builder.WebApplication"/>.
/// </summary>
public static class OptionalProviderStartupLogger
{
    /// <summary>
    /// Regista o estado de cada provider opcional registado na plataforma.
    /// </summary>
    /// <param name="logger">Logger do host.</param>
    /// <param name="environmentName">Nome do ambiente (Development/Staging/Production/…).</param>
    /// <param name="providerStatuses">
    /// Dicionário ordenado de <c>providerName → IsConfigured</c>. A ordem de iteração
    /// controla a ordem dos logs.
    /// </param>
    public static void LogProviderStatuses(
        ILogger logger,
        string environmentName,
        IReadOnlyDictionary<string, bool> providerStatuses)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(environmentName);
        ArgumentNullException.ThrowIfNull(providerStatuses);

        var isDevelopment = string.Equals(environmentName, Environments.Development, StringComparison.OrdinalIgnoreCase);
        var configured = providerStatuses.Where(p => p.Value).Select(p => p.Key).ToList();
        var notConfigured = providerStatuses.Where(p => !p.Value).Select(p => p.Key).ToList();

        logger.LogInformation(
            "Optional providers status — Environment: {Environment}, Configured: {ConfiguredCount}/{TotalCount}",
            environmentName,
            configured.Count,
            providerStatuses.Count);

        if (configured.Count > 0)
        {
            logger.LogInformation(
                "Optional providers configured: {Providers}",
                string.Join(", ", configured));
        }

        if (notConfigured.Count == 0)
        {
            return;
        }

        if (isDevelopment)
        {
            logger.LogInformation(
                "Optional providers running with Null* fallbacks (Development): {Providers}. " +
                "Related dashboards will show simulatedNote. Configure before promoting to non-Development environments.",
                string.Join(", ", notConfigured));
            return;
        }

        foreach (var providerName in notConfigured)
        {
            logger.LogWarning(
                "Optional provider '{Provider}' is NOT configured in {Environment} environment. " +
                "Operations delegated to this provider will be silently dropped by the Null* fallback. " +
                "See /admin/system-health or docs/deployment/PRODUCTION-BOOTSTRAP.md for setup instructions.",
                providerName,
                environmentName);
        }
    }
}
