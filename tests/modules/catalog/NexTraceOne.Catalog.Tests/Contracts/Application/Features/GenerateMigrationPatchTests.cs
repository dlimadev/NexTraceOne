using FluentAssertions;

using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Enums;

using NSubstitute;

using GenerateMigrationPatchFeature = NexTraceOne.Catalog.Application.Contracts.Features.GenerateMigrationPatch.GenerateMigrationPatch;

namespace NexTraceOne.Catalog.Tests.Contracts.Application.Features;

/// <summary>
/// Testes unitários do handler GenerateMigrationPatch.
/// Valida geração de sugestões de código para provider e consumer
/// quando um contrato muda entre versões.
/// </summary>
public sealed class GenerateMigrationPatchTests
{
    private static readonly string BaseSpec = """
        {
          "openapi": "3.0.0",
          "info": { "title": "Payments API", "version": "1.0.0" },
          "paths": {
            "/payments": {
              "post": {
                "operationId": "createPayment",
                "requestBody": { "required": true, "content": { "application/json": { "schema": { "type": "object" } } } },
                "responses": { "200": { "description": "OK" } }
              }
            }
          }
        }
        """;

    private static readonly string TargetSpec = """
        {
          "openapi": "3.0.0",
          "info": { "title": "Payments API", "version": "2.0.0" },
          "paths": {
            "/v2/payments": {
              "post": {
                "operationId": "createPaymentV2",
                "requestBody": { "required": true, "content": { "application/json": { "schema": { "type": "object" } } } },
                "responses": { "200": { "description": "OK" } }
              }
            }
          }
        }
        """;

    private static ContractVersion MakeVersion(string semVer, string spec, ContractProtocol protocol = ContractProtocol.OpenApi)
    {
        var result = ContractVersion.Import(Guid.NewGuid(), semVer, spec, "json", "upload", protocol);
        result.IsSuccess.Should().BeTrue();
        return result.Value;
    }

    // ─────────────────────────────────────────────────────────────────
    // Happy-path tests
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_AllTarget_ReturnsProviderAndConsumerSuggestions()
    {
        var repo = Substitute.For<IContractVersionRepository>();
        var baseVersion = MakeVersion("1.0.0", BaseSpec);
        var targetVersion = MakeVersion("2.0.0", TargetSpec);

        repo.GetByIdAsync(ContractVersionId.From(baseVersion.Id.Value), Arg.Any<CancellationToken>())
            .Returns(baseVersion);
        repo.GetByIdAsync(ContractVersionId.From(targetVersion.Id.Value), Arg.Any<CancellationToken>())
            .Returns(targetVersion);

        var handler = new GenerateMigrationPatchFeature.Handler(repo);
        var command = new GenerateMigrationPatchFeature.Command(
            baseVersion.Id.Value,
            targetVersion.Id.Value,
            GenerateMigrationPatchFeature.PatchTarget.All,
            "C#");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Protocol.Should().Be("OpenApi");
        result.Value.Language.Should().Be("C#");
        result.Value.BaseVersionId.Should().Be(baseVersion.Id.Value);
        result.Value.TargetVersionId.Should().Be(targetVersion.Id.Value);
    }

    [Fact]
    public async Task Handle_ProviderTarget_ReturnsOnlyProviderSuggestions()
    {
        var repo = Substitute.For<IContractVersionRepository>();
        var baseVersion = MakeVersion("1.0.0", BaseSpec);
        var targetVersion = MakeVersion("2.0.0", TargetSpec);

        repo.GetByIdAsync(ContractVersionId.From(baseVersion.Id.Value), Arg.Any<CancellationToken>())
            .Returns(baseVersion);
        repo.GetByIdAsync(ContractVersionId.From(targetVersion.Id.Value), Arg.Any<CancellationToken>())
            .Returns(targetVersion);

        var handler = new GenerateMigrationPatchFeature.Handler(repo);
        var command = new GenerateMigrationPatchFeature.Command(
            baseVersion.Id.Value,
            targetVersion.Id.Value,
            GenerateMigrationPatchFeature.PatchTarget.Provider,
            "TypeScript");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ConsumerSuggestions.Should().BeEmpty();
        result.Value.Language.Should().Be("TypeScript");
    }

    [Fact]
    public async Task Handle_ConsumerTarget_ReturnsOnlyConsumerSuggestions()
    {
        var repo = Substitute.For<IContractVersionRepository>();
        var baseVersion = MakeVersion("1.0.0", BaseSpec);
        var targetVersion = MakeVersion("2.0.0", TargetSpec);

        repo.GetByIdAsync(ContractVersionId.From(baseVersion.Id.Value), Arg.Any<CancellationToken>())
            .Returns(baseVersion);
        repo.GetByIdAsync(ContractVersionId.From(targetVersion.Id.Value), Arg.Any<CancellationToken>())
            .Returns(targetVersion);

        var handler = new GenerateMigrationPatchFeature.Handler(repo);
        var command = new GenerateMigrationPatchFeature.Command(
            baseVersion.Id.Value,
            targetVersion.Id.Value,
            GenerateMigrationPatchFeature.PatchTarget.Consumer,
            "Java");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ProviderSuggestions.Should().BeEmpty();
        result.Value.Language.Should().Be("Java");
    }

