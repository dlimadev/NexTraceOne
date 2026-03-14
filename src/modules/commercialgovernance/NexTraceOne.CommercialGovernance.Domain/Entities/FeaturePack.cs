using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Domain;
using NexTraceOne.BuildingBlocks.Domain.Primitives;

namespace NexTraceOne.CommercialCatalog.Domain.Entities;

/// <summary>
/// Aggregate Root que representa um pacote de funcionalidades (FeaturePack).
/// Agrupa capabilities relacionadas que podem ser associadas a planos comerciais.
///
/// Decisão de design:
/// - FeaturePack é aggregate root porque controla a consistência dos seus itens.
/// - Items são entidades filhas gerenciadas exclusivamente pelo aggregate.
/// - Code é único e imutável — serve como identificador de negócio (ex: "api-governance-pack").
/// - A associação com Plan é feita via PlanFeaturePackMapping (entidade separada)
///   para permitir relação N:N sem acoplar os aggregates.
/// </summary>
public sealed class FeaturePack : AggregateRoot<FeaturePackId>
{
    private readonly List<FeaturePackItem> _items = [];

    private FeaturePack() { }

    /// <summary>Código único do pacote (ex: "api-governance-pack"). Imutável após criação.</summary>
    public string Code { get; private set; } = string.Empty;

    /// <summary>Nome comercial do pacote de funcionalidades.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Descrição detalhada do pacote (opcional).</summary>
    public string? Description { get; private set; }

    /// <summary>Indica se o pacote está ativo e disponível para associação a planos.</summary>
    public bool IsActive { get; private set; }

    /// <summary>Itens (capabilities) que compõem este pacote. Somente leitura externamente.</summary>
    public IReadOnlyList<FeaturePackItem> Items => _items.AsReadOnly();

    /// <summary>
    /// Factory method para criação de um novo pacote de funcionalidades.
    /// O pacote é criado em estado ativo e sem itens (adicionar via AddItem).
    /// </summary>
    /// <param name="code">Código único e imutável do pacote.</param>
    /// <param name="name">Nome comercial do pacote.</param>
    /// <param name="description">Descrição detalhada do pacote (opcional).</param>
    public static FeaturePack Create(string code, string name, string? description = null)
    {
        Guard.Against.NullOrWhiteSpace(code);
        Guard.Against.NullOrWhiteSpace(name);

        return new FeaturePack
        {
            Id = FeaturePackId.New(),
            Code = code,
            Name = name,
            Description = description,
            IsActive = true
        };
    }

    /// <summary>
    /// Adiciona uma capability ao pacote.
    /// Não permite duplicatas pelo mesmo capabilityCode (case-insensitive).
    /// </summary>
    /// <param name="capabilityCode">Código da capability (ex: "catalog:write").</param>
    /// <param name="capabilityName">Nome amigável da capability.</param>
    /// <param name="defaultLimit">Limite padrão de uso (null = ilimitado).</param>
    public FeaturePackItem AddItem(string capabilityCode, string capabilityName, int? defaultLimit = null)
    {
        Guard.Against.NullOrWhiteSpace(capabilityCode);
        Guard.Against.NullOrWhiteSpace(capabilityName);

        if (defaultLimit.HasValue)
        {
            Guard.Against.NegativeOrZero(defaultLimit.Value);
        }

        var exists = _items.Any(i =>
            string.Equals(i.CapabilityCode, capabilityCode, StringComparison.OrdinalIgnoreCase));

        if (exists)
        {
            throw new InvalidOperationException(
                $"Capability '{capabilityCode}' already exists in this feature pack.");
        }

        var item = FeaturePackItem.Create(Id, capabilityCode, capabilityName, defaultLimit);
        _items.Add(item);
        return item;
    }

    /// <summary>
    /// Remove uma capability do pacote pelo código.
    /// Não lança exceção se o item não existir (idempotente).
    /// </summary>
    /// <param name="capabilityCode">Código da capability a remover.</param>
    public void RemoveItem(string capabilityCode)
    {
        Guard.Against.NullOrWhiteSpace(capabilityCode);

        var item = _items.FirstOrDefault(i =>
            string.Equals(i.CapabilityCode, capabilityCode, StringComparison.OrdinalIgnoreCase));

        if (item is not null)
        {
            _items.Remove(item);
        }
    }

    /// <summary>Ativa o pacote para disponibilizá-lo para associação a planos.</summary>
    public void Activate() => IsActive = true;

    /// <summary>Desativa o pacote, impedindo novas associações mas sem afetar planos existentes.</summary>
    public void Deactivate() => IsActive = false;
}

/// <summary>Identificador fortemente tipado de FeaturePack.</summary>
public sealed record FeaturePackId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static FeaturePackId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static FeaturePackId From(Guid id) => new(id);
}
