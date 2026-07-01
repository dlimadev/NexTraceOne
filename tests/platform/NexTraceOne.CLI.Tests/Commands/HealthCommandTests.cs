using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;

namespace NexTraceOne.CLI.Tests.Commands;

/// <summary>
/// Testes do HealthCommand: estrutura e comportamento em servidor inalcançável.
/// </summary>
public sealed class HealthCommandTests
{
    [Fact]
    public void Create_Returns_Health_Command()
    {
        var command = NexTraceOne.CLI.Commands.HealthCommand.Create();

        command.Name.Should().Be("health");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenUnreachable_Returns_ExitUnreachable()
    {
        var method = typeof(NexTraceOne.CLI.Commands.HealthCommand)
            .GetMethod("CheckHealthAsync", BindingFlags.NonPublic | BindingFlags.Static);
        method.Should().NotBeNull();

        var task = (Task<int>)method!.Invoke(null, [
            "http://localhost:1", null, "text", CancellationToken.None
        ])!;

        var exitCode = await task;

        exitCode.Should().Be(2); // ExitUnreachable
    }
}
