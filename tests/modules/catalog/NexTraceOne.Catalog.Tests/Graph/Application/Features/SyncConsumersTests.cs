using FluentAssertions;
using NSubstitute;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Catalog.Application.Graph.Abstractions;
using NexTraceOne.Catalog.Domain.Graph.Entities;
using SyncConsumersFeature = NexTraceOne.Catalog.Application.Graph.Features.SyncConsumers.SyncConsumers;

namespace NexTraceOne.Catalog.Tests.Graph.Application.Features;

/// <summary>
/// Testes do handler SyncConsumers para integração inbound externa.
/// Valida criação, atualização, idempotência e tratamento de falhas.
/// </summary>
public sealed class SyncConsumersTests
{
    private readonly IApiAssetRepository _apiAssetRepository = Substitute.For<IApiAssetRepository>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly ICatalogGraphUnitOfWork _unitOfWork = Substitute.For<ICatalogGraphUnitOfWork>();
    private readonly DateTimeOffset _now = new(2026, 03, 13, 10, 0, 0, TimeSpan.Zero);

    private SyncConsumersFeature.Handler CreateSut()
    {
        _dateTimeProvider.UtcNow.Returns(_now);
        return new SyncConsumersFeature.Handler(_apiAssetRepository, _dateTimeProvider, _unitOfWork);
    }

