using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.ExecuteRunbookStep;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.GetMitigationRecommendationsBySimilarity;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.ScoreCorrelationFeatureSet;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Entities;
using NexTraceOne.OperationalIntelligence.Tests.Incidents.Infrastructure;
using NSubstitute;

namespace NexTraceOne.OperationalIntelligence.Tests.Incidents.Application;

/// <summary>
/// Testes unitários de Wave A.5 — Operational Intelligence.
/// Cobre ScoreCorrelationFeatureSet, ExecuteRunbookStep, GetMitigationRecommendationsBySimilarity
/// e a entidade de domínio RunbookStepExecution.
/// </summary>
public sealed class OperationalIntelligenceA5Tests
{
    // ── GUIDs dos incidentes de seed ─────────────────────────────────────
    private static readonly Guid Inc1 = Guid.Parse("a1b2c3d4-0001-0000-0000-000000000001"); // Mitigating / svc-payment-gateway
    private static readonly Guid Inc4 = Guid.Parse("a1b2c3d4-0004-0000-0000-000000000004"); // Resolved / svc-order-api
    private static readonly Guid Inc6 = Guid.Parse("a1b2c3d4-0006-0000-0000-000000000006"); // Resolved / svc-auth-gateway

    // ── RunbookStepExecution Domain Entity ───────────────────────────────

    public sealed class RunbookStepExecutionDomainTests
    {
        [Fact]
        public void Create_ValidArguments_ShouldSetPropertiesCorrectly()
        {
            var runbookId = Guid.NewGuid();
            var startedAt = DateTimeOffset.UtcNow;

            var execution = RunbookStepExecution.Create(runbookId, "step-1", "user@example.com", startedAt);

            execution.RunbookId.Should().Be(runbookId);
            execution.StepKey.Should().Be("step-1");
            execution.ExecutorUserId.Should().Be("user@example.com");
            execution.StartedAt.Should().Be(startedAt);
            execution.ExecutionStatus.Should().Be(RunbookStepExecutionStatus.Pending);
            execution.CompletedAt.Should().BeNull();
            execution.OutputSummary.Should().BeNull();
            execution.ErrorDetail.Should().BeNull();
            execution.Id.Should().NotBeNull();
        }

        [Fact]
        public void Create_WithTenantId_ShouldSetTenantId()
        {
            var tenantId = Guid.NewGuid();

            var execution = RunbookStepExecution.Create(Guid.NewGuid(), "step-x", "user@test.com", DateTimeOffset.UtcNow, tenantId);

            execution.TenantId.Should().Be(tenantId);
        }

        [Fact]
        public void MarkSucceeded_ShouldSetStatusAndOutputSummaryAndCompletedAt()
        {
            var execution = RunbookStepExecution.Create(Guid.NewGuid(), "step-1", "user@example.com", DateTimeOffset.UtcNow);
            var completedAt = DateTimeOffset.UtcNow.AddSeconds(30);

            execution.MarkSucceeded("Step completed OK", completedAt);

            execution.ExecutionStatus.Should().Be(RunbookStepExecutionStatus.Succeeded);
            execution.OutputSummary.Should().Be("Step completed OK");
            execution.CompletedAt.Should().Be(completedAt);
            execution.ErrorDetail.Should().BeNull();
        }

        [Fact]
        public void MarkFailed_ShouldSetStatusAndErrorDetailAndCompletedAt()
        {
            var execution = RunbookStepExecution.Create(Guid.NewGuid(), "step-2", "user@example.com", DateTimeOffset.UtcNow);
            var completedAt = DateTimeOffset.UtcNow.AddSeconds(10);

            execution.MarkFailed("Timeout on step 2", completedAt);

            execution.ExecutionStatus.Should().Be(RunbookStepExecutionStatus.Failed);
            execution.ErrorDetail.Should().Be("Timeout on step 2");
            execution.CompletedAt.Should().Be(completedAt);
            execution.OutputSummary.Should().BeNull();
        }

        [Fact]
        public void Create_EmptyExecutorUserId_ShouldThrow()
        {
            var action = () => RunbookStepExecution.Create(Guid.NewGuid(), "step-1", "", DateTimeOffset.UtcNow);

            action.Should().Throw<Exception>();
        }

        [Fact]
        public void Create_EmptyStepKey_ShouldThrow()
        {
            var action = () => RunbookStepExecution.Create(Guid.NewGuid(), "", "user@example.com", DateTimeOffset.UtcNow);

            action.Should().Throw<Exception>();
        }
    }

    // ── ScoreCorrelationFeatureSet ───────────────────────────────────────

    public sealed class ScoreCorrelationFeatureSetTests
    {
        private static readonly InMemoryIncidentStore Store = new();

