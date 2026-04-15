#!/usr/bin/env bash
# =============================================================================
# NexTraceOne — pgvector Setup Script (Linux / macOS)
# =============================================================================
#
# Instala e configura a extensão pgvector no PostgreSQL para suporte a
# embeddings vectoriais e busca semântica ANN (HNSW/IVFFlat).
#
# Uso:
#   chmod +x scripts/db/setup-pgvector.sh
#   ./scripts/db/setup-pgvector.sh [OPTIONS]
#
# Options:
#   --host        Host do PostgreSQL (default: localhost)
#   --port        Porta do PostgreSQL (default: 5432)
#   --user        Utilizador superuser (default: nextraceone)
#   --db          Base de dados alvo (default: nextraceone)
#   --pg-version  Versão do PostgreSQL (default: 16)
#   --skip-apt    Não instalar via apt (apenas configurar extensão)
#   --docker      Instalar dentro do container Docker PostgreSQL
#   --help        Mostrar ajuda
#
# Requerimentos:
#   - PostgreSQL 14+ (recomendado 16)
#   - Acesso superuser ao cluster PostgreSQL
#   - apt/brew (para instalação de pacotes) OU imagem pgvector/pgvector:pg16
#
# =============================================================================

set -euo pipefail

# ── Defaults ──────────────────────────────────────────────────────────────────
PG_HOST="${POSTGRES_HOST:-localhost}"
PG_PORT="${POSTGRES_PORT:-5432}"
PG_USER="${POSTGRES_USER:-nextraceone}"
PG_DB="${POSTGRES_DB:-nextraceone}"
PG_VERSION="${POSTGRES_VERSION:-16}"
SKIP_APT=false
DOCKER_MODE=false

# ── Colours ───────────────────────────────────────────────────────────────────
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

log_info()    { echo -e "${BLUE}[INFO]${NC}  $*"; }
log_success() { echo -e "${GREEN}[OK]${NC}    $*"; }
log_warn()    { echo -e "${YELLOW}[WARN]${NC}  $*"; }
log_error()   { echo -e "${RED}[ERROR]${NC} $*" >&2; }

# ── Argument parsing ───────────────────────────────────────────────────────────
while [[ $# -gt 0 ]]; do
  case "$1" in
    --host)        PG_HOST="$2";    shift 2 ;;
    --port)        PG_PORT="$2";    shift 2 ;;
    --user)        PG_USER="$2";    shift 2 ;;
    --db)          PG_DB="$2";      shift 2 ;;
    --pg-version)  PG_VERSION="$2"; shift 2 ;;
    --skip-apt)    SKIP_APT=true;   shift ;;
    --docker)      DOCKER_MODE=true; shift ;;
    --help)
      sed -n '3,40p' "$0"
      exit 0
      ;;
    *)
      log_error "Unknown option: $1"
      exit 1
      ;;
  esac
done

echo ""
echo "╔══════════════════════════════════════════════════════════════╗"
echo "║         NexTraceOne — pgvector Setup (E-A01)                 ║"
echo "╚══════════════════════════════════════════════════════════════╝"
echo ""
log_info "Host:       $PG_HOST:$PG_PORT"
log_info "Database:   $PG_DB"
log_info "User:       $PG_USER"
log_info "PG Version: $PG_VERSION"
echo ""

# ── Step 1: Install pgvector OS packages ─────────────────────────────────────
if [[ "$DOCKER_MODE" == true ]]; then
  log_info "Docker mode: installing pgvector inside PostgreSQL container..."

  CONTAINER=$(docker ps --filter "name=postgres" --format "{{.Names}}" | head -1)
  if [[ -z "$CONTAINER" ]]; then
    CONTAINER=$(docker ps --filter "ancestor=postgres" --format "{{.Names}}" | head -1)
  fi
  if [[ -z "$CONTAINER" ]]; then
    log_warn "No PostgreSQL container found. Attempting to use 'nextraceone-postgres-1'."
    CONTAINER="nextraceone-postgres-1"
  fi

  log_info "PostgreSQL container: $CONTAINER"

  # Check if pgvector is already installed in the container
  if docker exec "$CONTAINER" psql -U "$PG_USER" -d "$PG_DB" \
      -c "SELECT extname FROM pg_extension WHERE extname = 'vector';" 2>/dev/null | grep -q "vector"; then
    log_success "pgvector extension already installed in container."
  else
    log_info "Installing pgvector packages in container..."
    docker exec "$CONTAINER" bash -c "
      apt-get update -qq 2>/dev/null || true
      apt-get install -y -qq postgresql-${PG_VERSION}-pgvector 2>/dev/null || \
      (apt-get install -y -qq build-essential git && \
       cd /tmp && git clone --branch v0.7.4 https://github.com/pgvector/pgvector.git && \
       cd pgvector && make && make install)
    " || log_warn "Package installation failed — trying alternative method"
  fi

