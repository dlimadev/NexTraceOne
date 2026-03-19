using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NexTraceOne.ChangeGovernance.Domain.Promotion.Entities;
using NexTraceOne.ChangeGovernance.Domain.RulesetGovernance.Entities;
using NexTraceOne.ChangeGovernance.Domain.Workflow.Entities;
using NexTraceOne.Governance.Domain.Entities;
using NexTraceOne.Governance.Domain.Enums;
using NexTraceOne.IntegrationTests.Infrastructure;
using Xunit;

namespace NexTraceOne.IntegrationTests.CriticalFlows;

/// <summary>
/// Testes de integração para GovernanceDbContext, WorkflowDbContext, PromotionDbContext
/// e RulesetGovernanceDbContext contra PostgreSQL real.
/// Valida persistência, queries EF, ciclos de vida e integridade de dados.
/// </summary>
[Collection(PostgreSqlIntegrationCollection.Name)]
public sealed class GovernanceWorkflowPostgreSqlTests(PostgreSqlIntegrationFixture fixture) : IntegrationTestBase(fixture)
{
    // ── Migrations coverage ───────────────────────────────────────────────────

    [Fact]
    public async Task Governance_AllDbContexts_Should_Have_AppliedMigrations()
    {
        var governanceMigrations = await Fixture.GetAppliedMigrationsCountAsync(Fixture.GovernanceConnectionString);

        governanceMigrations.Should().BeGreaterThan(0,
            "porque GovernanceDbContext deve ter migrations aplicadas");
    }

    [Fact]
    public async Task ChangeGovernance_Extended_Should_Have_AppliedMigrations()
    {
        var changeGovernanceMigrations = await Fixture.GetAppliedMigrationsCountAsync(Fixture.ChangeGovernanceConnectionString);

        // WorkflowDbContext, PromotionDbContext, and RulesetGovernanceDbContext
        // all share the change-governance database and apply additional migrations
        changeGovernanceMigrations.Should().BeGreaterThan(1,
            "porque WorkflowDbContext, PromotionDbContext e RulesetGovernanceDbContext aplicaram migrations extras");
    }

    // ── GovernanceDbContext ───────────────────────────────────────────────────

    [Fact]
    public async Task Governance_Should_Persist_Team_And_GovernancePack_And_Link()
    {
        await ResetStateAsync();

        await using var context = Fixture.CreateGovernanceDbContext();

        var team = Team.Create(
            name: "platform-core",
            displayName: "Platform Core Team",
            description: "Responsible for core platform APIs and governance enforcement",
            parentOrganizationUnit: "Engineering");

        var apiStandardsPack = GovernancePack.Create(
            name: "api-contract-standards",
            displayName: "API Contract Standards",
            description: "Enforces versioning, documentation and breaking change policies",
            category: GovernanceRuleCategory.Contracts);

        context.Teams.Add(team);
        context.Packs.Add(apiStandardsPack);
        await context.SaveChangesAsync();

        var persistedTeam = await context.Teams
            .AsNoTracking()
            .SingleAsync(t => t.Id == team.Id);

        var persistedPack = await context.Packs
            .AsNoTracking()
            .SingleAsync(p => p.Id == apiStandardsPack.Id);

        persistedTeam.Name.Should().Be("platform-core");
        persistedTeam.DisplayName.Should().Be("Platform Core Team");
        persistedTeam.Status.Should().Be(TeamStatus.Active);
        persistedTeam.ParentOrganizationUnit.Should().Be("Engineering");

        persistedPack.Name.Should().Be("api-contract-standards");
        persistedPack.Status.Should().Be(GovernancePackStatus.Draft);
        persistedPack.Category.Should().Be(GovernanceRuleCategory.Contracts);
        persistedPack.CurrentVersion.Should().BeNull("pack começa sem versão publicada");
    }

    [Fact]
    public async Task Governance_Should_Persist_MultipleTeams_And_Filter_ByStatus()
    {
        await ResetStateAsync();

        await using var context = Fixture.CreateGovernanceDbContext();

        var activeTeam1 = Team.Create("team-payments", "Payments Team");
        var activeTeam2 = Team.Create("team-orders", "Orders Team");
        var inactiveTeam = Team.Create("team-legacy", "Legacy Team");

        inactiveTeam.Deactivate();

        context.Teams.AddRange(activeTeam1, activeTeam2, inactiveTeam);
        await context.SaveChangesAsync();

        var activeTeams = await context.Teams
            .AsNoTracking()
            .Where(t => t.Status == TeamStatus.Active)
            .OrderBy(t => t.Name)
            .ToListAsync();

        activeTeams.Should().HaveCount(2);
        activeTeams[0].Name.Should().Be("team-orders");
        activeTeams[1].Name.Should().Be("team-payments");

        var allTeams = await context.Teams.AsNoTracking().CountAsync();
        allTeams.Should().Be(3);
    }

