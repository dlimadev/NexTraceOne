# Roadmap de Remediação Priorizado

**Projeto:** NexTraceOne
**Data da Avaliação:** 2026-03-27
**Escopo:** Plano de acção sequencial para resolver lacunas identificadas na avaliação de alinhamento

---

## 1. Visão Geral da Estratégia

Este roadmap organiza as remediações em 5 blocos sequenciais, priorizados pela combinação de:

1. **Impacto no produto** — Quanto a correção aproxima o NexTraceOne da visão de "fonte de verdade"
2. **Risco técnico** — Quanto a lacuna pode comprometer a credibilidade do sistema
3. **Dependências** — Quais correções desbloqueiam outras
4. **Esforço estimado** — Tamanho relativo da intervenção

### Princípio Orientador

As correções fundacionais (migrações, CancellationToken) devem ser resolvidas primeiro porque são pré-requisitos para trabalho subsequente. A eliminação de dados simulados é a segunda prioridade porque afecta directamente a credibilidade do produto como fonte de verdade.

### Resumo dos Blocos

| Bloco | Foco | Sprints | Itens | Esforço Relativo |
|-------|------|---------|-------|------------------|
| 1 | Correções Críticas | 1-2 | 6 | Alto |
| 2 | Correções Estruturais | 2-4 | 6 | Alto |
| 3 | Lacunas Estratégicas | 4-8 | 6 | Muito Alto |
| 4 | Limpeza e Consolidação | 3-6 | 4 | Médio |
| 5 | Produto e Experiência | 5-10 | 5 | Alto |

---

## 2. Bloco 1: Correções Críticas (Sprint 1-2)

**Objectivo:** Resolver lacunas fundacionais que impedem o funcionamento correcto de módulos inteiros e violam requisitos de qualidade enterprise.

**Justificação:** Sem migrações EF, módulos não podem persistir dados. Sem `CancellationToken`, operações async não podem ser canceladas correctamente. Sem eliminar `IsSimulated`, dados apresentados são falsos.

---

### P-CRIT-01: Adicionar CancellationToken a ~237 métodos async

**Prioridade:** CRÍTICA
**Esforço:** Alto (espalhado por todos os módulos)
**Risco:** Baixo (alteração mecânica, sem mudança de lógica)

**Descrição:**
Aproximadamente 237 métodos async no backend não recebem `CancellationToken` como parâmetro. Este é um requisito explícito das regras do produto (secção 20.1: "CancellationToken em toda operação async") e uma prática obrigatória para qualidade enterprise.

**Módulos afectados:**
- `identityaccess` — Repositories em `NexTraceOne.IdentityAccess.Infrastructure/Persistence/Repositories/`
- `governance` — Repositories em `NexTraceOne.Governance.Infrastructure/Persistence/Repositories/GovernanceRepositories.cs`
- `catalog` — Repositories e handlers em múltiplos sub-módulos
- `changegovernance` — Repositories em 4 sub-módulos de infraestrutura
- `operationalintelligence` — Repositories em 5 sub-módulos
- `aiknowledge` — Repositories em 3 sub-módulos
- `auditcompliance` — Repositories
- `notifications` — Repositories
- `configuration` — Repositories
- `integrations` — Repositories
- `knowledge` — Repositories
- `productanalytics` — Repositories

**Abordagem recomendada:**
1. Processar módulo a módulo, começando pelos mais utilizados
2. Propagar `CancellationToken` de handlers MediatR → services → repositories → EF queries
3. Usar `.ToListAsync(cancellationToken)`, `.FirstOrDefaultAsync(cancellationToken)`, etc.
4. Verificar que interfaces de repositório também são actualizadas

**Critério de conclusão:**
- Zero métodos async públicos sem `CancellationToken`
- Build sem erros
- Testes existentes passam

---

### P-CRIT-02: Criar migrações EF para módulo Knowledge

**Prioridade:** CRÍTICA
**Esforço:** Médio
**Risco:** Baixo (módulo novo, sem dados existentes)

**Descrição:**
O módulo Knowledge tem `KnowledgeDbContext` definido mas sem migrações EF, o que impede qualquer persistência de dados. Tabelas esperadas com prefixo `knw_`.

**Ficheiros de referência:**
- `src/modules/knowledge/NexTraceOne.Knowledge.Infrastructure/Persistence/KnowledgeDbContext.cs`

