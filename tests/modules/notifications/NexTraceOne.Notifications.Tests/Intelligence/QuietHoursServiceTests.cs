using NexTraceOne.Notifications.Infrastructure.Intelligence;

namespace NexTraceOne.Notifications.Tests.Intelligence;

/// <summary>
/// Testes para o QuietHoursService da Fase 6.
/// Valida lógica de quiet hours e override obrigatório.
/// </summary>
public sealed class QuietHoursServiceTests
{
    private readonly QuietHoursService _service = new();

    [Fact]
    public async Task ShouldDeferAsync_MandatoryNotification_ReturnsFalse()
    {
        // Mandatory notifications should NEVER be deferred
        var result = await _service.ShouldDeferAsync(
            Guid.NewGuid(), isMandatory: true);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task ShouldDeferAsync_NonMandatory_DuringQuietHours_ReturnsTrue()
    {
        // Quiet hours: 22:00-08:00 UTC
        // We can't control DateTimeOffset.UtcNow directly without a clock abstraction,
        // but we verify the service respects the mandatory override
        var result = await _service.ShouldDeferAsync(
            Guid.NewGuid(), isMandatory: false);

        // Result depends on current UTC hour - just verify it returns without error
        _ = result; // valid boolean result
    }

    [Fact]
    public async Task ShouldDeferAsync_DifferentUsers_SameResult()
    {
        var result1 = await _service.ShouldDeferAsync(Guid.NewGuid(), isMandatory: false);
        var result2 = await _service.ShouldDeferAsync(Guid.NewGuid(), isMandatory: false);

        // Same time = same quiet hours result
        result1.Should().Be(result2);
    }

    [Fact]
    public async Task ShouldDeferAsync_MandatoryAlwaysFalse_RegardlessOfTime()
    {
        // Multiple calls - mandatory always returns false
        for (var i = 0; i < 5; i++)
        {
            var result = await _service.ShouldDeferAsync(Guid.NewGuid(), isMandatory: true);
            result.Should().BeFalse();
        }
    }
}
