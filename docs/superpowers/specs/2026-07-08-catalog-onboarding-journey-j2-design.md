# Redesign da Jornada de Criação/Onboarding (J2) — Catálogo + Contratos

**Data:** 2026-07-08
**Estado:** Design aprovado (brainstorming) — pronto para plano de implementação
**Âmbito:** Fase 1 (unificar + reparar) implementável já; Fase 2 (self-service / golden paths) desenhada como evolução, **não** construída agora.
**Persona dominante:** produtor/owner de serviço + integrador.
**Antecedente:** J1 (Descoberta, browse-first) entregue nos ciclos 24–25; J2 ficou explicitamente como "próximo possível".

---

## 1. Problema (estado atual, verificado no código)

A criação/onboarding está fragmentada e parcialmente partida:

1. **CTA "Registar serviço" está morto** — o `<Button>` no cabeçalho do `ServiceCatalogPage` não tem `onClick`; clicar não faz nada.
2. **Criação de serviço inacessível** — existe em `/services/new` (`ServiceDetailPage` em modo *create*, form-workspace com preview ao vivo), mas nada no catálogo navega para lá.
3. **Caminhos de contrato duplicados** — `/contracts/new` (wizard de 4 passos) **e** `/contracts/studio/{new,rest,async,soap,graphql}` (DraftStudioPage) competem como entradas.
4. **Código morto** — `ServiceRegistrationOverlay`, `ContractImportOverlay`, `ServiceInterfaceOverlay` e o base `WizardOverlay` não têm referências fora dos próprios testes.
5. **AI scaffold desconectado** — `/catalog/templates/:id/scaffold` (`AiScaffoldWizardPage`) não integra nenhuma jornada.
6. **Sem jornada ponta-a-ponta** — registar serviço → interfaces → contrato são fluxos separados e desligados.

## 2. Objetivo

Elevar a criação/onboarding ao nível da Descoberta: **uma jornada guiada ponta-a-ponta** (registar serviço → interface/contrato opcionais no mesmo percurso → serviço governado, com next-steps explícitos), reparando o que está partido e eliminando a fragmentação — **reusando** as peças que já são boas.

## 3. Abordagem escolhida

**A — Wizard unificado "Onboard a Service"** (stepper full-page único), preferido a (B) wizards encadeados e (C) workspace progressivo. A é a mais fiel ao "um percurso guiado", dá entrada única e reusa o wizard de contrato. As ideias de C alimentam a Fase 2.

---

## 4. Fase 1 — Design

### 4.1 Forma da jornada, shell e persistência

- **Entrada única:** "Registar serviço" (cabeçalho do catálogo + card no estado-vazio do Browse) → **`/services/onboard`** (rota full-page dedicada, mesma linguagem do wizard de contrato).
- **Shell (`OnboardWizardShell`):** rail de progresso persistente à esquerda + **preview de identidade ao vivo** à direita (reusa `ServiceIdentityCard`). Footer com Anterior/Seguinte/Saltar. DS + tokens semânticos apenas; i18n nos 4 locales.
- **4 passos:** (1) Identidade & Ownership → (2) Interface *(opcional)* → (3) Contrato *(opcional/saltável)* → (4) Rever & concluir.
- **Modelo de persistência — "serviço criado cedo":** o serviço é criado **ao sair do Passo 1** (`registerService` → `serviceId`, lifecycle inicial **Planning**). Interface e contrato **anexam-se ao serviço já existente** (é o que os endpoints exigem). Cada passo persiste ao avançar. "Concluir" → `/services/:id` com painel de próximos passos.
  - Justificação: espelha a realidade da API sem hacks; se o utilizador sair após o Passo 1, o serviço já existe e completa depois via next-steps (concretiza o "next steps explícitos").

### 4.2 Conteúdo dos passos + mapa de reuso (extração para fonte única)

| Passo | Reusa | Extração partilhada | Persistência ao avançar |
|-------|-------|---------------------|--------------------------|
| 1. Identidade & Ownership | campos/validadores do modo *create* do `ServiceDetailPage` (`EditFormState`/`EMPTY_FORM`) | `ServiceIdentityForm` (usado pelo wizard **e** pelo modo-edição do detalhe) | `registerService()` → `serviceId` |
| 2. Interface *(opcional)* | form do `CreateServiceInterfacePage` | `ServiceInterfaceForm` (usado pela página autónoma **e** pelo passo) | `createServiceInterface(serviceId, …)` |
| 3. Contrato *(opcional)* | `TypeModeTab` + `DetailsTab` do `CreateContractPage` | esses componentes extraídos e reutilizados **com o serviço já vinculado** (salta o service-picker) | `createDraft(…)` ligado ao serviço |
| 4. Rever & concluir | — | resumo com honest-null nos opcionais saltados | — → `/services/:id` |

