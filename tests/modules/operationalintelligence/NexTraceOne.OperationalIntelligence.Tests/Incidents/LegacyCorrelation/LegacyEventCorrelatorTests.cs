using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.LegacyAssets.Features.ListLegacyAssets;
using NexTraceOne.OperationalIntelligence.Infrastructure.Incidents.Services;

namespace NexTraceOne.OperationalIntelligence.Tests.Incidents.LegacyCorrelation;

/// <summary>
/// Testes unitários do LegacyEventCorrelator.
/// Valida que o correlator de eventos legacy consulta o catálogo e retorna
/// resultados adequados para job, queue, system e program.
/// </summary>
public sealed class LegacyEventCorrelatorTests
{
    private readonly ISender _sender;
    private readonly LegacyEventCorrelator _correlator;

    public LegacyEventCorrelatorTests()
    {
        _sender = Substitute.For<ISender>();
        _correlator = new LegacyEventCorrelator(_sender, NullLogger<LegacyEventCorrelator>.Instance);
    }

    [Fact]
    public async Task CorrelateByJobName_WithMatchingAsset_ReturnsCorrelated()
    {
        SetupCatalogResponse("BATCHJOB1", "BatchJob", "BATCHJOB1", "Batch Job 1", "team-legacy");

        var result = await _correlator.CorrelateByJobNameAsync("BATCHJOB1", "SYS1", CancellationToken.None);

        result.IsCorrelated.Should().BeTrue();
        result.AssetName.Should().Be("BATCHJOB1");
        result.MatchMethod.Should().Be("JobName");
    }

    [Fact]
    public async Task CorrelateByJobName_NoMatch_ReturnsNotCorrelated()
    {
        SetupEmptyCatalogResponse();

        var result = await _correlator.CorrelateByJobNameAsync("UNKNOWN_JOB", "SYS1", CancellationToken.None);

        result.IsCorrelated.Should().BeFalse();
        result.MatchMethod.Should().Be("JobName");
    }

    [Fact]
    public async Task CorrelateByQueue_WithMatchingAsset_ReturnsCorrelated()
    {
        SetupCatalogResponse("ORDERS.IN", "MqQueue", "ORDERS.IN", "Orders Inbound Queue", "team-mq");

        var result = await _correlator.CorrelateByQueueAsync("QM1", "ORDERS.IN", CancellationToken.None);

        result.IsCorrelated.Should().BeTrue();
        result.AssetName.Should().Be("ORDERS.IN");
        result.MatchMethod.Should().Be("QueueName");
    }

    [Fact]
    public async Task CorrelateBySystem_WithMatchingAsset_ReturnsCorrelated()
    {
        SetupCatalogResponse("SYSPROD1", "MainframeSystem", "SYSPROD1", "Production System 1", "team-mainframe");

        var result = await _correlator.CorrelateBySystemNameAsync("SYSPROD1", CancellationToken.None);

        result.IsCorrelated.Should().BeTrue();
        result.AssetName.Should().Be("SYSPROD1");
        result.MatchMethod.Should().Be("SystemName");
    }

    [Fact]
    public async Task CorrelateByProgram_WithMatchingAsset_ReturnsCorrelated()
    {
        SetupCatalogResponse("PGMCALC", "CobolProgram", "PGMCALC", "Calculation Program", "team-cobol");

        var result = await _correlator.CorrelateByProgramNameAsync("PGMCALC", "SYS1", CancellationToken.None);

        result.IsCorrelated.Should().BeTrue();
        result.AssetName.Should().Be("PGMCALC");
        result.MatchMethod.Should().Be("ProgramName");
    }

    private void SetupCatalogResponse(
        string name, string assetType, string displayName, string teamName, string team)
    {
        var items = new List<ListLegacyAssets.LegacyAssetSummaryDto>
        {
            new(Guid.NewGuid(), assetType, name, displayName, team, "mainframe", "High", "Active")
        };
        var response = new ListLegacyAssets.Response(items);

        _sender.Send(Arg.Any<ListLegacyAssets.Query>(), Arg.Any<CancellationToken>())
            .Returns(Result<ListLegacyAssets.Response>.Success(response));
    }

    private void SetupEmptyCatalogResponse()
    {
        var response = new ListLegacyAssets.Response(new List<ListLegacyAssets.LegacyAssetSummaryDto>());

        _sender.Send(Arg.Any<ListLegacyAssets.Query>(), Arg.Any<CancellationToken>())
            .Returns(Result<ListLegacyAssets.Response>.Success(response));
    }
}
