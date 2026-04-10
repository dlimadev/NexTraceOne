using NexTraceOne.Catalog.Application.Templates.Abstractions;
using NexTraceOne.Catalog.Domain.Templates.Entities;
using NexTraceOne.Catalog.Domain.Templates.Enums;

using CreateServiceTemplateFeature = NexTraceOne.Catalog.Application.Templates.Features.CreateServiceTemplate.CreateServiceTemplate;
using ActivateServiceTemplateFeature = NexTraceOne.Catalog.Application.Templates.Features.ActivateServiceTemplate.ActivateServiceTemplate;
using DeactivateServiceTemplateFeature = NexTraceOne.Catalog.Application.Templates.Features.DeactivateServiceTemplate.DeactivateServiceTemplate;
using ScaffoldServiceFromTemplateFeature = NexTraceOne.Catalog.Application.Templates.Features.ScaffoldServiceFromTemplate.ScaffoldServiceFromTemplate;

namespace NexTraceOne.Catalog.Tests.Templates.Application.Features;

/// <summary>
/// Testes dos handlers de criação, ativação, desativação e scaffolding de templates
/// na camada Application do módulo Templates.
/// Utiliza NSubstitute em vez de repositório in-memory para isolar a camada Application.
/// Cobre CreateServiceTemplate, ActivateServiceTemplate, DeactivateServiceTemplate
/// e ScaffoldServiceFromTemplate.
/// </summary>
public sealed class ServiceTemplateFeatureTests
{
    // ── Helper: cria um template ativo para testes de scaffolding ──

    private static ServiceTemplate CreateActiveTemplate(string slug = "rest-api-template")
    {
        var template = ServiceTemplate.Create(
            slug: slug,
            displayName: "REST API Template",
            description: "Standard REST API template for microservices",
            version: "1.0.0",
            serviceType: TemplateServiceType.RestApi,
            language: TemplateLanguage.DotNet,
            defaultDomain: "Finance",
            defaultTeam: "Platform Team",
            tags: new[] { "rest", "api" },
            governancePolicyIds: null,
            baseContractSpec: """{"openapi":"3.0.0","info":{"title":"{{ServiceName}}","version":"1.0.0"}}""",
            scaffoldingManifestJson: """[{"Path":"src/{{ServiceName}}/Program.cs","Content":"// {{ServiceNamePascal}} entry point"}]""");

        template.Activate();
        return template;
    }

    // ── CreateServiceTemplate ─────────────────────────────────────────

