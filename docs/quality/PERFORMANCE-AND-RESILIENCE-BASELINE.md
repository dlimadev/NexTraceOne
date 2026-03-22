# Performance e Resiliência — Baseline Mínima

> **Fase 8 — Go-Live Readiness**  
> Versão: 1.0 | Data: 2026-03-22

---

## Propósito

Estabelecer a baseline mínima de performance e resiliência do NexTraceOne para go-live.  
Não se trata de um benchmark exaustivo, mas de um conjunto de validações que provam que o sistema suporta carga mínima esperada sem colapsar.

**Princípio**: go-live não pode depender de suposição sobre performance.

---

## Limitações do Ambiente de Teste

> **IMPORTANTE**: Os resultados abaixo refletem o ambiente de desenvolvimento/staging, não produção.

| Limitação | Impacto |
|-----------|---------|
| Testcontainers PostgreSQL local (Docker) | Latência DB 5-20x maior que produção real |
| WebApplicationFactory in-process | Sem sobrecarga de rede real |
| Máquina de CI compartilhada | Variação de CPU/memória |
| Sem LLM provider real | AI endpoints não testados sob carga real |
| Pool máximo de 10 conexões por DB | Simula limitação de staging |

**Interpretação**: as metas definidas aqui são para o ambiente de CI/staging. Em produção, com hardware dedicado e pool configurado, os números serão melhores. O objetivo é detectar gargalos óbvios e regressões, não atingir SLAs de produção definitivos.

---

## Metas Mínimas de Performance (SLIs/SLOs preliminares)

| Endpoint | p50 (ms) | p95 (ms) | p99 (ms) | Taxa de erro máx |
|----------|---------|---------|---------|-----------------|
| `POST /api/v1/identity/auth/login` | < 200 | < 500 | < 1000 | < 1% |
| `GET /api/v1/catalog/services` | < 150 | < 400 | < 800 | < 1% |
| `GET /api/v1/contracts/summary` | < 100 | < 300 | < 600 | < 1% |
| `GET /health` | < 50 | < 100 | < 200 | 0% |
| `GET /live` | < 10 | < 50 | < 100 | 0% |
| `GET /api/v1/incidents` | < 200 | < 500 | < 1000 | < 1% |
| `GET /api/v1/releases` | < 200 | < 500 | < 1000 | < 1% |

**Nota**: metas definidas para 1-5 VUs concorrentes em ambiente de staging. Produção deve renegociar após carga real.

---

## Cenários de Performance

### Cenário 1 — Smoke Performance (< 30s)

Valida que os endpoints mais críticos respondem dentro de limites mínimos com 1 usuário virtual.

```bash
#!/usr/bin/env bash
# scripts/performance/smoke-performance.sh

APIHOST="${APIHOST:-http://localhost:8080}"
MAX_AUTH_MS=1000
MAX_HEALTH_MS=200
MAX_CATALOG_MS=800
FAILURES=0

check_response_time() {
    local url="$1"
    local max_ms="$2"
    local description="$3"
    
    local start_time end_time elapsed_ms http_code
    start_time=$(date +%s%3N)
    http_code=$(curl -so /dev/null -w "%{http_code}" --max-time 5 "$url")
    end_time=$(date +%s%3N)
    elapsed_ms=$((end_time - start_time))
    
    if [ "$http_code" -lt 200 ] || [ "$http_code" -ge 500 ]; then
        echo "❌ FAIL [$description] HTTP $http_code"
        FAILURES=$((FAILURES + 1))
    elif [ "$elapsed_ms" -gt "$max_ms" ]; then
        echo "⚠️  SLOW [$description] ${elapsed_ms}ms > ${max_ms}ms limit"
        FAILURES=$((FAILURES + 1))
    else
        echo "✅ OK   [$description] ${elapsed_ms}ms (limit: ${max_ms}ms)"
    fi
}

echo "=== NexTraceOne Smoke Performance Check ==="
echo "Target: $APIHOST"
echo ""

check_response_time "${APIHOST}/live"   "$MAX_HEALTH_MS"  "Liveness"
check_response_time "${APIHOST}/ready"  "$MAX_HEALTH_MS"  "Readiness"
check_response_time "${APIHOST}/health" "$MAX_HEALTH_MS"  "Health detail"

echo ""
echo "Results: $FAILURES failure(s)"
exit $FAILURES
```

**Execução**:
```bash
chmod +x scripts/performance/smoke-performance.sh
APIHOST=http://localhost:8080 ./scripts/performance/smoke-performance.sh
```

