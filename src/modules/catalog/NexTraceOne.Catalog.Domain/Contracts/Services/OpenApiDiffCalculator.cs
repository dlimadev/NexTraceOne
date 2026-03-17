using System.Text.Json;

using NexTraceOne.BuildingBlocks.Core.Enums;
using NexTraceOne.Catalog.Domain.Contracts.ValueObjects;

namespace NexTraceOne.Catalog.Domain.Contracts.Services;

/// <summary>
/// Serviço de domínio responsável pelo cálculo de diff semântico entre especificações OpenAPI.
/// Contém lógica pura de comparação para detecção de mudanças breaking, aditivas e non-breaking.
/// A extração de dados das specs é delegada ao OpenApiSpecParser, separando
/// a responsabilidade de parsing JSON da lógica de análise de diferenças.
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
        var basePaths = OpenApiSpecParser.ExtractPathsAndMethods(baseSpecContent);
        var targetPaths = OpenApiSpecParser.ExtractPathsAndMethods(targetSpecContent);

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
    /// Compara parâmetros de um endpoint específico entre duas versões de spec.
    /// Detecta parâmetros removidos (breaking), parâmetros obrigatórios adicionados (breaking)
    /// e parâmetros opcionais adicionados (aditivo).
    /// Utiliza o OpenApiSpecParser para extração dos dados de cada spec.
    /// </summary>
    private static void ComputeParameterDiff(
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
            var baseParams = OpenApiSpecParser.ExtractParameters(baseSpec, path, method);
            var targetParams = OpenApiSpecParser.ExtractParameters(targetSpec, path, method);

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
}
