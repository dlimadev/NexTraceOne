using FluentAssertions;
using NexTraceOne.Identity.Domain.Entities;
using NexTraceOne.Identity.Domain.Events;
using NexTraceOne.Identity.Domain.ValueObjects;

namespace NexTraceOne.Identity.Tests.Domain.Entities;

/// <summary>
/// Testes de domínio para o aggregate User.
/// </summary>
public sealed class UserTests
{
    [Fact]
    public void CreateLocal_Should_RaiseDomainEvent_When_InputIsValid()
    {
        var user = User.CreateLocal(
            Email.Create("alice@example.com"),
            FullName.Create("Alice", "Doe"),
            HashedPassword.FromPlainText("P@ssw0rd123"));

        user.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<UserCreatedDomainEvent>();
    }

    [Fact]
    public void RegisterFailedLogin_Should_LockUser_When_MaxAttemptsIsReached()
    {
        var user = User.CreateLocal(
            Email.Create("alice@example.com"),
            FullName.Create("Alice", "Doe"),
            HashedPassword.FromPlainText("P@ssw0rd123"));
        var now = DateTimeOffset.UtcNow;

        for (var attempt = 0; attempt < 5; attempt++)
        {
            user.RegisterFailedLogin(now);
        }

        user.IsLocked(now).Should().BeTrue();
        user.DomainEvents.Should().Contain(x => x is UserLockedDomainEvent);
    }

    [Fact]
    public void RegisterSuccessfulLogin_Should_ResetFailureState_When_LoginSucceeds()
    {
        var user = User.CreateLocal(
            Email.Create("alice@example.com"),
            FullName.Create("Alice", "Doe"),
            HashedPassword.FromPlainText("P@ssw0rd123"));
        var now = DateTimeOffset.UtcNow;

        user.RegisterFailedLogin(now);
        user.RegisterSuccessfulLogin(now);

        user.FailedLoginAttempts.Should().Be(0);
        user.LockoutEnd.Should().BeNull();
        user.LastLoginAt.Should().Be(now);
    }
}
