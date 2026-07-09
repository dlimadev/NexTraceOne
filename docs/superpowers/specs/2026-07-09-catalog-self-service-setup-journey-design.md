# Redesign da Jornada Self-service / Setup guiado (J2 Fase 2, fatia 1)

**Data:** 2026-07-09
**Estado:** Design (brainstorming) — pronto para plano de implementação
**Âmbito:** Fatia 1 da J2 Fase 2 — checklist de setup orientado por maturidade + reboot do hub self-service. As restantes peças da Fase 2 (pré-preenchimento template→onboarding, AI-scaffold-cria-serviço, tabs do developer portal, drill-through de dashboards) ficam **fora de escopo**, desenhadas noutras fatias.
**Persona:** produtor/owner de serviço.
**Antecedente:** J2 Fase 1 (wizard `/services/onboard`, "serviço criado cedo" em Planning) entregue no ciclo 26. A secção 5 da spec da J2 (`2026-07-08-catalog-onboarding-journey-j2-design.md`) esboçou esta Fase 2.

---

## 1. Problema (estado atual, verificado no código)

1. **Serviço nasce em Planning e fica órfão.** O onboarding cria o serviço em `Planning` mas o produtor não tem, no detalhe do serviço, um caminho guiado do que falta para o tornar governado/`Active`. O `ServiceLifecyclePanel` permite transitar o estado, mas não diz **o que** falta.
2. **Hub self-service é um mural de links estático e partido.** `SelfServicePortalPage` tem grupos de cards mas com **links mortos/errados**: `createService → /catalog/services/create` (inexistente; onboarding é `/services/onboard`), `createAiScaffold/generateAdr → /catalog/scaffold` (inexistente; é `/catalog/templates/:id/scaffold`), `viewContractHealth → /contracts/governance/health` (é `/contracts/health`). Não reflete o trabalho pendente do produtor nem liga ao onboarding.
3. **Sinal de maturidade existe mas não é acionável.** `getServiceMaturity` devolve `level` (já usado no health-strip do ciclo 25) e as dimensões `hasOwnership/hasContracts/hasDocumentation/hasRunbook/hasMonitoring/hasRepository`; nada as transforma em ações.

## 2. Objetivo

Fechar a jornada do produtor: **do serviço recém-criado (Planning) até governado (Active)**, com um checklist de setup acionável no detalhe do serviço, e um hub self-service que reflete o trabalho pendente e arranca a jornada. Reusar o que já existe (lifecycle panel, maturity query, deep-links de criação); nada especulativo.

## 3. Abordagem escolhida

**Checklist derivado + hub personalizado**, preferido a (B) "só corrigir links do hub" (não fecha a jornada) e a (C) "novo endpoint de setup no backend" (viola YAGNI — os sinais já existem no frontend). O checklist deriva as dimensões **honest-null dos dados já carregados** no detalhe do serviço (sem depender de forma de endpoint que possa faltar), complementado pelo `level`/dimensões do `getServiceMaturity` quando presentes.

---

## 4. Design

### 4.1 Componente A — `ServiceSetupChecklist` (detalhe do serviço)

Novo componente renderizado no `ServiceDetailPage` (modo view), em destaque quando o serviço **não** está `Active` (lifecycle em `Planning`/`Development`/`Staging`); recolhido/secundário quando `Active`.

**Dimensões** (cada uma: label i18n, `done: boolean`, e CTA de deep-link para a preencher). Derivadas honest-null:

| Dimensão | `done` quando | CTA (deep-link) |
|----------|---------------|-----------------|
| Ownership | `service.technicalOwner` presente | modo edição → tab Ownership |
| Repositório | `service.repositoryUrl` \|\| `service.gitRepository` | modo edição → tab References |
| Documentação | `service.documentationUrl` presente | modo edição → tab References |
| Interface | `(service.apis?.length ?? 0) > 0` | `/services/:id/interfaces/new` |
| Contrato | `contracts.length > 0` | `/contracts/new?serviceId=:id` |

