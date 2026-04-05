using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.OperationalIntelligence.Domain.Reliability.Entities;

/// <summary>
/// Previsão de capacidade de um recurso de serviço, estimando saturação e risco.
/// </summary>
public sealed class CapacityForecast : Entity<CapacityForecastId>
{
    private CapacityForecast() { }

    public string ServiceId { get; private set; } = string.Empty;
    public string ServiceName { get; private set; } = string.Empty;
    public string Environment { get; private set; } = string.Empty;
    public string ResourceType { get; private set; } = string.Empty;
    public decimal CurrentUtilizationPercent { get; private set; }
    public decimal GrowthRatePercentPerDay { get; private set; }
    public int? EstimatedDaysToSaturation { get; private set; }
    public string? SaturationRisk { get; private set; }
    public string? Notes { get; private set; }
    public DateTimeOffset ComputedAt { get; private set; }

    private static readonly string[] ValidResourceTypes = ["CPU", "Memory", "Disk", "Connections", "Requests"];

    /// <summary>Cria uma nova previsão de capacidade para um serviço e recurso.</summary>
    public static Result<CapacityForecast> Create(
        string serviceId,
        string serviceName,
        string environment,
        string resourceType,
        decimal currentUtilizationPercent,
        decimal growthRatePercentPerDay,
        int? estimatedDaysToSaturation,
        string? notes,
        DateTimeOffset computedAt)
    {
        if (string.IsNullOrWhiteSpace(serviceId) || serviceId.Length > 200)
            return Error.Validation("INVALID_SERVICE_ID", "ServiceId is required.");
        if (string.IsNullOrWhiteSpace(serviceName) || serviceName.Length > 200)
            return Error.Validation("INVALID_SERVICE_NAME", "ServiceName is required.");
        if (string.IsNullOrWhiteSpace(environment) || environment.Length > 100)
            return Error.Validation("INVALID_ENVIRONMENT", "Environment is required.");
        if (!ValidResourceTypes.Contains(resourceType))
            return Error.Validation("INVALID_RESOURCE_TYPE", "Valid resource types: CPU, Memory, Disk, Connections, Requests.");
        if (currentUtilizationPercent < 0m || currentUtilizationPercent > 100m)
            return Error.Validation("INVALID_UTILIZATION", "Utilization must be between 0 and 100.");

        var saturationRisk = ComputeSaturationRisk(estimatedDaysToSaturation);

        return Result<CapacityForecast>.Success(new CapacityForecast
        {
            Id = CapacityForecastId.New(),
            ServiceId = serviceId,
            ServiceName = serviceName,
            Environment = environment,
            ResourceType = resourceType,
            CurrentUtilizationPercent = currentUtilizationPercent,
            GrowthRatePercentPerDay = growthRatePercentPerDay,
            EstimatedDaysToSaturation = estimatedDaysToSaturation,
            SaturationRisk = saturationRisk,
            Notes = notes,
            ComputedAt = computedAt
        });
    }

    /// <summary>Computa o risco de saturação a partir dos dias estimados para saturação.</summary>
    public static string? ComputeSaturationRisk(int? days) =>
        days switch
        {
            null => "Low",
            <= 7 => "Immediate",
            <= 30 => "Near",
            <= 60 => "Moderate",
            _ => "Low"
        };
}

/// <summary>Identificador fortemente tipado de CapacityForecast.</summary>
public sealed record CapacityForecastId(Guid Value) : TypedIdBase(Value)
{
    public static CapacityForecastId New() => new(Guid.NewGuid());
    public static CapacityForecastId From(Guid id) => new(id);
}
