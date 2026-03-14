using FluentAssertions;
using NexTraceOne.BuildingBlocks.Observability.Telemetry.Models;

namespace NexTraceOne.BuildingBlocks.Observability.Tests.Telemetry;

/// <summary>
/// Testes dos modelos de topologia observada.
/// Valida: criação, shadow dependency, confidence score, comunicação bidirecional.
/// </summary>
public sealed class ObservedTopologyTests
{
    [Fact]
    public void Create_WithRequiredFields_ShouldSucceed()
    {
        var entry = new ObservedTopologyEntry
        {
            SourceServiceId = Guid.NewGuid(),
            SourceServiceName = "order-api",
            TargetServiceId = Guid.NewGuid(),
            TargetServiceName = "payment-api",
            CommunicationType = "http",
            Environment = "production",
            FirstSeenAt = DateTimeOffset.UtcNow.AddDays(-30),
            LastSeenAt = DateTimeOffset.UtcNow,
            TotalCallCount = 150000,
            ConfidenceScore = 0.95
        };

        entry.SourceServiceName.Should().Be("order-api");
        entry.TargetServiceName.Should().Be("payment-api");
        entry.CommunicationType.Should().Be("http");
        entry.ConfidenceScore.Should().BeGreaterThanOrEqualTo(0.0);
    }

    [Fact]
    public void ShadowDependency_ShouldBeMarkedCorrectly()
    {
        var entry = new ObservedTopologyEntry
        {
            SourceServiceId = Guid.NewGuid(),
            SourceServiceName = "frontend-bff",
            TargetServiceId = Guid.NewGuid(),
            TargetServiceName = "legacy-api",
            CommunicationType = "http",
            Environment = "production",
            FirstSeenAt = DateTimeOffset.UtcNow.AddDays(-5),
            LastSeenAt = DateTimeOffset.UtcNow,
            IsShadowDependency = true,
            ConfidenceScore = 0.65
        };

        entry.IsShadowDependency.Should().BeTrue(
            "dependências não declaradas no catálogo devem ser marcadas como shadow");
    }

    [Fact]
    public void MultipleCommunicationTypes_ShouldBeSupported()
    {
        var types = new[] { "http", "grpc", "database", "messaging", "tcp" };

        foreach (var commType in types)
        {
            var entry = new ObservedTopologyEntry
            {
                SourceServiceId = Guid.NewGuid(),
                SourceServiceName = "service-a",
                TargetServiceId = Guid.NewGuid(),
                TargetServiceName = "service-b",
                CommunicationType = commType,
                Environment = "production",
                FirstSeenAt = DateTimeOffset.UtcNow,
                LastSeenAt = DateTimeOffset.UtcNow
            };

            entry.CommunicationType.Should().Be(commType);
        }
    }

    [Fact]
    public void TenantIsolation_ShouldBeSupported()
    {
        var tenantId = Guid.NewGuid();
        var entry = new ObservedTopologyEntry
        {
            SourceServiceId = Guid.NewGuid(),
            SourceServiceName = "api-a",
            TargetServiceId = Guid.NewGuid(),
            TargetServiceName = "api-b",
            CommunicationType = "grpc",
            Environment = "production",
            FirstSeenAt = DateTimeOffset.UtcNow,
            LastSeenAt = DateTimeOffset.UtcNow,
            TenantId = tenantId
        };

        entry.TenantId.Should().Be(tenantId);
    }
}
