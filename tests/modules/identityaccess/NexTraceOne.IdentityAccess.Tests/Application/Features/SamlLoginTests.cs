using MediatR;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Application.Features.FederatedLogin;

using LocalLoginFeature = NexTraceOne.IdentityAccess.Application.Features.LocalLogin.LocalLogin;
using StartSamlLoginFeature = NexTraceOne.IdentityAccess.Application.Features.StartSamlLogin.StartSamlLogin;
using SamlAcsCallbackFeature = NexTraceOne.IdentityAccess.Application.Features.SamlAcsCallback.SamlAcsCallback;

namespace NexTraceOne.IdentityAccess.Tests.Application.Features;

/// <summary>
/// Testes unitários para os handlers StartSamlLogin e SamlAcsCallback.
/// Cobrem cenários de sucesso, configuração em falta e resposta SAML inválida.
/// </summary>
public sealed class SamlLoginTests
{
    [Fact]
    public async Task StartSamlLogin_Should_Return_RedirectUrl_When_Config_Is_Present()
    {
        // Arrange
        var config = new SamlSsoConfig(
            EntityId: "https://nextraceone.example.com",
            SsoUrl: "https://idp.example.com/sso",
            SloUrl: "https://idp.example.com/slo",
            IdpCertificate: "CERT_PEM",
            JitProvisioningEnabled: true,
            DefaultRole: "Viewer");

        var configProvider = Substitute.For<ISamlConfigProvider>();
        var samlService = Substitute.For<ISamlService>();
        var clock = Substitute.For<IDateTimeProvider>();

        configProvider.GetActiveConfigAsync(Arg.Any<CancellationToken>())
            .Returns(config);
        samlService.BuildAuthnRequestUrl(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<string>(), Arg.Any<string>())
            .Returns("https://idp.example.com/sso?SAMLRequest=deflated&RelayState=%2F");

        var handler = new StartSamlLoginFeature.Handler(configProvider, samlService, clock);

        // Act
        var result = await handler.Handle(new StartSamlLoginFeature.Query(), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.RedirectUrl.Should().StartWith("https://idp.example.com/sso");
        result.Value.RequestId.Should().StartWith("_");
        samlService.Received(1).BuildAuthnRequestUrl(
            config.SsoUrl, config.EntityId, "/auth/saml/acs",
            Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task StartSamlLogin_Should_Return_Failure_When_Config_Is_Missing()
    {
        // Arrange
        var configProvider = Substitute.For<ISamlConfigProvider>();
        var samlService = Substitute.For<ISamlService>();
        var clock = Substitute.For<IDateTimeProvider>();

        configProvider.GetActiveConfigAsync(Arg.Any<CancellationToken>())
            .Returns((SamlSsoConfig?)null);

        var handler = new StartSamlLoginFeature.Handler(configProvider, samlService, clock);

        // Act
        var result = await handler.Handle(new StartSamlLoginFeature.Query(), CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.Saml.NotConfigured");
        samlService.DidNotReceive().BuildAuthnRequestUrl(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task SamlAcsCallback_Should_Return_Failure_When_SamlResponse_Is_Empty()
    {
        // Arrange — validar directamente o Validator (pipeline de validação)
        var validator = new SamlAcsCallbackFeature.Validator();

        // Act
        var validationResult = await validator.ValidateAsync(
            new SamlAcsCallbackFeature.Command(string.Empty));

        // Assert
        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(e => e.PropertyName == "SamlResponse");
    }

    [Fact]
    public async Task SamlAcsCallback_Should_Call_FederatedLogin_When_ParseSucceeds()
    {
        // Arrange
        var config = new SamlSsoConfig(
            EntityId: "https://nextraceone.example.com",
            SsoUrl: "https://idp.example.com/sso",
            SloUrl: "https://idp.example.com/slo",
            IdpCertificate: "CERT_PEM",
            JitProvisioningEnabled: true,
            DefaultRole: "Viewer");

        var configProvider = Substitute.For<ISamlConfigProvider>();
        var samlService = Substitute.For<ISamlService>();
        var mediator = Substitute.For<ISender>();

        configProvider.GetActiveConfigAsync(Arg.Any<CancellationToken>())
            .Returns(config);

        var parsedAssertion = new SamlParsedAssertion(
            NameId: "user@idp.example.com",
            Email: "user@example.com",
            Name: "Test User",
            Groups: ["engineers"]);

        samlService.ParseSamlResponse(Arg.Any<string>(), Arg.Any<string>())
            .Returns(parsedAssertion);

        var loginResponse = new LocalLoginFeature.LoginResponse(
            AccessToken: "access-token",
            RefreshToken: "refresh-token",
            ExpiresIn: 3600,
            User: new LocalLoginFeature.UserResponse(
                Guid.NewGuid(), "user@example.com", "Test User",
                Guid.NewGuid(), "Viewer", []));

        mediator.Send(Arg.Any<FederatedLogin.Command>(), Arg.Any<CancellationToken>())
            .Returns(Result<LocalLoginFeature.LoginResponse>.Success(loginResponse));

        var handler = new SamlAcsCallbackFeature.Handler(configProvider, samlService, mediator);
        var command = new SamlAcsCallbackFeature.Command(
            SamlResponse: "base64encodedresponse",
            RelayState: "/dashboard");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.AccessToken.Should().Be("access-token");
        result.Value.ReturnTo.Should().Be("/dashboard");

        await mediator.Received(1).Send(
            Arg.Is<FederatedLogin.Command>(c =>
                c.Provider == "saml" &&
                c.ExternalId == parsedAssertion.NameId &&
                c.Email == parsedAssertion.Email),
            Arg.Any<CancellationToken>());
    }
}
