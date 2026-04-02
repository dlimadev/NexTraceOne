#!/bin/bash
# ═══════════════════════════════════════════════════════════════════════════════
# NexTraceOne Lab — Verify Telemetry
#
# Verifica se a telemetria está a ser ingerida corretamente no Elasticsearch.
#
# Uso:
#   ./scripts/verify-telemetry.sh
# ═══════════════════════════════════════════════════════════════════════════════

set -euo pipefail

ES_URL="${ELASTICSEARCH_ENDPOINT:-http://localhost:9200}"

echo "═══════════════════════════════════════════════════════════"
echo " NexTraceOne Lab — Telemetry Verification"
echo "═══════════════════════════════════════════════════════════"
echo ""

# Check Elasticsearch health
echo "Elasticsearch cluster health:"
curl -sf "$ES_URL/_cluster/health?pretty" 2>/dev/null || echo "  ✗ Elasticsearch not reachable"
echo ""

# Check OTel Collector health
echo "OTel Collector health:"
curl -sf "http://localhost:13133" 2>/dev/null && echo "  ✓ Healthy" || echo "  ✗ Not reachable"
echo ""

# Count documents per index
echo "Document counts per index:"
echo "────────────────────────────────────────────────────────────"

for index in "nextraceone-obs-traces" "nextraceone-obs-logs" "nextraceone-obs-metrics"; do
    count=$(curl -sf "$ES_URL/$index/_count" 2>/dev/null | grep -o '"count":[0-9]*' | cut -d: -f2 || echo "N/A")
    printf "  %-35s %s documents\n" "$index" "$count"
done

echo ""
echo "────────────────────────────────────────────────────────────"

# Show recent traces
echo ""
echo "Most recent traces (last 5):"
curl -sf "$ES_URL/nextraceone-obs-traces/_search?pretty" \
    -H "Content-Type: application/json" \
    -d '{
        "size": 5,
        "sort": [{"@timestamp": "desc"}],
        "_source": ["@timestamp", "name", "service.name", "status", "duration"]
    }' 2>/dev/null || echo "  No traces found"

echo ""
echo "════════════════════════════════════════════════════════════"
echo " Service endpoints:"
echo "   Order Service:     http://localhost:5010/health"
echo "   Payment Service:   http://localhost:5020/health"
echo "   Inventory Service: http://localhost:5030/health"
echo "   Elasticsearch:     http://localhost:9200"
echo "   Kibana:            http://localhost:5601"
echo "   OTel Collector:    http://localhost:13133"
echo "════════════════════════════════════════════════════════════"
