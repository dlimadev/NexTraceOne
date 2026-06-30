using FluentAssertions;
using NexTrace.Sdk.Clients;

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

    [Fact]
    public async Task SignArtifactAsync_Sends_Post_And_Returns_Signed()
    {
        var handler = MockHttpMessageHandler.WithJsonResponse("""
        {
            "artifactId": "art-1",
            "artifactName": "payments:1.2.3",
            "checksum": "sha256:abc",
            "signature": "MEUCIQ...",
            "signedAt": "2026-06-30T10:00:00Z",
            "signerIdentity": "ci@nextraceone",
            "sbomJson": "{}",
            "transparencyLogEntry": "rekor-123"
        }
        """);

        using var client = new NexTraceSdkClient(new HttpClient(handler) { BaseAddress = new Uri("http://localhost") });

        var signed = await client.Security.SignArtifactAsync(new SignArtifactRequest
        {
            ArtifactPath = "payments:1.2.3",
            ArtifactType = "docker-image",
            Version = "1.2.3"
        });

        signed.Should().NotBeNull();
        signed!.ArtifactId.Should().Be("art-1");
        signed.SignerIdentity.Should().Be("ci@nextraceone");
        handler.Requests[0].Method.Should().Be(HttpMethod.Post);
        handler.Requests[0].RequestUri!.PathAndQuery.Should().Be("/api/v1/governance/artifact-signing/sign");
    }

    [Fact]
    public async Task VerifyArtifactAsync_Returns_Verification()
    {
        var handler = MockHttpMessageHandler.WithJsonResponse("""
        {
            "isValid": false,
            "artifactId": "art-1",
            "verifiedAt": "2026-06-30T10:00:00Z",
            "signerIdentity": "",
            "errors": ["signature mismatch"],
            "warnings": []
        }
        """);

        using var client = new NexTraceSdkClient(new HttpClient(handler) { BaseAddress = new Uri("http://localhost") });

        var result = await client.Security.VerifyArtifactAsync("art-1");

        result.Should().NotBeNull();
        result!.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("signature mismatch");
        handler.Requests[0].Method.Should().Be(HttpMethod.Post);
        handler.Requests[0].RequestUri!.PathAndQuery.Should().Be("/api/v1/governance/artifact-signing/verify");
    }

    [Fact]
    public async Task VerifyArtifactAsync_Throws_On_Empty_Id()
    {
        var handler = MockHttpMessageHandler.WithJsonResponse("{}");
        using var client = new NexTraceSdkClient(new HttpClient(handler) { BaseAddress = new Uri("http://localhost") });

        var act = async () => await client.Security.VerifyArtifactAsync("");

        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
