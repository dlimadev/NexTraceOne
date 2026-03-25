# PARTE 11 — Security, Permissions & Capabilities Review

> **Módulo:** AI & Knowledge (07)
> **Data:** 2026-03-25
> **Prompt:** N13

---

## 1. Permissões registadas

### 1.1 RolePermissionCatalog.cs

| Permissão | Admin | PowerUser | Developer | Viewer |
|-----------|-------|-----------|-----------|--------|
| `ai:assistant:read` | ✅ | ✅ | ✅ | ❌ |
| `ai:assistant:write` | ✅ | ✅ | ✅ | ❌ |
| `ai:governance:read` | ✅ | ✅ | ❌ | ❌ |
| `ai:governance:write` | ✅ | ❌ | ❌ | ❌ |
| `ai:ide:read` | ✅ | ✅ | ❌ | ❌ |
| `ai:ide:write` | ✅ | ❌ | ❌ | ❌ |
| `ai:runtime:write` | ❌ | ✅ | ❌ | ❌ |
| `platform:admin:read` | ✅ | ❌ | ❌ | ❌ |

### 1.2 Análise de gaps

| # | Gap | Severidade |
|---|-----|------------|
| SEC-01 | Falta `ai:agents:execute` — execução de agents usa `ai:assistant:write` genérico | 🟠 MÉDIA |
| SEC-02 | Falta `ai:agents:manage` — CRUD de agents usa `ai:assistant:write` genérico | 🟠 MÉDIA |
| SEC-03 | Falta `ai:external:read/write` — acesso a external AI não diferenciado | 🟡 MÉDIA |
| SEC-04 | Falta permissão granular por tool de agent | 🟠 MÉDIA |
| SEC-05 | Falta permissão de exportação de dados de IA | 🟡 BAIXA |
| SEC-06 | `ai:runtime:write` só para PowerUser — Developer deveria ter para análise | 🟡 BAIXA |

---

## 2. Permissões por página

| Página | Permissão | Guard | Estado |
|--------|-----------|-------|--------|
| AI Assistant | `ai:assistant:read` | ProtectedRoute | ✅ OK |
| AI Agents | `ai:assistant:read` | ProtectedRoute | ⚠️ Deveria ser `ai:agents:read` |
| Agent Detail | `ai:assistant:read` | ProtectedRoute | ⚠️ Deveria diferenciar read/execute |
| Model Registry | `ai:governance:read` | ProtectedRoute | ✅ OK |
| AI Policies | `ai:governance:read` | ProtectedRoute | ✅ OK |
| AI Routing | `ai:governance:read` | ProtectedRoute | ✅ OK |
| IDE Integrations | `ai:governance:read` | ProtectedRoute | ⚠️ Deveria ser `ai:ide:read` |
| Token Budgets | `ai:governance:read` | ProtectedRoute | ✅ OK |
| AI Audit | `ai:governance:read` | ProtectedRoute | ✅ OK |
| AI Analysis | `ai:runtime:write` | ProtectedRoute | ⚠️ Demasiado restritivo |
| AI Config (admin) | `platform:admin:read` | ProtectedRoute | ✅ OK |

---

## 3. Enforcement no backend

### 3.1 Endpoints de Governance

| Endpoint | Permissão esperada | Enforcement verificado |
|----------|-------------------|----------------------|
| GET /api/v1/ai/models | ai:governance:read | ⚠️ Verificar RequirePermission |
| POST /api/v1/ai/models | ai:governance:write | ⚠️ Verificar |
| POST /api/v1/ai/agents/{id}/execute | ai:assistant:write | ⚠️ Insuficiente — deveria ser ai:agents:execute |
| POST /api/v1/ai/assistant/chat | ai:assistant:write | ⚠️ Verificar |

### 3.2 Authorization service

**Interface:** `IAiModelAuthorizationService` — usa `AIAccessPolicy` para determinar se um utilizador pode usar um modelo.

**Estado:** ✅ Existe mecanismo de scope-based authorization para modelos.

**Gap:** Não existe equivalente para agents (`IAiAgentAuthorizationService`).

---

## 4. Capabilities de IA

### 4.1 Modelo de capabilities

O módulo usa `AIAccessPolicy` com scope (Role, User, Team, Tenant) para controlar acesso a modelos. Isto é um bom modelo base.

### 4.2 Gaps de capabilities

