using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Governance.Application.SecurityGate.Features.AcknowledgeFinding;
using NexTraceOne.Governance.Application.SecurityGate.Features.EvaluateSecurityGate;
using NexTraceOne.Governance.Application.SecurityGate.Features.GenerateSecurityReport;
using NexTraceOne.Governance.Application.SecurityGate.Features.GetSecurityDashboard;
using NexTraceOne.Governance.Application.SecurityGate.Features.GetSecurityScanResult;
using NexTraceOne.Governance.Application.SecurityGate.Features.ListSecurityFindings;
using NexTraceOne.Governance.Application.SecurityGate.Features.ScanContractSecurity;
using NexTraceOne.Governance.Application.SecurityGate.Features.ScanGeneratedCode;
using NexTraceOne.Governance.Application.SecurityGate.Ports;
using NexTraceOne.Governance.Application.SecurityGate.Services;
using NexTraceOne.Governance.Domain.SecurityGate.Entities;
using NexTraceOne.Governance.Domain.SecurityGate.Enums;

namespace NexTraceOne.Governance.Tests.Application.SecurityGate;

/// <summary>
/// Testes de unidade Phase 3 — Security Gate Pipeline.
/// </summary>
public sealed class SecurityGateTests
{
    private readonly ISecurityScanRepository _repo = Substitute.For<ISecurityScanRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    // ── InternalSastScanner ───────────────────────────────────────────────

    [Fact]
    public void InternalSastScanner_Scan_DetectsHardcodedPassword()
    {
        var scanId = Guid.NewGuid();
        var content = """var password = "MySecret123";""";
        var findings = InternalSastScanner.Scan(scanId, "Service.cs", content);

        findings.Should().Contain(f => f.Category == SecurityCategory.HardcodedSecrets);
    }

    [Fact]
    public void InternalSastScanner_Scan_DetectsInsecureCrypto()
    {
        var scanId = Guid.NewGuid();
        var content = "var md5 = MD5.Create();";
        var findings = InternalSastScanner.Scan(scanId, "Crypto.cs", content);

        findings.Should().Contain(f => f.Category == SecurityCategory.InsecureCrypto);
    }

    [Fact]
    public void InternalSastScanner_Scan_DetectsSqlInjection()
    {
        var scanId = Guid.NewGuid();
        var content = """var sql = "SELECT * FROM Users WHERE Id = " + userId;""";
        var findings = InternalSastScanner.Scan(scanId, "Repo.cs", content);

        findings.Should().Contain(f => f.Category == SecurityCategory.Injection);
    }

    [Fact]
    public void InternalSastScanner_Scan_CleanCode_ReturnsNoFindings()
    {
        var scanId = Guid.NewGuid();
        var content = """
            public sealed class UserQuery(IDbContext db)
            {
                public async Task<User?> GetAsync(Guid id, CancellationToken ct)
                    => await db.Users.FindAsync([id], ct);
            }
            """;
        var findings = InternalSastScanner.Scan(scanId, "UserQuery.cs", content);

        findings.Should().BeEmpty();
    }

    [Fact]
    public void InternalSastScanner_Scan_DetectsCorsMisconfiguration()
    {
        var scanId = Guid.NewGuid();
        var content = "app.UseCors(p => p.AllowAnyOrigin());";
        var findings = InternalSastScanner.Scan(scanId, "Program.cs", content);

        findings.Should().Contain(f => f.Category == SecurityCategory.SecurityMisconfiguration);
    }

    [Fact]
    public void InternalSastScanner_Scan_DetectsInsecureDeserialization()
    {
        var scanId = Guid.NewGuid();
        var content = "var obj = new BinaryFormatter().Deserialize(stream);";
        var findings = InternalSastScanner.Scan(scanId, "Serializer.cs", content);

        findings.Should().Contain(f => f.Category == SecurityCategory.InsecureDeserialization);
    }

