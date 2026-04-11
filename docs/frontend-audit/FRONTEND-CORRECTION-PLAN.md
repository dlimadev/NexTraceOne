# NexTraceOne — Frontend Correction Plan

**Data:** 2026-04-11  
**Baseado em:** [FRONTEND-AUDIT-REPORT.md](./FRONTEND-AUDIT-REPORT.md)  
**Estado:** Em execução

---

## Resumo de Progresso

| Fase | Descrição | Itens | Estado |
|------|-----------|-------|--------|
| Fase 1 | Correções Críticas (C-01 a C-05) | 5 | 🔲 Pendente |
| Fase 2 | Correções Altas (H-01 a H-03) | 3 | 🔲 Pendente |
| Fase 3 | Correções Médias (M-01 a M-03) | 3 | 🔲 Pendente |

---

## Fase 1 — Correções Críticas

### C-01: Adicionar PageErrorState às 5 Páginas de Alta Severidade

**Impacto:** UX + Resiliência  
**Padrão a seguir:**
```tsx
import { PageErrorState } from '../../../components/PageErrorState';
import { PageLoadingState } from '../../../components/PageLoadingState';

// Dentro do componente, antes do return principal:
if (isLoading || isPending) return <PageContainer><PageLoadingState /></PageContainer>;
if (isError) return <PageContainer><PageErrorState /></PageContainer>;
```

- [ ] C-01.1 — `features/ai-hub/pages/AgentDetailPage.tsx`
- [ ] C-01.2 — `features/catalog/pages/SelfServicePortalPage.tsx`
- [ ] C-01.3 — `features/contracts/cdct/ConsumerDrivenContractPage.tsx`
- [ ] C-01.4 — `features/contracts/publication/PublicationCenterPage.tsx`
- [ ] C-01.5 — `features/governance/pages/GovernanceGatesPage.tsx`

---

### C-02: Migrar 81 Placeholders Hardcoded para i18n

**Impacto:** i18n + Conformidade multilingue  
**Padrão a seguir:**
```tsx
// ANTES:
placeholder="UserService"

// DEPOIS:
placeholder={t('contracts.soap.placeholder.serviceName', 'UserService')}
```

- [ ] C-02.01 — `contracts/workspace/builders/VisualLegacyContractBuilder.tsx` (11 ocorrências)
- [ ] C-02.02 — `contracts/workspace/builders/VisualEventBuilder.tsx` (10 ocorrências)
- [ ] C-02.03 — `contracts/workspace/builders/VisualWorkserviceBuilder.tsx` (8 ocorrências)
- [ ] C-02.04 — `contracts/workspace/builders/VisualWebhookBuilder.tsx` (8 ocorrências)
- [ ] C-02.05 — `contracts/workspace/builders/VisualSoapBuilder.tsx` (8 ocorrências)
- [ ] C-02.06 — `contracts/workspace/sections/SecuritySection.tsx` (4 ocorrências)
- [ ] C-02.07 — `contracts/workspace/builders/VisualSharedSchemaBuilder.tsx` (3 ocorrências)
- [ ] C-02.08 — `ai-hub/pages/AiAnalysisPage.tsx` (2 ocorrências)
- [ ] C-02.09 — `ai-hub/pages/AiAgentsPage.tsx` (1 ocorrência)
- [ ] C-02.10 — `governance/pages/ApiPolicyAsCodePage.tsx` (1 ocorrência)
- [ ] C-02.11 — `contracts/cdct/ConsumerDrivenContractPage.tsx` (2 ocorrências)
- [ ] C-02.12 — `contracts/governance/ContractHealthTimelinePage.tsx` (1 ocorrência)
- [ ] C-02.13 — `contracts/canonical/CanonicalEntityImpactCascadePage.tsx` (1 ocorrência)
- [ ] C-02.14 — `catalog/pages/AiScaffoldWizardPage.tsx` (1 ocorrência)
- [ ] C-02.15 — `contracts/workspace/builders/shared/SchemaCompositionEditor.tsx` (1 ocorrência)
- [ ] C-02.16 — Adicionar chaves i18n aos 4 locales (en, pt-BR, pt-PT, es)

---

### C-03: Converter Inline Styles do EnvironmentsPage para Tailwind

**Impacto:** Consistência + Manutenibilidade  
**Ficheiro:** `features/identity-access/pages/EnvironmentsPage.tsx` (28 inline styles)

