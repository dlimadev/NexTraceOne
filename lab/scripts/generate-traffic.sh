#!/bin/bash
# ═══════════════════════════════════════════════════════════════════════════════
# NexTraceOne Lab — Traffic Generator
#
# Gera tráfego realista para os fake services do laboratório.
# Produz mix de operações: criação de encomendas, consultas e cancelamentos.
#
# Uso:
#   ./scripts/generate-traffic.sh                  # 50 requests (default)
#   ./scripts/generate-traffic.sh 200              # 200 requests
#   ./scripts/generate-traffic.sh 0                # Modo contínuo (Ctrl+C para parar)
# ═══════════════════════════════════════════════════════════════════════════════

set -euo pipefail

ORDER_SERVICE_URL="${ORDER_SERVICE_URL:-http://localhost:5010}"
INVENTORY_SERVICE_URL="${INVENTORY_SERVICE_URL:-http://localhost:5030}"
PAYMENT_SERVICE_URL="${PAYMENT_SERVICE_URL:-http://localhost:5020}"
REQUEST_COUNT="${1:-50}"
DELAY_MS="${2:-500}"

# Product IDs available in the lab
PRODUCTS=("prod-100" "prod-101" "prod-102" "prod-103" "prod-104" "prod-105" "prod-106" "prod-107" "prod-108" "prod-109")
CUSTOMERS=("cust-001" "cust-002" "cust-003" "cust-004" "cust-005" "cust-010" "cust-020" "cust-050")

echo "═══════════════════════════════════════════════════════════"
echo " NexTraceOne Lab — Traffic Generator"
echo "═══════════════════════════════════════════════════════════"
echo " Order Service:     $ORDER_SERVICE_URL"
echo " Payment Service:   $PAYMENT_SERVICE_URL"
echo " Inventory Service: $INVENTORY_SERVICE_URL"
echo " Request Count:     ${REQUEST_COUNT} (0 = continuous)"
echo " Delay:             ${DELAY_MS}ms between requests"
echo "═══════════════════════════════════════════════════════════"
echo ""

# Wait for services to be ready
echo "Checking service health..."
for url in "$ORDER_SERVICE_URL/health" "$PAYMENT_SERVICE_URL/health" "$INVENTORY_SERVICE_URL/health"; do
    if ! curl -sf "$url" > /dev/null 2>&1; then
        echo "WARNING: $url is not responding. Services may not be ready."
    else
        echo "  ✓ $url is healthy"
    fi
done
echo ""

counter=0
success=0
errors=0
created_orders=()

generate_request() {
    counter=$((counter + 1))
    local action=$((RANDOM % 10))

    # 50% — Create order (main flow: order → payment → inventory)
    if [ "$action" -lt 5 ]; then
        local customer="${CUSTOMERS[$((RANDOM % ${#CUSTOMERS[@]}))]}"
        local product="${PRODUCTS[$((RANDOM % ${#PRODUCTS[@]}))]}"
        local quantity=$((RANDOM % 5 + 1))

        echo -n "[$counter] POST /api/orders (customer=$customer, product=$product, qty=$quantity)... "
        response=$(curl -sf -o /dev/null -w "%{http_code}" \
            -X POST "$ORDER_SERVICE_URL/api/orders" \
            -H "Content-Type: application/json" \
            -d "{\"customerId\": \"$customer\", \"items\": [{\"productId\": \"$product\", \"quantity\": $quantity}]}" \
            2>/dev/null || echo "000")

        if [ "$response" = "201" ]; then
            echo "✓ Created ($response)"
            success=$((success + 1))
        else
            echo "✗ Failed ($response)"
            errors=$((errors + 1))
        fi

    # 20% — List orders
    elif [ "$action" -lt 7 ]; then
        echo -n "[$counter] GET /api/orders... "
        response=$(curl -sf -o /dev/null -w "%{http_code}" \
            "$ORDER_SERVICE_URL/api/orders" 2>/dev/null || echo "000")
        if [ "$response" = "200" ]; then
            echo "✓ OK ($response)"
            success=$((success + 1))
        else
            echo "✗ Failed ($response)"
            errors=$((errors + 1))
        fi

    # 15% — Check inventory
    elif [ "$action" -lt 8 ]; then
        local product="${PRODUCTS[$((RANDOM % ${#PRODUCTS[@]}))]}"
        echo -n "[$counter] GET /api/inventory/$product... "
        response=$(curl -sf -o /dev/null -w "%{http_code}" \
            "$INVENTORY_SERVICE_URL/api/inventory/$product" 2>/dev/null || echo "000")
        if [ "$response" = "200" ]; then
            echo "✓ OK ($response)"
            success=$((success + 1))
        else
            echo "✗ Failed ($response)"
            errors=$((errors + 1))
        fi

    # 10% — List all inventory
    elif [ "$action" -lt 9 ]; then
        echo -n "[$counter] GET /api/inventory... "
        response=$(curl -sf -o /dev/null -w "%{http_code}" \
            "$INVENTORY_SERVICE_URL/api/inventory" 2>/dev/null || echo "000")
        if [ "$response" = "200" ]; then
            echo "✓ OK ($response)"
            success=$((success + 1))
        else
            echo "✗ Failed ($response)"
            errors=$((errors + 1))
        fi

    # 5% — Direct payment check (simulates payment status query)
    else
        echo -n "[$counter] GET /api/payments/PAY-UNKNOWN... "
        response=$(curl -sf -o /dev/null -w "%{http_code}" \
            "$PAYMENT_SERVICE_URL/api/payments/PAY-UNKNOWN" 2>/dev/null || echo "000")
        if [ "$response" = "200" ] || [ "$response" = "404" ]; then
            echo "✓ OK ($response)"
            success=$((success + 1))
        else
            echo "✗ Failed ($response)"
            errors=$((errors + 1))
        fi
    fi

    # Delay between requests
    sleep "$(echo "scale=3; $DELAY_MS/1000" | bc 2>/dev/null || echo "0.5")"
}

# Main loop
if [ "$REQUEST_COUNT" = "0" ]; then
    echo "Running in continuous mode. Press Ctrl+C to stop."
    echo ""
    trap 'echo ""; echo "═══════════════════════════════════════════════════════════"; echo "Completed: $counter requests | Success: $success | Errors: $errors"; echo "═══════════════════════════════════════════════════════════"; exit 0' INT
    while true; do
        generate_request
    done
else
    echo "Generating $REQUEST_COUNT requests..."
    echo ""
    for _ in $(seq 1 "$REQUEST_COUNT"); do
        generate_request
    done
fi

echo ""
echo "═══════════════════════════════════════════════════════════"
echo " Traffic Generation Complete"
echo "═══════════════════════════════════════════════════════════"
echo " Total:   $counter requests"
echo " Success: $success"
echo " Errors:  $errors"
echo "═══════════════════════════════════════════════════════════"
