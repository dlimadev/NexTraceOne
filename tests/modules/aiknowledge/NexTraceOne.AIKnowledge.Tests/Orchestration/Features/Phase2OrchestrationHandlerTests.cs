using Microsoft.Extensions.Logging;
using NexTraceOne.AIKnowledge.Application.Orchestration.Abstractions;
using NexTraceOne.AIKnowledge.Application.Orchestration.Features.GenerateRobotFrameworkDraft;
using NexTraceOne.AIKnowledge.Application.Orchestration.Features.GenerateTestScenarios;
using NexTraceOne.AIKnowledge.Application.Orchestration.Features.GetAiConversationHistory;
using NexTraceOne.AIKnowledge.Application.Orchestration.Features.SummarizeReleaseForApproval;
using NexTraceOne.AIKnowledge.Application.Orchestration.Features.ValidateKnowledgeCapture;
using NexTraceOne.AIKnowledge.Domain.ExternalAI.Ports;
using NexTraceOne.AIKnowledge.Domain.Orchestration.Entities;
using NexTraceOne.AIKnowledge.Domain.Orchestration.Enums;
using NexTraceOne.BuildingBlocks.Application.Abstractions;

namespace NexTraceOne.AIKnowledge.Tests.Orchestration.Features;

/// <summary>
/// Testes unitários dos handlers de Orquestração implementados na Fase 2.
/// Valida: GetAiConversationHistory, ValidateKnowledgeCapture, GenerateTestScenarios,
/// GenerateRobotFrameworkDraft, SummarizeReleaseForApproval.
/// </summary>
public sealed class Phase2OrchestrationHandlerTests
{
    private static readonly DateTimeOffset FixedNow = new(2025, 6, 1, 10, 0, 0, TimeSpan.Zero);
    private static readonly Guid ReleaseId = Guid.NewGuid();

    // ── GetAiConversationHistory ──────────────────────────────────────────

