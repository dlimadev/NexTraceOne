#!/bin/bash
# ═══════════════════════════════════════════════════════════════════════════════
# NexTraceOne Lab — Setup Elasticsearch Indices
#
# Creates index templates for the laboratory environment.
# Run this after Elasticsearch is healthy.
#
# Uso:
#   ./scripts/setup-elasticsearch.sh
# ═══════════════════════════════════════════════════════════════════════════════

set -euo pipefail

ES_URL="${ELASTICSEARCH_ENDPOINT:-http://localhost:9200}"

echo "═══════════════════════════════════════════════════════════"
echo " NexTraceOne Lab — Elasticsearch Index Setup"
echo "═══════════════════════════════════════════════════════════"
echo " Elasticsearch: $ES_URL"
echo "═══════════════════════════════════════════════════════════"
echo ""

echo "Waiting for Elasticsearch..."
for i in $(seq 1 30); do
    if curl -sf "$ES_URL/_cluster/health" > /dev/null 2>&1; then
        echo "  ✓ Elasticsearch is ready"
        break
    fi
    echo "  Attempt $i/30 — waiting 5s..."
    sleep 5
done

echo ""
echo "Creating index templates..."

# Traces
echo -n "  Creating nextraceone-obs-traces template... "
curl -sf -X PUT "$ES_URL/_index_template/nextraceone-obs-traces" \
  -H "Content-Type: application/json" \
  -d '{
    "index_patterns": ["nextraceone-obs-traces*"],
    "priority": 100,
    "template": {
      "settings": { "number_of_shards": 1, "number_of_replicas": 0, "refresh_interval": "5s" },
      "mappings": {
        "properties": {
          "@timestamp": { "type": "date" },
          "trace_id": { "type": "keyword" },
          "span_id": { "type": "keyword" },
          "parent_span_id": { "type": "keyword" },
          "name": { "type": "keyword" },
          "kind": { "type": "keyword" },
          "status": { "type": "keyword" },
          "duration": { "type": "long" },
          "service.name": { "type": "keyword" },
          "service.namespace": { "type": "keyword" },
          "deployment.environment": { "type": "keyword" },
          "http.method": { "type": "keyword" },
          "http.route": { "type": "keyword" },
          "http.status_code": { "type": "integer" },
          "lab.environment": { "type": "keyword" }
        }
      }
    }
  }' && echo "✓" || echo "✗"

# Logs
echo -n "  Creating nextraceone-obs-logs template... "
curl -sf -X PUT "$ES_URL/_index_template/nextraceone-obs-logs" \
  -H "Content-Type: application/json" \
  -d '{
    "index_patterns": ["nextraceone-obs-logs*"],
    "priority": 100,
    "template": {
      "settings": { "number_of_shards": 1, "number_of_replicas": 0, "refresh_interval": "5s" },
      "mappings": {
        "properties": {
          "@timestamp": { "type": "date" },
          "trace_id": { "type": "keyword" },
          "span_id": { "type": "keyword" },
          "severity": { "type": "keyword" },
          "body": { "type": "text" },
          "service.name": { "type": "keyword" },
          "service.namespace": { "type": "keyword" },
          "deployment.environment": { "type": "keyword" },
          "lab.environment": { "type": "keyword" }
        }
      }
    }
  }' && echo "✓" || echo "✗"

# Metrics
echo -n "  Creating nextraceone-obs-metrics template... "
curl -sf -X PUT "$ES_URL/_index_template/nextraceone-obs-metrics" \
  -H "Content-Type: application/json" \
  -d '{
    "index_patterns": ["nextraceone-obs-metrics*"],
    "priority": 100,
    "template": {
      "settings": { "number_of_shards": 1, "number_of_replicas": 0, "refresh_interval": "10s" },
      "mappings": {
        "properties": {
          "@timestamp": { "type": "date" },
          "metric.name": { "type": "keyword" },
          "metric.type": { "type": "keyword" },
          "metric.value": { "type": "double" },
          "service.name": { "type": "keyword" },
          "service.namespace": { "type": "keyword" },
          "deployment.environment": { "type": "keyword" },
          "lab.environment": { "type": "keyword" }
        }
      }
    }
  }' && echo "✓" || echo "✗"

echo ""
echo "Verifying indices..."
curl -sf "$ES_URL/_cat/templates/nextraceone*?v" 2>/dev/null || echo "  (no templates found yet)"

echo ""
echo "═══════════════════════════════════════════════════════════"
echo " Setup complete!"
echo "═══════════════════════════════════════════════════════════"
echo ""
echo " Next steps:"
echo "   1. Start the lab services: docker compose -f docker-compose.lab.yml up -d"
echo "   2. Generate traffic:       ./scripts/generate-traffic.sh"
echo "   3. Check Kibana:           http://localhost:5601"
echo "   4. Query Elasticsearch:    curl http://localhost:9200/nextraceone-obs-traces/_search?pretty"
