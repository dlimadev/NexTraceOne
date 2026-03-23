# NexTraceOne — Load Testing Suite

Formal load testing baseline for the NexTraceOne platform using [k6](https://k6.io/).

## Prerequisites

Install k6:
```bash
# macOS
brew install k6

# Linux (Debian/Ubuntu)
sudo gpg -k
sudo gpg --no-default-keyring --keyring /usr/share/keyrings/k6-archive-keyring.gpg --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys C5AD17C747E3415A3642D57D77C6C491D6AC1D68
echo "deb [signed-by=/usr/share/keyrings/k6-archive-keyring.gpg] https://dl.k6.io/deb stable main" | sudo tee /etc/apt/sources.list.d/k6.list
sudo apt-get update && sudo apt-get install k6

# Docker
docker pull grafana/k6
```

## Environment Variables

| Variable | Default | Description |
|----------|---------|-------------|
| `K6_BASE_URL` | `http://localhost:5187` | API base URL |
| `K6_USERNAME` | `admin@acme.com` | Login email |
| `K6_PASSWORD` | `Admin123!` | Login password |
| `K6_TENANT_ID` | `tenant-001` | Tenant identifier |
| `K6_ENV` | `local` | Environment profile (`local`, `staging`, `production`) |

## Running Tests

```bash
# Single scenario
k6 run scenarios/auth-load.js
k6 run scenarios/catalog-load.js
k6 run scenarios/contracts-load.js
k6 run scenarios/governance-load.js
k6 run scenarios/mixed-load.js

# All scenarios
bash run-all.sh
```

## Scenarios

| Scenario | Endpoints | VUs (smoke/load/stress) |
|----------|-----------|------------------------|
| `auth-load` | login, refresh, me | 1 / 10 / 25 |
| `catalog-load` | services, summary, graph | 1 / 15 / 30 |
| `contracts-load` | list, summary | 1 / 10 / 20 |
| `governance-load` | health, exec, finops, compliance | 1 / 10 / — |
| `mixed-load` | realistic journey across modules | 20 (avg) / 40 (peak) |

## Thresholds

- **p95 latency** < 2000ms
- **p99 latency** < 5000ms
- **Error rate** < 5%
- **Throughput** > 10 req/s

## Interpreting Results

k6 outputs a summary with:
- `http_req_duration` — Response time percentiles (avg, med, p90, p95, p99)
- `http_req_failed` — Percentage of failed requests
- `http_reqs` — Total requests and rate
- `checks` — Pass/fail rate of assertions

Thresholds marked with ✓ passed; ✗ failed.

## Limitations

- Results depend heavily on the test environment (local vs. CI vs. staging)
- Database state affects response times
- Single-machine tests cannot simulate real geographic distribution
- Results should be compared across runs on the same environment for trends
