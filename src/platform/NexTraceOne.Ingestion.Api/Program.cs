using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using NexTraceOne.BuildingBlocks.Application;
using NexTraceOne.BuildingBlocks.Infrastructure.HealthChecks;
using NexTraceOne.BuildingBlocks.Observability.HealthChecks;
using NexTraceOne.BuildingBlocks.Security;
using NexTraceOne.BuildingBlocks.Security.Authentication;
using NexTraceOne.BuildingBlocks.Security.MultiTenancy;
using NexTraceOne.ChangeGovernance.API.ChangeIntelligence.Endpoints;
using NexTraceOne.Governance.Infrastructure;
using NexTraceOne.Governance.Infrastructure.Persistence;
using NexTraceOne.Ingestion.Api;
using NexTraceOne.Ingestion.Api.Endpoints;
using NexTraceOne.Ingestion.Api.Security;
using NexTraceOne.Integrations.Application;
using NexTraceOne.Integrations.Infrastructure;
using NexTraceOne.OperationalIntelligence.API.Cost.Endpoints;
using NexTraceOne.OperationalIntelligence.API.Runtime.Endpoints;
using NexTraceOne.OperationalIntelligence.Application.Incidents;
using NexTraceOne.OperationalIntelligence.Infrastructure.Incidents;
using Scalar.AspNetCore;
using Serilog;
using System.Diagnostics;

/// <summary>
/// Ponto de entrada do NexTraceOne Ingestion API.
///
/// Este serviço é o entry point oficial para integrações externas:
/// - Eventos de deployment (GitHub, GitLab, Jenkins, Azure DevOps)
/// - Eventos de promoção entre ambientes
/// - Atualizações de consumidores e dependências
/// - Sinais de runtime e snapshots estruturados de saúde
/// - Sincronização de contratos de fontes externas
/// - Ingestão de commits de repositórios VCS
/// - Ingestão de releases, feature flags, canary rollouts e observações pós-release
/// - Ingestão de snapshots de custo de infraestrutura (FinOps)
/// - Criação e consulta de incidentes operacionais
///
/// Separado do ApiHost principal para:
/// 1. Isolamento de carga — integrações externas não afetam o portal interno
/// 2. Políticas de rate-limiting diferenciadas por origem
/// 3. Autenticação via API Key (sem sessão de usuário)
/// 4. Preparação para futura extração como serviço independente
/// </summary>
var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, config) =>
    config.ReadFrom.Configuration(context.Configuration));

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler(_ => { });
builder.Services.AddBuildingBlocksApplication(builder.Configuration);
builder.Services.AddNexTraceHealthChecks();

// OpenAPI — documentação do serviço de ingestão para consumidores externos
builder.Services.AddOpenApi("ingestion", options =>
{
    options.AddDocumentTransformer((document, context, _) =>
    {
        document.Info.Title = "NexTraceOne Ingestion API";
        document.Info.Version = "v1";
        document.Info.Description = """
            Entry point oficial para integrações externas com o NexTraceOne.

            Permite que pipelines CI/CD, agentes de runtime e sistemas externos
            notifiquem o NexTraceOne sobre eventos relevantes e consultem dados operacionais:

            ## Escrita (integrations:write)

            - **Deployments**: eventos de deploy originados em GitHub, GitLab, Jenkins, Azure DevOps
            - **Promotions**: promoções de release entre ambientes (ex: staging → production)
            - **Runtime Signals**: sinais genéricos de serviços em execução
            - **Runtime Snapshots**: snapshots estruturados de saúde (latência, CPU, memória, error rate)
            - **Consumers**: atualizações de dependências e consumidores de contratos
            - **Contracts**: sincronização de contratos de APIs e eventos de fontes externas
            - **Commits**: ingestão de commits de repositórios VCS (GitHub, GitLab, Azure DevOps)
            - **Releases**: ingestão de releases externas, feature flags, canary rollouts, observações e rollbacks
            - **FinOps**: snapshots de custo de infraestrutura (AWS, Azure, GCP)
            - **Incidents**: criação de incidentes a partir de sistemas externos de alerta

            ## Leitura (integrations:read)

            - **Releases**: listar e consultar releases, advisories, blast radius e post-release reviews
            - **Services**: saúde de runtime por serviço e ambiente
            - **Incidents**: listar e consultar detalhe de incidentes

            ## Autenticação

            Todas as rotas requerem autenticação via **API Key** no header `X-Api-Key`.
            - Operações de escrita requerem a permissão `integrations:write`.
            - Operações de leitura requerem a permissão `integrations:read`.
            """;
        return System.Threading.Tasks.Task.CompletedTask;
    });
});
builder.Services.AddHealthChecks()
    .AddCheck<DbContextConnectivityHealthCheck<GovernanceDbContext>>(
        "governance-db",
        failureStatus: HealthStatus.Unhealthy,
        tags: ["ready", "health"]);
