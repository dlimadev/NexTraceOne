using FluentAssertions;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.BuildingBlocks.Core.Tests.Results;

/// <summary>
/// Testes para o Result{T} pattern.
/// </summary>
public sealed class ResultTests
{
    [Fact]
    public void Success_Should_SetIsSuccess_When_ValueProvided()
    {
        var result = Result<int>.Success(42);

        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.Value.Should().Be(42);
    }

    [Fact]
    public void ImplicitError_Should_SetIsFailure_When_ErrorProvided()
    {
        Result<int> result = Error.NotFound("Test.NotFound", "Not found");

        result.IsFailure.Should().BeTrue();
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Test.NotFound");
    }

    [Fact]
    public void Value_Should_Throw_When_ResultIsFailure()
    {
        Result<string> result = Error.Business("Test.Business", "Business error");

        var action = () => _ = result.Value;
        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Error_Should_Throw_When_ResultIsSuccess()
    {
        var result = Result<string>.Success("ok");

        var action = () => _ = result.Error;
        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Map_Should_TransformValue_When_ResultIsSuccess()
    {
        var result = Result<int>.Success(5);

        var mapped = result.Map(v => v * 2);

        mapped.IsSuccess.Should().BeTrue();
        mapped.Value.Should().Be(10);
    }

    [Fact]
    public void Map_Should_PropagateError_When_ResultIsFailure()
    {
        Result<int> result = Error.Validation("Test.Validation", "Invalid");

        var mapped = result.Map(v => v * 2);

        mapped.IsFailure.Should().BeTrue();
        mapped.Error.Code.Should().Be("Test.Validation");
    }

    [Fact]
    public void OnSuccess_Should_ExecuteAction_When_ResultIsSuccess()
    {
        var result = Result<int>.Success(7);
        var called = false;

        result.OnSuccess(_ => called = true);

        called.Should().BeTrue();
    }

    [Fact]
    public void OnFailure_Should_ExecuteAction_When_ResultIsFailure()
    {
        Result<int> result = Error.Forbidden("Test.Forbidden", "Forbidden");
        var called = false;

        result.OnFailure(_ => called = true);

        called.Should().BeTrue();
    }
}
