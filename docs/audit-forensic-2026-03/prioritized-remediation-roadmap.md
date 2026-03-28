# Roadmap de Remediação Priorizado — NexTraceOne
**Auditoria Forense | 28 de Março de 2026**

---

## Princípio Guia

> Fechar os fluxos centrais antes de ampliar superfície. Cada item deve aproximar o NexTraceOne da visão como source of truth, plataforma de governança e plataforma de confiança em mudanças.

---

## BLOCO 1 — Correções Críticas
*Fluxos centrais inoperantes, quebras funcionais severas*

### 1.1 Engine de Correlação Dinâmica incident↔change
**Prioridade: CRÍTICA | Pilar: Operational Reliability | Fluxo: 3**

Estado atual: `EfIncidentStore` (678 linhas) existe com `IncidentDbContext` e `CorrelationEvent` DbSet. Falta a engine que correlaciona dinamicamente por timestamp + serviço + ambiente.

**Ações:**
1. Implementar `CorrelationEngine` em `OperationalIntelligence.Application`
2. Ao criar/atualizar incidente, buscar mudanças recentes no mesmo serviço/ambiente via `ChangeIntelligenceDbContext`
3. Calcular score de correlação por proximidade temporal
4. Persistir em `CorrelationEvent` DbSet
5. Conectar `IncidentsPage.tsx` à API real — remover `mockIncidents` inline
6. Conectar `RunbookPage.tsx` ao `RunbookRecord` DB em vez de 3 hardcoded
7. Conectar `CreateMitigationWorkflow` à persistência real em `MitigationRecord`

**Evidência de estado:** `src/frontend/src/features/operations/` | `src/modules/operationalintelligence/`

---

### 1.2 Conectar AI Assistant a LLM Real
**Prioridade: CRÍTICA | Pilar: AI-assisted Operations | Fluxo: 4**

Estado atual: `SendAssistantMessage` handler (256 linhas) tem lógica de routing e context building mas retorna resposta hardcoded. Ollama configurado em `localhost:11434`. `IExternalAIRoutingPort` existe como abstração.

**Ações:**
1. Implementar `OllamaExternalAIRoutingPort` que chama `localhost:11434` via HTTP
2. Conectar `SendAssistantMessage` handler ao `IExternalAIRoutingPort` → Ollama
3. Conectar `AiAssistantPage.tsx` à API real de conversações — remover `mockConversations`
4. Garantir que `AiTokenUsageLedger` e `AiAuditEntry` são populados com dados reais
5. Implementar os 8 handlers ExternalAI (Phase 03.x stubs)

**Evidência de estado:** `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Application/Features/SendAssistantMessage/` | `AssistantPanel.tsx`

---

### 1.3 Ativar Processamento de Outbox em DbContexts Prioritários
**Prioridade: CRÍTICA | Pilar: Operational Consistency**

Estado atual: Apenas `IdentityDbContext` tem outbox processado. 23 outros DbContexts têm tabelas `OutboxMessages` sem processador. Eventos de domínio não propagam.

**Ações:**
1. Ativar outbox processor para `CatalogGraphDbContext` no BackgroundWorkers
2. Ativar outbox processor para `ChangeIntelligenceDbContext`
3. Ativar outbox processor para `IncidentDbContext`
4. Ativar outbox processor para `AiOrchestrationDbContext`
5. Definir consumidores dos eventos para cada contexto

**Evidência:** `src/platform/NexTraceOne.BackgroundWorkers/`

---

## BLOCO 2 — Correções Estruturais
*Bounded contexts, cross-module interfaces, schema e contratos*

### 2.1 Implementar Cross-Module Interfaces Prioritárias
**Prioridade: ALTA | Impacto: Developer Portal, Governance, FinOps**

**Ordem de implementação:**
1. `IContractsModule` → desbloqueia Developer Portal (7 stubs) e SearchCatalog
2. `IChangeIntelligenceModule` → desbloqueia Governance com dados reais de mudanças
3. `ICostIntelligenceModule` → desbloqueia FinOps real em Governance
4. `IRuntimeIntelligenceModule` → desbloqueia Reliability real
5. `IPromotionModule`, `IRulesetGovernanceModule` (fase seguinte)
6. `IAiOrchestrationModule`, `IExternalAiModule` (junto com Bloco 1.2)

**Evidência:** `src/modules/catalog/NexTraceOne.Catalog.Contracts/Contracts/ServiceInterfaces/IContractsModule.cs`

