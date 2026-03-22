# Validação Pós-Deploy — NexTraceOne

## Smoke checks obrigatórios

Execute após qualquer deploy em staging ou produção.

---

## 1. Health checks dos serviços backend

### ApiHost

```bash
APIHOST="http(s)://<apihost-url>"

# Liveness — serviço está vivo
curl -sf "${APIHOST}/live" | jq .
# Esperado: {"status":"Healthy"}

# Readiness — serviço pronto para tráfego (inclui DB check)
curl -sf "${APIHOST}/ready" | jq .
# Esperado: {"status":"Healthy"}

# Detalhes completos
curl -sf "${APIHOST}/health" | jq .
```

### BackgroundWorkers

```bash
WORKERS="http(s)://<workers-url>"

curl -sf "${WORKERS}/live" | jq .
# Esperado: {"status":"Healthy"}
```

### Ingestion API

```bash
INGESTION="http(s)://<ingestion-url>"

curl -sf "${INGESTION}/live" | jq .
# Esperado: {"status":"Healthy"}
```

---

## 2. Frontend carregando

```bash
FRONTEND="http(s)://<frontend-url>"

# HTTP 200 + HTML válido
HTTP_STATUS=$(curl -sLo /dev/null -w "%{http_code}" "${FRONTEND}")
echo "Status: ${HTTP_STATUS}"
# Esperado: 200

# Verificar se é HTML (não 404/error page)
curl -sf "${FRONTEND}" | grep -q "<html"
echo "Frontend HTML: OK"
```

---

## 3. Conectividade com banco de dados

O `/ready` já testa conectividade com banco.
Se `/ready` retornar `Unhealthy`, verificar:

```bash
# Verificar detalhe do health check
curl -sf "${APIHOST}/health" | jq '.entries.database'

# Teste direto de conexão (se acesso ao servidor)
psql -h <pg-host> -U nextraceone -d nextraceone_identity -c "SELECT 1"
```

---

## 4. Migrations aplicadas

Verificar se a migration mais recente está presente:

```bash
# Via dotnet ef (requer acesso ao servidor com .NET SDK)
dotnet ef migrations list \
  --project src/platform/NexTraceOne.ApiHost/NexTraceOne.ApiHost.csproj \
  --context NexTraceOne.IdentityAccess.Infrastructure.Persistence.IdentityDbContext \
  --connection "<connection-string>"
# A última migration deve estar marcada como applied
```

---

## 5. Workers ativos

```bash
WORKERS="http(s)://<workers-url>"

# Verificar se jobs estão registados
curl -sf "${WORKERS}/health" | jq '.entries'
# Esperado: jobs registados e ativos
```

---

## 6. Observabilidade operacional

```bash
# OTel Collector health
curl -sf http://localhost:13133/

# Grafana acessível
curl -sf http://localhost:3000/api/health | jq .
# Esperado: {"database":"ok","version":"..."}
```

---

## Checklist pós-deploy

```
[ ] ApiHost /live → Healthy
[ ] ApiHost /ready → Healthy (inclui DB)
[ ] BackgroundWorkers /live → Healthy
[ ] Ingestion /live → Healthy
[ ] Frontend carregando (HTTP 200)
[ ] Sem aumento de erros 5xx nos logs (Grafana)
[ ] Workers registados e ativos
[ ] Migrations aplicadas (última migration presente)
[ ] OTel Collector recebendo dados
[ ] Grafana dashboards operacionais
```

---

## Sinais de sucesso

| Sinal | Onde verificar |
|---|---|
| `/live` → `Healthy` | Todos os serviços |
| `/ready` → `Healthy` | ApiHost, Ingestion |
| Frontend HTTP 200 | Browser / curl |
| Logs sem critical errors | Grafana / Loki |
| Traces chegando ao Tempo | Grafana Tempo |
| DB acessível | Health check |

---

## Sinais de falha — agir imediatamente

| Sinal | Ação |
|---|---|
| `/ready` → `Unhealthy` | Verificar logs + DB. Se não resolver em 5min → rollback |
| Frontend 503/504 | Verificar se ApiHost está UP |
| Erro 500 em endpoints críticos | Verificar logs. Decidir rollback vs hotfix |
| Workers parados | Verificar logs. Restart se necessário |
| Migration failed no pipeline | Não avançar com deploy. Corrigir migration |

---

## Referência de URLs por ambiente

| Ambiente | ApiHost | Frontend | Ingestion |
|---|---|---|---|
| Development | `http://localhost:8080` | `http://localhost:5173` | `http://localhost:8082` |
| Staging (compose) | `http://localhost:8080` | `http://localhost:3000` | `http://localhost:8082` |
| Staging (real) | Conforme `STAGING_APIHOST_URL` | Conforme `STAGING_FRONTEND_URL` | Conforme `STAGING_INGESTION_URL` |
