using System.Net;
using System.Text.Json;
using FluentAssertions;
using NexTrace.Sdk.Clients;

namespace NexTrace.Sdk.Tests;

public class ContractClientTests
{
    [Fact]
    public async Task DiffContractAsync_Returns_Diff()
    {
        var handler = MockHttpMessageHandler.WithJsonResponse("""
        {
            "fromVersion": "1.0.0",
            "toVersion": "2.0.0",
            "hasBreakingChanges": true,
            "summary": "Removed /users/{id}"
        }
        """);
        using var client = new NexTraceSdkClient(new HttpClient(handler) { BaseAddress = new Uri("http://localhost") });

        var diff = await client.Contracts.DiffContractAsync("contract-a", "contract-b");

        diff.Should().NotBeNull();
        diff!.HasBreakingChanges.Should().BeTrue();
        handler.Requests[0].RequestUri!.PathAndQuery.Should().Be("/api/v1/contracts/diff?from=contract-a&to=contract-b");
    }

    [Fact]
    public async Task VerifyContractAsync_Sends_Post_And_Returns_Result()
    {
        var handler = new MockHttpMessageHandler(async (request, ct) =>
        {
            var body = await request.Content!.ReadAsStringAsync(ct);
            body.Should().Contain("contract-1");
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = MockHttpMessageHandler.CreateJsonContent("""
                {
                    "contractId": "contract-1",
                    "isValid": false,
                    "violations": ["missing field"]
                }
                """)
            };
        });
        using var client = new NexTraceSdkClient(new HttpClient(handler) { BaseAddress = new Uri("http://localhost") });

        var result = await client.Contracts.VerifyContractAsync(new VerifyContractRequest
        {
            ContractId = "contract-1",
            SpecContent = "openapi: 3.0.0"
        });

        result.Should().NotBeNull();
        result!.IsValid.Should().BeFalse();
        handler.Requests[0].Method.Should().Be(HttpMethod.Post);
    }

    [Fact]
    public async Task MigrationPatchAsync_Returns_Patch()
    {
        var handler = MockHttpMessageHandler.WithJsonResponse("""
        {
            "fromVersion": "1.0.0",
            "toVersion": "2.0.0",
            "patch": "--- a/users.yaml\\n+++ b/users.yaml",
            "hasBreakingChanges": true
        }
        """);
        using var client = new NexTraceSdkClient(new HttpClient(handler) { BaseAddress = new Uri("http://localhost") });

        var patch = await client.Contracts.MigrationPatchAsync("from-id", "to-id");

        patch.Should().NotBeNull();
        patch!.HasBreakingChanges.Should().BeTrue();
    }
}
