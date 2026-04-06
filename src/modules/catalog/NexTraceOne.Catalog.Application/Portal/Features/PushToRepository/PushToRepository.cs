using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.Catalog.Application.Portal.Features.PushToRepository;

/// <summary>
/// Feature: PushToRepository — exportação de ficheiros scaffoldados para um repositório Git.
///
/// Fase 5 MVP: gera os comandos Git que o developer deve executar para fazer push dos ficheiros
/// gerados para o repositório alvo. Em versão futura, esta feature pode invocar directamente
/// os conectores de Integrations (GitHub/GitLab/Azure DevOps) para push automático.
///
/// O resultado inclui:
/// - Comandos Git prontos a copiar/executar
/// - Instrução para criar branch de feature se aplicável
/// - Indicação do número de ficheiros e dimensão estimada
///
/// Estrutura VSA: Command + Validator + Handler + Response em ficheiro único.
/// </summary>
public static class PushToRepository
{
    /// <summary>Comando para exportar ficheiros scaffoldados para repositório Git.</summary>
    public sealed record Command(
        string RepositoryUrl,
        string BranchName,
        IReadOnlyList<ExportFile> Files,
        string? CommitMessage = null,
        string? PullRequestTitle = null,
        GitProvider Provider = GitProvider.GitHub) : ICommand<Response>;

    /// <summary>Representa um ficheiro a exportar para o repositório.</summary>
    public sealed record ExportFile(
        string Path,
        string Content);

    /// <summary>Fornecedor de repositório Git.</summary>
    public enum GitProvider
    {
        GitHub,
        GitLab,
        AzureDevOps,
        Generic
    }

    /// <summary>Valida a entrada do comando.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.RepositoryUrl).NotEmpty().MaximumLength(500);
            RuleFor(x => x.BranchName)
                .NotEmpty()
                .MaximumLength(200)
                .Matches(@"^[a-zA-Z0-9/_.-]+$")
                .WithMessage("Branch name must contain only alphanumeric characters, slashes, underscores, hyphens and dots.");
            RuleFor(x => x.Files).NotEmpty().WithMessage("At least one file is required.");
            RuleFor(x => x.Files.Count).LessThanOrEqualTo(500).WithMessage("Maximum 500 files per push.");
            RuleFor(x => x.CommitMessage).MaximumLength(500).When(x => x.CommitMessage is not null);
            RuleFor(x => x.PullRequestTitle).MaximumLength(300).When(x => x.PullRequestTitle is not null);
        }
    }

    /// <summary>
    /// Handler que gera as instruções Git para push dos ficheiros para o repositório.
    /// Calcula estatísticas dos ficheiros e produz comandos prontos a executar.
    /// </summary>
    public sealed class Handler : ICommandHandler<Command, Response>
    {
        public Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            cancellationToken.ThrowIfCancellationRequested();

            var commitMessage = request.CommitMessage ?? BuildDefaultCommitMessage(request);
            var gitCommands = BuildGitCommands(request, commitMessage);
            var prInstructions = BuildPullRequestInstructions(request);
            var fileCount = request.Files.Count;
            var estimatedSizeKb = request.Files.Sum(f => f.Content.Length) / 1024;

            return Task.FromResult(Result<Response>.Success(new Response(
                RepositoryUrl: request.RepositoryUrl,
                BranchName: request.BranchName,
                Provider: request.Provider,
                FileCount: fileCount,
                EstimatedSizeKb: estimatedSizeKb,
                CommitMessage: commitMessage,
                GitCommands: gitCommands,
                PullRequestInstructions: prInstructions,
                GeneratedAt: DateTimeOffset.UtcNow)));
        }

        private static string BuildDefaultCommitMessage(Command request)
        {
            var fileCount = request.Files.Count;
            return request.PullRequestTitle is not null
                ? $"feat: {request.PullRequestTitle}"
                : $"feat: scaffold {fileCount} file{(fileCount == 1 ? "" : "s")} via NexTraceOne Service Creation Studio";
        }

        private static IReadOnlyList<string> BuildGitCommands(Command request, string commitMessage)
        {
            var cmds = new List<string>
            {
                $"git clone {request.RepositoryUrl} .",
                $"git checkout -b {request.BranchName}",
                "# Copy generated files to the repository directory",
            };

            foreach (var file in request.Files)
                cmds.Add($"# File: {file.Path} ({file.Content.Length} bytes)");

            cmds.Add($"git add .");
            cmds.Add($"git commit -m \"{EscapeQuotes(commitMessage)}\"");
            cmds.Add($"git push origin {request.BranchName}");

            return cmds.AsReadOnly();
        }

        private static string? BuildPullRequestInstructions(Command request)
        {
            if (request.PullRequestTitle is null)
                return null;

            return request.Provider switch
            {
                GitProvider.GitHub =>
                    $"gh pr create --title \"{EscapeQuotes(request.PullRequestTitle)}\" --body \"Generated by NexTraceOne Service Creation Studio\" --base main --head {request.BranchName}",
                GitProvider.GitLab =>
                    $"glab mr create --title \"{EscapeQuotes(request.PullRequestTitle)}\" --source-branch {request.BranchName} --target-branch main",
                GitProvider.AzureDevOps =>
                    $"az repos pr create --title \"{EscapeQuotes(request.PullRequestTitle)}\" --source-branch {request.BranchName} --target-branch main",
                _ =>
                    $"# Open a Pull Request: '{request.PullRequestTitle}' from {request.BranchName} → main"
            };
        }

        private static string EscapeQuotes(string s) => s.Replace("\"", "\\\"");
    }

    /// <summary>Resultado com instruções de push para o repositório.</summary>
    public sealed record Response(
        string RepositoryUrl,
        string BranchName,
        GitProvider Provider,
        int FileCount,
        int EstimatedSizeKb,
        string CommitMessage,
        IReadOnlyList<string> GitCommands,
        string? PullRequestInstructions,
        DateTimeOffset GeneratedAt);
}
