using Ardalis.GuardClauses;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Entities;
using NexTraceOne.OperationalIntelligence.Contracts.Cost.ServiceInterfaces;
using NexTraceOne.OperationalIntelligence.Contracts.Runtime.ServiceInterfaces;

using System.Globalization;

namespace NexTraceOne.Governance.Application.Features.GetGreenOpsReport;

/// <summary>
/// Feature: GetGreenOpsReport — relatório de operações sustentáveis (Green Ops).
/// Usa IRuntimeIntelligenceModule para métricas de CPU/memória e calcula emissões de CO₂.
/// Quando dados de runtime estão disponíveis, SimulatedNote é vazio.
/// </summary>
public static class GetGreenOpsReport
{
    // Constantes de energia por tipo de recurso (estimativas típicas de datacenter)
    private const double WattsPerCpuCore = 10.0;    // watts por core a 100 % de uso
    private const double WattsPerGbRam = 0.375;      // watts por GB de RAM
    private const double HostCpuCores = 16.0;        // estimativa de cores num host típico
    private const double HostRamGb = 16.0;           // estimativa de RAM num host típico
    private const int HoursPerMonth = 720;

    /// <summary>Query sem parâmetros — retorna relatório de emissões e configuração GreenOps.</summary>
    public sealed record Query() : IQuery<GreenOpsReport>;

    /// <summary>Comando para atualizar configuração GreenOps.</summary>
    public sealed record UpdateGreenOpsConfig(
        double IntensityFactorKgPerKwh,
        double EsgTargetKgCo2PerMonth,
        string DatacenterRegion) : ICommand<GreenOpsReport>;

