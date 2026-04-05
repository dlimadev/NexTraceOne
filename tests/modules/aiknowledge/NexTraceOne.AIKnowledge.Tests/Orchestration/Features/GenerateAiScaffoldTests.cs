using Microsoft.Extensions.Logging;

using NexTraceOne.AIKnowledge.Application.Orchestration.Features.GenerateAiScaffold;
using NexTraceOne.AIKnowledge.Domain.ExternalAI.Ports;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Catalog.Contracts.Templates.ServiceInterfaces;

namespace NexTraceOne.AIKnowledge.Tests.Orchestration.Features;

/// <summary>
/// Testes unitários do handler GenerateAiScaffold.
/// Valida: geração de scaffold via IA, fallback quando provider indisponível,
/// template não encontrado, validação de comando, parsing de resposta JSON da IA.
/// </summary>
public sealed class GenerateAiScaffoldTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 4, 5, 12, 0, 0, TimeSpan.Zero);

    private readonly ICatalogTemplatesModule _catalogTemplates = Substitute.For<ICatalogTemplatesModule>();
    private readonly IExternalAIRoutingPort _routingPort = Substitute.For<IExternalAIRoutingPort>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly ILogger<GenerateAiScaffold.Handler> _logger = Substitute.For<ILogger<GenerateAiScaffold.Handler>>();

    private GenerateAiScaffold.Handler CreateHandler() =>
        new(_catalogTemplates, _routingPort, _currentUser, _dateTimeProvider, _logger);

    private static ServiceTemplateSummary CreateTestTemplate() =>
        new(
            TemplateId: Guid.NewGuid(),
            Slug: "dotnet-rest-api",
            DisplayName: ".NET REST API Template",
            Description: "Standard .NET REST API with OpenAPI contract.",
            Version: "1.0.0",
            ServiceType: "RestApi",
            Language: "DotNet",
            DefaultDomain: "platform",
            DefaultTeam: "platform-team",
            Tags: new[] { "dotnet", "rest" },
            BaseContractSpec: null,
            ScaffoldingManifestJson: null,
            RepositoryTemplateUrl: null);

    private static GenerateAiScaffold.Command CreateValidCommand(Guid? templateId = null) =>
        new(
            TemplateId: templateId ?? Guid.NewGuid(),
            TemplateSlug: null,
            ServiceName: "payment-api",
            ServiceDescription: "Payment processing service with CRUD for payments and refunds.",
            TeamName: "payments-team",
            Domain: "payments",
            LanguageOverride: null,
            MainEntities: "Payment, Refund",
            AdditionalRequirements: null,
            PreferredProvider: null);

    // ── Success scenarios ──────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ValidCommand_WithAiResponse_ShouldReturnGeneratedFiles()
    {
        var template = CreateTestTemplate();
        var command = CreateValidCommand(template.TemplateId);

        _dateTimeProvider.UtcNow.Returns(FixedNow);
        _currentUser.Id.Returns("user-123");
        _catalogTemplates.GetActiveTemplateAsync(command.TemplateId!.Value, Arg.Any<CancellationToken>())
            .Returns(template);

        const string aiResponse = """
        [
          {"path":"src/Controllers/PaymentController.cs","content":"using Microsoft.AspNetCore.Mvc;\n\n[ApiController]\n[Route(\"api/payments\")]\npublic class PaymentController { }"},
          {"path":"README.md","content":"# PaymentApi\n\nPayment processing service."}
        ]
        """;
        _routingPort.RouteQueryAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(),
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(aiResponse);

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ServiceName.Should().Be("payment-api");
        result.Value.TemplateSlug.Should().Be("dotnet-rest-api");
        result.Value.Language.Should().Be("DotNet");
        result.Value.Domain.Should().Be("payments");
        result.Value.TeamName.Should().Be("payments-team");
        result.Value.Files.Should().HaveCount(2);
        result.Value.Files[0].Path.Should().Be("src/Controllers/PaymentController.cs");
        result.Value.IsFallback.Should().BeFalse();
        result.Value.ScaffoldId.Should().NotBeEmpty();
        result.Value.GeneratedAt.Should().Be(FixedNow);
    }

    [Fact]
    public async Task Handle_WithTemplateSlug_ShouldResolveBySlug()
    {
        var template = CreateTestTemplate();
        var command = new GenerateAiScaffold.Command(
            TemplateId: null,
            TemplateSlug: "dotnet-rest-api",
            ServiceName: "order-api",
            ServiceDescription: "Order management service.",
            TeamName: null, Domain: null,
            LanguageOverride: null, MainEntities: null,
            AdditionalRequirements: null, PreferredProvider: null);

        _dateTimeProvider.UtcNow.Returns(FixedNow);
        _currentUser.Id.Returns("user-456");
        _catalogTemplates.GetActiveTemplateBySlugAsync("dotnet-rest-api", Arg.Any<CancellationToken>())
            .Returns(template);

        _routingPort.RouteQueryAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(),
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns("""[{"path":"README.md","content":"# OrderApi"}]""");

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ServiceName.Should().Be("order-api");
        result.Value.Domain.Should().Be("platform"); // Uses template default
        result.Value.TeamName.Should().Be("platform-team"); // Uses template default
    }

    [Fact]
    public async Task Handle_WithLanguageOverride_ShouldUseOverriddenLanguage()
    {
        var template = CreateTestTemplate();
        var command = new GenerateAiScaffold.Command(
            TemplateId: template.TemplateId,
            TemplateSlug: null,
            ServiceName: "billing-api",
            ServiceDescription: "Billing service.",
            TeamName: null, Domain: null,
            LanguageOverride: "Java",
            MainEntities: null, AdditionalRequirements: null,
            PreferredProvider: null);

        _dateTimeProvider.UtcNow.Returns(FixedNow);
        _currentUser.Id.Returns("user-789");
        _catalogTemplates.GetActiveTemplateAsync(template.TemplateId, Arg.Any<CancellationToken>())
            .Returns(template);
        _routingPort.RouteQueryAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(),
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns("""[{"path":"pom.xml","content":"<project></project>"}]""");

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Language.Should().Be("Java");
    }

    // ── Fallback scenarios ─────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenAiReturnsFallback_ShouldGenerateMinimalFiles()
    {
        var template = CreateTestTemplate();
        var command = CreateValidCommand(template.TemplateId);

        _dateTimeProvider.UtcNow.Returns(FixedNow);
        _currentUser.Id.Returns("user-123");
        _catalogTemplates.GetActiveTemplateAsync(command.TemplateId!.Value, Arg.Any<CancellationToken>())
            .Returns(template);
        _routingPort.RouteQueryAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(),
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns("[FALLBACK_PROVIDER_UNAVAILABLE] AI provider is unavailable.");

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsFallback.Should().BeTrue();
        result.Value.Files.Should().HaveCountGreaterThanOrEqualTo(2);
        result.Value.Files.Should().Contain(f => f.Path == "README.md");
        result.Value.Files.Should().Contain(f => f.Path == ".nextraceone.json");
    }

    // ── Error scenarios ────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_TemplateNotFound_ShouldReturnNotFoundError()
    {
        var command = CreateValidCommand();
        _catalogTemplates.GetActiveTemplateAsync(command.TemplateId!.Value, Arg.Any<CancellationToken>())
            .Returns((ServiceTemplateSummary?)null);

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Contain("TemplateNotFound");
    }

    [Fact]
    public async Task Handle_TemplateNotFoundBySlug_ShouldReturnNotFoundError()
    {
        var command = new GenerateAiScaffold.Command(
            TemplateId: null,
            TemplateSlug: "non-existent-template",
            ServiceName: "test-api",
            ServiceDescription: "Test.",
            TeamName: null, Domain: null,
            LanguageOverride: null, MainEntities: null,
            AdditionalRequirements: null, PreferredProvider: null);

        _catalogTemplates.GetActiveTemplateBySlugAsync("non-existent-template", Arg.Any<CancellationToken>())
            .Returns((ServiceTemplateSummary?)null);

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Contain("TemplateNotFound");
    }

    [Fact]
    public async Task Handle_AiProviderThrowsException_ShouldReturnBusinessError()
    {
        var template = CreateTestTemplate();
        var command = CreateValidCommand(template.TemplateId);

        _dateTimeProvider.UtcNow.Returns(FixedNow);
        _currentUser.Id.Returns("user-123");
        _catalogTemplates.GetActiveTemplateAsync(command.TemplateId!.Value, Arg.Any<CancellationToken>())
            .Returns(template);
        _routingPort.RouteQueryAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(),
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns<string>(_ => throw new InvalidOperationException("Connection refused"));

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Contain("Unavailable");
    }

    // ── JSON parsing edge cases ────────────────────────────────────────────

    [Fact]
    public async Task Handle_AiReturnsCodeFencedJson_ShouldParseCorrectly()
    {
        var template = CreateTestTemplate();
        var command = CreateValidCommand(template.TemplateId);

        _dateTimeProvider.UtcNow.Returns(FixedNow);
        _currentUser.Id.Returns("user-123");
        _catalogTemplates.GetActiveTemplateAsync(command.TemplateId!.Value, Arg.Any<CancellationToken>())
            .Returns(template);

        const string aiResponse = """
        ```json
        [{"path":"src/Program.cs","content":"var builder = WebApplication.CreateBuilder(args);"}]
        ```
        """;
        _routingPort.RouteQueryAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(),
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(aiResponse);

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Files.Should().HaveCount(1);
        result.Value.Files[0].Path.Should().Be("src/Program.cs");
    }

    [Fact]
    public async Task Handle_AiReturnsInvalidJson_ShouldReturnRawAsFallbackFile()
    {
        var template = CreateTestTemplate();
        var command = CreateValidCommand(template.TemplateId);

        _dateTimeProvider.UtcNow.Returns(FixedNow);
        _currentUser.Id.Returns("user-123");
        _catalogTemplates.GetActiveTemplateAsync(command.TemplateId!.Value, Arg.Any<CancellationToken>())
            .Returns(template);

        const string aiResponse = "This is not valid JSON at all";
        _routingPort.RouteQueryAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(),
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(aiResponse);

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Files.Should().HaveCount(1);
        result.Value.Files[0].Path.Should().Contain("SCAFFOLD_OUTPUT");
        result.Value.Files[0].Content.Should().Be(aiResponse);
    }

    // ── Validator tests ────────────────────────────────────────────────────

    [Fact]
    public async Task Validator_ValidCommand_ShouldPass()
    {
        var validator = new GenerateAiScaffold.Validator();
        var command = CreateValidCommand();

        var result = await validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validator_MissingTemplateIdAndSlug_ShouldFail()
    {
        var validator = new GenerateAiScaffold.Validator();
        var command = new GenerateAiScaffold.Command(
            TemplateId: null, TemplateSlug: null,
            ServiceName: "my-api", ServiceDescription: "Test.",
            TeamName: null, Domain: null,
            LanguageOverride: null, MainEntities: null,
            AdditionalRequirements: null, PreferredProvider: null);

        var result = await validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("UPPERCASE")]
    [InlineData("name with spaces")]
    [InlineData("name_underscores")]
    public async Task Validator_InvalidServiceName_ShouldFail(string serviceName)
    {
        var validator = new GenerateAiScaffold.Validator();
        var command = new GenerateAiScaffold.Command(
            TemplateId: Guid.NewGuid(), TemplateSlug: null,
            ServiceName: serviceName,
            ServiceDescription: "Test.",
            TeamName: null, Domain: null,
            LanguageOverride: null, MainEntities: null,
            AdditionalRequirements: null, PreferredProvider: null);

        var result = await validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Validator_EmptyDescription_ShouldFail()
    {
        var validator = new GenerateAiScaffold.Validator();
        var command = new GenerateAiScaffold.Command(
            TemplateId: Guid.NewGuid(), TemplateSlug: null,
            ServiceName: "my-api",
            ServiceDescription: "",
            TeamName: null, Domain: null,
            LanguageOverride: null, MainEntities: null,
            AdditionalRequirements: null, PreferredProvider: null);

        var result = await validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Validator_CommandWithSlugOnly_ShouldPass()
    {
        var validator = new GenerateAiScaffold.Validator();
        var command = new GenerateAiScaffold.Command(
            TemplateId: null, TemplateSlug: "my-template",
            ServiceName: "my-api",
            ServiceDescription: "A test service.",
            TeamName: null, Domain: null,
            LanguageOverride: null, MainEntities: null,
            AdditionalRequirements: null, PreferredProvider: null);

        var result = await validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }
}
