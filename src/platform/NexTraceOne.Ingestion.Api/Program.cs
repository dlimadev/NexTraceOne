using Serilog;

/// <summary>
/// Ponto de entrada do NexTraceOne Ingestion API.
///
/// Este serviço é o entry point oficial para integrações externas:
/// - Eventos de deployment (GitHub, GitLab, Jenkins, Azure DevOps)
/// - Eventos de promoção entre ambientes
/// - Atualizações de consumidores e dependências
/// - Sinais de runtime e marcadores operacionais
/// - Sincronização de contratos de fontes externas
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

var app = builder.Build();

// Health check
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "ingestion-api" }))
    .WithTags("Health");

// ============================================================
// Deployment Events — recebe notificações de CI/CD
// ============================================================
var deployments = app.MapGroup("/api/v1/deployments")
    .WithTags("Deployments");

deployments.MapPost("/events", () =>
    Results.Accepted(null, new { message = "Deployment event received", status = "queued" }))
    .WithDescription("Receives deployment event notifications from CI/CD platforms");

// ============================================================
// Promotion Events — recebe eventos de promoção entre ambientes
// ============================================================
var promotions = app.MapGroup("/api/v1/promotions")
    .WithTags("Promotions");

promotions.MapPost("/events", () =>
    Results.Accepted(null, new { message = "Promotion event received", status = "queued" }))
    .WithDescription("Receives promotion event notifications");

// ============================================================
// Runtime Signals — recebe sinais operacionais
// ============================================================
var runtime = app.MapGroup("/api/v1/runtime")
    .WithTags("Runtime");

runtime.MapPost("/signals", () =>
    Results.Accepted(null, new { message = "Runtime signal received", status = "queued" }))
    .WithDescription("Receives runtime signals and markers from monitored services");

// ============================================================
// Consumer Updates — recebe atualizações de dependências
// ============================================================
var consumers = app.MapGroup("/api/v1/consumers")
    .WithTags("Consumers");

consumers.MapPost("/sync", () =>
    Results.Accepted(null, new { message = "Consumer update received", status = "queued" }))
    .WithDescription("Receives consumer/dependency update notifications");

// ============================================================
// Contract Sync — recebe contratos de fontes externas
// ============================================================
var contracts = app.MapGroup("/api/v1/contracts")
    .WithTags("Contracts");

contracts.MapPost("/sync", () =>
    Results.Accepted(null, new { message = "Contract sync received", status = "queued" }))
    .WithDescription("Receives contract synchronization from external sources");

app.Run();
