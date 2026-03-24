# NexTraceOne — Recomendação de Priorização para Revisão Detalhada

**Data:** 2026-03-24
**Baseado em:** Auditoria estrutural completa do repositório
**Objetivo:** Definir a ordem e o foco da próxima fase de revisão detalhada módulo a módulo

---

## Critérios de Priorização

Cada módulo foi avaliado nos seguintes critérios:

| Critério | Peso | Descrição |
|----------|------|-----------|
| **Criticidade para o produto** | Alto | O módulo é core da proposta de valor? |
| **Risco funcional** | Alto | Há código sem rota, comportamentos quebrados, permissões erradas? |
| **Exposição no menu** | Alto | Quantos utilizadores verão estes problemas imediatamente? |
| **Divergência doc/código** | Médio | Qual é a distância entre o que está documentado e o que existe? |
| **Dependências** | Médio | Este módulo bloqueia ou é bloqueado por outros? |
| **Risco técnico** | Médio | Migrations recentes, refactoring em andamento, SIM/PLAN não resolvidos? |
| **Impacto transversal** | Médio | Afeta múltiplos módulos ou é isolado? |

---

## PRIORIDADE 1 — Ação Imediata (antes de qualquer revisão detalhada)

Estas são correções bloqueantes que afetam a navegação e experiência de produto imediatamente.

### P1.1 — Contratos: 3 Rotas Ausentes + 1 Redirect Quebrado

**Problema:** Secção "Contracts" no menu tem 6 itens; 3 sem rota e 1 com redirect incorreto.

| Item | Ação Necessária |
|------|----------------|
| `sidebar.contractGovernance` → `/contracts/governance` | Registar rota em App.tsx para `ContractGovernancePage` |
| `sidebar.spectralRulesets` → `/contracts/spectral` | Registar rota em App.tsx para `SpectralRulesetManagerPage` |
| `sidebar.canonicalEntities` → `/contracts/canonical` | Registar rota em App.tsx para `CanonicalEntityCatalogPage` |
| `sidebar.contractStudio` → `/contracts/studio` | Corrigir: ou remover redirect e criar rota sem parâmetro, ou alterar comportamento do menu |

**Impacto:** 3 itens de menu visivelmente não funcionais para qualquer utilizador com `contracts:read`.

### P1.2 — Runbooks: Permissão Inconsistente

**Problema:** Sidebar usa `operations:runbooks:read` mas App.tsx usa `operations:incidents:read` para `/operations/runbooks`.

**Ação:** Alinhar uma das fontes — decidir qual permissão é correta e aplicar em ambos.

---

## PRIORIDADE 2 — Revisão Modular Urgente (próxima fase)

### MÓDULO 1: Contracts (prioridade máxima)

**Justificativa:**
- Funcionalidade central do produto (contratos de API são a proposta de valor core)
- 4 páginas órfãs sem rota (`governance`, `spectral`, `canonical`, `portal`)
- Secção do menu mais quebrada (50% dos itens com problema)
- Backend rico com 30+ application features
- Documentação insuficiente para o estado atual

**O que revisar:**
- Estado real de cada sub-página (governance, spectral, canonical, portal): são funcionais? Estão integradas com backend real?
- Fluxo completo: criação → draft → studio → workspace → publicação → governance
- Alinhamento entre ContractCatalogPage, DraftStudioPage e ContractWorkspacePage
- i18n cobertura das páginas de contratos
- Fluxo de aprovação (ApprovalWorkflow mencionado em IMPLEMENTATION-STATUS como IMPL)
- ContractPortalPage — relação com DeveloperPortalPage

**Risco identificado:** As 4 páginas podem estar funcionalmente completas mas simplesmente não registadas — ou podem ser esqueletos incompletos.

---

### MÓDULO 2: Governance (prioridade alta)

**Justificativa:**
- Módulo mais rico do frontend (20+ páginas)
- Backend com 17 endpoint modules
- 10+ páginas sem item no menu (WaiversPage, EnterpriseControlsPage, EvidencePackagesPage, MaturityScorecardsPage, BenchmarkingPage, DelegatedAdminPage + sub-páginas de finops/executive/risk)
- Visibilidade máxima — executive, compliance e finops são críticos para decisores

**O que revisar:**
- Navegação interna: como o utilizador chega a waivers, controls, evidence, maturity, benchmarking?
- Estado real dos dados (SIM vs IMPL) para executive overview, finops, risk
- Alinhamento das 17 backend endpoint modules com as 20+ frontend pages
- GovernanceConfigurationPage: o que é possível configurar? Está conectado ao backend?
- Packs: o que são governance packs na prática? O fluxo está completo?
- FinOps: ServiceFinOpsPage, TeamFinOpsPage, DomainFinOpsPage — dados reais ou simulados?

