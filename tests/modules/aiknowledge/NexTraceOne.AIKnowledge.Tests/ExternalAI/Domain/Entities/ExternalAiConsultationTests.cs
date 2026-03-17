using NexTraceOne.AIKnowledge.Domain.ExternalAI.Entities;
using NexTraceOne.AIKnowledge.Domain.ExternalAI.Enums;

namespace NexTraceOne.AIKnowledge.Tests.ExternalAI.Domain.Entities;

/// <summary>Testes unitários da entidade ExternalAiConsultation — ciclo de vida e invariantes.</summary>
public sealed class ExternalAiConsultationTests
{
    private static readonly DateTimeOffset FixedNow = new(2025, 6, 1, 10, 0, 0, TimeSpan.Zero);

    private static ExternalAiConsultation CreateConsultation() =>
        ExternalAiConsultation.Create(
            ExternalAiProviderId.New(),
            "Release v2.0 - OrderService - Breaking change in POST /orders",
            "What is the impact of removing the 'discount' field from POST /orders?",
            "dev@company.com",
            FixedNow);

    // ── Create ────────────────────────────────────────────────────────────

    [Fact]
    public void Create_ShouldInitializeWithPendingStatus()
    {
        var consultation = CreateConsultation();

        consultation.Status.Should().Be(ConsultationStatus.Pending);
        consultation.Response.Should().BeNull();
        consultation.TokensUsed.Should().Be(0);
        consultation.Confidence.Should().Be(0m);
        consultation.CompletedAt.Should().BeNull();
    }

    [Fact]
    public void Create_ShouldSetAllRequiredProperties()
    {
        var consultation = CreateConsultation();

        consultation.Context.Should().Contain("OrderService");
        consultation.Query.Should().Contain("discount");
        consultation.RequestedBy.Should().Be("dev@company.com");
        consultation.RequestedAt.Should().Be(FixedNow);
    }

    // ── RecordResponse ────────────────────────────────────────────────────

    [Fact]
    public void RecordResponse_WithValidData_ShouldCompleteConsultation()
    {
        var consultation = CreateConsultation();
        var completedAt = FixedNow.AddMinutes(5);

        var result = consultation.RecordResponse(
            "The removal of 'discount' field is a breaking change affecting 3 consumers.",
            1500, 0.85m, completedAt);

        result.IsSuccess.Should().BeTrue();
        consultation.Status.Should().Be(ConsultationStatus.Completed);
        consultation.Response.Should().Contain("breaking change");
        consultation.TokensUsed.Should().Be(1500);
        consultation.Confidence.Should().Be(0.85m);
        consultation.CompletedAt.Should().Be(completedAt);
    }

    [Fact]
    public void RecordResponse_WhenAlreadyCompleted_ShouldFail()
    {
        var consultation = CreateConsultation();
        consultation.RecordResponse("First response", 500, 0.9m, FixedNow.AddMinutes(1));

        var result = consultation.RecordResponse("Second response", 300, 0.8m, FixedNow.AddMinutes(2));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("AlreadyCompleted");
    }

    [Fact]
    public void RecordResponse_WithInvalidConfidence_ShouldFail()
    {
        var consultation = CreateConsultation();

        var result = consultation.RecordResponse("Response", 100, 1.5m, FixedNow.AddMinutes(1));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvalidConfidence");
    }

    // ── MarkFailed ────────────────────────────────────────────────────────

    [Fact]
    public void MarkFailed_ShouldTransitionToFailed()
    {
        var consultation = CreateConsultation();

        var result = consultation.MarkFailed("Provider timeout after 30s", FixedNow.AddMinutes(1));

        result.IsSuccess.Should().BeTrue();
        consultation.Status.Should().Be(ConsultationStatus.Failed);
        consultation.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public void MarkFailed_WhenAlreadyCompleted_ShouldFail()
    {
        var consultation = CreateConsultation();
        consultation.RecordResponse("OK", 100, 0.9m, FixedNow.AddMinutes(1));

        var result = consultation.MarkFailed("Error", FixedNow.AddMinutes(2));

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void MarkFailed_Twice_ShouldFail()
    {
        var consultation = CreateConsultation();
        consultation.MarkFailed("First error", FixedNow.AddMinutes(1));

        var result = consultation.MarkFailed("Second error", FixedNow.AddMinutes(2));

        result.IsFailure.Should().BeTrue();
    }
}
