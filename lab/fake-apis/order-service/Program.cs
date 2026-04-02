// ═══════════════════════════════════════════════════════════════════════════════
// NexTraceOne Lab — Order Service (Fake API #1)
//
// Simula um serviço de gestão de encomendas com:
// - OpenTelemetry distributed tracing
// - Chamadas em cadeia para payment-service e inventory-service
// - Erros aleatórios para simular cenários reais (~5% de falha)
// - Latência variável para simular condições reais
// ═══════════════════════════════════════════════════════════════════════════════

using System.Collections.Concurrent;
using System.Diagnostics;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

var serviceName = Environment.GetEnvironmentVariable("OTEL_SERVICE_NAME") ?? "order-service";
var otlpEndpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT") ?? "http://localhost:4317";

// OpenTelemetry configuration
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
        .AddSource("OrderService")
        .AddOtlpExporter(opts => opts.Endpoint = new Uri(otlpEndpoint)))
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddMeter("OrderService")
        .AddOtlpExporter(opts => opts.Endpoint = new Uri(otlpEndpoint)));

builder.Logging.AddOpenTelemetry(opts =>
{
    opts.IncludeScopes = true;
    opts.IncludeFormattedMessage = true;
    opts.AddOtlpExporter(o => o.Endpoint = new Uri(otlpEndpoint));
});

builder.Services.AddHttpClient("PaymentService", client =>
{
    var baseUrl = builder.Configuration["PaymentService:BaseUrl"] ?? "http://localhost:5020";
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(10);
});

builder.Services.AddHttpClient("InventoryService", client =>
{
    var baseUrl = builder.Configuration["InventoryService:BaseUrl"] ?? "http://localhost:5030";
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(10);
});

var app = builder.Build();

var activitySource = new ActivitySource("OrderService");
var orders = new ConcurrentDictionary<string, Order>();
var random = new Random();

// Health check
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = serviceName, timestamp = DateTime.UtcNow }));

// List all orders
app.MapGet("/api/orders", (ILogger<Program> logger) =>
{
    using var activity = activitySource.StartActivity("ListOrders");
    activity?.SetTag("order.count", orders.Count);

    logger.LogInformation("Listing all orders. Count: {OrderCount}", orders.Count);

    return Results.Ok(orders.Values.OrderByDescending(o => o.CreatedAt).ToList());
});

// Get order by ID
app.MapGet("/api/orders/{id}", (string id, ILogger<Program> logger) =>
{
    using var activity = activitySource.StartActivity("GetOrder");
    activity?.SetTag("order.id", id);

    if (orders.TryGetValue(id, out var order))
    {
        logger.LogInformation("Order found: {OrderId}", id);
        return Results.Ok(order);
    }

    logger.LogWarning("Order not found: {OrderId}", id);
    return Results.NotFound(new { error = "Order not found", orderId = id });
});

// Create order (triggers payment + inventory calls)
app.MapPost("/api/orders", async (
    CreateOrderRequest request,
    IHttpClientFactory httpClientFactory,
    ILogger<Program> logger) =>
{
    using var activity = activitySource.StartActivity("CreateOrder");

    var orderId = $"ORD-{Guid.NewGuid():N}"[..12].ToUpperInvariant();
    activity?.SetTag("order.id", orderId);
    activity?.SetTag("order.customer_id", request.CustomerId);
    activity?.SetTag("order.items_count", request.Items?.Count ?? 0);

    logger.LogInformation("Creating order {OrderId} for customer {CustomerId} with {ItemCount} items",
        orderId, request.CustomerId, request.Items?.Count ?? 0);

    // Simulate random latency (50-300ms)
    await Task.Delay(random.Next(50, 300));

    // Simulate random failure (~5%)
    if (random.Next(100) < 5)
    {
        logger.LogError("Random failure creating order {OrderId}", orderId);
        activity?.SetStatus(ActivityStatusCode.Error, "Random failure during order creation");
        return Results.StatusCode(500);
    }

    // Step 1: Reserve inventory
    try
    {
        var inventoryClient = httpClientFactory.CreateClient("InventoryService");
        var reserveRequest = new { orderId, items = request.Items };
        var inventoryResponse = await inventoryClient.PostAsJsonAsync("/api/inventory/reserve", reserveRequest);

        if (!inventoryResponse.IsSuccessStatusCode)
        {
            logger.LogWarning("Inventory reservation failed for order {OrderId}. Status: {StatusCode}",
                orderId, inventoryResponse.StatusCode);
            activity?.AddEvent(new ActivityEvent("InventoryReservationFailed"));
            return Results.Conflict(new { error = "Insufficient stock", orderId });
        }

        logger.LogInformation("Inventory reserved for order {OrderId}", orderId);
        activity?.AddEvent(new ActivityEvent("InventoryReserved"));
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to call inventory service for order {OrderId}", orderId);
        activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
    }

    // Step 2: Process payment
    try
    {
        var paymentClient = httpClientFactory.CreateClient("PaymentService");
        var totalAmount = (request.Items?.Sum(i => i.Quantity * 29.99m) ?? 0m);
        var paymentRequest = new { orderId, amount = totalAmount, currency = "EUR", customerId = request.CustomerId };
        var paymentResponse = await paymentClient.PostAsJsonAsync("/api/payments", paymentRequest);

        if (!paymentResponse.IsSuccessStatusCode)
        {
            logger.LogWarning("Payment failed for order {OrderId}. Status: {StatusCode}",
                orderId, paymentResponse.StatusCode);
            activity?.AddEvent(new ActivityEvent("PaymentFailed"));
            return Results.UnprocessableEntity(new { error = "Payment failed", orderId });
        }

        logger.LogInformation("Payment processed for order {OrderId}", orderId);
        activity?.AddEvent(new ActivityEvent("PaymentProcessed"));
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to call payment service for order {OrderId}", orderId);
        activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
    }

    // Create order
    var order = new Order
    {
        Id = orderId,
        CustomerId = request.CustomerId ?? "unknown",
        Items = request.Items ?? [],
        Status = "Confirmed",
        CreatedAt = DateTime.UtcNow,
        TotalAmount = request.Items?.Sum(i => i.Quantity * 29.99m) ?? 0m
    };

    orders[orderId] = order;

    logger.LogInformation("Order {OrderId} created successfully. Total: {TotalAmount} EUR",
        orderId, order.TotalAmount);
    activity?.SetTag("order.status", order.Status);
    activity?.SetTag("order.total_amount", order.TotalAmount);

    return Results.Created($"/api/orders/{orderId}", order);
});

// Cancel order
app.MapDelete("/api/orders/{id}", (string id, ILogger<Program> logger) =>
{
    using var activity = activitySource.StartActivity("CancelOrder");
    activity?.SetTag("order.id", id);

    if (orders.TryGetValue(id, out var order))
    {
        order.Status = "Cancelled";
        logger.LogInformation("Order {OrderId} cancelled", id);
        return Results.Ok(order);
    }

    logger.LogWarning("Cannot cancel: order not found {OrderId}", id);
    return Results.NotFound(new { error = "Order not found", orderId = id });
});

app.Run();

// ── Models ──────────────────────────────────────────────────────────────────
record CreateOrderRequest(string? CustomerId, List<OrderItem>? Items);

record OrderItem(string? ProductId, int Quantity);

class Order
{
    public string Id { get; set; } = "";
    public string CustomerId { get; set; } = "";
    public List<OrderItem> Items { get; set; } = [];
    public string Status { get; set; } = "Pending";
    public DateTime CreatedAt { get; set; }
    public decimal TotalAmount { get; set; }
}
