using Microsoft.Extensions.Logging;
using NexTraceOne.AIKnowledge.Application.Orchestration.Features.GenerateArchitectureDecisionRecord;
using NexTraceOne.AIKnowledge.Application.Orchestration.Features.RecommendTemplateForService;
using NexTraceOne.AIKnowledge.Domain.ExternalAI.Ports;

namespace NexTraceOne.AIKnowledge.Tests.Orchestration.Features;

/// <summary>
/// Testes de unidade Phase 7 — Acceleration Features:
/// GenerateArchitectureDecisionRecord e RecommendTemplateForService.
/// </summary>
public sealed class AccelerationFeaturesTests
{
    private readonly IExternalAIRoutingPort _routingPort = Substitute.For<IExternalAIRoutingPort>();
    private readonly ILogger<GenerateArchitectureDecisionRecord.Handler> _adrLogger =
        Substitute.For<ILogger<GenerateArchitectureDecisionRecord.Handler>>();
    private readonly ILogger<RecommendTemplateForService.Handler> _recLogger =
        Substitute.For<ILogger<RecommendTemplateForService.Handler>>();

    private GenerateArchitectureDecisionRecord.Handler CreateAdrHandler() =>
        new(_routingPort, _adrLogger);

    private RecommendTemplateForService.Handler CreateRecommendHandler() =>
        new(_routingPort, _recLogger);

    private static readonly RecommendTemplateForService.TemplateInfo[] SampleTemplates =
    [
        new("dotnet-rest-api", ".NET REST API", "Standard .NET REST API", "RestApi", "dotnet", ["rest", "csharp", "openapi"]),
        new("nodejs-express", "Node.js Express", "Express.js REST API", "RestApi", "nodejs", ["rest", "javascript", "express"]),
        new("java-spring-boot", "Java Spring Boot", "Spring Boot REST API", "RestApi", "java", ["rest", "java", "spring"]),
    ];

    // ── GenerateArchitectureDecisionRecord ────────────────────────────────

    [Fact]
    public async Task GenerateAdr_Handle_WithAiResponse_ReturnsParsedAdr()
    {
        var adrMarkdown = """
            # ADR-001: Use Clean Architecture for UserService

            Date: 2026-04-06
            Status: Accepted

            ## Context
            The team needs a scalable architecture for the new UserService.

            ## Decision
            We will use Clean Architecture with Domain-Driven Design.

            ## Consequences
            - Positive: Clear separation of concerns
            - Negative: More initial boilerplate
            """;

        _routingPort.RouteQueryAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(),
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(adrMarkdown);

        var handler = CreateAdrHandler();
        var command = new GenerateArchitectureDecisionRecord.Command(
            "UserService",
            "Deciding on architecture for a new user management service",
            "Clean Architecture",
            ".NET 10 + EF Core",
            "dotnet-rest-api");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ServiceName.Should().Be("UserService");
        result.Value.MarkdownContent.Should().Contain("Clean Architecture");
        result.Value.SuggestedFilename.Should().StartWith("docs/adr/");
        result.Value.IsFallback.Should().BeFalse();
    }

    [Fact]
    public async Task GenerateAdr_Handle_ProviderUnavailable_ReturnsFallback()
    {
        _routingPort.RouteQueryAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(),
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns<string>(_ => throw new InvalidOperationException("Provider down"));

        var handler = CreateAdrHandler();
        var command = new GenerateArchitectureDecisionRecord.Command(
            "PaymentService",
            "Architecture decision for payment processing");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsFallback.Should().BeTrue();
        result.Value.MarkdownContent.Should().Contain("Draft");
        result.Value.MarkdownContent.Should().Contain("PaymentService");
    }

