using Microsoft.Extensions.Logging.Abstractions;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Core.Enums;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Application.Portal.Abstractions;
using NexTraceOne.Catalog.Application.Templates.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Portal.Enums;
using NexTraceOne.Catalog.Domain.Templates.Entities;
using NexTraceOne.Catalog.Domain.Templates.Enums;

using ActivateServiceTemplateFeature = NexTraceOne.Catalog.Application.Templates.Features.ActivateServiceTemplate.ActivateServiceTemplate;
using ClassifyBreakingChangeFeature = NexTraceOne.Catalog.Application.Contracts.Features.ClassifyBreakingChange.ClassifyBreakingChange;
using CreateServiceTemplateFeature = NexTraceOne.Catalog.Application.Templates.Features.CreateServiceTemplate.CreateServiceTemplate;
using DeactivateServiceTemplateFeature = NexTraceOne.Catalog.Application.Templates.Features.DeactivateServiceTemplate.DeactivateServiceTemplate;
using EvaluateContractRulesFeature = NexTraceOne.Catalog.Application.Contracts.Features.EvaluateContractRules.EvaluateContractRules;
using EvaluateDesignGuidelinesFeature = NexTraceOne.Catalog.Application.Contracts.Features.EvaluateDesignGuidelines.EvaluateDesignGuidelines;
using GenerateClientSdkFromContractFeature = NexTraceOne.Catalog.Application.Portal.ContractPipeline.Features.GenerateClientSdkFromContract.GenerateClientSdkFromContract;
using GenerateCodeFeature = NexTraceOne.Catalog.Application.Portal.Features.GenerateCode.GenerateCode;
using GenerateContractTestsFeature = NexTraceOne.Catalog.Application.Portal.ContractPipeline.Features.GenerateContractTests.GenerateContractTests;
using GenerateMockServerFeature = NexTraceOne.Catalog.Application.Portal.ContractPipeline.Features.GenerateMockServer.GenerateMockServer;

namespace NexTraceOne.Catalog.Tests.Contracts.Application.Features;

/// <summary>
/// Testes de handlers da camada Application para ServiceTemplate e funcionalidades
/// de governança de contratos (rules, design guidelines, breaking change, code gen, tests, SDK, mock).
/// </summary>
public sealed class ServiceTemplateAndGovernanceTests
{
    private static readonly DateTimeOffset FixedNow = new(2025, 06, 01, 10, 0, 0, TimeSpan.Zero);

    private const string BaseSpec = """{"openapi":"3.0.0","info":{"title":"Test","version":"1.0.0"},"paths":{"/users":{"get":{"operationId":"getUsers","tags":["users"],"responses":{"200":{"description":"OK"}}}}}}""";

    // ── CreateServiceTemplate ─────────────────────────────────────────

