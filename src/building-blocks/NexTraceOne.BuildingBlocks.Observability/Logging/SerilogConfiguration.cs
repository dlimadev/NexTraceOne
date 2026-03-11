namespace NexTraceOne.BuildingBlocks.Observability.Logging;

/// <summary>
/// Configuração centralizada do Serilog para toda a plataforma.
/// Inclui: enrichers (Environment, MachineName, ThreadId),
/// sinks (Console, File, PostgreSQL), destructuring de objetos de domínio.
/// </summary>
public static class SerilogConfiguration
{
    // TODO: Implementar ConfigureSerilog(IHostBuilder)
}
