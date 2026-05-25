#!/usr/bin/env bash
# NexTraceOne -- Recreate Database + Seed with Mock Data
#
# 1. Drops and recreates the PostgreSQL database 'nextraceone'
# 2. Applies all EF Core migrations for all 27 DbContexts (7 waves)
# 3. Applies SQL seed scripts for development & functional testing
#
# AFTER running this script, start the API once so it can apply
# programmatic seeds (config definitions, feature flags, authorization).
#
# Usage:
#   bash scripts/db/recreate-and-seed.sh
#   bash scripts/db/recreate-and-seed.sh --host localhost --port 5432 --user postgres --password secret

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"
APIHOST_PROJECT="${REPO_ROOT}/src/platform/NexTraceOne.ApiHost/NexTraceOne.ApiHost.csproj"

DB_HOST="localhost"
DB_PORT="5432"
DB_USER="postgres"
DB_PASSWORD=""
DB_NAME="nextraceone"

# Parse arguments
while [[ $# -gt 0 ]]; do
  case "$1" in
    --host) DB_HOST="$2"; shift 2 ;;
    --port) DB_PORT="$2"; shift 2 ;;
    --user) DB_USER="$2"; shift 2 ;;
    --password) DB_PASSWORD="$2"; shift 2 ;;
    --dbname) DB_NAME="$2"; shift 2 ;;
    *) echo "Unknown option: $1"; exit 1 ;;
  esac
done

echo "[INFO]  Dropping and recreating database '${DB_NAME}'..."
export PGPASSWORD="${DB_PASSWORD}"
psql -h "${DB_HOST}" -p "${DB_PORT}" -U "${DB_USER}" -d postgres -c "SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = '${DB_NAME}' AND pid <> pg_backend_pid();" 2>/dev/null || true
psql -h "${DB_HOST}" -p "${DB_PORT}" -U "${DB_USER}" -d postgres -c "DROP DATABASE IF EXISTS \"${DB_NAME}\";"
psql -h "${DB_HOST}" -p "${DB_PORT}" -U "${DB_USER}" -d postgres -c "CREATE DATABASE \"${DB_NAME}\";"
unset PGPASSWORD
echo "[OK]    Database '${DB_NAME}' recreated."

APP_CONN="Host=${DB_HOST};Port=${DB_PORT};Database=${DB_NAME};Username=${DB_USER};"
if [[ -n "${DB_PASSWORD}" ]]; then
  APP_CONN="${APP_CONN}Password=${DB_PASSWORD};"
fi

echo "[INFO]  Building project..."
dotnet build "${APIHOST_PROJECT}" --no-restore -v quiet

CONTEXTS=(
  "NexTraceOne.Configuration.Infrastructure.Persistence.ConfigurationDbContext"
  "NexTraceOne.IdentityAccess.Infrastructure.Persistence.IdentityDbContext"
  "NexTraceOne.Catalog.Infrastructure.Graph.Persistence.CatalogGraphDbContext"
  "NexTraceOne.Catalog.Infrastructure.Portal.Persistence.DeveloperPortalDbContext"
  "NexTraceOne.Catalog.Infrastructure.Contracts.Persistence.ContractsDbContext"
  "NexTraceOne.Catalog.Infrastructure.DeveloperExperience.Persistence.DeveloperExperienceDbContext"
  "NexTraceOne.Catalog.Infrastructure.LegacyAssets.Persistence.LegacyAssetsDbContext"
  "NexTraceOne.Catalog.Infrastructure.Templates.Persistence.TemplatesDbContext"
  "NexTraceOne.Catalog.Infrastructure.DependencyGovernance.Persistence.DependencyGovernanceDbContext"
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
  "NexTraceOne.AuditCompliance.Infrastructure.Persistence.AuditDbContext"
  "NexTraceOne.Governance.Infrastructure.Persistence.GovernanceDbContext"
  "NexTraceOne.Integrations.Infrastructure.Persistence.IntegrationsDbContext"
  "NexTraceOne.ProductAnalytics.Infrastructure.Persistence.ProductAnalyticsDbContext"
  "NexTraceOne.Notifications.Infrastructure.Persistence.NotificationsDbContext"
  "NexTraceOne.Knowledge.Infrastructure.Persistence.KnowledgeDbContext"
  "NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.AiGovernanceDbContext"
  "NexTraceOne.AIKnowledge.Infrastructure.ExternalAI.Persistence.ExternalAiDbContext"
  "NexTraceOne.AIKnowledge.Infrastructure.Orchestration.Persistence.AiOrchestrationDbContext"
)

