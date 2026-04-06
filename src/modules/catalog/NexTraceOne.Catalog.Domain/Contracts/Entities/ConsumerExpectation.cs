using Ardalis.GuardClauses;

using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.Catalog.Domain.Contracts.Entities;

/// <summary>
/// Entidade que representa a expectativa de um serviço consumidor sobre um contrato publicado.
/// Permite implementar Consumer-Driven Contract Testing (CDCT): o consumidor declara
/// quais endpoints, campos e comportamentos espera do provider, possibilitando verificação
/// automática de compatibilidade quando novas versões do contrato são publicadas.
/// </summary>
public sealed class ConsumerExpectation : Entity<ConsumerExpectationId>
{
    private ConsumerExpectation() { }

    /// <summary>Identificador do ativo de API (contrato) ao qual esta expectativa se refere.</summary>
    public Guid ApiAssetId { get; private set; }

    /// <summary>Nome do serviço consumidor que regista esta expectativa.</summary>
    public string ConsumerServiceName { get; private set; } = string.Empty;

    /// <summary>Domínio de negócio do serviço consumidor.</summary>
    public string ConsumerDomain { get; private set; } = string.Empty;

    /// <summary>
    /// JSON com o subconjunto esperado de endpoints, schemas e comportamentos.
    /// Formato livre — pode conter lista de paths, operationIds, campos de resposta,
    /// status codes esperados, etc.
    /// </summary>
    public string ExpectedSubsetJson { get; private set; } = "{}";

    /// <summary>Notas adicionais sobre a expectativa (ex: cenários específicos, restrições).</summary>
    public string Notes { get; private set; } = string.Empty;

    /// <summary>Data e hora de registo desta expectativa.</summary>
    public DateTimeOffset RegisteredAt { get; private set; }

    /// <summary>Indica se esta expectativa ainda está activa.</summary>
    public bool IsActive { get; private set; } = true;

    /// <summary>Cria uma nova expectativa de consumidor para um contrato.</summary>
    public static ConsumerExpectation Create(
        Guid apiAssetId,
        string consumerServiceName,
        string consumerDomain,
        string expectedSubsetJson,
        string? notes,
        DateTimeOffset registeredAt)
    {
        Guard.Against.Default(apiAssetId);
        Guard.Against.NullOrWhiteSpace(consumerServiceName);
        Guard.Against.NullOrWhiteSpace(consumerDomain);
        Guard.Against.NullOrWhiteSpace(expectedSubsetJson);

        return new ConsumerExpectation
        {
            Id = ConsumerExpectationId.New(),
            ApiAssetId = apiAssetId,
            ConsumerServiceName = consumerServiceName,
            ConsumerDomain = consumerDomain,
            ExpectedSubsetJson = expectedSubsetJson,
            Notes = notes ?? string.Empty,
            RegisteredAt = registeredAt,
            IsActive = true
        };
    }

    /// <summary>Actualiza a expectativa registada.</summary>
    public void Update(
        string expectedSubsetJson,
        string? notes)
    {
        Guard.Against.NullOrWhiteSpace(expectedSubsetJson);
        ExpectedSubsetJson = expectedSubsetJson;
        Notes = notes ?? string.Empty;
    }

    /// <summary>Desactiva esta expectativa.</summary>
    public void Deactivate() => IsActive = false;
}

/// <summary>Identificador fortemente tipado de ConsumerExpectation.</summary>
public sealed record ConsumerExpectationId(Guid Value) : TypedIdBase(Value)
{
    public static ConsumerExpectationId New() => new(Guid.NewGuid());
    public static ConsumerExpectationId From(Guid value) => new(value);
}
