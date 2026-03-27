# Relatório de Alinhamento de Produto — NexTraceOne
**Auditoria Forense | Março 2026**

---

## Objetivo

Verificar se o repositório atual reflete o NexTraceOne como: source of truth operacional, plataforma de contract governance, change intelligence, operational consistency, AI governance, knowledge hub e FinOps contextual.

---

## 1. Alinhamento Global com a Visão Oficial

### Visão declarada
> "NexTraceOne é uma plataforma enterprise unificada para governança de serviços e contratos, change intelligence, confiança em mudanças de produção, confiabilidade operacional orientada por equipas, inteligência operacional assistida por IA, conhecimento operacional governado e otimização contextual de operação e custo."

### Veredicto de alinhamento: **PARCIALMENTE ALINHADO**

O repositório reflete corretamente a visão nos pilares 1 (Service Governance), 2 (Contract Governance) e 3 (Change Intelligence). Nos pilares 4 a 10, a implementação é fragmentada, mock ou inexistente.

---

## 2. Alinhamento por Pilar Oficial

| Pilar | Módulo Principal | Implementação Real? | Alinhamento | Evidência |
|---|---|---|---|---|
| 1. Service Governance | Catalog (Graph) | Sim — RegisterServiceAsset, dependencies, topology | **ALIGNED** | `src/modules/catalog/NexTraceOne.Catalog.Infrastructure/Graph/` |
| 2. Contract Governance | Catalog (Contracts) | Sim — REST, SOAP, Event, Background | **ALIGNED** | `src/modules/catalog/NexTraceOne.Catalog.Infrastructure/Contracts/` |
| 3. Change Intelligence | Change Governance | Sim — blast radius, advisory, approval, rollback | **ALIGNED** | `src/modules/changegovernance/` |
| 4. Operational Reliability | Operational Intelligence | Parcial — Incidents real mas correlação mock | **PARTIAL** | `src/modules/operationalintelligence/` |
| 5. Operational Consistency | Governance | 100% mock com `IsSimulated: true` | **MOCK** | `src/modules/governance/` |
| 6. AI-assisted Operations | AI Knowledge | Parcial — Gov real, ExternalAI TODO, assistant mock | **PARTIAL** | `src/modules/aiknowledge/` |
| 7. Source of Truth & Knowledge | Catalog + Knowledge | Catalog real; Knowledge DbContext sem migrações | **PARTIAL** | `src/modules/knowledge/`, `src/modules/catalog/` |
| 8. AI Governance | AI Knowledge (Governance) | AI Governance funcional — modelos, políticas, budgets | **ALIGNED** | `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Infrastructure/Governance/` |
| 9. Operational Intelligence | Operational Intelligence | Runtime+Cost têm DbContexts; handlers parcialmente mock | **PARTIAL** | `src/modules/operationalintelligence/` |
| 10. FinOps contextual | Governance (FinOps handlers) | 100% hardcoded — `IsSimulated: true` | **MOCK** | `src/modules/governance/` FinOps handlers |

---

## 3. O Produto Está Virando Algo que Não Deve?

### Anti-padrão: dashboard genérico de observabilidade
**Status: RISCO MÉDIO**

A estrutura de Runtime Intelligence e Cost Intelligence existe, mas a telemetria ingerida via `/api/ingestion/*` não está correlacionada dinamicamente com serviços, contratos e mudanças. Os dashboards de observabilidade existem como widgets sem narrativa operacional completa.

Evidência: `docs/IMPLEMENTATION-STATUS.md` — "IRuntimeIntelligenceModule: PLAN — no implementation"; "ICostIntelligenceModule: PLAN — no implementation"

### Anti-padrão: chat IA sem governança
**Status: RISCO ALTO**

O AI Assistant (`AiAssistantPage.tsx`) usa `mockConversations` hardcoded. Quando funcionar, a governança existe (AI Governance DbContext, políticas, budgets), mas o assistant em si retorna respostas hardcoded sem LLM real.

Evidência: `docs/CORE-FLOW-GAPS.md` — "SendAssistantMessage retorna respostas hardcoded — sem integração com LLM real"

### Anti-padrão: telas bonitas mas vazias
**Status: CONFIRMADO para Governance e FinOps**

25 páginas de Governance no frontend conectadas ao backend retornam dados com `IsSimulated: true`. A experiência visual existe mas não entrega valor real.

Evidência: `docs/IMPLEMENTATION-STATUS.md` Governance section — "Benchmarking: SIM"; "FinOps (domain/team/service): SIM"; "Executive drill-down: SIM"

### Anti-padrão: pedido de GUID ao utilizador
**Status: NÃO DETECTADO nas áreas core**

A navegação usa slugs e IDs de forma interna, não exposta ao utilizador final.

---

## 4. Personas — Reflexo no Produto

