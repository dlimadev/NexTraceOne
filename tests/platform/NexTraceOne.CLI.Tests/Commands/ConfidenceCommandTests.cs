using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;

namespace NexTraceOne.CLI.Tests.Commands;

/// <summary>
/// Testes do ConfidenceCommand: estrutura e comportamento em servidor inalcançável.
/// </summary>
public sealed class ConfidenceCommandTests
{
    [Fact]
    public void Create_Returns_Command_With_Score()
    {
        var command = NexTraceOne.CLI.Commands.ConfidenceCommand.Create();

        command.Name.Should().Be("confidence");
        command.Subcommands.Should().Contain(c => c.Name == "score");
    }

    [Fact]
    public async Task FetchAndDisplayScoreAsync_WhenUnreachable_Returns_ExitError()
    {
        var method = typeof(NexTraceOne.CLI.Commands.ConfidenceCommand)
            .GetMethod("FetchAndDisplayScoreAsync", BindingFlags.NonPublic | BindingFlags.Static);
        method.Should().NotBeNull();

        var task = (Task<int>)method!.Invoke(null, [
            "release-123", "http://localhost:1", null, 0, "text", CancellationToken.None
        ])!;

        var exitCode = await task;

        exitCode.Should().Be(2); // ExitError
    }
}
