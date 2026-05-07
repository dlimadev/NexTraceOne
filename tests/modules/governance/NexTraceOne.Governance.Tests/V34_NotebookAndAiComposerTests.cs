using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Application.Features.ComposeAiDashboard;
using NexTraceOne.Governance.Application.Features.CreateNotebook;
using NexTraceOne.Governance.Application.Features.GetNotebook;
using NexTraceOne.Governance.Domain.Entities;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Tests;

/// <summary>
/// Unit tests for Wave V3.4 — AI-assisted Dashboard Creation &amp; Notebook Mode.
/// Covers: Notebook domain entity, cell management, ComposeAiDashboard handler.
/// </summary>
public sealed class V34_NotebookAndAiComposerTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 4, 26, 10, 0, 0, TimeSpan.Zero);

    // ── Notebook.Create ───────────────────────────────────────────────────────

    [Fact]
    public void Notebook_Create_WithValidData_ShouldInitialiseCorrectly()
    {
        var n = Notebook.Create("My Notebook", "desc", "tenant1", "user1", "Engineer", FixedNow);

        n.Title.Should().Be("My Notebook");
        n.Description.Should().Be("desc");
        n.TenantId.Should().Be("tenant1");
        n.CreatedByUserId.Should().Be("user1");
        n.Persona.Should().Be("Engineer");
        n.Status.Should().Be(NotebookStatus.Draft);
        n.Cells.Should().BeEmpty();
        n.CurrentRevisionNumber.Should().Be(0);
        n.SharingPolicy.Should().Be(SharingPolicy.Private);
        n.Id.Value.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Notebook_Create_WithEmptyTitle_ShouldThrow()
    {
        Action act = () => Notebook.Create("", null, "tenant1", "user1", "Engineer", FixedNow);
        act.Should().Throw<Exception>();
    }

    [Fact]
    public void Notebook_Create_TitleTooLong_ShouldThrow()
    {
        var longTitle = new string('x', 201);
        Action act = () => Notebook.Create(longTitle, null, "tenant1", "user1", "Engineer", FixedNow);
        act.Should().Throw<Exception>();
    }

    [Fact]
    public void Notebook_Create_WithTeamId_ShouldSetTeamId()
    {
        var n = Notebook.Create("N", null, "t1", "u1", "TechLead", FixedNow, "team-42");
        n.TeamId.Should().Be("team-42");
    }

    // ── AddCell ───────────────────────────────────────────────────────────────

    [Fact]
    public void Notebook_AddCell_Markdown_ShouldAppendAndIncrementRevision()
    {
        var n = Notebook.Create("N", null, "t1", "u1", "Engineer", FixedNow);
        var cell = Notebook.CreateMarkdownCell(1, "# Title", FixedNow);
        n.AddCell(cell, FixedNow);

        n.Cells.Should().HaveCount(1);
        n.Cells[0].CellType.Should().Be(NotebookCellType.Markdown);
        n.Cells[0].Content.Should().Be("# Title");
        n.CurrentRevisionNumber.Should().Be(1);
    }

    [Fact]
    public void Notebook_AddCell_Query_ShouldSetCorrectType()
    {
        var n = Notebook.Create("N", null, "t1", "u1", "Engineer", FixedNow);
        var cell = Notebook.CreateQueryCell(1, "SELECT * FROM services", FixedNow);
        n.AddCell(cell, FixedNow);

        n.Cells[0].CellType.Should().Be(NotebookCellType.Query);
    }

    [Fact]
    public void Notebook_AddCell_Widget_ShouldSetCorrectType()
    {
        var n = Notebook.Create("N", null, "t1", "u1", "Engineer", FixedNow);
        var cell = Notebook.CreateWidgetCell(1, "slo-gauge", FixedNow);
        n.AddCell(cell, FixedNow);

        n.Cells[0].CellType.Should().Be(NotebookCellType.Widget);
    }

    [Fact]
    public void Notebook_AddCell_Ai_ShouldSetCorrectType()
    {
        var n = Notebook.Create("N", null, "t1", "u1", "Engineer", FixedNow);
        var cell = Notebook.CreateAiCell(1, "Summarize this incident", FixedNow);
        n.AddCell(cell, FixedNow);

        n.Cells[0].CellType.Should().Be(NotebookCellType.Ai);
    }

    // ── UpdateCellOutput ──────────────────────────────────────────────────────

    [Fact]
    public void Notebook_UpdateCellOutput_ShouldUpdateOutput()
    {
        var n = Notebook.Create("N", null, "t1", "u1", "Engineer", FixedNow);
        var cell = Notebook.CreateQueryCell(1, "SELECT * FROM services", FixedNow);
        n.AddCell(cell, FixedNow);

        n.UpdateCellOutput(cell.Id, """{"rows":10}""", FixedNow.AddMinutes(1));

        n.Cells[0].OutputJson.Should().Be("""{"rows":10}""");
    }

    // ── Update ────────────────────────────────────────────────────────────────

    [Fact]
    public void Notebook_Update_ShouldChangeTitleAndIncrementRevision()
    {
        var n = Notebook.Create("Old", null, "t1", "u1", "Engineer", FixedNow);
        var cells = new List<NotebookCell>
        {
            Notebook.CreateMarkdownCell(1, "content", FixedNow)
        };
        n.Update("New Title", "desc", cells, null, FixedNow.AddMinutes(5));

        n.Title.Should().Be("New Title");
        n.Cells.Should().HaveCount(1);
        n.CurrentRevisionNumber.Should().Be(1);
    }

    // ── SharingPolicy ─────────────────────────────────────────────────────────

    [Fact]
    public void Notebook_SetSharingPolicy_ShouldUpdate()
    {
        var n = Notebook.Create("N", null, "t1", "u1", "Engineer", FixedNow);
        var policy = new SharingPolicy(DashboardSharingScope.Team, DashboardSharingPermission.Read, null);
        n.SetSharingPolicy(policy, FixedNow);

        n.SharingPolicy.Scope.Should().Be(DashboardSharingScope.Team);
    }

    // ── Publish / Archive ─────────────────────────────────────────────────────

    [Fact]
    public void Notebook_Publish_ShouldChangeStatus()
    {
        var n = Notebook.Create("N", null, "t1", "u1", "Engineer", FixedNow);
        n.Publish(FixedNow);
        n.Status.Should().Be(NotebookStatus.Published);
    }

    [Fact]
    public void Notebook_Archive_ShouldChangeStatus()
    {
        var n = Notebook.Create("N", null, "t1", "u1", "Engineer", FixedNow);
        n.Archive(FixedNow);
        n.Status.Should().Be(NotebookStatus.Archived);
    }

    // ── LinkDashboard ─────────────────────────────────────────────────────────

    [Fact]
    public void Notebook_LinkDashboard_ShouldSetLinkedId()
    {
        var n = Notebook.Create("N", null, "t1", "u1", "Engineer", FixedNow);
        var dashId = new CustomDashboardId(Guid.NewGuid());
        n.LinkDashboard(dashId, FixedNow);

        n.LinkedDashboardId.Should().Be(dashId);
    }

    // ── ComposeAiDashboard.Handler ────────────────────────────────────────────

    [Fact]
    public async Task ComposeAiDashboard_SloPrompt_ShouldProposeSloWidgets()
    {
        var handler = new ComposeAiDashboard.Handler(Substitute.For<IAiDashboardComposerService>());
        var cmd = new ComposeAiDashboard.Command(
            "SLO and reliability dashboard for payment service",
            "tenant1", "user1", "Engineer",
            null, null, ["svc-payment"]);

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsSimulated.Should().BeTrue();
        result.Value.ProposedWidgets.Should().Contain(w => w.WidgetType == "slo-gauge");
        result.Value.ProposedVariables.Should().Contain(v => v.Key == "$env");
    }

    [Fact]
    public async Task ComposeAiDashboard_IncidentPrompt_ShouldProposeIncidentWidgets()
    {
        var handler = new ComposeAiDashboard.Handler(Substitute.For<IAiDashboardComposerService>());
        var cmd = new ComposeAiDashboard.Command(
            "incident on-call overview",
            "tenant1", "user1", "Engineer",
            null, null, null);

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ProposedWidgets.Should().Contain(w => w.WidgetType == "incident-summary");
    }

    [Fact]
    public async Task ComposeAiDashboard_DoraPrompt_ShouldProposeDoraWidget()
    {
        var handler = new ComposeAiDashboard.Handler(Substitute.For<IAiDashboardComposerService>());
        var cmd = new ComposeAiDashboard.Command(
            "DORA metrics overview",
            "tenant1", "user1", "TechLead",
            "team-backend", "production", null);

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ProposedWidgets.Should().Contain(w => w.WidgetType == "dora-metrics");
    }

    [Fact]
    public async Task ComposeAiDashboard_ExecPersona_ShouldProposeTopServicesWidget()
    {
        var handler = new ComposeAiDashboard.Handler(Substitute.For<IAiDashboardComposerService>());
        var cmd = new ComposeAiDashboard.Command(
            "executive overview",
            "tenant1", "user1", "Executive",
            null, null, null);

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ProposedWidgets.Should().Contain(w => w.WidgetType == "top-services");
    }

    [Fact]
    public async Task ComposeAiDashboard_CostPrompt_ShouldProposeCostWidget()
    {
        var handler = new ComposeAiDashboard.Handler(Substitute.For<IAiDashboardComposerService>());
        var cmd = new ComposeAiDashboard.Command(
            "FinOps cost trend budget",
            "tenant1", "user1", "Executive",
            null, null, null);

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ProposedWidgets.Should().Contain(w => w.WidgetType == "cost-trend");
    }

    [Fact]
    public async Task ComposeAiDashboard_AlwaysContainsTimeRangeVariable()
    {
        var handler = new ComposeAiDashboard.Handler(Substitute.For<IAiDashboardComposerService>());
        var cmd = new ComposeAiDashboard.Command(
            "anything", "t1", "u1", "Engineer", null, null, null);

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.Value.ProposedVariables.Should().Contain(v => v.Key == "$timeRange");
    }

    [Fact]
    public async Task ComposeAiDashboard_WithTeam_ShouldIncludeTeamVariable()
    {
        var handler = new ComposeAiDashboard.Handler(Substitute.For<IAiDashboardComposerService>());
        var cmd = new ComposeAiDashboard.Command(
            "team health", "t1", "u1", "TechLead", "team-alpha", null, null);

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.Value.ProposedVariables.Should().Contain(v => v.Key == "$team" && v.DefaultValue == "team-alpha");
    }

    // ── GetNotebook response mapping ──────────────────────────────────────────

    [Fact]
    public void GetNotebook_MapToResponse_ShouldMapAllFields()
    {
        var notebook = Notebook.Create("Test", "desc", "t1", "u1", "Engineer", FixedNow, "team1");
        notebook.AddCell(Notebook.CreateMarkdownCell(1, "# Hello", FixedNow), FixedNow);
        notebook.AddCell(Notebook.CreateQueryCell(2, "SELECT * FROM services", FixedNow), FixedNow);

        var response = GetNotebook.Handler.MapToResponse(notebook);

        response.Title.Should().Be("Test");
        response.Persona.Should().Be("Engineer");
        response.Cells.Should().HaveCount(2);
        response.Cells[0].CellType.Should().Be("Markdown");
        response.Cells[1].CellType.Should().Be("Query");
        response.Status.Should().Be("Draft");
        response.CurrentRevisionNumber.Should().Be(2);
    }

    // ── CreateNotebook.Validator ──────────────────────────────────────────────

    [Fact]
    public void CreateNotebook_Validator_EmptyTitle_ShouldFail()
    {
        var validator = new CreateNotebook.Validator();
        var cmd = new CreateNotebook.Command("", null, "t1", "u1", "Engineer", null, null);
        var result = validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void CreateNotebook_Validator_ValidCommand_ShouldPass()
    {
        var validator = new CreateNotebook.Validator();
        var cmd = new CreateNotebook.Command("My Notebook", null, "t1", "u1", "Engineer", null, null);
        var result = validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    // ── NotebookCellType enum coverage ────────────────────────────────────────

    [Theory]
    [InlineData(NotebookCellType.Markdown)]
    [InlineData(NotebookCellType.Query)]
    [InlineData(NotebookCellType.Widget)]
    [InlineData(NotebookCellType.Action)]
    [InlineData(NotebookCellType.Ai)]
    public void NotebookCellType_AllValues_ShouldBeRepresentable(NotebookCellType cellType)
    {
        var cell = new NotebookCell(
            new NotebookCellId(Guid.NewGuid()),
            cellType, 1, "content", null, false, FixedNow, FixedNow);

        cell.CellType.Should().Be(cellType);
    }

    // ── NotebookId strong typing ───────────────────────────────────────────────

    [Fact]
    public void NotebookId_TwoWithSameGuid_ShouldBeEqual()
    {
        var guid = Guid.NewGuid();
        new NotebookId(guid).Should().Be(new NotebookId(guid));
    }

    // ── Multiple cells ordering ───────────────────────────────────────────────

    [Fact]
    public void Notebook_MultipleCells_ShouldPreserveOrder()
    {
        var n = Notebook.Create("N", null, "t1", "u1", "Engineer", FixedNow);
        for (int i = 1; i <= 5; i++)
            n.AddCell(Notebook.CreateMarkdownCell(i, $"Cell {i}", FixedNow), FixedNow);

        n.Cells.Should().HaveCount(5);
        n.Cells[4].Content.Should().Be("Cell 5");
    }
}