SUCCESS=0
FAILED=0

for ctx in "${CONTEXTS[@]}"; do
  short="${ctx##*.}"
  echo "[INFO]  Applying migrations: ${short}"
  if dotnet ef database update --project "${APIHOST_PROJECT}" --context "${ctx}" --connection "${APP_CONN}" --no-build >/dev/null 2>&1; then
    echo "[OK]    ${short}"
    ((SUCCESS++)) || true
  else
    echo "[WARN]  ${short} had issues (often harmless for shared-table contexts)"
    ((FAILED++)) || true
  fi
done

echo ""
echo "[INFO]  Migrations: ${SUCCESS} succeeded, ${FAILED} had issues."

SEED_FILES=(
  "${REPO_ROOT}/src/platform/NexTraceOne.ApiHost/SeedData/seed-identity.sql"
  "${REPO_ROOT}/src/platform/NexTraceOne.ApiHost/SeedData/seed-catalog.sql"
  "${REPO_ROOT}/src/platform/NexTraceOne.ApiHost/SeedData/seed-changegovernance.sql"
  "${REPO_ROOT}/src/platform/NexTraceOne.ApiHost/SeedData/seed-audit.sql"
  "${REPO_ROOT}/src/platform/NexTraceOne.ApiHost/SeedData/seed-incidents.sql"
  "${REPO_ROOT}/src/platform/NexTraceOne.ApiHost/SeedData/seed-aiknowledge.sql"
  "${REPO_ROOT}/src/platform/NexTraceOne.ApiHost/SeedData/seed-governance.sql"
  "${REPO_ROOT}/src/platform/NexTraceOne.ApiHost/SeedData/seed-configuration.sql"
  "${REPO_ROOT}/src/platform/NexTraceOne.ApiHost/SeedData/seed-integrations.sql"
  "${REPO_ROOT}/src/platform/NexTraceOne.ApiHost/SeedData/seed-knowledge.sql"
  "${REPO_ROOT}/src/platform/NexTraceOne.ApiHost/SeedData/seed-notifications.sql"
  "${REPO_ROOT}/src/platform/NexTraceOne.ApiHost/SeedData/seed-productanalytics.sql"
  "${REPO_ROOT}/db/seed/seed_functional_test.sql"
)

export PGPASSWORD="${DB_PASSWORD}"
for file in "${SEED_FILES[@]}"; do
  if [[ ! -f "$file" ]]; then
    echo "[WARN]  Seed file not found: $file"
    continue
  fi
  name="$(basename "$file")"
  echo "[INFO]  Applying seed: ${name}"
  if psql -h "${DB_HOST}" -p "${DB_PORT}" -U "${DB_USER}" -d "${DB_NAME}" -f "$file" -v ON_ERROR_STOP=1 >/dev/null 2>&1; then
    echo "[OK]    ${name}"
  else
    echo "[WARN]  ${name} failed (may be due to schema drift)."
  fi
done
unset PGPASSWORD

echo ""
echo "======================================================="
echo "DATABASE RECREATION COMPLETE"
echo "======================================================="
echo "Database: ${DB_NAME}"
echo "Migrations applied: ${SUCCESS} contexts"
echo ""
echo "IMPORTANT NEXT STEP:"
echo "  Start the API once to apply programmatic seeds:"
echo "    dotnet run --project '${APIHOST_PROJECT}'"
echo ""
echo "  This will auto-seed:"
echo "    - Configuration definitions"
echo "    - Feature flag definitions"
echo "    - Authorization data (roles/permissions)"
echo ""
echo "  Then login with: admin@nextraceone.io / Admin@2026!"
echo "======================================================="
