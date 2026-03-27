# Relatório de Licensing e Self-Hosted Readiness — NexTraceOne
**Auditoria Forense | Março 2026**

---

## 1. Licensing — Estado

### Commercial Governance (Removido no PR-17)
**Status: REMOVIDO**

O módulo de Commercial Governance que incluía licensing foi removido no PR-17 por não estar alinhado ao núcleo do produto. Não há seção de licensing no appsettings.json atual e não há DbContext de licensing ativo.

**Impacto:** Se licensing/entitlements for requisito para self-hosted enterprise:
- Não há mecanismo de activation
- Não há heartbeat/revocation
- Não há fingerprinting de instalação
- Não há enforcement de entitlements

**Evidência:** `docs/REBASELINE.md` — "~~Commercial Governance~~ — REMOVIDO (PR-17)"

### Assembly Integrity
**Status: READY**

- `AssemblyIntegrityChecker.VerifyOrThrow()` no startup
- Controlado por `NEXTRACE_SKIP_INTEGRITY` env var
- Anti-tampering básico implementado

**Evidência:** `src/building-blocks/NexTraceOne.BuildingBlocks.Security/Integrity/AssemblyIntegrityChecker.cs`

### Avaliação de Risco de Licensing

Se o produto for distribuído como self-hosted enterprise com cobrança por utilização:
- **Risco Alto**: Não há mecanismo de controlo de entitlements
- **Ação necessária**: Reimplementar licensing com abordagem diferente da removida
- **Não bloqueia**: Se o modelo de negócio for SaaS ou licença única sem enforcement

---

## 2. Self-Hosted Readiness — Avaliação

### Critérios Verificados

| Critério | Estado | Evidência |
|---|---|---|
| Sem segredos hardcoded | ✅ Sim | .env.example com placeholders obrigatórios |
| Configuração via env vars | ✅ Sim | JWT_SECRET, NEXTRACE_ENCRYPTION_KEY, POSTGRES_PASSWORD |
| Docker Compose funcional | ✅ Sim | docker-compose.yml com todos os serviços |
| IIS support declarado | Verificar | Documentado; não verificado em detalhe no código |
| Windows + Linux | ✅ .NET 10 multiplataforma | global.json, Directory.Build.props |
| Scripts de migração manual | ✅ Sim | `scripts/db/apply-migrations.sh` + `.ps1` |
| NEXTRACE_AUTO_MIGRATE=false por padrão | ✅ Sim | .env.example |
| Backup/restore scripts | ✅ Sim | `scripts/db/backup.sh`, `restore.sh`, `verify-restore.sh` |
| Sem Redis obrigatório | ✅ Sim | Não detectado no stack |
| Sem Temporal obrigatório | ✅ Sim | Quartz.NET no lugar |
| Sem OpenSearch obrigatório | ✅ Sim | PostgreSQL FTS implícito |
| Dependências open-source | ✅ Sim | PostgreSQL, Ollama, OpenTelemetry |
| SMTP support | Verificar | NotificationsDbContext existe; canal SMTP não auditado em detalhe |

---

## 3. Docker Compose — Estado

**Status: CONFIGURADO**

Serviços no docker-compose.yml:
- PostgreSQL 16
- Redis (verificar necessidade — pode ser desnecessário)
- ClickHouse
- OTEL Collector
- NexTraceOne.ApiHost
- NexTraceOne.Ingestion.Api
- NexTraceOne.BackgroundWorkers
- Frontend

**4 Dockerfiles:**
- `Dockerfile.apihost`
- `Dockerfile.frontend`
- `Dockerfile.ingestion`
- `Dockerfile.workers`

**Gap:** Redis no docker-compose — verificar se é realmente necessário ou se pode ser eliminado conforme direção arquitetural.

---

## 4. Infra Scripts — Estado

| Script | Estado | Propósito |
|---|---|---|
| `scripts/db/apply-migrations.sh` | READY | Aplicar migrações por DbContext |
| `scripts/db/apply-migrations.ps1` | READY | Windows — aplicar migrações |
| `scripts/db/backup.sh` | READY | Backup automático |
| `scripts/db/restore.sh` | READY | Restore de backup |
| `scripts/db/restore-all.sh` | READY | Full restore |
| `scripts/db/verify-restore.sh` | READY | Validação de integridade do restore |
| `scripts/deploy/rollback.sh` | READY | Rollback de deployment |
| `scripts/deploy/smoke-check.sh` | READY | Validação pós-deploy |
| `scripts/quality/check-no-demo-artifacts.sh` | READY | Anti-demo guardrail |
| `scripts/observability/verify-pipeline.sh` | Verificar | Health check do pipeline de telemetria |
| `scripts/performance/smoke-performance.sh` | Verificar | Baseline de performance |

---

## 5. Recomendações

| Ação | Prioridade | Justificativa |
|---|---|---|
| Definir estratégia de licensing para self-hosted | Alta | Módulo removido, sem substituto |
| Verificar necessidade de Redis no docker-compose | Média | Potencial dependência desnecessária |
| Documentar suporte IIS com exemplos de configuração | Média | Self-hosted Windows enterprise |
| Verificar suporte SMTP no módulo Notifications | Média | Canal obrigatório para notificações on-prem |
| Criar guia de configuração mínima para self-hosted | Alta | Facilita adoção enterprise |
