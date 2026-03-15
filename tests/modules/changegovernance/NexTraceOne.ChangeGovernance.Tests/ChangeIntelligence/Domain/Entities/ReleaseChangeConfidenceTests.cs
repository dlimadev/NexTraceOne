using NexTraceOne.BuildingBlocks.Core.Enums;
using NexTraceOne.ChangeIntelligence.Domain.Entities;
using NexTraceOne.ChangeIntelligence.Domain.Enums;

namespace NexTraceOne.ChangeIntelligence.Tests.Domain.Entities;

/// <summary>
/// Testes de domínio para as extensões de Change Confidence na entidade Release.
/// Fase 4.4: ConfidenceStatus, ValidationStatus, ChangeType e metadados.
/// </summary>
public sealed class ReleaseChangeConfidenceTests
{
    private static readonly DateTimeOffset FixedNow = new(2025, 6, 1, 10, 0, 0, TimeSpan.Zero);

    private static Release CreateRelease() =>
        Release.Create(Guid.NewGuid(), "TestService", "1.0.0", "prod", "https://ci/pipeline/1", "abc123def456", FixedNow);

    // ── ConfidenceStatus ──────────────────────────────────────────────

    [Fact]
    public void Release_ShouldHaveNotAssessed_ConfidenceByDefault()
    {
        var release = CreateRelease();

        release.ConfidenceStatus.Should().Be(ConfidenceStatus.NotAssessed);
    }

    [Theory]
    [InlineData(ConfidenceStatus.Validated)]
    [InlineData(ConfidenceStatus.NeedsAttention)]
    [InlineData(ConfidenceStatus.SuspectedRegression)]
    [InlineData(ConfidenceStatus.CorrelatedWithIncident)]
    [InlineData(ConfidenceStatus.Mitigated)]
    public void UpdateConfidenceStatus_ShouldSetValue(ConfidenceStatus status)
    {
        var release = CreateRelease();

        release.UpdateConfidenceStatus(status);

        release.ConfidenceStatus.Should().Be(status);
    }

    // ── ValidationStatus ──────────────────────────────────────────────

    [Fact]
    public void Release_ShouldHavePending_ValidationByDefault()
    {
        var release = CreateRelease();

        release.ValidationStatus.Should().Be(ValidationStatus.Pending);
    }

    [Theory]
    [InlineData(ValidationStatus.InProgress)]
    [InlineData(ValidationStatus.Passed)]
    [InlineData(ValidationStatus.Failed)]
    [InlineData(ValidationStatus.Skipped)]
    public void UpdateValidationStatus_ShouldSetValue(ValidationStatus status)
    {
        var release = CreateRelease();

        release.UpdateValidationStatus(status);

        release.ValidationStatus.Should().Be(status);
    }

    // ── ChangeType ────────────────────────────────────────────────────

    [Fact]
    public void Release_ShouldHaveDeployment_ChangeTypeByDefault()
    {
        var release = CreateRelease();

        release.ChangeType.Should().Be(ChangeType.Deployment);
    }

    [Theory]
    [InlineData(ChangeType.ConfigurationChange)]
    [InlineData(ChangeType.ContractChange)]
    [InlineData(ChangeType.SchemaChange)]
    [InlineData(ChangeType.DependencyChange)]
    [InlineData(ChangeType.PolicyChange)]
    [InlineData(ChangeType.OperationalChange)]
    public void SetChangeType_ShouldSetValue(ChangeType changeType)
    {
        var release = CreateRelease();

        release.SetChangeType(changeType);

        release.ChangeType.Should().Be(changeType);
    }

    // ── Metadata ──────────────────────────────────────────────────────

    [Fact]
    public void SetMetadata_ShouldSetAllFields()
    {
        var release = CreateRelease();

        release.SetMetadata("Team Alpha", "Payments", "Critical payment flow update");

        release.TeamName.Should().Be("Team Alpha");
        release.Domain.Should().Be("Payments");
        release.Description.Should().Be("Critical payment flow update");
    }

    [Fact]
    public void SetMetadata_ShouldAcceptNullValues()
    {
        var release = CreateRelease();

        release.SetMetadata(null, null, null);

        release.TeamName.Should().BeNull();
        release.Domain.Should().BeNull();
        release.Description.Should().BeNull();
    }
}
