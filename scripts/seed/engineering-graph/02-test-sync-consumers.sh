#!/usr/bin/env bash
# ============================================================================
# NexTraceOne — Engineering Graph: Teste de Integração Inbound Externa
# ============================================================================
# Testa o endpoint de sincronização de consumidores vindos de sistemas externos.
# Pré-requisitos:
#   - API do NexTraceOne rodando
#   - Massa de teste do 01-seed-via-api.sh já executada
#   - curl e jq disponíveis
#
# Uso:
#   chmod +x scripts/seed/engineering-graph/02-test-sync-consumers.sh
#   ./scripts/seed/engineering-graph/02-test-sync-consumers.sh [BASE_URL]
# ============================================================================

set -euo pipefail

BASE_URL="${1:-http://localhost:5000/api/v1/engineeringgraph}"
echo "=== NexTraceOne — Teste de Integração Inbound (SyncConsumers) ==="
echo "Base URL: $BASE_URL"
echo ""

# Obter o grafo atual para extrair IDs de APIs existentes
GRAPH=$(curl -s "$BASE_URL/graph" -H "Accept: application/json")
FIRST_API_ID=$(echo "$GRAPH" | jq -r '.apis[0].apiAssetId // empty')

if [[ -z "$FIRST_API_ID" ]]; then
  echo "❌ Nenhuma API encontrada. Execute o script 01-seed-via-api.sh primeiro."
  exit 1
fi

echo "Usando API ID: $FIRST_API_ID"
echo ""

echo "──────────────────────────────────────────────────"
echo "Teste 1: Sync com um novo consumidor (deve criar)"
echo "──────────────────────────────────────────────────"
RESPONSE=$(curl -s -w "\n%{http_code}" -X POST "$BASE_URL/integration/v1/consumers/sync" \
  -H "Content-Type: application/json" \
  -d "{
    \"items\": [
      {
        \"apiAssetId\": \"$FIRST_API_ID\",
        \"consumerName\": \"external-crm-service\",
        \"consumerKind\": \"Service\",
        \"consumerEnvironment\": \"Production\",
        \"externalReference\": \"kong-gateway/crm-integration\",
        \"confidenceScore\": 0.92
      }
    ],
    \"sourceSystem\": \"KongGateway\",
    \"correlationId\": \"test-001\"
  }")
HTTP_CODE=$(echo "$RESPONSE" | tail -1)
BODY=$(echo "$RESPONSE" | sed '$d')
echo "HTTP: $HTTP_CODE"
echo "$BODY" | jq .
echo ""

echo "──────────────────────────────────────────────────"
echo "Teste 2: Sync idempotente (deve atualizar)"
echo "──────────────────────────────────────────────────"
RESPONSE=$(curl -s -w "\n%{http_code}" -X POST "$BASE_URL/integration/v1/consumers/sync" \
  -H "Content-Type: application/json" \
  -d "{
    \"items\": [
      {
        \"apiAssetId\": \"$FIRST_API_ID\",
        \"consumerName\": \"external-crm-service\",
        \"consumerKind\": \"Service\",
        \"consumerEnvironment\": \"Production\",
        \"externalReference\": \"kong-gateway/crm-integration-v2\",
        \"confidenceScore\": 0.95
      }
    ],
    \"sourceSystem\": \"KongGateway\",
    \"correlationId\": \"test-002\"
  }")
HTTP_CODE=$(echo "$RESPONSE" | tail -1)
BODY=$(echo "$RESPONSE" | sed '$d')
echo "HTTP: $HTTP_CODE"
echo "$BODY" | jq .
echo ""

echo "──────────────────────────────────────────────────"
echo "Teste 3: Sync com API inexistente (deve reportar falha)"
echo "──────────────────────────────────────────────────"
RESPONSE=$(curl -s -w "\n%{http_code}" -X POST "$BASE_URL/integration/v1/consumers/sync" \
  -H "Content-Type: application/json" \
  -d '{
    "items": [
      {
        "apiAssetId": "00000000-0000-0000-0000-000000000000",
        "consumerName": "ghost-service",
        "consumerKind": "Service",
        "consumerEnvironment": "Production",
        "externalReference": "ref/ghost",
        "confidenceScore": 0.80
      }
    ],
    "sourceSystem": "TestHarness",
    "correlationId": "test-003"
  }')
HTTP_CODE=$(echo "$RESPONSE" | tail -1)
BODY=$(echo "$RESPONSE" | sed '$d')
echo "HTTP: $HTTP_CODE"
echo "$BODY" | jq .
echo ""

echo "──────────────────────────────────────────────────"
echo "Teste 4: Sync em lote misto (sucesso + falha)"
echo "──────────────────────────────────────────────────"
RESPONSE=$(curl -s -w "\n%{http_code}" -X POST "$BASE_URL/integration/v1/consumers/sync" \
  -H "Content-Type: application/json" \
  -d "{
    \"items\": [
      {
        \"apiAssetId\": \"$FIRST_API_ID\",
        \"consumerName\": \"external-erp-service\",
        \"consumerKind\": \"Service\",
        \"consumerEnvironment\": \"Staging\",
        \"externalReference\": \"erp/integration\",
        \"confidenceScore\": 0.88
      },
      {
        \"apiAssetId\": \"00000000-0000-0000-0000-000000000001\",
        \"consumerName\": \"missing-service\",
        \"consumerKind\": \"Service\",
        \"consumerEnvironment\": \"Production\",
        \"externalReference\": \"ref/missing\",
        \"confidenceScore\": 0.70
      }
    ],
    \"sourceSystem\": \"CICDPipeline\",
    \"correlationId\": \"test-004\"
  }")
HTTP_CODE=$(echo "$RESPONSE" | tail -1)
BODY=$(echo "$RESPONSE" | sed '$d')
echo "HTTP: $HTTP_CODE"
echo "$BODY" | jq .
echo ""

echo "=== Testes de integração concluídos ==="
