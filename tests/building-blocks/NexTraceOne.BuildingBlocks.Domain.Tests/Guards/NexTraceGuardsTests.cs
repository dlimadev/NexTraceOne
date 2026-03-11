using System;
using Ardalis.GuardClauses;
using FluentAssertions;
using NexTraceOne.BuildingBlocks.Domain.Guards;

namespace NexTraceOne.BuildingBlocks.Domain.Tests.Guards;

/// <summary>
/// Testes para as guards específicas do domínio NexTraceOne.
/// </summary>
public sealed class NexTraceGuardsTests
{
    [Theory]
    [InlineData("1.0.0")]
    [InlineData("2.10.3-beta.1")]
    [InlineData("5.4.0+build.9")]
    public void InvalidSemanticVersion_Should_ReturnNormalizedVersion_When_ValueIsValid(string version)
    {
        var result = Guard.Against.InvalidSemanticVersion(version);

        result.Should().Be(version);
    }

    [Theory]
    [InlineData("1")]
    [InlineData("1.0")]
    [InlineData("01.0.0")]
    [InlineData("version")]
    public void InvalidSemanticVersion_Should_Throw_When_ValueIsInvalid(string version)
    {
        var act = () => Guard.Against.InvalidSemanticVersion(version);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("Integration")]
    [InlineData("QA")]
    [InlineData("UAT")]
    [InlineData("Staging")]
    [InlineData("Production")]
    public void UngovernedEnvironment_Should_ReturnEnvironment_When_ValueIsGoverned(string environment)
    {
        var result = Guard.Against.UngovernedEnvironment(environment);

        result.Should().Be(environment);
    }

    [Theory]
    [InlineData("Development")]
    [InlineData("Local")]
    [InlineData("Sandbox")]
    public void UngovernedEnvironment_Should_Throw_When_ValueIsNotGoverned(string environment)
    {
        var act = () => Guard.Against.UngovernedEnvironment(environment);

        act.Should().Throw<ArgumentException>();
    }
}
