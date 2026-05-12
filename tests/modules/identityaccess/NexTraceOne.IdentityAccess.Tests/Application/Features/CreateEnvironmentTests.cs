using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Domain.Entities;
using NexTraceOne.IdentityAccess.Domain.Enums;
using NexTraceOne.IdentityAccess.Tests.TestDoubles;

using CreateEnvironmentFeature = NexTraceOne.IdentityAccess.Application.Features.CreateEnvironment.CreateEnvironment;

using DomainEnvironment = NexTraceOne.IdentityAccess.Domain.Entities.Environment;

namespace NexTraceOne.IdentityAccess.Tests.Application.Features;

/// <summary>
/// Testes da feature CreateEnvironment.
/// Cobre criação de ambientes por tenant, validação de slug único, e designação de produção principal.
/// </summary>
public sealed class CreateEnvironmentTests
{
    private readonly DateTimeOffset _now = new(2026, 03, 21, 10, 0, 0, TimeSpan.Zero);
    private readonly TenantId _tenantId = TenantId.From(Guid.NewGuid());

    private IDateTimeProvider CreateClock() =>
        CreateClock(_now);

    private static IDateTimeProvider CreateClock(DateTimeOffset now)
    {
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(now);
        return clock;
    }

    private CreateEnvironmentFeature.Handler CreateHandler(
        IEnvironmentRepository? repo = null,
        ICurrentTenant? currentTenant = null)
    {
        return new CreateEnvironmentFeature.Handler(
            currentTenant ?? new TestCurrentTenant(_tenantId.Value),
            repo ?? Substitute.For<IEnvironmentRepository>(),
            CreateClock());
    }

    [Fact]
    public async Task Handle_Should_CreateEnvironment_When_CommandIsValid()
    {
        var repo = Substitute.For<IEnvironmentRepository>();
        repo.SlugExistsAsync(_tenantId, "qa", Arg.Any<CancellationToken>()).Returns(false);
        repo.GetPrimaryProductionAsync(_tenantId, Arg.Any<CancellationToken>()).Returns((DomainEnvironment?)null);

        var sut = CreateHandler(repo);

        var result = await sut.Handle(new CreateEnvironmentFeature.Command(
            "QA", "qa", 1, "Validation", "Medium", null, null, null, null, false),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("QA");
        result.Value.Slug.Should().Be("qa");
        repo.Received(1).Add(Arg.Any<DomainEnvironment>());
    }

    [Fact]
    public async Task Handle_Should_Fail_When_SlugAlreadyExists()
    {
        var repo = Substitute.For<IEnvironmentRepository>();
        repo.SlugExistsAsync(_tenantId, "qa", Arg.Any<CancellationToken>()).Returns(true);

        var sut = CreateHandler(repo);

        var result = await sut.Handle(new CreateEnvironmentFeature.Command(
            "QA", "qa", 1, "Validation", "Medium", null, null, null, null, false),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("Identity.Environment.SlugAlreadyExists");
    }

    [Fact]
    public async Task Handle_Should_Fail_When_PrimaryProductionAlreadyExistsAndNewOneRequested()
    {
        var existingPrimary = DomainEnvironment.Create(
            _tenantId, "Production", "production", 10, _now,
            EnvironmentProfile.Production, EnvironmentCriticality.Critical);
        existingPrimary.DesignateAsPrimaryProduction();

        var repo = Substitute.For<IEnvironmentRepository>();
        repo.SlugExistsAsync(_tenantId, "prod-dr", Arg.Any<CancellationToken>()).Returns(false);
        repo.GetPrimaryProductionAsync(_tenantId, Arg.Any<CancellationToken>()).Returns(existingPrimary);

        var sut = CreateHandler(repo);

        var result = await sut.Handle(new CreateEnvironmentFeature.Command(
            "Prod DR", "prod-dr", 11, "DisasterRecovery", "Critical", null, null, null, null, true),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("Identity.Environment.PrimaryProductionAlreadyExists");
    }

    [Fact]
    public async Task Handle_Should_CreatePrimaryProduction_When_NoneExists()
    {
        var repo = Substitute.For<IEnvironmentRepository>();
        repo.SlugExistsAsync(_tenantId, "production", Arg.Any<CancellationToken>()).Returns(false);
        repo.GetPrimaryProductionAsync(_tenantId, Arg.Any<CancellationToken>()).Returns((DomainEnvironment?)null);

        var sut = CreateHandler(repo);

        var result = await sut.Handle(new CreateEnvironmentFeature.Command(
            "Production", "production", 10, "Production", "Critical", "PROD", null, null, null, true),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        repo.Received(1).Add(Arg.Is<DomainEnvironment>(e =>
            e.IsPrimaryProduction && e.Profile == EnvironmentProfile.Production));
    }

    [Fact]
    public async Task Handle_Should_Fail_When_TenantContextMissing()
    {
        var noTenant = new TestCurrentTenant(Guid.Empty);
        var sut = CreateHandler(currentTenant: noTenant);

        var result = await sut.Handle(new CreateEnvironmentFeature.Command(
            "DEV", "dev", 0, "Development", "Low", null, null, null, null, false),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }
}


