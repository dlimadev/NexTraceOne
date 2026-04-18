using System.CommandLine;
using Spectre.Console;

namespace NexTraceOne.CLI.Commands;

/// <summary>
/// Comando 'nex completion' — gera scripts de shell completion para bash, zsh e powershell.
/// Permite ao utilizador instalar completions automáticas no terminal, aumentando produtividade.
/// </summary>
public static class CompletionCommand
{
    public static Command Create()
    {
        var shellArg = new Argument<string>("shell")
        {
            Description = "Target shell: bash, zsh, or powershell."
        };

        var command = new Command("completion", "Generate shell completion scripts for nex CLI.")
        {
            shellArg
        };

        command.SetAction((parseResult, _) =>
        {
            var shell = parseResult.GetValue(shellArg) ?? string.Empty;
            return Task.FromResult(Execute(shell));
        });

        return command;
    }

    private static int Execute(string shell)
    {
        switch (shell.ToLowerInvariant())
        {
            case "bash":
                Console.WriteLine(BashCompletion);
                Console.Error.WriteLine();
                Console.Error.WriteLine("# To install: source <(nex completion bash)");
                Console.Error.WriteLine("# Or add to ~/.bashrc: nex completion bash >> ~/.nex_completion && source ~/.nex_completion");
                return 0;

            case "zsh":
                Console.WriteLine(ZshCompletion);
                Console.Error.WriteLine();
                Console.Error.WriteLine("# To install: source <(nex completion zsh)");
                Console.Error.WriteLine("# Or add to ~/.zshrc: nex completion zsh >> ~/.nex_completion && source ~/.nex_completion");
                return 0;

            case "powershell":
                Console.WriteLine(PowerShellCompletion);
                Console.Error.WriteLine();
                Console.Error.WriteLine("# To install: nex completion powershell | Out-String | Invoke-Expression");
                Console.Error.WriteLine("# Or add to $PROFILE: nex completion powershell >> $PROFILE");
                return 0;

            default:
                AnsiConsole.MarkupLine($"[red]Error:[/] Unknown shell [yellow]{shell.EscapeMarkup()}[/]. Supported: bash, zsh, powershell.");
                return 1;
        }
    }

    // ── Bash completion script ─────────────────────────────────────────────────

    private const string BashCompletion = """
        # NexTraceOne CLI bash completion
        _nex_completion() {
            local cur prev words
            COMPREPLY=()
            cur="${COMP_WORDS[COMP_CWORD]}"
            prev="${COMP_WORDS[COMP_CWORD-1]}"
            words="${COMP_WORDS[*]}"

            local commands="validate catalog contract change incident health config mcp report completion"
            local catalog_commands="list get"
            local contract_commands="list verify diff changelog sync"
            local change_commands="report blast-radius list promote"
            local incident_commands="list get report"
            local config_commands="set get"
            local mcp_commands="tools configure call"
            local report_commands="dora changes-summary"
            local completion_shells="bash zsh powershell"

            case "${COMP_WORDS[1]}" in
                catalog)   COMPREPLY=( $(compgen -W "${catalog_commands}" -- "${cur}") )   ;;
                contract)  COMPREPLY=( $(compgen -W "${contract_commands}" -- "${cur}") )  ;;
                change)    COMPREPLY=( $(compgen -W "${change_commands}" -- "${cur}") )    ;;
                incident)  COMPREPLY=( $(compgen -W "${incident_commands}" -- "${cur}") )  ;;
                config)    COMPREPLY=( $(compgen -W "${config_commands}" -- "${cur}") )    ;;
                mcp)       COMPREPLY=( $(compgen -W "${mcp_commands}" -- "${cur}") )       ;;
                report)    COMPREPLY=( $(compgen -W "${report_commands}" -- "${cur}") )    ;;
                completion)COMPREPLY=( $(compgen -W "${completion_shells}" -- "${cur}") )  ;;
                *)         COMPREPLY=( $(compgen -W "${commands}" -- "${cur}") )           ;;
            esac
        }
        complete -F _nex_completion nex
        """;

