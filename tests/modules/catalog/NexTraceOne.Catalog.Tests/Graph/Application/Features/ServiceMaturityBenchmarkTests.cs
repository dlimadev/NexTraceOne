using NexTraceOne.Catalog.Application.Graph.Abstractions;
using NexTraceOne.Catalog.Domain.Graph.Entities;
using NexTraceOne.Catalog.Domain.Graph.Enums;

using GetServiceMaturityBenchmarkFeature = NexTraceOne.Catalog.Application.Graph.Features.GetServiceMaturityBenchmark.GetServiceMaturityBenchmark;

namespace NexTraceOne.Catalog.Tests.Graph.Application.Features;

/// <summary>
/// Testes do handler GetServiceMaturityBenchmark — benchmark de maturidade por equipa e domínio.
/// Valida ranking, filtros, limite TopN e comportamento com lista vazia.
/// </summary>
public sealed class ServiceMaturityBenchmarkTests
{
    private static ServiceAsset CreateService(
        string name,
        string domain,
        string teamName,
        string? technicalOwner = null,
        string? repositoryUrl = null,
        string? documentationUrl = null,
        string? description = null)
    {
        var service = ServiceAsset.Create(name, domain, teamName, Guid.NewGuid());

        if (technicalOwner is not null || repositoryUrl is not null
            || documentationUrl is not null || description is not null)
        {
            service.UpdateDetails(
                displayName: name,
                description: description ?? string.Empty,
                serviceType: ServiceType.RestApi,
                systemArea: string.Empty,
                criticality: Criticality.Medium,
                lifecycleStatus: LifecycleStatus.Active,
                exposureType: ExposureType.Internal,
                documentationUrl: documentationUrl ?? string.Empty,
                repositoryUrl: repositoryUrl ?? string.Empty);

            if (technicalOwner is not null)
                service.UpdateOwnership(teamName, technicalOwner, string.Empty);
        }

        return service;
    }

    // ── Equipas ranqueadas por score ──────────────────────────────────

