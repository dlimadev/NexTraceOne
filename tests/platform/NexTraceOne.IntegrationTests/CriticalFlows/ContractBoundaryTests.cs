using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;
using NexTraceOne.AuditCompliance.Domain.Entities;
using NexTraceOne.Catalog.Domain.Graph.Entities;
using NexTraceOne.Catalog.Domain.Graph.Enums;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Enums;
using NexTraceOne.Governance.Domain.Entities;
using NexTraceOne.Governance.Domain.Enums;
using NexTraceOne.IntegrationTests.Infrastructure;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Entities;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Enums;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Entities;
using Xunit;

namespace NexTraceOne.IntegrationTests.CriticalFlows;

/// <summary>
/// Testes de fronteira de contrato entre módulos críticos do NexTraceOne.
///
/// Estes testes validam que as fronteiras cross-module respeitam os shapes esperados:
/// identificadores compartilhados (GUIDs, slugs) são consistentes entre módulos e
/// entidades de módulos diferentes podem coexistir nos mesmos databases sem conflito.
///
/// Abordagem: testes persistem dados em módulo A e consultam via módulo B
/// usando os mesmos DbContexts reais — sem mocks, sem stubs.
///
/// Classificação: ALTA CONFIANÇA
/// - PostgreSQL real via Testcontainers
/// - Migrations aplicadas
/// - Reset de estado via Respawn entre cenários
///
/// Fronteiras cobertas:
/// 1. Reliability (OI) ↔ Catalog — ServiceName como identificador compartilhado por convenção
/// 2. ChangeGovernance ↔ Incidents — coexistência no database de operations
/// 3. AIKnowledge ↔ Catalog — referência de ServiceId em conversas AI
/// 4. AuditCompliance ↔ Identity — coexistência no database de identity
/// 5. Governance ↔ Catalog — TeamName como identificador convencional compartilhado
/// </summary>
[Collection(PostgreSqlIntegrationCollection.Name)]
public sealed class ContractBoundaryTests(PostgreSqlIntegrationFixture fixture) : IntegrationTestBase(fixture)
{
    // ── Fronteira 1: Reliability (OI) ↔ Catalog ──────────────────────────────

    /// <summary>
    /// Valida que um serviço criado no Catalog pode ser referenciado pelo
    /// módulo de Reliability via ServiceName (identificador por convenção).
    ///
    /// Contrato crítico: ServiceName no RuntimeSnapshot deve corresponder ao Name
    /// do ServiceAsset em Catalog. Não há FK real entre databases — o contrato é
    /// por convenção de naming.
    /// </summary>
    [Fact]
    public async Task Catalog_ServiceName_Referenced_By_Reliability_Should_Be_Consistent()
    {
        await ResetStateAsync();

        // Arrange — criar serviço no Catalog
        await using var catalogContext = Fixture.CreateCatalogGraphDbContext();

        var service = ServiceAsset.Create("reliability-boundary-svc", "platform", "team-platform");
        service.UpdateDetails(
            displayName: "Reliability Boundary Service",
            description: "Serviço de teste para validação de fronteira Reliability↔Catalog",
            serviceType: ServiceType.RestApi,
            systemArea: "Platform",
            criticality: Criticality.High,
            lifecycleStatus: LifecycleStatus.Active,
            exposureType: ExposureType.Internal,
            documentationUrl: null,
            repositoryUrl: null);

        catalogContext.ServiceAssets.Add(service);
        await catalogContext.SaveChangesAsync();

        // Act — criar RuntimeSnapshot referenciando o ServiceName do Catalog
        await using var runtimeContext = Fixture.CreateRuntimeIntelligenceDbContext();

        var snapshot = RuntimeSnapshot.Create(
            serviceName: "reliability-boundary-svc",
            environment: "staging",
            avgLatencyMs: 120m,
            p99LatencyMs: 350m,
            errorRate: 0.02m,
            requestsPerSecond: 45m,
            cpuUsagePercent: 35m,
            memoryUsageMb: 256m,
            activeInstances: 2,
            capturedAt: DateTimeOffset.UtcNow,
            source: "contract-boundary-test");

        runtimeContext.RuntimeSnapshots.Add(snapshot);
        await runtimeContext.SaveChangesAsync();

        // Assert — o ServiceName no snapshot corresponde ao Name do serviço no Catalog
        var persistedService = await catalogContext.ServiceAssets
            .AsNoTracking()
            .SingleOrDefaultAsync(s => s.Name == "reliability-boundary-svc");

        var persistedSnapshot = await runtimeContext.RuntimeSnapshots
            .AsNoTracking()
            .SingleOrDefaultAsync(s => s.ServiceName == "reliability-boundary-svc");

        persistedService.Should().NotBeNull("o serviço deve estar persistido no Catalog");
        persistedSnapshot.Should().NotBeNull("o snapshot de runtime deve estar persistido em OI");

        // Contrato: o ServiceName no snapshot deve corresponder ao Name no Catalog
        persistedSnapshot!.ServiceName.Should().Be(persistedService!.Name,
            "o identificador de serviço deve ser consistente entre Reliability e Catalog (contrato por convenção de naming)");

        persistedSnapshot.Environment.Should().Be("staging",
            "o ambiente deve ser preservado corretamente no snapshot");
    }

