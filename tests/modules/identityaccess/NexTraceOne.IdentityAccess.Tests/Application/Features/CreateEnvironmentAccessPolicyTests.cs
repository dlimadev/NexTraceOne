using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Application.Features.CreateEnvironmentAccessPolicy;
using NexTraceOne.IdentityAccess.Application.Features.DeleteEnvironmentAccessPolicy;
using NexTraceOne.IdentityAccess.Application.Features.GetEnvironmentAccessPolicies;
using NexTraceOne.IdentityAccess.Domain.Entities;
using NexTraceOne.IdentityAccess.Tests.TestDoubles;

namespace NexTraceOne.IdentityAccess.Tests.Application.Features;

/// <summary>
/// Testes unitários do handler CreateEnvironmentAccessPolicy e funcionalidades relacionadas (W5-05).
/// Cobre: criação, listagem, eliminação e validação de políticas de acesso por ambiente.
/// </summary>
public sealed class CreateEnvironmentAccessPolicyTests
{
    private readonly IEnvironmentAccessPolicyRepository _repository =
        Substitute.For<IEnvironmentAccessPolicyRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentTenant _tenant = Substitute.For<ICurrentTenant>();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();

    public CreateEnvironmentAccessPolicyTests()
    {
        _tenant.Id.Returns(Guid.NewGuid());
        _clock.UtcNow.Returns(DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task Handle_ValidCommand_CreatesPolicy()
    {
        var handler = new CreateEnvironmentAccessPolicy.Handler(
            _repository, _unitOfWork, _tenant, _clock);
        var command = new CreateEnvironmentAccessPolicy.Command(
            "prod-policy",
            ["production"],
            ["admin", "sre"],
            ["developer"],
            "sre-lead");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.PolicyId.Should().NotBeEmpty();
        await _repository.Received(1).AddAsync(
            Arg.Any<EnvironmentAccessPolicy>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_EmptyPolicyName_FailsValidation()
    {
        var validator = new CreateEnvironmentAccessPolicy.Validator();
        var command = new CreateEnvironmentAccessPolicy.Command("", ["prod"], ["admin"], [], null);

        var result = await validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_EmptyEnvironments_FailsValidation()
    {
        var validator = new CreateEnvironmentAccessPolicy.Validator();
        var command = new CreateEnvironmentAccessPolicy.Command("policy", [], ["admin"], [], null);

        var result = await validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task GetPolicies_ReturnsTenantPolicies()
    {
        var tenantId = _tenant.Id;
        var policies = new List<EnvironmentAccessPolicy>
        {
            EnvironmentAccessPolicy.Create(
                "p1", tenantId, ["prod"], ["admin"], [], null, DateTimeOffset.UtcNow),
        };
        _repository.ListByTenantAsync(tenantId, Arg.Any<CancellationToken>()).Returns(policies);

        var handler = new GetEnvironmentAccessPolicies.Handler(_repository, _tenant);
        var result = await handler.Handle(new GetEnvironmentAccessPolicies.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value[0].PolicyName.Should().Be("p1");
    }

    [Fact]
    public async Task DeletePolicy_ExistingPolicy_Deactivates()
    {
        var policy = EnvironmentAccessPolicy.Create(
            "p1", _tenant.Id, ["prod"], ["admin"], [], null, DateTimeOffset.UtcNow);
        _repository
            .GetByIdAsync(Arg.Any<EnvironmentAccessPolicyId>(), Arg.Any<CancellationToken>())
            .Returns(policy);

        var handler = new DeleteEnvironmentAccessPolicy.Handler(_repository, _unitOfWork);
        var result = await handler.Handle(
            new DeleteEnvironmentAccessPolicy.Command(policy.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        policy.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task DeletePolicy_NonExistent_ReturnsNotFound()
    {
        _repository
            .GetByIdAsync(Arg.Any<EnvironmentAccessPolicyId>(), Arg.Any<CancellationToken>())
            .Returns((EnvironmentAccessPolicy?)null);

        var handler = new DeleteEnvironmentAccessPolicy.Handler(_repository, _unitOfWork);
        var result = await handler.Handle(
            new DeleteEnvironmentAccessPolicy.Command(Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task CreatePolicy_WithJitRoles_SetsJitApprovalField()
    {
        var handler = new CreateEnvironmentAccessPolicy.Handler(
            _repository, _unitOfWork, _tenant, _clock);
        var command = new CreateEnvironmentAccessPolicy.Command(
            "restricted-prod",
            ["production"],
            [],
            ["engineer", "analyst"],
            "security-team");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _repository.Received(1).AddAsync(
            Arg.Is<EnvironmentAccessPolicy>(p =>
                p.RequireJitForRoles.Contains("engineer") &&
                p.JitApprovalRequiredFrom == "security-team"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetPolicies_EmptyTenant_ReturnsEmpty()
    {
        _repository
            .ListByTenantAsync(_tenant.Id, Arg.Any<CancellationToken>())
            .Returns(new List<EnvironmentAccessPolicy>());

        var handler = new GetEnvironmentAccessPolicies.Handler(_repository, _tenant);
        var result = await handler.Handle(new GetEnvironmentAccessPolicies.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task EnvironmentAccessPolicy_Create_SetsAllFields()
    {
        var tenantId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var policy = EnvironmentAccessPolicy.Create(
            "test-policy", tenantId,
            ["prod", "staging"], ["admin"], ["dev"],
            "approver-role", now);

        policy.PolicyName.Should().Be("test-policy");
        policy.TenantId.Should().Be(tenantId);
        policy.Environments.Should().Contain("prod");
        policy.AllowedRoles.Should().Contain("admin");
        policy.RequireJitForRoles.Should().Contain("dev");
        policy.JitApprovalRequiredFrom.Should().Be("approver-role");
        policy.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task EnvironmentAccessPolicy_Deactivate_SetsIsActiveFalse()
    {
        var policy = EnvironmentAccessPolicy.Create(
            "p", Guid.NewGuid(), ["prod"], ["admin"], [], null, DateTimeOffset.UtcNow);

        policy.Deactivate();

        policy.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task EnvironmentAccessPolicy_Update_ChangesFields()
    {
        var policy = EnvironmentAccessPolicy.Create(
            "old-name", Guid.NewGuid(), ["prod"], ["admin"], [], null, DateTimeOffset.UtcNow);

        policy.Update("new-name", ["staging"], ["sre"], ["dev"], "approver");

        policy.PolicyName.Should().Be("new-name");
        policy.Environments.Should().Contain("staging");
        policy.AllowedRoles.Should().Contain("sre");
        policy.RequireJitForRoles.Should().Contain("dev");
        policy.JitApprovalRequiredFrom.Should().Be("approver");
    }
}
