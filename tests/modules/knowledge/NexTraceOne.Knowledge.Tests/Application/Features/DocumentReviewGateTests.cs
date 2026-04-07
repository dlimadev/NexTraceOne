using FluentAssertions;
using NSubstitute;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Contracts.DTOs;
using NexTraceOne.Configuration.Domain.Enums;

using ReviewGateFeature = NexTraceOne.Knowledge.Application.Features.ValidateDocumentReviewGate.ValidateDocumentReviewGate;

namespace NexTraceOne.Knowledge.Tests.Application.Features;

/// <summary>
/// Testes de ValidateDocumentReviewGate — gate de revisão de documentos de conhecimento.
/// </summary>
public sealed class DocumentReviewGateTests
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

    [Fact]
    public async Task Should_Not_Require_Review_When_No_Roles_Configured()
    {
        var (config, dt) = CreateMocks();
        config.ResolveEffectiveValueAsync("knowledge.document.review_roles", Arg.Any<ConfigurationScope>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(CreateConfig("knowledge.document.review_roles", "[]"));
        config.ResolveEffectiveValueAsync("knowledge.auto_capture.categories", Arg.Any<ConfigurationScope>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(CreateConfig("knowledge.auto_capture.categories", """["PostMortem"]"""));

        var sut = new ReviewGateFeature.Handler(config, dt);
        var result = await sut.Handle(
            new ReviewGateFeature.Query("Runbook: deploy service X", "Runbook", "author@co.com", false),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ReviewRequired.Should().BeFalse();
        result.Value.IsReviewed.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Require_Review_When_Roles_Configured()
    {
        var (config, dt) = CreateMocks();
        config.ResolveEffectiveValueAsync("knowledge.document.review_roles", Arg.Any<ConfigurationScope>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(CreateConfig("knowledge.document.review_roles", """["TechLead","Architect"]"""));
        config.ResolveEffectiveValueAsync("knowledge.auto_capture.categories", Arg.Any<ConfigurationScope>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(CreateConfig("knowledge.auto_capture.categories", """["PostMortem"]"""));

        var sut = new ReviewGateFeature.Handler(config, dt);
        var result = await sut.Handle(
            new ReviewGateFeature.Query("ADR: Choose ElasticSearch", "DecisionRecord", "author@co.com", false),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ReviewRequired.Should().BeTrue();
        result.Value.IsReviewed.Should().BeFalse();
        result.Value.ReviewRoles.Should().Contain("TechLead");
    }

    [Fact]
    public async Task Should_Pass_Review_When_Already_Reviewed()
    {
        var (config, dt) = CreateMocks();
        config.ResolveEffectiveValueAsync("knowledge.document.review_roles", Arg.Any<ConfigurationScope>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(CreateConfig("knowledge.document.review_roles", """["TechLead"]"""));
        config.ResolveEffectiveValueAsync("knowledge.auto_capture.categories", Arg.Any<ConfigurationScope>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(CreateConfig("knowledge.auto_capture.categories", """["PostMortem"]"""));

        var sut = new ReviewGateFeature.Handler(config, dt);
        var result = await sut.Handle(
            new ReviewGateFeature.Query("Incident Analysis: outage 2025-06", "PostMortem", "author@co.com", true),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ReviewRequired.Should().BeTrue();
        result.Value.IsReviewed.Should().BeTrue();
        result.Value.IsAutoCapturedCategory.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Detect_Auto_Captured_Category()
    {
        var (config, dt) = CreateMocks();
        config.ResolveEffectiveValueAsync("knowledge.document.review_roles", Arg.Any<ConfigurationScope>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(CreateConfig("knowledge.document.review_roles", "[]"));
        config.ResolveEffectiveValueAsync("knowledge.auto_capture.categories", Arg.Any<ConfigurationScope>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(CreateConfig("knowledge.auto_capture.categories", """["ComplianceEvidence","DecisionRecord","ChangeLog"]"""));

        var sut = new ReviewGateFeature.Handler(config, dt);
        var result = await sut.Handle(
            new ReviewGateFeature.Query("Compliance Report Q1", "ComplianceEvidence", "auditor@co.com", false),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsAutoCapturedCategory.Should().BeTrue();
    }
}