| Persona | Áreas de Produto Disponíveis | Lacunas para Esta Persona |
|---|---|---|
| Engineer | Contract Studio, Service Catalog, Change Confidence, AI Assistant | AI Assistant não funcional; Developer Portal parcial |
| Tech Lead | Change Intelligence, Blast Radius, Approval workflows | Correlação incident↔change mock |
| Architect | Service Topology, Contract Governance, Source of Truth | Topology real mas cross-module interfaces ausentes |
| Product | Governance Overview, Maturity Scorecards | Governance 100% mock |
| Executive | Executive Overview, FinOps, Compliance | FinOps e Compliance 100% mock |
| Platform Admin | Identity, Environments, Configuration, Audit | Sólido — Identity 100% real |
| Auditor | Audit Trail, Chain Integrity, Security Events | Sólido — Audit Compliance 100% real |

**Conclusão**: O produto atende bem às personas de Engineer (parcial), Tech Lead e Platform Admin/Auditor. As personas de Executive, Product e Architect recebem predominantemente dados simulados.

---

## 5. Regra de Source of Truth — Verificação

| Entidade | Consultável com Confiança? | Evidência |
|---|---|---|
| Serviços | Sim | CatalogGraphDbContext, RegisterServiceAsset |
| Ownership | Sim | Graph service assets com ownership |
| Equipas | Parcial | Governance retorna dados mock |
| Ambientes | Sim | IdentityAccess EnvironmentEndpoints |
| Contratos | Sim | ContractsDbContext, ContractVersion |
| Dependências | Parcial | Graph existe; cross-module IContractsModule PLAN |
| Mudanças | Sim | ChangeIntelligenceDbContext completo |
| Incidentes | Não | Dados hardcoded/seed; correlação não dinâmica |
| Evidências | Sim | EvidencePack com WorkflowDbContext |
| Documentação operacional | Parcial | Knowledge DbContext sem migrações geradas |
| Políticas | Sim | AI Governance policies; RulesetGovernanceDbContext |
| Conhecimento governado | Parcial | Runbooks hardcoded; Knowledge module sem migrações |

---

## 6. Decisões Antigas que Ainda Carrega

| Resíduo | Localização | Recomendação |
|---|---|---|
| Módulo Commercial Governance removido (PR-17) | Referências em docs antigos | Confirmar remoção completa de docs e referencias residuais |
| `IsSimulated: true` como pattern de mock | Handlers de Governance, Reliability, FinOps | Manter padrão para rastreabilidade; priorizar substituição |
| InMemoryIncidentStore (citado em docs antigos) | Substituído por EfIncidentStore | Verificar se resíduo de código `InMemoryIncidentStore` ainda existe |
| 7 stubs intencionais no Developer Portal | `docs/REBASELINE.md` §Portal | Documentar como roadmap, não remover silenciosamente |

---

## 7. Alinhamento à Base Técnica Alvo

| Tecnologia Alvo | Presente? | Estado |
|---|---|---|
| .NET 10 / ASP.NET Core 10 | Sim | `global.json`, `Directory.Build.props` |
| EF Core + Npgsql + PostgreSQL 16 | Sim | 24 DbContexts, docker-compose.yml |
| MediatR (CQRS) | Sim | Building blocks Application |
| FluentValidation | Sim | Behaviors no BuildingBlocks.Application |
| Quartz.NET | Verificar | BackgroundWorkers project existe |
| OpenTelemetry | Sim | BuildingBlocks.Observability, otel-collector config |
| Serilog | Sim | appsettings.json Serilog section |
| React 18 + TypeScript + Vite | Sim | `src/frontend/package.json` |
| TanStack Router + Query | Sim | `src/frontend/` dependencies |
| Zustand | Verificar | Pattern usado em contexts |
| Tailwind CSS + Radix UI | Sim | Frontend config |
| Apache ECharts | Verificar | Dashboards de analytics |
| Playwright (E2E) | Sim | `src/frontend/e2e/`, `playwright.config.ts` |
| ClickHouse (analíticos) | Parcial | Schema SQL em `build/clickhouse/`; integração não completa |
| Redis | Ausente | Correto — evitado conforme direção |

---

## 8. Recomendações de Alinhamento

1. **Fechar Fluxo 3 (Incidents)** — é o gap mais crítico de alinhamento de produto
2. **Conectar FinOps ao CostIntelligenceDbContext** — dados reais existem, handlers retornam mock
3. **Implementar cross-module interfaces prioritárias** — IContractsModule e IChangeIntelligenceModule primeiro
4. **Ativar processamento de outbox em todos os DbContexts** — propagação de eventos é fundação do produto
5. **Governance persistence layer** — sem isso, o pilar de Operational Consistency é falso
6. **Garantir que AI governance envolve o assistant** — AI sem governança ativa contradiz a visão
