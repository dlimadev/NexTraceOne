#!/usr/bin/env bash
# ============================================================================
# NexTraceOne — Engineering Graph: Script de Seed via API
# ============================================================================
# Popula o módulo Engineering Graph com massa de teste realista via API HTTP.
# Pré-requisitos:
#   - API do NexTraceOne rodando (dotnet run --project src/platform/NexTraceOne.ApiHost)
#   - curl disponível
#   - jq disponível (para formatação e extração de IDs)
#
# Uso:
#   chmod +x scripts/seed/engineering-graph/01-seed-via-api.sh
#   ./scripts/seed/engineering-graph/01-seed-via-api.sh [BASE_URL]
#
# O BASE_URL padrão é http://localhost:5000/api/v1/engineeringgraph
# ============================================================================

set -euo pipefail

BASE_URL="${1:-http://localhost:5000/api/v1/engineeringgraph}"
echo "=== NexTraceOne Engineering Graph — Seed Data ==="
echo "Base URL: $BASE_URL"
echo ""

# ── Função auxiliar para POST com tratamento de erro ───────────────────────
post() {
  local endpoint="$1"
  local data="$2"
  local label="$3"
  local response
  response=$(curl -s -w "\n%{http_code}" -X POST "$BASE_URL$endpoint" \
    -H "Content-Type: application/json" \
    -d "$data")
  local http_code
  http_code=$(echo "$response" | tail -1)
  local body
  body=$(echo "$response" | sed '$d')
  if [[ "$http_code" -ge 200 && "$http_code" -lt 300 ]]; then
    echo "  ✅ $label (HTTP $http_code)"
    echo "$body"
  elif [[ "$http_code" == "409" ]]; then
    echo "  ⚠️  $label — já existe (HTTP 409)"
    echo "$body"
  else
    echo "  ❌ $label — falha (HTTP $http_code)"
    echo "$body"
  fi
}

# ── Função auxiliar para GET ───────────────────────────────────────────────
get() {
  local endpoint="$1"
  curl -s "$BASE_URL$endpoint" -H "Accept: application/json"
}

echo "──────────────────────────────────────────────────"
echo "1. Registrando Domínios e Serviços"
echo "──────────────────────────────────────────────────"

# Domínio: Payments
SVC_PAYMENTS=$(post "/services" \
  '{"name":"payments-service","domain":"Payments","teamName":"Payments Team"}' \
  "payments-service (Payments)")
SVC_PAYMENTS_ID=$(echo "$SVC_PAYMENTS" | jq -r '.id // empty')

# Domínio: Identity
SVC_IDENTITY=$(post "/services" \
  '{"name":"identity-service","domain":"Identity","teamName":"Identity Squad"}' \
  "identity-service (Identity)")
SVC_IDENTITY_ID=$(echo "$SVC_IDENTITY" | jq -r '.id // empty')

# Domínio: Commerce
SVC_ORDERS=$(post "/services" \
  '{"name":"orders-service","domain":"Commerce","teamName":"Core Commerce"}' \
  "orders-service (Commerce)")
SVC_ORDERS_ID=$(echo "$SVC_ORDERS" | jq -r '.id // empty')

SVC_CATALOG=$(post "/services" \
  '{"name":"catalog-service","domain":"Commerce","teamName":"Core Commerce"}' \
  "catalog-service (Commerce)")
SVC_CATALOG_ID=$(echo "$SVC_CATALOG" | jq -r '.id // empty')

SVC_CHECKOUT=$(post "/services" \
  '{"name":"checkout-service","domain":"Commerce","teamName":"Checkout Squad"}' \
  "checkout-service (Commerce)")
SVC_CHECKOUT_ID=$(echo "$SVC_CHECKOUT" | jq -r '.id // empty')

# Domínio: Billing
SVC_BILLING=$(post "/services" \
  '{"name":"billing-service","domain":"Billing","teamName":"Billing Team"}' \
  "billing-service (Billing)")
SVC_BILLING_ID=$(echo "$SVC_BILLING" | jq -r '.id // empty')

