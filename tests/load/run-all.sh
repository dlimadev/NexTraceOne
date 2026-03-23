#!/usr/bin/env bash
# Run all k6 load test scenarios sequentially and collect results.
# Usage: bash run-all.sh
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
RESULTS_DIR="${SCRIPT_DIR}/results"
mkdir -p "${RESULTS_DIR}"

SCENARIOS=(
  "auth-load"
  "catalog-load"
  "contracts-load"
  "governance-load"
  "mixed-load"
)

TIMESTAMP=$(date +%Y%m%d-%H%M%S)
PASS=0
FAIL=0

echo "═══════════════════════════════════════════════════"
echo "  NexTraceOne — k6 Load Test Suite"
echo "  $(date)"
echo "═══════════════════════════════════════════════════"
echo ""

for scenario in "${SCENARIOS[@]}"; do
  echo "▶ Running: ${scenario}..."
  OUTFILE="${RESULTS_DIR}/${scenario}-${TIMESTAMP}.txt"
  if k6 run "${SCRIPT_DIR}/scenarios/${scenario}.js" 2>&1 | tee "${OUTFILE}"; then
    echo "  ✓ ${scenario} passed"
    PASS=$((PASS + 1))
  else
    echo "  ✗ ${scenario} FAILED (see ${OUTFILE})"
    FAIL=$((FAIL + 1))
  fi
  echo ""
done

echo "═══════════════════════════════════════════════════"
echo "  Summary: ${PASS} passed, ${FAIL} failed"
echo "  Results saved to: ${RESULTS_DIR}/"
echo "═══════════════════════════════════════════════════"

exit ${FAIL}
