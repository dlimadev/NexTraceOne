using FluentAssertions;
using NSubstitute;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Catalog.Application.Graph.Abstractions;
using NexTraceOne.Catalog.Domain.Graph.Entities;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Contracts.DTOs;
using NexTraceOne.Configuration.Domain.Enums;
using RegisterServiceAssetFeature = NexTraceOne.Catalog.Application.Graph.Features.RegisterServiceAsset.RegisterServiceAsset;

namespace NexTraceOne.Catalog.Tests.Graph.Application.Features;

/// <summary>
/// Testes do approval gate na criação de serviços — parametrização catalog.service.creation.approval_required.
/// </summary>
public sealed class RegisterServiceAssetApprovalTests
{
    [Fact]
    public async Task RegisterServiceAsset_Should_SetPendingApproval_When_ApprovalRequired()
    {
        // Arrange
        var repository = Substitute.For<IServiceAssetRepository>();
        var configService = Substitute.For<IConfigurationResolutionService>();
        var unitOfWork = Substitute.For<ICatalogGraphUnitOfWork>();

        repository.GetByNameAsync("order-service", Arg.Any<CancellationToken>())
            .Returns((ServiceAsset?)null);

        configService.ResolveEffectiveValueAsync(
                "catalog.service.creation.approval_required",
                ConfigurationScope.Tenant,
                null,
                Arg.Any<CancellationToken>())
            .Returns(new EffectiveConfigurationDto(
                "catalog.service.creation.approval_required",
                "true",
                "Tenant",
                null,
                false,
                false,
                "catalog.service.creation.approval_required",
                "Boolean",
                false,
                1));

        var sut = new RegisterServiceAssetFeature.Handler(repository, configService, unitOfWork);

        // Act
        var result = await sut.Handle(
            new RegisterServiceAssetFeature.Command("order-service", "Commerce", "Orders Team"),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.LifecycleStatus.Should().Be("PendingApproval");
        result.Value.IsPendingApproval.Should().BeTrue();
    }

    [Fact]
    public async Task RegisterServiceAsset_Should_NotSetPendingApproval_When_ApprovalNotRequired()
    {
        // Arrange
        var repository = Substitute.For<IServiceAssetRepository>();
        var configService = Substitute.For<IConfigurationResolutionService>();
        var unitOfWork = Substitute.For<ICatalogGraphUnitOfWork>();

        repository.GetByNameAsync("order-service", Arg.Any<CancellationToken>())
            .Returns((ServiceAsset?)null);

        configService.ResolveEffectiveValueAsync(
                "catalog.service.creation.approval_required",
                ConfigurationScope.Tenant,
                null,
                Arg.Any<CancellationToken>())
            .Returns(new EffectiveConfigurationDto(
                "catalog.service.creation.approval_required",
                "false",
                "Tenant",
                null,
                false,
                true,
                "catalog.service.creation.approval_required",
                "Boolean",
                false,
                1));

        var sut = new RegisterServiceAssetFeature.Handler(repository, configService, unitOfWork);

        // Act
        var result = await sut.Handle(
            new RegisterServiceAssetFeature.Command("order-service", "Commerce", "Orders Team"),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.LifecycleStatus.Should().Be("Active");
        result.Value.IsPendingApproval.Should().BeFalse();
    }

    [Fact]
    public async Task RegisterServiceAsset_Should_NotSetPendingApproval_When_ConfigReturnsNull()
    {
        // Arrange
        var repository = Substitute.For<IServiceAssetRepository>();
        var configService = Substitute.For<IConfigurationResolutionService>();
        var unitOfWork = Substitute.For<ICatalogGraphUnitOfWork>();

        repository.GetByNameAsync("order-service", Arg.Any<CancellationToken>())
            .Returns((ServiceAsset?)null);

        configService.ResolveEffectiveValueAsync(
                "catalog.service.creation.approval_required",
                ConfigurationScope.Tenant,
                null,
                Arg.Any<CancellationToken>())
            .Returns((EffectiveConfigurationDto?)null);

        var sut = new RegisterServiceAssetFeature.Handler(repository, configService, unitOfWork);

        // Act
        var result = await sut.Handle(
            new RegisterServiceAssetFeature.Command("order-service", "Commerce", "Orders Team"),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.LifecycleStatus.Should().Be("Active");
        result.Value.IsPendingApproval.Should().BeFalse();
    }
}
