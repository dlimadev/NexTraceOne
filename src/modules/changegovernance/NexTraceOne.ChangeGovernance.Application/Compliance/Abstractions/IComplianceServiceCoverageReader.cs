namespace NexTraceOne.ChangeGovernance.Application.Compliance.Abstractions;

/// <summary>
/// Abstracção bounded-context-safe para leitura de cobertura de compliance por serviço.
///
/// Permite que <see cref="Features.GetComplianceCoverageMatrixReport.GetComplianceCoverageMatrixReport"/>
/// obtenha, de forma isolada, os estados de avaliação de compliance por serviço e por standard,
/// sem depender directamente de outros módulos ou de implementações de relatórios de compliance individuais.
///
/// A implementação nula (<c>NullComplianceServiceCoverageReader</c>) retorna uma lista vazia,
/// sinalizando que nenhum serviço tem avaliação registada — estado honesto enquanto a integração
/// com avaliações per-serviço não estiver disponível.
///
/// Wave U.1 — Compliance Coverage Matrix Report.
/// </summary>
public interface IComplianceServiceCoverageReader
{
    /// <summary>
    /// Retorna a lista de avaliações de compliance por serviço e standard para o tenant indicado,
    /// dentro do intervalo temporal especificado.
    /// </summary>
    /// <param name="tenantId">Identificador do tenant.</param>
    /// <param name="from">Início do intervalo temporal.</param>
    /// <param name="to">Fim do intervalo temporal.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Lista de entradas de cobertura por serviço e standard.</returns>
    Task<IReadOnlyList<ServiceStandardCoverage>> ListCoverageAsync(
        string tenantId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken);

    /// <summary>
    /// Registo de cobertura de um standard de compliance para um serviço.
    /// </summary>
    /// <param name="ServiceName">Nome do serviço avaliado.</param>
    /// <param name="Standard">Identificador do standard (e.g. "SOC2", "GDPR").</param>
    /// <param name="Status">Estado de conformidade.</param>
    public sealed record ServiceStandardCoverage(
        string ServiceName,
        string Standard,
        ComplianceCoverageStatus Status);
}

/// <summary>
/// Estado de avaliação de conformidade de um serviço para um standard específico.
/// </summary>
public enum ComplianceCoverageStatus
{
    /// <summary>Standard avaliado — todos os controlos em conformidade.</summary>
    Compliant,
    /// <summary>Standard avaliado — alguns controlos em conformidade parcial.</summary>
    PartiallyCompliant,
    /// <summary>Standard avaliado — controlos em não conformidade.</summary>
    NonCompliant,
    /// <summary>Standard não avaliado para este serviço.</summary>
    NotAssessed
}
