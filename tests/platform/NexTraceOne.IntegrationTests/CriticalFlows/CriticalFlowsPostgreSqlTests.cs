using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Enums;
using NexTraceOne.Catalog.Domain.Graph.Entities;
using NexTraceOne.Catalog.Domain.Graph.Enums;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Enums;
using NexTraceOne.IdentityAccess.Domain.Entities;
using NexTraceOne.IdentityAccess.Domain.ValueObjects;
using NexTraceOne.IntegrationTests.Infrastructure;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Entities;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Enums;
using Xunit;

namespace NexTraceOne.IntegrationTests.CriticalFlows;

[Collection(PostgreSqlIntegrationCollection.Name)]
public sealed class CriticalFlowsPostgreSqlTests(PostgreSqlIntegrationFixture fixture) : IntegrationTestBase(fixture)
{
    [RequiresDockerFact]
    public async Task Migrations_Should_Be_Applied_For_All_Module_Databases()
    {
        var catalogMigrations = await Fixture.GetAppliedMigrationsCountAsync(Fixture.CatalogConnectionString);
        var changeGovernanceMigrations = await Fixture.GetAppliedMigrationsCountAsync(Fixture.ChangeGovernanceConnectionString);
        var identityMigrations = await Fixture.GetAppliedMigrationsCountAsync(Fixture.IdentityConnectionString);
        var incidentsMigrations = await Fixture.GetAppliedMigrationsCountAsync(Fixture.IncidentsConnectionString);
        var aiKnowledgeMigrations = await Fixture.GetAppliedMigrationsCountAsync(Fixture.AiKnowledgeConnectionString);
        var governanceMigrations = await Fixture.GetAppliedMigrationsCountAsync(Fixture.GovernanceConnectionString);
        var auditMigrations = await Fixture.GetAppliedMigrationsCountAsync(Fixture.AuditConnectionString);

        catalogMigrations.Should().BeGreaterThan(0, "catalog database has CatalogGraph + Contracts + Portal migrations");
        changeGovernanceMigrations.Should().BeGreaterThan(0, "change-governance database has ChangeIntelligence + Workflow + Promotion + Ruleset migrations");
        identityMigrations.Should().BeGreaterThan(0, "identity database has IdentityAccess migrations");
        incidentsMigrations.Should().BeGreaterThan(0, "incidents database has OI Incidents + Runtime + Cost migrations");
        aiKnowledgeMigrations.Should().BeGreaterThan(0, "aiknowledge database has AiGovernance + ExternalAi + AiOrchestration migrations");
        governanceMigrations.Should().BeGreaterThan(0, "governance database has Governance module migrations");
        auditMigrations.Should().BeGreaterThan(0, "audit database has AuditCompliance migrations");
    }

    [RequiresDockerFact]
    public async Task Catalog_SourceOfTruth_Should_Persist_And_Query_ApiOwnerJoin()
    {
        await ResetStateAsync();

        await using var context = Fixture.CreateCatalogGraphDbContext();

        var service = ServiceAsset.Create("orders-service", "sales", "team-orders", Guid.NewGuid());
        service.UpdateDetails(
            displayName: "Orders Service",
            description: "Source of Truth owner for Orders API",
            serviceType: ServiceType.RestApi,
            systemArea: "Commerce",
            criticality: Criticality.High,
            lifecycleStatus: LifecycleStatus.Active,
            exposureType: ExposureType.Internal,
            documentationUrl: "https://docs.nextrace.local/orders",
            repositoryUrl: "https://git.nextrace.local/orders-service");

        var api = ApiAsset.Register("Orders API", "/api/orders", "1.0.0", "internal", service);

        context.ServiceAssets.Add(service);
        context.ApiAssets.Add(api);
        await context.SaveChangesAsync();

        var projection = await context.ApiAssets
            .AsNoTracking()
            .Where(asset => asset.Id == api.Id)
            .Select(asset => new
            {
                ApiName = asset.Name,
                OwnerName = asset.OwnerService.Name,
                OwnerTeam = asset.OwnerService.TeamName
            })
            .SingleAsync();

        projection.ApiName.Should().Be("Orders API");
        projection.OwnerName.Should().Be("orders-service");
        projection.OwnerTeam.Should().Be("team-orders");
    }

