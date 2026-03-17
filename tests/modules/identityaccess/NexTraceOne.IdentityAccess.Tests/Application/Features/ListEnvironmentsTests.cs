using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Domain.Entities;
using NexTraceOne.IdentityAccess.Tests.TestDoubles;

using ListEnvironmentsFeature = NexTraceOne.IdentityAccess.Application.Features.ListEnvironments.ListEnvironments;
using Environment = NexTraceOne.IdentityAccess.Domain.Entities.Environment;

namespace NexTraceOne.IdentityAccess.Tests.Application.Features;

/// <summary>
/// Testes da feature ListEnvironments.
/// Cobre cenários de listagem de ambientes do tenant atual,
/// incluindo validação de contexto de tenant obrigatório.
/// </summary>
public sealed class ListEnvironmentsTests
{
    private readonly DateTimeOffset _now = new(2025, 03, 10, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Handle_Should_ReturnEnvironments_When_TenantContextIsValid()
    {
        var tenantId = TenantId.From(Guid.NewGuid());
        var currentTenant = new TestCurrentTenant(tenantId.Value);

        var environments = new List<Environment>
        {
            Environment.Create(tenantId, "Development", "development", 0, _now),
            Environment.Create(tenantId, "Pre-Production", "pre-production", 1, _now),
            Environment.Create(tenantId, "Production", "production", 2, _now)
        };

        var environmentRepository = Substitute.For<IEnvironmentRepository>();
        environmentRepository.ListByTenantAsync(tenantId, Arg.Any<CancellationToken>()).Returns(environments);

        var sut = new ListEnvironmentsFeature.Handler(currentTenant, environmentRepository);

        var result = await sut.Handle(new ListEnvironmentsFeature.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(3);
        result.Value[0].Name.Should().Be("Development");
        result.Value[0].Slug.Should().Be("development");
        result.Value[1].Name.Should().Be("Pre-Production");
        result.Value[2].Name.Should().Be("Production");
    }

    [Fact]
    public async Task Handle_Should_ReturnEmpty_When_NoEnvironmentsExist()
    {
        var tenantId = TenantId.From(Guid.NewGuid());
        var currentTenant = new TestCurrentTenant(tenantId.Value);

        var environmentRepository = Substitute.For<IEnvironmentRepository>();
        environmentRepository.ListByTenantAsync(tenantId, Arg.Any<CancellationToken>())
            .Returns(new List<Environment>());

        var sut = new ListEnvironmentsFeature.Handler(currentTenant, environmentRepository);

        var result = await sut.Handle(new ListEnvironmentsFeature.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_Should_ReturnError_When_TenantContextIsMissing()
    {
        var currentTenant = new TestCurrentTenant(Guid.Empty);
        var environmentRepository = Substitute.For<IEnvironmentRepository>();

        var sut = new ListEnvironmentsFeature.Handler(currentTenant, environmentRepository);

        var result = await sut.Handle(new ListEnvironmentsFeature.Query(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.Tenant.ContextRequired");
    }
}
