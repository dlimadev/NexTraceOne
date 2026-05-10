namespace NexTraceOne.BackgroundWorkers.Configuration;

/// <summary>
/// Configurações do ContractConsumerIngestionJob.
/// Controla frequência, ambientes observados, janela de lookback e tenant-scope.
/// Secção de configuração: BackgroundWorkers:ContractConsumerIngestion
/// </summary>
public sealed class ContractConsumerIngestionOptions
{
    /// <summary>Secção de configuração no appsettings.</summary>
    public const string SectionName = "BackgroundWorkers:ContractConsumerIngestion";

    /// <summary>Habilita ou desabilita a execução do job. Padrão: true.</summary>
    public bool Enabled { get; init; } = true;

    /// <summary>Intervalo entre ciclos de ingestão. Padrão: 15 minutos.</summary>
    public TimeSpan IntervalBetweenCycles { get; init; } = TimeSpan.FromMinutes(15);

    /// <summary>Janela de lookback para traces OTel. Padrão: 1 hora.</summary>
    public TimeSpan LookbackWindow { get; init; } = TimeSpan.FromHours(1);

    /// <summary>
    /// Ambientes OTel a observar para consumer tracking.
    /// Padrão: production e staging.
    /// </summary>
    public List<string> Environments { get; init; } = ["production", "staging"];

    /// <summary>
    /// Número máximo de traces a consultar por ambiente por ciclo.
    /// Padrão: 5000.
    /// </summary>
    public int MaxTracesPerEnvironment { get; init; } = 5000;

    /// <summary>
    /// Tenant ID usado para registrar o inventário de consumidores.
    /// Em deployments multi-tenant, este valor é o tenant raiz do sistema.
    /// Padrão: "system".
    /// </summary>
    public string TenantId { get; init; } = "system";
}