---

### MÓDULO 3: AI Hub (prioridade alta)

**Justificativa:**
- Proposta de valor diferenciadora do produto
- 10 páginas + AgentDetailPage
- Backend com 3 DbContexts e 5 endpoint modules
- Documentação espalhada em 5+ ficheiros
- Risco de handlers SIM não eliminados

**O que revisar:**
- AiAssistantPage: tem integração real com `ExternalAiEndpointModule` ou usa mock?
- AiAgentsPage + AgentDetailPage: fluxo de criação/gestão de agentes está completo?
- ModelRegistryPage: modelos reais ou lista simulada?
- AiRoutingPage: routing de requests de IA está implementado?
- TokenBudgetPage: budgets reais com controlo de gastos ou apenas UI?
- AiAnalysisPage: usa `ai:runtime:write` — diferente dos outros. Qual é o propósito?
- Consolidar documentação de AI Hub num único documento por subdomínio

---

### MÓDULO 4: OperationalIntelligence / Operations (prioridade alta)

**Justificativa:**
- Backend mais complexo por DbContexts (5) com 7 endpoint modules
- Frontends confusamente divididos entre `operations` (8 páginas, no menu) e `operational-intelligence` (1 página config, sem menu)
- Documentação de reliability existe mas não cobre Automation, Cost, Runtime
- Automation e Cost Intelligence são features de alto valor

**O que revisar:**
- AutomationWorkflowsPage: automações reais ou dados simulados?
- CostIntelligence: custo real de infra ou mock? `AddCostImportPipeline` migration sugere pipeline de importação
- EnvironmentComparisonPage: comparação real entre ambientes ou dados demo?
- Incidents: fluxo completo de criação → mitigação → resolução?
- Runbooks: são manuais executáveis ou apenas templates?
- Naming: clarificar distinção conceptual entre `operations` (frontend) e `OperationalIntelligence` (backend)

---

### MÓDULO 5: Identity Access (prioridade alta)

**Justificativa:**
- Módulo de segurança — erros têm impacto direto na segurança do produto
- `EnvironmentsPage` sem item no menu — gestão de ambientes invisível
- Break Glass, JIT Access, Delegations são features de segurança críticas
- IMPLEMENTATION-STATUS menciona `Environments: PARTIAL`

**O que revisar:**
- EnvironmentsPage: por que está sem menu? É intencional? Como é acessada?
- Break Glass: fluxo completo de pedido → aprovação → acesso → revogação?
- JIT Access: fluxo funcional ou apenas UI?
- Delegations: fluxo de delegação completo?
- Access Review: revisões periódicas ou ad-hoc? Funcionamento real?
- Tenancy: tenant isolation real vs PARTIAL status

---

## PRIORIDADE 3 — Revisão Modular Importante

### MÓDULO 6: Change Governance

**Justificativa:**
- Proposta de valor core: change confidence é diferenciador
- Backend rico com 4 DbContexts e 9 endpoint modules
- Frontend com 5 páginas funcionais
- Documentação parcial (CHANGE-CONFIDENCE.md existe)

**O que revisar:**
- ChangeCatalogPage: dados reais de mudanças ou simulados?
- ReleasesPage: releases reais integradas com CI/CD?
- WorkflowPage: workflows configuráveis funcionais?
- PromotionPage: promoção entre ambientes com validação real?
- Alinhamento entre `sidebar.changeConfidence` (label) e `ChangeCatalogPage` (conteúdo)

---

### MÓDULO 7: Catalog (Serviços, Source of Truth, Portal)

**Justificativa:**
- ServiceCatalog é a entrada principal do produto para engenheiros
- Source of Truth é feature diferenciadora
- Developer Portal é complexo (`/portal/*`)
- IMPLEMENTATION-STATUS: ServiceCatalog IMPL, Dependencies PLAN

**O que revisar:**
- ServiceCatalogPage (graph): grafo de dependências funcional com dados reais?
- ServiceDetailPage: dados completos de serviço?
- SourceOfTruthExplorerPage: dados reais de source of truth ou demo?
- DeveloperPortalPage: `/portal/*` cobre todo o portal ou é placeholder?
- Dependencies/Topology: status PLAN — o que o utilizador vê quando não há dados?

---

### MÓDULO 8: Integrations

**Justificativa:**
- Integração é crítica para ingestão de dados reais
- Sem dados reais → produto é demo

**O que revisar:**
- IntegrationHubPage: conectores reais configuráveis?
- IngestionExecutionsPage: histórico real de execuções?
- IngestionFreshnessPage: frescor real de dados por conector?
- ConnectorDetailPage: configuração funcional de conectores?
- Relação com `NexTraceOne.Ingestion.Api`

---

