using System.Xml.Linq;

#pragma warning disable CA1031 // Captura genérica necessária para resiliência ao processar specs malformadas

namespace NexTraceOne.Catalog.Domain.Contracts.Services;

/// <summary>
/// Parser responsável pela extração estruturada de dados de especificações WSDL (XML).
/// WSDL define contratos SOAP/Web Services: operações ficam em elementos "portType",
/// cada operação tem mensagens "input" e "output", e os "types" definem schemas de dados.
/// Suporta tanto WSDL com namespace (wsdl:) quanto sem namespace (plain XML).
/// Em caso de XML malformado, retorna estruturas vazias para não bloquear o diff.
/// </summary>
public static class WsdlSpecParser
{
    /// <summary>Namespace XML padrão do WSDL 1.1.</summary>
    private static readonly XNamespace WsdlNs = "http://schemas.xmlsoap.org/wsdl/";

    /// <summary>
    /// Extrai mapa de serviços/portTypes e suas operações a partir de uma especificação WSDL.
    /// Cada portType agrupa operações que definem a interface do serviço.
    /// Retorna um dicionário cujas chaves são os nomes dos portTypes
    /// e cujos valores são os conjuntos de operações definidas.
    /// Tenta parsing com namespace WSDL primeiro; se não encontrar, tenta sem namespace.
    /// </summary>
    /// <param name="specContent">Conteúdo XML da spec WSDL.</param>
    /// <returns>Dicionário portType → conjunto de nomes de operação (case-insensitive).</returns>
    public static Dictionary<string, HashSet<string>> ExtractOperations(string specContent)
    {
        var result = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        try
        {
            var doc = XDocument.Parse(specContent);
            var root = doc.Root;
            if (root is null)
                return result;

            // Tenta parsing com namespace WSDL padrão
            var portTypes = root.Descendants(WsdlNs + "portType").ToList();

            // Fallback para WSDL sem namespace (plain XML)
            if (portTypes.Count == 0)
                portTypes = root.Descendants("portType").ToList();

            foreach (var portType in portTypes)
            {
                var portTypeName = portType.Attribute("name")?.Value ?? string.Empty;
                if (string.IsNullOrEmpty(portTypeName))
                    continue;

                var operations = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                // Busca operações dentro do portType — com e sem namespace
                var opElements = portType.Elements(WsdlNs + "operation")
                    .Concat(portType.Elements("operation"));

                foreach (var op in opElements)
                {
                    var opName = op.Attribute("name")?.Value;
                    if (!string.IsNullOrEmpty(opName))
                        operations.Add(opName);
                }

                result[portTypeName] = operations;
            }
        }
        catch (Exception) { /* XML inválido — retorna dicionário vazio para não bloquear o diff */ }
        return result;
    }

    /// <summary>
    /// Extrai as partes (parts) de uma mensagem associada a uma operação WSDL.
    /// Navega pelas operações de cada portType procurando a operação com o nome especificado,
    /// identifica a mensagem de input, e extrai os elementos "part" da mensagem correspondente.
    /// Partes com atributo "element" ou "type" são consideradas obrigatórias por padrão no WSDL.
    /// </summary>
    /// <param name="specContent">Conteúdo XML da spec WSDL.</param>
    /// <param name="operationName">Nome da operação (ex: "GetUser").</param>
    /// <returns>Dicionário nome da parte → obrigatório (true por padrão em WSDL).</returns>
    public static Dictionary<string, bool> ExtractMessageParts(string specContent, string operationName)
    {
        var result = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        try
        {
            var doc = XDocument.Parse(specContent);
            var root = doc.Root;
            if (root is null)
                return result;

            // Localiza a mensagem de input da operação — procura nos portTypes
            var messageName = FindInputMessageName(root, operationName);
            if (string.IsNullOrEmpty(messageName))
                return result;

            // Remove prefixo de namespace do nome da mensagem (ex: "tns:GetUserRequest" → "GetUserRequest")
            var localMessageName = messageName.Contains(':')
                ? messageName[(messageName.IndexOf(':') + 1)..]
                : messageName;

            // Busca a definição da mensagem e extrai suas parts
            var messages = root.Descendants(WsdlNs + "message")
                .Concat(root.Descendants("message"));

            foreach (var message in messages)
            {
                var name = message.Attribute("name")?.Value;
                if (!string.Equals(name, localMessageName, StringComparison.OrdinalIgnoreCase))
                    continue;

                var parts = message.Elements(WsdlNs + "part")
                    .Concat(message.Elements("part"));

                foreach (var part in parts)
                {
                    var partName = part.Attribute("name")?.Value;
                    if (!string.IsNullOrEmpty(partName))
                        result[partName] = true; // Em WSDL, parts de mensagem são obrigatórias por padrão
                }
                break;
            }
        }
        catch (Exception) { /* XML inválido — retorna dicionário vazio para não bloquear o diff */ }
        return result;
    }

    /// <summary>
    /// Localiza o nome da mensagem de input associada a uma operação em qualquer portType do WSDL.
    /// Percorre todos os portTypes e suas operações até encontrar o match pelo nome da operação.
    /// </summary>
    private static string? FindInputMessageName(XElement root, string operationName)
    {
        var portTypes = root.Descendants(WsdlNs + "portType")
            .Concat(root.Descendants("portType"));

        foreach (var portType in portTypes)
        {
            var operations = portType.Elements(WsdlNs + "operation")
                .Concat(portType.Elements("operation"));

            foreach (var op in operations)
            {
                var opName = op.Attribute("name")?.Value;
                if (!string.Equals(opName, operationName, StringComparison.OrdinalIgnoreCase))
                    continue;

                var input = op.Element(WsdlNs + "input") ?? op.Element("input");
                return input?.Attribute("message")?.Value;
            }
        }

        return null;
    }
}