    [Fact]
    public async Task Handle_Should_ReturnTeamsRankedByScore()
    {
        // Team A: tem tudo (score=1.0); Team B: só teamName+teamOwner (score=0.25)
        var services = new List<ServiceAsset>
        {
            CreateService("svc-a1", "Finance", "Team A", "owner@a.com", "https://repo.a", "https://docs.a", "Service A"),
            CreateService("svc-b1", "Finance", "Team B"),
        };

        var repository = Substitute.For<IServiceAssetRepository>();
        repository.ListFilteredAsync(
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<ServiceType?>(),
            Arg.Any<Criticality?>(), Arg.Any<LifecycleStatus?>(), Arg.Any<ExposureType?>(),
            Arg.Any<string?>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns((services, services.Count));

        var sut = new GetServiceMaturityBenchmarkFeature.Handler(repository);
        var result = await sut.Handle(
            new GetServiceMaturityBenchmarkFeature.Query(),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Teams.Should().HaveCount(2);
        result.Value.Teams[0].TeamName.Should().Be("Team A");
        result.Value.Teams[0].Rank.Should().Be(1);
        result.Value.Teams[1].TeamName.Should().Be("Team B");
        result.Value.Teams[1].Rank.Should().Be(2);
        result.Value.Teams[0].AverageMaturityScore.Should().BeGreaterThan(result.Value.Teams[1].AverageMaturityScore);
    }

    // ── Domínios ranqueados por score ─────────────────────────────────

    [Fact]
    public async Task Handle_Should_ReturnDomainsRankedByScore()
    {
        var services = new List<ServiceAsset>
        {
            // Finance: tem tudo
            CreateService("svc-f1", "Finance", "Team A", "owner@a.com", "https://repo.a", "https://docs.a", "Service F"),
            // Payments: só nome
            CreateService("svc-p1", "Payments", "Team B"),
        };

        var repository = Substitute.For<IServiceAssetRepository>();
        repository.ListFilteredAsync(
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<ServiceType?>(),
            Arg.Any<Criticality?>(), Arg.Any<LifecycleStatus?>(), Arg.Any<ExposureType?>(),
            Arg.Any<string?>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns((services, services.Count));

        var sut = new GetServiceMaturityBenchmarkFeature.Handler(repository);
        var result = await sut.Handle(
            new GetServiceMaturityBenchmarkFeature.Query(),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Domains.Should().HaveCount(2);
        result.Value.Domains[0].DomainName.Should().Be("Finance");
        result.Value.Domains[0].Rank.Should().Be(1);
    }

    // ── Limite TopN respeitado ────────────────────────────────────────

    [Fact]
    public async Task Handle_Should_RespectTopN_Limit()
    {
        var services = Enumerable.Range(1, 15)
            .Select(i => CreateService($"svc-{i}", $"Domain{i}", $"Team{i}"))
            .ToList();

        var repository = Substitute.For<IServiceAssetRepository>();
        repository.ListFilteredAsync(
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<ServiceType?>(),
            Arg.Any<Criticality?>(), Arg.Any<LifecycleStatus?>(), Arg.Any<ExposureType?>(),
            Arg.Any<string?>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns((services, services.Count));

        var sut = new GetServiceMaturityBenchmarkFeature.Handler(repository);
        var result = await sut.Handle(
            new GetServiceMaturityBenchmarkFeature.Query(TopN: 5),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Teams.Should().HaveCount(5);
        result.Value.Domains.Should().HaveCount(5);
    }

    // ── Filtro de domínio aplicado ─────────────────────────────────────

    [Fact]
    public async Task Handle_Should_ApplyDomainFilter_ToRepository()
    {
        var services = new List<ServiceAsset>
        {
            CreateService("svc-1", "Finance", "Team A")
        };

        var repository = Substitute.For<IServiceAssetRepository>();
        repository.ListFilteredAsync(
            teamName: null,
            domain: "Finance",
            serviceType: null,
            criticality: null,
            lifecycleStatus: null,
            exposureType: null,
            searchTerm: null,
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Any<CancellationToken>())
            .Returns((services, services.Count));

        var sut = new GetServiceMaturityBenchmarkFeature.Handler(repository);
        var result = await sut.Handle(
            new GetServiceMaturityBenchmarkFeature.Query(Domain: "Finance"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await repository.Received(1).ListFilteredAsync(
            teamName: null,
            domain: "Finance",
            serviceType: null,
            criticality: null,
            lifecycleStatus: null,
            exposureType: null,
            searchTerm: null,
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Any<CancellationToken>());
    }

    // ── Filtro de equipa aplicado ──────────────────────────────────────

    [Fact]
    public async Task Handle_Should_ApplyTeamNameFilter_ToRepository()
    {
        var services = new List<ServiceAsset>
        {
            CreateService("svc-1", "Finance", "Team A")
        };

        var repository = Substitute.For<IServiceAssetRepository>();
        repository.ListFilteredAsync(
            teamName: "Team A",
            domain: null,
            serviceType: null,
            criticality: null,
            lifecycleStatus: null,
            exposureType: null,
            searchTerm: null,
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Any<CancellationToken>())
            .Returns((services, services.Count));

        var sut = new GetServiceMaturityBenchmarkFeature.Handler(repository);
        var result = await sut.Handle(
            new GetServiceMaturityBenchmarkFeature.Query(TeamName: "Team A"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await repository.Received(1).ListFilteredAsync(
            teamName: "Team A",
            domain: null,
            serviceType: null,
            criticality: null,
            lifecycleStatus: null,
            exposureType: null,
            searchTerm: null,
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Any<CancellationToken>());
    }

    // ── Lista vazia retorna benchmark vazio ───────────────────────────

    [Fact]
    public async Task Handle_Should_ReturnEmptyBenchmark_When_NoServicesExist()
    {
        var repository = Substitute.For<IServiceAssetRepository>();
        repository.ListFilteredAsync(
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<ServiceType?>(),
            Arg.Any<Criticality?>(), Arg.Any<LifecycleStatus?>(), Arg.Any<ExposureType?>(),
            Arg.Any<string?>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns((new List<ServiceAsset>(), 0));

        var sut = new GetServiceMaturityBenchmarkFeature.Handler(repository);
        var result = await sut.Handle(
            new GetServiceMaturityBenchmarkFeature.Query(),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Teams.Should().BeEmpty();
        result.Value.Domains.Should().BeEmpty();
        result.Value.BenchmarkComputedAt.Should().NotBeNullOrEmpty();
    }
}
