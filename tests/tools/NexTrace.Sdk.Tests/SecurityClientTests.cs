using FluentAssertions;

namespace NexTrace.Sdk.Tests;

public class SecurityClientTests
{
    [Fact]
    public async Task GetDependencyHealthAsync_Returns_Health_Dashboard()
    {
        var handler = MockHttpMessageHandler.WithJsonResponse("""
        {
            "serviceId": "11111111-1111-1111-1111-111111111111",
            "healthScore": 72,
            "lastScanAt": "2026-06-23T10:00:00Z",
            "totalDeps": 40,
            "directDeps": 12,
            "transitiveDeps": 28,
            "criticalVulnCount": 1,
            "highVulnCount": 2,
            "mediumVulnCount": 3,
            "lowVulnCount": 4,
            "outdatedCount": 5,
            "deprecatedCount": 1,
            "licenseRiskCounts": { "High": 2, "Low": 38 }
        }
        """);

        using var client = new NexTraceSdkClient(new HttpClient(handler) { BaseAddress = new Uri("http://localhost") });

        var health = await client.Security.GetDependencyHealthAsync("11111111-1111-1111-1111-111111111111");

        health.Should().NotBeNull();
        health!.HealthScore.Should().Be(72);
        health.CriticalVulnCount.Should().Be(1);
        health.HighVulnCount.Should().Be(2);
        health.LicenseRiskCounts.Should().ContainKey("High");
        handler.Requests.Should().ContainSingle();
        handler.Requests[0].Method.Should().Be(HttpMethod.Get);
        handler.Requests[0].RequestUri!.PathAndQuery
            .Should().Be("/api/v1/catalog/dependencies/11111111-1111-1111-1111-111111111111/health");
    }

    [Fact]
    public async Task ListVulnerableServicesAsync_Passes_MinSeverity_Query()
    {
        var handler = MockHttpMessageHandler.WithJsonResponse("""
        [
            {
                "profileId": "aaaaaaaa-0000-0000-0000-000000000001",
                "serviceId": "22222222-2222-2222-2222-222222222222",
                "healthScore": 40,
                "criticalCount": 2,
                "highCount": 3,
                "mediumCount": 1,
                "lastScanAt": "2026-06-23T10:00:00Z"
            }
        ]
        """);

        using var client = new NexTraceSdkClient(new HttpClient(handler) { BaseAddress = new Uri("http://localhost") });

        var services = await client.Security.ListVulnerableServicesAsync("Critical");

        services.Should().ContainSingle();
        services[0].CriticalCount.Should().Be(2);
        handler.Requests[0].RequestUri!.PathAndQuery
            .Should().Be("/api/v1/catalog/dependencies/vulnerable?minSeverity=Critical");
    }

    [Fact]
    public async Task ListVulnerableServicesAsync_Without_Severity_Omits_Query()
    {
        var handler = MockHttpMessageHandler.WithJsonResponse("[]");
        using var client = new NexTraceSdkClient(new HttpClient(handler) { BaseAddress = new Uri("http://localhost") });

        var services = await client.Security.ListVulnerableServicesAsync();

        services.Should().BeEmpty();
        handler.Requests[0].RequestUri!.PathAndQuery
            .Should().Be("/api/v1/catalog/dependencies/vulnerable");
    }

    [Fact]
    public async Task GetDependencyHealthAsync_Throws_On_Empty_ServiceId()
    {
        var handler = MockHttpMessageHandler.WithJsonResponse("{}");
        using var client = new NexTraceSdkClient(new HttpClient(handler) { BaseAddress = new Uri("http://localhost") });

        var act = async () => await client.Security.GetDependencyHealthAsync("");

        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
