using NexTraceOne.Catalog.Application.Portal.Features.PushToRepository;

namespace NexTraceOne.Catalog.Tests.Phase5Preview;

/// <summary>
/// Testes de unidade Phase 5 — Preview, Export &amp; Catalog Registration.
/// Cobre AutoRegisterScaffoldedService e PushToRepository.
/// </summary>
public sealed class Phase5PreviewTests
{
    // ─────────────────────────────────────────────────────────────────────────
    // PushToRepository — handler puro (sem dependências externas)
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task PushToRepository_GeneratesGitCommands_ForGitHub()
    {
        var handler = new PushToRepository.Handler();
        var command = new PushToRepository.Command(
            RepositoryUrl: "https://github.com/org/my-service",
            BranchName: "feat/scaffold-my-service",
            Files: [new PushToRepository.ExportFile("src/Program.cs", "// entry point")],
            CommitMessage: "feat: add scaffolded service",
            Provider: PushToRepository.GitProvider.GitHub);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.GitCommands.Should().NotBeEmpty();
        result.Value.GitCommands.Should().Contain(c => c.Contains("git clone"));
        result.Value.GitCommands.Should().Contain(c => c.Contains("git add"));
        result.Value.GitCommands.Should().Contain(c => c.Contains("git commit"));
        result.Value.GitCommands.Should().Contain(c => c.Contains("git push"));
        result.Value.BranchName.Should().Be("feat/scaffold-my-service");
        result.Value.FileCount.Should().Be(1);
    }

    [Fact]
    public async Task PushToRepository_GeneratesPrCommand_WhenTitleProvided_GitHub()
    {
        var handler = new PushToRepository.Handler();
        var command = new PushToRepository.Command(
            RepositoryUrl: "https://github.com/org/repo",
            BranchName: "feat/new-svc",
            Files: [new PushToRepository.ExportFile("Startup.cs", "code")],
            PullRequestTitle: "My Service Scaffold",
            Provider: PushToRepository.GitProvider.GitHub);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.PullRequestInstructions.Should().NotBeNull();
        result.Value.PullRequestInstructions.Should().Contain("gh pr create");
        result.Value.PullRequestInstructions.Should().Contain("My Service Scaffold");
    }

    [Fact]
    public async Task PushToRepository_GeneratesPrCommand_ForGitLab()
    {
        var handler = new PushToRepository.Handler();
        var command = new PushToRepository.Command(
            RepositoryUrl: "https://gitlab.com/org/repo",
            BranchName: "feat/svc",
            Files: [new PushToRepository.ExportFile("main.go", "package main")],
            PullRequestTitle: "New Service",
            Provider: PushToRepository.GitProvider.GitLab);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.PullRequestInstructions.Should().Contain("glab mr create");
    }

    [Fact]
    public async Task PushToRepository_GeneratesPrCommand_ForAzureDevOps()
    {
        var handler = new PushToRepository.Handler();
        var command = new PushToRepository.Command(
            RepositoryUrl: "https://dev.azure.com/org/project/_git/repo",
            BranchName: "feat/svc",
            Files: [new PushToRepository.ExportFile("pom.xml", "<project/>")],
            PullRequestTitle: "Java Service",
            Provider: PushToRepository.GitProvider.AzureDevOps);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.PullRequestInstructions.Should().Contain("az repos pr create");
    }

    [Fact]
    public async Task PushToRepository_NoPrInstructions_WhenTitleNotProvided()
    {
        var handler = new PushToRepository.Handler();
        var command = new PushToRepository.Command(
            RepositoryUrl: "https://github.com/org/repo",
            BranchName: "feat/no-pr",
            Files: [new PushToRepository.ExportFile("file.cs", "code")]);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.PullRequestInstructions.Should().BeNull();
    }

    [Fact]
    public async Task PushToRepository_DefaultCommitMessage_WhenNoneProvided()
    {
        var handler = new PushToRepository.Handler();
        var command = new PushToRepository.Command(
            RepositoryUrl: "https://github.com/org/repo",
            BranchName: "feat/default",
            Files:
            [
                new PushToRepository.ExportFile("a.cs", "x"),
                new PushToRepository.ExportFile("b.cs", "y"),
            ]);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.CommitMessage.Should().Contain("scaffold 2 files");
    }

    [Fact]
    public async Task PushToRepository_CalculatesEstimatedSize()
    {
        var handler = new PushToRepository.Handler();
        var content = new string('x', 2048); // 2 KB
        var command = new PushToRepository.Command(
            RepositoryUrl: "https://github.com/org/repo",
            BranchName: "feat/size",
            Files: [new PushToRepository.ExportFile("big.cs", content)]);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.EstimatedSizeKb.Should().Be(2);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // PushToRepository — Validator
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void PushToRepository_Validator_Fails_WhenRepositoryUrlEmpty()
    {
        var validator = new PushToRepository.Validator();
        var command = new PushToRepository.Command(
            RepositoryUrl: "",
            BranchName: "main",
            Files: [new PushToRepository.ExportFile("a.cs", "code")]);

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "RepositoryUrl");
    }

