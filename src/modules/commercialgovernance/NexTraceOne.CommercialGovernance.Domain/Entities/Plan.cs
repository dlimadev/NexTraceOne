using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.Licensing.Domain.Enums;

namespace NexTraceOne.CommercialCatalog.Domain.Entities;

/// <summary>
/// Aggregate Root que representa um plano comercial da plataforma.
/// Define a combinação de modelo comercial, modelo de deployment, limites de ativação
/// e período de tolerância que será associado a licenças emitidas.
///
/// Decisão de design:
/// - Plan é independente de License: um plano é um template comercial
///   que pode ser referenciado por múltiplas licenças.
/// - PriceTag é meramente informativo (display) — billing real é externo.
/// - TrialDurationDays é opcional: apenas planos com modelo Trial o utilizam.
/// - GracePeriodDays define a tolerância pós-expiração herdada pelas licenças.
/// - Code é único e imutável após criação — serve como identificador de negócio.
/// </summary>
public sealed class Plan : AggregateRoot<PlanId>
{
    private Plan() { }

    /// <summary>Código único do plano (ex: "enterprise-annual"). Imutável após criação.</summary>
    public string Code { get; private set; } = string.Empty;

    /// <summary>Nome comercial do plano.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Descrição detalhada do plano (opcional).</summary>
    public string? Description { get; private set; }

    /// <summary>Modelo comercial associado ao plano (Perpetual, Subscription, etc.).</summary>
    public CommercialModel CommercialModel { get; private set; }

    /// <summary>Modelo de deployment associado ao plano (SaaS, SelfHosted, OnPremise).</summary>
    public DeploymentModel DeploymentModel { get; private set; }

    /// <summary>Indica se o plano está ativo e disponível para novas contratações.</summary>
    public bool IsActive { get; private set; }

    /// <summary>Duração do período trial em dias (null se o plano não suporta trial).</summary>
    public int? TrialDurationDays { get; private set; }

    /// <summary>
    /// Dias de tolerância após expiração.
    /// Licenças emitidas sob este plano herdam este valor como grace period.
    /// </summary>
    public int GracePeriodDays { get; private set; }

    /// <summary>Limite máximo de ativações simultâneas permitidas pelo plano.</summary>
    public int MaxActivations { get; private set; }

    /// <summary>Rótulo de preço para exibição (ex: "R$ 2.500/mês"). Apenas informativo.</summary>
    public string? PriceTag { get; private set; }

    /// <summary>
    /// Factory method para criação de um novo plano comercial.
    /// O plano é criado em estado ativo por padrão.
    /// </summary>
    /// <param name="code">Código único e imutável do plano.</param>
    /// <param name="name">Nome comercial do plano.</param>
    /// <param name="commercialModel">Modelo comercial (Perpetual, Subscription, etc.).</param>
    /// <param name="deploymentModel">Modelo de deployment (SaaS, SelfHosted, OnPremise).</param>
    /// <param name="maxActivations">Limite máximo de ativações simultâneas.</param>
    /// <param name="gracePeriodDays">Dias de tolerância após expiração.</param>
    /// <param name="description">Descrição detalhada do plano (opcional).</param>
    /// <param name="trialDurationDays">Duração do trial em dias (opcional).</param>
    /// <param name="priceTag">Rótulo de preço para exibição (opcional).</param>
    public static Plan Create(
        string code,
        string name,
        CommercialModel commercialModel,
        DeploymentModel deploymentModel,
        int maxActivations,
        int gracePeriodDays,
        string? description = null,
        int? trialDurationDays = null,
        string? priceTag = null)
    {
        Guard.Against.NullOrWhiteSpace(code);
        Guard.Against.NullOrWhiteSpace(name);
        Guard.Against.NegativeOrZero(maxActivations);
        Guard.Against.Negative(gracePeriodDays);

        if (trialDurationDays.HasValue)
        {
            Guard.Against.NegativeOrZero(trialDurationDays.Value);
        }

        return new Plan
        {
            Id = PlanId.New(),
            Code = code,
            Name = name,
            Description = description,
            CommercialModel = commercialModel,
            DeploymentModel = deploymentModel,
            IsActive = true,
            TrialDurationDays = trialDurationDays,
            GracePeriodDays = gracePeriodDays,
            MaxActivations = maxActivations,
            PriceTag = priceTag
        };
    }

    /// <summary>Ativa o plano para disponibilizá-lo para novas contratações.</summary>
    public void Activate() => IsActive = true;

    /// <summary>Desativa o plano, impedindo novas contratações mas sem afetar licenças existentes.</summary>
    public void Deactivate() => IsActive = false;

    /// <summary>
    /// Atualiza informações editáveis do plano.
    /// Código, modelo comercial e modelo de deployment são imutáveis após criação.
    /// </summary>
    /// <param name="name">Novo nome comercial do plano.</param>
    /// <param name="description">Nova descrição (null para limpar).</param>
    /// <param name="priceTag">Novo rótulo de preço (null para limpar).</param>
    public void UpdateDetails(string name, string? description, string? priceTag)
    {
        Guard.Against.NullOrWhiteSpace(name);

        Name = name;
        Description = description;
        PriceTag = priceTag;
    }
}

/// <summary>Identificador fortemente tipado de Plan.</summary>
public sealed record PlanId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static PlanId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static PlanId From(Guid id) => new(id);
}
