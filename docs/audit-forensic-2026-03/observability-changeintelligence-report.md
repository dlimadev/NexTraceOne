# Relatório de Observabilidade e Change Intelligence — NexTraceOne
**Auditoria Forense | 28 de Março de 2026**

---

## Objetivo da Área no Contexto do Produto

Observabilidade no NexTraceOne é meio, não fim. Telemetria deve estar contextualizada por serviço, contrato, mudança, ambiente e incidente. Change Intelligence é o pilar diferenciador — conectar mudanças a contexto operacional completo.

---

## Observabilidade — Estado Atual

### Stack de Observabilidade Configurada

| Componente | Estado | Ficheiro |
|---|---|---|
| OpenTelemetry SDK (.NET) | ✅ Configurado | `BuildingBlocks.Observability` |
| OTLP Exporter | ✅ Configurado | `appsettings.json` → `localhost:4317` |
| OTel Collector | ✅ Config existe | `build/otel-collector/otel-collector.yaml` |
| ClickHouse (analytics) | ✅ Config existe | `build/clickhouse/` |
| Serilog | ✅ Configurado | `SerilogConfiguration` em BuildingBlocks |
| Prometheus / Grafana | ⚠️ Não confirmado | Não encontrado em configuração ativa |
| Loki / Tempo | ⚠️ Não confirmado | Direção arquitetural aponta para ClickHouse |
| Docker Compose Telemetry | ✅ Existe | `build/otel-collector/docker-compose.telemetry.yaml` |

### Telemetria Instrumentada

| Fonte | Estado | Evidência |
|---|---|---|
| Traces HTTP (ASP.NET) | ✅ | `NexTraceActivitySources` configurado |
| Métricas customizadas | ✅ | `NexTraceMeters` configurado |
| Logs estruturados | ✅ | Serilog com enrichment |
| ClickHouse analytics writer | ✅ | Implementação real + Null fallback |
| Health checks | ✅ | `NexTraceHealthChecks` framework |
| Traces de domínio (change events) | ⚠️ | Sem validação E2E de pipeline completo |

### O que NÃO foi validado E2E

- Ingestão de traces/metrics de ponta a ponta (API → Collector → ClickHouse)
- Correlação de traces com mudanças e incidentes
- Retenção e consulta de telemetria histórica
- Alertas baseados em telemetria

**Evidência de gap:** `appsettings.json` aponta `OtlpEndpoint: "http://localhost:4317"` — sem override confirmado por ambiente de produção.

---

## Ingestão de Telemetria Externa

### NexTraceOne.Ingestion.Api

Serviço dedicado à ingestão de dados externos. Tem `appsettings.json` próprio.

**Fontes suportadas (documentadas):**
- Traces OpenTelemetry
- Logs estruturados
- Eventos de deploy/change (CI/CD)
- Eventos Kafka
- Logs IIS
- Logs de aplicações .NET

**Estado real:** A estrutura existe mas a validação de ingestão E2E não foi confirmada. `Integrations` module com conectores stub bloqueia ingestão via CI/CD externo (GitLab, Jenkins, etc.).

---

## Change Intelligence — Estado Detalhado

Este é o pilar **mais maduro e funcional** do produto.

### Capacidades Confirmadas como REAIS

| Capacidade | Handler/Feature | Persistência |
|---|---|---|
| Releases com identidade própria | `CreateRelease`, `GetRelease` | `ChangeIntelligenceDbContext` |
| Blast Radius computation | `ComputeBlastRadius` | `ChangeIntelligenceDbContext` |
| Change scores / risk scoring | `GetChangeScores` | `ChangeIntelligenceDbContext` |
| Freeze windows | `CreateFreezeWindow`, `IsFrozen` | `ChangeIntelligenceDbContext` |
| Rollback assessments | `GetRollbackAssessment` | `ChangeIntelligenceDbContext` |
| Approval workflows | `RequestApproval`, `ApproveChange`, `RejectChange` | `WorkflowDbContext` |
| Evidence packs | `GenerateEvidencePack`, `AttachEvidence` | `WorkflowDbContext` |
| SLA policies de workflow | `GetWorkflowSlaPolicy` | `WorkflowDbContext` |
| Promotion requests | `CreatePromotionRequest`, `EvaluateGate` | `PromotionDbContext` |
| Gate evaluations | `GetGateEvaluation` | `PromotionDbContext` |
| Spectral lint (Contract Policies) | `LintContract`, `GetLintResults` | `RulesetGovernanceDbContext` |
| Decision trail | `GetDecisionTrail` | Auditado em `AuditDbContext` |
| Change timeline | `GetChangeTimeline` | `ChangeIntelligenceDbContext` |

