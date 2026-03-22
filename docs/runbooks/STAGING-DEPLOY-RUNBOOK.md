# Runbook — Deploy em Staging

## Pré-requisitos

- Acesso ao repositório GitHub com permissão `write`
- GitHub Secrets configurados (environment `staging`):
  - `STAGING_CONN_IDENTITY`
  - `STAGING_CONN_CATALOG`
  - `STAGING_CONN_OPERATIONS`
  - `STAGING_CONN_AI`
- GitHub Variables configurados:
  - `STAGING_APIHOST_URL`
  - `STAGING_FRONTEND_URL`
- Servidor staging com Docker Compose instalado (se deploy manual)

---

## Fluxo 1 — Deploy via Pipeline (recomendado)

### Passo 1 — Merge para `main`

```bash
git checkout main
git merge --no-ff feature/<nome>
git push origin main
```

O pipeline `staging.yml` dispara automaticamente.

### Passo 2 — Acompanhar pipeline

1. Ir para: `https://github.com/dlimadev/NexTraceOne/actions`
2. Selecionar o run de `Staging`
3. Verificar jobs:
   - `build-images` — Builda e pusha imagens para `ghcr.io`
   - `run-migrations` — Aplica migrations no staging
   - `smoke-check` — Valida `/live`, `/ready`, frontend

### Passo 3 — Validar smoke checks

Se o job `smoke-check` passar, o deploy está completo.
Conferir no GitHub Actions Summary o deployment report.

---

## Fluxo 2 — Deploy manual via `workflow_dispatch`

1. Ir para: `Actions → Staging → Run workflow`
2. Preencher:
   - Branch: `main`
   - `run_migrations`: `true` (ou `false` se migrations já aplicadas)
   - `skip_smoke`: `false`
3. Clicar `Run workflow`

---

## Fluxo 3 — Deploy manual via Docker Compose (servidor)

```bash
# 1. Conectar ao servidor staging
ssh user@staging-server

# 2. Ir ao diretório do projeto
cd /opt/nextraceone

# 3. Pull das últimas imagens
docker compose pull

# 4. Aplicar migrations (se necessário)
export CONN_IDENTITY="$(cat /etc/secrets/conn_identity)"
export CONN_CATALOG="$(cat /etc/secrets/conn_catalog)"
export CONN_OPERATIONS="$(cat /etc/secrets/conn_operations)"
export CONN_AI="$(cat /etc/secrets/conn_ai)"
bash scripts/db/apply-migrations.sh --env Staging

# 5. Subir serviços com a nova imagem
docker compose up -d

# 6. Verificar saúde
docker compose ps
curl http://localhost:8080/ready
```

---

## Validação pós-deploy

Ver [POST-DEPLOY-VALIDATION.md](./POST-DEPLOY-VALIDATION.md) para checklist completo.

### Validação mínima rápida

```bash
APIHOST="https://<staging-url>"

# Liveness
curl -sf "${APIHOST}/live"

# Readiness (com checks de banco)
curl -sf "${APIHOST}/ready"

# Frontend carregando
curl -sf "https://<frontend-url>/" | grep -q "<html"
echo "Frontend OK"
```

---

## Troubleshooting comum

### Serviço não responde após deploy

```bash
# Verificar logs
docker compose logs apihost --tail=50
docker compose logs workers --tail=50

# Verificar health
docker compose ps
```

### Migration falhou

```bash
# Ver último erro
bash scripts/db/apply-migrations.sh --dry-run

# Rollback para versão anterior
bash scripts/db/apply-migrations.sh --env Staging
# (se o script falhar, ver ROLLBACK-RUNBOOK.md)
```

### Imagem não encontrada

```bash
# Verificar se imagens estão no registry
docker pull ghcr.io/dlimadev/nextraceone-apihost:staging

# Se não disponível, rebuild manual
docker build -f Dockerfile.apihost -t nextraceone/apihost:staging .
```

### Banco inacessível

```bash
# Testar conexão
psql -h <host> -U nextraceone -d nextraceone_identity -c "SELECT 1"

# Verificar se banco existe
psql -h <host> -U nextraceone -c "\l" | grep nextraceone
```

---

## Rollback

Se o deploy falhar ou causar degradação, ver [ROLLBACK-RUNBOOK.md](./ROLLBACK-RUNBOOK.md).
