#!/usr/bin/env bash
# ═══════════════════════════════════════════════════════════════════════════════
# NexTraceOne — Full Database Restore (Bash)
#
# Restaura TODOS os 4 bancos de dados PostgreSQL a partir de backups comprimidos.
# Uso seguro em procedimentos de disaster recovery e restore validations.
#
# Uso:
#   bash scripts/db/restore-all.sh --env production --input-dir ./backups
#   bash scripts/db/restore-all.sh --force
#   bash scripts/db/restore-all.sh --help
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
FORCE=false
VERIFY=true
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

Restaura TODOS os 4 bancos de dados PostgreSQL do NexTraceOne a partir de backups.
Para cada banco, seleciona automaticamente o backup mais recente no diretório de input.

Opções:
  --env <env>           Ambiente (local|staging|production). Padrão: local
  --input-dir <dir>     Diretório dos backups. Padrão: ./backups
  --force               Ignorar confirmação de segurança
  --no-verify           Não executar verificação pós-restore
  --help, -h            Exibir esta ajuda

Variáveis de ambiente:
  PGHOST       Host do PostgreSQL (padrão: localhost)
  PGPORT       Porta do PostgreSQL (padrão: 5432)
  PGUSER       Utilizador do PostgreSQL (padrão: nextraceone)
  PGPASSWORD   Password do PostgreSQL

Bancos restaurados:
  nextraceone_identity, nextraceone_catalog, nextraceone_operations, nextraceone_ai

Exemplos:
  # Restaurar todos os bancos (ambiente local)
  bash scripts/db/restore-all.sh

  # Restaurar em production sem confirmação (para pipelines automatizados)
  bash scripts/db/restore-all.sh --env production --force

  # Restaurar sem verificação pós-restore
  bash scripts/db/restore-all.sh --no-verify
EOF
}

# ── Parse arguments ───────────────────────────────────────────────────────────
while [[ $# -gt 0 ]]; do
  case "$1" in
    --env)        ENV="$2"; shift 2 ;;
    --input-dir)  INPUT_DIR="$2"; shift 2 ;;
    --force)      FORCE=true; shift ;;
    --no-verify)  VERIFY=false; shift ;;
    --help|-h)    usage; exit 0 ;;
    *) log_error "Argumento desconhecido: $1"; usage; exit 1 ;;
  esac
done

# ── Safety confirmation ──────────────────────────────────────────────────────
confirm_restore() {
  if [[ "$FORCE" == "true" ]]; then
    return 0
  fi

  echo ""
  log_warn "═══════════════════════════════════════════════════════════════"
  log_warn "  ATENÇÃO: Operação de FULL RESTORE — TODOS os bancos"
  log_warn "═══════════════════════════════════════════════════════════════"
  log_warn "  Ambiente:   ${ENV}"
  log_warn "  Host:       ${PGHOST}:${PGPORT}"
  log_warn "  Input Dir:  ${INPUT_DIR}"
  log_warn "  Bancos:     ${ALL_DATABASES[*]}"
  log_warn ""
  log_warn "  Esta operação irá substituir os dados atuais de TODOS os bancos."
  log_warn "  Recomendação: faça backup antes de restaurar."
  log_warn "═══════════════════════════════════════════════════════════════"

  if [[ "$ENV" == "production" ]]; then
    log_warn ""
    log_warn "  ⚠️  AMBIENTE PRODUCTION DETECTADO ⚠️"
    log_warn "  Esta operação é DESTRUTIVA e irá substituir dados de produção."
    log_warn ""
  fi

  echo ""
  echo -n "Tem certeza que deseja restaurar TODOS os bancos? (yes/no): "
  read -r CONFIRM

  if [[ "$CONFIRM" != "yes" ]]; then
    log_info "Operação cancelada pelo utilizador."
    exit 0
  fi
}

# ── Main ──────────────────────────────────────────────────────────────────────
main() {
  log_info "═══════════════════════════════════════════════════════"
  log_info "NexTraceOne — Full Database Restore (All 4 Databases)"
  log_info "═══════════════════════════════════════════════════════"

  # Check input directory
  if [[ ! -d "$INPUT_DIR" ]]; then
    log_error "Diretório de backups não encontrado: ${INPUT_DIR}"
    exit 1
  fi

  confirm_restore

  log_info "Ambiente:   ${ENV}"
  log_info "Host:       ${PGHOST}:${PGPORT}"
  log_info "Utilizador: ${PGUSER}"
  log_info "Input Dir:  ${INPUT_DIR}"
  echo ""

  local failed=()
  local succeeded=()
  local skipped=()

  for db in "${ALL_DATABASES[@]}"; do
    # Find latest backup for this database
    local backup_file
    backup_file=$(find "$INPUT_DIR" -maxdepth 1 -name "${db}_*.sql.gz" -type f 2>/dev/null | sort -r | head -n 1)

    if [[ -z "$backup_file" ]]; then
      log_warn "Nenhum backup encontrado para ${db} — ignorando."
      skipped+=("$db")
      continue
    fi

    log_info "Restaurando ${db} a partir de ${backup_file}..."

    if bash "${SCRIPT_DIR}/restore.sh" \
      --database "$db" \
      --env "$ENV" \
      --input-dir "$INPUT_DIR" \
      --file "$(basename "$backup_file")" \
      --force; then
      succeeded+=("$db")
    else
      failed+=("$db")
    fi
    echo ""
  done

  # ── Post-restore verification ─────────────────────────────────────────────
  if [[ "$VERIFY" == "true" ]] && [[ ${#succeeded[@]} -gt 0 ]]; then
    echo ""
    log_info "═══════════════════════════════════"
    log_info "VERIFICAÇÃO PÓS-RESTORE"
    log_info "═══════════════════════════════════"

    for db in "${succeeded[@]}"; do
      log_info "Verificando ${db}..."
      bash "${SCRIPT_DIR}/verify-restore.sh" --database "$db" --env "$ENV" || true
      echo ""
    done
  fi

  # ── Summary ───────────────────────────────────────────────────────────────
  echo ""
  log_info "═══════════════════════════════════════════════════════"
  log_info "RESULTADO DO FULL RESTORE"
  log_info "═══════════════════════════════════════════════════════"
  log_info "Ambiente:       ${ENV}"
  log_info "Bem-sucedidos:  ${#succeeded[@]} — ${succeeded[*]:-nenhum}"
  log_info "Falharam:       ${#failed[@]} — ${failed[*]:-nenhum}"
  log_info "Ignorados:      ${#skipped[@]} — ${skipped[*]:-nenhum}"
  log_info "═══════════════════════════════════════════════════════"

  if [[ ${#failed[@]} -gt 0 ]]; then
    log_error "Restore falhou para: ${failed[*]}"
    exit 1
  fi

  if [[ ${#skipped[@]} -eq ${#ALL_DATABASES[@]} ]]; then
    log_error "Nenhum banco foi restaurado — nenhum backup encontrado."
    exit 1
  fi

  log_success "Full restore concluído com sucesso."
}

main
