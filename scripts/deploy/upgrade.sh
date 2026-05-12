#!/usr/bin/env bash
# ═══════════════════════════════════════════════════════════════════════════════
# NexTraceOne — Upgrade Script com Preflight Check e Rollback Automático
#
# Uso:
#   bash upgrade.sh --preflight                    # Executa apenas validações
#   bash upgrade.sh --version 1.2.3                # Executa upgrade completo
#   bash upgrade.sh --version 1.2.3 --skip-smoke   # Skip post-deploy smoke check
#   bash upgrade.sh --backup-only                  # Apenas backup, sem upgrade
#
# Exit codes:
#   0 — upgrade bem-sucedido
#   1 — falha no upgrade (rollback executado)
#   2 — argumentos inválidos ou preflight falhou
# ═══════════════════════════════════════════════════════════════════════════════

set -euo pipefail

# ── Configuração ─────────────────────────────────────────────────────────────
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
INSTALL_DIR="${NEXTRACE_INSTALL_DIR:-/opt/nextraceone}"
BACKUP_DIR="${INSTALL_DIR}/backups"
LOG_FILE="/var/log/nextraceone/upgrade.log"
TIMESTAMP=$(date +%Y%m%d_%H%M%S)
VERSION=""
PREFLIGHT_ONLY=false
SKIP_SMOKE=false
BACKUP_ONLY=false
REQUIRED_DISK_MB=2048
SMOKE_TIMEOUT=120

# Serviços a gerir
SERVICES=("nextraceone-apihost" "nextraceone-workers" "nextraceone-ingestion")

# ── Logging ──────────────────────────────────────────────────────────────────
log() {
    local level="$1"
    shift
    local message="$*"
    local timestamp=$(date '+%Y-%m-%d %H:%M:%S')
    echo "[${timestamp}] [${level}] ${message}" | tee -a "${LOG_FILE}"
}

log_info() { log "INFO" "$@"; }
log_warn() { log "WARN" "$@"; }
log_error() { log "ERROR" "$@"; }

# ── Argumentos ───────────────────────────────────────────────────────────────
parse_args() {
    while [[ $# -gt 0 ]]; do
        case "$1" in
            --preflight)
                PREFLIGHT_ONLY=true
                shift
                ;;
            --version)
                VERSION="$2"
                shift 2
                ;;
            --skip-smoke)
                SKIP_SMOKE=true
                shift
                ;;
            --backup-only)
                BACKUP_ONLY=true
                shift
                ;;
            --help)
                echo "Uso: bash upgrade.sh [OPÇÕES]"
                echo ""
                echo "Opções:"
                echo "  --preflight       Executa apenas validações pré-upgrade"
                echo "  --version VER     Versão a instalar (obrigatório para upgrade)"
                echo "  --skip-smoke      Ignora smoke check pós-deploy"
                echo "  --backup-only     Apenas backup, sem executar upgrade"
                echo "  --help            Mostra esta ajuda"
                exit 0
                ;;
            *)
                log_error "Argumento desconhecido: $1"
                exit 2
                ;;
        esac
    done
}

# ── Preflight Checks ─────────────────────────────────────────────────────────
preflight_checks() {
    log_info "=== Executando validações pré-upgrade ==="
    local errors=0

    # 1. Verificar permissões de escrita
    log_info "Verificando permissões de escrita em ${INSTALL_DIR}..."
    if ! touch "${INSTALL_DIR}/.write_test" 2>/dev/null; then
        log_error "Sem permissão de escrita em ${INSTALL_DIR}"
        ((errors++))
    else
        rm -f "${INSTALL_DIR}/.write_test"
        log_info "✓ Permissões de escrita OK"
    fi

    # 2. Verificar espaço em disco (>2GB livres)
    log_info "Verificando espaço em disco disponível..."
    local available_kb
    available_kb=$(df -k "${INSTALL_DIR}" | awk 'NR==2 {print $4}')
    local available_mb=$((available_kb / 1024))
    if [[ ${available_mb} -lt ${REQUIRED_DISK_MB} ]]; then
        log_error "Espaço em disco insuficiente: ${available_mb}MB disponíveis, ${REQUIRED_DISK_MB}MB necessários"
        ((errors++))
    else
        log_info "✓ Espaço em disco OK (${available_mb}MB disponíveis)"
    fi

    # 3. Verificar PostgreSQL acessível
    log_info "Verificando conectividade PostgreSQL..."
    if command -v psql &>/dev/null; then
        if ! psql -h localhost -U nextraceone -d nextraceone -c "SELECT 1;" &>/dev/null; then
            log_warn "PostgreSQL não responde em localhost (pode ser normal se usar container)"
        else
            log_info "✓ PostgreSQL acessível"
        fi
    else
        log_warn "psql não encontrado - pulando verificação PostgreSQL"
    fi

    # 4. Verificar portas disponíveis (apenas se serviços estão parados)
    log_info "Verificando portas 8080, 8081, 8082..."
    for port in 8080 8081 8082; do
        if ss -tuln 2>/dev/null | grep -q ":${port} " || netstat -tuln 2>/dev/null | grep -q ":${port} "; then
            log_warn "Porta ${port} já está em uso"
        else
            log_info "✓ Porta ${port} disponível"
        fi
    done

    # 5. Verificar backup existente
    log_info "Verificando backups existentes..."
    if [[ -d "${BACKUP_DIR}" ]] && [[ $(ls -A "${BACKUP_DIR}" 2>/dev/null) ]]; then
        local latest_backup
        latest_backup=$(ls -t "${BACKUP_DIR}" | head -n1)
        log_info "✓ Último backup: ${latest_backup}"
    else
        log_warn "Nenhum backup encontrado - será criado antes do upgrade"
    fi

    # Resultado
    if [[ ${errors} -gt 0 ]]; then
        log_error "Preflight falhou com ${errors} erro(s)"
        return 1
    fi

    log_info "=== Todas as validações pré-upgrade passaram ==="
    return 0
}