    // ── Fronteira 2: ChangeGovernance ↔ Incidents ────────────────────────────

    /// <summary>
    /// Valida que Release (ChangeGovernance) e IncidentRecord (OI) podem coexistir
    /// no mesmo database de operations sem conflito de schema ou naming.
    ///
    /// Contrato crítico: ChangeGovernance e OI compartilham o database nextraceone_operations.
    /// As tabelas devem ter prefixos únicos que evitem qualquer conflito.
    /// </summary>
    [Fact]
    public async Task ChangeGovernance_And_Incidents_Should_Coexist_In_Operations_Database()
    {
        await ResetStateAsync();

        // Arrange + Act — persistir Release
        await using var changeContext = Fixture.CreateChangeIntelligenceDbContext();

        var apiAssetId = Guid.NewGuid();
        var release = Release.Create(
            apiAssetId: apiAssetId,
            serviceName: "payments-service",
            version: "2.1.0",
            environment: "staging",
            pipelineSource: "github-actions/payments.yml",
            commitSha: "abc12345",
            createdAt: DateTimeOffset.UtcNow);

        changeContext.Releases.Add(release);
        await changeContext.SaveChangesAsync();

        // Act — persistir IncidentRecord no mesmo banco (operations)
        await using var incidentContext = Fixture.CreateIncidentDbContext();

        var incident = IncidentRecord.Create(
            id: IncidentRecordId.New(),
            externalRef: "CB-TEST-001",
            title: "Contract Boundary Test Incident",
            description: "Incidente criado para validar coexistência com ChangeGovernance",
            type: IncidentType.ServiceDegradation,
            severity: IncidentSeverity.Major,
            status: IncidentStatus.Investigating,
            serviceId: apiAssetId.ToString(),
            serviceName: "Payments Service",
            ownerTeam: "team-payments",
            impactedDomain: "finance",
            environment: "staging",
            detectedAt: DateTimeOffset.UtcNow.AddMinutes(-5),
            lastUpdatedAt: DateTimeOffset.UtcNow,
            hasCorrelation: false,
            correlationConfidence: CorrelationConfidence.NotAssessed,
            mitigationStatus: MitigationStatus.NotStarted);

        incidentContext.Incidents.Add(incident);
        await incidentContext.SaveChangesAsync();

        // Assert — ambas as entidades existem e foram persistidas sem conflito
        var persistedRelease = await changeContext.Releases
            .AsNoTracking()
            .SingleOrDefaultAsync(r => r.Version == "2.1.0");

        var persistedIncident = await incidentContext.Incidents
            .AsNoTracking()
            .SingleOrDefaultAsync(i => i.Title == "Contract Boundary Test Incident");

        persistedRelease.Should().NotBeNull(
            "Release deve persistir no database operations sem conflito com Incidents");
        persistedIncident.Should().NotBeNull(
            "IncidentRecord deve persistir no database operations sem conflito com ChangeGovernance");

        // Contrato: ServiceId referenciado no incidente deve ser o mesmo asset do release
        persistedIncident!.ServiceId.Should().Be(apiAssetId.ToString(),
            "o ServiceId no incidente deve referenciar o mesmo asset do release");

        persistedRelease!.Environment.Should().Be("staging",
            "o ambiente do release deve ser preservado corretamente");
    }

