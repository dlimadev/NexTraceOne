using Ardalis.GuardClauses;

namespace NexTraceOne.BuildingBlocks.Domain.Guards;

/// <summary>
/// Extensões de Guard específicas do domínio NexTraceOne.
/// Complementam as guards genéricas do Ardalis.GuardClauses com
/// validações de negócio recorrentes na plataforma.
/// Uso: Guard.Against.InvalidSemanticVersion(version);
/// </summary>
public static class NexTraceGuards
{
    /// <summary>
    /// Verifica que a string representa uma versão semântica válida (SemVer 2.0).
    /// Lança ArgumentException se inválida, convertida para HTTP 400 pelo middleware.
    /// </summary>
    public static string InvalidSemanticVersion(this IGuardClause _, string version, string paramName = "version")
    {
        // TODO: Implementar validação SemVer 2.0 com regex
        throw new NotImplementedException();
    }

    /// <summary>
    /// Verifica que o ambiente informado é um ambiente governado válido.
    /// Ambientes não governados (ex: Development) são rejeitados.
    /// </summary>
    public static string UngovernedEnvironment(this IGuardClause _, string environment, string paramName = "environment")
    {
        // TODO: Implementar validação de ambiente governado
        throw new NotImplementedException();
    }

    /// <summary>
    /// Verifica que o TenantId não é Guid.Empty.
    /// Requisições sem tenant ativo são rejeitadas.
    /// </summary>
    public static Guid EmptyTenantId(this IGuardClause _, Guid tenantId, string paramName = "tenantId")
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId cannot be empty.", paramName);
        return tenantId;
    }
}