builder.Services.AddBuildingBlocksSecurity(builder.Configuration);
builder.Services.AddAuthorization(options =>
{
    // Política de escrita — usada por endpoints de ingestão (POST)
    options.AddPolicy(IngestionApiSecurity.PolicyName, policy =>
    {
        policy.AuthenticationSchemes.Add(ApiKeyAuthenticationOptions.SchemeName);
        policy.RequireAuthenticatedUser();
        policy.RequireClaim("auth_method", "api_key");
        policy.RequireClaim("permissions", IngestionApiSecurity.RequiredPermission);
    });

    // Política de leitura — usada por endpoints de consulta (GET)
    options.AddPolicy(IngestionApiSecurity.ReadPolicyName, policy =>
    {
        policy.AuthenticationSchemes.Add(ApiKeyAuthenticationOptions.SchemeName);
        policy.RequireAuthenticatedUser();
        policy.RequireClaim("auth_method", "api_key");
        policy.RequireClaim("permissions", IngestionApiSecurity.RequiredReadPermission);
    });
});

// Add Governance Infrastructure for persistence
builder.Services.AddGovernanceInfrastructure(builder.Configuration);

// Add Integrations module — repositórios de conectores, execuções e fontes de ingestão
builder.Services.AddIntegrationsApplication(builder.Configuration);
builder.Services.AddIntegrationsInfrastructure(builder.Configuration);

// Add ChangeGovernance module — correlação de deploys, Change Intelligence e advisories
builder.Services.AddChangeIntelligenceModule(builder.Configuration);

// Add OperationalIntelligence modules — runtime snapshots, custo e incidentes
builder.Services.AddRuntimeIntelligenceModule(builder.Configuration);
builder.Services.AddCostIntelligenceModule(builder.Configuration);
builder.Services.AddIncidentsApplication(builder.Configuration);
builder.Services.AddIncidentsInfrastructure(builder.Configuration);

var app = builder.Build();

app.ValidateIngestionSecurityConfiguration();

app.UseHttpsRedirection();
app.Use(async (context, next) =>
{
    var headers = context.Response.Headers;
    headers["X-Content-Type-Options"] = "nosniff";
    headers["X-Frame-Options"] = "DENY";
    headers["X-XSS-Protection"] = "0";
    headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=()";
    headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'";
    headers["Cache-Control"] = "no-store";
    headers["Pragma"] = "no-cache";

    if (!app.Environment.IsDevelopment())
    {
        headers["Strict-Transport-Security"] = "max-age=63072000; includeSubDomains; preload";
    }

    await next();
});
app.UseExceptionHandler();
app.UseStatusCodePages(async statusCodeContext =>
{
    var httpContext = statusCodeContext.HttpContext;
    if (httpContext.Response.HasStarted)
    {
        return;
    }

    var statusCode = httpContext.Response.StatusCode;
    if (statusCode is not (StatusCodes.Status401Unauthorized or StatusCodes.Status403Forbidden))
    {
        return;
    }

    var detail = statusCode == StatusCodes.Status401Unauthorized
        ? "A valid ingestion API key is required."
        : "The authenticated ingestion client is not authorized for this operation.";

    await Results.Problem(
        statusCode: statusCode,
        title: statusCode == StatusCodes.Status401Unauthorized ? "Unauthorized" : "Forbidden",
        detail: detail,
        extensions: new Dictionary<string, object?>
        {
            ["traceId"] = Activity.Current?.Id ?? httpContext.TraceIdentifier
        })
        .ExecuteAsync(httpContext);
});
app.Use(async (context, next) =>
{
    var correlationId = IngestionCorrelationHelper.ResolveCorrelationId(context);
    context.Response.Headers[IngestionApiSecurity.CorrelationHeaderName] = correlationId;
    await next();
});
app.UseAuthentication();
app.UseMiddleware<TenantResolutionMiddleware>();
app.UseAuthorization();

// OpenAPI + Scalar — disponível em todos os ambientes (Ingestion API é consumida por sistemas externos)
app.MapOpenApi("/openapi/{documentName}.json");
app.MapScalarApiReference(options =>
{
    options.Title = "NexTraceOne Ingestion API";
    options.Theme = ScalarTheme.BluePlanet;
    options.DefaultOpenAllTags = true;
});

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = HealthCheckResponseWriter.WriteResponse
}).AllowAnonymous();

app.MapHealthChecks("/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = HealthCheckResponseWriter.WriteResponse
}).AllowAnonymous();

app.MapHealthChecks("/live", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live"),
    ResponseWriter = HealthCheckResponseWriter.WriteResponse
}).AllowAnonymous();

IngestionEndpointModule.MapEndpoints(app);

app.Run();
