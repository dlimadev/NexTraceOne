using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using NexTraceOne.BuildingBlocks.Application.Abstractions;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Automation.Persistence;

/// <summary>
/// Design-time DbContext factory for EF Core migrations of AutomationDbContext.
/// </summary>
internal sealed class AutomationDbContextDesignTimeFactory : IDesignTimeDbContextFactory<AutomationDbContext>
{
    public AutomationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AutomationDbContext>();
        var connectionString = Environment.GetEnvironmentVariable("NEXTRACEONE_CONNECTION_STRING")
            ?? "Host=localhost;Port=5432;Database=nextraceone;Username=nextraceone;Password=";

        optionsBuilder.UseNpgsql(connectionString,
            npgsql => npgsql.MigrationsAssembly(typeof(AutomationDbContext).Assembly.FullName));

        return new AutomationDbContext(
            optionsBuilder.Options,
            new DesignTimeCurrentTenant(),
            new DesignTimeCurrentUser(),
            new DesignTimeDateTimeProvider());
    }

    private sealed class DesignTimeCurrentTenant : ICurrentTenant
    {
        public Guid Id => Guid.Empty;
        public string Slug => "design-time";
        public string Name => "Design Time";
        public bool IsActive => false;
        public bool HasCapability(string capability) => false;
    }

    private sealed class DesignTimeCurrentUser : ICurrentUser
    {
        public string Id => "design-time";
        public string Name => "Design Time";
        public string Email => "design-time@nextraceone.local";
        public bool IsAuthenticated => false;
        public bool HasPermission(string permission) => false;
    }

    private sealed class DesignTimeDateTimeProvider : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
        public DateOnly UtcToday => DateOnly.FromDateTime(UtcNow.UtcDateTime);
    }
}