**Mapeamento de conversão:**
```
style={{ display: 'flex', gap: '8px' }}          → className="flex gap-2"
style={{ display: 'grid', gap: '12px' }}         → className="grid gap-3"
style={{ display: 'block', width: '100%', ... }} → className="block w-full mt-1 px-2 py-1.5 rounded border border-edge"
style={{ color: 'var(--t-muted)' }}              → className="text-muted"
style={{ fontSize: '12px', ... }}                → className="text-xs text-muted"
```

- [ ] C-03.1 — Converter todas as 28 ocorrências de `style={{}}` para classes Tailwind

---

### C-04: Corrigir PageSection com Título Vazio

**Impacto:** UX — secções sem título confundem o utilizador

- [ ] C-04.1 — `features/operations/pages/PredictiveIntelligencePage.tsx:501` — adicionar título i18n ou remover PageSection
- [ ] C-04.2 — `features/governance/pages/ApiPolicyAsCodePage.tsx:127` — adicionar título i18n ou remover PageSection

---

### C-05: Migrar aria-labels Hardcoded para i18n

**Impacto:** Acessibilidade + i18n  
**Padrão:**
```tsx
// ANTES:
aria-label="Close"

// DEPOIS:
aria-label={t('common.close')}
```

- [ ] C-05.1 — `components/Breadcrumbs.tsx` — `"Breadcrumbs"` → `t('nav.breadcrumbs')`
- [ ] C-05.2 — `components/Modal.tsx` — `"Close"` → `t('common.close')`
- [ ] C-05.3 — `components/Drawer.tsx` — `"Close"` → `t('common.close')`
- [ ] C-05.4 — `components/Toast.tsx` — `"Notifications"` → `t('notifications.title')`, `"Dismiss"` → `t('common.dismiss')`
- [ ] C-05.5 — `components/NexTraceLogo.tsx` — `"NexTraceOne"` → `t('brand.name')` (×2)
- [ ] C-05.6 — `contracts/governance/ContractHealthTimelinePage.tsx` — `"API Asset ID"` → `t('contracts.health.assetIdLabel')`
- [ ] C-05.7 — Adicionar chaves i18n aos 4 locales

---

## Fase 2 — Correções Altas

### H-01: Decompor Componentes > 900 Linhas

**Impacto:** Manutenibilidade + Performance  
**Estratégia:** Extrair sub-componentes lógicos (tabs, secções, modais) para ficheiros separados no mesmo diretório.

- [ ] H-01.1 — `AiAssistantPage.tsx` (1.213 linhas) → Extrair ChatMessageList, ChatInput, SuggestionPanel
- [ ] H-01.2 — `VisualRestBuilder.tsx` (1.124 linhas) → Extrair EndpointEditor, ResponseEditor, ParameterEditor
- [ ] H-01.3 — `AssistantPanel.tsx` (1.004 linhas) → Extrair MessageBubble, TypingIndicator, ContextPanel
- [ ] H-01.4 — `ServiceCatalogPage.tsx` (1.001 linhas) → Extrair CatalogFilters, CatalogGrid, CatalogTable
- [ ] H-01.5 — `DeveloperPortalPage.tsx` (991 linhas) → Extrair PortalSidebar, ApiExplorer, SchemaViewer

---

### H-02: Corrigir Acessibilidade em Elementos Clickáveis

**Impacto:** Acessibilidade (WCAG 2.1)

- [ ] H-02.1 — `features/catalog/pages/ServiceDiscoveryPage.tsx:403` — Adicionar `role="dialog"` ao overlay e `role="button"` com aria-label

---

### H-03: Converter Inline Styles Restantes para Tailwind

**Impacto:** Consistência do design system  
**Abordagem:** Por ficheiro, converter `style={{}}` para classes Tailwind equivalentes.