# ── Backup ───────────────────────────────────────────────────────────────────
create_backup() {
    log_info "=== Criando backup pré-upgrade ==="
    
    mkdir -p "${BACKUP_DIR}"
    local backup_path="${BACKUP_DIR}/backup_${TIMESTAMP}"
    mkdir -p "${backup_path}"

    # Backup de configuração
    if [[ -d "${INSTALL_DIR}/config" ]]; then
        log_info "Backup de configuração..."
        cp -r "${INSTALL_DIR}/config" "${backup_path}/config"
    fi

    # Backup de dados (se aplicável)
    if [[ -d "${INSTALL_DIR}/data" ]]; then
        log_info "Backup de dados..."
        cp -r "${INSTALL_DIR}/data" "${backup_path}/data"
    fi

    # Backup de binários atuais
    for service in "${SERVICES[@]}"; do
        local service_dir="${INSTALL_DIR}/${service}"
        if [[ -d "${service_dir}" ]]; then
            log_info "Backup de ${service}..."
            cp -r "${service_dir}" "${backup_path}/${service}"
        fi
    done

    # Comprimir backup
    log_info "Comprimindo backup..."
    tar czf "${backup_path}.tar.gz" -C "${BACKUP_DIR}" "backup_${TIMESTAMP}"
    rm -rf "${backup_path}"

    log_info "✓ Backup criado: ${backup_path}.tar.gz"
    echo "${backup_path}.tar.gz"
}

