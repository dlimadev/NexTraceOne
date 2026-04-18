using System.Linq;

using Microsoft.Extensions.Logging;

using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;
using NexTraceOne.AIKnowledge.Infrastructure.Runtime.Tools;
using NexTraceOne.BuildingBlocks.Application.Abstractions;

namespace NexTraceOne.AIKnowledge.Tests.Runtime.Tools;

/// <summary>Testes unitários das tools reais: ListServicesInfoTool, GetServiceHealthTool, ListRecentChangesTool.</summary>
public sealed class RealToolTests
{
    [Fact]
    public async Task ListServicesInfoTool_ShouldExecuteSuccessfully()
    {
        var logger = Substitute.For<ILogger<ListServicesInfoTool>>();
        var tool = new ListServicesInfoTool(logger);

        tool.Definition.Name.Should().Be("list_services");
        tool.Definition.Category.Should().Be("service_catalog");

        var result = await tool.ExecuteAsync("{\"environment\":\"production\"}", CancellationToken.None);

        result.Success.Should().BeTrue();
        result.ToolName.Should().Be("list_services");
        result.Output.Should().Contain("list_services");
    }

    [Fact]
    public async Task ListServicesInfoTool_ShouldHandleEmptyArgs()
    {
        var logger = Substitute.For<ILogger<ListServicesInfoTool>>();
        var tool = new ListServicesInfoTool(logger);

        var result = await tool.ExecuteAsync("", CancellationToken.None);

        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task GetServiceHealthTool_ShouldExecuteSuccessfully()
    {
        var logger = Substitute.For<ILogger<GetServiceHealthTool>>();
        var tool = new GetServiceHealthTool(logger);

        tool.Definition.Name.Should().Be("get_service_health");
        tool.Definition.Category.Should().Be("operational_intelligence");

        var result = await tool.ExecuteAsync(
            "{\"service_name\":\"order-api\"}", CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Output.Should().Contain("order-api");
    }

    [Fact]
    public async Task GetServiceHealthTool_ShouldFail_WhenServiceNameMissing()
    {
        var logger = Substitute.For<ILogger<GetServiceHealthTool>>();
        var tool = new GetServiceHealthTool(logger);

        var result = await tool.ExecuteAsync("{}", CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("service_name");
    }

    [Fact]
    public async Task ListRecentChangesTool_ShouldExecuteSuccessfully()
    {
        var changeReader = Substitute.For<IChangeGroundingReader>();
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(DateTimeOffset.UtcNow);
        changeReader.FindRecentReleasesAsync(default, default, default, default, default, default, default)
            .ReturnsForAnyArgs(new List<ReleaseGroundingContext>().AsReadOnly());
        var logger = Substitute.For<ILogger<ListRecentChangesTool>>();
        var tool = new ListRecentChangesTool(changeReader, clock, logger);

        tool.Definition.Name.Should().Be("list_recent_changes");
        tool.Definition.Category.Should().Be("change_intelligence");

        var result = await tool.ExecuteAsync(
            "{\"service_name\":\"payment-svc\",\"days\":3}", CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Output.Should().Contain("list_recent_changes");
    }

    [Fact]
    public async Task ListRecentChangesTool_ShouldHandleInvalidJson()
    {
        var changeReader = Substitute.For<IChangeGroundingReader>();
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(DateTimeOffset.UtcNow);
        changeReader.FindRecentReleasesAsync(default, default, default, default, default, default, default)
            .ReturnsForAnyArgs(new List<ReleaseGroundingContext>().AsReadOnly());
        var logger = Substitute.For<ILogger<ListRecentChangesTool>>();
        var tool = new ListRecentChangesTool(changeReader, clock, logger);

        var result = await tool.ExecuteAsync("not-json", CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Output.Should().Contain("list_recent_changes");
        result.Output.Should().Contain("success");
    }

    [Fact]
    public void AllThreeTools_ShouldHaveUniqueNames()
    {
        var changeReader = Substitute.For<IChangeGroundingReader>();
        var clock = Substitute.For<IDateTimeProvider>();
        var tools = new IAgentTool[]
        {
            new ListServicesInfoTool(Substitute.For<ILogger<ListServicesInfoTool>>()),
            new GetServiceHealthTool(Substitute.For<ILogger<GetServiceHealthTool>>()),
            new ListRecentChangesTool(changeReader, clock, Substitute.For<ILogger<ListRecentChangesTool>>()),
        };

        var names = tools.Select(t => t.Definition.Name).ToList();
        names.Should().OnlyHaveUniqueItems();
        names.Should().HaveCount(3);
    }

    [Fact]
    public void AllThreeTools_ShouldHaveNonEmptyDescriptions()
    {
        var changeReader = Substitute.For<IChangeGroundingReader>();
        var clock = Substitute.For<IDateTimeProvider>();
        var tools = new IAgentTool[]
        {
            new ListServicesInfoTool(Substitute.For<ILogger<ListServicesInfoTool>>()),
            new GetServiceHealthTool(Substitute.For<ILogger<GetServiceHealthTool>>()),
            new ListRecentChangesTool(changeReader, clock, Substitute.For<ILogger<ListRecentChangesTool>>()),
        };

        foreach (var tool in tools)
        {
            tool.Definition.Description.Should().NotBeNullOrWhiteSpace();
            tool.Definition.Category.Should().NotBeNullOrWhiteSpace();
        }
    }
}