---

### 2.2 Governance Module — Substituir Handlers Mock por Persistência Real
**Prioridade: ALTA | Pilar: Operational Consistency, FinOps**

Estado atual: 22+ ficheiros, ~74 handlers com `IsSimulated: true`. `GovernanceDbContext` existe com 3 migrações mas não é consultado.

**Ações:**
1. Implementar `ListTeams` consultando `IdentityDbContext` (teams via ownership)
2. Implementar `ListDomains` e `GetDomainDetail` com persistência real
3. Implementar `GetDomainFinOps`, `GetServiceFinOps`, `GetFinOpsTrends` consumindo `CostIntelligenceDbContext` via `ICostIntelligenceModule` (depende de 2.1)
4. Substituir restantes handlers mock progressivamente

---

### 2.3 Resolver Warnings CS8632 Nullable
**Prioridade: ALTA | Pilar: Operational Reliability**

516 warnings de nullable no projeto. Risco de `NullReferenceException` em runtime.

**Ações:**
1. Activar `<Nullable>enable</Nullable>` em `Directory.Build.props`
2. Resolver warnings módulo a módulo começando pelos módulos core (Catalog, ChangeGovernance)
3. Usar `!` (null-forgiving) apenas quando garantido; preferir null-checks reais

---

## BLOCO 3 — Fechamento de Lacunas Estratégicas
*Source of truth, knowledge hub, FinOps, AI governance*

### 3.1 Knowledge Hub — Completar
**Prioridade: ALTA | Pilar: Source of Truth & Operational Knowledge**

Estado atual: `KnowledgeDbContext` com 3 migrações; 34 ficheiros; features básicas implementadas. Sem conectividade com AI context.

**Ações:**
1. Verificar deployabilidade das 3 migrações existentes
2. Implementar endpoints de criação e consulta de notas operacionais
3. Conectar `KnowledgeDbContext` como fonte para context builders de AI
4. Conectar `KnowledgePage` frontend à API real

---

### 3.2 FinOps — Conectar a Dados Reais
**Prioridade: ALTA | Pilar: FinOps Contextual**

Estado atual: `CostIntelligenceDbContext` com 7 migrações existe. Handlers de Governance retornam `IsSimulated: true`.

**Dependência:** Requer `ICostIntelligenceModule` (Bloco 2.1, item 3)

**Ações:**
1. Implementar `ICostIntelligenceModule` em `OperationalIntelligence.Infrastructure`
2. Registar como serviço no ApiHost
3. Atualizar handlers de FinOps em Governance para consumir via interface

---

### 3.3 Definir Estratégia de Licensing
**Prioridade: ESTRATÉGICA | Pilar: Licensing & Entitlements**

Estado atual: Módulo removido em PR-17. Sem enforcement de licença.

**Ações:**
1. Decidir abordagem: novo módulo interno vs. biblioteca de licensing externa
2. Implementar: activation + heartbeat + entitlements mínimos
3. Construir sobre `AssemblyIntegrityChecker` existente
4. Documentar estratégia em ADR

---

### 3.4 Conectores CI/CD para Ingestão de Eventos
**Prioridade: ALTA | Pilar: Change Intelligence**

Estado atual: `Integrations` module com `IntegrationsDbContext` mas conectores são stubs.

**Ações:**
1. Implementar conector GitLab (webhook de deploy/pipeline events)
2. Implementar conector GitHub Actions (workflow run events)
3. Mapear eventos para entidades de `ChangeGovernance` via ingestão
4. Documentar esquema de webhook para cada conector

---

## BLOCO 4 — Limpeza e Consolidação
*Resíduos, duplicação, documentação obsoleta*

### 4.1 Arquivar Relatórios de Fases Anteriores
**Prioridade: BAIXA**

Mover `docs/audits/` para `docs/archive/audits/` com README explicativo.

### 4.2 Atualizar FRONTEND-ARCHITECTURE.md e DESIGN-SYSTEM.md
**Prioridade: MÉDIA**

Remover referências a TanStack Router e Radix UI que não estão implementados. Documentar stack real.

### 4.3 Marcar AI Docs como Aspiracionais
**Prioridade: MÉDIA**

`AI-ASSISTED-OPERATIONS.md` e `AI-DEVELOPER-EXPERIENCE.md` devem indicar claramente "visão futura" vs. "estado actual".

