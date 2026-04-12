using NexTraceOne.Configuration.Domain.Entities;

namespace NexTraceOne.Configuration.Tests.Domain;

/// <summary>
/// Testes de unidade para as entidades de workflow: AutomationRule, ChangeChecklist, ContractTemplate e ScheduledReport.
/// Valida criação, invariantes, toggle e actualização de estado.
/// </summary>
public sealed class WorkflowEntitiesTests
{
    // ── AutomationRule — Create happy path ────────────────────────────────────

    [Fact]
    public void AutomationRule_Create_WithValidData_ShouldReturn()
    {
        var now = DateTimeOffset.UtcNow;
        var rule = AutomationRule.Create(
            "tenant1", "Notify on Change", "on_change_created", "[]", "[]", "user1", now);

        Assert.NotNull(rule);
        Assert.Equal("Notify on Change", rule.Name);
        Assert.Equal("on_change_created", rule.Trigger);
        Assert.True(rule.IsEnabled);
        Assert.Equal("tenant1", rule.TenantId);
        Assert.NotEqual(Guid.Empty, rule.Id.Value);
    }

    [Theory]
    [InlineData("on_change_created")]
    [InlineData("on_incident_opened")]
    [InlineData("on_contract_published")]
    [InlineData("on_approval_expired")]
    public void AutomationRule_Create_WithAllValidTriggers_ShouldSucceed(string trigger)
    {
        var rule = AutomationRule.Create("t1", "Rule", trigger, "[]", "[]", "user1", DateTimeOffset.UtcNow);
        Assert.Equal(trigger, rule.Trigger);
    }

    [Fact]
    public void AutomationRule_Create_WithInvalidTrigger_ShouldThrow()
    {
        var act = () => AutomationRule.Create(
            "t1", "Rule", "on_unknown_event", "[]", "[]", "user1", DateTimeOffset.UtcNow);
        Assert.ThrowsAny<Exception>(act);
    }

    [Fact]
    public void AutomationRule_Create_WithEmptyName_ShouldThrow()
    {
        var act = () => AutomationRule.Create(
            "t1", "", "on_change_created", "[]", "[]", "user1", DateTimeOffset.UtcNow);
        Assert.ThrowsAny<Exception>(act);
    }

    [Fact]
    public void AutomationRule_Create_WithNullConditions_ShouldDefaultToEmptyArray()
    {
        var rule = AutomationRule.Create(
            "t1", "Rule", "on_change_created", null!, null!, "user1", DateTimeOffset.UtcNow);
        Assert.Equal("[]", rule.ConditionsJson);
        Assert.Equal("[]", rule.ActionsJson);
    }

    // ── AutomationRule — Toggle ───────────────────────────────────────────────

    [Fact]
    public void AutomationRule_Toggle_ShouldChangeEnabledState()
    {
        var now = DateTimeOffset.UtcNow;
        var rule = AutomationRule.Create(
            "t1", "Rule", "on_change_created", "[]", "[]", "user1", now);

        Assert.True(rule.IsEnabled);

        rule.Toggle(false, now.AddMinutes(1));
        Assert.False(rule.IsEnabled);

        rule.Toggle(true, now.AddMinutes(2));
        Assert.True(rule.IsEnabled);
    }

    // ── ChangeChecklist — Create ──────────────────────────────────────────────

    [Fact]
    public void ChangeChecklist_Create_WithItems_ShouldReturn()
    {
        var now = DateTimeOffset.UtcNow;
        var items = new[] { "Review tests", "Check rollback plan", "Notify team" };
        var cl = ChangeChecklist.Create(
            "tenant1", "Production Deploy", "standard", "production", true, items, now);

        Assert.Equal("Production Deploy", cl.Name);
        Assert.Equal("standard", cl.ChangeType);
        Assert.Equal("production", cl.Environment);
        Assert.True(cl.IsRequired);
        Assert.Equal(3, cl.Items.Count);
    }

    [Fact]
    public void ChangeChecklist_Create_WithNullEnvironment_ShouldBeNull()
    {
        var cl = ChangeChecklist.Create(
            "t1", "Checklist", "hotfix", null, false, [], DateTimeOffset.UtcNow);
        Assert.Null(cl.Environment);
    }

    [Fact]
    public void ChangeChecklist_Create_WithEmptyName_ShouldThrow()
    {
        var act = () => ChangeChecklist.Create(
            "t1", "", "standard", null, true, [], DateTimeOffset.UtcNow);
        Assert.ThrowsAny<Exception>(act);
    }

    // ── ChangeChecklist — UpdateItems ─────────────────────────────────────────

    [Fact]
    public void ChangeChecklist_UpdateItems_ShouldReplaceItemsAndFlag()
    {
        var now = DateTimeOffset.UtcNow;
        var cl = ChangeChecklist.Create(
            "t1", "Deploy", "standard", null, true, ["Item A"], now);

        cl.UpdateItems(["Item B", "Item C"], false, now.AddMinutes(1));

        Assert.Equal(2, cl.Items.Count);
        Assert.Contains("Item B", cl.Items);
        Assert.False(cl.IsRequired);
    }

