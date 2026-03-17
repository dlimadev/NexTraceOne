using System.Text.Json;

#pragma warning disable CA1031 // Captura genérica necessária para resiliência ao processar specs malformadas

namespace NexTraceOne.Catalog.Domain.Contracts.Services;

/// <summary>
/// Parser responsável pela extração estruturada de dados de especificações OpenAPI em JSON.
/// Isola toda a lógica de parsing JSON da lógica de comparação/diff,
/// permitindo que o OpenApiDiffCalculator se concentre exclusivamente na detecção de mudanças.
/// Em caso de specs malformadas, retorna estruturas vazias para não bloquear o diff.
/// </summary>
public static class OpenApiSpecParser
{
    /// <summary>
    /// Extrai mapa de caminhos e seus métodos HTTP a partir de uma especificação OpenAPI em JSON.
    /// Retorna um dicionário cujas chaves são os paths (ex: "/users") e cujos valores
    /// são os conjuntos de métodos HTTP (GET, POST, etc.) definidos em cada path.
    /// Filtra apenas métodos HTTP válidos (GET, POST, PUT, DELETE, PATCH, HEAD, OPTIONS).
    /// </summary>
    /// <param name="specContent">Conteúdo JSON da spec OpenAPI.</param>
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
    /// Extrai os parâmetros de um endpoint específico de uma spec OpenAPI.
    /// Retorna dicionário nome → obrigatório (bool) para cada parâmetro encontrado.
    /// Parâmetros sem nome válido são ignorados silenciosamente.
    /// </summary>
    /// <param name="specContent">Conteúdo JSON da spec OpenAPI.</param>
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
