namespace NexTraceOne.BuildingBlocks.Application.Correlation;

/// <summary>
/// Implementação nula de IPromotionRiskSignalProvider.
/// Retorna avaliação sem risco — sem dados de módulos operacionais.
/// Implementações concretas são fornecidas pelos módulos operacionais.
/// </summary>
internal sealed class NullPromotionRiskSignalProvider : IPromotionRiskSignalProvider
{
    /// <inheritdoc />
    public Task<PromotionRiskAssessment> AssessPromotionRiskAsync(
        Guid tenantId,
        Guid sourceEnvironmentId,
        Guid targetEnvironmentId,
        string? serviceName,
        DateTimeOffset since,
        CancellationToken cancellationToken = default)
        => Task.FromResult(new PromotionRiskAssessment
        {
            TenantId = tenantId,
            SourceEnvironmentId = sourceEnvironmentId,
            TargetEnvironmentId = targetEnvironmentId,
            ServiceName = serviceName,
            AssessedAt = DateTimeOffset.UtcNow,
            RiskLevel = PromotionRiskLevel.None,
            RiskScore = 0.0,
            Signals = []
        });
}
