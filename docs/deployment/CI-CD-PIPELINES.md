# CI/CD Pipelines — NexTraceOne

## Workflows criados

Todos os workflows estão em `.github/workflows/`.

---

## `ci.yml` — Pipeline de Integração Contínua

### Gatilhos
- `push` para `main`, `develop`, `release/**`
- `pull_request` para `main`, `develop`

### Jobs e dependências

```
validate
  ├── build-backend
  │   ├── test-backend-unit
  │   └── test-backend-integration
  └── build-frontend
      └── test-frontend
```

### Job: `validate`
- Executa `bash scripts/quality/check-no-demo-artifacts.sh`
- Bloqueia se houver artefatos demo/preview/simulados

### Job: `build-backend`
- `dotnet restore NexTraceOne.sln`
- `dotnet build NexTraceOne.sln --configuration Release`
- Cache NuGet por hash de `Directory.Packages.props`

### Job: `test-backend-unit`
- `dotnet test` com filtro `FullyQualifiedName!~IntegrationTests&FullyQualifiedName!~E2E`
- Publica resultados `.trx` como artefato

### Job: `test-backend-integration`
- Requer PostgreSQL 16 via `services:`
- Executa `tests/platform/NexTraceOne.IntegrationTests/`
- Publica resultados `.trx` como artefato

### Job: `build-frontend`
- `npm ci` com cache por `package-lock.json`
- `npx tsc --noEmit` (typecheck)
- `npm run lint`
- `npm run build`
- Publica `dist/` como artefato

### Job: `test-frontend`
- `npm test` (vitest run)
- Publica resultados como artefato

### Variáveis utilizadas
| Variável | Valor |
|---|---|
| `DOTNET_VERSION` | `10.0.x` |
| `NODE_VERSION` | `22` |
| `DOTNET_NOLOGO` | `true` |
| `DOTNET_SKIP_FIRST_TIME_EXPERIENCE` | `true` |

---

## `security.yml` — Pipeline de Segurança

### Gatilhos
- `push` para `main`
- `pull_request` para `main`, `develop`
- `schedule`: segundas-feiras às 02:00 UTC

### Jobs

### Job: `dependency-scan`
- `dotnet list package --vulnerable --include-transitive`
- Falha se vulnerabilidades encontradas

### Job: `frontend-audit`
- `npm audit --audit-level=high`
- Falha em vulnerabilidades HIGH ou CRITICAL

### Job: `codeql-analysis`
- Matrix: `csharp`, `javascript-typescript`
- Usa `github/codeql-action`
- Queries `security-and-quality`
- Resultados visíveis na aba Security do repositório

### Job: `docker-scan` (apenas `main`)
- Matrix: `apihost`, `workers`, `ingestion`, `frontend`
- Usa Trivy (`aquasecurity/trivy-action`)
- Severity: `CRITICAL,HIGH`
- Resultados SARIF enviados para Security tab

---

## `staging.yml` — Pipeline de Entrega Staging

### Gatilhos
- `push` para `main` (após CI)
- `workflow_dispatch` com inputs opcionais

### Inputs manuais
| Input | Padrão | Descrição |
|---|---|---|
| `run_migrations` | `true` | Aplicar migrations no staging |
| `skip_smoke` | `false` | Pular smoke checks |

### Jobs

### Job: `build-images`
- Matrix: `apihost`, `workers`, `ingestion`, `frontend`
- Tags: `<SHORT_SHA>` + `staging`
- Registry: `ghcr.io` (GitHub Container Registry)
- Cache de layer usando `cache-from: type=registry`

### Job: `run-migrations`
- Requer environment `staging`
- Usa `scripts/db/apply-migrations.sh`
- Lê connection strings de GitHub Secrets

### Job: `smoke-check`
- `GET /live` → espera `{"status":"Healthy"}`
- `GET /ready` → espera `{"status":"Healthy"}`
- Frontend `HTTP 200`
- Gera deployment summary no GitHub Actions

---

## `e2e.yml` — Testes E2E (Nightly)

### Gatilhos
- `schedule`: diariamente às 03:00 UTC
- `workflow_dispatch` com `base_url` opcional

### Comportamento
- Sobe stack local via `docker compose`
- Executa `npx playwright test`
- Publica relatório HTML como artefato

---

## Secrets e Variables necessários

### GitHub Secrets (por environment `staging`)
| Secret | Descrição |
|---|---|
| `STAGING_CONN_IDENTITY` | Connection string `nextraceone_identity` |
| `STAGING_CONN_CATALOG` | Connection string `nextraceone_catalog` |
| `STAGING_CONN_OPERATIONS` | Connection string `nextraceone_operations` |
| `STAGING_CONN_AI` | Connection string `nextraceone_ai` |

### GitHub Variables (nível repositório)
| Variable | Descrição |
|---|---|
| `STAGING_APIHOST_URL` | URL base do ApiHost em staging |
| `STAGING_FRONTEND_URL` | URL pública do Frontend em staging |
| `STAGING_INGESTION_URL` | URL base do Ingestion API em staging |

---

## Artefatos publicados pelo pipeline

| Artefato | Job origem | Retenção |
|---|---|---|
| `unit-test-results` | `test-backend-unit` | padrão |
| `integration-test-results` | `test-backend-integration` | padrão |
| `frontend-dist` | `build-frontend` | 7 dias |
| `frontend-test-results` | `test-frontend` | padrão |
| `playwright-report` | `e2e` | 14 dias |

---

## Estratégia de tagging de imagens

| Tag | Quando aplicada | Uso |
|---|---|---|
| `<8-char-SHA>` | Todo push para main | Rastreabilidade e rollback |
| `staging` | Todo push para main | "Latest staging" |
| `latest` | Não utilizada automaticamente | Manual/releases futuras |

Exemplo: `ghcr.io/dlimadev/nextraceone-apihost:a1b2c3d4`
