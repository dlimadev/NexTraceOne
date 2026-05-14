#!/bin/bash
# ============================================================================
# Script de Exportação de Dados do ClickHouse para Elasticsearch
# NexTraceOne - Operational Intelligence Module
# ============================================================================
# Este script exporta dados históricos do ClickHouse e importa no Elasticsearch.
# Útil para migração entre backends ou backup.
# 
# Uso: ./export-clickhouse-to-elastic.sh [days] [output_dir]
# Exemplo: ./export-clickhouse-to-elastic.sh 30 /tmp/telemetry-export
# ============================================================================

set -euo pipefail

# Cores para output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Configurações padrão
DAYS_TO_EXPORT=${1:-7}
OUTPUT_DIR=${2:-"/tmp/telemetry-export"}
CLICKHOUSE_HOST="${CLICKHOUSE_HOST:-localhost}"
CLICKHOUSE_PORT="${CLICKHOUSE_PORT:-9000}"
CLICKHOUSE_DB="${CLICKHOUSE_DB:-nextrace_telemetry}"
ELASTIC_ENDPOINT="${ELASTIC_ENDPOINT:-http://localhost:9200}"
BATCH_SIZE=1000

log_info() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

log_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

log_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Criar diretório de output
mkdir -p "$OUTPUT_DIR"

log_info "═══════════════════════════════════════════════════════════"
log_info "Exportação de Telemetria: ClickHouse → Elasticsearch"
log_info "Período: últimos ${DAYS_TO_EXPORT} dias"
log_info "Output: ${OUTPUT_DIR}"
log_info "═══════════════════════════════════════════════════════════"

# Passo 1: Validar conectividade
log_info "Passo 1: Validando conectividade..."

if ! command -v clickhouse-client &> /dev/null; then
    log_error "clickhouse-client não encontrado. Instale-o primeiro."
    exit 1
fi

if ! command -v curl &> /dev/null; then
    log_error "curl não encontrado."
    exit 1
fi

# Testar ClickHouse
if clickhouse-client --host "$CLICKHOUSE_HOST" --port "$CLICKHOUSE_PORT" --query "SELECT 1" &> /dev/null; then
    log_success "ClickHouse acessível (${CLICKHOUSE_HOST}:${CLICKHOUSE_PORT})"
else
    log_error "Não foi possível conectar ao ClickHouse"
    exit 1
fi

# Testar Elasticsearch
HTTP_STATUS=$(curl -s -o /dev/null -w "%{http_code}" "${ELASTIC_ENDPOINT}/_cluster/health")
if [ "$HTTP_STATUS" == "200" ]; then
    log_success "Elasticsearch acessível (HTTP $HTTP_STATUS)"
else
    log_error "Não foi possível conectar ao Elasticsearch (HTTP $HTTP_STATUS)"
    exit 1
fi

# Passo 2: Exportar eventos do ClickHouse
log_info "Passo 2: Exportando eventos do ClickHouse..."

EVENTS_FILE="${OUTPUT_DIR}/events.json"
log_info "Exportando eventos (últimos ${DAYS_TO_EXPORT} dias)..."

clickhouse-client --host "$CLICKHOUSE_HOST" --port "$CLICKHOUSE_PORT" --database "$CLICKHOUSE_DB" \
    --query "SELECT * FROM events WHERE timestamp >= now() - INTERVAL ${DAYS_TO_EXPORT} DAY FORMAT JSONEachRow" \
    > "$EVENTS_FILE" 2>&1 || {
        log_error "Falha ao exportar eventos"
        exit 1
    }

EVENT_COUNT=$(wc -l < "$EVENTS_FILE")
log_success "Eventos exportados: ${EVENT_COUNT}"

# Passo 3: Exportar logs do ClickHouse
log_info "Passo 3: Exportando logs do ClickHouse..."

LOGS_FILE="${OUTPUT_DIR}/logs.json"
log_info "Exportando logs (últimos ${DAYS_TO_EXPORT} dias)..."

clickhouse-client --host "$CLICKHOUSE_HOST" --port "$CLICKHOUSE_PORT" --database "$CLICKHOUSE_DB" \
    --query "SELECT * FROM logs WHERE timestamp >= now() - INTERVAL ${DAYS_TO_EXPORT} DAY FORMAT JSONEachRow" \
    > "$LOGS_FILE" 2>&1 || {
        log_error "Falha ao exportar logs"
        exit 1
    }

LOG_COUNT=$(wc -l < "$LOGS_FILE")
log_success "Logs exportados: ${LOG_COUNT}"

# Passo 4: Importar eventos no Elasticsearch
log_info "Passo 4: Importando eventos no Elasticsearch..."