- Obrigatórios do Passo 1: nome, domínio, equipa, tipo (os que `registerService` exige); restantes opcionais.
- O `/contracts/new` autónomo mantém-se (adicionar contrato a serviço existente, com picker) e renderiza **exatamente os mesmos** componentes de tipo/modo/details — fonte única.

### 4.3 Unificação e limpeza

1. **CTA morto → ligado** ("Registar serviço" → `/services/onboard`).
2. **Contrato duplicado → consolidado:**
   - `/contracts/studio/:draftId` = editor de draft (mantém-se; destino do `createDraft`).
   - `/contracts/studio/{rest,async,soap,graphql}` = rotas **mortas** (nada lhes navega) → **removidas**.
   - `/contracts/studio/new` (draft em branco) = mantém-se como entrada secundária "avançado: do zero"; `/contracts/new` fica como a entrada guiada primária.
3. **Cluster de overlays órfão → removido** — `ServiceRegistrationOverlay`, `ContractImportOverlay`, `ServiceInterfaceOverlay`, `WizardOverlay` + testes (verificado: sem referências não-teste). Grep-guard na implementação.
4. **AI scaffold → reservado** (`/catalog/templates/:id/scaffold` intocado; entrada golden-path na Fase 2).

### 4.4 Estados

- **Validação por passo:** Zod + erros inline; "Seguinte" desativado até válido. Passos 2/3 só validam se começarem a ser preenchidos.
- **Saltar:** Passos 2 e 3 têm "Saltar" visível → avançam sem persistir; honest-null no resumo.
- **Saída/retoma:** após o Passo 1 o serviço existe (Planning). "Cancelar" antes do Passo 1 → descarta; depois → "Sair, o serviço está guardado; termina depois" → `/services/:id`. Retoma (Fase 1) pelo painel de próximos passos no detalhe. Nunca há meio-estado órfão.
- **Erros parciais:** persistência incremental por passo → sem rollback (entidades aditivas e independentes; falha fica no passo, permite repetir, não perde o já criado).
- **Pending:** cada "Seguinte" mostra estado de carregamento (`mutation.isPending`).

### 4.5 Testes (Fase 1)

- **Unit:** `ServiceIdentityForm`/`ServiceInterfaceForm` (render+validação+onChange); `OnboardWizardShell` (navegação/progresso); orquestrador (transições, saltar, persistência serviço-cedo com mutations mockadas, erro parcial); honest-null no resumo.
- **e2e (Playwright, mock):** jornada completa (serviço → saltar interface → contrato → concluir → `/services/:id`); caminho saltar-tudo; sair-após-Passo-1. Mocks de `registerService`, `createServiceInterface`, `createDraft`.
- **i18n:** chaves novas nos 4 locales (`en/es/pt-BR/pt-PT`); `validate:i18n` PASS.
- **Regressão:** remoção de overlays + rotas mortas não parte nada (grep-guard + suíte completa verde).
- **Gates:** `tsc`, `eslint`, `vitest`, `validate:i18n`, `npm run build`, e2e — todos verdes.

---

## 5. Fase 2 — Evolução (design-only; assenta na Fase 1, não a reescreve)

1. **Checklist de setup guiado no detalhe do serviço** — o painel de próximos passos evolui para um checklist **orientado por maturidade real** (dimensões já em `ServiceMaturityItemDto`: `hasOwnership/hasContracts/hasDocumentation/hasRunbook/hasMonitoring/hasRepository`). Serviço "gradua" de Planning → Active.
2. **Golden paths / templates** — escolha de entrada no topo do onboard ("Do zero" vs "Template/golden path"), ligando `AiScaffoldWizardPage`/templates; pré-preenche padrões comuns (microserviço REST, produtor Kafka…).
3. **IA de primeira classe** — modo "AI Generation" do contrato + scaffold por template tornam-se centrais (descrever → IA propõe identidade/interface/contrato em rascunho).
4. **Retoma-no-wizard** — persistir o progresso para um serviço Planning reentrar no passo exato.
5. **Maturidade como progresso** — o health-strip (Maturity) do ciclo 25 vira medidor de progresso de onboarding.
6. **Portal self-service** — `SelfServicePortalPage` hospeda "Onboard a new service" (golden paths, templates recentes, defaults de equipa).

---

## 6. Fora de escopo (Fase 1)

- Construir a Fase 2 (só desenhada aqui).
- Redesenhar o editor de studio (`DraftStudioPage`) ou Monaco — preservados.
- Alterar backend/endpoints — a Fase 1 orquestra os endpoints existentes (`registerService`, `createServiceInterface`, `createDraft`).
- Refatorações não relacionadas com a jornada de criação.

## 7. Ligações

- Descoberta (J1): [[project_betterstack_redesign]] ciclos 24–25.
- Health-strip/maturidade ligados no ciclo 25 (base da Fase 2).
