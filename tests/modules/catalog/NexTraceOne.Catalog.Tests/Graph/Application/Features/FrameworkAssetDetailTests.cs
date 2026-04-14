using FluentAssertions;
using NSubstitute;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Catalog.Application.Graph.Abstractions;
using NexTraceOne.Catalog.Domain.Graph.Entities;
using NexTraceOne.Catalog.Domain.Graph.Enums;
using RegisterFrameworkDetailFeature = NexTraceOne.Catalog.Application.Graph.Features.RegisterFrameworkDetail.RegisterFrameworkDetail;
using UpdateFrameworkDetailFeature = NexTraceOne.Catalog.Application.Graph.Features.UpdateFrameworkDetail.UpdateFrameworkDetail;
using GetFrameworkDetailFeature = NexTraceOne.Catalog.Application.Graph.Features.GetFrameworkDetail.GetFrameworkDetail;
using PublishFrameworkVersionFeature = NexTraceOne.Catalog.Application.Graph.Features.PublishFrameworkVersion.PublishFrameworkVersion;

namespace NexTraceOne.Catalog.Tests.Graph.Application.Features;

/// <summary>
/// Testes dos handlers de Framework/SDK do catálogo de serviços.
/// Cobre registo, atualização, consulta e publicação de versão.
/// </summary>
public sealed class FrameworkAssetDetailTests
{
    // ── Helpers ────────────────────────────────────────────────────────

    private static ServiceAsset CreateFrameworkService()
    {
        var service = ServiceAsset.Create("auth-sdk", "Platform", "SDK Team");
        service.UpdateDetails(
            "Auth SDK",
            "Authentication SDK",
            ServiceType.Framework,
            "Platform",
            Criticality.High,
            LifecycleStatus.Active,
            ExposureType.Internal,
            "",
            "");
        return service;
    }

    private static ServiceAsset CreateNonFrameworkService()
    {
        var service = ServiceAsset.Create("payments-api", "Finance", "Payments Team");
        // default ServiceType is RestApi
        return service;
    }

    private static FrameworkAssetDetail CreateFrameworkDetail(ServiceAssetId serviceAssetId)
    {
        return FrameworkAssetDetail.Create(
            serviceAssetId,
            "NexTrace.Auth.SDK",
            "C#",
            "NuGet",
            "https://nuget.example.com",
            "2.1.0",
            "1.0.0",
            ".NET 10",
            "Internal",
            "https://ci.example.com/auth-sdk",
            "https://docs.example.com/auth-sdk/changelog");
    }

    // ── RegisterFrameworkDetail ────────────────────────────────────────

