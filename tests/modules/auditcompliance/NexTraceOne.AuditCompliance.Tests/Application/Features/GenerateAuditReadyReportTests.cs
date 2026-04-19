using NexTraceOne.AuditCompliance.Application.Abstractions;
using NexTraceOne.AuditCompliance.Application.Features.GenerateAuditReadyReport;
using NexTraceOne.AuditCompliance.Domain.Entities;
using NexTraceOne.AuditCompliance.Domain.Enums;
using NexTraceOne.BuildingBlocks.Application.Abstractions;

namespace NexTraceOne.AuditCompliance.Tests.Application.Features;

/// <summary>
/// Testes da feature GenerateAuditReadyReport (Phase 3.5 — Audit-ready PDF/XLSX export):
///   - Geração de relatório enterprise-ready com assinatura digital SHA-256
///   - Sumário executivo com totais por módulo e tipo de ação
///   - Validação de formato (JSON/PDF/XLSX) e período máximo de 366 dias
///   - Relatório determinístico: mesma entrada → mesma assinatura
/// </summary>
public sealed class GenerateAuditReadyReportTests
{
    private readonly IAuditEventRepository _auditEventRepository = Substitute.For<IAuditEventRepository>();
    private readonly IComplianceResultRepository _complianceResultRepository = Substitute.For<IComplianceResultRepository>();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();

    private static readonly DateTimeOffset FixedNow = new(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);
    private readonly Guid _tenantId = Guid.NewGuid();

    public GenerateAuditReadyReportTests()
    {
        _clock.UtcNow.Returns(FixedNow);
    }

    private GenerateAuditReadyReport.Handler CreateHandler() =>
        new(_auditEventRepository, _complianceResultRepository, _clock);

    // ── Validation ─────────────────────────────────────────────────────────

    [Theory]
    [InlineData("JSON")]
    [InlineData("PDF")]
    [InlineData("XLSX")]
    public async Task GenerateAuditReadyReport_Validator_SupportedFormats_ShouldPass(string format)
    {
        var validator = new GenerateAuditReadyReport.Validator();
        var from = DateTimeOffset.UtcNow.AddDays(-30);
        var to = DateTimeOffset.UtcNow;

        var result = await validator.ValidateAsync(
            new GenerateAuditReadyReport.Query(_tenantId, from, to, format));

        result.IsValid.Should().BeTrue($"Format '{format}' should be supported.");
    }

    [Theory]
    [InlineData("CSV")]
    [InlineData("XML")]
    [InlineData("HTML")]
    [InlineData("")]
    public async Task GenerateAuditReadyReport_Validator_UnsupportedFormat_ShouldFail(string format)
    {
        var validator = new GenerateAuditReadyReport.Validator();
        var from = DateTimeOffset.UtcNow.AddDays(-30);
        var to = DateTimeOffset.UtcNow;

        var result = await validator.ValidateAsync(
            new GenerateAuditReadyReport.Query(_tenantId, from, to, format));

        result.IsValid.Should().BeFalse($"Format '{format}' should not be supported.");
        result.Errors.Should().Contain(e => e.PropertyName == "Format");
    }

