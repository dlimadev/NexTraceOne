using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Entities;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Enums;

namespace NexTraceOne.OperationalIntelligence.Application.Runtime.Features.CreateCustomChart;

/// <summary>
/// Feature: CreateCustomChart — cria um gráfico customizado para o utilizador.
/// Permite visualizações personalizadas de métricas da plataforma.
/// </summary>
public static class CreateCustomChart
{
    private static readonly string[] ValidTimeRanges = ["last_1h", "last_6h", "last_24h", "last_7d", "last_30d", "last_90d"];

    public sealed record Command(
        string TenantId,
        string UserId,
        string Name,
        ChartType ChartType,
        string MetricQuery,
        string TimeRange,
        string? FiltersJson) : ICommand<Response>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.TenantId).NotEmpty();
            RuleFor(x => x.UserId).NotEmpty();
            RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
            RuleFor(x => x.MetricQuery).NotEmpty().MaximumLength(4000);
            RuleFor(x => x.TimeRange).NotEmpty()
                .Must(t => ValidTimeRanges.Contains(t))
                .WithMessage($"TimeRange must be one of: {string.Join(", ", ValidTimeRanges)}");
        }
    }

    public sealed class Handler(
        ICustomChartRepository repository,
        IDateTimeProvider clock) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            var chart = CustomChart.Create(
                request.TenantId,
                request.UserId,
                request.Name,
                request.ChartType,
                request.MetricQuery,
                request.TimeRange,
                request.FiltersJson,
                clock.UtcNow);

            await repository.AddAsync(chart, cancellationToken);

            return Result<Response>.Success(new Response(
                chart.Id.Value,
                chart.Name,
                chart.ChartType.ToString(),
                chart.TimeRange,
                chart.CreatedAt));
        }
    }

    public sealed record Response(
        Guid ChartId,
        string Name,
        string ChartType,
        string TimeRange,
        DateTimeOffset CreatedAt);
}
