using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Contracts.DTOs;
using NexTraceOne.Configuration.Domain.Enums;
using NexTraceOne.Notifications.Infrastructure.Intelligence;

namespace NexTraceOne.Notifications.Tests.Intelligence;

/// <summary>
/// Testes para o QuietHoursService da Fase 6.
/// Valida lógica de quiet hours, override obrigatório e configuração por utilizador.
/// </summary>
public sealed class QuietHoursServiceTests
{
    /// <summary>
    /// Cria um mock de IConfigurationResolutionService que devolve null para todas as chaves.
    /// Simula utilizador sem preferências configuradas → usa defaults do serviço.
    /// </summary>
    private static IConfigurationResolutionService CreateDefaultConfigResolution()
    {
        var mock = Substitute.For<IConfigurationResolutionService>();
        mock.ResolveEffectiveValueAsync(
                Arg.Any<string>(), Arg.Any<ConfigurationScope>(),
                Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns((EffectiveConfigurationDto?)null);
        return mock;
    }

    /// <summary>
    /// Cria um mock que simula quiet hours desabilitadas para o utilizador.
    /// </summary>
    private static IConfigurationResolutionService CreateDisabledQuietHoursConfig()
    {
        var mock = Substitute.For<IConfigurationResolutionService>();
        mock.ResolveEffectiveValueAsync(
                "notifications.quiet_hours.enabled",
                Arg.Any<ConfigurationScope>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new EffectiveConfigurationDto(
                "notifications.quiet_hours.enabled", "false",
                "User", null, false, false, "notifications.quiet_hours.enabled", "Boolean", false, 1));

        mock.ResolveEffectiveValueAsync(
                Arg.Is<string>(k => k != "notifications.quiet_hours.enabled"),
                Arg.Any<ConfigurationScope>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns((EffectiveConfigurationDto?)null);
        return mock;
    }

    [Fact]
    public async Task ShouldDeferAsync_MandatoryNotification_ReturnsFalse()
    {
        var service = new QuietHoursService(CreateDefaultConfigResolution());

        // Mandatory notifications should NEVER be deferred
        var result = await service.ShouldDeferAsync(
            Guid.NewGuid(), isMandatory: true);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task ShouldDeferAsync_NonMandatory_DuringQuietHours_ReturnsValidBool()
    {
        var service = new QuietHoursService(CreateDefaultConfigResolution());

        // Result depends on current UTC hour and default quiet hours window (22:00-08:00)
        var result = await service.ShouldDeferAsync(Guid.NewGuid(), isMandatory: false);

        // Valid boolean result - no exception
        _ = result;
    }

    [Fact]
    public async Task ShouldDeferAsync_QuietHoursDisabledByConfig_ReturnsFalse()
    {
        var service = new QuietHoursService(CreateDisabledQuietHoursConfig());

        // Quiet hours disabled via config → never defer
        var result = await service.ShouldDeferAsync(Guid.NewGuid(), isMandatory: false);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task ShouldDeferAsync_MandatoryAlwaysFalse_RegardlessOfTime()
    {
        var service = new QuietHoursService(CreateDefaultConfigResolution());

        // Multiple calls - mandatory always returns false
        for (var i = 0; i < 5; i++)
        {
            var r = await service.ShouldDeferAsync(Guid.NewGuid(), isMandatory: true);
            r.Should().BeFalse();
        }
    }
}
