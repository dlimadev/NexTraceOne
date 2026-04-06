using MediatR;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.API.GraphQL;
using NexTraceOne.Catalog.API.GraphQL.Types;
using NexTraceOne.Catalog.Application.Contracts.Features.ListContractsByService;
using NexTraceOne.Catalog.Application.DeveloperExperience.Features.GetDeveloperNpsSummary;
using NexTraceOne.Catalog.Application.Graph.Features.ListServices;

namespace NexTraceOne.Catalog.Tests.GraphQL;

/// <summary>
/// Testes unitários para o GraphQL Federation Gateway — Phase 5.3 MVP.
/// Cobrem: CatalogQuery (serviços, contratos, NPS), tipos GraphQL e mapeamento de resposta.
/// </summary>
public sealed class CatalogGraphQLGatewayTests
{
    private static readonly Guid ServiceId1 = Guid.Parse("11111111-0000-0000-0000-000000000001");
    private static readonly Guid ServiceId2 = Guid.Parse("22222222-0000-0000-0000-000000000002");
    private static readonly Guid VersionId1 = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly Guid ApiAssetId1 = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000001");

    private static readonly DateTimeOffset FixedNow = new(2026, 4, 6, 12, 0, 0, TimeSpan.Zero);

    // ─────────────────────────────────────────────────────────────────────
    // ServiceType GraphQL type
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void ServiceType_ShouldHaveAllRequiredFields()
    {
        var type = new ServiceType
        {
            ServiceId = ServiceId1,
            Name = "order-service",
            DisplayName = "Order Service",
            Description = "Manages customer orders",
            ServiceKind = "RestApi",
            Domain = "Orders",
            SystemArea = "Commerce",
            TeamName = "Team Alpha",
            TechnicalOwner = "john.doe",
            Criticality = "High",
            LifecycleStatus = "Active",
            ExposureType = "Internal"
        };

        type.ServiceId.Should().Be(ServiceId1);
        type.Name.Should().Be("order-service");
        type.Domain.Should().Be("Orders");
        type.ServiceKind.Should().Be("RestApi");
        type.Criticality.Should().Be("High");
    }

    // ─────────────────────────────────────────────────────────────────────
    // ContractSummaryType GraphQL type
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void ContractSummaryType_ShouldHaveAllRequiredFields()
    {
        var type = new ContractSummaryType
        {
            VersionId = VersionId1,
            ApiAssetId = ApiAssetId1,
            ServiceId = ServiceId1,
            ApiName = "Order API",
            ApiRoutePattern = "/api/v1/orders",
            SemVer = "1.2.0",
            Protocol = "REST",
            LifecycleState = "Active",
            IsLocked = true,
            CreatedAt = FixedNow
        };

        type.VersionId.Should().Be(VersionId1);
        type.ServiceId.Should().Be(ServiceId1);
        type.SemVer.Should().Be("1.2.0");
        type.Protocol.Should().Be("REST");
        type.IsLocked.Should().BeTrue();
    }

    // ─────────────────────────────────────────────────────────────────────
    // NpsSummaryType GraphQL type
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void NpsSummaryType_ShouldHaveAllRequiredFields()
    {
        var type = new NpsSummaryType
        {
            TeamId = "team-alpha",
            Period = "2026-Q1",
            TotalResponses = 50,
            NpsScore = 45.5m,
            PromoterPercent = 60m,
            PassivePercent = 25.5m,
            DetractorPercent = 14.5m,
            PromoterCount = 30,
            PassiveCount = 13,
            DetractorCount = 7,
            AvgToolSatisfaction = 4.2m,
            AvgProcessSatisfaction = 3.8m,
            AvgPlatformSatisfaction = 4.5m
        };

        type.TeamId.Should().Be("team-alpha");
        type.NpsScore.Should().Be(45.5m);
        type.TotalResponses.Should().Be(50);
        type.PromoterPercent.Should().Be(60m);
    }

