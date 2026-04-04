using FluentAssertions;

using NexTraceOne.Catalog.Application.Templates.Features.CreateServiceTemplate;
using NexTraceOne.Catalog.Application.Templates.Features.GetServiceTemplate;
using NexTraceOne.Catalog.Application.Templates.Features.ListServiceTemplates;
using NexTraceOne.Catalog.Application.Templates.Features.ScaffoldServiceFromTemplate;
using NexTraceOne.Catalog.Domain.Templates.Enums;

using Xunit;

namespace NexTraceOne.Catalog.Tests.Templates;

/// <summary>
/// Testes unitários para as features de Service Templates &amp; Scaffolding (Phase 3.1):
///   - CreateServiceTemplate: criação com validação de slug único
///   - GetServiceTemplate: resolução por id e por slug
///   - ListServiceTemplates: listagem com filtros
///   - ScaffoldServiceFromTemplate: geração de plano de scaffolding com variáveis
/// </summary>
public sealed class ServiceTemplateTests
{
    private readonly InMemoryServiceTemplateRepository _repository = new();

    // ── CreateServiceTemplate ──────────────────────────────────────────────

    [Fact]
    public async Task CreateServiceTemplate_ValidCommand_ShouldPersistAndReturnTemplate()
    {
        var handler = new CreateServiceTemplate.Handler(_repository);
        var command = new CreateServiceTemplate.Command(
            Slug: "my-new-api",
            DisplayName: "My New API",
            Description: "Test template for unit tests.",
            Version: "1.0.0",
            ServiceType: TemplateServiceType.RestApi,
            Language: TemplateLanguage.DotNet,
            DefaultDomain: "core",
            DefaultTeam: "backend-team",
            Tags: new[] { "test" }.ToList().AsReadOnly());

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Slug.Should().Be("my-new-api");
        result.Value.TemplateId.Should().NotBeEmpty();
        result.Value.ServiceType.Should().Be(TemplateServiceType.RestApi);
        result.Value.Language.Should().Be(TemplateLanguage.DotNet);
    }

    [Fact]
    public async Task CreateServiceTemplate_DuplicateSlug_ShouldReturnConflict()
    {
        var handler = new CreateServiceTemplate.Handler(_repository);
        var command = new CreateServiceTemplate.Command(
            Slug: "dotnet-rest-api", // Already exists in seed
            DisplayName: "Duplicate",
            Description: "Should fail.",
            Version: "1.0.0",
            ServiceType: TemplateServiceType.RestApi,
            Language: TemplateLanguage.DotNet,
            DefaultDomain: "platform",
            DefaultTeam: "team");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Contain("DuplicateSlug");
    }