**Acções:**
1. Verificar entity configurations no `KnowledgeDbContext`
2. Gerar migração inicial: `dotnet ef migrations add InitialCreate --project <Knowledge.Infrastructure> --startup-project <ApiHost>`
3. Verificar que tabelas `knw_*` são geradas correctamente
4. Aplicar migração em ambiente de desenvolvimento
5. Validar que operações CRUD básicas funcionam

**Dependências:**
- Nenhuma (módulo independente)

**Critério de conclusão:**
- Migração criada e aplicável
- Tabelas `knw_*` criadas na base de dados
- Operações de escrita/leitura funcionais

---

### P-CRIT-03: Criar migrações EF para módulo Integrations

**Prioridade:** CRÍTICA
**Esforço:** Médio
**Risco:** Baixo (módulo novo, sem dados existentes)

**Descrição:**
O módulo Integrations tem `IntegrationsDbContext` definido com entidades para connectors, ingestion sources e executions (tabelas `int_*`), mas sem migrações EF formais.

**Ficheiros de referência:**
- `src/modules/integrations/NexTraceOne.Integrations.Infrastructure/Persistence/IntegrationsDbContext.cs`

**Acções:**
1. Verificar entity configurations e mapeamentos
2. Gerar migração inicial
3. Verificar tabelas `int_connectors`, `int_ingestion_sources`, `int_ingestion_executions`
4. Aplicar e validar

**Dependências:**
- Nenhuma (módulo independente)

**Critério de conclusão:**
- Migração criada e aplicável
- Tabelas `int_*` criadas correctamente

---

### P-CRIT-04: Criar migrações EF para módulo ProductAnalytics

**Prioridade:** CRÍTICA
**Esforço:** Médio
**Risco:** Baixo (módulo novo, sem dados existentes)

**Descrição:**
O módulo ProductAnalytics tem `ProductAnalyticsDbContext` definido com tabelas `pan_*` esperadas, mas sem migrações EF formais.

**Ficheiros de referência:**
- `src/modules/productanalytics/NexTraceOne.ProductAnalytics.Infrastructure/Persistence/ProductAnalyticsDbContext.cs`

**Acções:**
1. Verificar entity configurations
2. Gerar migração inicial
3. Verificar tabela `pan_analytics_events` e outras
4. Aplicar e validar

**Dependências:**
- Nenhuma (módulo independente)

**Critério de conclusão:**
- Migração criada e aplicável
- Tabelas `pan_*` criadas correctamente

---

### P-CRIT-05: Substituir IsSimulated em 13 handlers de OperationalIntelligence

**Prioridade:** CRÍTICA
**Esforço:** Alto
**Risco:** Médio (mudança de lógica; dados reais podem revelar bugs)

**Descrição:**
13 handlers no módulo OperationalIntelligence retornam dados simulados com `IsSimulated = true`. Isto afecta directamente os pilares de Service Reliability e Operational Intelligence, tornando qualquer informação de SLO, burn rate, error budget e health score uma demonstração sem valor operacional real.

**Handlers afectados (dentro de `src/modules/operationalintelligence/NexTraceOne.OperationalIntelligence.Application/Features/`):**
- Handlers de reliability snapshots
- Handlers de burn rates
- Handlers de error budgets
- Handlers de service health
- Handlers de cost intelligence
- Handlers de automation audit trail

**Abordagem recomendada:**
1. Para cada handler, substituir dados fabricados por queries reais ao respectivo DbContext
2. Quando não existirem dados reais, retornar conjuntos vazios com empty state adequado
3. Remover flag `IsSimulated` do response DTO ou defini-la como `false`
4. Manter contratos de resposta estáveis (não alterar DTOs publicados)
5. Adicionar logging quando queries retornarem conjuntos vazios

**Critério de conclusão:**
- Zero handlers com `IsSimulated = true`
- Build sem erros
- Frontend mostra empty states quando não há dados (em vez de dados falsos)

---

### P-CRIT-06: Substituir IsSimulated em handlers de Governance FinOps

**Prioridade:** CRÍTICA
**Esforço:** Alto
**Risco:** Médio (mesmo padrão de P-CRIT-05)

**Descrição:**
Handlers de FinOps no módulo Governance retornam dados simulados, afectando 6+ páginas frontend.

