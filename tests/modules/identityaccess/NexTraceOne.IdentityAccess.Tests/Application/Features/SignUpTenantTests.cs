using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Application.Features.SignUpTenant;
using NexTraceOne.IdentityAccess.Domain.Entities;
using NexTraceOne.IdentityAccess.Domain.ValueObjects;
using NexTraceOne.IdentityAccess.Tests.TestDoubles;

namespace NexTraceOne.IdentityAccess.Tests.Application.Features;

/// <summary>
/// Testes unitários do handler SignUpTenant (cadastro self-service).
/// Cobre: happy path, email em uso, slug em uso, roles não semeados.
/// </summary>
public sealed class SignUpTenantTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 6, 12, 10, 0, 0, TimeSpan.Zero);

    private readonly ITenantRepository _tenants = Substitute.For<ITenantRepository>();
    private readonly ITenantLicenseRepository _licenses = Substitute.For<ITenantLicenseRepository>();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IRoleRepository _roles = Substitute.For<IRoleRepository>();
    private readonly ITenantMembershipRepository _memberships = Substitute.For<ITenantMembershipRepository>();
    private readonly IAccountActivationTokenRepository _tokens = Substitute.For<IAccountActivationTokenRepository>();
    private readonly IIdentityNotifier _notifier = Substitute.For<IIdentityNotifier>();
    private readonly IPasswordHasher _hasher = Substitute.For<IPasswordHasher>();
    private readonly IIdentityAccessUnitOfWork _uow = Substitute.For<IIdentityAccessUnitOfWork>();

    private SignUpTenant.Handler CreateHandler()
    {
        _hasher.Hash(Arg.Any<string>()).Returns("hashed-placeholder");
        _uow.CommitAsync(Arg.Any<CancellationToken>()).Returns(1);
        return new SignUpTenant.Handler(
            _tenants, _licenses, _users, _roles, _memberships,
            _tokens, _notifier, _hasher, _uow, new TestDateTimeProvider(FixedNow));
    }

    private static SignUpTenant.Command ValidCommand() =>
        new("Acme Corp", "acme-corp", "owner@acme.com", "Ana", "Silva");

    [Fact]
    public async Task Handle_ValidCommand_ShouldProvisionTenantUserAndSendActivation()
    {
        _users.ExistsAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>()).Returns(false);
        _tenants.SlugExistsAsync("acme-corp", Arg.Any<CancellationToken>()).Returns(false);
        _roles.GetByNameAsync(Role.PlatformAdmin, Arg.Any<CancellationToken>())
            .Returns(Role.CreateSystem(RoleId.New(), Role.PlatformAdmin, "Admin"));

        var handler = CreateHandler();
        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Slug.Should().Be("acme-corp");
        result.Value.ActivationEmailSent.Should().BeTrue();

        _tenants.Received(1).Add(Arg.Any<Tenant>());
        _licenses.Received(1).Add(Arg.Is<TenantLicense>(l => l.Plan == TenantPlan.Trial));
        _users.Received(1).Add(Arg.Any<User>());
        _memberships.Received(1).Add(Arg.Any<TenantMembership>());
        _tokens.Received(1).Add(Arg.Any<AccountActivationToken>());
        await _uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
        await _notifier.Received(1).SendAccountActivationAsync(
            "owner@acme.com", "Ana", Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_EmailAlreadyExists_ShouldReturnConflict()
    {
        _users.ExistsAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>()).Returns(true);

        var handler = CreateHandler();
        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("signup.emailInUse");
        _tenants.DidNotReceive().Add(Arg.Any<Tenant>());
        await _uow.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SlugAlreadyExists_ShouldReturnConflict()
    {
        _users.ExistsAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>()).Returns(false);
        _tenants.SlugExistsAsync("acme-corp", Arg.Any<CancellationToken>()).Returns(true);

        var handler = CreateHandler();
        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("signup.slugInUse");
        await _uow.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_RolesNotSeeded_ShouldReturnBusinessError()
    {
        _users.ExistsAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>()).Returns(false);
        _tenants.SlugExistsAsync("acme-corp", Arg.Any<CancellationToken>()).Returns(false);
        _roles.GetByNameAsync(Role.PlatformAdmin, Arg.Any<CancellationToken>())
            .Returns((Role?)null);

        var handler = CreateHandler();
        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("signup.rolesNotSeeded");
        await _uow.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }
}
