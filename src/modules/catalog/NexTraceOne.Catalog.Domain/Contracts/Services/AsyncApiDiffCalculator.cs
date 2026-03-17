using NexTraceOne.BuildingBlocks.Core.Enums;
using NexTraceOne.Catalog.Domain.Contracts.ValueObjects;

namespace NexTraceOne.Catalog.Domain.Contracts.Services;

/// <summary>
/// Serviço de domínio responsável pelo cálculo de diff semântico entre especificações AsyncAPI.
/// Compara canais e operações (publish/subscribe) em vez de paths e métodos HTTP.
/// Também detecta mudanças nos schemas de mensagem de cada operação.
/// A extração de dados é delegada ao <see cref="AsyncApiSpecParser"/>.
/// </summary>
public static class AsyncApiDiffCalculator
{
    /// <summary>
    /// Computa o diff semântico completo entre duas especificações AsyncAPI.
    /// Detecta canais e operações adicionados/removidos, bem como mudanças nos schemas de mensagem,
    /// e classifica o nível geral da mudança (Breaking, Additive ou NonBreaking).
    /// </summary>
    /// <param name="baseSpecContent">Conteúdo JSON da spec base (versão anterior).</param>
    /// <param name="targetSpecContent">Conteúdo JSON da spec alvo (versão mais recente).</param>
    /// <returns>Resultado com listas de mudanças categorizadas e o nível de mudança calculado.</returns>
    public static OpenApiDiffCalculator.DiffResult ComputeDiff(string baseSpecContent, string targetSpecContent)
    {
        var baseChannels = AsyncApiSpecParser.ExtractChannelsAndOperations(baseSpecContent);
        var targetChannels = AsyncApiSpecParser.ExtractChannelsAndOperations(targetSpecContent);

        var breaking = new List<ChangeEntry>();
        var additive = new List<ChangeEntry>();
        var nonBreaking = new List<ChangeEntry>();

        // Canais removidos — breaking change pois consumidores/produtores existentes dependem deles
        foreach (var channel in baseChannels.Keys.Except(targetChannels.Keys, StringComparer.OrdinalIgnoreCase))
        {
            breaking.Add(new ChangeEntry("ChannelRemoved", channel, null, $"Channel '{channel}' was removed.", true));
        }

        // Canais adicionados — mudança aditiva, não afeta consumidores existentes
        foreach (var channel in targetChannels.Keys.Except(baseChannels.Keys, StringComparer.OrdinalIgnoreCase))
        {
            additive.Add(new ChangeEntry("ChannelAdded", channel, null, $"Channel '{channel}' was added.", false));
        }

        // Canais comuns — compara operações e schemas de mensagem em profundidade
        foreach (var channel in baseChannels.Keys.Intersect(targetChannels.Keys, StringComparer.OrdinalIgnoreCase))
        {
            var baseOps = baseChannels[channel];
            var targetOps = targetChannels[channel];

            foreach (var op in baseOps.Except(targetOps, StringComparer.OrdinalIgnoreCase))
                breaking.Add(new ChangeEntry("OperationRemoved", channel, op, $"Operation '{op}' was removed from channel '{channel}'.", true));

            foreach (var op in targetOps.Except(baseOps, StringComparer.OrdinalIgnoreCase))
                additive.Add(new ChangeEntry("OperationAdded", channel, op, $"Operation '{op}' was added to channel '{channel}'.", false));

            // Compara schemas de mensagem nas operações comuns
            foreach (var op in baseOps.Intersect(targetOps, StringComparer.OrdinalIgnoreCase))
            {
                ComputeMessageSchemaDiff(
                    baseSpecContent,
                    targetSpecContent,
                    channel, op,
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
    /// Compara o schema de mensagem de uma operação específica entre duas versões de spec AsyncAPI.
    /// Detecta campos removidos (breaking), campos obrigatórios adicionados (breaking)
    /// e campos opcionais adicionados (aditivo).
    /// </summary>
    private static void ComputeMessageSchemaDiff(
        string baseSpec,
        string targetSpec,
        string channel,
        string operation,
        List<ChangeEntry> breaking,
        List<ChangeEntry> additive,
        List<ChangeEntry> nonBreaking)
    {
        var baseSchema = AsyncApiSpecParser.ExtractMessageSchema(baseSpec, channel, operation);
        var targetSchema = AsyncApiSpecParser.ExtractMessageSchema(targetSpec, channel, operation);

        foreach (var (name, _) in baseSchema.Where(f => !targetSchema.ContainsKey(f.Key)))
            breaking.Add(new ChangeEntry("FieldRemoved", channel, operation, $"Field '{name}' was removed from '{operation}' message on channel '{channel}'.", true));

        foreach (var (name, required) in targetSchema.Where(f => !baseSchema.ContainsKey(f.Key)))
        {
            if (required)
                breaking.Add(new ChangeEntry("FieldRequired", channel, operation, $"Required field '{name}' was added to '{operation}' message on channel '{channel}'.", true));
            else
                additive.Add(new ChangeEntry("FieldAdded", channel, operation, $"Optional field '{name}' was added to '{operation}' message on channel '{channel}'.", false));
        }
    }
}
