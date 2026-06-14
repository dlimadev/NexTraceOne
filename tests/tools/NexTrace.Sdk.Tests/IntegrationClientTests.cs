using System.Net;
using System.Text.Json;
using FluentAssertions;
using NexTrace.Sdk.Clients;

namespace NexTrace.Sdk.Tests;

public class IntegrationClientTests
{
    [Fact]
    public async Task SearchServicesAsync_Returns_Matches()
    {
        var handler = MockHttpMessageHandler.WithJsonResponse("""
        {
            "items": [
                { "serviceId": "11111111-1111-1111-1111-111111111111", "name": "payments-api" },
                { "serviceId": "22222222-2222-2222-2222-222222222222", "name": "payments-mock" }
            ]
        }
        """);
        using var client = new NexTraceSdkClient(new HttpClient(handler) { BaseAddress = new Uri("http://localhost") });

        var results = await client.Integrations.SearchServicesAsync("payments", CancellationToken.None);

        results.Should().HaveCount(2);
        handler.Requests[0].RequestUri!.PathAndQuery.Should().Be("/api/v1/catalog/services/search?q=payments");
    }

    [Fact]
    public async Task GenerateConsumerClientAsync_Orchestrates_Generation()
    {
        var handler = new MockHttpMessageHandler(async (request, ct) =>
        {
            var path = request.RequestUri!.PathAndQuery;
            var json = path switch
            {
                "/api/v1/catalog/services/search?q=payments-api" => """
                    {"items":[{"serviceId":"11111111-1111-1111-1111-111111111111","name":"payments-api"}]}
                    """,
                "/api/v1/catalog/services/11111111-1111-1111-1111-111111111111" => """
                    {"serviceId":"11111111-1111-1111-1111-111111111111","name":"payments-api","apis":[{"apiId":"aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa","name":"Payments API"}]}
                    """,
                "/api/v1/contracts/by-service/11111111-1111-1111-1111-111111111111" => """
                    {"contracts":[{"versionId":"cccccccc-cccc-cccc-cccc-cccccccccccc","apiAssetId":"aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa","apiName":"Payments API","semVer":"1.0.0","protocol":"REST","lifecycleState":"Active"}]}
                    """,
                "/api/v1/contracts/cccccccc-cccc-cccc-cccc-cccccccccccc/detail" => """
                    {"id":"cccccccc-cccc-cccc-cccc-cccccccccccc","apiAssetId":"aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa","semVer":"1.0.0","specContent":"openapi: 3.0.0"}
                    """,
                "/api/v1/contracts/generate-code" => """
                    {"serviceName":"orders-consumer","title":"Payments API","schemaCount":2,"operationCount":3,"files":[{"path":"src/OrdersConsumer.Contracts/PaymentDto.cs","content":"// dto"}]}
                    """,
                _ => "{}"
            };

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = MockHttpMessageHandler.CreateJsonContent(json)
            };
        });

        using var client = new NexTraceSdkClient(new HttpClient(handler) { BaseAddress = new Uri("http://localhost") });

