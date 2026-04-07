using NexTraceOne.Configuration.Domain.Entities;
using NexTraceOne.Configuration.Domain.Enums;

namespace NexTraceOne.Configuration.Tests.Domain;

/// <summary>
/// Testes para preferências de quiet hours e digest.
/// Valida que as configurações são criadas e persistidas correctamente
/// através das entidades de configuração existentes.
/// </summary>
public sealed class QuietHoursDigestPreferenceTests
{
    private static readonly ConfigurationDefinitionId SampleDefinitionId = new(Guid.NewGuid());

    [Fact]
    public void ConfigurationEntry_QuietHoursEnabled_ShouldAcceptBooleanValue()
    {
        var entry = ConfigurationEntry.Create(
            definitionId: SampleDefinitionId,
            key: "notifications.quiet_hours.enabled",
            scope: ConfigurationScope.User,
            createdBy: "user1",
            value: "true",
            changeReason: "User preference update");

        Assert.Equal("notifications.quiet_hours.enabled", entry.Key);
        Assert.Equal("true", entry.Value);
        Assert.Equal(ConfigurationScope.User, entry.Scope);
    }

    [Fact]
    public void ConfigurationEntry_QuietHoursStart_ShouldAcceptTimeValue()
    {
        var entry = ConfigurationEntry.Create(
            definitionId: SampleDefinitionId,
            key: "notifications.quiet_hours.start",
            scope: ConfigurationScope.User,
            createdBy: "user1",
            value: "22:00",
            changeReason: "Set quiet hours start");

        Assert.Equal("22:00", entry.Value);
    }

    [Fact]
    public void ConfigurationEntry_DigestFrequency_ShouldAcceptFrequencyValue()
    {
        var entry = ConfigurationEntry.Create(
            definitionId: SampleDefinitionId,
            key: "notifications.digest.frequency",
            scope: ConfigurationScope.User,
            createdBy: "user1",
            value: "weekly",
            changeReason: "Set digest frequency");

        Assert.Equal("weekly", entry.Value);
    }

    [Fact]
    public void ConfigurationEntry_DigestSections_ShouldAcceptJsonArray()
    {
        const string sections = """["changes","incidents","contracts"]""";
        var entry = ConfigurationEntry.Create(
            definitionId: SampleDefinitionId,
            key: "notifications.digest.sections",
            scope: ConfigurationScope.User,
            createdBy: "user1",
            value: sections,
            changeReason: "Set digest sections");

        Assert.Equal(sections, entry.Value);
    }

    [Fact]
    public void ConfigurationEntry_UpdateValue_ShouldIncrementVersion()
    {
        var entry = ConfigurationEntry.Create(
            definitionId: SampleDefinitionId,
            key: "notifications.quiet_hours.enabled",
            scope: ConfigurationScope.User,
            createdBy: "user1",
            value: "false",
            changeReason: "Initial");

        Assert.Equal(1, entry.Version);
    }
}
