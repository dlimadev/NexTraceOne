namespace NexTraceOne.IntegrationTests.Infrastructure;

public abstract class IntegrationTestBase(PostgreSqlIntegrationFixture fixture)
{
    protected PostgreSqlIntegrationFixture Fixture { get; } = fixture;

    protected Task ResetStateAsync(CancellationToken cancellationToken = default)
        => Fixture.ResetDatabasesAsync(cancellationToken);
}
