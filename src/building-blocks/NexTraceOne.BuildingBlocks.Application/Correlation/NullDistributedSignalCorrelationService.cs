namespace NexTraceOne.BuildingBlocks.Application.Correlation;

/// <summary>
/// Implementação nula de IDistributedSignalCorrelationService.
/// Retorna correlações vazias — sem dados de módulos operacionais.
///
/// Registrada como padrão em BuildingBlocks para permitir que o sistema
/// funcione sem implementação concreta (desenvolvimento, testes unitários).
///
/// Implementações concretas são fornecidas pelos módulos operacionais
/// (OperationalIntelligence, ChangeGovernance) que injetam dados reais.
/// </summary>
internal sealed class NullDistributedSignalCorrelationService : IDistributedSignalCorrelationService
{
    /// <inheritdoc />
    public Task<DistributedSignalCorrelation> CorrelateSignalsAsync(
        Guid tenantId,
        Guid environmentId,
        string serviceName,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default)
        => Task.FromResult(new DistributedSignalCorrelation
        {
            TenantId = tenantId,
            EnvironmentId = environmentId,
            ServiceName = serviceName,
            From = from,
            To = to,
            IncidentCount = 0,
            ReleaseCount = 0,
            CorrelationScore = 0.0,
            HasPromotionRiskSignals = false,
            Signals = []
        });

    /// <inheritdoc />
    public Task<EnvironmentSignalComparison> CompareEnvironmentsAsync(
        Guid tenantId,
        Guid sourceEnvironmentId,
        Guid targetEnvironmentId,
        string serviceName,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default)
        => Task.FromResult(new EnvironmentSignalComparison
        {
            TenantId = tenantId,
            SourceEnvironmentId = sourceEnvironmentId,
            TargetEnvironmentId = targetEnvironmentId,
            ServiceName = serviceName,
            HasRegression = false,
            DivergenceScore = 0.0,
            DivergenceSignals = []
        });
}
