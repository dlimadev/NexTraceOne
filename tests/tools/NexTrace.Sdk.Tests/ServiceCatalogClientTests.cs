using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using FluentAssertions;
using NexTrace.Sdk.Clients;

namespace NexTrace.Sdk.Tests;

public class ServiceCatalogClientTests
{
    [Fact]
    public async Task GetServiceAsync_Returns_Service_When_Found()
    {
        var handler = MockHttpMessageHandler.WithJsonResponse("""
        {
            "id": "svc-1",
            "name": "payments",
            "team": "payments-team",
            "tier": "critical",
            "status": "active"
        }
        """);

        using var client = new NexTraceSdkClient(new HttpClient(handler) { BaseAddress = new Uri("http://localhost") });

        var service = await client.Services.GetServiceAsync("payments");

        service.Should().NotBeNull();
        service!.Name.Should().Be("payments");
        service.Team.Should().Be("payments-team");
        handler.Requests.Should().ContainSingle();
        handler.Requests[0].Method.Should().Be(HttpMethod.Get);
        handler.Requests[0].RequestUri!.PathAndQuery.Should().Be("/api/v1/catalog/services/payments");
    }

    [Fact]
    public async Task CreateServiceAsync_Sends_Post_With_Correct_Body()
    {
        var handler = new MockHttpMessageHandler(async (request, ct) =>
        {
            var body = await request.Content!.ReadAsStringAsync(ct);
            var payload = JsonSerializer.Deserialize<CreateServiceRequest>(body);

            var response = new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = MockHttpMessageHandler.CreateJsonContent(JsonSerializer.Serialize(new ServiceSummary
                {
                    Id = "svc-2",
                    Name = payload!.Name,
                    Team = payload.Team,
                    Tier = payload.Tier,
                    Status = "active"
                }))
            };
            return response;
        });

        using var client = new NexTraceSdkClient(new HttpClient(handler) { BaseAddress = new Uri("http://localhost") });

        var created = await client.Services.CreateServiceAsync(new CreateServiceRequest
        {
            Name = "orders",
            Team = "orders-team",
            Tier = "standard"
        });

        created.Should().NotBeNull();
        created!.Name.Should().Be("orders");
        handler.Requests.Should().ContainSingle();
        handler.Requests[0].Method.Should().Be(HttpMethod.Post);
    }

    [Fact]
    public async Task DeleteServiceAsync_Sends_Delete()
    {
        var handler = MockHttpMessageHandler.WithJsonResponse("{}", HttpStatusCode.NoContent);
        using var client = new NexTraceSdkClient(new HttpClient(handler) { BaseAddress = new Uri("http://localhost") });

        await client.Services.DeleteServiceAsync("legacy-service");

        handler.Requests.Should().ContainSingle();
        handler.Requests[0].Method.Should().Be(HttpMethod.Delete);
        handler.Requests[0].RequestUri!.PathAndQuery.Should().Be("/api/v1/catalog/services/legacy-service");
    }

    [Fact]
    public async Task ListServicesAsync_Filters_By_Team()
    {
        var handler = MockHttpMessageHandler.WithJsonResponse("""
        [
            { "id": "svc-3", "name": "inventory", "team": "logistics", "tier": "critical", "status": "active" }
        ]
        """);
        using var client = new NexTraceSdkClient(new HttpClient(handler) { BaseAddress = new Uri("http://localhost") });

        var services = await client.Services.ListServicesAsync("logistics");

        services.Should().ContainSingle();
        handler.Requests[0].RequestUri!.PathAndQuery.Should().Be("/api/v1/catalog/services?team=logistics");
    }
}