SVC_INVOICING=$(post "/services" \
  '{"name":"invoicing-service","domain":"Billing","teamName":"Billing Team"}' \
  "invoicing-service (Billing)")
SVC_INVOICING_ID=$(echo "$SVC_INVOICING" | jq -r '.id // empty')

# Domínio: Notifications
SVC_NOTIFICATIONS=$(post "/services" \
  '{"name":"notifications-service","domain":"Notifications","teamName":"Platform Team"}' \
  "notifications-service (Notifications)")
SVC_NOTIFICATIONS_ID=$(echo "$SVC_NOTIFICATIONS" | jq -r '.id // empty')

# Domínio: Analytics (serviço isolado — poucos consumidores)
SVC_ANALYTICS=$(post "/services" \
  '{"name":"analytics-service","domain":"Analytics","teamName":"Data Team"}' \
  "analytics-service (Analytics)")
SVC_ANALYTICS_ID=$(echo "$SVC_ANALYTICS" | jq -r '.id // empty')

# Domínio: Gateway (serviço crítico — muitos consumidores)
SVC_GATEWAY=$(post "/services" \
  '{"name":"api-gateway","domain":"Platform","teamName":"Platform Team"}' \
  "api-gateway (Platform)")
SVC_GATEWAY_ID=$(echo "$SVC_GATEWAY" | jq -r '.id // empty')

echo ""
echo "──────────────────────────────────────────────────"
echo "2. Registrando APIs"
echo "──────────────────────────────────────────────────"

# APIs do Payments
API_PAYMENTS=$(post "/apis" \
  "{\"name\":\"Payments API\",\"routePattern\":\"/api/v1/payments\",\"version\":\"2.1.0\",\"visibility\":\"Internal\",\"ownerServiceId\":\"$SVC_PAYMENTS_ID\"}" \
  "Payments API")
API_PAYMENTS_ID=$(echo "$API_PAYMENTS" | jq -r '.id // empty')

API_PAYMENTS_WEBHOOK=$(post "/apis" \
  "{\"name\":\"Payments Webhook\",\"routePattern\":\"/webhooks/payments\",\"version\":\"1.0.0\",\"visibility\":\"Public\",\"ownerServiceId\":\"$SVC_PAYMENTS_ID\"}" \
  "Payments Webhook")
API_PAYMENTS_WEBHOOK_ID=$(echo "$API_PAYMENTS_WEBHOOK" | jq -r '.id // empty')

# APIs do Identity
API_AUTH=$(post "/apis" \
  "{\"name\":\"Auth API\",\"routePattern\":\"/api/v1/auth\",\"version\":\"3.0.0\",\"visibility\":\"Internal\",\"ownerServiceId\":\"$SVC_IDENTITY_ID\"}" \
  "Auth API")
API_AUTH_ID=$(echo "$API_AUTH" | jq -r '.id // empty')

# APIs do Orders
API_ORDERS=$(post "/apis" \
  "{\"name\":\"Orders API\",\"routePattern\":\"/api/v1/orders\",\"version\":\"1.5.0\",\"visibility\":\"Internal\",\"ownerServiceId\":\"$SVC_ORDERS_ID\"}" \
  "Orders API")
API_ORDERS_ID=$(echo "$API_ORDERS" | jq -r '.id // empty')

# APIs do Catalog
API_CATALOG=$(post "/apis" \
  "{\"name\":\"Catalog API\",\"routePattern\":\"/api/v1/catalog\",\"version\":\"2.0.0\",\"visibility\":\"Public\",\"ownerServiceId\":\"$SVC_CATALOG_ID\"}" \
  "Catalog API")
API_CATALOG_ID=$(echo "$API_CATALOG" | jq -r '.id // empty')

# APIs do Checkout
API_CHECKOUT=$(post "/apis" \
  "{\"name\":\"Checkout API\",\"routePattern\":\"/api/v1/checkout\",\"version\":\"1.2.0\",\"visibility\":\"Internal\",\"ownerServiceId\":\"$SVC_CHECKOUT_ID\"}" \
  "Checkout API")
