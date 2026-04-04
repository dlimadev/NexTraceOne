using System.Xml.Linq;
using System.Text.Json;

#pragma warning disable CA1031 // Captura genérica necessária para resiliência ao processar WSDL malformado

namespace NexTraceOne.Catalog.Domain.Contracts.Services;

/// <summary>
/// Serviço de domínio responsável pela extração de metadados estruturados de WSDL
/// para popular o SoapContractDetail e o SoapDraftMetadata.
/// Extrai: nome do serviço, targetNamespace, binding SOAP (versão 1.1/1.2),
/// endpoint, portType, nome do binding e mapa serializado de operações por portType.
/// </summary>
public static class WsdlMetadataExtractor
{
    private static readonly XNamespace WsdlNs = "http://schemas.xmlsoap.org/wsdl/";
    private static readonly XNamespace Soap11Ns = "http://schemas.xmlsoap.org/wsdl/soap/";
    private static readonly XNamespace Soap12Ns = "http://schemas.xmlsoap.org/wsdl/soap12/";

    /// <summary>Resultado da extração de metadados de um WSDL.</summary>
    public sealed record WsdlMetadata(
        string ServiceName,
        string TargetNamespace,
        string SoapVersion,
        string? EndpointUrl,
        string? PortTypeName,
        string? BindingName,
        string ExtractedOperationsJson);

    /// <summary>
    /// Extrai os metadados estruturados relevantes de uma especificação WSDL.
    /// Retorna valores padrão seguros quando o WSDL está malformado ou incompleto.
    /// </summary>
    /// <param name="wsdlContent">Conteúdo XML da especificação WSDL.</param>
    /// <param name="fallbackServiceName">Nome de fallback quando não encontrado no WSDL.</param>
    public static WsdlMetadata Extract(string wsdlContent, string fallbackServiceName = "SoapService")
    {
        try
        {
            var doc = XDocument.Parse(wsdlContent);
            var root = doc.Root;
            if (root is null)
                return BuildDefault(fallbackServiceName);

            var targetNamespace = root.Attribute("targetNamespace")?.Value
                ?? (root.Attribute("name") is { } nameAttr ? $"http://example.com/{nameAttr.Value}" : "http://example.com/service");

            var serviceName = ExtractServiceName(root, fallbackServiceName);
            var (soapVersion, endpointUrl, bindingName) = ExtractBindingInfo(root);
            var portTypeName = ExtractPortTypeName(root);
            var operationsMap = WsdlSpecParser.ExtractOperations(wsdlContent);
            var operationsJson = SerializeOperationsMap(operationsMap);

            return new WsdlMetadata(
                ServiceName: serviceName,
                TargetNamespace: targetNamespace,
                SoapVersion: soapVersion,
                EndpointUrl: endpointUrl,
                PortTypeName: portTypeName,
                BindingName: bindingName,
                ExtractedOperationsJson: operationsJson);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceWarning(
                "WsdlMetadataExtractor: Failed to extract WSDL metadata — {0}: {1}", ex.GetType().Name, ex.Message);
            return BuildDefault(fallbackServiceName);
        }
    }

    private static WsdlMetadata BuildDefault(string serviceName) =>
        new(serviceName, "http://example.com/service", "1.1", null, null, null, "{}");

    private static string ExtractServiceName(XElement root, string fallback)
    {
        // Tenta atributo "name" na raiz <definitions>
        var nameAttr = root.Attribute("name")?.Value;
        if (!string.IsNullOrWhiteSpace(nameAttr))
            return nameAttr;

        // Tenta elemento <service name="...">
        var serviceElem = root.Descendants(WsdlNs + "service")
            .Concat(root.Descendants("service"))
            .FirstOrDefault();

        return serviceElem?.Attribute("name")?.Value ?? fallback;
    }

    private static (string soapVersion, string? endpointUrl, string? bindingName) ExtractBindingInfo(XElement root)
    {
        // Verifica binding SOAP 1.2 primeiro
        var soap12Binding = root.Descendants(Soap12Ns + "binding").FirstOrDefault();
        if (soap12Binding is not null)
        {
            var address12 = root.Descendants(Soap12Ns + "address").FirstOrDefault();
            var bindingElement12 = root.Descendants(WsdlNs + "binding")
                .Concat(root.Descendants("binding"))
                .FirstOrDefault();
            return ("1.2", address12?.Attribute("location")?.Value, bindingElement12?.Attribute("name")?.Value);
        }

        // SOAP 1.1
        var soap11Address = root.Descendants(Soap11Ns + "address")
            .Concat(root.Descendants("address"))
            .FirstOrDefault();

        var bindingElement = root.Descendants(WsdlNs + "binding")
            .Concat(root.Descendants("binding"))
            .FirstOrDefault();

        return ("1.1", soap11Address?.Attribute("location")?.Value, bindingElement?.Attribute("name")?.Value);
    }

    private static string? ExtractPortTypeName(XElement root)
    {
        var portType = root.Descendants(WsdlNs + "portType")
            .Concat(root.Descendants("portType"))
            .FirstOrDefault();

        return portType?.Attribute("name")?.Value;
    }

    private static string SerializeOperationsMap(Dictionary<string, HashSet<string>> operationsMap)
    {
        if (operationsMap.Count == 0)
            return "{}";

        var serializable = operationsMap.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.OrderBy(op => op).ToList());

        return JsonSerializer.Serialize(serializable);
    }
}
