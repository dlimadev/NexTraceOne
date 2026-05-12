using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NexTraceOne.AIKnowledge.Domain.ExternalAI.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;
using NexTraceOne.AIKnowledge.Domain.Orchestration.Entities;
using NexTraceOne.IntegrationTests.Infrastructure;
using Xunit;

namespace NexTraceOne.IntegrationTests.CriticalFlows;

/// <summary>
/// Testes de integração para os DbContexts do módulo AIKnowledge contra PostgreSQL real.
/// Cobre AiGovernanceDbContext, ExternalAiDbContext e AiOrchestrationDbContext.
/// </summary>
[Collection(PostgreSqlIntegrationCollection.Name)]
public sealed class AiGovernancePostgreSqlTests(PostgreSqlIntegrationFixture fixture) : IntegrationTestBase(fixture)
{
    // ── Migrations coverage ──────────────────────────────────────────────────

    [RequiresDockerFact]
    public async Task AiKnowledge_AllDbContexts_Should_Have_AppliedMigrations()
    {
        var aiGovernanceMigrations = await Fixture.GetAppliedMigrationsCountAsync(Fixture.AiKnowledgeConnectionString);
        var externalAiMigrations = await Fixture.GetAppliedMigrationsCountAsync(Fixture.ExternalAiConnectionString);
        var aiOrchMigrations = await Fixture.GetAppliedMigrationsCountAsync(Fixture.AiOrchestrationConnectionString);

        aiGovernanceMigrations.Should().BeGreaterThan(0, "AiGovernanceDbContext deve ter migrations no banco aiknowledge");
        externalAiMigrations.Should().BeGreaterThan(0, "ExternalAiDbContext deve ter migrations no banco externalai");
        aiOrchMigrations.Should().BeGreaterThan(0, "AiOrchestrationDbContext deve ter migrations no banco aiorchestration");
    }

    [RequiresDockerFact]
    public async Task AiKnowledge_Tables_Should_Exist_After_Migrations()
    {
        var conversationsExist = await Fixture.TableExistsAsync(Fixture.AiKnowledgeConnectionString, "ai_gov_conversations");
        var modelsExist = await Fixture.TableExistsAsync(Fixture.AiKnowledgeConnectionString, "ai_gov_models");
        var providersExist = await Fixture.TableExistsAsync(Fixture.ExternalAiConnectionString, "ext_ai_providers");

        conversationsExist.Should().BeTrue("ai_gov_conversations deve ser criada pela migration AiGovernance");
        modelsExist.Should().BeTrue("ai_gov_models deve ser criada pela migration AiGovernance");
        providersExist.Should().BeTrue("ext_ai_providers deve ser criada pela migration ExternalAi no banco externalai");
    }

    // ── AiGovernanceDbContext ────────────────────────────────────────────────

    [RequiresDockerFact]
    public async Task AiGovernance_Should_Persist_Conversation_And_Messages()
    {
        await ResetStateAsync();

        await using var context = Fixture.CreateAiGovernanceDbContext();

        var conversation = AiAssistantConversation.Start(
            title: "Investigação de Incidente — Orders API",
            persona: "Engineer",
            clientType: AIClientType.Web,
            defaultContextScope: "services,incidents,changes",
            createdBy: "engineer@nextraceone.io",
            serviceId: Guid.NewGuid());

        context.Conversations.Add(conversation);
        await context.SaveChangesAsync();

        var now = DateTimeOffset.UtcNow;

        var userMsg = AiMessage.UserMessage(
            conversationId: conversation.Id.Value,
            content: "Qual é a causa provável do aumento de latência no Orders API após o último deploy?",
            timestamp: now);

        var assistantMsg = AiMessage.AssistantMessage(
            conversationId: conversation.Id.Value,
            content: "Com base na correlação temporal entre o deploy v2.1.0 e o aumento de latência p99, a causa mais provável é...",
            modelName: "deepseek-r1:1.5b",
            provider: "Internal",
            isInternal: true,
            promptTokens: 245,
            completionTokens: 312,
            appliedPolicyName: "engineer-standard",
            groundingSources: "incidents,services,changes",
            contextReferences: "INC-2026-001,orders-service",
            correlationId: Guid.NewGuid().ToString(),
            timestamp: now.AddSeconds(3));

        conversation.RecordMessage("deepseek-r1:1.5b", now.AddSeconds(3));

        context.Messages.Add(userMsg);
        context.Messages.Add(assistantMsg);
        await context.SaveChangesAsync();

        var persistedConversation = await context.Conversations
            .AsNoTracking()
            .SingleAsync(c => c.Id == conversation.Id);

        persistedConversation.Title.Should().Be("Investigação de Incidente — Orders API");
        persistedConversation.Persona.Should().Be("Engineer");
        persistedConversation.ClientType.Should().Be(AIClientType.Web);
        persistedConversation.IsActive.Should().BeTrue();
        persistedConversation.MessageCount.Should().Be(1);
        persistedConversation.LastModelUsed.Should().Be("deepseek-r1:1.5b");

        var messages = await context.Messages
            .AsNoTracking()
            .Where(m => m.ConversationId == conversation.Id.Value)
            .OrderBy(m => m.Timestamp)
            .ToListAsync();

        messages.Should().HaveCount(2);
        messages[0].Role.Should().Be("user");
        messages[1].Role.Should().Be("assistant");
        messages[1].Provider.Should().Be("Internal");
        messages[1].IsInternalModel.Should().BeTrue();
        messages[1].PromptTokens.Should().Be(245);
        messages[1].CompletionTokens.Should().Be(312);
    }