API_CHECKOUT_ID=$(echo "$API_CHECKOUT" | jq -r '.id // empty')

# APIs do Billing
API_BILLING=$(post "/apis" \
  "{\"name\":\"Billing API\",\"routePattern\":\"/api/v1/billing\",\"version\":\"1.0.0\",\"visibility\":\"Internal\",\"ownerServiceId\":\"$SVC_BILLING_ID\"}" \
  "Billing API")
API_BILLING_ID=$(echo "$API_BILLING" | jq -r '.id // empty')

# APIs do Invoicing
API_INVOICING=$(post "/apis" \
  "{\"name\":\"Invoicing API\",\"routePattern\":\"/api/v1/invoicing\",\"version\":\"1.1.0\",\"visibility\":\"Internal\",\"ownerServiceId\":\"$SVC_INVOICING_ID\"}" \
  "Invoicing API")
API_INVOICING_ID=$(echo "$API_INVOICING" | jq -r '.id // empty')

# APIs do Notifications
API_NOTIFICATIONS=$(post "/apis" \
  "{\"name\":\"Notifications API\",\"routePattern\":\"/api/v1/notifications\",\"version\":\"1.0.0\",\"visibility\":\"Internal\",\"ownerServiceId\":\"$SVC_NOTIFICATIONS_ID\"}" \
  "Notifications API")
API_NOTIFICATIONS_ID=$(echo "$API_NOTIFICATIONS" | jq -r '.id // empty')

# APIs do Analytics (isolado)
API_ANALYTICS=$(post "/apis" \
  "{\"name\":\"Analytics API\",\"routePattern\":\"/api/v1/analytics\",\"version\":\"0.9.0\",\"visibility\":\"Internal\",\"ownerServiceId\":\"$SVC_ANALYTICS_ID\"}" \
  "Analytics API")
API_ANALYTICS_ID=$(echo "$API_ANALYTICS" | jq -r '.id // empty')

# APIs do Gateway (crítico — muitos consumidores)
API_GATEWAY=$(post "/apis" \
  "{\"name\":\"API Gateway\",\"routePattern\":\"/api/v1/gateway\",\"version\":\"4.0.0\",\"visibility\":\"Public\",\"ownerServiceId\":\"$SVC_GATEWAY_ID\"}" \
  "API Gateway")
API_GATEWAY_ID=$(echo "$API_GATEWAY" | jq -r '.id // empty')

echo ""
echo "──────────────────────────────────────────────────"
echo "3. Mapeando Relações de Consumo (Dependências)"
echo "──────────────────────────────────────────────────"

# Checkout consome Orders, Payments, Catalog (relações diretas)
post "/apis/$API_ORDERS_ID/consumers" \
  '{"consumerName":"checkout-service","consumerKind":"Service","consumerEnvironment":"Production","sourceType":"CatalogImport","externalReference":"catalog/checkout-orders.yaml","confidenceScore":0.95}' \
  "checkout → Orders API"

post "/apis/$API_PAYMENTS_ID/consumers" \
  '{"consumerName":"checkout-service","consumerKind":"Service","consumerEnvironment":"Production","sourceType":"CatalogImport","externalReference":"catalog/checkout-payments.yaml","confidenceScore":0.92}' \
  "checkout → Payments API"

post "/apis/$API_CATALOG_ID/consumers" \
  '{"consumerName":"checkout-service","consumerKind":"Service","consumerEnvironment":"Production","sourceType":"OpenTelemetry","externalReference":"otel:trace:checkout-catalog-001","confidenceScore":0.88}' \
  "checkout → Catalog API"

# Billing consome Payments e Orders
post "/apis/$API_PAYMENTS_ID/consumers" \
  '{"consumerName":"billing-service","consumerKind":"Service","consumerEnvironment":"Production","sourceType":"CatalogImport","externalReference":"catalog/billing-payments.yaml","confidenceScore":0.97}' \
  "billing → Payments API"

