using FluentAssertions;
using Microsoft.Extensions.Logging;
using NexTraceOne.ApiHost;
using NSubstitute;

namespace NexTraceOne.IntegrationTests.Startup;
using NexTraceOne.IntegrationTests.Infrastructure;

/// <summary>
/// OPS-03 — unit tests for <see cref="OptionalProviderStartupLogger"/>.
///
/// This class is fixture-free (doesn't require PostgreSQL or the ApiHost test fixture)
/// because the helper is a pure function that only receives (logger, environment, statuses).
/// </summary>
public sealed class OptionalProviderStartupLoggerTests
{
    private readonly ILogger _logger = Substitute.For<ILogger>();

    private static IReadOnlyDictionary<string, bool> Statuses(
        bool canary = false, bool backup = false, bool kafka = false, bool cloudBilling = false, bool saml = false)
        => new Dictionary<string, bool>
        {
            ["canary"] = canary,
            ["backup"] = backup,
            ["kafka"] = kafka,
            ["cloudBilling"] = cloudBilling,
            ["saml"] = saml,
        };

    private int CountAtLevel(LogLevel level)
        => _logger.ReceivedCalls()
            .Count(c => c.GetMethodInfo().Name == "Log"
                        && c.GetArguments()[0] is LogLevel lvl && lvl == level);

    [RequiresDockerFact]
    public void LogProviderStatuses_ProductionAllMissing_LogsWarningPerProvider()
    {
        OptionalProviderStartupLogger.LogProviderStatuses(_logger, "Production", Statuses());

        // Five providers not configured → five warnings
        CountAtLevel(LogLevel.Warning).Should().Be(5);
    }

    [RequiresDockerFact]
    public void LogProviderStatuses_DevelopmentAllMissing_EmitsNoWarnings()
    {
        OptionalProviderStartupLogger.LogProviderStatuses(_logger, "Development", Statuses());

        CountAtLevel(LogLevel.Warning).Should().Be(0);
        // The summary + the aggregated not-configured line are logged as Information.
        CountAtLevel(LogLevel.Information).Should().BeGreaterThanOrEqualTo(1);
    }

    [RequiresDockerFact]
    public void LogProviderStatuses_AllConfigured_EmitsNoWarnings()
    {
        OptionalProviderStartupLogger.LogProviderStatuses(
            _logger,
            "Production",
            Statuses(canary: true, backup: true, kafka: true, cloudBilling: true, saml: true));

        CountAtLevel(LogLevel.Warning).Should().Be(0);
        CountAtLevel(LogLevel.Information).Should().BeGreaterThanOrEqualTo(1);
    }

    [RequiresDockerFact]
    public void LogProviderStatuses_StagingMixed_LogsWarningOnlyForMissing()
    {
        OptionalProviderStartupLogger.LogProviderStatuses(
            _logger,
            "Staging",
            Statuses(canary: true, backup: false, kafka: true, cloudBilling: false, saml: true));

        // backup + cloudBilling not configured → 2 warnings; canary + kafka + saml are configured
        CountAtLevel(LogLevel.Warning).Should().Be(2);
    }

    [RequiresDockerFact]
    public void LogProviderStatuses_EnvironmentNameIsCaseInsensitiveForDevelopment()
    {
        OptionalProviderStartupLogger.LogProviderStatuses(_logger, "development", Statuses());

        // Case-insensitive match → treated as development, no warnings.
        CountAtLevel(LogLevel.Warning).Should().Be(0);
    }

    [RequiresDockerFact]
    public void LogProviderStatuses_NullLogger_Throws()
    {
        var act = () => OptionalProviderStartupLogger.LogProviderStatuses(null!, "Production", Statuses());

        act.Should().Throw<ArgumentNullException>();
    }

    [RequiresDockerFact]
    public void LogProviderStatuses_NullEnvironment_Throws()
    {
        var act = () => OptionalProviderStartupLogger.LogProviderStatuses(_logger, null!, Statuses());

        act.Should().Throw<ArgumentNullException>();
    }

    [RequiresDockerFact]
    public void LogProviderStatuses_NullStatuses_Throws()
    {
        var act = () => OptionalProviderStartupLogger.LogProviderStatuses(_logger, "Production", null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