**Handlers afectados:**
- `src/modules/governance/NexTraceOne.Governance.Application/Features/GetDomainFinOps/GetDomainFinOps.cs`
- `src/modules/governance/NexTraceOne.Governance.Application/Features/GetServiceFinOps/GetServiceFinOps.cs`
- `src/modules/governance/NexTraceOne.Governance.Application/Features/GetTeamFinOps/GetTeamFinOps.cs`
- `src/modules/governance/NexTraceOne.Governance.Application/Features/GetFinOpsSummary/GetFinOpsSummary.cs`
- `src/modules/governance/NexTraceOne.Governance.Application/Features/GetFinOpsTrends/GetFinOpsTrends.cs`
- `src/modules/governance/NexTraceOne.Governance.Application/Features/GetBenchmarking/GetBenchmarking.cs`
- `src/modules/governance/NexTraceOne.Governance.Application/Features/GetWasteSignals/GetWasteSignals.cs`
- `src/modules/governance/NexTraceOne.Governance.Application/Features/GetEfficiencyIndicators/GetEfficiencyIndicators.cs`
- `src/modules/governance/NexTraceOne.Governance.Application/Features/GetExecutiveTrends/GetExecutiveTrends.cs`
- `src/modules/governance/NexTraceOne.Governance.Application/Features/GetExecutiveDrillDown/GetExecutiveDrillDown.cs`

**Inclui também ProductAnalytics:**
- `src/modules/productanalytics/NexTraceOne.ProductAnalytics.Application/Features/GetFrictionIndicators/GetFrictionIndicators.cs`

**Abordagem recomendada:**
1. Mesma abordagem de P-CRIT-05
2. Substituir dados fabricados por queries reais a `GovernanceDbContext` e `CostIntelligenceDbContext`
3. Quando não houver dados de custo reais, retornar resposta com valores zero e indicação clara de "sem dados"
4. Frontend deve interpretar conjuntos vazios com empty state (pode remover DemoBanner)

**Critério de conclusão:**
- Zero handlers FinOps com `IsSimulated = true`
- Páginas frontend mostram empty states adequados

---

## 3. Bloco 2: Correções Estruturais (Sprint 2-4)

**Objectivo:** Completar handlers vazios e substituir mocks no frontend, eliminando funcionalidade ilusória.

**Justificação:** Handlers vazios e mocks frontend criam a impressão de funcionalidade completa quando na realidade não existe implementação. Isto é um anti-padrão explícito do produto.

---

### P-STRUCT-01: Implementar handlers reais de ExternalAI

**Prioridade:** ALTA
**Esforço:** Alto
**Risco:** Médio (integração com providers externos)

**Descrição:**
6 handlers de ExternalAI no módulo AIKnowledge estão vazios (D-024 a D-029), impedindo integração real com providers de IA externos (OpenAI, Azure OpenAI, Anthropic, etc.).

**Ficheiros afectados:**
- `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Application/` — handlers em Features de ExternalAI

**Acções:**
1. Implementar handler de registo de provider externo
2. Implementar handler de configuração de endpoint/credenciais
3. Implementar handler de teste de conectividade
4. Implementar handler de invocação real via HTTP client
5. Implementar handler de gestão de lifecycle (activar/desactivar)
6. Implementar handler de monitorização de uso

**Dependências:**
- `ExternalAiDbContext` (já existe com migrações)
- Model Registry (READY)

---

### P-STRUCT-02: Implementar handlers reais de Orchestration

**Prioridade:** ALTA
**Esforço:** Alto
**Risco:** Médio

**Descrição:**
5 handlers de Orchestration no módulo AIKnowledge estão vazios (D-046), limitando a capacidade de agentes especializados.

**Ficheiros afectados:**
- `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Application/` — handlers em Features de Orchestration

**Acções:**
1. Implementar handler de criação de sessão de orquestração
2. Implementar handler de execução de pipeline de agente
3. Implementar handler de gestão de contexto de conversação
4. Implementar handler de routing de modelo por política
5. Implementar handler de agregação de resultados

**Dependências:**
- `AiOrchestrationDbContext` (já existe com migrações)
- P-STRUCT-01 (ExternalAI para invocação real)

---

### P-STRUCT-03: Completar handlers com TODO no Governance

**Prioridade:** ALTA
**Esforço:** Médio
**Risco:** Baixo

**Descrição:**
8 handlers no módulo Governance têm marcadores TODO indicando implementação incompleta (D-039, D-040, D-047). Isto afecta governance packs, compliance reporting e risk assessment.

