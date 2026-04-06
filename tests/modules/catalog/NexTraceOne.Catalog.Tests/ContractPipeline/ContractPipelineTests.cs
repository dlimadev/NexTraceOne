using NexTraceOne.Catalog.Application.Portal.ContractPipeline.Features.GenerateClientSdkFromContract;
using NexTraceOne.Catalog.Application.Portal.ContractPipeline.Features.GenerateContractTests;
using NexTraceOne.Catalog.Application.Portal.ContractPipeline.Features.GenerateMockServer;
using NexTraceOne.Catalog.Application.Portal.ContractPipeline.Features.GeneratePostmanCollection;
using NexTraceOne.Catalog.Application.Portal.ContractPipeline.Features.GenerateServerFromContract;

namespace NexTraceOne.Catalog.Tests.ContractPipeline;

/// <summary>
/// Testes de unidade Phase 4 — Contract-to-Code Pipeline.
/// </summary>
public sealed class ContractPipelineTests
{
    private static readonly Guid ContractVersionId = Guid.NewGuid();

    private static readonly string SampleOpenApiJson = """
        {
            "openapi": "3.0.0",
            "info": { "title": "Users API", "version": "1.0.0" },
            "paths": {
                "/users": {
                    "get": { "summary": "List users", "responses": {} },
                    "post": { "summary": "Create user", "responses": {} }
                },
                "/users/{id}": {
                    "get": { "summary": "Get user by id", "responses": {} },
                    "delete": { "summary": "Delete user", "responses": {} }
                }
            }
        }
        """;

    // ── GenerateServerFromContract ────────────────────────────────────────

    [Fact]
    public async Task GenerateServerFromContract_DotNet_GeneratesControllerFile()
    {
        var handler = new GenerateServerFromContract.Handler();
        var command = new GenerateServerFromContract.Command(ContractVersionId, "dotnet", "UsersService");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Files.Should().NotBeEmpty();
        result.Value.Files.Should().Contain(f => f.FileName.EndsWith(".cs"));
        result.Value.TargetLanguage.Should().Be("dotnet");
    }

    [Fact]
    public async Task GenerateServerFromContract_TypeScript_GeneratesFile()
    {
        var handler = new GenerateServerFromContract.Handler();
        var command = new GenerateServerFromContract.Command(ContractVersionId, "nodejs", "UsersSvc");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Files.Should().NotBeEmpty();
        result.Value.Files.Should().Contain(f => f.Language == "javascript");
    }

    [Fact]
    public async Task GenerateServerFromContract_Java_GeneratesFile()
    {
        var handler = new GenerateServerFromContract.Handler();
        var command = new GenerateServerFromContract.Command(ContractVersionId, "java", "UsersService");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Files.Should().Contain(f => f.Language == "java");
    }

    [Fact]
    public async Task GenerateServerFromContract_Go_GeneratesFile()
    {
        var handler = new GenerateServerFromContract.Handler();
        var command = new GenerateServerFromContract.Command(ContractVersionId, "go", "UsersService");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Files.Should().Contain(f => f.Language == "go");
    }

    [Fact]
    public async Task GenerateServerFromContract_Python_GeneratesFile()
    {
        var handler = new GenerateServerFromContract.Handler();
        var command = new GenerateServerFromContract.Command(ContractVersionId, "python", "UsersService");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Files.Should().Contain(f => f.Language == "python");
    }

    [Fact]
    public void GenerateServerFromContract_Validator_UnsupportedLanguage_Fails()
    {
        var validator = new GenerateServerFromContract.Validator();
        var result = validator.Validate(new GenerateServerFromContract.Command(ContractVersionId, "pascal", "Svc"));
        result.IsValid.Should().BeFalse();
    }

    // ── GenerateMockServer ────────────────────────────────────────────────

    [Fact]
    public async Task GenerateMockServer_WireMock_GeneratesStubFiles()
    {
        var handler = new GenerateMockServer.Handler();
        var command = new GenerateMockServer.Command(ContractVersionId, SampleOpenApiJson, "wiremock");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.MockServerType.Should().Be("wiremock");
        result.Value.Files.Should().NotBeEmpty();
        result.Value.Instructions.Should().Contain("WireMock");
    }

    [Fact]
    public async Task GenerateMockServer_JsonServer_GeneratesDbAndRoutes()
    {
        var handler = new GenerateMockServer.Handler();
        var command = new GenerateMockServer.Command(ContractVersionId, SampleOpenApiJson, "json-server");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Files.Should().Contain(f => f.FileName == "db.json");
        result.Value.Files.Should().Contain(f => f.FileName == "routes.json");
        result.Value.Instructions.Should().Contain("json-server");
    }

    [Fact]
    public void GenerateMockServer_Validator_InvalidType_Fails()
    {
        var validator = new GenerateMockServer.Validator();
        var result = validator.Validate(new GenerateMockServer.Command(ContractVersionId, "{}", "swagger-mock"));
        result.IsValid.Should().BeFalse();
    }

