# Backlog Final de Limpeza Estrutural — NexTraceOne

> Prompt N16 — Parte 9 | Data: 2026-03-25 | Fase: Encerramento da Trilha N

---

## 1. Resumo

Este backlog consolida todas as ações de limpeza estrutural identificadas nos relatórios N16, organizadas por prioridade e natureza, para execução antes ou durante a fase E.

**Total de itens: 35**
- **A. Remoções obrigatórias:** 13 itens (~13h)
- **B. Correções obrigatórias antes da fase E:** 9 itens (~30h)
- **C. Ajustes durante a fase E:** 8 itens (~36h)
- **D. Itens que podem permanecer temporariamente:** 5 itens

---

## A. Remoções Obrigatórias

> Devem ser executadas **antes** ou **no início** da trilha E.

### A1. Remoção de Resíduos de Licensing

| ID | Ação | Ficheiro | Estimativa | Prioridade |
|---|---|---|---|---|
| A1-01 | Remover 17 licensing permissions (Admin: 16, TechLead: 1) | `RolePermissionCatalog.cs` | 1h | P1 |
| A1-02 | Remover `licensing:write` das permissões delegáveis | `CreateDelegation.cs` | 0.5h | P1 |
| A1-03 | Remover HasData licensing seeds (na recriação migrations) | `PermissionConfiguration.cs` | 0.5h | P2 |
| A1-04 | Remover mapeamento `'licensing'` de breadcrumbs | `Breadcrumbs.tsx` | 0.5h | P2 |
| A1-05 | Remover mapeamento `'vendor'` de navigation utils | `navigation.ts` | 0.5h | P2 |
| A1-06 | Reescrever comentário MfaPolicy sem "licensing" | `MfaPolicy.cs` | 0.5h | P3 |
| A1-07 | Reescrever guidanceAdmin sem "licensing" (en, pt-BR, es) | Locale files | 0.5h | P3 |

**Subtotal: 4h**

### A2. Remoção de Mocks

| ID | Ação | Ficheiro | Estimativa | Prioridade |
|---|---|---|---|---|
| A2-01 | Remover "fake assistant response" de strings i18n | `en.json`, `pt-BR.json` | 0.5h | P2 |

**Subtotal: 0.5h**

### A3. Remoção de Stubs Irrelevantes

| ID | Ação | Ficheiro | Estimativa | Prioridade |
|---|---|---|---|---|
| *(nenhum stub para remoção imediata — todos são CAN_DELAY ou MUST_IMPLEMENT)* | — | — | — | — |

### A4. Remoção de Placeholders

| ID | Ação | Ficheiro | Estimativa | Prioridade |
|---|---|---|---|---|
| A4-01 | Condicionar AI Assistant page a feature flag real | `AiAssistantPage.tsx` | 2h | P2 |
| A4-02 | Condicionar AI Analysis page a feature flag real | `AiAnalysisPage.tsx` | 2h | P2 |
| A4-03 | Condicionar Automation audit trail a dados reais | `AutomationWorkflowsPage.tsx` | 2h | P2 |

**Subtotal: 6h**

### A5. Remoção de Resíduos Fora do Escopo

| ID | Ação | Ficheiro | Estimativa | Prioridade |
|---|---|---|---|---|
| A5-01 | Documentar InMemoryIncidentStore como temporário | `InMemoryIncidentStore.cs` | 1h | P2 |
| A5-02 | Criar issue tracking para remoção do InMemoryStore | Backlog | 0.5h | P2 |

**Subtotal: 1.5h**

**Total Secção A: ~12h**

---

## B. Correções Obrigatórias Antes da Fase E

> Devem ser resolvidas antes de iniciar os E-prompts pesados de remediação.

### B1. Incoerências Docs vs Código

| ID | Ação | Descrição | Estimativa | Prioridade |
|---|---|---|---|---|
| B1-01 | Validar permissões no frontend vs RolePermissionCatalog | `analytics:read`, `notifications:read` referenciadas mas não no catálogo | 4h | P1 |
| B1-02 | Adicionar `notifications:*` ao RolePermissionCatalog | Blocker documentado em N8 | 2h | P1 |
| B1-03 | Adicionar `analytics:*` ao RolePermissionCatalog | Requer extração OI-03 primeiro | 2h | P2 |
| B1-04 | Adicionar `env:*` ao RolePermissionCatalog | Requer extração OI-04 primeiro | 2h | P2 |