    [Fact]
    public void ChangeChecklist_UpdateItems_ShouldFilterBlankItems()
    {
        var now = DateTimeOffset.UtcNow;
        var cl = ChangeChecklist.Create("t1", "Deploy", "standard", null, true, [], now);
        cl.UpdateItems(["Valid Item", "  ", ""], false, now.AddMinutes(1));

        Assert.Equal(1, cl.Items.Count);
    }

    // ── ContractTemplate — Create ─────────────────────────────────────────────

    [Fact]
    public void ContractTemplate_Create_WithValidType_ShouldReturn()
    {
        var now = DateTimeOffset.UtcNow;
        var tmpl = ContractTemplate.Create(
            "t1", "REST Template", "REST", "{}", "Base REST template", "user1", false, now);

        Assert.Equal("REST Template", tmpl.Name);
        Assert.Equal("REST", tmpl.ContractType);
        Assert.False(tmpl.IsBuiltIn);
    }

    [Theory]
    [InlineData("REST")]
    [InlineData("SOAP")]
    [InlineData("Event")]
    [InlineData("AsyncAPI")]
    [InlineData("Background")]
    public void ContractTemplate_Create_WithAllValidTypes_ShouldSucceed(string contractType)
    {
        var tmpl = ContractTemplate.Create(
            "t1", "T", contractType, "{}", "", "user1", false, DateTimeOffset.UtcNow);
        Assert.Equal(contractType, tmpl.ContractType);
    }

    [Fact]
    public void ContractTemplate_Create_WithInvalidType_ShouldNormalizeToRest()
    {
        var tmpl = ContractTemplate.Create(
            "t1", "T", "unknown-type", "{}", "", "user1", false, DateTimeOffset.UtcNow);
        Assert.Equal("REST", tmpl.ContractType);
    }

    [Fact]
    public void ContractTemplate_Create_WithEmptyName_ShouldThrow()
    {
        var act = () => ContractTemplate.Create(
            "t1", "", "REST", "{}", "", "user1", false, DateTimeOffset.UtcNow);
        Assert.ThrowsAny<Exception>(act);
    }

    // ── ScheduledReport — Create ──────────────────────────────────────────────

    [Fact]
    public void ScheduledReport_Create_WithValidScheduleAndFormat_ShouldReturn()
    {
        var now = DateTimeOffset.UtcNow;
        var report = ScheduledReport.Create(
            "t1", "user1", "Weekly Compliance", "compliance", "{}", "weekly", "[]", "pdf", now);

        Assert.Equal("Weekly Compliance", report.Name);
        Assert.Equal("weekly", report.Schedule);
        Assert.Equal("pdf", report.Format);
        Assert.True(report.IsEnabled);
        Assert.Null(report.LastSentAt);
    }

    [Theory]
    [InlineData("daily")]
    [InlineData("weekly")]
    [InlineData("monthly")]
    public void ScheduledReport_Create_WithAllValidSchedules_ShouldSucceed(string schedule)
    {
        var report = ScheduledReport.Create(
            "t1", "u1", "Report", "type", "{}", schedule, "[]", "pdf", DateTimeOffset.UtcNow);
        Assert.Equal(schedule, report.Schedule);
    }

    [Fact]
    public void ScheduledReport_Create_WithInvalidSchedule_ShouldNormalizeToWeekly()
    {
        var report = ScheduledReport.Create(
            "t1", "u1", "Report", "type", "{}", "quarterly", "[]", "pdf", DateTimeOffset.UtcNow);
        Assert.Equal("weekly", report.Schedule);
    }

    [Fact]
    public void ScheduledReport_Create_WithInvalidFormat_ShouldNormalizeToPdf()
    {
        var report = ScheduledReport.Create(
            "t1", "u1", "Report", "type", "{}", "daily", "[]", "xlsx", DateTimeOffset.UtcNow);
        Assert.Equal("pdf", report.Format);
    }

    // ── ScheduledReport — Toggle ──────────────────────────────────────────────

    [Fact]
    public void ScheduledReport_Toggle_ShouldChangeEnabledState()
    {
        var now = DateTimeOffset.UtcNow;
        var report = ScheduledReport.Create(
            "t1", "u1", "Report", "compliance", "{}", "weekly", "[]", "pdf", now);

        Assert.True(report.IsEnabled);

        report.Toggle(false, now.AddMinutes(1));
        Assert.False(report.IsEnabled);

        report.Toggle(true, now.AddMinutes(2));
        Assert.True(report.IsEnabled);
    }

    // ── ScheduledReport — MarkSent ────────────────────────────────────────────

    [Fact]
    public void ScheduledReport_MarkSent_ShouldUpdateLastSentAt()
    {
        var now = DateTimeOffset.UtcNow;
        var report = ScheduledReport.Create(
            "t1", "u1", "Report", "compliance", "{}", "weekly", "[]", "pdf", now);

        Assert.Null(report.LastSentAt);

        var sentAt = now.AddDays(7);
        report.MarkSent(sentAt);

        Assert.Equal(sentAt, report.LastSentAt);
    }
}
