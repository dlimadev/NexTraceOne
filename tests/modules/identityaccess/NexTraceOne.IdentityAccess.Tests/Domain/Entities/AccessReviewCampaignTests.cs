using NexTraceOne.IdentityAccess.Domain.Entities;

namespace NexTraceOne.IdentityAccess.Tests.Domain.Entities;

/// <summary>
/// Testes de domínio para AccessReviewCampaign e AccessReviewItem.
/// Cobre criação de campanha, adição de itens, decisões, auto-revogação por prazo e conclusão.
/// </summary>
public sealed class AccessReviewCampaignTests
{
    private static readonly DateTimeOffset Now = new(2025, 6, 15, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_Should_StartAsOpen_When_CampaignCreated()
    {
        var campaign = AccessReviewCampaign.Create(
            TenantId.New(), "Q2 2025 Access Review", UserId.New(), Now);

        campaign.Status.Should().Be(AccessReviewCampaignStatus.Open);
        campaign.Deadline.Should().Be(Now.Add(AccessReviewCampaign.DefaultReviewDeadline));
        campaign.Items.Should().BeEmpty();
    }

    [Fact]
    public void AddItem_Should_CreatePendingReviewItem_When_Called()
    {
        var campaign = AccessReviewCampaign.Create(
            TenantId.New(), "Q2 2025 Access Review", UserId.New(), Now);

        var item = campaign.AddItem(UserId.New(), RoleId.New(), "Developer", UserId.New());

        campaign.Items.Should().HaveCount(1);
        item.Decision.Should().Be(AccessReviewDecision.Pending);
    }

    [Fact]
    public void Confirm_Should_MarkItemAsConfirmed_When_ReviewerDecides()
    {
        var campaign = AccessReviewCampaign.Create(
            TenantId.New(), "Q2 2025 Access Review", UserId.New(), Now);

        var item = campaign.AddItem(UserId.New(), RoleId.New(), "Developer", UserId.New());
        item.Confirm(UserId.New(), "Access is still required for current project.", Now.AddDays(1));

        item.Decision.Should().Be(AccessReviewDecision.Confirmed);
        item.ReviewerComment.Should().NotBeEmpty();
        item.DecidedAt.Should().Be(Now.AddDays(1));
    }

    [Fact]
    public void Revoke_Should_MarkItemAsRevoked_When_ReviewerDecides()
    {
        var campaign = AccessReviewCampaign.Create(
            TenantId.New(), "Q2 2025 Access Review", UserId.New(), Now);

        var item = campaign.AddItem(UserId.New(), RoleId.New(), "TechLead", UserId.New());
        item.Revoke(UserId.New(), "User moved to a different team.", Now.AddDays(2));

        item.Decision.Should().Be(AccessReviewDecision.Revoked);
    }

    [Fact]
    public void TryComplete_Should_CompleteCampaign_When_AllItemsDecided()
    {
        var campaign = AccessReviewCampaign.Create(
            TenantId.New(), "Q2 2025 Access Review", UserId.New(), Now);

        var item1 = campaign.AddItem(UserId.New(), RoleId.New(), "Developer", UserId.New());
        var item2 = campaign.AddItem(UserId.New(), RoleId.New(), "Viewer", UserId.New());

        item1.Confirm(UserId.New(), null, Now.AddDays(1));
        item2.Revoke(UserId.New(), "No longer needed.", Now.AddDays(1));

        campaign.TryComplete(Now.AddDays(1));

        campaign.Status.Should().Be(AccessReviewCampaignStatus.Completed);
        campaign.CompletedAt.Should().Be(Now.AddDays(1));
    }

    [Fact]
    public void TryComplete_Should_RemainOpen_When_PendingItemsExist()
    {
        var campaign = AccessReviewCampaign.Create(
            TenantId.New(), "Q2 2025 Access Review", UserId.New(), Now);

        campaign.AddItem(UserId.New(), RoleId.New(), "Developer", UserId.New());
        var item2 = campaign.AddItem(UserId.New(), RoleId.New(), "Viewer", UserId.New());
        item2.Confirm(UserId.New(), null, Now.AddDays(1));

        campaign.TryComplete(Now.AddDays(1));

        campaign.Status.Should().Be(AccessReviewCampaignStatus.Open);
    }

    [Fact]
    public void ProcessDeadline_Should_AutoRevokeAndComplete_When_DeadlineExceeded()
    {
        var campaign = AccessReviewCampaign.Create(
            TenantId.New(), "Q2 2025 Access Review", UserId.New(), Now);

        campaign.AddItem(UserId.New(), RoleId.New(), "Developer", UserId.New());
        campaign.AddItem(UserId.New(), RoleId.New(), "TechLead", UserId.New());

        var afterDeadline = Now.Add(AccessReviewCampaign.DefaultReviewDeadline).AddMinutes(1);
        campaign.ProcessDeadline(afterDeadline);

        campaign.Status.Should().Be(AccessReviewCampaignStatus.Completed);
        campaign.Items.Should().AllSatisfy(item =>
        {
            item.Decision.Should().Be(AccessReviewDecision.AutoRevoked);
            item.ReviewerComment.Should().Contain("review deadline exceeded");
        });
    }
}
