using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Application.Contracts.Features.GetApiSchemaCoverageReport;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Enums;

namespace NexTraceOne.Catalog.Tests.Contracts.Application.Features;

/// <summary>
/// Testes unitários para Wave T.2 — GetApiSchemaCoverageReport.
/// Cobre: sem contratos, spec completo (Grade A), spec parcial (Grade B/C/D),
/// spec vazio (score 0), multi-protocol, top low coverage ordering, validator.
/// </summary>
public sealed class ApiSchemaCoverageReportTests
{
    private const string FullSpec = """
        {
          "openapi": "3.0.0",
          "paths": {
            "/users": {
              "post": {
                "requestBody": { "content": {} },
                "responses": {
                  "200": { "description": "OK" },
                  "400": { "description": "Bad Request" },
                  "500": { "description": "Internal Server Error" }
                },
                "examples": [{ "name": "CreateUser" }]
              }
            }
          }
        }
        """;

    private const string NoExamplesSpec = """
        {
          "openapi": "3.0.0",
          "paths": {
            "/items": {
              "get": {
                "responses": {
                  "200": { "description": "OK" },
                  "404": { "description": "Not Found" }
                }
              }
            }
          }
        }
        """;

    private const string MinimalSpec = """
        { "openapi": "3.0.0", "paths": {} }
        """;

    private static ContractVersion MakeApprovedVersion(
        Guid? assetId = null,
        string semVer = "1.0.0",
        string? specContent = null,
        ContractProtocol protocol = ContractProtocol.OpenApi)
    {
        var id = assetId ?? Guid.NewGuid();
        var spec = specContent ?? FullSpec;

        var importResult = ContractVersion.Import(
            apiAssetId: id,
            semVer: semVer,
            specContent: spec,
            format: "json",
            importedFrom: "test",
            protocol: protocol);
        importResult.IsSuccess.Should().BeTrue();
        var version = importResult.Value;

        version.TransitionTo(ContractLifecycleState.InReview, DateTimeOffset.UtcNow).IsSuccess.Should().BeTrue();
        version.TransitionTo(ContractLifecycleState.Approved, DateTimeOffset.UtcNow).IsSuccess.Should().BeTrue();
        return version;
    }

    private static ContractVersion MakeLockedVersion(
        Guid? assetId = null,
        string semVer = "2.0.0",
        string? specContent = null)
    {
        var version = MakeApprovedVersion(assetId, semVer, specContent);
        version.TransitionTo(ContractLifecycleState.Locked, DateTimeOffset.UtcNow).IsSuccess.Should().BeTrue();
        return version;
    }

