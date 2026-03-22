# ═══════════════════════════════════════════════════════════════════════════════
# NexTraceOne — Apply Database Migrations (PowerShell)
#
# Aplica migrações de todos os DbContexts nos 4 bancos lógicos.
# Uso seguro em CI/CD Windows, Staging e procedimentos de Production.
#
# Uso:
#   .\scripts\db\apply-migrations.ps1 -Env Staging
#   .\scripts\db\apply-migrations.ps1 -DryRun
#   .\scripts\db\apply-migrations.ps1 -Env Production
# ═══════════════════════════════════════════════════════════════════════════════

[CmdletBinding()]
param(
    [Parameter()]
    [ValidateSet("Development", "Staging", "Production")]
    [string]$Env = $env:MIGRATION_ENV ?? "Staging",

    [Parameter()]
    [switch]$DryRun,

    [Parameter()]
    [string]$ConnIdentity  = $env:CONN_IDENTITY,

    [Parameter()]
    [string]$ConnCatalog   = $env:CONN_CATALOG,

    [Parameter()]
    [string]$ConnOperations = $env:CONN_OPERATIONS,

    [Parameter()]
    [string]$ConnAi        = $env:CONN_AI
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$RepoRoot = Resolve-Path "$PSScriptRoot\..\.."
$ApiHostProject = Join-Path $RepoRoot "src\platform\NexTraceOne.ApiHost\NexTraceOne.ApiHost.csproj"

function Write-Info    { param($msg) Write-Host "[INFO]  $msg" -ForegroundColor Cyan }
function Write-Success { param($msg) Write-Host "[OK]    $msg" -ForegroundColor Green }
function Write-Warn    { param($msg) Write-Host "[WARN]  $msg" -ForegroundColor Yellow }
function Write-Fail    { param($msg) Write-Host "[ERROR] $msg" -ForegroundColor Red }

# ── Production guard ──────────────────────────────────────────────────────────
if ($Env -eq "Production") {
    Write-Warn "══════════════════════════════════════════════════════"
    Write-Warn "  ATENÇÃO: Aplicando migrations em PRODUCTION"
    Write-Warn "  Certifique-se de ter backup recente antes de prosseguir."
    Write-Warn "══════════════════════════════════════════════════════"
    $confirm = Read-Host "Digite 'confirmo' para prosseguir"
    if ($confirm -ne "confirmo") {
        Write-Fail "Operação cancelada pelo utilizador."
        exit 1
    }
}

# ── Check prerequisites ───────────────────────────────────────────────────────
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Fail "dotnet SDK não encontrado. Instale .NET SDK 10."
    exit 1
}

$efInstalled = dotnet tool list --global 2>$null | Select-String "dotnet-ef"
if (-not $efInstalled) {
    $efVersion = "10.0.5"
    Write-Info "Instalando dotnet-ef tool (versão $efVersion)..."
    dotnet tool install --global dotnet-ef --version $efVersion
}

