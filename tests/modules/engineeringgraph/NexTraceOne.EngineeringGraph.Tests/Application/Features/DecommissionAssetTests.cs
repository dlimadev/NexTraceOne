using FluentAssertions;
using MediatR;
using NSubstitute;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.EngineeringGraph.Application.Abstractions;
using NexTraceOne.EngineeringGraph.Domain.Entities;
using DecommissionAssetFeature = NexTraceOne.EngineeringGraph.Application.Features.DecommissionAsset.DecommissionAsset;

namespace NexTraceOne.EngineeringGraph.Tests.Application.Features;

/// <summary>
/// Testes do handler DecommissionAsset que marca um ativo de API como descomissionado,
/// impedindo novos mapeamentos e atualizações de metadados.
/// Cenários cobertos: descomissionamento bem-sucedido, ativo já descomissionado e ativo inexistente.
/// </summary>
public sealed class DecommissionAssetTests
{
    private readonly IApiAssetRepository _apiAssetRepository = Substitute.For<IApiAssetRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly DecommissionAssetFeature.Handler _sut;

    public DecommissionAssetTests()
    {
        _sut = new DecommissionAssetFeature.Handler(_apiAssetRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_Should_DecommissionApiAsset_When_AssetIsActive()
    {
        // Arrange — API ativa pronta para descomissionamento
        var ownerService = ServiceAsset.Create("payments-service", "Finance", "Payments Team");
        var api = ApiAsset.Register("Payments API", "/api/payments", "1.0.0", "Internal", ownerService);

        _apiAssetRepository.GetByIdAsync(
                Arg.Is<ApiAssetId>(id => id.Value == api.Id.Value),
                Arg.Any<CancellationToken>())
            .Returns(api);

        // Act
        var result = await _sut.Handle(
            new DecommissionAssetFeature.Command(api.Id.Value), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        api.IsDecommissioned.Should().BeTrue();
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_When_ApiAlreadyDecommissioned()
    {
        // Arrange — API já descomissionada (Decommission chamado previamente)
        var ownerService = ServiceAsset.Create("legacy-service", "Legacy", "Legacy Team");
        var api = ApiAsset.Register("Legacy API", "/api/legacy", "0.9.0", "Internal", ownerService);
        api.Decommission();

        _apiAssetRepository.GetByIdAsync(
                Arg.Is<ApiAssetId>(id => id.Value == api.Id.Value),
                Arg.Any<CancellationToken>())
            .Returns(api);

        // Act
        var result = await _sut.Handle(
            new DecommissionAssetFeature.Command(api.Id.Value), CancellationToken.None);

        // Assert — segundo descomissionamento retorna erro de conflito
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("EngineeringGraph.ApiAsset.Decommissioned");
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_When_ApiDoesNotExist()
    {
        // Arrange — API inexistente no repositório
        var nonExistentId = Guid.NewGuid();

        _apiAssetRepository.GetByIdAsync(Arg.Any<ApiAssetId>(), Arg.Any<CancellationToken>())
            .Returns((ApiAsset?)null);

        // Act
        var result = await _sut.Handle(
            new DecommissionAssetFeature.Command(nonExistentId), CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("EngineeringGraph.ApiAsset.NotFound");
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }
}
