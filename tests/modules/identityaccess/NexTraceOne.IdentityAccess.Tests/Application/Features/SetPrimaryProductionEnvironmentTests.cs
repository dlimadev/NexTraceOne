using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Domain.Entities;
using NexTraceOne.IdentityAccess.Domain.Enums;
using NexTraceOne.IdentityAccess.Tests.TestDoubles;

using SetPrimaryProductionFeature = NexTraceOne.IdentityAccess.Application.Features.SetPrimaryProductionEnvironment.SetPrimaryProductionEnvironment;

using DomainEnvironment = NexTraceOne.IdentityAccess.Domain.Entities.Environment;

namespace NexTraceOne.IdentityAccess.Tests.Application.Features;

/// <summary>
/// Testes da feature SetPrimaryProductionEnvironment.
/// Cobre designação de produção principal, revogação do anterior e validações de segurança.
/// </summary>
public sealed class SetPrimaryProductionEnvironmentTests
{
    private readonly DateTimeOffset _now = new(2026, 03, 21, 10, 0, 0, TimeSpan.Zero);
    private readonly TenantId _tenantId = TenantId.From(Guid.NewGuid());

    private DomainEnvironment CreateActiveEnv(string name, string slug,
        EnvironmentProfile profile = EnvironmentProfile.Production,
        bool isPrimary = false)
    {
        var env = DomainEnvironment.Create(_tenantId, name, slug, 10, _now, profile, EnvironmentCriticality.Critical);
        if (isPrimary)
            env.DesignateAsPrimaryProduction();
        return env;
    }

    private SetPrimaryProductionFeature.Handler CreateHandler(IEnvironmentRepository repo)
        => new(new TestCurrentTenant(_tenantId.Value), repo);

    [Fact]
    public async Task Handle_Should_DesignateNewPrimary_When_NoPreviousPrimaryExists()
    {
        var target = CreateActiveEnv("Production", "production");

        var repo = Substitute.For<IEnvironmentRepository>();
        repo.GetByIdForTenantAsync(target.Id, _tenantId, Arg.Any<CancellationToken>()).Returns(target);
        repo.GetPrimaryProductionAsync(_tenantId, Arg.Any<CancellationToken>()).Returns((DomainEnvironment?)null);

        var result = await CreateHandler(repo).Handle(
            new SetPrimaryProductionFeature.Command(target.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        target.IsPrimaryProduction.Should().BeTrue();
        result.Value.PreviousPrimaryEnvironmentId.Should().BeNull();
    }

    [Fact]
    public async Task Handle_Should_RevokePreviousPrimary_When_AlreadyExists()
    {
        var oldPrimary = CreateActiveEnv("Old Production", "old-production", isPrimary: true);
        var newTarget = CreateActiveEnv("New Production", "new-production");

        var repo = Substitute.For<IEnvironmentRepository>();
        repo.GetByIdForTenantAsync(newTarget.Id, _tenantId, Arg.Any<CancellationToken>()).Returns(newTarget);
        repo.GetPrimaryProductionAsync(_tenantId, Arg.Any<CancellationToken>()).Returns(oldPrimary);

        var result = await CreateHandler(repo).Handle(
            new SetPrimaryProductionFeature.Command(newTarget.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        newTarget.IsPrimaryProduction.Should().BeTrue();
        oldPrimary.IsPrimaryProduction.Should().BeFalse();
        result.Value.PreviousPrimaryEnvironmentId.Should().Be(oldPrimary.Id.Value);
    }

    [Fact]
    public async Task Handle_Should_ReturnNoChange_When_AlreadyPrimary()
    {
        var target = CreateActiveEnv("Production", "production", isPrimary: true);

        var repo = Substitute.For<IEnvironmentRepository>();
        repo.GetByIdForTenantAsync(target.Id, _tenantId, Arg.Any<CancellationToken>()).Returns(target);

        var result = await CreateHandler(repo).Handle(
            new SetPrimaryProductionFeature.Command(target.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        target.IsPrimaryProduction.Should().BeTrue();
        result.Value.PreviousPrimaryEnvironmentId.Should().BeNull();
    }

    [Fact]
    public async Task Handle_Should_Fail_When_EnvironmentNotFound()
    {
        var repo = Substitute.For<IEnvironmentRepository>();
        repo.GetByIdForTenantAsync(Arg.Any<EnvironmentId>(), _tenantId, Arg.Any<CancellationToken>())
            .Returns((DomainEnvironment?)null);

        var result = await CreateHandler(repo).Handle(
            new SetPrimaryProductionFeature.Command(Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("Identity.Environment.NotFound");
    }

    [Fact]
    public async Task Handle_Should_Fail_When_EnvironmentIsInactive()
    {
        var target = CreateActiveEnv("Production", "production");
        target.Deactivate();

        var repo = Substitute.For<IEnvironmentRepository>();
        repo.GetByIdForTenantAsync(target.Id, _tenantId, Arg.Any<CancellationToken>()).Returns(target);

        var result = await CreateHandler(repo).Handle(
            new SetPrimaryProductionFeature.Command(target.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("Identity.Environment.CannotDesignateInactiveAsPrimaryProduction");
    }
}


