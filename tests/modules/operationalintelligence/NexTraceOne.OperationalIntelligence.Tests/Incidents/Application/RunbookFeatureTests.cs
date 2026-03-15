using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.GetRunbookDetail;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.ListRunbooks;

namespace NexTraceOne.OperationalIntelligence.Tests.Incidents.Application;

/// <summary>
/// Testes unitários para as features de Runbooks do subdomínio Incidents.
/// Verificam handlers, validators e respostas das queries de runbooks operacionais.
/// </summary>
public sealed class RunbookFeatureTests
{
    // ── ListRunbooks ─────────────────────────────────────────────────

    [Fact]
    public async Task ListRunbooks_NoFilters_ShouldReturnAllRunbooks()
    {
        var handler = new ListRunbooks.Handler();
        var query = new ListRunbooks.Query(null, null, null);

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Runbooks.Should().HaveCount(3);
    }

    [Fact]
    public async Task ListRunbooks_FilterBySearchTerm_ShouldReturnMatching()
    {
        var handler = new ListRunbooks.Handler();
        var query = new ListRunbooks.Query(null, null, "Payment");

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Runbooks.Should().ContainSingle();
        result.Value.Runbooks[0].Title.Should().Contain("Payment");
    }

    [Fact]
    public async Task ListRunbooks_FilterByService_ShouldReturnFiltered()
    {
        var handler = new ListRunbooks.Handler();
        var query = new ListRunbooks.Query("catalog-service", null, null);

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Runbooks.Should().ContainSingle();
        result.Value.Runbooks[0].LinkedServiceId.Should().Be("catalog-service");
    }

    [Fact]
    public async Task ListRunbooks_FilterByIncidentType_ShouldReturnFiltered()
    {
        var handler = new ListRunbooks.Handler();
        var query = new ListRunbooks.Query(null, "DependencyFailure", null);

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Runbooks.Should().ContainSingle();
        result.Value.Runbooks[0].LinkedIncidentType.Should().Be("DependencyFailure");
    }

    [Fact]
    public void ListRunbooks_Validator_ShouldAcceptNullFilters()
    {
        var validator = new ListRunbooks.Validator();
        var query = new ListRunbooks.Query(null, null, null);

        var result = validator.Validate(query);

        result.IsValid.Should().BeTrue();
    }

    // ── GetRunbookDetail ─────────────────────────────────────────────

    [Fact]
    public async Task GetRunbookDetail_KnownRunbook_ShouldReturnDetailWithSteps()
    {
        var handler = new GetRunbookDetail.Handler();
        var query = new GetRunbookDetail.Query("bb000001-0001-0000-0000-000000000001");

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Title.Should().Be("Payment Gateway Rollback Procedure");
        result.Value.LinkedServiceId.Should().Be("payment-service");
        result.Value.Steps.Should().HaveCount(6);
        result.Value.Preconditions.Should().HaveCount(3);
        result.Value.PostValidationGuidance.Should().NotBeNullOrEmpty();
        result.Value.CreatedBy.Should().Be("platform-team@nextraceone.io");
    }

    [Fact]
    public async Task GetRunbookDetail_UnknownRunbook_ShouldReturnError()
    {
        var handler = new GetRunbookDetail.Handler();
        var query = new GetRunbookDetail.Query("nonexistent-runbook-id");

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    [Fact]
    public void GetRunbookDetail_Validator_ShouldRejectEmptyRunbookId()
    {
        var validator = new GetRunbookDetail.Validator();
        var query = new GetRunbookDetail.Query("");

        var result = validator.Validate(query);

        result.IsValid.Should().BeFalse();
    }
}
