# NexTraceOne — Ondas de Desenvolvimento: Legacy / Mainframe / Core Systems

> **Última atualização:** Março 2026
> **Tipo:** Plano de desenvolvimento por ondas
> **Referência:** [PRODUCT-VISION.md](./PRODUCT-VISION.md) · [ROADMAP.md](./ROADMAP.md) · [IMPLEMENTATION-STATUS.md](./IMPLEMENTATION-STATUS.md)

---

## Sumário Executivo

O NexTraceOne evolui de uma plataforma centrada em serviços modernos (REST, Kafka, gRPC) para uma **plataforma enterprise híbrida** capaz de tratar **core systems legados** como cidadãos de primeira classe.

Esta capability inclui suporte nativo a:

- **Mainframe IBM Z / z/OS** — LPARs, sysplexes, regiões
- **COBOL** — programas, copybooks, compilação
- **CICS / IMS** — transações online e batch
- **Batch / JCL** — jobs, chains, SLAs, janelas operacionais
- **DB2 for z/OS** — artefactos de base de dados
- **IBM MQ / MQ for z/OS** — queue managers, filas, canais, topologia
- **z/OS Connect / CICS TG** — exposição de APIs e gateways
- **Telemetria legacy** — SMF, SYSLOG, job logs, CICS statistics
- **Governança de contratos legacy** — copybooks, layouts fixos, MQ contracts
- **Change intelligence para core systems** — blast radius híbrido, risco, CAB

### Por que esta capability é crítica

Em ambientes enterprise reais — bancos, seguradoras, utilities, governo — **60-80% do processamento transacional crítico** acontece em mainframe. O NexTraceOne afirma ser "a fonte de verdade para serviços, contratos, mudanças e conhecimento operacional". Sem suporte a core systems, essa promessa é **incompleta**.

### Impacto global estimado

| Dimensão | Estimativa |
|---|---|
| Módulos backend impactados | 8 de 12 |
| Novos bounded contexts / sub-domínios | 6+ |
| Novas entidades de domínio | ~40-60 |
| Novas tabelas PostgreSQL | ~25-35 |
| Novas tabelas ClickHouse | ~8-12 |
| Novos endpoints API | ~15-20 |
| Novas páginas frontend | ~10-15 |
| Novas chaves i18n | ~500+ |
| Novos pipelines de ingestão | ~3-5 |
| Novos background jobs | ~3-5 |
| Duração estimada total | 40-55 semanas (~10-14 meses) |

---

## Pilares do produto reforçados

Toda evolução desta frente reforça estes pilares oficiais:

1. **Service Governance** — ativos mainframe como cidadãos de primeira classe
2. **Contract Governance** — copybooks, MQ contracts, layouts fixos
3. **Change Intelligence & Production Change Confidence** — blast radius híbrido
4. **Operational Reliability** — batch intelligence, messaging intelligence
5. **Operational Consistency** — janelas operacionais, SLAs batch
6. **AI-assisted Operations** — IA com contexto mainframe
7. **Source of Truth & Operational Knowledge** — fonte de verdade unificada

---

## Mapa de Ondas

