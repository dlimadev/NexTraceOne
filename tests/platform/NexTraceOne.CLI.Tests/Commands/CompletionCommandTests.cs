using System;
using System.IO;
using FluentAssertions;

namespace NexTraceOne.CLI.Tests.Commands;

/// <summary>
/// Testes do CompletionCommand — gera scripts de shell completion para bash, zsh, powershell.
/// </summary>
public sealed class CompletionCommandTests
{
    [Fact]
    public void CompletionCommand_Create_ReturnsCommandNamed_completion()
    {
        var command = NexTraceOne.CLI.Commands.CompletionCommand.Create();
        command.Name.Should().Be("completion");
    }

    [Fact]
    public void CompletionCommand_HasOneArgument_ForShell()
    {
        var command = NexTraceOne.CLI.Commands.CompletionCommand.Create();
        command.Arguments.Should().HaveCount(1);
        command.Arguments[0].Name.Should().Be("shell");
    }

    [Theory]
    [InlineData("bash")]
    [InlineData("zsh")]
    [InlineData("powershell")]
    public void CompletionCommand_OutputsScriptContainingCommandName_ForAllShells(string shell)
    {
        // We capture console output by invoking the Execute private method via the public command
        var output = InvokeCompletion(shell);
        output.Should().Contain("nex");
    }

    [Fact]
    public void CompletionCommand_BashOutput_ContainsKnownSubcommands()
    {
        var output = InvokeCompletion("bash");
        output.Should().Contain("catalog");
        output.Should().Contain("contract");
        output.Should().Contain("change");
        output.Should().Contain("incident");
        output.Should().Contain("report");
        output.Should().Contain("completion");
    }

    [Fact]
    public void CompletionCommand_ZshOutput_ContainsSubcommandDescriptions()
    {
        var output = InvokeCompletion("zsh");
        output.Should().Contain("catalog");
        output.Should().Contain("contract");
        output.Should().Contain("promote");
    }

    [Fact]
    public void CompletionCommand_PowerShellOutput_ContainsRegisterArgumentCompleter()
    {
        var output = InvokeCompletion("powershell");
        output.Should().Contain("Register-ArgumentCompleter");
        output.Should().Contain("promote");
        output.Should().Contain("dora");
        output.Should().Contain("changes-summary");
    }

    [Fact]
    public void CompletionCommand_BashOutput_ContainsPromoteSubcommand()
    {
        var output = InvokeCompletion("bash");
        output.Should().Contain("promote");
    }

    [Fact]
    public void CompletionCommand_BashOutput_ContainsDoraSubcommand()
    {
        var output = InvokeCompletion("bash");
        output.Should().Contain("dora");
        output.Should().Contain("changes-summary");
    }

    // ── Helper ─────────────────────────────────────────────────────────────────

    private static string InvokeCompletion(string shell)
    {
        var originalOut = Console.Out;
        using var sw = new StringWriter();
        Console.SetOut(sw);

        try
        {
            // Directly access the private Execute method via reflection
            var method = typeof(NexTraceOne.CLI.Commands.CompletionCommand)
                .GetMethod("Execute",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            method!.Invoke(null, [shell]);
            return sw.ToString();
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }
}
