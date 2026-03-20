using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Entities;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Enums;

namespace NexTraceOne.OperationalIntelligence.Tests.Incidents;

/// <summary>
/// Testes unitários para o contexto de tenant/ambiente nas entidades IncidentRecord e Release.
/// Verificam enriquecimento de contexto, semântica idempotente e retrocompatibilidade.
/// </summary>
public sealed class TenantContextTests
{
    private static IncidentRecord CreateSampleIncident() =>
        IncidentRecord.Create(
            IncidentRecordId.New(),
            externalRef: "INC-TEST-001",
            title: "Test Incident",
            description: "Description",
            type: IncidentType.ServiceDegradation,
            severity: IncidentSeverity.Minor,
            status: IncidentStatus.Open,
            serviceId: "svc-1",
            serviceName: "Service One",
            ownerTeam: "team-a",
            impactedDomain: null,
            environment: "staging",
            detectedAt: DateTimeOffset.UtcNow,
            lastUpdatedAt: DateTimeOffset.UtcNow,
            hasCorrelation: false,
            correlationConfidence: CorrelationConfidence.NotAssessed,
            mitigationStatus: MitigationStatus.NotStarted);

    [Fact]
    public void IncidentRecord_SetTenantContext_SetsValues()
    {
        var incident = CreateSampleIncident();
        var tenantId = Guid.NewGuid();
        var environmentId = Guid.NewGuid();

        incident.SetTenantContext(tenantId, environmentId);

        incident.TenantId.Should().Be(tenantId);
        incident.EnvironmentId.Should().Be(environmentId);
    }

    [Fact]
    public void IncidentRecord_SetTenantContext_IsIdempotent()
    {
        var incident = CreateSampleIncident();
        var originalTenantId = Guid.NewGuid();
        var originalEnvironmentId = Guid.NewGuid();

        incident.SetTenantContext(originalTenantId, originalEnvironmentId);

        // Second call with different values should NOT overwrite
        incident.SetTenantContext(Guid.NewGuid(), Guid.NewGuid());

        incident.TenantId.Should().Be(originalTenantId);
        incident.EnvironmentId.Should().Be(originalEnvironmentId);
    }

    [Fact]
    public void CreateIncidentInput_HasBackwardCompatibleDefaults()
    {
        var input = new CreateIncidentInput(
            Title: "Test",
            Description: "Desc",
            IncidentType: IncidentType.ServiceDegradation,
            Severity: IncidentSeverity.Minor,
            ServiceId: "svc-1",
            ServiceDisplayName: "Service One",
            OwnerTeam: "team-a",
            ImpactedDomain: null,
            Environment: "staging",
            DetectedAtUtc: null);

        input.TenantId.Should().BeNull();
        input.EnvironmentId.Should().BeNull();
    }

    [Fact]
    public void Release_SetTenantContext_SetsValues()
    {
        var release = Release.Create(
            apiAssetId: Guid.NewGuid(),
            serviceName: "my-service",
            version: "1.0.0",
            environment: "staging",
            pipelineSource: "https://ci.example.com/pipeline/1",
            commitSha: "abc123def456",
            createdAt: DateTimeOffset.UtcNow);

        var tenantId = Guid.NewGuid();
        var environmentId = Guid.NewGuid();

        release.SetTenantContext(tenantId, environmentId);

        release.TenantId.Should().Be(tenantId);
        release.EnvironmentId.Should().Be(environmentId);
    }

    [Fact]
    public void Release_SetTenantContext_IsIdempotent()
    {
        var release = Release.Create(
            apiAssetId: Guid.NewGuid(),
            serviceName: "my-service",
            version: "1.0.0",
            environment: "staging",
            pipelineSource: "https://ci.example.com/pipeline/1",
            commitSha: "abc123def456",
            createdAt: DateTimeOffset.UtcNow);

        var originalTenantId = Guid.NewGuid();
        var originalEnvironmentId = Guid.NewGuid();

        release.SetTenantContext(originalTenantId, originalEnvironmentId);
        release.SetTenantContext(Guid.NewGuid(), Guid.NewGuid());

        release.TenantId.Should().Be(originalTenantId);
        release.EnvironmentId.Should().Be(originalEnvironmentId);
    }
}
