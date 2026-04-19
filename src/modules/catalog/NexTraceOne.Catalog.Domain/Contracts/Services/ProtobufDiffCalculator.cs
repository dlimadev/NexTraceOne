using NexTraceOne.BuildingBlocks.Core.Enums;
using NexTraceOne.Catalog.Domain.Contracts.ValueObjects;

namespace NexTraceOne.Catalog.Domain.Contracts.Services;

/// <summary>
/// Serviço de domínio responsável pelo cálculo de diff semântico entre especificações Protocol Buffers (.proto).
/// Aplica as regras de breaking change do Protobuf Wire Format:
/// <list type="bullet">
///   <item>Remoção de message ou service → breaking</item>
///   <item>Remoção de RPC de um service → breaking</item>
///   <item>Remoção de field de message → breaking (decoders podem ter comportamento indefinido)</item>
///   <item>Reutilização de número de campo para tipo diferente → breaking (wire format incompatível)</item>
///   <item>Remoção de valor de enum → breaking (decoders podem falhar em valores desconhecidos)</item>
///   <item>Adição de message, service, RPC ou field → aditivo</item>
///   <item>Adição de valor de enum → non-breaking (recomenda-se sempre ter UNKNOWN=0)</item>
/// </list>
/// A extração de dados é delegada ao <see cref="ProtobufSpecParser"/>.
/// </summary>
public static class ProtobufDiffCalculator
{
    /// <summary>
    /// Computa o diff semântico completo entre dois ficheiros .proto.
    /// </summary>
    /// <param name="baseProtoContent">Conteúdo .proto base (versão anterior).</param>
    /// <param name="targetProtoContent">Conteúdo .proto alvo (versão mais recente).</param>
    /// <returns>Resultado com listas de mudanças categorizadas e o nível de mudança calculado.</returns>
    public static OpenApiDiffCalculator.DiffResult ComputeDiff(
        string baseProtoContent, string targetProtoContent)
    {
        var baseMessages = ProtobufSpecParser.ExtractMessages(baseProtoContent);
        var targetMessages = ProtobufSpecParser.ExtractMessages(targetProtoContent);

        var baseServices = ProtobufSpecParser.ExtractServices(baseProtoContent);
        var targetServices = ProtobufSpecParser.ExtractServices(targetProtoContent);

        var baseEnums = ProtobufSpecParser.ExtractEnums(baseProtoContent);
        var targetEnums = ProtobufSpecParser.ExtractEnums(targetProtoContent);

        var breaking = new List<ChangeEntry>();
        var additive = new List<ChangeEntry>();
        var nonBreaking = new List<ChangeEntry>();

        // ── Messages ─────────────────────────────────────────────────────────

        // Messages removidas — breaking
        foreach (var msgName in baseMessages.Keys.Except(targetMessages.Keys, StringComparer.OrdinalIgnoreCase))
        {
            breaking.Add(new ChangeEntry(
                "MessageRemoved", msgName, null,
                $"Message '{msgName}' was removed.", true));
        }

        // Messages adicionadas — aditivo
        foreach (var msgName in targetMessages.Keys.Except(baseMessages.Keys, StringComparer.OrdinalIgnoreCase))
        {
            additive.Add(new ChangeEntry(
                "MessageAdded", msgName, null,
                $"Message '{msgName}' was added.", false));
        }

        // Messages comuns — compara fields e field numbers
        foreach (var msgName in baseMessages.Keys.Intersect(targetMessages.Keys, StringComparer.OrdinalIgnoreCase))
        {
            var baseFields = baseMessages[msgName];
            var targetFields = targetMessages[msgName];

            // Fields removidos — breaking
            foreach (var fieldName in baseFields.Keys.Except(targetFields.Keys, StringComparer.OrdinalIgnoreCase))
            {
                breaking.Add(new ChangeEntry(
                    "FieldRemoved", msgName, fieldName,
                    $"Field '{fieldName}' was removed from message '{msgName}'.", true));
            }

            // Fields adicionados — aditivo
            foreach (var fieldName in targetFields.Keys.Except(baseFields.Keys, StringComparer.OrdinalIgnoreCase))
            {
                additive.Add(new ChangeEntry(
                    "FieldAdded", msgName, fieldName,
                    $"Field '{fieldName}' was added to message '{msgName}'.", false));
            }

            // Field number reutilizado para nome diferente — breaking (wire format incompatível)
            var baseByNumber = baseFields.ToDictionary(kv => kv.Value, kv => kv.Key);
            var targetByNumber = targetFields.ToDictionary(kv => kv.Value, kv => kv.Key);
            foreach (var (number, baseName) in baseByNumber)
            {
                if (targetByNumber.TryGetValue(number, out var targetName)
                    && !string.Equals(baseName, targetName, StringComparison.OrdinalIgnoreCase))
                {
                    breaking.Add(new ChangeEntry(
                        "FieldNumberReused", msgName, targetName,
                        $"Field number {number} in message '{msgName}' was reused: '{baseName}' → '{targetName}'.",
                        true));
                }
            }
        }

        // ── Services ─────────────────────────────────────────────────────────

        // Services removidos — breaking
        foreach (var svcName in baseServices.Keys.Except(targetServices.Keys, StringComparer.OrdinalIgnoreCase))
        {
            breaking.Add(new ChangeEntry(
                "ServiceRemoved", svcName, null,
                $"Service '{svcName}' was removed.", true));
        }

        // Services adicionados — aditivo
        foreach (var svcName in targetServices.Keys.Except(baseServices.Keys, StringComparer.OrdinalIgnoreCase))
        {
            additive.Add(new ChangeEntry(
                "ServiceAdded", svcName, null,
                $"Service '{svcName}' was added.", false));
        }

        // Services comuns — compara RPCs
        foreach (var svcName in baseServices.Keys.Intersect(targetServices.Keys, StringComparer.OrdinalIgnoreCase))
        {
            var baseRpcs = baseServices[svcName];
            var targetRpcs = targetServices[svcName];

            foreach (var rpc in baseRpcs.Except(targetRpcs, StringComparer.OrdinalIgnoreCase))
            {
                breaking.Add(new ChangeEntry(
                    "RpcRemoved", svcName, rpc,
                    $"RPC '{rpc}' was removed from service '{svcName}'.", true));
            }

            foreach (var rpc in targetRpcs.Except(baseRpcs, StringComparer.OrdinalIgnoreCase))
            {
                additive.Add(new ChangeEntry(
                    "RpcAdded", svcName, rpc,
                    $"RPC '{rpc}' was added to service '{svcName}'.", false));
            }
        }

        // ── Enums ─────────────────────────────────────────────────────────────

        // Enums removidos — breaking
        foreach (var enumName in baseEnums.Keys.Except(targetEnums.Keys, StringComparer.OrdinalIgnoreCase))
        {
            breaking.Add(new ChangeEntry(
                "EnumRemoved", enumName, null,
                $"Enum '{enumName}' was removed.", true));
        }

        // Valores de enum removidos — breaking
        foreach (var enumName in baseEnums.Keys.Intersect(targetEnums.Keys, StringComparer.OrdinalIgnoreCase))
        {
            var baseValues = baseEnums[enumName];
            var targetValues = targetEnums[enumName];

            foreach (var value in baseValues.Except(targetValues, StringComparer.OrdinalIgnoreCase))
            {
                breaking.Add(new ChangeEntry(
                    "EnumValueRemoved", enumName, value,
                    $"Enum value '{value}' was removed from '{enumName}'.", true));
            }

            foreach (var value in targetValues.Except(baseValues, StringComparer.OrdinalIgnoreCase))
            {
                nonBreaking.Add(new ChangeEntry(
                    "EnumValueAdded", enumName, value,
                    $"Enum value '{value}' was added to '{enumName}'.", false));
            }
        }

        // Enums adicionados — aditivo
        foreach (var enumName in targetEnums.Keys.Except(baseEnums.Keys, StringComparer.OrdinalIgnoreCase))
        {
            additive.Add(new ChangeEntry(
                "EnumAdded", enumName, null,
                $"Enum '{enumName}' was added.", false));
        }

        var changeLevel = breaking.Count > 0
            ? ChangeLevel.Breaking
            : additive.Count > 0
                ? ChangeLevel.Additive
                : ChangeLevel.NonBreaking;

        return new OpenApiDiffCalculator.DiffResult(breaking, additive, nonBreaking, changeLevel);
    }
}
