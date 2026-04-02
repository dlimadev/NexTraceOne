// ═══════════════════════════════════════════════════════════════════════════════
// NexTraceOne Lab — Payment Service (Fake API #2)
//
// Simula um serviço de pagamentos com:
// - OpenTelemetry distributed tracing
// - Validação de pagamentos com regras simuladas
// - Chamadas ao inventory-service para validação cruzada
// - Erros aleatórios (~5%) e latência variável
// ═══════════════════════════════════════════════════════════════════════════════

using System.Collections.Concurrent;
using System.Diagnostics;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

var serviceName = Environment.GetEnvironmentVariable("OTEL_SERVICE_NAME") ?? "payment-service";
var otlpEndpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT") ?? "http://localhost:4317";

builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r
        .AddService(serviceName: serviceName, serviceVersion: "1.0.0")
        .AddAttributes(new Dictionary<string, object>
        {
            ["deployment.environment"] = "laboratory",
            ["service.namespace"] = "nextraceone-lab"
        }))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation(opts =>
        {
            opts.RecordException = true;
            opts.Filter = ctx => !ctx.Request.Path.StartsWithSegments("/health");
        })
        .AddHttpClientInstrumentation(opts => opts.RecordException = true)
        .AddSource("PaymentService")
        .AddOtlpExporter(opts => opts.Endpoint = new Uri(otlpEndpoint)))
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddMeter("PaymentService")
        .AddOtlpExporter(opts => opts.Endpoint = new Uri(otlpEndpoint)));

builder.Logging.AddOpenTelemetry(opts =>
{
    opts.IncludeScopes = true;
    opts.IncludeFormattedMessage = true;
    opts.AddOtlpExporter(o => o.Endpoint = new Uri(otlpEndpoint));
});

builder.Services.AddHttpClient("InventoryService", client =>
{
    var baseUrl = builder.Configuration["InventoryService:BaseUrl"] ?? "http://localhost:5030";
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(10);
});

var app = builder.Build();

var activitySource = new ActivitySource("PaymentService");
var payments = new ConcurrentDictionary<string, Payment>();
var random = new Random();

// Health check
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = serviceName, timestamp = DateTime.UtcNow }));

// Process payment
app.MapPost("/api/payments", async (
    ProcessPaymentRequest request,
    IHttpClientFactory httpClientFactory,
    ILogger<Program> logger) =>
{
    using var activity = activitySource.StartActivity("ProcessPayment");

    var paymentId = $"PAY-{Guid.NewGuid():N}"[..12].ToUpperInvariant();
    activity?.SetTag("payment.id", paymentId);
    activity?.SetTag("payment.order_id", request.OrderId);
    activity?.SetTag("payment.amount", request.Amount);
    activity?.SetTag("payment.currency", request.Currency);

    logger.LogInformation("Processing payment {PaymentId} for order {OrderId}. Amount: {Amount} {Currency}",
        paymentId, request.OrderId, request.Amount, request.Currency);

    // Simulate payment processing latency (100-500ms)
    await Task.Delay(random.Next(100, 500));

    // Simulate random failure (~5%)
    if (random.Next(100) < 5)
    {
        logger.LogError("Payment gateway timeout for {PaymentId}", paymentId);
        activity?.SetStatus(ActivityStatusCode.Error, "Payment gateway timeout");
        return Results.StatusCode(503);
    }

    // Validate amount
    if (request.Amount <= 0)
    {
        logger.LogWarning("Invalid amount {Amount} for payment {PaymentId}", request.Amount, paymentId);
        activity?.SetStatus(ActivityStatusCode.Error, "Invalid amount");
        return Results.BadRequest(new { error = "Invalid amount", paymentId });
    }

    // Simulate fraud check (~3% rejection)
    if (random.Next(100) < 3)
    {
        logger.LogWarning("Fraud check failed for payment {PaymentId}. Order: {OrderId}",
            paymentId, request.OrderId);
        activity?.AddEvent(new ActivityEvent("FraudCheckFailed"));

        var rejectedPayment = new Payment
        {
            Id = paymentId,
            OrderId = request.OrderId ?? "",
            Amount = request.Amount,
            Currency = request.Currency ?? "EUR",
            Status = "Rejected",
            Reason = "Fraud check failed",
            ProcessedAt = DateTime.UtcNow
        };
        payments[paymentId] = rejectedPayment;
        return Results.UnprocessableEntity(rejectedPayment);
    }

    // Call inventory service for cross-validation (optional enrichment)
    try
    {
        var inventoryClient = httpClientFactory.CreateClient("InventoryService");
        var inventoryResponse = await inventoryClient.GetAsync("/api/inventory/prod-100");
        if (inventoryResponse.IsSuccessStatusCode)
        {
            activity?.AddEvent(new ActivityEvent("InventoryValidated"));
        }
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Inventory cross-validation failed for payment {PaymentId}. Continuing...", paymentId);
    }

    var payment = new Payment
    {
        Id = paymentId,
        OrderId = request.OrderId ?? "",
        Amount = request.Amount,
        Currency = request.Currency ?? "EUR",
        Status = "Approved",
        Reason = "Payment approved",
        ProcessedAt = DateTime.UtcNow
    };

    payments[paymentId] = payment;

    logger.LogInformation("Payment {PaymentId} approved for order {OrderId}. Amount: {Amount} {Currency}",
        paymentId, request.OrderId, request.Amount, request.Currency);
    activity?.SetTag("payment.status", payment.Status);

    return Results.Ok(payment);
});

// Get payment status
app.MapGet("/api/payments/{id}", (string id, ILogger<Program> logger) =>
{
    using var activity = activitySource.StartActivity("GetPayment");
    activity?.SetTag("payment.id", id);

    if (payments.TryGetValue(id, out var payment))
    {
        logger.LogInformation("Payment found: {PaymentId}", id);
        return Results.Ok(payment);
    }

    logger.LogWarning("Payment not found: {PaymentId}", id);
    return Results.NotFound(new { error = "Payment not found", paymentId = id });
});

app.Run();

// ── Models ──────────────────────────────────────────────────────────────────
record ProcessPaymentRequest(string? OrderId, decimal Amount, string? Currency, string? CustomerId);

class Payment
{
    public string Id { get; set; } = "";
    public string OrderId { get; set; } = "";
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "EUR";
    public string Status { get; set; } = "Pending";
    public string Reason { get; set; } = "";
    public DateTime ProcessedAt { get; set; }
}