**Ficheiros afectados:**
- `src/modules/governance/NexTraceOne.Governance.Application/Features/` — handlers específicos com TODO

**Acções:**
1. Listar todos os TODOs nos handlers
2. Para cada TODO, implementar a lógica pendente
3. Verificar que os contratos de resposta são cumpridos
4. Adicionar testes unitários para os fluxos completados

**Dependências:**
- `GovernanceDbContext` (já existe com migrações)

---

### P-STRUCT-04: Substituir dados mock em 6 páginas frontend

**Prioridade:** ALTA
**Esforço:** Médio
**Risco:** Baixo (frontend apenas)

**Descrição:**
6 páginas frontend (D-030 a D-035) utilizam dados mock hardcoded em vez de consumir dados reais da API. Estas páginas mostram `DemoBanner` indicando dados de demonstração.

**Páginas afectadas:**
- `FinOpsPage`
- `DomainFinOpsPage`
- `ServiceFinOpsPage`
- `TeamFinOpsPage`
- `ExecutiveFinOpsPage`
- `BenchmarkingPage`

**Acções:**
1. Para cada página, substituir dados mock por chamadas API reais (TanStack Query)
2. Implementar estados de loading, erro e vazio adequados
3. Remover `DemoBanner` quando os dados vierem da API real
4. Manter i18n em todos os textos
5. Testar com e sem dados no backend

**Dependências:**
- P-CRIT-06 (handlers FinOps com dados reais)

---

### P-STRUCT-05: Adicionar projecto Contracts ao módulo ProductAnalytics

**Prioridade:** MÉDIA
**Esforço:** Baixo
**Risco:** Baixo

**Descrição:**
O módulo ProductAnalytics não tem projecto `NexTraceOne.ProductAnalytics.Contracts`, quebrando o padrão de comunicação entre módulos via contratos públicos.

**Acções:**
1. Criar projecto `NexTraceOne.ProductAnalytics.Contracts`
2. Mover DTOs públicos e events para o projecto de contratos
3. Actualizar referências entre módulos
4. Adicionar ao `NexTraceOne.sln`

**Dependências:**
- Nenhuma

---

### P-STRUCT-06: Implementar event publishing para Integrations/ProductAnalytics

**Prioridade:** MÉDIA
**Esforço:** Médio
**Risco:** Baixo

**Descrição:**
Os módulos Integrations e ProductAnalytics não publicam eventos de domínio, impedindo reacção cross-module a alterações.

**Acções:**
1. Definir eventos de domínio relevantes (e.g., `IntegrationConnected`, `AnalyticsEventRecorded`)
2. Implementar publicação via MediatR notifications
3. Adicionar handlers de reacção nos módulos interessados
4. Testar fluxo de publicação e consumo

**Dependências:**
- P-CRIT-03, P-CRIT-04 (migrações necessárias primeiro)

---

## 4. Bloco 3: Lacunas Estratégicas (Sprint 4-8)

**Objectivo:** Preencher lacunas que impedem o produto de cumprir a sua proposta de valor diferenciadora.

**Justificação:** Estas lacunas representam capacidades centrais do produto que existem conceptualmente mas não estão operacionais.

---

### P-STRAT-01: Completar módulo Knowledge

**Prioridade:** ALTA
**Esforço:** Muito Alto
**Risco:** Médio

**Descrição:**
O módulo Knowledge é novo e incompleto. Para cumprir o pilar "Source of Truth & Operational Knowledge", precisa de:

**Funcionalidades a implementar:**
1. Full-Text Search dentro de artigos de conhecimento
2. Operações de update e delete de artigos
3. Relações cross-module: knowledge ↔ serviço, knowledge ↔ contrato, knowledge ↔ mudança
4. Categorização e tagging de artigos
5. Versionamento de artigos
6. Contribuições e aprovações
7. Frontend dedicado (Knowledge Hub pages)

**Dependências:**
- P-CRIT-02 (migrações do Knowledge)

---

### P-STRAT-02: Completar módulo Integrations

**Prioridade:** ALTA
**Esforço:** Muito Alto
**Risco:** Alto (integração com sistemas externos)

**Descrição:**
O módulo Integrations tem estrutura mas sem provider adapters reais. Para que o NexTraceOne ingira dados de mudanças, deploys e pipelines, precisa de conectores funcionais.

**Adapters a implementar:**
1. **GitLab** — webhooks de deploy, merge requests, pipelines
2. **Jenkins** — build events, deploy events
3. **GitHub** — webhooks de deploy, pull requests, actions
4. **Azure DevOps** — release events, pipeline events