### MÓDULO 9: Notifications

**Justificativa:**
- Módulo transversal — notificações de todos os outros módulos passam aqui
- 25+ guias de configuração mas sem user guide
- Sem item no menu sidebar

**O que revisar:**
- NotificationCenterPage: notificações reais ou simuladas?
- NotificationPreferencesPage: preferências salvas?
- NotificationConfigurationPage: templates e canais configuráveis?
- Como o utilizador acede às notificações? Via topbar bell?
- Canais implementados: email? Teams? Webhook?

---

## PRIORIDADE 4 — Revisão Modular Complementar

### MÓDULO 10: Product Analytics

- 5 páginas funcionais
- Backend via `ProductAnalyticsEndpointModule` em Governance
- Sem documentação de produto
- Dados reais ou simulados?

### MÓDULO 11: AuditCompliance

- Módulo mínimo — 1 página, 1 endpoint, 2 migrations
- Sem documentação
- Escopo real não claro — o que o `/audit` mostra?

### MÓDULO 12: Configuration Admin

- 9 sub-páginas de configuração
- 30+ guias de configuração documentados
- Verificar se configurações são persistidas via `ConfigurationEndpointModule`

---

## PRIORIDADE 5 — Documentação (paralelo à revisão modular)

### Doc 1: Consolidação de Markdown

- Mover 160 ficheiros históricos para `docs/archive/`
- Reescrever `MODULES-AND-PAGES.md`
- Consolidar documentação de AI Hub
- Criar documentação de módulo para AuditCompliance, Configuration, Notifications

### Doc 2: User Guide Completo

- Expandir `docs/user-guide/` para cobrir todos os 15 módulos
- Documentar navegação interna (como chegar a sub-páginas de governance)
- Documentar acesso a notificações e ambientes

### Doc 3: Atualização de Documentos Técnicos

- `ARCHITECTURE-OVERVIEW.md` — refletir 20 DbContexts e migrations de março 2026
- `FRONTEND-ARCHITECTURE.md` — atualizar versões de bibliotecas
- `IMPLEMENTATION-STATUS.md` — adicionar módulos recentes se aplicável

---

## Calendário Sugerido para Revisão Detalhada

| Semana | Módulo(s) | Foco |
|--------|-----------|------|
| **Imediato** | Contracts (P1.1), Runbooks (P1.2) | Correções críticas de rota e permissão |
| **Semana 1** | Contracts (completo) | Rotas, fluxos, sub-páginas, integração backend |
| **Semana 2** | Governance | Navegação interna, sub-páginas, dados reais |
| **Semana 3** | AI Hub | Integração real vs SIM, consolidação doc |
| **Semana 4** | OperationalIntelligence + Operations | Automação, custo, incidents completos |
| **Semana 5** | Identity Access | Segurança, environments, fluxos críticos |
| **Semana 6** | Change Governance + Catalog | Fluxos de mudança e catálogo |
| **Semana 7** | Integrations + Notifications | Ingestão real, notificações funcionais |
| **Semana 8** | Analytics + AuditCompliance + Config | Módulos complementares |
| **Semana 9-10** | Documentação | Consolidação, archive, reescrita |

---

## Critérios de Conclusão da Revisão Detalhada

Para cada módulo, a revisão é concluída quando:

1. **Todas as páginas mapeadas** — rota, componente, permissão, estado (IMPL/SIM/PLAN)
2. **Todos os endpoints mapeados** — endpoint real, handler status, dados reais vs simulados
3. **i18n verificado** — chaves em todos os locales
4. **Loading/Error/Empty states verificados** — cada página tem tratamento de estado
5. **Formulários validados** — campos técnicos não expostos ao utilizador, validação correta
6. **Navegação verificada** — como o utilizador chega a cada página e como sai
7. **Documentação atualizada** — pelo menos 1 documento de módulo refletindo o estado atual

---

## Resumo da Ordem de Revisão

```
IMEDIATO (antes de tudo)
  └── Fix: Contracts (3 rotas + redirect)
  └── Fix: Runbooks permissão

FASE 1 (revisão modular — alta prioridade)
  ├── Contracts (completo)
  ├── Governance
  ├── AI Hub
  ├── OperationalIntelligence / Operations
  └── Identity Access

FASE 2 (revisão modular — prioridade média)
  ├── Change Governance
  ├── Catalog (Services + SoT + Portal)
  ├── Integrations
  └── Notifications

FASE 3 (revisão complementar)
  ├── Product Analytics
  ├── Audit Compliance
  └── Configuration Admin

PARALELO (documentação)
  ├── Archive 160 ficheiros históricos
  ├── Reescrever MODULES-AND-PAGES
  ├── Expandir user-guide
  └── Atualizar docs técnicos
```
