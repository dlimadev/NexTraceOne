using FluentAssertions;
using NexTraceOne.BuildingBlocks.Domain.Results;

namespace NexTraceOne.BuildingBlocks.Domain.Tests.Results;

/// <summary>
/// Testes para o record Error com suporte a i18n.
/// </summary>
public sealed class ErrorTests
{
    [Fact]
    public void NotFound_Should_StoreMessageArgs_When_ArgumentsAreProvided()
    {
        var error = Error.NotFound("Identity.User.NotFound", "User '{0}' was not found.", "alice@example.com");

        error.MessageArgs.Should().ContainSingle().Which.Should().Be("alice@example.com");
    }

    [Fact]
    public void FormattedMessage_Should_FormatTemplate_When_ArgumentsAreProvided()
    {
        var error = Error.Validation("Validation.Failed", "Validation failed for {0}.", "Email");

        error.FormattedMessage.Should().Be("Validation failed for Email.");
    }

    [Fact]
    public void FormattedMessage_Should_ReturnOriginalMessage_When_NoArgumentsAreProvided()
    {
        var error = Error.Business("Identity.InvalidCredentials", "Invalid credentials.");

        error.FormattedMessage.Should().Be("Invalid credentials.");
    }
}
