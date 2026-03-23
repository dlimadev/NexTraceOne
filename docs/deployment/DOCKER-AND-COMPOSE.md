# Docker e Compose — NexTraceOne

## Dockerfiles

Todos os Dockerfiles estão na raiz do repositório.
Todos usam o padrão multi-stage: `restore → publish → runtime`.

---

### `Dockerfile.apihost`

**Executável**: `NexTraceOne.ApiHost`
**Porta**: `8080`
**Runtime base**: `mcr.microsoft.com/dotnet/aspnet:10.0-alpine`

Stages:
1. `restore` — `dotnet restore` com cache de camadas
2. `publish` — `dotnet publish --configuration Release --no-restore`
3. `runtime` — copia `/app/publish` para imagem alpine enxuta

Características:
- Usuário não-root (`nextraceone`)
- Health check via `GET /live`
- Variáveis de ambiente configuráveis externamente
- Sem secrets baked-in

---

### `Dockerfile.workers`

**Executável**: `NexTraceOne.BackgroundWorkers`
**Porta**: `8081`
**Runtime base**: `mcr.microsoft.com/dotnet/aspnet:10.0-alpine`

Mesmo padrão do `apihost`.
Start period de 90s (workers demoram mais para inicializar jobs).

---

### `Dockerfile.ingestion`

**Executável**: `NexTraceOne.Ingestion.Api`
**Porta**: `8082`
**Runtime base**: `mcr.microsoft.com/dotnet/aspnet:10.0-alpine`

Entry point para integrações externas (CI/CD events, deployments, promotions).

---

### `Dockerfile.frontend`

**Executável**: Frontend React + Vite
**Porta**: `80`
**Runtime base**: `nginx:1.27-alpine`

Stages:
1. `deps` — `npm ci` (cache de `node_modules`)
2. `build` — `npm run build` (TypeScript + Vite)
3. `runtime` — copia `dist/` para nginx com configuração customizada

Build args:
- `VITE_API_BASE_URL` — URL base da API (injetada em build time)
- `VITE_INGESTION_URL` — URL da Ingestion API (injetada em build time)

Configuração nginx: `infra/nginx/nginx.frontend.conf`
- Headers de segurança
- Gzip
- Cache de assets estáticos
- SPA fallback (React Router)
- `/health` endpoint para probes

---

## Infra de suporte

### `infra/postgres/init-databases.sql`

Cria os 4 bancos lógicos na primeira inicialização do container PostgreSQL:
- `nextraceone_identity`
- `nextraceone_catalog`
- `nextraceone_operations`
- `nextraceone_ai`

Executado automaticamente via `docker-entrypoint-initdb.d`.

### `infra/nginx/nginx.frontend.conf`

Configuração nginx para servir a SPA:
- Fallback `try_files $uri /index.html` para React Router
- Security headers (X-Frame-Options, X-Content-Type-Options, etc.)
- Cache imutável para assets com hash (`.js`, `.css`)

---

## Docker Compose

### `docker-compose.yml` — Stack completa

Serviços incluídos:

| Serviço | Imagem/Build | Porta host | Propósito |
|---|---|---|---|
| `postgres` | `postgres:16-alpine` | 5432 | Banco de dados (4 DBs lógicos) |
| `clickhouse` | `clickhouse/clickhouse-server:24.8-alpine` | 8123, 9000 | Provider de observabilidade (logs, traces, métricas) |
| `otel-collector` | `otel/opentelemetry-collector-contrib:0.115.0` | 4317, 4318 | OTLP ingestion pipeline |
| `apihost` | `Dockerfile.apihost` | 8080 | API principal |
| `workers` | `Dockerfile.workers` | 8081 | Background jobs |
| `ingestion` | `Dockerfile.ingestion` | 8082 | Integrations endpoint |
| `frontend` | `Dockerfile.frontend` | 3000 | React SPA |

Dependências:
```
postgres → (health) → apihost → (health) → workers
postgres → (health) → ingestion
clickhouse → (health) → otel-collector
otel-collector → apihost, workers, ingestion
apihost → (health) → frontend
```

### `docker-compose.override.yml` — Overrides de desenvolvimento

Aplicado automaticamente em `docker compose up` quando presente.
Configura:
- `ASPNETCORE_ENVIRONMENT=Development` (seeds, logs verbose)
- `NEXTRACE_AUTO_MIGRATE=true`
- `NEXTRACE_SKIP_INTEGRITY=true`
- Connection strings para banco local

### `.env.example` — Template de configuração

Copiar para `.env` e preencher antes de usar:
```bash
cp .env.example .env
# Editar .env com valores reais
docker compose up -d
```

**Nunca commitar o arquivo `.env` com valores reais.**

---

## Como subir localmente

### Pré-requisitos
- Docker Desktop ou Docker Engine + Compose v2
- .NET 10 SDK (apenas para desenvolvimento)

### Setup inicial
```bash
# 1. Clonar o repositório
git clone https://github.com/dlimadev/NexTraceOne.git
cd NexTraceOne

# 2. Configurar variáveis de ambiente
cp .env.example .env
# Editar .env — mínimo: POSTGRES_PASSWORD, JWT_SECRET

# 3. Subir stack completa
docker compose up -d

# 4. Verificar serviços
docker compose ps
```

### Validar saúde dos serviços
```bash
# ApiHost
curl http://localhost:8080/live
curl http://localhost:8080/ready

# BackgroundWorkers
curl http://localhost:8081/live

# Ingestion
curl http://localhost:8082/live

# Frontend
curl http://localhost:3000/health

# ClickHouse
curl http://localhost:8123/?query=SELECT%201
```

### Parar e limpar
```bash
# Parar serviços (preservar dados)
docker compose down

# Parar e remover volumes (dados perdidos)
docker compose down -v
```

### Verificar configuração sem subir
```bash
docker compose config
```

---

## Desenvolvimento local (sem Docker)

Para desenvolvimento com hot-reload:
```bash
# Terminal 1: Infraestrutura
docker compose up -d postgres clickhouse otel-collector

# Terminal 2: ApiHost
cd src/platform/NexTraceOne.ApiHost
dotnet run

# Terminal 3: Frontend
cd src/frontend
npm run dev
```

---

## Como a stack se conecta

```
Browser :3000
  └→ nginx (frontend container)
       └→ /api/* proxy → ApiHost :8080
            ├→ PostgreSQL :5432 (4 databases)
            └→ OTel Collector :4317 (traces/logs/metrics → ClickHouse)

ApiHost :8080
  └→ BackgroundWorkers :8081 (indireta via PostgreSQL/EventBus)

External CI/CD
  └→ Ingestion API :8082
       └→ PostgreSQL :5432
```
