using Microsoft.AspNetCore.Authentication;

namespace NexTraceOne.BuildingBlocks.Security.Authentication;

/// <summary>
/// Opções de configuração para autenticação via API key.
/// Contém a lista de chaves configuradas e suas associações a tenants/permissões.
///
/// Em MVP1, as chaves são lidas de appsettings.json (seção Security:ApiKeys).
/// Em produção, migrar para armazenamento criptografado no banco de dados.
/// </summary>
public sealed class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions
{
    /// <summary>Nome do esquema de autenticação registrado.</summary>
    public const string SchemeName = "ApiKey";

    /// <summary>
    /// Lista de API keys configuradas. Cada key vincula um clientId
    /// a um tenant e conjunto de permissões.
    /// </summary>
    public List<ApiKeyConfiguration> ConfiguredKeys { get; set; } = [];
}

/// <summary>
/// Configuração individual de uma API key para integração sistema-a-sistema.
/// Vincula uma chave a um cliente, tenant e permissões específicas.
/// </summary>
public sealed class ApiKeyConfiguration
{
    /// <summary>Valor da API key (segredo compartilhado).</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Identificador único do cliente/sistema externo.</summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>Nome amigável do cliente para logs e auditoria.</summary>
    public string ClientName { get; set; } = string.Empty;

    /// <summary>TenantId ao qual esta key está vinculada.</summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>Permissões concedidas a esta API key.</summary>
    public List<string> Permissions { get; set; } = [];
}