    [Fact]
    public async Task RegisterFrameworkDetail_Should_CreateDetail_When_ServiceIsFrameworkType()
    {
        var serviceRepo = Substitute.For<IServiceAssetRepository>();
        var frameworkRepo = Substitute.For<IFrameworkAssetDetailRepository>();
        var unitOfWork = Substitute.For<ICatalogGraphUnitOfWork>();
        var sut = new RegisterFrameworkDetailFeature.Handler(serviceRepo, frameworkRepo, unitOfWork);

        var service = CreateFrameworkService();
        serviceRepo.GetByIdAsync(Arg.Any<ServiceAssetId>(), Arg.Any<CancellationToken>())
            .Returns(service);
        frameworkRepo.GetByServiceAssetIdAsync(Arg.Any<ServiceAssetId>(), Arg.Any<CancellationToken>())
            .Returns((FrameworkAssetDetail?)null);

        var result = await sut.Handle(
            new RegisterFrameworkDetailFeature.Command(
                service.Id.Value,
                "NexTrace.Auth.SDK",
                "C#",
                "NuGet",
                "https://nuget.example.com",
                "1.0.0",
                "1.0.0",
                ".NET 10",
                "Internal",
                "https://ci.example.com/auth-sdk",
                "https://docs.example.com/changelog"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.PackageName.Should().Be("NexTrace.Auth.SDK");
        result.Value.Language.Should().Be("C#");
        result.Value.PackageManager.Should().Be("NuGet");
        result.Value.LatestVersion.Should().Be("1.0.0");
        result.Value.TargetPlatform.Should().Be(".NET 10");
        frameworkRepo.Received(1).Add(Arg.Any<FrameworkAssetDetail>());
        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RegisterFrameworkDetail_Should_ReturnError_When_ServiceNotFound()
    {
        var serviceRepo = Substitute.For<IServiceAssetRepository>();
        var frameworkRepo = Substitute.For<IFrameworkAssetDetailRepository>();
        var unitOfWork = Substitute.For<ICatalogGraphUnitOfWork>();
        var sut = new RegisterFrameworkDetailFeature.Handler(serviceRepo, frameworkRepo, unitOfWork);

        serviceRepo.GetByIdAsync(Arg.Any<ServiceAssetId>(), Arg.Any<CancellationToken>())
            .Returns((ServiceAsset?)null);

        var result = await sut.Handle(
            new RegisterFrameworkDetailFeature.Command(
                Guid.NewGuid(), "Pkg", "C#", "NuGet"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CatalogGraph.ServiceAsset.NotFoundById");
    }

    [Fact]
    public async Task RegisterFrameworkDetail_Should_ReturnError_When_ServiceNotFrameworkType()
    {
        var serviceRepo = Substitute.For<IServiceAssetRepository>();
        var frameworkRepo = Substitute.For<IFrameworkAssetDetailRepository>();
        var unitOfWork = Substitute.For<ICatalogGraphUnitOfWork>();
        var sut = new RegisterFrameworkDetailFeature.Handler(serviceRepo, frameworkRepo, unitOfWork);

        var service = CreateNonFrameworkService();
        serviceRepo.GetByIdAsync(Arg.Any<ServiceAssetId>(), Arg.Any<CancellationToken>())
            .Returns(service);

        var result = await sut.Handle(
            new RegisterFrameworkDetailFeature.Command(
                service.Id.Value, "Pkg", "C#", "NuGet"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CatalogGraph.ServiceAsset.NotFrameworkType");
    }

    [Fact]
    public async Task RegisterFrameworkDetail_Should_ReturnError_When_DetailAlreadyExists()
    {
        var serviceRepo = Substitute.For<IServiceAssetRepository>();
        var frameworkRepo = Substitute.For<IFrameworkAssetDetailRepository>();
        var unitOfWork = Substitute.For<ICatalogGraphUnitOfWork>();
        var sut = new RegisterFrameworkDetailFeature.Handler(serviceRepo, frameworkRepo, unitOfWork);

        var service = CreateFrameworkService();
        serviceRepo.GetByIdAsync(Arg.Any<ServiceAssetId>(), Arg.Any<CancellationToken>())
            .Returns(service);

        var existingDetail = CreateFrameworkDetail(service.Id);
        frameworkRepo.GetByServiceAssetIdAsync(Arg.Any<ServiceAssetId>(), Arg.Any<CancellationToken>())
            .Returns(existingDetail);

        var result = await sut.Handle(
            new RegisterFrameworkDetailFeature.Command(
                service.Id.Value, "Pkg", "C#", "NuGet"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CatalogGraph.FrameworkDetail.AlreadyExists");
    }

    // ── UpdateFrameworkDetail ─────────────────────────────────────────

    [Fact]
    public async Task UpdateFrameworkDetail_Should_UpdateFields()
    {
        var frameworkRepo = Substitute.For<IFrameworkAssetDetailRepository>();
        var unitOfWork = Substitute.For<ICatalogGraphUnitOfWork>();
        var sut = new UpdateFrameworkDetailFeature.Handler(frameworkRepo, unitOfWork);

        var service = CreateFrameworkService();
        var detail = CreateFrameworkDetail(service.Id);
        frameworkRepo.GetByServiceAssetIdAsync(Arg.Any<ServiceAssetId>(), Arg.Any<CancellationToken>())
            .Returns(detail);

        var result = await sut.Handle(
            new UpdateFrameworkDetailFeature.Command(
                service.Id.Value,
                "NexTrace.Auth.SDK.v2",
                "TypeScript",
                "npm",
                "https://npm.example.com",
                "3.0.0",
                "2.0.0",
                "Node 22",
                "MIT",
                "https://ci.example.com/auth-sdk-v2",
                "https://docs.example.com/auth-sdk-v2/changelog"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.PackageName.Should().Be("NexTrace.Auth.SDK.v2");
        result.Value.Language.Should().Be("TypeScript");
        result.Value.PackageManager.Should().Be("npm");
        result.Value.LatestVersion.Should().Be("3.0.0");
        result.Value.TargetPlatform.Should().Be("Node 22");
        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateFrameworkDetail_Should_ReturnError_When_NotFound()
    {
        var frameworkRepo = Substitute.For<IFrameworkAssetDetailRepository>();
        var unitOfWork = Substitute.For<ICatalogGraphUnitOfWork>();
        var sut = new UpdateFrameworkDetailFeature.Handler(frameworkRepo, unitOfWork);

        frameworkRepo.GetByServiceAssetIdAsync(Arg.Any<ServiceAssetId>(), Arg.Any<CancellationToken>())
            .Returns((FrameworkAssetDetail?)null);

        var result = await sut.Handle(
            new UpdateFrameworkDetailFeature.Command(
                Guid.NewGuid(), "Pkg", "C#", "NuGet"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CatalogGraph.FrameworkDetail.NotFound");
    }

    // ── GetFrameworkDetail ────────────────────────────────────────────

    [Fact]
    public async Task GetFrameworkDetail_Should_ReturnDetail_When_Exists()
    {
        var frameworkRepo = Substitute.For<IFrameworkAssetDetailRepository>();
        var sut = new GetFrameworkDetailFeature.Handler(frameworkRepo);

        var service = CreateFrameworkService();
        var detail = CreateFrameworkDetail(service.Id);
        frameworkRepo.GetByServiceAssetIdAsync(Arg.Any<ServiceAssetId>(), Arg.Any<CancellationToken>())
            .Returns(detail);

        var result = await sut.Handle(
            new GetFrameworkDetailFeature.Query(service.Id.Value),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.PackageName.Should().Be("NexTrace.Auth.SDK");
        result.Value.Language.Should().Be("C#");
        result.Value.PackageManager.Should().Be("NuGet");
        result.Value.ArtifactRegistryUrl.Should().Be("https://nuget.example.com");
        result.Value.LatestVersion.Should().Be("2.1.0");
        result.Value.MinSupportedVersion.Should().Be("1.0.0");
        result.Value.TargetPlatform.Should().Be(".NET 10");
        result.Value.LicenseType.Should().Be("Internal");
    }

    [Fact]
    public async Task GetFrameworkDetail_Should_ReturnError_When_NotFound()
    {
        var frameworkRepo = Substitute.For<IFrameworkAssetDetailRepository>();
        var sut = new GetFrameworkDetailFeature.Handler(frameworkRepo);

        frameworkRepo.GetByServiceAssetIdAsync(Arg.Any<ServiceAssetId>(), Arg.Any<CancellationToken>())
            .Returns((FrameworkAssetDetail?)null);

        var result = await sut.Handle(
            new GetFrameworkDetailFeature.Query(Guid.NewGuid()),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CatalogGraph.FrameworkDetail.NotFound");
    }

    // ── PublishFrameworkVersion ────────────────────────────────────────

    [Fact]
    public async Task PublishFrameworkVersion_Should_UpdateLatestVersion()
    {
        var frameworkRepo = Substitute.For<IFrameworkAssetDetailRepository>();
        var unitOfWork = Substitute.For<ICatalogGraphUnitOfWork>();
        var sut = new PublishFrameworkVersionFeature.Handler(frameworkRepo, unitOfWork);

        var service = CreateFrameworkService();
        var detail = CreateFrameworkDetail(service.Id);
        frameworkRepo.GetByServiceAssetIdAsync(Arg.Any<ServiceAssetId>(), Arg.Any<CancellationToken>())
            .Returns(detail);

        var result = await sut.Handle(
            new PublishFrameworkVersionFeature.Command(service.Id.Value, "3.0.0"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.LatestVersion.Should().Be("3.0.0");
        result.Value.PackageName.Should().Be("NexTrace.Auth.SDK");
        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    // ── Domain Entity Tests ───────────────────────────────────────────

    [Fact]
    public void FrameworkAssetDetail_Domain_Create_Should_SetFields()
    {
        var serviceId = ServiceAssetId.New();
        var detail = FrameworkAssetDetail.Create(
            serviceId,
            "NexTrace.Core",
            "C#",
            "NuGet",
            "https://registry.example.com",
            "1.0.0",
            "0.9.0",
            ".NET 10",
            "MIT",
            "https://ci.example.com/core",
            "https://docs.example.com/core/changelog");

        detail.Id.Should().NotBe(default(FrameworkAssetDetailId));
        detail.ServiceAssetId.Should().Be(serviceId);
        detail.PackageName.Should().Be("NexTrace.Core");
        detail.Language.Should().Be("C#");
        detail.PackageManager.Should().Be("NuGet");
        detail.ArtifactRegistryUrl.Should().Be("https://registry.example.com");
        detail.LatestVersion.Should().Be("1.0.0");
        detail.MinSupportedVersion.Should().Be("0.9.0");
        detail.TargetPlatform.Should().Be(".NET 10");
        detail.LicenseType.Should().Be("MIT");
        detail.BuildPipelineUrl.Should().Be("https://ci.example.com/core");
        detail.ChangelogUrl.Should().Be("https://docs.example.com/core/changelog");
        detail.KnownConsumerCount.Should().Be(0);
    }

    [Fact]
    public void FrameworkAssetDetail_Domain_PublishVersion_Should_UpdateLatestVersion()
    {
        var detail = FrameworkAssetDetail.Create(
            ServiceAssetId.New(),
            "NexTrace.Core",
            "C#",
            "NuGet",
            latestVersion: "1.0.0");

        detail.PublishVersion("2.0.0");

        detail.LatestVersion.Should().Be("2.0.0");
    }
}
