using FluentAssertions;
using Microsoft.Extensions.Localization;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Core.Results;
using NSubstitute;

namespace NexTraceOne.BuildingBlocks.Application.Tests.Localization;

/// <summary>
/// Testes para a localização de erros e títulos compartilhados.
/// </summary>
public sealed class ErrorLocalizerTests
{
    [Fact]
    public void Localize_Should_ReturnLocalizedMessage_When_ResourceExists()
    {
        var stringLocalizer = Substitute.For<IStringLocalizer<SharedMessages>>();
        stringLocalizer["Identity.User.NotFound"].Returns(new LocalizedString("Identity.User.NotFound", "User '{0}' was not found.", false));
        var sut = new ErrorLocalizer(stringLocalizer);
        var error = Error.NotFound("Identity.User.NotFound", "Fallback '{0}'", "alice@example.com");

        var message = sut.Localize(error);

        message.Should().Be("User 'alice@example.com' was not found.");
    }

    [Fact]
    public void Localize_Should_ReturnFormattedFallback_When_ResourceDoesNotExist()
    {
        var stringLocalizer = Substitute.For<IStringLocalizer<SharedMessages>>();
        stringLocalizer["Identity.User.NotFound"].Returns(new LocalizedString("Identity.User.NotFound", "Identity.User.NotFound", true));
        var sut = new ErrorLocalizer(stringLocalizer);
        var error = Error.NotFound("Identity.User.NotFound", "User '{0}' was not found.", "alice@example.com");

        var message = sut.Localize(error);

        message.Should().Be("User 'alice@example.com' was not found.");
    }

    [Fact]
    public void LocalizeTitle_Should_ReturnLocalizedTitle_When_ResourceExists()
    {
        var stringLocalizer = Substitute.For<IStringLocalizer<SharedMessages>>();
        stringLocalizer["Title.Validation"].Returns(new LocalizedString("Title.Validation", "Falha de validação", false));
        var sut = new ErrorLocalizer(stringLocalizer);

        var title = sut.LocalizeTitle(ErrorType.Validation);

        title.Should().Be("Falha de validação");
    }
}