**Dependências:**
- P-CRIT-03 (migrações do Integrations)

---

### P-STRAT-03: Melhoramento do AI Grounding

**Prioridade:** ALTA
**Esforço:** Alto
**Risco:** Médio

**Descrição:**
O AI grounding actual é básico. Para que a IA do NexTraceOne seja verdadeiramente contextual, precisa de:

1. Cross-module entity lookup (serviços, contratos, mudanças, incidentes)
2. Vector search para documentação e conhecimento operacional
3. Retrieval-Augmented Generation (RAG) com fontes governadas
4. Grounding contextual por tenant, ambiente e permissões

**Dependências:**
- P-STRAT-01 (Knowledge completo para indexação)
- P-STRUCT-01 (ExternalAI para invocação real)

---

### P-STRAT-04: Ingestão real de telemetria para ChangeGovernance

**Prioridade:** ALTA
**Esforço:** Muito Alto
**Risco:** Alto (volume de dados, performance)

**Descrição:**
Os 4 DbContexts de ChangeGovernance (`ChangeIntelligenceDbContext`, `PromotionDbContext`, `RulesetGovernanceDbContext`, `WorkflowDbContext`) têm entidades para scoring, blast radius e correlação, mas dependem de dados reais de deploy e telemetria.

**Funcionalidades a implementar:**
1. Ingestão de eventos de deploy via webhooks/Integrations
2. Correlação temporal entre deploys e métricas de runtime
3. Cálculo real de blast radius baseado em topologia de serviços
4. Scoring de confiança baseado em dados comparativos (non-prod vs. prod)
5. Validação pós-change com métricas reais

**Dependências:**
- P-STRAT-02 (Integrations para ingestão de eventos)
- P-CRIT-05 (IsSimulated removido em OperationalIntelligence)

---

### P-STRAT-05: Integração real de FinOps

**Prioridade:** MÉDIA
**Esforço:** Alto
**Risco:** Médio

**Descrição:**
FinOps contextual é um pilar do produto mas todos os dados são simulados. É necessário integrar com fontes reais de custo.

**Funcionalidades a implementar:**
1. Ingestão de dados de custo de cloud providers (AWS Cost Explorer, Azure Cost Management)
2. Atribuição de custo por serviço, equipa e domínio
3. Correlação de custo com mudanças (cost attribution)
4. Detecção de desperdício baseada em métricas reais
5. Benchmarking com dados históricos reais

**Dependências:**
- P-CRIT-06 (IsSimulated removido em FinOps handlers)
- P-STRAT-02 (Integrations para conectores de custo)

---

### P-STRAT-06: Módulo de Licensing & Entitlements

**Prioridade:** MÉDIA (mas estratégica para deployment enterprise)
**Esforço:** Muito Alto
**Risco:** Alto (segurança, anti-tampering)

**Descrição:**
O módulo Licensing não existe e é requisito estratégico para deployment enterprise e self-hosted. Conforme documentado nas regras do produto (secção 17).

**Funcionalidades a implementar:**
1. Activação de licença (online e offline)
2. Validação recorrente com heartbeat
3. Machine fingerprinting
4. Entitlements por capacidade/módulo
5. Trial/freemium
6. Revogação remota
7. Assembly integrity verification
8. Anti-tampering básico
9. Storage de licença e adapters

**Dependências:**
- Nenhuma (módulo novo independente)

---

## 5. Bloco 4: Limpeza e Consolidação (Sprint 3-6)

**Objectivo:** Reduzir dívida técnica acumulada e alinhar decisões técnicas com a visão documentada.

**Justificação:** Resíduos técnicos e desvios da stack alvo, se não tratados, acumulam-se e dificultam evolução futura.

---

### P-CLEAN-01: Arquivar documentação obsoleta

**Prioridade:** BAIXA
**Esforço:** Baixo
**Risco:** Baixo

**Descrição:**
Identificados ~704 documentos antigos em `docs/` que podem estar desactualizados face ao estado actual do projecto.

**Acções:**
1. Mover documentos obsoletos para `docs/archive/`
2. Preservar referências importantes
3. Actualizar índices documentais

---

### P-CLEAN-02: Consolidar enum TrendDirection duplicado

**Prioridade:** BAIXA
**Esforço:** Baixo
**Risco:** Baixo