    [Fact]
    public async Task Handle_DefaultsLanguageToCSharp_WhenNullProvided()
    {
        var repo = Substitute.For<IContractVersionRepository>();
        var baseVersion = MakeVersion("1.0.0", BaseSpec);
        var targetVersion = MakeVersion("2.0.0", TargetSpec);

        repo.GetByIdAsync(ContractVersionId.From(baseVersion.Id.Value), Arg.Any<CancellationToken>())
            .Returns(baseVersion);
        repo.GetByIdAsync(ContractVersionId.From(targetVersion.Id.Value), Arg.Any<CancellationToken>())
            .Returns(targetVersion);

        var handler = new GenerateMigrationPatchFeature.Handler(repo);
        var command = new GenerateMigrationPatchFeature.Command(
            baseVersion.Id.Value,
            targetVersion.Id.Value,
            GenerateMigrationPatchFeature.PatchTarget.All,
            null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Language.Should().Be("C#");
    }

    [Fact]
    public async Task Handle_ReturnsGeneratedAt_AsUtcNow()
    {
        var repo = Substitute.For<IContractVersionRepository>();
        var baseVersion = MakeVersion("1.0.0", BaseSpec);
        var targetVersion = MakeVersion("2.0.0", TargetSpec);

        repo.GetByIdAsync(ContractVersionId.From(baseVersion.Id.Value), Arg.Any<CancellationToken>())
            .Returns(baseVersion);
        repo.GetByIdAsync(ContractVersionId.From(targetVersion.Id.Value), Arg.Any<CancellationToken>())
            .Returns(targetVersion);

        var before = DateTime.UtcNow;
        var handler = new GenerateMigrationPatchFeature.Handler(repo);
        var result = await handler.Handle(
            new GenerateMigrationPatchFeature.Command(
                baseVersion.Id.Value, targetVersion.Id.Value,
                GenerateMigrationPatchFeature.PatchTarget.All, "C#"),
            CancellationToken.None);
        var after = DateTime.UtcNow;

        result.IsSuccess.Should().BeTrue();
        result.Value.GeneratedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    // ─────────────────────────────────────────────────────────────────
    // Error-path tests
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ReturnsNotFound_WhenBaseVersionMissing()
    {
        var repo = Substitute.For<IContractVersionRepository>();
        repo.GetByIdAsync(Arg.Any<ContractVersionId>(), Arg.Any<CancellationToken>())
            .Returns((ContractVersion?)null);

        var handler = new GenerateMigrationPatchFeature.Handler(repo);
        var result = await handler.Handle(
            new GenerateMigrationPatchFeature.Command(
                Guid.NewGuid(), Guid.NewGuid(),
                GenerateMigrationPatchFeature.PatchTarget.All, null),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Contain("NotFound");
    }

    [Fact]
    public async Task Handle_ReturnsError_WhenProtocolMismatch()
    {
        var repo = Substitute.For<IContractVersionRepository>();
        var baseVersion = MakeVersion("1.0.0", BaseSpec, ContractProtocol.OpenApi);
        var targetVersion = MakeVersion("2.0.0", "<wsdl/>", ContractProtocol.Wsdl);

        repo.GetByIdAsync(ContractVersionId.From(baseVersion.Id.Value), Arg.Any<CancellationToken>())
            .Returns(baseVersion);
        repo.GetByIdAsync(ContractVersionId.From(targetVersion.Id.Value), Arg.Any<CancellationToken>())
            .Returns(targetVersion);

        var handler = new GenerateMigrationPatchFeature.Handler(repo);
        var result = await handler.Handle(
            new GenerateMigrationPatchFeature.Command(
                baseVersion.Id.Value, targetVersion.Id.Value,
                GenerateMigrationPatchFeature.PatchTarget.All, null),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Contain("ProtocolMismatch");
    }

    // ─────────────────────────────────────────────────────────────────
    // Validator tests
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Validator_Fails_WhenBaseVersionIdEmpty()
    {
        var validator = new GenerateMigrationPatchFeature.Validator();
        var result = validator.Validate(new GenerateMigrationPatchFeature.Command(
            Guid.Empty, Guid.NewGuid(),
            GenerateMigrationPatchFeature.PatchTarget.All, null));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "BaseVersionId");
    }

    [Fact]
    public void Validator_Fails_WhenVersionsAreIdentical()
    {
        var id = Guid.NewGuid();
        var validator = new GenerateMigrationPatchFeature.Validator();
        var result = validator.Validate(new GenerateMigrationPatchFeature.Command(
            id, id,
            GenerateMigrationPatchFeature.PatchTarget.All, null));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("different"));
    }

    [Fact]
    public void Validator_Passes_WhenAllFieldsValid()
    {
        var validator = new GenerateMigrationPatchFeature.Validator();
        var result = validator.Validate(new GenerateMigrationPatchFeature.Command(
            Guid.NewGuid(), Guid.NewGuid(),
            GenerateMigrationPatchFeature.PatchTarget.All, "Python"));

        result.IsValid.Should().BeTrue();
    }
}