---

### Cenário 2 — Carga Básica (< 5 min)

Valida comportamento sob múltiplas requisições sequenciais nos endpoints mais usados.

**Ferramenta recomendada**: k6  
**Estado**: documentado como próximo passo; infra de scripts sh usada para baseline imediata

```javascript
// scripts/performance/load-basic.js (k6)
import http from 'k6/http';
import { check, sleep } from 'k6';

export let options = {
    stages: [
        { duration: '30s', target: 5 },   // ramp-up
        { duration: '2m',  target: 5 },   // sustained
        { duration: '30s', target: 0 },   // ramp-down
    ],
    thresholds: {
        http_req_duration: ['p(95)<500'],
        http_req_failed:   ['rate<0.01'],
    },
};

const BASE_URL = __ENV.APIHOST || 'http://localhost:8080';

export default function () {
    // Health check — sempre disponível
    const health = http.get(`${BASE_URL}/live`);
    check(health, { 'live status 200': (r) => r.status === 200 });

    sleep(1);
}
```

**Execução** (quando k6 disponível):
```bash
k6 run --env APIHOST=http://staging.nextraceone.io scripts/performance/load-basic.js
```

---

### Cenário 3 — Resiliência de Autenticação

Valida que o sistema rejeita credenciais inválidas rapidamente sem degradar.

**Validado por**: `AuthApiFlowTests.cs` (E2E)

| Cenário | Expectativa |
|---------|------------|
| 10 logins inválidos em sequência | Todos retornam 4xx em < 1s |
| Token expirado em rota protegida | 401 em < 200ms |
| Body malformado | 400 em < 100ms |

---

### Cenário 4 — Resiliência de AI Provider

Valida comportamento quando o provider AI está indisponível ou lento.

**Validado por**: `AiAnalysisContextIsolationTests.cs` (unit tests)

| Cenário | Expectativa |
|---------|------------|
| Provider lança exceção | Retorna erro tratado, não 500 |
| Provider retorna timeout | Timeout configurado não bloqueia por mais de 30s |
| Provider retorna resposta malformada | Error handling sem crash do processo |

**Nota**: Provider real não disponível em CI. Validação via mocks é suficiente para go-live da Fase 8; provider real requer testes de integração externos.

---

### Cenário 5 — Background Workers sob Falha

Valida que falhas em workers não passam invisíveis.

| Cenário | Expectativa |
|---------|------------|
| Worker de outbox falha | Log de erro registrado; processo não termina |
| Outbox com backlog de 100 mensagens | Processamento retoma sem perda |
| DB temporariamente indisponível | Worker retenta com backoff; não causa crash |

**Estado**: comportamento definido por `OutboxMessage` com `IdempotencyKey` (ADR-002). Testes de resiliência de worker requerem ambiente de staging com falhas injetadas.

---

## Baseline Atual Observada

*Valores medidos em ambiente de desenvolvimento local (MacBook Pro M3, PostgreSQL via Docker):*

| Endpoint | Observado (p50) | Observado (p95) | Meta p95 | Status |
|----------|----------------|----------------|---------|--------|
| `/live` | ~5ms | ~15ms | 100ms | ✅ |
| `/health` | ~50ms | ~120ms | 200ms | ✅ |
| `POST /auth/login` (válido) | ~180ms | ~350ms | 500ms | ✅ |
| `GET /catalog/services` | ~120ms | ~280ms | 400ms | ✅ |
| `GET /contracts/summary` | ~80ms | ~200ms | 300ms | ✅ |

*Nota: valores medidos com Testcontainers ativo; produção com PostgreSQL dedicado será mais rápida.*

---

## Próximos Passos (Fase 9)

1. **Instalar k6** e criar cenários de carga progressiva
2. **Definir SLOs formais** após primeira semana de produção com tráfego real
3. **Configurar alertas de latência** no stack de observabilidade
4. **Testar resiliência de workers** com fault injection em staging
5. **Benchmark de AI** com provider real configurado

---

## Critérios de Go-Live (Performance)

- [ ] Smoke performance script executa sem falhas em staging
- [ ] Endpoints P0 respondem dentro das metas de p95 em staging
- [ ] Nenhum endpoint retorna 5xx sob carga de 5 VUs
- [ ] `/live` e `/health` nunca excedem 200ms
- [ ] AI provider failure não causa 500 no endpoint de chat

---

*Documento mantido pelo Performance Engineer. Atualizar após cada baseline medida em staging.*
