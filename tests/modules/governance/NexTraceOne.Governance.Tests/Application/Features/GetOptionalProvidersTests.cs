using System.Linq;
using NexTraceOne.Governance.Application.Features.GetOptionalProviders;
using NexTraceOne.Integrations.Domain;

namespace NexTraceOne.Governance.Tests.Application.Features;

/// <summary>
/// Testes unitários para GetOptionalProviders (CFG-01 SystemHealthPage).
/// Verifica que o handler agrega correctamente o estado <c>IsConfigured</c> de cada
/// provider opcional e devolve contagens coerentes.
/// </summary>
public sealed class GetOptionalProvidersTests
{
    private static GetOptionalProviders.Handler CreateHandler(
        bool canary = false,
        bool backup = false,
        bool kafka = false,
        bool cloudBilling = false)
    {
        var canaryProvider = Substitute.For<ICanaryProvider>();
        canaryProvider.IsConfigured.Returns(canary);

        var backupProvider = Substitute.For<IBackupProvider>();
        backupProvider.IsConfigured.Returns(backup);

        var kafkaProducer = Substitute.For<IKafkaEventProducer>();
        kafkaProducer.IsConfigured.Returns(kafka);

        var cloudBillingProvider = Substitute.For<ICloudBillingProvider>();
        cloudBillingProvider.IsConfigured.Returns(cloudBilling);

        return new GetOptionalProviders.Handler(
            canaryProvider, backupProvider, kafkaProducer, cloudBillingProvider);
    }

    [Fact]
    public async Task Handle_AllProvidersNotConfigured_ReturnsAllNotConfigured()
    {
        var handler = CreateHandler();

        var result = await handler.Handle(new GetOptionalProviders.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ConfiguredCount.Should().Be(0);
        result.Value.TotalCount.Should().Be(4);
        result.Value.Providers.Should().HaveCount(4);
        result.Value.Providers.Should()
            .OnlyContain(p => p.Status == GetOptionalProviders.OptionalProviderStatus.NotConfigured);
    }

    [Fact]
    public async Task Handle_AllProvidersConfigured_ReturnsAllConfigured()
    {
        var handler = CreateHandler(canary: true, backup: true, kafka: true, cloudBilling: true);

        var result = await handler.Handle(new GetOptionalProviders.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ConfiguredCount.Should().Be(4);
        result.Value.TotalCount.Should().Be(4);
        result.Value.Providers.Should()
            .OnlyContain(p => p.Status == GetOptionalProviders.OptionalProviderStatus.Configured);
    }

    [Fact]
    public async Task Handle_MixedConfiguration_ReturnsCorrectCounts()
    {
        var handler = CreateHandler(canary: true, backup: false, kafka: true, cloudBilling: false);

        var result = await handler.Handle(new GetOptionalProviders.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ConfiguredCount.Should().Be(2);
        result.Value.TotalCount.Should().Be(4);

        var canary = result.Value.Providers.Single(p => p.Name == "canary");
        canary.Status.Should().Be(GetOptionalProviders.OptionalProviderStatus.Configured);

        var backup = result.Value.Providers.Single(p => p.Name == "backup");
        backup.Status.Should().Be(GetOptionalProviders.OptionalProviderStatus.NotConfigured);

        var kafka = result.Value.Providers.Single(p => p.Name == "kafka");
        kafka.Status.Should().Be(GetOptionalProviders.OptionalProviderStatus.Configured);

        var cloudBilling = result.Value.Providers.Single(p => p.Name == "cloudBilling");
        cloudBilling.Status.Should().Be(GetOptionalProviders.OptionalProviderStatus.NotConfigured);
    }

    [Fact]
    public async Task Handle_AlwaysReturnsProviderMetadata()
    {
        var handler = CreateHandler();

        var result = await handler.Handle(new GetOptionalProviders.Query(), CancellationToken.None);

        result.Value.Providers.Should().OnlyContain(p =>
            !string.IsNullOrWhiteSpace(p.Name)
            && !string.IsNullOrWhiteSpace(p.Category)
            && !string.IsNullOrWhiteSpace(p.ConfigKeyPrefix)
            && !string.IsNullOrWhiteSpace(p.DocsPath)
            && !string.IsNullOrWhiteSpace(p.Description));
    }

    [Fact]
    public async Task Handle_ProvidersAreCategorizedConsistently()
    {
        var handler = CreateHandler();

        var result = await handler.Handle(new GetOptionalProviders.Query(), CancellationToken.None);

        result.Value.Providers.Single(p => p.Name == "canary").Category.Should().Be("operations");
        result.Value.Providers.Single(p => p.Name == "backup").Category.Should().Be("operations");
        result.Value.Providers.Single(p => p.Name == "kafka").Category.Should().Be("integrations");
        result.Value.Providers.Single(p => p.Name == "cloudBilling").Category.Should().Be("finops");
    }

    [Fact]
    public async Task Handle_CheckedAtIsRecent()
    {
        var handler = CreateHandler();
        var before = DateTimeOffset.UtcNow.AddSeconds(-1);

        var result = await handler.Handle(new GetOptionalProviders.Query(), CancellationToken.None);

        var after = DateTimeOffset.UtcNow.AddSeconds(1);
        result.Value.CheckedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }
}
