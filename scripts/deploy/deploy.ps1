# ═══════════════════════════════════════════════════════════════════════════════
# NexTraceOne — Deploy Automático com Migrações (PowerShell / Windows)
#
# Orquestra o deploy completo de uma nova versão:
#   1. Pull das imagens Docker do registo
#   2. Aplicação de migrations de base de dados (todos os DbContexts)
#   3. Recriação dos containers com as novas imagens
#   4. Smoke check de saúde pós-deploy
#   5. Rollback automático em caso de falha
#
# Uso:
#   .\scripts\deploy\deploy.ps1 -Tag v1.2.3 -Registry ghcr.io/owner/nextraceone
#   .\scripts\deploy\deploy.ps1 -Tag abc123 -Registry ghcr.io/owner/nextraceone -Env Staging
#   .\scripts\deploy\deploy.ps1 -DryRun -Tag v1.2.3 -Registry ghcr.io/owner/nextraceone
# ═══════════════════════════════════════════════════════════════════════════════

[CmdletBinding(SupportsShouldProcess)]
param(
    [Parameter(Mandatory)]
    [string]$Tag,

    [Parameter(Mandatory)]
    [string]$Registry,

    [Parameter()]
    [ValidateSet("Development", "Staging", "Production")]
    [string]$Env = $env:MIGRATION_ENV ?? "Staging",

    [Parameter()]
    [switch]$SkipSmoke,

    [Parameter()]
    [switch]$SkipMigration,

    [Parameter()]
    [switch]$SkipRollback,

    [Parameter()]
    [switch]$DryRun,

    [Parameter()]
    [string]$ApiUrl     = $env:APIHOST_URL ?? "",

    [Parameter()]
    [string]$FrontendUrl = $env:FRONTEND_URL ?? "",

    [Parameter()]
    [int]$SmokeTimeout = 60,

    [Parameter()]
    [string]$ConnIdentity   = $env:CONN_IDENTITY  ?? "",
    [Parameter()]
    [string]$ConnCatalog    = $env:CONN_CATALOG   ?? "",
    [Parameter()]
    [string]$ConnOperations = $env:CONN_OPERATIONS ?? "",
    [Parameter()]
    [string]$ConnAi         = $env:CONN_AI        ?? ""
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$RepoRoot    = Resolve-Path "$PSScriptRoot\..\.."
$ComposeFile = Join-Path $RepoRoot "docker-compose.production.yml"
$Services    = @("apihost", "workers", "ingestion", "frontend")
$PreviousTag = ""

# ── Colors ────────────────────────────────────────────────────────────────────
function Write-Info    { param([string]$msg) Write-Host "$(Get-Date -Format 'yyyy-MM-ddTHH:mm:ssZ' -AsUTC) [INFO]  $msg" -ForegroundColor Cyan }
function Write-OK      { param([string]$msg) Write-Host "$(Get-Date -Format 'yyyy-MM-ddTHH:mm:ssZ' -AsUTC) [OK]    $msg" -ForegroundColor Green }
function Write-Warn    { param([string]$msg) Write-Host "$(Get-Date -Format 'yyyy-MM-ddTHH:mm:ssZ' -AsUTC) [WARN]  $msg" -ForegroundColor Yellow }
function Write-Err     { param([string]$msg) Write-Host "$(Get-Date -Format 'yyyy-MM-ddTHH:mm:ssZ' -AsUTC) [ERROR] $msg" -ForegroundColor Red }
function Write-Step    { param([string]$msg) Write-Host "`n$('═' * 60)`n══ $msg`n$('═' * 60)" -ForegroundColor Blue }

# ── Helper: executa ou simula comando ─────────────────────────────────────────
function Invoke-Deploy {
    param([string]$Cmd, [string[]]$Args)
    if ($DryRun) {
        Write-Host "[DRY-RUN] $Cmd $($Args -join ' ')" -ForegroundColor Yellow
        return
    }
    & $Cmd @Args
    if ($LASTEXITCODE -ne 0) {
        throw "Comando falhou com exit code $LASTEXITCODE: $Cmd $($Args -join ' ')"
    }
}

# ── Step 1: Capturar tag anterior ─────────────────────────────────────────────
function Get-PreviousTag {
    Write-Step "Capturando estado anterior para rollback"
    try {
        $img = docker inspect --format='{{index .Config.Image}}' "nextraceone-apihost" 2>$null
        if ($img -match ':([^:]+)$') {
            $script:PreviousTag = $Matches[1]
            Write-Info "Tag anterior detectada: $PreviousTag"
        }
    } catch {
        Write-Warn "Não foi possível detectar tag anterior — rollback não disponível"
        $script:SkipRollback = $true
    }
}

# ── Step 2: Pull de imagens ──────────────────────────────────────────────────
function Invoke-PullImages {
    Write-Step "Pulling imagens do registo $Registry (tag: $Tag)"
    foreach ($service in $Services) {
        $image = "$Registry/${service}:$Tag"
        Write-Info "Pulling $image..."
        Invoke-Deploy "docker" @("pull", $image)
        Write-OK "$service → $image"
    }
}

# ── Step 3: Aplicar migrations ───────────────────────────────────────────────
function Invoke-ApplyMigrations {
    Write-Step "Aplicando migrations (ambiente: $Env)"

    $migScript = Join-Path $RepoRoot "scripts\db\apply-migrations.ps1"
    if (-not (Test-Path $migScript)) {
        throw "Script de migrations não encontrado: $migScript"
    }

    $params = @{ Env = $Env }
    if ($ConnIdentity)   { $params["ConnIdentity"]   = $ConnIdentity }
    if ($ConnCatalog)    { $params["ConnCatalog"]    = $ConnCatalog }
    if ($ConnOperations) { $params["ConnOperations"] = $ConnOperations }
    if ($ConnAi)         { $params["ConnAi"]         = $ConnAi }
    if ($DryRun)         { $params["DryRun"]         = $true }

    if (-not $DryRun) {
        & $migScript @params
        if ($LASTEXITCODE -ne 0) { throw "Migrations falharam com exit code $LASTEXITCODE" }
    } else {
        Write-Host "[DRY-RUN] & $migScript $($params | ConvertTo-Json -Compress)" -ForegroundColor Yellow
    }

    Write-OK "Migrations aplicadas com sucesso"
}

# ── Step 4: Recriar containers ───────────────────────────────────────────────
function Invoke-RecreateContainers {
    Write-Step "Recreando containers com tag $Tag"

    if (-not (Test-Path $ComposeFile)) {
        throw "docker-compose.production.yml não encontrado: $ComposeFile"
    }

    $env:NEXTRACEONE_IMAGE_TAG = $Tag
    $env:NEXTRACEONE_REGISTRY  = $Registry

    Invoke-Deploy "docker" @("compose", "-f", $ComposeFile, "up", "-d", "--remove-orphans")

    Write-OK "Containers recriados com tag $Tag"
}

# ── Step 5: Smoke check ──────────────────────────────────────────────────────
function Invoke-SmokeCheck {
    Write-Step "Executando smoke check (timeout: ${SmokeTimeout}s)"

    $smokeScript = Join-Path $RepoRoot "scripts\deploy\smoke-check.sh"
    if (-not (Test-Path $smokeScript)) {
        Write-Warn "smoke-check.sh não encontrado — saltando smoke check"
        return
    }

    $args = @("--timeout", $SmokeTimeout)
    if ($ApiUrl)      { $args += @("--api-url",      $ApiUrl)      }
    if ($FrontendUrl) { $args += @("--frontend-url", $FrontendUrl) }

    if (-not $DryRun) {
        bash $smokeScript @args
        if ($LASTEXITCODE -ne 0) { throw "Smoke check falhou" }
    } else {
        Write-Host "[DRY-RUN] bash $smokeScript $($args -join ' ')" -ForegroundColor Yellow
    }

    Write-OK "Smoke check passou"
}

# ── Step 6: Rollback ─────────────────────────────────────────────────────────
function Invoke-Rollback {
    if ($SkipRollback) {
        Write-Warn "Rollback desactivado (--SkipRollback)"
        return
    }
    if ([string]::IsNullOrWhiteSpace($PreviousTag)) {
        Write-Err "Rollback impossível: tag anterior não conhecida"
        return
    }

    Write-Warn "Iniciando rollback para tag: $PreviousTag"

    $rollbackScript = Join-Path $RepoRoot "scripts\deploy\rollback.sh"
    if (Test-Path $rollbackScript) {
        if (-not $DryRun) {
            bash $rollbackScript --tag $PreviousTag --registry $Registry --skip-health
        } else {
            Write-Host "[DRY-RUN] bash $rollbackScript --tag $PreviousTag --registry $Registry --skip-health" -ForegroundColor Yellow
        }
    } else {
        $env:NEXTRACEONE_IMAGE_TAG = $PreviousTag
        $env:NEXTRACEONE_REGISTRY  = $Registry
        Invoke-Deploy "docker" @("compose", "-f", $ComposeFile, "up", "-d", "--remove-orphans")
    }

    Write-Warn "Rollback concluído para tag: $PreviousTag"
}

# ── Main ─────────────────────────────────────────────────────────────────────
Write-Host "`n╔════════════════════════════════════════════╗" -ForegroundColor Green
Write-Host "║  NexTraceOne — Deploy Automático (PS)      ║" -ForegroundColor Green
Write-Host "╚════════════════════════════════════════════╝" -ForegroundColor Green
Write-Info "Tag: $Tag | Registo: $Registry | Env: $Env"
if ($DryRun) { Write-Warn "MODO DRY-RUN ACTIVO — nenhum comando será executado" }

$DeployStart = Get-Date

Get-PreviousTag

try {
    Invoke-PullImages

    if (-not $SkipMigration) {
        Invoke-ApplyMigrations
    } else {
        Write-Warn "Migrations ignoradas (--SkipMigration)"
    }

    Invoke-RecreateContainers

    if (-not $SkipSmoke) {
        Invoke-SmokeCheck
    } else {
        Write-Warn "Smoke check ignorado (--SkipSmoke)"
    }

    $Elapsed = [int]((Get-Date) - $DeployStart).TotalSeconds
    Write-Host "`n╔══════════════════════════════════════════════════╗" -ForegroundColor Green
    Write-Host "║  Deploy concluído com sucesso!                   ║" -ForegroundColor Green
    Write-Host "║  Tag: $Tag" -ForegroundColor Green
    Write-Host "║  Duração: ${Elapsed}s" -ForegroundColor Green
    Write-Host "╚══════════════════════════════════════════════════╝" -ForegroundColor Green

} catch {
    Write-Err "Deploy falhou: $_"
    Invoke-Rollback
    Write-Err "Deploy de $Tag FALHOU"
    exit 1
}
