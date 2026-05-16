namespace NexTraceOne.AIKnowledge.Domain.Governance.ValueObjects;

/// <summary>
/// Value object para derivação consistente de slugs a partir de nomes.
/// Garante que a normalização é igual em todas as entidades de governança de IA.
/// </summary>
public static class SlugHelper
{
    /// <summary>
    /// Deriva um slug a partir de um nome, substituindo espaços e dois-pontos por hífens.
    /// Se um slug explícito for fornecido, retorna-o diretamente.
    /// </summary>
    public static string Derive(string name, string? explicitSlug = null)
    {
        if (!string.IsNullOrWhiteSpace(explicitSlug))
            return explicitSlug;

        return name.ToLowerInvariant()
            .Replace(' ', '-')
            .Replace(':', '-');
    }
}
