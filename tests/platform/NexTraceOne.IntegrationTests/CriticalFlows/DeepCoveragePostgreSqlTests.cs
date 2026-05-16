using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Enums;
using NexTraceOne.Catalog.Domain.Graph.Entities;
using NexTraceOne.Catalog.Domain.Graph.Enums;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;
using NexTraceOne.IdentityAccess.Domain.Entities;
using NexTraceOne.IdentityAccess.Domain.ValueObjects;
using NexTraceOne.IntegrationTests.Infrastructure;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Entities;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Enums;
using Xunit;

namespace NexTraceOne.IntegrationTests.CriticalFlows;

/// <summary>
/// Testes de integração de cobertura profunda com PostgreSQL real.
/// Valida persistência, queries EF, JSONB, joins e migrations para todos os domínios core.
/// </summary>
[Collection(PostgreSqlIntegrationCollection.Name)]
public sealed class DeepCoveragePostgreSqlTests(PostgreSqlIntegrationFixture fixture) : IntegrationTestBase(fixture)
{
    // ─── Catalog: Topology ──────────────────────────────────────────────────

    [RequiresDockerFact]
    public async Task Catalog_Topology_Should_Persist_MultipleServices_And_QueryOwnership()
    {
        await ResetStateAsync();

        await using var context = Fixture.CreateCatalogGraphDbContext();

        var serviceA = ServiceAsset.Create("payments-service", "finance", "team-payments", Guid.NewGuid());
        serviceA.UpdateDetails(
            displayName: "Payments Service",
            description: "Handles payment processing",
            serviceType: ServiceType.RestApi,
            systemArea: "Finance",
            criticality: Criticality.Critical,
            lifecycleStatus: LifecycleStatus.Active,
            exposureType: ExposureType.Internal,
            documentationUrl: null,
            repositoryUrl: null);

        var serviceB = ServiceAsset.Create("notifications-service", "platform", "team-platform", Guid.NewGuid());
        serviceB.UpdateDetails(
            displayName: "Notifications Service",
            description: "Sends notifications",
            serviceType: ServiceType.BackgroundService,
            systemArea: "Platform",
            criticality: Criticality.Medium,
            lifecycleStatus: LifecycleStatus.Active,
            exposureType: ExposureType.Internal,
            documentationUrl: null,
            repositoryUrl: null);

        var apiA = ApiAsset.Register("Payments API", "/api/payments", "2.0.0", "internal", serviceA);
        var apiB = ApiAsset.Register("Notifications API", "/api/notifications", "1.0.0", "internal", serviceB);

        context.ServiceAssets.AddRange(serviceA, serviceB);
        context.ApiAssets.AddRange(apiA, apiB);
        await context.SaveChangesAsync();

        var topology = await context.ApiAssets
            .AsNoTracking()
            .Select(api => new
            {
                ApiName = api.Name,
                RoutePattern = api.RoutePattern,
                OwnerDomain = api.OwnerService.Domain,
                OwnerTeam = api.OwnerService.TeamName,
                Criticality = api.OwnerService.Criticality
            })
            .OrderBy(api => api.ApiName)
            .ToListAsync();

        topology.Should().HaveCount(2);
        topology[0].ApiName.Should().Be("Notifications API");
        topology[0].OwnerTeam.Should().Be("team-platform");
        topology[1].ApiName.Should().Be("Payments API");
        topology[1].OwnerDomain.Should().Be("finance");
        topology[1].Criticality.Should().Be(Criticality.Critical);
    }

    [RequiresDockerFact]
    public async Task Catalog_Should_Persist_ConsumerRelationship_From_OtelInference()
    {
        await ResetStateAsync();

        await using var context = Fixture.CreateCatalogGraphDbContext();

        var ownerService = ServiceAsset.Create("orders-api-service", "commerce", "team-commerce", Guid.NewGuid());
        var api = ApiAsset.Register("Orders API", "/api/v1/orders", "1.0.0", "internal", ownerService);

        var result = api.InferDependencyFromOtel(
            consumerName: "billing-service",
            environment: "production",
            externalReference: "span-otel-ref-001",
            observedAt: DateTimeOffset.UtcNow.AddMinutes(-5),
            confidenceScore: 0.92m);

        result.IsSuccess.Should().BeTrue();

        context.ServiceAssets.Add(ownerService);
        context.ApiAssets.Add(api);
        await context.SaveChangesAsync();

        var persisted = await context.ApiAssets
            .AsNoTracking()
            .Include(a => a.ConsumerRelationships)
            .SingleAsync(a => a.Id == api.Id);

        persisted.ConsumerRelationships.Should().HaveCount(1);
        persisted.ConsumerRelationships[0].ConsumerName.Should().Be("billing-service");
        persisted.ConsumerRelationships[0].ConfidenceScore.Should().Be(0.92m);
    }

