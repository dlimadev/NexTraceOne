using System.Globalization;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.IdentityAccess.Application.Abstractions;

namespace NexTraceOne.IdentityAccess.Infrastructure.Services;

/// <summary>
/// Configuração da integração de billing com o Stripe.
/// SecretKey/WebhookSecret devem vir de user-secrets ou variáveis de ambiente.
/// PriceIds mapeia o nome do plano (Starter/Professional/Enterprise) para o
/// price id recorrente criado no dashboard do Stripe.
/// </summary>
public sealed class StripeBillingOptions
{
    public const string SectionName = "Billing:Stripe";

    /// <summary>Se a integração de billing está habilitada.</summary>
    public bool Enabled { get; set; }

    /// <summary>Chave secreta da API do Stripe (sk_...).</summary>
    public string? SecretKey { get; set; }

    /// <summary>Segredo de assinatura do webhook (whsec_...).</summary>
    public string? WebhookSecret { get; set; }

    /// <summary>URL de retorno após pagamento concluído.</summary>
    public string SuccessUrl { get; set; } = "https://app.nextraceone.com/saas/licensing?checkout=success";

    /// <summary>URL de retorno após cancelamento do checkout.</summary>
    public string CancelUrl { get; set; } = "https://app.nextraceone.com/saas/licensing?checkout=cancelled";

    /// <summary>Mapa plano → price id recorrente do Stripe.</summary>
    public Dictionary<string, string> PriceIds { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Tolerância (segundos) para o timestamp da assinatura do webhook.</summary>
    public int WebhookToleranceSeconds { get; set; } = 300;
}

/// <summary>
/// Gateway de billing via API REST do Stripe (sem SDK — apenas HttpClient).
/// Cria sessões de Checkout em modo subscription e verifica assinaturas de
/// webhook no esquema t/v1 (HMAC-SHA256 sobre "{t}.{payload}").
/// </summary>
internal sealed class StripeBillingGateway(
    IHttpClientFactory httpClientFactory,
    IOptions<StripeBillingOptions> options,
    IDateTimeProvider clock,
    ILogger<StripeBillingGateway> logger) : IBillingGateway
{
    private const string CheckoutSessionsUrl = "https://api.stripe.com/v1/checkout/sessions";

    /// <inheritdoc/>
    public async Task<Result<string>> CreateCheckoutSessionUrlAsync(
        Guid tenantId,
        string plan,
        CancellationToken cancellationToken = default)
    {
        var settings = options.Value;

        if (!settings.Enabled || string.IsNullOrWhiteSpace(settings.SecretKey))
            return Error.Business("billing.disabled", "Billing is not configured for this environment.");

        if (!settings.PriceIds.TryGetValue(plan, out var priceId) || string.IsNullOrWhiteSpace(priceId))
            return Error.Validation("billing.unknownPlan", $"No price configured for plan '{plan}'.");

        var form = new Dictionary<string, string>
        {
            ["mode"] = "subscription",
            ["line_items[0][price]"] = priceId,
            ["line_items[0][quantity]"] = "1",
            ["success_url"] = settings.SuccessUrl,
            ["cancel_url"] = settings.CancelUrl,
            ["client_reference_id"] = tenantId.ToString(),
            ["metadata[tenant_id]"] = tenantId.ToString(),
            ["metadata[plan]"] = plan,
        };

        var client = httpClientFactory.CreateClient("NexTraceOneStripe");
        using var request = new HttpRequestMessage(HttpMethod.Post, CheckoutSessionsUrl)
        {
            Content = new FormUrlEncodedContent(form)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.SecretKey);

        var response = await client.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogError(
                "Stripe checkout session creation failed for tenant {TenantId}: HTTP {StatusCode}",
                tenantId, (int)response.StatusCode);
            return Error.Business("billing.checkoutFailed", "The payment provider rejected the checkout request.");
        }

        using var document = JsonDocument.Parse(body);
        var url = document.RootElement.TryGetProperty("url", out var urlProp) ? urlProp.GetString() : null;

        if (string.IsNullOrWhiteSpace(url))
            return Error.Business("billing.checkoutFailed", "The payment provider returned no checkout URL.");

        logger.LogInformation("Stripe checkout session created for tenant {TenantId} (plan {Plan})", tenantId, plan);
        return Result<string>.Success(url);
    }

    /// <inheritdoc/>
    public bool VerifyWebhookSignature(string payload, string signatureHeader)
    {
        var secret = options.Value.WebhookSecret;
        if (string.IsNullOrWhiteSpace(secret))
            return false;

        long timestamp = 0;
        var signatures = new List<string>();

        foreach (var part in signatureHeader.Split(','))
        {
            var kv = part.Split('=', 2);
            if (kv.Length != 2)
                continue;
            var key = kv[0].Trim();
            if (key == "t")
                _ = long.TryParse(kv[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out timestamp);
            else if (key == "v1")
                signatures.Add(kv[1].Trim());
        }

        if (timestamp == 0 || signatures.Count == 0)
            return false;

        var age = Math.Abs(clock.UtcNow.ToUnixTimeSeconds() - timestamp);
        if (age > options.Value.WebhookToleranceSeconds)
            return false;

        var signedPayload = $"{timestamp.ToString(CultureInfo.InvariantCulture)}.{payload}";
        var expected = Convert.ToHexStringLower(
            HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(signedPayload)));

        return signatures.Any(s =>
            CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(s),
                Encoding.UTF8.GetBytes(expected)));
    }
}
