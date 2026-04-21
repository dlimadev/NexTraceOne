using NexTraceOne.IdentityAccess.Application.Features.GetMigrationHealthReport;

namespace NexTraceOne.IdentityAccess.Tests.Application.Features;

/// <summary>
/// Testes unitários para a feature GetMigrationHealthReport.
/// Verifica: módulos, ordem de upgrade, passos, verificações pós-migração e tenant nulo.
/// Wave D backlog — Upgrade path automatizado para migrações EF Core.
/// </summary>
public sealed class GetMigrationHealthReportTests
{
    private static readonly GetMigrationHealthReport.Handler Handler = new();

    [Fact]
    public async Task GetMigrationHealthReport_Handler_Returns_All_Modules()
    {
        var result = await Handler.Handle(new GetMigrationHealthReport.Query(null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalModules.Should().Be(12);
        result.Value.Modules.Should().HaveCount(12);
    }

    [Fact]
    public async Task GetMigrationHealthReport_Handler_Modules_Are_Ordered_By_UpgradeOrder()
    {
        var result = await Handler.Handle(new GetMigrationHealthReport.Query(null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var orders = result.Value.Modules.Select(m => m.UpgradeOrder).ToList();
        orders.Should().BeInAscendingOrder();
        result.Value.Modules.First().ModuleName.Should().Be("IdentityAccess");
    }

    [Fact]
    public async Task GetMigrationHealthReport_Handler_Includes_Upgrade_Steps()
    {
        var result = await Handler.Handle(new GetMigrationHealthReport.Query("tenant-1"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.UpgradeSteps.Should().NotBeEmpty();
        result.Value.UpgradeSteps.Select(s => s.Order).Should().BeInAscendingOrder();
        result.Value.UpgradeSteps.Should().Contain(s => s.Name == "Backup");
        result.Value.UpgradeSteps.Should().Contain(s => s.Name == "Migrate");
        result.Value.UpgradeSteps.Should().Contain(s => s.Name == "Verify");
        result.Value.RecommendedMaintenanceWindow.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task GetMigrationHealthReport_Handler_Returns_Post_Migration_Checks()
    {
        var result = await Handler.Handle(new GetMigrationHealthReport.Query(null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.PostMigrationChecks.Should().NotBeEmpty();
        result.Value.PostMigrationChecks.Should().Contain(c => c.Contains("/health/live"));
        result.Value.PostMigrationChecks.Should().Contain(c => c.Contains("/health/ready"));
    }

    [Fact]
    public async Task GetMigrationHealthReport_Handler_Accepts_Null_TenantId()
    {
        var result = await Handler.Handle(new GetMigrationHealthReport.Query(null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalModules.Should().BeGreaterThan(0);
    }
}
