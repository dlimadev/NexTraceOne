using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;

namespace NexTraceOne.CLI.Tests.Commands;

/// <summary>
/// Testes do ComplianceCommand: estrutura e comportamento em servidor inalcançável.
/// </summary>
public sealed class ComplianceCommandTests
{
    [Fact]
    public void Create_Returns_Command_With_Check()
    {
        var command = NexTraceOne.CLI.Commands.ComplianceCommand.Create();

        command.Name.Should().Be("compliance");
        command.Subcommands.Should().Contain(c => c.Name == "check");
    }

    [Fact]
    public async Task FetchAndDisplayCoverageAsync_WhenUnreachable_Returns_ExitError()
    {
        var method = typeof(NexTraceOne.CLI.Commands.ComplianceCommand)
            .GetMethod("FetchAndDisplayCoverageAsync", BindingFlags.NonPublic | BindingFlags.Static);
        method.Should().NotBeNull();

        var task = (Task<int>)method!.Invoke(null, [
            "SOC2", "http://localhost:1", null, "text", CancellationToken.None
        ])!;

        var exitCode = await task;

        exitCode.Should().Be(2); // ExitError
    }
}
