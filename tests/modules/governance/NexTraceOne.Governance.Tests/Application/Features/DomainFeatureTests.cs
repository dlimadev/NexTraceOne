using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Catalog.Contracts.Graph.ServiceInterfaces;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Application.Features.CreateDomain;
using NexTraceOne.Governance.Application.Features.GetDomainDetail;
using NexTraceOne.Governance.Application.Features.ListDomains;
using NexTraceOne.Governance.Application.Features.UpdateDomain;
using NexTraceOne.Governance.Domain.Entities;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Tests.Application.Features;

/// <summary>
/// Testes de unidade para as features de gestão de domínios de negócio.
/// </summary>
public sealed class DomainFeatureTests
{
    private readonly IGovernanceDomainRepository _domainRepository = Substitute.For<IGovernanceDomainRepository>();
    private readonly ITeamDomainLinkRepository _teamDomainLinkRepository = Substitute.For<ITeamDomainLinkRepository>();
    private readonly ITeamRepository _teamRepository = Substitute.For<ITeamRepository>();
    private readonly ICatalogGraphModule _catalogGraph = Substitute.For<ICatalogGraphModule>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    // ── CreateDomain ──

    [Fact]
    public async Task CreateDomain_ValidData_ShouldReturnDomainId()
    {
        // Arrange
        _domainRepository.GetByNameAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((GovernanceDomain?)null);
        _unitOfWork.CommitAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        var handler = new CreateDomain.Handler(_domainRepository, _unitOfWork);
        var command = new CreateDomain.Command("payments", "Payments Domain", "Payment processing", "High", "Financial");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.DomainId.Should().NotBeNullOrWhiteSpace();
        Guid.TryParse(result.Value.DomainId, out _).Should().BeTrue();
        await _domainRepository.Received(1).AddAsync(Arg.Any<GovernanceDomain>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateDomain_DuplicateName_ShouldReturnConflictError()
    {
        // Arrange
        var existing = GovernanceDomain.Create("payments", "Payments");
        _domainRepository.GetByNameAsync("payments", Arg.Any<CancellationToken>())
            .Returns(existing);

        var handler = new CreateDomain.Handler(_domainRepository, _unitOfWork);
        var command = new CreateDomain.Command("payments", "Payments", null, "High", null);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("DOMAIN_NAME_EXISTS");
    }

    [Fact]
    public async Task CreateDomain_InvalidCriticality_ShouldReturnValidationError()
    {
        // Arrange
        _domainRepository.GetByNameAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((GovernanceDomain?)null);

        var handler = new CreateDomain.Handler(_domainRepository, _unitOfWork);
        var command = new CreateDomain.Command("test", "Test", null, "SuperCritical", null);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("INVALID_CRITICALITY");
    }

    [Theory]
    [InlineData("Low")]
    [InlineData("Medium")]
    [InlineData("High")]
    [InlineData("Critical")]
    public async Task CreateDomain_AllValidCriticalities_ShouldSucceed(string criticality)
    {
        // Arrange
        _domainRepository.GetByNameAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((GovernanceDomain?)null);
        _unitOfWork.CommitAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        var handler = new CreateDomain.Handler(_domainRepository, _unitOfWork);
        var command = new CreateDomain.Command($"domain-{criticality}", $"Domain {criticality}", null, criticality, null);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    // ── ListDomains ──

    [Fact]
    public async Task ListDomains_WithData_ShouldReturnItems()
    {
        // Arrange
        var domains = new List<GovernanceDomain>
        {
            GovernanceDomain.Create("commerce", "Commerce", "Commerce domain", DomainCriticality.High),
            GovernanceDomain.Create("identity", "Identity", "Identity domain", DomainCriticality.Critical)
        };

        _domainRepository.ListAsync(Arg.Any<DomainCriticality?>(), Arg.Any<CancellationToken>())
            .Returns(domains);
        _teamDomainLinkRepository.ListByDomainIdAsync(Arg.Any<GovernanceDomainId>(), Arg.Any<CancellationToken>())
            .Returns(new List<TeamDomainLink>());
        _catalogGraph.CountServicesByDomainAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(3);

        var handler = new ListDomains.Handler(_domainRepository, _teamDomainLinkRepository, _catalogGraph);
        var query = new ListDomains.Query();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Domains.Should().HaveCount(2);
        result.Value.Domains[0].ServiceCount.Should().Be(3);
    }

    [Fact]
    public async Task ListDomains_EmptyDatabase_ShouldReturnEmptyList()
    {
        // Arrange
        _domainRepository.ListAsync(Arg.Any<DomainCriticality?>(), Arg.Any<CancellationToken>())
            .Returns(new List<GovernanceDomain>());

        var handler = new ListDomains.Handler(_domainRepository, _teamDomainLinkRepository, _catalogGraph);

        // Act
        var result = await handler.Handle(new ListDomains.Query(), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Domains.Should().BeEmpty();
    }

    // ── GetDomainDetail ──

    [Fact]
    public async Task GetDomainDetail_ValidId_ShouldReturnDetail()
    {
        // Arrange
        var domain = GovernanceDomain.Create("commerce", "Commerce", "E-commerce domain", DomainCriticality.High);
        _domainRepository.GetByIdAsync(Arg.Any<GovernanceDomainId>(), Arg.Any<CancellationToken>())
            .Returns(domain);
        _teamDomainLinkRepository.ListByDomainIdAsync(Arg.Any<GovernanceDomainId>(), Arg.Any<CancellationToken>())
            .Returns(new List<TeamDomainLink>());
        _catalogGraph.CountServicesByDomainAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(10);

        var handler = new GetDomainDetail.Handler(_domainRepository, _teamDomainLinkRepository, _teamRepository, _catalogGraph);
        var query = new GetDomainDetail.Query(domain.Id.Value.ToString());

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("commerce");
        result.Value.Criticality.Should().Be("High");
        result.Value.ServiceCount.Should().Be(10);
    }

    [Fact]
    public async Task GetDomainDetail_InvalidGuidFormat_ShouldReturnValidationError()
    {
        // Arrange
        var handler = new GetDomainDetail.Handler(_domainRepository, _teamDomainLinkRepository, _teamRepository, _catalogGraph);

        // Act
        var result = await handler.Handle(new GetDomainDetail.Query("not-valid"), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("INVALID_DOMAIN_ID");
    }

    [Fact]
    public async Task GetDomainDetail_NotFound_ShouldReturnNotFoundError()
    {
        // Arrange
        _domainRepository.GetByIdAsync(Arg.Any<GovernanceDomainId>(), Arg.Any<CancellationToken>())
            .Returns((GovernanceDomain?)null);

        var handler = new GetDomainDetail.Handler(_domainRepository, _teamDomainLinkRepository, _teamRepository, _catalogGraph);

        // Act
        var result = await handler.Handle(new GetDomainDetail.Query(Guid.NewGuid().ToString()), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("DOMAIN_NOT_FOUND");
    }

    // ── UpdateDomain ──

    [Fact]
    public async Task UpdateDomain_ValidData_ShouldSucceed()
    {
        // Arrange
        var domain = GovernanceDomain.Create("commerce", "Commerce");
        _domainRepository.GetByIdAsync(Arg.Any<GovernanceDomainId>(), Arg.Any<CancellationToken>())
            .Returns(domain);
        _unitOfWork.CommitAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        var handler = new UpdateDomain.Handler(_domainRepository, _unitOfWork);
        var command = new UpdateDomain.Command(domain.Id.Value.ToString(), "Commerce Updated", "New desc", "Critical", "Business");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _domainRepository.Received(1).UpdateAsync(Arg.Any<GovernanceDomain>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateDomain_InvalidCriticality_ShouldReturnValidationError()
    {
        // Arrange
        var domain = GovernanceDomain.Create("test", "Test");
        _domainRepository.GetByIdAsync(Arg.Any<GovernanceDomainId>(), Arg.Any<CancellationToken>())
            .Returns(domain);

        var handler = new UpdateDomain.Handler(_domainRepository, _unitOfWork);
        var command = new UpdateDomain.Command(domain.Id.Value.ToString(), "Test", null, "InvalidValue", null);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("INVALID_CRITICALITY");
    }

    [Fact]
    public async Task UpdateDomain_InvalidGuidFormat_ShouldReturnValidationError()
    {
        // Arrange
        var handler = new UpdateDomain.Handler(_domainRepository, _unitOfWork);
        var command = new UpdateDomain.Command("bad-guid", "Name", null, "High", null);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("INVALID_DOMAIN_ID");
    }

    [Fact]
    public async Task UpdateDomain_NotFound_ShouldReturnNotFoundError()
    {
        // Arrange
        _domainRepository.GetByIdAsync(Arg.Any<GovernanceDomainId>(), Arg.Any<CancellationToken>())
            .Returns((GovernanceDomain?)null);

        var handler = new UpdateDomain.Handler(_domainRepository, _unitOfWork);
        var command = new UpdateDomain.Command(Guid.NewGuid().ToString(), "Name", null, "High", null);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("DOMAIN_NOT_FOUND");
    }
}
