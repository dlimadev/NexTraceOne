# ============================================================================
# NexTraceOne - Recreate Database + Seed with Mock Data
#
# 1. Drops and recreates the PostgreSQL database `nextraceone`
# 2. Applies all EF Core migrations for all 27 DbContexts (7 waves)
# 3. Applies SQL seed scripts for development & functional testing
#
# AFTER running this script, start the API once so it can apply
# programmatic seeds (config definitions, feature flags, authorization).
#
# Usage:
#   .\scripts\db\recreate-and-seed.ps1
#   .\scripts\db\recreate-and-seed.ps1 -DbHost localhost -DbPort 5432 -DbUser postgres -DbPassword secret
# ============================================================================

[CmdletBinding()]
param(
    [string]$DbHost = "localhost",
    [int]$DbPort = 5432,
    [string]$DbUser = "postgres",
    [string]$DbPassword = "ouro18",
    [string]$DbName = "nextraceone",
    [string]$ConnString = $null
)

$ErrorActionPreference = "Stop"

$RepoRoot = Resolve-Path "$PSScriptRoot\..\.."
$ApiHostProject = Join-Path $RepoRoot "src\platform\NexTraceOne.ApiHost\NexTraceOne.ApiHost.csproj"

function Write-Info    { param($msg) Write-Host "[INFO]  $msg" -ForegroundColor Cyan }
function Write-Success { param($msg) Write-Host "[OK]    $msg" -ForegroundColor Green }
function Write-Warn    { param($msg) Write-Host "[WARN]  $msg" -ForegroundColor Yellow }
function Write-Fail    { param($msg) Write-Host "[ERROR] $msg" -ForegroundColor Red }

# -- Connection strings --------------------------------------------------------
if ($ConnString) {
    $MasterConn = $ConnString
    $AppConn = $ConnString
} else {
    $passSegment = if ($DbPassword) { "Password=$DbPassword;" } else { "" }
    $MasterConn = "Host=$DbHost;Port=$DbPort;Database=postgres;Username=$DbUser;${passSegment}"
    $AppConn = "Host=$DbHost;Port=$DbPort;Database=$DbName;Username=$DbUser;${passSegment}"
}

# -- Check prerequisites ------------------------------------------------------
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Fail "dotnet SDK not found. Install .NET SDK 10."
    exit 1
}

$efInstalled = dotnet tool list --global 2>$null | Select-String "dotnet-ef"
if (-not $efInstalled) {
    Write-Info "Installing dotnet-ef tool..."
    dotnet tool install --global dotnet-ef --version 10.0.5
}

if (-not (Get-Command psql -ErrorAction SilentlyContinue)) {
    Write-Fail "psql not found in PATH. Install PostgreSQL client or add to PATH."
    exit 1
}

# -- Step 1: Drop & Recreate database -----------------------------------------
Write-Info "Dropping and recreating database '$DbName'..."
try {
    $env:PGPASSWORD = $DbPassword
    $termSql = "SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = '$DbName' AND pid <> pg_backend_pid();"
    $dropSql = "DROP DATABASE IF EXISTS ""$DbName"";"
    $createSql = "CREATE DATABASE ""$DbName"";"
    
    psql -h $DbHost -p $DbPort -U $DbUser -d postgres -c $termSql 2>$null | Out-Null
    psql -h $DbHost -p $DbPort -U $DbUser -d postgres -c $dropSql 2>$null | Out-Null
    psql -h $DbHost -p $DbPort -U $DbUser -d postgres -c $createSql 2>$null | Out-Null
    Remove-Item Env:\PGPASSWORD -ErrorAction SilentlyContinue
    Write-Success "Database '$DbName' recreated."
} catch {
    Write-Fail "Failed to recreate database: $_"
    exit 1
}