Ficheiros prioritários (excluindo EnvironmentsPage já tratado em C-03):
- [ ] H-03.01 — `contracts/workspace/builders/shared/SchemaPropertyEditor.tsx` (5 inline styles)
- [ ] H-03.02 — `contracts/catalog/components/CatalogTable.tsx`
- [ ] H-03.03 — `contracts/catalog/components/CatalogToolbar.tsx`
- [ ] H-03.04 — `contracts/workspace/sections/ApprovalsSection.tsx`
- [ ] H-03.05 — `contracts/workspace/sections/ContractSection.tsx`
- [ ] H-03.06 — `contracts/workspace/sections/ScorecardSection.tsx`
- [ ] H-03.07 — `contracts/workspace/sections/SecuritySection.tsx`
- [ ] H-03.08 — `contracts/workspace/components/StudioRail.tsx`
- [ ] H-03.09 — `contracts/governance/ContractGovernancePage.tsx`
- [ ] H-03.10 — `contracts/governance/ContractHealthTimelinePage.tsx`
- [ ] H-03.11 — `change-governance/components/ReleasesIntelligenceTab.tsx`
- [ ] H-03.12 — `change-governance/pages/ChangeCatalogPage.tsx`
- [ ] H-03.13 — `change-governance/pages/ChangeDetailPage.tsx`
- [ ] H-03.14 — `governance/pages/CompliancePage.tsx`
- [ ] H-03.15 — `governance/pages/DomainDetailPage.tsx`
- [ ] H-03.16 — `governance/pages/EnterpriseControlsPage.tsx`
- [ ] H-03.17 — `governance/pages/ExecutiveOverviewPage.tsx`
- [ ] H-03.18 — `governance/pages/MaturityScorecardsPage.tsx`
- [ ] H-03.19 — `governance/pages/ReportsPage.tsx`
- [ ] H-03.20 — `governance/pages/ServiceScorecardPage.tsx`
- [ ] H-03.21 — `governance/pages/TeamDetailPage.tsx`
- [ ] H-03.22 — `configuration/pages/BrandingAdminPage.tsx`
- [ ] H-03.23 — `configuration/pages/ParameterComplianceDashboardPage.tsx`
- [ ] H-03.24 — `catalog/components/DependencyGraph.tsx`
- [ ] H-03.25 — `catalog/pages/ServiceMaturityPage.tsx`
- [ ] H-03.26 — `catalog/pages/ServiceScorecardPage.tsx`
- [ ] H-03.27 — `ai-hub/components/AssistantPanel.tsx` (animation-delay — pode manter)
- [ ] H-03.28 — `ai-hub/pages/AiAssistantPage.tsx` (animation-delay — pode manter)
- [ ] H-03.29 — `ai-hub/pages/AiRoutingPage.tsx`
- [ ] H-03.30 — `ai-hub/pages/TokenBudgetPage.tsx`
- [ ] H-03.31 — `identity-access/components/AuthCard.tsx`
- [ ] H-03.32 — `identity-access/components/AuthShell.tsx`
- [ ] H-03.33 — `product-analytics/pages/AdoptionFunnelPage.tsx`
- [ ] H-03.34 — `product-analytics/pages/JourneyFunnelPage.tsx`
- [ ] H-03.35 — `product-analytics/pages/ModuleAdoptionPage.tsx`
- [ ] H-03.36 — `product-analytics/pages/PersonaUsagePage.tsx`
- [ ] H-03.37 — `product-analytics/pages/TimeToValuePage.tsx`
- [ ] H-03.38 — `product-analytics/pages/ValueTrackingPage.tsx`

---

## Fase 3 — Correções Médias (Roadmap)

### M-01: Decompor Ficheiros 500-900 Linhas

**Prioridade:** Roadmap  
**Ficheiros:** 29 ficheiros entre 500 e 900 linhas (ver audit report §7.1)

### M-02: Adicionar React.memo a Componentes Pesados

**Prioridade:** Roadmap  
**Estratégia:** Wrapping de componentes que recebem props complexas e são renderizados em listas.

### M-03: Aumentar Cobertura de Testes Frontend

**Prioridade:** Roadmap  
**Meta:** 27% → 50%+  
**Foco:** Páginas críticas (Service Catalog, Contract Workspace, Change Detail, Incidents)

---

## Regras de Execução

1. **Cada correção deve preservar comportamento existente** — sem regressões.
2. **TypeScript e build devem passar após cada correção** — `tsc --noEmit && vite build`.
3. **i18n keys devem ser adicionadas nos 4 locales** — en, pt-BR, pt-PT, es.
4. **Testes existentes devem continuar a passar** — `vitest run`.
5. **Inline styles convertidos devem usar tokens de design existentes** — não criar novos.
6. **Ficheiros decompostos devem manter exports públicos iguais** — sem breaking changes.
7. **Não alterar funcionalidade** — apenas qualidade, consistência, acessibilidade e i18n.

---

*Plano gerado a partir da auditoria frontend de 2026-04-11.*
