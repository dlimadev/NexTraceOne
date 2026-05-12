#!/bin/bash
# ============================================================================
# NEXTRACEONE - Production Readiness Validation Script
# ============================================================================
# Este script valida automaticamente os problemas identificados na análise
# forense e gera um relatório de conformidade.
#
# Uso: bash scripts/validate-production-readiness.sh
# ============================================================================

set -e  # Exit on error

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo -e "${BLUE}╔═══════════════════════════════════════════════════════════╗${NC}"
echo -e "${BLUE}║   NexTraceOne - Production Readiness Validator          ║${NC}"
echo -e "${BLUE}║   $(date '+%Y-%m-%d %H:%M:%S')                              ║${NC}"
echo -e "${BLUE}╚═══════════════════════════════════════════════════════════╝${NC}"
echo ""

# Counters
PASS_COUNT=0
FAIL_COUNT=0
WARN_COUNT=0

# Function to check and report
check_pass() {
    echo -e "${GREEN}✓ PASS:${NC} $1"
    ((PASS_COUNT++))
}

check_fail() {
    echo -e "${RED}✗ FAIL:${NC} $1"
    ((FAIL_COUNT++))
}

check_warn() {
    echo -e "${YELLOW}⚠ WARN:${NC} $1"
    ((WARN_COUNT++))
}

# ============================================================================
# SECTION 1: Build & Compilation
# ============================================================================
echo -e "${BLUE}[1/8] Validating Build & Compilation...${NC}"

# Check for compilation errors
if dotnet build NexTraceOne.sln --configuration Release > /tmp/build.log 2>&1; then
    check_pass "Solution builds successfully"
else
    check_fail "Solution has compilation errors (see /tmp/build.log)"
fi

# Check for warnings
WARNING_COUNT=$(grep -c "warning CS" /tmp/build.log 2>/dev/null || echo "0")
if [ "$WARNING_COUNT" -eq 0 ]; then
    check_pass "No compilation warnings"
else
    check_warn "$WARNING_COUNT compilation warnings found"
fi

# Check for TODOs in production code
TODO_COUNT=$(grep -r "// TODO:" src/ --include="*.cs" 2>/dev/null | wc -l || echo "0")
if [ "$TODO_COUNT" -eq 0 ]; then
    check_pass "No TODOs in production code"
else
    check_warn "$TODO_COUNT TODOs found in production code"
fi

echo ""

# ============================================================================
# SECTION 2: Tests
# ============================================================================
echo -e "${BLUE}[2/8] Validating Tests...${NC}"

# Run unit tests
echo "Running unit tests..."
if dotnet test NexTraceOne.sln --configuration Release --filter "FullyQualifiedName~Tests&FullyQualifiedName!~Integration" --no-build > /tmp/unit-tests.log 2>&1; then
    UNIT_PASS=$(grep -oP 'Aprovado:\s+\K\d+' /tmp/unit-tests.log || echo "0")
    UNIT_FAIL=$(grep -oP 'Com falha:\s+\K\d+' /tmp/unit-tests.log || echo "0")
    check_pass "Unit tests: $UNIT_PASS passed, $UNIT_FAIL failed"
else
    check_fail "Unit tests execution failed"
fi

# Run integration tests
echo "Running integration tests..."
if dotnet test tests/integration/NexTraceOne.IntegrationTests --configuration Release --no-build > /tmp/integration-tests.log 2>&1; then
    INT_PASS=$(grep -oP 'Aprovado:\s+\K\d+' /tmp/integration-tests.log || echo "0")
    INT_FAIL=$(grep -oP 'Com falha:\s+\K\d+' /tmp/integration-tests.log || echo "0")
    
    if [ "$INT_FAIL" -eq 0 ]; then
        check_pass "Integration tests: $INT_PASS passed, 0 failed"
    else
        check_fail "Integration tests: $INT_PASS passed, $INT_FAIL failed"
    fi
else
    check_fail "Integration tests execution failed"
fi

echo ""

# ============================================================================
# SECTION 3: Security Configuration
# ============================================================================
echo -e "${BLUE}[3/8] Validating Security Configuration...${NC}"

# Check for placeholder passwords in appsettings.json
PLACEHOLDER_COUNT=$(grep -c "REPLACE_VIA_ENV" src/platform/NexTraceOne.ApiHost/appsettings.json 2>/dev/null || echo "0")
if [ "$PLACEHOLDER_COUNT" -eq 0 ]; then
    check_pass "No placeholder passwords in appsettings.json"
else
    check_warn "$PLACEHOLDER_COUNT placeholder passwords found (should be set via env vars)"
fi

# Check JWT Secret configuration
if grep -q '"Secret"' src/platform/NexTraceOne.ApiHost/appsettings.json 2>/dev/null; then
    JWT_SECRET=$(grep '"Secret"' src/platform/NexTraceOne.ApiHost/appsettings.json | grep -v "REPLACE_VIA_ENV" || echo "")
    if [ -n "$JWT_SECRET" ]; then
        check_pass "JWT Secret is configured"
    else
        check_fail "JWT Secret contains placeholder or is missing"
    fi
else
    check_warn "JWT Secret not found in appsettings.json (may be set via env var)"
fi

# Check CORS configuration
if grep -q '"AllowedOrigins"' src/platform/NexTraceOne.ApiHost/appsettings.json 2>/dev/null; then
    if grep -q '"*"' src/platform/NexTraceOne.ApiHost/appsettings.json 2>/dev/null; then
        check_fail "CORS allows all origins (*) - security risk in production"
    else
        check_pass "CORS configured with specific origins"
    fi
