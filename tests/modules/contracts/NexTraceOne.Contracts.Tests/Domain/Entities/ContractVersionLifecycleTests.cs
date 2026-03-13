using NexTraceOne.Contracts.Domain.Entities;
using NexTraceOne.Contracts.Domain.Enums;
using NexTraceOne.Contracts.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace NexTraceOne.Contracts.Tests.Domain.Entities;

/// <summary>
/// Testes do ciclo de vida (lifecycle) de ContractVersion.
/// Valida a máquina de estados: Draft → InReview → Approved → Locked → Deprecated → Sunset → Retired.
/// </summary>
public sealed class ContractVersionLifecycleTests
{
    private static ContractVersion CreateDraftContract()
    {
        var result = ContractVersion.Import(
            Guid.NewGuid(),
            "1.0.0",
            """{"openapi":"3.1.0","info":{"title":"Test","version":"1.0.0"},"paths":{}}""",
            "json",
            "upload");
        return result.Value;
    }

    [Fact]
    public void Import_Should_SetLifecycleStateToDraft()
    {
        var contract = CreateDraftContract();
        contract.LifecycleState.Should().Be(ContractLifecycleState.Draft);
    }

    [Fact]
    public void Import_Should_SetProtocolToOpenApiByDefault()
    {
        var contract = CreateDraftContract();
        contract.Protocol.Should().Be(ContractProtocol.OpenApi);
    }

    [Fact]
    public void Import_Should_SetSpecifiedProtocol()
    {
        var result = ContractVersion.Import(
            Guid.NewGuid(), "1.0.0",
            "<definitions/>", "xml", "upload",
            ContractProtocol.Wsdl);
        result.IsSuccess.Should().BeTrue();
        result.Value.Protocol.Should().Be(ContractProtocol.Wsdl);
    }

    [Fact]
    public void TransitionTo_Should_Succeed_FromDraftToInReview()
    {
        var contract = CreateDraftContract();
        var result = contract.TransitionTo(ContractLifecycleState.InReview, DateTimeOffset.UtcNow);
        result.IsSuccess.Should().BeTrue();
        contract.LifecycleState.Should().Be(ContractLifecycleState.InReview);
    }

    [Fact]
    public void TransitionTo_Should_Succeed_FromInReviewToApproved()
    {
        var contract = CreateDraftContract();
        contract.TransitionTo(ContractLifecycleState.InReview, DateTimeOffset.UtcNow);

        var result = contract.TransitionTo(ContractLifecycleState.Approved, DateTimeOffset.UtcNow);
        result.IsSuccess.Should().BeTrue();
        contract.LifecycleState.Should().Be(ContractLifecycleState.Approved);
    }

    [Fact]
    public void TransitionTo_Should_Succeed_FromApprovedToLocked()
    {
        var contract = CreateDraftContract();
        contract.TransitionTo(ContractLifecycleState.InReview, DateTimeOffset.UtcNow);
        contract.TransitionTo(ContractLifecycleState.Approved, DateTimeOffset.UtcNow);

        var result = contract.TransitionTo(ContractLifecycleState.Locked, DateTimeOffset.UtcNow);
        result.IsSuccess.Should().BeTrue();
        contract.LifecycleState.Should().Be(ContractLifecycleState.Locked);
        contract.IsLocked.Should().BeTrue();
    }

    [Fact]
    public void TransitionTo_Should_Fail_FromDraftToLocked()
    {
        var contract = CreateDraftContract();
        var result = contract.TransitionTo(ContractLifecycleState.Locked, DateTimeOffset.UtcNow);
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Contracts.Lifecycle.InvalidTransition");
    }

    [Fact]
    public void TransitionTo_Should_Fail_FromLockedToDraft()
    {
        var contract = CreateDraftContract();
        contract.TransitionTo(ContractLifecycleState.InReview, DateTimeOffset.UtcNow);
        contract.TransitionTo(ContractLifecycleState.Approved, DateTimeOffset.UtcNow);
        contract.TransitionTo(ContractLifecycleState.Locked, DateTimeOffset.UtcNow);

        var result = contract.TransitionTo(ContractLifecycleState.Draft, DateTimeOffset.UtcNow);
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void TransitionTo_Should_AllowReturnFromInReviewToDraft()
    {
        var contract = CreateDraftContract();
        contract.TransitionTo(ContractLifecycleState.InReview, DateTimeOffset.UtcNow);

        var result = contract.TransitionTo(ContractLifecycleState.Draft, DateTimeOffset.UtcNow);
        result.IsSuccess.Should().BeTrue();
        contract.LifecycleState.Should().Be(ContractLifecycleState.Draft);
    }

    [Fact]
    public void TransitionTo_Should_AllowReturnFromApprovedToInReview()
    {
        var contract = CreateDraftContract();
        contract.TransitionTo(ContractLifecycleState.InReview, DateTimeOffset.UtcNow);
        contract.TransitionTo(ContractLifecycleState.Approved, DateTimeOffset.UtcNow);

        var result = contract.TransitionTo(ContractLifecycleState.InReview, DateTimeOffset.UtcNow);
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Deprecate_Should_SetDeprecationFields()
    {
        var contract = CreateDraftContract();
        contract.TransitionTo(ContractLifecycleState.InReview, DateTimeOffset.UtcNow);
        contract.TransitionTo(ContractLifecycleState.Approved, DateTimeOffset.UtcNow);
        contract.TransitionTo(ContractLifecycleState.Locked, DateTimeOffset.UtcNow);

        var sunset = DateTimeOffset.UtcNow.AddMonths(6);
        var result = contract.Deprecate("Moving to v2", DateTimeOffset.UtcNow, sunset);

        result.IsSuccess.Should().BeTrue();
        contract.LifecycleState.Should().Be(ContractLifecycleState.Deprecated);
        contract.DeprecationNotice.Should().Be("Moving to v2");
        contract.SunsetDate.Should().Be(sunset);
    }

    [Fact]
    public void Deprecate_Should_Fail_WhenContractIsDraft()
    {
        var contract = CreateDraftContract();
        var result = contract.Deprecate("Too early", DateTimeOffset.UtcNow, null);
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Lock_Should_SetLifecycleStateToLocked()
    {
        var contract = CreateDraftContract();
        var result = contract.Lock("admin", DateTimeOffset.UtcNow);
        result.IsSuccess.Should().BeTrue();
        contract.LifecycleState.Should().Be(ContractLifecycleState.Locked);
    }

    [Fact]
    public void FullLifecycleTransition_Should_TraverseAllStates()
    {
        var contract = CreateDraftContract();
        var now = DateTimeOffset.UtcNow;

        contract.TransitionTo(ContractLifecycleState.InReview, now).IsSuccess.Should().BeTrue();
        contract.TransitionTo(ContractLifecycleState.Approved, now).IsSuccess.Should().BeTrue();
        contract.TransitionTo(ContractLifecycleState.Locked, now).IsSuccess.Should().BeTrue();
        contract.TransitionTo(ContractLifecycleState.Deprecated, now).IsSuccess.Should().BeTrue();
        contract.TransitionTo(ContractLifecycleState.Sunset, now).IsSuccess.Should().BeTrue();
        contract.TransitionTo(ContractLifecycleState.Retired, now).IsSuccess.Should().BeTrue();

        contract.LifecycleState.Should().Be(ContractLifecycleState.Retired);
    }
}
