using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Application.Features.ActivateTenant;
using NexTraceOne.IdentityAccess.Domain.Entities;
using NexTraceOne.IdentityAccess.Tests.TestDoubles;

namespace NexTraceOne.IdentityAccess.Tests.Application.Features;

/// <summary>
/// Testes unitários do handler ActivateTenant.
/// Cobre: tenant não encontrado, tenant já activo, reactivação bem-sucedida.
/// </summary>
public sealed class ActivateTenantTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 5, 10, 10, 0, 0, TimeSpan.Zero);
    private static readonly Guid TenantId = Guid.NewGuid();

    private static (
        ITenantRepository tenantRepo,
        ActivateTenant.Handler handler) CreateHandler()
    {
        var tenantRepo = Substitute.For<ITenantRepository>();
        var clock = new TestDateTimeProvider(FixedNow);
        var handler = new ActivateTenant.Handler(tenantRepo, clock);
        return (tenantRepo, handler);
    }

    private static Tenant CreateActiveTenant()
    {
        var tenant = Tenant.Create("Test Corp", "test-corp", FixedNow.AddDays(-30));
        return tenant;
    }

    private static Tenant CreateDeactivatedTenant()
    {
        var tenant = Tenant.Create("Inactive Corp", "inactive-corp", FixedNow.AddDays(-30));
        tenant.Deactivate(FixedNow.AddDays(-5));
        return tenant;
    }

    [Fact]
    public async Task Handle_Should_ReturnError_WhenTenantNotFound()
    {
        var (tenantRepo, handler) = CreateHandler();
        tenantRepo.GetByIdAsync(Arg.Any<TenantId>(), Arg.Any<CancellationToken>())
            .Returns((Tenant?)null);

        var result = await handler.Handle(
            new ActivateTenant.Command(TenantId),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("TenantNotFound");
    }

    [Fact]
    public async Task Handle_Should_ReturnValidationError_WhenTenantAlreadyActive()
    {
        var (tenantRepo, handler) = CreateHandler();
        var tenant = CreateActiveTenant();
        tenantRepo.GetByIdAsync(Arg.Any<TenantId>(), Arg.Any<CancellationToken>())
            .Returns(tenant);

        var result = await handler.Handle(
            new ActivateTenant.Command(TenantId),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public async Task Handle_Should_ActivateTenant_WhenDeactivated()
    {
        var (tenantRepo, handler) = CreateHandler();
        var tenant = CreateDeactivatedTenant();
        tenant.IsActive.Should().BeFalse("precondition: tenant must be deactivated");
        tenantRepo.GetByIdAsync(Arg.Any<TenantId>(), Arg.Any<CancellationToken>())
            .Returns(tenant);

        var result = await handler.Handle(
            new ActivateTenant.Command(TenantId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsActive.Should().BeTrue();
        tenant.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_Should_ReturnTenantId_InResponse()
    {
        var (tenantRepo, handler) = CreateHandler();
        var tenant = CreateDeactivatedTenant();
        tenantRepo.GetByIdAsync(Arg.Any<TenantId>(), Arg.Any<CancellationToken>())
            .Returns(tenant);

        var result = await handler.Handle(
            new ActivateTenant.Command(TenantId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TenantId.Should().Be(tenant.Id.Value);
    }
}