    // ─── Contracts: Artifact persistence ────────────────────────────────────

    [RequiresDockerFact]
    public async Task Contracts_Should_Persist_ContractVersion_With_Artifact_And_FilterByType()
    {
        await ResetStateAsync();

        await using var context = Fixture.CreateContractsDbContext();

        var versionResult = ContractVersion.Import(
            apiAssetId: Guid.NewGuid(),
            semVer: "2.1.0",
            specContent: "{\"openapi\":\"3.1.0\",\"info\":{\"title\":\"Orders\"}}",
            format: "json",
            importedFrom: "upload",
            protocol: ContractProtocol.OpenApi);

        versionResult.IsSuccess.Should().BeTrue();
        var version = versionResult.Value;

        var conformanceArtifact = ContractArtifact.Create(
            contractVersionId: version.Id,
            artifactType: ContractArtifactType.ProviderConformanceTest,
            name: "orders-conformance-tests.cs",
            content: "// generated test file content",
            contentFormat: "csharp",
            generatedBy: "automation",
            generatedAt: DateTimeOffset.UtcNow,
            isAiGenerated: false);

        var documentationArtifact = ContractArtifact.Create(
            contractVersionId: version.Id,
            artifactType: ContractArtifactType.Documentation,
            name: "orders-api-docs.md",
            content: "# Orders API Documentation",
            contentFormat: "markdown",
            generatedBy: "ai-local",
            generatedAt: DateTimeOffset.UtcNow,
            isAiGenerated: true);

        version.AddArtifact(conformanceArtifact);
        version.AddArtifact(documentationArtifact);
        context.ContractVersions.Add(version);
        await context.SaveChangesAsync();

        var conformanceCount = await context.ContractArtifacts
            .AsNoTracking()
            .Where(a => a.ContractVersionId == version.Id && a.ArtifactType == ContractArtifactType.ProviderConformanceTest)
            .CountAsync();

        var aiGeneratedDocs = await context.ContractArtifacts
            .AsNoTracking()
            .Where(a => a.ContractVersionId == version.Id && a.IsAiGenerated)
            .Select(a => new { a.Name, a.ContentFormat })
            .ToListAsync();

        conformanceCount.Should().Be(1);
        aiGeneratedDocs.Should().HaveCount(1);
        aiGeneratedDocs[0].Name.Should().Be("orders-api-docs.md");
        aiGeneratedDocs[0].ContentFormat.Should().Be("markdown");
    }

    [RequiresDockerFact]
    public async Task Contracts_Should_Persist_RuleViolation_With_NullRulesetId_And_WithRulesetId()
    {
        await ResetStateAsync();

        await using var context = Fixture.CreateContractsDbContext();

        var versionResult = ContractVersion.Import(
            apiAssetId: Guid.NewGuid(),
            semVer: "1.2.0",
            specContent: "{\"openapi\":\"3.0.0\"}",
            format: "json",
            importedFrom: "upload",
            protocol: ContractProtocol.OpenApi);

        var version = versionResult.Value;
        var externalRulesetId = Guid.NewGuid();

        var internalViolation = ContractRuleViolation.Create(
            contractVersionId: version.Id,
            rulesetId: null,
            ruleName: "internal-rule-check",
            severity: "Warning",
            message: "Detected by internal engine — no ruleset required.",
            path: "/paths/~1health",
            detectedAt: DateTimeOffset.UtcNow);

        var externalViolation = ContractRuleViolation.Create(
            contractVersionId: version.Id,
            rulesetId: externalRulesetId,
            ruleName: "org-naming-convention",
            severity: "Error",
            message: "Path names must use kebab-case.",
            path: "/paths/~1getUsers",
            detectedAt: DateTimeOffset.UtcNow,
            suggestedFix: "Rename to /paths/~1get-users");

        version.AddRuleViolation(internalViolation);
        version.AddRuleViolation(externalViolation);
        context.ContractVersions.Add(version);
        await context.SaveChangesAsync();

        var violations = await context.ContractVersions
            .AsNoTracking()
            .Include(v => v.RuleViolations)
            .Where(v => v.Id == version.Id)
            .SelectMany(v => v.RuleViolations)
            .OrderBy(v => v.RuleName)
            .ToListAsync();

        violations.Should().HaveCount(2);

        var withoutRuleset = violations.Single(v => v.RuleName == "internal-rule-check");
        withoutRuleset.RulesetId.Should().BeNull();

        var withRuleset = violations.Single(v => v.RuleName == "org-naming-convention");
        withRuleset.RulesetId.Should().Be(externalRulesetId);
        withRuleset.SuggestedFix.Should().Contain("get-users");
    }

