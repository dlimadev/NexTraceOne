using Microsoft.Extensions.Logging;
using NexTraceOne.AIKnowledge.Application.Orchestration.Features.EvaluateArchitectureFitness;
using NexTraceOne.AIKnowledge.Application.Orchestration.Features.EvaluateDocumentationQuality;
using NexTraceOne.AIKnowledge.Domain.ExternalAI.Ports;

namespace NexTraceOne.AIKnowledge.Tests.Orchestration.Features;

/// <summary>
/// Testes de unidade Phase 6 — AI Quality Gates:
/// EvaluateArchitectureFitness e EvaluateDocumentationQuality.
/// </summary>
public sealed class AiQualityGatesTests
{
    private readonly IExternalAIRoutingPort _routingPort = Substitute.For<IExternalAIRoutingPort>();
    private readonly ILogger<EvaluateArchitectureFitness.Handler> _archLogger =
        Substitute.For<ILogger<EvaluateArchitectureFitness.Handler>>();
    private readonly ILogger<EvaluateDocumentationQuality.Handler> _docLogger =
        Substitute.For<ILogger<EvaluateDocumentationQuality.Handler>>();

    private EvaluateArchitectureFitness.Handler CreateArchitectureHandler() =>
        new(_routingPort, _archLogger);

    private EvaluateDocumentationQuality.Handler CreateDocumentationHandler() =>
        new(_routingPort, _docLogger);

    private static readonly EvaluateArchitectureFitness.CodeFile[] SampleCodeFiles =
    [
        new("UserController.cs", "public class UserController : ControllerBase { [HttpGet] public IActionResult Get() => Ok(); }"),
        new("UserService.cs", "public class UserService { private readonly IUserRepository _repo; public UserService(IUserRepository repo) { _repo = repo; } }")
    ];

    // ── EvaluateArchitectureFitness ───────────────────────────────────────

    [Fact]
    public async Task EvaluateArchitectureFitness_Handle_WithAiResponse_ParsesResponse()
    {
        var aiJson = """
            {
                "overallFitness": "Good",
                "score": 78,
                "violations": [
                    {
                        "rule": "NAMING_CONVENTIONS",
                        "file": "UserController.cs",
                        "severity": "Low",
                        "description": "Handler should implement ICommandHandler",
                        "suggestion": "Rename and implement interface"
                    }
                ],
                "passedChecks": ["CQRS_COMPLIANCE", "SECURITY_BASELINE"]
            }
            """;

        _routingPort.RouteQueryAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(),
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(aiJson);

        var handler = CreateArchitectureHandler();
        var command = new EvaluateArchitectureFitness.Command(Guid.NewGuid(), "UserService", SampleCodeFiles);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.OverallFitness.Should().Be("Good");
        result.Value.Score.Should().Be(78);
        result.Value.Violations.Should().HaveCount(1);
        result.Value.Violations[0].Rule.Should().Be("NAMING_CONVENTIONS");
        result.Value.PassedChecks.Should().Contain("CQRS_COMPLIANCE");
        result.Value.IsFallback.Should().BeFalse();
    }

    [Fact]
    public async Task EvaluateArchitectureFitness_Handle_ProviderUnavailable_ReturnsFallback()
    {
        _routingPort.RouteQueryAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(),
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns<string>(_ => throw new InvalidOperationException("AI provider down"));

        var handler = CreateArchitectureHandler();
        var command = new EvaluateArchitectureFitness.Command(null, "UserService", SampleCodeFiles);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsFallback.Should().BeTrue();
        result.Value.OverallFitness.Should().Be("Pending");
    }