    // ── Fronteira 3: AIKnowledge ↔ Catalog ───────────────────────────────────

    /// <summary>
    /// Valida que uma conversa AI pode referenciar um ServiceId do Catalog
    /// sem violação de constraints ou inconsistência de identificador.
    ///
    /// Contrato crítico: AIKnowledge usa ServiceId (GUID) como referência externa
    /// ao Catalog sem FK real. O contrato é por convenção.
    /// </summary>
    [Fact]
    public async Task AIKnowledge_Conversation_Can_Reference_Catalog_ServiceId()
    {
        await ResetStateAsync();

        // Arrange — criar serviço no Catalog
        await using var catalogContext = Fixture.CreateCatalogGraphDbContext();

        var service = ServiceAsset.Create("ai-boundary-svc", "platform", "team-platform");
        service.UpdateDetails(
            displayName: "AI Boundary Service",
            description: "Serviço para teste de fronteira AIKnowledge↔Catalog",
            serviceType: ServiceType.RestApi,
            systemArea: "Platform",
            criticality: Criticality.Medium,
            lifecycleStatus: LifecycleStatus.Active,
            exposureType: ExposureType.Internal,
            documentationUrl: null,
            repositoryUrl: null);

        catalogContext.ServiceAssets.Add(service);
        await catalogContext.SaveChangesAsync();

        // Act — criar conversa AI com referência ao ServiceId do Catalog
        await using var aiContext = Fixture.CreateAiGovernanceDbContext();

        var conversation = AiAssistantConversation.Start(
            title: "Análise do AI Boundary Service",
            persona: "Engineer",
            clientType: AIClientType.Web,
            defaultContextScope: "services,contracts,incidents",
            createdBy: "engineer@nextraceone.io",
            serviceId: service.Id.Value);

        aiContext.Conversations.Add(conversation);
        await aiContext.SaveChangesAsync();

        // Assert — a conversa AI foi persistida com o ServiceId correto
        var persistedConversation = await aiContext.Conversations
            .AsNoTracking()
            .SingleOrDefaultAsync(c => c.Title == "Análise do AI Boundary Service");

        var persistedService = await catalogContext.ServiceAssets
            .AsNoTracking()
            .SingleOrDefaultAsync(s => s.Name == "ai-boundary-svc");

        persistedConversation.Should().NotBeNull(
            "a conversa AI deve persistir com referência ao ServiceId do Catalog");
        persistedService.Should().NotBeNull(
            "o serviço deve existir no Catalog para validar a referência");

        // Contrato: ServiceId na conversa corresponde ao Id do serviço no Catalog
        persistedConversation!.ServiceId.Should().Be(persistedService!.Id.Value,
            "a conversa AI deve referenciar o ServiceId correto do Catalog");

        persistedConversation.Persona.Should().Be("Engineer",
            "a persona deve ser preservada na conversa AI");
        persistedConversation.DefaultContextScope.Should().Contain("services",
            "o scope de contexto deve incluir 'services' para grounding correto com o Catalog");
    }

    // ── Fronteira 4: AuditCompliance ↔ Identity ──────────────────────────────

    /// <summary>
    /// Valida que as tabelas de AuditCompliance e IdentityAccess coexistem
    /// no mesmo database (nextraceone_identity) sem conflito de schema.
    ///
    /// Contrato crítico: ambos os módulos partilham o database identity mas
    /// usam prefixos únicos para evitar colisão de nomes de tabelas.
    /// </summary>
    [Fact]
    public async Task AuditCompliance_Tables_Should_Coexist_With_Identity_Tables()
    {
        // Assert — verificar que as tabelas de ambos os módulos existem no mesmo banco
        var auditEventsTableExists = await Fixture.TableExistsAsync(
            Fixture.AuditConnectionString, "audit_events");

        var identityUsersTableExists = await Fixture.TableExistsAsync(
            Fixture.IdentityConnectionString, "ia_users");

        // As duas connection strings apontam para o mesmo banco (identity)
        Fixture.AuditConnectionString.Should().Be(Fixture.IdentityConnectionString,
            "AuditCompliance e IdentityAccess devem compartilhar o mesmo database identity (ADR-001)");

        auditEventsTableExists.Should().BeTrue(
            "a tabela audit_events deve existir no database identity após migrations do AuditCompliance");

        identityUsersTableExists.Should().BeTrue(
            "a tabela ia_users deve existir no database identity após migrations do IdentityAccess");
    }