# ── Executar Upgrade ─────────────────────────────────────────────────────────
execute_upgrade() {
    local release_zip="$1"
    
    log_info "=== Executando upgrade para versão ${VERSION} ==="

    # Extrair release bundle
    log_info "Extraindo release bundle..."
    local temp_dir=$(mktemp -d)
    unzip -q "${release_zip}" -d "${temp_dir}"

    # Parar serviços
    log_info "Parando serviços..."
    for service in "${SERVICES[@]}"; do
        if systemctl is-active --quiet "${service}" 2>/dev/null; then
            systemctl stop "${service}"
            log_info "✓ ${service} parado"
        fi
    done

    # Instalar novos binários (linux-x64 por padrão)
    local platform_dir="${temp_dir}/release/linux-x64"
    if [[ ! -d "${platform_dir}" ]]; then
        log_error "Diretório linux-x64 não encontrado no release bundle"
        rm -rf "${temp_dir}"
        return 1
    fi

    for service in apihost workers ingestion; do
        local service_name="nextraceone-${service}"
        local source_dir="${platform_dir}/${service}"
        local target_dir="${INSTALL_DIR}/${service_name}"

        if [[ -d "${source_dir}" ]]; then
            log_info "Instalando ${service_name}..."
            rm -rf "${target_dir}"
            mkdir -p "${target_dir}"
            cp -r "${source_dir}/." "${target_dir}/"
            chmod +x "${target_dir}"/* 2>/dev/null || true
            log_info "✓ ${service_name} instalado"
        fi
    done

    # Limpar temp
    rm -rf "${temp_dir}"

    # Iniciar serviços
    log_info "Iniciando serviços..."
    for service in "${SERVICES[@]}"; do
        if systemctl start "${service}" 2>/dev/null; then
            log_info "✓ ${service} iniciado"
        else
            log_error "Falha ao iniciar ${service}"
            return 1
        fi
    done

    log_info "=== Upgrade concluído ==="
    return 0
}

# ── Rollback ─────────────────────────────────────────────────────────────────
execute_rollback() {
    local backup_file="$1"
    
    log_warn "=== Executando rollback para backup: ${backup_file} ==="

    if [[ ! -f "${backup_file}" ]]; then
        log_error "Backup não encontrado: ${backup_file}"
        return 1
    fi

    # Parar serviços
    for service in "${SERVICES[@]}"; do
        systemctl stop "${service}" 2>/dev/null || true
    done

    # Restaurar backup
    local temp_dir=$(mktemp -d)
    tar xzf "${backup_file}" -C "${temp_dir}"
    local backup_content="${temp_dir}/backup_${TIMESTAMP}"

    # Restaurar binários
    for service in "${SERVICES[@]}"; do
        if [[ -d "${backup_content}/${service}" ]]; then
            rm -rf "${INSTALL_DIR}/${service}"
            cp -r "${backup_content}/${service}" "${INSTALL_DIR}/${service}"
            log_info "✓ ${service} restaurado"
        fi
    done

    # Restaurar configuração
    if [[ -d "${backup_content}/config" ]]; then
        rm -rf "${INSTALL_DIR}/config"
        cp -r "${backup_content}/config" "${INSTALL_DIR}/config"
        log_info "✓ Configuração restaurada"
    fi

    rm -rf "${temp_dir}"

    # Iniciar serviços
    for service in "${SERVICES[@]}"; do
        systemctl start "${service}" 2>/dev/null || true
    done

    log_warn "=== Rollback concluído ==="
    return 0
}

# ── Smoke Check ──────────────────────────────────────────────────────────────
smoke_check() {
    log_info "=== Executando smoke check pós-deploy ==="
    
    local api_url="${NEXTRACE_API_URL:-http://localhost:8080}"
    local timeout=${SMOKE_TIMEOUT}
    local elapsed=0

    while [[ ${elapsed} -lt ${timeout} ]]; do
        if curl -fsS "${api_url}/health" &>/dev/null; then
            log_info "✓ API Host saudável"
            
            # Verificar outros endpoints críticos
            if curl -fsS "${api_url}/api/v1/identity/health" &>/dev/null; then
                log_info "✓ Identity module saudável"
            fi
            
            log_info "=== Smoke check passou ==="
            return 0
        fi
        
        sleep 5
        elapsed=$((elapsed + 5))
        log_info "Aguardando serviços ficarem saudáveis... (${elapsed}s/${timeout}s)"
    done

    log_error "Smoke check falhou após ${timeout}s"
    return 1
}

# ── Main ─────────────────────────────────────────────────────────────────────
main() {
    parse_args "$@"

    # Criar diretório de logs
    mkdir -p "$(dirname "${LOG_FILE}")"

    log_info "=========================================="
    log_info "NexTraceOne Upgrade Script"
    log_info "Timestamp: ${TIMESTAMP}"
    log_info "=========================================="

    # Preflight check (sempre executado)
    if ! preflight_checks; then
        log_error "Validações pré-upgrade falharam. Abortando."
        exit 2
    fi

    # Modo preflight only
    if [[ "${PREFLIGHT_ONLY}" == "true" ]]; then
        log_info "Modo preflight apenas. Saindo."
        exit 0
    fi

    # Validar versão
    if [[ -z "${VERSION}" ]] && [[ "${BACKUP_ONLY}" != "true" ]]; then
        log_error "Versão é obrigatória para upgrade. Use --version X.Y.Z"
        exit 2
    fi

    # Criar backup
    local backup_file
    backup_file=$(create_backup)

    # Modo backup only
    if [[ "${BACKUP_ONLY}" == "true" ]]; then
        log_info "Backup apenas concluído: ${backup_file}"
        exit 0
    fi

    # Localizar release bundle
    local release_zip="${SCRIPT_DIR}/nextraceone-v${VERSION}-release.zip"
    if [[ ! -f "${release_zip}" ]]; then
        log_error "Release bundle não encontrado: ${release_zip}"
        exit 1
    fi

    # Executar upgrade
    if execute_upgrade "${release_zip}"; then
        # Smoke check (se não foi skipado)
        if [[ "${SKIP_SMOKE}" != "true" ]]; then
            if smoke_check; then
                log_info "Upgrade bem-sucedido!"
                exit 0
            else
                log_error "Smoke check falhou. Executando rollback..."
                execute_rollback "${backup_file}"
                exit 1
            fi
        else
            log_info "Upgrade concluído (smoke check ignorado)"
            exit 0
        fi
    else
        log_error "Upgrade falhou. Executando rollback..."
        execute_rollback "${backup_file}"
        exit 1
    fi
}

main "$@"