    [Fact]
    public async Task CreateServiceTemplate_Should_ReturnResponse_When_SlugIsUnique()
    {
        var repository = Substitute.For<IServiceTemplateRepository>();
        var sut = new CreateServiceTemplateFeature.Handler(repository);

        repository.ExistsBySlugAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var result = await sut.Handle(
            new CreateServiceTemplateFeature.Command(
                Slug: "payment-api",
                DisplayName: "Payment API Template",
                Description: "Template for payment services",
                Version: "1.0.0",
                ServiceType: TemplateServiceType.RestApi,
                Language: TemplateLanguage.DotNet,
                DefaultDomain: "Payments",
                DefaultTeam: "payments-team"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Slug.Should().Be("payment-api");
        result.Value.DisplayName.Should().Be("Payment API Template");
        result.Value.ServiceType.Should().Be(TemplateServiceType.RestApi);
        result.Value.Language.Should().Be(TemplateLanguage.DotNet);
        await repository.Received(1).AddAsync(Arg.Any<ServiceTemplate>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateServiceTemplate_Should_ReturnError_When_SlugIsDuplicate()
    {
        var repository = Substitute.For<IServiceTemplateRepository>();
        var sut = new CreateServiceTemplateFeature.Handler(repository);

        repository.ExistsBySlugAsync("payment-api", Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await sut.Handle(
            new CreateServiceTemplateFeature.Command(
                Slug: "payment-api",
                DisplayName: "Payment API Template",
                Description: "Template for payment services",
                Version: "1.0.0",
                ServiceType: TemplateServiceType.RestApi,
                Language: TemplateLanguage.DotNet,
                DefaultDomain: "Payments",
                DefaultTeam: "payments-team"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ServiceTemplate.DuplicateSlug");
        await repository.DidNotReceive().AddAsync(Arg.Any<ServiceTemplate>(), Arg.Any<CancellationToken>());
    }

    // ── ActivateServiceTemplate ───────────────────────────────────────

    [Fact]
    public async Task ActivateServiceTemplate_Should_ReturnResponse_When_TemplateExists()
    {
        var repository = Substitute.For<IServiceTemplateRepository>();
        var sut = new ActivateServiceTemplateFeature.Handler(repository);

        var templateId = Guid.NewGuid();
        var template = ServiceTemplate.Create(
            slug: "my-template",
            displayName: "My Template",
            description: "A test template",
            version: "1.0.0",
            serviceType: TemplateServiceType.RestApi,
            language: TemplateLanguage.DotNet,
            defaultDomain: "Test",
            defaultTeam: "test-team");

        repository.GetByIdAsync(templateId, Arg.Any<CancellationToken>())
            .Returns(template);

        var result = await sut.Handle(
            new ActivateServiceTemplateFeature.Command(templateId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsActive.Should().BeTrue();
        await repository.Received(1).UpdateAsync(Arg.Any<ServiceTemplate>(), Arg.Any<CancellationToken>());
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

        var templateId = Guid.NewGuid();
        var template = ServiceTemplate.Create(
            slug: "my-template",
            displayName: "My Template",
            description: "A test template",
            version: "1.0.0",
            serviceType: TemplateServiceType.RestApi,
            language: TemplateLanguage.DotNet,
            defaultDomain: "Test",
            defaultTeam: "test-team");

        repository.GetByIdAsync(templateId, Arg.Any<CancellationToken>())
            .Returns(template);

        var result = await sut.Handle(
            new DeactivateServiceTemplateFeature.Command(templateId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsActive.Should().BeFalse();
        await repository.Received(1).UpdateAsync(Arg.Any<ServiceTemplate>(), Arg.Any<CancellationToken>());
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

    // ── EvaluateContractRules ─────────────────────────────────────────

    [Fact]
    public async Task EvaluateContractRules_Should_ReturnResponse_When_VersionExists()
    {
        var repository = Substitute.For<IContractVersionRepository>();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        var sut = new EvaluateContractRulesFeature.Handler(repository, dateTimeProvider);

        var version = ContractVersion.Import(Guid.NewGuid(), "1.0.0", BaseSpec, "json", "upload").Value;
        repository.GetByIdAsync(Arg.Any<ContractVersionId>(), Arg.Any<CancellationToken>())
            .Returns(version);

        var result = await sut.Handle(
            new EvaluateContractRulesFeature.Query(version.Id.Value),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ContractVersionId.Should().Be(version.Id.Value);
        result.Value.Violations.Should().NotBeNull();
    }

    // ── EvaluateDesignGuidelines ──────────────────────────────────────

    [Fact]
    public async Task EvaluateDesignGuidelines_Should_ReturnScoreAndViolations_When_VersionExists()
    {
        var repository = Substitute.For<IContractVersionRepository>();
        var sut = new EvaluateDesignGuidelinesFeature.Handler(repository);

        var version = ContractVersion.Import(Guid.NewGuid(), "1.0.0", BaseSpec, "json", "upload").Value;
        repository.GetByIdAsync(Arg.Any<ContractVersionId>(), Arg.Any<CancellationToken>())
            .Returns(version);

        var result = await sut.Handle(
            new EvaluateDesignGuidelinesFeature.Query(version.Id.Value),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ContractVersionId.Should().Be(version.Id.Value);
        result.Value.Score.Should().BeGreaterThanOrEqualTo(0);
        result.Value.Violations.Should().NotBeNull();
    }

    // ── ClassifyBreakingChange ────────────────────────────────────────

    [Fact]
    public async Task ClassifyBreakingChange_Should_ReturnNotFound_When_VersionDoesNotExist()
    {
        var repository = Substitute.For<IContractVersionRepository>();
        var sut = new ClassifyBreakingChangeFeature.Handler(repository);

        repository.GetByIdAsync(Arg.Any<ContractVersionId>(), Arg.Any<CancellationToken>())
            .Returns((ContractVersion?)null);

        var result = await sut.Handle(
            new ClassifyBreakingChangeFeature.Query(Guid.NewGuid()),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Contracts.ContractVersion.NotFound");
    }

    // ── GenerateCode ──────────────────────────────────────────────────

    [Fact]
    public async Task GenerateCode_Should_ReturnResponse_When_CommandIsValid()
    {
        var repository = Substitute.For<ICodeGenerationRepository>();
        var unitOfWork = Substitute.For<IPortalUnitOfWork>();
        var clock = Substitute.For<IDateTimeProvider>();
        var sut = new GenerateCodeFeature.Handler(repository, unitOfWork, clock);

        clock.UtcNow.Returns(FixedNow);

        var result = await sut.Handle(
            new GenerateCodeFeature.Command(
                ApiAssetId: Guid.NewGuid(),
                ApiName: "UsersApi",
                ContractVersion: "1.0.0",
                RequestedById: Guid.NewGuid(),
                Language: "CSharp",
                GenerationType: GenerationType.SdkClient),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Language.Should().Be("CSharp");
        result.Value.GeneratedCode.Should().NotBeNullOrWhiteSpace();
        repository.Received(1).Add(Arg.Any<NexTraceOne.Catalog.Domain.Portal.Entities.CodeGenerationRecord>());
        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    // ── GenerateContractTests ─────────────────────────────────────────

    [Fact]
    public async Task GenerateContractTests_Should_ReturnTestFiles_When_CommandIsValid()
    {
        var version = ContractVersion.Import(Guid.NewGuid(), "1.0.0", BaseSpec, "json", "upload").Value;
        var repo = Substitute.For<IContractVersionRepository>();
        repo.GetByIdAsync(Arg.Any<ContractVersionId>(), Arg.Any<CancellationToken>()).Returns(version);
        var sut = new GenerateContractTestsFeature.Handler(repo);

        var result = await sut.Handle(
            new GenerateContractTestsFeature.Command(
                ContractVersionId: Guid.NewGuid(),
                ServiceName: "UsersService",
                TestFramework: "xunit"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TestFramework.Should().Be("xunit");
        result.Value.Files.Should().NotBeEmpty();
        result.Value.TestCount.Should().BeGreaterThan(0);
    }

    // ── GenerateClientSdkFromContract ─────────────────────────────────

    [Fact]
    public async Task GenerateClientSdkFromContract_Should_ReturnFiles_When_CommandIsValid()
    {
        var sut = new GenerateClientSdkFromContractFeature.Handler();

        var result = await sut.Handle(
            new GenerateClientSdkFromContractFeature.Command(
                ContractVersionId: Guid.NewGuid(),
                TargetLanguage: "dotnet",
                ClientName: "OrdersClient"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TargetLanguage.Should().Be("dotnet");
        result.Value.ClientName.Should().Be("OrdersClient");
        result.Value.Files.Should().NotBeEmpty();
    }

    // ── GenerateMockServer ────────────────────────────────────────────

    [Fact]
    public async Task GenerateMockServer_Should_ReturnFiles_When_CommandIsValid()
    {
        var version = ContractVersion.Import(Guid.NewGuid(), "1.0.0", BaseSpec, "json", "upload").Value;
        var repo = Substitute.For<IContractVersionRepository>();
        repo.GetByIdAsync(Arg.Any<ContractVersionId>(), Arg.Any<CancellationToken>()).Returns(version);
        var sut = new GenerateMockServerFeature.Handler(repo, NullLogger<GenerateMockServerFeature.Handler>.Instance);

        var result = await sut.Handle(
            new GenerateMockServerFeature.Command(
                ContractVersionId: Guid.NewGuid(),
                MockServerType: "wiremock"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.MockServerType.Should().Be("wiremock");
        result.Value.Files.Should().NotBeEmpty();
        result.Value.Instructions.Should().NotBeNullOrWhiteSpace();
    }
}