    [Fact]
    public async Task EvaluateArchitectureFitness_Handle_FallbackProviderResponse_ReturnsFallback()
    {
        _routingPort.RouteQueryAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(),
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns("[FALLBACK_PROVIDER_UNAVAILABLE] No provider configured.");

        var handler = CreateArchitectureHandler();
        var command = new EvaluateArchitectureFitness.Command(null, "UserService", SampleCodeFiles);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsFallback.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateArchitectureFitness_Handle_InvalidAiJson_ReturnsFallback()
    {
        _routingPort.RouteQueryAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(),
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns("This is not valid JSON at all.");

        var handler = CreateArchitectureHandler();
        var command = new EvaluateArchitectureFitness.Command(null, "TestService", SampleCodeFiles);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsFallback.Should().BeTrue();
    }

    [Fact]
    public void EvaluateArchitectureFitness_Validator_EmptyServiceName_Fails()
    {
        var validator = new EvaluateArchitectureFitness.Validator();
        var result = validator.Validate(new EvaluateArchitectureFitness.Command(null, "", SampleCodeFiles));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void EvaluateArchitectureFitness_Validator_EmptyFiles_Fails()
    {
        var validator = new EvaluateArchitectureFitness.Validator();
        var result = validator.Validate(new EvaluateArchitectureFitness.Command(null, "Service", []));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task EvaluateArchitectureFitness_Handle_NoViolations_ExcellentFitness()
    {
        var aiJson = """{"overallFitness":"Excellent","score":97,"violations":[],"passedChecks":["BOUNDED_CONTEXT_ISOLATION","DEPENDENCY_DIRECTION","NAMING_CONVENTIONS","IMMUTABILITY","CQRS_COMPLIANCE","SECURITY_BASELINE","TESTABILITY"]}""";

        _routingPort.RouteQueryAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(),
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(aiJson);

        var handler = CreateArchitectureHandler();
        var result = await handler.Handle(new EvaluateArchitectureFitness.Command(null, "CleanService", SampleCodeFiles), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.OverallFitness.Should().Be("Excellent");
        result.Value.Violations.Should().BeEmpty();
        result.Value.PassedChecks.Should().HaveCount(7);
    }

    // ── EvaluateDocumentationQuality ──────────────────────────────────────

    private static readonly EvaluateDocumentationQuality.DocumentFile[] SampleDocFiles =
    [
        new("README.md", "# UserService\n\nA service for managing users.", "markdown"),
        new("UserController.cs", "/// <summary>User management controller.</summary>\npublic class UserController {}", "code")
    ];

    [Fact]
    public async Task EvaluateDocumentationQuality_Handle_WithAiResponse_ParsesResponse()
    {
        var aiJson = """
            {
                "overallScore": 72,
                "dimensions": [
                    {
                        "name": "XML_DOC_COVERAGE",
                        "score": 85,
                        "gaps": ["Missing summary on UserController.Create"],
                        "recommendations": ["Add XML doc to all public methods"]
                    },
                    {
                        "name": "README_COMPLETENESS",
                        "score": 60,
                        "gaps": ["Missing setup instructions", "Missing API overview"],
                        "recommendations": ["Add Getting Started section", "Add API reference"]
                    }
                ]
            }
            """;

        _routingPort.RouteQueryAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(),
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(aiJson);

        var handler = CreateDocumentationHandler();
        var result = await handler.Handle(new EvaluateDocumentationQuality.Command("UserService", SampleDocFiles), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.OverallScore.Should().Be(72);
        result.Value.QualityLevel.Should().Be("Needs Improvement");
        result.Value.Dimensions.Should().HaveCount(2);
        result.Value.Dimensions[0].Name.Should().Be("XML_DOC_COVERAGE");
        result.Value.IsFallback.Should().BeFalse();
    }

    [Fact]
    public async Task EvaluateDocumentationQuality_Handle_ProviderUnavailable_ReturnsFallback()
    {
        _routingPort.RouteQueryAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(),
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns<string>(_ => throw new InvalidOperationException("Network error"));

        var handler = CreateDocumentationHandler();
        var result = await handler.Handle(new EvaluateDocumentationQuality.Command("Svc", SampleDocFiles), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsFallback.Should().BeTrue();
    }

    [Theory]
    [InlineData(95, "Excellent")]
    [InlineData(80, "Good")]
    [InlineData(65, "Needs Improvement")]
    [InlineData(40, "Poor")]
    public async Task EvaluateDocumentationQuality_ScoreToLevel_ReturnsCorrectLevel(int score, string expectedLevel)
    {
        var aiJson = $"{{\"overallScore\":{score},\"dimensions\":[]}}";

        _routingPort.RouteQueryAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(),
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(aiJson);

        var handler = CreateDocumentationHandler();
        var result = await handler.Handle(new EvaluateDocumentationQuality.Command("Svc", SampleDocFiles), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.QualityLevel.Should().Be(expectedLevel);
    }

    [Fact]
    public void EvaluateDocumentationQuality_Validator_EmptyServiceName_Fails()
    {
        var validator = new EvaluateDocumentationQuality.Validator();
        var result = validator.Validate(new EvaluateDocumentationQuality.Command("", SampleDocFiles));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void EvaluateDocumentationQuality_Validator_EmptyFiles_Fails()
    {
        var validator = new EvaluateDocumentationQuality.Validator();
        var result = validator.Validate(new EvaluateDocumentationQuality.Command("Service", []));
        result.IsValid.Should().BeFalse();
    }
}
