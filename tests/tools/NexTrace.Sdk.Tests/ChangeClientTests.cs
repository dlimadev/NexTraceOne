using System.Net;
using System.Text.Json;
using FluentAssertions;
using NexTrace.Sdk.Clients;

namespace NexTrace.Sdk.Tests;

public class ChangeClientTests
{
    [Fact]
    public async Task GetConfidenceScoreAsync_Returns_Score()
    {
        var handler = MockHttpMessageHandler.WithJsonResponse("""
        {
            "releaseId": "rel-1",
            "score": 87.5,
            "tier": "A",
            "recommendation": "go"
        }
        """);
        using var client = new NexTraceSdkClient(new HttpClient(handler) { BaseAddress = new Uri("http://localhost") });

        var score = await client.Changes.GetConfidenceScoreAsync("rel-1");

        score.Should().NotBeNull();
        score!.Score.Should().Be(87.5);
        score.Tier.Should().Be("A");
    }

    [Fact]
    public async Task RequestPromotionAsync_Sends_Post()
    {
        var handler = new MockHttpMessageHandler(async (request, ct) =>
        {
            var body = await request.Content!.ReadAsStringAsync(ct);
            var payload = JsonSerializer.Deserialize<PromotionRequestRequest>(body);
            payload!.ReleaseId.Should().Be("rel-1");

            return new HttpResponseMessage(HttpStatusCode.Accepted)
            {
                Content = MockHttpMessageHandler.CreateJsonContent(JsonSerializer.Serialize(new PromotionRequest
                {
                    Id = "promo-1",
                    ReleaseId = payload.ReleaseId,
                    TargetEnvironment = payload.TargetEnvironment,
                    Status = "pending"
                }))
            };
        });
        using var client = new NexTraceSdkClient(new HttpClient(handler) { BaseAddress = new Uri("http://localhost") });

        var result = await client.Changes.RequestPromotionAsync(new PromotionRequestRequest
        {
            ReleaseId = "rel-1",
            TargetEnvironment = "production",
            Justification = "hotfix"
        });

        result.Should().NotBeNull();
        result!.Status.Should().Be("pending");
    }
}