    // ─────────────────────────────────────────────────────────────────────
    // CatalogQuery — GetServicesAsync
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetServices_ReturnsEmptyList_WhenMediatorReturnsError()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<ListServices.Query>(), Arg.Any<CancellationToken>())
            .Returns(Error.NotFound("NOT_FOUND", "No services"));

        var query = new CatalogQuery();
        var result = await query.GetServicesAsync(mediator, cancellationToken: CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetServices_MapsAllFieldsCorrectly_WhenMediatorSucceeds()
    {
        var mediator = Substitute.For<IMediator>();
        var serviceItem = new ListServices.ServiceListItem(
            ServiceId: ServiceId1,
            Name: "order-service",
            DisplayName: "Order Service",
            Description: "Manages orders",
            ServiceType: "RestApi",
            Domain: "Orders",
            SystemArea: "Commerce",
            TeamName: "Team Alpha",
            TechnicalOwner: "john",
            Criticality: "High",
            LifecycleStatus: "Active",
            ExposureType: "Internal");

        var response = new ListServices.Response(
            Items: [serviceItem],
            TotalCount: 1);

        mediator.Send(Arg.Any<ListServices.Query>(), Arg.Any<CancellationToken>())
            .Returns(response);

        var query = new CatalogQuery();
        var result = await query.GetServicesAsync(mediator, domain: "Orders", cancellationToken: CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].ServiceId.Should().Be(ServiceId1);
        result[0].Name.Should().Be("order-service");
        result[0].Domain.Should().Be("Orders");
        result[0].ServiceKind.Should().Be("RestApi");
        result[0].Criticality.Should().Be("High");
    }

    [Fact]
    public async Task GetServices_ParsesCriticalityFilter_CaseInsensitive()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<ListServices.Query>(), Arg.Any<CancellationToken>())
            .Returns(new ListServices.Response(Items: [], TotalCount: 0));

        var query = new CatalogQuery();
        await query.GetServicesAsync(mediator, criticality: "critical", cancellationToken: CancellationToken.None);

        await mediator.Received(1).Send(
            Arg.Is<ListServices.Query>(q => q.Criticality == NexTraceOne.Catalog.Domain.Graph.Enums.Criticality.Critical),
            Arg.Any<CancellationToken>());
    }

    // ─────────────────────────────────────────────────────────────────────
    // CatalogQuery — GetContractsAsync
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetContracts_ReturnsEmptyList_WhenMediatorReturnsError()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<ListContractsByService.Query>(), Arg.Any<CancellationToken>())
            .Returns(Error.NotFound("NOT_FOUND", "Service not found"));

        var query = new CatalogQuery();
        var result = await query.GetContractsAsync(mediator, ServiceId1, CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetContracts_MapsContractFields_WhenMediatorSucceeds()
    {
        var mediator = Substitute.For<IMediator>();
        var contractItem = new ListContractsByService.ServiceContractItem(
            VersionId: VersionId1,
            ApiAssetId: ApiAssetId1,
            ApiName: "Order API",
            ApiRoutePattern: "/api/v1/orders",
            SemVer: "1.0.0",
            Protocol: "REST",
            LifecycleState: "Active",
            IsLocked: false,
            CreatedAt: FixedNow);

        var contractResponse = new ListContractsByService.Response(
            ServiceId: ServiceId1,
            Contracts: [contractItem],
            TotalCount: 1);

        mediator.Send(Arg.Any<ListContractsByService.Query>(), Arg.Any<CancellationToken>())
            .Returns(contractResponse);

        var query = new CatalogQuery();
        var result = await query.GetContractsAsync(mediator, ServiceId1, CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].VersionId.Should().Be(VersionId1);
        result[0].ServiceId.Should().Be(ServiceId1);
        result[0].Protocol.Should().Be("REST");
        result[0].IsLocked.Should().BeFalse();
    }

    // ─────────────────────────────────────────────────────────────────────
    // CatalogQuery — GetNpsSummaryAsync
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetNpsSummary_ReturnsNull_WhenMediatorReturnsError()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<GetDeveloperNpsSummary.Query>(), Arg.Any<CancellationToken>())
            .Returns(Error.NotFound("NOT_FOUND", "No surveys"));

        var query = new CatalogQuery();
        var result = await query.GetNpsSummaryAsync(mediator, "team-beta", cancellationToken: CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetNpsSummary_MapsNpsFields_WhenMediatorSucceeds()
    {
        var mediator = Substitute.For<IMediator>();
        var npsResponse = new GetDeveloperNpsSummary.Response(
            TeamId: "team-alpha",
            Period: "2026-Q1",
            TotalResponses: 40,
            NpsScore: 52m,
            PromoterPercent: 65m,
            PassivePercent: 22m,
            DetractorPercent: 13m,
            PromoterCount: 26,
            PassiveCount: 9,
            DetractorCount: 5,
            AvgToolSatisfaction: 4.1m,
            AvgProcessSatisfaction: 3.9m,
            AvgPlatformSatisfaction: 4.3m);

        mediator.Send(Arg.Any<GetDeveloperNpsSummary.Query>(), Arg.Any<CancellationToken>())
            .Returns(npsResponse);

        var query = new CatalogQuery();
        var result = await query.GetNpsSummaryAsync(mediator, "team-alpha", period: "2026-Q1", cancellationToken: CancellationToken.None);

        result.Should().NotBeNull();
        result!.TeamId.Should().Be("team-alpha");
        result.NpsScore.Should().Be(52m);
        result.PromoterPercent.Should().Be(65m);
        result.TotalResponses.Should().Be(40);
    }
}
