namespace NexTraceOne.BuildingBlocks.Application.Nql;

/// <summary>
/// Contexto de execução de uma query NQL — carregado a partir da sessão do utilizador.
/// Garante isolamento de tenant, ambiente e persona (row-level security).
/// </summary>
public sealed record NqlExecutionContext(
    string TenantId,
    string? EnvironmentId,
    string Persona,
    string UserId);

/// <summary>
/// Serviço de governança de queries NQL.
/// Valida, controla e executa planos NQL com:
/// — isolamento de tenant e ambiente (RLS)
/// — row cap e timeout configuráveis
/// — controlo de acesso por persona
/// — auditoria de execução
/// Wave V3.2 — Query-driven Widgets &amp; Widget SDK.
/// </summary>
public interface IQueryGovernanceService
{
    /// <summary>
    /// Valida a query NQL: sintaxe + regras de governance (módulo permitido, persona, etc.).
    /// Nunca executa; apenas valida.
    /// </summary>
    NqlValidationResult Validate(string query, NqlExecutionContext context);

    /// <summary>
    /// Executa a query NQL sob governance total.
    /// Retorna <see cref="NqlQueryResult"/> com <c>IsSimulated = true</c> quando
    /// os dados reais do módulo alvo ainda não estão disponíveis (honest gap pattern).
    /// </summary>
    Task<NqlQueryResult> ExecuteAsync(
        NqlPlan plan,
        NqlExecutionContext context,
        CancellationToken ct = default);
}