| Onda | Nome | Duração | Dependências | Detalhe |
|------|------|---------|-------------|---------|
| **0** | Estratégia e Baseline | 2 semanas | — | [WAVE-00-STRATEGY.md](./legacy/WAVE-00-STRATEGY.md) |
| **1** | Foundation de Domínio e Catálogo | 4-6 semanas | Onda 0 | [WAVE-01-CATALOG-FOUNDATION.md](./legacy/WAVE-01-CATALOG-FOUNDATION.md) |
| **2** | Ingestão de Telemetria Legacy | 4-6 semanas | Onda 1 | [WAVE-02-TELEMETRY-INGESTION.md](./legacy/WAVE-02-TELEMETRY-INGESTION.md) |
| **3** | Normalização e Correlação | 3-4 semanas | Ondas 1, 2 | [WAVE-03-NORMALIZATION-CORRELATION.md](./legacy/WAVE-03-NORMALIZATION-CORRELATION.md) |
| **4** | Legacy Contract Governance | 4-5 semanas | Onda 1 | [WAVE-04-CONTRACT-GOVERNANCE.md](./legacy/WAVE-04-CONTRACT-GOVERNANCE.md) |
| **5** | Hybrid Dependency Graph | 3-4 semanas | Ondas 1, 3 | [WAVE-05-HYBRID-GRAPH.md](./legacy/WAVE-05-HYBRID-GRAPH.md) |
| **6** | Core Change Intelligence | 4-5 semanas | Ondas 4, 5 | [WAVE-06-CHANGE-INTELLIGENCE.md](./legacy/WAVE-06-CHANGE-INTELLIGENCE.md) |
| **7** | Batch Intelligence | 4-5 semanas | Ondas 1, 2 | [WAVE-07-BATCH-INTELLIGENCE.md](./legacy/WAVE-07-BATCH-INTELLIGENCE.md) |
| **8** | Messaging Intelligence | 3-4 semanas | Ondas 1, 2 | [WAVE-08-MESSAGING-INTELLIGENCE.md](./legacy/WAVE-08-MESSAGING-INTELLIGENCE.md) |
| **9** | IA Assistiva para Legacy | 3-4 semanas | Ondas 6, 7, 8 | [WAVE-09-AI-ASSISTIVE.md](./legacy/WAVE-09-AI-ASSISTIVE.md) |
| **10** | Workflow, Aprovação e Políticas | 2-3 semanas | Onda 6 | [WAVE-10-WORKFLOW-POLICIES.md](./legacy/WAVE-10-WORKFLOW-POLICIES.md) |
| **11** | Frontend Enterprise | 3-4 semanas | Todas anteriores | [WAVE-11-FRONTEND-ENTERPRISE.md](./legacy/WAVE-11-FRONTEND-ENTERPRISE.md) |
| **12** | Segurança, Readiness e Operação | 2-3 semanas | Todas anteriores | [WAVE-12-SECURITY-READINESS.md](./legacy/WAVE-12-SECURITY-READINESS.md) |

---

## Diagrama de Dependências entre Ondas

```
Onda 0 ─── Estratégia e Baseline
   │
   ├── Onda 1 ─── Foundation de Catálogo
   │      │
   │      ├── Onda 2 ─── Ingestão Telemetria    (pode paralelo com Onda 4)
   │      │      │
   │      │      └── Onda 3 ─── Correlação
   │      │
   │      ├── Onda 4 ─── Contract Governance     (pode paralelo com Onda 2)
   │      │
   │      └── Onda 5 ─── Hybrid Graph            (requer Ondas 1 + 3)
   │
   ├── Onda 6 ─── Change Intelligence            (requer Ondas 4 + 5)
   │
   ├── Onda 7 ─── Batch Intelligence             (paralelo com Onda 8)
   │
   ├── Onda 8 ─── Messaging Intelligence         (paralelo com Onda 7)
   │
   ├── Onda 9 ─── IA Assistiva                   (requer Ondas 6 + 7 + 8)
   │
   ├── Onda 10 ── Workflow e Políticas            (requer Onda 6)
   │
   ├── Onda 11 ── Frontend Enterprise             (requer todas)
   │
   └── Onda 12 ── Segurança e Readiness           (FINAL)
```

---

## Quick Wins Possíveis

1. **Onda 0** (2 semanas) — extensão de enums e feature flags já habilita base técnica
2. **Onda 1 parcial** — catálogo de ativos legacy manual = primeira entrega visível em ~6-8 semanas
3. **Ondas 7 e 8 em paralelo** — batch e messaging intelligence podem ser desenvolvidos simultaneamente
4. **IA tools** — ferramentas de IA para context legacy podem ser adicionadas incrementalmente

---

## Modos de Maturidade para Clientes

O produto deve suportar clientes com diferentes níveis de maturidade em observabilidade legacy:

| Modo | Descrição | Capabilities |
|------|-----------|-------------|
| **Manual** | Import manual, CSV, UI | Catálogo, contratos, dependências via UI/API |
| **Hybrid** | Manual + alguma automação | Catálogo + ingestão parcial de batch/MQ events |
| **Connected** | Automação via conectores | Full ingestão via OTel Collector + APIs |
| **Native** | OTel + Z CDP + OMEGAMON | Full observabilidade com telemetria nativa |

