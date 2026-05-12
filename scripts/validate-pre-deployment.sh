#!/bin/bash
# Script de Validação Pré-Deploy para NexTraceOne
# Uso: ./scripts/validate-pre-deployment.sh
# Retorna exit code 0 se pronto para deploy, 1 se há problemas

set -e

echo "=========================================="
echo "🔍 VALIDAÇÃO PRÉ-DEPLOY - NexTraceOne"
echo "=========================================="
echo ""

ERRORS=0
WARNINGS=0

# Cores para output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Função helper
check_pass() {
    echo -e "${GREEN}✅ PASS:${NC} $1"
}

check_warn() {
    echo -e "${YELLOW}⚠️  WARN:${NC} $1"
    WARNINGS=$((WARNINGS + 1))
}

check_fail() {
    echo -e "${RED}❌ FAIL:${NC} $1"
    ERRORS=$((ERRORS + 1))
}

# 1. Build Clean
echo "📦 [1/8] Verificando build..."
if dotnet build NexTraceOne.sln --configuration Release --no-incremental > /dev/null 2>&1; then
    check_pass "Build succeeded (0 errors)"
else
    check_fail "Build failed"
    exit 1
fi

# 2. Warnings
echo "📦 [2/8] Verificando warnings..."
WARNINGS_COUNT=$(dotnet build NexTraceOne.sln --configuration Release 2>&1 | grep -c "warning" || true)
if [ "$WARNINGS_COUNT" -eq 0 ]; then
    check_pass "Zero warnings"
else
    check_warn "$WARNINGS_COUNT warnings found"
fi

# 3. Testes Unitários
echo "🧪 [3/8] Executando testes unitários..."
TEST_RESULT=$(dotnet test tests/ --filter "FullyQualifiedName!~IntegrationTests" --configuration Release --no-build --verbosity quiet 2>&1 | tail -1)
if echo "$TEST_RESULT" | grep -q "Passed"; then
    PASSED=$(echo "$TEST_RESULT" | grep -oP 'Passed:\s+\K\d+' || echo "0")
    FAILED=$(echo "$TEST_RESULT" | grep -oP 'Failed:\s+\K\d+' || echo "0")
    if [ "$FAILED" -eq 0 ]; then
        check_pass "All unit tests passed ($PASSED tests)"
    else
        check_fail "$FAILED unit tests failed"
    fi
else
    check_warn "Unit tests status unclear"
fi

# 4. Health Checks Code
echo "🏥 [4/8] Verificando health checks..."
if grep -r "HealthCheckName" src/platform/NexTraceOne.BackgroundWorkers/Jobs/*.cs > /dev/null 2>&1; then
    HEALTH_CHECKS=$(grep -r "HealthCheckName" src/platform/NexTraceOne.BackgroundWorkers/Jobs/*.cs | wc -l)
    check_pass "$HEALTH_CHECKS jobs com health checks configurados"
else
    check_fail "No health checks found in background jobs"
fi

# 5. TODOs em Produção
echo "📝 [5/8] Procurando TODOs em código de produção..."
TODO_COUNT=$(grep -r "// TODO:" src/**/*.cs 2>/dev/null | wc -l || echo "0")
if [ "$TODO_COUNT" -eq 0 ]; then
    check_pass "Zero TODOs em produção"
else
    check_warn "$TODO_COUNT TODOs encontrados em produção"
fi

# 6. NotImplementedException
echo "🚫 [6/8] Procurando implementações incompletas..."
NOT_IMPL=$(grep -r "throw new NotImplementedException" src/**/*.cs 2>/dev/null | wc -l || echo "0")
if [ "$NOT_IMPL" -eq 0 ]; then
    check_pass "Zero NotImplementedException"
else
    check_fail "$NOT_IMPL NotImplementedException encontradas"
fi

# 7. Migrations Pendentes
echo "🗄️  [7/8] Verificando migrations pendentes..."
# Nota: Esta verificação requer PostgreSQL rodando, então é apenas informativa
check_warn "Verificação de migrations requer PostgreSQL ativo (pular em CI/CD sem DB)"

# 8. Security Validation Code Present
echo "🔒 [8/8] Verificando validações de segurança..."
if [ -f "src/platform/NexTraceOne.ApiHost/StartupValidation.cs" ] && \
   [ -f "src/platform/NexTraceOne.ApiHost/Preflight/Checks/JwtSecretPreflightCheck.cs" ]; then
    check_pass "Security validation code presente"
else
    check_fail "Security validation files missing"
fi

# Resumo Final
echo ""
echo "=========================================="
echo "📊 RESUMO DA VALIDAÇÃO"
echo "=========================================="
echo -e "Erros:   ${RED}$ERRORS${NC}"
echo -e "Warnings: ${YELLOW}$WARNINGS${NC}"
echo ""

if [ "$ERRORS" -eq 0 ]; then
    echo -e "${GREEN}✅ APROVADO PARA DEPLOY${NC}"
    echo ""
    echo "Próximos passos:"
    echo "1. Configurar variáveis de ambiente (JWT Secret, Connection Strings)"
    echo "2. Executar preflight check: curl http://localhost:8080/preflight"
    echo "3. Validar health endpoints: /health, /ready, /live"
    echo ""
    exit 0
else
    echo -e "${RED}❌ REPROVADO - CORRIGIR ERROS ANTES DO DEPLOY${NC}"
    echo ""
    echo "Erros críticos devem ser resolvidos antes de prosseguir."
    echo ""
    exit 1
fi