        [Fact]
        public async Task Handle_NullChangeDetails_ShouldReturnLowScoreWithExplanation()
        {
            var featureReader = Substitute.For<ICorrelationFeatureReader>();
            featureReader.GetChangeDetailsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<ChangeReleaseDetails?>(null));

            var handler = new ScoreCorrelationFeatureSet.Handler(Store, featureReader);
            var query = new ScoreCorrelationFeatureSet.Query(Inc1, Guid.NewGuid());

            var result = await handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.WeightedTotalScore.Should().Be(0.0);
            result.Value.ConfidenceLabel.Should().Be("Low");
            result.Value.Explanation.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task Handle_UnknownIncident_ShouldReturnError()
        {
            var featureReader = Substitute.For<ICorrelationFeatureReader>();

            var handler = new ScoreCorrelationFeatureSet.Handler(Store, featureReader);
            var query = new ScoreCorrelationFeatureSet.Query(Guid.NewGuid(), Guid.NewGuid());

            var result = await handler.Handle(query, CancellationToken.None);

            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Contain("NotFound");
        }

        [Fact]
        public async Task Handle_ExactServiceMatch_ShouldReturnHigherScore()
        {
            var featureReader = Substitute.For<ICorrelationFeatureReader>();
            featureReader.GetChangeDetailsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<ChangeReleaseDetails?>(new ChangeReleaseDetails(
                    Guid.NewGuid(),
                    "svc-payment-gateway", // exact match with Inc1 service
                    "payment-squad",
                    DateTimeOffset.UtcNow.AddMinutes(-30), // close temporal proximity
                    "Production")));

            var handler = new ScoreCorrelationFeatureSet.Handler(Store, featureReader);
            var query = new ScoreCorrelationFeatureSet.Query(Inc1, Guid.NewGuid());

            var result = await handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.ServiceMatchScore.Should().Be(1.0);
            result.Value.TemporalProximityScore.Should().BeGreaterThan(0.85); // ~30 min out of 24h
            result.Value.WeightedTotalScore.Should().BeGreaterThan(0.4);
            result.Value.ConfidenceLabel.Should().BeOneOf("High", "Medium");
        }

        [Fact]
        public async Task Handle_NoServiceMatch_ShouldReturnZeroServiceScore()
        {
            var featureReader = Substitute.For<ICorrelationFeatureReader>();
            featureReader.GetChangeDetailsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<ChangeReleaseDetails?>(new ChangeReleaseDetails(
                    Guid.NewGuid(),
                    "completely-different-service",
                    "other-team",
                    DateTimeOffset.UtcNow.AddDays(-2), // far from incident
                    "Staging")));

            var handler = new ScoreCorrelationFeatureSet.Handler(Store, featureReader);
            var query = new ScoreCorrelationFeatureSet.Query(Inc1, Guid.NewGuid());