    [RequiresDockerFact]
    public async Task AiGovernance_Should_Persist_Multiple_Conversations_And_Filter_By_CreatedBy()
    {
        await ResetStateAsync();

        await using var context = Fixture.CreateAiGovernanceDbContext();

        var eng1Conv = AiAssistantConversation.Start(
            title: "Debug Orders Latency",
            persona: "Engineer",
            clientType: AIClientType.Web,
            defaultContextScope: "services",
            createdBy: "eng.one@nextraceone.io");

        var eng2Conv = AiAssistantConversation.Start(
            title: "Contract Review Payments API",
            persona: "TechLead",
            clientType: AIClientType.VsCode,
            defaultContextScope: "contracts",
            createdBy: "tech.lead@nextraceone.io");

        var eng2Conv2 = AiAssistantConversation.Start(
            title: "Change Impact Analysis",
            persona: "TechLead",
            clientType: AIClientType.Web,
            defaultContextScope: "changes,services",
            createdBy: "tech.lead@nextraceone.io");

        context.Conversations.AddRange(eng1Conv, eng2Conv, eng2Conv2);
        await context.SaveChangesAsync();

        var techLeadConversations = await context.Conversations
            .AsNoTracking()
            .Where(c => c.CreatedBy == "tech.lead@nextraceone.io" && c.IsActive)
            .OrderBy(c => c.Title)
            .ToListAsync();

        techLeadConversations.Should().HaveCount(2);
        techLeadConversations[0].Title.Should().Be("Change Impact Analysis");
        techLeadConversations[1].Title.Should().Be("Contract Review Payments API");
        techLeadConversations[1].ClientType.Should().Be(AIClientType.VsCode);
    }

    // ── ExternalAiDbContext ──────────────────────────────────────────────────

    [RequiresDockerFact]
    public async Task ExternalAi_Should_Persist_Provider_And_Query_ActiveProviders()
    {
        await ResetStateAsync();

        await using var context = Fixture.CreateExternalAiDbContext();

        var primaryProvider = ExternalAiProvider.Register(
            name: "OpenAI GPT-4o",
            endpoint: "https://api.openai.com/v1/chat/completions",
            modelName: "gpt-4o",
            maxTokensPerRequest: 8192,
            costPerToken: 0.00003m,
            priority: 1,
            registeredAt: DateTimeOffset.UtcNow);

        var fallbackProvider = ExternalAiProvider.Register(
            name: "Azure OpenAI GPT-4",
            endpoint: "https://myazure.openai.azure.com/openai/deployments/gpt-4/chat/completions",
            modelName: "gpt-4",
            maxTokensPerRequest: 4096,
            costPerToken: 0.00006m,
            priority: 2,
            registeredAt: DateTimeOffset.UtcNow);

        fallbackProvider.Deactivate(); // Returns Result<Unit> — side effect on entity state is what matters

        context.Providers.AddRange(primaryProvider, fallbackProvider);
        await context.SaveChangesAsync();

        var activeProviders = await context.Providers
            .AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.Priority)
            .ToListAsync();

