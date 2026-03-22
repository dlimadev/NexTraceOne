# Fase 7 — Entregabilidade e Deploy do NexTraceOne

## Escopo executado

A Fase 7 transforma o NexTraceOne de um produto com código maduro mas sem pipeline de entrega, em uma plataforma com **entregabilidade real e reproduzível**.

## O que estava ausente antes da Fase 7

| Item | Estado anterior |
|---|---|
| GitHub Actions workflows | Ausente (apenas `.github/copilot-instructions.md`) |
| Dockerfiles | Ausentes (nenhum para nenhum executável) |
| `docker-compose.yml` completo | Ausente (apenas telemetria) |
| Script de migrations para CI/CD | Ausente |
| Documentação de deploy | Ausente |
| Runbooks operacionais | Ausentes |

## O que foi implementado

### 1. Pipelines CI/CD (`./github/workflows/`)

| Workflow | Trigger | Propósito |
|---|---|---|
| `ci.yml` | PR + push main/develop | Build + test backend + build + test frontend |
| `security.yml` | PR + push main + semanal | NuGet scan, npm audit, CodeQL, Trivy |
| `staging.yml` | Push main + manual | Build imagens, push registry, migrations, smoke checks |
| `e2e.yml` | Nightly + manual | Playwright E2E (pesado, não bloqueia PR) |

### 2. Containerização (`Dockerfile.*`)

| Artefato | Executável | Base runtime |
|---|---|---|
| `Dockerfile.apihost` | `NexTraceOne.ApiHost` | `mcr.microsoft.com/dotnet/aspnet:10.0-alpine` |
| `Dockerfile.workers` | `NexTraceOne.BackgroundWorkers` | `mcr.microsoft.com/dotnet/aspnet:10.0-alpine` |
| `Dockerfile.ingestion` | `NexTraceOne.Ingestion.Api` | `mcr.microsoft.com/dotnet/aspnet:10.0-alpine` |
| `Dockerfile.frontend` | Frontend React/Vite | `nginx:1.27-alpine` |

Todos são multi-stage builds: `restore → publish → runtime`.

### 3. Docker Compose completo

| Ficheiro | Propósito |
|---|---|
| `docker-compose.yml` | Stack completa: postgres + app services + observabilidade |
| `docker-compose.override.yml` | Overrides para desenvolvimento local |
| `.env.example` | Template seguro de variáveis de ambiente |
| `infra/postgres/init-databases.sql` | Cria os 4 bancos lógicos |
| `infra/nginx/nginx.frontend.conf` | Configuração nginx do frontend |

### 4. Estratégia de migrations

| Ficheiro | Propósito |
|---|---|
| `scripts/db/apply-migrations.sh` | Script Bash para CI/CD e operação |
| `scripts/db/apply-migrations.ps1` | Script PowerShell para Windows |

O comportamento por ambiente já estava implementado em `WebApplicationExtensions.cs`:
- **Production**: `NEXTRACE_AUTO_MIGRATE=true` lança exceção — bloqueado
- **Development**: auto-migrate sempre ativo
- **Staging**: opt-in via `NEXTRACE_AUTO_MIGRATE=true`

### 5. Infraestrutura de suporte

- `infra/postgres/init-databases.sql` — provisionamento dos 4 bancos
- `infra/nginx/nginx.frontend.conf` — configuração nginx SPA

## Componentes containerizados

```
NexTraceOne.ApiHost         → porta 8080 → imagem nextraceone/apihost
NexTraceOne.BackgroundWorkers → porta 8081 → imagem nextraceone/workers
NexTraceOne.Ingestion.Api   → porta 8082 → imagem nextraceone/ingestion
Frontend (React+Vite+nginx) → porta 3000 → imagem nextraceone/frontend
```

## Mudança no readiness de entrega

| Dimensão | Antes | Depois |
|---|---|---|
| Build automatizado | ❌ | ✅ CI pipeline |
| Tests automatizados | ❌ | ✅ Unit + integration no CI |
| Containerização | ❌ | ✅ 4 imagens multi-stage |
| Stack local reproduzível | ❌ | ✅ `docker compose up` |
| Migrations controladas | ⚠️ (partial) | ✅ Scripts + docs |
| Security scanning | ❌ | ✅ CodeQL + Trivy + npm audit |
| Runbooks operacionais | ❌ | ✅ Deploy + Rollback + Validação |
| Documentação de deploy | ❌ | ✅ Completa |

## Próximos passos recomendados

1. Configurar os GitHub Secrets de staging (`STAGING_CONN_*`)
2. Configurar as GitHub Variables (`STAGING_APIHOST_URL`, `STAGING_FRONTEND_URL`)
3. Validar `docker compose up` com `.env` real em ambiente staging
4. Executar migrations em staging pela primeira vez via pipeline
5. Ativar branch protection com CI obrigatório em `main`
