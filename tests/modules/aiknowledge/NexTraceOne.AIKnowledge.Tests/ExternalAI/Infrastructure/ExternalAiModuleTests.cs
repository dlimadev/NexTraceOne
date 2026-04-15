using Microsoft.EntityFrameworkCore;

using NexTraceOne.AIKnowledge.Contracts.ExternalAI.ServiceInterfaces;
using NexTraceOne.AIKnowledge.Domain.ExternalAI.Entities;
using NexTraceOne.AIKnowledge.Infrastructure.ExternalAI.Persistence;
using NexTraceOne.AIKnowledge.Infrastructure.ExternalAI.Services;
using NexTraceOne.BuildingBlocks.Application.Abstractions;

namespace NexTraceOne.AIKnowledge.Tests.ExternalAI.Infrastructure;

public sealed class ExternalAiModuleTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 3, 28, 15, 30, 0, TimeSpan.Zero);

    [Fact]
    public async Task GetAvailableProvidersAsync_ShouldReturnProvidersOrderedByPriority()
    {
        await using var db = CreateDbContext();
        db.Providers.Add(ExternalAiProvider.Register("fallback", "http://fallback", "gpt-4o", 4096, 0.001m, 2, FixedNow));
        db.Providers.Add(ExternalAiProvider.Register("primary", "http://primary", "gpt-4o", 4096, 0.001m, 1, FixedNow));
        await db.SaveChangesAsync();

        var sut = CreateSut(db);

        var result = await sut.GetAvailableProvidersAsync(CancellationToken.None);

        result.Should().HaveCount(2);
        result[0].Name.Should().Be("primary");
        result[1].Name.Should().Be("fallback");
    }

    [Fact]
    public async Task GetAvailableProvidersAsync_WhenActivePolicyExists_ShouldExposePolicyCapabilities()
    {
        await using var db = CreateDbContext();
        db.Providers.Add(ExternalAiProvider.Register("provider", "http://provider", "gpt-4o", 4096, 0.001m, 1, FixedNow));
        db.Policies.Add(ExternalAiPolicy.Create("policy-1", "desc", 100, 1000, false, "change-analysis,error-diagnosis", FixedNow));
        await db.SaveChangesAsync();

        var sut = CreateSut(db);

        var result = await sut.GetAvailableProvidersAsync(CancellationToken.None);

        result.Should().ContainSingle();
        result[0].Capabilities.Should().BeEquivalentTo(["change-analysis", "error-diagnosis"]);
    }

    [Fact]
    public async Task GetProviderHealthAsync_WhenProviderExists_ShouldReturnHealthyStatusForActiveProvider()
    {
        await using var db = CreateDbContext();
        var provider = ExternalAiProvider.Register("provider", "http://provider", "gpt-4o", 4096, 0.001m, 1, FixedNow);
        db.Providers.Add(provider);
        await db.SaveChangesAsync();

        var sut = CreateSut(db);

        var result = await sut.GetProviderHealthAsync(provider.Id.Value, CancellationToken.None);

        result.Should().NotBeNull();
        result!.ProviderId.Should().Be(provider.Id.Value);
        result.Status.Should().Be("Healthy");
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task GetProviderHealthAsync_WhenProviderMissing_ShouldReturnNull()
    {
        await using var db = CreateDbContext();
        var sut = CreateSut(db);

        var result = await sut.GetProviderHealthAsync(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task RouteRequestAsync_WhenPreferredProviderIsActive_ShouldSelectPreferred()
    {
        await using var db = CreateDbContext();
        db.Providers.Add(ExternalAiProvider.Register("secondary", "http://secondary", "gpt-4o", 4096, 0.001m, 2, FixedNow));
        db.Providers.Add(ExternalAiProvider.Register("preferred", "http://preferred", "gpt-4o", 4096, 0.001m, 1, FixedNow));
        await db.SaveChangesAsync();

        var sut = CreateSut(db);

        var result = await sut.RouteRequestAsync("change-analysis", "preferred", ct: CancellationToken.None);

        result.Should().NotBeNull();
        result!.ProviderName.Should().Be("preferred");
        result.Reason.Should().Be("Preferred provider selected.");
        result.FallbackProviderId.Should().NotBeNull();
    }

    [Fact]
    public async Task RouteRequestAsync_WhenPreferredProviderUnavailable_ShouldFallbackToHighestPriorityActive()
    {
        await using var db = CreateDbContext();
        db.Providers.Add(ExternalAiProvider.Register("secondary", "http://secondary", "gpt-4o", 4096, 0.001m, 2, FixedNow));
        db.Providers.Add(ExternalAiProvider.Register("primary", "http://primary", "gpt-4o", 4096, 0.001m, 1, FixedNow));
        await db.SaveChangesAsync();

        var sut = CreateSut(db);

        var result = await sut.RouteRequestAsync("change-analysis", "missing-provider", ct: CancellationToken.None);

        result.Should().NotBeNull();
        result!.ProviderName.Should().Be("primary");
        result.Reason.Should().Be("Selected highest-priority active provider.");
    }

    [Fact]
    public async Task RouteRequestAsync_WhenCapabilityRequiresApproval_ShouldReturnNull()
    {
        await using var db = CreateDbContext();
        db.Providers.Add(ExternalAiProvider.Register("primary", "http://primary", "gpt-4o", 4096, 0.001m, 1, FixedNow));
        db.Policies.Add(ExternalAiPolicy.Create("approval", "desc", 100, 1000, true, "change-analysis", FixedNow));
        await db.SaveChangesAsync();

        var sut = CreateSut(db);

        var result = await sut.RouteRequestAsync("change-analysis", null, ct: CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task RouteRequestAsync_WhenEnvironmentIsProduction_ShouldReturnNull_WhenActivePolicyCoverCapability()
    {
        await using var db = CreateDbContext();
        db.Providers.Add(ExternalAiProvider.Register("primary", "http://primary", "gpt-4o", 4096, 0.001m, 1, FixedNow));
        // Policy does NOT require approval but covers the capability — production block applies
        db.Policies.Add(ExternalAiPolicy.Create("prod-guard", "desc", 100, 1000, false, "change-analysis", FixedNow));
        await db.SaveChangesAsync();

        var sut = CreateSut(db);

        var result = await sut.RouteRequestAsync("change-analysis", null, "production", CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task RouteRequestAsync_WhenEnvironmentIsNotProduction_ShouldProceed_EvenWhenPolicyCoverCapability()
    {
        await using var db = CreateDbContext();
        db.Providers.Add(ExternalAiProvider.Register("primary", "http://primary", "gpt-4o", 4096, 0.001m, 1, FixedNow));
        // Policy covers capability but does not require approval; environment is non-production
        db.Policies.Add(ExternalAiPolicy.Create("dev-policy", "desc", 100, 1000, false, "change-analysis", FixedNow));
        await db.SaveChangesAsync();

        var sut = CreateSut(db);

        var result = await sut.RouteRequestAsync("change-analysis", null, "development", CancellationToken.None);

        result.Should().NotBeNull();
        result!.ProviderName.Should().Be("primary");
    }

    private static ExternalAiDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ExternalAiDbContext>()
            .UseInMemoryDatabase($"external-ai-module-tests-{Guid.NewGuid():N}")
            .Options;

        return new ExternalAiDbContext(
            options,
            new TestCurrentTenant(),
            new TestCurrentUser(),
            new TestDateTimeProvider());
    }

    private static IExternalAiModule CreateSut(ExternalAiDbContext db)
        => new ExternalAiModule(db);

    private sealed class TestCurrentTenant : ICurrentTenant
    {
        public Guid Id => Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        public string Slug => "tests";
        public string Name => "Tests";
        public bool IsActive => true;
        public bool HasCapability(string capability) => true;
    }

    private sealed class TestCurrentUser : ICurrentUser
    {
        public string Id => "external-ai-tests-user";
        public string Name => "ExternalAI Tests";
        public string Email => "externalai.tests@nextraceone.local";
        public string? Persona { get; } = null;
        public bool IsAuthenticated => true;
        public bool HasPermission(string permission) => true;
    }

    private sealed class TestDateTimeProvider : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => FixedNow;
        public DateOnly UtcToday => DateOnly.FromDateTime(FixedNow.UtcDateTime);
    }
}