### Correlação Pós-Change

| Capacidade | Estado | Gap |
|---|---|---|
| Post-change gate evaluations | ✅ REAL | — |
| `RecordMitigationValidation` | ⚠️ PARCIAL | Validação incompleta |
| Correlação automática incident↔change | ❌ AUSENTE | Engine dinâmica não existe |

### Change Intelligence vs. Environments

A análise de comportamento em ambientes não produtivos (requisito crítico do CLAUDE.md §9.3):
- Promotion governance distingue ambientes explicitamente ✅
- Gate evaluations por ambiente ✅
- Comparação não-produção vs. produção: ⚠️ estrutura existe, relatório comparativo não confirmado

---

## Correlação Incident↔Change — Gap Crítico

**Requirement (CLAUDE.md §9.4):** Correlação com incidentes é capacidade obrigatória.

**Estado actual:**
- `CorrelationEvent` existe como DbSet no `IncidentDbContext`
- Seed data JSON inclui correlações estáticas
- Nenhuma engine dinâmica que correlacione automaticamente por timestamp + serviço + environment
- Frontend `IncidentsPage.tsx` não consome correlações do backend

**O que precisa existir:**
1. Engine que, ao criar incidente, busca mudanças recentes no mesmo serviço/ambiente
2. Score de correlação por proximidade temporal e serviço afetado
3. API `POST /incidents/{id}/correlation` para registar correlações detectadas
4. Frontend consumindo correlações reais

---

## Análise Comparativa Não-Produção vs. Produção

**Requirement:** Análise de comportamento em ambientes não produtivos é mandatória para prevenir falhas em produção.

**Estado:**
- Promotion governance com gate evaluations por ambiente: ✅ real
- Comparação de scores entre staging e production: ⚠️ estrutura existe
- Relatório comparativo de telemetria entre ambientes: ❌ não confirmado
- Pre-production telemetry analysis como input para Change Confidence: ❌ não confirmado

---

## Avaliação da Direção Arquitetural

### ClickHouse como Destino

`build/clickhouse/` confirma que ClickHouse está no plano. `BuildingBlocks.Observability` tem `ClickHouseAnalyticsWriter`. Direção alinhada ao CLAUDE.md §10.3.

### Avoidance de Stacks Externas como Centro

O produto não acoplou a Loki/Tempo/Prometheus como centro. OpenTelemetry como abstração de exportação é a escolha correta para self-hosted flexibility.

---

## Gaps Identificados

| Gap | Impacto | Prioridade |
|---|---|---|
| Pipeline telemetria E2E não validado | Observabilidade pode não estar a funcionar em produção | Alta |
| Engine correlação incident↔change ausente | Fluxo 3 inoperante | **Crítica** |
| Correlação não-produção vs. produção não confirmada | Change confidence incompleto | Alta |
| OtlpEndpoint hardcoded para localhost | Produção sem telemetria sem override | Alta |
| Conectores CI/CD stubs | Sem ingestão de eventos de deploy real | Alta |

---

## Recomendações

1. **Crítico:** Implementar engine de correlação dinâmica incident↔change
2. **Alta:** Validar pipeline E2E de telemetria (API → Collector → ClickHouse) em ambiente de staging
3. **Alta:** Override obrigatório de `OtlpEndpoint` documentado para self-hosted
4. **Alta:** Implementar conectores de ingestão CI/CD (GitLab, Jenkins) no módulo Integrations
5. **Média:** Implementar relatório comparativo de telemetria entre ambientes para Change Confidence

---

*Data: 28 de Março de 2026*
