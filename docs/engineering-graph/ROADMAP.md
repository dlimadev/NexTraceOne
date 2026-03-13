# Engineering Graph — Roadmap do Módulo

> **Última atualização:** Março 2026

## Status Atual

### Concluído ✅

**Domain Layer (100%)**
- ✅ ApiAsset (Aggregate Root) com factory method, decommission, discovery sources
- ✅ ServiceAsset (Entity) com domínio, time e nome
- ✅ ConsumerRelationship (Entity) com upsert, refresh e confidence score
- ✅ ConsumerAsset (Entity) com kind e environment
- ✅ DiscoverySource (Entity) com proveniência e confiança
- ✅ GraphSnapshot (Entity) para materialização temporal do grafo
- ✅ NodeHealthRecord (Entity) para overlays explicáveis
- ✅ SavedGraphView (Entity) para visões personalizadas
- ✅ 5 enumerações (NodeType, EdgeType, HealthStatus, OverlayMode, RelationshipSemantic)
- ✅ 14 erros de domínio com códigos i18n
- ✅ Typed IDs para todas as entidades

**Application Layer (100%)**
- ✅ 21 features VSA (Command/Query + Validator + Handler + Response)
- ✅ Registro de ativos: RegisterServiceAsset, RegisterApiAsset
- ✅ Mapeamento: MapConsumerRelationship, UpdateAssetMetadata
- ✅ Consultas: GetAssetGraph, GetAssetDetail, SearchAssets
- ✅ Discovery: InferDependencyFromOtel, ValidateDiscoveredDependency
- ✅ Lifecycle: DecommissionAsset
- ✅ Subgrafo contextual: GetSubgraph (mini-grafos com profundidade configurável)
- ✅ Propagação de impacto: GetImpactPropagation (blast radius direto + transitivo)
- ✅ Temporalidade: CreateGraphSnapshot, ListSnapshots, GetTemporalDiff
- ✅ Overlays: GetNodeHealth (6 modos: Health, ChangeVelocity, Risk, Cost, ObservabilityDebt)
- ✅ Saved Views: CreateSavedView, ListSavedViews
- ✅ **Integração Inbound: SyncConsumers** (batch upsert externo com idempotência)
- ✅ 5 interfaces de repositório

**Infrastructure Layer (100%)**
- ✅ EngineeringGraphDbContext com 8 DbSets
- ✅ 5 repositórios (ApiAsset, ServiceAsset, GraphSnapshot, NodeHealth, SavedGraphView)
- ✅ 8 configurações EF Core
- ✅ Migration inicial (20260312083851)
- ✅ EngineeringGraphModuleService (contrato cross-module)
- ✅ DI completo com interceptors (Audit, TenantRLS)

**API Layer (100%)**
- ✅ 18 endpoints Minimal API
- ✅ Endpoint de integração inbound: `/integration/v1/consumers/sync`
- ✅ Versionamento de rota para integração
- ✅ Localização de erros via IErrorLocalizer

**Contracts Layer (100%)**
- ✅ IEngineeringGraphModule (ApiAssetExistsAsync, ServiceAssetExistsAsync)

**Frontend (100%)**
- ✅ EngineeringGraphPage.tsx (857 linhas) com 5 abas
- ✅ API client com 15 funções
- ✅ i18n completo em en.json e pt-BR.json (incluindo integração)
- ✅ Loading states, error states, empty states
- ✅ Formulários de registro de serviço e API
- ✅ Visualização de grafo com legenda
- ✅ Análise de impacto com controle de profundidade
- ✅ Diff temporal com comparação de snapshots

**Testes (100% dos fluxos críticos)**
- ✅ 37 testes unitários
- ✅ Cobertura de: registro, mapeamento, impacto, temporal, decommission, subgrafo, sync
- ✅ Testes de domínio: ApiAsset, ServiceAsset, ConsumerRelationship
- ✅ Testes de validação: SyncConsumers (empty items, too many items)

