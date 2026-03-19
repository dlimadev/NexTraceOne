using Xunit;

namespace NexTraceOne.E2E.Tests.Infrastructure;

/// <summary>
/// Coleção xUnit que compartilha uma única instância do ApiE2EFixture entre todos os testes E2E.
/// DisableParallelization garante que os testes HTTP/DB não se executem em paralelo,
/// evitando conflitos de estado e contention no container PostgreSQL.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class ApiE2ECollection : ICollectionFixture<ApiE2EFixture>
{
    public const string Name = "api-e2e";
}
