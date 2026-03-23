using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using NexTraceOne.Notifications.Contracts.ServiceInterfaces;
using NexTraceOne.Notifications.Infrastructure.Routing;

namespace NexTraceOne.Notifications.Tests.Routing;

public sealed class NotificationRecipientResolverTests
{
    private NotificationRecipientResolver CreateResolver() =>
        new(NullLoggerFactory.Instance.CreateLogger<NotificationRecipientResolver>());

    private static NotificationRequest CreateRequest(
        IReadOnlyList<Guid>? userIds = null,
        IReadOnlyList<string>? roles = null,
        IReadOnlyList<Guid>? teamIds = null) => new()
    {
        EventType = "TestEvent",
        Category = "Incident",
        Severity = "Warning",
        Title = "Test",
        Message = "Test message",
        SourceModule = "TestModule",
        RecipientUserIds = userIds,
        RecipientRoles = roles,
        RecipientTeamIds = teamIds,
    };

    [Fact]
    public async Task ResolveRecipientsAsync_ExplicitUserIds_ReturnsThoseIds()
    {
        // Arrange
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var request = CreateRequest(userIds: [id1, id2]);
        var resolver = CreateResolver();

        // Act
        var result = await resolver.ResolveRecipientsAsync(request, CancellationToken.None);

        // Assert
        result.Should().BeEquivalentTo([id1, id2]);
    }

    [Fact]
    public async Task ResolveRecipientsAsync_EmptyGuid_Excluded()
    {
        // Arrange
        var validId = Guid.NewGuid();
        var request = CreateRequest(userIds: [validId, Guid.Empty]);
        var resolver = CreateResolver();

        // Act
        var result = await resolver.ResolveRecipientsAsync(request, CancellationToken.None);

        // Assert
        result.Should().ContainSingle().Which.Should().Be(validId);
    }

    [Fact]
    public async Task ResolveRecipientsAsync_NoRecipients_ReturnsEmpty()
    {
        // Arrange
        var request = CreateRequest();
        var resolver = CreateResolver();

        // Act
        var result = await resolver.ResolveRecipientsAsync(request, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ResolveRecipientsAsync_DuplicateIds_Deduplicated()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = CreateRequest(userIds: [id, id, id]);
        var resolver = CreateResolver();

        // Act
        var result = await resolver.ResolveRecipientsAsync(request, CancellationToken.None);

        // Assert
        result.Should().ContainSingle().Which.Should().Be(id);
    }

    [Fact]
    public async Task ResolveRecipientsAsync_OnlyRoles_ReturnsEmptyWithWarning()
    {
        // Arrange
        var request = CreateRequest(roles: ["Admin", "Operator"]);
        var resolver = CreateResolver();

        // Act
        var result = await resolver.ResolveRecipientsAsync(request, CancellationToken.None);

        // Assert — roles not yet implemented, returns empty
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ResolveRecipientsAsync_OnlyTeams_ReturnsEmptyWithWarning()
    {
        // Arrange
        var request = CreateRequest(teamIds: [Guid.NewGuid(), Guid.NewGuid()]);
        var resolver = CreateResolver();

        // Act
        var result = await resolver.ResolveRecipientsAsync(request, CancellationToken.None);

        // Assert — team resolution not yet implemented, returns empty
        result.Should().BeEmpty();
    }
}
