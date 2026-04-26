using Microsoft.Extensions.Logging;
using NexTraceOne.BuildingBlocks.Application.Nql;
using NexTraceOne.Governance.Application.Features.ExecuteNqlQuery;
using NexTraceOne.Governance.Application.Features.GetDashboardAnnotations;
using NexTraceOne.Governance.Application.Features.ValidateNqlQuery;
using NexTraceOne.Governance.Infrastructure.Persistence;

namespace NexTraceOne.Governance.Tests;

/// <summary>
/// Wave V3.2 — Query-driven Widgets &amp; Widget SDK.
/// Tests: NQL parser, NQL governance, handlers, annotations.
/// </summary>
public sealed class V32_QueryDrivenWidgetsTests
{
    // ── NQL Parser: Valid queries ─────────────────────────────────────────

    [Fact]
    public void Parse_SimpleFrom_ReturnsPlan()
    {
        var result = NqlParser.Parse("FROM catalog.services");

        result.IsValid.Should().BeTrue();
        result.Plan.Should().NotBeNull();
        result.Plan!.Entity.Should().Be(NqlEntity.CatalogServices);
        result.Plan.Filters.Should().BeEmpty();
        result.Plan.Limit.Should().Be(100);
    }

    [Fact]
    public void Parse_FromWithWhere_ParsesFilter()
    {
        var result = NqlParser.Parse("FROM catalog.services WHERE tier = 'Critical'");

        result.IsValid.Should().BeTrue();
        result.Plan!.Filters.Should().HaveCount(1);
        result.Plan.Filters[0].Field.Should().Be("tier");
        result.Plan.Filters[0].Operator.Should().Be(NqlFilterOperator.Equals);
        result.Plan.Filters[0].Value.Should().Be("Critical");
    }

    [Fact]
    public void Parse_FromWithMultipleAndFilters_ParsesAllFilters()
    {
        var result = NqlParser.Parse("FROM operations.incidents WHERE status = 'open' AND severity = 'P1'");

        result.IsValid.Should().BeTrue();
        result.Plan!.Filters.Should().HaveCount(2);
        result.Plan.Filters[0].Field.Should().Be("status");
        result.Plan.Filters[1].Field.Should().Be("severity");
    }

    [Fact]
    public void Parse_FromWithOrderBy_ParsesOrderBy()
    {
        var result = NqlParser.Parse("FROM catalog.services ORDER BY name ASC");

        result.IsValid.Should().BeTrue();
        result.Plan!.OrderBy.Should().NotBeNull();
        result.Plan.OrderBy!.Field.Should().Be("name");
        result.Plan.OrderBy.Direction.Should().Be(NqlSortDirection.Asc);
    }

    [Fact]
    public void Parse_FromWithOrderByDesc_ParsesDescOrder()
    {
        var result = NqlParser.Parse("FROM finops.costs ORDER BY cost_usd DESC");

        result.IsValid.Should().BeTrue();
        result.Plan!.OrderBy!.Direction.Should().Be(NqlSortDirection.Desc);
    }

    [Fact]
    public void Parse_FromWithLimit_ParsesLimit()
    {
        var result = NqlParser.Parse("FROM catalog.contracts LIMIT 25");

        result.IsValid.Should().BeTrue();
        result.Plan!.Limit.Should().Be(25);
    }

    [Fact]
    public void Parse_FromWithGroupBy_ParsesGroupBy()
    {
        var result = NqlParser.Parse("FROM operations.incidents GROUP BY service ORDER BY count DESC");

        result.IsValid.Should().BeTrue();
        result.Plan!.GroupBy.Should().Contain("service");
    }

    [Fact]
    public void Parse_WithRenderAs_ParsesRenderHint()
    {
        var result = NqlParser.Parse("FROM catalog.services RENDER AS bar");

        result.IsValid.Should().BeTrue();
        result.Plan!.RenderHint.Should().Be("bar");
    }

    [Fact]
    public void Parse_AllEntities_AreResolved()
    {
        var sources = NqlEntityMap.ValidSources;
        foreach (var src in sources)
        {
            var r = NqlParser.Parse($"FROM {src}");
            r.IsValid.Should().BeTrue(because: $"'{src}' should be a valid NQL source");
        }
    }

    [Fact]
    public void Parse_NotEqualsOperator_IsHandled()
    {
        var result = NqlParser.Parse("FROM catalog.services WHERE tier != 'Experimental'");

        result.IsValid.Should().BeTrue();
        result.Plan!.Filters[0].Operator.Should().Be(NqlFilterOperator.NotEquals);
    }

    [Fact]
    public void Parse_GreaterThanOperator_IsHandled()
    {
        var result = NqlParser.Parse("FROM finops.costs WHERE cost_usd > '1000'");

        result.IsValid.Should().BeTrue();
        result.Plan!.Filters[0].Operator.Should().Be(NqlFilterOperator.GreaterThan);
    }