    // ─── ChangeGovernance: BlastRadius join ─────────────────────────────────

    [RequiresDockerFact]
    public async Task ChangeGovernance_Should_Persist_Release_And_BlastRadiusReport_With_Join()
    {
        await ResetStateAsync();

        await using var context = Fixture.CreateChangeIntelligenceDbContext();

        var release = Release.Create(
            tenantId: Guid.NewGuid(),
            apiAssetId: Guid.NewGuid(),
            serviceName: "billing-service",
            version: "2026.03.19",
            environment: "production",
            pipelineSource: "github-actions/billing.yml",
            commitSha: "abc123de",
            createdAt: DateTimeOffset.UtcNow);

        release.SetMetadata("team-finance", "finance", "Billing v3 release");
        release.SetChangeScore(0.72m).IsSuccess.Should().BeTrue();

        var blast = BlastRadiusReport.Calculate(
            releaseId: release.Id,
            apiAssetId: release.ApiAssetId,
            directConsumers: ["orders-service", "checkout-service"],
            transitiveConsumers: ["reporting-service"],
            calculatedAt: DateTimeOffset.UtcNow);

        context.Releases.Add(release);
        context.BlastRadiusReports.Add(blast);
        await context.SaveChangesAsync();

        var joined = await (
            from r in context.Releases.AsNoTracking()
            join b in context.BlastRadiusReports.AsNoTracking() on r.Id equals b.ReleaseId
            where r.Id == release.Id
            select new
            {
                r.ServiceName,
                r.ChangeScore,
                b.TotalAffectedConsumers,
                b.DirectConsumers,
                b.TransitiveConsumers
            }).SingleAsync();

        joined.ServiceName.Should().Be("billing-service");
        joined.ChangeScore.Should().Be(0.72m);
        joined.TotalAffectedConsumers.Should().Be(3);
        joined.DirectConsumers.Should().Contain("orders-service");
        joined.TransitiveConsumers.Should().Contain("reporting-service");
    }

    [RequiresDockerFact]
    public async Task ChangeGovernance_Should_Filter_Releases_By_Environment_And_Score()
    {
        await ResetStateAsync();

        await using var context = Fixture.CreateChangeIntelligenceDbContext();

        var apiId = Guid.NewGuid();
        var prodRelease = Release.Create(Guid.NewGuid(), apiId, "auth-service", "2026.1", "production", "pipeline", "sha1", DateTimeOffset.UtcNow);
        prodRelease.SetChangeScore(0.90m);

        var stagingRelease = Release.Create(Guid.NewGuid(), apiId, "auth-service", "2026.2", "staging", "pipeline", "sha2", DateTimeOffset.UtcNow);
        stagingRelease.SetChangeScore(0.30m);

        context.Releases.AddRange(prodRelease, stagingRelease);
        await context.SaveChangesAsync();

        var highRiskProdReleases = await context.Releases
            .AsNoTracking()
            .Where(r => r.Environment == "production" && r.ChangeScore >= 0.80m)
            .Select(r => new { r.ServiceName, r.Environment, r.ChangeScore })
            .ToListAsync();

        highRiskProdReleases.Should().HaveCount(1);
        highRiskProdReleases[0].ServiceName.Should().Be("auth-service");
        highRiskProdReleases[0].ChangeScore.Should().Be(0.90m);
    }

    // ─── IdentityAccess: Role + Membership join ──────────────────────────────

