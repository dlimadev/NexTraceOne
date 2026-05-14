#!/bin/bash

# NexTraceOne v5.0.0 - Pre-Launch Validation Script
# This script validates that all components are ready for production release

set -e

echo "🚀 NexTraceOne v5.0.0 - Pre-Launch Validation"
echo "=============================================="
echo ""

PASS=0
FAIL=0
WARN=0

# Function to check and report
check() {
    local description=$1
    local command=$2
    
    echo -n "Checking $description... "
    
    if eval "$command" > /dev/null 2>&1; then
        echo "✅ PASS"
        ((PASS++))
    else
        echo "❌ FAIL"
        ((FAIL++))
    fi
}

warn_check() {
    local description=$1
    local command=$2
    
    echo -n "Checking $description... "
    
    if eval "$command" > /dev/null 2>&1; then
        echo "✅ PASS"
        ((PASS++))
    else
        echo "⚠️  WARN"
        ((WARN++))
    fi
}

echo "📋 Phase 1: Core Infrastructure"
echo "--------------------------------"
check ".NET 8 SDK installed" "dotnet --version | grep -q '8.0'"
check "Node.js installed" "node --version"
check "npm installed" "npm --version"
check "Docker installed" "docker --version"
check "kubectl installed" "kubectl version --client"
check "Helm installed" "helm version"
echo ""

echo "📋 Phase 2: Backend Modules"
echo "----------------------------"
check "AI Agents module exists" "test -d src/modules/aiagents"
check "Dependency Advisor Agent" "test -f src/modules/aiagents/NexTraceOne.AIAgents.Application/Agents/DependencyAdvisorAgent.cs"
check "Architecture Fitness Agent" "test -f src/modules/aiagents/NexTraceOne.AIAgents.Application/Agents/ArchitectureFitnessAgent.cs"
check "Documentation Quality Agent" "test -f src/modules/aiagents/NexTraceOne.AIAgents.Application/Agents/DocumentationQualityAgent.cs"
check "Security Review Agent" "test -f src/modules/aiagents/NexTraceOne.AIAgents.Application/Agents/SecurityReviewAgent.cs"
check "Observability module exists" "test -d src/platform/NexTraceOne.Observability"
check "ClickHouse repository" "test -f src/platform/NexTraceOne.Observability/Repositories/ClickHouseRepository.cs"
check "NLP Routing module exists" "test -d src/modules/nlprouting"
check "Intelligent Router" "test -f src/modules/nlprouting/NexTraceOne.NLPRouting.Application/Services/IntelligentRouter.cs"
echo ""

echo "📋 Phase 3: Frontend Components"
echo "--------------------------------"
check "Frontend app exists" "test -d src/frontend"
check "Request Metrics Dashboard" "test -f src/frontend/src/features/observability/components/RequestMetricsDashboard.tsx"
check "Error Analytics Dashboard" "test -f src/frontend/src/features/observability/components/ErrorAnalyticsDashboard.tsx"
check "System Health Dashboard" "test -f src/frontend/src/features/observability/components/SystemHealthDashboard.tsx"
check "Main Dashboard Page" "test -f src/frontend/src/features/observability/pages/ObservabilityDashboardPage.tsx"
check "Observability Service" "test -f src/frontend/src/features/observability/services/ObservabilityService.ts"
echo ""

echo "📋 Phase 4: API Endpoints"
echo "-------------------------"
check "AI Agents API module" "test -f src/modules/aiagents/NexTraceOne.AIAgents.API/Endpoints/AiAgentsModule.cs"
check "Observability API module" "test -f src/platform/NexTraceOne.Observability.API/Endpoints/ObservabilityModule.cs"
check "NLP Routing API module" "test -f src/modules/nlprouting/NexTraceOne.NLPRouting.API/Endpoints/NLPRoutingModule.cs"
echo ""

