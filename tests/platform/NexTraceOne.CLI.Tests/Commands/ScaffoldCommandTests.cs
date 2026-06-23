using FluentAssertions;

namespace NexTraceOne.CLI.Tests.Commands;

/// <summary>
/// Testes de estrutura do ScaffoldCommand (service/templates/init/register).
/// </summary>
public sealed class ScaffoldCommandTests
{
    [Fact]
    public void Create_Returns_Command_With_Expected_Subcommands()
    {
        var command = NexTraceOne.CLI.Commands.ScaffoldCommand.Create();

        command.Name.Should().Be("scaffold");
        command.Subcommands.Should().Contain(c => c.Name == "service");
        command.Subcommands.Should().Contain(c => c.Name == "templates");
        command.Subcommands.Should().Contain(c => c.Name == "init");
        command.Subcommands.Should().Contain(c => c.Name == "register");
    }
}
