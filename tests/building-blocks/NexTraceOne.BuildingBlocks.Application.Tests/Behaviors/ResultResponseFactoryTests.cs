using NexTraceOne.BuildingBlocks.Application.Behaviors;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.BuildingBlocks.Application.Tests.Behaviors;

/// <summary>
/// Testes unitários para <see cref="ResultResponseFactory"/>.
/// Valida a criação de respostas de falha via reflection, a verificação de sucesso
/// e a extração de estado de objetos <see cref="Result{T}"/>.
/// </summary>
public sealed class ResultResponseFactoryTests
{
    [Fact]
    public void CreateFailureResponse_Should_ReturnFailureResult_When_ErrorProvided()
    {
        // Arrange
        var error = Error.Validation("test.error", "Test error message");

        // Act
        var result = ResultResponseFactory.CreateFailureResponse<Result<string>>(error);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("test.error");
        result.Error.Message.Should().Be("Test error message");
    }

    [Fact]
    public void CreateFailureResponse_Should_ThrowInvalidOperationException_When_ResponseTypeIsNotResult()
    {
        // Arrange
        var error = Error.Validation("test.error", "Test error message");

        // Act
        var act = () => ResultResponseFactory.CreateFailureResponse<string>(error);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*must be a Result<T>*");
    }

    [Fact]
    public void IsSuccessfulResult_Should_ReturnTrue_When_ResultIsSuccess()
    {
        // Arrange
        Result<string> result = Result<string>.Success("ok");

        // Act
        var isSuccessful = ResultResponseFactory.IsSuccessfulResult(result);

        // Assert
        isSuccessful.Should().BeTrue();
    }

    [Fact]
    public void IsSuccessfulResult_Should_ReturnFalse_When_ResultIsFailure()
    {
        // Arrange
        Result<string> result = Error.Validation("fail", "Failure");

        // Act
        var isSuccessful = ResultResponseFactory.IsSuccessfulResult(result);

        // Assert
        isSuccessful.Should().BeFalse();
    }

    [Fact]
    public void IsSuccessfulResult_Should_ReturnTrue_When_ResponseTypeIsNotResult()
    {
        // Arrange — tipo string não é Result<T>, deve assumir sucesso
        var response = "plain string";

        // Act
        var isSuccessful = ResultResponseFactory.IsSuccessfulResult(response);

        // Assert
        isSuccessful.Should().BeTrue();
    }

    [Fact]
    public void TryGetResultState_Should_ReturnTrue_When_ResponseIsResult()
    {
        // Arrange
        Result<string> result = Result<string>.Success("value");

        // Act
        var extracted = ResultResponseFactory.TryGetResultState(result, out var isSuccess, out var error);

        // Assert
        extracted.Should().BeTrue();
        isSuccess.Should().BeTrue();
        error.Should().BeNull();
    }

    [Fact]
    public void TryGetResultState_Should_ReturnFalse_When_ResponseIsNotResult()
    {
        // Arrange — tipo int não é Result<T>
        var response = 42;

        // Act
        var extracted = ResultResponseFactory.TryGetResultState(response, out var isSuccess, out var error);

        // Assert
        extracted.Should().BeFalse();
        isSuccess.Should().BeFalse();
        error.Should().BeNull();
    }

    [Fact]
    public void TryGetResultState_Should_ExtractError_When_ResultIsFailed()
    {
        // Arrange
        var expectedError = Error.NotFound("entity.notfound", "Entity not found");
        Result<string> result = expectedError;

        // Act
        var extracted = ResultResponseFactory.TryGetResultState(result, out var isSuccess, out var error);

        // Assert
        extracted.Should().BeTrue();
        isSuccess.Should().BeFalse();
        error.Should().NotBeNull();
        error!.Code.Should().Be("entity.notfound");
        error.Type.Should().Be(ErrorType.NotFound);
    }
}
