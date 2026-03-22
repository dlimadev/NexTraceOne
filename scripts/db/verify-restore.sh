#!/usr/bin/env bash
# ═══════════════════════════════════════════════════════════════════════════════
# NexTraceOne — Verify Database Restore (Bash)
#
# Verifica a integridade de um banco de dados PostgreSQL após restore.
# Executa queries de validação: existência, tabelas, contagens e migrações.
#
# Uso:
#   bash scripts/db/verify-restore.sh --database nextraceone_identity
#   bash scripts/db/verify-restore.sh --database nextraceone_catalog --env staging
#   bash scripts/db/verify-restore.sh --help
#
# Variáveis de ambiente suportadas:
#   PGHOST       Host do PostgreSQL (padrão: localhost)
#   PGPORT       Porta do PostgreSQL (padrão: 5432)
#   PGUSER       Utilizador do PostgreSQL (padrão: nextraceone)
#   PGPASSWORD   Password do PostgreSQL
# ═══════════════════════════════════════════════════════════════════════════════

set -euo pipefail

# ── Defaults ─────────────────────────────────────────────────────────────────
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"

ENV="local"
DATABASE=""
PGHOST="${PGHOST:-localhost}"
PGPORT="${PGPORT:-5432}"
PGUSER="${PGUSER:-nextraceone}"

ALL_DATABASES=(
  "nextraceone_identity"
  "nextraceone_catalog"
  "nextraceone_operations"
  "nextraceone_ai"
)

# Key tables per database for row-count verification
declare -A KEY_TABLES=(
  ["nextraceone_identity"]="identity.users identity.roles audit.audit_entries"
  ["nextraceone_catalog"]="catalog.services catalog.contracts catalog.api_endpoints"
  ["nextraceone_operations"]="changes.change_records governance.rulesets incidents.incidents"
  ["nextraceone_ai"]="ai.models ai.policies ai.orchestration_sessions"
)

# ── Colors ────────────────────────────────────────────────────────────────────
RED='\033[0;31m'; GREEN='\033[0;32m'; YELLOW='\033[1;33m'; BLUE='\033[0;34m'; NC='\033[0m'

log_info()    { echo -e "${BLUE}[INFO]${NC}  $(date '+%Y-%m-%d %H:%M:%S') $*"; }
log_success() { echo -e "${GREEN}[OK]${NC}    $(date '+%Y-%m-%d %H:%M:%S') $*"; }
log_warn()    { echo -e "${YELLOW}[WARN]${NC}  $(date '+%Y-%m-%d %H:%M:%S') $*"; }
log_error()   { echo -e "${RED}[ERROR]${NC} $(date '+%Y-%m-%d %H:%M:%S') $*" >&2; }

# ── Help ──────────────────────────────────────────────────────────────────────
usage() {
  cat <<EOF
Uso: $0 [opções]

Verifica a integridade de um banco de dados PostgreSQL do NexTraceOne após restore.

Opções:
  --env <env>           Ambiente (local|staging|production). Padrão: local
  --database <name>     Nome do banco a verificar (obrigatório)
  --help, -h            Exibir esta ajuda

Variáveis de ambiente:
  PGHOST       Host do PostgreSQL (padrão: localhost)
  PGPORT       Porta do PostgreSQL (padrão: 5432)
  PGUSER       Utilizador do PostgreSQL (padrão: nextraceone)
  PGPASSWORD   Password do PostgreSQL

Bancos disponíveis:
  nextraceone_identity, nextraceone_catalog, nextraceone_operations, nextraceone_ai

Exemplos:
  # Verificar banco identity
  bash scripts/db/verify-restore.sh --database nextraceone_identity

  # Verificar banco catalog em staging
  bash scripts/db/verify-restore.sh --database nextraceone_catalog --env staging
EOF
}

