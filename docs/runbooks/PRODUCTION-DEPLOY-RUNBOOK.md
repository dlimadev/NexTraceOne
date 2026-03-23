# Runbook — Deploy em Produção

> **NexTraceOne — Operação**  
> Versão: 1.0 | Data: 2026-03-22

---

## Pré-requisitos

- [ ] Deploy em staging executado e validado (smoke checks passando)
- [ ] Checklist de go-live preenchido (`/docs/checklists/GO-LIVE-CHECKLIST.md`)
- [ ] Aprovação formal do Release Readiness Lead
- [ ] Backup do banco de produção confirmado (ou snapshot automático)
- [ ] Janela de manutenção comunicada (se aplicável)
- [ ] GitHub Secrets configurados no environment `production`:
  - `PRODUCTION_CONN_IDENTITY`
  - `PRODUCTION_CONN_CATALOG`
  - `PRODUCTION_CONN_OPERATIONS`
  - `PRODUCTION_CONN_AI`
- [ ] GitHub Variables configurados:
  - `PRODUCTION_APIHOST_URL`
  - `PRODUCTION_FRONTEND_URL`
- [ ] Runtime environment variables definidas nos containers:
  - `Jwt__Secret` (mínimo 32 chars, gerar com `openssl rand -base64 48`)
  - `ASPNETCORE_ENVIRONMENT=Production`
  - Connection strings para todos os 19 DbContexts (ver [PRODUCTION-SECRETS-PROVISIONING.md](PRODUCTION-SECRETS-PROVISIONING.md))

---

## Fluxo de Deploy

### Passo 1 — Verificar estado do branch `main`

```bash
git log --oneline -10
git status
```

Confirmar que o commit a ser deployado é o mesmo validado em staging.

### Passo 2 — Disparar pipeline de produção

**Via GitHub Actions (recomendado)**:
1. Ir para: `https://github.com/dlimadev/NexTraceOne/actions`
2. Selecionar workflow de produção
3. Clicar em "Run workflow" → Branch: `main`
4. Aguardar jobs: `build-images` → `run-migrations` → `smoke-check`

**Via deploy manual (fallback)**:
```bash
# Definir tag da imagem a deployar (SHA do commit)
export IMAGE_TAG=<sha-8-chars>
export REGISTRY=ghcr.io/dlimadev

# Pull das imagens
docker pull ${REGISTRY}/nextraceone-apihost:${IMAGE_TAG}
docker pull ${REGISTRY}/nextraceone-workers:${IMAGE_TAG}
docker pull ${REGISTRY}/nextraceone-frontend:${IMAGE_TAG}

# Deploy via compose
IMAGE_TAG=${IMAGE_TAG} docker compose -f docker-compose.yml up -d --no-build

# Aguardar startup (30s)
sleep 30
```

### Passo 3 — Aplicar migrations

As migrations são aplicadas automaticamente no startup do ApiHost se configurado:
```
APPLY_MIGRATIONS_ON_STARTUP=true
```

Para aplicação manual (se startup não aplicar automaticamente):
```bash
bash scripts/db/apply-migrations.sh \
  --env production \
  --connection-string "${PROD_CONN_OPERATIONS}"
```

### Passo 4 — Validar smoke checks pós-deploy

```bash
APIHOST="https://${PROD_APIHOST_URL}"

# Liveness
curl -sf "${APIHOST}/live" | jq .
# Esperado: {"status":"Healthy"}

# Readiness (com DB check)
curl -sf "${APIHOST}/ready" | jq .
# Esperado: {"status":"Healthy"}

# Health completo
curl -sf "${APIHOST}/health" | jq .

# Login básico
curl -X POST "${APIHOST}/api/v1/identity/auth/login" \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@nextraceone.io","password":"<ADMIN_PASSWORD>"}' | jq .accessToken
```

Consultar `docs/runbooks/POST-DEPLOY-VALIDATION.md` para checklist completo.

### Passo 5 — Monitorar por 15 minutos pós-deploy

- Verificar logs do ApiHost para erros inesperados:
  ```bash
  docker logs nextraceone-apihost --tail 200 -f
  ```
- Verificar logs dos workers:
  ```bash
  docker logs nextraceone-workers --tail 100 -f
  ```
- Checar taxa de erros no dashboard de observabilidade (se disponível)

### Passo 6 — Declarar deploy concluído

Quando:
- Smoke checks passam
- Nenhum erro 5xx nos primeiros 15 minutos
- Health endpoints retornam "Healthy"

Comunicar via canal de operações: **"Deploy v{version} em produção confirmado em {timestamp}"**

---

## Rollback

Se qualquer smoke check falhar ou erros críticos aparecerem:

1. **Decisão imediata**: Rollback ou hotfix?
   - Ver `docs/runbooks/ROLLBACK-RUNBOOK.md` para critérios

2. **Rollback de imagens**:
   ```bash
   export IMAGE_TAG=<tag-anterior-estavel>
   IMAGE_TAG=${IMAGE_TAG} docker compose up -d --no-build apihost workers frontend
   ```

3. **Validar após rollback**: repetir Passo 4

---

## Contatos e Escalamento

| Situação | Ação |
|----------|------|
| Migrations falharam | Ver `docs/runbooks/MIGRATION-FAILURE-RUNBOOK.md` |
| Health check retorna Unhealthy | Ver `docs/runbooks/INCIDENT-RESPONSE-PLAYBOOK.md` |
| AI provider degradado | Ver `docs/runbooks/AI-PROVIDER-DEGRADATION-RUNBOOK.md` |
| Falha de workers | Ver `docs/runbooks/POST-DEPLOY-VALIDATION.md` |

---

## Evidências a Registrar

Após deploy bem-sucedido, registrar:

```
Deploy de Produção — NexTraceOne
Data/Hora: _______________
Versão/Tag: _______________
Executor: _______________
Smoke check /live: OK / FAIL
Smoke check /ready: OK / FAIL
Smoke check login: OK / FAIL
Incidentes ocorridos: Nenhum / Descrever
Decisão final: Deploy concluído / Rollback executado
```

---

*Runbook mantido pelo Platform Admin. Testar em staging antes de executar em produção.*
