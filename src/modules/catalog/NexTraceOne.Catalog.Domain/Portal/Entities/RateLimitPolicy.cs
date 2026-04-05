using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;
using NexTraceOne.Catalog.Domain.Portal.Errors;

namespace NexTraceOne.Catalog.Domain.Portal.Entities;

/// <summary>
/// Política de rate limiting por contrato de API.
/// Define limites de requisições por janela de tempo para controlo de consumo.
/// </summary>
public sealed class RateLimitPolicy : Entity<RateLimitPolicyId>
{
    private RateLimitPolicy() { }

    /// <summary>API a que esta política se aplica.</summary>
    public Guid ApiAssetId { get; private set; }
    public int RequestsPerMinute { get; private set; }
    public int RequestsPerHour { get; private set; }
    public int RequestsPerDay { get; private set; }
    public int BurstLimit { get; private set; }
    public bool IsEnabled { get; private set; }
    public string? Notes { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public string CreatedBy { get; private set; } = string.Empty;

    public static Result<RateLimitPolicy> Create(
        Guid apiAssetId,
        int requestsPerMinute,
        int requestsPerHour,
        int requestsPerDay,
        int burstLimit,
        string? notes,
        string createdBy,
        DateTimeOffset now)
    {
        if (apiAssetId == Guid.Empty)
            return Error.Validation("RATE_LIMIT_INVALID_API", "API asset ID must not be empty.");
        if (requestsPerMinute <= 0 || requestsPerMinute > 10000)
            return DeveloperPortalErrors.InvalidRateLimitValues();
        if (requestsPerHour <= requestsPerMinute)
            return DeveloperPortalErrors.InvalidRateLimitValues();
        if (requestsPerDay <= requestsPerHour)
            return DeveloperPortalErrors.InvalidRateLimitValues();
        if (burstLimit < 1)
            return DeveloperPortalErrors.InvalidRateLimitValues();
        if (string.IsNullOrWhiteSpace(createdBy))
            return Error.Validation("RATE_LIMIT_INVALID_CREATOR", "CreatedBy must not be empty.");

        return Result<RateLimitPolicy>.Success(new RateLimitPolicy
        {
            Id = RateLimitPolicyId.New(),
            ApiAssetId = apiAssetId,
            RequestsPerMinute = requestsPerMinute,
            RequestsPerHour = requestsPerHour,
            RequestsPerDay = requestsPerDay,
            BurstLimit = burstLimit,
            IsEnabled = true,
            Notes = notes,
            CreatedBy = createdBy,
            CreatedAt = now,
            UpdatedAt = now
        });
    }

    /// <summary>Atualiza configuração da política de rate limit.</summary>
    public void Update(
        int requestsPerMinute,
        int requestsPerHour,
        int requestsPerDay,
        int burstLimit,
        bool isEnabled,
        string? notes,
        DateTimeOffset updatedAt)
    {
        RequestsPerMinute = requestsPerMinute;
        RequestsPerHour = requestsPerHour;
        RequestsPerDay = requestsPerDay;
        BurstLimit = burstLimit;
        IsEnabled = isEnabled;
        Notes = notes;
        UpdatedAt = updatedAt;
    }
}

/// <summary>Identificador fortemente tipado de RateLimitPolicy.</summary>
public sealed record RateLimitPolicyId(Guid Value) : TypedIdBase(Value)
{
    public static RateLimitPolicyId New() => new(Guid.NewGuid());
    public static RateLimitPolicyId From(Guid id) => new(id);
}