    // ── Zsh completion script ──────────────────────────────────────────────────

    private const string ZshCompletion = """
        # NexTraceOne CLI zsh completion
        #compdef nex

        _nex() {
            local state

            _arguments \
                '1: :->command' \
                '*: :->args'

            case $state in
                command)
                    _values 'commands' \
                        'validate[Validate a contract manifest file]' \
                        'catalog[Query the NexTraceOne service catalog]' \
                        'contract[Contract compliance commands]' \
                        'change[Report, inspect and list change records]' \
                        'incident[List, inspect and report incidents]' \
                        'health[Check server connectivity and health]' \
                        'config[Manage CLI configuration]' \
                        'mcp[Interact with the MCP server]' \
                        'report[Generate operational reports]' \
                        'completion[Generate shell completion scripts]'
                    ;;
                args)
                    case ${words[2]} in
                        catalog)
                            _values 'subcommands' 'list[List all services]' 'get[Get service details]'
                            ;;
                        contract)
                            _values 'subcommands' \
                                'list[List contracts]' \
                                'verify[Verify contract compliance]' \
                                'diff[Show diff between versions]' \
                                'changelog[Show contract changelog]' \
                                'sync[Sync contract to NexTraceOne]'
                            ;;
                        change)
                            _values 'subcommands' \
                                'report[Report a new change]' \
                                'blast-radius[Analyse blast radius]' \
                                'list[List change records]' \
                                'promote[Promote a release to the next environment]'
                            ;;
                        incident)
                            _values 'subcommands' 'list[List incidents]' 'get[Get incident details]' 'report[Report an incident]'
                            ;;
                        config)
                            _values 'subcommands' 'set[Set a configuration value]' 'get[Get a configuration value]'
                            ;;
                        mcp)
                            _values 'subcommands' 'tools[List MCP tools]' 'configure[Generate MCP config]' 'call[Invoke an MCP tool]'
                            ;;
                        report)
                            _values 'subcommands' \
                                'dora[DORA metrics report]' \
                                'changes-summary[Changes summary report]'
                            ;;
                        completion)
                            _values 'shells' 'bash' 'zsh' 'powershell'
                            ;;
                    esac
                    ;;
            esac
        }

        _nex "$@"
        """;

    // ── PowerShell completion script ───────────────────────────────────────────

    private const string PowerShellCompletion = """
        # NexTraceOne CLI PowerShell completion
        Register-ArgumentCompleter -Native -CommandName nex -ScriptBlock {
            param($wordToComplete, $commandAst, $cursorPosition)

            $commands = @('validate','catalog','contract','change','incident','health','config','mcp','report','completion')
            $subcommands = @{
                'catalog'    = @('list','get')
                'contract'   = @('list','verify','diff','changelog','sync')
                'change'     = @('report','blast-radius','list','promote')
                'incident'   = @('list','get','report')
                'config'     = @('set','get')
                'mcp'        = @('tools','configure','call')
                'report'     = @('dora','changes-summary')
                'completion' = @('bash','zsh','powershell')
            }

            $tokens = $commandAst.CommandElements
            if ($tokens.Count -ge 2) {
                $rootCmd = $tokens[1].Value
                if ($subcommands.ContainsKey($rootCmd)) {
                    return $subcommands[$rootCmd] | Where-Object { $_ -like "$wordToComplete*" } |
                        ForEach-Object { [System.Management.Automation.CompletionResult]::new($_, $_, 'ParameterValue', $_) }
                }
            }

            return $commands | Where-Object { $_ -like "$wordToComplete*" } |
                ForEach-Object { [System.Management.Automation.CompletionResult]::new($_, $_, 'ParameterValue', $_) }
        }
        """;
}
