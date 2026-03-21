using NexTraceOne.IdentityAccess.Domain.Entities;
using NexTraceOne.IdentityAccess.Domain.Enums;

using DomainEnvironment = NexTraceOne.IdentityAccess.Domain.Entities.Environment;

namespace NexTraceOne.IdentityAccess.Tests.Domain.Entities;

/// <summary>
/// Testes de domínio para a entidade Environment — campos e comportamentos da Fase 1.
/// Cobre EnvironmentProfile, EnvironmentCriticality, IsProductionLike, Code, Region e Description.
/// </summary>
public sealed class EnvironmentPhase1Tests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 15, 10, 0, 0, TimeSpan.Zero);
    private static readonly TenantId TenantId = TenantId.New();

    [Fact]
    public void Create_LegacyOverload_Should_DefaultToDevProfile_When_ProfileNotProvided()
    {
        var env = DomainEnvironment.Create(TenantId, "DEV", "dev", 0, Now);

        env.Profile.Should().Be(EnvironmentProfile.Development);
        env.Criticality.Should().Be(EnvironmentCriticality.Low);
        env.IsProductionLike.Should().BeFalse();
        env.Code.Should().BeNull();
        env.Region.Should().BeNull();
        env.Description.Should().BeNull();
    }

    [Fact]
    public void Create_FullOverload_Should_SetProfileAndCriticality_When_Provided()
    {
        var env = DomainEnvironment.Create(
            TenantId, "PROD-BR", "prod-br", 10, Now,
            profile: EnvironmentProfile.Production,
            criticality: EnvironmentCriticality.Critical,
            code: "PROD-BR",
            region: "br-east-1",
            description: "Ambiente de produção Brasil");

        env.Profile.Should().Be(EnvironmentProfile.Production);
        env.Criticality.Should().Be(EnvironmentCriticality.Critical);
        env.IsProductionLike.Should().BeTrue();
        env.Code.Should().Be("PROD-BR");
        env.Region.Should().Be("br-east-1");
        env.Description.Should().Be("Ambiente de produção Brasil");
    }

    [Fact]
    public void Create_Should_AutoSetIsProductionLike_When_ProfileIsProduction()
    {
        var env = DomainEnvironment.Create(TenantId, "PROD", "prod", 5, Now, EnvironmentProfile.Production);

        env.IsProductionLike.Should().BeTrue();
    }

    [Fact]
    public void Create_Should_AutoSetIsProductionLike_When_ProfileIsDisasterRecovery()
    {
        var env = DomainEnvironment.Create(TenantId, "DR", "dr", 8, Now, EnvironmentProfile.DisasterRecovery);

        env.IsProductionLike.Should().BeTrue();
    }

    [Fact]
    public void Create_Should_NotSetIsProductionLike_When_ProfileIsValidation()
    {
        var env = DomainEnvironment.Create(TenantId, "QA", "qa", 2, Now, EnvironmentProfile.Validation);

        env.IsProductionLike.Should().BeFalse();
    }

    [Fact]
    public void Create_Should_OverrideAutoIsProductionLike_When_ExplicitValueProvided()
    {
        var env = DomainEnvironment.Create(
            TenantId, "STAGING", "staging", 3, Now,
            EnvironmentProfile.Staging,
            isProductionLike: true);

        env.IsProductionLike.Should().BeTrue();
    }

    [Fact]
    public void UpdateProfile_Should_UpdateProfileCriticalityAndIsProductionLike()
    {
        var env = DomainEnvironment.Create(TenantId, "QA", "qa", 2, Now, EnvironmentProfile.Validation);

        env.UpdateProfile(EnvironmentProfile.Production, EnvironmentCriticality.Critical);

        env.Profile.Should().Be(EnvironmentProfile.Production);
        env.Criticality.Should().Be(EnvironmentCriticality.Critical);
        env.IsProductionLike.Should().BeTrue();
    }

    [Fact]
    public void UpdateLocationInfo_Should_UpdateCodeRegionAndDescription()
    {
        var env = DomainEnvironment.Create(TenantId, "DEV", "dev", 0, Now, EnvironmentProfile.Development);

        env.UpdateLocationInfo("DEV-EU", "eu-west-1", "Dev Europe");

        env.Code.Should().Be("DEV-EU");
        env.Region.Should().Be("eu-west-1");
        env.Description.Should().Be("Dev Europe");
    }

    [Theory]
    [InlineData(EnvironmentProfile.Development, false)]
    [InlineData(EnvironmentProfile.Validation, false)]
    [InlineData(EnvironmentProfile.Staging, false)]
    [InlineData(EnvironmentProfile.Production, true)]
    [InlineData(EnvironmentProfile.DisasterRecovery, true)]
    [InlineData(EnvironmentProfile.Sandbox, false)]
    [InlineData(EnvironmentProfile.Training, false)]
    [InlineData(EnvironmentProfile.UserAcceptanceTesting, false)]
    public void Create_Should_InferIsProductionLike_Correctly_ForAllProfiles(
        EnvironmentProfile profile, bool expectedProductionLike)
    {
        var env = DomainEnvironment.Create(TenantId, "Env", "env", 0, Now, profile);

        env.IsProductionLike.Should().Be(expectedProductionLike);
    }

    [Fact]
    public void Create_Should_NormalizeSlugToLowercase_WhenCreatedWithFullOverload()
    {
        var env = DomainEnvironment.Create(TenantId, "QA-EUROPA", "QA-EUROPA", 2, Now, EnvironmentProfile.Validation);

        env.Slug.Should().Be("qa-europa");
    }

    [Fact]
    public void Create_Should_Fail_When_TenantIdIsNull()
    {
        var act = () => DomainEnvironment.Create(null!, "name", "slug", 0, Now, EnvironmentProfile.Development);

        act.Should().Throw<Exception>();
    }
}