    [Fact]
    public async Task GenerateAdr_Handle_FallbackResponse_ReturnsFallback()
    {
        _routingPort.RouteQueryAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(),
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns("[FALLBACK_PROVIDER_UNAVAILABLE] No AI configured.");

        var handler = CreateAdrHandler();
        var result = await handler.Handle(
            new GenerateArchitectureDecisionRecord.Command("Svc", "Context"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsFallback.Should().BeTrue();
    }

    [Fact]
    public void GenerateAdr_Validator_EmptyServiceName_Fails()
    {
        var validator = new GenerateArchitectureDecisionRecord.Validator();
        var result = validator.Validate(new GenerateArchitectureDecisionRecord.Command("", "Context"));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void GenerateAdr_Validator_EmptyContext_Fails()
    {
        var validator = new GenerateArchitectureDecisionRecord.Validator();
        var result = validator.Validate(new GenerateArchitectureDecisionRecord.Command("Service", ""));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task GenerateAdr_Handle_SuggestedFilenameIsSanitized()
    {
        _routingPort.RouteQueryAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(),
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns("[FALLBACK_PROVIDER_UNAVAILABLE]");

        var handler = CreateAdrHandler();
        var result = await handler.Handle(
            new GenerateArchitectureDecisionRecord.Command("My Service With Spaces", "Context"),
            CancellationToken.None);

        result.Value.SuggestedFilename.Should().NotContain(" ");
        result.Value.SuggestedFilename.Should().StartWith("docs/adr/");
    }

    // ── RecommendTemplateForService ───────────────────────────────────────

    [Fact]
    public async Task RecommendTemplate_Handle_WithAiResponse_ParsesRecommendations()
    {
        var aiJson = """
            {
                "recommendations": [
                    {
                        "slug": "dotnet-rest-api",
                        "score": 92,
                        "reason": "Best fit for a .NET service with REST endpoints",
                        "fitSummary": "Excellent match for enterprise .NET development",
                        "potentialGaps": ["No GraphQL support out of the box"]
                    },
                    {
                        "slug": "nodejs-express",
                        "score": 65,
                        "reason": "Good for lightweight APIs but less suitable for .NET teams",
                        "fitSummary": "Alternative if lightweight is preferred",
                        "potentialGaps": ["Different language ecosystem", "Less DDD support"]
                    }
                ]
            }
            """;

        _routingPort.RouteQueryAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(),
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(aiJson);

        var handler = CreateRecommendHandler();
        var command = new RecommendTemplateForService.Command(
            "A microservice for managing user accounts with REST endpoints and EF Core",
            "dotnet",
            "Identity",
            "Core Team",
            SampleTemplates);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Recommendations.Should().HaveCount(2);
        result.Value.Recommendations[0].Slug.Should().Be("dotnet-rest-api");
        result.Value.Recommendations[0].Score.Should().Be(92);
        result.Value.IsFallback.Should().BeFalse();
    }

    [Fact]
    public async Task RecommendTemplate_Handle_ProviderUnavailable_ReturnsFallbackWithFirstTemplates()
    {
        _routingPort.RouteQueryAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(),
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns<string>(_ => throw new InvalidOperationException("Timeout"));

        var handler = CreateRecommendHandler();
        var result = await handler.Handle(
            new RecommendTemplateForService.Command("A user service", null, null, null, SampleTemplates, 2),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsFallback.Should().BeTrue();
        result.Value.Recommendations.Should().HaveCount(2); // limited to MaxRecommendations=2
    }

    [Fact]
    public async Task RecommendTemplate_Handle_ReturnsOrderedByScore()
    {
        var aiJson = """
            {
                "recommendations": [
                    { "slug": "nodejs-express", "score": 50, "reason": "ok", "fitSummary": "ok", "potentialGaps": [] },
                    { "slug": "dotnet-rest-api", "score": 95, "reason": "best", "fitSummary": "best", "potentialGaps": [] }
                ]
            }
            """;

        _routingPort.RouteQueryAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(),
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(aiJson);

        var handler = CreateRecommendHandler();
        var result = await handler.Handle(
            new RecommendTemplateForService.Command("A .NET user service", "dotnet", null, null, SampleTemplates),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Recommendations.Should().BeInDescendingOrder(r => r.Score);
        result.Value.Recommendations[0].Slug.Should().Be("dotnet-rest-api");
    }

    [Fact]
    public void RecommendTemplate_Validator_EmptyDescription_Fails()
    {
        var validator = new RecommendTemplateForService.Validator();
        var result = validator.Validate(new RecommendTemplateForService.Command("", null, null, null, SampleTemplates));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void RecommendTemplate_Validator_EmptyTemplates_Fails()
    {
        var validator = new RecommendTemplateForService.Validator();
        var result = validator.Validate(new RecommendTemplateForService.Command("A service", null, null, null, []));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void RecommendTemplate_Validator_MaxRecommendations_OutOfRange_Fails()
    {
        var validator = new RecommendTemplateForService.Validator();
        var result = validator.Validate(new RecommendTemplateForService.Command("A service", null, null, null, SampleTemplates, 20));
        result.IsValid.Should().BeFalse();
    }
}