- **Contrato é honest-null por tipo:** se `supportsContracts(serviceType)` for falso, a linha mostra-se como **N/A** (não como "por fazer") — nunca empurra uma ação impossível.
- **Runbook/Monitoring:** incluídos **apenas** se `getServiceMaturity` devolver `hasRunbook`/`hasMonitoring` (CTA → knowledge/observability respetivos); se o endpoint não os expuser, **omitidos** (honest-null — não avaliar o que não se conhece). Não inventar "por fazer".
- **Progresso:** "N de M concluídos" (M = dimensões aplicáveis, excluindo N/A e omitidas) + barra. Quando todas as aplicáveis estão `done` **e** o serviço não é `Active`, mostra CTA **"Promover a Active"** que aciona a transição via o `ServiceLifecyclePanel`/`serviceCatalogApi` já existente (não duplicar lógica de transição — reusar).
- Cada CTA de edição entra no modo edição do `ServiceDetailPage` com a tab certa (o `ServiceDetailPage` já tem `enterEditMode` + `activeFormTab`); o checklist chama um handler passado por prop (ex.: `onEditField('ownership'|'references')`).
- Honest-null global: só renderiza dimensões avaliáveis; se nenhuma for avaliável, o componente oculta-se.

### 4.2 Componente B — reboot de `SelfServicePortalPage` (hub)

Reenquadrar como **launchpad do produtor**, mantendo os grupos úteis mas:

1. **Corrigir os links partidos** (verbatim): `createService → /services/onboard`; scaffold/adr → `/catalog/templates`; `viewContractHealth → /contracts/health`; contratos por tipo → `/contracts/new?type=RestApi` / `?type=Event` (alinhado ao wizard v5).
2. **Golden path primário no topo:** bloco destacado **"Onboard a service"** → `/services/onboard`, e **"Start from a template"** → `/catalog/templates`.
3. **Secção "Services needing setup":** query aos serviços em `Planning`/`Development` (via `serviceCatalogApi.listServices` filtrando lifecycle), top N com progresso de checklist resumido, deep-link para `/services/:id` (onde o checklist guia). **Honest-null:** se não houver, a secção oculta-se.
4. Manter os restantes grupos de ações (changes/access/operations/ai) como atalhos, com os links validados.

Isto liga o hub → checklist → serviço Active, fechando o loop.

### 4.3 Reuso / extração

- `ServiceSetupChecklist` é um componente novo e focado; consome `ServiceDetail` + `contracts` (já carregados) + `maturity` (já consultado no `ServiceDetailPage` desde o ciclo 25) + `supportsContracts` (já existe).
- `SelfServicePortalPage` reusa o padrão de card/grid atual; a secção "needing setup" reusa `serviceCatalogApi.listServices`.
- Nenhuma alteração de backend/endpoint.

### 4.4 Estados

- **Loading:** o `ServiceDetailPage` já gere loading do serviço; o checklist renderiza a partir de dados carregados (sem query própria além do `maturity` já existente). A secção "needing setup" do hub tem skeleton/empty próprios.
- **Erro:** honest-null — sem dados, oculta.
- **Serviço Active:** checklist recolhido/resumido ("setup completo"); não intrusivo.
- **Pending (promover):** o CTA "Promover a Active" mostra estado de carregamento da mutação de transição existente.

### 4.5 Testes

- **Unit `ServiceSetupChecklist`:** cada dimensão done/undone; Contrato N/A quando `!supportsContracts`; Runbook/Monitoring omitidos quando ausentes do maturity; progresso N/M correto; CTA "Promover" só aparece quando tudo aplicável done e não-Active; honest-null (oculta sem dimensões).
- **Unit `SelfServicePortalPage`:** links corrigidos resolvem para as rotas reais (sem `/catalog/services/create`, `/catalog/scaffold`, `/contracts/governance/health`); secção "needing setup" honest-null (oculta sem serviços em Planning).
- **e2e:** detalhe de um serviço Planning mostra o checklist com CTAs; clicar "Add contract" navega para `/contracts/new?serviceId=`; hub mostra "Onboard a service" → `/services/onboard` e um serviço em Planning na secção needing-setup.
- **i18n:** chaves novas nos 4 locales; `validate:i18n` PASS.
- **Gates:** `tsc`/`eslint`/`vitest`/`validate:i18n`/`npm run build`/e2e verdes.

---

## 5. Fora de escopo (esta fatia)

- Pré-preenchimento template→`/services/onboard` (golden path que injeta defaults no wizard).
- AI scaffold criar um serviço de catálogo (hoje gera ficheiros/ZIP).
- Tabs do Developer Portal (Inbox/Subscriptions/Playground) e redesenho interno de Templates.
- Drill-through dos dashboards de inteligência de serviço (Grupo C do audit).

## 6. Ligações

- J2 Fase 1: [[project-betterstack-redesign]] ciclo 26; spec `2026-07-08-catalog-onboarding-journey-j2-design.md` (secção 5 = origem desta fatia).
- Health-strip/maturity ligados no ciclo 25 (base do checklist).
