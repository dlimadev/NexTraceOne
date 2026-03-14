using Ardalis.GuardClauses;
using MediatR;
using NexTraceOne.BuildingBlocks.Core;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.VersionCommunication.Domain.Errors;

namespace NexTraceOne.VersionCommunication.Domain.Entities;

/// <summary>
/// Entidade que representa o schedule de deprecação de uma versão específica de API.
/// Rastreia o anúncio de deprecação, a data de sunset (fim de suporte) e o enforcement
/// ativo da deprecação. Permite extensão controlada da data de sunset quando necessário
/// para acomodar consumidores que ainda não completaram a migração.
/// </summary>
public sealed class VersionDeprecationSchedule : AuditableEntity<VersionDeprecationScheduleId>
{
    private VersionDeprecationSchedule() { }

    /// <summary>Identificador do ativo de API no módulo Catalog cuja versão será deprecada.</summary>
    public Guid ApiAssetId { get; private set; }

    /// <summary>Versão da API que está sendo deprecada.</summary>
    public string Version { get; private set; } = string.Empty;

    /// <summary>Data/hora UTC em que a deprecação foi oficialmente anunciada.</summary>
    public DateTimeOffset DeprecationAnnouncedAt { get; private set; }

    /// <summary>Data/hora UTC prevista para o fim de suporte (sunset) da versão.</summary>
    public DateTimeOffset SunsetDate { get; private set; }

    /// <summary>Indica se o bloqueio de acesso à versão deprecada está ativo.</summary>
    public bool IsEnforced { get; private set; }

    /// <summary>Número de consumidores afetados pela deprecação desta versão.</summary>
    public int AffectedConsumerCount { get; private set; }

    /// <summary>Observações adicionais sobre o schedule de deprecação.</summary>
    public string? Notes { get; private set; }

    /// <summary>
    /// Cria um novo schedule de deprecação de versão de API.
    /// O schedule é criado em modo não-enforced, permitindo acesso contínuo
    /// à versão deprecada até que o enforcement seja ativado explicitamente.
    /// </summary>
    public static VersionDeprecationSchedule Create(
        Guid apiAssetId,
        string version,
        DateTimeOffset deprecationAnnouncedAt,
        DateTimeOffset sunsetDate,
        int affectedConsumerCount,
        string? notes = null)
    {
        Guard.Against.Default(apiAssetId);
        Guard.Against.NullOrWhiteSpace(version);
        Guard.Against.Negative(affectedConsumerCount);

        return new VersionDeprecationSchedule
        {
            Id = VersionDeprecationScheduleId.New(),
            ApiAssetId = apiAssetId,
            Version = version,
            DeprecationAnnouncedAt = deprecationAnnouncedAt,
            SunsetDate = sunsetDate,
            IsEnforced = false,
            AffectedConsumerCount = affectedConsumerCount,
            Notes = notes
        };
    }

    /// <summary>
    /// Ativa o enforcement da deprecação, bloqueando efetivamente o acesso à versão.
    /// Retorna falha se a deprecação já estiver em modo enforced.
    /// </summary>
    public Result<Unit> Enforce()
    {
        if (IsEnforced)
            return VersionCommunicationErrors.DeprecationAlreadyEnforced();

        IsEnforced = true;
        return Unit.Value;
    }

    /// <summary>
    /// Estende a data de sunset para acomodar consumidores que ainda não migraram.
    /// A nova data deve ser posterior à data de sunset atual.
    /// Retorna falha se a nova data for anterior ou igual à atual.
    /// </summary>
    public Result<Unit> Extend(DateTimeOffset newSunsetDate)
    {
        if (newSunsetDate <= SunsetDate)
            return VersionCommunicationErrors.InvalidSunsetDateExtension(
                SunsetDate.ToString("o"), newSunsetDate.ToString("o"));

        SunsetDate = newSunsetDate;
        return Unit.Value;
    }
}

/// <summary>Identificador fortemente tipado de VersionDeprecationSchedule.</summary>
public sealed record VersionDeprecationScheduleId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static VersionDeprecationScheduleId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static VersionDeprecationScheduleId From(Guid id) => new(id);
}
