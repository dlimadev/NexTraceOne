using System.Runtime.InteropServices;

namespace NexTraceOne.ApiHost.OnPrem;

/// <summary>
/// Serviço de avaliação de hardware para recomendação de modelos LLM locais.
/// Detecta CPU, RAM total/disponível, GPU e espaço em disco para
/// determinar quais modelos Ollama são compatíveis com o servidor actual.
/// </summary>
public sealed class HardwareAssessmentService
{
    private static readonly IReadOnlyList<ModelSpec> KnownModels = new List<ModelSpec>
    {
        new("deepseek-r1:1.5b",  "DeepSeek R1 1.5B",  1.1, 2.0,  "Padrão recomendado — baixíssimo consumo, ideal para on-prem com recursos limitados"),
        new("deepseek-r1:7b",    "DeepSeek R1 7B",    4.7, 7.0,  "Excelente relação qualidade/consumo para uso interactivo"),
        new("llama3.2:3b",       "Llama 3.2 3B",      2.0, 3.5,  "Bom para contextos maiores e análise de código"),
        new("qwen2.5:7b",        "Qwen 2.5 7B",       4.7, 7.0,  "Alta qualidade em análise de código e contratos"),
        new("llama3.1:8b",       "Llama 3.1 8B",      4.9, 8.0,  "Equilibrado para uso geral com contextos longos"),
        new("llama3.1:13b",      "Llama 3.1 13B",     8.0, 12.0, "Melhor qualidade; requer servidor dedicado"),
        new("mistral-nemo:12b",  "Mistral Nemo 12B",  7.1, 11.0, "Excelente para análise multilíngue"),
        new("llama3.1:70b",      "Llama 3.1 70B",    40.0, 56.0, "Qualidade máxima; requer hardware de alto nível com GPU"),
        new("nomic-embed-text",  "Nomic Embed Text",  0.3,  0.5,  "Embeddings para RAG/search semântico — baixíssimo consumo"),
        new("mxbai-embed-large", "MxBai Embed Large", 0.7,  1.0,  "Embeddings de alta qualidade para RAG"),
    };

    /// <summary>Executa a avaliação completa do hardware e retorna o relatório de compatibilidade.</summary>
    public Task<HardwareAssessmentReport> AssessAsync(CancellationToken ct = default)
    {
        var totalRamGb  = GetTotalRamGb();
        var availRamGb  = GetAvailableRamGb();
        var cpuCores    = Environment.ProcessorCount;
        var cpuModel    = GetCpuModel();
        var diskFreeGb  = GetDiskFreeGb();
        var hasGpu      = DetectGpu();
        var gpuModel    = hasGpu ? GetGpuModel() : null;
        var gpuVramGb   = hasGpu ? GetGpuVramGb() : 0;
        var modelAdvice = BuildModelAdvice(availRamGb, diskFreeGb, hasGpu, gpuVramGb);

        var report = new HardwareAssessmentReport(
            CpuModel:       cpuModel,
            CpuCores:       cpuCores,
            TotalRamGb:     totalRamGb,
            AvailableRamGb: availRamGb,
            DiskFreeGb:     diskFreeGb,
            HasGpu:         hasGpu,
            GpuModel:       gpuModel,
            GpuVramGb:      gpuVramGb,
            OsDescription:  RuntimeInformation.OSDescription,
            Models:         modelAdvice,
            AssessedAt:     DateTimeOffset.UtcNow);

        return Task.FromResult(report);
    }

    private static IReadOnlyList<ModelAdvice> BuildModelAdvice(
        double availRamGb, double diskFreeGb, bool hasGpu, double gpuVramGb)
    {
        return KnownModels.Select(m =>
        {
            var ramOk  = availRamGb >= m.RequiredRamGb;
            var diskOk = diskFreeGb >= m.SizeGb;
            var gpuAcc = hasGpu && gpuVramGb >= m.RequiredRamGb;

            // Estimate tokens/s: 15 tok/s base at 7B, scales inversely with param count
            var baseToks  = 15.0 / Math.Max(m.ParamsB / 7.0, 0.1);
            var estTokSec = gpuAcc ? baseToks * 8.0 : baseToks;

            string status;
            string? warning = null;

            if (!ramOk)
            {
                status  = "Incompatible";
                warning = $"Requer {m.RequiredRamGb:F1} GB RAM disponível; disponível: {availRamGb:F1} GB";
            }
            else if (!diskOk)
            {
                status  = "Incompatible";
                warning = $"Requer {m.SizeGb:F1} GB de espaço em disco; disponível: {diskFreeGb:F1} GB";
            }
            else if (availRamGb < m.RequiredRamGb * 1.5)
            {
                status  = "Compatible";
                warning = "Funciona mas será lento para uso concorrente — recomenda-se servidor dedicado";
            }
            else
            {
                status = "Compatible";
            }

            return new ModelAdvice(
                Name:             m.Name,
                DisplayName:      m.DisplayName,
                SizeGb:           m.SizeGb,
                RequiredRamGb:    m.RequiredRamGb,
                EstTokPerSec:     Math.Round(estTokSec, 1),
                AcceleratedByGpu: gpuAcc,
                Status:           status,
                Warning:          warning,
                Description:      m.Description);
        }).ToList();
    }

