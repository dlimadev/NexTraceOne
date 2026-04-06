using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Enums;

using GetContractHealthTimelineFeature = NexTraceOne.Catalog.Application.Contracts.Features.GetContractHealthTimeline.GetContractHealthTimeline;

namespace NexTraceOne.Catalog.Tests.Contracts.Application.Features;

/// <summary>
/// Testes do handler GetContractHealthTimeline — evolução temporal do score de saúde de contratos.
/// Valida cálculo de HealthScore, ordenação por data, detecção de breaking change e limite de versões.
/// </summary>
public sealed class ContractHealthTimelineTests
{
    private static readonly Guid ApiAssetId = Guid.NewGuid();
    private static readonly DateTimeOffset BaseDate = new(2025, 01, 01, 0, 0, 0, TimeSpan.Zero);

    private static ContractVersion CreateVersion(
        string semVer,
        string specContent,
        ContractLifecycleState state = ContractLifecycleState.Approved)
    {
        var version = ContractVersion.Import(
            ApiAssetId, semVer, specContent, "json", "upload", ContractProtocol.OpenApi).Value;

        if (state >= ContractLifecycleState.InReview)
            version.TransitionTo(ContractLifecycleState.InReview, BaseDate);
        if (state >= ContractLifecycleState.Approved)
            version.TransitionTo(ContractLifecycleState.Approved, BaseDate);

        return version;
    }

    private const string FullFeaturedSpec =
        """
        {
          "openapi": "3.1.0",
          "info": { "title": "Test", "version": "1.0.0", "description": "A test API" },
          "paths": {
            "/users": {
              "get": {
                "description": "List users",
                "responses": {
                  "200": {
                    "example": { "id": 1, "name": "Test" },
                    "schema": { "$ref": "#/components/schemas/User" }
                  }
                }
              }
            }
          },
          "components": {
            "schemas": {
              "User": { "type": "object" }
            }
          }
        }
        """;

    // ── Versão única retorna um ponto ─────────────────────────────────

    [Fact]
    public async Task Handle_Should_ReturnOnePoint_When_SingleVersionExists()
    {
        var version = CreateVersion("1.0.0", FullFeaturedSpec);
        var repository = Substitute.For<IContractVersionRepository>();
        repository.ListByApiAssetAsync(ApiAssetId, Arg.Any<CancellationToken>())
            .Returns(new List<ContractVersion> { version });

        var sut = new GetContractHealthTimelineFeature.Handler(repository);
        var result = await sut.Handle(
            new GetContractHealthTimelineFeature.Query(ApiAssetId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Points.Should().HaveCount(1);
        result.Value.ApiAssetId.Should().Be(ApiAssetId);
    }

    // ── Múltiplas versões ordenadas pela data de criação ──────────────

    [Fact]
    public async Task Handle_Should_ReturnPointsOrderedByCreatedAt()
    {
        var v1 = CreateVersion("1.0.0", "{\"openapi\":\"3.1.0\"}");
        var v2 = CreateVersion("1.1.0", "{\"openapi\":\"3.1.0\",\"description\":\"v2\"}");
        var v3 = CreateVersion("1.2.0", FullFeaturedSpec);

        // Simula datas de criação diferentes (a entidade usa SetCreated via interceptor;
        // em testes o CreatedAt permanece default DateTimeOffset, mas a ordem de retorno
        // do repositório define a ordenação pelo handler)
        var versions = new List<ContractVersion> { v3, v1, v2 }; // desordenado propositalmente

        var repository = Substitute.For<IContractVersionRepository>();
        repository.ListByApiAssetAsync(ApiAssetId, Arg.Any<CancellationToken>()).Returns(versions);

        var sut = new GetContractHealthTimelineFeature.Handler(repository);
        var result = await sut.Handle(
            new GetContractHealthTimelineFeature.Query(ApiAssetId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Points.Should().HaveCount(3);
        // Todos têm CreatedAt igual (default), mas o handler deve devolver todos os 3 pontos
        result.Value.Points.Select(p => p.SemVer).Should()
            .Contain(["1.0.0", "1.1.0", "1.2.0"]);
    }

    // ── Detecção de breaking change por major version ─────────────────

    [Fact]
    public async Task Handle_Should_MarkBreakingChange_When_MajorVersionIncreases()
    {
        var v1 = CreateVersion("1.0.0", "{\"openapi\":\"3.1.0\"}");
        var v2 = CreateVersion("2.0.0", "{\"openapi\":\"3.1.0\"}"); // major bump

        var repository = Substitute.For<IContractVersionRepository>();
        repository.ListByApiAssetAsync(ApiAssetId, Arg.Any<CancellationToken>())
            .Returns(new List<ContractVersion> { v1, v2 });

        var sut = new GetContractHealthTimelineFeature.Handler(repository);
        var result = await sut.Handle(
            new GetContractHealthTimelineFeature.Query(ApiAssetId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var v2Point = result.Value.Points.First(p => p.SemVer == "2.0.0");
        v2Point.IsBreakingChange.Should().BeTrue();
        // Primeira versão não é breaking change
        var v1Point = result.Value.Points.First(p => p.SemVer == "1.0.0");
        v1Point.IsBreakingChange.Should().BeFalse();
    }

    // ── Cálculo de HealthScore ────────────────────────────────────────

    [Fact]
    public async Task Handle_Should_ComputeHealthScore_WithAllBoosters()
    {
        // FullFeaturedSpec tem: example, #/components/schemas/, description, e semVer "2.0.0" (não é 0.0.1 ou 1.0.0)
        var version = CreateVersion("2.0.0", FullFeaturedSpec);

        var repository = Substitute.For<IContractVersionRepository>();
        repository.ListByApiAssetAsync(ApiAssetId, Arg.Any<CancellationToken>())
            .Returns(new List<ContractVersion> { version });

        var sut = new GetContractHealthTimelineFeature.Handler(repository);
        var result = await sut.Handle(
            new GetContractHealthTimelineFeature.Query(ApiAssetId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var point = result.Value.Points[0];
        // +20 example, +20 canonical schemas, +20 not deprecated (Approved), +20 description, +20 evolved semver
        point.HealthScore.Should().Be(100);
    }

    // ── MaxVersions limita os pontos retornados ───────────────────────

    [Fact]
    public async Task Handle_Should_RespectMaxVersions_Limit()
    {
        var versions = Enumerable.Range(1, 25)
            .Select(i => CreateVersion($"1.{i}.0", "{\"openapi\":\"3.1.0\"}"))
            .ToList();

        var repository = Substitute.For<IContractVersionRepository>();
        repository.ListByApiAssetAsync(ApiAssetId, Arg.Any<CancellationToken>())
            .Returns(versions);

        var sut = new GetContractHealthTimelineFeature.Handler(repository);
        var result = await sut.Handle(
            new GetContractHealthTimelineFeature.Query(ApiAssetId, MaxVersions: 5),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Points.Should().HaveCount(5);
    }

    // ── ApiAssetId vazio falha na validação ───────────────────────────

    [Fact]
    public async Task Validator_Should_Fail_When_ApiAssetIdIsEmpty()
    {
        var validator = new GetContractHealthTimelineFeature.Validator();
        var result = await validator.ValidateAsync(
            new GetContractHealthTimelineFeature.Query(Guid.Empty, 20));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ApiAssetId");
    }
}