post "/apis/$API_ORDERS_ID/consumers" \
  '{"consumerName":"billing-service","consumerKind":"Service","consumerEnvironment":"Production","sourceType":"OpenTelemetry","externalReference":"otel:trace:billing-orders-001","confidenceScore":0.85}' \
  "billing → Orders API"

# Invoicing consome Billing
post "/apis/$API_BILLING_ID/consumers" \
  '{"consumerName":"invoicing-service","consumerKind":"Service","consumerEnvironment":"Production","sourceType":"CatalogImport","externalReference":"catalog/invoicing-billing.yaml","confidenceScore":0.90}' \
  "invoicing → Billing API"

# Notifications consome Orders e Payments (eventos)
post "/apis/$API_ORDERS_ID/consumers" \
  '{"consumerName":"notifications-service","consumerKind":"Service","consumerEnvironment":"Production","sourceType":"OpenTelemetry","externalReference":"otel:trace:notif-orders-001","confidenceScore":0.78}' \
  "notifications → Orders API"

post "/apis/$API_PAYMENTS_ID/consumers" \
  '{"consumerName":"notifications-service","consumerKind":"Service","consumerEnvironment":"Production","sourceType":"OpenTelemetry","externalReference":"otel:trace:notif-payments-001","confidenceScore":0.75}' \
  "notifications → Payments API"

# Todos consomem Auth API (dependência transversal crítica)
for consumer in "payments-service" "orders-service" "catalog-service" "checkout-service" "billing-service" "invoicing-service" "notifications-service" "analytics-service"; do
  post "/apis/$API_AUTH_ID/consumers" \
    "{\"consumerName\":\"$consumer\",\"consumerKind\":\"Service\",\"consumerEnvironment\":\"Production\",\"sourceType\":\"CatalogImport\",\"externalReference\":\"catalog/${consumer}-auth.yaml\",\"confidenceScore\":0.99}" \
    "$consumer → Auth API"
done

# Gateway é consumido por todos os serviços (cenário de nó crítico)
for consumer in "payments-service" "orders-service" "catalog-service" "checkout-service" "billing-service" "invoicing-service" "notifications-service" "analytics-service" "identity-service"; do
  post "/apis/$API_GATEWAY_ID/consumers" \
    "{\"consumerName\":\"$consumer\",\"consumerKind\":\"Service\",\"consumerEnvironment\":\"Production\",\"sourceType\":\"CatalogImport\",\"externalReference\":\"catalog/${consumer}-gateway.yaml\",\"confidenceScore\":0.98}" \
    "$consumer → API Gateway"
done

# Analytics consome Orders (baixa confiança — inferido)
post "/apis/$API_ORDERS_ID/consumers" \
  '{"consumerName":"analytics-service","consumerKind":"Job","consumerEnvironment":"Production","sourceType":"OpenTelemetry","externalReference":"otel:trace:analytics-orders-batch","confidenceScore":0.55}' \
  "analytics → Orders API (baixa confiança)"

echo ""
echo "──────────────────────────────────────────────────"
echo "4. Criando Snapshots Temporais"
echo "──────────────────────────────────────────────────"

post "/snapshots" \
  '{"label":"Baseline — Estado Inicial","createdBy":"seed-script"}' \
  "Snapshot: Baseline"

echo ""
echo "=== Seed concluído com sucesso ==="
echo "Dados inseridos:"
echo "  - 10 serviços em 6 domínios"
echo "  - 11 APIs"
echo "  - ~25 relações de consumo"
echo "  - 1 snapshot temporal baseline"
echo ""
echo "Cenários cobertos:"
echo "  - Serviço crítico (api-gateway) com 9 consumidores"
echo "  - Serviço isolado (analytics-service) com poucos consumidores"
echo "  - Dependência transversal (Auth API consumida por todos)"
echo "  - Relações diretas e transitivas (checkout → orders → billing)"
echo "  - Confiança variável (0.55 a 0.99)"
echo "  - Múltiplos domínios (Payments, Identity, Commerce, Billing, Notifications, Analytics)"
echo "  - Múltiplos times (Payments Team, Identity Squad, Core Commerce, etc.)"