---

## Estado Atual do Projeto — Base para Evolução

### O que já existe e ajuda

| Capacidade | Estado | Relevância |
|---|---|---|
| `ServiceType` com `LegacySystem` no DB | ✅ Check constraint já aceita | Ponto de partida |
| `ContractType.Soap` + `ContractProtocol.Wsdl` | ✅ Existe | SOAP legado suportado |
| Import WSDL funcional | ✅ Existe | Reutilizável para SOAP legacy |
| Dependency Graph (NodeType + EdgeType) | ✅ Modelo extensível | Grafo genérico |
| Release + ChangeEvent + BlastRadiusReport | ✅ 195+ testes | Modelo maduro extensível |
| Ingestion API (5 endpoints) | ✅ Pipeline funcional | Extensível para mainframe |
| ClickHouse OTel schema | ✅ Tabelas logs/traces/métricas | Extensível para legacy |
| OpenTelemetry Collector | ✅ Backbone de ingestão | Pivotal para legacy |
| Background Workers (outbox, drift, expiration) | ✅ Padrão extensível | Para jobs legacy |
| AI Tool infrastructure | ✅ Pattern reutilizável | Para queries legacy |
| Audit hash chain (SHA-256) | ✅ Imutável | Sem alteração necessária |
| RBAC + multi-tenant + environment-aware | ✅ Maduro | Extensível |
| Workflow + Approval + EvidencePack | ✅ Reutilizável | Para CAB mainframe |

### Gaps críticos identificados

| Gap | Descrição | Impacto |
|---|---|---|
| ❌ Sem entidades mainframe | LPAR, COBOL, CICS, IMS, Batch, MQ não modelados | Catálogo cego para mainframe |
| ❌ Sem batch intelligence | Sem jobs, chains, SLA, baselines | Bancos e seguradoras não adotam |
| ❌ Sem messaging intelligence | Sem MQ topology, depth, throughput | MQ é backbone enterprise |
| ❌ Sem copybook parser | Contratos legacy mais comuns | Governança incompleta |
| ❌ Sem telemetria legacy | Sem SMF, SYSLOG, job logs | Sem observabilidade real |
| ❌ Sem change intelligence legacy | Sem impacto copybook, blast radius legacy | Change confidence parcial |
| ❌ Sem dependency tracking híbrido | REST → MQ → CICS não rastreável | Blast radius parcial |
| ⚠️ ServiceType enum desalinhado | C# enum tem 8 valores; DB tem 9+ | Sincronização necessária |
| ⚠️ Outbox processing inativo | 23 DbContexts sem consumers | Pré-requisito para eventos |

---

## Refatorações Necessárias (Pré-Requisitos)

### Obrigatórias antes da Onda 1

| Refatoração | Motivo |
|---|---|
| Alinhar `ServiceType` enum C# com DB constraint | Enum tem 8 valores; DB tem 9+ (LegacySystem, Gateway, etc.) |
| Ativar outbox processing para todos os DbContexts | Novos sub-contexts precisam de outbox ativo |
| Consolidar `IncidentChangeCorrelation` | Precisa funcionar para correlacionar incidents legacy |

### Recomendadas

| Refatoração | Motivo |
|---|---|
| Abstrair `IBlastRadiusCalculator` interface | Para injetar legacy blast radius calculator |
| Normalizar `ConnectorType` no `IntegrationConnector` | Para incluir "Mainframe", "BatchScheduler", "MQExporter" |
| Extensão do `GenericIngestionPayloadParser` | Aliases para campos legacy (jobName, returnCode, queueName) |

---

## Riscos Macro e Mitigação

