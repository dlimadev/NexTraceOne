# Integrations — Security and Permissions Review

> **Module:** Integrations (12)  
> **Date:** 2026-03-25  
> **Status:** Review completo

---

## 1. Permissões por página

| Página | Rota | Permissão necessária | Guard frontend | Estado |
|--------|------|---------------------|---------------|--------|
| IntegrationHubPage | `/integrations` | `integrations:read` | ✅ Verificado | ✅ |
| ConnectorDetailPage | `/integrations/:id` | `integrations:read` | ✅ Verificado | ✅ |
| IngestionExecutionsPage | `/integrations/executions` | `integrations:read` | ✅ Verificado | ✅ |
| IngestionFreshnessPage | `/integrations/freshness` | `integrations:read` | ✅ Verificado | ✅ |

---

## 2. Permissões por acção

| Acção | Permissão actual | Roles com acesso | Estado |
|-------|-----------------|-----------------|--------|
| Listar conectores | `integrations:read` | PlatformAdmin, TechLead, Developer | ✅ |
| Ver detalhe de conector | `integrations:read` | PlatformAdmin, TechLead, Developer | ✅ |
| Listar fontes | `integrations:read` | PlatformAdmin, TechLead, Developer | ✅ |
| Listar execuções | `integrations:read` | PlatformAdmin, TechLead, Developer | ✅ |
| Ver health | `integrations:read` | PlatformAdmin, TechLead, Developer | ✅ |
| Ver freshness | `integrations:read` | PlatformAdmin, TechLead, Developer | ✅ |
| Retry conector | `integrations:write` | PlatformAdmin | ✅ |
| Reprocessar execução | `integrations:write` | PlatformAdmin | ✅ |
| Criar conector | — | — | ❌ Endpoint não existe |
| Editar conector | — | — | ❌ Endpoint não existe |
| Eliminar conector | — | — | ❌ Endpoint não existe |
| Testar conexão | — | — | ❌ Endpoint não existe |
| Gerir credenciais | — | — | ❌ Endpoint não existe |

---

## 3. Guards no frontend

| Componente | Guard | Estado |
|-----------|-------|--------|
| Menu "Integration Hub" | `integrations:read` | ✅ Verificado — condicionado a permissão |
| Botão "Retry" | `integrations:write` | ✅ Verificado — disabled sem permissão |
| Botão "Reprocess" | `integrations:write` | ✅ Verificado — disabled sem permissão |
| Botão "Create" | — | ❌ Não existe |
| Botão "Edit" | — | ❌ Não existe |
| Botão "Delete" | — | ❌ Não existe |

---

## 4. Enforcement no backend

| Endpoint | Atributo de autorização | Estado |
|----------|----------------------|--------|
| GET `/connectors` | `RequirePermission("integrations:read")` | ✅ |
| GET `/connectors/{id}` | `RequirePermission("integrations:read")` | ✅ |
| GET `/ingestion/sources` | `RequirePermission("integrations:read")` | ✅ |
| GET `/ingestion/executions` | `RequirePermission("integrations:read")` | ✅ |
| GET `/integrations/health` | `RequirePermission("integrations:read")` | ✅ |
| GET `/ingestion/freshness` | `RequirePermission("integrations:read")` | ✅ |
| POST `/connectors/{id}/retry` | `RequirePermission("integrations:write")` | ✅ |
| POST `/executions/{id}/reprocess` | `RequirePermission("integrations:write")` | ✅ |

---

## 5. Acções sensíveis

| # | Acção sensível | Risco | Permissão recomendada | Estado |
|---|---------------|-------|----------------------|--------|
| S-01 | **Criar integração com sistema externo** | ALTO — expõe dados do tenant a sistema externo | `integrations:write` + approval workflow | ❌ Não implementado |
| S-02 | **Editar configuração de conector** | ALTO — pode alterar endpoint para destino malicioso | `integrations:write` | ❌ Não implementado |
| S-03 | **Testar conexão** | MÉDIO — envia dados de teste para sistema externo | `integrations:write` | ❌ Não implementado |
| S-04 | **Reprocessar execução** | MÉDIO — pode re-enviar dados sensíveis | `integrations:write` | ✅ Implementado |
| S-05 | **Eliminar conector** | ALTO — perda de configuração e histórico | `integrations:admin` (nova) | ❌ Não implementado |
| S-06 | **Gerir credenciais** | CRÍTICO — API keys, tokens, secrets | `integrations:admin` (nova) | ❌ Não implementado |
| S-07 | **Activar conector desactivado** | MÉDIO — retoma fluxo de dados | `integrations:write` | ❌ Não implementado |

