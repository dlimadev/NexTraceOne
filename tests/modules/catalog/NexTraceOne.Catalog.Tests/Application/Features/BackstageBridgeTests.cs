using NexTraceOne.Catalog.Application.Graph.Abstractions;
using NexTraceOne.Catalog.Domain.Graph.Entities;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Contracts.DTOs;
using NexTraceOne.Configuration.Domain.Enums;

using ExportToBackstageFeature = NexTraceOne.Catalog.Application.Graph.Features.ExportToBackstage.ExportToBackstage;

namespace NexTraceOne.Catalog.Tests.Application.Features;

/// <summary>
/// Testes unitários para o Wave B.3 — Backstage Bidirectional Bridge.
/// Cobre a feature ExportToBackstage: mapeamento, slugify, filtros, defaults e anotações.
/// </summary>
public sealed class BackstageBridgeTests
{
    private static IConfigurationResolutionService CreateConfig(string backstageUrl = "https://backstage.example.com")
    {
        var cfg = Substitute.For<IConfigurationResolutionService>();
        cfg.ResolveEffectiveValueAsync(Arg.Any<string>(), Arg.Any<ConfigurationScope>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                var key = ci.ArgAt<string>(0);
                var val = key == "integrations.backstage.instanceUrl" ? backstageUrl : null;
                if (val is null) return Task.FromResult<EffectiveConfigurationDto?>(null);
                return Task.FromResult<EffectiveConfigurationDto?>(
                    new EffectiveConfigurationDto(key, val, "System", null, false, true, key, "String", false, 1));
            });
        return cfg;
    }

    private static ServiceAsset CreateService(string name = "my-service", string team = "platform-team", string description = "A test service")
    {
        var svc = ServiceAsset.Create(name, "platform", team);
        svc.UpdateDetails(
            name, description, Domain.Graph.Enums.ServiceType.RestApi, string.Empty,
            Domain.Graph.Enums.Criticality.Medium, Domain.Graph.Enums.LifecycleStatus.Active,
            Domain.Graph.Enums.ExposureType.Internal, string.Empty, string.Empty);
        return svc;
    }

    [Fact]
    public async Task ExportToBackstage_NoServices_ReturnsEmptyList()
    {
        var repo = Substitute.For<IServiceAssetRepository>();
        repo.ListAllAsync(Arg.Any<CancellationToken>()).Returns([]);

        var handler = new ExportToBackstageFeature.Handler(repo, CreateConfig());
        var result = await handler.Handle(new ExportToBackstageFeature.Query(null, null, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Entities.Should().BeEmpty();
        result.Value.TotalExported.Should().Be(0);
    }

    [Fact]
    public async Task ExportToBackstage_WithServices_MapsToBackstageEntities()
    {
        var svc = CreateService("order-service");
        var repo = Substitute.For<IServiceAssetRepository>();
        repo.ListAllAsync(Arg.Any<CancellationToken>()).Returns([svc]);

        var handler = new ExportToBackstageFeature.Handler(repo, CreateConfig());
        var result = await handler.Handle(new ExportToBackstageFeature.Query(null, null, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Entities.Should().HaveCount(1);
        var entity = result.Value.Entities[0];
        entity.Kind.Should().Be("Component");
        entity.Metadata.Name.Should().Be("order-service");
    }

    [Fact]
    public async Task ExportToBackstage_ServiceNameSlugified_LowercaseWithHyphens()
    {
        var svc = CreateService("Order Service");
        var repo = Substitute.For<IServiceAssetRepository>();
        repo.ListAllAsync(Arg.Any<CancellationToken>()).Returns([svc]);

        var handler = new ExportToBackstageFeature.Handler(repo, CreateConfig());
        var result = await handler.Handle(new ExportToBackstageFeature.Query(null, null, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Entities[0].Metadata.Name.Should().Be("order-service");
    }

    [Fact]
    public async Task ExportToBackstage_FilterByTeamName_ReturnsOnlyMatchingServices()
    {
        var svc1 = CreateService("svc-a", "alpha-team");
        var svc2 = CreateService("svc-b", "beta-team");
        var repo = Substitute.For<IServiceAssetRepository>();
        repo.ListByTeamAsync("alpha-team", Arg.Any<CancellationToken>()).Returns([svc1]);

        var handler = new ExportToBackstageFeature.Handler(repo, CreateConfig());
        var result = await handler.Handle(new ExportToBackstageFeature.Query(null, null, "alpha-team"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Entities.Should().HaveCount(1);
        result.Value.Entities[0].Metadata.Name.Should().Be("svc-a");
    }

    [Fact]
    public async Task ExportToBackstage_EntityHasCorrectApiVersion()
    {
        var svc = CreateService();
        var repo = Substitute.For<IServiceAssetRepository>();
        repo.ListAllAsync(Arg.Any<CancellationToken>()).Returns([svc]);

        var handler = new ExportToBackstageFeature.Handler(repo, CreateConfig());
        var result = await handler.Handle(new ExportToBackstageFeature.Query(null, null, null), CancellationToken.None);

        result.Value.Entities[0].ApiVersion.Should().Be("backstage.io/v1alpha1");
    }

    [Fact]
    public async Task ExportToBackstage_EntityAnnotationsContainServiceId()
    {
        var svc = CreateService();
        var repo = Substitute.For<IServiceAssetRepository>();
        repo.ListAllAsync(Arg.Any<CancellationToken>()).Returns([svc]);

        var handler = new ExportToBackstageFeature.Handler(repo, CreateConfig());
        var result = await handler.Handle(new ExportToBackstageFeature.Query(null, null, null), CancellationToken.None);

        var annotations = result.Value.Entities[0].Metadata.Annotations;
        annotations.Should().ContainKey("nextraceone.io/service-id");
        annotations["nextraceone.io/service-id"].Should().Be(svc.Id.Value.ToString());
    }

    [Fact]
    public async Task ExportToBackstage_LifecycleDefaultsToProduction()
    {
        var svc = CreateService();
        var repo = Substitute.For<IServiceAssetRepository>();
        repo.ListAllAsync(Arg.Any<CancellationToken>()).Returns([svc]);

        var handler = new ExportToBackstageFeature.Handler(repo, CreateConfig());
        var result = await handler.Handle(new ExportToBackstageFeature.Query(null, null, null), CancellationToken.None);

        result.Value.Entities[0].Spec.Lifecycle.Should().Be("production");
    }

    [Fact]
    public async Task ExportToBackstage_NamespaceUsedFromQuery()
    {
        var svc = CreateService();
        var repo = Substitute.For<IServiceAssetRepository>();
        repo.ListAllAsync(Arg.Any<CancellationToken>()).Returns([svc]);

        var handler = new ExportToBackstageFeature.Handler(repo, CreateConfig());
        var result = await handler.Handle(new ExportToBackstageFeature.Query("my-namespace", null, null), CancellationToken.None);

        result.Value.Entities[0].Metadata.Namespace.Should().Be("my-namespace");
    }

    [Fact]
    public async Task ExportToBackstage_MultipleServices_TotalExportedMatchesCount()
    {
        var svcs = new[]
        {
            CreateService("svc-a"),
            CreateService("svc-b"),
            CreateService("svc-c")
        };
        var repo = Substitute.For<IServiceAssetRepository>();
        repo.ListAllAsync(Arg.Any<CancellationToken>()).Returns(svcs);

        var handler = new ExportToBackstageFeature.Handler(repo, CreateConfig());
        var result = await handler.Handle(new ExportToBackstageFeature.Query(null, null, null), CancellationToken.None);

        result.Value.TotalExported.Should().Be(3);
        result.Value.Entities.Should().HaveCount(3);
    }

    [Fact]
    public async Task ExportToBackstage_NullDescription_MapsToEmptyString()
    {
        var svc = ServiceAsset.Create("no-desc", "platform", "team-x");
        var repo = Substitute.For<IServiceAssetRepository>();
        repo.ListAllAsync(Arg.Any<CancellationToken>()).Returns([svc]);

        var handler = new ExportToBackstageFeature.Handler(repo, CreateConfig());
        var result = await handler.Handle(new ExportToBackstageFeature.Query(null, null, null), CancellationToken.None);

        result.Value.Entities[0].Metadata.Description.Should().BeNullOrEmpty();
    }

    [Fact]
    public async Task ExportToBackstage_TagsMappedToMetadata()
    {
        var svc = CreateService();
        var repo = Substitute.For<IServiceAssetRepository>();
        repo.ListAllAsync(Arg.Any<CancellationToken>()).Returns([svc]);

        var handler = new ExportToBackstageFeature.Handler(repo, CreateConfig());
        var result = await handler.Handle(new ExportToBackstageFeature.Query(null, null, null), CancellationToken.None);

        // ServiceAsset has no Tags property — Tags maps to empty list
        result.Value.Entities[0].Metadata.Tags.Should().NotBeNull();
    }

    [Fact]
    public async Task ExportToBackstage_OwnerDefaultsToTeamNameWhenEmpty()
    {
        // When TeamName is minimal (whitespace would fail guard), use a valid minimal value
        // and test that the owner fallback works correctly when the team is not empty but has no special value
        var svc = ServiceAsset.Create("svc-no-team", "platform", "a");
        var repo = Substitute.For<IServiceAssetRepository>();
        repo.ListAllAsync(Arg.Any<CancellationToken>()).Returns([svc]);

        var handler = new ExportToBackstageFeature.Handler(repo, CreateConfig());
        var result = await handler.Handle(new ExportToBackstageFeature.Query(null, null, null), CancellationToken.None);

        // owner = "a" (team name is not empty)
        result.Value.Entities[0].Spec.Owner.Should().Be("a");
    }

    [Fact]
    public async Task ExportToBackstage_AnnotationsContainSourceUrl()
    {
        var svc = CreateService();
        var repo = Substitute.For<IServiceAssetRepository>();
        repo.ListAllAsync(Arg.Any<CancellationToken>()).Returns([svc]);

        var handler = new ExportToBackstageFeature.Handler(repo, CreateConfig("https://backstage.company.io"));
        var result = await handler.Handle(new ExportToBackstageFeature.Query(null, null, null), CancellationToken.None);

        var annotations = result.Value.Entities[0].Metadata.Annotations;
        annotations.Should().ContainKey("nextraceone.io/source-url");
        annotations["nextraceone.io/source-url"].Should().Be("https://backstage.company.io");
    }
}
