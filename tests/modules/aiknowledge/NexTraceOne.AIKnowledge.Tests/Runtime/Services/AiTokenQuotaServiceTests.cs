using Microsoft.Extensions.Logging;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Infrastructure.Runtime.Services;
using NexTraceOne.BuildingBlocks.Application.Abstractions;

namespace NexTraceOne.AIKnowledge.Tests.Runtime.Services;

/// <summary>Testes unitários do AiTokenQuotaService — validação e registo de quotas.</summary>
public sealed class AiTokenQuotaServiceTests
{
    private static readonly DateTimeOffset FixedNow = new(2025, 6, 15, 14, 30, 0, TimeSpan.Zero);

    private readonly IAiTokenQuotaPolicyRepository _policyRepo = Substitute.For<IAiTokenQuotaPolicyRepository>();
    private readonly IAiTokenUsageLedgerRepository _ledgerRepo = Substitute.For<IAiTokenUsageLedgerRepository>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly ILogger<AiTokenQuotaService> _logger = Substitute.For<ILogger<AiTokenQuotaService>>();

    private AiTokenQuotaService CreateService()
    {
        _dateTimeProvider.UtcNow.Returns(FixedNow);
        return new AiTokenQuotaService(_policyRepo, _ledgerRepo, _dateTimeProvider, _logger);
    }

    [Fact]
    public async Task ValidateQuota_NoPolicy_ShouldAllow()
    {
        _policyRepo.GetForUserAsync("user-1", Arg.Any<CancellationToken>())
            .Returns(new List<AiTokenQuotaPolicy>());
        _policyRepo.GetForTenantAsync("tenant-1", Arg.Any<CancellationToken>())
            .Returns(new List<AiTokenQuotaPolicy>());

        var svc = CreateService();

        var result = await svc.ValidateQuotaAsync("user-1", "tenant-1", "openai", "gpt-4o", 500);

        result.IsAllowed.Should().BeTrue();
        result.BlockReason.Should().BeNull();
    }

    [Fact]
    public async Task ValidateQuota_WithinLimit_ShouldAllow()
    {
        var policy = AiTokenQuotaPolicy.Create(
            "quota", "desc", "user", "user-1", null, null,
            4000, 4000, 8000, 100_000, 2_000_000, 50_000_000,
            true, false, true);

        _policyRepo.GetForUserAsync("user-1", Arg.Any<CancellationToken>())
            .Returns(new List<AiTokenQuotaPolicy> { policy });
        _policyRepo.GetForTenantAsync("tenant-1", Arg.Any<CancellationToken>())
            .Returns(new List<AiTokenQuotaPolicy>());

        _ledgerRepo.GetTotalTokensForPeriodAsync("user-1", Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(1000L);

        var svc = CreateService();

        var result = await svc.ValidateQuotaAsync("user-1", "tenant-1", "openai", "gpt-4o", 500);

        result.IsAllowed.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateQuota_ExceedsDailyLimit_ShouldBlock()
    {
        var policy = AiTokenQuotaPolicy.Create(
            "daily-quota", "desc", "user", "user-1", null, null,
            4000, 4000, 8000, 10_000, 2_000_000, 50_000_000,
            true, false, true);

        _policyRepo.GetForUserAsync("user-1", Arg.Any<CancellationToken>())
            .Returns(new List<AiTokenQuotaPolicy> { policy });
        _policyRepo.GetForTenantAsync("tenant-1", Arg.Any<CancellationToken>())
            .Returns(new List<AiTokenQuotaPolicy>());

        var startOfDay = new DateTimeOffset(2025, 6, 15, 0, 0, 0, TimeSpan.Zero);
        _ledgerRepo.GetTotalTokensForPeriodAsync("user-1", startOfDay, FixedNow, Arg.Any<CancellationToken>())
            .Returns(9_800L);
        _ledgerRepo.GetTotalTokensForPeriodAsync("user-1", Arg.Any<DateTimeOffset>(), FixedNow, Arg.Any<CancellationToken>())
            .Returns(9_800L);

        var svc = CreateService();

        var result = await svc.ValidateQuotaAsync("user-1", "tenant-1", "openai", "gpt-4o", 500);

        result.IsAllowed.Should().BeFalse();
        result.BlockReason.Should().Contain("Daily token limit");
        result.PolicyName.Should().Be("daily-quota");
    }

    [Fact]
    public async Task ValidateQuota_ExceedsMonthlyLimit_ShouldBlock()
    {
        var policy = AiTokenQuotaPolicy.Create(
            "monthly-quota", "desc", "user", "user-1", null, null,
            4000, 4000, 8000, 10_000_000, 50_000, 50_000_000,
            true, false, true);

        _policyRepo.GetForUserAsync("user-1", Arg.Any<CancellationToken>())
            .Returns(new List<AiTokenQuotaPolicy> { policy });
        _policyRepo.GetForTenantAsync("tenant-1", Arg.Any<CancellationToken>())
            .Returns(new List<AiTokenQuotaPolicy>());

        var startOfDay = new DateTimeOffset(2025, 6, 15, 0, 0, 0, TimeSpan.Zero);
        var startOfMonth = new DateTimeOffset(2025, 6, 1, 0, 0, 0, TimeSpan.Zero);

        // Daily usage is fine
        _ledgerRepo.GetTotalTokensForPeriodAsync("user-1", startOfDay, FixedNow, Arg.Any<CancellationToken>())
            .Returns(1_000L);
        // Monthly usage exceeds
        _ledgerRepo.GetTotalTokensForPeriodAsync("user-1", startOfMonth, FixedNow, Arg.Any<CancellationToken>())
            .Returns(49_800L);

        var svc = CreateService();

        var result = await svc.ValidateQuotaAsync("user-1", "tenant-1", "openai", "gpt-4o", 500);

        result.IsAllowed.Should().BeFalse();
        result.BlockReason.Should().Contain("Monthly token limit");
        result.PolicyName.Should().Be("monthly-quota");
    }

    [Fact]
    public async Task RecordUsage_ShouldCreateLedgerEntry()
    {
        var svc = CreateService();

        await svc.RecordUsageAsync(
            "user-1", "tenant-1", "openai", "gpt-4o", "GPT-4o",
            promptTokens: 200, completionTokens: 300,
            "req-001", "exec-001", "Success", 1234.5);

        await _ledgerRepo.Received(1).AddAsync(
            Arg.Is<AiTokenUsageLedger>(e =>
                e.UserId == "user-1" &&
                e.TenantId == "tenant-1" &&
                e.ProviderId == "openai" &&
                e.ModelId == "gpt-4o" &&
                e.ModelName == "GPT-4o" &&
                e.PromptTokens == 200 &&
                e.CompletionTokens == 300 &&
                e.TotalTokens == 500 &&
                e.RequestId == "req-001" &&
                e.ExecutionId == "exec-001" &&
                e.Status == "Success"),
            Arg.Any<CancellationToken>());
    }
}
