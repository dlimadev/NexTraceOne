namespace NexTraceOne.Catalog.Application.Contracts.Abstractions;

/// <summary>
/// Abstração de leitura de resultados de testes de contrato para relatórios de cobertura.
///
/// Fornece dados agregados por serviço e par produtor-consumidor, cobrindo:
/// contagens de testes por estado (Passed/Failed/Pending), pares produtor-consumidor
/// testados e APIs cobertas.
/// Desacopla o handler de contract test coverage das implementações concretas de repositório.
///
/// Wave AE.1 — GetContractTestCoverageReport.
/// </summary>
public interface IContractTestReader
{
    /// <summary>
    /// Lista entradas de cobertura de testes de contrato por serviço do tenant.
    /// </summary>
    Task<IReadOnlyList<ContractTestEntry>> ListByTenantAsync(
        string tenantId,
        int lookbackDays,
        CancellationToken ct);
}

/// <summary>
/// Estado de um teste de contrato.
/// Wave AE.1.
/// </summary>
public enum ContractTestStatus
{
    /// <summary>Teste executado e passou.</summary>
    Passed,
    /// <summary>Teste executado e falhou.</summary>
    Failed,
    /// <summary>Teste registado mas ainda não executado.</summary>
    Pending
}

/// <summary>
/// Entrada de cobertura de testes de contrato para um serviço/par produtor-consumidor.
/// Wave AE.1.
/// </summary>
public sealed record ContractTestEntry(
    /// <summary>Identificador do ativo de API testado.</summary>
    string ApiAssetId,
    /// <summary>Nome do serviço produtor.</summary>
    string ProducerServiceName,
    /// <summary>Nome do serviço consumidor.</summary>
    string ConsumerServiceName,
    /// <summary>Nome do tier do serviço produtor: Critical, Standard ou Experimental.</summary>
    string ProducerServiceTier,
    /// <summary>Estado do teste mais recente.</summary>
    ContractTestStatus LatestStatus,
    /// <summary>Total de execuções no período.</summary>
    int TotalExecutions,
    /// <summary>Total de execuções passadas no período.</summary>
    int PassedCount,
    /// <summary>Total de execuções falhadas no período.</summary>
    int FailedCount,
    /// <summary>Data e hora da última execução.</summary>
    DateTimeOffset LastTestedAt);
