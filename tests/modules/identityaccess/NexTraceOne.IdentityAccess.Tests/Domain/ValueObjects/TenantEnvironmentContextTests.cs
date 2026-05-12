using NexTraceOne.IdentityAccess.Domain.Entities;
using NexTraceOne.IdentityAccess.Domain.Enums;
using NexTraceOne.IdentityAccess.Domain.ValueObjects;

namespace NexTraceOne.IdentityAccess.Tests.Domain.ValueObjects;

/// <summary>
/// Testes para o value object TenantEnvironmentContext.
/// Cobre criação, igualdade, e comportamentos de negócio (allowsDeepAiAnalysis, isPreProductionCandidate).
/// </summary>
public sealed class TenantEnvironmentContextTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 15, 10, 0, 0, TimeSpan.Zero);
    private static readonly TenantId TenantId = TenantId.New();

    private static NexTraceOne.IdentityAccess.Domain.Entities.Environment CreateEnvironment(EnvironmentProfile profile, bool? isProductionLike = null)
        => NexTraceOne.IdentityAccess.Domain.Entities.Environment.Create(
            TenantId,
            profile.ToString(),
            profile.ToString().ToLower(),
            0,
            Now,
            profile,
            isProductionLike: isProductionLike);

    [Fact]
    public void From_Should_CreateContext_WithCorrectValues()
    {
        var env = CreateEnvironment(EnvironmentProfile.Production);
        var context = TenantEnvironmentContext.From(env);

        context.TenantId.Should().Be(env.TenantId);
        context.EnvironmentId.Should().Be(env.Id);
        context.Profile.Should().Be(EnvironmentProfile.Production);
        context.IsProductionLike.Should().BeTrue();
        context.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Create_Should_AllowExplicitValues()
    {
        var tenantId = TenantId.New();
        var environmentId = EnvironmentId.New();
        var context = TenantEnvironmentContext.Create(
            tenantId, environmentId, EnvironmentProfile.Staging,
            EnvironmentCriticality.High, isProductionLike: false, isActive: true);

        context.TenantId.Should().Be(tenantId);
        context.EnvironmentId.Should().Be(environmentId);
        context.Profile.Should().Be(EnvironmentProfile.Staging);
    }

    [Fact]
    public void Equality_Should_BeBasedOnTenantIdAndEnvironmentId()
    {
        var tenantId = TenantId.New();
        var environmentId = EnvironmentId.New();

        var ctx1 = TenantEnvironmentContext.Create(tenantId, environmentId, EnvironmentProfile.Production, EnvironmentCriticality.Critical, true, true);
        var ctx2 = TenantEnvironmentContext.Create(tenantId, environmentId, EnvironmentProfile.Staging, EnvironmentCriticality.Low, false, false);

        ctx1.Should().Be(ctx2);
    }

    [Fact]
    public void Equality_Should_BeDifferent_WhenEnvironmentIdDiffers()
    {
        var tenantId = TenantId.New();
        var ctx1 = TenantEnvironmentContext.Create(tenantId, EnvironmentId.New(), EnvironmentProfile.Production, EnvironmentCriticality.Critical, true, true);
        var ctx2 = TenantEnvironmentContext.Create(tenantId, EnvironmentId.New(), EnvironmentProfile.Production, EnvironmentCriticality.Critical, true, true);

        ctx1.Should().NotBe(ctx2);
    }

    [Fact]
    public void AllowsDeepAiAnalysis_Should_BeTrue_ForActiveNonProductionEnvironment()
    {
        var env = CreateEnvironment(EnvironmentProfile.Staging);
        var context = TenantEnvironmentContext.From(env);

        context.AllowsDeepAiAnalysis().Should().BeTrue();
    }

    [Fact]
    public void AllowsDeepAiAnalysis_Should_BeFalse_ForProductionEnvironment()
    {
        var env = CreateEnvironment(EnvironmentProfile.Production);
        var context = TenantEnvironmentContext.From(env);

        context.AllowsDeepAiAnalysis().Should().BeFalse();
    }

    [Fact]
    public void RequiresProductionSafeguards_Should_BeTrue_ForProductionLike()
    {
        var env = CreateEnvironment(EnvironmentProfile.Production);
        var context = TenantEnvironmentContext.From(env);

        context.RequiresProductionSafeguards().Should().BeTrue();
    }

    [Theory]
    [InlineData(EnvironmentProfile.Staging, true)]
    [InlineData(EnvironmentProfile.UserAcceptanceTesting, true)]
    [InlineData(EnvironmentProfile.Development, false)]
    [InlineData(EnvironmentProfile.Validation, false)]
    public void IsPreProductionCandidate_Should_ReturnCorrectValue(
        EnvironmentProfile profile, bool expected)
    {
        var env = CreateEnvironment(profile);
        var context = TenantEnvironmentContext.From(env);

        context.IsPreProductionCandidate().Should().Be(expected);
    }

    [Fact]
    public void ToString_Should_ContainTenantAndEnvironmentInfo()
    {
        var env = CreateEnvironment(EnvironmentProfile.Production);
        var context = TenantEnvironmentContext.From(env);

        var str = context.ToString();
        str.Should().Contain("Tenant=");
        str.Should().Contain("Env=");
        str.Should().Contain("Production");
    }

    [Fact]
    public void From_Should_Throw_WhenEnvironmentIsNull()
    {
        var act = () => TenantEnvironmentContext.From(null!);

        act.Should().Throw<ArgumentNullException>();
    }
}