# ── DbContext → DB mapping ─────────────────────────────────────────────────────
$contexts = @(
    @{ Name = "IdentityDbContext";           Full = "NexTraceOne.IdentityAccess.Infrastructure.Persistence.IdentityDbContext";                                      Conn = $ConnIdentity   }
    @{ Name = "AuditDbContext";              Full = "NexTraceOne.AuditCompliance.Infrastructure.Persistence.AuditDbContext";                                         Conn = $ConnIdentity   }
    @{ Name = "CatalogGraphDbContext";       Full = "NexTraceOne.Catalog.Infrastructure.Graph.Persistence.CatalogGraphDbContext";                                    Conn = $ConnCatalog    }
    @{ Name = "ContractsDbContext";          Full = "NexTraceOne.Catalog.Infrastructure.Contracts.Persistence.ContractsDbContext";                                   Conn = $ConnCatalog    }
    @{ Name = "DeveloperPortalDbContext";    Full = "NexTraceOne.Catalog.Infrastructure.Portal.Persistence.DeveloperPortalDbContext";                                Conn = $ConnCatalog    }
    @{ Name = "ChangeIntelligenceDbContext"; Full = "NexTraceOne.ChangeGovernance.Infrastructure.ChangeIntelligence.Persistence.ChangeIntelligenceDbContext";        Conn = $ConnOperations }
    @{ Name = "RulesetGovernanceDbContext";  Full = "NexTraceOne.ChangeGovernance.Infrastructure.RulesetGovernance.Persistence.RulesetGovernanceDbContext";          Conn = $ConnOperations }
    @{ Name = "WorkflowDbContext";           Full = "NexTraceOne.ChangeGovernance.Infrastructure.Workflow.Persistence.WorkflowDbContext";                            Conn = $ConnOperations }
    @{ Name = "PromotionDbContext";          Full = "NexTraceOne.ChangeGovernance.Infrastructure.Promotion.Persistence.PromotionDbContext";                          Conn = $ConnOperations }
    @{ Name = "GovernanceDbContext";         Full = "NexTraceOne.Governance.Infrastructure.Persistence.GovernanceDbContext";                                         Conn = $ConnOperations }
    @{ Name = "IncidentDbContext";           Full = "NexTraceOne.OperationalIntelligence.Infrastructure.Incidents.Persistence.IncidentDbContext";                    Conn = $ConnOperations }
    @{ Name = "RuntimeIntelligenceDbContext";Full = "NexTraceOne.OperationalIntelligence.Infrastructure.Runtime.Persistence.RuntimeIntelligenceDbContext";           Conn = $ConnOperations }
    @{ Name = "CostIntelligenceDbContext";   Full = "NexTraceOne.OperationalIntelligence.Infrastructure.Cost.Persistence.CostIntelligenceDbContext";                 Conn = $ConnOperations }
    @{ Name = "AiGovernanceDbContext";       Full = "NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.AiGovernanceDbContext";                           Conn = $ConnAi         }
    @{ Name = "ExternalAiDbContext";         Full = "NexTraceOne.AIKnowledge.Infrastructure.ExternalAI.Persistence.ExternalAiDbContext";                             Conn = $ConnAi         }
    @{ Name = "AiOrchestrationDbContext";    Full = "NexTraceOne.AIKnowledge.Infrastructure.Orchestration.Persistence.AiOrchestrationDbContext";                     Conn = $ConnAi         }
)

$failed  = @()
$success = @()

Write-Info "Iniciando migrations — Ambiente: $Env | Dry-run: $DryRun"
Write-Info "Projeto: $ApiHostProject"
Write-Host ""

foreach ($ctx in $contexts) {
    if ([string]::IsNullOrWhiteSpace($ctx.Conn)) {
        Write-Warn "Skipping $($ctx.Name) — connection string não definida"
        continue
    }

    if ($DryRun) {
        Write-Info "Listando migrações pendentes: $($ctx.Name)"
        dotnet ef migrations list `
            --project $ApiHostProject `
            --context $ctx.Full `
            --connection $ctx.Conn `
            --no-build 2>$null
    }
    else {
        Write-Info "Aplicando migrations: $($ctx.Name)"
        dotnet ef database update `
            --project $ApiHostProject `
            --context $ctx.Full `
            --connection $ctx.Conn `
            --no-build

        if ($LASTEXITCODE -eq 0) {
            Write-Success "✓ $($ctx.Name)"
            $success += $ctx.Name
        }
        else {
            Write-Fail "✗ $($ctx.Name) — FALHOU"
            $failed += $ctx.Name
        }
    }
}

# ── Summary ───────────────────────────────────────────────────────────────────
Write-Host ""
Write-Info "═══════════════════════════════════════"
Write-Info "RESULTADO DAS MIGRATIONS"
Write-Info "Ambiente: $Env"
Write-Info "Bem-sucedidos: $($success.Count)"
Write-Info "Falharam: $($failed.Count)"
Write-Info "═══════════════════════════════════════"

if ($failed.Count -gt 0) {
    Write-Fail "Contexts com falha:"
    $failed | ForEach-Object { Write-Fail "  - $_" }
    exit 1
}

Write-Success "Todas as migrations aplicadas com sucesso."
