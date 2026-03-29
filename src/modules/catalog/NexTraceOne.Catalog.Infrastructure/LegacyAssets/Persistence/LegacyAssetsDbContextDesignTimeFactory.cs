using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using NexTraceOne.BuildingBlocks.Application.Abstractions;

namespace NexTraceOne.Catalog.Infrastructure.LegacyAssets.Persistence;

/// <summary>
/// Fábrica de DbContext para uso em tempo de design (EF Core migrations).
/// Fornece implementações mínimas de ICurrentTenant, ICurrentUser e IDateTimeProvider.
/// </summary>
internal sealed class LegacyAssetsDbContextDesignTimeFactory : IDesignTimeDbContextFactory<LegacyAssetsDbContext>
{
    /// <summary>Cria instância do LegacyAssetsDbContext configurada para geração de migrations.</summary>
    public LegacyAssetsDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<LegacyAssetsDbContext>();
        var connectionString = Environment.GetEnvironmentVariable("NEXTRACEONE_CONNECTION_STRING")
            ?? "Host=localhost;Port=5432;Database=nextraceone;Username=nextraceone;Password=";

        optionsBuilder.UseNpgsql(connectionString,
            npgsql => npgsql.MigrationsAssembly(typeof(LegacyAssetsDbContext).Assembly.FullName));

        return new LegacyAssetsDbContext(
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
