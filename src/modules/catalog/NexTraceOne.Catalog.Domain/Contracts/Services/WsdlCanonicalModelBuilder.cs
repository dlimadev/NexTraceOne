using System.Xml.Linq;

using NexTraceOne.Catalog.Domain.Contracts.Enums;
using NexTraceOne.Catalog.Domain.Contracts.ValueObjects;

#pragma warning disable CA1031

namespace NexTraceOne.Catalog.Domain.Contracts.Services;

/// <summary>
/// Constrói o modelo canônico a partir de especificações WSDL/SOAP (XML).
/// </summary>
internal static class WsdlCanonicalModelBuilder
{
    /// <summary>
    /// Constrói modelo canônico a partir de especificação WSDL em XML.
    /// </summary>
    internal static ContractCanonicalModel Build(string specContent)
    {
        try
        {
            var xdoc = XDocument.Parse(specContent);
            var root = xdoc.Root;
            if (root is null) return CanonicalModelHelpers.EmptyModel(ContractProtocol.Wsdl);

            var wsdlNs = XNamespace.Get("http://schemas.xmlsoap.org/wsdl/");
            var title = root.Attribute("name")?.Value ?? "Untitled WSDL Service";

            var operations = ExtractWsdlOperations(root, wsdlNs);
            var schemas = ExtractWsdlSchemas(root);

            return new ContractCanonicalModel(
                ContractProtocol.Wsdl, title, "1.1", null,
                operations, schemas, [], [], [],
                operations.Count, schemas.Count,
                false, false,
                operations.Any(o => !string.IsNullOrWhiteSpace(o.Description)));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceWarning(
                "CanonicalModelBuilder: Failed to parse WSDL spec — {0}: {1}", ex.GetType().Name, ex.Message);
            return CanonicalModelHelpers.EmptyModel(ContractProtocol.Wsdl);
        }
    }

    private static List<ContractOperation> ExtractWsdlOperations(XElement root, XNamespace wsdlNs)
    {
        var ops = new List<ContractOperation>();
        var portTypes = root.Descendants(wsdlNs + "portType").Concat(root.Descendants("portType"));

        foreach (var pt in portTypes)
        {
            var ptName = pt.Attribute("name")?.Value ?? "";
            var operations = pt.Elements(wsdlNs + "operation").Concat(pt.Elements("operation"));

            foreach (var op in operations)
            {
                var name = op.Attribute("name")?.Value ?? "";
                var doc = op.Element(wsdlNs + "documentation")?.Value ?? op.Element("documentation")?.Value;
                ops.Add(new ContractOperation($"{ptName}.{name}", name, doc, "SOAP", ptName, [], [], false));
            }
        }
        return ops;
    }

    private static List<ContractSchemaElement> ExtractWsdlSchemas(XElement root)
    {
        var schemas = new List<ContractSchemaElement>();
        var xsdNs = XNamespace.Get("http://www.w3.org/2001/XMLSchema");
        var elements = root.Descendants(xsdNs + "element").Concat(root.Descendants("element"));

        // Limite defensivo para evitar consumo excessivo de memória em WSDLs com schemas muito extensos
        foreach (var el in elements.Take(50))
        {
            var name = el.Attribute("name")?.Value;
            if (!string.IsNullOrEmpty(name))
            {
                var type = el.Attribute("type")?.Value ?? "complex";
                schemas.Add(new ContractSchemaElement(name, type, false));
            }
        }
        return schemas;
    }
}