    /// <summary>
    /// Valida que um AuditEvent pode ser persistido no mesmo banco onde o IdentityAccess opera,
    /// sem conflito de transações ou constraints.
    /// </summary>
    [Fact]
    public async Task AuditEvent_Can_Be_Persisted_In_Identity_Database()
    {
        await ResetStateAsync();

        // Act — criar e persistir AuditEvent no banco identity
        await using var auditContext = Fixture.CreateAuditDbContext();

        var auditEvent = AuditEvent.Record(
            sourceModule: "IdentityAccess",
            actionType: "UserCreated",
            resourceId: Guid.NewGuid().ToString(),
            resourceType: "User",
            performedBy: "system@nextraceone.io",
            occurredAt: DateTimeOffset.UtcNow,
            tenantId: Guid.Parse("11111111-1111-1111-1111-111111111111"),
            payload: """{"email":"test@example.com","role":"Engineer"}""");

        auditContext.AuditEvents.Add(auditEvent);
        await auditContext.SaveChangesAsync();

        // Assert — o evento foi persistido com todos os campos corretos
        var persistedEvent = await auditContext.AuditEvents
            .AsNoTracking()
            .SingleOrDefaultAsync(e => e.ActionType == "UserCreated" && e.SourceModule == "IdentityAccess");

        persistedEvent.Should().NotBeNull(
            "AuditEvent deve persistir no banco identity sem conflito com IdentityAccess");
        persistedEvent!.SourceModule.Should().Be("IdentityAccess",
            "o módulo de origem deve ser preservado na trilha de auditoria");
        persistedEvent.ActionType.Should().Be("UserCreated",
            "o tipo de ação deve ser preservado para rastreabilidade");
        persistedEvent.TenantId.Should().Be(Guid.Parse("11111111-1111-1111-1111-111111111111"),
            "o TenantId deve ser preservado para isolamento multi-tenant");
    }

    // ── Fronteira 5: Governance ↔ Catalog ─────────────────────────────────────

    /// <summary>
    /// Valida que o TeamName em Governance é compatível por convenção
    /// com o campo TeamName de ServiceAsset em Catalog.
    ///
    /// Contrato crítico: não há FK entre Governance e Catalog.
    /// A referência é por TeamName (string) por convenção. Este teste garante que
    /// a convenção de naming está alinhada entre os módulos.
    /// </summary>
    [Fact]
    public async Task Governance_Team_Name_Matches_Catalog_TeamName_Convention()
    {
        await ResetStateAsync();

        const string teamName = "team-payments";

        // Arrange — criar Team em Governance
        await using var governanceContext = Fixture.CreateGovernanceDbContext();

        var team = Team.Create(
            name: teamName,
            displayName: "Payments Team",
            description: "Equipa responsável pelo módulo de pagamentos",
            parentOrganizationUnit: "Engineering");

        governanceContext.Teams.Add(team);
        await governanceContext.SaveChangesAsync();

        // Arrange — criar ServiceAsset em Catalog referenciando o mesmo TeamName
        await using var catalogContext = Fixture.CreateCatalogGraphDbContext();

        var service = ServiceAsset.Create(
            name: "payments-api-v3",
            domain: "finance",
            teamName: teamName);

        service.UpdateDetails(
            displayName: "Payments API v3",
            description: "Serviço para teste de convenção de TeamName Governance↔Catalog",
            serviceType: ServiceType.RestApi,
            systemArea: "Finance",
            criticality: Criticality.Critical,
            lifecycleStatus: LifecycleStatus.Active,
            exposureType: ExposureType.Internal,
            documentationUrl: null,
            repositoryUrl: null);

        catalogContext.ServiceAssets.Add(service);
        await catalogContext.SaveChangesAsync();

        // Assert — o TeamName em Governance corresponde ao TeamName em Catalog
        var persistedTeam = await governanceContext.Teams
            .AsNoTracking()
            .SingleOrDefaultAsync(t => t.Name == teamName);

        var persistedService = await catalogContext.ServiceAssets
            .AsNoTracking()
            .SingleOrDefaultAsync(s => s.TeamName == teamName);

        persistedTeam.Should().NotBeNull(
            "o Team deve existir em Governance com o name correto");
        persistedService.Should().NotBeNull(
            "o ServiceAsset deve existir em Catalog referenciando o mesmo TeamName");

        // Contrato por convenção: Team.Name == ServiceAsset.TeamName
        persistedTeam!.Name.Should().Be(persistedService!.TeamName,
            "o identificador de equipa deve ser idêntico em Governance e Catalog (contrato por convenção de naming)");

        persistedTeam.Status.Should().Be(TeamStatus.Active,
            "uma equipa criada deve estar ativa por padrão");
    }

