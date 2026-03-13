using System.Text.Json;

#pragma warning disable CA1031 // Captura genérica necessária para resiliência ao processar specs malformadas

namespace NexTraceOne.Contracts.Domain.Services;

/// <summary>
/// Parser responsável pela extração estruturada de dados de especificações Swagger 2.0 em JSON.
/// Swagger 2.0 difere do OpenAPI 3.x na estrutura geral: utiliza "swagger": "2.0" como
/// identificador de versão, e propriedades como "host", "basePath", "consumes" e "produces"
/// ficam no nível raiz em vez de dentro de cada operação.
/// Em caso de specs malformadas, retorna estruturas vazias para não bloquear o diff.
/// </summary>
public static class SwaggerSpecParser
{
    /// <summary>
    /// Extrai mapa de caminhos e seus métodos HTTP a partir de uma especificação Swagger 2.0 em JSON.
    /// A estrutura de "paths" é análoga ao OpenAPI 3.x — cada path contém métodos HTTP como chaves.
    /// Filtra apenas métodos HTTP válidos (GET, POST, PUT, DELETE, PATCH, HEAD, OPTIONS).
    /// </summary>
    /// <param name="specContent">Conteúdo JSON da spec Swagger 2.0.</param>
    /// <returns>Dicionário path → conjunto de métodos HTTP (case-insensitive).</returns>
    public static Dictionary<string, HashSet<string>> ExtractPathsAndMethods(string specContent)
    {
        var result = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        try
        {
            using var doc = JsonDocument.Parse(specContent);
            if (doc.RootElement.TryGetProperty("paths", out var paths))
            {
                foreach (var path in paths.EnumerateObject())
                {
                    var methods = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var method in path.Value.EnumerateObject())
                    {
                        var m = method.Name.ToUpperInvariant();
                        if (m is "GET" or "POST" or "PUT" or "DELETE" or "PATCH" or "HEAD" or "OPTIONS")
                            methods.Add(m);
                    }
                    result[path.Name] = methods;
                }
            }
        }
        catch (JsonException) { /* Spec inválida — retorna dicionário vazio para não bloquear o diff */ }
        return result;
    }

    /// <summary>
    /// Extrai os parâmetros de um endpoint específico de uma spec Swagger 2.0.
    /// Em Swagger 2.0, parâmetros podem estar definidos tanto no nível da operação
    /// quanto no nível do path. Este parser extrai apenas parâmetros da operação.
    /// Retorna dicionário nome → obrigatório (bool) para cada parâmetro encontrado.
    /// </summary>
    /// <param name="specContent">Conteúdo JSON da spec Swagger 2.0.</param>
    /// <param name="path">Caminho do endpoint (ex: "/users/{id}").</param>
    /// <param name="method">Método HTTP (ex: "get").</param>
    /// <returns>Dicionário nome do parâmetro → indicador de obrigatoriedade (case-insensitive).</returns>
    public static Dictionary<string, bool> ExtractParameters(string specContent, string path, string method)
    {
        var result = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        try
        {
            using var doc = JsonDocument.Parse(specContent);
            if (doc.RootElement.TryGetProperty("paths", out var paths)
                && paths.TryGetProperty(path, out var pathEl)
                && pathEl.TryGetProperty(method.ToLowerInvariant(), out var methodEl)
                && methodEl.TryGetProperty("parameters", out var parameters)
                && parameters.ValueKind == JsonValueKind.Array)
            {
                foreach (var param in parameters.EnumerateArray())
                {
                    var name = param.TryGetProperty("name", out var n) ? n.GetString() ?? string.Empty : string.Empty;
                    var required = param.TryGetProperty("required", out var r) && r.ValueKind == JsonValueKind.True;
                    if (!string.IsNullOrEmpty(name))
                        result[name] = required;
                }
            }
        }
        catch (JsonException) { /* Ignora erros de parse para retornar dicionário vazio */ }
        return result;
    }
}
