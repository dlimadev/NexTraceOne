using System.Collections.Frozen;
using System.Globalization;

namespace NexTraceOne.BuildingBlocks.Security.Authorization;

/// <summary>
/// Mapeia códigos de permissão no formato plano ("módulo:recurso:ação" ou "módulo:ação")
/// para o triplo (Module, Page, Action) usado pela entidade ModuleAccessPolicy.
///
/// Convenção de mapeamento:
/// - 3 partes "prefix:resource:action" → Module=MappedModule(prefix), Page=PascalCase(resource), Action=PascalCase(action)
/// - 2 partes "prefix:action" → Module=MappedModule(prefix), Page="*", Action=PascalCase(action)
///
/// Os prefixos de módulo são mapeados para os nomes do <c>ModuleAccessPolicyCatalog</c>
/// via tabela estática. Prefixos desconhecidos são convertidos para PascalCase por convenção.
/// </summary>
public static class PermissionCodeMapper
{
    /// <summary>
    /// Resultado do mapeamento de um permission code para o triplo Module/Page/Action.
    /// </summary>
    /// <param name="Module">Nome do módulo da plataforma (ex.: "AI", "Identity").</param>
    /// <param name="Page">Página ou sub-área (ex.: "Runtime", "Users"). "*" quando não especificado.</param>
    /// <param name="Action">Ação granular (ex.: "Read", "Write", "Approve").</param>
    public sealed record ModulePageAction(string Module, string Page, string Action);

    /// <summary>
    /// Mapeamento estático de prefixos de permission code para nomes de módulo
    /// conforme definidos no ModuleAccessPolicyCatalog.
    /// </summary>
    private static readonly FrozenDictionary<string, string> ModulePrefixMap =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["identity"] = "Identity",
            ["catalog"] = "Catalog",
            ["contracts"] = "Contracts",
            ["developer-portal"] = "DeveloperPortal",
            ["change-intelligence"] = "ChangeIntelligence",
            ["workflow"] = "Workflow",
            ["operations"] = "Operations",
            ["governance"] = "Governance",
            ["promotion"] = "Promotion",
            ["audit"] = "Audit",
            ["ai"] = "AI",
            ["integrations"] = "Integrations",
            ["platform"] = "Platform",
            ["configuration"] = "Configuration",
            ["notifications"] = "Notifications",
            ["env"] = "Environments",
            ["analytics"] = "Governance",
            ["rulesets"] = "Governance"
        }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Converte um permission code plano no triplo (Module, Page, Action).
    /// </summary>
    /// <param name="permissionCode">Código de permissão (ex.: "ai:runtime:write", "contracts:read").</param>
    /// <returns>O triplo mapeado, ou <c>null</c> se o código for inválido.</returns>
    public static ModulePageAction? Map(string permissionCode)
    {
        if (string.IsNullOrWhiteSpace(permissionCode))
            return null;

        var parts = permissionCode.Split(':');

        return parts.Length switch
        {
            3 => new ModulePageAction(
                ResolveModule(parts[0]),
                ToPascalCase(parts[1]),
                ToPascalCase(parts[2])),
            2 => new ModulePageAction(
                ResolveModule(parts[0]),
                "*",
                ToPascalCase(parts[1])),
            _ => null
        };
    }

    /// <summary>
    /// Verifica se um permission code pode ser mapeado para Module/Page/Action.
    /// </summary>
    public static bool CanMap(string permissionCode)
        => Map(permissionCode) is not null;

    /// <summary>
    /// Retorna todos os prefixos de módulo conhecidos.
    /// </summary>
    public static IReadOnlyCollection<string> GetKnownModulePrefixes()
        => ModulePrefixMap.Keys;

    /// <summary>
    /// Resolve o nome do módulo a partir do prefixo do permission code.
    /// </summary>
    private static string ResolveModule(string prefix)
        => ModulePrefixMap.TryGetValue(prefix, out var module)
            ? module
            : ToPascalCase(prefix);

    /// <summary>
    /// Converte uma string kebab-case ou lowercase para PascalCase.
    /// Ex.: "runtime" → "Runtime", "jit-access" → "JitAccess", "break-glass" → "BreakGlass"
    /// </summary>
    private static string ToPascalCase(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return value;

        if (value == "*")
            return "*";

        if (!value.Contains('-'))
        {
            return string.Create(value.Length, value, static (span, state) =>
            {
                state.AsSpan().CopyTo(span);
                span[0] = char.ToUpper(span[0], CultureInfo.InvariantCulture);
            });
        }

        var parts = value.Split('-');
        var totalLength = parts.Sum(p => p.Length);

        return string.Create(totalLength, parts, static (span, segments) =>
        {
            var pos = 0;
            foreach (var segment in segments)
            {
                if (segment.Length == 0)
                    continue;

                segment.AsSpan().CopyTo(span[pos..]);
                span[pos] = char.ToUpper(span[pos], CultureInfo.InvariantCulture);
                pos += segment.Length;
            }
        });
    }
}
