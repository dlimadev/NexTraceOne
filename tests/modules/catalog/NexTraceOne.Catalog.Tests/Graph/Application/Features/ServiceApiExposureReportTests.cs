using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Catalog.Application.Graph.Abstractions;
using NexTraceOne.Catalog.Application.Graph.Features.GetServiceApiExposureReport;
using NexTraceOne.Catalog.Domain.Graph.Entities;
using NexTraceOne.Catalog.Domain.Graph.Enums;

namespace NexTraceOne.Catalog.Tests.Graph.Application.Features;

/// <summary>
/// Testes unitários para Wave P.1 — GetServiceApiExposureReport.
/// Cobre: relatório vazio, contagem de serviços e APIs, serviços órfãos,
/// serviços de alta exposição, distribuição por visibilidade e tipo de exposição,
/// ranking por contagem de APIs e média de APIs por serviço.
/// </summary>
public sealed class ServiceApiExposureReportTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 4, 21, 12, 0, 0, TimeSpan.Zero);

    private static IDateTimeProvider CreateClock()
    {
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(FixedNow);
        return clock;
    }

    private static ServiceAsset MakeService(
        string name,
        ExposureType exposureType = ExposureType.Internal)
    {
        var svc = ServiceAsset.Create(name, "Domain", "Team-A");
        svc.UpdateDetails(
            displayName: name,
            description: string.Empty,
            serviceType: ServiceType.RestApi,
            systemArea: string.Empty,
            criticality: Criticality.Medium,
            lifecycleStatus: LifecycleStatus.Active,
            exposureType: exposureType,
            documentationUrl: string.Empty,
            repositoryUrl: string.Empty);
        return svc;
    }

    private static ApiAsset MakeApi(ServiceAsset owner, string name, string visibility = "Internal")
        => ApiAsset.Register(name, $"/api/{name}", "1.0.0", visibility, owner);

    private static GetServiceApiExposureReport.Handler CreateHandler(
        IReadOnlyList<ServiceAsset> services,
        IReadOnlyList<ApiAsset> apis)
    {
        var svcRepo = Substitute.For<IServiceAssetRepository>();
        svcRepo.ListAllAsync(Arg.Any<CancellationToken>()).Returns(services);

        var apiRepo = Substitute.For<IApiAssetRepository>();
        apiRepo.ListAllAsync(Arg.Any<CancellationToken>()).Returns(apis);

        return new GetServiceApiExposureReport.Handler(svcRepo, apiRepo, CreateClock());
    }

    // ── Empty report ──────────────────────────────────────────────────────

    [Fact]
    public async Task Report_Empty_When_No_Services()
    {
        var handler = CreateHandler([], []);
        var result = await handler.Handle(new GetServiceApiExposureReport.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalServices.Should().Be(0);
        result.Value.TotalApis.Should().Be(0);
        result.Value.OrphanedServiceCount.Should().Be(0);
        result.Value.HighExposureServiceCount.Should().Be(0);
        result.Value.TopServicesByApiCount.Should().BeEmpty();
    }

    // ── Totals ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Report_Counts_Services_And_Apis_Correctly()
    {
        var svc1 = MakeService("svc-1");
        var svc2 = MakeService("svc-2");
        var api1 = MakeApi(svc1, "api-1");
        var api2 = MakeApi(svc1, "api-2");
        var api3 = MakeApi(svc2, "api-3");

        var handler = CreateHandler([svc1, svc2], [api1, api2, api3]);
        var result = await handler.Handle(new GetServiceApiExposureReport.Query(), CancellationToken.None);

        result.Value.TotalServices.Should().Be(2);
        result.Value.TotalApis.Should().Be(3);
    }

    // ── Orphaned services ─────────────────────────────────────────────────

    [Fact]
    public async Task OrphanedServiceCount_Is_Services_With_No_Apis()
    {
        var svc1 = MakeService("svc-1");
        var svc2 = MakeService("svc-2"); // orphaned — no APIs
        var api1 = MakeApi(svc1, "api-1");

        var handler = CreateHandler([svc1, svc2], [api1]);
        var result = await handler.Handle(new GetServiceApiExposureReport.Query(), CancellationToken.None);

        result.Value.OrphanedServiceCount.Should().Be(1);
        result.Value.TotalApis.Should().Be(1);
    }

    [Fact]
    public async Task AllServices_Orphaned_When_No_Apis()
    {
        var svc1 = MakeService("svc-1");
        var svc2 = MakeService("svc-2");

        var handler = CreateHandler([svc1, svc2], []);
        var result = await handler.Handle(new GetServiceApiExposureReport.Query(), CancellationToken.None);

        result.Value.OrphanedServiceCount.Should().Be(2);
        result.Value.TotalApis.Should().Be(0);
    }

    // ── High exposure ─────────────────────────────────────────────────────

    [Fact]
    public async Task HighExposureServiceCount_Based_On_Threshold()
    {
        var svc1 = MakeService("svc-1");
        var svc2 = MakeService("svc-2");
        // svc-1 has 5 APIs → high exposure (threshold=5)
        var apis = Enumerable.Range(1, 5).Select(i => MakeApi(svc1, $"api-{i}")).ToList();
        // svc-2 has 2 APIs → not high exposure
        apis.AddRange(Enumerable.Range(6, 2).Select(i => MakeApi(svc2, $"api-{i}")));

        var handler = CreateHandler([svc1, svc2], apis);
        var result = await handler.Handle(new GetServiceApiExposureReport.Query(HighExposureThreshold: 5), CancellationToken.None);

        result.Value.HighExposureServiceCount.Should().Be(1);
    }

    [Fact]
    public async Task HighExposureServiceCount_Zero_When_All_Below_Threshold()
    {
        var svc1 = MakeService("svc-1");
        var api1 = MakeApi(svc1, "api-1");

        var handler = CreateHandler([svc1], [api1]);
        var result = await handler.Handle(new GetServiceApiExposureReport.Query(HighExposureThreshold: 5), CancellationToken.None);

        result.Value.HighExposureServiceCount.Should().Be(0);
    }

    // ── Visibility distribution ───────────────────────────────────────────

    [Fact]
    public async Task ApisByVisibility_Correctly_Classified()
    {
        var svc = MakeService("svc-1");
        var pub = MakeApi(svc, "public-api", "Public");
        var intern = MakeApi(svc, "internal-api", "Internal");
        var partner = MakeApi(svc, "partner-api", "Partner");
        var other = MakeApi(svc, "other-api", "Unknown");

        var handler = CreateHandler([svc], [pub, intern, partner, other]);
        var result = await handler.Handle(new GetServiceApiExposureReport.Query(), CancellationToken.None);

        result.Value.ApisByVisibility.PublicCount.Should().Be(1);
        result.Value.ApisByVisibility.InternalCount.Should().Be(1);
        result.Value.ApisByVisibility.PartnerCount.Should().Be(1);
        result.Value.ApisByVisibility.OtherCount.Should().Be(1);
    }

    // ── Exposure type distribution ────────────────────────────────────────

    [Fact]
    public async Task ServicesByExposureType_Correctly_Distributed()
    {
        var internal1 = MakeService("svc-i1", ExposureType.Internal);
        var internal2 = MakeService("svc-i2", ExposureType.Internal);
        var external = MakeService("svc-e", ExposureType.External);
        var partner = MakeService("svc-p", ExposureType.Partner);

        var handler = CreateHandler([internal1, internal2, external, partner], []);
        var result = await handler.Handle(new GetServiceApiExposureReport.Query(), CancellationToken.None);

        result.Value.ServicesByExposureType.InternalCount.Should().Be(2);
        result.Value.ServicesByExposureType.ExternalCount.Should().Be(1);
        result.Value.ServicesByExposureType.PartnerCount.Should().Be(1);
    }

    // ── Top services by API count ─────────────────────────────────────────

    [Fact]
    public async Task TopServicesByApiCount_Sorted_Descending()
    {
        var svc1 = MakeService("svc-low");
        var svc2 = MakeService("svc-high");
        var apis1 = Enumerable.Range(1, 2).Select(i => MakeApi(svc1, $"api-{i}")).ToList();
        var apis2 = Enumerable.Range(10, 5).Select(i => MakeApi(svc2, $"api-{i}")).ToList();

        var handler = CreateHandler([svc1, svc2], [.. apis1, .. apis2]);
        var result = await handler.Handle(new GetServiceApiExposureReport.Query(MaxTopServices: 5), CancellationToken.None);

        result.Value.TopServicesByApiCount.Should().HaveCount(2);
        result.Value.TopServicesByApiCount[0].ServiceName.Should().Be("svc-high");
        result.Value.TopServicesByApiCount[0].ApiCount.Should().Be(5);
        result.Value.TopServicesByApiCount[1].ServiceName.Should().Be("svc-low");
        result.Value.TopServicesByApiCount[1].ApiCount.Should().Be(2);
    }

    [Fact]
    public async Task TopServicesByApiCount_Limited_By_MaxTopServices()
    {
        var services = Enumerable.Range(1, 10).Select(i => MakeService($"svc-{i}")).ToList();
        var apis = services.SelectMany((s, idx) =>
            Enumerable.Range(1, idx + 1).Select(i => MakeApi(s, $"api-{s.Name}-{i}"))).ToList();

        var handler = CreateHandler(services, apis);
        var result = await handler.Handle(new GetServiceApiExposureReport.Query(MaxTopServices: 3), CancellationToken.None);

        result.Value.TopServicesByApiCount.Should().HaveCount(3);
    }

    // ── ApiPerServiceAvg ──────────────────────────────────────────────────

    [Fact]
    public async Task ApiPerServiceAvg_Calculated_Correctly()
    {
        var svc1 = MakeService("svc-1");
        var svc2 = MakeService("svc-2");
        var api1 = MakeApi(svc1, "api-1");
        var api2 = MakeApi(svc1, "api-2");
        var api3 = MakeApi(svc1, "api-3");
        // svc2 has no APIs → total 3 APIs / 2 services = 1.5

        var handler = CreateHandler([svc1, svc2], [api1, api2, api3]);
        var result = await handler.Handle(new GetServiceApiExposureReport.Query(), CancellationToken.None);

        result.Value.ApiPerServiceAvg.Should().Be(1.5m);
    }

    // ── IsHighExposure flag ───────────────────────────────────────────────

    [Fact]
    public async Task ServiceEntry_IsHighExposure_Flag_Correct()
    {
        var svc = MakeService("svc-1");
        var apis = Enumerable.Range(1, 5).Select(i => MakeApi(svc, $"api-{i}")).ToList();

        var handler = CreateHandler([svc], apis);
        var result = await handler.Handle(new GetServiceApiExposureReport.Query(HighExposureThreshold: 5), CancellationToken.None);

        result.Value.TopServicesByApiCount[0].IsHighExposure.Should().BeTrue();
    }

    // ── GeneratedAt ───────────────────────────────────────────────────────

    [Fact]
    public async Task Report_GeneratedAt_Is_UtcNow()
    {
        var handler = CreateHandler([], []);
        var result = await handler.Handle(new GetServiceApiExposureReport.Query(), CancellationToken.None);

        result.Value.GeneratedAt.Should().Be(FixedNow);
    }

    // ── Validator ─────────────────────────────────────────────────────────

    [Theory]
    [InlineData(0, 5)]
    [InlineData(101, 5)]
    [InlineData(10, 0)]
    [InlineData(10, 101)]
    public void Validator_Rejects_OutOfRange_Values(int maxTopServices, int highExposureThreshold)
    {
        var validator = new GetServiceApiExposureReport.Validator();
        var result = validator.Validate(new GetServiceApiExposureReport.Query(maxTopServices, highExposureThreshold));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_Accepts_Valid_Query()
    {
        var validator = new GetServiceApiExposureReport.Validator();
        var result = validator.Validate(new GetServiceApiExposureReport.Query(10, 5));
        result.IsValid.Should().BeTrue();
    }
}
