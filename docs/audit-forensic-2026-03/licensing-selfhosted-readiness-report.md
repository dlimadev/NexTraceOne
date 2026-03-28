# Relatório de Licensing e Self-Hosted Readiness — NexTraceOne
**Auditoria Forense | 28 de Março de 2026**

---

## Objetivo da Área no Contexto do Produto

Licensing e proteção de código são requisitos estratégicos do NexTraceOne enterprise. Self-hosted readiness determina se o produto pode ser instalado e operado pelo cliente sem dependência de cloud proprietária.

---

## Licensing — Estado Atual

### Módulo de Licensing

**Estado: AUSENTE**

O módulo de licensing foi removido no PR-17. Não existe nenhum DbContext de licensing ativo, nenhum enforcement de licença em runtime, nenhum mecanismo de ativação ou heartbeat.

**Evidência:**
- `docs/ROADMAP.md`: "Módulo Commercial Governance — REMOVIDO (Removed in PR-17, module no longer exists) | PR-17 — módulo não alinhado ao núcleo do produto; sem DbContext de licensing ativo"
- Nenhum módulo `licensing` em `src/modules/`
- Nenhum `LicensingDbContext` nos 24 DbContexts inventariados

### O que Existe (Base para Futuro)

| Componente | Localização | Estado |
|---|---|---|
| `AssemblyIntegrityChecker` | `BuildingBlocks.Security` | ✅ Presente |
| `NexTraceOne.IntegrityCheck: true` config | `appsettings.json` | ✅ Ativo |
| AES-256-GCM encryption | `BuildingBlocks.Security` | ✅ Presente |
| Multi-tenancy com RLS | `BuildingBlocks.Infrastructure` | ✅ Presente |
| Entitlements concept (documentado) | `docs/` | ⚠️ Documentado, não implementado |

### Capacidades de Licensing Ausentes (CLAUDE.md §17.2)

| Capacidade | Estado |
|---|---|
| Self-hosted license validation | ❌ AUSENTE |
| Online license validation | ❌ AUSENTE |
| Offline license validation | ❌ AUSENTE |
| Entitlements por capacidade | ❌ AUSENTE |
| Trial/freemium enforcement | ❌ AUSENTE |
| Backend enforcement | ❌ AUSENTE |
| Activation workflow | ❌ AUSENTE |
| Heartbeat | ❌ AUSENTE |
| Remote revocation | ❌ AUSENTE |
| Machine fingerprinting | ❌ AUSENTE |
| Assembly integrity verification | ✅ Parcialmente (hash sem assinatura) |
| Anti-tampering | ⚠️ Base presente (`AssemblyIntegrityChecker`) |
| Anti-debugging | ❌ AUSENTE |

---

## Self-Hosted Readiness — Estado Atual

### Infraestrutura de Deployment

| Componente | Estado | Evidência |
|---|---|---|
| Docker Compose (POC/avaliação) | ✅ Presente | `docker-compose.yml`, `docker-compose.override.yml` |
| Dockerfiles | ✅ 4 Dockerfiles | ApiHost, Frontend, Ingestion, Workers |
| IIS suporte (Windows) | ✅ Documentado | `DEPLOYMENT-ARCHITECTURE.md` |
| PostgreSQL 16 (base central) | ✅ Presente | 22 connection strings configuradas |
| SMTP | ✅ Config presente | `appsettings.json` |
| Scripts de DB | ✅ Presentes | `scripts/db/` — apply-migrations, backup, restore |
| Scripts de deploy | ✅ Presentes | `scripts/deploy/` — smoke-check, rollback |
| Nginx config (frontend) | ✅ Presente | `infra/nginx/nginx.frontend.conf` |
| Kubernetes | ❌ Não presente (evolução futura) | Conforme CLAUDE.md |

### Configuração Self-Hosted

| Item | Estado |
|---|---|
| Sem segredos hardcoded | ✅ `REPLACE_VIA_ENV` em todas as passwords |
| Database initialization script | ✅ `infra/postgres/init-databases.sql` |
| Environment variable documentation | ⚠️ Parcial — `ENVIRONMENT-VARIABLES.md` existe mas completude não verificada |
| Override por ambiente (prod vs. dev) | ✅ `appsettings.json` + `appsettings.Development.json` |
| Health checks para load balancer | ✅ `NexTraceHealthChecks` no BuildingBlocks |
| Smoke checks pós-deploy | ✅ `scripts/deploy/smoke-check.sh` |

### Gaps de Self-Hosted Readiness

| Gap | Impacto | Prioridade |
|---|---|---|
| Sem variáveis de ambiente documentadas completamente | Operador não sabe o que configurar | Alta |
| OpenTelemetry hardcoded para localhost | Telemetria não funciona sem override | Alta |
| Sem runbook de instalação completo (IIS + Windows) | Dificulta self-hosted em Windows | Média |
| Sem script de setup pós-instalação | Operador precisa de orientação | Média |
| Licensing ausente | Produto pode ser instalado sem licença | Estratégica |

---

## Avaliação de Risco — Licensing Ausente

| Risco | Severidade |
|---|---|
| Produto pode ser instalado e usado sem validação comercial | Alta (comercial) |
| Sem capability de entitlements — sem controlo de feature tiers | Alta |
| Sem heartbeat — produto pode rodar após expiração sem saber | Alta |
| Sem revogação remota | Alta |
| `AssemblyIntegrityChecker` presente mas sem assinatura criptográfica de código | Média (técnica) |

---

## Recomendações

### Licensing (Estratégico)
1. Definir estratégia de licensing pós-PR-17: novo módulo? biblioteca externa? SaaS licensing service?
2. Implementar enforcement mínimo: activation + heartbeat + entitlements
3. Construir sobre `AssemblyIntegrityChecker` existente para adicionar assinatura de código

### Self-Hosted (Operacional)
1. **Alta:** Criar documento completo de variáveis de ambiente obrigatórias
2. **Alta:** Garantir override de `OtlpEndpoint` documentado e com exemplo
3. **Média:** Criar runbook de instalação IIS + Windows
4. **Média:** Criar script de setup pós-instalação (apply-migrations + seed + smoke-check)
5. **Baixa:** Documentar Kubernetes como evolução futura com guia de migração do Docker Compose

---

*Data: 28 de Março de 2026*
