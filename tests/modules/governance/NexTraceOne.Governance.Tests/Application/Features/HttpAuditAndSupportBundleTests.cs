using System.Linq;
using FluentAssertions;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Application.Features.GetExternalHttpAudit;
using NexTraceOne.Governance.Application.Features.GetSupportBundles;
using NexTraceOne.Governance.Domain.Entities;
using NSubstitute;

namespace NexTraceOne.Governance.Tests.Application.Features;

/// <summary>
/// Testes para GetExternalHttpAudit e GetSupportBundles.
/// Verifica integração com IHttpAuditReader e ISupportBundleRepository.
/// </summary>
public sealed class HttpAuditAndSupportBundleTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 4, 19, 13, 0, 0, TimeSpan.Zero);

    // ── GetExternalHttpAudit ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetExternalHttpAudit_WhenAuditReaderReturnsEmptyPage_ShouldReturnSuccessWithNote()
    {
        var auditReader = Substitute.For<IHttpAuditReader>();
        auditReader.QueryAsync(Arg.Any<HttpAuditFilter>(), Arg.Any<CancellationToken>())
            .Returns(new HttpAuditPage([], 0, IsLiveData: false));

        var handler = new GetExternalHttpAudit.Handler(auditReader);
        var result = await handler.Handle(
            new GetExternalHttpAudit.Query(null, null, null, null, 1, 20),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Entries.Should().BeEmpty();
        result.Value.Total.Should().Be(0);
        result.Value.SimulatedNote.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task GetExternalHttpAudit_WhenAuditReaderReturnsLiveData_ShouldReturnEntriesWithoutNote()
    {
        var entries = new[]
        {
            new HttpAuditEntry("trace-001", "https://api.example.com/v1/orders", "GET", 200, 45, "order-service", FixedNow),
            new HttpAuditEntry("trace-002", "https://api.example.com/v1/payments", "POST", 201, 120, "payment-service", FixedNow)
        };

        var auditReader = Substitute.For<IHttpAuditReader>();
        auditReader.QueryAsync(Arg.Any<HttpAuditFilter>(), Arg.Any<CancellationToken>())
            .Returns(new HttpAuditPage(entries, 2, IsLiveData: true));

        var handler = new GetExternalHttpAudit.Handler(auditReader);
        var result = await handler.Handle(
            new GetExternalHttpAudit.Query(null, null, null, null, 1, 20),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Entries.Should().HaveCount(2);
        result.Value.Total.Should().Be(2);
        result.Value.SimulatedNote.Should().BeEmpty();
    }

    [Fact]
    public async Task GetExternalHttpAudit_ShouldPassFilterToAuditReader()
    {
        var auditReader = Substitute.For<IHttpAuditReader>();
        auditReader.QueryAsync(Arg.Any<HttpAuditFilter>(), Arg.Any<CancellationToken>())
            .Returns(new HttpAuditPage([], 0, IsLiveData: false));

        var handler = new GetExternalHttpAudit.Handler(auditReader);
        await handler.Handle(
            new GetExternalHttpAudit.Query("api.example.com", "order-service", FixedNow.AddDays(-1), FixedNow, 2, 10),
            CancellationToken.None);

        await auditReader.Received(1).QueryAsync(
            Arg.Is<HttpAuditFilter>(f =>
                f.Destination == "api.example.com" &&
                f.Context == "order-service" &&
                f.Page == 2 &&
                f.PageSize == 10),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetExternalHttpAudit_ShouldMapEntriesCorrectly()
    {
        var entry = new HttpAuditEntry("trace-xyz", "POST /payments", "POST", 500, 350, "checkout-svc", FixedNow);

        var auditReader = Substitute.For<IHttpAuditReader>();
        auditReader.QueryAsync(Arg.Any<HttpAuditFilter>(), Arg.Any<CancellationToken>())
            .Returns(new HttpAuditPage([entry], 1, IsLiveData: true));

        var handler = new GetExternalHttpAudit.Handler(auditReader);
        var result = await handler.Handle(
            new GetExternalHttpAudit.Query(null, null, null, null, 1, 20),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var dto = result.Value.Entries.Single();
        dto.Id.Should().Be("trace-xyz");
        dto.Method.Should().Be("POST");
        dto.StatusCode.Should().Be(500);
        dto.DurationMs.Should().Be(350);
        dto.Context.Should().Be("checkout-svc");
        dto.OccurredAt.Should().Be(FixedNow);
    }

    // ── GetSupportBundles — List ──────────────────────────────────────────────────

    [Fact]
    public async Task GetSupportBundles_List_ShouldReturnEmptyListWhenNoBundlesExist()
    {
        var repository = Substitute.For<ISupportBundleRepository>();
        repository.ListAsync(null, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<SupportBundle>());

        var handler = new GetSupportBundles.Handler(repository);
        var result = await handler.Handle(new GetSupportBundles.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Bundles.Should().BeEmpty();
        result.Value.Total.Should().Be(0);
        result.Value.SimulatedNote.Should().BeEmpty();
    }

    [Fact]
    public async Task GetSupportBundles_List_ShouldReturnBundlesWithCorrectDownloadUrl()
    {
        var bundle = SupportBundle.Create(
            includesLogs: true, includesConfig: true, includesDb: false,
            tenantId: null, now: FixedNow);
        bundle.MarkGenerating(FixedNow);
        bundle.MarkReady(new byte[] { 0x50, 0x4B, 0x03, 0x04 }, FixedNow); // ZIP magic bytes

        var repository = Substitute.For<ISupportBundleRepository>();
        repository.ListAsync(null, Arg.Any<CancellationToken>())
            .Returns(new[] { bundle });

        var handler = new GetSupportBundles.Handler(repository);
        var result = await handler.Handle(new GetSupportBundles.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Bundles.Should().HaveCount(1);
        var dto = result.Value.Bundles[0];
        dto.Status.Should().Be("Ready");
        dto.DownloadUrl.Should().Contain(bundle.Id.Value.ToString());
        dto.SizeMb.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task GetSupportBundles_List_ShouldReturnNullDownloadUrlForNonReadyBundle()
    {
        var bundle = SupportBundle.Create(
            includesLogs: true, includesConfig: false, includesDb: false,
            tenantId: null, now: FixedNow);

        var repository = Substitute.For<ISupportBundleRepository>();
        repository.ListAsync(null, Arg.Any<CancellationToken>())
            .Returns(new[] { bundle });

        var handler = new GetSupportBundles.Handler(repository);
        var result = await handler.Handle(new GetSupportBundles.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Bundles.Single().DownloadUrl.Should().BeNull();
    }

    // ── GetSupportBundles — Download ──────────────────────────────────────────────

    [Fact]
    public async Task GetSupportBundles_Download_ShouldReturnNotFoundWhenBundleDoesNotExist()
    {
        var repository = Substitute.For<ISupportBundleRepository>();
        repository.GetByIdAsync(Arg.Any<SupportBundleId>(), Arg.Any<CancellationToken>())
            .Returns((SupportBundle?)null);

        var handler = new GetSupportBundles.DownloadHandler(repository);
        var result = await handler.Handle(new GetSupportBundles.DownloadBundle(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(NexTraceOne.BuildingBlocks.Core.Results.ErrorType.NotFound);
    }

    [Fact]
    public async Task GetSupportBundles_Download_ShouldReturnConflictWhenBundleNotReady()
    {
        var bundle = SupportBundle.Create(
            includesLogs: true, includesConfig: false, includesDb: false,
            tenantId: null, now: FixedNow);
        // Status is still "Pending"

        var repository = Substitute.For<ISupportBundleRepository>();
        repository.GetByIdAsync(Arg.Any<SupportBundleId>(), Arg.Any<CancellationToken>())
            .Returns(bundle);

        var handler = new GetSupportBundles.DownloadHandler(repository);
        var result = await handler.Handle(new GetSupportBundles.DownloadBundle(bundle.Id.Value), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(NexTraceOne.BuildingBlocks.Core.Results.ErrorType.Conflict);
    }

    [Fact]
    public async Task GetSupportBundles_Download_ShouldReturnFileWhenBundleIsReady()
    {
        var zipBytes = new byte[] { 0x50, 0x4B, 0x03, 0x04, 0x14, 0x00 };
        var bundle = SupportBundle.Create(
            includesLogs: true, includesConfig: true, includesDb: false,
            tenantId: null, now: FixedNow);
        bundle.MarkGenerating(FixedNow);
        bundle.MarkReady(zipBytes, FixedNow);

        var repository = Substitute.For<ISupportBundleRepository>();
        repository.GetByIdAsync(Arg.Any<SupportBundleId>(), Arg.Any<CancellationToken>())
            .Returns(bundle);

        var handler = new GetSupportBundles.DownloadHandler(repository);
        var result = await handler.Handle(new GetSupportBundles.DownloadBundle(bundle.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Content.Should().BeEquivalentTo(zipBytes);
        result.Value.FileName.Should().Contain("support-bundle");
        result.Value.FileName.Should().EndWith(".zip");
    }

    // ── SupportBundle domain entity ───────────────────────────────────────────────

    [Fact]
    public void SupportBundle_Create_ShouldHavePendingStatus()
    {
        var bundle = SupportBundle.Create(true, true, false, null, FixedNow);

        bundle.Status.Should().Be("Pending");
        bundle.IncludesLogs.Should().BeTrue();
        bundle.IncludesConfig.Should().BeTrue();
        bundle.IncludesDb.Should().BeFalse();
        bundle.ZipContent.Should().BeNull();
        bundle.SizeMb.Should().BeNull();
    }

    [Fact]
    public void SupportBundle_MarkReady_ShouldSetZipContentAndSizeMb()
    {
        var bundle = SupportBundle.Create(true, true, true, null, FixedNow);
        var content = new byte[1024]; // 1 KB
        bundle.MarkGenerating(FixedNow);
        bundle.MarkReady(content, FixedNow.AddSeconds(2));

        bundle.Status.Should().Be("Ready");
        bundle.ZipContent.Should().BeEquivalentTo(content);
        bundle.SizeMb.Should().BeGreaterThanOrEqualTo(0);
        bundle.CompletedAt.Should().Be(FixedNow.AddSeconds(2));
    }

    [Fact]
    public void SupportBundle_MarkFailed_ShouldSetFailedStatus()
    {
        var bundle = SupportBundle.Create(false, false, false, null, FixedNow);
        bundle.MarkGenerating(FixedNow);
        bundle.MarkFailed(FixedNow.AddSeconds(1));

        bundle.Status.Should().Be("Failed");
        bundle.CompletedAt.Should().Be(FixedNow.AddSeconds(1));
    }
}