    [RequiresDockerFact]
    public async Task Contracts_Should_Persist_Version_And_RuleViolation_With_RealEfQuery()
    {
        await ResetStateAsync();

        await using var context = Fixture.CreateContractsDbContext();

        var versionResult = ContractVersion.Import(
            apiAssetId: Guid.NewGuid(),
            semVer: "1.0.0",
            specContent: "{\"openapi\":\"3.0.0\"}",
            format: "json",
            importedFrom: "upload",
            protocol: ContractProtocol.OpenApi);

        versionResult.IsSuccess.Should().BeTrue();
        var version = versionResult.Value;

        var violation = ContractRuleViolation.Create(
            contractVersionId: version.Id,
            rulesetId: null,
            ruleName: "required-examples",
            severity: "Warning",
            message: "Contract should define examples for critical endpoints.",
            path: "/paths/~1orders/get",
            detectedAt: DateTimeOffset.UtcNow,
            suggestedFix: "Provide at least one example payload.");

        version.AddRuleViolation(violation);
        context.ContractVersions.Add(version);
        await context.SaveChangesAsync();

        var persisted = await context.ContractVersions
            .AsNoTracking()
            .Include(contract => contract.RuleViolations)
            .SingleAsync(contract => contract.Id == version.Id);

        persisted.SemVer.Should().Be("1.0.0");
        persisted.Protocol.Should().Be(ContractProtocol.OpenApi);
        persisted.RuleViolations.Should().ContainSingle();
        persisted.RuleViolations.Single().RuleName.Should().Be("required-examples");
    }

    [RequiresDockerFact]
    public async Task ChangeGovernance_Should_Persist_Release_And_ExternalMarker_With_Jsonb()
    {
        await ResetStateAsync();

        await using var context = Fixture.CreateChangeIntelligenceDbContext();

        var release = Release.Create(
            tenantId: Guid.NewGuid(),
            apiAssetId: Guid.NewGuid(),
            serviceName: "orders-service",
            version: "2026.03.18",
            environment: "production",
            pipelineSource: "github-actions/orders.yml",
            commitSha: "7e3d1c2f",
            createdAt: DateTimeOffset.UtcNow);

        release.SetMetadata("team-orders", "sales", "Release with contract updates");
        release.SetChangeScore(0.84m).IsSuccess.Should().BeTrue();

        var marker = ExternalMarker.Create(
            releaseId: release.Id,
            markerType: MarkerType.DeploymentFinished,
            sourceSystem: "GitHub",
            externalId: "run-4578",
            payload: "{\"deployment\":\"success\",\"region\":\"eu-west-1\"}",
            occurredAt: DateTimeOffset.UtcNow.AddMinutes(-2),
            receivedAt: DateTimeOffset.UtcNow);

        context.Releases.Add(release);
        context.ExternalMarkers.Add(marker);
        await context.SaveChangesAsync();

        var joined = await (
            from storedRelease in context.Releases.AsNoTracking()
            join storedMarker in context.ExternalMarkers.AsNoTracking()
                on storedRelease.Id equals storedMarker.ReleaseId
            where storedRelease.Id == release.Id
            select new
            {
                storedRelease.ServiceName,
                storedRelease.ChangeScore,
                storedMarker.SourceSystem,
                storedMarker.Payload
            }).SingleAsync();

        joined.ServiceName.Should().Be("orders-service");
        joined.ChangeScore.Should().Be(0.84m);
        joined.SourceSystem.Should().Be("GitHub");
        joined.Payload.Should().Contain("deployment");

        var payloadColumnType = await Fixture.GetColumnDataTypeAsync(
            Fixture.ChangeGovernanceConnectionString,
            tableName: "ci_external_markers",
            columnName: "Payload");

        payloadColumnType.Should().Be("jsonb");
    }

