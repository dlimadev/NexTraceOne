using Xunit;

namespace NexTraceOne.IntegrationTests.Infrastructure;

/// <summary>
/// Attribute que substitui [Fact] e automaticamente pula testes quando Docker não está disponível.
/// Uso: [RequiresDockerFact] em vez de [Fact]
/// </summary>
public sealed class RequiresDockerFactAttribute : FactAttribute
{
    public RequiresDockerFactAttribute()
    {
        if (!PostgreSqlIntegrationFixture.DockerAvailable)
        {
            Skip = "Docker não está disponível. Instale Docker Desktop para executar testes de integração com PostgreSQL.";
        }
    }
}
