// ═══════════════════════════════════════════════════════════════════════════════
// NexTraceOne Lab — Inventory Service (Fake API #3)
//
// Simula um serviço de inventário com:
// - OpenTelemetry distributed tracing
// - Gestão de stock em memória com dados seed
// - Reserva e libertação de itens
// - Erros aleatórios (~5%) e latência variável
// ═══════════════════════════════════════════════════════════════════════════════

using System.Collections.Concurrent;
using System.Diagnostics;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

var serviceName = Environment.GetEnvironmentVariable("OTEL_SERVICE_NAME") ?? "inventory-service";
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
        .AddSource("InventoryService")
        .AddOtlpExporter(opts => opts.Endpoint = new Uri(otlpEndpoint)))
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddMeter("InventoryService")
        .AddOtlpExporter(opts => opts.Endpoint = new Uri(otlpEndpoint)));

builder.Logging.AddOpenTelemetry(opts =>
{
    opts.IncludeScopes = true;
    opts.IncludeFormattedMessage = true;
    opts.AddOtlpExporter(o => o.Endpoint = new Uri(otlpEndpoint));
});

var app = builder.Build();

var activitySource = new ActivitySource("InventoryService");
var random = new Random();

// Seed inventory data
var inventory = new ConcurrentDictionary<string, InventoryItem>(
    new Dictionary<string, InventoryItem>
    {
        ["prod-100"] = new() { ProductId = "prod-100", Name = "NexTraceOne Enterprise License", Stock = 500, Reserved = 0 },
        ["prod-101"] = new() { ProductId = "prod-101", Name = "Professional Support Plan", Stock = 200, Reserved = 0 },
        ["prod-102"] = new() { ProductId = "prod-102", Name = "Observability Add-on", Stock = 1000, Reserved = 0 },
        ["prod-103"] = new() { ProductId = "prod-103", Name = "AI Governance Module", Stock = 150, Reserved = 0 },
        ["prod-104"] = new() { ProductId = "prod-104", Name = "Change Intelligence Pack", Stock = 300, Reserved = 0 },
        ["prod-105"] = new() { ProductId = "prod-105", Name = "FinOps Dashboard License", Stock = 100, Reserved = 0 },
        ["prod-106"] = new() { ProductId = "prod-106", Name = "Contract Studio Pro", Stock = 250, Reserved = 0 },
        ["prod-107"] = new() { ProductId = "prod-107", Name = "IDE Extension Bundle", Stock = 800, Reserved = 0 },
        ["prod-108"] = new() { ProductId = "prod-108", Name = "Incident Response Toolkit", Stock = 50, Reserved = 0 },
        ["prod-109"] = new() { ProductId = "prod-109", Name = "Compliance Reporting Module", Stock = 75, Reserved = 0 },
    });

var reservations = new ConcurrentDictionary<string, List<ReservationItem>>();

// Health check
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = serviceName, timestamp = DateTime.UtcNow }));

// Get inventory for a product
app.MapGet("/api/inventory/{productId}", (string productId, ILogger<Program> logger) =>
{
    using var activity = activitySource.StartActivity("GetInventory");
    activity?.SetTag("inventory.product_id", productId);

    // Simulate latency (10-100ms)
    Thread.Sleep(random.Next(10, 100));

    if (inventory.TryGetValue(productId, out var item))
    {
        logger.LogInformation("Inventory check for {ProductId}: Stock={Stock}, Reserved={Reserved}",
            productId, item.Stock, item.Reserved);
        activity?.SetTag("inventory.stock", item.Stock);
        activity?.SetTag("inventory.available", item.Stock - item.Reserved);
        return Results.Ok(new
        {
            item.ProductId,
            item.Name,
            item.Stock,
            item.Reserved,
            Available = item.Stock - item.Reserved
        });
    }

    logger.LogWarning("Product not found in inventory: {ProductId}", productId);
    return Results.NotFound(new { error = "Product not found", productId });
});

// List all inventory
app.MapGet("/api/inventory", (ILogger<Program> logger) =>
{
    using var activity = activitySource.StartActivity("ListInventory");
    activity?.SetTag("inventory.total_products", inventory.Count);

    logger.LogInformation("Listing all inventory. Products: {Count}", inventory.Count);

    return Results.Ok(inventory.Values.Select(i => new
    {
        i.ProductId,
        i.Name,
        i.Stock,
        i.Reserved,
        Available = i.Stock - i.Reserved
    }).ToList());
});

