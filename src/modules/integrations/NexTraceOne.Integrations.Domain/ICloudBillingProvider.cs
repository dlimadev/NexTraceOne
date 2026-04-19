namespace NexTraceOne.Integrations.Domain;

/// <summary>
/// Contrato para integração com provedores de billing cloud (AWS Cost Explorer, Azure Cost Management, GCP Billing).
/// A implementação padrão é NullCloudBillingProvider.
/// Ativar com featureflag: FinOps:Billing:Provider = "aws" | "azure" | "gcp" | "manual"
/// </summary>
public interface ICloudBillingProvider
{
    /// <summary>Nome do provider (aws, azure, gcp, manual, null).</summary>
    string ProviderName { get; }

    /// <summary>Indica se o provider está configurado e disponível.</summary>
    bool IsConfigured { get; }

    /// <summary>Importa registos de billing para o período indicado (YYYY-MM).</summary>
    Task<IReadOnlyList<CloudBillingRecord>> FetchBillingRecordsAsync(
        string period,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Registo de billing importado de um provider cloud.
/// </summary>
/// <param name="ServiceId">Identificador do serviço no catálogo NexTraceOne.</param>
/// <param name="ServiceName">Nome do serviço ou recurso cloud.</param>
/// <param name="Team">Equipa responsável, se disponível nos tags.</param>
/// <param name="Domain">Domínio de negócio, se disponível nos tags.</param>
/// <param name="Environment">Ambiente (production, staging, etc.), se disponível nos tags.</param>
/// <param name="TotalCost">Custo total no período.</param>
/// <param name="Currency">Código de moeda ISO 4217 (ex: USD, EUR).</param>
/// <param name="Period">Período de billing no formato YYYY-MM.</param>
/// <param name="Source">Nome do provider de origem (aws, azure, gcp, manual).</param>
public sealed record CloudBillingRecord(
    string ServiceId,
    string ServiceName,
    string? Team,
    string? Domain,
    string? Environment,
    decimal TotalCost,
    string Currency,
    string Period,
    string Source);
