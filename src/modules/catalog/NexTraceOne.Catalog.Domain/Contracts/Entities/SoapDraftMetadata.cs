using Ardalis.GuardClauses;

using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.Catalog.Domain.Contracts.Entities;

/// <summary>
/// Entidade que armazena metadados SOAP/WSDL específicos para um ContractDraft em edição.
/// Permite que o Contract Studio mantenha informações de serviço SOAP (nome, namespace,
/// endpoint, binding, operações) desacopladas do conteúdo SpecContent genérico do draft.
/// Vinculada a um ContractDraft com Protocol = Wsdl e ContractType = Soap.
/// </summary>
public sealed class SoapDraftMetadata : Entity<SoapDraftMetadataId>
{
    private SoapDraftMetadata() { }

    /// <summary>Identificador do draft de contrato SOAP ao qual este metadado pertence.</summary>
    public ContractDraftId ContractDraftId { get; private set; } = null!;

    /// <summary>Nome do serviço SOAP definido no draft.</summary>
    public string ServiceName { get; private set; } = string.Empty;

    /// <summary>Namespace XML alvo do serviço SOAP (targetNamespace).</summary>
    public string TargetNamespace { get; private set; } = string.Empty;

    /// <summary>Versão do protocolo SOAP: "1.1" ou "1.2".</summary>
    public string SoapVersion { get; private set; } = "1.1";

    /// <summary>URL do endpoint de serviço SOAP.</summary>
    public string? EndpointUrl { get; private set; }

    /// <summary>Nome do portType definido no draft.</summary>
    public string? PortTypeName { get; private set; }

    /// <summary>Nome do binding SOAP definido no draft.</summary>
    public string? BindingName { get; private set; }

    /// <summary>
    /// JSON serializado das operações definidas no draft pelo editor visual SOAP.
    /// Formato: { "PortTypeName": ["Op1", "Op2", ...], ... }
    /// </summary>
    public string OperationsJson { get; private set; } = "{}";

    /// <summary>
    /// Cria novos metadados SOAP para um draft de contrato.
    /// </summary>
    public static SoapDraftMetadata Create(
        ContractDraftId contractDraftId,
        string serviceName,
        string targetNamespace,
        string soapVersion = "1.1",
        string? endpointUrl = null,
        string? portTypeName = null,
        string? bindingName = null,
        string operationsJson = "{}")
    {
        Guard.Against.Null(contractDraftId);
        Guard.Against.NullOrWhiteSpace(serviceName);
        Guard.Against.NullOrWhiteSpace(targetNamespace);

        return new SoapDraftMetadata
        {
            Id = SoapDraftMetadataId.New(),
            ContractDraftId = contractDraftId,
            ServiceName = serviceName,
            TargetNamespace = targetNamespace,
            SoapVersion = soapVersion is "1.1" or "1.2" ? soapVersion : "1.1",
            EndpointUrl = endpointUrl,
            PortTypeName = portTypeName,
            BindingName = bindingName,
            OperationsJson = operationsJson
        };
    }

    /// <summary>Atualiza os metadados SOAP do draft quando o utilizador edita no Studio visual.</summary>
    public void Update(
        string serviceName,
        string targetNamespace,
        string soapVersion,
        string operationsJson,
        string? endpointUrl = null,
        string? portTypeName = null,
        string? bindingName = null)
    {
        Guard.Against.NullOrWhiteSpace(serviceName);
        Guard.Against.NullOrWhiteSpace(targetNamespace);

        ServiceName = serviceName;
        TargetNamespace = targetNamespace;
        SoapVersion = soapVersion is "1.1" or "1.2" ? soapVersion : "1.1";
        EndpointUrl = endpointUrl;
        PortTypeName = portTypeName;
        BindingName = bindingName;
        OperationsJson = operationsJson;
    }
}

/// <summary>Identificador fortemente tipado de SoapDraftMetadata.</summary>
public sealed record SoapDraftMetadataId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static SoapDraftMetadataId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static SoapDraftMetadataId From(Guid id) => new(id);
}
