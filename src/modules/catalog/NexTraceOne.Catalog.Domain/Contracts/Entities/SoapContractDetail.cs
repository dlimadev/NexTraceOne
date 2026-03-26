using Ardalis.GuardClauses;

using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.Catalog.Domain.Contracts.Entities;

/// <summary>
/// Entidade específica para metadados de contratos SOAP/WSDL publicados.
/// Captura informações estruturais extraídas do WSDL que não existem em contratos REST:
/// portTypes, operações, binding (SOAP 1.1/1.2), namespace alvo e endpoint de serviço.
/// Vinculada a uma ContractVersion com Protocol = Wsdl.
/// </summary>
public sealed class SoapContractDetail : AuditableEntity<SoapContractDetailId>
{
    private SoapContractDetail() { }

    /// <summary>Identificador da versão de contrato WSDL/SOAP à qual este detalhe pertence.</summary>
    public ContractVersionId ContractVersionId { get; private set; } = null!;

    /// <summary>Nome do serviço SOAP (atributo "name" em &lt;definitions&gt; ou &lt;service&gt; do WSDL).</summary>
    public string ServiceName { get; private set; } = string.Empty;

    /// <summary>Namespace XML alvo do serviço SOAP (targetNamespace).</summary>
    public string TargetNamespace { get; private set; } = string.Empty;

    /// <summary>Versão do protocolo SOAP: "1.1" ou "1.2".</summary>
    public string SoapVersion { get; private set; } = "1.1";

    /// <summary>URL do endpoint de serviço SOAP (location no &lt;soap:address&gt;).</summary>
    public string? EndpointUrl { get; private set; }

    /// <summary>URL ou caminho de origem do ficheiro WSDL (importação externa).</summary>
    public string? WsdlSourceUrl { get; private set; }

    /// <summary>Nome do portType principal extraído do WSDL.</summary>
    public string? PortTypeName { get; private set; }

    /// <summary>Nome do binding SOAP extraído do WSDL.</summary>
    public string? BindingName { get; private set; }

    /// <summary>
    /// JSON serializado das operações extraídas do WSDL.
    /// Formato: { "PortTypeName": ["Op1", "Op2", ...], ... }
    /// Permite consultas e exibição das operações sem re-parsear o WSDL.
    /// </summary>
    public string ExtractedOperationsJson { get; private set; } = "{}";

    /// <summary>
    /// Cria um novo SoapContractDetail a partir de dados extraídos do WSDL.
    /// </summary>
    public static Result<SoapContractDetail> Create(
        ContractVersionId contractVersionId,
        string serviceName,
        string targetNamespace,
        string soapVersion,
        string extractedOperationsJson,
        string? endpointUrl = null,
        string? wsdlSourceUrl = null,
        string? portTypeName = null,
        string? bindingName = null)
    {
        Guard.Against.Null(contractVersionId);
        Guard.Against.NullOrWhiteSpace(serviceName);
        Guard.Against.NullOrWhiteSpace(targetNamespace);

        if (soapVersion is not "1.1" and not "1.2")
            return Error.Validation("Contracts.Soap.InvalidSoapVersion", "SOAP version must be '1.1' or '1.2'.");

        return new SoapContractDetail
        {
            Id = SoapContractDetailId.New(),
            ContractVersionId = contractVersionId,
            ServiceName = serviceName,
            TargetNamespace = targetNamespace,
            SoapVersion = soapVersion,
            EndpointUrl = endpointUrl,
            WsdlSourceUrl = wsdlSourceUrl,
            PortTypeName = portTypeName,
            BindingName = bindingName,
            ExtractedOperationsJson = extractedOperationsJson
        };
    }

    /// <summary>Atualiza a URL do endpoint de serviço SOAP.</summary>
    public void UpdateEndpoint(string endpointUrl)
    {
        Guard.Against.NullOrWhiteSpace(endpointUrl);
        EndpointUrl = endpointUrl;
    }

    /// <summary>Atualiza os metadados SOAP extraídos após re-parsing do WSDL.</summary>
    public void UpdateFromParsing(
        string serviceName,
        string targetNamespace,
        string soapVersion,
        string extractedOperationsJson,
        string? portTypeName = null,
        string? bindingName = null)
    {
        Guard.Against.NullOrWhiteSpace(serviceName);
        Guard.Against.NullOrWhiteSpace(targetNamespace);

        ServiceName = serviceName;
        TargetNamespace = targetNamespace;
        SoapVersion = soapVersion;
        ExtractedOperationsJson = extractedOperationsJson;
        PortTypeName = portTypeName;
        BindingName = bindingName;
    }
}

/// <summary>Identificador fortemente tipado de SoapContractDetail.</summary>
public sealed record SoapContractDetailId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static SoapContractDetailId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static SoapContractDetailId From(Guid id) => new(id);
}