    // ── GeneratePostmanCollection ─────────────────────────────────────────

    [Fact]
    public async Task GeneratePostmanCollection_ValidContract_GeneratesCollection()
    {
        var handler = new GeneratePostmanCollection.Handler();
        var command = new GeneratePostmanCollection.Command(ContractVersionId, SampleOpenApiJson, "Users API Collection");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.CollectionJson.Should().Contain("Users API Collection");
        result.Value.EndpointCount.Should().Be(4); // GET /users, POST /users, GET /users/{id}, DELETE /users/{id}
    }

    [Fact]
    public async Task GeneratePostmanCollection_InvalidJson_GeneratesEmptyCollection()
    {
        var handler = new GeneratePostmanCollection.Handler();
        var command = new GeneratePostmanCollection.Command(ContractVersionId, "invalid json", "Test");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.EndpointCount.Should().Be(0);
    }

    [Fact]
    public void GeneratePostmanCollection_Validator_EmptyName_Fails()
    {
        var validator = new GeneratePostmanCollection.Validator();
        var result = validator.Validate(new GeneratePostmanCollection.Command(ContractVersionId, "{}", ""));
        result.IsValid.Should().BeFalse();
    }

    // ── GenerateContractTests ─────────────────────────────────────────────

    [Fact]
    public async Task GenerateContractTests_XUnit_GeneratesTestFile()
    {
        var handler = new GenerateContractTests.Handler();
        var command = new GenerateContractTests.Command(ContractVersionId, SampleOpenApiJson, "UsersService", "xunit");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TestFramework.Should().Be("xunit");
        result.Value.TestCount.Should().Be(4);
        result.Value.Files.Should().Contain(f => f.Language == "csharp");
    }

    [Fact]
    public async Task GenerateContractTests_Jest_GeneratesTestFile()
    {
        var handler = new GenerateContractTests.Handler();
        var command = new GenerateContractTests.Command(ContractVersionId, SampleOpenApiJson, "UsersService", "jest");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Files.Should().Contain(f => f.Language == "javascript");
    }

    [Fact]
    public async Task GenerateContractTests_Robot_GeneratesTestFile()
    {
        var handler = new GenerateContractTests.Handler();
        var command = new GenerateContractTests.Command(ContractVersionId, SampleOpenApiJson, "UsersService", "robot");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Files.Should().Contain(f => f.Language == "robot");
    }

    [Fact]
    public void GenerateContractTests_Validator_UnsupportedFramework_Fails()
    {
        var validator = new GenerateContractTests.Validator();
        var result = validator.Validate(new GenerateContractTests.Command(ContractVersionId, "{}", "Svc", "mocha-chai"));
        result.IsValid.Should().BeFalse();
    }

    // ── GenerateClientSdkFromContract ─────────────────────────────────────

    [Fact]
    public async Task GenerateClientSdkFromContract_DotNet_GeneratesClientFile()
    {
        var handler = new GenerateClientSdkFromContract.Handler();
        var command = new GenerateClientSdkFromContract.Command(ContractVersionId, "dotnet", "UsersApiClient");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Files.Should().Contain(f => f.Language == "csharp");
        result.Value.ClientName.Should().Be("UsersApiClient");
    }

    [Fact]
    public async Task GenerateClientSdkFromContract_TypeScript_GeneratesClientFile()
    {
        var handler = new GenerateClientSdkFromContract.Handler();
        var command = new GenerateClientSdkFromContract.Command(ContractVersionId, "typescript", "UsersClient");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Files.Should().Contain(f => f.Language == "typescript");
    }

    [Fact]
    public async Task GenerateClientSdkFromContract_Python_GeneratesClientFile()
    {
        var handler = new GenerateClientSdkFromContract.Handler();
        var command = new GenerateClientSdkFromContract.Command(ContractVersionId, "python", "UsersClient");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Files.Should().Contain(f => f.Language == "python");
    }

    [Fact]
    public void GenerateClientSdkFromContract_Validator_UnsupportedLanguage_Fails()
    {
        var validator = new GenerateClientSdkFromContract.Validator();
        var result = validator.Validate(new GenerateClientSdkFromContract.Command(ContractVersionId, "ruby", "Client"));
        result.IsValid.Should().BeFalse();
    }

    // ── Shared tests ──────────────────────────────────────────────────────

    [Fact]
    public async Task GenerateServerFromContract_EmptyServiceName_ValidatorFails()
    {
        var validator = new GenerateServerFromContract.Validator();
        var result = validator.Validate(new GenerateServerFromContract.Command(ContractVersionId, "dotnet", ""));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task GenerateServerFromContract_EmptyContractVersion_ValidatorFails()
    {
        var validator = new GenerateServerFromContract.Validator();
        var result = validator.Validate(new GenerateServerFromContract.Command(Guid.Empty, "dotnet", "Svc"));
        result.IsValid.Should().BeFalse();
    }
}
