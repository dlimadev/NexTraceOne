using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Application.Graph.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Graph.Entities;

using ValidateCdctFeature = NexTraceOne.Catalog.Application.Contracts.Features.ValidateConsumerDrivenContract.ValidateConsumerDrivenContract;

namespace NexTraceOne.Catalog.Tests.Contracts.Application.Features;

/// <summary>
/// Testes da feature ValidateConsumerDrivenContract — validação de contratos
/// orientados pelo consumidor (CDCT) contra a especificação do provider.
/// </summary>
public sealed class ConsumerDrivenContractTests
{
    private const string FullSpec = """
        {
            "openapi": "3.1.0",
            "info": { "title": "Users API", "version": "1.0.0" },
            "paths": {
                "/users": {
                    "get": {
                        "responses": {
                            "200": {
                                "description": "OK",
                                "content": {
                                    "application/json": {
                                        "schema": {
                                            "type": "object",
                                            "properties": {
                                                "id": { "type": "string" },
                                                "name": { "type": "string" },
                                                "email": { "type": "string" }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                },
                "/users/{id}": {
                    "get": {
                        "responses": {
                            "200": {
                                "description": "OK",
                                "content": {
                                    "application/json": {
                                        "schema": {
                                            "type": "object",
                                            "properties": {
                                                "id": { "type": "string" },
                                                "name": { "type": "string" },
                                                "email": { "type": "string" },
                                                "createdAt": { "type": "string" }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        """;

    private static ServiceAsset CreateService()
        => ServiceAsset.Create("UsersService", "identity", "team-platform");

    [Fact]
    public async Task Should_Pass_When_Contract_Satisfies_All_Expectations()
    {
        var service = CreateService();
        var apiAsset = ApiAsset.Register("UsersApi", "/api/users", "1.0.0", "Internal", service);
        var contractResult = ContractVersion.Import(apiAsset.Id.Value, "1.0.0", FullSpec, "json", "upload");
        var contract = contractResult.Value;

        var apiRepo = Substitute.For<IApiAssetRepository>();
        var contractRepo = Substitute.For<IContractVersionRepository>();

        apiRepo.GetByIdAsync(apiAsset.Id, Arg.Any<CancellationToken>())
            .Returns(apiAsset);
        contractRepo.GetLatestByApiAssetAsync(apiAsset.Id.Value, Arg.Any<CancellationToken>())
            .Returns(contract);

        var handler = new ValidateCdctFeature.Handler(apiRepo, contractRepo);
        var command = new ValidateCdctFeature.Command(
            apiAsset.Id.Value,
            [
                new ValidateCdctFeature.ConsumerExpectation(
                    "FrontendApp", "/users", "get", ["id", "name", "email"])
            ]);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Passed.Should().Be(1);
        result.Value.Failed.Should().Be(0);
        result.Value.Results[0].IsCompatible.Should().BeTrue();
        result.Value.Results[0].PathExists.Should().BeTrue();
        result.Value.Results[0].MethodExists.Should().BeTrue();
        result.Value.Results[0].MissingFields.Should().BeEmpty();
    }

    [Fact]
    public async Task Should_Fail_When_Path_Does_Not_Exist()
    {
        var service = CreateService();
        var apiAsset = ApiAsset.Register("UsersApi", "/api/users", "1.0.0", "Internal", service);
        var contractResult = ContractVersion.Import(apiAsset.Id.Value, "1.0.0", FullSpec, "json", "upload");
        var contract = contractResult.Value;

        var apiRepo = Substitute.For<IApiAssetRepository>();
        var contractRepo = Substitute.For<IContractVersionRepository>();

        apiRepo.GetByIdAsync(apiAsset.Id, Arg.Any<CancellationToken>())
            .Returns(apiAsset);
        contractRepo.GetLatestByApiAssetAsync(apiAsset.Id.Value, Arg.Any<CancellationToken>())
            .Returns(contract);

        var handler = new ValidateCdctFeature.Handler(apiRepo, contractRepo);
        var command = new ValidateCdctFeature.Command(
            apiAsset.Id.Value,
            [
                new ValidateCdctFeature.ConsumerExpectation(
                    "MobileApp", "/orders", "get", ["id"])
            ]);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Passed.Should().Be(0);
        result.Value.Failed.Should().Be(1);
        result.Value.Results[0].PathExists.Should().BeFalse();
        result.Value.Results[0].IsCompatible.Should().BeFalse();
    }

    [Fact]
    public async Task Should_Report_Missing_Fields_When_Schema_Lacks_Required_Properties()
    {
        var service = CreateService();
        var apiAsset = ApiAsset.Register("UsersApi", "/api/users", "1.0.0", "Internal", service);
        var contractResult = ContractVersion.Import(apiAsset.Id.Value, "1.0.0", FullSpec, "json", "upload");
        var contract = contractResult.Value;

        var apiRepo = Substitute.For<IApiAssetRepository>();
        var contractRepo = Substitute.For<IContractVersionRepository>();

        apiRepo.GetByIdAsync(apiAsset.Id, Arg.Any<CancellationToken>())
            .Returns(apiAsset);
        contractRepo.GetLatestByApiAssetAsync(apiAsset.Id.Value, Arg.Any<CancellationToken>())
            .Returns(contract);

        var handler = new ValidateCdctFeature.Handler(apiRepo, contractRepo);
        var command = new ValidateCdctFeature.Command(
            apiAsset.Id.Value,
            [
                new ValidateCdctFeature.ConsumerExpectation(
                    "ReportService", "/users", "get", ["id", "name", "phone", "address"])
            ]);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Failed.Should().Be(1);
        result.Value.Results[0].MissingFields.Should().Contain("phone");
        result.Value.Results[0].MissingFields.Should().Contain("address");
        result.Value.Results[0].IsCompatible.Should().BeFalse();
    }

    [Fact]
    public async Task Should_Return_Error_When_ApiAsset_Not_Found()
    {
        var apiRepo = Substitute.For<IApiAssetRepository>();
        var contractRepo = Substitute.For<IContractVersionRepository>();

        var nonExistentId = Guid.NewGuid();
        apiRepo.GetByIdAsync(ApiAssetId.From(nonExistentId), Arg.Any<CancellationToken>())
            .Returns((ApiAsset?)null);

        var handler = new ValidateCdctFeature.Handler(apiRepo, contractRepo);
        var command = new ValidateCdctFeature.Command(
            nonExistentId,
            [
                new ValidateCdctFeature.ConsumerExpectation(
                    "SomeConsumer", "/test", "get", [])
            ]);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CatalogGraph.ApiAsset.NotFound");
    }
}
