using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Enums;

using DetectContractDriftFeature = NexTraceOne.Catalog.Application.Contracts.Features.DetectContractDrift.DetectContractDrift;

namespace NexTraceOne.Catalog.Tests.Contracts.Application.Features;

/// <summary>
/// Testes do handler DetectContractDrift — detecção de desvios entre contrato publicado e traces observados.
/// Valida ghost endpoints, endpoints não declarados, DriftScore e Status.
/// </summary>
public sealed class ContractDriftDetectionTests
{
    private static readonly Guid ApiAssetId = Guid.NewGuid();

    /// <summary>
    /// Especificação OpenAPI simples com dois endpoints: GET /users e POST /users.
    /// </summary>
    private const string TwoEndpointSpec =
        """
        {
          "openapi": "3.1.0",
          "paths": {
            "/users": {
              "get": { "responses": { "200": { "description": "OK" } } },
              "post": { "responses": { "201": { "description": "Created" } } }
            }
          }
        }
        """;

    private static ContractVersion CreateVersion(string spec = TwoEndpointSpec) =>
        ContractVersion.Import(ApiAssetId, "1.0.0", spec, "json", "upload", ContractProtocol.OpenApi).Value;

    // ── Contrato limpo (todos os endpoints observados) ────────────────

    [Fact]
    public async Task Handle_Should_ReturnClean_When_AllDeclaredEndpointsAreObserved()
    {
        var version = CreateVersion();
        var repository = Substitute.For<IContractVersionRepository>();
        repository.GetLatestByApiAssetAsync(ApiAssetId, Arg.Any<CancellationToken>()).Returns(version);

        var sut = new DetectContractDriftFeature.Handler(repository);
        var result = await sut.Handle(
            new DetectContractDriftFeature.Query(ApiAssetId,
            [
                new("GET", "/users"),
                new("POST", "/users")
            ]),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be("Clean");
        result.Value.DriftScore.Should().Be(1.0);
        result.Value.GhostEndpoints.Should().BeEmpty();
        result.Value.UndeclaredEndpoints.Should().BeEmpty();
    }

    // ── Drift menor (>80% observado) ──────────────────────────────────

    [Fact]
    public async Task Handle_Should_ReturnMinor_When_MostDeclaredEndpointsAreObserved()
    {
        // Especificação com 6 endpoints inline: 5 observados → 5/6 ≈ 0.8333 > 0.8 → Minor
        const string sixEndpointSpec =
            """
            {
              "paths": {
                "/a": { "get": {}, "post": {}, "put": {}, "delete": {}, "patch": {} },
                "/b": { "get": {} }
              }
            }
            """;
        var version = ContractVersion.Import(ApiAssetId, "1.0.0", sixEndpointSpec, "json", "upload").Value;
        var repository = Substitute.For<IContractVersionRepository>();
        repository.GetLatestByApiAssetAsync(ApiAssetId, Arg.Any<CancellationToken>()).Returns(version);

        var sut = new DetectContractDriftFeature.Handler(repository);
        var result = await sut.Handle(
            new DetectContractDriftFeature.Query(ApiAssetId,
            [
                new("GET", "/a"),
                new("POST", "/a"),
                new("PUT", "/a"),
                new("DELETE", "/a"),
                new("PATCH", "/a")
                // GET /b não observado → 5/6 ≈ 0.8333 > 0.8 → Minor
            ]),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.DriftScore.Should().BeGreaterThan(0.8);
        result.Value.Status.Should().Be("Minor");
        result.Value.GhostEndpoints.Should().HaveCount(1);
    }

    // ── Drift crítico (0 observações) ─────────────────────────────────

    [Fact]
    public async Task Handle_Should_ReturnCritical_When_NoObservationsMatch()
    {
        var version = CreateVersion();
        var repository = Substitute.For<IContractVersionRepository>();
        repository.GetLatestByApiAssetAsync(ApiAssetId, Arg.Any<CancellationToken>()).Returns(version);

        var sut = new DetectContractDriftFeature.Handler(repository);
        var result = await sut.Handle(
            new DetectContractDriftFeature.Query(ApiAssetId,
            [
                new("GET", "/other"),
                new("POST", "/another")
            ]),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be("Critical");
        result.Value.DriftScore.Should().Be(0.0);
        result.Value.GhostEndpoints.Should().HaveCount(2);
    }

    // ── Detecção de ghost endpoints ───────────────────────────────────

    [Fact]
    public async Task Handle_Should_ReturnGhostEndpoints_When_DeclaredButNeverObserved()
    {
        var version = CreateVersion(); // GET /users + POST /users
        var repository = Substitute.For<IContractVersionRepository>();
        repository.GetLatestByApiAssetAsync(ApiAssetId, Arg.Any<CancellationToken>()).Returns(version);

        var sut = new DetectContractDriftFeature.Handler(repository);
        var result = await sut.Handle(
            new DetectContractDriftFeature.Query(ApiAssetId,
            [
                new("GET", "/users")
                // POST /users não observado → ghost
            ]),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.GhostEndpoints.Should().HaveCount(1);
        result.Value.GhostEndpoints[0].Method.Should().Be("POST");
        result.Value.GhostEndpoints[0].Path.Should().Be("/users");
    }

    // ── Detecção de endpoints não declarados ──────────────────────────

    [Fact]
    public async Task Handle_Should_ReturnUndeclaredEndpoints_When_ObservedButNotInContract()
    {
        var version = CreateVersion(); // GET /users + POST /users
        var repository = Substitute.For<IContractVersionRepository>();
        repository.GetLatestByApiAssetAsync(ApiAssetId, Arg.Any<CancellationToken>()).Returns(version);

        var sut = new DetectContractDriftFeature.Handler(repository);
        var result = await sut.Handle(
            new DetectContractDriftFeature.Query(ApiAssetId,
            [
                new("GET", "/users"),
                new("POST", "/users"),
                new("DELETE", "/users/123") // não declarado
            ]),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.UndeclaredEndpoints.Should().HaveCount(1);
        result.Value.UndeclaredEndpoints[0].Method.Should().Be("DELETE");
        result.Value.UndeclaredEndpoints[0].Path.Should().Be("/users/123");
    }

    // ── Lista de operações vazia retorna Critical ─────────────────────

    [Fact]
    public async Task Handle_Should_ReturnCritical_When_ObservedListIsEmpty()
    {
        var version = CreateVersion();
        var repository = Substitute.For<IContractVersionRepository>();
        repository.GetLatestByApiAssetAsync(ApiAssetId, Arg.Any<CancellationToken>()).Returns(version);

        var sut = new DetectContractDriftFeature.Handler(repository);
        var result = await sut.Handle(
            new DetectContractDriftFeature.Query(ApiAssetId, []),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be("Critical");
        result.Value.DriftScore.Should().Be(0.0);
    }

    // ── Contrato não encontrado retorna erro ──────────────────────────

    [Fact]
    public async Task Handle_Should_ReturnError_When_ContractNotFound()
    {
        var repository = Substitute.For<IContractVersionRepository>();
        repository.GetLatestByApiAssetAsync(ApiAssetId, Arg.Any<CancellationToken>())
            .Returns((ContractVersion?)null);

        var sut = new DetectContractDriftFeature.Handler(repository);
        var result = await sut.Handle(
            new DetectContractDriftFeature.Query(ApiAssetId, []),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Contracts.ContractVersion.NotFound");
    }

    // ── Precisão do DriftScore ────────────────────────────────────────

    [Fact]
    public async Task Handle_Should_ComputeDriftScoreAccurately()
    {
        var version = CreateVersion(); // 2 endpoints: GET /users, POST /users
        var repository = Substitute.For<IContractVersionRepository>();
        repository.GetLatestByApiAssetAsync(ApiAssetId, Arg.Any<CancellationToken>()).Returns(version);

        var sut = new DetectContractDriftFeature.Handler(repository);
        var result = await sut.Handle(
            new DetectContractDriftFeature.Query(ApiAssetId,
            [
                new("GET", "/users")
                // apenas 1 dos 2 observado → score = 0.5, que não é > 0.5 → Critical
            ]),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.DriftScore.Should().Be(0.5);
        result.Value.Status.Should().Be("Critical"); // 0.5 is not > 0.5
    }
}
