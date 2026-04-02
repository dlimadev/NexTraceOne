#!/bin/bash
# ═══════════════════════════════════════════════════════════════════════════════
# NexTraceOne Lab — Elasticsearch Index Setup
#
# Creates index templates for the laboratory environment.
# This script runs after Elasticsearch is healthy.
# ═══════════════════════════════════════════════════════════════════════════════

ES_URL="${ELASTICSEARCH_ENDPOINT:-http://localhost:9200}"

echo "Waiting for Elasticsearch to be ready..."
until curl -sf "$ES_URL/_cluster/health" > /dev/null 2>&1; do
  sleep 2
done

echo "Creating index templates for NexTraceOne Lab..."

# Traces index template
curl -sf -X PUT "$ES_URL/_index_template/nextraceone-obs-traces" \
  -H "Content-Type: application/json" \
  -d '{
    "index_patterns": ["nextraceone-obs-traces*"],
    "priority": 100,
    "template": {
      "settings": {
        "number_of_shards": 1,
        "number_of_replicas": 0,
        "refresh_interval": "5s"
      },
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
          "http.url": { "type": "keyword" },
          "lab.environment": { "type": "keyword" }
        }
      }
    }
  }'

echo ""

# Logs index template
curl -sf -X PUT "$ES_URL/_index_template/nextraceone-obs-logs" \
  -H "Content-Type: application/json" \
  -d '{
    "index_patterns": ["nextraceone-obs-logs*"],
    "priority": 100,
    "template": {
      "settings": {
        "number_of_shards": 1,
        "number_of_replicas": 0,
        "refresh_interval": "5s"
      },
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
  }'

echo ""

# Metrics index template
curl -sf -X PUT "$ES_URL/_index_template/nextraceone-obs-metrics" \
  -H "Content-Type: application/json" \
  -d '{
    "index_patterns": ["nextraceone-obs-metrics*"],
    "priority": 100,
    "template": {
      "settings": {
        "number_of_shards": 1,
        "number_of_replicas": 0,
        "refresh_interval": "10s"
      },
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
  }'

echo ""
echo "Index templates created successfully."
