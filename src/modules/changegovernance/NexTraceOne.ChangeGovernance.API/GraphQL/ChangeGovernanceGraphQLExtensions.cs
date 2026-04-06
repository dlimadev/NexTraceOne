using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace NexTraceOne.ChangeGovernance.API.GraphQL;

/// <summary>
/// Extensões de DI para integrar dados de Change Intelligence no GraphQL Federation Gateway.
/// Usa AddTypeExtension para schema stitching sem criar acoplamento direto ao Catalog.API.
/// </summary>
public static class ChangeGovernanceGraphQLExtensions
{
    /// <summary>
    /// Adiciona os resolvers de Change Intelligence ao schema GraphQL existente.
    /// Deve ser encadeado após <c>AddCatalogGraphQL()</c> no ApiHost.
    /// </summary>
    public static IRequestExecutorBuilder AddChangeGovernanceQueryExtension(
        this IRequestExecutorBuilder builder)
    {
        return builder.AddTypeExtension<ChangeGovernanceQuery>();
    }
}
