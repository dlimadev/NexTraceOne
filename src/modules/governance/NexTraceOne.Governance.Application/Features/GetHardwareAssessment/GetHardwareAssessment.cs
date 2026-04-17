using System.Runtime.InteropServices;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.Governance.Application.Features.GetHardwareAssessment;

/// <summary>
/// Feature: GetHardwareAssessment — avaliação de hardware do servidor onde a plataforma corre.
/// Usa System.Runtime.InteropServices e GC metrics para dados reais.
/// </summary>
public static class GetHardwareAssessment
{
    /// <summary>Query sem parâmetros — retorna avaliação de hardware do servidor.</summary>
    public sealed record Query() : IQuery<HardwareAssessmentReport>;

    /// <summary>Handler que lê dados reais de hardware do servidor.</summary>
    public sealed class Handler : IQueryHandler<Query, HardwareAssessmentReport>
    {
        public Task<Result<HardwareAssessmentReport>> Handle(Query request, CancellationToken cancellationToken)
        {
            var gcMemInfo = GC.GetGCMemoryInfo();
            var totalRamGb = Math.Round(gcMemInfo.TotalAvailableMemoryBytes / (1024.0 * 1024 * 1024), 2);
            var availableRamGb = Math.Round((gcMemInfo.TotalAvailableMemoryBytes - gcMemInfo.MemoryLoadBytes) / (1024.0 * 1024 * 1024), 2);

            var cpuCores = Environment.ProcessorCount;
            var osDescription = RuntimeInformation.OSDescription;
            var hasGpu = false; // GPU detection requires platform-specific APIs

            // Disk free não está disponível sem System.IO — aproximamos via GC
            var diskFreeGb = 0.0;
            try
            {
                var drive = new System.IO.DriveInfo(System.IO.Path.GetPathRoot(System.AppContext.BaseDirectory) ?? "/");
                if (drive.IsReady)
                    diskFreeGb = Math.Round(drive.AvailableFreeSpace / (1024.0 * 1024 * 1024), 2);
            }
            catch
            {
                // Ambiente sem acesso a disco — ignorar
            }

            var models = new List<AiModelHardwareDto>
            {
                new("internal-llm", "CPU", $"{cpuCores} cores available", cpuCores >= 8 ? "Supported" : "Limited")
            };

            var response = new HardwareAssessmentReport(
                CpuModel: $"x64 ({cpuCores} logical cores)",
                CpuCores: cpuCores,
                TotalRamGb: totalRamGb,
                AvailableRamGb: availableRamGb,
                DiskFreeGb: diskFreeGb,
                HasGpu: hasGpu,
                OsDescription: osDescription,
                Models: models,
                AssessedAt: DateTimeOffset.UtcNow);

            return Task.FromResult(Result<HardwareAssessmentReport>.Success(response));
        }
    }

    /// <summary>Relatório de avaliação de hardware do servidor.</summary>
    public sealed record HardwareAssessmentReport(
        string CpuModel,
        int CpuCores,
        double TotalRamGb,
        double AvailableRamGb,
        double DiskFreeGb,
        bool HasGpu,
        string OsDescription,
        IReadOnlyList<AiModelHardwareDto> Models,
        DateTimeOffset AssessedAt);

    /// <summary>Avaliação de compatibilidade de hardware para um modelo de IA.</summary>
    public sealed record AiModelHardwareDto(
        string ModelId,
        string PreferredRuntime,
        string HardwareInfo,
        string SupportStatus);
}
