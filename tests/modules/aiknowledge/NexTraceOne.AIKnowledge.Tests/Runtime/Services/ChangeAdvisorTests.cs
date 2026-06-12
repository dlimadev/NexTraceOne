using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.SemanticKernel;
using NexTraceOne.AIKnowledge.Application.Features.AIAgents.ChangeAdvisor;
using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;
using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions.SemanticKernel;
using NexTraceOne.BuildingBlocks.Application.Abstractions;

namespace NexTraceOne.AIKnowledge.Tests.Runtime.Services;

public sealed class ChangeAdvisorTests
{
    private readonly IAiKernelService _kernelService = Substitute.For<IAiKernelService>();
    private readonly IAiExecutionGateway _aiGateway = Substitute.For<IAiExecutionGateway>();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();
    private readonly ILogger<ChangeAdvisor.Handler> _logger = NullLogger<ChangeAdvisor.Handler>.Instance;

    public ChangeAdvisorTests()
    {
        _clock.UtcNow.Returns(DateTimeOffset.UtcNow);
        _aiGateway.PreviewExecutionAsync(Arg.Any<AiExecutionRequest>(), Arg.Any<CancellationToken>())
            .Returns(new AiExecutionPlan(
                ProviderType: AiProviderType.Internal,
                ProviderId: "ollama",
                ModelId: "ollama",
                ModelDisplayName: "Ollama",
                IsAvailable: true,
                UnavailabilityReason: null,
                EstimatedCost: null,
                AppliedPolicies: []));
    }

    private ChangeAdvisor.Handler CreateHandler() => new(_kernelService, _aiGateway, _clock, _logger);

    [Fact]
    public async Task Handle_ValidChange_ReturnsStructuredResponse()
    {


        var kernel = Kernel.CreateBuilder().Build();
        _kernelService.CreateKernel("ollama", "ollama").Returns(kernel);
        _kernelService.ExecuteChatAsync(Arg.Any<Kernel>(), Arg.Any<string>(), Arg.Any<IReadOnlyList<ChatMessage>>(), Arg.Any<CancellationToken>())
            .Returns("""
                {
                  "riskLevel": "Medium",
                  "blastRadius": "Team",
                  "readinessScore": 78,
                  "impact": {
                    "userImpact": 2,
                    "dataImpact": 1,
                    "operationalImpact": 3,
                    "complianceImpact": 0,
                    "detailedAnalysis": "Moderate operational impact due to deployment process changes."
                  },
                  "rollback": {
                    "estimatedTime": "10 minutes",
                    "complexity": "Low",
                    "prerequisites": ["Database backup"],
                    "procedure": "Revert migration and redeploy previous version."
                  },
                  "approvalRecommended": true,
                  "mitigations": [
                    { "priority": 1, "action": "Run smoke tests in staging", "targetArea": "Testing", "expectedOutcome": "Catch regressions early" }
                  ],
                  "summary": "Change is well-scoped with manageable risk."
                }
                """);

        var handler = CreateHandler();
        var result = await handler.Handle(
            new ChangeAdvisor.Command(
                "Update payment gateway SDK to v3.0",
                "production",
                "deployment",
                "payment-service, billing-service"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.RiskLevel.Should().Be("Medium");
        result.Value.BlastRadius.Should().Be("Team");
        result.Value.ReadinessScore.Should().Be(78);
        result.Value.Impact.UserImpact.Should().Be(2);
        result.Value.Impact.DataImpact.Should().Be(1);
        result.Value.Impact.OperationalImpact.Should().Be(3);
        result.Value.Impact.ComplianceImpact.Should().Be(0);
        result.Value.Rollback.EstimatedTime.Should().Be("10 minutes");
        result.Value.Rollback.Complexity.Should().Be("Low");
        result.Value.Rollback.Prerequisites.Should().Contain("Database backup");
        result.Value.ApprovalRecommended.Should().BeTrue();
        result.Value.Mitigations.Should().HaveCount(1);
        result.Value.Mitigations[0].Priority.Should().Be(1);
        result.Value.Summary.Should().Contain("manageable risk");

        kernel.Data.Should().ContainKey("GroundingQuery");
    }

    [Fact]
    public async Task Handle_NoProvider_ReturnsNotAvailableError()
    {
        _aiGateway.PreviewExecutionAsync(Arg.Any<AiExecutionRequest>(), Arg.Any<CancellationToken>())
            .Returns(new AiExecutionPlan(
                ProviderType: AiProviderType.Null,
                ProviderId: "null",
                ModelId: "null",
                ModelDisplayName: "Null",
                IsAvailable: false,
                UnavailabilityReason: "No provider available",
                EstimatedCost: null,
                AppliedPolicies: []));

        var handler = CreateHandler();
        var result = await handler.Handle(new ChangeAdvisor.Command("Deploy new feature"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AI.NotAvailable");
    }

    [Fact]
    public async Task Handle_InvalidJson_ReturnsFallbackResponse()
    {
        var kernel = Kernel.CreateBuilder().Build();
        _kernelService.CreateKernel("ollama", "ollama").Returns(kernel);
        _kernelService.ExecuteChatAsync(Arg.Any<Kernel>(), Arg.Any<string>(), Arg.Any<IReadOnlyList<ChatMessage>>(), Arg.Any<CancellationToken>())
            .Returns("This is not valid JSON");

        var handler = CreateHandler();
        var result = await handler.Handle(new ChangeAdvisor.Command("Schema migration"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.RiskLevel.Should().Be("Unknown");
        result.Value.ApprovalRecommended.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_LowReadinessScore_ReturnsLowValue()
    {
        var kernel = Kernel.CreateBuilder().Build();
        _kernelService.CreateKernel("ollama", "ollama").Returns(kernel);

        _kernelService.ExecuteChatAsync(Arg.Any<Kernel>(), Arg.Any<string>(), Arg.Any<IReadOnlyList<ChatMessage>>(), Arg.Any<CancellationToken>())
            .Returns("""
                {
                  "riskLevel": "Critical",
                  "blastRadius": "Organization-Wide",
                  "readinessScore": 15,
                  "impact": {
                    "userImpact": 4,
                    "dataImpact": 3,
                    "operationalImpact": 5,
                    "complianceImpact": 2,
                    "detailedAnalysis": "High risk change with low readiness."
                  },
                  "rollback": {
                    "estimatedTime": "30 minutes",
                    "complexity": "High",
                    "prerequisites": ["Database snapshot", "Config backup"],
                    "procedure": "Restore snapshot and redeploy"
                  },
                  "approvalRecommended": false,
                  "mitigations": [
                    { "priority": 1, "action": "Run full regression suite", "targetArea": "Testing", "expectedOutcome": "Identify issues before production" }
                  ],
                  "summary": "High risk, proceed with caution"
                }
                """);

        var handler = CreateHandler();
        var result = await handler.Handle(new ChangeAdvisor.Command("Major schema migration"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ReadinessScore.Should().Be(15);
        result.Value.RiskLevel.Should().Be("Critical");
        result.Value.ApprovalRecommended.Should().BeFalse();
        result.Value.Impact.OperationalImpact.Should().Be(5);
        result.Value.Rollback.Complexity.Should().Be("High");
    }
}
