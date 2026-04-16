using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;
using NexTraceOne.Catalog.Domain.Graph.Enums;
using NexTraceOne.Catalog.Domain.Graph.Errors;

namespace NexTraceOne.Catalog.Domain.Graph.Entities;

/// <summary>
/// Interface exposta por um serviço — representa um ponto de contrato concreto
/// com tipo, protocolo, estado e política de autenticação.
/// É o elemento que vincula um contrato a um serviço com contexto de runtime.
/// </summary>
public sealed class ServiceInterface : AuditableEntity<ServiceInterfaceId>
{
    private ServiceInterface() { }

    // ── Referência ao serviço ─────────────────────────────────────────

    /// <summary>Identificador do ServiceAsset dono desta interface.</summary>
    public Guid ServiceAssetId { get; private set; }

    // ── Identidade ────────────────────────────────────────────────────

    /// <summary>Nome descritivo da interface.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Descrição do propósito da interface.</summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>Tipo técnico da interface (REST, SOAP, Kafka, gRPC, etc.).</summary>
    public InterfaceType InterfaceType { get; private set; }

    /// <summary>Estado actual do ciclo de vida da interface.</summary>
    public InterfaceStatus Status { get; private set; } = InterfaceStatus.Active;

    /// <summary>Âmbito de exposição da interface (Internal, Partner, Public).</summary>
    public ExposureType ExposureScope { get; private set; } = ExposureType.Internal;

    // ── Detalhes por protocolo ────────────────────────────────────────

    /// <summary>Base path da interface REST ou SOAP (ex: /api/v1/orders).</summary>
    public string BasePath { get; private set; } = string.Empty;

    /// <summary>Nome do tópico Kafka associado.</summary>
    public string TopicName { get; private set; } = string.Empty;

    /// <summary>Namespace WSDL para interfaces SOAP.</summary>
    public string WsdlNamespace { get; private set; } = string.Empty;

    /// <summary>Nome do serviço gRPC (proto service name).</summary>
    public string GrpcServiceName { get; private set; } = string.Empty;

    /// <summary>Expressão Cron para interfaces do tipo Background ou ScheduledJob.</summary>
    public string ScheduleCron { get; private set; } = string.Empty;

    // ── Contexto operacional ──────────────────────────────────────────

    /// <summary>Slug ou identificador do ambiente primário desta interface.</summary>
    public string EnvironmentId { get; private set; } = string.Empty;

    /// <summary>Objetivo de SLO para esta interface específica.</summary>
    public string SloTarget { get; private set; } = string.Empty;

    /// <summary>Indica se esta interface exige um contrato vinculado.</summary>
    public bool RequiresContract { get; private set; }

    // ── Segurança ─────────────────────────────────────────────────────

    /// <summary>Esquema de autenticação aplicado a esta interface.</summary>
    public InterfaceAuthScheme AuthScheme { get; private set; } = InterfaceAuthScheme.None;

    /// <summary>Política de rate limiting aplicada (texto livre ou referência).</summary>
    public string RateLimitPolicy { get; private set; } = string.Empty;

    // ── Documentação ──────────────────────────────────────────────────

    /// <summary>URL da documentação desta interface.</summary>
    public string DocumentationUrl { get; private set; } = string.Empty;

    // ── Deprecação ────────────────────────────────────────────────────

    /// <summary>Data de início da deprecação da interface.</summary>
    public DateTimeOffset? DeprecationDate { get; private set; }

    /// <summary>Data de sunset — quando a interface será retirada.</summary>
    public DateTimeOffset? SunsetDate { get; private set; }

    /// <summary>Aviso de deprecação com contexto e alternativas.</summary>
    public string? DeprecationNotice { get; private set; }

    // ── Concorrência ──────────────────────────────────────────────────

    /// <summary>
    /// Token de concorrência otimista (PostgreSQL xmin).
    /// Utilizado pelo EF Core para detetar conflitos de escrita concorrente.
    /// </summary>
    public uint RowVersion { get; set; }

    // ── Computed ──────────────────────────────────────────────────────

    /// <summary>Indica se a interface está em estado de deprecação ou sunset.</summary>
    public bool IsDeprecated => Status is InterfaceStatus.Deprecated or InterfaceStatus.Sunset;

    // ── Factory ───────────────────────────────────────────────────────

    /// <summary>Cria uma nova interface de serviço com os campos obrigatórios.</summary>
    public static ServiceInterface Create(Guid serviceAssetId, string name, InterfaceType type)
        => new()
        {
            Id = ServiceInterfaceId.New(),
            ServiceAssetId = serviceAssetId,
            Name = Guard.Against.NullOrWhiteSpace(name),
            InterfaceType = type,
            Status = InterfaceStatus.Active
        };

    // ── Mutações controladas ──────────────────────────────────────────

    /// <summary>Atualiza os campos editáveis da interface.</summary>
    public void UpdateDetails(
        string description,
        ExposureType exposureScope,
        string basePath,
        string topicName,
        string wsdlNamespace,
        string grpcServiceName,
        string scheduleCron,
        string environmentId,
        string sloTarget,
        bool requiresContract,
        InterfaceAuthScheme authScheme,
        string rateLimitPolicy,
        string documentationUrl)
    {
        Description = description ?? string.Empty;
        ExposureScope = exposureScope;
        BasePath = basePath ?? string.Empty;
        TopicName = topicName ?? string.Empty;
        WsdlNamespace = wsdlNamespace ?? string.Empty;
        GrpcServiceName = grpcServiceName ?? string.Empty;
        ScheduleCron = scheduleCron ?? string.Empty;
        EnvironmentId = environmentId ?? string.Empty;
        SloTarget = sloTarget ?? string.Empty;
        RequiresContract = requiresContract;
        AuthScheme = authScheme;
        RateLimitPolicy = rateLimitPolicy ?? string.Empty;
        DocumentationUrl = documentationUrl ?? string.Empty;
    }

    /// <summary>Marca a interface como depreciada com aviso e datas.</summary>
    public void Deprecate(DateTimeOffset? deprecationDate, DateTimeOffset? sunsetDate, string? notice)
    {
        Status = InterfaceStatus.Deprecated;
        DeprecationDate = deprecationDate;
        SunsetDate = sunsetDate;
        DeprecationNotice = notice;
    }

    /// <summary>Retira definitivamente a interface — transição terminal.</summary>
    public void Retire()
    {
        Status = InterfaceStatus.Retired;
    }
}

/// <summary>Identificador fortemente tipado de ServiceInterface.</summary>
public sealed record ServiceInterfaceId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static ServiceInterfaceId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static ServiceInterfaceId From(Guid id) => new(id);
}
