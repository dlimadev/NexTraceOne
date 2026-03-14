using FluentAssertions;
using NexTraceOne.BuildingBlocks.Core.Primitives;

namespace NexTraceOne.BuildingBlocks.Core.Tests.Primitives;

/// <summary>
/// Testes para a classe base ValueObject.
/// </summary>
public sealed class ValueObjectTests
{
    private sealed class Money(decimal amount, string currency) : ValueObject
    {
        public decimal Amount { get; } = amount;
        public string Currency { get; } = currency;

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Amount;
            yield return Currency;
        }
    }

    [Fact]
    public void Equals_Should_ReturnTrue_When_ComponentsAreEqual()
    {
        var money1 = new Money(10.00m, "BRL");
        var money2 = new Money(10.00m, "BRL");

        money1.Equals(money2).Should().BeTrue();
    }

    [Fact]
    public void Equals_Should_ReturnFalse_When_ComponentsDiffer()
    {
        var money1 = new Money(10.00m, "BRL");
        var money2 = new Money(10.00m, "USD");

        money1.Equals(money2).Should().BeFalse();
    }

    [Fact]
    public void OperatorEquals_Should_ReturnTrue_When_ComponentsAreEqual()
    {
        var money1 = new Money(99.90m, "BRL");
        var money2 = new Money(99.90m, "BRL");

        (money1 == money2).Should().BeTrue();
    }

    [Fact]
    public void GetHashCode_Should_BeEqual_When_ComponentsAreEqual()
    {
        var money1 = new Money(42.00m, "EUR");
        var money2 = new Money(42.00m, "EUR");

        money1.GetHashCode().Should().Be(money2.GetHashCode());
    }
}
