#!/usr/bin/env bash
# ═══════════════════════════════════════════════════════════════════════════════
# NexTraceOne — Database Restore (Bash)
#
# Restaura um banco de dados PostgreSQL a partir de backup comprimido (.sql.gz).
# Uso seguro em CI/CD, Staging e procedimentos manuais de Production.
#
# Uso:
#   bash scripts/db/restore.sh --database nextraceone_identity
#   bash scripts/db/restore.sh --database nextraceone_catalog --file backup.sql.gz
#   bash scripts/db/restore.sh --help
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
INPUT_DIR="./backups"
DATABASE=""
BACKUP_FILE=""
FORCE=false
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

Restaura um banco de dados PostgreSQL do NexTraceOne a partir de backup .sql.gz.

Opções:
  --env <env>           Ambiente (local|staging|production). Padrão: local
  --input-dir <dir>     Diretório dos backups. Padrão: ./backups
  --database <name>     Nome do banco a restaurar (obrigatório)
  --file <file>         Ficheiro de backup específico (padrão: mais recente)
  --force               Ignorar confirmação de segurança
  --help, -h            Exibir esta ajuda

Variáveis de ambiente:
  PGHOST       Host do PostgreSQL (padrão: localhost)
  PGPORT       Porta do PostgreSQL (padrão: 5432)
  PGUSER       Utilizador do PostgreSQL (padrão: nextraceone)
  PGPASSWORD   Password do PostgreSQL

Bancos disponíveis:
  nextraceone_identity, nextraceone_catalog, nextraceone_operations, nextraceone_ai

Exemplos:
  # Restaurar último backup do banco identity
  bash scripts/db/restore.sh --database nextraceone_identity

  # Restaurar ficheiro específico sem confirmação
  bash scripts/db/restore.sh --database nextraceone_catalog --file backup.sql.gz --force

  # Restaurar de diretório customizado
  bash scripts/db/restore.sh --database nextraceone_ai --input-dir /mnt/backups
EOF
}

# ── Parse arguments ───────────────────────────────────────────────────────────
while [[ $# -gt 0 ]]; do
  case "$1" in
    --env)       ENV="$2"; shift 2 ;;
    --input-dir) INPUT_DIR="$2"; shift 2 ;;
    --database)  DATABASE="$2"; shift 2 ;;
    --file)      BACKUP_FILE="$2"; shift 2 ;;
    --force)     FORCE=true; shift ;;
    --help|-h)   usage; exit 0 ;;
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

  if ! command -v gunzip &>/dev/null; then
    log_error "gunzip não encontrado. Instale gzip."
    exit 1
  fi
}

# ── Resolve backup file ──────────────────────────────────────────────────────
resolve_backup_file() {
  if [[ -n "$BACKUP_FILE" ]]; then
    # If a relative path without directory, look in INPUT_DIR
    if [[ "$BACKUP_FILE" != /* ]] && [[ ! -f "$BACKUP_FILE" ]]; then
      BACKUP_FILE="${INPUT_DIR}/${BACKUP_FILE}"
    fi
  else
    # Find the latest backup for this database
    if [[ ! -d "$INPUT_DIR" ]]; then
      log_error "Diretório de backups não encontrado: ${INPUT_DIR}"
      exit 1
    fi

    BACKUP_FILE=$(find "$INPUT_DIR" -maxdepth 1 -name "${DATABASE}_*.sql.gz" -type f | sort -r | head -n 1)

    if [[ -z "$BACKUP_FILE" ]]; then
      log_error "Nenhum backup encontrado para ${DATABASE} em ${INPUT_DIR}"
      exit 1
    fi

    log_info "Backup mais recente encontrado: ${BACKUP_FILE}"
  fi

  if [[ ! -f "$BACKUP_FILE" ]]; then
    log_error "Ficheiro de backup não encontrado: ${BACKUP_FILE}"
    exit 1
  fi
}

# ── Safety confirmation ──────────────────────────────────────────────────────
confirm_restore() {
  if [[ "$FORCE" == "true" ]]; then
    return 0
  fi

  local file_size
  file_size="$(du -h "$BACKUP_FILE" | cut -f1)"

  echo ""
  log_warn "═══════════════════════════════════════════════════════"
  log_warn "  ATENÇÃO: Operação de Restore"
  log_warn "═══════════════════════════════════════════════════════"
  log_warn "  Banco:    ${DATABASE}"
  log_warn "  Ambiente: ${ENV}"
  log_warn "  Host:     ${PGHOST}:${PGPORT}"
  log_warn "  Ficheiro: ${BACKUP_FILE} (${file_size})"
  log_warn ""
  log_warn "  Esta operação irá substituir os dados atuais do banco."
  log_warn "═══════════════════════════════════════════════════════"
  echo ""
  echo -n "Tem certeza que deseja restaurar? (yes/no): "
  read -r CONFIRM

  if [[ "$CONFIRM" != "yes" ]]; then
    log_info "Operação cancelada pelo utilizador."
    exit 0
  fi
}

# ── Restore database ─────────────────────────────────────────────────────────
restore_database() {
  local file_size
  file_size="$(du -h "$BACKUP_FILE" | cut -f1)"

  log_info "Restaurando ${DATABASE} a partir de ${BACKUP_FILE} (${file_size})..."

  if gunzip --stdout "$BACKUP_FILE" | psql \
    --host="$PGHOST" \
    --port="$PGPORT" \
    --username="$PGUSER" \
    --no-password \
    --dbname="$DATABASE" \
    --quiet \
    --set ON_ERROR_STOP=1; then
    log_success "✓ ${DATABASE} restaurado com sucesso."
    return 0
  else
    log_error "✗ ${DATABASE} — Restore falhou."
    return 1
  fi
}

# ── Main ──────────────────────────────────────────────────────────────────────
main() {
  log_info "═══════════════════════════════════════════════════════"
  log_info "NexTraceOne — Database Restore"
  log_info "═══════════════════════════════════════════════════════"

  check_prerequisites
  validate_database
  resolve_backup_file
  confirm_restore

  log_info "Ambiente:   ${ENV}"
  log_info "Host:       ${PGHOST}:${PGPORT}"
  log_info "Utilizador: ${PGUSER}"
  log_info "Banco:      ${DATABASE}"
  log_info "Ficheiro:   ${BACKUP_FILE}"
  echo ""

  if restore_database; then
    echo ""
    log_info "═══════════════════════════════════════"
    log_info "RESULTADO DO RESTORE"
    log_success "Banco ${DATABASE} restaurado com sucesso."
    log_info "═══════════════════════════════════════"
    log_info "Recomendação: execute verify-restore.sh para validar a integridade."
    log_info "  bash scripts/db/verify-restore.sh --database ${DATABASE} --env ${ENV}"
  else
    echo ""
    log_info "═══════════════════════════════════════"
    log_error "RESULTADO DO RESTORE"
    log_error "Falha ao restaurar ${DATABASE}."
    log_info "═══════════════════════════════════════"
    exit 1
  fi
}

main
