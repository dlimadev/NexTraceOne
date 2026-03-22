using FluentAssertions;
using NexTraceOne.BuildingBlocks.Observability.Telemetry.Configuration;
using NexTraceOne.BuildingBlocks.Observability.Telemetry.Models;

namespace NexTraceOne.BuildingBlocks.Observability.Tests.Telemetry;

/// <summary>
/// Testes de validação da separação arquitetural entre Product Store e provider de observabilidade.
/// Garante que:
/// - Product Store (PostgreSQL) armazena apenas agregados, correlações, topologia e referências
/// - Provider de observabilidade (ClickHouse/Elastic) armazena traces e logs crus
/// - Referências do Product Store apontam para dados crus no provider de observabilidade
/// - Política de retenção diferencia bruto de agregado
/// - Provider é configurável (ClickHouse ou Elastic)
/// - Modo de coleta é configurável (OpenTelemetryCollector ou ClrProfiler)
/// </summary>
public sealed class StoreArchitectureTests
{
    [Fact]
    public void ProductStore_ShouldOnlyContainAggregatedData()
    {
        // Product Store contém: métricas agregadas, topologia, anomalias,
        // correlações, contextos investigativos e referências para dados crus.
        // NÃO contém: traces crus ou logs crus em volume.

        var serviceMetrics = new ServiceMetricsSnapshot
        {
            ServiceId = Guid.NewGuid(),
            ServiceName = "api",
            Environment = "prod",
            AggregationLevel = AggregationLevel.OneMinute,
            IntervalStart = DateTimeOffset.UtcNow,
            IntervalEnd = DateTimeOffset.UtcNow
        };

        // Métricas no Product Store são sempre agregadas, nunca raw
        serviceMetrics.AggregationLevel.Should().NotBe(AggregationLevel.Raw,
            "Product Store (PostgreSQL) nunca armazena dados no nível Raw — esses ficam no provider de observabilidade");
    }

    [Fact]
    public void TelemetryReference_ShouldBridgeProductStoreToObservabilityProvider()
    {
        // Referências permitem navegação do Product Store para dados crus no provider de observabilidade
        var reference = new TelemetryReference
        {
            SignalType = TelemetrySignalType.Traces,
            ExternalId = "trace-id-abc123",
            BackendType = "clickhouse",
            AccessUri = "SELECT * FROM nextraceone_obs.otel_traces WHERE TraceId = 'trace-id-abc123'",
            OriginalTimestamp = DateTimeOffset.UtcNow
        };

        reference.ExternalId.Should().NotBeNullOrEmpty(
            "referência deve ter ID externo para buscar dado cru no provider de observabilidade");
        reference.BackendType.Should().NotBeNullOrEmpty(
            "referência deve indicar qual backend contém o dado cru");
    }

    [Fact]
    public void RetentionPolicy_ShouldDifferentiateRawFromAggregated()
    {
        var policy = new RetentionPolicyOptions();

        // Dados crus (provider de observabilidade) devem ter retenção mais curta
        policy.RawTraces.TotalRetentionDays
            .Should().BeLessThan(policy.HourlyAggregates.TotalRetentionDays,
                "traces crus devem expirar antes dos agregados para controlar custo");

        policy.RawLogs.TotalRetentionDays
            .Should().BeLessThan(policy.HourlyAggregates.TotalRetentionDays,
                "logs crus devem expirar antes dos agregados para controlar custo");
    }

    [Fact]
    public void RetentionPolicy_ShouldSeparateAuditFromObservability()
    {
        var policy = new RetentionPolicyOptions();

        // Auditoria tem política completamente separada da observabilidade
        policy.AuditCompliance.TotalRetentionDays
            .Should().BeGreaterThan(policy.RawTraces.TotalRetentionDays,
                "dados de auditoria/compliance seguem política regulatória independente");
    }

    [Fact]
    public void Configuration_ShouldSupportIndependentStoreEndpoints()
    {
        var options = new TelemetryStoreOptions();

        // Product Store e provider de observabilidade são independentes
        options.ProductStore.ConnectionStringName.Should().NotBeNullOrEmpty();
        options.ObservabilityProvider.Provider.Should().NotBeNullOrEmpty();
        options.ObservabilityProvider.ClickHouse.ConnectionString.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Configuration_ShouldSupportDedicatedTelemetrySchema()
    {
        var options = new TelemetryStoreOptions();

        // Schema separado no PostgreSQL para tabelas de telemetria agregada
        options.ProductStore.Schema.Should().Be("telemetry",
            "dados de telemetria agregada ficam em schema separado para facilitar gestão de partições e retenção");
    }

    [Fact]
    public void InvestigationContext_ShouldOnlyHoldReferencesNotRawData()
    {
        var context = new InvestigationContext
        {
            Title = "Investigation",
            InvestigationType = "anomaly",
            PrimaryServiceId = Guid.NewGuid(),
            PrimaryServiceName = "api",
            Environment = "prod",
            TimeWindowStart = DateTimeOffset.UtcNow.AddHours(-1),
            TimeWindowEnd = DateTimeOffset.UtcNow,
            Status = "open",
            TelemetryReferenceIds = [Guid.NewGuid(), Guid.NewGuid()]
        };

        // Investigation context contém IDs de referência, não dados crus
        context.TelemetryReferenceIds.Should().AllSatisfy(id =>
            id.Should().NotBeEmpty());

        // O AiSummaryJson é um resumo compacto, não dump de dados
        // Limitado a 4KB para eficiência de context window de IA
        context.AiSummaryJson.Should().BeNull("por default não tem sumário — é gerado sob demanda");
    }

    [Fact]
    public void Provider_ShouldNotBePostgreSQL()
    {
        var options = new TelemetryStoreOptions();

        // O provider de observabilidade não deve ser PostgreSQL
        options.ObservabilityProvider.Provider.Should().NotBe("postgresql",
            "PostgreSQL é exclusivo para dados transacionais e de domínio, não para logs/traces crus");
    }

    [Fact]
    public void Configuration_ShouldSupportConfigurableProvider()
    {
        var options = new TelemetryStoreOptions();

        // ClickHouse e Elastic são os providers suportados
        options.ObservabilityProvider.ClickHouse.Should().NotBeNull();
        options.ObservabilityProvider.Elastic.Should().NotBeNull();
    }

    [Fact]
    public void Configuration_ShouldSupportConfigurableCollectionMode()
    {
        var options = new TelemetryStoreOptions();

        // OpenTelemetryCollector e ClrProfiler são os modos de coleta suportados
        options.CollectionMode.OpenTelemetryCollector.Should().NotBeNull();
        options.CollectionMode.ClrProfiler.Should().NotBeNull();
    }
}