**Descrição:**
Enum `TrendDirection` pode estar duplicado em múltiplos módulos. Consolidar numa localização canónica.

**Acções:**
1. Identificar todas as definições de `TrendDirection`
2. Criar definição canónica em shared kernel ou contracts
3. Actualizar referências

---

### P-CLEAN-03: Rever itens de dívida whitelisted

**Prioridade:** BAIXA
**Esforço:** Variável
**Risco:** Baixo

**Descrição:**
À medida que itens do roadmap são concluídos, rever a lista de dívida técnica whitelisted e remover items resolvidos.

**Acções:**
1. Manter registo de itens whitelisted
2. Após cada sprint, verificar quais foram resolvidos
3. Actualizar documentação de dívida

---

### P-CLEAN-04: Alinhar React Router v7 com TanStack Router documentado

**Prioridade:** MÉDIA
**Esforço:** Alto (se migrar) ou Baixo (se documentar decisão)
**Risco:** Alto (se migrar — mudança estrutural no frontend)

**Descrição:**
O frontend usa React Router v7 (`^7.13.1`) e React 19 (`^19.2.0`), enquanto a stack alvo documentada especifica TanStack Router e React 18. Zustand também não é utilizado.

**Opções:**
1. **Migrar para TanStack Router** — Esforço alto, risco alto, alinha com documentação
2. **Documentar decisão de manter React Router v7** — Esforço baixo, actualiza documentação para reflectir realidade

**Recomendação:** Documentar formalmente a decisão. React Router v7 é moderno e funcional; a migração não traz valor proporcional ao risco nesta fase.

**Acções mínimas:**
1. Documentar decisão explícita sobre router
2. Documentar decisão sobre React 19 vs. 18
3. Avaliar se Zustand é necessário ou se TanStack Query é suficiente
4. Actualizar documentação de stack para reflectir realidade

---

## 6. Bloco 5: Produto e Experiência (Sprint 5-10)

**Objectivo:** Completar experiências de produto que faltam e melhorar a qualidade da UX por persona.

**Justificação:** O produto deve falar de forma diferente com cada persona e oferecer fluxos completos, não apenas backends funcionais.

---

### P-UX-01: Release Calendar UI

**Prioridade:** MÉDIA
**Esforço:** Médio
**Risco:** Baixo

**Descrição:**
Freeze windows existem como entidades no módulo ChangeGovernance mas não há UI de calendário. A visualização temporal de releases é essencial para Change Intelligence.

**Funcionalidades:**
1. Vista de calendário mensal/semanal
2. Visualização de freeze windows
3. Releases planeadas vs. executadas
4. Filtro por ambiente e serviço
5. Indicadores de risco por janela temporal

**Dependências:**
- P-STRAT-04 (dados reais de releases)

---

### P-UX-02: Knowledge Hub frontend pages

**Prioridade:** ALTA
**Esforço:** Alto
**Risco:** Baixo

**Descrição:**
O módulo Knowledge não tem páginas frontend dedicadas. Para cumprir o pilar "Source of Truth & Operational Knowledge", é necessário:

**Páginas a criar:**
1. Lista de artigos de conhecimento com pesquisa FTS
2. Detalhe de artigo com metadata e relações
3. Editor de artigo com markdown
4. Vista de relações cross-module (serviço ↔ artigo, contrato ↔ artigo)
5. Dashboard de Knowledge Hub com métricas de cobertura

**Dependências:**
- P-STRAT-01 (Knowledge backend completo)

---

### P-UX-03: DeveloperPortal melhorado

**Prioridade:** MÉDIA
**Esforço:** Médio
**Risco:** Baixo

**Descrição:**
O Developer Portal existe mas é básico. Melhorar a experiência de descoberta e consumo de contratos.

**Melhoramentos:**
1. Pesquisa avançada de contratos
2. Exemplos interactivos
3. Try-it-out para REST APIs
4. Documentação de evento com payloads de exemplo
5. Vista de dependências de contrato

---

### P-UX-04: Dashboard persona-aware melhorado

**Prioridade:** MÉDIA
**Esforço:** Alto
**Risco:** Baixo

**Descrição:**
O dashboard deve ser diferente para cada persona (conforme secção 6 das regras do produto).