    [Theory]
    [InlineData("")]
    [InlineData("Invalid Slug")]
    [InlineData("UPPERCASE")]
    [InlineData("slug with spaces")]
    public async Task CreateServiceTemplate_Validator_InvalidSlug_ShouldFail(string slug)
    {
        var validator = new CreateServiceTemplate.Validator();
        var command = new CreateServiceTemplate.Command(
            Slug: slug,
            DisplayName: "Test",
            Description: "Test.",
            Version: "1.0.0",
            ServiceType: TemplateServiceType.RestApi,
            Language: TemplateLanguage.DotNet,
            DefaultDomain: "core",
            DefaultTeam: "team");

        var result = await validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task CreateServiceTemplate_Validator_ValidSlug_ShouldPass()
    {
        var validator = new CreateServiceTemplate.Validator();
        var command = new CreateServiceTemplate.Command(
            Slug: "valid-kebab-case-slug",
            DisplayName: "Test Template",
            Description: "A valid description.",
            Version: "2.1.0",
            ServiceType: TemplateServiceType.EventDriven,
            Language: TemplateLanguage.NodeJs,
            DefaultDomain: "events",
            DefaultTeam: "event-team");

        var result = await validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    // ── GetServiceTemplate ──────────────────────────────────────────────────

    [Fact]
    public async Task GetServiceTemplate_BySlug_ExistingSlug_ShouldReturnTemplate()
    {
        var handler = new GetServiceTemplate.Handler(_repository);

        var result = await handler.Handle(
            new GetServiceTemplate.Query(null, "dotnet-rest-api"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Slug.Should().Be("dotnet-rest-api");
        result.Value.ServiceType.Should().Be(TemplateServiceType.RestApi);
        result.Value.Language.Should().Be(TemplateLanguage.DotNet);
        result.Value.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task GetServiceTemplate_BySlug_UnknownSlug_ShouldReturnNotFound()
    {
        var handler = new GetServiceTemplate.Handler(_repository);

        var result = await handler.Handle(
            new GetServiceTemplate.Query(null, "non-existent-template"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Contain("NotFound");
    }

    [Fact]
    public async Task GetServiceTemplate_ById_ExistingId_ShouldReturnTemplate()
    {
        var handler = new GetServiceTemplate.Handler(_repository);
        var existing = _repository.GetFirstTemplate();

        var result = await handler.Handle(
            new GetServiceTemplate.Query(existing.Id.Value, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TemplateId.Should().Be(existing.Id.Value);
    }

    [Fact]
    public async Task GetServiceTemplate_Validator_NeitherIdNorSlug_ShouldFail()
    {
        var validator = new GetServiceTemplate.Validator();

        var result = await validator.ValidateAsync(new GetServiceTemplate.Query(null, null));

        result.IsValid.Should().BeFalse();
    }

    // ── ListServiceTemplates ────────────────────────────────────────────────

    [Fact]
    public async Task ListServiceTemplates_NoFilters_ShouldReturnAllSeedTemplates()
    {
        var handler = new ListServiceTemplates.Handler(_repository);

        var result = await handler.Handle(new ListServiceTemplates.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Total.Should().BeGreaterThanOrEqualTo(3); // 3 seed templates
        result.Value.Items.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ListServiceTemplates_FilterByLanguage_ShouldReturnOnlyMatchingTemplates()
    {
        var handler = new ListServiceTemplates.Handler(_repository);

        var result = await handler.Handle(
            new ListServiceTemplates.Query(Language: TemplateLanguage.NodeJs),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().AllSatisfy(t => t.Language.Should().Be(TemplateLanguage.NodeJs));
    }

    [Fact]
    public async Task ListServiceTemplates_FilterByServiceType_ShouldReturnOnlyMatchingTemplates()
    {
        var handler = new ListServiceTemplates.Handler(_repository);

        var result = await handler.Handle(
            new ListServiceTemplates.Query(ServiceType: TemplateServiceType.EventDriven),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().AllSatisfy(t => t.ServiceType.Should().Be(TemplateServiceType.EventDriven));
    }

    [Fact]
    public async Task ListServiceTemplates_SearchByName_ShouldReturnMatchingTemplates()
    {
        var handler = new ListServiceTemplates.Handler(_repository);

        var result = await handler.Handle(
            new ListServiceTemplates.Query(Search: "dotnet"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().NotBeEmpty();
    }

    // ── ScaffoldServiceFromTemplate ────────────────────────────────────────

    [Fact]
    public async Task ScaffoldServiceFromTemplate_BySlug_ValidCommand_ShouldReturnScaffoldingPlan()
    {
        var handler = new ScaffoldServiceFromTemplate.Handler(_repository);
        var command = new ScaffoldServiceFromTemplate.Command(
            TemplateId: null,
            TemplateSlug: "dotnet-rest-api",
            ServiceName: "my-payment-api",
            TeamName: "payments-team",
            Domain: "payments");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ServiceName.Should().Be("my-payment-api");
        result.Value.TemplateSlug.Should().Be("dotnet-rest-api");
        result.Value.Domain.Should().Be("payments");
        result.Value.TeamName.Should().Be("payments-team");
        result.Value.Files.Should().NotBeEmpty();
        result.Value.ScaffoldingId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ScaffoldServiceFromTemplate_VariablesAreSubstituted_ShouldReplaceInFiles()
    {
        var handler = new ScaffoldServiceFromTemplate.Handler(_repository);
        var command = new ScaffoldServiceFromTemplate.Command(
            TemplateId: null,
            TemplateSlug: "dotnet-rest-api",
            ServiceName: "order-service",
            TeamName: "orders-team",
            Domain: "commerce");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        // README.md deve ter o nome do serviço substituído
        var readme = result.Value.Files.FirstOrDefault(f => f.Path == "README.md");
        readme.Should().NotBeNull();
        readme!.Content.Should().Contain("order-service");
        readme.Content.Should().NotContain("{{ServiceName}}");
    }

    [Fact]
    public async Task ScaffoldServiceFromTemplate_DisabledTemplate_ShouldReturnError()
    {
        // Desativar o template antes do teste
        var template = _repository.GetFirstTemplate();
        template.Deactivate();

        var handler = new ScaffoldServiceFromTemplate.Handler(_repository);
        var command = new ScaffoldServiceFromTemplate.Command(
            TemplateId: template.Id.Value,
            TemplateSlug: null,
            ServiceName: "some-service");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Contain("Disabled");

        // Reativar para não afetar outros testes
        template.Activate();
    }

    [Fact]
    public async Task ScaffoldServiceFromTemplate_UnknownTemplate_ShouldReturnNotFound()
    {
        var handler = new ScaffoldServiceFromTemplate.Handler(_repository);
        var command = new ScaffoldServiceFromTemplate.Command(
            TemplateId: null,
            TemplateSlug: "non-existent-slug",
            ServiceName: "some-service");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Contain("NotFound");
    }

    [Theory]
    [InlineData("Invalid Name")]
    [InlineData("UPPERCASE")]
    [InlineData("name_with_underscores")]
    public async Task ScaffoldServiceFromTemplate_Validator_InvalidServiceName_ShouldFail(string serviceName)
    {
        var validator = new ScaffoldServiceFromTemplate.Validator();
        var command = new ScaffoldServiceFromTemplate.Command(
            TemplateId: Guid.NewGuid(),
            TemplateSlug: null,
            ServiceName: serviceName);

        var result = await validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task ScaffoldServiceFromTemplate_Validator_ValidKebabCaseServiceName_ShouldPass()
    {
        var validator = new ScaffoldServiceFromTemplate.Validator();
        var command = new ScaffoldServiceFromTemplate.Command(
            TemplateId: Guid.NewGuid(),
            TemplateSlug: null,
            ServiceName: "valid-service-name");

        var result = await validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }
}
