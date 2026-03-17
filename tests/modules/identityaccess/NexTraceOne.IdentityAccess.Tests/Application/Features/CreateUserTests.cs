using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Domain.Entities;
using NexTraceOne.IdentityAccess.Domain.ValueObjects;
using NexTraceOne.IdentityAccess.Tests.TestDoubles;

using CreateUserFeature = NexTraceOne.IdentityAccess.Application.Features.CreateUser.CreateUser;

namespace NexTraceOne.IdentityAccess.Tests.Application.Features;

/// <summary>
/// Testes da feature CreateUser.
/// </summary>
public sealed class CreateUserTests
{
    [Fact]
    public async Task Handle_Should_CreateUser_When_EmailIsAvailable()
    {
        var now = new DateTimeOffset(2025, 01, 10, 10, 0, 0, TimeSpan.Zero);
        var role = Role.CreateSystem(RoleId.New(), Role.Developer, "Developer access");
        var userRepository = Substitute.For<IUserRepository>();
        var roleRepository = Substitute.For<IRoleRepository>();
        var membershipRepository = Substitute.For<ITenantMembershipRepository>();
        var passwordHasher = Substitute.For<IPasswordHasher>();
        var sut = new CreateUserFeature.Handler(
            userRepository,
            roleRepository,
            membershipRepository,
            new TestDateTimeProvider(now),
            passwordHasher);

        userRepository.ExistsAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>()).Returns(false);
        roleRepository.GetByIdAsync(Arg.Any<RoleId>(), Arg.Any<CancellationToken>()).Returns(role);
        passwordHasher.Hash("P@ssw0rd123").Returns(HashedPassword.FromPlainText("P@ssw0rd123").Value);

        var result = await sut.Handle(
            new CreateUserFeature.Command("alice@example.com", "Alice", "Doe", "P@ssw0rd123", Guid.NewGuid(), role.Id.Value),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        userRepository.Received(1).Add(Arg.Any<User>());
        membershipRepository.Received(1).Add(Arg.Any<TenantMembership>());
    }

    [Fact]
    public async Task Handle_Should_ReturnConflict_When_EmailAlreadyExists()
    {
        var userRepository = Substitute.For<IUserRepository>();
        var sut = new CreateUserFeature.Handler(
            userRepository,
            Substitute.For<IRoleRepository>(),
            Substitute.For<ITenantMembershipRepository>(),
            new TestDateTimeProvider(DateTimeOffset.UtcNow),
            Substitute.For<IPasswordHasher>());

        userRepository.ExistsAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>()).Returns(true);

        var result = await sut.Handle(
            new CreateUserFeature.Command("alice@example.com", "Alice", "Doe", "P@ssw0rd123", Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.User.EmailAlreadyExists");
    }
}
