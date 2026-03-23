# ONDA 0 — REALINHAMENTO DE BASELINE, ARQUITETURA DE OBSERVABILIDADE E RECLASSIFICAÇÃO DO BACKLOG

> **Data:** 2026-03-23
> **Tipo:** Auditoria confirmatória e realinhamento arquitetural
> **Escopo:** Baseline do NexTraceOne para programa final de conclusão
> **Autor:** Principal Staff Engineer / Enterprise Product Auditor

---

## 1. RESUMO EXECUTIVO

### O que esta onda corrigiu conceitualmente

A Onda 0 executou um realinhamento completo do baseline do programa de conclusão do NexTraceOne. O relatório mestre de auditoria (24 gaps) continha uma premissa arquitetural incorreta: **Grafana como componente obrigatório da stack de observabilidade**.

Essa premissa já havia sido abandonada pelo projeto. A stack oficial migrou de Tempo/Loki/Grafana para uma arquitetura com **provider configurável (ClickHouse padrão, Elastic alternativa)**, onde o próprio NexTraceOne é a superfície de visualização e troubleshooting.

O GAP-012 ("Grafana dashboards ausentes") foi **reclassificado** para refletir a realidade: a necessidade real é validar e documentar a superfície operacional de visualização que o produto já oferece nativamente.

### Por que isso era necessário

Sem este realinhamento, as ondas seguintes iriam:

1. Gastar esforço implementando dashboards Grafana que o projeto não utiliza;
2. Tratar como gap algo que é uma decisão arquitetural consciente;
3. Manter um backlog com premissas falsas, gerando ruído e desperdício;
4. Comprometer a credibilidade do plano de execução.

### Impacto nas próximas ondas

- **Onda 1** pode começar imediatamente após esta entrega;
- **Grafana não é mais dependência de nenhuma onda**;
- O backlog foi reclassificado com decisões executivas claras (Corrigir / Substituir / Descartar / Adiar);
- Cada onda tem critérios de aceite definidos e não depende de premissas abandonadas.

---

## 2. ARQUITETURA OPERACIONAL REAL

### 2.1 Stack de observabilidade confirmada

A arquitetura de observabilidade do NexTraceOne é **provider-agnostic** e opera com os seguintes componentes confirmados no repositório:

| Componente | Papel | Status | Evidência |
|-----------|-------|--------|-----------|
| **OpenTelemetry SDK (.NET)** | Instrumentação das aplicações | ✅ Ativo | `NexTraceOne.BuildingBlocks.Observability` |
| **OTel Collector** | Pipeline central de ingestão | ✅ Ativo | `build/otel-collector/otel-collector.yaml` (310 linhas) |
| **ClickHouse** | Provider de observabilidade padrão | ✅ Ativo | `docker-compose.yml`, `build/clickhouse/init-schema.sql` |
| **Elastic** | Provider alternativo (enterprise) | ✅ Configurável | `ElasticObservabilityProvider.cs` |
| **PostgreSQL (schema telemetry)** | Product Store (agregados, topologia) | ✅ Ativo | `appsettings.json` → `Telemetry:ProductStore` |
| **Grafana** | Visualização externa | ❌ Removido | Sem serviço em `docker-compose.yml`, sem diretório `build/observability/grafana/` |
| **Tempo** | Backend de traces externo | ❌ Removido | Substituído por ClickHouse `otel_traces` |
| **Loki** | Backend de logs externo | ❌ Removido | Substituído por ClickHouse `otel_logs` |
| **Prometheus** | Backend de métricas externo | ❌ Removido | Substituído por ClickHouse `otel_metrics` |

### 2.2 Pipeline OTLP confirmado

```
Aplicações (.NET via OpenTelemetry SDK)
        │
        ├── OTLP gRPC (4317) / HTTP (4318)
        │
  ┌─────┴──────┐
  │    OTel     │  Receivers: OTLP, Prometheus scrape, HostMetrics
  │  Collector  │  Processors: batch, memory_limiter, tail_sampling, redaction, filter
  │  (Pipeline) │  Exporters: ClickHouse (raw) + OTLP (Product Store)
  └─────┬──────┘  Connectors: spanmetrics (traces → métricas derivadas)
        │
  ┌─────┴──────┐
  │ ClickHouse  │  Tabelas: otel_logs, otel_traces, otel_metrics
  │ (Analítico) │  Retenção: logs 30d, traces 30d, métricas 90d
  └────────────┘  Motor: MergeTree com ZSTD, particionamento mensal
```

