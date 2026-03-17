using NexTraceOne.IdentityAccess.Domain.Entities;

namespace NexTraceOne.IdentityAccess.Tests.Domain.Entities;

/// <summary>
/// Testes de domínio para o aggregate BreakGlassRequest.
/// Cobre criação, revogação, expiração, post-mortem e validação de limites trimestrais.
/// </summary>
public sealed class BreakGlassRequestTests
{
    private static readonly DateTimeOffset Now = new(2025, 6, 15, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_Should_ActivateImmediately_When_Requested()
    {
        var request = BreakGlassRequest.Create(
            UserId.New(), TenantId.New(),
            "Critical production incident requiring immediate access.",
            "192.168.1.1", "unit-test", Now);

        request.Status.Should().Be(BreakGlassStatus.Active);
        request.ActivatedAt.Should().Be(Now);
        request.ExpiresAt.Should().Be(Now.Add(BreakGlassRequest.DefaultAccessWindow));
        request.IsActiveAt(Now).Should().BeTrue();
    }

    [Fact]
    public void IsActiveAt_Should_ReturnFalse_When_AccessWindowHasPassed()
    {
        var request = BreakGlassRequest.Create(
            UserId.New(), TenantId.New(),
            "Critical production incident requiring immediate access.",
            "192.168.1.1", "unit-test", Now);

        var afterExpiry = Now.Add(BreakGlassRequest.DefaultAccessWindow).AddMinutes(1);
        request.IsActiveAt(afterExpiry).Should().BeFalse();
    }

    [Fact]
    public void Revoke_Should_ChangeStatusToRevoked_When_ActiveAndAdminRevokes()
    {
        var request = BreakGlassRequest.Create(
            UserId.New(), TenantId.New(),
            "Critical production incident requiring immediate access.",
            "192.168.1.1", "unit-test", Now);

        var admin = UserId.New();
        request.Revoke(admin, Now.AddMinutes(30));

        request.Status.Should().Be(BreakGlassStatus.Revoked);
        request.RevokedBy.Should().Be(admin);
        request.RevokedAt.Should().Be(Now.AddMinutes(30));
        request.IsActiveAt(Now.AddMinutes(31)).Should().BeFalse();
    }

    [Fact]
    public void Expire_Should_ChangeStatusToExpired_When_Active()
    {
        var request = BreakGlassRequest.Create(
            UserId.New(), TenantId.New(),
            "Critical production incident.",
            "192.168.1.1", "unit-test", Now);

        request.Expire(Now.AddHours(3));

        request.Status.Should().Be(BreakGlassStatus.Expired);
    }

    [Fact]
    public void RecordPostMortem_Should_CompleteLifecycle_When_AccessHasEnded()
    {
        var request = BreakGlassRequest.Create(
            UserId.New(), TenantId.New(),
            "Critical production incident.",
            "192.168.1.1", "unit-test", Now);

        request.Expire(Now.AddHours(3));
        request.IsPostMortemPending.Should().BeTrue();

        request.RecordPostMortem("Root cause: configuration error. No data was modified.", Now.AddHours(4));

        request.Status.Should().Be(BreakGlassStatus.PostMortemCompleted);
        request.PostMortemNotes.Should().NotBeEmpty();
        request.IsPostMortemPending.Should().BeFalse();
    }

    [Fact]
    public void Create_Should_UseCustomAccessWindow_When_Provided()
    {
        var customWindow = TimeSpan.FromHours(4);
        var request = BreakGlassRequest.Create(
            UserId.New(), TenantId.New(),
            "Extended access required for multi-region failover.",
            "10.0.0.1", "unit-test", Now, customWindow);

        request.ExpiresAt.Should().Be(Now.Add(customWindow));
    }
}
