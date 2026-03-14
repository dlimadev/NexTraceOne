using FluentAssertions;
using NexTraceOne.CommercialCatalog.Domain.Entities;
using NexTraceOne.Licensing.Domain.Enums;
using Xunit;

namespace NexTraceOne.CommercialGovernance.Tests.Domain.Entities;

/// <summary>
/// Testes unitários para as entidades do subdomínio CommercialCatalog.
/// Cobre Plan, FeaturePack, FeaturePackItem e PlanFeaturePackMapping.
/// </summary>
public sealed class CommercialCatalogTests
{
    // ─── Plan ────────────────────────────────────────────────────────

    [Fact]
    public void Plan_Create_ShouldSetProperties()
    {
        var plan = Plan.Create(
            "enterprise-annual",
            "Enterprise Annual",
            CommercialModel.Subscription,
            DeploymentModel.SelfHosted,
            maxActivations: 10,
            gracePeriodDays: 30,
            description: "Plano enterprise anual",
            trialDurationDays: 14,
            priceTag: "R$ 2.500/mês");

        plan.Id.Value.Should().NotBeEmpty();
        plan.Code.Should().Be("enterprise-annual");
        plan.Name.Should().Be("Enterprise Annual");
        plan.CommercialModel.Should().Be(CommercialModel.Subscription);
        plan.DeploymentModel.Should().Be(DeploymentModel.SelfHosted);
        plan.MaxActivations.Should().Be(10);
        plan.GracePeriodDays.Should().Be(30);
        plan.Description.Should().Be("Plano enterprise anual");
        plan.TrialDurationDays.Should().Be(14);
        plan.PriceTag.Should().Be("R$ 2.500/mês");
        plan.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Plan_Activate_ShouldSetIsActiveTrue()
    {
        var plan = Plan.Create(
            "starter", "Starter", CommercialModel.Subscription,
            DeploymentModel.SaaS, 1, 0);
        plan.Deactivate();

        plan.Activate();

        plan.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Plan_Deactivate_ShouldSetIsActiveFalse()
    {
        var plan = Plan.Create(
            "starter", "Starter", CommercialModel.Subscription,
            DeploymentModel.SaaS, 1, 0);

        plan.Deactivate();

        plan.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Plan_UpdateDetails_ShouldUpdateNameAndDescription()
    {
        var plan = Plan.Create(
            "starter", "Starter", CommercialModel.Subscription,
            DeploymentModel.SaaS, 1, 0);

        plan.UpdateDetails("Starter Pro", "Updated description", "R$ 500/mês");

        plan.Name.Should().Be("Starter Pro");
        plan.Description.Should().Be("Updated description");
        plan.PriceTag.Should().Be("R$ 500/mês");
    }

    // ─── FeaturePack ─────────────────────────────────────────────────

    [Fact]
    public void FeaturePack_Create_ShouldSetProperties()
    {
        var pack = FeaturePack.Create(
            "api-governance-pack",
            "API Governance Pack",
            "Pacote de governança de APIs");

        pack.Id.Value.Should().NotBeEmpty();
        pack.Code.Should().Be("api-governance-pack");
        pack.Name.Should().Be("API Governance Pack");
        pack.Description.Should().Be("Pacote de governança de APIs");
        pack.IsActive.Should().BeTrue();
        pack.Items.Should().BeEmpty();
    }

    [Fact]
    public void FeaturePack_AddItem_ShouldAddToItems()
    {
        var pack = FeaturePack.Create("pack-001", "Pack 001");

        var item = pack.AddItem("catalog:read", "Catalog Read", 100);

        pack.Items.Should().ContainSingle();
        item.CapabilityCode.Should().Be("catalog:read");
        item.CapabilityName.Should().Be("Catalog Read");
        item.DefaultLimit.Should().Be(100);
    }

    [Fact]
    public void FeaturePack_RemoveItem_ShouldRemoveFromItems()
    {
        var pack = FeaturePack.Create("pack-001", "Pack 001");
        pack.AddItem("catalog:read", "Catalog Read");
        pack.AddItem("catalog:write", "Catalog Write");

        pack.RemoveItem("catalog:read");

        pack.Items.Should().ContainSingle();
        pack.Items[0].CapabilityCode.Should().Be("catalog:write");
    }

    [Fact]
    public void FeaturePack_AddItem_DuplicateCode_ShouldFail()
    {
        var pack = FeaturePack.Create("pack-001", "Pack 001");
        pack.AddItem("catalog:read", "Catalog Read");

        var act = () => pack.AddItem("catalog:read", "Catalog Read Duplicate");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*already exists*");
    }

    // ─── FeaturePackItem ─────────────────────────────────────────────

    [Fact]
    public void FeaturePackItem_Create_ShouldSetProperties()
    {
        var packId = FeaturePackId.New();
        var item = FeaturePackItem.Create(packId, "contracts:diff", "Contract Diff", 50);

        item.Id.Value.Should().NotBeEmpty();
        item.FeaturePackId.Should().Be(packId);
        item.CapabilityCode.Should().Be("contracts:diff");
        item.CapabilityName.Should().Be("Contract Diff");
        item.DefaultLimit.Should().Be(50);
    }

    // ─── PlanFeaturePackMapping ──────────────────────────────────────

    [Fact]
    public void PlanFeaturePackMapping_Create_ShouldSetProperties()
    {
        var planId = PlanId.New();
        var featurePackId = FeaturePackId.New();

        var mapping = PlanFeaturePackMapping.Create(planId, featurePackId);

        mapping.Id.Value.Should().NotBeEmpty();
        mapping.PlanId.Should().Be(planId);
        mapping.FeaturePackId.Should().Be(featurePackId);
    }
}
