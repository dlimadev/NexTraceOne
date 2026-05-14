#!/bin/bash

# ClickHouse Deployment Script for NexTraceOne
# This script deploys ClickHouse cluster and initializes the schema

set -e

NAMESPACE="nextraceone"
CLICKHOUSE_VERSION="24.1"

echo "🚀 Deploying ClickHouse Cluster for NexTraceOne..."

# Step 1: Create namespace if not exists
echo "📦 Creating namespace..."
kubectl create namespace $NAMESPACE --dry-run=client -o yaml | kubectl apply -f -

# Step 2: Deploy ClickHouse cluster
echo "🔧 Deploying ClickHouse cluster (3 nodes)..."
kubectl apply -f deploy/clickhouse/clickhouse-cluster.yaml -n $NAMESPACE

# Wait for ClickHouse to be ready
echo "⏳ Waiting for ClickHouse pods to be ready..."
kubectl wait --for=condition=ready pod -l app=clickhouse -n $NAMESPACE --timeout=300s

# Step 3: Initialize schema
echo "📊 Initializing ClickHouse schema..."
kubectl exec -it clickhouse-0 -n $NAMESPACE -- clickhouse-client --query="$(cat deploy/clickhouse/schema.sql)"

# Step 4: Verify deployment
echo "✅ Verifying deployment..."
kubectl get pods -l app=clickhouse -n $NAMESPACE
kubectl get svc -l app=clickhouse -n $NAMESPACE

# Step 5: Test connection
echo "🧪 Testing ClickHouse connection..."
kubectl exec -it clickhouse-0 -n $NAMESPACE -- clickhouse-client --query="SELECT count() FROM nextraceone.events"

echo ""
echo "🎉 ClickHouse deployment completed successfully!"
echo ""
echo "Connection details:"
echo "  HTTP Port: 8123"
echo "  Native Port: 9000"
echo "  Database: nextraceone"
echo ""
echo "To connect locally:"
echo "  kubectl port-forward svc/clickhouse 8123:8123 -n $NAMESPACE"
echo "  Then access http://localhost:8123/play"
echo ""