    // ── SecurityScanResult Domain ─────────────────────────────────────────

    [Fact]
    public void SecurityScanResult_Create_DefaultsToCleanPassedGate()
    {
        var result = SecurityScanResult.Create(ScanTarget.GeneratedCode, Guid.NewGuid(), ScanProvider.Internal);

        result.PassedGate.Should().BeTrue();
        result.OverallRisk.Should().Be(SecurityRiskLevel.Clean);
        result.Findings.Should().BeEmpty();
    }

    [Fact]
    public void SecurityScanResult_AddFindings_WithCritical_FailsGate()
    {
        var result = SecurityScanResult.Create(ScanTarget.GeneratedCode, Guid.NewGuid(), ScanProvider.Internal);
        var finding = SecurityFinding.Create(result.Id.Value, "SAST-001", SecurityCategory.Injection, FindingSeverity.Critical, "file.cs", "desc", "fix");
        result.AddFindings([finding]);

        result.PassedGate.Should().BeFalse();
        result.OverallRisk.Should().Be(SecurityRiskLevel.Critical);
    }

    [Fact]
    public void SecurityScanResult_AddFindings_WithOnlyLow_PassesGate()
    {
        var result = SecurityScanResult.Create(ScanTarget.GeneratedCode, Guid.NewGuid(), ScanProvider.Internal);
        var finding = SecurityFinding.Create(result.Id.Value, "SAST-010", SecurityCategory.MissingInputValidation, FindingSeverity.Low, "file.cs", "desc", "fix");
        result.AddFindings([finding]);

        result.PassedGate.Should().BeTrue();
        result.OverallRisk.Should().Be(SecurityRiskLevel.Low);
    }

    [Fact]
    public void SecurityScanResult_ReEvaluateGate_WithCustomThresholds()
    {
        var result = SecurityScanResult.Create(ScanTarget.GeneratedCode, Guid.NewGuid(), ScanProvider.Internal);
        var findings = new List<SecurityFinding>();
        for (var i = 0; i < 4; i++)
            findings.Add(SecurityFinding.Create(result.Id.Value, "SAST-001", SecurityCategory.Injection, FindingSeverity.High, "file.cs", "desc", "fix"));
        result.AddFindings(findings); // 4 high findings — fails with default maxHigh=3

        result.PassedGate.Should().BeFalse();

        result.ReEvaluateGate(maxCritical: 0, maxHighFindings: 5); // relax to 5 high allowed
        result.PassedGate.Should().BeTrue();
    }

    [Fact]
    public void SecurityFinding_Acknowledge_ChangesStatus()
    {
        var finding = SecurityFinding.Create(Guid.NewGuid(), "SAST-001", SecurityCategory.Injection, FindingSeverity.High, "file.cs", "desc", "fix");
        finding.Acknowledge();
        finding.Status.Should().Be(FindingStatus.Acknowledged);
    }

    [Fact]
    public void SecurityFinding_MarkAsFalsePositive_ChangesStatus()
    {
        var finding = SecurityFinding.Create(Guid.NewGuid(), "SAST-001", SecurityCategory.Injection, FindingSeverity.High, "file.cs", "desc", "fix");
        finding.MarkAsFalsePositive();
        finding.Status.Should().Be(FindingStatus.FalsePositive);
    }

    // ── ScanGeneratedCode Handler ─────────────────────────────────────────

