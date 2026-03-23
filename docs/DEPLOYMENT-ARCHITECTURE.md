# DEPLOYMENT-ARCHITECTURE.md — NexTraceOne

> **Data:** Março 2026
> **Scope:** Arquitetura de deploy, componentes, configuração e operacionalização.
> **Princípio:** Self-hosted enterprise. On-premise first. Integrações externas opcionais.

---

## Visão Geral

O NexTraceOne é uma plataforma **sovereign** — designed para operar inteiramente
dentro do perímetro controlado do cliente, sem dependências de serviços cloud externos
obrigatórios. Toda a observabilidade, persistência e inteligência funciona on-premise.

```
┌─────────────────────────────────────────────────────────────────────┐
│                        NexTraceOne Platform                         │
│                                                                     │
│  ┌─────────────┐   ┌──────────────────┐   ┌──────────────────────┐ │
│  │   Frontend  │   │   NexTraceOne    │   │  NexTraceOne.        │ │
│  │  (React SPA)│──▶│   ApiHost        │   │  Ingestion.Api       │ │
│  │  Port 5173  │   │  Port 8080/8443  │   │  Port 8090           │ │
│  └─────────────┘   └────────┬─────────┘   └──────────┬───────────┘ │
│                             │                          │            │
│         ┌───────────────────┼──────────────────────────┘            │
│         ▼                   ▼                                       │
│  ┌─────────────────────────────────────────────────────────────┐   │
│  │                      PostgreSQL                             │   │
│  │  Bases: identity · catalog · changegovernance ·            │   │
│  │         operationalintelligence · audit · aiknowledge       │   │
│  └─────────────────────────────────────────────────────────────┘   │
│                                                                     │
│  ┌──────────────────┐   ┌──────────┐   ┌────────────────────────┐  │
│  │ BackgroundWorkers│   │  Ollama  │   │  Observability Stack   │  │
│  │  (Quartz jobs)   │   │  (LLM)   │   │  OTel Collector        │  │
│  └──────────────────┘   └──────────┘   │  ClickHouse / Elastic  │  │
│                                         └────────────────────────┘  │
└─────────────────────────────────────────────────────────────────────┘
```

---

## Componentes

### NexTraceOne.ApiHost

- **Papel:** Host principal da plataforma — expõe todas as APIs de domínio.
- **Porta:** 8080 (HTTP), 8443 (HTTPS opcionalmente via proxy reverso)
- **Framework:** ASP.NET Core 8 Minimal API
- **Módulos registados:** IdentityAccess, Catalog, Contracts, ChangeGovernance,
  ChangeWorkflow, RulesetGovernance, Promotion, AuditCompliance, DeveloperPortal,
  Governance, OperationalIntelligence, AIKnowledge
- **Inicialização:** aplica EF Core migrations automáticas no startup (`ApplyDatabaseMigrationsAsync`)
- **Health endpoints:** `/health`, `/ready`, `/live`

### NexTraceOne.BackgroundWorkers

- **Papel:** Workers Quartz.NET para jobs assíncronos.
- **Jobs ativos:**
  - `OutboxProcessorJob` — processa eventos do Outbox Pattern a cada 5 segundos
  - `IdentityExpirationJob` — expira sessões JIT, break glass, delegações a cada 60 segundos
- **Sem HTTP** — process standalone sem exposição de portas

### NexTraceOne.Ingestion.Api

- **Papel:** Endpoint receptor de eventos de ingestão (deployments, CI/CD webhooks).
- **Porta:** 8090
- **Autenticação:** JWT Bearer (mesmo issuer do ApiHost) + API Key para sistemas externos

### Frontend (React SPA)

- **Tecnologia:** React 19 + Vite + TypeScript
- **Porta dev:** 5173
- **Build prod:** arquivos estáticos em `dist/` — servir via nginx ou CDN on-premise
- **Fontes:** Inter + JetBrains Mono servidas localmente via `@fontsource` (sem CDN externo)

### PostgreSQL

- **Versão recomendada:** 15+
- **Bases de dados:** 6 bases separadas por contexto bounded (ver `ENVIRONMENT-VARIABLES.md`)
- **RLS:** Row-Level Security implementado via EF Core interceptors (não via PostgreSQL nativo)
- **Migrations:** aplicadas automaticamente no startup via `dotnet ef database update`

### Ollama (AI Runtime — Opcional)

- **Papel:** Provider de LLM local para o AI Assistant
- **Porta:** 11434
- **Modelos testados:** `deepseek-r1:1.5b` (padrão)
- **Alternativa:** OpenAI (configurável via `AiRuntime__OpenAI__*`)

### Stack de Observabilidade (Configurável)

| Componente | Porta | Papel |
|-----------|-------|-------|
| OTel Collector | 4317 (gRPC), 4318 (HTTP) | Receptor e pipeline de traces, logs e métricas |
| ClickHouse | 8123 (HTTP), 9000 (native) | Provider de observabilidade (default) — logs, traces, métricas |
| Elastic | Configurável | Provider de observabilidade alternativo (integração com stack existente) |

