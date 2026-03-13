using NexTraceOne.BuildingBlocks.Domain.Enums;
using NexTraceOne.Contracts.Domain.ValueObjects;

namespace NexTraceOne.Contracts.Domain.Services;

/// <summary>
/// Serviço de domínio responsável pelo cálculo de diff semântico entre especificações Swagger 2.0.
/// Contém lógica pura de comparação análoga ao <see cref="OpenApiDiffCalculator"/>,
/// mas utiliza o <see cref="SwaggerSpecParser"/> para extração de dados.
/// Swagger 2.0 e OpenAPI 3.x compartilham a mesma estrutura de paths/methods,
/// portanto a lógica de diff é equivalente.
/// </summary>
public static class SwaggerDiffCalculator
{
    /// <summary>
    /// Computa o diff semântico completo entre duas especificações Swagger 2.0.
    /// Detecta caminhos e métodos adicionados/removidos, bem como mudanças em parâmetros,
    /// e classifica o nível geral da mudança (Breaking, Additive ou NonBreaking).
    /// </summary>
    /// <param name="baseSpecContent">Conteúdo JSON da spec base (versão anterior).</param>
    /// <param name="targetSpecContent">Conteúdo JSON da spec alvo (versão mais recente).</param>
    /// <returns>Resultado com listas de mudanças categorizadas e o nível de mudança calculado.</returns>
    public static OpenApiDiffCalculator.DiffResult ComputeDiff(string baseSpecContent, string targetSpecContent)
    {
        var basePaths = SwaggerSpecParser.ExtractPathsAndMethods(baseSpecContent);
        var targetPaths = SwaggerSpecParser.ExtractPathsAndMethods(targetSpecContent);

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

        return new OpenApiDiffCalculator.DiffResult(
            breaking.AsReadOnly(),
            additive.AsReadOnly(),
            nonBreaking.AsReadOnly(),
            changeLevel);
    }

    /// <summary>
    /// Compara parâmetros de um endpoint específico entre duas versões de spec Swagger 2.0.
    /// Detecta parâmetros removidos (breaking), parâmetros obrigatórios adicionados (breaking)
    /// e parâmetros opcionais adicionados (aditivo).
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
        var baseParams = SwaggerSpecParser.ExtractParameters(baseSpec, path, method);
        var targetParams = SwaggerSpecParser.ExtractParameters(targetSpec, path, method);

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
}