    [Fact]
    public async Task ScanGeneratedCode_Handle_StoresResultAndReturnsResponse()
    {
        _repo.AddAsync(Arg.Any<SecurityScanResult>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _uow.CommitAsync(Arg.Any<CancellationToken>()).Returns(1);

        var handler = new ScanGeneratedCode.Handler(_repo, _uow);
        var command = new ScanGeneratedCode.Command(
            Guid.NewGuid(),
            [new ScanGeneratedCode.CodeFile("Service.cs", """string password = "supersecretpassword";""", "cs")]);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalFindings.Should().BeGreaterThan(0);
        await _repo.Received(1).AddAsync(Arg.Any<SecurityScanResult>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ScanGeneratedCode_Handle_CleanCode_PassesGate()
    {
        _repo.AddAsync(Arg.Any<SecurityScanResult>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _uow.CommitAsync(Arg.Any<CancellationToken>()).Returns(1);

        var handler = new ScanGeneratedCode.Handler(_repo, _uow);
        var command = new ScanGeneratedCode.Command(
            Guid.NewGuid(),
            [new ScanGeneratedCode.CodeFile("Readme.md", "# Hello World", "md")]);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.PassedGate.Should().BeTrue();
    }

    // ── ScanContractSecurity Handler ──────────────────────────────────────

    [Fact]
    public async Task ScanContractSecurity_Handle_DetectsMissingSecuritySchemes()
    {
        _repo.AddAsync(Arg.Any<SecurityScanResult>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _uow.CommitAsync(Arg.Any<CancellationToken>()).Returns(1);

        var handler = new ScanContractSecurity.Handler(_repo, _uow);
        var contractJson = """{"openapi":"3.0.0","info":{"title":"test","version":"1.0"},"paths":{"/users":{"get":{}}}}""";
        var command = new ScanContractSecurity.Command(Guid.NewGuid(), contractJson);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalFindings.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ScanContractSecurity_Handle_InvalidJson_StillSucceeds()
    {
        _repo.AddAsync(Arg.Any<SecurityScanResult>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _uow.CommitAsync(Arg.Any<CancellationToken>()).Returns(1);

        var handler = new ScanContractSecurity.Handler(_repo, _uow);
        var command = new ScanContractSecurity.Command(Guid.NewGuid(), "not valid json");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue(); // parsing failure produces info finding
    }

    // ── GetSecurityScanResult Handler ──────────────────────────────────────

    [Fact]
    public async Task GetSecurityScanResult_Handle_NotFound_ReturnsFailure()
    {
        _repo.FindByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((SecurityScanResult?)null);

        var handler = new GetSecurityScanResult.Handler(_repo);
        var result = await handler.Handle(new GetSecurityScanResult.Query(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("SECURITY_SCAN_NOT_FOUND");
    }

    [Fact]
    public async Task GetSecurityScanResult_Handle_Found_ReturnsResponse()
    {
        var scan = SecurityScanResult.Create(ScanTarget.GeneratedCode, Guid.NewGuid(), ScanProvider.Internal);
        _repo.FindByIdAsync(scan.Id.Value, Arg.Any<CancellationToken>()).Returns(scan);

        var handler = new GetSecurityScanResult.Handler(_repo);
        var result = await handler.Handle(new GetSecurityScanResult.Query(scan.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ScanId.Should().Be(scan.Id.Value);
    }

    // ── AcknowledgeFinding Handler ────────────────────────────────────────

    [Fact]
    public async Task AcknowledgeFinding_Handle_NotFound_ReturnsFailure()
    {
        _repo.FindByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((SecurityScanResult?)null);

        var handler = new AcknowledgeFinding.Handler(_repo, _uow);
        var result = await handler.Handle(
            new AcknowledgeFinding.Command(Guid.NewGuid(), Guid.NewGuid(), FindingStatus.Acknowledged),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    // ── GetSecurityDashboard Handler ──────────────────────────────────────

    [Fact]
    public async Task GetSecurityDashboard_Handle_ReturnsAggregatedMetrics()
    {
        _repo.GetScanCountsAsync(Arg.Any<CancellationToken>()).Returns((10, 8));
        _repo.GetTopCategoriesAsync(5, Arg.Any<CancellationToken>())
            .Returns(new List<(string, int)> { ("HardcodedSecrets", 5) });

        var handler = new GetSecurityDashboard.Handler(_repo);
        var result = await handler.Handle(new GetSecurityDashboard.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalScans.Should().Be(10);
        result.Value.PassedScans.Should().Be(8);
        result.Value.GatePassRate.Should().Be(80.0);
        result.Value.TopCategories.Should().HaveCount(1);
    }

    // ── EvaluateSecurityGate Handler ──────────────────────────────────────

    [Fact]
    public async Task EvaluateSecurityGate_Handle_ReEvaluatesWithCustomThresholds()
    {
        var scan = SecurityScanResult.Create(ScanTarget.GeneratedCode, Guid.NewGuid(), ScanProvider.Internal);
        var finding = SecurityFinding.Create(scan.Id.Value, "SAST-001", SecurityCategory.Injection, FindingSeverity.High, "file.cs", "desc", "fix");
        scan.AddFindings([finding]);

        _repo.FindByIdAsync(scan.Id.Value, Arg.Any<CancellationToken>()).Returns(scan);
        _repo.UpdateAsync(Arg.Any<SecurityScanResult>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        _uow.CommitAsync(Arg.Any<CancellationToken>()).Returns(1);

        var handler = new EvaluateSecurityGate.Handler(_repo, _uow);
        var result = await handler.Handle(
            new EvaluateSecurityGate.Command(scan.Id.Value, MaxCritical: 0, MaxHigh: 5),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.PassedGate.Should().BeTrue(); // 1 High <= 5 max
    }

    // ── GenerateSecurityReport Handler ────────────────────────────────────

    [Fact]
    public async Task GenerateSecurityReport_Handle_ReturnsJsonReport()
    {
        var scan = SecurityScanResult.Create(ScanTarget.Contract, Guid.NewGuid(), ScanProvider.Internal);
        var finding = SecurityFinding.Create(scan.Id.Value, "CONTRACT-001", SecurityCategory.BrokenAuth, FindingSeverity.Medium, "contract.json", "desc", "fix");
        scan.AddFindings([finding]);

        _repo.FindByIdAsync(scan.Id.Value, Arg.Any<CancellationToken>()).Returns(scan);

        var handler = new GenerateSecurityReport.Handler(_repo);
        var result = await handler.Handle(new GenerateSecurityReport.Query(scan.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ReportJson.Should().Contain("ScanId");
        result.Value.ReportJson.Should().Contain("Recommendations");
    }

    // ── ListSecurityFindings Handler ──────────────────────────────────────

    [Fact]
    public async Task ListSecurityFindings_Handle_ReturnsPagedFindings()
    {
        var finding = SecurityFinding.Create(Guid.NewGuid(), "SAST-001", SecurityCategory.Injection, FindingSeverity.High, "f.cs", "desc", "fix");
        _repo.ListFindingsAsync(null, FindingSeverity.Info, null, null, 20, 1, Arg.Any<CancellationToken>())
            .Returns(new List<SecurityFinding> { finding });

        var handler = new ListSecurityFindings.Handler(_repo);
        var result = await handler.Handle(new ListSecurityFindings.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
    }

    // ── Validators ────────────────────────────────────────────────────────

    [Fact]
    public void ScanGeneratedCode_Validator_EmptyTargetId_Fails()
    {
        var validator = new ScanGeneratedCode.Validator();
        var result = validator.Validate(new ScanGeneratedCode.Command(Guid.Empty, []));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void ScanContractSecurity_Validator_EmptyJson_Fails()
    {
        var validator = new ScanContractSecurity.Validator();
        var result = validator.Validate(new ScanContractSecurity.Command(Guid.NewGuid(), ""));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void AcknowledgeFinding_Validator_InvalidStatus_Fails()
    {
        var validator = new AcknowledgeFinding.Validator();
        var result = validator.Validate(new AcknowledgeFinding.Command(Guid.NewGuid(), Guid.NewGuid(), FindingStatus.Open));
        result.IsValid.Should().BeFalse();
    }
}
