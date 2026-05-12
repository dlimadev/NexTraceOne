namespace NexTraceOne.BuildingBlocks.Observability.Observability;

/// <summary>
/// Modo de operação da stack de observabilidade.
/// Permite ajustar o uso de recursos em instalações on-premises com capacidade limitada.
/// W7-03: Lightweight Mode.
/// </summary>
public enum ObservabilityMode
{
    /// <summary>
    /// Modo completo: Elasticsearch + ClickHouse, dashboard de observabilidade activado.
    /// Recomendado para servidores com mais de 16 GB de RAM.
    /// </summary>
    Full,

    /// <summary>
    /// Modo reduzido: apenas PostgreSQL como analytics writer; sem Elasticsearch/ClickHouse.
    /// Recomendado para servidores entre 8 GB e 16 GB de RAM.
    /// </summary>
    Lite,

    /// <summary>
    /// Modo mínimo: apenas Serilog file sink; sem dashboard de observabilidade.
    /// Recomendado para servidores com menos de 8 GB de RAM ou instalações edge.
    /// </summary>
    Minimal,
}
