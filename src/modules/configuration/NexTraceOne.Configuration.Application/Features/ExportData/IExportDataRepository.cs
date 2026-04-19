namespace NexTraceOne.Configuration.Application.Features.ExportData;

/// <summary>Contrato de repositório para exportação de dados genérica por entidade.</summary>
public interface IExportDataRepository
{
    /// <summary>
    /// Retorna linhas de dados para exportação da entidade especificada.
    /// Cada linha é um dicionário coluna→valor.
    /// </summary>
    Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> GetExportRowsAsync(
        string entity,
        string[]? columns,
        CancellationToken cancellationToken = default);
}