**Melhoramentos por persona:**
1. **Engineer** — Serviços que é owner, mudanças recentes, incidentes activos
2. **Tech Lead** — Health da equipa, SLOs, mudanças pendentes de aprovação
3. **Architect** — Topologia, contratos em risco, breaking changes
4. **Executive** — FinOps summary, compliance, risk overview
5. **Platform Admin** — Integrações, configuração, auditoria

---

### P-UX-05: Dashboards de governance reporting completos

**Prioridade:** MÉDIA
**Esforço:** Alto
**Risco:** Baixo

**Descrição:**
Relatórios de governance existem parcialmente. Completar para cobrir todos os cenários enterprise.

**Relatórios a completar:**
1. Compliance score por domínio/equipa
2. Governance pack adoption rate
3. Policy violation trends
4. Risk heatmap com drill-down temporal
5. Maturity scorecard evolutivo

**Dependências:**
- P-STRUCT-03 (TODO handlers completados)

---

## 7. Diagrama de Dependências

```
Bloco 1 (Fundacional)
├── P-CRIT-01 (CancellationToken) ─── independente, paralelo
├── P-CRIT-02 (Knowledge migrations) ──┐
├── P-CRIT-03 (Integrations migrations)├── desbloqueia Bloco 3
├── P-CRIT-04 (ProductAnalytics migrations)
├── P-CRIT-05 (IsSimulated OpIntel) ───┐
└── P-CRIT-06 (IsSimulated FinOps) ────┴── desbloqueia P-STRUCT-04

Bloco 2 (Estrutural)
├── P-STRUCT-01 (ExternalAI) ──── desbloqueia P-STRAT-03
├── P-STRUCT-02 (Orchestration) ── depende de P-STRUCT-01
├── P-STRUCT-03 (TODO Governance) ── independente
├── P-STRUCT-04 (Frontend mock) ── depende de P-CRIT-06
├── P-STRUCT-05 (PA Contracts) ── independente
└── P-STRUCT-06 (Events) ── depende de P-CRIT-03, P-CRIT-04

Bloco 3 (Estratégico)
├── P-STRAT-01 (Knowledge) ── depende de P-CRIT-02
├── P-STRAT-02 (Integrations) ── depende de P-CRIT-03
├── P-STRAT-03 (AI Grounding) ── depende de P-STRAT-01, P-STRUCT-01
├── P-STRAT-04 (Telemetria) ── depende de P-STRAT-02, P-CRIT-05
├── P-STRAT-05 (FinOps real) ── depende de P-CRIT-06, P-STRAT-02
└── P-STRAT-06 (Licensing) ── independente

Bloco 5 (UX)
├── P-UX-01 (Calendar) ── depende de P-STRAT-04
├── P-UX-02 (Knowledge Hub) ── depende de P-STRAT-01
├── P-UX-03 (DevPortal) ── independente
├── P-UX-04 (Persona dashboard) ── independente
└── P-UX-05 (Governance reports) ── depende de P-STRUCT-03
```

---

## 8. Métricas de Progresso

### Indicadores Chave

| Métrica | Valor Actual | Meta Sprint 2 | Meta Sprint 4 | Meta Sprint 8 |
|---------|-------------|---------------|---------------|---------------|
| Handlers com IsSimulated | 31 | 0 | 0 | 0 |
| Handlers vazios | ~11 | ~11 | 0 | 0 |
| Handlers com TODO | ~8 | ~8 | 0 | 0 |
| Módulos sem migrações | 3 | 0 | 0 | 0 |
| Métodos async sem CancellationToken | ~237 | 0 | 0 | 0 |
| Páginas frontend com mock data | 6+ | 6 | 0 | 0 |
| Alinhamento global com visão | ~65% | ~75% | ~85% | ~92% |

---

## 9. Riscos do Roadmap

| Risco | Probabilidade | Impacto | Mitigação |
|-------|--------------|---------|-----------|
| Substituição de IsSimulated revela bugs de schema | Média | Médio | Testar queries em ambiente de dev antes |
| Migrações EF conflitam com schema existente | Baixa | Alto | Gerar migrações incrementais, testar em BD limpa |
| Integrations adapters quebram com APIs externas | Alta | Médio | Começar com um provider (GitHub), expandir depois |
| Migração frontend (router) causa regressões | Alta | Alto | Não migrar; documentar decisão |
| Volume de CancellationToken changes causa merge conflicts | Média | Médio | Processar módulo a módulo, merge frequente |

---

*Documento gerado como parte da avaliação de estado do projecto NexTraceOne em 2026-03-27.*