    [Fact]
    public async Task GenerateAuditReadyReport_Validator_FromAfterTo_ShouldFail()
    {
        var validator = new GenerateAuditReadyReport.Validator();
        var from = DateTimeOffset.UtcNow;
        var to = DateTimeOffset.UtcNow.AddDays(-1); // to < from

        var result = await validator.ValidateAsync(
            new GenerateAuditReadyReport.Query(_tenantId, from, to, "JSON"));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "From");
    }

    [Fact]
    public async Task GenerateAuditReadyReport_Validator_PeriodOver366Days_ShouldFail()
    {
        var validator = new GenerateAuditReadyReport.Validator();
        var from = DateTimeOffset.UtcNow.AddDays(-400);
        var to = DateTimeOffset.UtcNow;

        var result = await validator.ValidateAsync(
            new GenerateAuditReadyReport.Query(_tenantId, from, to, "JSON"));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "To");
    }

    [Fact]
    public async Task GenerateAuditReadyReport_Validator_EmptyTenantId_ShouldFail()
    {
        var validator = new GenerateAuditReadyReport.Validator();
        var from = DateTimeOffset.UtcNow.AddDays(-30);
        var to = DateTimeOffset.UtcNow;

        var result = await validator.ValidateAsync(
            new GenerateAuditReadyReport.Query(Guid.Empty, from, to, "JSON"));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }

    // ── Handler ────────────────────────────────────────────────────────────

    [Fact]
    public async Task GenerateAuditReadyReport_WithEvents_ShouldReturnSignedReport()
    {
        var from = DateTimeOffset.UtcNow.AddDays(-7);
        var to = DateTimeOffset.UtcNow;
        var events = BuildSampleEvents(from);

        _auditEventRepository
            .SearchAsync(null, null, null, from, to, 1, 50_000, Arg.Any<CancellationToken>())
            .Returns(events);
        _complianceResultRepository
            .ListAsync(null, null, null, Arg.Any<CancellationToken>())
            .Returns(new List<ComplianceResult>().AsReadOnly());

        var handler = CreateHandler();
        var result = await handler.Handle(
            new GenerateAuditReadyReport.Query(_tenantId, from, to, "JSON"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TenantId.Should().Be(_tenantId);
        result.Value.Format.Should().Be("JSON");
        result.Value.Summary.TotalEvents.Should().Be(events.Count);
        result.Value.Entries.Should().HaveCount(events.Count);
        result.Value.DigitalSignature.Should().NotBeNullOrEmpty("signature must be computed");
        result.Value.SignatureAlgorithm.Should().Be("SHA-256");
        result.Value.ReportId.Should().NotBeEmpty();
        result.Value.GeneratedBy.Should().Contain("NexTraceOne");
    }

    [Fact]
    public async Task GenerateAuditReadyReport_EmptyPeriod_ShouldReturnEmptyReport()
    {
        var from = DateTimeOffset.UtcNow.AddDays(-7);
        var to = DateTimeOffset.UtcNow;

        _auditEventRepository
            .SearchAsync(null, null, null, from, to, 1, 50_000, Arg.Any<CancellationToken>())
            .Returns(new List<AuditEvent>());
        _complianceResultRepository
            .ListAsync(null, null, null, Arg.Any<CancellationToken>())
            .Returns(new List<ComplianceResult>().AsReadOnly());

        var handler = CreateHandler();
        var result = await handler.Handle(
            new GenerateAuditReadyReport.Query(_tenantId, from, to, "JSON"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Summary.TotalEvents.Should().Be(0);
        result.Value.Entries.Should().BeEmpty();
        result.Value.DigitalSignature.Should().NotBeNullOrEmpty("signature computed even for empty report");
        result.Value.Summary.ComplianceRate.Should().Be(100m, "no checks = 100% compliance");
    }

    [Fact]
    public async Task GenerateAuditReadyReport_SamePeriodAndTenant_SignatureShouldBeDeterministic()
    {
        var from = DateTimeOffset.UtcNow.AddDays(-7);
        var to = DateTimeOffset.UtcNow;
        var events = BuildSampleEvents(from);

        _auditEventRepository
            .SearchAsync(null, null, null, from, to, 1, 50_000, Arg.Any<CancellationToken>())
            .Returns(events);
        _complianceResultRepository
            .ListAsync(null, null, null, Arg.Any<CancellationToken>())
            .Returns(new List<ComplianceResult>().AsReadOnly());

        var handler = CreateHandler();
        var result1 = await handler.Handle(
            new GenerateAuditReadyReport.Query(_tenantId, from, to, "JSON"), CancellationToken.None);
        var result2 = await handler.Handle(
            new GenerateAuditReadyReport.Query(_tenantId, from, to, "JSON"), CancellationToken.None);

        result1.IsSuccess.Should().BeTrue();
        result2.IsSuccess.Should().BeTrue();
        result1.Value.DigitalSignature.Should().Be(result2.Value.DigitalSignature,
            "same inputs must produce same signature");
    }

    [Fact]
    public async Task GenerateAuditReadyReport_WithEvents_SummaryByModuleShouldBeAggregated()
    {
        var from = DateTimeOffset.UtcNow.AddDays(-7);
        var to = DateTimeOffset.UtcNow;
        var events = BuildSampleEvents(from);

        _auditEventRepository
            .SearchAsync(null, null, null, from, to, 1, 50_000, Arg.Any<CancellationToken>())
            .Returns(events);
        _complianceResultRepository
            .ListAsync(null, null, null, Arg.Any<CancellationToken>())
            .Returns(new List<ComplianceResult>().AsReadOnly());

        var handler = CreateHandler();
        var result = await handler.Handle(
            new GenerateAuditReadyReport.Query(_tenantId, from, to, "PDF"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Format.Should().Be("PDF");
        result.Value.Summary.EventsByModule.Should().NotBeEmpty();
        result.Value.Summary.UniqueModules.Should().BeGreaterThan(0);
        result.Value.Summary.UniqueActors.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GenerateAuditReadyReport_CustomTitle_ShouldBeReflected()
    {
        var from = DateTimeOffset.UtcNow.AddDays(-30);
        var to = DateTimeOffset.UtcNow;
        const string customTitle = "Q1 2026 SOC 2 Audit Report";

        _auditEventRepository
            .SearchAsync(null, null, null, from, to, 1, 50_000, Arg.Any<CancellationToken>())
            .Returns(new List<AuditEvent>());
        _complianceResultRepository
            .ListAsync(null, null, null, Arg.Any<CancellationToken>())
            .Returns(new List<ComplianceResult>().AsReadOnly());

        var handler = CreateHandler();
        var result = await handler.Handle(
            new GenerateAuditReadyReport.Query(_tenantId, from, to, "XLSX", customTitle), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Title.Should().Be(customTitle);
        result.Value.Format.Should().Be("XLSX");
    }

    private static IReadOnlyList<AuditEvent> BuildSampleEvents(DateTimeOffset baseTime)
    {
        var tenantId = Guid.NewGuid();
        return new List<AuditEvent>
        {
            AuditEvent.Record("Catalog", "ServiceCreated", "svc-1", "Service", "user1@org.com",
                baseTime.AddDays(1), tenantId),
            AuditEvent.Record("IdentityAccess", "UserCreated", "usr-1", "User", "admin@org.com",
                baseTime.AddDays(2), tenantId),
            AuditEvent.Record("Catalog", "ServiceUpdated", "svc-1", "Service", "user2@org.com",
                baseTime.AddDays(3), tenantId),
            AuditEvent.Record("ChangeGovernance", "ChangeApproved", "chg-1", "Change", "tech-lead@org.com",
                baseTime.AddDays(4), tenantId),
        };
    }

    [Fact]
    public async Task GenerateAuditReadyReport_ShouldUseIDateTimeProviderForGeneratedAt()
    {
        var from = FixedNow.AddDays(-7);
        var to = FixedNow;

        _auditEventRepository.SearchAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<DateTimeOffset?>(), Arg.Any<DateTimeOffset?>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(BuildSampleEvents(from));
        _complianceResultRepository.ListAsync(Arg.Any<CompliancePolicyId?>(), Arg.Any<AuditCampaignId?>(), Arg.Any<ComplianceOutcome?>(), Arg.Any<CancellationToken>())
            .Returns(new List<ComplianceResult>());

        var handler = CreateHandler();
        var result = await handler.Handle(
            new GenerateAuditReadyReport.Query(_tenantId, from, to, "JSON"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.GeneratedAt.Should().Be(FixedNow,
            "GeneratedAt should come from IDateTimeProvider, not DateTimeOffset.UtcNow");
    }
}
