using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Application.Contracts.Features.PromoteSchemaToCanonicalEntity;
using NexTraceOne.Catalog.Domain.Contracts.Entities;

namespace NexTraceOne.Catalog.Tests.Contracts.Application.Features;

/// <summary>
/// Testes unitários para PromoteSchemaToCanonicalEntity — promoção de um schema de contrato
/// a entidade canónica reutilizável.
/// </summary>
public sealed class PromoteSchemaToCanonicalEntityTests
{
    private const string OpenApiSpec =
        "{\"openapi\":\"3.0.0\",\"components\":{\"schemas\":{\"Payment\":{\"type\":\"object\",\"properties\":{\"amount\":{\"type\":\"number\"}}}}}}";
    private const string JsonSchemaSpec =
        "{\"definitions\":{\"Order\":{\"type\":\"object\"}}}";

    private static ContractVersion MakeContract(string spec) =>
        ContractVersion.Import(Guid.NewGuid(), "1.0.0", spec, "json", "test").Value!;

    private static (PromoteSchemaToCanonicalEntity.Handler Handler,
        ICanonicalEntityRepository CanonicalRepo, IUnitOfWork Uow) CreateHandler(ContractVersion? contract)
    {
        var contractRepo = Substitute.For<IContractVersionRepository>();
        contractRepo.GetByIdAsync(Arg.Any<ContractVersionId>(), Arg.Any<CancellationToken>()).Returns(contract);
        var canonicalRepo = Substitute.For<ICanonicalEntityRepository>();
        var uow = Substitute.For<IUnitOfWork>();
        return (new PromoteSchemaToCanonicalEntity.Handler(contractRepo, canonicalRepo, uow), canonicalRepo, uow);
    }

    private static PromoteSchemaToCanonicalEntity.Command ValidCommand(string schemaName = "Payment") =>
        new(Guid.NewGuid(), schemaName, "Payment", "Billing", "Core", "ana.silva@nextraceone.dev");

    [Fact]
    public async Task Promote_OpenApiSchema_CreatesCanonicalEntityAndCommits()
    {
        CanonicalEntity? captured = null;
        var (handler, canonicalRepo, uow) = CreateHandler(MakeContract(OpenApiSpec));
        canonicalRepo.When(r => r.Add(Arg.Any<CanonicalEntity>())).Do(ci => captured = ci.Arg<CanonicalEntity>());

        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().NotBeNullOrWhiteSpace();
        captured.Should().NotBeNull();
        captured!.Name.Should().Be("Payment");
        captured.Domain.Should().Be("Billing");
        captured.SchemaContent.Should().Contain("amount");
        await uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Promote_JsonSchemaDefinition_ExtractsFromDefinitions()
    {
        CanonicalEntity? captured = null;
        var (handler, canonicalRepo, _) = CreateHandler(MakeContract(JsonSchemaSpec));
        canonicalRepo.When(r => r.Add(Arg.Any<CanonicalEntity>())).Do(ci => captured = ci.Arg<CanonicalEntity>());

        var result = await handler.Handle(ValidCommand("Order"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        captured!.SchemaContent.Should().Contain("object");
    }

    [Fact]
    public async Task Promote_ContractNotFound_ReturnsNotFound()
    {
        var (handler, _, _) = CreateHandler(contract: null);

        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ContractVersion.NotFound");
    }

    [Fact]
    public async Task Promote_SchemaNameMissing_ReturnsBusinessError()
    {
        var (handler, _, _) = CreateHandler(MakeContract(OpenApiSpec));

        var result = await handler.Handle(ValidCommand("DoesNotExist"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CanonicalEntity.SchemaNotFound");
    }

    [Fact]
    public async Task Promote_InvalidJsonSpec_ReturnsSchemaNotFound()
    {
        var (handler, _, _) = CreateHandler(MakeContract("not-a-json: spec"));

        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CanonicalEntity.SchemaNotFound");
    }

    [Theory]
    [InlineData("SchemaName")]
    [InlineData("Name")]
    [InlineData("Domain")]
    [InlineData("Category")]
    [InlineData("Owner")]
    public void Validator_EmptyRequiredField_Fails(string field)
    {
        var command = field switch
        {
            "SchemaName" => ValidCommand() with { SchemaName = "" },
            "Name"       => ValidCommand() with { Name = "" },
            "Domain"     => ValidCommand() with { Domain = "" },
            "Category"   => ValidCommand() with { Category = "" },
            "Owner"      => ValidCommand() with { Owner = "" },
            _            => ValidCommand()
        };

        new PromoteSchemaToCanonicalEntity.Validator().Validate(command).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_ValidCommand_Passes()
    {
        new PromoteSchemaToCanonicalEntity.Validator().Validate(ValidCommand()).IsValid.Should().BeTrue();
    }
}
