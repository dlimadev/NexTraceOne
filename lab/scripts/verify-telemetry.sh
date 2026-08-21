#!/bin/bash
# ═══════════════════════════════════════════════════════════════════════════════
# NexTraceOne Lab — Verify Telemetry
#
# Verifica se o OTel Collector está de pé e se as fake APIs respondem.
#
# Sem backend de telemetria: o Elasticsearch foi removido do produto
# (ClickHouse é o provider único) e retirado do lab. O collector exporta via
# `debug`, pelo que a telemetria se inspecciona nos seus logs:
#
#   docker compose -f docker-compose.lab.yml logs -f otel-collector
#
# Uso:
#   ./scripts/verify-telemetry.sh
# ═══════════════════════════════════════════════════════════════════════════════

set -euo pipefail

echo "═══════════════════════════════════════════════════════════"
echo " NexTraceOne Lab — Telemetry Verification"
echo "═══════════════════════════════════════════════════════════"
echo ""

echo "OTel Collector health:"
curl -sf "http://localhost:13133" >/dev/null 2>&1 && echo "  ✓ Healthy" || echo "  ✗ Not reachable"
echo ""

echo "Fake API health:"
for svc in "Order:5010" "Payment:5020" "Inventory:5030"; do
    name="${svc%%:*}"
    port="${svc##*:}"
    if curl -sf "http://localhost:$port/health" >/dev/null 2>&1; then
        printf "  ✓ %-12s http://localhost:%s/health\n" "$name" "$port"
    else
        printf "  ✗ %-12s http://localhost:%s/health\n" "$name" "$port"
    fi
done
echo ""

echo "────────────────────────────────────────────────────────────"
echo " Telemetria exportada via \`debug\` — inspeccionar com:"
echo "   docker compose -f docker-compose.lab.yml logs -f otel-collector"
echo "════════════════════════════════════════════════════════════"
