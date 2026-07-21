using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Catalog.Application.Portal.Abstractions;
using NexTraceOne.Catalog.Application.Portal.Features.ExecutePlayground;
using NexTraceOne.Catalog.Domain.Portal.Entities;

namespace NexTraceOne.Catalog.Tests.Portal.Application.Features;

/// <summary>
/// Testes unitários para ExecutePlayground — execução sandbox de pedidos do Developer Portal.
/// </summary>
public sealed class ExecutePlaygroundTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 6, 1, 12, 0, 0, TimeSpan.Zero);

    private static IDateTimeProvider CreateClock()
    {
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(FixedNow);
        return clock;
    }

    private static ExecutePlayground.Command ValidCommand() =>
        new(Guid.NewGuid(), "Payments API", Guid.NewGuid(), "GET", "/payments/123",
            RequestBody: null, RequestHeaders: null, Environment: "sandbox");

    [Fact]
    public async Task Execute_ReturnsSimulatedSuccessAndRecordsSession()
    {
        var repo = Substitute.For<IPlaygroundSessionRepository>();
        var handler = new ExecutePlayground.Handler(repo, CreateClock());

        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.StatusCode.Should().Be(200);
        result.Value.ResponseStatusCode.Should().Be(200);
        result.Value.ResponseBody.Should().Contain("sandbox");
        result.Value.ExecutedAt.Should().Be(FixedNow);
        result.Value.DurationMs.Should().BeInRange(20, 100);
        repo.Received(1).Add(Arg.Any<PlaygroundSession>());
    }

    [Fact]
    public async Task Execute_PersistedSessionMatchesRequest()
    {
        PlaygroundSession? captured = null;
        var repo = Substitute.For<IPlaygroundSessionRepository>();
        repo.When(r => r.Add(Arg.Any<PlaygroundSession>())).Do(ci => captured = ci.Arg<PlaygroundSession>());
        var handler = new ExecutePlayground.Handler(repo, CreateClock());
        var command = ValidCommand() with { HttpMethod = "POST", RequestPath = "/orders" };

        await handler.Handle(command, CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.HttpMethod.Should().Be("POST");
        captured.RequestPath.Should().Be("/orders");
        captured.Environment.Should().Be("sandbox");
        captured.ResponseStatusCode.Should().Be(200);
    }

    [Theory]
    [InlineData("ApiName")]
    [InlineData("HttpMethod")]
    [InlineData("RequestPath")]
    public void Validator_EmptyRequiredField_Fails(string field)
    {
        var command = field switch
        {
            "ApiName"     => ValidCommand() with { ApiName = "" },
            "HttpMethod"  => ValidCommand() with { HttpMethod = "" },
            "RequestPath" => ValidCommand() with { RequestPath = "" },
            _             => ValidCommand()
        };

        new ExecutePlayground.Validator().Validate(command).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_EmptyApiAssetId_Fails()
    {
        var command = ValidCommand() with { ApiAssetId = Guid.Empty };
        new ExecutePlayground.Validator().Validate(command).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_ValidCommand_Passes()
    {
        new ExecutePlayground.Validator().Validate(ValidCommand()).IsValid.Should().BeTrue();
    }
}
