namespace NexTraceOne.BackgroundWorkers.Configuration;

/// <summary>
/// Configurações do DriftDetectionJob.
/// Controla frequência, ambientes analisados, tolerância e janela de análise.
/// Secção de configuração: BackgroundWorkers:DriftDetection
/// </summary>
public sealed class DriftDetectionOptions
{
    /// <summary>Secção de configuração no appsettings.</summary>
    public const string SectionName = "BackgroundWorkers:DriftDetection";

    /// <summary>Habilita ou desabilita a execução do job. Padrão: true.</summary>
    public bool Enabled { get; init; } = true;

    /// <summary>Intervalo entre ciclos de detecção. Padrão: 5 minutos.</summary>
    public TimeSpan IntervalBetweenCycles { get; init; } = TimeSpan.FromMinutes(5);

    /// <summary>Janela de análise para snapshots recentes. Padrão: 1 hora.</summary>
    public TimeSpan AnalysisWindow { get; init; } = TimeSpan.FromHours(1);

    /// <summary>
    /// Lista de ambientes comparáveis. Vazia = todos os ambientes são analisados.
    /// Exemplo: ["dev", "test", "qa", "uat", "staging", "production"]
    /// </summary>
    public List<string> ComparableEnvironments { get; init; } = [];

    /// <summary>Percentual de tolerância para desvio antes de criar um finding. Padrão: 10%.</summary>
    public decimal TolerancePercent { get; init; } = 10m;
}
