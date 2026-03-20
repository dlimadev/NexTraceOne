namespace NexTraceOne.BuildingBlocks.Application.Integrations;

/// <summary>
/// Descreve um binding de integração específico para um tenant e ambiente.
///
/// Um binding representa a configuração de como um serviço externo (Kafka, HTTP, ITSM, etc.)
/// deve ser acessado no contexto de um tenant + ambiente específico.
///
/// Exemplos de uso:
/// - Kafka de QA do TenantABC → bootstrap=kafka-qa.tenantabc.internal
/// - Webhook de PROD do TenantXYZ → url=https://prod.webhook.tenantxyz.com
/// - ITSM Sandbox do TenantABC em UAT → url=https://sandbox.itsm.tenantabc.com
///
/// REGRA DE SEGURANÇA:
/// Nunca usar binding de PROD em ambiente não-produtivo e vice-versa,
/// a menos que a política do tenant/ambiente explicitamente permita (sandbox controlado).
/// O IIntegrationContextResolver valida esta regra antes de retornar o binding.
/// </summary>
public sealed record IntegrationBindingDescriptor
{
    /// <summary>Identificador único do binding.</summary>
    public required Guid BindingId { get; init; }

    /// <summary>Tenant ao qual este binding pertence.</summary>
    public required Guid TenantId { get; init; }

    /// <summary>Ambiente ao qual este binding pertence. Null = binding global do tenant.</summary>
    public Guid? EnvironmentId { get; init; }

    /// <summary>
    /// Tipo de integração. Exemplos: "kafka", "http", "webhook", "itsm", "idp", "k8s".
    /// Usados como chave para lookup pelo IIntegrationContextResolver.
    /// </summary>
    public required string IntegrationType { get; init; }

    /// <summary>Nome único do binding dentro do escopo tenant+ambiente+tipo.</summary>
    public required string BindingName { get; init; }

    /// <summary>Endpoint primário (URL, bootstrap server, connection string).</summary>
    public required string Endpoint { get; init; }

    /// <summary>
    /// Indica se este binding aponta para um ambiente de produção real.
    /// Usado pelo resolver para validar que operações críticas não acessem sandbox
    /// e que operações de teste não acessem produção inadvertidamente.
    /// </summary>
    public bool IsProductionBinding { get; init; }

    /// <summary>Indica se o binding está ativo e pode ser usado.</summary>
    public bool IsActive { get; init; } = true;

    /// <summary>Metadata adicional específica do tipo de integração (JSON).</summary>
    public string? MetadataJson { get; init; }

    /// <summary>Data/hora UTC de criação do binding.</summary>
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}