### 2.3 Papel real dos componentes

| Componente | Responsabilidade |
|-----------|-----------------|
| **OTel Collector** | Ingestão, processamento (sampling, redação PII, filtragem), exportação |
| **ClickHouse** | Armazenamento analítico de dados brutos (logs, traces, métricas) |
| **Product Store (PostgreSQL)** | Agregados, topologia observada, correlações, contextos de investigação |
| **IObservabilityProvider** | Abstração para queries (QueryLogsAsync, QueryTracesAsync, QueryMetricsAsync) |
| **ICollectionModeStrategy** | Abstração para modo de coleta (OpenTelemetryCollector vs CLR Profiler) |

### 2.4 Ausência de Grafana como decisão arquitetural

A remoção do Grafana foi uma **decisão arquitetural consciente e documentada**, fundamentada em:

1. **Princípio Source of Truth** — O NexTraceOne é a fonte de verdade; depender de Grafana para visualização contradiz este princípio;
2. **Complexidade operacional** — Três backends separados (Tempo, Loki, Grafana) aumentam complexidade;
3. **Eficiência de correlação** — Backend unificado (ClickHouse) permite melhor correlação de dados;
4. **Flexibilidade** — Padrão provider-agnostic permite trocar entre ClickHouse e Elastic;
5. **Produto-cêntrico** — Telemetria é consumida internamente pelas features do produto.

**Evidência documental:** `docs/observability/architecture-overview.md` (linhas 18, 23, 426-431) explica detalhadamente a razão da migração.

### 2.5 Superfície oficial de troubleshooting e visualização operacional

O NexTraceOne oferece nativamente as seguintes superfícies operacionais **sem dependência de Grafana**:

| Superfície | Rota | Capacidade |
|-----------|------|-----------|
| **Incidents** | `/operations/incidents` | Lista, detalhe, timeline, correlação, evidências, mitigação |
| **Service Reliability** | `/operations/reliability` | Status, métricas (latência, error rate, throughput), anomalias, tendências |
| **Environment Comparison** | `/operations/runtime-comparison` | Comparação antes/depois de releases, drift detection, observability score |
| **Runbooks** | `/operations/runbooks` | Documentação operacional contextualizada |
| **Platform Operations** | `/platform/operations` | Health de subsistemas, background jobs, filas de eventos |
| **Automation Workflows** | `/operations/automation` | Workflows de automação operacional |

**Dados disponíveis nativamente no produto:**
- ✅ Métricas agregadas (latência, error rate, throughput, disponibilidade, CPU, memória)
- ✅ Incidentes com timeline e correlação
- ✅ Health de serviços e tendências
- ✅ Drift detection entre ambientes
- ✅ Correlação mudança-incidente
- ✅ Dependências de serviços
- ✅ Health de subsistemas da plataforma

**Dados que requerem acesso direto ao ClickHouse (sem UI nativa):**
- ⚠️ Logs brutos (consulta via ClickHouse SQL ou ferramenta externa)
- ⚠️ Traces distribuídos completos (spans individuais)
- ⚠️ Métricas históricas em série temporal (dashboards custom)

**Ferramentas complementares:**
- `scripts/observability/verify-pipeline.sh` — Verifica saúde do pipeline OTLP→ClickHouse
- ClickHouse SQL client — Consultas ad-hoc em `otel_logs`, `otel_traces`, `otel_metrics`
- OTel Collector Prometheus endpoint (`:8888`) — Métricas do próprio collector

---

## 3. RECLASSIFICAÇÃO DOS GAPS

### 3.1 Resumo da reclassificação

| Decisão | Quantidade | Gaps |
|---------|-----------|------|
| **Corrigir nesta jornada** | 16 | GAP-001 a GAP-010, GAP-011, GAP-013 a GAP-016, GAP-022 |
| **Substituir por gap atualizado** | 1 | GAP-012 |
| **Adiar para pós-go-live** | 5 | GAP-017, GAP-018, GAP-019, GAP-023, GAP-024 |
| **Descartar** | 0 | — |
| **Novo gap introduzido** | 2 | GAP-012-R (substitui GAP-012), GAP-020 reclassificado |