else
    check_warn "CORS configuration not found in appsettings.json"
fi

echo ""

# ============================================================================
# SECTION 4: Health Checks
# ============================================================================
echo -e "${BLUE}[4/8] Validating Health Checks...${NC}"

# Check for health check registrations
HEALTH_CHECK_COUNT=$(grep -r "AddHealthCheck\|AddCheck<" src/platform/NexTraceOne.ApiHost/Program.cs 2>/dev/null | wc -l || echo "0")
if [ "$HEALTH_CHECK_COUNT" -gt 5 ]; then
    check_pass "$HEALTH_CHECK_COUNT health checks registered"
else
    check_warn "Only $HEALTH_CHECK_COUNT health checks found (expected more)"
fi

# Check for incomplete health checks (TODOs)
HEALTH_TODO=$(grep -c "TODO.*HealthCheck" src/platform/NexTraceOne.BackgroundWorkers/Jobs/*.cs 2>/dev/null || echo "0")
if [ "$HEALTH_TODO" -eq 0 ]; then
    check_pass "No incomplete health check implementations"
else
    check_fail "$HEALTH_TODO health checks have TODOs/incomplete implementations"
fi

echo ""

# ============================================================================
# SECTION 5: Database Migrations
# ============================================================================
echo -e "${BLUE}[5/8] Validating Database Migrations...${NC}"

# Check for pending migrations
if command -v dotnet-ef &> /dev/null; then
    PENDING_MIGRATIONS=$(dotnet ef migrations list --project src/platform/NexTraceOne.ApiHost 2>/dev/null | grep -c "Pending" || echo "0")
    if [ "$PENDING_MIGRATIONS" -eq 0 ]; then
        check_pass "No pending database migrations"
    else
        check_warn "$PENDING_MIGRATIONS pending migrations found"
    fi
else
    check_warn "dotnet-ef tool not installed - skipping migration check"
fi

echo ""

# ============================================================================
# SECTION 6: Docker & Infrastructure
# ============================================================================
echo -e "${BLUE}[6/8] Validating Docker & Infrastructure...${NC}"

# Check Docker availability
if command -v docker &> /dev/null && docker info > /dev/null 2>&1; then
    check_pass "Docker is available and running"
else
    check_warn "Docker not available - some integration tests may fail"
fi

# Check docker-compose files
if [ -f "docker-compose.yml" ] && [ -f "docker-compose.production.yml" ]; then
    check_pass "Docker Compose files present"
else
    check_fail "Missing Docker Compose files"
fi

echo ""

# ============================================================================
# SECTION 7: Code Quality
# ============================================================================
echo -e "${BLUE}[7/8] Validating Code Quality...${NC}"

# Check for NotImplementedException in production code
NOT_IMPL=$(grep -r "throw new NotImplementedException" src/ --include="*.cs" 2>/dev/null | wc -l || echo "0")
if [ "$NOT_IMPL" -eq 0 ]; then
    check_pass "No NotImplementedException in production code"
else
    check_fail "$NOT_IMPL NotImplementedException found in production code"
fi

# Check for console.writeline in production (should use logging)
CONSOLE_WRITE=$(grep -r "Console.WriteLine\|Console.Error" src/ --include="*.cs" 2>/dev/null | grep -v "// " | wc -l || echo "0")
if [ "$CONSOLE_WRITE" -eq 0 ]; then
    check_pass "No Console.WriteLine in production code"
else
    check_warn "$CONSOLE_WRITE Console.WriteLine calls found (should use ILogger)"
fi

echo ""

# ============================================================================
# SECTION 8: Documentation
# ============================================================================
echo -e "${BLUE}[8/8] Validating Documentation...${NC}"

# Check for README
if [ -f "README.md" ]; then
    check_pass "README.md exists"
else
    check_fail "README.md missing"
fi

# Check for CLAUDE.md
if [ -f "CLAUDE.md" ]; then
    check_pass "CLAUDE.md exists"
else
    check_warn "CLAUDE.md missing"
fi

# Check for .env.example
if [ -f ".env.example" ]; then
    check_pass ".env.example exists"
else
    check_fail ".env.example missing"
fi

echo ""

# ============================================================================
# FINAL SUMMARY
# ============================================================================
echo -e "${BLUE}╔═══════════════════════════════════════════════════════════╗${NC}"
echo -e "${BLUE}║                    VALIDATION SUMMARY                    ║${NC}"
echo -e "${BLUE}╚═══════════════════════════════════════════════════════════╝${NC}"
echo ""
echo -e "${GREEN}Passed:${NC}   $PASS_COUNT"
echo -e "${RED}Failed:${NC}   $FAIL_COUNT"
echo -e "${YELLOW}Warnings:${NC} $WARN_COUNT"
echo ""

TOTAL=$((PASS_COUNT + FAIL_COUNT + WARN_COUNT))
if [ "$TOTAL" -gt 0 ]; then
    SCORE=$((PASS_COUNT * 100 / TOTAL))
else
    SCORE=0
fi

echo -e "Production Readiness Score: ${SCORE}%"
echo ""

if [ "$FAIL_COUNT" -eq 0 ] && [ "$SCORE" -ge 95 ]; then
    echo -e "${GREEN}🎉 EXCELLENT! Project is ready for production!${NC}"
    exit 0
elif [ "$FAIL_COUNT" -le 2 ] && [ "$SCORE" -ge 85 ]; then
    echo -e "${YELLOW}⚠️  GOOD! Minor issues to fix before production.${NC}"
    exit 1
else
    echo -e "${RED}❌ NOT READY! Critical issues must be resolved.${NC}"
    exit 2
fi
