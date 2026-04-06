using HotChocolate.Execution.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using NexTraceOne.Catalog.API.GraphQL.Publishers;

namespace NexTraceOne.Catalog.API.GraphQL;

/// <summary>
/// Extensões de DI para o GraphQL Federation Gateway do NexTraceOne.
/// Usa o padrão [ExtendObjectType] para que cada módulo possa adicionar os seus próprios
/// resolvers ao tipo Query raiz sem criar acoplamento direto entre módulos.
/// Inclui suporte a Subscriptions real-time via WebSocket (HotChocolate in-memory).
/// </summary>
public static class CatalogGraphQLExtensions
{
    /// <summary>
    /// Adiciona o servidor GraphQL base e os resolvers do Catalog ao schema.
    /// Inclui Subscriptions real-time via WebSocket para mudanças e incidentes.
    /// Usa [ExtendObjectType("Query")] e [ExtendObjectType("Subscription")] para suporte a schema stitching.
    /// </summary>
    /// <param name="services">Container de serviços.</param>
    /// <param name="includeExceptionDetails">
    /// Quando <c>true</c>, inclui detalhes de exceção nas respostas GraphQL.
    /// Deve ser ativado apenas em Development para facilitar diagnóstico.
    /// </param>
    public static IRequestExecutorBuilder AddCatalogGraphQL(
        this IServiceCollection services,
        bool includeExceptionDetails = false)
    {
        services.AddScoped<IGraphQLEventPublisher, GraphQLEventPublisher>();

        return services
            .AddGraphQLServer()
            .AddQueryType(descriptor => descriptor.Name("Query"))
            .AddTypeExtension<CatalogQuery>()
            .AddSubscriptionType(descriptor => descriptor.Name("Subscription"))
            .AddTypeExtension<CatalogSubscription>()
            .AddInMemorySubscriptions()
            .ModifyRequestOptions(opt => opt.IncludeExceptionDetails = includeExceptionDetails);
    }

    /// <summary>
    /// Mapeia o endpoint GraphQL em /api/v1/graphql.
    /// Disponibiliza GET e POST para consultas e introspection.
    /// Inclui WebSocket middleware para subscriptions real-time.
    /// </summary>
    public static IEndpointRouteBuilder MapCatalogGraphQL(this IEndpointRouteBuilder app)
    {
        app.MapGraphQL("/api/v1/graphql");
        return app;
    }
}
