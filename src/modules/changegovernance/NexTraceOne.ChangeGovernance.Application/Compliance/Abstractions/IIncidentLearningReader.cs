namespace NexTraceOne.ChangeGovernance.Application.Compliance.Abstractions;

/// <summary>
/// Abstracção bounded-context-safe para leitura de runbooks aprovados por serviço.
/// Permite que o módulo ChangeGovernance consulte runbooks propostos gerados a partir
/// de incidentes (definidos no módulo Knowledge) sem acoplar directamente os dois contextos.
///
/// A implementação produtiva é fornecida pelo módulo Knowledge.
/// A implementação nula (<see cref="NullIncidentLearningReader"/>) é usada como honest-null default.
///
/// Wave T.1 — Post-Incident Learning Report.
/// </summary>
public interface IIncidentLearningReader
{
    /// <summary>
    /// Retorna o conjunto de nomes de serviço que têm pelo menos um runbook aprovado
    /// criado a partir de um incidente no período especificado.
    /// </summary>
    Task<IReadOnlyList<string>> ListServicesWithApprovedRunbookAsync(
        string tenantId,
        DateTimeOffset since,
        DateTimeOffset until,
        CancellationToken ct = default);
}