# -- Step 2: Apply EF Core migrations (all 27 DbContexts in 7 waves) ----------
$contexts = @(
    # Wave 1 - Foundation
    "NexTraceOne.Configuration.Infrastructure.Persistence.ConfigurationDbContext"
    "NexTraceOne.IdentityAccess.Infrastructure.Persistence.IdentityDbContext"
    # Wave 2 - Catalog
    "NexTraceOne.Catalog.Infrastructure.Graph.Persistence.CatalogGraphDbContext"
    "NexTraceOne.Catalog.Infrastructure.Portal.Persistence.DeveloperPortalDbContext"
    "NexTraceOne.Catalog.Infrastructure.Contracts.Persistence.ContractsDbContext"
    "NexTraceOne.Catalog.Infrastructure.DeveloperExperience.Persistence.DeveloperExperienceDbContext"
    "NexTraceOne.Catalog.Infrastructure.LegacyAssets.Persistence.LegacyAssetsDbContext"
    "NexTraceOne.Catalog.Infrastructure.Templates.Persistence.TemplatesDbContext"
    "NexTraceOne.Catalog.Infrastructure.DependencyGovernance.Persistence.DependencyGovernanceDbContext"
    # Wave 3 - Change Governance & Operational Intelligence
    "NexTraceOne.ChangeGovernance.Infrastructure.ChangeIntelligence.Persistence.ChangeIntelligenceDbContext"
    "NexTraceOne.ChangeGovernance.Infrastructure.RulesetGovernance.Persistence.RulesetGovernanceDbContext"
    "NexTraceOne.ChangeGovernance.Infrastructure.Workflow.Persistence.WorkflowDbContext"
    "NexTraceOne.ChangeGovernance.Infrastructure.Promotion.Persistence.PromotionDbContext"
    "NexTraceOne.OperationalIntelligence.Infrastructure.Incidents.Persistence.IncidentDbContext"
    "NexTraceOne.OperationalIntelligence.Infrastructure.Runtime.Persistence.RuntimeIntelligenceDbContext"
    "NexTraceOne.OperationalIntelligence.Infrastructure.Cost.Persistence.CostIntelligenceDbContext"
    "NexTraceOne.OperationalIntelligence.Infrastructure.Reliability.Persistence.ReliabilityDbContext"
    "NexTraceOne.OperationalIntelligence.Infrastructure.Automation.Persistence.AutomationDbContext"
    "NexTraceOne.OperationalIntelligence.Infrastructure.TelemetryStore.Persistence.TelemetryStoreDbContext"
    # Wave 4 - Audit & Governance
    "NexTraceOne.AuditCompliance.Infrastructure.Persistence.AuditDbContext"
    "NexTraceOne.Governance.Infrastructure.Persistence.GovernanceDbContext"
    # Wave 5 - Integrations & Product Analytics
    "NexTraceOne.Integrations.Infrastructure.Persistence.IntegrationsDbContext"
    "NexTraceOne.ProductAnalytics.Infrastructure.Persistence.ProductAnalyticsDbContext"
    # Wave 6 - Notifications & Knowledge
    "NexTraceOne.Notifications.Infrastructure.Persistence.NotificationsDbContext"
    "NexTraceOne.Knowledge.Infrastructure.Persistence.KnowledgeDbContext"
    # Wave 7 - AI
    "NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.AiGovernanceDbContext"
    "NexTraceOne.AIKnowledge.Infrastructure.ExternalAI.Persistence.ExternalAiDbContext"
    "NexTraceOne.AIKnowledge.Infrastructure.Orchestration.Persistence.AiOrchestrationDbContext"
)

Write-Info "Building project..."
dotnet build $ApiHostProject --no-restore -v quiet | Out-Null

$failed = @()
$success = @()

foreach ($ctx in $contexts) {
    $short = $ctx.Split('.')[-1]
    Write-Info "Applying migrations: $short"
    $ea = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    $output = dotnet ef database update --project $ApiHostProject --context $ctx --connection $AppConn --no-build 2>&1
    $exitCode = $LASTEXITCODE
    $ErrorActionPreference = $ea
    if ($exitCode -eq 0) {
        Write-Success "OK $short"
        $success += $short
    } else {
        Write-Warn "WARN $short exited with code $exitCode"
        $failed += $short
    }
}