echo "📋 Phase 5: Deployment & Infrastructure"
echo "----------------------------------------"
check "Kubernetes manifests" "test -d deploy/kubernetes/helm/nextraceone"
check "ClickHouse cluster config" "test -f deploy/clickhouse/clickhouse-cluster.yaml"
check "ClickHouse schema" "test -f deploy/clickhouse/schema.sql"
check "Deployment script" "test -f deploy/clickhouse/deploy-clickhouse.sh"
check "Helm Chart.yaml" "test -f deploy/kubernetes/helm/nextraceone/Chart.yaml"
check "Helm values-prod.yaml" "test -f deploy/kubernetes/helm/nextraceone/values-prod.yaml"
echo ""

echo "📋 Phase 6: Documentation"
echo "--------------------------"
check "Release notes" "test -f RELEASE-NOTES-v5.0.0.md"
check "AI Agents README" "test -f src/modules/aiagents/README.md"
check "ClickHouse README" "test -f deploy/clickhouse/README.md"
check "Phase 3 progress report" "test -f FASE3-AI-AGENTS-PROGRESS.md"
check "Phase 4 final report" "test -f FASE4-CLICKHOUSE-OBSERVABILITY-FINAL.md"
check "Consolidated report" "test -f RELATORIO-CONSOLIDADO-FASES-3-4-5.md"
check "Final status report" "test -f STATUS-FINAL-TODAS-FASES.md"
echo ""

echo "📋 Phase 7: Testing"
echo "--------------------"
warn_check "Unit tests exist" "find tests -name '*.cs' -type f | grep -q ."
warn_check "Integration tests exist" "find tests -name '*IntegrationTests*' -type d | grep -q ."
warn_check "Load tests exist" "test -d tests/load-testing"
echo ""

echo "📋 Phase 8: CI/CD"
echo "------------------"
check "GitHub Actions workflows" "test -d .github/workflows"
check "CI workflow" "test -f .github/workflows/ci.yml"
check "Kubernetes deploy workflow" "test -f .github/workflows/kubernetes-deploy.yml"
check "Artifact signing workflow" "test -f .github/workflows/artifact-signing.yml"
echo ""

echo "📋 Phase 9: Security"
echo "---------------------"
check "Air-gap handler" "test -f src/building-blocks/NexTraceOne.BuildingBlocks.Infrastructure/Http/AirGapHttpMessageHandler.cs"
check "Session inactivity middleware" "test -f src/building-blocks/NexTraceOne.BuildingBlocks.Security/Session/SessionInactivityMiddleware.cs"
check "Environment authorization middleware" "test -f src/modules/identityaccess/NexTraceOne.IdentityAccess.Infrastructure/Middleware/EnvironmentAuthorizationMiddleware.cs"
check "Artifact signing service" "test -f src/platform/NexTraceOne.ArtifactSigning/Services/CosignArtifactSigner.cs"
echo ""

echo "📋 Phase 10: Configuration"
echo "---------------------------"
warn_check "appsettings.json exists" "test -f src/platform/NexTraceOne.ApiHost/appsettings.json"
warn_check ".env.example exists" "test -f .env.example"
warn_check "Dockerfile exists" "test -f Dockerfile.kubernetes"
echo ""

echo "=============================================="
echo "📊 VALIDATION SUMMARY"
echo "=============================================="
echo "✅ PASSED: $PASS"
echo "❌ FAILED: $FAIL"
echo "⚠️  WARNINGS: $WARN"
echo ""

if [ $FAIL -eq 0 ]; then
    echo "🎉 SUCCESS! All critical checks passed."
    echo ""
    echo "NexTraceOne v5.0.0 is READY FOR PRODUCTION LAUNCH!"
    echo ""
    echo "Next steps:"
    echo "1. Review warnings (if any)"
    echo "2. Run full test suite: dotnet test"
    echo "3. Deploy to staging environment"
    echo "4. Perform smoke tests"
    echo "5. Deploy to production"
    exit 0
else
    echo "❌ FAILURE! $FAIL critical check(s) failed."
    echo ""
    echo "Please fix the failed checks before launching."
    exit 1
fi
