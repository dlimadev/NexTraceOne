using System.CommandLine;
using System.Text.Json;
using NexTraceOne.CLI.Models;
using NexTraceOne.CLI.Services;
using Spectre.Console;

namespace NexTraceOne.CLI.Commands;

/// <summary>
/// Comando 'nex validate' — valida manifestos de contrato offline.
/// Lê um ficheiro JSON e verifica estrutura, campos obrigatórios e regras de negócio.
/// </summary>
public static class ValidateCommand
{
    private const int ExitSuccess = 0;
    private const int ExitValidationFailed = 1;
    private const int ExitFileError = 2;

    private static readonly JsonSerializerOptions JsonReadOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static readonly JsonSerializerOptions JsonWriteOptions = new()
    {
        WriteIndented = true
    };

    public static Command Create()
    {
        var fileArgument = new Argument<FileInfo>("file")
        {
            Description = "Path to the contract manifest JSON file to validate."
        };

        var formatOption = new Option<string>("--format")
        {
            Description = "Output format: text (default) or json.",
            DefaultValueFactory = _ => "text"
        };

        var strictOption = new Option<bool>("--strict")
        {
            Description = "Treat warnings as errors (exit code 1 if any warnings found)."
        };

        var command = new Command("validate", "Validate a contract manifest file against NexTraceOne rules.");
        command.Add(fileArgument);
        command.Add(formatOption);
        command.Add(strictOption);

        command.SetAction((parseResult, cancellationToken) =>
        {
            var file = parseResult.GetValue(fileArgument)!;
            var format = parseResult.GetValue(formatOption) ?? "text";
            var strict = parseResult.GetValue(strictOption);

            return ExecuteAsync(file, format, strict, cancellationToken);
        });

        return command;
    }

    private static async Task<int> ExecuteAsync(FileInfo file, string format, bool strict, CancellationToken cancellationToken)
    {
        if (!file.Exists)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] File not found: [yellow]{file.FullName.EscapeMarkup()}[/]");
            return ExitFileError;
        }

        ContractManifest manifest;
        try
        {
            await using var stream = file.OpenRead();
            manifest = await JsonSerializer.DeserializeAsync<ContractManifest>(
                stream, JsonReadOptions, cancellationToken).ConfigureAwait(false)
                ?? new ContractManifest();
        }
        catch (JsonException ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] Invalid JSON in file: [yellow]{ex.Message.EscapeMarkup()}[/]");
            return ExitFileError;
        }

        var issues = ContractValidator.Validate(manifest);
        var summary = ValidationSummary.FromIssues(issues);

        var hasErrors = summary.ErrorCount > 0 || summary.BlockedCount > 0;
        var hasWarnings = summary.WarningCount > 0;
        var failed = hasErrors || (strict && hasWarnings);

        if (string.Equals(format, "json", StringComparison.OrdinalIgnoreCase))
        {
            RenderJson(issues, summary, strict);
        }
        else
        {
            RenderText(file, issues, summary, strict);
        }

        return failed ? ExitValidationFailed : ExitSuccess;
    }

    private static void RenderText(FileInfo file, IReadOnlyList<ValidationIssue> issues, ValidationSummary summary, bool strict)
    {
        AnsiConsole.MarkupLine($"\n[bold]Validating:[/] {file.Name.EscapeMarkup()}");

        if (issues.Count == 0)
        {
            AnsiConsole.MarkupLine("[green]✓ No issues found. Contract manifest is valid.[/]\n");
            return;
        }

        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn(new TableColumn("[bold]Severity[/]").Centered())
            .AddColumn(new TableColumn("[bold]Rule[/]"))
            .AddColumn(new TableColumn("[bold]Path[/]"))
            .AddColumn(new TableColumn("[bold]Message[/]"));

        foreach (var issue in issues)
        {
            var severityMarkup = issue.Severity switch
            {
                ValidationSeverity.Error => "[red]ERROR[/]",
                ValidationSeverity.Blocked => "[red bold]BLOCKED[/]",
                ValidationSeverity.Warning => "[yellow]WARNING[/]",
                ValidationSeverity.Hint => "[blue]HINT[/]",
                ValidationSeverity.Info => "[grey]INFO[/]",
                _ => issue.Severity.ToString()
            };

            table.AddRow(
                severityMarkup,
                issue.RuleId.EscapeMarkup(),
                issue.Path.EscapeMarkup(),
                issue.Message.EscapeMarkup());
        }

        AnsiConsole.Write(table);

        var summaryParts = new List<string>();
        if (summary.ErrorCount > 0) summaryParts.Add($"[red]{summary.ErrorCount} error(s)[/]");
        if (summary.BlockedCount > 0) summaryParts.Add($"[red]{summary.BlockedCount} blocked[/]");
        if (summary.WarningCount > 0) summaryParts.Add($"[yellow]{summary.WarningCount} warning(s)[/]");
        if (summary.HintCount > 0) summaryParts.Add($"[blue]{summary.HintCount} hint(s)[/]");
        if (summary.InfoCount > 0) summaryParts.Add($"[grey]{summary.InfoCount} info(s)[/]");

        AnsiConsole.MarkupLine($"\n[bold]Summary:[/] {string.Join(", ", summaryParts)}");

        if (strict && summary.WarningCount > 0 && summary.ErrorCount == 0 && summary.BlockedCount == 0)
        {
            AnsiConsole.MarkupLine("[yellow]⚠ Strict mode: warnings treated as errors.[/]");
        }

        var hasErrors = summary.ErrorCount > 0 || summary.BlockedCount > 0;
        var failed = hasErrors || (strict && summary.WarningCount > 0);

        AnsiConsole.MarkupLine(failed
            ? "[red]✗ Validation failed.[/]\n"
            : "[green]✓ Validation passed.[/]\n");
    }

    private static void RenderJson(IReadOnlyList<ValidationIssue> issues, ValidationSummary summary, bool strict)
    {
        var output = new
        {
            issues = issues.Select(i => new
            {
                ruleId = i.RuleId,
                ruleName = i.RuleName,
                severity = i.Severity.ToString().ToLowerInvariant(),
                message = i.Message,
                path = i.Path,
                suggestedFix = i.SuggestedFix
            }),
            summary = new
            {
                totalIssues = summary.TotalIssues,
                errors = summary.ErrorCount,
                warnings = summary.WarningCount,
                hints = summary.HintCount,
                infos = summary.InfoCount,
                blocked = summary.BlockedCount,
                isValid = summary.ErrorCount == 0 && summary.BlockedCount == 0 && (!strict || summary.WarningCount == 0)
            }
        };

        var json = JsonSerializer.Serialize(output, JsonWriteOptions);
        Console.WriteLine(json);
    }
}
