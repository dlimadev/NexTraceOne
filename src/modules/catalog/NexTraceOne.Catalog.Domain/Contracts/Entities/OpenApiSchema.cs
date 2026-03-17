using System.Text.Json;
using System.Text.RegularExpressions;

using Ardalis.GuardClauses;

namespace NexTraceOne.Catalog.Domain.Contracts.Entities;

/// <summary>
/// Value object que representa metadados extraídos de uma especificação OpenAPI.
/// Não é persistido como tabela separada — é computado a partir do conteúdo da spec.
/// </summary>
public sealed class OpenApiSchema
{
    /// <summary>Regex compilada para detecção de paths YAML no nível correto de indentação.</summary>
    private static readonly Regex YamlPathLineRegex = new(@"^  /[^\s].*:?\s*$", RegexOptions.Compiled, TimeSpan.FromSeconds(1));
    private OpenApiSchema() { }

    /// <summary>Número de paths definidos na spec.</summary>
    public int PathCount { get; private set; }

    /// <summary>Número total de endpoints (paths × métodos HTTP).</summary>
    public int EndpointCount { get; private set; }

    /// <summary>Indica se a spec define esquemas de segurança.</summary>
    public bool HasSecurity { get; private set; }

    /// <summary>Versão declarada na spec (campo info.version).</summary>
    public string Version { get; private set; } = string.Empty;

    /// <summary>
    /// Realiza o parse básico de uma especificação OpenAPI e extrai seus metadados.
    /// Suporta formato JSON; YAML é tratado como texto plano com contagem heurística.
    /// </summary>
    public static OpenApiSchema Parse(string specContent, string format)
    {
        Guard.Against.NullOrWhiteSpace(specContent);
        Guard.Against.NullOrWhiteSpace(format);

        if (string.Equals(format, "json", StringComparison.OrdinalIgnoreCase))
            return ParseJson(specContent);

        return ParseYamlHeuristic(specContent);
    }

    private static OpenApiSchema ParseJson(string specContent)
    {
        try
        {
            using var doc = JsonDocument.Parse(specContent);
            var root = doc.RootElement;

            var version = root.TryGetProperty("info", out var info) && info.TryGetProperty("version", out var ver)
                ? ver.GetString() ?? string.Empty
                : string.Empty;

            var (pathCount, endpointCount) = CountPathsJson(root);
            var hasSecurity = root.TryGetProperty("components", out var components)
                              && components.TryGetProperty("securitySchemes", out var schemes)
                              && schemes.ValueKind == JsonValueKind.Object
                              && schemes.EnumerateObject().Any();

            return new OpenApiSchema
            {
                Version = version,
                PathCount = pathCount,
                EndpointCount = endpointCount,
                HasSecurity = hasSecurity
            };
        }
        catch (JsonException)
        {
            return new OpenApiSchema();
        }
    }

    private static (int pathCount, int endpointCount) CountPathsJson(JsonElement root)
    {
        if (!root.TryGetProperty("paths", out var paths) || paths.ValueKind != JsonValueKind.Object)
            return (0, 0);

        var httpMethods = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { "get", "post", "put", "patch", "delete", "head", "options", "trace" };

        var pathCount = 0;
        var endpointCount = 0;

        foreach (var path in paths.EnumerateObject())
        {
            pathCount++;
            if (path.Value.ValueKind == JsonValueKind.Object)
                endpointCount += path.Value.EnumerateObject().Count(m => httpMethods.Contains(m.Name));
        }

        return (pathCount, endpointCount);
    }

    private static OpenApiSchema ParseYamlHeuristic(string specContent)
    {
        var lines = specContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        var versionLine = lines.FirstOrDefault(l => l.TrimStart().StartsWith("version:", StringComparison.OrdinalIgnoreCase));
        var version = versionLine is not null
            ? versionLine.Split(':', 2).LastOrDefault()?.Trim().Trim('"', '\'') ?? string.Empty
            : string.Empty;

        // Conta apenas linhas que representam paths OpenAPI: indentadas com 2 espaços e começando com /
        var pathCount = lines.Count(l => YamlPathLineRegex.IsMatch(l));
        var httpMethods = new[] { "    get:", "    post:", "    put:", "    patch:", "    delete:", "    head:", "    options:", "    trace:" };
        var endpointCount = lines.Count(l => httpMethods.Any(m => l.StartsWith(m, StringComparison.OrdinalIgnoreCase)));
        var hasSecurity = lines.Any(l => l.TrimStart().StartsWith("securitySchemes:", StringComparison.OrdinalIgnoreCase));

        return new OpenApiSchema
        {
            Version = version,
            PathCount = pathCount,
            EndpointCount = endpointCount,
            HasSecurity = hasSecurity
        };
    }
}
