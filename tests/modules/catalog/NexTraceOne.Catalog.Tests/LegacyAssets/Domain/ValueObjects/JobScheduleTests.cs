using NexTraceOne.Catalog.Domain.LegacyAssets.ValueObjects;

namespace NexTraceOne.Catalog.Tests.LegacyAssets.Domain.ValueObjects;

/// <summary>
/// Testes do value object JobSchedule do sub-domínio Legacy Assets.
/// Cobre criação, validação de invariantes e valores opcionais.
/// </summary>
public sealed class JobScheduleTests
{
    [Fact]
    public void Create_WithValidInput_ShouldSucceed()
    {
        var schedule = JobSchedule.Create("DAILY", "Every 24h", "00:00-06:00", "0 0 * * *");

        schedule.ScheduleType.Should().Be("DAILY");
        schedule.Frequency.Should().Be("Every 24h");
        schedule.Window.Should().Be("00:00-06:00");
        schedule.CronExpression.Should().Be("0 0 * * *");
    }

    [Fact]
    public void Create_WithNullScheduleType_ShouldThrow()
    {
        var act = () => JobSchedule.Create(null!);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithOptionalValues_ShouldDefaultToEmpty()
    {
        var schedule = JobSchedule.Create("WEEKLY");

        schedule.Frequency.Should().BeEmpty();
        schedule.Window.Should().BeEmpty();
        schedule.CronExpression.Should().BeEmpty();
    }

    [Fact]
    public void Equality_SameValues_ShouldBeEqual()
    {
        var s1 = JobSchedule.Create("DAILY", "Every 24h", "00:00-06:00", "0 0 * * *");
        var s2 = JobSchedule.Create("DAILY", "Every 24h", "00:00-06:00", "0 0 * * *");

        s1.Should().Be(s2);
    }
}
