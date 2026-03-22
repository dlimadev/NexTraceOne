#!/usr/bin/env bash
# ═══════════════════════════════════════════════════════════════════════════════
# NexTraceOne — Database Backup (Bash)
#
# Cria backups comprimidos (.sql.gz) dos 4 bancos lógicos PostgreSQL.
# Uso seguro em CI/CD, Staging e procedimentos manuais de Production.
#
# Uso:
#   bash scripts/db/backup.sh --env production --output-dir ./backups
#   bash scripts/db/backup.sh --databases nextraceone_identity,nextraceone_catalog
#   bash scripts/db/backup.sh --help
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
OUTPUT_DIR="./backups"
DATABASES_ARG=""
PGHOST="${PGHOST:-localhost}"
PGPORT="${PGPORT:-5432}"
PGUSER="${PGUSER:-nextraceone}"

ALL_DATABASES=(
  "nextraceone_identity"
  "nextraceone_catalog"
  "nextraceone_operations"
  "nextraceone_ai"
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

Cria backups comprimidos (.sql.gz) dos bancos de dados PostgreSQL do NexTraceOne.

Opções:
  --env <env>           Ambiente (local|staging|production). Padrão: local
  --output-dir <dir>    Diretório de saída dos backups. Padrão: ./backups
  --databases <list>    Lista de bancos separados por vírgula. Padrão: todos
  --help, -h            Exibir esta ajuda

Variáveis de ambiente:
  PGHOST       Host do PostgreSQL (padrão: localhost)
  PGPORT       Porta do PostgreSQL (padrão: 5432)
  PGUSER       Utilizador do PostgreSQL (padrão: nextraceone)
  PGPASSWORD   Password do PostgreSQL

Bancos disponíveis:
  nextraceone_identity, nextraceone_catalog, nextraceone_operations, nextraceone_ai

Exemplos:
  # Backup de todos os bancos (ambiente local)
  bash scripts/db/backup.sh

  # Backup em production com diretório customizado
  bash scripts/db/backup.sh --env production --output-dir /mnt/backups

  # Backup de bancos específicos
  bash scripts/db/backup.sh --databases nextraceone_identity,nextraceone_catalog
EOF
}

# ── Parse arguments ───────────────────────────────────────────────────────────
while [[ $# -gt 0 ]]; do
  case "$1" in
    --env)        ENV="$2"; shift 2 ;;
    --output-dir) OUTPUT_DIR="$2"; shift 2 ;;
    --databases)  DATABASES_ARG="$2"; shift 2 ;;
    --help|-h)    usage; exit 0 ;;
    *) log_error "Argumento desconhecido: $1"; usage; exit 1 ;;
  esac
done

# ── Resolve database list ─────────────────────────────────────────────────────
resolve_databases() {
  if [[ -n "$DATABASES_ARG" ]]; then
    IFS=',' read -ra DATABASES <<< "$DATABASES_ARG"
    for db in "${DATABASES[@]}"; do
      local found=false
      for valid in "${ALL_DATABASES[@]}"; do
        if [[ "$db" == "$valid" ]]; then
          found=true
          break
        fi
      done
      if [[ "$found" == "false" ]]; then
        log_error "Banco desconhecido: $db"
        log_error "Bancos válidos: ${ALL_DATABASES[*]}"
        exit 1
      fi
    done
  else
    DATABASES=("${ALL_DATABASES[@]}")
  fi
}

# ── Check prerequisites ───────────────────────────────────────────────────────
check_prerequisites() {
  if ! command -v pg_dump &>/dev/null; then
    log_error "pg_dump não encontrado. Instale PostgreSQL client tools."
    exit 1
  fi

  if ! command -v gzip &>/dev/null; then
    log_error "gzip não encontrado. Instale gzip."
    exit 1
  fi
}

# ── Create output directory ──────────────────────────────────────────────────
prepare_output_dir() {
  if [[ ! -d "$OUTPUT_DIR" ]]; then
    log_info "Criando diretório de backups: ${OUTPUT_DIR}"
    mkdir -p "$OUTPUT_DIR"
  fi
}

# ── Backup a single database ─────────────────────────────────────────────────
backup_database() {
  local db_name="$1"
  local timestamp
  timestamp="$(date '+%Y%m%d_%H%M%S')"
  local filename="${db_name}_${ENV}_${timestamp}.sql.gz"
  local filepath="${OUTPUT_DIR}/${filename}"

  log_info "Backup: ${db_name} → ${filepath}"

  if pg_dump \
    --host="$PGHOST" \
    --port="$PGPORT" \
    --username="$PGUSER" \
    --no-password \
    --format=plain \
    --clean \
    --if-exists \
    --no-owner \
    --no-privileges \
    "$db_name" | gzip > "$filepath"; then

    local size
    size="$(du -h "$filepath" | cut -f1)"
    log_success "✓ ${db_name} — ${filename} (${size})"
    return 0
  else
    log_error "✗ ${db_name} — Backup falhou"
    rm -f "$filepath"
    return 1
  fi
}

# ── Main ──────────────────────────────────────────────────────────────────────
main() {
  log_info "═══════════════════════════════════════════════════════"
  log_info "NexTraceOne — Database Backup"
  log_info "═══════════════════════════════════════════════════════"

  check_prerequisites
  resolve_databases
  prepare_output_dir

  log_info "Ambiente:   ${ENV}"
  log_info "Host:       ${PGHOST}:${PGPORT}"
  log_info "Utilizador: ${PGUSER}"
  log_info "Output:     ${OUTPUT_DIR}"
  log_info "Bancos:     ${DATABASES[*]}"
  echo ""

  local failed=()
  local succeeded=()

  for db in "${DATABASES[@]}"; do
    if backup_database "$db"; then
      succeeded+=("$db")
    else
      failed+=("$db")
    fi
  done

  # ── Summary ───────────────────────────────────────────────────────────────
  echo ""
  log_info "═══════════════════════════════════════"
  log_info "RESULTADO DO BACKUP"
  log_info "Ambiente:       ${ENV}"
  log_info "Bem-sucedidos:  ${#succeeded[@]}"
  log_info "Falharam:       ${#failed[@]}"
  log_info "═══════════════════════════════════════"

  if [[ ${#failed[@]} -gt 0 ]]; then
    log_error "Bancos com falha:"
    for db in "${failed[@]}"; do
      log_error "  - ${db}"
    done
    exit 1
  fi

  log_success "Todos os backups criados com sucesso."
}

main