### 4.4 Verificar `fix-pagination-defaults.ps1`
**Prioridade: BAIXA**

Script pontual — remover se objetivo já alcançado.

---

## BLOCO 5 — Experiência e Produto
*UI por persona, dashboards coerentes, i18n, responsividade*

### 5.1 E2E como Gate Obrigatório de Merge
**Prioridade: ALTA | Impacto: Qualidade garantida no merge**

Adicionar Playwright E2E ao `ci.yml` como job obrigatório. Sem E2E, regressões críticas podem entrar em `main`.

### 5.2 Reescrever incidents.spec.ts
**Prioridade: ALTA** (após completar Bloco 1.1)

Quando `IncidentsPage.tsx` estiver conectada à API real, reescrever o E2E para validar correlação dinâmica real.

### 5.3 Padronizar Loading, Error e Empty States
**Prioridade: MÉDIA | Impacto: UX consistente**

Garantir que todas as páginas têm estados de loading, erro e vazio consistentes. `PageStateDisplay.tsx` já existe — usar em todas as páginas.

### 5.4 Avaliar Alinhamento de Stack Frontend
**Prioridade: MÉDIA | Impacto: Dívida técnica vs. roadmap**

Decisão: manter react-router-dom v7 (que é excelente) ou migrar para TanStack Router conforme CLAUDE.md. Documentar decisão como ADR.

### 5.5 Persona Awareness no Dashboard
**Prioridade: MÉDIA | Impacto: Produto diferenciado**

`DashboardPage.tsx` deve adaptar-se por persona. `PersonaQuickstart.tsx` existe (150 linhas). Implementar lógica de personalização por persona baseada em `ICurrentUser`.

---

## Tabela Resumo de Prioridades

| Item | Bloco | Prioridade | Pilar | Esforço |
|---|---|---|---|---|
| Engine correlação incident↔change | 1.1 | **CRÍTICA** | Operational Reliability | Alto |
| AI Assistant → LLM real | 1.2 | **CRÍTICA** | AI-assisted Operations | Alto |
| Outbox processing (4 DbContexts) | 1.3 | **CRÍTICA** | Operational Consistency | Médio |
| `IContractsModule` cross-module | 2.1 | **ALTA** | Service Governance | Médio |
| `IChangeIntelligenceModule` | 2.1 | **ALTA** | Operational Intelligence | Médio |
| `ICostIntelligenceModule` | 2.1 | **ALTA** | FinOps | Médio |
| Governance handlers reais | 2.2 | **ALTA** | Operational Consistency | Alto |
| CS8632 nullable warnings | 2.3 | **ALTA** | Reliability | Médio |
| Knowledge Hub completar | 3.1 | **ALTA** | Source of Truth | Médio |
| FinOps dados reais | 3.2 | **ALTA** | FinOps | Médio (depende 2.1) |
| Estratégia Licensing | 3.3 | **ESTRATÉGICA** | Licensing | Alto |
| Conectores CI/CD | 3.4 | **ALTA** | Change Intelligence | Alto |
| E2E como gate CI | 5.1 | **ALTA** | Qualidade | Baixo |
| Reescrever incidents.spec.ts | 5.2 | **ALTA** (pós 1.1) | Qualidade | Médio |
| Arquivar docs de fases | 4.1 | Baixa | — | Baixo |
| Atualizar FRONTEND-ARCHITECTURE | 4.2 | Média | Documentação | Baixo |
| Padronizar loading/error/empty | 5.3 | Média | UX | Médio |
| Avaliar stack frontend | 5.4 | Média | Technical Debt | Alto |
| Persona awareness dashboard | 5.5 | Média | UX | Médio |

---

## Sequência de Execução Recomendada

```
Sprint 1: 1.1 (Incident correlation) + 1.3 (Outbox activation)
Sprint 2: 1.2 (AI Assistant LLM) + 2.1 IContractsModule
Sprint 3: 2.1 IChangeIntelligenceModule + 2.2 Governance partial
Sprint 4: 2.1 ICostIntelligenceModule + 3.2 FinOps
Sprint 5: 3.1 Knowledge Hub + 3.4 CI/CD connectors
Sprint 6: 2.3 Nullable + 5.1 E2E gate + 4.2 Docs
Parallel: 3.3 Licensing strategy (depende de decisão de produto)
```

---

*Data: 28 de Março de 2026*