    [Fact]
    public async Task CreateServiceTemplate_Should_ReturnResponse_When_SlugIsUnique()
    {
        var repository = Substitute.For<IServiceTemplateRepository>();
        var sut = new CreateServiceTemplateFeature.Handler(repository);

        repository.ExistsBySlugAsync("payment-api", Arg.Any<CancellationToken>())
            .Returns(false);

        var result = await sut.Handle(
            new CreateServiceTemplateFeature.Command(
                "payment-api", "Payment API", "Template for payment APIs", "1.0.0",
                TemplateServiceType.RestApi, TemplateLanguage.DotNet,
                "Finance", "Platform Team"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Slug.Should().Be("payment-api");
        result.Value.DisplayName.Should().Be("Payment API");
        result.Value.ServiceType.Should().Be(TemplateServiceType.RestApi);
        result.Value.Language.Should().Be(TemplateLanguage.DotNet);
        await repository.Received(1).AddAsync(Arg.Any<ServiceTemplate>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateServiceTemplate_Should_ReturnFailure_When_SlugAlreadyExists()
    {
        var repository = Substitute.For<IServiceTemplateRepository>();
        var sut = new CreateServiceTemplateFeature.Handler(repository);

        repository.ExistsBySlugAsync("existing-slug", Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await sut.Handle(
            new CreateServiceTemplateFeature.Command(
                "existing-slug", "Existing Template", "Already exists", "1.0.0",
                TemplateServiceType.RestApi, TemplateLanguage.DotNet,
                "Finance", "Team"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ServiceTemplate.DuplicateSlug");
        await repository.DidNotReceive().AddAsync(Arg.Any<ServiceTemplate>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateServiceTemplate_Validator_Should_Fail_When_SlugIsInvalidFormat()
    {
        var validator = new CreateServiceTemplateFeature.Validator();

        var validationResult = await validator.ValidateAsync(
            new CreateServiceTemplateFeature.Command(
                "INVALID_SLUG!", "Display", "Desc", "1.0.0",
                TemplateServiceType.RestApi, TemplateLanguage.DotNet,
                "Domain", "Team"));

        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(e => e.PropertyName == "Slug");
    }

    [Fact]
    public async Task CreateServiceTemplate_Validator_Should_Fail_When_DisplayNameIsEmpty()
    {
        var validator = new CreateServiceTemplateFeature.Validator();

        var validationResult = await validator.ValidateAsync(
            new CreateServiceTemplateFeature.Command(
                "valid-slug", "", "Desc", "1.0.0",
                TemplateServiceType.RestApi, TemplateLanguage.DotNet,
                "Domain", "Team"));

        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(e => e.PropertyName == "DisplayName");
    }

    // ── ActivateServiceTemplate ───────────────────────────────────────

    [Fact]
    public async Task ActivateServiceTemplate_Should_ReturnResponse_When_TemplateExists()
    {
        var repository = Substitute.For<IServiceTemplateRepository>();
        var sut = new ActivateServiceTemplateFeature.Handler(repository);

        var template = CreateActiveTemplate();
        template.Deactivate();

        repository.GetByIdAsync(template.Id.Value, Arg.Any<CancellationToken>())
            .Returns(template);

        var result = await sut.Handle(
            new ActivateServiceTemplateFeature.Command(template.Id.Value),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TemplateId.Should().Be(template.Id.Value);
        result.Value.IsActive.Should().BeTrue();
        await repository.Received(1).UpdateAsync(template, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ActivateServiceTemplate_Should_ReturnNotFound_When_TemplateDoesNotExist()
    {
        var repository = Substitute.For<IServiceTemplateRepository>();
        var sut = new ActivateServiceTemplateFeature.Handler(repository);

        repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((ServiceTemplate?)null);

        var result = await sut.Handle(
            new ActivateServiceTemplateFeature.Command(Guid.NewGuid()),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ServiceTemplate.NotFound");
        await repository.DidNotReceive().UpdateAsync(Arg.Any<ServiceTemplate>(), Arg.Any<CancellationToken>());
    }

    // ── DeactivateServiceTemplate ─────────────────────────────────────

    [Fact]
    public async Task DeactivateServiceTemplate_Should_ReturnResponse_When_TemplateExists()
    {
        var repository = Substitute.For<IServiceTemplateRepository>();
        var sut = new DeactivateServiceTemplateFeature.Handler(repository);

        var template = CreateActiveTemplate();

        repository.GetByIdAsync(template.Id.Value, Arg.Any<CancellationToken>())
            .Returns(template);

        var result = await sut.Handle(
            new DeactivateServiceTemplateFeature.Command(template.Id.Value),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TemplateId.Should().Be(template.Id.Value);
        result.Value.IsActive.Should().BeFalse();
        await repository.Received(1).UpdateAsync(template, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeactivateServiceTemplate_Should_ReturnNotFound_When_TemplateDoesNotExist()
    {
        var repository = Substitute.For<IServiceTemplateRepository>();
        var sut = new DeactivateServiceTemplateFeature.Handler(repository);

        repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((ServiceTemplate?)null);

        var result = await sut.Handle(
            new DeactivateServiceTemplateFeature.Command(Guid.NewGuid()),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ServiceTemplate.NotFound");
        await repository.DidNotReceive().UpdateAsync(Arg.Any<ServiceTemplate>(), Arg.Any<CancellationToken>());
    }

    // ── ScaffoldServiceFromTemplate ───────────────────────────────────

    [Fact]
    public async Task ScaffoldServiceFromTemplate_Should_ReturnPlan_When_TemplateIsActiveById()
    {
        var repository = Substitute.For<IServiceTemplateRepository>();
        var sut = new ScaffoldServiceFromTemplateFeature.Handler(repository);

        var template = CreateActiveTemplate();

        repository.GetByIdAsync(template.Id.Value, Arg.Any<CancellationToken>())
            .Returns(template);

        var result = await sut.Handle(
            new ScaffoldServiceFromTemplateFeature.Command(
                TemplateId: template.Id.Value,
                TemplateSlug: null,
                ServiceName: "checkout-service",
                TeamName: "Checkout Team",
                Domain: "Commerce"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ServiceName.Should().Be("checkout-service");
        result.Value.TemplateId.Should().Be(template.Id.Value);
        result.Value.Domain.Should().Be("Commerce");
        result.Value.TeamName.Should().Be("Checkout Team");
        result.Value.Files.Should().NotBeEmpty();
        await repository.Received(1).UpdateAsync(template, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ScaffoldServiceFromTemplate_Should_ReturnNotFound_When_TemplateDoesNotExist()
    {
        var repository = Substitute.For<IServiceTemplateRepository>();
        var sut = new ScaffoldServiceFromTemplateFeature.Handler(repository);

        repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((ServiceTemplate?)null);

        var result = await sut.Handle(
            new ScaffoldServiceFromTemplateFeature.Command(
                TemplateId: Guid.NewGuid(),
                TemplateSlug: null,
                ServiceName: "new-service"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ServiceTemplate.NotFound");
    }

    [Fact]
    public async Task ScaffoldServiceFromTemplate_Should_ReturnDisabled_When_TemplateIsInactive()
    {
        var repository = Substitute.For<IServiceTemplateRepository>();
        var sut = new ScaffoldServiceFromTemplateFeature.Handler(repository);

        var template = CreateActiveTemplate();
        template.Deactivate();

        repository.GetByIdAsync(template.Id.Value, Arg.Any<CancellationToken>())
            .Returns(template);

        var result = await sut.Handle(
            new ScaffoldServiceFromTemplateFeature.Command(
                TemplateId: template.Id.Value,
                TemplateSlug: null,
                ServiceName: "new-service"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ServiceTemplate.Disabled");
    }

    [Fact]
    public async Task ScaffoldServiceFromTemplate_Should_ResolveBySlug_When_TemplateSlugProvided()
    {
        var repository = Substitute.For<IServiceTemplateRepository>();
        var sut = new ScaffoldServiceFromTemplateFeature.Handler(repository);

        var template = CreateActiveTemplate("my-template");

        repository.GetBySlugAsync("my-template", Arg.Any<CancellationToken>())
            .Returns(template);

        var result = await sut.Handle(
            new ScaffoldServiceFromTemplateFeature.Command(
                TemplateId: null,
                TemplateSlug: "my-template",
                ServiceName: "new-service"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TemplateSlug.Should().Be("my-template");
    }

    [Fact]
    public async Task ScaffoldServiceFromTemplate_Validator_Should_Fail_When_ServiceNameIsInvalid()
    {
        var validator = new ScaffoldServiceFromTemplateFeature.Validator();

        var validationResult = await validator.ValidateAsync(
            new ScaffoldServiceFromTemplateFeature.Command(
                TemplateId: Guid.NewGuid(),
                TemplateSlug: null,
                ServiceName: "INVALID_NAME!"));

        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(e => e.PropertyName == "ServiceName");
    }

    [Fact]
    public async Task ScaffoldServiceFromTemplate_Validator_Should_Fail_When_NeitherIdNorSlug()
    {
        var validator = new ScaffoldServiceFromTemplateFeature.Validator();

        var validationResult = await validator.ValidateAsync(
            new ScaffoldServiceFromTemplateFeature.Command(
                TemplateId: null,
                TemplateSlug: null,
                ServiceName: "valid-service"));

        validationResult.IsValid.Should().BeFalse();
    }
}
