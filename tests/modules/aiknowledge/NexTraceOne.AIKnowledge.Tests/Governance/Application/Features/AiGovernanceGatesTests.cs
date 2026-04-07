using FluentAssertions;
using NSubstitute;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Contracts.DTOs;
using NexTraceOne.Configuration.Domain.Enums;

using ValidateExtModel = NexTraceOne.AIKnowledge.Application.Governance.Features.ValidateExternalModelUsage.ValidateExternalModelUsage;
using ValidateAgent = NexTraceOne.AIKnowledge.Application.Governance.Features.ValidateCustomAgentCreation.ValidateCustomAgentCreation;

namespace NexTraceOne.AIKnowledge.Tests.Governance.Application.Features;

/// <summary>
/// Testes de ValidateExternalModelUsage e ValidateCustomAgentCreation — gates de governança de IA.
/// </summary>
public sealed class AiGovernanceGatesTests
{
    private static readonly DateTimeOffset FixedNow = new(2025, 6, 1, 10, 0, 0, TimeSpan.Zero);

    private static EffectiveConfigurationDto CreateConfig(string key, string? value) =>
        new(key, value, "Tenant", null, false, false, key, "string", false, 1);

    private static (IConfigurationResolutionService config, IDateTimeProvider dt) CreateMocks()
    {
        var config = Substitute.For<IConfigurationResolutionService>();
        var dt = Substitute.For<IDateTimeProvider>();
        dt.UtcNow.Returns(FixedNow);
        return (config, dt);
    }

    // ── External Model Usage ────────────────────────────────

    [Fact]
    public async Task ExternalModel_Should_Allow_Internal_Models()
    {
        var (config, dt) = CreateMocks();
        var sut = new ValidateExtModel.Handler(config, dt);
        var result = await sut.Handle(
            new ValidateExtModel.Query("local-llm", "Internal", IsExternal: false, ContainsSensitiveData: false, HasApproval: false),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsAllowed.Should().BeTrue();
    }

    [Fact]
    public async Task ExternalModel_Should_Block_Without_Approval()
    {
        var (config, dt) = CreateMocks();
        config.ResolveEffectiveValueAsync("ai.external_models.require_approval", Arg.Any<ConfigurationScope>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(CreateConfig("ai.external_models.require_approval", "true"));
        config.ResolveEffectiveValueAsync("ai.data_classification.block_sensitive", Arg.Any<ConfigurationScope>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(CreateConfig("ai.data_classification.block_sensitive", "false"));

        var sut = new ValidateExtModel.Handler(config, dt);
        var result = await sut.Handle(
            new ValidateExtModel.Query("gpt-4", "OpenAI", IsExternal: true, ContainsSensitiveData: false, HasApproval: false),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsAllowed.Should().BeFalse();
        result.Value.Gates.Should().ContainSingle(g => g.GateName == "ExternalModelApproval" && !g.Passed);
    }

    [Fact]
    public async Task ExternalModel_Should_Allow_With_Approval()
    {
        var (config, dt) = CreateMocks();
        config.ResolveEffectiveValueAsync("ai.external_models.require_approval", Arg.Any<ConfigurationScope>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(CreateConfig("ai.external_models.require_approval", "true"));
        config.ResolveEffectiveValueAsync("ai.data_classification.block_sensitive", Arg.Any<ConfigurationScope>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(CreateConfig("ai.data_classification.block_sensitive", "false"));

        var sut = new ValidateExtModel.Handler(config, dt);
        var result = await sut.Handle(
            new ValidateExtModel.Query("gpt-4", "OpenAI", IsExternal: true, ContainsSensitiveData: false, HasApproval: true),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsAllowed.Should().BeTrue();
    }

    [Fact]
    public async Task ExternalModel_Should_Block_Sensitive_Data()
    {
        var (config, dt) = CreateMocks();
        config.ResolveEffectiveValueAsync("ai.external_models.require_approval", Arg.Any<ConfigurationScope>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(CreateConfig("ai.external_models.require_approval", "false"));
        config.ResolveEffectiveValueAsync("ai.data_classification.block_sensitive", Arg.Any<ConfigurationScope>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(CreateConfig("ai.data_classification.block_sensitive", "true"));

        var sut = new ValidateExtModel.Handler(config, dt);
        var result = await sut.Handle(
            new ValidateExtModel.Query("gpt-4", "OpenAI", IsExternal: true, ContainsSensitiveData: true, HasApproval: false),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsAllowed.Should().BeFalse();
        result.Value.Gates.Should().ContainSingle(g => g.GateName == "SensitiveDataBlock" && !g.Passed);
    }

    // ── Custom Agent Creation ──────────────────────────────

    [Fact]
    public async Task CustomAgent_Should_Allow_When_Approval_Not_Required()
    {
        var (config, dt) = CreateMocks();
        config.ResolveEffectiveValueAsync("ai.agents.custom_creation.require_approval", Arg.Any<ConfigurationScope>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(CreateConfig("ai.agents.custom_creation.require_approval", "false"));

        var sut = new ValidateAgent.Handler(config, dt);
        var result = await sut.Handle(
            new ValidateAgent.Query("my-agent", "user@co.com", HasApproval: false),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsAllowed.Should().BeTrue();
        result.Value.ApprovalRequired.Should().BeFalse();
    }

    [Fact]
    public async Task CustomAgent_Should_Block_Without_Approval()
    {
        var (config, dt) = CreateMocks();
        config.ResolveEffectiveValueAsync("ai.agents.custom_creation.require_approval", Arg.Any<ConfigurationScope>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(CreateConfig("ai.agents.custom_creation.require_approval", "true"));

        var sut = new ValidateAgent.Handler(config, dt);
        var result = await sut.Handle(
            new ValidateAgent.Query("my-agent", "user@co.com", HasApproval: false),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsAllowed.Should().BeFalse();
        result.Value.ApprovalRequired.Should().BeTrue();
    }

    [Fact]
    public async Task CustomAgent_Should_Allow_With_Approval()
    {
        var (config, dt) = CreateMocks();
        config.ResolveEffectiveValueAsync("ai.agents.custom_creation.require_approval", Arg.Any<ConfigurationScope>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(CreateConfig("ai.agents.custom_creation.require_approval", "true"));

        var sut = new ValidateAgent.Handler(config, dt);
        var result = await sut.Handle(
            new ValidateAgent.Query("my-agent", "user@co.com", HasApproval: true),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsAllowed.Should().BeTrue();
    }
}
