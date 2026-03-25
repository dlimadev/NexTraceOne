# Relatório de Licensing, Protecção de Código e Self-Hosted Readiness — NexTraceOne

**Data:** 25 de março de 2026

---

## 1. Objectivo

Auditar o estado de licensing, entitlements, protecção de código e readiness para deployment self-hosted/on-prem.

---

## 2. Módulo de Licensing — MISSING

### 2.1 Estado

**Resultado da auditoria:** Não existe módulo de Licensing no repositório.

Pesquisa realizada em:
- `src/modules/` — nenhuma pasta `licensing` encontrada
- Solution file — nenhum projecto de licensing encontrado
- Entidades em módulos existentes — sem entidade `License`, `Entitlement`, `Subscription`

**Impacto:** O produto não tem mecanismo de licensing, activação, validação de entitlements ou controlo de features por tier.

---

## 3. Assembly Integrity — PARTIAL

### 3.1 Implementação

**Ficheiro:** `src/building-blocks/NexTraceOne.BuildingBlocks.Security/Integrity/AssemblyIntegrityChecker.cs`

**O que existe:**
- Verificação de hash de assemblies para detectar tampering
- Pode ser desactivado via `NEXTRACE_SKIP_INTEGRITY=true`

**Problema crítico:** O workflow `.github/workflows/security.yml` usa `NEXTRACE_SKIP_INTEGRITY=true` durante builds, o que invalida o propósito da protecção.

### 3.2 Anti-debug, Anti-tamper

**Estado:** Não encontrado explicitamente além do `AssemblyIntegrityChecker`.

---

## 4. Encriptação de Campos

### 4.1 Implementação

**Ficheiro:** `src/building-blocks/NexTraceOne.BuildingBlocks.Security/Encryption/AesGcmEncryptor.cs`

**Estado:** PARTIAL

- AES-256-GCM com nonce aleatório — implementação correcta
- `[EncryptedField]` attribute para marcação transparente
- **Problema:** Fallback key hardcoded para non-Production — ver security report

---

## 5. Self-Hosted Readiness

### 5.1 Docker Compose

**Ficheiro:** `docker-compose.yml`

**Estado: READY para POC/Avaliação**

Inclui:
- PostgreSQL 16 (open-source, sem licença proprietária)
- ClickHouse 24.8 (open-source Apache 2.0)
- OpenTelemetry Collector (Apache 2.0)
- ApiHost, BackgroundWorkers, Ingestion.Api, Frontend

**Positivo:** Nenhum componente proprietário na stack base.

### 5.2 IIS / Windows Support

**Scripts PowerShell:**
- `scripts/db/apply-migrations.ps1` — suporte explícito a Windows

**Estado:** PARTIAL — scripts existem; documentação de IIS não verificada em detalhe

### 5.3 Linux Support

- `scripts/db/apply-migrations.sh` — bash scripts para Linux
- Dockerfiles baseados em Alpine (Linux)

**Estado:** READY

### 5.4 SMTP Support

**Ficheiro:** `appsettings.json` — secção SMTP esperada

**Estado:** Configuração SMTP não encontrada explicitamente no appsettings auditado. NotificationsDbContext existe mas sem entidade de canal SMTP.

**Lacuna:** Sem evidência de integração SMTP funcional

### 5.5 Dependências Problemáticas para Self-Hosted

| Dependência | Tipo | Alternativa |
|------------|------|-------------|
| PostgreSQL 16 | Open-source | — (opção correcta) |
| ClickHouse 24.8 | Open-source | — (opção correcta) |
| OpenTelemetry | Open-source | — (opção correcta) |
| Redis | NÃO USADO | — (correcto — sem Redis) |
| OpenSearch | NÃO USADO | — (correcto — sem OpenSearch) |
| Temporal | NÃO USADO | — (correcto — usa Quartz) |

**Verificação de restrições do produto:**
- ✅ Sem Redis (como especificado no target)
- ✅ Sem OpenSearch (PostgreSQL FTS suficiente para fase actual)
- ✅ Sem Temporal (Quartz.NET + PostgreSQL)
- ✅ Sem dependências cloud proprietárias obrigatórias

---

## 6. Entitlements e Feature Gating

### 6.1 Frontend

**Ficheiro:** `src/frontend/src/releaseScope.ts`

```typescript
export const finalProductionIncludedRoutePrefixes = [
  '/contracts', '/changes', '/releases', '/operations', // ...34 rotas
]
export const finalProductionExcludedRoutePrefixes = []
```

**Estado:** Feature gating por rota existe mas não está ligado a entitlements de licensing.

### 6.2 Backend

**Estado:** Sem mecanismo de entitlements no backend. Sem validação de features habilitadas por licença.

---

## 7. Heartbeat / Online Licensing

**Estado:** MISSING — sem mecanismo de heartbeat ou validação online de licença

---

## 8. Self-Hosted Configuration

### 8.1 Positivo

- `.env.example` cobre todas as variáveis necessárias
- `NEXTRACE_AUTO_MIGRATE` para controlar migrations automáticas
- `NEXTRACE_SKIP_INTEGRITY` para CI/CD (problemático em produção)
- Scripts de backup e restore para PostgreSQL

### 8.2 Falta

- Documentação de instalação em IIS completa
- Guia de sizing para ambientes self-hosted
- Checklist de hardening de segurança para produção
- Guia de backup/restore documentado
- Processo de actualização/upgrade

---

## 9. Kubernetes Readiness

**Target:** "evolução posterior para Kubernetes"

**Estado actual:** Preparação básica via Docker Compose. Sem Helm charts, sem Kubernetes manifests.

**Readiness:** LOW para Kubernetes — seria necessário:
- Helm charts ou Kubernetes manifests
- ConfigMaps e Secrets para configuração
- PersistentVolumeClaims para PostgreSQL e ClickHouse
- Ingress controller configurado
- Horizontal Pod Autoscaler

---

## 10. Resumo

| Área | Estado | Prioridade |
|------|--------|------------|
| Módulo Licensing | MISSING | P1 |
| Assembly Integrity | PARTIAL | P2 |
| Self-Hosted Docker | READY | — |
| IIS/Windows | PARTIAL | P2 |
| SMTP | PARTIAL | P2 |
| Anti-tamper | PARTIAL | P3 |
| Heartbeat/Activation | MISSING | P1 |
| Feature Entitlements | MISSING | P1 |
| Kubernetes | NOT STARTED | P3 |
| Kubernetes Readiness | LOW | P3 |

---

## 11. Recomendações

| Prioridade | Acção |
|-----------|-------|
| P1 | Criar módulo Licensing com entidades: License, Entitlement, ActivationToken |
| P1 | Implementar feature gating backend ligado a entitlements |
| P1 | Implementar heartbeat/activation (online e offline) |
| P2 | Completar documentação de instalação IIS |
| P2 | Implementar integração SMTP no módulo Notifications |
| P2 | Corrigir NEXTRACE_SKIP_INTEGRITY no pipeline de segurança |
| P2 | Criar guia de hardening para ambientes self-hosted |
| P3 | Criar Helm charts para deployment Kubernetes |
| P3 | Implementar anti-debug completo |
