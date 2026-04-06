using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Application.Features.RunPreCommitGovernanceCheck;
using NexTraceOne.Governance.Domain.Entities;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Tests.Application.Features;

/// <summary>
/// Testes de unidade Phase 7.6 — RunPreCommitGovernanceCheck.
/// Cobre naming conventions, API versioning, ownership e error-handling rules.
/// </summary>
public sealed class RunPreCommitGovernanceCheckTests
{
    private readonly IPolicyAsCodeRepository _policyRepo = Substitute.For<IPolicyAsCodeRepository>();

    private static PolicyAsCodeDefinition BuildActivePolicy(
        string name, PolicyEnforcementMode mode = PolicyEnforcementMode.Advisory)
    {
        return PolicyAsCodeDefinition.Create(
            tenantId: Guid.NewGuid(),
            name: name,
            displayName: name,
            description: null,
            version: "1.0",
            format: PolicyDefinitionFormat.Json,
            definitionContent: "{ }",
            enforcementMode: mode,
            registeredBy: "test");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Happy path — sem violações
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ValidService_ReturnsNoViolations()
    {
        _policyRepo.ListAsync(Arg.Any<PolicyDefinitionStatus?>(), Arg.Any<PolicyEnforcementMode?>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var handler = new RunPreCommitGovernanceCheck.Handler(_policyRepo);
        var command = new RunPreCommitGovernanceCheck.Command(
            ServiceName: "payment-api",
            Domain: "payments",
            TechnicalOwner: "alice@company.com",
            RepositoryUrl: "https://github.com/org/payment-api",
            ExposedApiPaths: ["/api/v1/payments", "/api/v1/payments/{id}"],
            PolicyNamesToEnforce: []);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Blocked.Should().BeFalse();
        result.Value.ViolationCount.Should().Be(0);
        result.Value.Summary.Should().Contain("passed");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Naming conventions
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_UpperCaseServiceName_ProducesNamingViolation()
    {
        _policyRepo.ListAsync(Arg.Any<PolicyDefinitionStatus?>(), Arg.Any<PolicyEnforcementMode?>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var handler = new RunPreCommitGovernanceCheck.Handler(_policyRepo);
        var command = new RunPreCommitGovernanceCheck.Command(
            ServiceName: "PaymentAPI",
            Domain: "payments",
            TechnicalOwner: "alice",
            RepositoryUrl: "https://github.com/org/repo",
            ExposedApiPaths: ["/api/v1/payments"],
            PolicyNamesToEnforce: []);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Violations.Should().Contain(v => v.RuleId == "NAMING-001");
    }

    [Fact]
    public async Task Handle_NamingViolation_WithHardEnforce_Blocks()
    {
        var namingPolicy = BuildActivePolicy("naming-convention", PolicyEnforcementMode.HardEnforce);
        _policyRepo.ListAsync(Arg.Any<PolicyDefinitionStatus?>(), Arg.Any<PolicyEnforcementMode?>(), Arg.Any<CancellationToken>())
            .Returns([namingPolicy]);

        var handler = new RunPreCommitGovernanceCheck.Handler(_policyRepo);
        var command = new RunPreCommitGovernanceCheck.Command(
            ServiceName: "BAD_NAME",
            Domain: "payments",
            TechnicalOwner: "alice",
            RepositoryUrl: "https://github.com/org/repo",
            ExposedApiPaths: ["/api/v1/resource"],
            PolicyNamesToEnforce: ["naming-convention"]);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Blocked.Should().BeTrue();
        result.Value.Violations.Should().Contain(v => v.Severity == RunPreCommitGovernanceCheck.ViolationSeverity.Error);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // API versioning
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_PathWithoutVersionSegment_ProducesVersionViolation()
    {
        _policyRepo.ListAsync(Arg.Any<PolicyDefinitionStatus?>(), Arg.Any<PolicyEnforcementMode?>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var handler = new RunPreCommitGovernanceCheck.Handler(_policyRepo);
        var command = new RunPreCommitGovernanceCheck.Command(
            ServiceName: "order-service",
            Domain: "orders",
            TechnicalOwner: "bob",
            RepositoryUrl: "https://github.com/org/repo",
            ExposedApiPaths: ["/orders", "/orders/{id}"],
            PolicyNamesToEnforce: []);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Violations.Should().Contain(v => v.RuleId == "VERSION-001");
    }

    [Fact]
    public async Task Handle_PathWithVersionSegment_NoVersionViolation()
    {
        _policyRepo.ListAsync(Arg.Any<PolicyDefinitionStatus?>(), Arg.Any<PolicyEnforcementMode?>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var handler = new RunPreCommitGovernanceCheck.Handler(_policyRepo);
        var command = new RunPreCommitGovernanceCheck.Command(
            ServiceName: "order-service",
            Domain: "orders",
            TechnicalOwner: "bob",
            RepositoryUrl: "https://github.com/org/repo",
            ExposedApiPaths: ["/api/v1/orders", "/api/v2/orders"],
            PolicyNamesToEnforce: []);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Violations.Should().NotContain(v => v.RuleId == "VERSION-001");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Ownership
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_MissingTechnicalOwner_ProducesOwnershipViolation()
    {
        _policyRepo.ListAsync(Arg.Any<PolicyDefinitionStatus?>(), Arg.Any<PolicyEnforcementMode?>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var handler = new RunPreCommitGovernanceCheck.Handler(_policyRepo);
        var command = new RunPreCommitGovernanceCheck.Command(
            ServiceName: "catalog-service",
            Domain: "catalog",
            TechnicalOwner: null,
            RepositoryUrl: null,
            ExposedApiPaths: ["/api/v1/catalog"],
            PolicyNamesToEnforce: []);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Violations.Should().Contain(v => v.RuleId == "OWNERSHIP-001");
        result.Value.Violations.Should().Contain(v => v.RuleId == "OWNERSHIP-002");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Auto-fix suggestions
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ViolationsWithAutoFix_ReturnsSuggestions()
    {
        _policyRepo.ListAsync(Arg.Any<PolicyDefinitionStatus?>(), Arg.Any<PolicyEnforcementMode?>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var handler = new RunPreCommitGovernanceCheck.Handler(_policyRepo);
        var command = new RunPreCommitGovernanceCheck.Command(
            ServiceName: "BadName",
            Domain: "payments",
            TechnicalOwner: "owner",
            RepositoryUrl: "https://github.com/org/repo",
            ExposedApiPaths: ["/payments"],
            PolicyNamesToEnforce: []);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.AutoFixSuggestions.Should().NotBeEmpty();
        result.Value.AutoFixSuggestions.Should().Contain(s => s.Contains("NAMING-001") || s.Contains("VERSION-001"));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Validator
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Validator_EmptyServiceName_Fails()
    {
        var validator = new RunPreCommitGovernanceCheck.Validator();
        var result = validator.Validate(new RunPreCommitGovernanceCheck.Command(
            ServiceName: "",
            Domain: "payments",
            TechnicalOwner: null,
            RepositoryUrl: null,
            ExposedApiPaths: [],
            PolicyNamesToEnforce: []));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ServiceName");
    }

    [Fact]
    public void Validator_EmptyDomain_Fails()
    {
        var validator = new RunPreCommitGovernanceCheck.Validator();
        var result = validator.Validate(new RunPreCommitGovernanceCheck.Command(
            ServiceName: "my-service",
            Domain: "",
            TechnicalOwner: null,
            RepositoryUrl: null,
            ExposedApiPaths: [],
            PolicyNamesToEnforce: []));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Domain");
    }

    [Fact]
    public void Validator_ValidCommand_Passes()
    {
        var validator = new RunPreCommitGovernanceCheck.Validator();
        var result = validator.Validate(new RunPreCommitGovernanceCheck.Command(
            ServiceName: "valid-service",
            Domain: "payments",
            TechnicalOwner: "owner@company.com",
            RepositoryUrl: "https://github.com/org/repo",
            ExposedApiPaths: ["/api/v1/resources"],
            PolicyNamesToEnforce: []));
        result.IsValid.Should().BeTrue();
    }
}
