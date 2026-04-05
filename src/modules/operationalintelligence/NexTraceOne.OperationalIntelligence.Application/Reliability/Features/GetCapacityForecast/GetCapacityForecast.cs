using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Reliability.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Reliability.Entities;

namespace NexTraceOne.OperationalIntelligence.Application.Reliability.Features.GetCapacityForecast;

/// <summary>
/// Feature: GetCapacityForecast — computa e persiste uma previsão de capacidade
/// para um recurso específico de um serviço.
/// </summary>
public static class GetCapacityForecast
{
    public sealed record Command(
        string ServiceId,
        string ServiceName,
        string Environment,
        string ResourceType,
        decimal CurrentUtilizationPercent,
        decimal DailyGrowthRatePercent,
        string? Notes) : ICommand<Response>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ServiceId).NotEmpty().MaximumLength(200);
            RuleFor(x => x.ServiceName).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Environment).NotEmpty().MaximumLength(100);
            RuleFor(x => x.ResourceType)
                .Must(x => new[] { "CPU", "Memory", "Disk", "Connections", "Requests" }.Contains(x))
                .WithMessage("Valid resource types: CPU, Memory, Disk, Connections, Requests.");
            RuleFor(x => x.CurrentUtilizationPercent).InclusiveBetween(0m, 100m);
        }
    }

    public sealed class Handler(
        ICapacityForecastRepository repository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider clock) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            int? daysToSaturation = null;
            if (request.DailyGrowthRatePercent > 0m)
            {
                var raw = (int)Math.Ceiling((100m - request.CurrentUtilizationPercent) / request.DailyGrowthRatePercent);
                if (raw <= 90) daysToSaturation = raw;
            }

            var result = CapacityForecast.Create(
                request.ServiceId,
                request.ServiceName,
                request.Environment,
                request.ResourceType,
                request.CurrentUtilizationPercent,
                request.DailyGrowthRatePercent,
                daysToSaturation,
                request.Notes,
                clock.UtcNow);

            if (!result.IsSuccess) return result.Error;

            repository.Add(result.Value!);
            await unitOfWork.CommitAsync(cancellationToken);

            var forecast = result.Value!;
            return Result<Response>.Success(new Response(
                forecast.Id.Value,
                forecast.ServiceId,
                forecast.ResourceType,
                forecast.CurrentUtilizationPercent,
                forecast.GrowthRatePercentPerDay,
                forecast.EstimatedDaysToSaturation,
                forecast.SaturationRisk,
                forecast.ComputedAt));
        }
    }

    public sealed record Response(
        Guid ForecastId,
        string ServiceId,
        string ResourceType,
        decimal CurrentUtilizationPercent,
        decimal GrowthRatePercentPerDay,
        int? EstimatedDaysToSaturation,
        string? SaturationRisk,
        DateTimeOffset ComputedAt);
}
