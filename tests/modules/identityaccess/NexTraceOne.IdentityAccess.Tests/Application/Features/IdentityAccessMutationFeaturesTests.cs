using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Application.Features.UpdateEnvironment;
using NexTraceOne.IdentityAccess.Application.Features.UpdateTenant;
using NexTraceOne.IdentityAccess.Domain.Entities;
using NexTraceOne.IdentityAccess.Domain.Enums;
using NexTraceOne.IdentityAccess.Tests.TestDoubles;

using DomainEnvironment = NexTraceOne.IdentityAccess.Domain.Entities.Environment;

namespace NexTraceOne.IdentityAccess.Tests.Application.Features;

/// <summary>
/// Testes de unidade para as features de mutação do IdentityAccess:
/// UpdateTenant e UpdateEnvironment.
/// </summary>
public sealed class IdentityAccessMutationFeaturesTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 4, 19, 12, 0, 0, TimeSpan.Zero);

    private static IDateTimeProvider CreateClock() =>
        new TestDateTimeProvider(FixedNow);

    // ═══════════════════════════════════════════════════════════════════
    // UpdateTenant
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task UpdateTenant_Should_UpdateNameAndOrganizationInfo()
    {
        var tenant = Tenant.Create("OldName", "old-slug", FixedNow);

        var tenantRepo = Substitute.For<ITenantRepository>();
        tenantRepo.GetByIdAsync(tenant.Id, Arg.Any<CancellationToken>()).Returns(tenant);

        var handler = new UpdateTenant.Handler(tenantRepo, CreateClock());
        var command = new UpdateTenant.Command(
            tenant.Id.Value,
            "NewName Corp",
            "New Legal Name Ltda",
            "12.345.678/0001-00");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TenantId.Should().Be(tenant.Id.Value);
        result.Value.Name.Should().Be("NewName Corp");
        result.Value.LegalName.Should().Be("New Legal Name Ltda");
        result.Value.TaxId.Should().Be("12.345.678/0001-00");
    }

    [Fact]
    public async Task UpdateTenant_Should_UpdateName_WithNullLegalNameAndTaxId()
    {
        var tenant = Tenant.Create("OriginalName", "orig", FixedNow);

        var tenantRepo = Substitute.For<ITenantRepository>();
        tenantRepo.GetByIdAsync(tenant.Id, Arg.Any<CancellationToken>()).Returns(tenant);

        var handler = new UpdateTenant.Handler(tenantRepo, CreateClock());
        var command = new UpdateTenant.Command(
            tenant.Id.Value,
            "Updated Name",
            LegalName: null,
            TaxId: null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Updated Name");
        result.Value.LegalName.Should().BeNull();
        result.Value.TaxId.Should().BeNull();
    }

    [Fact]
    public async Task UpdateTenant_Should_ReturnError_When_TenantNotFound()
    {
        var tenantRepo = Substitute.For<ITenantRepository>();
        tenantRepo.GetByIdAsync(Arg.Any<TenantId>(), Arg.Any<CancellationToken>()).Returns((Tenant?)null);

        var handler = new UpdateTenant.Handler(tenantRepo, CreateClock());
        var result = await handler.Handle(
            new UpdateTenant.Command(Guid.NewGuid(), "Name", null, null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    [Fact]
    public void UpdateTenant_Validator_Should_Reject_EmptyName()
    {
        var validator = new UpdateTenant.Validator();
        var result = validator.Validate(new UpdateTenant.Command(Guid.NewGuid(), "", null, null));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void UpdateTenant_Validator_Should_Reject_EmptyTenantId()
    {
        var validator = new UpdateTenant.Validator();
        var result = validator.Validate(new UpdateTenant.Command(Guid.Empty, "Valid Name", null, null));
        result.IsValid.Should().BeFalse();
    }

    // ═══════════════════════════════════════════════════════════════════
    // UpdateEnvironment
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task UpdateEnvironment_Should_UpdateNameProfileAndCriticality()
    {
        var tenantId = TenantId.From(Guid.NewGuid());
        var currentTenant = new TestCurrentTenant(tenantId.Value);
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.Id.Returns("user-123");

        var env = DomainEnvironment.Create(tenantId, "Dev", "dev", 0, FixedNow);

        var envRepo = Substitute.For<IEnvironmentRepository>();
        envRepo.GetByIdForTenantAsync(env.Id, tenantId, Arg.Any<CancellationToken>()).Returns(env);

        var handler = new UpdateEnvironment.Handler(currentTenant, currentUser, CreateClock(), envRepo);
        var command = new UpdateEnvironment.Command(
            env.Id.Value,
            "Development Updated",
            SortOrder: 1,
            Profile: "Development",
            Criticality: "Low",
            Code: "DEV",
            Description: "Development environment",
            Region: "BR-East",
            IsProductionLike: false);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.EnvironmentId.Should().Be(env.Id.Value);
        result.Value.Name.Should().Be("Development Updated");
        result.Value.IsProductionLike.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateEnvironment_Should_UpdateToProductionProfile_SetIsProductionLikeTrue()
    {
        var tenantId = TenantId.From(Guid.NewGuid());
        var currentTenant = new TestCurrentTenant(tenantId.Value);
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.Id.Returns("admin-456");

        var env = DomainEnvironment.Create(tenantId, "Pre-Prod", "pre-prod", 2, FixedNow);

        var envRepo = Substitute.For<IEnvironmentRepository>();
        envRepo.GetByIdForTenantAsync(env.Id, tenantId, Arg.Any<CancellationToken>()).Returns(env);

        var handler = new UpdateEnvironment.Handler(currentTenant, currentUser, CreateClock(), envRepo);
        var command = new UpdateEnvironment.Command(
            env.Id.Value,
            "Production",
            SortOrder: 3,
            Profile: "Production",
            Criticality: "Critical",
            Code: "PROD",
            Description: "Production environment",
            Region: "BR-South",
            IsProductionLike: true);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Profile.Should().Be("production");
        result.Value.IsProductionLike.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateEnvironment_Should_ReturnError_When_EnvironmentNotFound()
    {
        var tenantId = TenantId.From(Guid.NewGuid());
        var currentTenant = new TestCurrentTenant(tenantId.Value);
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.Id.Returns("user-789");

        var envRepo = Substitute.For<IEnvironmentRepository>();
        envRepo.GetByIdForTenantAsync(Arg.Any<EnvironmentId>(), Arg.Any<TenantId>(), Arg.Any<CancellationToken>())
            .Returns((DomainEnvironment?)null);

        var handler = new UpdateEnvironment.Handler(currentTenant, currentUser, CreateClock(), envRepo);
        var result = await handler.Handle(
            new UpdateEnvironment.Command(Guid.NewGuid(), "X", 0, "Development", "Low", null, null, null, null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    [Fact]
    public async Task UpdateEnvironment_Should_ReturnError_When_NoTenantContext()
    {
        var emptyTenantContext = new TestCurrentTenant(Guid.Empty);
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.Id.Returns("user-000");

        var envRepo = Substitute.For<IEnvironmentRepository>();

        var handler = new UpdateEnvironment.Handler(emptyTenantContext, currentUser, CreateClock(), envRepo);
        var result = await handler.Handle(
            new UpdateEnvironment.Command(Guid.NewGuid(), "X", 0, "Development", "Low", null, null, null, null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.Tenant.ContextRequired");
    }

    [Fact]
    public void UpdateEnvironment_Validator_Should_Reject_InvalidProfile()
    {
        var validator = new UpdateEnvironment.Validator();
        var result = validator.Validate(
            new UpdateEnvironment.Command(Guid.NewGuid(), "Name", 0, "InvalidProfile", "Low", null, null, null, null));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void UpdateEnvironment_Validator_Should_Reject_EmptyName()
    {
        var validator = new UpdateEnvironment.Validator();
        var result = validator.Validate(
            new UpdateEnvironment.Command(Guid.NewGuid(), "", 0, "Development", "Low", null, null, null, null));
        result.IsValid.Should().BeFalse();
    }
}