---

## 6. Escopo por tenant

| Aspecto | Estado | Detalhe |
|---------|--------|---------|
| RLS por TenantId | ✅ | Via `TenantRlsInterceptor` em `NexTraceDbContextBase` |
| Conectores isolados por tenant | ✅ | `tenant_id` em todas as tabelas |
| Cross-tenant access prevention | ✅ | RLS policy no PostgreSQL |
| Shared connectors entre tenants | ❌ | Não suportado (cada tenant tem os seus conectores) |

---

## 7. Escopo por environment

| Aspecto | Estado | Detalhe |
|---------|--------|---------|
| Environment como campo | ✅ | `IntegrationConnector.Environment` como string |
| Filtro por environment | ✅ | Endpoint de listagem permite filtro |
| Isolamento por environment | ❌ | Environment é filtro, não isolamento — falta `EnvironmentId` formal |
| Environment-aware permissions | ❌ | Sem permissão diferenciada por environment |

**Gap:** Environment é string livre ("Production", "Staging") em vez de `EnvironmentId` Guid referenciando Environment Management.

---

## 8. Auditoria de acções críticas

| Acção | Audit event | Estado |
|-------|------------|--------|
| Criar conector | — | ❌ Endpoint não existe, sem audit |
| Editar conector | — | ❌ Endpoint não existe |
| Eliminar conector | — | ❌ Endpoint não existe |
| Activar/Desactivar | — | ❌ Endpoint não existe |
| Retry conector | — | ❌ Sem publicação de evento |
| Reprocessar execução | — | ❌ Sem publicação de evento |
| Testar conexão | — | ❌ Endpoint não existe |
| Gerir credenciais | — | ❌ Endpoint não existe |

**Resultado: ZERO acções auditadas.** 🔴

---

## 9. Análise de segurança de credenciais

| Aspecto | Estado | Recomendação |
|---------|--------|-------------|
| Armazenamento de API keys | ❌ Não implementado | Usar `EncryptionInterceptor` (AES-256-GCM) |
| Rotação de credenciais | ❌ Não implementado | Endpoint de rotação + audit trail |
| Exposição de secrets em logs | ⚠️ Não verificado | Garantir que `ErrorMessage` não contém credentials |
| Exposição de secrets em API responses | ⚠️ Não verificado | Garantir que GET connector não retorna credentials |
| Secret masking no frontend | ❌ Não implementado | Exibir `•••••` para campos sensíveis |

---

## 10. Permissões a criar

| Permissão | Descrição | Roles |
|-----------|-----------|-------|
| `integrations:admin` | Acções destrutivas e gestão de credenciais | PlatformAdmin |
| `integrations:test` | Testar conexões (sem alterar) | PlatformAdmin, TechLead |

**Nota:** `integrations:read` e `integrations:write` já existem em `RolePermissionCatalog.cs`.

---

## 11. Backlog de correcções de segurança

| # | Item | Prioridade | Tipo | Esforço |
|---|------|-----------|------|---------|
| S-01 | Publicar audit events para todas as acções de escrita | 🔴 P0_BLOCKER | FUNCTIONAL_FIX | 4h |
| S-02 | Criar permissão `integrations:admin` | 🔴 P1_CRITICAL | FUNCTIONAL_FIX | 2h |
| S-03 | Implementar credential encryption com `EncryptionInterceptor` | 🔴 P1_CRITICAL | STRUCTURAL_FIX | 4h |
| S-04 | Garantir que credentials não aparecem em API responses | 🔴 P1_CRITICAL | FUNCTIONAL_FIX | 2h |
| S-05 | Garantir que credentials não aparecem em logs/ErrorMessage | 🟡 P2_HIGH | FUNCTIONAL_FIX | 2h |
| S-06 | Substituir Environment string por EnvironmentId formal | 🟡 P2_HIGH | STRUCTURAL_FIX | 3h |
| S-07 | Implementar secret masking no frontend | 🟡 P2_HIGH | FUNCTIONAL_FIX | 2h |
| S-08 | Validar endpoint URL format para prevenir SSRF | 🔴 P1_CRITICAL | FUNCTIONAL_FIX | 2h |
| S-09 | Rate limiting em webhook endpoints | 🟡 P2_HIGH | STRUCTURAL_FIX | 3h |
| S-10 | Webhook secret validation | 🟡 P2_HIGH | FUNCTIONAL_FIX | 3h |

**Total estimado: ~27h**