    [RequiresDockerFact]
    public async Task IdentityAccess_Should_Persist_User_Role_Membership_And_QueryJoin()
    {
        await ResetStateAsync();

        await using var context = Fixture.CreateIdentityDbContext();

        var user = User.CreateLocal(
            Email.Create("techlead@nextraceone.io"),
            FullName.Create("Carlos", "Mendes"),
            HashedPassword.FromPlainText("S3cur3Pass!"));

        var role = Role.CreateCustom(name: "TechLead", description: "Tech lead with approval rights");

        var tenantId = TenantId.New();

        var membership = TenantMembership.Create(
            userId: user.Id,
            tenantId: tenantId,
            roleId: role.Id,
            joinedAt: DateTimeOffset.UtcNow);

        context.Users.Add(user);
        context.Roles.Add(role);
        context.TenantMemberships.Add(membership);
        await context.SaveChangesAsync();

        var result = await (
            from m in context.TenantMemberships.AsNoTracking()
            join u in context.Users.AsNoTracking() on m.UserId equals u.Id
            join r in context.Roles.AsNoTracking() on m.RoleId equals r.Id
            where m.TenantId == tenantId && m.IsActive
            select new
            {
                UserEmail = u.Email.Value,
                UserFullName = u.FullName.Value,
                RoleName = r.Name,
                m.IsActive
            }).SingleAsync();

        result.UserEmail.Should().Be("techlead@nextraceone.io");
        result.UserFullName.Should().Be("Carlos Mendes");
        result.RoleName.Should().Be("TechLead");
        result.IsActive.Should().BeTrue();
    }

    [RequiresDockerFact]
    public async Task IdentityAccess_Should_Query_Active_Members_Per_Tenant()
    {
        await ResetStateAsync();

        await using var context = Fixture.CreateIdentityDbContext();

        var tenantId = TenantId.New();
        var roleId = RoleId.New();

        var user1 = User.CreateLocal(Email.Create("eng1@nextraceone.io"), FullName.Create("Ana", "Souza"), HashedPassword.FromPlainText("Str0ngPass1!"));
        var user2 = User.CreateLocal(Email.Create("eng2@nextraceone.io"), FullName.Create("Bruno", "Lima"), HashedPassword.FromPlainText("Str0ngPass2!"));
        var user3 = User.CreateLocal(Email.Create("eng3@nextraceone.io"), FullName.Create("Carla", "Faria"), HashedPassword.FromPlainText("Str0ngPass3!"));

        var m1 = TenantMembership.Create(user1.Id, tenantId, roleId, DateTimeOffset.UtcNow);
        var m2 = TenantMembership.Create(user2.Id, tenantId, roleId, DateTimeOffset.UtcNow);
        var m3 = TenantMembership.Create(user3.Id, tenantId, roleId, DateTimeOffset.UtcNow);
        m3.Deactivate();

        context.Users.AddRange(user1, user2, user3);
        context.TenantMemberships.AddRange(m1, m2, m3);
        await context.SaveChangesAsync();

        var activeCount = await context.TenantMemberships
            .AsNoTracking()
            .Where(m => m.TenantId == tenantId && m.IsActive)
            .CountAsync();

        activeCount.Should().Be(2);
    }

    // ─── Incidents: Runbook JSONB + Correlation query ────────────────────────

    [RequiresDockerFact]
    public async Task Incidents_Should_Persist_Runbook_And_ValidateStepsJson_IsJsonb()
    {
        await ResetStateAsync();

        await using var context = Fixture.CreateIncidentDbContext();

        var runbook = RunbookRecord.Create(
            id: RunbookRecordId.New(),
            title: "High Latency Rollback Runbook",
            description: "Steps to roll back a service causing high latency in production.",
            linkedService: "orders-service",
            linkedIncidentType: "ServiceDegradation",
            stepsJson: "[{\"step\":1,\"action\":\"check metrics\"},{\"step\":2,\"action\":\"rollback canary\"}]",
            prerequisitesJson: "[{\"prereq\":\"access to ArgoCD\"}]",
            postNotes: "Verify SLO compliance after rollback.",
            maintainedBy: "platform-oncall",
            publishedAt: DateTimeOffset.UtcNow,
            lastReviewedAt: DateTimeOffset.UtcNow.AddDays(-7));

        context.Runbooks.Add(runbook);
        await context.SaveChangesAsync();

        var persisted = await context.Runbooks
            .AsNoTracking()
            .SingleAsync(r => r.Id == runbook.Id);

        persisted.Title.Should().Be("High Latency Rollback Runbook");
        persisted.LinkedService.Should().Be("orders-service");
        persisted.StepsJson.Should().Contain("rollback canary");
        persisted.PrerequisitesJson.Should().Contain("ArgoCD");

        var stepsColumnType = await Fixture.GetColumnDataTypeAsync(
            Fixture.IncidentsConnectionString,
            tableName: "oi_runbooks",
            columnName: "StepsJson");

        stepsColumnType.Should().Be("jsonb");
    }

