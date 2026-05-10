using MediatR;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Application.Features.ProvisionTenant;
using SeedModulePolicies = NexTraceOne.IdentityAccess.Application.Features.SeedDefaultModuleAccessPolicies.SeedDefaultModuleAccessPolicies;
using SeedRolePerms = NexTraceOne.IdentityAccess.Application.Features.SeedDefaultRolePermissions.SeedDefaultRolePermissions;
using NexTraceOne.IdentityAccess.Tests.TestDoubles;

namespace NexTraceOne.IdentityAccess.Tests.Application.Features;

/// <summary>
/// Testes unitários do handler ProvisionTenant.
/// Cobre: happy path, slug conflict, plano trial com validade, seeding pós-commit.
/// </summary>
public sealed class ProvisionTenantTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 5, 9, 10, 0, 0, TimeSpan.Zero);

    private static (
        ITenantRepository tenants,
        ITenantLicenseRepository licenses,
        IIdentityAccessUnitOfWork uow,
        ISender sender,
        ProvisionTenant.Handler handler) CreateHandler()
    {
        var tenants = Substitute.For<ITenantRepository>();
        var licenses = Substitute.For<ITenantLicenseRepository>();
        var uow = Substitute.For<IIdentityAccessUnitOfWork>();
        var sender = Substitute.For<ISender>();
        var clock = new TestDateTimeProvider(FixedNow);

        var handler = new ProvisionTenant.Handler(tenants, licenses, clock, uow, sender);
        return (tenants, licenses, uow, sender, handler);
    }

    [Fact]
    public async Task Handle_Should_ReturnSuccess_WhenValidCommandProvided()
    {
        var (tenants, licenses, uow, sender, handler) = CreateHandler();
        tenants.SlugExistsAsync("acme-corp", Arg.Any<CancellationToken>()).Returns(false);
        uow.CommitAsync(Arg.Any<CancellationToken>()).Returns(1);

        var cmd = new ProvisionTenant.Command(
            "Acme Corp", "acme-corp", "Professional", 10, null, null);

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Acme Corp");
        result.Value.Slug.Should().Be("acme-corp");
        result.Value.Plan.Should().Be("Professional");
        result.Value.LicenseProvisioned.Should().BeTrue();
        result.Value.LicenseId.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_Should_AddTenantAndLicense_ToRepositories()
    {
        var (tenants, licenses, uow, _, handler) = CreateHandler();
        tenants.SlugExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);

        var cmd = new ProvisionTenant.Command(
            "Test Inc", "test-inc", "Enterprise", 50, "Test Inc Legal", "99.999.999/0001-99");

        await handler.Handle(cmd, CancellationToken.None);

        tenants.Received(1).Add(Arg.Is<Domain.Entities.Tenant>(t =>
            t.Name == "Test Inc" && t.Slug == "test-inc"));
        licenses.Received(1).Add(Arg.Any<Domain.Entities.TenantLicense>());
        await uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ReturnConflict_WhenSlugAlreadyExists()
    {
        var (tenants, _, _, _, handler) = CreateHandler();
        tenants.SlugExistsAsync("existing-slug", Arg.Any<CancellationToken>()).Returns(true);

        var cmd = new ProvisionTenant.Command("Name", "existing-slug", "Starter", 0, null, null);

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Type.Should().Be(ErrorType.Conflict);
        result.Error.Code.Should().Be("tenant.slugInUse");
    }

    [Fact]
    public async Task Handle_Should_ReturnValidation_WhenInvalidPlanProvided()
    {
        var (tenants, _, _, _, handler) = CreateHandler();
        tenants.SlugExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);

        var cmd = new ProvisionTenant.Command("Name", "slug-ok", "InvalidPlan", 5, null, null);

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Type.Should().Be(ErrorType.Validation);
        result.Error.Code.Should().Be("tenant.invalidPlan");
    }

    [Fact]
    public async Task Handle_Should_SetTrialExpiry_WhenPlanIsTrial()
    {
        var (tenants, licenses, uow, _, handler) = CreateHandler();
        tenants.SlugExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);

        Domain.Entities.TenantLicense? capturedLicense = null;
        licenses.When(r => r.Add(Arg.Any<Domain.Entities.TenantLicense>()))
            .Do(call => capturedLicense = call.Arg<Domain.Entities.TenantLicense>());

        var cmd = new ProvisionTenant.Command("Trial Co", "trial-co", "Trial", 5, null, null);
        await handler.Handle(cmd, CancellationToken.None);

        capturedLicense.Should().NotBeNull();
        capturedLicense!.ValidUntil.Should().NotBeNull();
        capturedLicense.ValidUntil!.Value.Date.Should().Be(FixedNow.AddDays(14).Date);
    }

    [Fact]
    public async Task Handle_Should_SeedDefaultRolesAndPolicies_AfterCommit()
    {
        var (tenants, _, uow, sender, handler) = CreateHandler();
        tenants.SlugExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);

        var callOrder = new List<string>();
        uow.When(u => u.CommitAsync(Arg.Any<CancellationToken>()))
            .Do(_ => callOrder.Add("commit"));
        sender.When(s => s.Send(Arg.Any<SeedRolePerms.Command>(), Arg.Any<CancellationToken>()))
            .Do(_ => callOrder.Add("seed-roles"));
        sender.When(s => s.Send(Arg.Any<SeedModulePolicies.Command>(), Arg.Any<CancellationToken>()))
            .Do(_ => callOrder.Add("seed-policies"));

        var cmd = new ProvisionTenant.Command("Ordered Corp", "ordered-corp", "Enterprise", 10, null, null);
        await handler.Handle(cmd, CancellationToken.None);

        callOrder.Should().ContainInOrder("commit", "seed-roles", "seed-policies");
    }

    [Fact]
    public async Task Handle_Should_NormalizeSlugToLowercase()
    {
        var (tenants, _, _, _, handler) = CreateHandler();
        tenants.SlugExistsAsync("my-corp", Arg.Any<CancellationToken>()).Returns(false);

        Domain.Entities.Tenant? capturedTenant = null;
        tenants.When(r => r.Add(Arg.Any<Domain.Entities.Tenant>()))
            .Do(call => capturedTenant = call.Arg<Domain.Entities.Tenant>());

        var cmd = new ProvisionTenant.Command("My Corp", "My-Corp", "Starter", 0, null, null);
        await handler.Handle(cmd, CancellationToken.None);

        capturedTenant!.Slug.Should().Be("my-corp");
    }
}
