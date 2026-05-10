#!/usr/bin/env bash
# =============================================================================
# NexTraceOne — On-Premises Upgrade Script (W3-02)
#
# Uso: ./upgrade.sh [--version v1.2.3] [--install-dir /opt/nextraceone]
#
# Passos:
#   1. Para os serviços NexTrace (systemd)
#   2. Cria backup do directório actual
#   3. Extrai novo bundle
#   4. Restaura configuração (appsettings.json, .env)
#   5. Reinicia serviços
#   6. Verifica health checks
# =============================================================================

set -euo pipefail

# ── Configuração ──────────────────────────────────────────────────────────────

INSTALL_DIR="${NEXTTRACE_INSTALL_DIR:-/opt/nextraceone}"
BACKUP_DIR="${NEXTTRACE_BACKUP_DIR:-/var/nextraceone/upgrade-backups}"
HEALTH_URL="${NEXTTRACE_HEALTH_URL:-http://localhost:5000/health}"
MAX_HEALTH_RETRIES=12
HEALTH_INTERVAL=5

SERVICES=(
    "nextraceone-api"
    "nextraceone-workers"
    "nextraceone-ingestion"
)

# ── Helpers ───────────────────────────────────────────────────────────────────

log() { echo "[$(date -Iseconds)] $*" >&2; }
error() { log "ERROR: $*"; exit 1; }

check_root() {
    if [[ $EUID -ne 0 ]]; then
        error "Este script deve ser executado como root (sudo ./upgrade.sh)."
    fi
}

stop_services() {
    log "A parar serviços NexTraceOne..."
    for svc in "${SERVICES[@]}"; do
        if systemctl is-active --quiet "$svc" 2>/dev/null; then
            systemctl stop "$svc" && log "  ✓ $svc parado"
        else
            log "  ⚠ $svc não estava activo — a ignorar"
        fi
    done
}

start_services() {
    log "A iniciar serviços NexTraceOne..."
    for svc in "${SERVICES[@]}"; do
        systemctl start "$svc" && log "  ✓ $svc iniciado"
    done
}

backup_current() {
    local timestamp
    timestamp=$(date +%Y%m%d_%H%M%S)
    local backup_path="$BACKUP_DIR/pre-upgrade_$timestamp"

    mkdir -p "$BACKUP_DIR"
    log "A criar backup de '$INSTALL_DIR' em '$backup_path'..."

    if [[ -d "$INSTALL_DIR" ]]; then
        cp -a "$INSTALL_DIR" "$backup_path"
        log "  ✓ Backup criado: $backup_path"
    else
        log "  ⚠ Directório de instalação '$INSTALL_DIR' não existe — sem backup"
    fi
}

extract_bundle() {
    local bundle="$1"
    log "A extrair bundle '$bundle' para '$INSTALL_DIR'..."

    # Preserva ficheiros de configuração
    local configs=()
    for f in appsettings.json appsettings.Production.json .env; do
        local path="$INSTALL_DIR/apihost/$f"
        [[ -f "$path" ]] && configs+=("$path")
    done

    mkdir -p "$INSTALL_DIR"
    tar -xzf "$bundle" -C "$INSTALL_DIR"
    log "  ✓ Bundle extraído"

    # Restaura configuração (sobrepõe defaults do bundle)
    for cfg in "${configs[@]}"; do
        local filename
        filename=$(basename "$cfg")
        local target="$INSTALL_DIR/apihost/$filename"
        cp "$cfg" "$target"
        log "  ✓ Configuração restaurada: $filename"
    done
}

wait_for_health() {
    log "A verificar health check em $HEALTH_URL..."
    local retries=0
    while [[ $retries -lt $MAX_HEALTH_RETRIES ]]; do
        local status
        status=$(curl -sf -o /dev/null -w "%{http_code}" "$HEALTH_URL" 2>/dev/null || echo "000")

        if [[ "$status" == "200" ]]; then
            log "  ✓ Health check OK após $((retries * HEALTH_INTERVAL))s"
            return 0
        fi

        log "  ⟳ Health check: $status — a aguardar ${HEALTH_INTERVAL}s (tentativa $((retries + 1))/$MAX_HEALTH_RETRIES)..."
        sleep "$HEALTH_INTERVAL"
        ((retries++))
    done

    error "Health check falhou após $((MAX_HEALTH_RETRIES * HEALTH_INTERVAL))s. Verificar logs: journalctl -u nextraceone-api -n 50"
}

# ── Main ──────────────────────────────────────────────────────────────────────

main() {
    local bundle=""

    while [[ $# -gt 0 ]]; do
        case "$1" in
            --bundle) bundle="$2"; shift 2;;
            --install-dir) INSTALL_DIR="$2"; shift 2;;
            --help|-h)
                echo "Uso: $0 --bundle nextraceone-linux-x64-v1.2.3.tar.gz [--install-dir /opt/nextraceone]"
                exit 0
                ;;
            *) error "Argumento desconhecido: $1";;
        esac
    done

    [[ -z "$bundle" ]] && error "Argumento --bundle é obrigatório."
    [[ -f "$bundle" ]] || error "Ficheiro de bundle não encontrado: $bundle"

    check_root

    log "=== NexTraceOne Upgrade ==="
    log "Bundle: $bundle"
    log "Install dir: $INSTALL_DIR"

    stop_services
    backup_current
    extract_bundle "$bundle"
    start_services
    wait_for_health

    log "=== Upgrade concluído com sucesso! ==="
}

main "$@"
