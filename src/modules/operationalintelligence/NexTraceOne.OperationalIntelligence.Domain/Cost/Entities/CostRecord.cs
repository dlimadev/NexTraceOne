using Ardalis.GuardClauses;

using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;
using NexTraceOne.OperationalIntelligence.Domain.Cost.Errors;

namespace NexTraceOne.OperationalIntelligence.Domain.Cost.Entities;

/// <summary>
/// Entidade que representa um registo individual de custo com atribuição completa
/// a serviço, equipa, domínio e ambiente. Cada registo pertence a um batch de importação
/// e contém o custo total para o período indicado.
///
/// Invariantes:
/// - ServiceId e ServiceName são obrigatórios.
/// - TotalCost é sempre não-negativo.
/// - Period e Source são obrigatórios.
///
/// REGRA DDD: A validação de invariantes ocorre no factory method — não há estado inválido.
/// </summary>
public sealed class CostRecord : AuditableEntity<CostRecordId>
{
    private CostRecord() { }

    /// <summary>Identificador do batch de importação ao qual este registo pertence.</summary>
    public Guid BatchId { get; private set; }

    /// <summary>Identificador do serviço ao qual este custo é atribuído.</summary>
    public string ServiceId { get; private set; } = string.Empty;

    /// <summary>Nome legível do serviço.</summary>
    public string ServiceName { get; private set; } = string.Empty;

    /// <summary>Equipa responsável pelo serviço. Null se não atribuído.</summary>
    public string? Team { get; private set; }

    /// <summary>Domínio de negócio ao qual o serviço pertence. Null se não atribuído.</summary>
    public string? Domain { get; private set; }

    /// <summary>Ambiente onde o custo foi apurado (dev, staging, prod). Null se não especificado.</summary>
    public string? Environment { get; private set; }

    /// <summary>Período ao qual o custo se refere (ex: "2026-03").</summary>
    public string Period { get; private set; } = string.Empty;

    /// <summary>Custo total para este serviço no período indicado.</summary>
    public decimal TotalCost { get; private set; }

    /// <summary>Moeda do custo (ex: "USD", "EUR").</summary>
    public string Currency { get; private set; } = "USD";

    /// <summary>Fonte dos dados de custo (ex: "AWS CUR", "Azure Cost Management").</summary>
    public string Source { get; private set; } = string.Empty;

    /// <summary>Data/hora UTC em que o custo foi registado na fonte original.</summary>
    public DateTimeOffset RecordedAt { get; private set; }

    /// <summary>
    /// Identificador da release/mudança à qual este custo está correlacionado.
    /// Null quando o custo ainda não foi associado a uma release específica.
    /// Permite ligar custo a change intelligence para análise de impacto financeiro de deploys.
    /// </summary>
    public Guid? ReleaseId { get; private set; }

    /// <summary>
    /// Factory method para criação de um registo de custo validado.
    /// Garante todas as invariantes da entidade — não existe CostRecord em estado inválido.
    /// </summary>
    public static Result<CostRecord> Create(
        Guid batchId,
        string serviceId,
        string serviceName,
        string? team,
        string? domain,
        string? environment,
        string period,
        decimal totalCost,
        string currency,
        string source,
        DateTimeOffset recordedAt)
    {
        Guard.Against.Default(batchId);
        Guard.Against.NullOrWhiteSpace(serviceId);
        Guard.Against.NullOrWhiteSpace(serviceName);
        Guard.Against.NullOrWhiteSpace(period);
        Guard.Against.NullOrWhiteSpace(currency);
        Guard.Against.NullOrWhiteSpace(source);

        if (totalCost < 0)
            return CostIntelligenceErrors.NegativeCost(totalCost);

        return new CostRecord
        {
            Id = CostRecordId.New(),
            BatchId = batchId,
            ServiceId = serviceId,
            ServiceName = serviceName,
            Team = team,
            Domain = domain,
            Environment = environment,
            Period = period,
            TotalCost = totalCost,
            Currency = currency,
            Source = source,
            RecordedAt = recordedAt
        };
    }

    /// <summary>
    /// Associa este registo de custo a uma release/mudança específica.
    /// Permite correlacionar custo operacional com mudanças de produção para análise de impacto financeiro.
    /// </summary>
    /// <param name="releaseId">Identificador da release no módulo Change Governance.</param>
    public void AssignRelease(Guid releaseId)
    {
        Guard.Against.Default(releaseId);
        ReleaseId = releaseId;
    }

    /// <summary>
    /// Remove a associação a uma release.
    /// Deve ser usado quando a correlação estava incorreta.
    /// </summary>
    public void ClearRelease()
    {
        ReleaseId = null;
    }
}

/// <summary>Identificador fortemente tipado de CostRecord.</summary>
public sealed record CostRecordId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static CostRecordId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static CostRecordId From(Guid id) => new(id);
}
