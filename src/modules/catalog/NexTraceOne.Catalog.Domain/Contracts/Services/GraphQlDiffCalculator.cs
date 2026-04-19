using NexTraceOne.BuildingBlocks.Core.Enums;
using NexTraceOne.Catalog.Domain.Contracts.ValueObjects;

namespace NexTraceOne.Catalog.Domain.Contracts.Services;

/// <summary>
/// Serviço de domínio responsável pelo cálculo de diff semântico entre especificações GraphQL SDL.
/// Compara tipos, campos e enums. Os root types (Query, Mutation, Subscription) são tratados
/// de forma especial pois alterações nos seus campos impactam diretamente os consumidores da API.
/// A extração de dados é delegada ao <see cref="GraphQlSpecParser"/>.
/// </summary>
public static class GraphQlDiffCalculator
{
    /// <summary>
    /// Computa o diff semântico completo entre dois schemas GraphQL SDL.
    /// Regras de breaking change GraphQL:
    /// <list type="bullet">
    ///   <item>Remoção de tipo existente → breaking</item>
    ///   <item>Remoção de campo de root type (Query/Mutation/Subscription) → breaking</item>
    ///   <item>Remoção de campo em tipo não-root → breaking (clientes podem depender)</item>
    ///   <item>Remoção de valor de enum → breaking</item>
    ///   <item>Adição de tipo → aditivo</item>
    ///   <item>Adição de campo → aditivo (optional por convenção GraphQL)</item>
    ///   <item>Adição de valor de enum → non-breaking</item>
    /// </list>
    /// </summary>
    /// <param name="baseSchemaContent">Conteúdo SDL base (versão anterior).</param>
    /// <param name="targetSchemaContent">Conteúdo SDL alvo (versão mais recente).</param>
    /// <returns>Resultado com listas de mudanças categorizadas e o nível de mudança calculado.</returns>
    public static OpenApiDiffCalculator.DiffResult ComputeDiff(
        string baseSchemaContent, string targetSchemaContent)
    {
        var baseTypes = GraphQlSpecParser.ExtractTypesAndFields(baseSchemaContent);
        var targetTypes = GraphQlSpecParser.ExtractTypesAndFields(targetSchemaContent);

        var baseEnums = GraphQlSpecParser.ExtractEnums(baseSchemaContent);
        var targetEnums = GraphQlSpecParser.ExtractEnums(targetSchemaContent);

        var breaking = new List<ChangeEntry>();
        var additive = new List<ChangeEntry>();
        var nonBreaking = new List<ChangeEntry>();

        // Tipos removidos — breaking (qualquer cliente que use o tipo fica quebrado)
        foreach (var typeName in baseTypes.Keys.Except(targetTypes.Keys, StringComparer.OrdinalIgnoreCase))
        {
            breaking.Add(new ChangeEntry(
                "TypeRemoved", typeName, null,
                $"Type '{typeName}' was removed.", true));
        }

        // Tipos adicionados — aditivo
        foreach (var typeName in targetTypes.Keys.Except(baseTypes.Keys, StringComparer.OrdinalIgnoreCase))
        {
            additive.Add(new ChangeEntry(
                "TypeAdded", typeName, null,
                $"Type '{typeName}' was added.", false));
        }

        // Tipos comuns — compara campos
        foreach (var typeName in baseTypes.Keys.Intersect(targetTypes.Keys, StringComparer.OrdinalIgnoreCase))
        {
            var baseFields = baseTypes[typeName];
            var targetFields = targetTypes[typeName];
            var isRoot = GraphQlSpecParser.IsRootType(typeName);

            foreach (var field in baseFields.Except(targetFields, StringComparer.OrdinalIgnoreCase))
            {
                breaking.Add(new ChangeEntry(
                    isRoot ? "RootFieldRemoved" : "FieldRemoved",
                    typeName, field,
                    $"Field '{field}' was removed from type '{typeName}'.", true));
            }

            foreach (var field in targetFields.Except(baseFields, StringComparer.OrdinalIgnoreCase))
            {
                additive.Add(new ChangeEntry(
                    isRoot ? "RootFieldAdded" : "FieldAdded",
                    typeName, field,
                    $"Field '{field}' was added to type '{typeName}'.", false));
            }
        }

        // Enums removidos — breaking
        foreach (var enumName in baseEnums.Keys.Except(targetEnums.Keys, StringComparer.OrdinalIgnoreCase))
        {
            breaking.Add(new ChangeEntry(
                "EnumRemoved", enumName, null,
                $"Enum '{enumName}' was removed.", true));
        }

        // Valores de enum removidos — breaking (switch/exhaustive match quebra)
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

            // Valores adicionados — non-breaking (leitores não precisam de conhecer novos valores
            // mas podem causar problemas em code paths exhaustivos — considera-se non-breaking
            // porque é questão de implementação do cliente, não de incompatibilidade de schema)
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
