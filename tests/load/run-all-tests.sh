#!/bin/bash
# Script de automação para execução completa de load tests
# Uso: ./run-all-tests.sh [smoke|load|stress|spike|endurance|all]

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SCENARIOS_DIR="$SCRIPT_DIR/scenarios"
REPORTS_DIR="$SCRIPT_DIR/reports"
TIMESTAMP=$(date +%Y%m%d_%H%M%S)

# Cores para output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Verificar se k6 está instalado
if ! command -v k6 &> /dev/null; then
    echo -e "${RED}❌ k6 não está instalado. Instale em: https://k6.io/docs/getting-started/installation/${NC}"
    exit 1
fi

echo -e "${BLUE}╔═══════════════════════════════════════════════════════════╗${NC}"
echo -e "${BLUE}║         NexTraceOne Load Testing Framework               ║${NC}"
echo -e "${BLUE}╚═══════════════════════════════════════════════════════════╝${NC}"
echo ""

# Determinar quais testes executar
TEST_TYPE=${1:-all}

run_test() {
    local test_name=$1
    local test_file=$2
    local duration=$3

    echo -e "${YELLOW}▶ Executando: ${test_name}${NC}"
    echo -e "   Duração estimada: ${duration}"
    echo -e "   Relatório: ${REPORTS_DIR}/${test_name}_${TIMESTAMP}.json"
    echo ""

    # Criar diretório de relatórios se não existir
    mkdir -p "$REPORTS_DIR"

    # Executar teste com output JSON
    if k6 run "$test_file" --out json="${REPORTS_DIR}/${test_name}_${TIMESTAMP}.json"; then
        echo -e "${GREEN}✅ ${test_name} concluído com sucesso!${NC}"
    else
        echo -e "${RED}❌ ${test_name} falhou! Verifique os logs acima.${NC}"
        return 1
    fi

    echo ""
    echo "─────────────────────────────────────────────────────────────"
    echo ""
}

case $TEST_TYPE in
    smoke)
        run_test "smoke-test" "$SCENARIOS_DIR/smoke-test.js" "30 segundos"
        ;;
    load)
        run_test "load-test" "$SCENARIOS_DIR/load-test.js" "9 minutos"
        ;;
    stress)
        run_test "stress-test" "$SCENARIOS_DIR/stress-test.js" "24 minutos"
        ;;
    spike)
        run_test "spike-test" "$SCENARIOS_DIR/spike-test.js" "8 minutos"
        ;;
    endurance)
        run_test "endurance-test" "$SCENARIOS_DIR/endurance-test.js" "1 hora"
        ;;
    all)
        echo -e "${BLUE}📋 Executando TODOS os testes em sequência...${NC}"
        echo ""

        # Smoke test primeiro (validação rápida)
        run_test "smoke-test" "$SCENARIOS_DIR/smoke-test.js" "30 segundos" || {
            echo -e "${RED}⚠️  Smoke test falhou. Abortando testes restantes.${NC}"
            exit 1
        }

        # Load test
        run_test "load-test" "$SCENARIOS_DIR/load-test.js" "9 minutos"

        # Stress test
        run_test "stress-test" "$SCENARIOS_DIR/stress-test.js" "24 minutos"

        # Spike test
        run_test "spike-test" "$SCENARIOS_DIR/spike-test.js" "8 minutos"

        echo -e "${YELLOW}⚠️  Endurance test (1h) não executado automaticamente.${NC}"
        echo -e "${YELLOW}   Para executar: ./run-all-tests.sh endurance${NC}"
        ;;
    *)
        echo -e "${RED}❌ Tipo de teste desconhecido: ${TEST_TYPE}${NC}"
        echo ""
        echo "Uso: $0 [smoke|load|stress|spike|endurance|all]"
        echo ""
        echo "Tipos disponíveis:"
        echo "  smoke     - Validação rápida (30s)"
        echo "  load      - Carga normal (9min)"
        echo "  stress    - Carga extrema (24min)"
        echo "  spike     - Picos súbitos (8min)"
        echo "  endurance - Longa duração (1h)"
        echo "  all       - Executar smoke + load + stress + spike"
        exit 1
        ;;
esac

echo ""
echo -e "${GREEN}╔═══════════════════════════════════════════════════════════╗${NC}"
echo -e "${GREEN}║                    Testes Concluídos!                     ║${NC}"
echo -e "${GREEN}╚═══════════════════════════════════════════════════════════╝${NC}"
echo ""
echo -e "Relatórios salvos em: ${BLUE}${REPORTS_DIR}${NC}"
echo ""
echo -e "Para visualizar resultados:"
echo -e "  k6 inspect ${REPORTS_DIR}/*_${TIMESTAMP}.json"
echo ""
