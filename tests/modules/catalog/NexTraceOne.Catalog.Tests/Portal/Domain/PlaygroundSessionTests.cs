using NexTraceOne.Catalog.Domain.Portal.Entities;

namespace NexTraceOne.Catalog.Tests.Portal.Domain;

/// <summary>
/// Testes de domínio para o aggregate PlaygroundSession.
/// Valida criação com dados válidos e atribuição correta de todas as propriedades.
/// </summary>
public sealed class PlaygroundSessionTests
{
    private static readonly DateTimeOffset Now = new(2025, 06, 15, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_Should_ReturnSession_When_InputIsValid()
    {
        var apiAssetId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var session = PlaygroundSession.Create(
            apiAssetId,
            "Payments API",
            userId,
            "POST",
            "/api/payments",
            """{"amount":100,"currency":"EUR"}""",
            """{"Content-Type":"application/json"}""",
            201,
            """{"id":"pay-001","status":"created"}""",
            durationMs: 245,
            Now);

        session.ApiAssetId.Should().Be(apiAssetId);
        session.ApiName.Should().Be("Payments API");
        session.UserId.Should().Be(userId);
        session.HttpMethod.Should().Be("POST");
        session.RequestPath.Should().Be("/api/payments");
        session.RequestBody.Should().Contain("amount");
        session.RequestHeaders.Should().Contain("application/json");
        session.ResponseStatusCode.Should().Be(201);
        session.ResponseBody.Should().Contain("pay-001");
        session.DurationMs.Should().Be(245);
        session.Environment.Should().Be("sandbox");
        session.ExecutedAt.Should().Be(Now);
    }

    [Fact]
    public void Create_Should_AcceptNullableFields()
    {
        var session = PlaygroundSession.Create(
            Guid.NewGuid(),
            "Catalog API",
            Guid.NewGuid(),
            "GET",
            "/api/catalog",
            requestBody: null,
            requestHeaders: null,
            200,
            responseBody: null,
            durationMs: 50,
            Now);

        session.RequestBody.Should().BeNull();
        session.RequestHeaders.Should().BeNull();
        session.ResponseBody.Should().BeNull();
        session.Environment.Should().Be("sandbox");
    }

    [Fact]
    public void Create_Should_AlwaysSetSandboxEnvironment()
    {
        var session = PlaygroundSession.Create(
            Guid.NewGuid(),
            "Users API",
            Guid.NewGuid(),
            "DELETE",
            "/api/users/123",
            null,
            null,
            204,
            null,
            durationMs: 30,
            Now);

        session.Environment.Should().Be("sandbox");
    }
}