# ── Parse arguments ───────────────────────────────────────────────────────────
while [[ $# -gt 0 ]]; do
  case "$1" in
    --env)      ENV="$2"; shift 2 ;;
    --database) DATABASE="$2"; shift 2 ;;
    --help|-h)  usage; exit 0 ;;
    *) log_error "Argumento desconhecido: $1"; usage; exit 1 ;;
  esac
done

# ── Validate database ────────────────────────────────────────────────────────
validate_database() {
  if [[ -z "$DATABASE" ]]; then
    log_error "Parâmetro --database é obrigatório."
    usage
    exit 1
  fi

  local valid=false
  for db in "${ALL_DATABASES[@]}"; do
    if [[ "$DATABASE" == "$db" ]]; then
      valid=true
      break
    fi
  done

  if [[ "$valid" == "false" ]]; then
    log_error "Banco desconhecido: $DATABASE"
    log_error "Bancos válidos: ${ALL_DATABASES[*]}"
    exit 1
  fi
}

# ── Check prerequisites ───────────────────────────────────────────────────────
check_prerequisites() {
  if ! command -v psql &>/dev/null; then
    log_error "psql não encontrado. Instale PostgreSQL client tools."
    exit 1
  fi
}

# ── Helper: run query and return result ───────────────────────────────────────
run_query() {
  local query="$1"
  psql \
    --host="$PGHOST" \
    --port="$PGPORT" \
    --username="$PGUSER" \
    --no-password \
    --dbname="$DATABASE" \
    --tuples-only \
    --no-align \
    --quiet \
    -c "$query" 2>/dev/null
}

# ── Check 1: Database exists ─────────────────────────────────────────────────
check_database_exists() {
  log_info "Verificação 1: Banco de dados existe..."

  local result
  result=$(psql \
    --host="$PGHOST" \
    --port="$PGPORT" \
    --username="$PGUSER" \
    --no-password \
    --dbname="postgres" \
    --tuples-only \
    --no-align \
    --quiet \
    -c "SELECT 1 FROM pg_database WHERE datname = '${DATABASE}';" 2>/dev/null) || true

  if [[ "$result" == "1" ]]; then
    log_success "✓ Banco ${DATABASE} existe."
    return 0
  else
    log_error "✗ Banco ${DATABASE} não encontrado."
    return 1
  fi
}

