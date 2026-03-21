using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Domain.Reliability.Enums;

namespace NexTraceOne.OperationalIntelligence.Application.Reliability.Features.GetServiceReliabilityTrend;

/// <summary>
/// Feature: GetServiceReliabilityTrend — obtém a tendência de confiabilidade
/// de um serviço ao longo do tempo. Retorna direção, período, e indicadores resumidos.
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class GetServiceReliabilityTrend
{
    /// <summary>Query para obter tendência de confiabilidade de um serviço.</summary>
    public sealed record Query(string ServiceId) : IQuery<Response>;

    /// <summary>Valida os parâmetros de consulta.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ServiceId).NotEmpty().MaximumLength(200);
        }
    }

    /// <summary>
    /// Handler que compõe a tendência de confiabilidade do serviço.
    /// </summary>
    public sealed class Handler : IQueryHandler<Query, Response>
    {
        public Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var response = request.ServiceId.ToLowerInvariant() switch
            {
                "svc-order-api" => new Response(
                    "svc-order-api", TrendDirection.Stable, "7d",
                    "Availability, latency and error rate all within normal ranges over the last 7 days.",
                    [
                        new TrendDataPoint(DateTimeOffset.UtcNow.AddDays(-7), ReliabilityStatus.Healthy, 99.95m, 0.3m),
                        new TrendDataPoint(DateTimeOffset.UtcNow.AddDays(-5), ReliabilityStatus.Healthy, 99.97m, 0.2m),
                        new TrendDataPoint(DateTimeOffset.UtcNow.AddDays(-3), ReliabilityStatus.Healthy, 99.93m, 0.4m),
                        new TrendDataPoint(DateTimeOffset.UtcNow.AddDays(-1), ReliabilityStatus.Healthy, 99.95m, 0.3m),
                        new TrendDataPoint(DateTimeOffset.UtcNow, ReliabilityStatus.Healthy, 99.96m, 0.3m),
                    ]),
                "svc-payment-gateway" => new Response(
                    "svc-payment-gateway", TrendDirection.Declining, "24h",
                    "Error rate increased from 1.0% to 5.2% after deployment v3.1.0.",
                    [
                        new TrendDataPoint(DateTimeOffset.UtcNow.AddHours(-24), ReliabilityStatus.Healthy, 99.0m, 1.0m),
                        new TrendDataPoint(DateTimeOffset.UtcNow.AddHours(-18), ReliabilityStatus.Healthy, 99.1m, 0.9m),
                        new TrendDataPoint(DateTimeOffset.UtcNow.AddHours(-12), ReliabilityStatus.Healthy, 98.8m, 1.2m),
                        new TrendDataPoint(DateTimeOffset.UtcNow.AddHours(-6), ReliabilityStatus.Degraded, 94.8m, 5.2m),
                        new TrendDataPoint(DateTimeOffset.UtcNow, ReliabilityStatus.Degraded, 94.5m, 5.5m),
                    ]),
                _ => new Response(
                    request.ServiceId, TrendDirection.Stable, "7d",
                    "Insufficient data to determine trend.",
                    [])
            };

            return Task.FromResult(Result<Response>.Success(response));
        }
    }

    /// <summary>Ponto de dados na tendência de confiabilidade.</summary>
    public sealed record TrendDataPoint(
        DateTimeOffset Timestamp,
        ReliabilityStatus Status,
        decimal AvailabilityPercent,
        decimal ErrorRatePercent);

    /// <summary>Resposta com tendência de confiabilidade do serviço.</summary>
    public sealed record Response(
        string ServiceId,
        TrendDirection Direction,
        string Timeframe,
        string Summary,
        IReadOnlyList<TrendDataPoint> DataPoints,
        bool IsSimulated = true,
        string DataSource = "demo");
}
