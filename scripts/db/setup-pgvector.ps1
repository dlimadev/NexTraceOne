#Requires -Version 5.1
<#
.SYNOPSIS
    NexTraceOne — pgvector Setup Script (Windows / PowerShell)

.DESCRIPTION
    Instala e configura a extensão pgvector no PostgreSQL para suporte a
    embeddings vectoriais e busca semântica ANN (HNSW/IVFFlat).

    Suporta:
    - Docker Desktop (modo recomendado para desenvolvimento)
    - PostgreSQL instalado localmente no Windows
    - PostgreSQL acessível remotamente via psql

.PARAMETER Host
    Host do PostgreSQL (default: localhost)

.PARAMETER Port
    Porta do PostgreSQL (default: 5432)

.PARAMETER User
    Utilizador PostgreSQL com permissões de superuser (default: nextraceone)

.PARAMETER Database
    Base de dados alvo (default: nextraceone)

.PARAMETER PgVersion
    Versão do PostgreSQL (default: 16)

.PARAMETER DockerMode
    Instalar dentro do container Docker PostgreSQL

.PARAMETER SkipInstall
    Não instalar pacotes — apenas configurar a extensão na base de dados

.EXAMPLE
    # Setup via Docker (modo recomendado para dev local)
    .\scripts\db\setup-pgvector.ps1 -DockerMode

.EXAMPLE
    # Setup com PostgreSQL local
    .\scripts\db\setup-pgvector.ps1 -User postgres -Database nextraceone

.EXAMPLE
    # Apenas configurar extensão (pgvector já instalado)
    .\scripts\db\setup-pgvector.ps1 -SkipInstall

.NOTES
    Referência: docs/AI-MODULE-EXECUTION-PLAN-V2.md — E-A01
    Data: 2026-04-15
#>

