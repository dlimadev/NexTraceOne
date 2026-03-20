using Xunit;

namespace NexTraceOne.IntegrationTests.Infrastructure;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class ApiHostPostgreSqlCollection : ICollectionFixture<ApiHostPostgreSqlFixture>
{
    public const string Name = "apihost-postgresql-integration";
}
