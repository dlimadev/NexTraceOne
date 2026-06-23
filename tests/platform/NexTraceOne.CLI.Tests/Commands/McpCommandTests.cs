using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;

namespace NexTraceOne.CLI.Tests.Commands;

/// <summary>
/// Testes do McpCommand: estrutura e geração de configuração MCP em ficheiro.
/// </summary>
public sealed class McpCommandTests
{
    [Fact]
    public void Create_Returns_Command_With_Tools_Configure_Call()
    {
        var command = NexTraceOne.CLI.Commands.McpCommand.Create();

        command.Name.Should().Be("mcp");
        command.Subcommands.Should().Contain(c => c.Name == "tools");
        command.Subcommands.Should().Contain(c => c.Name == "configure");
        command.Subcommands.Should().Contain(c => c.Name == "call");
    }

    [Fact]
    public async Task ConfigureMcpAsync_WritesConfigFile_ToCustomOutput()
    {
        var method = typeof(NexTraceOne.CLI.Commands.McpCommand)
            .GetMethod("ConfigureMcpAsync", BindingFlags.NonPublic | BindingFlags.Static);
        method.Should().NotBeNull();

        var outputPath = Path.Combine(Path.GetTempPath(), $"nex-mcp-{Guid.NewGuid()}.json");

        try
        {
            var task = (Task<int>)method!.Invoke(null, [
                "http://localhost:5000", null, "custom", outputPath, CancellationToken.None
            ])!;

            var exitCode = await task;

            exitCode.Should().Be(0); // ExitSuccess
            File.Exists(outputPath).Should().BeTrue();
            var content = await File.ReadAllTextAsync(outputPath);
            content.Should().Contain("/api/v1/ai/mcp");
        }
        finally
        {
            try { File.Delete(outputPath); } catch { /* best effort */ }
        }
    }
}