        activeProviders.Should().HaveCount(1);
        activeProviders[0].Name.Should().Be("OpenAI GPT-4o");
        activeProviders[0].ModelName.Should().Be("gpt-4o");
        activeProviders[0].Priority.Should().Be(1);
        activeProviders[0].MaxTokensPerRequest.Should().Be(8192);
        activeProviders[0].CostPerToken.Should().Be(0.00003m);
    }

    // ── AiOrchestrationDbContext ─────────────────────────────────────────────

    [RequiresDockerFact]
    public async Task AiOrchestration_Should_Persist_Conversation_And_Query_ByServiceName()
    {
        await ResetStateAsync();

        await using var context = Fixture.CreateAiOrchestrationDbContext();

        var releaseId = Guid.NewGuid();

        var conversation = AiConversation.Start(
            serviceName: "orders-service",
            topic: "Release impact analysis after v2.1.0 deployment",
            startedBy: "engineer@nextraceone.io",
            startedAt: DateTimeOffset.UtcNow,
            releaseId: releaseId);

        context.Conversations.Add(conversation);
        await context.SaveChangesAsync();

        var serviceConversations = await context.Conversations
            .AsNoTracking()
            .Where(c => c.ServiceName == "orders-service")
            .ToListAsync();

        serviceConversations.Should().HaveCount(1);
        serviceConversations[0].Topic.Should().Be("Release impact analysis after v2.1.0 deployment");
        serviceConversations[0].StartedBy.Should().Be("engineer@nextraceone.io");
        serviceConversations[0].ReleaseId.Should().Be(releaseId);
        serviceConversations[0].TurnCount.Should().Be(0);
    }

    // ── Cross-context: isolated databases ────────────────────────────────────

    [RequiresDockerFact]
    public async Task AiKnowledge_AllThreeContexts_Have_Isolated_Databases_And_Coexist()
    {
        await ResetStateAsync();

        var aiGovCtx = Fixture.CreateAiGovernanceDbContext();
        var extAiCtx = Fixture.CreateExternalAiDbContext();
        var orchCtx = Fixture.CreateAiOrchestrationDbContext();

        await using (aiGovCtx)
        await using (extAiCtx)
        await using (orchCtx)
        {
            var conversation = AiAssistantConversation.Start(
                "Shared DB test conversation", "Engineer", AIClientType.Web, "services", "user@test.io");

            var provider = ExternalAiProvider.Register(
                "Test Provider", "https://api.test.io", "test-model", 1024, 0.01m, 10, DateTimeOffset.UtcNow);

            var orchConv = AiConversation.Start(
                serviceName: "test-service",
                topic: "Orchestration test topic",
                startedBy: "architect@test.io",
                startedAt: DateTimeOffset.UtcNow);

            aiGovCtx.Conversations.Add(conversation);
            extAiCtx.Providers.Add(provider);
            orchCtx.Conversations.Add(orchConv);

            await aiGovCtx.SaveChangesAsync();
            await extAiCtx.SaveChangesAsync();
            await orchCtx.SaveChangesAsync();
        }

        // Each context has its own isolated database — verify each has migrations applied
        var aiGovMigrations = await Fixture.GetAppliedMigrationsCountAsync(Fixture.AiKnowledgeConnectionString);
        var extAiMigrations = await Fixture.GetAppliedMigrationsCountAsync(Fixture.ExternalAiConnectionString);
        var orchMigrations = await Fixture.GetAppliedMigrationsCountAsync(Fixture.AiOrchestrationConnectionString);

        aiGovMigrations.Should().BeGreaterThan(0, "AiGovernanceDbContext deve ter migrations aplicadas na sua base isolada");
        extAiMigrations.Should().BeGreaterThan(0, "ExternalAiDbContext deve ter migrations aplicadas na sua base isolada");
        orchMigrations.Should().BeGreaterThan(0, "AiOrchestrationDbContext deve ter migrations aplicadas na sua base isolada");
    }
}