    [Fact]
    public async Task GetHistory_ShouldReturnPaginatedConversations()
    {
        var repo = Substitute.For<IAiOrchestrationConversationRepository>();

        var conversations = new List<AiConversation>
        {
            AiConversation.Start("PaymentService", "Change analysis for v2", "user@test.io", FixedNow, ReleaseId),
            AiConversation.Start("OrderService", "Incident investigation", "user@test.io", FixedNow, null)
        };

        repo.ListHistoryAsync(null, null, null, null, null, null, 1, 20, Arg.Any<CancellationToken>())
            .Returns((conversations, 2));

        var handler = new GetAiConversationHistory.Handler(repo);
        var result = await handler.Handle(
            new GetAiConversationHistory.Query(null, null, null, null, null, null, 1, 20),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(2);
        result.Value.TotalCount.Should().Be(2);
        result.Value.TotalPages.Should().Be(1);
    }

    [Fact]
    public async Task GetHistory_ShouldReturnEmpty_WhenNoConversations()
    {
        var repo = Substitute.For<IAiOrchestrationConversationRepository>();
        repo.ListHistoryAsync(Arg.Any<Guid?>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<ConversationStatus?>(), Arg.Any<DateTimeOffset?>(), Arg.Any<DateTimeOffset?>(),
            Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns((new List<AiConversation>(), 0));

        var handler = new GetAiConversationHistory.Handler(repo);
        var result = await handler.Handle(
            new GetAiConversationHistory.Query(null, null, null, null, null, null, 1, 20),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().BeEmpty();
    }

    // ── ValidateKnowledgeCapture ──────────────────────────────────────────

    [Fact]
    public async Task ValidateCapture_ShouldReturnValid_WhenEntryIsComplete()
    {
        var repo = Substitute.For<IKnowledgeCaptureEntryRepository>();
        var conversation = AiConversation.Start("Service", "Topic", "user", FixedNow, ReleaseId);

        var entryResult = KnowledgeCaptureEntry.Suggest(
            conversation.Id,
            "Complete Knowledge Entry Title Here",
            "This is a complete content for the knowledge capture entry with sufficient length for validation.",
            "change-analysis",
            0.85m,
            FixedNow);
        var entry = entryResult.Value!;

        repo.GetByIdAsync(Arg.Any<KnowledgeCaptureEntryId>(), Arg.Any<CancellationToken>()).Returns(entry);
        repo.HasDuplicateTitleInConversationAsync(Arg.Any<AiConversationId>(), Arg.Any<KnowledgeCaptureEntryId>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);

        var handler = new ValidateKnowledgeCapture.Handler(repo);
        var result = await handler.Handle(new ValidateKnowledgeCapture.Command(entry.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.IsValid.Should().BeTrue();
        result.Value.Issues.Should().BeEmpty();
        result.Value.Recommendation.Should().Contain("ready for approval");
    }

    [Fact]
    public async Task ValidateCapture_ShouldReturnInvalid_WhenRelevanceIsTooLow()
    {
        var repo = Substitute.For<IKnowledgeCaptureEntryRepository>();
        var conversation = AiConversation.Start("Service", "Topic", "user", FixedNow, ReleaseId);

        var entryResult = KnowledgeCaptureEntry.Suggest(
            conversation.Id,
            "Entry Title That Is Long Enough",
            "Content that is definitely long enough to pass the minimum length requirement for validation purposes.",
            "source",
            0.15m, // below threshold
            FixedNow);
        var entry = entryResult.Value!;

        repo.GetByIdAsync(Arg.Any<KnowledgeCaptureEntryId>(), Arg.Any<CancellationToken>()).Returns(entry);
        repo.HasDuplicateTitleInConversationAsync(Arg.Any<AiConversationId>(), Arg.Any<KnowledgeCaptureEntryId>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);

        var handler = new ValidateKnowledgeCapture.Handler(repo);
        var result = await handler.Handle(new ValidateKnowledgeCapture.Command(entry.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.IsValid.Should().BeFalse();
        result.Value.Issues.Should().Contain(x => x.Contains("Relevance"));
    }

    [Fact]
    public async Task ValidateCapture_ShouldReturnNotFound_WhenEntryDoesNotExist()
    {
        var repo = Substitute.For<IKnowledgeCaptureEntryRepository>();
        repo.GetByIdAsync(Arg.Any<KnowledgeCaptureEntryId>(), Arg.Any<CancellationToken>()).Returns((KnowledgeCaptureEntry?)null);

        var handler = new ValidateKnowledgeCapture.Handler(repo);
        var result = await handler.Handle(new ValidateKnowledgeCapture.Command(Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateCapture_ShouldWarn_WhenDuplicateTitleExists()
    {
        var repo = Substitute.For<IKnowledgeCaptureEntryRepository>();
        var conversation = AiConversation.Start("Service", "Topic", "user", FixedNow, ReleaseId);

        var entryResult = KnowledgeCaptureEntry.Suggest(
            conversation.Id,
            "Duplicate Title That Exists Elsewhere",
            "This content is long enough to pass the minimum length requirement for validation purposes.",
            "source",
            0.75m,
            FixedNow);
        var entry = entryResult.Value!;

        repo.GetByIdAsync(Arg.Any<KnowledgeCaptureEntryId>(), Arg.Any<CancellationToken>()).Returns(entry);
        repo.HasDuplicateTitleInConversationAsync(Arg.Any<AiConversationId>(), Arg.Any<KnowledgeCaptureEntryId>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);

        var handler = new ValidateKnowledgeCapture.Handler(repo);
        var result = await handler.Handle(new ValidateKnowledgeCapture.Command(entry.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.IsValid.Should().BeTrue();
        result.Value.Warnings.Should().Contain(x => x.Contains("duplication"));
    }

    // ── GenerateTestScenarios ─────────────────────────────────────────────

    [Fact]
    public async Task GenerateTestScenarios_ShouldReturnContent_WhenProviderResponds()
    {
        var routingPort = Substitute.For<IExternalAIRoutingPort>();
        var artifactRepo = Substitute.For<IGeneratedTestArtifactRepository>();
        var currentUser = Substitute.For<ICurrentUser>();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        var logger = Substitute.For<ILogger<GenerateTestScenarios.Handler>>();

        routingPort.RouteQueryAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns("Scenario 1: Happy path\nScenario 2: Error path");
        dateTimeProvider.UtcNow.Returns(FixedNow);
        currentUser.Email.Returns("engineer@nextraceone.io");

        var handler = new GenerateTestScenarios.Handler(routingPort, artifactRepo, currentUser, dateTimeProvider, logger);
        var command = new GenerateTestScenarios.Command(
            "PaymentService", "POST /payments\nReturns 201 on success", null, null, ReleaseId, "xunit", null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ServiceName.Should().Be("PaymentService");
        result.Value.TestFramework.Should().Be("xunit");
        result.Value.GeneratedContent.Should().Contain("Scenario");
        result.Value.IsFallback.Should().BeFalse();
        await artifactRepo.Received(1).AddAsync(Arg.Any<GeneratedTestArtifact>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateTestScenarios_ShouldNotPersistArtifact_WhenNoReleaseId()
    {
        var routingPort = Substitute.For<IExternalAIRoutingPort>();
        var artifactRepo = Substitute.For<IGeneratedTestArtifactRepository>();
        var currentUser = Substitute.For<ICurrentUser>();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        var logger = Substitute.For<ILogger<GenerateTestScenarios.Handler>>();

        routingPort.RouteQueryAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns("Generated scenarios content here");
        dateTimeProvider.UtcNow.Returns(FixedNow);

        var handler = new GenerateTestScenarios.Handler(routingPort, artifactRepo, currentUser, dateTimeProvider, logger);
        var command = new GenerateTestScenarios.Command(
            "OrderService", "Order creation spec", null, null, null, null, null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ArtifactId.Should().BeNull();
        await artifactRepo.DidNotReceive().AddAsync(Arg.Any<GeneratedTestArtifact>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateTestScenarios_ShouldReturnError_WhenProviderThrows()
    {
        var routingPort = Substitute.For<IExternalAIRoutingPort>();
        var artifactRepo = Substitute.For<IGeneratedTestArtifactRepository>();
        var currentUser = Substitute.For<ICurrentUser>();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        var logger = Substitute.For<ILogger<GenerateTestScenarios.Handler>>();

        routingPort.RouteQueryAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<string>(new InvalidOperationException("Provider unreachable")));
        dateTimeProvider.UtcNow.Returns(FixedNow);

        var handler = new GenerateTestScenarios.Handler(routingPort, artifactRepo, currentUser, dateTimeProvider, logger);
        var command = new GenerateTestScenarios.Command("Svc", "spec content here", null, null, null, null, null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Contain("Unavailable");
    }

    // ── GenerateRobotFrameworkDraft ───────────────────────────────────────

    [Fact]
    public async Task GenerateRobotDraft_ShouldReturnDraft_WhenProviderResponds()
    {
        var routingPort = Substitute.For<IExternalAIRoutingPort>();
        var artifactRepo = Substitute.For<IGeneratedTestArtifactRepository>();
        var currentUser = Substitute.For<ICurrentUser>();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        var logger = Substitute.For<ILogger<GenerateRobotFrameworkDraft.Handler>>();

        routingPort.RouteQueryAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns("*** Settings ***\nLibrary  RequestsLibrary\n\n*** Test Cases ***\nCreate Payment\n    [Documentation]  Happy path");
        dateTimeProvider.UtcNow.Returns(FixedNow);

        var handler = new GenerateRobotFrameworkDraft.Handler(routingPort, artifactRepo, currentUser, dateTimeProvider, logger);
        var command = new GenerateRobotFrameworkDraft.Command(
            "PaymentService", null, "POST /v1/payments — creates a payment", null, "CreatePayment", ReleaseId, null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ServiceName.Should().Be("PaymentService");
        result.Value.GeneratedDraft.Should().Contain("Settings");
        result.Value.IsFallback.Should().BeFalse();
    }

    [Fact]
    public async Task GenerateRobotDraft_ShouldIncludeWarning_WhenNoSpecOrContract()
    {
        var routingPort = Substitute.For<IExternalAIRoutingPort>();
        var artifactRepo = Substitute.For<IGeneratedTestArtifactRepository>();
        var currentUser = Substitute.For<ICurrentUser>();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        var logger = Substitute.For<ILogger<GenerateRobotFrameworkDraft.Handler>>();

        routingPort.RouteQueryAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns("*** Test Cases ***\nBasic test");
        dateTimeProvider.UtcNow.Returns(FixedNow);

        var handler = new GenerateRobotFrameworkDraft.Handler(routingPort, artifactRepo, currentUser, dateTimeProvider, logger);
        // Provide only EndpointDescription — no Spec, no ContractSummary
        var command = new GenerateRobotFrameworkDraft.Command(
            "MyService", null, "GET /health returns 200", null, null, null, null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Warnings.Should().Contain(x => x.Contains("No contract or formal spec"));
    }

    // ── SummarizeReleaseForApproval ───────────────────────────────────────

    [Fact]
    public async Task SummarizeRelease_ShouldReturnSummary_WhenDataAndProviderAvailable()
    {
        var routingPort = Substitute.For<IExternalAIRoutingPort>();
        var convRepo = Substitute.For<IAiOrchestrationConversationRepository>();
        var artifactRepo = Substitute.For<IGeneratedTestArtifactRepository>();
        var currentUser = Substitute.For<ICurrentUser>();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        var logger = Substitute.For<ILogger<SummarizeReleaseForApproval.Handler>>();

        convRepo.GetRecentByReleaseAsync(ReleaseId, 5, Arg.Any<CancellationToken>())
            .Returns(new List<ConversationSummaryData>
            {
                new("Change analysis", 3, "Completed", "High confidence change")
            });

        artifactRepo.GetRecentByReleaseAsync(ReleaseId, 10, Arg.Any<CancellationToken>())
            .Returns(new List<ArtifactSummaryData>
            {
                new("PaymentService", "xunit", "Accepted", 0.9m)
            });

        routingPort.RouteQueryAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns("Executive summary: This release adds payment processing capabilities with low risk.");
        dateTimeProvider.UtcNow.Returns(FixedNow);
        currentUser.Email.Returns("pm@nextraceone.io");

        var handler = new SummarizeReleaseForApproval.Handler(
            routingPort, convRepo, artifactRepo, currentUser, dateTimeProvider, logger);

        var command = new SummarizeReleaseForApproval.Command(
            ReleaseId, "v2.5.0-payment-processing", "Adds payment gateway integration",
            new[] { "PaymentService", "OrderService" },
            new[] { "External API dependency" },
            null, null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ReleaseName.Should().Be("v2.5.0-payment-processing");
        result.Value.Summary.Should().Contain("Executive summary");
        result.Value.AnalysedConversationsCount.Should().Be(1);
        result.Value.AnalysedArtifactsCount.Should().Be(1);
        result.Value.ConfidenceIndicators.Should().Contain(x => x.Contains("conversation"));
        result.Value.IsFallback.Should().BeFalse();
    }

    [Fact]
    public async Task SummarizeRelease_ShouldIncludeLimitation_WhenNoHistoryOrArtifacts()
    {
        var routingPort = Substitute.For<IExternalAIRoutingPort>();
        var convRepo = Substitute.For<IAiOrchestrationConversationRepository>();
        var artifactRepo = Substitute.For<IGeneratedTestArtifactRepository>();
        var currentUser = Substitute.For<ICurrentUser>();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        var logger = Substitute.For<ILogger<SummarizeReleaseForApproval.Handler>>();

        convRepo.GetRecentByReleaseAsync(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<ConversationSummaryData>());
        artifactRepo.GetRecentByReleaseAsync(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<ArtifactSummaryData>());

        routingPort.RouteQueryAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns("Generic summary without context.");
        dateTimeProvider.UtcNow.Returns(FixedNow);

        var handler = new SummarizeReleaseForApproval.Handler(
            routingPort, convRepo, artifactRepo, currentUser, dateTimeProvider, logger);

        var result = await handler.Handle(
            new SummarizeReleaseForApproval.Command(ReleaseId, "v1.0.0", null, null, null, null, null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Limitations.Should().Contain(x => x.Contains("No AI conversation history"));
    }
}
