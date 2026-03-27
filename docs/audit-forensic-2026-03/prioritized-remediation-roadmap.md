# Roadmap de Remediação Priorizado — NexTraceOne
**Auditoria Forense | Março 2026**

---

## Princípio Guia

> Fechar os fluxos centrais antes de ampliar superfície. Cada item deve aproximar o NexTraceOne da visão como source of truth, plataforma de governança e plataforma de confiança em mudanças.

---

## BLOCO 1 — Correções Críticas
*Segurança, quebras funcionais severas, fluxos centrais inoperantes*

### 1.1 Engine de Correlação Dinâmica incident↔change
**Prioridade: CRÍTICA | Esforço estimado: Alto**

O fluxo 3 (Incident Correlation & Mitigation) está 0% funcional. EfIncidentStore existe, IncidentDbContext tem 5 DbSets e migração. Falta a engine que correlaciona incidentes com mudanças via timestamps e serviços.

**Ações:**
1. Implementar engine de correlação em `OperationalIntelligence.Application`
2. Adicionar `POST /incidents` e `PATCH /incidents/{id}/correlation`
3. Substituir dados hardcoded no `GetMitigationHistory` por queries EF reais
4. Conectar `IncidentsPage.tsx` à API real (remover `mockIncidents` inline)
5. Usar `RunbookRecord` persistido em vez dos 3 hardcoded

**Evidência:** `docs/CORE-FLOW-GAPS.md` §Fluxo 3

---

### 1.2 Conectar AI Assistant a LLM Real
**Prioridade: CRÍTICA | Esforço estimado: Alto**

`SendAssistantMessage` retorna respostas hardcoded. Ollama está configurado (`localhost:11434`). `IExternalAIRoutingPort` existe como abstração.

**Ações:**
1. Conectar `SendAssistantMessage` handler ao `IExternalAIRoutingPort` → Ollama provider
2. Implementar os 8 handlers ExternalAI (TODO stubs)
3. Conectar `AiAssistantPage.tsx` à API de conversas real
4. Garantir que conversas são persistidas e auditadas via AiGovernanceDbContext

**Evidência:** `docs/CORE-FLOW-GAPS.md` §Fluxo 4

---

### 1.3 Ativar Processamento de Outbox em Todos os DbContexts
**Prioridade: CRÍTICA | Esforço estimado: Médio**

Apenas IdentityDbContext tem outbox processado. 23 DbContexts têm tabelas de outbox sem processamento. Eventos de domínio não propagam entre módulos.

**Ações:**
1. Identificar DbContexts prioritários para outbox (Catalog, ChangeGovernance, OperationalIntelligence)
2. Ativar outbox processor para cada DbContext prioritário no BackgroundWorkers
3. Definir consumidores dos eventos para cada contexto

**Evidência:** `docs/IMPLEMENTATION-STATUS.md` §Infrastructure

---

## BLOCO 2 — Correções Estruturais
*Bounded contexts, schema/modelagem, contratos, cross-module interfaces*

### 2.1 Implementar Cross-Module Interfaces Prioritárias
**Prioridade: ALTA | Esforço estimado: Alto**

8 interfaces cross-module estão definidas como PLAN sem consumidores. Governance, FinOps e Developer Portal dependem destas interfaces.

**Ordem de implementação:**
1. `IContractsModule` — desbloqueia Developer Portal (7 stubs)
2. `IChangeIntelligenceModule` — desbloqueia Governance real
3. `ICostIntelligenceModule` — desbloqueia FinOps real
4. `IRuntimeIntelligenceModule` — desbloqueia Reliability real
5. `IPromotionModule`, `IRulesetGovernanceModule` (fase seguinte)
6. `IAiOrchestrationModule`, `IExternalAiModule` (junto com Bloco 1.2)

**Evidência:** `docs/IMPLEMENTATION-STATUS.md` §Cross-Module Contract Health

---

### 2.2 Gerar Migrações para DbContexts sem Schema Deployável
**Prioridade: ALTA | Esforço estimado: Médio**

DbContexts com ModelSnapshot mas sem migrações confirmadas não podem ser deployados.

**Ações por DbContext:**
1. `RuntimeIntelligenceDbContext` — `dotnet ef migrations add InitialCreate`
2. `CostIntelligenceDbContext` — idem
3. `AiOrchestrationDbContext` — idem
4. `ExternalAiDbContext` — idem
5. `IntegrationsDbContext` — idem
6. `KnowledgeDbContext` — idem
7. `ProductAnalyticsDbContext` — idem (apenas se product analytics for prioridade)

**Evidência:** `docs/REBASELINE.md` §Dívidas A1, A2

---

### 2.3 Governance Module — Persistence Layer
**Prioridade: ALTA | Esforço estimado: Alto**

74 handlers retornam `IsSimulated: true`. Para que Governance seja real, precisa de persistence própria ou consumo real via cross-module interfaces.

**Abordagem recomendada:**
1. Implementar cross-module interfaces primeiro (2.1)
2. Substituir handlers mock de Teams/Domains por queries a IdentityAccess e Catalog
3. Substituir handlers de FinOps por queries a CostIntelligenceModule
4. Adicionar persistence própria apenas para dados que não existem noutros módulos (ex: Governance Packs, Waivers)

---

### 2.4 Configurar OTEL Endpoint por Ambiente
**Prioridade: ALTA | Esforço estimado: Baixo**

`appsettings.json` aponta OTEL para `localhost:4317`. Em produção, precisa de endpoint real.

