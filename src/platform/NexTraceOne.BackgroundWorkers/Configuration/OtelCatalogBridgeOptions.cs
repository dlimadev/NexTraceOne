namespace NexTraceOne.BackgroundWorkers.Configuration;

/// <summary>
/// Opções de configuração para o OtelCatalogBridgeJob.
/// </summary>
public sealed class OtelCatalogBridgeOptions
{
    public const string SectionName = "OtelCatalogBridge";

    /// <summary>Activa ou desactiva o job (padrão: true).</summary>
    public bool Enabled { get; init; } = true;

    /// <summary>Número mínimo de traces para considerar um serviço de alto tráfego (padrão: 1000).</summary>
    public long MinTraceCountToAlert { get; init; } = 1000;

    /// <summary>Número máximo de serviços a processar por ciclo (padrão: 50).</summary>
    public int MaxServicesToProcessPerCycle { get; init; } = 50;

    /// <summary>Intervalo entre ciclos de detecção (padrão: 30 minutos).</summary>
    public TimeSpan IntervalBetweenCycles { get; init; } = TimeSpan.FromMinutes(30);
}