### 3.2 Gaps mantidos (Corrigir)

| Gap | Descrição | Camada | Prioridade |
|-----|-----------|--------|-----------|
| GAP-001 | Secrets de produção não configurados | Production Blocker | P0 |
| GAP-002 | Backup automatizado não configurado | Production Blocker | P0 |
| GAP-003 | GetEfficiencyIndicators retorna demo | Enterprise Credibility Blocker | P1 |
| GAP-004 | GetWasteSignals retorna demo | Enterprise Credibility Blocker | P1 |
| GAP-005 | GetFrictionIndicators retorna demo | Enterprise Credibility Blocker | P1 |
| GAP-006 | RunComplianceChecks retorna mock | Enterprise Credibility Blocker | P1 |
| GAP-007 | GenerateDraftFromAi usa template stub | Enterprise Credibility Blocker | P1 |
| GAP-008 | DocumentRetrievalService é stub | Enterprise Credibility Blocker | P1 |
| GAP-009 | TelemetryRetrievalService é stub | Hardening / Operational Maturity | P2 |
| GAP-010 | EncryptionInterceptor ausente | Enterprise Credibility Blocker | P1 |
| GAP-011 | GetExecutiveDrillDown flag inconsistente | Hardening / Operational Maturity | P2 |
| GAP-013 | EvidencePackages preview badge | Hardening / Operational Maturity | P2 |
| GAP-014 | GovernancePackDetail preview badge | Hardening / Operational Maturity | P2 |
| GAP-015 | Rate limiting limitado a auth | Enterprise Credibility Blocker | P1 |
| GAP-016 | GetPlatformHealth subsistemas hardcoded | Hardening / Operational Maturity | P2 |
| GAP-022 | Alerting não integrado a incidents | Enterprise Credibility Blocker | P2 |

### 3.3 Gap substituído

| Gap Original | Decisão | Novo Gap |
|-------------|---------|----------|
| GAP-012: Grafana dashboards ausentes | **Substituir** | GAP-012-R: Superfície de visualização operacional sem Grafana precisa estar validada e documentada |

**GAP-012-R — Definição completa:**

- **Título:** Camada de visualização operacional e troubleshooting sem Grafana precisa estar claramente definida, validada e documentada
- **Módulo:** Observability / Operations
- **Tipo:** Documentação + Validação
- **Severidade:** Medium
- **Camada:** Hardening / Operational Maturity
- **Prioridade:** P2

**O novo gap responde:**
| Pergunta | Resposta |
|----------|---------|
| Onde o operador vê logs? | ClickHouse SQL client (consultas em `otel_logs`); produto não tem log viewer nativo |
| Onde vê traces? | ClickHouse SQL client (consultas em `otel_traces`); produto mostra correlações em Incidents |
| Onde vê métricas? | Produto mostra métricas agregadas em Service Reliability e Environment Comparison |
| Como investiga anomalias/drift? | Environment Comparison Page + Drift Detection automático |
| Como faz troubleshooting? | Incidents → correlação → runbooks → ClickHouse para deep dive |
| Qual é a superfície oficial? | 6 páginas operacionais do produto + ClickHouse para dados brutos |

**Critério de aceite do GAP-012-R:**
1. Documentação operacional atualizada com a superfície real de troubleshooting
2. Runbook de troubleshooting sem referência a Grafana
3. Validação de que as páginas operacionais do produto cobrem os cenários críticos
4. Documentação de como acessar dados brutos via ClickHouse quando necessário

**Status:** Parcialmente resolvido pela Onda 0 (este documento). Completar na Onda de Hardening.

### 3.4 Gaps adiados (Pós-go-live)

| Gap | Descrição | Razão do adiamento | Camada |
|-----|-----------|-------------------|--------|
| GAP-017 | Load testing formal | Smoke testing existe; load test formal não bloqueia produção | Post-Go-Live Improvement |
| GAP-018 | Playwright E2E frontend | Testes unitários e integração existem; E2E é melhoria | Post-Go-Live Improvement |
| GAP-019 | Refresh token E2E | Funcionalidade testada unitariamente; E2E é melhoria | Post-Go-Live Improvement |
| GAP-023 | ProductStore não implementado | Referenciado em docs mas sem necessidade imediata comprovada | Post-Go-Live Improvement |
| GAP-024 | ESLint warnings no frontend | 108 erros pré-existentes; não afetam funcionalidade | Post-Go-Live Improvement |

