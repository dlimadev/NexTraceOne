using NexTraceOne.Catalog.Domain.Portal.Entities;
using NexTraceOne.Catalog.Domain.Portal.Enums;

namespace NexTraceOne.Catalog.Tests.Portal.Domain;

/// <summary>
/// Testes de domínio para a entidade ContractPublicationEntry.
/// Valida criação, transições de estado (Publish, Withdraw, MarkAsDeprecated)
/// e casos de erro por transições inválidas.
/// </summary>
public sealed class ContractPublicationEntryTests
{
    private static readonly DateTimeOffset Now = new(2025, 06, 15, 10, 0, 0, TimeSpan.Zero);
    private static readonly Guid VersionId = Guid.NewGuid();
    private static readonly Guid AssetId = Guid.NewGuid();

    private static ContractPublicationEntry BuildPendingEntry()
    {
        var result = ContractPublicationEntry.Create(
            VersionId, AssetId, "Payments API", "2.1.0", "jsmith",
            PublicationVisibility.Internal, "Initial release of v2.1.0");
        result.IsSuccess.Should().BeTrue();
        return result.Value;
    }

    [Fact]
    public void Create_Should_ReturnPendingEntry_When_InputIsValid()
    {
        var entry = BuildPendingEntry();

        entry.ContractVersionId.Should().Be(VersionId);
        entry.ApiAssetId.Should().Be(AssetId);
        entry.ContractTitle.Should().Be("Payments API");
        entry.SemVer.Should().Be("2.1.0");
        entry.Status.Should().Be(ContractPublicationStatus.PendingPublication);
        entry.Visibility.Should().Be(PublicationVisibility.Internal);
        entry.PublishedBy.Should().Be("jsmith");
        entry.ReleaseNotes.Should().Be("Initial release of v2.1.0");
        entry.PublishedAt.Should().BeNull();
    }

    [Fact]
    public void Publish_Should_TransitionToPublished_When_StatusIsPending()
    {
        var entry = BuildPendingEntry();

        var result = entry.Publish(Now);

        result.IsSuccess.Should().BeTrue();
        entry.Status.Should().Be(ContractPublicationStatus.Published);
        entry.PublishedAt.Should().Be(Now);
    }

    [Fact]
    public void Publish_Should_ReturnFailure_When_AlreadyPublished()
    {
        var entry = BuildPendingEntry();
        entry.Publish(Now);

        var second = entry.Publish(Now.AddMinutes(1));

        second.IsFailure.Should().BeTrue();
        second.Error.Code.Should().Contain("InvalidTransition");
    }

    [Fact]
    public void Withdraw_Should_TransitionToWithdrawn_When_Published()
    {
        var entry = BuildPendingEntry();
        entry.Publish(Now);

        var result = entry.Withdraw("admin", "Replaced by v3", Now.AddDays(1));

        result.IsSuccess.Should().BeTrue();
        entry.Status.Should().Be(ContractPublicationStatus.Withdrawn);
        entry.WithdrawnBy.Should().Be("admin");
        entry.WithdrawalReason.Should().Be("Replaced by v3");
        entry.WithdrawnAt.Should().Be(Now.AddDays(1));
    }

    [Fact]
    public void Withdraw_Should_ReturnFailure_When_NotPublished()
    {
        var entry = BuildPendingEntry(); // PendingPublication

        var result = entry.Withdraw("admin", null, Now);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvalidTransition");
    }

    [Fact]
    public void MarkAsDeprecated_Should_Succeed_When_Published()
    {
        var entry = BuildPendingEntry();
        entry.Publish(Now);

        var result = entry.MarkAsDeprecated();

        result.IsSuccess.Should().BeTrue();
        entry.Status.Should().Be(ContractPublicationStatus.Deprecated);
    }

    [Fact]
    public void MarkAsDeprecated_Should_ReturnFailure_When_Pending()
    {
        var entry = BuildPendingEntry();

        var result = entry.MarkAsDeprecated();

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Create_Should_ThrowGuardException_When_TitleIsEmpty()
    {
        var act = () => ContractPublicationEntry.Create(VersionId, AssetId, "", "1.0.0", "jsmith");

        act.Should().Throw<ArgumentException>();
    }
}