INDEX_DATE=$(date +%Y.%m.%d)
EVENTS_INDEX="nextrace-events-${INDEX_DATE}"

# Criar índice com mapping
cat > "${OUTPUT_DIR}/events_mapping.json" <<EOF
{
  "mappings": {
    "properties": {
      "timestamp": { "type": "date" },
      "event_id": { "type": "keyword" },
      "event_type": { "type": "keyword" },
      "service_name": { "type": "keyword" },
      "environment": { "type": "keyword" },
      "endpoint": { "type": "keyword" },
      "http_method": { "type": "keyword" },
      "status_code": { "type": "integer" },
      "duration_ms": { "type": "float" },
      "error_message": { "type": "text" },
      "error_type": { "type": "keyword" },
      "tags": { "type": "keyword" },
      "metadata": { "type": "object" }
    }
  }
}
EOF

curl -X PUT "${ELASTIC_ENDPOINT}/${EVENTS_INDEX}" \
     -H 'Content-Type: application/json' \
     -d @"${OUTPUT_DIR}/events_mapping.json" > /dev/null 2>&1

log_info "Índice criado: ${EVENTS_INDEX}"

# Importar em batches
IMPORTED=0
while IFS= read -r line; do
    if [ -z "$line" ]; then continue; fi
    
    EVENT_ID=$(echo "$line" | jq -r '.event_id')
    curl -s -X PUT "${ELASTIC_ENDPOINT}/${EVENTS_INDEX}/_doc/${EVENT_ID}" \
         -H 'Content-Type: application/json' \
         -d "$line" > /dev/null 2>&1
    
    IMPORTED=$((IMPORTED + 1))
    if [ $((IMPORTED % 100)) -eq 0 ]; then
        log_info "Progresso eventos: ${IMPORTED}/${EVENT_COUNT}"
    fi
done < "$EVENTS_FILE"

log_success "Eventos importados: ${IMPORTED}"

# Passo 5: Importar logs no Elasticsearch
log_info "Passo 5: Importando logs no Elasticsearch..."

LOGS_INDEX="nextrace-logs-${INDEX_DATE}"

cat > "${OUTPUT_DIR}/logs_mapping.json" <<EOF
{
  "mappings": {
    "properties": {
      "log_id": { "type": "keyword" },
      "timestamp": { "type": "date" },
      "service_name": { "type": "keyword" },
      "environment": { "type": "keyword" },
      "severity": { "type": "keyword" },
      "message": { "type": "text" },
      "attributes_json": { "type": "object" }
    }
  }
}
EOF

curl -X PUT "${ELASTIC_ENDPOINT}/${LOGS_INDEX}" \
     -H 'Content-Type: application/json' \
     -d @"${OUTPUT_DIR}/logs_mapping.json" > /dev/null 2>&1

log_info "Índice criado: ${LOGS_INDEX}"

IMPORTED_LOGS=0
while IFS= read -r line; do
    if [ -z "$line" ]; then continue; fi
    
    LOG_ID=$(echo "$line" | jq -r '.log_id')
    curl -s -X PUT "${ELASTIC_ENDPOINT}/${LOGS_INDEX}/_doc/${LOG_ID}" \
         -H 'Content-Type: application/json' \
         -d "$line" > /dev/null 2>&1
    
    IMPORTED_LOGS=$((IMPORTED_LOGS + 1))
    if [ $((IMPORTED_LOGS % 100)) -eq 0 ]; then
        log_info "Progresso logs: ${IMPORTED_LOGS}/${LOG_COUNT}"
    fi
done < "$LOGS_FILE"

log_success "Logs importados: ${IMPORTED_LOGS}"

# Passo 6: Resumo final
log_info "Passo 6: Resumo da migração"
echo ""
log_info "═══════════════════════════════════════════════════════════"
log_success "Migração concluída!"
echo ""
log_info "Arquivos exportados:"
log_info "  - Eventos: ${EVENTS_FILE} (${EVENT_COUNT} registros)"
log_info "  - Logs: ${LOGS_FILE} (${LOG_COUNT} registros)"
echo ""
log_info "Índices Elasticsearch criados:"
log_info "  - ${EVENTS_INDEX}"
log_info "  - ${LOGS_INDEX}"
echo ""
log_info "Verifique os dados:"
log_info "  curl ${ELASTIC_ENDPOINT}/${EVENTS_INDEX}/_count"
log_info "  curl ${ELASTIC_ENDPOINT}/${LOGS_INDEX}/_count"
log_info "═══════════════════════════════════════════════════════════"

exit 0
