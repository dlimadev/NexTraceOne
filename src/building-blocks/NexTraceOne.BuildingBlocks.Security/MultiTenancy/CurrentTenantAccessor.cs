using NexTraceOne.BuildingBlocks.Application.Abstractions;

namespace NexTraceOne.BuildingBlocks.Security.MultiTenancy;

/// <summary>
/// Implementação scoped do tenant atual resolvido por middleware.
/// </summary>
public sealed class CurrentTenantAccessor : ICurrentTenant
{
    private readonly HashSet<string> _capabilities = [];

    /// <inheritdoc />
    public Guid Id { get; private set; }

    /// <inheritdoc />
    public string Slug { get; private set; } = string.Empty;

    /// <inheritdoc />
    public string Name { get; private set; } = string.Empty;

    /// <inheritdoc />
    public bool IsActive { get; private set; }

    /// <summary>Atualiza o tenant ativo da requisição atual.</summary>
    public void Set(Guid id, string slug, string name, bool isActive, IEnumerable<string>? capabilities = null)
    {
        Id = id;
        Slug = slug;
        Name = name;
        IsActive = isActive;

        _capabilities.Clear();
        if (capabilities is null)
        {
            return;
        }

        foreach (var capability in capabilities)
        {
            _capabilities.Add(capability);
        }
    }

    /// <inheritdoc />
    public bool HasCapability(string capability) => _capabilities.Contains(capability);
}