    [Fact]
    public void Parse_LikeOperator_IsHandled()
    {
        var result = NqlParser.Parse("FROM knowledge.docs WHERE title LIKE 'runbook'");

        result.IsValid.Should().BeTrue();
        result.Plan!.Filters[0].Operator.Should().Be(NqlFilterOperator.Like);
    }

    // ── NQL Parser: Invalid queries ───────────────────────────────────────

    [Fact]
    public void Parse_EmptyQuery_ReturnsInvalid()
    {
        var result = NqlParser.Parse("");

        result.IsValid.Should().BeFalse();
        result.Error.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Parse_NullQuery_ReturnsInvalid()
    {
        var result = NqlParser.Parse(null);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Parse_MissingFrom_ReturnsInvalid()
    {
        var result = NqlParser.Parse("catalog.services WHERE tier = 'Critical'");

        result.IsValid.Should().BeFalse();
        result.Error.Should().Contain("FROM");
    }

    [Fact]
    public void Parse_UnknownEntity_ReturnsInvalid()
    {
        var result = NqlParser.Parse("FROM unknown.entity");

        result.IsValid.Should().BeFalse();
        result.Error.Should().Contain("unknown.entity");
    }

    [Fact]
    public void Parse_LimitExceedingMax_ReturnsInvalid()
    {
        var result = NqlParser.Parse("FROM catalog.services LIMIT 9999");

        result.IsValid.Should().BeFalse();
        result.Error.Should().Contain("1000");
    }

    [Fact]
    public void Parse_TrailingGarbage_ReturnsInvalid()
    {
        var result = NqlParser.Parse("FROM catalog.services LIMIT 10 garbage");

        result.IsValid.Should().BeFalse();
        result.Error.Should().Contain("garbage");
    }

    // ── NQL Entity Map ────────────────────────────────────────────────────

    [Fact]
    public void NqlEntityMap_HasTenOrMoreSources()
    {
        NqlEntityMap.ValidSources.Count.Should().BeGreaterThanOrEqualTo(10);
    }

    [Fact]
    public void NqlEntityMap_TryParse_CaseInsensitive()
    {
        NqlEntityMap.TryParse("CATALOG.SERVICES", out var entity).Should().BeTrue();
        entity.Should().Be(NqlEntity.CatalogServices);
    }

    // ── DefaultQueryGovernanceService ────────────────────────────────────

    [Fact]
    public void GovernanceService_Validate_RejectsEmptyTenantId()
    {
        var svc = new DefaultQueryGovernanceService(Substitute.For<ILogger<DefaultQueryGovernanceService>>());
        var ctx = new NqlExecutionContext("", null, "Engineer", "user-1");

        var result = svc.Validate("FROM catalog.services", ctx);

        result.IsValid.Should().BeFalse();
        result.Error.Should().Contain("TenantId");
    }

    [Fact]
    public void GovernanceService_Validate_AcceptsValidQuery()
    {
        var svc = new DefaultQueryGovernanceService(Substitute.For<ILogger<DefaultQueryGovernanceService>>());
        var ctx = new NqlExecutionContext("tenant-1", null, "Engineer", "user-1");

        var result = svc.Validate("FROM catalog.services WHERE tier = 'Critical'", ctx);

        result.IsValid.Should().BeTrue();
        result.Plan.Should().NotBeNull();
    }

    [Fact]
    public async Task GovernanceService_Execute_CrossModuleReturnsSimulated()
    {
        var svc = new DefaultQueryGovernanceService(Substitute.For<ILogger<DefaultQueryGovernanceService>>());
        var ctx = new NqlExecutionContext("tenant-1", null, "Engineer", "user-1");
        var plan = NqlParser.Parse("FROM catalog.services LIMIT 5").Plan!;

        var result = await svc.ExecuteAsync(plan, ctx);

        result.IsSimulated.Should().BeTrue();
        result.SimulatedNote.Should().NotBeNullOrEmpty();
        result.Columns.Should().NotBeEmpty();
        result.Rows.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GovernanceService_Execute_RespectsRowCap()
    {
        var svc = new DefaultQueryGovernanceService(Substitute.For<ILogger<DefaultQueryGovernanceService>>());
        var ctx = new NqlExecutionContext("tenant-1", null, "Engineer", "user-1");
        var plan = NqlParser.Parse("FROM finops.costs LIMIT 3").Plan!;

        var result = await svc.ExecuteAsync(plan, ctx);

        result.TotalRows.Should().BeLessThanOrEqualTo(3);
    }

    [Fact]
    public async Task GovernanceService_Execute_ReturnsRenderHint()
    {
        var svc = new DefaultQueryGovernanceService(Substitute.For<ILogger<DefaultQueryGovernanceService>>());
        var ctx = new NqlExecutionContext("tenant-1", null, "Engineer", "user-1");
        var plan = NqlParser.Parse("FROM operations.incidents RENDER AS bar").Plan!;

        var result = await svc.ExecuteAsync(plan, ctx);

        result.RenderHint.Should().Be("bar");
    }

    // ── ExecuteNqlQuery Handler ───────────────────────────────────────────

    [Fact]
    public async Task ExecuteNqlQuery_Handler_ReturnsSimulatedResult()
    {
        var svc = new DefaultQueryGovernanceService(Substitute.For<ILogger<DefaultQueryGovernanceService>>());
        var handler = new ExecuteNqlQuery.Handler(svc);

        var query = new ExecuteNqlQuery.Query(
            NqlQuery: "FROM catalog.services LIMIT 10",
            TenantId: "tenant-1",
            EnvironmentId: null,
            Persona: "Engineer",
            UserId: "user-1");

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ParsedEntity.Should().Be("CatalogServices");
        result.Value.Columns.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ExecuteNqlQuery_Handler_RejectsInvalidSyntax()
    {
        var svc = new DefaultQueryGovernanceService(Substitute.For<ILogger<DefaultQueryGovernanceService>>());
        var handler = new ExecuteNqlQuery.Handler(svc);

        var query = new ExecuteNqlQuery.Query(
            NqlQuery: "INVALID SYNTAX HERE",
            TenantId: "tenant-1",
            EnvironmentId: null,
            Persona: "Engineer",
            UserId: "user-1");

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("NqlQuery.Invalid");
    }

    // ── ValidateNqlQuery Handler ──────────────────────────────────────────

    [Fact]
    public async Task ValidateNqlQuery_Handler_ReturnsValidForCorrectQuery()
    {
        var svc = new DefaultQueryGovernanceService(Substitute.For<ILogger<DefaultQueryGovernanceService>>());
        var handler = new ValidateNqlQuery.Handler(svc);

        var query = new ValidateNqlQuery.Query(
            NqlQuery: "FROM changes.releases ORDER BY deployed_at DESC LIMIT 20",
            TenantId: "tenant-1",
            Persona: "Engineer",
            UserId: "user-1");

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.IsValid.Should().BeTrue();
        result.Value.ParsedEntity.Should().Be("ChangesReleases");
        result.Value.ParsedLimit.Should().Be(20);
        result.Value.Error.Should().BeNull();
    }

    [Fact]
    public async Task ValidateNqlQuery_Handler_ReturnsInvalidForSyntaxError()
    {
        var svc = new DefaultQueryGovernanceService(Substitute.For<ILogger<DefaultQueryGovernanceService>>());
        var handler = new ValidateNqlQuery.Handler(svc);

        var query = new ValidateNqlQuery.Query(
            NqlQuery: "FROM unknown.module",
            TenantId: "tenant-1",
            Persona: "Engineer",
            UserId: "user-1");

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.IsValid.Should().BeFalse();
        result.Value.Error.Should().NotBeNull();
        result.Value.Error!.Code.Should().Be("NQL.SyntaxError");
    }

    // ── GetDashboardAnnotations Handler ──────────────────────────────────

    [Fact]
    public async Task GetDashboardAnnotations_ReturnsSimulatedAnnotations()
    {
        var handler = new GetDashboardAnnotations.Handler();
        var now = DateTimeOffset.UtcNow;

        var query = new GetDashboardAnnotations.Query(
            TenantId: "tenant-1",
            From: now.AddHours(-24),
            To: now,
            ServiceNames: null,
            MaxPerSource: 50);

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Annotations.Should().NotBeEmpty();
        result.Value.Sources.Should().HaveCount(4);
        result.Value.TotalCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetDashboardAnnotations_FiltersAnnosByServiceName()
    {
        var handler = new GetDashboardAnnotations.Handler();
        var now = DateTimeOffset.UtcNow;

        var query = new GetDashboardAnnotations.Query(
            TenantId: "tenant-1",
            From: now.AddHours(-24),
            To: now,
            ServiceNames: ["payment-service"],
            MaxPerSource: 50);

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Annotations.Should().OnlyContain(a =>
            a.ServiceName == null || a.ServiceName == "payment-service");
    }

    [Fact]
    public async Task GetDashboardAnnotations_AllAnnotationsWithinTimeRange()
    {
        var handler = new GetDashboardAnnotations.Handler();
        var now = DateTimeOffset.UtcNow;
        var from = now.AddHours(-24);
        var to = now;

        var query = new GetDashboardAnnotations.Query("tenant-1", from, to);
        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Annotations.Should().OnlyContain(a =>
            a.Timestamp >= from && a.Timestamp <= to);
    }

    [Fact]
    public async Task GetDashboardAnnotations_SourcesAreSimulated()
    {
        var handler = new GetDashboardAnnotations.Handler();
        var now = DateTimeOffset.UtcNow;

        var query = new GetDashboardAnnotations.Query("tenant-1", now.AddHours(-1), now);
        var result = await handler.Handle(query, CancellationToken.None);

        result.Value!.Sources.Should().AllSatisfy(s => s.IsSimulated.Should().BeTrue());
    }
}