Write-Host ""
Write-Info "Migrations: $($success.Count) succeeded, $($failed.Count) had issues (often harmless for shared-table contexts)."

# -- Step 3: Apply SQL seed scripts -------------------------------------------
$seedFiles = @(
    (Join-Path $RepoRoot "src\platform\NexTraceOne.ApiHost\SeedData\seed-identity.sql"),
    (Join-Path $RepoRoot "src\platform\NexTraceOne.ApiHost\SeedData\seed-catalog.sql"),
    (Join-Path $RepoRoot "src\platform\NexTraceOne.ApiHost\SeedData\seed-changegovernance.sql"),
    (Join-Path $RepoRoot "src\platform\NexTraceOne.ApiHost\SeedData\seed-audit.sql"),
    (Join-Path $RepoRoot "src\platform\NexTraceOne.ApiHost\SeedData\seed-incidents.sql"),
    (Join-Path $RepoRoot "src\platform\NexTraceOne.ApiHost\SeedData\seed-aiknowledge.sql"),
    (Join-Path $RepoRoot "src\platform\NexTraceOne.ApiHost\SeedData\seed-governance.sql"),
    (Join-Path $RepoRoot "src\platform\NexTraceOne.ApiHost\SeedData\seed-configuration.sql"),
    (Join-Path $RepoRoot "src\platform\NexTraceOne.ApiHost\SeedData\seed-integrations.sql"),
    (Join-Path $RepoRoot "src\platform\NexTraceOne.ApiHost\SeedData\seed-knowledge.sql"),
    (Join-Path $RepoRoot "src\platform\NexTraceOne.ApiHost\SeedData\seed-notifications.sql"),
    (Join-Path $RepoRoot "src\platform\NexTraceOne.ApiHost\SeedData\seed-productanalytics.sql"),
    (Join-Path $RepoRoot "db\seed\seed_functional_test.sql")
)

foreach ($file in $seedFiles) {
    if (-not (Test-Path $file)) {
        Write-Warn "Seed file not found: $file"
        continue
    }
    $name = Split-Path $file -Leaf
    Write-Info "Applying seed: $name"
    try {
        $env:PGPASSWORD = $DbPassword
        $output = psql -h $DbHost -p $DbPort -U $DbUser -d $DbName -f $file -v ON_ERROR_STOP=1 2>&1
        Remove-Item Env:\PGPASSWORD -ErrorAction SilentlyContinue
        if ($LASTEXITCODE -ne 0) {
            Write-Warn "WARN $name failed (exit $LASTEXITCODE). This may be due to schema drift in seed files."
            Write-Warn "   Output: $output"
        } else {
            Write-Success "OK $name"
        }
    } catch {
        Write-Warn "WARN $name failed: $_"
    }
}

# -- Summary ------------------------------------------------------------------
Write-Host ""
Write-Info "======================================================="
Write-Info "DATABASE RECREATION COMPLETE"
Write-Info "======================================================="
Write-Info "Database: $DbName"
Write-Info "Migrations applied: $($success.Count) contexts"
if ($failed.Count -gt 0) {
    Write-Warn "Contexts with issues: $($failed.Count) (usually shared-table contexts)"
}
Write-Info ""
Write-Info "IMPORTANT NEXT STEP:"
Write-Info "  Start the API once to apply programmatic seeds:"
Write-Info "    dotnet run --project '$ApiHostProject'"
Write-Info ""
Write-Info "  This will auto-seed:"
Write-Info "    - Configuration definitions"
Write-Info "    - Feature flag definitions"
Write-Info "    - Authorization data (roles/permissions from RolePermissionCatalog)"
Write-Info ""
Write-Info "  Then login with: admin@nextraceone.io / Admin@2026!"
Write-Info "======================================================="