// Reserve inventory items
app.MapPost("/api/inventory/reserve", async (ReserveRequest request, ILogger<Program> logger) =>
{
    using var activity = activitySource.StartActivity("ReserveInventory");
    activity?.SetTag("reservation.order_id", request.OrderId);
    activity?.SetTag("reservation.items_count", request.Items?.Count ?? 0);

    logger.LogInformation("Reserving inventory for order {OrderId}. Items: {ItemCount}",
        request.OrderId, request.Items?.Count ?? 0);

    // Simulate processing latency (20-150ms)
    await Task.Delay(random.Next(20, 150));

    // Simulate random failure (~5%)
    if (random.Next(100) < 5)
    {
        logger.LogError("Random inventory system failure during reservation for order {OrderId}", request.OrderId);
        activity?.SetStatus(ActivityStatusCode.Error, "Inventory system failure");
        return Results.StatusCode(500);
    }

    var reservedItems = new List<ReservationItem>();

    foreach (var item in request.Items ?? [])
    {
        if (!inventory.TryGetValue(item.ProductId ?? "", out var inventoryItem))
        {
            logger.LogWarning("Product {ProductId} not found for reservation", item.ProductId);
            activity?.AddEvent(new ActivityEvent("ProductNotFound",
                tags: new ActivityTagsCollection { { "product_id", item.ProductId } }));
            continue;
        }

        var available = inventoryItem.Stock - inventoryItem.Reserved;
        if (available < item.Quantity)
        {
            logger.LogWarning("Insufficient stock for {ProductId}. Available: {Available}, Requested: {Requested}",
                item.ProductId, available, item.Quantity);
            activity?.SetStatus(ActivityStatusCode.Error, "Insufficient stock");
            return Results.Conflict(new
            {
                error = "Insufficient stock",
                productId = item.ProductId,
                available,
                requested = item.Quantity
            });
        }

        inventoryItem.Reserved += item.Quantity;
        reservedItems.Add(new ReservationItem
        {
            ProductId = item.ProductId ?? "",
            Quantity = item.Quantity,
            ReservedAt = DateTime.UtcNow
        });

        activity?.AddEvent(new ActivityEvent("ItemReserved",
            tags: new ActivityTagsCollection
            {
                { "product_id", item.ProductId },
                { "quantity", item.Quantity }
            }));
    }

    var reservationId = $"RSV-{Guid.NewGuid():N}"[..12].ToUpperInvariant();
    reservations[reservationId] = reservedItems;

    logger.LogInformation("Reservation {ReservationId} created for order {OrderId}. Items: {ItemCount}",
        reservationId, request.OrderId, reservedItems.Count);
    activity?.SetTag("reservation.id", reservationId);

    return Results.Ok(new
    {
        reservationId,
        orderId = request.OrderId,
        items = reservedItems,
        status = "Reserved"
    });
});

// Release reservation
app.MapPost("/api/inventory/release", (ReleaseRequest request, ILogger<Program> logger) =>
{
    using var activity = activitySource.StartActivity("ReleaseInventory");
    activity?.SetTag("reservation.id", request.ReservationId);

    if (!reservations.TryRemove(request.ReservationId ?? "", out var items))
    {
        logger.LogWarning("Reservation not found: {ReservationId}", request.ReservationId);
        return Results.NotFound(new { error = "Reservation not found", reservationId = request.ReservationId });
    }

    foreach (var item in items)
    {
        if (inventory.TryGetValue(item.ProductId, out var inventoryItem))
        {
            inventoryItem.Reserved = Math.Max(0, inventoryItem.Reserved - item.Quantity);
        }
    }

    logger.LogInformation("Reservation {ReservationId} released. Items: {ItemCount}",
        request.ReservationId, items.Count);

    return Results.Ok(new
    {
        reservationId = request.ReservationId,
        releasedItems = items.Count,
        status = "Released"
    });
});

app.Run();

// ── Models ──────────────────────────────────────────────────────────────────
class InventoryItem
{
    public string ProductId { get; set; } = "";
    public string Name { get; set; } = "";
    public int Stock { get; set; }
    public int Reserved { get; set; }
}

record ReserveRequest(string? OrderId, List<ReserveItemRequest>? Items);
record ReserveItemRequest(string? ProductId, int Quantity);
record ReleaseRequest(string? ReservationId);

class ReservationItem
{
    public string ProductId { get; set; } = "";
    public int Quantity { get; set; }
    public DateTime ReservedAt { get; set; }
}