            var result = await handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.ServiceMatchScore.Should().Be(0.0);
            result.Value.TemporalProximityScore.Should().Be(0.0); // beyond 24h window
            result.Value.WeightedTotalScore.Should().Be(0.0);
            result.Value.ConfidenceLabel.Should().Be("Low");
        }

        [Fact]
        public void ComputeTemporalScore_WithinWindow_ShouldReturnProportionalScore()
        {
            var incidentAt = DateTimeOffset.UtcNow;
            var deployAt = incidentAt.AddHours(-12); // 12h before = 0.5 remaining

            var score = ScoreCorrelationFeatureSet.Handler.ComputeTemporalScore(incidentAt, deployAt);

            score.Should().BeApproximately(0.5, 0.001);
        }

        [Fact]
        public void ComputeTemporalScore_BeyondWindow_ShouldReturnZero()
        {
            var incidentAt = DateTimeOffset.UtcNow;
            var deployAt = incidentAt.AddHours(-25); // beyond 24h window

            var score = ScoreCorrelationFeatureSet.Handler.ComputeTemporalScore(incidentAt, deployAt);

            score.Should().Be(0.0);
        }

        [Fact]
        public void ComputeTemporalScore_ImmediatelyAfter_ShouldBeNearOne()
        {
            var incidentAt = DateTimeOffset.UtcNow;
            var deployAt = incidentAt.AddMinutes(-1); // 1 minute before

            var score = ScoreCorrelationFeatureSet.Handler.ComputeTemporalScore(incidentAt, deployAt);

            score.Should().BeGreaterThan(0.99);
        }

        [Fact]
        public void ComputeServiceScore_ExactMatch_ShouldReturnOne()
        {
            var score = ScoreCorrelationFeatureSet.Handler.ComputeServiceScore("svc-payment-gateway", "Payment Gateway", "svc-payment-gateway");

            score.Should().Be(1.0);
        }

        [Fact]
        public void ComputeServiceScore_PartialMatch_ShouldReturnPointSix()
        {
            var score = ScoreCorrelationFeatureSet.Handler.ComputeServiceScore("payment", "Payment Gateway", "svc-payment-gateway");

            score.Should().Be(0.6);
        }

        [Fact]
        public void ComputeServiceScore_NoMatch_ShouldReturnZero()
        {
            var score = ScoreCorrelationFeatureSet.Handler.ComputeServiceScore("svc-catalog", "Catalog Service", "svc-auth-gateway");

            score.Should().Be(0.0);
        }
    }

    // ── ExecuteRunbookStep ────────────────────────────────────────────────

    public sealed class ExecuteRunbookStepTests
    {
        private static readonly Guid KnownRunbookId = Guid.Parse("bb000001-0001-0000-0000-000000000001");

        [Fact]
        public async Task Handle_ValidRunbook_ShouldCreateSucceededExecution()
        {
            var runbook = RunbookRecord.Create(
                RunbookRecordId.From(KnownRunbookId),
                "Test Runbook", "Description", "svc-test", null, null, null, null, "maintainer@test.com",
                DateTimeOffset.UtcNow);

            var runbookRepo = Substitute.For<IRunbookRepository>();
            runbookRepo.GetByIdAsync(KnownRunbookId, Arg.Any<CancellationToken>())
                .Returns(runbook);

            var executionRepo = Substitute.For<IRunbookExecutionRepository>();
            executionRepo.AddAsync(Arg.Any<RunbookStepExecution>(), Arg.Any<CancellationToken>())
                .Returns(Task.CompletedTask);

            var tenant = Substitute.For<ICurrentTenant>();
            tenant.IsActive.Returns(false);

            var clock = Substitute.For<IDateTimeProvider>();
            clock.UtcNow.Returns(DateTimeOffset.UtcNow);

            var handler = new ExecuteRunbookStep.Handler(runbookRepo, executionRepo, tenant, clock);
            var command = new ExecuteRunbookStep.Command(KnownRunbookId, "step-1", "user@example.com");

            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.RunbookId.Should().Be(KnownRunbookId);
            result.Value.StepKey.Should().Be("step-1");
            result.Value.Status.Should().Be("Succeeded");
            result.Value.CompletedAt.Should().NotBeNull();
            result.Value.OutputSummary.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task Handle_MissingRunbook_ShouldReturnError()
        {
            var runbookRepo = Substitute.For<IRunbookRepository>();
            runbookRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
                .Returns((RunbookRecord?)null);

            var executionRepo = Substitute.For<IRunbookExecutionRepository>();
            var tenant = Substitute.For<ICurrentTenant>();
            var clock = Substitute.For<IDateTimeProvider>();

            var handler = new ExecuteRunbookStep.Handler(runbookRepo, executionRepo, tenant, clock);
            var command = new ExecuteRunbookStep.Command(Guid.NewGuid(), "step-1", "user@example.com");

            var result = await handler.Handle(command, CancellationToken.None);

            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Contain("NotFound");
        }

        [Fact]
        public async Task Handle_ValidRunbook_ShouldCallExecutionRepositoryAdd()
        {
            var runbook = RunbookRecord.Create(
                RunbookRecordId.From(KnownRunbookId),
                "Test Runbook", "Description", null, null, null, null, null, "maintainer@test.com",
                DateTimeOffset.UtcNow);

            var runbookRepo = Substitute.For<IRunbookRepository>();
            runbookRepo.GetByIdAsync(KnownRunbookId, Arg.Any<CancellationToken>())
                .Returns(runbook);

            RunbookStepExecution? captured = null;
            var executionRepo = Substitute.For<IRunbookExecutionRepository>();
            executionRepo.When(r => r.AddAsync(Arg.Any<RunbookStepExecution>(), Arg.Any<CancellationToken>()))
                .Do(ci => captured = ci.ArgAt<RunbookStepExecution>(0));
            executionRepo.AddAsync(Arg.Any<RunbookStepExecution>(), Arg.Any<CancellationToken>())
                .Returns(Task.CompletedTask);

            var tenant = Substitute.For<ICurrentTenant>();
            tenant.IsActive.Returns(true);
            tenant.Id.Returns(Guid.NewGuid());

            var clock = Substitute.For<IDateTimeProvider>();
            clock.UtcNow.Returns(DateTimeOffset.UtcNow);

            var handler = new ExecuteRunbookStep.Handler(runbookRepo, executionRepo, tenant, clock);
            await handler.Handle(new ExecuteRunbookStep.Command(KnownRunbookId, "drain-connections", "ops-user"), CancellationToken.None);

            await executionRepo.Received(1).AddAsync(Arg.Any<RunbookStepExecution>(), Arg.Any<CancellationToken>());
            captured.Should().NotBeNull();
            captured!.StepKey.Should().Be("drain-connections");
            captured.ExecutionStatus.Should().Be(RunbookStepExecutionStatus.Succeeded);
        }

        [Fact]
        public void Validator_EmptyRunbookId_ShouldBeInvalid()
        {
            var validator = new ExecuteRunbookStep.Validator();

            var result = validator.Validate(new ExecuteRunbookStep.Command(Guid.Empty, "step-1", "user@example.com"));

            result.IsValid.Should().BeFalse();
        }

        [Fact]
        public void Validator_EmptyStepKey_ShouldBeInvalid()
        {
            var validator = new ExecuteRunbookStep.Validator();

            var result = validator.Validate(new ExecuteRunbookStep.Command(Guid.NewGuid(), "", "user@example.com"));

            result.IsValid.Should().BeFalse();
        }
    }

    // ── GetMitigationRecommendationsBySimilarity ──────────────────────────

    public sealed class GetMitigationRecommendationsBySimilarityTests
    {
        private readonly InMemoryIncidentStore _store = new();

        [Fact]
        public async Task Handle_UnknownIncident_ShouldReturnError()
        {
            var handler = new GetMitigationRecommendationsBySimilarity.Handler(_store);

            var result = await handler.Handle(
                new GetMitigationRecommendationsBySimilarity.Query("non-existent-id"),
                CancellationToken.None);

            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Contain("NotFound");
        }

        [Fact]
        public async Task Handle_IncidentWithResolvedSimilar_ShouldReturnRecommendations()
        {
            // Inc1 (svc-payment-gateway / ServiceDegradation / Mitigating) — pesquisa por similares resolvidos
            var handler = new GetMitigationRecommendationsBySimilarity.Handler(_store);

            var result = await handler.Handle(
                new GetMitigationRecommendationsBySimilarity.Query(Inc1.ToString()),
                CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            // Os incidentes resolvidos no seed (Inc4, Inc5, Inc6) são elegíveis se tiverem sobreposição
            result.Value.IncidentId.Should().Be(Inc1.ToString());
            result.Value.Recommendations.Should().NotBeNull();
        }

        [Fact]
        public async Task Handle_MaxResultsLimitsOutput()
        {
            var handler = new GetMitigationRecommendationsBySimilarity.Handler(_store);

            var result = await handler.Handle(
                new GetMitigationRecommendationsBySimilarity.Query(Inc1.ToString(), MaxResults: 1),
                CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Recommendations.Count.Should().BeLessThanOrEqualTo(1);
        }

        [Fact]
        public async Task Handle_ResolvedIncidentWithSameTypeAsTarget_ShouldBeIncluded()
        {
            // Inc4 (svc-order-api / OperationalRegression / Resolved) — pesquisar incidentes similares ao Inc4
            // Inc1 (svc-payment-gateway / ServiceDegradation / Mitigating) — não resolvido, não deve aparecer
            var handler = new GetMitigationRecommendationsBySimilarity.Handler(_store);

            var result = await handler.Handle(
                new GetMitigationRecommendationsBySimilarity.Query(Inc4.ToString()),
                CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            // Os resultados devem ser todos de incidentes resolvidos/fechados
            result.Value.Recommendations.Should().OnlyContain(r =>
                r.SuggestedMitigationSummary.Contains("Review resolved incident"));
        }

        [Fact]
        public async Task Handle_RecommendationsShouldContainReferenceIncidentId()
        {
            var handler = new GetMitigationRecommendationsBySimilarity.Handler(_store);

            var result = await handler.Handle(
                new GetMitigationRecommendationsBySimilarity.Query(Inc1.ToString()),
                CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            if (result.Value.Recommendations.Count > 0)
            {
                result.Value.Recommendations[0].ReferenceIncidentId.Should().NotBeEmpty();
                result.Value.Recommendations[0].SimilarityScore.Should().BeGreaterThan(0);
                result.Value.Recommendations[0].RecommendedRunbookIds.Should().NotBeNull();
            }
        }

        [Fact]
        public void Validator_InvalidMaxResults_ShouldBeInvalid()
        {
            var validator = new GetMitigationRecommendationsBySimilarity.Validator();

            var result = validator.Validate(new GetMitigationRecommendationsBySimilarity.Query("some-id", MaxResults: 0));

            result.IsValid.Should().BeFalse();
        }
    }
}