    // ── WorkflowDbContext ─────────────────────────────────────────────────────

    [Fact]
    public async Task Workflow_Should_Persist_Template_And_Instance_WithStatus()
    {
        await ResetStateAsync();

        await using var workflowContext = Fixture.CreateWorkflowDbContext();

        var template = WorkflowTemplate.Create(
            name: "API Breaking Change Approval",
            description: "Workflow for releasing breaking changes to production APIs",
            changeType: "Breaking",
            apiCriticality: "Critical",
            targetEnvironment: "Production",
            minimumApprovers: 2,
            createdAt: DateTimeOffset.UtcNow);

        workflowContext.WorkflowTemplates.Add(template);
        await workflowContext.SaveChangesAsync();

        var releaseId = Guid.NewGuid();

        var instance = WorkflowInstance.Create(
            workflowTemplateId: template.Id,
            releaseId: releaseId,
            submittedBy: "eng.lead@nextraceone.io",
            submittedAt: DateTimeOffset.UtcNow);

        workflowContext.WorkflowInstances.Add(instance);
        await workflowContext.SaveChangesAsync();

        var persistedTemplate = await workflowContext.WorkflowTemplates
            .AsNoTracking()
            .SingleAsync(t => t.Id == template.Id);

        var persistedInstance = await workflowContext.WorkflowInstances
            .AsNoTracking()
            .SingleAsync(i => i.Id == instance.Id);

        persistedTemplate.Name.Should().Be("API Breaking Change Approval");
        persistedTemplate.IsActive.Should().BeTrue();
        persistedTemplate.MinimumApprovers.Should().Be(2);
        persistedTemplate.ChangeType.Should().Be("Breaking");

        persistedInstance.WorkflowTemplateId.Should().Be(template.Id);
        persistedInstance.ReleaseId.Should().Be(releaseId);
        persistedInstance.SubmittedBy.Should().Be("eng.lead@nextraceone.io");
        persistedInstance.Status.Should().Be(ChangeGovernance.Domain.Workflow.Enums.WorkflowStatus.Draft);
    }

    [Fact]
    public async Task Workflow_Should_Query_Templates_By_TargetEnvironment()
    {
        await ResetStateAsync();

        await using var context = Fixture.CreateWorkflowDbContext();

        var prodTemplate = WorkflowTemplate.Create(
            "Production Deploy Approval", "Full approval workflow for production",
            "Breaking", "High", "Production", 3, DateTimeOffset.UtcNow);

        var stagingTemplate = WorkflowTemplate.Create(
            "Staging Deploy Approval", "Simplified approval for staging",
            "NonBreaking", "Medium", "Staging", 1, DateTimeOffset.UtcNow);

        context.WorkflowTemplates.AddRange(prodTemplate, stagingTemplate);
        await context.SaveChangesAsync();

        var productionTemplates = await context.WorkflowTemplates
            .AsNoTracking()
            .Where(t => t.TargetEnvironment == "Production" && t.IsActive)
            .ToListAsync();

        productionTemplates.Should().HaveCount(1);
        productionTemplates[0].Name.Should().Be("Production Deploy Approval");
        productionTemplates[0].MinimumApprovers.Should().Be(3);
    }

    // ── PromotionDbContext ────────────────────────────────────────────────────