### 3.5 Gap reclassificado

| Gap | Descrição Original | Nova Classificação | Camada |
|-----|--------------------|--------------------|--------|
| GAP-020 | AssistantPanel mock generator | **Corrigir** (baixa prioridade) | Hardening / Operational Maturity |
| GAP-021 | CORS por ambiente | **Corrigir** (baixa prioridade) | Hardening / Operational Maturity |

---

## 4. NOVO BACKLOG OFICIAL

### Resumo consolidado

| Camada | Gaps | Total |
|--------|------|-------|
| **Production Blocker** | GAP-001, GAP-002 | 2 |
| **Enterprise Credibility Blocker** | GAP-003 a GAP-008, GAP-010, GAP-015, GAP-022 | 9 |
| **Hardening / Operational Maturity** | GAP-009, GAP-011, GAP-012-R, GAP-013, GAP-014, GAP-016, GAP-020, GAP-021 | 8 |
| **Post-Go-Live Improvement** | GAP-017, GAP-018, GAP-019, GAP-023, GAP-024 | 5 |
| **Total** | | **24** |

---

## 5. NOVO PLANO POR ONDAS

> Ver documento completo: `NEXTRACEONE-UPDATED-WAVES-PLAN.md`

### Resumo

| Onda | Objetivo | Gaps | Esforço |
|------|---------|------|---------|
| **Onda 1** | Desbloqueio de produção | GAP-001, GAP-002 | 1-2 dias |
| **Onda 2** | Eliminar demo/stub do core | GAP-003 a GAP-008, GAP-010 | 2-3 semanas |
| **Onda 3** | Segurança e integração operacional | GAP-015, GAP-022 | 1 semana |
| **Onda 4** | Hardening e maturidade operacional | GAP-009, GAP-011, GAP-012-R, GAP-013, GAP-014, GAP-016, GAP-020, GAP-021 | 1-2 semanas |
| **Onda 5** | Qualidade e polish (pós-go-live) | GAP-017, GAP-018, GAP-019, GAP-023, GAP-024 | 2-3 semanas |

---

## 6. RECOMENDAÇÃO FINAL

### A Onda 1 pode começar?

**SIM.** A Onda 1 está pronta para execução imediata. Os dois gaps (GAP-001 e GAP-002) são puramente operacionais/infraestruturais e não dependem de nenhuma premissa corrigida nesta onda.

### Dependências removidas

1. **Grafana** — Removido como dependência de qualquer onda futura;
2. **Tempo/Loki** — Não são mais pré-requisitos de nenhum gap;
3. **Dashboards externos** — A superfície operacional do produto substitui a necessidade de dashboards Grafana para operação do dia-a-dia.

### Dependências esclarecidas

1. **ClickHouse** — Confirmado como provider padrão e único store de dados brutos;
2. **OTel Collector** — Confirmado como pipeline central de ingestão;
3. **Product Store (PostgreSQL)** — Confirmado para agregados e correlações;
4. **IObservabilityProvider** — Abstração que permite trocar ClickHouse por Elastic.

### Riscos que ainda precisam de atenção

| Risco | Impacto | Mitigação |
|-------|---------|-----------|
| Logs/traces brutos só acessíveis via ClickHouse SQL | Operadores precisam de conhecimento SQL | Documentar queries comuns em runbooks |
| Product Store (GAP-023) referenciado em docs mas não implementado | Agregados podem faltar para AI features | Avaliar necessidade na Onda 2 (AI) |
| 108 ESLint errors (GAP-024) acumulam technical debt | Dificultam manutenção frontend | Adiado para pós-go-live, risco aceitável |
| Correlation de incidents é seed-data based | Incidentes não se correlacionam dinamicamente | Endereçar na Onda 2 |

---

## APÊNDICE A — VESTÍGIOS DE GRAFANA NO REPOSITÓRIO

### Localização e classificação

