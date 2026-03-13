using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Identity.Application.Abstractions;
using NexTraceOne.Identity.Domain.Entities;
using NexTraceOne.Identity.Domain.ValueObjects;
using NexTraceOne.Identity.Tests.TestDoubles;
using GetCurrentUserFeature = NexTraceOne.Identity.Application.Features.GetCurrentUser.GetCurrentUser;

namespace NexTraceOne.Identity.Tests.Application.Features;

/// <summary>
/// Testes da feature GetCurrentUser (/me).
/// Cobre cenário de sucesso com perfil completo, utilizador não encontrado
/// e utilizador não autenticado.
/// </summary>
public sealed class GetCurrentUserTests
{
    private readonly DateTimeOffset _now = new(2025, 03, 10, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Handle_Should_ReturnProfile_When_UserIsAuthenticated()
    {
        var user = User.CreateLocal(
            Email.Create("alice@example.com"),
            FullName.Create("Alice", "Doe"),
            HashedPassword.FromPlainText("P@ssw0rd123"));

        var tenantId = TenantId.From(Guid.NewGuid());
        var roleId = RoleId.New();
        var membership = TenantMembership.Create(user.Id, tenantId, roleId, _now);
        var role = Role.CreateSystem(roleId, Role.PlatformAdmin, "Administrative access");

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.IsAuthenticated.Returns(true);
        currentUser.Id.Returns(user.Id.Value.ToString());

        var userRepository = Substitute.For<IUserRepository>();
        var roleRepository = Substitute.For<IRoleRepository>();
        var responseBuilder = Substitute.For<ILoginResponseBuilder>();

        userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        responseBuilder.ResolveMembershipAsync(user.Id, Arg.Any<CancellationToken>()).Returns(membership);
        roleRepository.GetByIdAsync(roleId, Arg.Any<CancellationToken>()).Returns(role);

        var sut = new GetCurrentUserFeature.Handler(currentUser, userRepository, roleRepository, responseBuilder);

        var result = await sut.Handle(new GetCurrentUserFeature.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Email.Should().Be("alice@example.com");
        result.Value.FirstName.Should().Be("Alice");
        result.Value.LastName.Should().Be("Doe");
        result.Value.RoleName.Should().Be(Role.PlatformAdmin);
        result.Value.Permissions.Should().NotBeEmpty();
        result.Value.TenantId.Should().Be(tenantId.Value);
    }

    [Fact]
    public async Task Handle_Should_ReturnError_When_UserNotFound()
    {
        var userId = Guid.NewGuid();

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.IsAuthenticated.Returns(true);
        currentUser.Id.Returns(userId.ToString());

        var userRepository = Substitute.For<IUserRepository>();
        var roleRepository = Substitute.For<IRoleRepository>();
        var responseBuilder = Substitute.For<ILoginResponseBuilder>();

        userRepository.GetByIdAsync(UserId.From(userId), Arg.Any<CancellationToken>())
            .Returns((User?)null);

        var sut = new GetCurrentUserFeature.Handler(currentUser, userRepository, roleRepository, responseBuilder);

        var result = await sut.Handle(new GetCurrentUserFeature.Query(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.User.NotFound");
    }

    [Fact]
    public async Task Handle_Should_ReturnError_When_NotAuthenticated()
    {
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.IsAuthenticated.Returns(false);

        var userRepository = Substitute.For<IUserRepository>();
        var roleRepository = Substitute.For<IRoleRepository>();
        var responseBuilder = Substitute.For<ILoginResponseBuilder>();

        var sut = new GetCurrentUserFeature.Handler(currentUser, userRepository, roleRepository, responseBuilder);

        var result = await sut.Handle(new GetCurrentUserFeature.Query(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.Auth.NotAuthenticated");
    }

    [Fact]
    public async Task Handle_Should_ReturnEmptyPermissions_When_NoMembership()
    {
        var user = User.CreateLocal(
            Email.Create("lonely@example.com"),
            FullName.Create("Lonely", "User"),
            HashedPassword.FromPlainText("P@ssw0rd123"));

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.IsAuthenticated.Returns(true);
        currentUser.Id.Returns(user.Id.Value.ToString());

        var userRepository = Substitute.For<IUserRepository>();
        var roleRepository = Substitute.For<IRoleRepository>();
        var responseBuilder = Substitute.For<ILoginResponseBuilder>();

        userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        responseBuilder.ResolveMembershipAsync(user.Id, Arg.Any<CancellationToken>())
            .Returns((TenantMembership?)null);

        var sut = new GetCurrentUserFeature.Handler(currentUser, userRepository, roleRepository, responseBuilder);

        var result = await sut.Handle(new GetCurrentUserFeature.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.RoleName.Should().BeEmpty();
        result.Value.Permissions.Should().BeEmpty();
    }
}