# ── Check 2: Count tables ────────────────────────────────────────────────────
check_table_count() {
  log_info "Verificação 2: Contagem de tabelas..."

  local count
  count=$(run_query "
    SELECT COUNT(*)
    FROM information_schema.tables
    WHERE table_schema NOT IN ('pg_catalog', 'information_schema')
      AND table_type = 'BASE TABLE';
  ") || true

  count=$(echo "$count" | tr -d '[:space:]')

  if [[ -n "$count" ]] && [[ "$count" -gt 0 ]]; then
    log_success "✓ Total de tabelas: ${count}"
    return 0
  else
    log_error "✗ Nenhuma tabela encontrada no banco ${DATABASE}."
    return 1
  fi
}

# ── Check 3: Key table row counts ────────────────────────────────────────────
check_key_tables() {
  log_info "Verificação 3: Contagem de registos em tabelas-chave..."

  local tables_str="${KEY_TABLES[$DATABASE]:-}"
  if [[ -z "$tables_str" ]]; then
    log_warn "Nenhuma tabela-chave configurada para ${DATABASE}. Ignorando."
    return 0
  fi

  local all_ok=true
  read -ra tables <<< "$tables_str"

  for table in "${tables[@]}"; do
    local count
    count=$(run_query "
      SELECT COUNT(*)
      FROM ${table};
    " 2>/dev/null) || true

    count=$(echo "$count" | tr -d '[:space:]')

    if [[ -n "$count" ]]; then
      log_info "  ${table}: ${count} registos"
    else
      log_warn "  ${table}: tabela não encontrada ou inacessível"
      all_ok=false
    fi
  done

  if [[ "$all_ok" == "true" ]]; then
    log_success "✓ Tabelas-chave verificadas."
  else
    log_warn "Algumas tabelas-chave não foram encontradas (pode ser esperado se o schema ainda não foi criado)."
  fi
  return 0
}

# ── Check 4: Migrations table ────────────────────────────────────────────────
check_migrations() {
  log_info "Verificação 4: Tabela de migrações (EF Core)..."

  local migrations_exist
  migrations_exist=$(run_query "
    SELECT COUNT(*)
    FROM information_schema.tables
    WHERE table_name = '__EFMigrationsHistory';
  ") || true

  migrations_exist=$(echo "$migrations_exist" | tr -d '[:space:]')

  if [[ "$migrations_exist" == "0" ]] || [[ -z "$migrations_exist" ]]; then
    log_warn "Tabela __EFMigrationsHistory não encontrada. Migrações EF Core podem não ter sido aplicadas."
    return 0
  fi

  local total
  total=$(run_query "SELECT COUNT(*) FROM \"__EFMigrationsHistory\";") || true
  total=$(echo "$total" | tr -d '[:space:]')

  local latest
  latest=$(run_query "
    SELECT \"MigrationId\"
    FROM \"__EFMigrationsHistory\"
    ORDER BY \"MigrationId\" DESC
    LIMIT 1;
  ") || true
  latest=$(echo "$latest" | tr -d '[:space:]')

  if [[ -n "$total" ]] && [[ "$total" -gt 0 ]]; then
    log_success "✓ Migrações aplicadas: ${total}"
    log_info "  Última migração: ${latest}"
    return 0
  else
    log_warn "Tabela __EFMigrationsHistory existe mas está vazia."
    return 0
  fi
}

# ── Check 5: List schemas ────────────────────────────────────────────────────
check_schemas() {
  log_info "Verificação 5: Schemas do banco..."

  local schemas
  schemas=$(run_query "
    SELECT schema_name
    FROM information_schema.schemata
    WHERE schema_name NOT IN ('pg_catalog', 'information_schema', 'pg_toast')
    ORDER BY schema_name;
  ") || true

  if [[ -n "$schemas" ]]; then
    log_success "✓ Schemas encontrados:"
    while IFS= read -r schema; do
      schema=$(echo "$schema" | tr -d '[:space:]')
      if [[ -n "$schema" ]]; then
        log_info "  - ${schema}"
      fi
    done <<< "$schemas"
    return 0
  else
    log_warn "Nenhum schema customizado encontrado."
    return 0
  fi
}

# ── Main ──────────────────────────────────────────────────────────────────────
main() {
  log_info "═══════════════════════════════════════════════════════"
  log_info "NexTraceOne — Verify Database Restore"
  log_info "═══════════════════════════════════════════════════════"

  check_prerequisites
  validate_database

  log_info "Ambiente:   ${ENV}"
  log_info "Host:       ${PGHOST}:${PGPORT}"
  log_info "Utilizador: ${PGUSER}"
  log_info "Banco:      ${DATABASE}"
  echo ""

  local checks_failed=0

  if ! check_database_exists; then
    log_error "Banco não existe. Verificações restantes não podem ser executadas."
    exit 1
  fi
  echo ""

  check_table_count  || ((checks_failed++)) || true
  echo ""
  check_key_tables   || ((checks_failed++)) || true
  echo ""
  check_migrations   || ((checks_failed++)) || true
  echo ""
  check_schemas      || ((checks_failed++)) || true

  # ── Summary ───────────────────────────────────────────────────────────────
  echo ""
  log_info "═══════════════════════════════════════"
  log_info "RESULTADO DA VERIFICAÇÃO"
  log_info "Banco: ${DATABASE}"
  log_info "═══════════════════════════════════════"

  if [[ "$checks_failed" -gt 0 ]]; then
    log_error "Verificação concluída com ${checks_failed} falha(s)."
    exit 1
  fi

  log_success "Todas as verificações passaram com sucesso."
}

main