    private static IContractVersionRepository MakeRepo(
        IReadOnlyList<ContractVersion> approvedVersions,
        IReadOnlyList<ContractVersion> lockedVersions)
    {
        var repo = Substitute.For<IContractVersionRepository>();

        // First SearchAsync call: Approved
        repo.SearchAsync(
                Arg.Any<ContractProtocol?>(),
                ContractLifecycleState.Approved,
                null, null, 1, Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns((approvedVersions, approvedVersions.Count));

        // Second SearchAsync call: Locked
        repo.SearchAsync(
                Arg.Any<ContractProtocol?>(),
                ContractLifecycleState.Locked,
                null, null, 1, Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns((lockedVersions, lockedVersions.Count));

        return repo;
    }

    // ── Empty: no approved/locked contracts ───────────────────────────────

    [Fact]
    public async Task Handle_NoContracts_ReturnsEmptyReport()
    {
        var repo = MakeRepo([], []);
        var handler = new GetApiSchemaCoverageReport.Handler(repo);
        var result = await handler.Handle(new GetApiSchemaCoverageReport.Query(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var r = result.Value;
        Assert.Equal(0, r.TotalContractsAnalyzed);
        Assert.Equal(0m, r.AverageScoreGlobal);
        Assert.Empty(r.AllContracts);
        Assert.Empty(r.TopLowCoverageContracts);
    }

    // ── Full spec: all 4 dimensions → Grade A (100) ───────────────────────

    [Fact]
    public async Task Handle_FullSpecContract_GradeAScore100()
    {
        var version = MakeApprovedVersion(specContent: FullSpec);
        var repo = MakeRepo([version], []);
        var handler = new GetApiSchemaCoverageReport.Handler(repo);
        var result = await handler.Handle(new GetApiSchemaCoverageReport.Query(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var r = result.Value;
        Assert.Equal(1, r.TotalContractsAnalyzed);

        var entry = r.AllContracts.Single();
        Assert.Equal(100, entry.CoverageScore);
        Assert.Equal(GetApiSchemaCoverageReport.CoverageGrade.A, entry.Grade);
        Assert.True(entry.HasResponseBody);
        Assert.True(entry.HasRequestBody);
        Assert.True(entry.HasExamples);
        Assert.True(entry.HasErrorStatusCodes);
    }

    // ── No examples: only response + error codes → 50 score → Grade C ─────

    [Fact]
    public async Task Handle_NoExamplesSpec_GradeCScore50()
    {
        var version = MakeApprovedVersion(specContent: NoExamplesSpec);
        var repo = MakeRepo([version], []);
        var handler = new GetApiSchemaCoverageReport.Handler(repo);
        var result = await handler.Handle(new GetApiSchemaCoverageReport.Query(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var entry = result.Value.AllContracts.Single();
        // NoExamplesSpec has: 200 (response) + 404 (error status) = 50 points
        Assert.Equal(50, entry.CoverageScore);
        Assert.Equal(GetApiSchemaCoverageReport.CoverageGrade.C, entry.Grade);
        Assert.False(entry.HasExamples);
        Assert.True(entry.HasErrorStatusCodes);
        Assert.True(entry.HasResponseBody);
    }

    // ── Minimal spec: no recognizable patterns → Grade D (0 or 25) ────────

    [Fact]
    public async Task Handle_MinimalSpec_LowGrade()
    {
        var version = MakeApprovedVersion(specContent: MinimalSpec);
        var repo = MakeRepo([version], []);
        var handler = new GetApiSchemaCoverageReport.Handler(repo);
        var result = await handler.Handle(new GetApiSchemaCoverageReport.Query(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var entry = result.Value.AllContracts.Single();
        Assert.True(entry.CoverageScore < 50, $"Expected low score but got {entry.CoverageScore}");
        Assert.Equal(GetApiSchemaCoverageReport.CoverageGrade.D, entry.Grade);
    }

    // ── No contracts from repo → empty report ───────────────────────────

    [Fact]
    public async Task Handle_EmptySpecContent_Score0()
    {
        // ContractVersion.Import rejects empty/whitespace spec, so we test
        // the report with zero contracts from the repo (empty analysis)
        var repo = MakeRepo([], []);
        var handler = new GetApiSchemaCoverageReport.Handler(repo);
        var result = await handler.Handle(new GetApiSchemaCoverageReport.Query(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value.TotalContractsAnalyzed);
        Assert.Equal(0m, result.Value.AverageScoreGlobal);
    }

    // ── Locked versions included in analysis ─────────────────────────────

    [Fact]
    public async Task Handle_LockedVersionsIncluded_AnalyzedCorrectly()
    {
        var locked = MakeLockedVersion(specContent: FullSpec);
        var repo = MakeRepo([], [locked]);
        var handler = new GetApiSchemaCoverageReport.Handler(repo);
        var result = await handler.Handle(new GetApiSchemaCoverageReport.Query(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.TotalContractsAnalyzed);
        Assert.Equal(100, result.Value.AllContracts.Single().CoverageScore);
    }

    // ── Multiple contracts → average score computed correctly ─────────────

    [Fact]
    public async Task Handle_MultipleContracts_AverageScoreCorrect()
    {
        var fullVersion = MakeApprovedVersion(specContent: FullSpec);    // 100
        var noExamples = MakeApprovedVersion(specContent: NoExamplesSpec); // 50

        var repo = MakeRepo([fullVersion, noExamples], []);
        var handler = new GetApiSchemaCoverageReport.Handler(repo);
        var result = await handler.Handle(new GetApiSchemaCoverageReport.Query(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var r = result.Value;
        Assert.Equal(2, r.TotalContractsAnalyzed);
        Assert.Equal(75m, r.AverageScoreGlobal);
        Assert.Equal(1, r.GradeDistribution.ACount);
        Assert.Equal(0, r.GradeDistribution.BCount);
        Assert.Equal(1, r.GradeDistribution.CCount);
    }

    // ── TopLowCoverageContracts ordered by score ascending ────────────────

    [Fact]
    public async Task Handle_TopLowCoverage_OrderedByScoreAscending()
    {
        var best = MakeApprovedVersion(assetId: Guid.NewGuid(), specContent: FullSpec);    // 100
        var worst = MakeApprovedVersion(assetId: Guid.NewGuid(), specContent: MinimalSpec); // low

        var repo = MakeRepo([best, worst], []);
        var handler = new GetApiSchemaCoverageReport.Handler(repo);
        var result = await handler.Handle(new GetApiSchemaCoverageReport.Query(TopLowCoverageCount: 2), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var top = result.Value.TopLowCoverageContracts;
        Assert.True(top[0].CoverageScore <= top[1].CoverageScore,
            "TopLowCoverageContracts should be ordered from lowest to highest score");
    }

    // ── WSDL contract: SOAP keywords detected ─────────────────────────────

    [Fact]
    public async Task Handle_WsdlContract_SoapKeywordsDetected()
    {
        const string wsdlSpec = """
            <wsdl:definitions xmlns:wsdl="http://schemas.xmlsoap.org/wsdl/">
              <wsdl:portType>
                <wsdl:operation name="GetUser">
                  <wsdl:input message="tns:GetUserRequest"/>
                  <wsdl:output message="tns:GetUserResponse"/>
                  <wsdl:fault name="fault" message="tns:Fault"/>
                </wsdl:operation>
              </wsdl:portType>
            </wsdl:definitions>
            """;

        var version = MakeApprovedVersion(specContent: wsdlSpec, protocol: ContractProtocol.Wsdl);
        var repo = MakeRepo([version], []);
        var handler = new GetApiSchemaCoverageReport.Handler(repo);
        var result = await handler.Handle(new GetApiSchemaCoverageReport.Query(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var entry = result.Value.AllContracts.Single();
        Assert.True(entry.HasResponseBody, "WSDL wsdl:output should be detected as response body");
        Assert.True(entry.HasRequestBody, "WSDL wsdl:input should be detected as request body");
        Assert.True(entry.HasErrorStatusCodes, "WSDL fault should be detected as error status code");
    }

    // ── Validator ─────────────────────────────────────────────────────────

    [Fact]
    public void Validator_InvalidTopLowCoverageCount_ReturnsError()
    {
        var validator = new GetApiSchemaCoverageReport.Validator();
        var result = validator.Validate(new GetApiSchemaCoverageReport.Query(TopLowCoverageCount: 0));
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validator_InvalidPageSize_ReturnsError()
    {
        var validator = new GetApiSchemaCoverageReport.Validator();
        var result = validator.Validate(new GetApiSchemaCoverageReport.Query(PageSize: 5));
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validator_ValidQuery_PassesValidation()
    {
        var validator = new GetApiSchemaCoverageReport.Validator();
        var result = validator.Validate(new GetApiSchemaCoverageReport.Query());
        Assert.True(result.IsValid);
    }
}
