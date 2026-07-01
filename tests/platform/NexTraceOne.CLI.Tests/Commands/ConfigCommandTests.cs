using FluentAssertions;

namespace NexTraceOne.CLI.Tests.Commands;

/// <summary>
/// Testes de estrutura do ConfigCommand (set/get).
/// </summary>
public sealed class ConfigCommandTests
{
    [Fact]
    public void Create_Returns_Command_With_Set_And_Get()
    {
        var command = NexTraceOne.CLI.Commands.ConfigCommand.Create();

        command.Name.Should().Be("config");
        command.Subcommands.Should().Contain(c => c.Name == "set");
        command.Subcommands.Should().Contain(c => c.Name == "get");
    }
}