| # | Gap | Descrição |
|---|-----|-----------|
| CAP-01 | Sem capability por tool | Tools de agents não têm controlo individual |
| CAP-02 | Sem capability por agent | Agents não têm política de acesso dedicada |
| CAP-03 | Sem rate limiting por capability | Apenas quotas globais de tokens |
| CAP-04 | Sem capability de exportação | Dados de IA exportáveis sem controlo |
| CAP-05 | Sem capability de external AI | Acesso a providers externos não diferenciado |

---

## 5. Ações sensíveis

| Ação | Risco | Controlo atual | Correção necessária |
|------|-------|---------------|-------------------|
| Executar agent | 🔴 ALTO — pode invocar tools | `ai:assistant:write` genérico | Adicionar `ai:agents:execute` + tool validation |
| Mudar provider/modelo | 🟠 MÉDIO — afeta toda a plataforma | `ai:governance:write` (Admin only) | ✅ OK |
| Editar definição de agent | 🟠 MÉDIO — system prompt + tools | `ai:assistant:write` | Adicionar `ai:agents:manage` |
| Consultar histórico de outros utilizadores | 🟠 MÉDIO — PII | Não diferenciado | Adicionar scope check por UserId |
| Usar capacidades externas | 🟠 MÉDIO — custo + dados saem | Não diferenciado | Adicionar `ai:external:write` |
| Aprovar knowledge capture | 🟡 MÉDIO — valida conhecimento | Não diferenciado | Adicionar workflow de aprovação |
| Alterar orçamento | 🟠 MÉDIO — impacto financeiro | `ai:governance:write` (Admin only) | ✅ OK |
| Exportar dados de IA | 🟡 MÉDIO — dados sensíveis | Não existe | Implementar com permissão dedicada |

---

## 6. Tenant isolation

| Aspecto | Estado |
|---------|--------|
| TenantId em todas as entidades | ✅ Via NexTraceDbContextBase |
| RLS (Row Level Security) | ✅ Via NexTraceDbContextBase |
| Cross-tenant query prevention | ✅ Via RLS filter |
| Tenant-specific models/policies | ⚠️ Schema suporta — enforcement a validar |
| Tenant-specific agents | ⚠️ Schema suporta (OwnershipType.Tenant) — enforcement a validar |

---

## 7. Auditoria de ações críticas

| Ação | Audit trail | Estado |
|------|------------|--------|
| Mensagem de chat enviada | `AiMessage` persistido | ✅ |
| Agent executado | `AiAgentExecution` persistido | ⚠️ Parcial |
| Token usage | `AIUsageEntry` + `AiTokenUsageLedger` | ⚠️ Parcial |
| Modelo criado/alterado | AuditColumns (CreatedAt, UpdatedAt) | ✅ |
| Provider criado/alterado | AuditColumns | ✅ |
| Política criada/alterada | AuditColumns | ✅ |
| External AI query | `AiExternalInferenceRecord` | ⚠️ Parcial |
| Knowledge approved/rejected | AuditColumns | ⚠️ Sem event publicado |

**Gap principal:** O módulo não publica domain events para o módulo Audit & Compliance. Toda a auditoria é interna (AuditColumns + entidades de log).

---

## 8. Backlog de correções de segurança

| # | Item | Prioridade | Esforço |
|---|------|------------|---------|
| S-01 | Adicionar `ai:agents:read`, `ai:agents:execute`, `ai:agents:manage` a RolePermissionCatalog | P1 | 4h |
| S-02 | Adicionar `ai:external:read`, `ai:external:write` a RolePermissionCatalog | P2 | 2h |
| S-03 | Implementar `IAiAgentAuthorizationService` | P1 | 8h |
| S-04 | Adicionar scope check por UserId para histórico de conversas | P1 | 4h |
| S-05 | Verificar encriptação de ApiKey em AiProvider | P0 | 2h |
| S-06 | Implementar tool permission validation em agent execution | P1 | 8h |
| S-07 | Publicar domain events para Audit & Compliance | P2 | 8h |
| S-08 | Implementar rate limiting por utilizador | P2 | 8h |
| S-09 | Corrigir permissões em rotas do frontend (agents, IDE) | P2 | 2h |
| S-10 | Adicionar enforcement de quota antes de execução de chat/agent | P1 | 4h |
| S-11 | Validar tenant isolation em todas as queries | P1 | 4h |

**Total estimado:** ~54h (~7 dias de trabalho)