| Arquivo | Tipo de Referência | Já tem nota histórica? | Ação |
|---------|-------------------|----------------------|------|
| `docs/observability/README.md` | Posicionamento (NexTraceOne não é Grafana) | N/A (não é referência) | ✅ OK |
| `docs/observability/architecture-overview.md` | Decisão arquitetural documentada | N/A (é a fonte da decisão) | ✅ OK |
| `.github/copilot-instructions.md` | Posicionamento de produto | N/A (não é referência) | ✅ OK |
| `docs/observability/PHASE-6-OBSERVABILITY-COMPLETION.md` | Histórico | ✅ Sim | ✅ OK |
| `docs/audits/PHASE-6-OBSERVABILITY-REPORT.md` | Histórico | ✅ Sim | ✅ OK |
| `docs/execution/PHASE-7-OBSERVABILITY-CLI-OPS.md` | Histórico | ✅ Sim | ✅ OK |
| `docs/audits/PHASE-7-OPERATIONAL-COMPLETENESS-REPORT.md` | Histórico | ✅ Sim | ✅ OK |
| `docs/audits/PHASE-7-DELIVERY-READINESS-REPORT.md` | Histórico | ✅ Sim | ✅ OK |
| `docs/assessment/09-OBSERVABILITY-AND-AI-READINESS.md` | Histórico | ✅ Sim | ✅ OK |
| `docs/assessment/10-PRODUCTION-READINESS.md` | Desatualizado | ❌ Não | ⚠️ Nota adicionada |
| `docs/assessment/12-RECOMMENDED-EXECUTION-PLAN.md` | Desatualizado | ❌ Não | ⚠️ Nota adicionada |
| `docs/audits/NEXTRACEONE-CURRENT-STATE-AND-100-PERCENT-GAP-REPORT.md` | Desatualizado (GAP-012) | ❌ Não | ⚠️ Nota adicionada |
| `docs/telemetry/TELEMETRY-ARCHITECTURE.md` | Decisão arquitetural | ❌ Não | ⚠️ Nota adicionada |
| `docs/architecture/phase-5/phase-5-telemetry-foundation.md` | Desatualizado | ❌ Não | ⚠️ Nota adicionada |
| `docs/AI-LOCAL-IMPLEMENTATION-AUDIT.md` | Desatualizado | ❌ Não | ⚠️ Nota adicionada |
| `docs/ANALISE-CRITICA-ARQUITETURAL.md` | Desatualizado | ❌ Não | ⚠️ Nota adicionada |

### Artefatos físicos de Grafana

| Artefato | Status |
|---------|--------|
| `build/observability/grafana/dashboards/` | ❌ Não existe |
| `build/observability/grafana/provisioning/` | ❌ Não existe |
| Serviço `grafana` em `docker-compose.yml` | ❌ Não existe |
| Serviço `tempo` em `docker-compose.yml` | ❌ Não existe |
| Serviço `loki` em `docker-compose.yml` | ❌ Não existe |
| Referências a Grafana em workflows CI/CD | ❌ Não existem |
| Referências a Grafana em scripts | ❌ Não existem |

**Conclusão:** Grafana não tem presença funcional no repositório. Todas as referências são documentais/históricas.

---

## APÊNDICE B — CONFIRMAÇÃO DE COMPONENTES ATIVOS

### Serviços em docker-compose.yml

| Serviço | Porta(s) | Papel |
|---------|---------|-------|
| PostgreSQL | 5432 | Base de dados de domínio (4 BDs lógicos) |
| ClickHouse | 8123, 9000 | Provider de observabilidade (logs, traces, métricas) |
| OTel Collector | 4317, 4318, 8888 | Pipeline de ingestão OTLP |
| ApiHost | 8080 | API principal |
| BackgroundWorkers | 8081 | Jobs (drift detection, monitoring) |
| Ingestion.Api | 8082 | Ingestão de telemetria externa |
| Frontend | 3000 | SPA React |

### Schema ClickHouse

| Tabela | Retenção | Motor |
|--------|---------|-------|
| `otel_logs` | 30 dias | MergeTree + ZSTD |
| `otel_traces` | 30 dias | MergeTree + ZSTD |
| `otel_metrics` | 90 dias | MergeTree + ZSTD |

### Testes de observabilidade

- **96 testes** em `NexTraceOne.BuildingBlocks.Observability.Tests`
- Cobrem: configuração de providers, modos de coleta, retenção, arquitetura Product Store, modelos, correlação

---

> **Este documento é o resultado oficial da Onda 0 e serve como fonte de verdade para o baseline do programa de conclusão do NexTraceOne.**