    [RequiresDockerFact]
    public async Task Incidents_Should_Filter_Runbooks_By_LinkedService()
    {
        await ResetStateAsync();

        await using var context = Fixture.CreateIncidentDbContext();

        var runbook1 = RunbookRecord.Create(RunbookRecordId.New(), "Orders Rollback", "desc1", "orders-service", null,
            "[{\"step\":\"validate\"}]", null, null, "oncall", DateTimeOffset.UtcNow);
        var runbook2 = RunbookRecord.Create(RunbookRecordId.New(), "Payments Rollback", "desc2", "payments-service", null,
            "[{\"step\":\"rollback\"}]", null, null, "oncall", DateTimeOffset.UtcNow);
        var runbook3 = RunbookRecord.Create(RunbookRecordId.New(), "Orders Failover", "desc3", "orders-service", null,
            "[{\"step\":\"failover\"}]", null, null, "oncall", DateTimeOffset.UtcNow);

        context.Runbooks.AddRange(runbook1, runbook2, runbook3);
        await context.SaveChangesAsync();

        var ordersRunbooks = await context.Runbooks
            .AsNoTracking()
            .Where(r => r.LinkedService == "orders-service")
            .OrderBy(r => r.Title)
            .Select(r => new { r.Title, r.StepsJson })
            .ToListAsync();

        ordersRunbooks.Should().HaveCount(2);
        ordersRunbooks[0].Title.Should().Be("Orders Failover");
        ordersRunbooks[1].Title.Should().Be("Orders Rollback");
    }

    [RequiresDockerFact]
    public async Task Incidents_Should_Persist_CorrelationData_And_QueryByConfidence()
    {
        await ResetStateAsync();

        await using var context = Fixture.CreateIncidentDbContext();

        var highConfidenceIncident = IncidentRecord.Create(
            id: IncidentRecordId.New(),
            externalRef: "INC-2026-HIGH",
            title: "Payments timeout after deploy",
            description: "p99 latency above threshold",
            type: IncidentType.ServiceDegradation,
            severity: IncidentSeverity.Critical,
            status: IncidentStatus.Investigating,
            serviceId: "payments-service",
            serviceName: "Payments Service",
            ownerTeam: "team-finance",
            impactedDomain: "finance",
            environment: "production",
            detectedAt: DateTimeOffset.UtcNow.AddMinutes(-20),
            lastUpdatedAt: DateTimeOffset.UtcNow,
            hasCorrelation: true,
            correlationConfidence: CorrelationConfidence.High,
            mitigationStatus: MitigationStatus.InProgress);

        highConfidenceIncident.SetCorrelation(
            analysis: "Temporal correlation with billing-service deploy at T-18min",
            correlatedChangesJson: "[{\"changeId\":\"a1b2c3\",\"type\":\"Deployment\"}]",
            correlatedServicesJson: "[{\"serviceId\":\"billing-service\"}]",
            correlatedDependenciesJson: "[{\"serviceId\":\"auth-service\",\"relationship\":\"calls\"}]",
            impactedContractsJson: "[{\"contractVersionId\":\"cv-001\",\"protocol\":\"OpenApi\"}]");

        var lowConfidenceIncident = IncidentRecord.Create(
            id: IncidentRecordId.New(),
            externalRef: "INC-2026-LOW",
            title: "Intermittent health check",
            description: "Health endpoint intermittently returning 503",
            type: IncidentType.ServiceDegradation,
            severity: IncidentSeverity.Minor,
            status: IncidentStatus.Monitoring,
            serviceId: "cache-service",
            serviceName: "Cache Service",
            ownerTeam: "team-platform",
            impactedDomain: "platform",
            environment: "production",
            detectedAt: DateTimeOffset.UtcNow.AddMinutes(-5),
            lastUpdatedAt: DateTimeOffset.UtcNow,
            hasCorrelation: false,
            correlationConfidence: CorrelationConfidence.NotAssessed,
            mitigationStatus: MitigationStatus.NotStarted);

        context.Incidents.AddRange(highConfidenceIncident, lowConfidenceIncident);
        await context.SaveChangesAsync();

        var highConfidenceResults = await context.Incidents
            .AsNoTracking()
            .Where(i => i.HasCorrelation && i.CorrelationConfidence == CorrelationConfidence.High)
            .Select(i => new
            {
                i.ExternalRef,
                i.CorrelatedChangesJson,
                i.ImpactedContractsJson
            })
            .ToListAsync();

        highConfidenceResults.Should().HaveCount(1);
        highConfidenceResults[0].ExternalRef.Should().Be("INC-2026-HIGH");
        highConfidenceResults[0].CorrelatedChangesJson.Should().Contain("Deployment");
        highConfidenceResults[0].ImpactedContractsJson.Should().Contain("OpenApi");
    }
}
