namespace NexTraceOne.BuildingBlocks.Application.Abstractions;

/// <summary>
/// Abstração para acesso ao tenant ativo na requisição atual.
/// Resolvido pelo TenantResolutionMiddleware a partir do JWT, header ou subdomínio.
/// Usado pelo TenantRlsInterceptor para configurar o RLS no PostgreSQL.
/// </summary>
public interface ICurrentTenant
{
    /// <summary>Identificador único do tenant ativo.</summary>
    Guid Id { get; }
    /// <summary>Slug do tenant (ex: "banco-xyz").</summary>
    string Slug { get; }
    /// <summary>Nome de exibição do tenant.</summary>
    string Name { get; }
    /// <summary>Indica se o tenant está ativo e pode realizar operações.</summary>
    bool IsActive { get; }
    /// <summary>Verifica se o tenant possui uma capability de licença específica.</summary>
    bool HasCapability(string capability);
}