    /// <summary>
    /// Handler de leitura do relatório GreenOps.
    /// Lê métricas de runtime, calcula emissões a partir de CPU/memória e aplica o factor de intensidade.
    /// Cai em modo estimado (SimulatedNote presente) se o runtime module não devolver dados.
    /// </summary>
    public sealed class Handler(
        IRuntimeIntelligenceModule runtimeModule,
        ICostIntelligenceModule costModule,
        IGreenOpsConfigurationRepository configRepository,
        IConfiguration fallbackConfig,
        ILogger<Handler> logger) : IQueryHandler<Query, GreenOpsReport>
    {
        public async Task<Result<GreenOpsReport>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            // Carregar configuração persistida (com fallback para IConfiguration)
            var persistedCfg = await configRepository.GetActiveAsync(null, cancellationToken);
            var intensityFactor = persistedCfg?.IntensityFactorKgPerKwh
                ?? (double.TryParse(fallbackConfig["Platform:GreenOps:IntensityFactorKgPerKwh"], NumberStyles.Any, CultureInfo.InvariantCulture, out var ifv) ? ifv : 0.233);
            var esgTarget = persistedCfg?.EsgTargetKgCo2PerMonth
                ?? (double.TryParse(fallbackConfig["Platform:GreenOps:EsgTargetKgCo2PerMonth"], NumberStyles.Any, CultureInfo.InvariantCulture, out var et) ? et : 100.0);
            var region = persistedCfg?.DatacenterRegion
                ?? (fallbackConfig["Platform:GreenOps:DatacenterRegion"] ?? "eu-west-1");

            var config = new GreenOpsConfig(
                IntensityFactorKgPerKwh: intensityFactor,
                EsgTargetKgCo2PerMonth: esgTarget,
                DatacenterRegion: region);

            // Tentar obter métricas de runtime reais
            PlatformAverageMetrics? metrics = null;
            try
            {
                metrics = await runtimeModule.GetPlatformAverageMetricsAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to read platform runtime metrics for GreenOps report — falling back to simulated data.");
            }

            if (metrics is null)
            {
                return Result<GreenOpsReport>.Success(new GreenOpsReport(
                    TotalKgCo2: 0.0,
                    TopServices: [],
                    Trend: "stable",
                    Config: config,
                    GeneratedAt: DateTimeOffset.UtcNow,
                    SimulatedNote: "GreenOps telemetry integration pending. Emissions will be computed from real workload data."));
            }

            // Calcular consumo de energia da plataforma (kWh/mês)
            var cpuFraction = metrics.CurrentCpuPct / 100.0;
            var memFraction = metrics.CurrentMemoryPct / 100.0;
            var cpuKwh = cpuFraction * HostCpuCores * WattsPerCpuCore * HoursPerMonth / 1000.0;
            var memKwh = memFraction * HostRamGb * WattsPerGbRam * HoursPerMonth / 1000.0;
            var totalKwh = cpuKwh + memKwh;
            var totalKgCo2 = Math.Round(totalKwh * intensityFactor, 2);

            // Construir top services por custo como proxy de emissões
            var topServices = new List<GreenOpsServiceDto>();
            try
            {
                var costRecords = await costModule.GetCostRecordsAsync(null, cancellationToken);
                if (costRecords.Count > 0)
                {
                    var totalCost = (double)costRecords.Sum(r => r.TotalCost);
                    if (totalCost > 0)
                    {
                        topServices = costRecords
                            .OrderByDescending(r => r.TotalCost)
                            .Take(5)
                            .Select(r =>
                            {
                                var fraction = (double)r.TotalCost / totalCost;
                                var svcKwh = Math.Round(totalKwh * fraction, 2);
                                var svcCo2 = Math.Round(svcKwh * intensityFactor, 2);
                                return new GreenOpsServiceDto(r.ServiceId, svcCo2, svcKwh);
                            })
                            .ToList();
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to load cost records for GreenOps top-services breakdown.");
            }

            // Tendência baseada na tendência de CPU
            var trend = metrics.CpuTrend switch
            {
                "increasing" => "increasing",
                "decreasing" => "decreasing",
                _ => "stable"
            };

            return Result<GreenOpsReport>.Success(new GreenOpsReport(
                TotalKgCo2: totalKgCo2,
                TopServices: topServices,
                Trend: trend,
                Config: config,
                GeneratedAt: DateTimeOffset.UtcNow,
                SimulatedNote: string.Empty));
        }
    }

    /// <summary>
    /// Handler de atualização de configuração GreenOps.
    /// Persiste o factor de intensidade e meta ESG em base de dados.
    /// </summary>
    public sealed class UpdateConfigHandler(
        IGreenOpsConfigurationRepository configRepository,
        IGovernanceUnitOfWork unitOfWork) : ICommandHandler<UpdateGreenOpsConfig, GreenOpsReport>
    {
        public async Task<Result<GreenOpsReport>> Handle(UpdateGreenOpsConfig request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var now = DateTimeOffset.UtcNow;
            var existing = await configRepository.GetActiveAsync(null, cancellationToken);

            if (existing is null)
            {
                var newConfig = GreenOpsConfiguration.Create(
                    request.IntensityFactorKgPerKwh,
                    request.EsgTargetKgCo2PerMonth,
                    request.DatacenterRegion,
                    now);
                await configRepository.AddAsync(newConfig, cancellationToken);
            }
            else
            {
                existing.Update(request.IntensityFactorKgPerKwh, request.EsgTargetKgCo2PerMonth, request.DatacenterRegion, now);
                configRepository.Update(existing);
            }

            await unitOfWork.CommitAsync(cancellationToken);

            var config = new GreenOpsConfig(
                IntensityFactorKgPerKwh: request.IntensityFactorKgPerKwh,
                EsgTargetKgCo2PerMonth: request.EsgTargetKgCo2PerMonth,
                DatacenterRegion: request.DatacenterRegion);

            return Result<GreenOpsReport>.Success(new GreenOpsReport(
                TotalKgCo2: 0.0,
                TopServices: [],
                Trend: "stable",
                Config: config,
                GeneratedAt: now,
                SimulatedNote: string.Empty));
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
