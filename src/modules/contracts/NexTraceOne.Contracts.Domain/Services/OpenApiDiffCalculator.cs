using System.Text.Json;
using NexTraceOne.BuildingBlocks.Domain.Enums;
using NexTraceOne.Contracts.Domain.ValueObjects;

#pragma warning disable CA1031 // Captura genérica necessária para resiliência ao processar specs malformadas

namespace NexTraceOne.Contracts.Domain.Services;

/// <summary>
/// Serviço de domínio responsável pelo cálculo de diff semântico entre especificações OpenAPI.
/// Contém lógica pura (sem I/O) para parsing de specs e detecção de mudanças breaking,
/// aditivas e non-breaking entre duas versões de contrato.
/// </summary>
public static class OpenApiDiffCalculator
{
    /// <summary>
    /// Resultado estruturado do diff semântico entre duas specs OpenAPI.
    /// Agrupa as mudanças por categoria e determina o nível de mudança geral.
    /// </summary>
    public sealed record DiffResult(
        IReadOnlyList<ChangeEntry> BreakingChanges,
        IReadOnlyList<ChangeEntry> AdditiveChanges,
        IReadOnlyList<ChangeEntry> NonBreakingChanges,
        ChangeLevel ChangeLevel);

    /// <summary>
    /// Computa o diff semântico completo entre duas especificações OpenAPI.
    /// Detecta caminhos e métodos adicionados/removidos, bem como mudanças em parâmetros,
    /// e classifica o nível geral da mudança (Breaking, Additive ou NonBreaking).
    /// </summary>
    /// <param name="baseSpecContent">Conteúdo JSON da spec base (versão anterior).</param>
    /// <param name="targetSpecContent">Conteúdo JSON da spec alvo (versão mais recente).</param>
    /// <returns>Resultado com listas de mudanças categorizadas e o nível de mudança calculado.</returns>
    public static DiffResult ComputeDiff(string baseSpecContent, string targetSpecContent)
    {
        var basePaths = ExtractPathsAndMethods(baseSpecContent);
        var targetPaths = ExtractPathsAndMethods(targetSpecContent);

        var breaking = new List<ChangeEntry>();
        var additive = new List<ChangeEntry>();
        var nonBreaking = new List<ChangeEntry>();

        // Caminhos removidos — breaking change pois consumidores existentes podem depender deles
        foreach (var path in basePaths.Keys.Except(targetPaths.Keys, StringComparer.OrdinalIgnoreCase))
        {
            breaking.Add(new ChangeEntry("PathRemoved", path, null, $"Path '{path}' was removed.", true));
        }

        // Caminhos adicionados — mudança aditiva, não afeta consumidores existentes
        foreach (var path in targetPaths.Keys.Except(basePaths.Keys, StringComparer.OrdinalIgnoreCase))
        {
            additive.Add(new ChangeEntry("PathAdded", path, null, $"Path '{path}' was added.", false));
        }

        // Caminhos comuns — compara métodos e parâmetros em profundidade
        foreach (var path in basePaths.Keys.Intersect(targetPaths.Keys, StringComparer.OrdinalIgnoreCase))
        {
            var baseMethods = basePaths[path];
            var targetMethods = targetPaths[path];

            foreach (var method in baseMethods.Except(targetMethods, StringComparer.OrdinalIgnoreCase))
                breaking.Add(new ChangeEntry("MethodRemoved", path, method, $"Method '{method}' was removed from '{path}'.", true));

            foreach (var method in targetMethods.Except(baseMethods, StringComparer.OrdinalIgnoreCase))
                additive.Add(new ChangeEntry("MethodAdded", path, method, $"Method '{method}' was added to '{path}'.", false));

            // Compara parâmetros nos métodos que existem em ambas as versões
            foreach (var method in baseMethods.Intersect(targetMethods, StringComparer.OrdinalIgnoreCase))
            {
                ComputeParameterDiff(
                    baseSpecContent,
                    targetSpecContent,
                    path, method,
                    breaking, additive, nonBreaking);
            }
        }

        var changeLevel = breaking.Count > 0
            ? ChangeLevel.Breaking
            : additive.Count > 0
                ? ChangeLevel.Additive
                : ChangeLevel.NonBreaking;

        return new DiffResult(
            breaking.AsReadOnly(),
            additive.AsReadOnly(),
            nonBreaking.AsReadOnly(),
            changeLevel);
    }

    /// <summary>
    /// Extrai mapa de caminhos e seus métodos HTTP a partir de uma especificação OpenAPI em JSON.
    /// Retorna um dicionário cujas chaves são os paths (ex: "/users") e cujos valores
    /// são os conjuntos de métodos HTTP (GET, POST, etc.) definidos em cada path.
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
    /// Compara parâmetros de um endpoint específico entre duas versões de spec.
    /// Detecta parâmetros removidos (breaking), parâmetros obrigatórios adicionados (breaking)
    /// e parâmetros opcionais adicionados (aditivo).
    /// </summary>
    /// <param name="baseSpec">Conteúdo JSON da spec base.</param>
    /// <param name="targetSpec">Conteúdo JSON da spec alvo.</param>
    /// <param name="path">Caminho do endpoint (ex: "/users/{id}").</param>
    /// <param name="method">Método HTTP (ex: "GET").</param>
    /// <param name="breaking">Lista para acumular mudanças breaking detectadas.</param>
    /// <param name="additive">Lista para acumular mudanças aditivas detectadas.</param>
    /// <param name="nonBreaking">Lista para acumular mudanças non-breaking detectadas.</param>
    public static void ComputeParameterDiff(
        string baseSpec,
        string targetSpec,
        string path,
        string method,
        List<ChangeEntry> breaking,
        List<ChangeEntry> additive,
        List<ChangeEntry> nonBreaking)
    {
        try
        {
            var baseParams = ExtractParameters(baseSpec, path, method);
            var targetParams = ExtractParameters(targetSpec, path, method);

            foreach (var (name, _) in baseParams.Where(p => !targetParams.ContainsKey(p.Key)))
                breaking.Add(new ChangeEntry("ParameterRemoved", path, method, $"Parameter '{name}' was removed from '{method} {path}'.", true));

            foreach (var (name, required) in targetParams.Where(p => !baseParams.ContainsKey(p.Key)))
            {
                if (required)
                    breaking.Add(new ChangeEntry("ParameterRequired", path, method, $"Required parameter '{name}' was added to '{method} {path}'.", true));
                else
                    additive.Add(new ChangeEntry("ParameterAdded", path, method, $"Optional parameter '{name}' was added to '{method} {path}'.", false));
            }
        }
        catch (JsonException) { /* Ignora erros de parse de parâmetros para não bloquear o diff global */ }
    }

    /// <summary>
    /// Extrai os parâmetros de um endpoint específico de uma spec OpenAPI.
    /// Retorna dicionário nome → obrigatório (bool) para cada parâmetro encontrado.
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
