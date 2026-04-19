using Microsoft.Extensions.Logging.Abstractions;
using NexTraceOne.Integrations.Infrastructure.CloudBilling;

namespace NexTraceOne.Integrations.Tests.Application.Features;

/// <summary>
/// Testes unitários para NullCloudBillingProvider.
/// Valida que a implementação nula retorna valores correctos e não lança excepções.
/// </summary>
public sealed class CloudBillingProviderTests
{
    private readonly NullCloudBillingProvider _sut =
        new(NullLogger<NullCloudBillingProvider>.Instance);

    [Fact]
    public void NullCloudBillingProvider_IsConfigured_ReturnsFalse()
    {
        _sut.IsConfigured.Should().BeFalse();
    }

    [Fact]
    public async Task NullCloudBillingProvider_FetchBillingRecords_ReturnsEmpty()
    {
        var records = await _sut.FetchBillingRecordsAsync("2025-01", CancellationToken.None);

        records.Should().NotBeNull();
        records.Should().BeEmpty();
    }

    [Fact]
    public void NullCloudBillingProvider_ProviderName_IsNull()
    {
        _sut.ProviderName.Should().Be("null");
    }
}