**Documentação**
- ✅ XML documentation em PT-BR em todas as classes e métodos públicos
- ✅ docs/engineering-graph/EXTERNAL-INTEGRATION-API.md
- ✅ docs/engineering-graph/ROADMAP.md
- ✅ scripts/seed/engineering-graph/ (scripts de massa de teste)

**Scripts e Massa de Teste**
- ✅ 00-reset.sql — limpeza da massa
- ✅ 01-seed-via-api.sh — 10 serviços, 11 APIs, ~25 relações, 1 snapshot
- ✅ 02-test-sync-consumers.sh — testes de integração inbound
- ✅ README.md com documentação completa

---

## Roadmap

### Now (Fase Atual — MVP1 Hardening)

| Item | Status | Prioridade |
|------|--------|------------|
| Performance com grafos grandes (>100 nós) | 🔲 Monitorar | P2 |
| Lazy loading de consumidores na UI | 🔲 Se necessário | P2 |
| Integration Events para novos consumidores | 🔲 Preparado | P2 |
| E2E tests com Playwright | 🔲 Quando infra disponível | P3 |

### Next (Fase Seguinte — Post-MVP1)

| Item | Status | Dependência |
|------|--------|-------------|
| ImportFromBackstage (stub → implementação) | 🔲 | Backstage adapter |
| ImportFromKongGateway (stub → implementação) | 🔲 | Kong adapter |
| Overlays de custo/dívida/velocidade (dados reais) | 🔲 | CostIntelligence, RuntimeIntelligence |
| Mini-grafos contextuais em pages de Release e Workflow | 🔲 | Frontend cross-module routing |
| Agrupamento automático por domínio/time na UI | 🔲 | Frontend |
| Deep links para subgrafos | 🔲 | Frontend routing |
| Saved views compartilhadas entre usuários | 🔲 | Frontend + permissões |
| Batch assíncrono para sync >100 itens | 🔲 | Background workers |
| Webhooks inversos para notificação de mudanças | 🔲 | EventBus + Outbox |

### Later (Futuro — MVP2+)

| Item | Status | Dependência |
|------|--------|-------------|
| Grafos com layout force-directed (Apache ECharts) | 🔲 | Frontend |
| Replay temporal animado | 🔲 | Múltiplos snapshots |
| Inferência automática de dependências via Istio/Envoy | 🔲 | RuntimeIntelligence |
| Score de risco calculado automaticamente | 🔲 | ChangeIntelligence + RulesetGovernance |
| Exportação do grafo (GraphML, DOT, JSON) | 🔲 | Frontend + API |
| API de integração para outros tipos de dados | 🔲 | Padrão SyncConsumers como referência |
| Rate limiting na API de integração | 🔲 | Middleware ou API Gateway |

---

## Dependências e Riscos

### Dependências de Outros Módulos

| Módulo | Dependência | Status |
|--------|-------------|--------|
| Identity & Access | Autenticação, tenant scoping, permissões | ✅ Funcional |
| ChangeIntelligence | Consulta blast radius via IEngineeringGraphModule | ✅ Contrato definido |
| Contracts | Vínculo API asset ↔ contrato | ✅ Via ApiAssetId |
| RuntimeIntelligence | Dados de health/overlay em tempo real | 🔲 Módulo pendente |
| CostIntelligence | Dados de custo por serviço | 🔲 Módulo pendente |
| DeveloperPortal | Catálogo navegável consumindo dados do grafo | 🔲 Módulo pendente |

### Riscos Técnicos

| Risco | Mitigação | Impacto |
|-------|-----------|---------|
| Performance com grafos >500 nós | Paginação, agrupamento, lazy loading | Médio |
| Concorrência em sync batch | UnitOfWork por request, idempotência | Baixo |
| Dados stale de health | CalculatedAt + TTL + sourceSystem para freshness | Baixo |
| Dependência de módulos futuros para overlays reais | NodeHealthRecord aceita dados de qualquer source | Baixo |
