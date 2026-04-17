using MediatR;
using Microsoft.Extensions.Configuration;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.Governance.Application.Features.GetGreenOpsReport;

/// <summary>
/// Feature: GetGreenOpsReport — relatório de operações sustentáveis (Green Ops).
/// Lê de IConfiguration "Platform:GreenOps:*".
/// </summary>
public static class GetGreenOpsReport
{
    /// <summary>Query sem parâmetros — retorna relatório de emissões e configuração GreenOps.</summary>
    public sealed record Query() : IQuery<GreenOpsReport>;

    /// <summary>Comando para atualizar configuração GreenOps.</summary>
    public sealed record UpdateGreenOpsConfig(
        double IntensityFactorKgPerKwh,
        double EsgTargetKgCo2PerMonth,
        string DatacenterRegion) : ICommand<GreenOpsReport>;

    /// <summary>Handler de leitura do relatório GreenOps.</summary>
    public sealed class Handler(IConfiguration configuration) : IQueryHandler<Query, GreenOpsReport>
    {
        public Task<Result<GreenOpsReport>> Handle(Query request, CancellationToken cancellationToken)
        {
            var intensityFactor = double.TryParse(configuration["Platform:GreenOps:IntensityFactorKgPerKwh"], out var ifv) ? ifv : 0.233;
            var esgTarget = double.TryParse(configuration["Platform:GreenOps:EsgTargetKgCo2PerMonth"], out var et) ? et : 100.0;
            var region = configuration["Platform:GreenOps:DatacenterRegion"] ?? "eu-west-1";

            var config = new GreenOpsConfig(
                IntensityFactorKgPerKwh: intensityFactor,
                EsgTargetKgCo2PerMonth: esgTarget,
                DatacenterRegion: region);

            var response = new GreenOpsReport(
                TotalKgCo2: 0.0,
                TopServices: [],
                Trend: "stable",
                Config: config,
                GeneratedAt: DateTimeOffset.UtcNow,
                SimulatedNote: "GreenOps telemetry integration pending. Emissions will be computed from real workload data.");

            return Task.FromResult(Result<GreenOpsReport>.Success(response));
        }
    }

    /// <summary>Handler de atualização de configuração GreenOps.</summary>
    public sealed class UpdateConfigHandler : ICommandHandler<UpdateGreenOpsConfig, GreenOpsReport>
    {
        public Task<Result<GreenOpsReport>> Handle(UpdateGreenOpsConfig request, CancellationToken cancellationToken)
        {
            var config = new GreenOpsConfig(
                IntensityFactorKgPerKwh: request.IntensityFactorKgPerKwh,
                EsgTargetKgCo2PerMonth: request.EsgTargetKgCo2PerMonth,
                DatacenterRegion: request.DatacenterRegion);

            var response = new GreenOpsReport(
                TotalKgCo2: 0.0,
                TopServices: [],
                Trend: "stable",
                Config: config,
                GeneratedAt: DateTimeOffset.UtcNow,
                SimulatedNote: string.Empty);

            return Task.FromResult(Result<GreenOpsReport>.Success(response));
        }
    }

    /// <summary>Relatório GreenOps da plataforma.</summary>
    public sealed record GreenOpsReport(
        double TotalKgCo2,
        IReadOnlyList<GreenOpsServiceDto> TopServices,
        string Trend,
        GreenOpsConfig Config,
        DateTimeOffset GeneratedAt,
        string SimulatedNote);

    /// <summary>Configuração GreenOps.</summary>
    public sealed record GreenOpsConfig(
        double IntensityFactorKgPerKwh,
        double EsgTargetKgCo2PerMonth,
        string DatacenterRegion);

    /// <summary>Serviço com maior impacto de emissões.</summary>
    public sealed record GreenOpsServiceDto(
        string ServiceName,
        double KgCo2,
        double KwhConsumed);
}