**Subtotal: 10h**

### B2. Gaps de Ownership

| ID | Ação | Descrição | Estimativa | Prioridade |
|---|---|---|---|---|
| B2-01 | Adicionar Environments ao sidebar com permissão adequada | `AppSidebar.tsx` — página existe mas sem entrada no menu | 2h | P2 |

**Subtotal: 2h**

### B3. UI Cosmética Perigosa

| ID | Ação | Descrição | Estimativa | Prioridade |
|---|---|---|---|---|
| B3-01 | Integrar DemoBanner em páginas FinOps com IsSimulated=true | `ServiceFinOpsPage.tsx` e similares | 4h | P2 |
| B3-02 | Adicionar warning em Product Analytics sobre dados parciais | `ProductAnalyticsOverviewPage.tsx` | 2h | P1 |

**Subtotal: 6h**

### B4. Endpoints Aparentes sem Backend Real

| ID | Ação | Descrição | Estimativa | Prioridade |
|---|---|---|---|---|
| B4-01 | Documentar que GetPersonaUsage retorna mock data | Endpoint Product Analytics | 1h | P1 |
| B4-02 | Documentar que Automation AuditTrail retorna dados simulados | Endpoint Ops Intelligence | 1h | P2 |

**Subtotal: 2h**

**Total Secção B: ~20h**

---

## C. Ajustes que Podem Ser Feitos Durante a Fase E

> Não bloqueiam o início da execução. Podem ser resolvidos incrementalmente.

| ID | Ação | Descrição | Estimativa | Prioridade |
|---|---|---|---|---|
| C-01 | Substituir InMemoryIncidentStore por IncidentDbContext real | `InMemoryIncidentStore.cs` | 8h | P1 |
| C-02 | Substituir GenerateSimulatedEntries por dados reais | `GetAutomationAuditTrail.cs` | 4h | P2 |
| C-03 | Implementar cálculos reais de waste/efficiency no FinOps | Governance FinOps handlers | 16h | P2 |
| C-04 | Garantir feature flags consumam Configuration module | `GetPlatformConfig.cs` | 4h | P2 |
| C-05 | Migrar suggested prompts para configuração persistida | `ListSuggestedPrompts.cs` | 4h | P3 |
| C-06 | Implementar CatalogGraphModuleService com dados reais | `CatalogGraphModuleService.cs` | 16h | P2 |
| C-07 | Adicionar health check real para AI sources | `AiSourceRegistryService.cs` | 8h | P3 |
| C-08 | Documentar AutomationActionCatalog como referência | `AutomationActionCatalog.cs` | 1h | P3 |

**Total Secção C: ~61h**

---

## D. Itens que Podem Permanecer Temporariamente

> Não bloqueiam execução e estão bem classificados.

| ID | Item | Justificação |
|---|---|---|
| D-01 | DemoBanner component (não utilizado) | Infra preparada para uso futuro — não engana utilizador |
| D-02 | EmptyState component | Componente legítimo e funcional |
| D-03 | "Coming soon" / "widgetComingSoon" i18n | Mensagens defensivas honestas para estados vazios |
| D-04 | IsSimulated flag pattern em FinOps | Padrão deliberado com flag explícito |
| D-05 | AI grounding "under development" message | Mensagem honesta sobre estado de desenvolvimento |

---

## 5. Resumo de Esforço

| Secção | Itens | Estimativa | Timing |
|---|---|---|---|
| A. Remoções obrigatórias | 13 | ~12h | Antes da fase E |
| B. Correções obrigatórias | 9 | ~20h | Antes ou no início da fase E |
| C. Ajustes incrementais | 8 | ~61h | Durante a fase E |
| D. Permanecem temporariamente | 5 | 0h | N/A |
| **Total** | **35** | **~93h** | |

---

## 6. Ordem de Execução Recomendada

1. **Sprint 0 (imediato):** Secção A completa (~12h) + B1-01, B1-02, B3-02, B4-01 (~8h) = **~20h**
2. **Sprint 1-2:** Restante da Secção B (~12h) + início da Secção C
3. **Sprints 3+:** Secção C incrementalmente durante execução dos E-prompts