| Risco | Severidade | Mitigação |
|---|---|---|
| Parser COBOL copybook complexo | Alta | Começar com subset (PIC S9, PIC X). Suportar import JSON como fallback |
| Fontes mainframe reais indisponíveis para teste | Alta | Mocks realistas. Formato JSON intermediário. Modo manual como fallback |
| Volume de dados batch/MQ em enterprise | Média | ClickHouse para volume. TTL policies. Agregações materializadas |
| Naming inconsistencies entre ambientes | Média | Matching flexível. Alias support. Manual mapping override |
| Acoplamento com IBM tools (Z CDP, OMEGAMON) | Alta | OTel Collector como backbone. Múltiplas fontes. Modo manual |
| Scope creep — Legacy é muito amplo | Média | Foco em IBM Z. Não misturar AS/400, Unix legacy nesta fase |
| Performance de grafo com muitos nós legacy | Média | Lazy loading. Clustering. Filtros obrigatórios |

---

## Definition of Done — Capability Legacy/Mainframe 100%

A capability está **completa** quando:

- [ ] Todos os tipos de ativos legacy registáveis e consultáveis
- [ ] Copybooks parseáveis, versionáveis, com diff semântico
- [ ] Grafo híbrido mostra dependências moderno ↔ legacy
- [ ] Change intelligence funcional para mudanças legacy
- [ ] Batch intelligence com SLA, baseline, regressão
- [ ] Messaging intelligence com topology, depth, anomalias
- [ ] Ingestão de telemetria legacy funcional
- [ ] IA com contexto legacy funcional
- [ ] RBAC e mascaramento de dados legacy
- [ ] Frontend responsivo com i18n completo
- [ ] ~500+ testes unitários + ~100+ integração + ~30+ E2E
- [ ] Documentação completa de integração e operação

---

## Documentos Detalhados por Onda

Cada onda tem documentação detalhada em `docs/legacy/`:

| Documento | Conteúdo |
|---|---|
| [WAVE-00-STRATEGY.md](./legacy/WAVE-00-STRATEGY.md) | Estratégia, baseline, enums, feature flags |
| [WAVE-01-CATALOG-FOUNDATION.md](./legacy/WAVE-01-CATALOG-FOUNDATION.md) | Entidades, CRUD, UI catálogo, migrações |
| [WAVE-02-TELEMETRY-INGESTION.md](./legacy/WAVE-02-TELEMETRY-INGESTION.md) | Ingestão, parsers, ClickHouse, OTel Collector |
| [WAVE-03-NORMALIZATION-CORRELATION.md](./legacy/WAVE-03-NORMALIZATION-CORRELATION.md) | Correlação, timeline unificada, incidents |
| [WAVE-04-CONTRACT-GOVERNANCE.md](./legacy/WAVE-04-CONTRACT-GOVERNANCE.md) | Copybook parser, versionamento, diff, MQ contracts |
| [WAVE-05-HYBRID-GRAPH.md](./legacy/WAVE-05-HYBRID-GRAPH.md) | Grafo híbrido, blast radius, visualização |
| [WAVE-06-CHANGE-INTELLIGENCE.md](./legacy/WAVE-06-CHANGE-INTELLIGENCE.md) | Change intelligence legacy, risk scoring, CAB |
| [WAVE-07-BATCH-INTELLIGENCE.md](./legacy/WAVE-07-BATCH-INTELLIGENCE.md) | Batch jobs, chains, SLA, baseline, regressão |
| [WAVE-08-MESSAGING-INTELLIGENCE.md](./legacy/WAVE-08-MESSAGING-INTELLIGENCE.md) | MQ topology, depth, throughput, anomalias |
| [WAVE-09-AI-ASSISTIVE.md](./legacy/WAVE-09-AI-ASSISTIVE.md) | AI tools legacy, investigação, impacto |
| [WAVE-10-WORKFLOW-POLICIES.md](./legacy/WAVE-10-WORKFLOW-POLICIES.md) | Workflow CAB, freeze windows, policies |
| [WAVE-11-FRONTEND-ENTERPRISE.md](./legacy/WAVE-11-FRONTEND-ENTERPRISE.md) | UX enterprise, dashboards, responsividade |
| [WAVE-12-SECURITY-READINESS.md](./legacy/WAVE-12-SECURITY-READINESS.md) | RBAC, masking, audit, hardening, operação |