**Ação:** Adicionar `OTEL_EXPORTER_OTLP_ENDPOINT` como env var obrigatória em produção e documentar no .env.example.

---

## BLOCO 3 — Fechamento de Lacunas Estratégicas
*Source of truth, contract governance, AI governance, knowledge hub, FinOps*

### 3.1 FinOps com Dados Reais
**Prioridade: ALTA | Esforço estimado: Médio**

CostIntelligenceDbContext existe com dados reais (CostSnapshot, trends, reports). Frontend FinOps consome Governance module que retorna mock.

**Ações:**
1. Implementar ICostIntelligenceModule (ver 2.1)
2. Redirecionar handlers de FinOps no Governance para consumir ICostIntelligenceModule
3. Remover `IsSimulated: true` dos handlers de FinOps

---

### 3.2 Knowledge Hub com Migrações
**Prioridade: ALTA | Esforço estimado: Médio**

KnowledgeDbContext existe sem migrações. Runbooks usam seed data. Conhecimento operacional é central para a visão do produto.

**Ações:**
1. Gerar migração para KnowledgeDbContext
2. Conectar handlers de runbooks ao KnowledgeDbContext real
3. Completar UX de Knowledge Hub no frontend

---

### 3.3 Conectar Enriquecimento de Contexto IA a Dados Reais
**Prioridade: ALTA | Esforço estimado: Médio**

`POST /ai/context/enrich` existe mas sem retrieval real. Contratos e mudanças têm dados reais acessíveis.

**Ações:**
1. Implementar retrieval de contratos via IContractsModule
2. Implementar retrieval de mudanças via IChangeIntelligenceModule
3. Implementar retrieval de incidents via IncidentDbContext
4. Validar grounding com LLM real (após Bloco 1.2)

---

### 3.4 Integração CI/CD — 1 Conector Real
**Prioridade: MÉDIA | Esforço estimado: Alto**

Change Intelligence depende de eventos de deploy. Ingestion API recebe payloads mas não processa.

**Ações:**
1. Processar payload real na Ingestion API para pelo menos GitHub Actions
2. Mapear evento de deploy para CreateRelease no ChangeGovernance
3. Documentar modelo canônico de evento de deploy

---

## BLOCO 4 — Limpeza e Consolidação

### 4.1 Eliminar 516 Warnings CS8632 Nullable
**Prioridade: MÉDIA | Esforço estimado: Médio**

**Ações:**
1. Habilitar `<Nullable>enable</Nullable>` no Directory.Build.props
2. Corrigir warnings módulo a módulo começando pelos core (Catalog, ChangeGovernance)
3. Adicionar gate de zero warnings no CI

---

### 4.2 Arquivar Documentação Histórica
**Prioridade: BAIXA | Esforço estimado: Baixo**

**Ações:**
1. Mover `docs/architecture/e14-* a e18-*` para `docs/archive/historical-execution/`
2. Mover `docs/architecture/p0-* a p1-*` para `docs/archive/security-hardening/`
3. Mover `docs/11-review-modular/` para `docs/archive/module-reviews/`
4. Criar `CURRENT-STATE.md` por módulo como fonte atual

---

### 4.3 Consolidar Documentação de Estado
**Prioridade: MÉDIA | Esforço estimado: Baixo**

**Ações:**
1. Atualizar `IMPLEMENTATION-STATUS.md` Incidents section para refletir EfIncidentStore
2. Atualizar `ROADMAP.md` Fluxo 3 para estado real
3. Criar protocolo de atualização semanal do REBASELINE.md

---

## BLOCO 5 — Experiência e Produto

### 5.1 Padronizar Loading States, Empty States e Error States
**Prioridade: MÉDIA | Esforço estimado: Médio**

83% das páginas sem EmptyState padronizado. 96% sem error states por secção.

**Ações:**
1. Criar componente `StateDisplay` (loading, empty, error) padronizado
2. Aplicar em todas as páginas core (Catalog, Change, Identity)
3. Aplicar em páginas restantes progressivamente

---

### 5.2 E2E Tests como Gate Obrigatório
**Prioridade: ALTA | Esforço estimado: Baixo**

E2E tests correm nightly mas não bloqueiam PRs.

**Ações:**
1. Adicionar E2E tests ao ci.yml como job obrigatório (subset dos specs mais críticos)
2. Ou: adicionar regra de branch protection que bloqueia merge se nightly E2E falhou

---

### 5.3 Dashboard com Semântica de Persona
**Prioridade: MÉDIA | Esforço estimado: Alto**

DashboardPage.tsx parece genérico — não reflete papel do utilizador autenticado.

**Ações:**
1. Conectar dashboard ao perfil de utilizador (roles, teams, owned services)
2. Mostrar mudanças recentes relevantes para a persona
3. Mostrar alertas de confiança em produção para Tech Lead / Architect
4. Mostrar KPIs de FinOps para Executive

---

## Resumo de Prioridades

| Bloco | Ações | Impacto | Quando |
|---|---|---|---|
| 1 — Correções Críticas | 3 ações | Ativa 2 fluxos centrais quebrados | Sprint imediato |
| 2 — Correções Estruturais | 4 ações | Fundação para Governance e FinOps reais | Sprint seguinte |
| 3 — Lacunas Estratégicas | 4 ações | Fecha pilares de produto | 2-3 sprints |
| 4 — Limpeza | 3 ações | Qualidade e clareza | Paralelo |
| 5 — Experiência | 3 ações | UX e confiança no produto | 3-4 sprints |