    [Fact]
    public void PushToRepository_Validator_Fails_WhenBranchNameHasInvalidChars()
    {
        var validator = new PushToRepository.Validator();
        var command = new PushToRepository.Command(
            RepositoryUrl: "https://github.com/org/repo",
            BranchName: "feat/invalid branch name!",
            Files: [new PushToRepository.ExportFile("a.cs", "code")]);

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "BranchName");
    }

    [Fact]
    public void PushToRepository_Validator_Fails_WhenNoFiles()
    {
        var validator = new PushToRepository.Validator();
        var command = new PushToRepository.Command(
            RepositoryUrl: "https://github.com/org/repo",
            BranchName: "main",
            Files: []);

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Files");
    }

    [Fact]
    public void PushToRepository_Validator_Passes_WithValidCommand()
    {
        var validator = new PushToRepository.Validator();
        var command = new PushToRepository.Command(
            RepositoryUrl: "https://github.com/org/repo",
            BranchName: "feat/valid-branch_1.0",
            Files:
            [
                new PushToRepository.ExportFile("src/Program.cs", "code"),
                new PushToRepository.ExportFile("README.md", "# Docs"),
            ],
            CommitMessage: "feat: initial scaffold",
            PullRequestTitle: "Add scaffolded service");

        var result = validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // AutoRegisterScaffoldedService — Validator
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void AutoRegisterScaffoldedService_Validator_Fails_WhenNameEmpty()
    {
        var validator = new Catalog.Application.Portal.Features.AutoRegisterScaffoldedService.AutoRegisterScaffoldedService.Validator();
        var command = new Catalog.Application.Portal.Features.AutoRegisterScaffoldedService.AutoRegisterScaffoldedService.Command(
            ServiceName: "",
            Domain: "commerce",
            TeamName: "checkout-team");

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ServiceName");
    }

    [Fact]
    public void AutoRegisterScaffoldedService_Validator_Fails_WhenDomainEmpty()
    {
        var validator = new Catalog.Application.Portal.Features.AutoRegisterScaffoldedService.AutoRegisterScaffoldedService.Validator();
        var command = new Catalog.Application.Portal.Features.AutoRegisterScaffoldedService.AutoRegisterScaffoldedService.Command(
            ServiceName: "PaymentService",
            Domain: "",
            TeamName: "payments");

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Domain");
    }

    [Fact]
    public void AutoRegisterScaffoldedService_Validator_Fails_WhenTeamNameEmpty()
    {
        var validator = new Catalog.Application.Portal.Features.AutoRegisterScaffoldedService.AutoRegisterScaffoldedService.Validator();
        var command = new Catalog.Application.Portal.Features.AutoRegisterScaffoldedService.AutoRegisterScaffoldedService.Command(
            ServiceName: "MyService",
            Domain: "platform",
            TeamName: "");

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TeamName");
    }

    [Fact]
    public void AutoRegisterScaffoldedService_Validator_Passes_WithMinimalCommand()
    {
        var validator = new Catalog.Application.Portal.Features.AutoRegisterScaffoldedService.AutoRegisterScaffoldedService.Validator();
        var command = new Catalog.Application.Portal.Features.AutoRegisterScaffoldedService.AutoRegisterScaffoldedService.Command(
            ServiceName: "UserService",
            Domain: "identity",
            TeamName: "platform-team");

        var result = validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void AutoRegisterScaffoldedService_Validator_Passes_WithAllFields()
    {
        var validator = new Catalog.Application.Portal.Features.AutoRegisterScaffoldedService.AutoRegisterScaffoldedService.Validator();
        var command = new Catalog.Application.Portal.Features.AutoRegisterScaffoldedService.AutoRegisterScaffoldedService.Command(
            ServiceName: "PaymentService",
            Domain: "finance",
            TeamName: "payment-team",
            Description: "Handles payment processing",
            ServiceType: "RestApi",
            Language: "DotNet",
            RepositoryUrl: "https://github.com/org/payment-service",
            DocumentationUrl: "https://docs.org/payment",
            TechnicalOwner: "jane.doe@org.com",
            BusinessOwner: "john.smith@org.com",
            ScaffoldId: Guid.NewGuid().ToString(),
            TemplateSlug: "dotnet-clean-rest-api");

        var result = validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }
}
