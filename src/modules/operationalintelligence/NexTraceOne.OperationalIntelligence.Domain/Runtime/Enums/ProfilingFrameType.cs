namespace NexTraceOne.OperationalIntelligence.Domain.Runtime.Enums;

/// <summary>
/// Tipo de frame de profiling — identifica o formato de origem dos dados ingestados.
/// Cada formato tem semântica diferente e determina como o motor de análise processa os frames.
/// </summary>
public enum ProfilingFrameType
{
    /// <summary>dotnet-trace / nettrace format — .NET runtime events (CPU samples, GC, JIT).</summary>
    DotNetTrace = 0,

    /// <summary>pprof format — Go, Java, Rust ou outros runtimes via pprof endpoint.</summary>
    Pprof = 1,

    /// <summary>async-profiler (JVM) — flame graphs de CPU e alocação de memória para JVM.</summary>
    AsyncProfiler = 2,

    /// <summary>Formato genérico de stack samples — para runtimes não tipificados.</summary>
    GenericStackSamples = 3
}
