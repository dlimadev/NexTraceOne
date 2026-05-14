#!/bin/bash
# ============================================================================
# Script de Migração de Backend de Telemetria
# NexTraceOne - Operational Intelligence Module
# ============================================================================
# Este script auxilia na migração entre ClickHouse e Elasticsearch.
# 
# Uso: ./migrate-telemetry-backend.sh [clickhouse|elasticsearch]
# Exemplo: ./migrate-telemetry-backend.sh clickhouse
# ============================================================================

set -euo pipefail

# Cores para output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Configurações
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
MODULE_DIR="$(dirname "$SCRIPT_DIR")"
CONFIG_FILE="${MODULE_DIR}/../../platform/NexTraceOne.ApiHost/appsettings.json"

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

# Verificar argumentos
if [ $# -ne 1 ]; then
    log_error "Uso: $0 [clickhouse|elasticsearch]"
    exit 1
fi

TARGET_BACKEND="$1"

if [[ "$TARGET_BACKEND" != "clickhouse" && "$TARGET_BACKEND" != "elasticsearch" ]]; then
    log_error "Backend inválido. Use 'clickhouse' ou 'elasticsearch'."
    exit 1
fi

log_info "Iniciando migração para backend: ${TARGET_BACKEND^^}"

# Passo 1: Backup da configuração atual
log_info "Passo 1: Fazendo backup da configuração atual..."
BACKUP_FILE="${CONFIG_FILE}.backup.$(date +%Y%m%d_%H%M%S)"
if [ -f "$CONFIG_FILE" ]; then
    cp "$CONFIG_FILE" "$BACKUP_FILE"
    log_success "Backup criado: $BACKUP_FILE"
else
    log_warning "Arquivo de configuração não encontrado: $CONFIG_FILE"
fi

# Passo 2: Validar conectividade com o backend de destino
log_info "Passo 2: Validando conectividade com ${TARGET_BACKEND^^}..."

if [ "$TARGET_BACKEND" == "clickhouse" ]; then
    # Extrair connection string do appsettings.json (simplificado)
    CLICKHOUSE_HOST=$(grep -o '"Host":"[^"]*"' "$CONFIG_FILE" | head -1 | cut -d'"' -f4)
    CLICKHOUSE_PORT=$(grep -o '"Port":[0-9]*' "$CONFIG_FILE" | head -1 | cut -d':' -f2)
    
    if command -v clickhouse-client &> /dev/null; then
        if clickhouse-client --host "${CLICKHOUSE_HOST:-localhost}" --port "${CLICKHOUSE_PORT:-9000}" --query "SELECT 1" &> /dev/null; then
            log_success "ClickHouse está acessível"
        else
            log_error "Não foi possível conectar ao ClickHouse. Verifique a configuração."
            exit 1
        fi
    else
        log_warning "clickhouse-client não encontrado. Pulando validação de conexão."
    fi
    
elif [ "$TARGET_BACKEND" == "elasticsearch" ]; then
    ES_ENDPOINT=$(grep -o '"Endpoint":"[^"]*"' "$CONFIG_FILE" | head -1 | cut -d'"' -f4)
    
    if command -v curl &> /dev/null; then
        HTTP_STATUS=$(curl -s -o /dev/null -w "%{http_code}" "${ES_ENDPOINT:-http://localhost:9200}/_cluster/health")
        if [ "$HTTP_STATUS" == "200" ]; then
            log_success "Elasticsearch está acessível (HTTP $HTTP_STATUS)"
        else
            log_error "Não foi possível conectar ao Elasticsearch (HTTP $HTTP_STATUS). Verifique a configuração."
            exit 1
        fi
    else
        log_warning "curl não encontrado. Pulando validação de conexão."
    fi
fi

# Passo 3: Atualizar configuração no appsettings.json
log_info "Passo 3: Atualizando configuração no appsettings.json..."

if [ ! -f "$CONFIG_FILE" ]; then
    log_error "Arquivo de configuração não encontrado: $CONFIG_FILE"
    exit 1
fi

# Usar jq para atualizar o JSON (se disponível)
if command -v jq &> /dev/null; then
    TEMP_FILE=$(mktemp)
    jq ".Telemetry.ObservabilityProvider.Provider = \"${TARGET_BACKEND}\"" "$CONFIG_FILE" > "$TEMP_FILE"
    mv "$TEMP_FILE" "$CONFIG_FILE"
    log_success "Configuração atualizada: Provider = ${TARGET_BACKEND}"
else
    log_warning "jq não encontrado. Atualização manual necessária."
    log_info "Edite o arquivo $CONFIG_FILE e altere:"
    log_info "  \"Provider\": \"${TARGET_BACKEND}\""
fi

# Passo 4: Executar scripts de schema (apenas para ClickHouse)
if [ "$TARGET_BACKEND" == "clickhouse" ]; then
    log_info "Passo 4: Executando script de schema do ClickHouse..."
    
    SCHEMA_SCRIPT="${SCRIPT_DIR}/clickhouse_schema.sql"
    if [ -f "$SCHEMA_SCRIPT" ]; then
        CLICKHOUSE_CONNECTION="Host=${CLICKHOUSE_HOST:-localhost};Port=${CLICKHOUSE_PORT:-9000};Database=nextrace_telemetry"
        
        if command -v clickhouse-client &> /dev/null; then
            clickhouse-client --multiquery < "$SCHEMA_SCRIPT" 2>&1 | tee "${SCRIPT_DIR}/schema_migration.log"
            log_success "Schema do ClickHouse aplicado com sucesso"
        else
            log_error "clickhouse-client não encontrado. Execute manualmente:"
            log_error "  cat $SCHEMA_SCRIPT | clickhouse-client --multiquery"
        fi
    else
        log_warning "Script de schema não encontrado: $SCHEMA_SCRIPT"
    fi
fi

# Passo 5: Instruções de pós-migração
log_info "Passo 5: Pós-migração"
echo ""
log_info "═══════════════════════════════════════════════════════════"
log_info "Migração concluída! Próximos passos:"
echo ""
log_info "1. Reinicie a aplicação NexTraceOne:"
log_info "   cd ${MODULE_DIR}/../../platform/NexTraceOne.ApiHost"
log_info "   dotnet run"
echo ""
log_info "2. Verifique os logs para confirmar o backend ativo:"
log_info "   grep -i 'telemetry\|backend' logs/*.log"
echo ""
log_info "3. Acesse o dashboard de observabilidade:"
log_info "   http://localhost:5000/observability"
echo ""
log_info "4. Valide que os dados estão sendo ingeridos:"
if [ "$TARGET_BACKEND" == "clickhouse" ]; then
    log_info "   clickhouse-client --query \"SELECT count() FROM nextrace_telemetry.events\""
elif [ "$TARGET_BACKEND" == "elasticsearch" ]; then
    log_info "   curl ${ES_ENDPOINT:-http://localhost:9200}/nextrace-logs-*/_count"
fi
echo ""
log_info "═══════════════════════════════════════════════════════════"
echo ""
log_success "Migração para ${TARGET_BACKEND^^} concluída com sucesso!"
log_warning "IMPORTANTE: Dados históricos NÃO foram migrados automaticamente."
log_warning "Use ferramentas de exportação/importação se necessário."

exit 0
