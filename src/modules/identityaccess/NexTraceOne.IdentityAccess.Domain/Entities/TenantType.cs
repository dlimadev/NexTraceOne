namespace NexTraceOne.IdentityAccess.Domain.Entities;

/// <summary>
/// Tipo organizacional de um Tenant, definindo seu papel na hierarquia.
///
/// Suporta cenários SaaS (múltiplos clientes independentes), on-premise
/// (matriz com filiais) e híbrido (holding com subsidiárias).
///
/// Regras de hierarquia:
/// - Organization: entidade independente (default, backward-compatible).
/// - Holding: tenant raiz que pode ter subsidiárias.
/// - Subsidiary: filho de um Holding ou Organization, herda políticas do pai.
/// - Department: subdivisão de uma Organization/Subsidiary (profundidade 3).
/// - Partner: parceiro/revendedor em cenário SaaS channel.
/// </summary>
public enum TenantType
{
    /// <summary>Empresa independente (default, backward-compatible).</summary>
    Organization = 0,

    /// <summary>Grupo/Holding — tenant raiz com subsidiárias.</summary>
    Holding = 1,

    /// <summary>Subsidiária/Filial de um Holding ou Organization.</summary>
    Subsidiary = 2,

    /// <summary>Departamento dentro de uma Organization ou Subsidiary.</summary>
    Department = 3,

    /// <summary>Parceiro/Revendedor em cenário SaaS channel.</summary>
    Partner = 4
}