    // ── Fronteira 6: RuntimeSnapshot ↔ IncidentRecord coexistência ───────────

    /// <summary>
    /// Valida que RuntimeSnapshot (OI) e IncidentRecord (OI) coexistem no mesmo
    /// database de operations sem conflito, usando prefixos distintos de tabela.
    /// </summary>
    [Fact]
    public async Task RuntimeSnapshot_And_IncidentRecord_Should_Coexist_In_Operations_Database()
    {
        await ResetStateAsync();

        // Act — persistir RuntimeSnapshot em OI
        await using var runtimeContext = Fixture.CreateRuntimeIntelligenceDbContext();

        var snapshot = RuntimeSnapshot.Create(
            serviceName: "coexistence-test-svc",
            environment: "staging",
            avgLatencyMs: 85m,
            p99LatencyMs: 210m,
            errorRate: 0.01m,
            requestsPerSecond: 120m,
            cpuUsagePercent: 28m,
            memoryUsageMb: 512m,
            activeInstances: 3,
            capturedAt: DateTimeOffset.UtcNow,
            source: "contract-boundary-test");

        runtimeContext.RuntimeSnapshots.Add(snapshot);
        await runtimeContext.SaveChangesAsync();

        // Act — persistir IncidentRecord no mesmo banco (operations)
        await using var incidentContext = Fixture.CreateIncidentDbContext();

        var incident = IncidentRecord.Create(
            id: IncidentRecordId.New(),
            externalRef: "CB-TEST-002",
            title: "Coexistence Test Incident",
            description: "Incidente para teste de coexistência no operations DB",
            type: IncidentType.ServiceDegradation,
            severity: IncidentSeverity.Minor,
            status: IncidentStatus.Investigating,
            serviceId: "coexistence-test-svc",
            serviceName: "Coexistence Test Service",
            ownerTeam: "team-platform",
            impactedDomain: null,
            environment: "staging",
            detectedAt: DateTimeOffset.UtcNow.AddMinutes(-2),
            lastUpdatedAt: DateTimeOffset.UtcNow,
            hasCorrelation: false,
            correlationConfidence: CorrelationConfidence.NotAssessed,
            mitigationStatus: MitigationStatus.NotStarted);

        incidentContext.Incidents.Add(incident);
        await incidentContext.SaveChangesAsync();

        // Assert — ambos os registos existem sem conflito
        var persistedSnapshot = await runtimeContext.RuntimeSnapshots
            .AsNoTracking()
            .SingleOrDefaultAsync(s => s.ServiceName == "coexistence-test-svc");

        var persistedIncident = await incidentContext.Incidents
            .AsNoTracking()
            .SingleOrDefaultAsync(i => i.Title == "Coexistence Test Incident");

        persistedSnapshot.Should().NotBeNull(
            "RuntimeSnapshot deve persistir sem conflito com IncidentRecord no operations DB");
        persistedIncident.Should().NotBeNull(
            "IncidentRecord deve persistir sem conflito com RuntimeSnapshot no operations DB");

        // Contrato: o ServiceId no incidente deve corresponder ao ServiceName no snapshot
        persistedSnapshot!.ServiceName.Should().Be(persistedIncident!.ServiceId,
            "o identificador de serviço deve ser consistente entre RuntimeSnapshot e IncidentRecord");
    }
}