    [RequiresDockerFact]
    public async Task IdentityAccess_Should_Persist_LocalUser_And_Retrieve_ByPrimaryKey()
    {
        await ResetStateAsync();

        await using var context = Fixture.CreateIdentityDbContext();

        var user = User.CreateLocal(
            Email.Create("engineer@nextraceone.io"),
            FullName.Create("Ana", "Silva"),
            HashedPassword.FromPlainText("Str0ngPass!"));

        user.RegisterSuccessfulLogin(DateTimeOffset.UtcNow);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var persisted = await context.Users
            .AsNoTracking()
            .SingleAsync(entity => entity.Id == user.Id);

        persisted.Email.Value.Should().Be("engineer@nextraceone.io");
        persisted.FullName.Value.Should().Be("Ana Silva");
        persisted.IsActive.Should().BeTrue();
        persisted.LastLoginAt.Should().NotBeNull();
    }

    [RequiresDockerFact]
    public async Task Incidents_Should_Persist_IncidentAndWorkflow_With_Join_And_JsonbColumns()
    {
        await ResetStateAsync();

        await using var context = Fixture.CreateIncidentDbContext();

        var incident = IncidentRecord.Create(
            id: IncidentRecordId.New(),
            externalRef: "INC-2026-0318",
            title: "Orders API latency spike",
            description: "p95 latency above SLO after deployment",
            type: IncidentType.ServiceDegradation,
            severity: IncidentSeverity.Major,
            status: IncidentStatus.Investigating,
            serviceId: "orders-service",
            serviceName: "Orders Service",
            ownerTeam: "team-orders",
            impactedDomain: "sales",
            environment: "production",
            detectedAt: DateTimeOffset.UtcNow.AddMinutes(-10),
            lastUpdatedAt: DateTimeOffset.UtcNow,
            hasCorrelation: true,
            correlationConfidence: CorrelationConfidence.High,
            mitigationStatus: MitigationStatus.InProgress);

        incident.SetMitigation(
            mitigationActionsJson: "[{\"action\":\"rollback\",\"status\":\"pending\"}]",
            mitigationRecommendedRunbooksJson: "[{\"id\":\"rb-1\",\"title\":\"Rollback Orders\"}]",
            narrative: "Rollback candidate identified.",
            hasEscalationPath: true,
            escalationPath: "Platform On-Call");

        var workflow = MitigationWorkflowRecord.Create(
            id: MitigationWorkflowRecordId.New(),
            incidentId: incident.ExternalRef,
            title: "Rollback Orders API",
            status: MitigationWorkflowStatus.InProgress,
            actionType: MitigationActionType.RollbackCandidate,
            riskLevel: RiskLevel.High,
            requiresApproval: true,
            createdByUser: "ops.lead",
            stepsJson: "[{\"step\":\"validate canary\"}]");

        context.Incidents.Add(incident);
        context.MitigationWorkflows.Add(workflow);
        await context.SaveChangesAsync();

        var correlated = await (
            from storedIncident in context.Incidents.AsNoTracking()
            join storedWorkflow in context.MitigationWorkflows.AsNoTracking()
                on storedIncident.ExternalRef equals storedWorkflow.IncidentId
            where storedIncident.Id == incident.Id
            select new
            {
                storedIncident.ExternalRef,
                storedIncident.MitigationActionsJson,
                WorkflowStatus = storedWorkflow.Status,
                storedWorkflow.StepsJson
            }).SingleAsync();

        correlated.ExternalRef.Should().Be("INC-2026-0318");
        correlated.WorkflowStatus.Should().Be(MitigationWorkflowStatus.InProgress);
        correlated.MitigationActionsJson.Should().Contain("rollback");
        correlated.StepsJson.Should().Contain("validate canary");

        var incidentColumnType = await Fixture.GetColumnDataTypeAsync(
            Fixture.IncidentsConnectionString,
            tableName: "oi_incidents",
            columnName: "MitigationActionsJson");

        var workflowColumnType = await Fixture.GetColumnDataTypeAsync(
            Fixture.IncidentsConnectionString,
            tableName: "oi_mitigation_workflows",
            columnName: "StepsJson");

        incidentColumnType.Should().Be("jsonb");
        workflowColumnType.Should().Be("jsonb");
    }
}