elif [[ "$SKIP_APT" == false ]]; then
  log_info "Installing pgvector OS package..."

  # Detect OS
  if command -v apt-get &>/dev/null; then
    # Debian/Ubuntu
    log_info "Detected apt-based system"
    if ! dpkg -l postgresql-"${PG_VERSION}"-pgvector &>/dev/null; then
      sudo apt-get update -qq
      sudo apt-get install -y postgresql-"${PG_VERSION}"-pgvector
      log_success "pgvector package installed via apt"
    else
      log_success "pgvector package already installed"
    fi

  elif command -v brew &>/dev/null; then
    # macOS Homebrew
    log_info "Detected Homebrew (macOS)"
    if ! brew list pgvector &>/dev/null; then
      brew install pgvector
      log_success "pgvector installed via Homebrew"
    else
      log_success "pgvector already installed via Homebrew"
    fi

  elif command -v yum &>/dev/null; then
    # RHEL/CentOS
    log_info "Detected yum-based system"
    sudo yum install -y pgvector_"${PG_VERSION}" || \
      log_warn "pgvector not available via yum — compile from source recommended"

  else
    log_warn "Package manager not recognized. Installing from source..."
    PGVECTOR_VERSION="v0.7.4"
    PGVECTOR_DIR="/tmp/pgvector-build"
    rm -rf "$PGVECTOR_DIR"
    git clone --branch "$PGVECTOR_VERSION" https://github.com/pgvector/pgvector.git "$PGVECTOR_DIR"
    cd "$PGVECTOR_DIR"
    make
    sudo make install
    cd -
    rm -rf "$PGVECTOR_DIR"
    log_success "pgvector compiled and installed from source"
  fi
else
  log_info "Skipping OS package installation (--skip-apt)"
fi

# ── Step 2: Enable extension in database ──────────────────────────────────────
log_info "Enabling 'vector' extension in database '$PG_DB'..."

PSQL_CMD="psql -h $PG_HOST -p $PG_PORT -U $PG_USER -d $PG_DB"

if [[ "$DOCKER_MODE" == true ]]; then
  PSQL_CMD="docker exec $CONTAINER psql -U $PG_USER -d $PG_DB"
fi

$PSQL_CMD << 'SQL'
-- Enable pgvector extension
CREATE EXTENSION IF NOT EXISTS vector;

-- Verify installation
SELECT
  extname AS extension,
  extversion AS version
FROM pg_extension
WHERE extname = 'vector';
SQL

log_success "Extension 'vector' enabled successfully"

# ── Step 3: Verify vector column on ai_knowledge_sources ──────────────────────
log_info "Checking for vector column on aik_knowledge_sources..."

COLUMN_EXISTS=$($PSQL_CMD -t -c "
  SELECT COUNT(*)
  FROM information_schema.columns
  WHERE table_name = 'aik_knowledge_sources'
    AND column_name = 'EmbeddingVector';
" 2>/dev/null | tr -d '[:space:]') || COLUMN_EXISTS=0

if [[ "$COLUMN_EXISTS" == "1" ]]; then
  log_success "Vector column 'EmbeddingVector' exists on aik_knowledge_sources"
else
  log_warn "Vector column not yet created — run EF Core migrations to create it:"
  echo ""
  echo "  # From the repository root:"
  echo "  dotnet ef database update \\
    --project src/modules/aiknowledge/NexTraceOne.AIKnowledge.Infrastructure \\
    --startup-project src/NexTraceOne \\
    --context AiGovernanceDbContext"
  echo ""
fi

# ── Step 4: Show HNSW index status ────────────────────────────────────────────
log_info "Checking HNSW index status..."

$PSQL_CMD << 'SQL'
SELECT
  indexname,
  indexdef
FROM pg_indexes
WHERE tablename = 'aik_knowledge_sources'
  AND indexname LIKE '%embedding%'
  AND indexdef LIKE '%hnsw%';
SQL

# ── Step 5: Show useful performance settings ──────────────────────────────────
echo ""
log_info "Recommended PostgreSQL settings for pgvector performance:"
echo ""
echo "  # In postgresql.conf:"
echo "  maintenance_work_mem = 1GB   # For HNSW index build"
echo "  max_parallel_workers_per_gather = 4"
echo ""
echo "  # For HNSW probing at query time (per session):"
echo "  SET hnsw.ef_search = 100;"
echo ""

log_success "pgvector setup complete!"
echo ""
echo "╔══════════════════════════════════════════════════════════════╗"
echo "║  pgvector is ready for semantic embeddings in NexTraceOne    ║"
echo "║                                                              ║"
echo "║  Next steps:                                                 ║"
echo "║  1. Run EF Core migrations (adds EmbeddingVector column)     ║"
echo "║  2. Start the application — EmbeddingIndexJob will index     ║"
echo "║     existing knowledge sources automatically                 ║"
echo "╚══════════════════════════════════════════════════════════════╝"
echo ""
