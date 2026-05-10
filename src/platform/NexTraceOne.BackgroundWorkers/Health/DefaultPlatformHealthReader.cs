using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using NexTraceOne.BuildingBlocks.Infrastructure.Outbox;
using NexTraceOne.IdentityAccess.Infrastructure.Persistence;

namespace NexTraceOne.BackgroundWorkers.Health;

/// <summary>
/// Implementação real do IPlatformHealthReader.
/// Usa IdentityDbContext como proxy representativo para contagem de outbox pendente.
/// Usa DriveInfo para uso de disco da partição principal.
/// </summary>
internal sealed class DefaultPlatformHealthReader(IServiceScopeFactory serviceScopeFactory) : IPlatformHealthReader
{
    public async Task<long> CountPendingOutboxAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        return await db.Set<OutboxMessage>()
            .CountAsync(m => m.ProcessedAt == null, cancellationToken);
    }

    public DiskUsageInfo GetPrimaryDiskUsage()
    {
        try
        {
            // Usa a partição raiz em Linux; em Windows usa a partição do processo.
            var path = OperatingSystem.IsWindows()
                ? Path.GetPathRoot(Environment.CurrentDirectory) ?? "C:\\"
                : "/";
            var drive = new DriveInfo(path);
            var used = drive.TotalSize - drive.AvailableFreeSpace;
            return new DiskUsageInfo(drive.TotalSize, used);
        }
        catch
        {
            return DiskUsageInfo.Unknown;
        }
    }
}