    private static double GetTotalRamGb()
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return GetLinuxMemInfoGb("MemTotal");
            }
        }
        catch { /* fallback below */ }

        return GC.GetGCMemoryInfo().TotalAvailableMemoryBytes / (1024.0 * 1024 * 1024);
    }

    private static double GetAvailableRamGb()
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return GetLinuxMemInfoGb("MemAvailable");
            }
        }
        catch { /* fallback below */ }

        return GC.GetGCMemoryInfo().TotalAvailableMemoryBytes / (1024.0 * 1024 * 1024);
    }

    private static double GetLinuxMemInfoGb(string key)
    {
        var lines = File.ReadAllLines("/proc/meminfo");
        foreach (var line in lines)
        {
            if (!line.StartsWith(key + ":", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var parts = line.Split([' '], StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2 && long.TryParse(parts[1], out var kb))
            {
                return kb / (1024.0 * 1024);
            }
        }

        return 0;
    }

    private static double GetDiskFreeGb()
    {
        try
        {
            var path  = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "C:\\" : "/";
            var drive = new DriveInfo(path);
            return drive.AvailableFreeSpace / (1024.0 * 1024 * 1024);
        }
        catch
        {
            return 0;
        }
    }

    private static string GetCpuModel()
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                var lines     = File.ReadAllLines("/proc/cpuinfo");
                var modelLine = lines.FirstOrDefault(l => l.StartsWith("model name", StringComparison.OrdinalIgnoreCase));
                if (modelLine is not null)
                {
                    return modelLine.Split(':').LastOrDefault()?.Trim() ?? "Unknown";
                }
            }
        }
        catch { /* fallback */ }

        return $"{RuntimeInformation.ProcessArchitecture} ({Environment.ProcessorCount} cores)";
    }

    private static bool DetectGpu()
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return File.Exists("/proc/driver/nvidia/version");
            }
        }
        catch { /* no GPU */ }

        return false;
    }

    private static string? GetGpuModel()
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) &&
                File.Exists("/proc/driver/nvidia/version"))
            {
                return "NVIDIA GPU (detected)";
            }
        }
        catch { /* ignore */ }

        return null;
    }

    private static double GetGpuVramGb()
    {
        // Without nvidia-smi bindings, return 0 as advisory fallback
        return 0;
    }

    private sealed record ModelSpec(
        string Name,
        string DisplayName,
        double SizeGb,
        double RequiredRamGb,
        string Description)
    {
        public double ParamsB => Name switch
        {
            var n when n.Contains("1.5b") => 1.5,
            var n when n.Contains("3b")   => 3.0,
            var n when n.Contains("7b")   => 7.0,
            var n when n.Contains("8b")   => 8.0,
            var n when n.Contains("12b")  => 12.0,
            var n when n.Contains("13b")  => 13.0,
            var n when n.Contains("70b")  => 70.0,
            _                             => 1.0
        };
    }
}

/// <summary>Relatório de avaliação de hardware para compatibilidade com modelos LLM locais.</summary>
public sealed record HardwareAssessmentReport(
    string CpuModel,
    int CpuCores,
    double TotalRamGb,
    double AvailableRamGb,
    double DiskFreeGb,
    bool HasGpu,
    string? GpuModel,
    double GpuVramGb,
    string OsDescription,
    IReadOnlyList<ModelAdvice> Models,
    DateTimeOffset AssessedAt);

/// <summary>Conselho de compatibilidade para um modelo LLM específico.</summary>
public sealed record ModelAdvice(
    string Name,
    string DisplayName,
    double SizeGb,
    double RequiredRamGb,
    double EstTokPerSec,
    bool AcceleratedByGpu,
    string Status,
    string? Warning,
    string Description);
