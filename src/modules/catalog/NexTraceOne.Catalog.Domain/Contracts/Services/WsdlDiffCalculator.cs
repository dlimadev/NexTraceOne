using NexTraceOne.BuildingBlocks.Core.Enums;
using NexTraceOne.Catalog.Domain.Contracts.ValueObjects;

namespace NexTraceOne.Catalog.Domain.Contracts.Services;

/// <summary>
/// Serviço de domínio responsável pelo cálculo de diff semântico entre especificações WSDL.
/// Compara portTypes e operações SOAP em vez de paths e métodos HTTP.
/// Também detecta mudanças nas message parts (parâmetros) de cada operação.
/// A extração de dados é delegada ao <see cref="WsdlSpecParser"/>.
/// </summary>
public static class WsdlDiffCalculator
{
    /// <summary>
    /// Computa o diff semântico completo entre duas especificações WSDL.
    /// Detecta portTypes e operações adicionados/removidos, bem como mudanças nas message parts,
    /// e classifica o nível geral da mudança (Breaking, Additive ou NonBreaking).
    /// </summary>
    /// <param name="baseSpecContent">Conteúdo XML da spec base (versão anterior).</param>
    /// <param name="targetSpecContent">Conteúdo XML da spec alvo (versão mais recente).</param>
    /// <returns>Resultado com listas de mudanças categorizadas e o nível de mudança calculado.</returns>
    public static OpenApiDiffCalculator.DiffResult ComputeDiff(string baseSpecContent, string targetSpecContent)
    {
        var baseOps = WsdlSpecParser.ExtractOperations(baseSpecContent);
        var targetOps = WsdlSpecParser.ExtractOperations(targetSpecContent);

        var breaking = new List<ChangeEntry>();
        var additive = new List<ChangeEntry>();
        var nonBreaking = new List<ChangeEntry>();

        // PortTypes removidos — breaking change pois consumidores existentes dependem deles
        foreach (var portType in baseOps.Keys.Except(targetOps.Keys, StringComparer.OrdinalIgnoreCase))
        {
            breaking.Add(new ChangeEntry("PortTypeRemoved", portType, null, $"PortType '{portType}' was removed.", true));
        }

        // PortTypes adicionados — mudança aditiva, não afeta consumidores existentes
        foreach (var portType in targetOps.Keys.Except(baseOps.Keys, StringComparer.OrdinalIgnoreCase))
        {
            additive.Add(new ChangeEntry("PortTypeAdded", portType, null, $"PortType '{portType}' was added.", false));
        }

        // PortTypes comuns — compara operações e message parts em profundidade
        foreach (var portType in baseOps.Keys.Intersect(targetOps.Keys, StringComparer.OrdinalIgnoreCase))
        {
            var baseOperations = baseOps[portType];
            var targetOperations = targetOps[portType];

            foreach (var op in baseOperations.Except(targetOperations, StringComparer.OrdinalIgnoreCase))
                breaking.Add(new ChangeEntry("OperationRemoved", portType, op, $"Operation '{op}' was removed from portType '{portType}'.", true));

            foreach (var op in targetOperations.Except(baseOperations, StringComparer.OrdinalIgnoreCase))
                additive.Add(new ChangeEntry("OperationAdded", portType, op, $"Operation '{op}' was added to portType '{portType}'.", false));

            // Compara message parts nas operações comuns
            foreach (var op in baseOperations.Intersect(targetOperations, StringComparer.OrdinalIgnoreCase))
            {
                ComputeMessagePartsDiff(
                    baseSpecContent,
                    targetSpecContent,
                    op,
                    portType,
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
    /// Compara as message parts de uma operação específica entre duas versões de spec WSDL.
    /// Detecta parts removidas (breaking) e parts adicionadas (aditivo).
    /// Em WSDL, todas as parts de mensagem são consideradas obrigatórias por padrão.
    /// </summary>
    private static void ComputeMessagePartsDiff(
        string baseSpec,
        string targetSpec,
        string operationName,
        string portType,
        List<ChangeEntry> breaking,
        List<ChangeEntry> additive,
        List<ChangeEntry> nonBreaking)
    {
        var baseParts = WsdlSpecParser.ExtractMessageParts(baseSpec, operationName);
        var targetParts = WsdlSpecParser.ExtractMessageParts(targetSpec, operationName);

        foreach (var (name, _) in baseParts.Where(p => !targetParts.ContainsKey(p.Key)))
            breaking.Add(new ChangeEntry("MessagePartRemoved", portType, operationName, $"Message part '{name}' was removed from operation '{operationName}' in portType '{portType}'.", true));

        // Em WSDL, parts adicionadas são sempre obrigatórias — portanto são breaking
        foreach (var (name, _) in targetParts.Where(p => !baseParts.ContainsKey(p.Key)))
            breaking.Add(new ChangeEntry("MessagePartAdded", portType, operationName, $"Message part '{name}' was added to operation '{operationName}' in portType '{portType}'.", true));
    }
}
