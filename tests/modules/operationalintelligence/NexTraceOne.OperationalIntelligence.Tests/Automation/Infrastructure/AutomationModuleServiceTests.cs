using Microsoft.EntityFrameworkCore;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.OperationalIntelligence.Contracts.Automation.ServiceInterfaces;
using NexTraceOne.OperationalIntelligence.Domain.Automation.Entities;
using NexTraceOne.OperationalIntelligence.Domain.Automation.Enums;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Enums;
using NexTraceOne.OperationalIntelligence.Infrastructure.Automation.Persistence;
using NexTraceOne.OperationalIntelligence.Infrastructure.Automation.Services;

namespace NexTraceOne.OperationalIntelligence.Tests.Automation.Infrastructure;

public sealed class AutomationModuleServiceTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 3, 30, 12, 0, 0, TimeSpan.Zero);

    // ── GetWorkflowStatusAsync ───────────────────────────────────────────

    [Fact]
    public async Task GetWorkflowStatusAsync_WhenWorkflowExists_ShouldReturnStatus()
    {
        await using var db = CreateDbContext();
        var workflow = AutomationWorkflowRecord.Create(
            "restart-controlled", "orders", null, null,
            "Service degraded", "admin", "orders", "production",
            RiskLevel.Medium, FixedNow);
        db.Workflows.Add(workflow);
        await db.SaveChangesAsync();

        var sut = CreateSut(db);

        var result = await sut.GetWorkflowStatusAsync(workflow.Id.Value.ToString());

        result.Should().Be("Draft");
    }

    [Fact]
    public async Task GetWorkflowStatusAsync_WhenNotFound_ShouldReturnNull()
    {
        await using var db = CreateDbContext();
        var sut = CreateSut(db);

        var result = await sut.GetWorkflowStatusAsync(Guid.NewGuid().ToString());

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetWorkflowStatusAsync_WhenInvalidGuid_ShouldReturnNull()
    {
        await using var db = CreateDbContext();
        var sut = CreateSut(db);

        var result = await sut.GetWorkflowStatusAsync("not-a-guid");

        result.Should().BeNull();
    }

    // ── GetActiveWorkflowsAsync ──────────────────────────────────────────

    [Fact]
    public async Task GetActiveWorkflowsAsync_ShouldReturnNonTerminalWorkflows()
    {
        await using var db = CreateDbContext();
        var active = AutomationWorkflowRecord.Create(
            "restart-controlled", "orders", null, null,
            "Service degraded", "admin", "orders", "production",
            RiskLevel.Medium, FixedNow);

        var completed = AutomationWorkflowRecord.Create(
            "verify-post-change", "orders", null, null,
            "Post-change verification", "admin", "orders", "production",
            RiskLevel.Low, FixedNow.AddHours(-2));
        completed.UpdateStatus(AutomationWorkflowStatus.Completed, FixedNow.AddHours(-1));

        db.Workflows.AddRange(active, completed);
        await db.SaveChangesAsync();

        var sut = CreateSut(db);

        var result = await sut.GetActiveWorkflowsAsync("orders");

        result.Should().ContainSingle();
        result[0].ActionType.Should().Be("restart-controlled");
    }

    [Fact]
    public async Task GetActiveWorkflowsAsync_WhenNoActiveWorkflows_ShouldReturnEmpty()
    {
        await using var db = CreateDbContext();
        var completed = AutomationWorkflowRecord.Create(
            "restart-controlled", "orders", null, null,
            "Completed", "admin", "orders", "production",
            RiskLevel.Low, FixedNow);
        completed.UpdateStatus(AutomationWorkflowStatus.Completed, FixedNow);
        db.Workflows.Add(completed);
        await db.SaveChangesAsync();

        var sut = CreateSut(db);

        var result = await sut.GetActiveWorkflowsAsync("orders");

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetActiveWorkflowsAsync_ShouldFilterByServiceName()
    {
        await using var db = CreateDbContext();
        var ordersWorkflow = AutomationWorkflowRecord.Create(
            "restart-controlled", "orders", null, null,
            "Orders restart", "admin", "orders", "production",
            RiskLevel.Medium, FixedNow);
        var paymentsWorkflow = AutomationWorkflowRecord.Create(
            "restart-controlled", "payments", null, null,
            "Payments restart", "admin", "payments", "production",
            RiskLevel.Medium, FixedNow);
        db.Workflows.AddRange(ordersWorkflow, paymentsWorkflow);
        await db.SaveChangesAsync();

        var sut = CreateSut(db);

        var result = await sut.GetActiveWorkflowsAsync("orders");

        result.Should().ContainSingle();
        result[0].ServiceName.Should().Be("orders");
    }

    // ── HasBlockingWorkflowsAsync ────────────────────────────────────────

    [Fact]
    public async Task HasBlockingWorkflowsAsync_WhenExecutingWorkflowExists_ShouldReturnTrue()
    {
        await using var db = CreateDbContext();
        var workflow = AutomationWorkflowRecord.Create(
            "restart-controlled", "orders", null, null,
            "Executing", "admin", "orders", "production",
            RiskLevel.High, FixedNow);
        workflow.UpdateStatus(AutomationWorkflowStatus.Executing, FixedNow);
        db.Workflows.Add(workflow);
        await db.SaveChangesAsync();

        var sut = CreateSut(db);

        var result = await sut.HasBlockingWorkflowsAsync("orders", "production");

        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasBlockingWorkflowsAsync_WhenAwaitingApproval_ShouldReturnTrue()
    {
        await using var db = CreateDbContext();
        var workflow = AutomationWorkflowRecord.Create(
            "restart-controlled", "orders", null, null,
            "Awaiting approval", "admin", "orders", "production",
            RiskLevel.High, FixedNow);
        workflow.UpdateStatus(AutomationWorkflowStatus.AwaitingApproval, FixedNow);
        db.Workflows.Add(workflow);
        await db.SaveChangesAsync();

        var sut = CreateSut(db);

        var result = await sut.HasBlockingWorkflowsAsync("orders", "production");

        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasBlockingWorkflowsAsync_WhenOnlyDraftWorkflows_ShouldReturnFalse()
    {
        await using var db = CreateDbContext();
        var workflow = AutomationWorkflowRecord.Create(
            "restart-controlled", "orders", null, null,
            "Draft only", "admin", "orders", "production",
            RiskLevel.Low, FixedNow);
        db.Workflows.Add(workflow);
        await db.SaveChangesAsync();

        var sut = CreateSut(db);

        var result = await sut.HasBlockingWorkflowsAsync("orders", "production");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task HasBlockingWorkflowsAsync_WhenNoWorkflows_ShouldReturnFalse()
    {
        await using var db = CreateDbContext();
        var sut = CreateSut(db);

        var result = await sut.HasBlockingWorkflowsAsync("orders", "production");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task HasBlockingWorkflowsAsync_ShouldFilterByEnvironment()
    {
        await using var db = CreateDbContext();
        var stagingWorkflow = AutomationWorkflowRecord.Create(
            "restart-controlled", "orders", null, null,
            "Staging restart", "admin", "orders", "staging",
            RiskLevel.High, FixedNow);
        stagingWorkflow.UpdateStatus(AutomationWorkflowStatus.Executing, FixedNow);
        db.Workflows.Add(stagingWorkflow);
        await db.SaveChangesAsync();

        var sut = CreateSut(db);

        var result = await sut.HasBlockingWorkflowsAsync("orders", "production");

        result.Should().BeFalse();
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static AutomationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AutomationDbContext>()
            .UseInMemoryDatabase($"automation-module-tests-{Guid.NewGuid():N}")
            .Options;

        return new AutomationDbContext(
            options,
            new TestCurrentTenant(),
            new TestCurrentUser(),
            new TestDateTimeProvider());
    }

    private static IAutomationModule CreateSut(AutomationDbContext db) => new AutomationModuleService(db);

    private sealed class TestCurrentTenant : ICurrentTenant
    {
        public Guid Id => Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        public string Slug => "tests";
        public string Name => "Tests";
        public bool IsActive => true;
        public bool HasCapability(string capability) => true;
    }

    private sealed class TestCurrentUser : ICurrentUser
    {
        public string Id => "automation-tests-user";
        public string Name => "Automation Tests";
        public string Email => "automation.tests@nextraceone.local";
        public string? Persona { get; } = null;
        public bool IsAuthenticated => true;
        public bool HasPermission(string permission) => true;
    }

    private sealed class TestDateTimeProvider : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => FixedNow;
        public DateOnly UtcToday => DateOnly.FromDateTime(FixedNow.UtcDateTime);
    }
}
