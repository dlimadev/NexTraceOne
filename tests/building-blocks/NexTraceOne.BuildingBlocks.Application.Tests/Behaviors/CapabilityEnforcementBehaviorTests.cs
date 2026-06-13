using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Behaviors;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.BuildingBlocks.Application.Tests.Behaviors;

/// <summary>
/// Testes unitários para <see cref="CapabilityEnforcementBehavior{TRequest,TResponse}"/>.
/// Valida: passagem sem atributo, bloqueio sem capability, execução com capability,
/// bypass para IPublicRequest e exigência de múltiplas capabilities.
/// </summary>
public sealed class CapabilityEnforcementBehaviorTests
{
    private sealed record PlainRequest;

    [RequiresCapability("contract_studio")]
    private sealed record PremiumRequest;

    [RequiresCapability("contract_studio")]
    private sealed record PublicPremiumRequest : IPublicRequest;

    [RequiresCapability("contract_studio")]
    [RequiresCapability("ai_enabled")]
    private sealed record DoublePremiumRequest;

    private readonly ICurrentTenant _tenant = Substitute.For<ICurrentTenant>();

    // MediatR 14: RequestHandlerDelegate<TResponse> recebe um CancellationToken.
    private static Task<Result<string>> Next(CancellationToken ct) => Task.FromResult(Result<string>.Success("executed"));

    [Fact]
    public async Task Handle_RequestWithoutAttribute_ShouldExecute()
    {
        var behavior = new CapabilityEnforcementBehavior<PlainRequest, Result<string>>(_tenant);

        var result = await behavior.Handle(new PlainRequest(), Next, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _tenant.DidNotReceive().HasCapability(Arg.Any<string>());
    }

    [Fact]
    public async Task Handle_TenantWithoutCapability_ShouldReturnForbidden()
    {
        _tenant.HasCapability("contract_studio").Returns(false);
        var behavior = new CapabilityEnforcementBehavior<PremiumRequest, Result<string>>(_tenant);

        var result = await behavior.Handle(new PremiumRequest(), Next, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CapabilityRequired");
        result.Error.Type.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task Handle_TenantWithCapability_ShouldExecute()
    {
        _tenant.HasCapability("contract_studio").Returns(true);
        var behavior = new CapabilityEnforcementBehavior<PremiumRequest, Result<string>>(_tenant);

        var result = await behavior.Handle(new PremiumRequest(), Next, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("executed");
    }

    [Fact]
    public async Task Handle_PublicRequest_ShouldBypassEnforcement()
    {
        var behavior = new CapabilityEnforcementBehavior<PublicPremiumRequest, Result<string>>(_tenant);

        var result = await behavior.Handle(new PublicPremiumRequest(), Next, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _tenant.DidNotReceive().HasCapability(Arg.Any<string>());
    }

    [Fact]
    public async Task Handle_MultipleCapabilities_ShouldRequireAll()
    {
        _tenant.HasCapability("contract_studio").Returns(true);
        _tenant.HasCapability("ai_enabled").Returns(false);
        var behavior = new CapabilityEnforcementBehavior<DoublePremiumRequest, Result<string>>(_tenant);

        var result = await behavior.Handle(new DoublePremiumRequest(), Next, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CapabilityRequired");
    }
}