    [Fact]
    public async Task SyncConsumers_Should_CreateRelationship_When_ConsumerIsNew()
    {
        var ownerService = ServiceAsset.Create("payments-service", "Finance", "Payments Team");
        var apiAsset = ApiAsset.Register("Payments API", "/api/payments", "1.0.0", "Internal", ownerService);
        _apiAssetRepository.GetByIdAsync(Arg.Any<ApiAssetId>(), Arg.Any<CancellationToken>()).Returns(apiAsset);

        var sut = CreateSut();
        var command = new SyncConsumersFeature.Command(
            [new SyncConsumersFeature.ConsumerSyncItem(
                apiAsset.Id.Value,
                "billing-service",
                "Service",
                "Production",
                "kong-gateway/billing",
                0.90m)],
            "KongGateway",
            "correlation-001");

        var result = await sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Created.Should().Be(1);
        result.Value.Updated.Should().Be(0);
        result.Value.Failed.Should().Be(0);
        result.Value.Total.Should().Be(1);
        result.Value.CorrelationId.Should().Be("correlation-001");
        result.Value.Results.Should().ContainSingle(r =>
            r.Outcome == SyncConsumersFeature.SyncOutcome.Created);
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SyncConsumers_Should_UpdateRelationship_When_ConsumerAlreadyExists()
    {
        var ownerService = ServiceAsset.Create("payments-service", "Finance", "Payments Team");
        var apiAsset = ApiAsset.Register("Payments API", "/api/payments", "1.0.0", "Internal", ownerService);

        // Primeira chamada cria a relação
        var consumer = ConsumerAsset.Create("billing-service", "Service", "Production");
        var source = DiscoverySource.Create("KongGateway", "kong-gateway/billing", _now.AddDays(-1), 0.80m);
        apiAsset.MapConsumerRelationship(consumer, source, _now.AddDays(-1));

        _apiAssetRepository.GetByIdAsync(Arg.Any<ApiAssetId>(), Arg.Any<CancellationToken>()).Returns(apiAsset);

        var sut = CreateSut();
        var command = new SyncConsumersFeature.Command(
            [new SyncConsumersFeature.ConsumerSyncItem(
                apiAsset.Id.Value,
                "billing-service",
                "Service",
                "Production",
                "kong-gateway/billing-v2",
                0.95m)],
            "KongGateway",
            null);

        var result = await sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Created.Should().Be(0);
        result.Value.Updated.Should().Be(1);
        result.Value.Failed.Should().Be(0);
    }

    [Fact]
    public async Task SyncConsumers_Should_ReportFailure_When_ApiAssetNotFound()
    {
        _apiAssetRepository.GetByIdAsync(Arg.Any<ApiAssetId>(), Arg.Any<CancellationToken>())
            .Returns((ApiAsset?)null);

        var sut = CreateSut();
        var command = new SyncConsumersFeature.Command(
            [new SyncConsumersFeature.ConsumerSyncItem(
                Guid.NewGuid(),
                "billing-service",
                "Service",
                "Production",
                "ext-ref",
                0.90m)],
            "ExternalSystem",
            null);

        var result = await sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Failed.Should().Be(1);
        result.Value.Created.Should().Be(0);
        result.Value.Results.Should().ContainSingle(r =>
            r.Outcome == SyncConsumersFeature.SyncOutcome.Failed
            && r.ErrorCode == "CatalogGraph.ApiAsset.NotFound");
    }

    [Fact]
    public async Task SyncConsumers_Should_ReportFailure_When_ApiAssetDecommissioned()
    {
        var ownerService = ServiceAsset.Create("payments-service", "Finance", "Payments Team");
        var apiAsset = ApiAsset.Register("Payments API", "/api/payments", "1.0.0", "Internal", ownerService);
        apiAsset.Decommission();

        _apiAssetRepository.GetByIdAsync(Arg.Any<ApiAssetId>(), Arg.Any<CancellationToken>()).Returns(apiAsset);

        var sut = CreateSut();
        var command = new SyncConsumersFeature.Command(
            [new SyncConsumersFeature.ConsumerSyncItem(
                apiAsset.Id.Value,
                "billing-service",
                "Service",
                "Production",
                "ext-ref",
                0.90m)],
            "ExternalSystem",
            null);

        var result = await sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Failed.Should().Be(1);
        result.Value.Results.Should().ContainSingle(r =>
            r.Outcome == SyncConsumersFeature.SyncOutcome.Failed
            && r.ErrorCode == "CatalogGraph.ApiAsset.Decommissioned");
    }

    [Fact]
    public async Task SyncConsumers_Should_ProcessMixedBatch_With_SuccessesAndFailures()
    {
        var ownerService = ServiceAsset.Create("payments-service", "Finance", "Payments Team");
        var apiAsset = ApiAsset.Register("Payments API", "/api/payments", "1.0.0", "Internal", ownerService);
        var missingId = Guid.NewGuid();

        _apiAssetRepository.GetByIdAsync(
            Arg.Is<ApiAssetId>(id => id.Value == apiAsset.Id.Value),
            Arg.Any<CancellationToken>()).Returns(apiAsset);
        _apiAssetRepository.GetByIdAsync(
            Arg.Is<ApiAssetId>(id => id.Value == missingId),
            Arg.Any<CancellationToken>()).Returns((ApiAsset?)null);

        var sut = CreateSut();
        var command = new SyncConsumersFeature.Command(
            [
                new SyncConsumersFeature.ConsumerSyncItem(
                    apiAsset.Id.Value, "billing-service", "Service", "Production", "ref-1", 0.90m),
                new SyncConsumersFeature.ConsumerSyncItem(
                    missingId, "unknown-service", "Service", "Production", "ref-2", 0.80m),
                new SyncConsumersFeature.ConsumerSyncItem(
                    apiAsset.Id.Value, "checkout-service", "Service", "Production", "ref-3", 0.85m),
            ],
            "CICDPipeline",
            "batch-001");

        var result = await sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Created.Should().Be(2);
        result.Value.Failed.Should().Be(1);
        result.Value.Total.Should().Be(3);
        result.Value.CorrelationId.Should().Be("batch-001");
    }

    [Fact]
    public async Task SyncConsumers_Validator_Should_Reject_EmptyItems()
    {
        var validator = new SyncConsumersFeature.Validator();
        var command = new SyncConsumersFeature.Command([], "ExternalSystem", null);

        var validationResult = await validator.ValidateAsync(command);

        validationResult.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task SyncConsumers_Validator_Should_Reject_TooManyItems()
    {
        var validator = new SyncConsumersFeature.Validator();
        var items = Enumerable.Range(1, 101)
            .Select(i => new SyncConsumersFeature.ConsumerSyncItem(
                Guid.NewGuid(), $"consumer-{i}", "Service", "Production", $"ref-{i}", 0.90m))
            .ToList();
        var command = new SyncConsumersFeature.Command(items, "ExternalSystem", null);

        var validationResult = await validator.ValidateAsync(command);

        validationResult.IsValid.Should().BeFalse();
    }
}
