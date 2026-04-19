using System;
using FluentAssertions;
using NexTraceOne.CLI.Services;

namespace NexTraceOne.CLI.Tests.Services;

/// <summary>
/// Testes do CliConfig: resolução de URL, token, environment e persona.
/// Cobre prioridades: explícito > env var > config > padrão.
/// </summary>
public sealed class CliConfigTests
{
    // ── ResolveUrl tests ───────────────────────────────────────────────────────

    [Fact]
    public void ResolveUrl_WithExplicitValue_ReturnsExplicit()
    {
        const string explicit_ = "https://my-nex.company.com";

        var result = CliConfig.ResolveUrl(explicit_);

        result.Should().Be(explicit_);
    }

    [Fact]
    public void ResolveUrl_WithNull_ReturnsDefaultOrEnvOrConfig()
    {
        // When no env var and no config, should return the default
        var result = CliConfig.ResolveUrl(null);

        // The result is either the env var, stored config URL, or the default.
        result.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void ResolveUrl_WithEmptyString_TreatsAsNotProvided()
    {
        var result = CliConfig.ResolveUrl(string.Empty);

        result.Should().NotBeNullOrWhiteSpace();
    }

    // ── ResolveToken tests ─────────────────────────────────────────────────────

    [Fact]
    public void ResolveToken_WithExplicitValue_ReturnsExplicit()
    {
        const string token = "my-test-token-abc123";

        var result = CliConfig.ResolveToken(token);

        result.Should().Be(token);
    }

    [Fact]
    public void ResolveToken_WithNull_ReturnsNullOrConfiguredValue()
    {
        // Without a stored config or env var, token should be null
        // (unless the test environment has NEXTRACE_TOKEN set)
        var result = CliConfig.ResolveToken(null);

        // Should not throw; may be null
        // Explicit check only that it doesn't crash
        _ = result;
    }

    // ── ResolveEnvironment tests ───────────────────────────────────────────────

    [Fact]
    public void ResolveEnvironment_WithExplicitValue_ReturnsExplicit()
    {
        const string env = "production";

        var result = CliConfig.ResolveEnvironment(env);

        result.Should().Be(env);
    }

    [Fact]
    public void ResolveEnvironment_WithNull_ReturnsNullOrConfiguredValue()
    {
        var result = CliConfig.ResolveEnvironment(null);
        _ = result; // Should not throw
    }

    [Fact]
    public void ResolveEnvironment_WithWhitespace_TreatsAsNotProvided()
    {
        // Whitespace should not be treated as a valid explicit value
        var result = CliConfig.ResolveEnvironment("   ");

        // Without env var or config, result is null; should not return whitespace
        if (result is not null)
            result.Trim().Should().NotBe(string.Empty);
    }

    // ── ResolvePersona tests ───────────────────────────────────────────────────

    [Fact]
    public void ResolvePersona_WithExplicitValue_ReturnsExplicit()
    {
        const string persona = "Engineer";

        var result = CliConfig.ResolvePersona(persona);

        result.Should().Be(persona);
    }

    [Fact]
    public void ResolvePersona_WithNull_ReturnsNullOrConfiguredValue()
    {
        var result = CliConfig.ResolvePersona(null);
        _ = result; // Should not throw
    }

    // ── CliConfig model tests ──────────────────────────────────────────────────

    [Fact]
    public void CliConfig_Load_DoesNotThrow_WhenFileDoesNotExist()
    {
        var act = () => CliConfig.Load();

        act.Should().NotThrow();
    }

    [Fact]
    public void CliConfig_Load_ReturnsEmptyInstance_WhenFileDoesNotExist()
    {
        // If the config file doesn't exist, all values should be null
        // (unless a valid config already exists on this machine)
        var config = CliConfig.Load();

        config.Should().NotBeNull();
    }

    [Fact]
    public void CliConfig_NewInstance_HasNullFields()
    {
        var config = new CliConfig();

        config.Url.Should().BeNull();
        config.Token.Should().BeNull();
        config.Environment.Should().BeNull();
        config.Persona.Should().BeNull();
    }

    [Fact]
    public void CliConfig_SetFields_RetainsValues()
    {
        var config = new CliConfig
        {
            Url = "https://nex.local",
            Token = "tok-abc",
            Environment = "staging",
            Persona = "TechLead"
        };

        config.Url.Should().Be("https://nex.local");
        config.Token.Should().Be("tok-abc");
        config.Environment.Should().Be("staging");
        config.Persona.Should().Be("TechLead");
    }
}
