using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;

namespace NexTraceOne.AIKnowledge.Tests.Governance.Domain.Entities;

/// <summary>
/// Testes unitários das entidades de IDE Integration do AI Governance.
/// Cobre AIIDEClientRegistration e AIIDECapabilityPolicy — Fase 4.
/// </summary>
public sealed class AiIdeEntityTests
{
    // ── AIIDEClientRegistration ─────────────────────────────────────────

    [Fact]
    public void IdeClient_Register_ShouldSetProperties()
    {
        var client = AIIDEClientRegistration.Register(
            "user-001", "Alice Engineer", AIClientType.VsCode,
            "1.90.0", "ALICE-LAPTOP");

        client.UserId.Should().Be("user-001");
        client.UserDisplayName.Should().Be("Alice Engineer");
        client.ClientType.Should().Be(AIClientType.VsCode);
        client.ClientVersion.Should().Be("1.90.0");
        client.DeviceIdentifier.Should().Be("ALICE-LAPTOP");
        client.IsActive.Should().BeTrue();
    }

    [Fact]
    public void IdeClient_Register_VisualStudio_ShouldSetClientType()
    {
        var client = AIIDEClientRegistration.Register(
            "user-002", "Bob Architect", AIClientType.VisualStudio,
            "17.10", null);

        client.ClientType.Should().Be(AIClientType.VisualStudio);
        client.DeviceIdentifier.Should().BeNull();
    }

    [Fact]
    public void IdeClient_RecordAccess_ShouldUpdateTimestampAndVersion()
    {
        var client = AIIDEClientRegistration.Register(
            "user-001", "Alice", AIClientType.VsCode, "1.89.0", "DEV-01");
        var accessAt = DateTimeOffset.UtcNow;

        var result = client.RecordAccess(accessAt, "1.90.0");

        result.IsSuccess.Should().BeTrue();
        client.LastAccessAt.Should().Be(accessAt);
        client.ClientVersion.Should().Be("1.90.0");
    }

    [Fact]
    public void IdeClient_Revoke_ShouldDeactivateWithReason()
    {
        var client = AIIDEClientRegistration.Register(
            "user-001", "Alice", AIClientType.VsCode, "1.90.0", "DEV-01");

        var result = client.Revoke("Security incident");

        result.IsSuccess.Should().BeTrue();
        client.IsActive.Should().BeFalse();
    }

    [Fact]
    public void IdeClient_Reactivate_AfterRevocation_ShouldSetActive()
    {
        var client = AIIDEClientRegistration.Register(
            "user-001", "Alice", AIClientType.VsCode, "1.90.0", "DEV-01");
        client.Revoke("Temporary revocation");

        var result = client.Reactivate();

        result.IsSuccess.Should().BeTrue();
        client.IsActive.Should().BeTrue();
    }

    // ── AIIDECapabilityPolicy ───────────────────────────────────────────

    [Fact]
    public void IdePolicy_Create_ShouldSetProperties()
    {
        var policy = AIIDECapabilityPolicy.Create(
            AIClientType.VsCode,
            "Engineer",
            "Chat,ServiceLookup,ContractLookup",
            "services,contracts",
            allowContractGeneration: true,
            allowIncidentTroubleshooting: false,
            allowExternalAI: false,
            maxTokensPerRequest: 4000);

        policy.ClientType.Should().Be(AIClientType.VsCode);
        policy.Persona.Should().Be("Engineer");
        policy.AllowedCommands.Should().Be("Chat,ServiceLookup,ContractLookup");
        policy.AllowedContextScopes.Should().Be("services,contracts");
        policy.AllowContractGeneration.Should().BeTrue();
        policy.AllowIncidentTroubleshooting.Should().BeFalse();
        policy.AllowExternalAI.Should().BeFalse();
        policy.MaxTokensPerRequest.Should().Be(4000);
        policy.IsActive.Should().BeTrue();
    }

    [Fact]
    public void IdePolicy_Create_WithNullPersona_ShouldApplyToAll()
    {
        var policy = AIIDECapabilityPolicy.Create(
            AIClientType.VisualStudio, null,
            "Chat", "services",
            false, false, false, 2000);

        policy.Persona.Should().BeNull();
        policy.IsActive.Should().BeTrue();
    }

    [Fact]
    public void IdePolicy_Update_ShouldModifyProperties()
    {
        var policy = AIIDECapabilityPolicy.Create(
            AIClientType.VsCode, "Engineer",
            "Chat", "services",
            false, false, false, 2000);

        var result = policy.Update(
            "Chat,ContractGenerate,IncidentLookup",
            "services,contracts,incidents",
            allowContractGeneration: true,
            allowIncidentTroubleshooting: true,
            allowExternalAI: true,
            maxTokensPerRequest: 8000);

        result.IsSuccess.Should().BeTrue();
        policy.AllowedCommands.Should().Be("Chat,ContractGenerate,IncidentLookup");
        policy.AllowedContextScopes.Should().Be("services,contracts,incidents");
        policy.AllowContractGeneration.Should().BeTrue();
        policy.AllowIncidentTroubleshooting.Should().BeTrue();
        policy.AllowExternalAI.Should().BeTrue();
        policy.MaxTokensPerRequest.Should().Be(8000);
    }

    [Fact]
    public void IdePolicy_Deactivate_ShouldSetInactive()
    {
        var policy = AIIDECapabilityPolicy.Create(
            AIClientType.VsCode, null,
            "Chat", "services",
            false, false, false, 2000);

        policy.Deactivate();

        policy.IsActive.Should().BeFalse();
    }

    [Fact]
    public void IdePolicy_Activate_AfterDeactivation_ShouldSetActive()
    {
        var policy = AIIDECapabilityPolicy.Create(
            AIClientType.VsCode, null,
            "Chat", "services",
            false, false, false, 2000);
        policy.Deactivate();

        policy.Activate();

        policy.IsActive.Should().BeTrue();
    }

    [Fact]
    public void IdePolicy_SetAllowedModels_ShouldUpdateModelIds()
    {
        var policy = AIIDECapabilityPolicy.Create(
            AIClientType.VsCode, null,
            "Chat", "services",
            false, false, false, 2000);

        var result = policy.SetAllowedModels("model-001,model-002");

        result.IsSuccess.Should().BeTrue();
        policy.AllowedModelIds.Should().Be("model-001,model-002");
    }
}
