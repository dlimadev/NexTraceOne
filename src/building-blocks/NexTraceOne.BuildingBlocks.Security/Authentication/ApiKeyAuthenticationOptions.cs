using Microsoft.AspNetCore.Authentication;

namespace NexTraceOne.BuildingBlocks.Security.Authentication;

/// <summary>
/// Opções de configuração para autenticação via API key.
/// Contém a lista de chaves configuradas e suas associações a tenants/permissões.
///
/// Suporta dois modos de armazenamento do segredo:
///   1. Hash SHA-256 (recomendado): Key contém o hash hex do segredo. KeyIsHashed = true.
///   2. Texto plano (MVP1/legacy): Key contém o segredo em claro. KeyIsHashed = false (default).
///
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
    /// <summary>
    /// Valor da API key ou hash SHA-256 hex do segredo (conforme KeyIsHashed).
    /// Quando KeyIsHashed = true, deve conter o hash hex lowercase de 64 caracteres.
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Indica se o valor em Key é um hash SHA-256 hex do segredo real.
    /// Quando true, o handler calcula SHA-256 do header recebido e compara com este hash.
    /// Quando false (default, legacy), compara o valor em claro.
    /// </summary>
    public bool KeyIsHashed { get; set; }

    /// <summary>Identificador único do cliente/sistema externo.</summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>Nome amigável do cliente para logs e auditoria.</summary>
    public string ClientName { get; set; } = string.Empty;

    /// <summary>TenantId ao qual esta key está vinculada.</summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>Permissões concedidas a esta API key.</summary>
    public List<string> Permissions { get; set; } = [];
}
