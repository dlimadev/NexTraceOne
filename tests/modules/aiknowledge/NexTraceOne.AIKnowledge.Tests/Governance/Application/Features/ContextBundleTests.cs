using System.Text.Json;

using NexTraceOne.AIKnowledge.Application.Governance.Features.SendAssistantMessage;

namespace NexTraceOne.AIKnowledge.Tests.Governance.Application.Features;

/// <summary>
/// Testes unitários para validação dos context bundles do AI Assistant.
/// Garante que bundles de contexto são corretamente criados, serializados e desserializados
/// para cada tipo de entidade: service, contract, change e incident.
/// </summary>
public sealed class ContextBundleTests
{
    // ── ContextBundleData ────────────────────────────────────────────────

    [Fact]
    public void ContextBundleData_ShouldRoundTripSerialize_ForService()
    {
        var bundle = new SendAssistantMessage.ContextBundleData(
            EntityType: "service",
            EntityName: "payment-service",
            EntityStatus: "Active",
            EntityDescription: "Payment processing service",
            Properties: new Dictionary<string, string>
            {
                ["team"] = "payments-team",
                ["domain"] = "finance",
                ["criticality"] = "Critical",
                ["serviceType"] = "REST API",
            },
            Relations: [
                new SendAssistantMessage.ContextBundleRelation(
                    "Contracts", "contract", "payment-api-v2", "Active",
                    new Dictionary<string, string> { ["protocol"] = "OpenApi", ["version"] = "2.1.0" }),
            ],
            Caveats: null);

        var json = JsonSerializer.Serialize(bundle);
        var deserialized = JsonSerializer.Deserialize<SendAssistantMessage.ContextBundleData>(
            json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        deserialized.Should().NotBeNull();
        deserialized!.EntityType.Should().Be("service");
        deserialized.EntityName.Should().Be("payment-service");
        deserialized.EntityStatus.Should().Be("Active");
        deserialized.Properties.Should().HaveCount(4);
        deserialized.Properties!["team"].Should().Be("payments-team");
        deserialized.Relations.Should().HaveCount(1);
        deserialized.Relations![0].Name.Should().Be("payment-api-v2");
        deserialized.Relations[0].Properties!["protocol"].Should().Be("OpenApi");
    }

    [Fact]
    public void ContextBundleData_ShouldRoundTripSerialize_ForContract()
    {
        var bundle = new SendAssistantMessage.ContextBundleData(
            EntityType: "contract",
            EntityName: "order-api — OpenApi",
            EntityStatus: "Active",
            EntityDescription: "Order management API contract",
            Properties: new Dictionary<string, string>
            {
                ["protocol"] = "OpenApi",
                ["version"] = "3.2.0",
                ["service"] = "order-service",
                ["format"] = "yaml",
            },
            Relations: [
                new SendAssistantMessage.ContextBundleRelation(
                    "Version History", "version", "v3.1.0", "Active",
                    new Dictionary<string, string> { ["created"] = "2026-01-15" }),
                new SendAssistantMessage.ContextBundleRelation(
                    "Violations", "violation", "must-have-description", "Warning",
                    new Dictionary<string, string> { ["message"] = "Operations should have descriptions" }),
            ],
            Caveats: null);

        var json = JsonSerializer.Serialize(bundle);
        var deserialized = JsonSerializer.Deserialize<SendAssistantMessage.ContextBundleData>(
            json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        deserialized.Should().NotBeNull();
        deserialized!.EntityType.Should().Be("contract");
        deserialized.Relations.Should().HaveCount(2);
        deserialized.Relations![0].RelationType.Should().Be("Version History");
        deserialized.Relations[1].RelationType.Should().Be("Violations");
    }

    [Fact]
    public void ContextBundleData_ShouldRoundTripSerialize_ForChange()
    {
        var bundle = new SendAssistantMessage.ContextBundleData(
            EntityType: "change",
            EntityName: "order-service — v3.2.0",
            EntityStatus: "PendingReview",
            EntityDescription: "Major version upgrade with new payment flow",
            Properties: new Dictionary<string, string>
            {
                ["changeType"] = "Major",
                ["environment"] = "Production",
                ["team"] = "order-team",
                ["score"] = "72",
                ["advisory"] = "ApproveWithConditions",
                ["confidence"] = "Medium",
            },
            Relations: [
                new SendAssistantMessage.ContextBundleRelation(
                    "Advisory Factors", "factor", "UnitTestCoverage", "Warning",
                    new Dictionary<string, string> { ["description"] = "Coverage below threshold" }),
                new SendAssistantMessage.ContextBundleRelation(
                    "Decision History", "decision", "Submitted for review", "Submitted",
                    new Dictionary<string, string> { ["source"] = "CI Pipeline" }),
            ],
            Caveats: ["No decision history"]);

        var json = JsonSerializer.Serialize(bundle);
        var deserialized = JsonSerializer.Deserialize<SendAssistantMessage.ContextBundleData>(
            json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        deserialized.Should().NotBeNull();
        deserialized!.EntityType.Should().Be("change");
        deserialized.Caveats.Should().Contain("No decision history");
        deserialized.Properties!["advisory"].Should().Be("ApproveWithConditions");
    }

    [Fact]
    public void ContextBundleData_ShouldRoundTripSerialize_ForIncident()
    {
        var bundle = new SendAssistantMessage.ContextBundleData(
            EntityType: "incident",
            EntityName: "INC-2847 — Payment latency spike",
            EntityStatus: "Investigating",
            EntityDescription: "Sudden increase in P99 latency for payment endpoints",
            Properties: new Dictionary<string, string>
            {
                ["severity"] = "High",
                ["team"] = "payments-team",
                ["domain"] = "finance",
                ["mitigationStatus"] = "InProgress",
                ["correlationConfidence"] = "High",
                ["correlationReason"] = "Temporal proximity to deployment",
            },
            Relations: [
                new SendAssistantMessage.ContextBundleRelation(
                    "Affected Services", "service", "payment-service", "Critical",
                    new Dictionary<string, string> { ["type"] = "REST API" }),
                new SendAssistantMessage.ContextBundleRelation(
                    "Runbooks", "runbook", "Payment Latency Recovery", null,
                    new Dictionary<string, string> { ["url"] = "https://wiki/runbooks/payment-latency" }),
                new SendAssistantMessage.ContextBundleRelation(
                    "Correlated Changes", "change", "deploy-v3.2.0", null, null),
            ],
            Caveats: null);

        var json = JsonSerializer.Serialize(bundle);
        var deserialized = JsonSerializer.Deserialize<SendAssistantMessage.ContextBundleData>(
            json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        deserialized.Should().NotBeNull();
        deserialized!.EntityType.Should().Be("incident");
        deserialized.Relations.Should().HaveCount(3);
        deserialized.Relations![0].RelationType.Should().Be("Affected Services");
        deserialized.Relations[1].RelationType.Should().Be("Runbooks");
        deserialized.Relations[2].RelationType.Should().Be("Correlated Changes");
        deserialized.Properties!["severity"].Should().Be("High");
    }

    // ── Response metadata shape ────────────────────────────────────────────

    [Fact]
    public void Response_ShouldSupportContextStrengthAndCaveats()
    {
        var response = new SendAssistantMessage.Response(
            ConversationId: Guid.NewGuid(),
            MessageId: Guid.NewGuid(),
            AssistantResponse: "Grounded analysis of service...",
            ModelUsed: "NexTrace-Internal-v1",
            Provider: "Internal",
            IsInternalModel: true,
            PromptTokens: 0,
            CompletionTokens: 0,
            AppliedPolicy: null,
            GroundingSources: ["Service Catalog", "Contract Registry"],
            ContextReferences: ["service:payment-service", "contract:payment-api-v2"],
            CorrelationId: "ctx-123",
            UseCaseType: "ServiceLookup",
            RoutingPath: "InternalOnly",
            ConfidenceLevel: "High",
            CostClass: "low",
            RoutingRationale: "Internal routing for service lookup",
            SourceWeightingSummary: "ServiceCatalog:40%,ContractRegistry:25%",
            EscalationReason: "None",
            ContextSummary: "Consulted: service (payment-service) with 4 properties, 1 related entities",
            SuggestedNextSteps: ["Review associated contracts", "Check dependency health"],
            ContextCaveats: null,
            ContextStrength: "strong");

        response.ContextSummary.Should().Contain("payment-service");
        response.ContextStrength.Should().Be("strong");
        response.SuggestedNextSteps.Should().HaveCount(2);
        response.ContextCaveats.Should().BeNull();
        response.ConfidenceLevel.Should().Be("High");
    }

    [Fact]
    public void Response_ShouldSupportWeakContextWithCaveats()
    {
        var response = new SendAssistantMessage.Response(
            ConversationId: Guid.NewGuid(),
            MessageId: Guid.NewGuid(),
            AssistantResponse: "Limited context available...",
            ModelUsed: "NexTrace-Internal-v1",
            Provider: "Internal",
            IsInternalModel: true,
            PromptTokens: 0,
            CompletionTokens: 0,
            AppliedPolicy: null,
            GroundingSources: ["Service Catalog"],
            ContextReferences: [],
            CorrelationId: "ctx-456",
            UseCaseType: "General",
            RoutingPath: "InternalOnly",
            ConfidenceLevel: "Low",
            CostClass: "low",
            RoutingRationale: "Internal routing, limited context",
            SourceWeightingSummary: "no-matches",
            EscalationReason: "None",
            ContextSummary: null,
            SuggestedNextSteps: null,
            ContextCaveats: ["Limited cross-entity context available"],
            ContextStrength: "none");

        response.ContextStrength.Should().Be("none");
        response.ContextCaveats.Should().Contain("Limited cross-entity context available");
        response.SuggestedNextSteps.Should().BeNull();
    }

    // ── ContextBundleRelation ───────────────────────────────────────────────

    [Fact]
    public void ContextBundleRelation_ShouldAllowNullProperties()
    {
        var relation = new SendAssistantMessage.ContextBundleRelation(
            "Correlated Changes", "change", "deploy-v3.2.0", null, null);

        relation.RelationType.Should().Be("Correlated Changes");
        relation.EntityType.Should().Be("change");
        relation.Name.Should().Be("deploy-v3.2.0");
        relation.Status.Should().BeNull();
        relation.Properties.Should().BeNull();
    }

    // ── Command with ContextBundle ──────────────────────────────────────────

    [Fact]
    public void Command_ShouldAcceptNullContextBundle()
    {
        var cmd = new SendAssistantMessage.Command(
            ConversationId: null,
            Message: "Tell me about this service",
            ContextScope: "Services",
            Persona: "Engineer",
            PreferredModelId: null,
            ClientType: "Web",
            ServiceId: Guid.NewGuid(),
            ContractId: null,
            IncidentId: null,
            ChangeId: null,
            TeamId: null,
            DomainId: null,
            ContextBundle: null);

        cmd.ContextBundle.Should().BeNull();
        cmd.Message.Should().Be("Tell me about this service");
    }

    [Fact]
    public void Command_ShouldAcceptContextBundleJson()
    {
        var bundle = new SendAssistantMessage.ContextBundleData(
            "service", "payment-service", "Active", "Payment processing",
            new Dictionary<string, string> { ["team"] = "payments" },
            [], null);

        var json = JsonSerializer.Serialize(bundle);

        var cmd = new SendAssistantMessage.Command(
            ConversationId: null,
            Message: "What contracts does this service have?",
            ContextScope: "Services",
            Persona: "Engineer",
            PreferredModelId: null,
            ClientType: "Web",
            ServiceId: Guid.NewGuid(),
            ContractId: null,
            IncidentId: null,
            ChangeId: null,
            TeamId: null,
            DomainId: null,
            ContextBundle: json);

        cmd.ContextBundle.Should().NotBeNull();
        cmd.ContextBundle.Should().Contain("payment-service");
    }
}