[CmdletBinding()]
param(
    [string]$Host       = $env:POSTGRES_HOST ?? 'localhost',
    [int]$Port          = [int]($env:POSTGRES_PORT ?? '5432'),
    [string]$User       = $env:POSTGRES_USER ?? 'nextraceone',
    [string]$Database   = $env:POSTGRES_DB ?? 'nextraceone',
    [string]$PgVersion  = $env:POSTGRES_VERSION ?? '16',
    [switch]$DockerMode,
    [switch]$SkipInstall
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# ── Helpers ────────────────────────────────────────────────────────────────────
function Write-Step   { Write-Host "[INFO]  $($args -join ' ')" -ForegroundColor Cyan }
function Write-Ok     { Write-Host "[OK]    $($args -join ' ')" -ForegroundColor Green }
function Write-Warn   { Write-Host "[WARN]  $($args -join ' ')" -ForegroundColor Yellow }
function Write-Err    { Write-Host "[ERROR] $($args -join ' ')" -ForegroundColor Red }

function Invoke-Psql {
    param([string]$Sql, [switch]$TupleOnly)

    $args_list = @('-h', $Host, '-p', $Port, '-U', $User, '-d', $Database)
    if ($TupleOnly) { $args_list += '-t' }

    if ($DockerMode) {
        $containerName = Get-DockerPostgresContainer
        $result = $Sql | docker exec -i $containerName psql -U $User -d $Database
    } else {
        $result = $Sql | & psql $args_list
    }

    if ($LASTEXITCODE -ne 0) {
        Write-Err "psql command failed (exit code $LASTEXITCODE)"
        throw "psql failed"
    }
    return $result
}

function Get-DockerPostgresContainer {
    $container = docker ps --filter "name=postgres" --format "{{.Names}}" 2>$null | Select-Object -First 1
    if (-not $container) {
        $container = docker ps --filter "ancestor=postgres" --format "{{.Names}}" 2>$null | Select-Object -First 1
    }
    if (-not $container) {
        $container = 'nextraceone-postgres-1'
        Write-Warn "No PostgreSQL container found. Using default: $container"
    }
    return $container
}

function Test-Command {
    param([string]$Cmd)
    return [bool](Get-Command $Cmd -ErrorAction SilentlyContinue)
}

# ── Banner ─────────────────────────────────────────────────────────────────────
Write-Host ""
Write-Host "╔══════════════════════════════════════════════════════════════╗" -ForegroundColor DarkCyan
Write-Host "║         NexTraceOne — pgvector Setup (E-A01)                 ║" -ForegroundColor DarkCyan
Write-Host "╚══════════════════════════════════════════════════════════════╝" -ForegroundColor DarkCyan
Write-Host ""
Write-Step "Host:       ${Host}:${Port}"
Write-Step "Database:   $Database"
Write-Step "User:       $User"
Write-Step "PG Version: $PgVersion"
Write-Step "Docker:     $DockerMode"
Write-Host ""

# ── Step 1: Install pgvector ───────────────────────────────────────────────────
if ($SkipInstall) {
    Write-Step "Skipping package installation (--SkipInstall)"
} elseif ($DockerMode) {
    Write-Step "Docker mode: installing pgvector in PostgreSQL container..."
    $container = Get-DockerPostgresContainer
    Write-Step "PostgreSQL container: $container"

    # Check if already installed
    $extCheck = docker exec $container psql -U $User -d $Database -t -c `
        "SELECT extname FROM pg_extension WHERE extname = 'vector';" 2>$null

    if ($extCheck -match 'vector') {
        Write-Ok "pgvector already installed in container."
    } else {
        Write-Step "Installing pgvector in container..."
        $installScript = @"
apt-get update -qq 2>/dev/null || true
apt-get install -y -qq postgresql-${PgVersion}-pgvector 2>/dev/null || (
    apt-get install -y -qq build-essential git &&
    cd /tmp &&
    git clone --branch v0.7.4 https://github.com/pgvector/pgvector.git &&
    cd pgvector && make && make install
)
"@
        $installScript | docker exec -i $container bash
        if ($LASTEXITCODE -ne 0) {
            Write-Warn "Package install returned non-zero. Proceeding anyway..."
        } else {
            Write-Ok "pgvector installed in container."
        }
    }
} else {
    # Local Windows PostgreSQL
    Write-Step "Local Windows mode: checking for pgvector..."

    # Try winget or chocolatey
    if (Test-Command 'winget') {
        Write-Step "Checking pgvector via winget..."
        Write-Warn "pgvector is not available via winget. Use Docker mode for easiest setup."
        Write-Warn "Alternative: use a prebuilt pgvector binary from:"
        Write-Host "  https://github.com/pgvector/pgvector/releases" -ForegroundColor DarkGray
    } elseif (Test-Command 'choco') {
        Write-Step "Attempting install via Chocolatey..."
        choco install pgvector --yes 2>$null
        if ($LASTEXITCODE -eq 0) {
            Write-Ok "pgvector installed via Chocolatey."
        } else {
            Write-Warn "Chocolatey install failed."
        }
    }

    Write-Host ""
    Write-Host "  Manual Windows installation instructions:" -ForegroundColor DarkYellow
    Write-Host "  1. Download prebuilt binary from: https://github.com/pgvector/pgvector/releases" -ForegroundColor DarkGray
    Write-Host "  2. Copy pgvector.dll to PostgreSQL lib directory (e.g., C:\Program Files\PostgreSQL\16\lib\)" -ForegroundColor DarkGray
    Write-Host "  3. Copy vector.control + vector-*.sql to share\extension directory" -ForegroundColor DarkGray
    Write-Host "  4. Re-run this script with -SkipInstall" -ForegroundColor DarkGray
    Write-Host ""
}

# ── Step 2: Enable extension in database ──────────────────────────────────────
Write-Step "Enabling 'vector' extension in database '$Database'..."

try {
    $result = Invoke-Psql @"
-- Enable pgvector extension
CREATE EXTENSION IF NOT EXISTS vector;

-- Verify installation
SELECT
  extname AS extension,
  extversion AS version
FROM pg_extension
WHERE extname = 'vector';
"@
    Write-Host $result
    Write-Ok "Extension 'vector' enabled successfully."
} catch {
    Write-Err "Failed to enable pgvector extension: $_"
    Write-Host ""
    Write-Host "  Troubleshooting:" -ForegroundColor DarkYellow
    Write-Host "  - Ensure the pgvector shared library is installed on the PostgreSQL server" -ForegroundColor DarkGray
    Write-Host "  - For Docker: use image 'pgvector/pgvector:pg16' in docker-compose.yml" -ForegroundColor DarkGray
    Write-Host "  - Ensure your user has SUPERUSER or CREATE EXTENSION privilege" -ForegroundColor DarkGray
    exit 1
}

# ── Step 3: Verify EmbeddingVector column ──────────────────────────────────────
Write-Step "Checking for EmbeddingVector column on aik_knowledge_sources..."

try {
    $colCheck = Invoke-Psql -TupleOnly @"
SELECT COUNT(*)
FROM information_schema.columns
WHERE table_name = 'aik_knowledge_sources'
  AND column_name = 'EmbeddingVector';
"@

    if ($colCheck -match '1') {
        Write-Ok "Vector column 'EmbeddingVector' exists on aik_knowledge_sources."
    } else {
        Write-Warn "Vector column not yet created. Run EF Core migrations:"
        Write-Host ""
        Write-Host "  # From the repository root:" -ForegroundColor DarkGray
        Write-Host "  dotnet ef database update \" -ForegroundColor DarkGray
        Write-Host "    --project src\modules\aiknowledge\NexTraceOne.AIKnowledge.Infrastructure \" -ForegroundColor DarkGray
        Write-Host "    --startup-project src\NexTraceOne \" -ForegroundColor DarkGray
        Write-Host "    --context AiGovernanceDbContext" -ForegroundColor DarkGray
        Write-Host ""
    }
} catch {
    Write-Warn "Could not check column (table may not exist yet): $_"
}

# ── Step 4: HNSW index status ──────────────────────────────────────────────────
Write-Step "Checking HNSW index status..."

try {
    $indexCheck = Invoke-Psql @"
SELECT indexname, indexdef
FROM pg_indexes
WHERE tablename = 'aik_knowledge_sources'
  AND indexname LIKE '%embedding%';
"@
    Write-Host $indexCheck
} catch {
    Write-Warn "Could not check indexes: $_"
}

# ── Step 5: Docker Compose recommendation ─────────────────────────────────────
Write-Host ""
Write-Step "Docker Compose recommendation for development:"
Write-Host ""
Write-Host "  Use the pgvector-enabled PostgreSQL image in docker-compose.yml:" -ForegroundColor DarkGray
Write-Host ""
Write-Host "  postgres:" -ForegroundColor DarkGray
Write-Host "    image: pgvector/pgvector:pg16   # replaces postgres:16-alpine" -ForegroundColor Green
Write-Host ""
Write-Host "  This image includes pgvector pre-installed and ready to use." -ForegroundColor DarkGray

# ── Done ───────────────────────────────────────────────────────────────────────
Write-Host ""
Write-Host "╔══════════════════════════════════════════════════════════════╗" -ForegroundColor DarkGreen
Write-Host "║  pgvector is ready for semantic embeddings in NexTraceOne    ║" -ForegroundColor DarkGreen
Write-Host "║                                                              ║" -ForegroundColor DarkGreen
Write-Host "║  Next steps:                                                 ║" -ForegroundColor DarkGreen
Write-Host "║  1. Run EF Core migrations (adds EmbeddingVector column)     ║" -ForegroundColor DarkGreen
Write-Host "║  2. Start the application — EmbeddingIndexJob will index     ║" -ForegroundColor DarkGreen
Write-Host "║     existing knowledge sources automatically                 ║" -ForegroundColor DarkGreen
Write-Host "╚══════════════════════════════════════════════════════════════╝" -ForegroundColor DarkGreen
Write-Host ""
