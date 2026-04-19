using Microsoft.Extensions.Logging;

using NexTraceOne.Integrations.Domain;

namespace NexTraceOne.Integrations.Infrastructure.CloudBilling;

/// <summary>
/// Implementação nula de ICloudBillingProvider.
/// Retorna sempre lista vazia enquanto nenhum provider cloud estiver configurado
/// via FinOps:Billing:Provider em appsettings.
/// Registo padrão no DI — substituir por AwsCloudBillingProvider, AzureCloudBillingProvider, etc.
/// </summary>
internal sealed class NullCloudBillingProvider(ILogger<NullCloudBillingProvider> logger) : ICloudBillingProvider
{
    /// <inheritdoc />
    public string ProviderName => "null";

    /// <inheritdoc />
    public bool IsConfigured => false;

    /// <inheritdoc />
    public Task<IReadOnlyList<CloudBillingRecord>> FetchBillingRecordsAsync(
        string period,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug(
            "NullCloudBillingProvider: No cloud billing provider configured — returning empty records for period {Period}",
            period);

        return Task.FromResult<IReadOnlyList<CloudBillingRecord>>(Array.Empty<CloudBillingRecord>());
    }
}
