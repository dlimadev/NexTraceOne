using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;

using SyncJiraFeature = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.SyncJiraWorkItems.SyncJiraWorkItems;

namespace NexTraceOne.ChangeGovernance.Tests.ChangeIntelligence.Application.Features;

/// <summary>
/// Testes de unidade para SyncJiraWorkItems — validação da decisão formal de deferral.
/// A integração Jira está formalmente diferida (PGLI) e deve retornar erro explícito.
/// </summary>
public sealed class SyncJiraWorkItemsTests
{
    private static readonly DateTimeOffset FixedNow = new(2025, 6, 1, 10, 0, 0, TimeSpan.Zero);

    private static Release CreateRelease() =>
        Release.Create(Guid.NewGuid(), "TestService", "1.0.0", "staging", "https://ci/pipeline/1", "abc123def456", FixedNow);

    [Fact]
    public async Task SyncJiraWorkItems_ExistingRelease_ShouldReturnDeferralError()
    {
        // Arrange
        var release = CreateRelease();
        var repository = Substitute.For<IReleaseRepository>();
        repository.GetByIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .Returns(release);

        var handler = new SyncJiraFeature.Handler(repository);
        var command = new SyncJiraFeature.Command(release.Id.Value);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("JIRA_INTEGRATION_DEFERRED");
        result.Error.Message.Should().Contain("formally deferred");
    }

    [Fact]
    public async Task SyncJiraWorkItems_ReleaseNotFound_ShouldReturnNotFoundError()
    {
        // Arrange
        var repository = Substitute.For<IReleaseRepository>();
        repository.GetByIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .Returns((Release?)null);

        var handler = new SyncJiraFeature.Handler(repository);
        var command = new SyncJiraFeature.Command(Guid.NewGuid());

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task SyncJiraWorkItems_ShouldNotReturnSuccessWithNotConfiguredMessage()
    {
        // Arrange — this test ensures the old "not configured" success behavior is gone
        var release = CreateRelease();
        var repository = Substitute.For<IReleaseRepository>();
        repository.GetByIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .Returns(release);

        var handler = new SyncJiraFeature.Handler(repository);
        var command = new SyncJiraFeature.Command(release.Id.Value);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert — must NOT succeed with a placeholder message
        result.IsSuccess.Should().BeFalse("SyncJiraWorkItems should return explicit deferral error, not a success with 'not configured'");
    }
}