> A stack de observabilidade é **configurável** — o provider pode ser ClickHouse (default, local)
> ou Elastic (integração com stack existente). O ApiHost funciona sem collector configurado;
> traces e métricas são silenciosamente descartados sem impacto na disponibilidade.
> Ver `docs/observability/` para documentação completa.

---

## Docker Compose

O `docker-compose.yml` na raiz do projeto cobre o ambiente de desenvolvimento completo.

```bash
# Subir todos os serviços
docker compose up -d

# Apenas PostgreSQL (mínimo para desenvolvimento backend)
docker compose up -d postgres

# Com stack de observabilidade (ClickHouse + OTel Collector)
docker compose up -d postgres clickhouse otel-collector
```

---

## Deploy em Produção (Recomendado)

### Topologia mínima

```
                  [Proxy Reverso: nginx/Traefik]
                          |
           ┌──────────────┼──────────────┐
           ▼              ▼              ▼
    [ApiHost:8080]  [Ingestion:8090]  [Frontend: static]
           |              |
           └──────────────┤
                          ▼
                   [PostgreSQL:5432]
```

### Usando Docker

```dockerfile
# Build do backend
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "NexTraceOne.ApiHost.dll"]
```

### Kubernetes (Opcional)

Manifestos Helm não incluídos nesta fase. Para deploy em Kubernetes:
1. Criar `ConfigMap` com `appsettings.Production.json`
2. Criar `Secret` com `NEXTRACE_ENCRYPTION_KEY` e `Jwt__Secret`
3. Apontar connection strings para o PostgreSQL do cluster
4. Configurar `livenessProbe`/`readinessProbe` nos endpoints de health
5. Configurar `HorizontalPodAutoscaler` se necessário (stateless — safe to scale)

---

## Sequência de Startup

1. `AssemblyIntegrityChecker.VerifyOrThrow()` — valida assinaturas de assemblies
2. `ValidateStartupConfiguration()` — valida variáveis obrigatórias
3. `ApplyDatabaseMigrationsAsync()` — aplica todas as migrations pendentes
4. `SeedDevelopmentDataAsync()` — seed idempotente (apenas em Development)
5. Middlewares registados na ordem: Compression → HTTPS Redirect → Rate Limiter →
   Security Headers → Exception Handler → Authentication → Tenant Resolution → Authorization
6. Endpoints mapeados via assembly scanning (`MapAllModuleEndpoints`)
7. Health checks registados (`/health`, `/ready`, `/live`)

---

## Segurança no Deploy

### Checklist obrigatório

- [ ] `NEXTRACE_ENCRYPTION_KEY` gerado aleatoriamente (`openssl rand -base64 32`)
- [ ] `Jwt__Secret` gerado aleatoriamente (`openssl rand -base64 48`)
- [ ] Passwords das connection strings únicas e rotativas
- [ ] CORS configurado com origin real do frontend (sem wildcards)
- [ ] HTTPS ativado em produção (proxy reverso recomendado)
- [ ] Endpoints de health não expostos publicamente (ou via rede interna apenas)
- [ ] Logs centralizados antes de go-live

### Headers de Segurança (configurados por padrão)

O ApiHost configura automaticamente no startup via `UseSecurityHeaders()`:

| Header | Valor |
|--------|-------|
| `Content-Security-Policy` | `default-src 'none'; frame-ancestors 'none'` (API) |
| `X-Frame-Options` | `DENY` |
| `X-Content-Type-Options` | `nosniff` |
| `Referrer-Policy` | `strict-origin-when-cross-origin` |
| `Strict-Transport-Security` | `max-age=31536000; includeSubDomains` |

---

## Observabilidade Operacional

### Logs

- Formato: JSON estruturado via Serilog
- Destinos padrão: Console + ficheiro rotativo em `logs/nextraceone-YYYY-MM-DD.log`
- Retenção de ficheiros: 30 dias
- Para centralização: logs fluem via OTLP para o provider configurado (ClickHouse ou Elastic)

### Traces

- Protocolo: OTLP gRPC para o collector configurado em `OpenTelemetry__Endpoint`
- Activity sources: Commands, Queries, Events, ExternalHttp, TelemetryPipeline
- Instrumentação automática: ASP.NET Core + HttpClient

### Metrics

- Protocolo: OTLP gRPC (mesmo endpoint dos traces)
- Instrumentação: ASP.NET Core + HttpClient + .NET Runtime

### Health Checks implementados

| Check | Tag | Descrição |
|-------|-----|-----------|
| PostgreSQL connectivity | `ready` | Verifica conexão ao DB principal |
| Self check | `live` | Processo em execução |

---

*Documento gerado como parte da Fase 8 — Segurança e Prontidão Operacional.*
*Última atualização: Março 2026.*
