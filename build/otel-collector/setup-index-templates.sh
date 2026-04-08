#!/bin/bash
# ──────────────────────────────────────────────────────────────────────────────
# NexTraceOne — Elasticsearch Index Template Setup
#
# Applies index templates for logs, traces and metrics indices.
# Run AFTER Elasticsearch is up and BEFORE the OTel Collector starts writing.
#
# Usage:
#   ./setup-index-templates.sh [ELASTICSEARCH_URL]
#
# Default: http://localhost:9200
# ──────────────────────────────────────────────────────────────────────────────

set -euo pipefail

ES_URL="${1:-http://localhost:9200}"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
TEMPLATES_DIR="${SCRIPT_DIR}/index-templates"

echo "NexTraceOne — Elasticsearch Index Template Setup"
echo "Elasticsearch URL: ${ES_URL}"
echo ""

# Wait for Elasticsearch to be ready
echo "Waiting for Elasticsearch..."
for i in $(seq 1 30); do
  if curl -s "${ES_URL}/_cluster/health" > /dev/null 2>&1; then
    echo "Elasticsearch is ready."
    break
  fi
  if [ "$i" -eq 30 ]; then
    echo "ERROR: Elasticsearch not reachable at ${ES_URL} after 30 attempts."
    exit 1
  fi
  sleep 2
done

echo ""

# Apply index templates
for template_file in "${TEMPLATES_DIR}"/*-template.json; do
  template_name=$(basename "${template_file}" -template.json)
  template_name="nextraceone-${template_name}"
  echo "Applying index template: ${template_name}"
  
  HTTP_CODE=$(curl -s -o /dev/null -w "%{http_code}" \
    -X PUT "${ES_URL}/_index_template/${template_name}" \
    -H "Content-Type: application/json" \
    -d @"${template_file}")
  
  if [ "${HTTP_CODE}" = "200" ]; then
    echo "  ✓ ${template_name} applied successfully"
  else
    echo "  ✗ ${template_name} failed (HTTP ${HTTP_CODE})"
    curl -s -X PUT "${ES_URL}/_index_template/${template_name}" \
      -H "Content-Type: application/json" \
      -d @"${template_file}"
    echo ""
  fi
done

echo ""
echo "Index template setup complete."
