using System.Linq;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Application.Governance.Features.GetIdeCapabilities;
using NexTraceOne.AIKnowledge.Application.Governance.Features.GetIdeSummary;
using NexTraceOne.AIKnowledge.Application.Governance.Features.RegisterIdeClient;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;
using NexTraceOne.BuildingBlocks.Application.Abstractions;

namespace NexTraceOne.AIKnowledge.Tests.Governance.Application.Features;

/// <summary>
/// Testes unitários para features de integração IDE:
/// GetIdeCapabilities, RegisterIdeClient e GetIdeSummary.
/// Valida resolução de capacidades por política, registo governado de clientes
/// e agregação de estado das integrações IDE activas.
/// </summary>
public sealed class IdeFeatureTests
{
    // ── GetIdeCapabilities ────────────────────────────────────────────────

    private readonly IAiIdeCapabilityPolicyRepository _capabilityPolicyRepo =
        Substitute.For<IAiIdeCapabilityPolicyRepository>();

    private readonly IAiIdeClientRegistrationRepository _clientRegistrationRepo =
        Substitute.For<IAiIdeClientRegistrationRepository>();

    [Fact]
    public async Task GetIdeCapabilities_WhenActivePolicyExists_ShouldReturnPolicyValues()
    {
        var policy = AIIDECapabilityPolicy.Create(
            AIClientType.VsCode,
            persona: "Engineer",
            allowedCommands: "Chat,ContractGenerate,ServiceLookup",
            allowedContextScopes: "services,contracts",
            allowContractGeneration: true,
            allowIncidentTroubleshooting: false,
            allowExternalAI: false,
            maxTokensPerRequest: 2048);

        _capabilityPolicyRepo
            .GetByClientTypeAndPersonaAsync(AIClientType.VsCode, "Engineer", Arg.Any<CancellationToken>())
            .Returns(policy);

        var handler = new GetIdeCapabilities.Handler(_capabilityPolicyRepo);
        var result = await handler.Handle(
            new GetIdeCapabilities.Query("VsCode", "Engineer"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsConfigured.Should().BeTrue();
        result.Value.ClientType.Should().Be("VsCode");
        result.Value.Persona.Should().Be("Engineer");
        result.Value.AllowedCommands.Should().Contain("Chat");
        result.Value.AllowedCommands.Should().Contain("ContractGenerate");
        result.Value.MaxTokensPerRequest.Should().Be(2048);
        result.Value.AllowContractGeneration.Should().BeTrue();
        result.Value.AllowIncidentTroubleshooting.Should().BeFalse();
    }

    [Fact]
    public async Task GetIdeCapabilities_WhenNoPolicyExists_ShouldReturnDefaults()
    {
        _capabilityPolicyRepo
            .GetByClientTypeAndPersonaAsync(Arg.Any<AIClientType>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns((AIIDECapabilityPolicy?)null);

        var handler = new GetIdeCapabilities.Handler(_capabilityPolicyRepo);
        var result = await handler.Handle(
            new GetIdeCapabilities.Query("VisualStudio", "TechLead"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsConfigured.Should().BeFalse();
        result.Value.AllowedCommands.Should().Contain("Chat");
        result.Value.AllowContractGeneration.Should().BeTrue();
        result.Value.MaxTokensPerRequest.Should().Be(4096);
    }

    [Fact]
    public async Task GetIdeCapabilities_WhenInvalidClientType_ShouldReturnFailure()
    {
        var handler = new GetIdeCapabilities.Handler(_capabilityPolicyRepo);
        var result = await handler.Handle(
            new GetIdeCapabilities.Query("Rider", null),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    // ── RegisterIdeClient ─────────────────────────────────────────────────

    [Fact]
    public async Task RegisterIdeClient_VsCode_ShouldPersistAndReturnRegistration()
    {
        AIIDEClientRegistration? persisted = null;
        _clientRegistrationRepo
            .AddAsync(Arg.Do<AIIDEClientRegistration>(r => persisted = r), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.Id.Returns("user-ide-test");
        currentUser.Name.Returns("IDE Test User");

        var handler = new RegisterIdeClient.Handler(_clientRegistrationRepo, currentUser);
        var result = await handler.Handle(
            new RegisterIdeClient.Command("VsCode", "1.5.0", "device-abc-123"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ClientType.Should().Be("VsCode");
        result.Value.IsActive.Should().BeTrue();
        result.Value.RegistrationId.Should().NotBe(Guid.Empty);
        persisted.Should().NotBeNull();
        persisted!.UserId.Should().Be("user-ide-test");
        persisted.ClientType.Should().Be(AIClientType.VsCode);
    }

    [Fact]
    public async Task RegisterIdeClient_VisualStudio_ShouldPersistCorrectClientType()
    {
        _clientRegistrationRepo
            .AddAsync(Arg.Any<AIIDEClientRegistration>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.Id.Returns("user-vs-test");
        currentUser.Name.Returns("VS Test User");

        var handler = new RegisterIdeClient.Handler(_clientRegistrationRepo, currentUser);
        var result = await handler.Handle(
            new RegisterIdeClient.Command("VisualStudio", "18.4.0", null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ClientType.Should().Be("VisualStudio");
    }

    [Fact]
    public async Task RegisterIdeClient_InvalidClientType_ShouldReturnFailure()
    {
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.Id.Returns("user-test");
        currentUser.Name.Returns("Test User");

        var handler = new RegisterIdeClient.Handler(_clientRegistrationRepo, currentUser);
        var result = await handler.Handle(
            new RegisterIdeClient.Command("Rider", null, null),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        await _clientRegistrationRepo.DidNotReceive()
            .AddAsync(Arg.Any<AIIDEClientRegistration>(), Arg.Any<CancellationToken>());
    }

    // ── GetIdeSummary ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetIdeSummary_WithNoClients_ShouldReturnReadyStatus()
    {
        _clientRegistrationRepo
            .ListAsync(Arg.Any<string?>(), AIClientType.VsCode, true, 1000, Arg.Any<CancellationToken>())
            .Returns([]);
        _clientRegistrationRepo
            .ListAsync(Arg.Any<string?>(), AIClientType.VisualStudio, true, 1000, Arg.Any<CancellationToken>())
            .Returns([]);
        _capabilityPolicyRepo
            .ListAsync(Arg.Any<AIClientType?>(), Arg.Any<bool?>(), 100, Arg.Any<CancellationToken>())
            .Returns([]);

        var handler = new GetIdeSummary.Handler(_clientRegistrationRepo, _capabilityPolicyRepo);
        var result = await handler.Handle(new GetIdeSummary.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalActiveClients.Should().Be(0);
        result.Value.TotalPolicies.Should().Be(0);
        result.Value.ClientTypes.Should().HaveCount(2);
        result.Value.ClientTypes.Should().AllSatisfy(ct => ct.Status.Should().Be("Ready"));
    }

    [Fact]
    public async Task GetIdeSummary_WithActiveVsCodeClients_ShouldReturnActiveStatus()
    {
        var registration = AIIDEClientRegistration.Register(
            "user-vs", "VS User", AIClientType.VsCode, "1.5.0", null);

        _clientRegistrationRepo
            .ListAsync(Arg.Any<string?>(), AIClientType.VsCode, true, 1000, Arg.Any<CancellationToken>())
            .Returns([registration]);
        _clientRegistrationRepo
            .ListAsync(Arg.Any<string?>(), AIClientType.VisualStudio, true, 1000, Arg.Any<CancellationToken>())
            .Returns([]);
        _capabilityPolicyRepo
            .ListAsync(Arg.Any<AIClientType?>(), Arg.Any<bool?>(), 100, Arg.Any<CancellationToken>())
            .Returns([]);

        var handler = new GetIdeSummary.Handler(_clientRegistrationRepo, _capabilityPolicyRepo);
        var result = await handler.Handle(new GetIdeSummary.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalActiveClients.Should().Be(1);
        var vsCode = result.Value.ClientTypes.First(ct => ct.ClientType == "VsCode");
        vsCode.Status.Should().Be("Active");
        vsCode.ActiveClients.Should().Be(1);
    }

    [Fact]
    public async Task GetIdeSummary_WithActivePolicies_ShouldReflectInSummary()
    {
        var policy = AIIDECapabilityPolicy.Create(
            AIClientType.VsCode, null,
            "Chat", "services", true, true, false, 4096);

        _clientRegistrationRepo
            .ListAsync(Arg.Any<string?>(), AIClientType.VsCode, true, 1000, Arg.Any<CancellationToken>())
            .Returns([]);
        _clientRegistrationRepo
            .ListAsync(Arg.Any<string?>(), AIClientType.VisualStudio, true, 1000, Arg.Any<CancellationToken>())
            .Returns([]);
        _capabilityPolicyRepo
            .ListAsync(Arg.Any<AIClientType?>(), Arg.Any<bool?>(), 100, Arg.Any<CancellationToken>())
            .Returns([policy]);

        var handler = new GetIdeSummary.Handler(_clientRegistrationRepo, _capabilityPolicyRepo);
        var result = await handler.Handle(new GetIdeSummary.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalPolicies.Should().Be(1);
        result.Value.ActivePolicies.Should().Be(1);
        var vsCode = result.Value.ClientTypes.First(ct => ct.ClientType == "VsCode");
        vsCode.HasCapabilityPolicy.Should().BeTrue();
    }
}