        var result = await client.Integrations.GenerateConsumerClientAsync(new GenerateConsumerClientRequest
        {
            ProviderName = "payments-api",
            ConsumerName = "orders-consumer",
            RootNamespace = "OrdersConsumer"
        }, CancellationToken.None);

        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.ProviderServiceId.Should().Be("11111111-1111-1111-1111-111111111111");
        result.TotalFiles.Should().Be(1);
        result.TotalOperations.Should().Be(3);
    }

    [Fact]
    public async Task GenerateConsumerClientAsync_WhenProviderNotFound_Returns_Error()
    {
        var handler = MockHttpMessageHandler.WithJsonResponse("""{"items":[]}""");
        using var client = new NexTraceSdkClient(new HttpClient(handler) { BaseAddress = new Uri("http://localhost") });

        var result = await client.Integrations.GenerateConsumerClientAsync(new GenerateConsumerClientRequest
        {
            ProviderName = "unknown-api",
            ConsumerName = "orders-consumer"
        }, CancellationToken.None);

        result.Should().NotBeNull();
        result!.Success.Should().BeFalse();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task RegisterConsumerAsync_Sends_Post_And_Returns_Relationship()
    {
        var handler = new MockHttpMessageHandler(async (request, ct) =>
        {
            request.RequestUri!.PathAndQuery.Should().Be("/api/v1/catalog/apis/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/consumers");
            request.Method.Should().Be(HttpMethod.Post);

            var body = await request.Content!.ReadAsStringAsync(ct);
            body.Should().Contain("orders-consumer");

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = MockHttpMessageHandler.CreateJsonContent("""
                {
                    "relationshipId": "rrrrrrrr-rrrr-rrrr-rrrr-rrrrrrrrrrrr",
                    "apiAssetId": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
                    "consumerName": "orders-consumer",
                    "sourceType": "cli",
                    "confidenceScore": 0.95
                }
                """)
            };
        });

        using var client = new NexTraceSdkClient(new HttpClient(handler) { BaseAddress = new Uri("http://localhost") });

        var result = await client.Integrations.RegisterConsumerAsync(new RegisterConsumerRequest
        {
            ApiAssetId = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
            ConsumerName = "orders-consumer",
            ConsumerKind = "Service",
            ConsumerEnvironment = "Production",
            SourceType = "cli",
            ExternalReference = "nex integration register",
            ConfidenceScore = 0.95m
        }, CancellationToken.None);

        result.Should().NotBeNull();
        result!.ConsumerName.Should().Be("orders-consumer");
        result.ConfidenceScore.Should().Be(0.95m);
    }

    [Fact]
    public async Task GenerateConsumerClientAsync_Applies_Routes_Filter()
    {
        var handler = new MockHttpMessageHandler(async (request, ct) =>
        {
            var path = request.RequestUri!.PathAndQuery;
            if (path == "/api/v1/contracts/generate-code")
            {
                var body = await request.Content!.ReadAsStringAsync(ct);
                body.Should().NotContain("/api/v1/admin");
                body.Should().Contain("/api/v1/payments");
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = MockHttpMessageHandler.CreateJsonContent("""
                    {"serviceName":"orders-consumer","title":"Payments API","schemaCount":1,"operationCount":1,"files":[]}
                    """)
                };
            }

            var json = path switch
            {
                "/api/v1/catalog/services/search?q=payments-api" => """
                    {"items":[{"serviceId":"11111111-1111-1111-1111-111111111111","name":"payments-api"}]}
                    """,
                "/api/v1/catalog/services/11111111-1111-1111-1111-111111111111" => """
                    {"serviceId":"11111111-1111-1111-1111-111111111111","name":"payments-api","apis":[]}
                    """,
                "/api/v1/contracts/by-service/11111111-1111-1111-1111-111111111111" => """
                    {"contracts":[{"versionId":"cccccccc-cccc-cccc-cccc-cccccccccccc","apiAssetId":"aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa","apiName":"Payments API","semVer":"1.0.0","protocol":"REST","lifecycleState":"Active"}]}
                    """,
                "/api/v1/contracts/cccccccc-cccc-cccc-cccc-cccccccccccc/detail" => """
                    {"id":"cccccccc-cccc-cccc-cccc-cccccccccccc","apiAssetId":"aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa","semVer":"1.0.0","specContent":"openapi: 3.0.0\ninfo:\n  title: Payments API\n  version: 1.0.0\npaths:\n  /api/v1/payments:\n    get:\n      operationId: listPayments\n      responses:\n        '200':\n          description: OK\n  /api/v1/admin:\n    get:\n      operationId: adminHealth\n      responses:\n        '200':\n          description: OK\n"}
                    """,
                _ => "{}"
            };

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = MockHttpMessageHandler.CreateJsonContent(json)
            };
        });

        using var client = new NexTraceSdkClient(new HttpClient(handler) { BaseAddress = new Uri("http://localhost") });

        var result = await client.Integrations.GenerateConsumerClientAsync(new GenerateConsumerClientRequest
        {
            ProviderName = "payments-api",
            ConsumerName = "orders-consumer",
            Routes = ["api/v1/payments"]
        }, CancellationToken.None);

        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
    }

    [Fact]
    public async Task GetImpactAsync_Returns_AffectedNodes()
    {
        var handler = MockHttpMessageHandler.WithJsonResponse("""
        {
            "rootNodeId": "11111111-1111-1111-1111-111111111111",
            "affectedNodes": [
                { "nodeId": "22222222-2222-2222-2222-222222222222", "name": "orders-consumer", "kind": "Service", "depth": 1 }
            ],
            "totalAffected": 1
        }
        """);
        using var client = new NexTraceSdkClient(new HttpClient(handler) { BaseAddress = new Uri("http://localhost") });

        var impact = await client.Integrations.GetImpactAsync("11111111-1111-1111-1111-111111111111", 3, CancellationToken.None);

        impact.Should().NotBeNull();
        impact!.TotalAffected.Should().Be(1);
        handler.Requests[0].RequestUri!.PathAndQuery.Should().Be("/api/v1/catalog/impact/11111111-1111-1111-1111-111111111111?maxDepth=3");
    }
}
