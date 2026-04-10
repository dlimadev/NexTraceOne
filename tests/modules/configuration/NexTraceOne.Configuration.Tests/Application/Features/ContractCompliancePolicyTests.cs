using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Domain.Entities;
using NexTraceOne.Configuration.Domain.Enums;

using CreateContractCompliancePolicyFeature = NexTraceOne.Configuration.Application.Features.CreateContractCompliancePolicy.CreateContractCompliancePolicy;
using ListContractCompliancePoliciesFeature = NexTraceOne.Configuration.Application.Features.ListContractCompliancePolicies.ListContractCompliancePolicies;
using GetEffectiveCompliancePolicyFeature = NexTraceOne.Configuration.Application.Features.GetEffectiveCompliancePolicy.GetEffectiveCompliancePolicy;
using DeleteContractCompliancePolicyFeature = NexTraceOne.Configuration.Application.Features.DeleteContractCompliancePolicy.DeleteContractCompliancePolicy;

namespace NexTraceOne.Configuration.Tests.Application.Features;

/// <summary>
/// Testes unitários para as features de política de compliance contratual no módulo Configuration.
/// Cobre CreateContractCompliancePolicy, ListContractCompliancePolicies,
/// GetEffectiveCompliancePolicy e DeleteContractCompliancePolicy.
/// </summary>
public sealed class ContractCompliancePolicyTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 7, 1, 10, 0, 0, TimeSpan.Zero);
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly string TenantIdStr = TenantId.ToString();

    private static ICurrentTenant CreateCurrentTenant()
    {
        var tenant = Substitute.For<ICurrentTenant>();
        tenant.Id.Returns(TenantId);
        return tenant;
    }

    private static ICurrentUser CreateAuthenticatedUser()
    {
        var user = Substitute.For<ICurrentUser>();
        user.Id.Returns("user@test.com");
        user.IsAuthenticated.Returns(true);
        return user;
    }

    private static ICurrentUser CreateUnauthenticatedUser()
    {
        var user = Substitute.For<ICurrentUser>();
        user.IsAuthenticated.Returns(false);
        return user;
    }

    private static IDateTimeProvider CreateClock()
    {
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(FixedNow);
        return clock;
    }

    private static CreateContractCompliancePolicyFeature.Command CreateValidCommand(
        string name = "Default Policy",
        int scope = (int)PolicyScope.Organization) =>
        new(
            Name: name,
            Description: "Default compliance policy for the organization",
            Scope: scope,
            ScopeId: null,
            VerificationMode: (int)VerificationMode.SpecFile,
            VerificationApproach: (int)VerificationApproach.Active,
            OnBreakingChange: (int)ComplianceAction.BlockDeploy,
            OnNonBreakingChange: (int)ComplianceAction.Warn,
            OnNewEndpoint: (int)ComplianceAction.Warn,
            OnRemovedEndpoint: (int)ComplianceAction.BlockDeploy,
            OnMissingContract: (int)ComplianceAction.Warn,
            OnContractNotApproved: (int)ComplianceAction.BlockBuild,
            AutoGenerateChangelog: true,
            RequireChangelogApproval: false,
            EnforceCdct: false,
            CdctFailureAction: (int)ComplianceAction.Warn,
            EnableRuntimeDriftDetection: false,
            DriftDetectionIntervalMinutes: 60,
            DriftThresholdForAlert: 0.1m,
            DriftThresholdForIncident: 0.3m,
            NotifyOnVerificationFailure: true,
            NotifyOnBreakingChange: true,
            NotifyOnDriftDetected: false,
            NotificationChannels: "[]");

    private static ContractCompliancePolicy CreateDomainPolicy(
        PolicyScope scope = PolicyScope.Organization,
        string? scopeId = null,
        bool isActive = true)
    {
        var policy = ContractCompliancePolicy.Create(
            tenantId: TenantIdStr,
            name: "Test Policy",
            description: "Test",
            scope: scope,
            scopeId: scopeId,
            verificationMode: VerificationMode.SpecFile,
            verificationApproach: VerificationApproach.Active,
            onBreakingChange: ComplianceAction.BlockDeploy,
            onNonBreakingChange: ComplianceAction.Warn,
            onNewEndpoint: ComplianceAction.Warn,
            onRemovedEndpoint: ComplianceAction.BlockDeploy,
            onMissingContract: ComplianceAction.Warn,
            onContractNotApproved: ComplianceAction.BlockBuild,
            createdAt: FixedNow);

        if (!isActive)
            policy.Deactivate();

        return policy;
    }

    // ══════════════════════════════════════════════════════════════════
    // CreateContractCompliancePolicy
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task CreateContractCompliancePolicy_Should_Create_Policy_Successfully()
    {
        var repo = Substitute.For<IContractCompliancePolicyRepository>();

        var sut = new CreateContractCompliancePolicyFeature.Handler(
            repo, CreateAuthenticatedUser(), CreateCurrentTenant(), CreateClock());

        var result = await sut.Handle(CreateValidCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Default Policy");
        result.Value.PolicyId.Should().NotBe(Guid.Empty);
        result.Value.CreatedAt.Should().Be(FixedNow);
        await repo.Received(1).AddAsync(Arg.Any<ContractCompliancePolicy>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateContractCompliancePolicy_Validator_Should_Reject_Empty_Name()
    {
        var validator = new CreateContractCompliancePolicyFeature.Validator();

        var validationResult = await validator.ValidateAsync(CreateValidCommand(name: ""));

        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public async Task CreateContractCompliancePolicy_Should_Return_Unauthorized_When_Not_Authenticated()
    {
        var repo = Substitute.For<IContractCompliancePolicyRepository>();

        var sut = new CreateContractCompliancePolicyFeature.Handler(
            repo, CreateUnauthenticatedUser(), CreateCurrentTenant(), CreateClock());

        var result = await sut.Handle(CreateValidCommand(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("User.NotAuthenticated");
        await repo.DidNotReceive().AddAsync(Arg.Any<ContractCompliancePolicy>(), Arg.Any<CancellationToken>());
    }

    // ══════════════════════════════════════════════════════════════════
    // ListContractCompliancePolicies
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ListContractCompliancePolicies_Should_Return_Policies_For_Tenant()
    {
        var repo = Substitute.For<IContractCompliancePolicyRepository>();
        var policy = CreateDomainPolicy();

        repo.ListByTenantAsync(TenantIdStr, Arg.Any<CancellationToken>())
            .Returns(new List<ContractCompliancePolicy> { policy });

        var sut = new ListContractCompliancePoliciesFeature.Handler(
            repo, CreateAuthenticatedUser(), CreateCurrentTenant());

        var result = await sut.Handle(
            new ListContractCompliancePoliciesFeature.Query(null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
        result.Value.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task ListContractCompliancePolicies_Should_Filter_By_Scope()
    {
        var repo = Substitute.For<IContractCompliancePolicyRepository>();
        var orgPolicy = CreateDomainPolicy(PolicyScope.Organization);
        var svcPolicy = CreateDomainPolicy(PolicyScope.Service, "svc-001");

        repo.ListByTenantAsync(TenantIdStr, Arg.Any<CancellationToken>())
            .Returns(new List<ContractCompliancePolicy> { orgPolicy, svcPolicy });

        var sut = new ListContractCompliancePoliciesFeature.Handler(
            repo, CreateAuthenticatedUser(), CreateCurrentTenant());

        var result = await sut.Handle(
            new ListContractCompliancePoliciesFeature.Query((int)PolicyScope.Service), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
        result.Value.Items[0].Scope.Should().Be("Service");
    }

    // ══════════════════════════════════════════════════════════════════
    // GetEffectiveCompliancePolicy
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetEffectiveCompliancePolicy_Should_Resolve_Service_Scope_First()
    {
        var repo = Substitute.For<IContractCompliancePolicyRepository>();
        var servicePolicy = CreateDomainPolicy(PolicyScope.Service, "svc-001");

        repo.GetByScopeAsync(TenantIdStr, PolicyScope.Service, "svc-001", Arg.Any<CancellationToken>())
            .Returns(servicePolicy);

        var sut = new GetEffectiveCompliancePolicyFeature.Handler(
            repo, CreateAuthenticatedUser(), CreateCurrentTenant());

        var result = await sut.Handle(
            new GetEffectiveCompliancePolicyFeature.Query("svc-001", "team-001", "prod"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ResolvedScope.Should().Be("Service");
        result.Value.PolicyId.Should().Be(servicePolicy.Id.Value);
    }

    [Fact]
    public async Task GetEffectiveCompliancePolicy_Should_Fallback_To_Organization_Scope()
    {
        var repo = Substitute.For<IContractCompliancePolicyRepository>();
        var orgPolicy = CreateDomainPolicy(PolicyScope.Organization);

        // Service and Team and Environment scopes return null
        repo.GetByScopeAsync(TenantIdStr, PolicyScope.Service, "svc-001", Arg.Any<CancellationToken>())
            .Returns((ContractCompliancePolicy?)null);
        repo.GetByScopeAsync(TenantIdStr, PolicyScope.Team, "team-001", Arg.Any<CancellationToken>())
            .Returns((ContractCompliancePolicy?)null);
        repo.GetByScopeAsync(TenantIdStr, PolicyScope.Environment, "prod", Arg.Any<CancellationToken>())
            .Returns((ContractCompliancePolicy?)null);
        repo.GetByScopeAsync(TenantIdStr, PolicyScope.Organization, null, Arg.Any<CancellationToken>())
            .Returns(orgPolicy);

        var sut = new GetEffectiveCompliancePolicyFeature.Handler(
            repo, CreateAuthenticatedUser(), CreateCurrentTenant());

        var result = await sut.Handle(
            new GetEffectiveCompliancePolicyFeature.Query("svc-001", "team-001", "prod"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ResolvedScope.Should().Be("Organization");
    }

    [Fact]
    public async Task GetEffectiveCompliancePolicy_Should_Return_Default_When_No_Policy_Exists()
    {
        var repo = Substitute.For<IContractCompliancePolicyRepository>();

        repo.GetByScopeAsync(Arg.Any<string>(), Arg.Any<PolicyScope>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns((ContractCompliancePolicy?)null);

        var sut = new GetEffectiveCompliancePolicyFeature.Handler(
            repo, CreateAuthenticatedUser(), CreateCurrentTenant());

        var result = await sut.Handle(
            new GetEffectiveCompliancePolicyFeature.Query(null, null, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ResolvedScope.Should().Be("None");
        result.Value.PolicyId.Should().BeNull();
    }

    // ══════════════════════════════════════════════════════════════════
    // DeleteContractCompliancePolicy
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task DeleteContractCompliancePolicy_Should_Delete_Policy()
    {
        var repo = Substitute.For<IContractCompliancePolicyRepository>();
        var policy = CreateDomainPolicy();

        repo.GetByIdAsync(policy.Id, Arg.Any<CancellationToken>()).Returns(policy);

        var sut = new DeleteContractCompliancePolicyFeature.Handler(
            repo, CreateAuthenticatedUser(), CreateCurrentTenant());

        var result = await sut.Handle(
            new DeleteContractCompliancePolicyFeature.Command(policy.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(policy.Id.Value);
        await repo.Received(1).DeleteAsync(policy.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteContractCompliancePolicy_Should_Return_Error_When_Not_Found()
    {
        var repo = Substitute.For<IContractCompliancePolicyRepository>();
        var policyId = Guid.NewGuid();

        repo.GetByIdAsync(new ContractCompliancePolicyId(policyId), Arg.Any<CancellationToken>())
            .Returns((ContractCompliancePolicy?)null);

        var sut = new DeleteContractCompliancePolicyFeature.Handler(
            repo, CreateAuthenticatedUser(), CreateCurrentTenant());

        var result = await sut.Handle(
            new DeleteContractCompliancePolicyFeature.Command(policyId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        await repo.DidNotReceive().DeleteAsync(Arg.Any<ContractCompliancePolicyId>(), Arg.Any<CancellationToken>());
    }
}