    [Fact]
    public async Task Promotion_Should_Persist_DeploymentEnvironment_And_Request()
    {
        await ResetStateAsync();

        await using var promotionContext = Fixture.CreatePromotionDbContext();

        var stagingEnv = DeploymentEnvironment.Create(
            name: "Staging",
            description: "Pre-production staging environment for final validation",
            order: 2,
            requiresApproval: false,
            requiresEvidencePack: true,
            createdAt: DateTimeOffset.UtcNow);

        var productionEnv = DeploymentEnvironment.Create(
            name: "Production",
            description: "Live production environment — requires full approval",
            order: 3,
            requiresApproval: true,
            requiresEvidencePack: true,
            createdAt: DateTimeOffset.UtcNow);

        promotionContext.DeploymentEnvironments.AddRange(stagingEnv, productionEnv);
        await promotionContext.SaveChangesAsync();

        var promotionRequest = PromotionRequest.Create(
            releaseId: Guid.NewGuid(),
            sourceEnvironmentId: stagingEnv.Id,
            targetEnvironmentId: productionEnv.Id,
            requestedBy: "eng.lead@nextraceone.io",
            requestedAt: DateTimeOffset.UtcNow);

        promotionContext.PromotionRequests.Add(promotionRequest);
        await promotionContext.SaveChangesAsync();

        var request = await promotionContext.PromotionRequests
            .AsNoTracking()
            .SingleAsync(r => r.Id == promotionRequest.Id);

        request.Status.Should().Be(ChangeGovernance.Domain.Promotion.Enums.PromotionStatus.Pending);
        request.RequestedBy.Should().Be("eng.lead@nextraceone.io");
        request.SourceEnvironmentId.Should().Be(stagingEnv.Id);
        request.TargetEnvironmentId.Should().Be(productionEnv.Id);
        request.CompletedAt.Should().BeNull("promoção ainda não foi concluída");

        var environments = await promotionContext.DeploymentEnvironments
            .AsNoTracking()
            .OrderBy(e => e.Order)
            .ToListAsync();

        environments.Should().HaveCount(2);
        environments[0].Name.Should().Be("Staging");
        environments[0].RequiresApproval.Should().BeFalse();
        environments[1].Name.Should().Be("Production");
        environments[1].RequiresApproval.Should().BeTrue();
    }

    // ── RulesetGovernanceDbContext ────────────────────────────────────────────

    [Fact]
    public async Task Ruleset_Should_Persist_Custom_Ruleset_And_Deactivate()
    {
        await ResetStateAsync();

        await using var rulesetContext = Fixture.CreateRulesetGovernanceDbContext();

        var ruleset = Ruleset.Create(
            name: "Breaking Change Prevention",
            description: "Prevents breaking changes from reaching production without approval",
            content: """{"rules":[{"id":"no-field-removal","severity":"error"},{"id":"no-type-change","severity":"error"}]}""",
            rulesetType: RulesetType.Custom,
            createdAt: DateTimeOffset.UtcNow);

        rulesetContext.Rulesets.Add(ruleset);
        await rulesetContext.SaveChangesAsync();

        var persisted = await rulesetContext.Rulesets
            .AsNoTracking()
            .SingleAsync(r => r.Id == ruleset.Id);

        persisted.Name.Should().Be("Breaking Change Prevention");
        persisted.IsActive.Should().BeTrue();
        persisted.RulesetType.Should().Be(RulesetType.Custom);
        persisted.Content.Should().Contain("no-field-removal");

        // Now archive it
        await using var context2 = Fixture.CreateRulesetGovernanceDbContext();
        var toArchive = await context2.Rulesets.SingleAsync(r => r.Id == ruleset.Id);
        toArchive.Archive();
        await context2.SaveChangesAsync();

        var archivedRuleset = await rulesetContext.Rulesets
            .AsNoTracking()
            .SingleAsync(r => r.Id == ruleset.Id);

        archivedRuleset.IsActive.Should().BeFalse("ruleset foi arquivado");
    }

    [Fact]
    public async Task Ruleset_Should_Filter_Active_Rulesets_For_Linting()
    {
        await ResetStateAsync();

        await using var context = Fixture.CreateRulesetGovernanceDbContext();

        var activeRuleset1 = Ruleset.Create(
            "OpenAPI Standards", "Enforces OpenAPI spec standards",
            """{"rules":[{"id":"require-description"}]}""",
            RulesetType.Default, DateTimeOffset.UtcNow);

        var activeRuleset2 = Ruleset.Create(
            "Security Headers", "Validates security headers in contract",
            """{"rules":[{"id":"require-auth-header"}]}""",
            RulesetType.Custom, DateTimeOffset.UtcNow);

        var archivedRuleset = Ruleset.Create(
            "Old Standards", "Deprecated ruleset",
            """{"rules":[]}""",
            RulesetType.Default, DateTimeOffset.UtcNow);
        archivedRuleset.Archive();

        context.Rulesets.AddRange(activeRuleset1, activeRuleset2, archivedRuleset);
        await context.SaveChangesAsync();

        var activeRulesets = await context.Rulesets
            .AsNoTracking()
            .Where(r => r.IsActive)
            .OrderBy(r => r.Name)
            .ToListAsync();

        activeRulesets.Should().HaveCount(2);
        activeRulesets[0].Name.Should().Be("OpenAPI Standards");
        activeRulesets[1].Name.Should().Be("Security Headers");
    }
}
