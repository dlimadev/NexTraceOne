# Módulo: Operational Intelligence — Cenários de Teste Funcionais

> Cobertura: Incidentes, SLO/SLA, DORA, Alertas, Chaos, Observabilidade, Mitigação, Correlação, FinOps Operacional

---

## Gestão de Incidentes

### TC-OI-001 — Criar incidente com severidade crítica

| Campo | Valor |
|-------|-------|
| **Módulo** | OperationalIntelligence |
| **Feature** | CreateIncident |
| **Tipo** | Unitário |
| **Prioridade** | Crítica |

**Pré-condições:**
- Tenant autenticado com capability `operational_intelligence`.

**Passos:**
1. Enviar `CreateIncident.Command(title: "Falha total no serviço de pagamentos", severity: Critical, affectedServices: ["payments-api"], tenantId)`.
2. Handler cria entidade `Incident` com `Status = Open`, `OpenedAt = now`.
3. Publica `IncidentOpenedEvent` no Outbox.
4. Commita.

**Resultado Esperado:**
- `result.IsSuccess == true`; `incidentId` retornado.
- Evento no Outbox para notificação de on-call.

**Critério de Aceite:** HTTP 201 com `{ incidentId, status: "Open" }`.

---

### TC-OI-002 — Triagem de incidente

| Campo | Valor |
|-------|-------|
| **Módulo** | OperationalIntelligence |
| **Feature** | TriageIncident |
| **Tipo** | Unitário |
| **Prioridade** | Crítica |

**Pré-condições:** Incidente `I1` no status `Open`.

**Passos:**
1. Enviar `TriageIncident.Command(I1.Id, assignedTo: "eng-oncall@empresa.com", priority: P1, tags: ["database", "latency"])`.
2. Handler valida status atual `== Open`.
3. Atualiza `Status = Triaged`, `AssignedTo`, `Priority`.

**Resultado Esperado:**
- `Status = Triaged`; histórico de transição registrado.

**Critério de Aceite:** HTTP 200; `GetIncidentDetail` reflete novo status.

---

### TC-OI-003 — Resolver incidente e iniciar PIR

| Campo | Valor |
|-------|-------|
| **Módulo** | OperationalIntelligence |
| **Feature** | ResolveIncident / StartPostIncidentReview |
| **Tipo** | Integração |
| **Prioridade** | Crítica |

**Pré-condições:** Incidente `I1` triado.

**Passos:**
1. `ResolveIncident.Command(I1.Id, resolution: "Rollback para v2.1.0", resolvedBy: "eng1")`.
2. `Status = Resolved`; `ResolvedAt` preenchido.
3. Publicar `IncidentResolvedEvent` no Outbox.
4. `StartPostIncidentReview.Command(I1.Id, scheduledFor: now+24h)`.

**Resultado Esperado:**
- PIR criado com `Status = Scheduled`.

**Critério de Aceite:** HTTP 200 em ambos; `GetPostIncidentReview` retorna PIR.

---

### TC-OI-004 — Encontrar incidentes similares

| Campo | Valor |
|-------|-------|
| **Módulo** | OperationalIntelligence |
| **Feature** | FindSimilarIncidents |
| **Tipo** | Unitário |
| **Prioridade** | Alta |

**Pré-condições:**
- Histórico com 50 incidentes; 3 com título e serviços similares a `I_new`.

**Passos:**
1. `FindSimilarIncidents.Query(title: "Timeout no serviço de pagamentos", services: ["payments-api"], topK: 5)`.

**Resultado Esperado:**
- Retorna até 5 incidentes; os 3 similares devem estar entre os primeiros.

**Critério de Aceite:** `result.Value.Items.Count <= 5`.

---

### TC-OI-005 — Correlacionar incidente com mudanças recentes

| Campo | Valor |
|-------|-------|
| **Módulo** | OperationalIntelligence |
| **Feature** | CorrelateIncidentWithChanges |
| **Tipo** | Integração |
| **Prioridade** | Alta |

**Pré-condições:**
- Deploy do serviço `payments-api` 30 min antes do incidente.

**Passos:**
1. `CorrelateIncidentWithChanges.Command(incidentId, correlationWindowMinutes: 60)`.
2. Handler busca releases/commits no período.
3. Retorna `CorrelatedChanges` com score de correlação.

**Resultado Esperado:**
- Deploy de `payments-api` listado com `CorrelationScore > 0.7`.

**Critério de Aceite:** HTTP 200; `correlatedChanges.length >= 1`.

---

### TC-OI-006 — Gerar narrativa de incidente com IA

| Campo | Valor |
|-------|-------|
| **Módulo** | OperationalIntelligence |
| **Feature** | GenerateIncidentNarrative |
| **Tipo** | Integração |
| **Prioridade** | Alta |

**Pré-condições:** Incidente com timeline, logs correlatos e change context.

**Passos:**
1. `GenerateIncidentNarrative.Command(incidentId)`.
2. Handler agrega contexto e chama `IAIKnowledgeModule.GenerateNarrative`.
3. Persiste narrativa.

**Resultado Esperado:**
- Narrativa em linguagem natural salva; `HasNarrative = true`.

**Critério de Aceite:** HTTP 200; `GetIncidentNarrative` retorna texto não vazio.

---

### TC-OI-007 — Progresso e conclusão do PIR

| Campo | Valor |
|-------|-------|
| **Módulo** | OperationalIntelligence |
| **Feature** | ProgressPostIncidentReview |
| **Tipo** | Unitário |
| **Prioridade** | Alta |

**Pré-condições:** PIR no status `InProgress`.

**Passos:**
1. `ProgressPostIncidentReview.Command(pirId, step: "RootCause", findings: "Deadlock em transação de BD")`.
2. Handler avança estado para `RootCauseIdentified`.
3. Segunda chamada com `step: "ActionItems"` move para `ActionItemsDefined`.
4. Terceira chamada com `step: "Complete"` finaliza.

**Resultado Esperado:**
- `Status = Completed`; `CompletedAt` preenchido.

**Critério de Aceite:** HTTP 200 nas 3 transições.

---

### TC-OI-008 — Evidência de incidente

| Campo | Valor |
|-------|-------|
| **Módulo** | OperationalIntelligence |
| **Feature** | GetIncidentEvidence |
| **Tipo** | Unitário |
| **Prioridade** | Média |

**Pré-condições:** Incidente com 5 evidências anexadas (logs, screenshots, traces).

**Passos:**
1. `GetIncidentEvidence.Query(incidentId)`.

**Resultado Esperado:**
- Lista com 5 evidências; `type` e `attachedAt` preenchidos.

**Critério de Aceite:** HTTP 200 `{ items: [...] }`.

---

## SLO / SLA / Error Budget

### TC-OI-009 — Registrar definição de SLO

| Campo | Valor |
|-------|-------|
| **Módulo** | OperationalIntelligence |
| **Feature** | RegisterSloDefinition |
| **Tipo** | Unitário |
| **Prioridade** | Crítica |

**Pré-condições:** Tenant autenticado.

**Passos:**
1. `RegisterSloDefinition.Command(serviceId, metric: "availability", target: 99.9, window: "30d", sliQuery: "rate(http_requests_total{status!~'5..'}[5m])")`.
2. Handler valida `target` entre 0 e 100.
3. Persiste.

**Resultado Esperado:**
- SLO criado com `IsActive = true`.

**Critério de Aceite:** HTTP 201.

---

### TC-OI-010 — Ingestão de observação SLO

| Campo | Valor |
|-------|-------|
| **Módulo** | OperationalIntelligence |
| **Feature** | IngestSloObservation |
| **Tipo** | Unitário |
| **Prioridade** | Crítica |

**Pré-condições:** SLO `S1` registrado.

**Passos:**
1. `IngestSloObservation.Command(sloId: S1.Id, value: 99.85, timestamp: now)`.
2. Handler persiste observação.

**Resultado Esperado:**
- Observação armazenada; série temporal atualizada.

**Critério de Aceite:** HTTP 201.

---

### TC-OI-011 — Calcular error budget

| Campo | Valor |
|-------|-------|
| **Módulo** | OperationalIntelligence |
| **Feature** | ComputeErrorBudget |
| **Tipo** | Unitário |
| **Prioridade** | Crítica |

**Pré-condições:**
- SLO de 99.9% para janela de 30 dias.
- Observações: availability média 99.7% nos últimos 30 dias.

**Passos:**
1. `ComputeErrorBudget.Command(sloId)`.
2. Handler: budget total = 43.2 min; consumido = 86.4 min (violação).

**Resultado Esperado:**
- `RemainingBudgetMinutes < 0` (budget esgotado).
- `BurnRate > 1.0`.

**Critério de Aceite:** HTTP 200; `errorBudgetExhausted: true`.

---

### TC-OI-012 — Alerta de burn rate de SLO

| Campo | Valor |
|-------|-------|
| **Módulo** | OperationalIntelligence |
| **Feature** | GetSloBurnRateAlert |
| **Tipo** | Unitário |
| **Prioridade** | Alta |

**Pré-condições:** SLO com burn rate atual 14x (queima o budget em 2h).

**Passos:**
1. `GetSloBurnRateAlert.Query(sloId)`.

**Resultado Esperado:**
- `AlertLevel = Critical`; `BurnRate = 14`; `TimeToExhaustion = 2h`.

**Critério de Aceite:** HTTP 200 com dados de alerta.

---

### TC-OI-013 — Conformidade de SLO (relatório)

| Campo | Valor |
|-------|-------|
| **Módulo** | OperationalIntelligence |
| **Feature** | GetSloComplianceSummary |
| **Tipo** | Integração |
| **Prioridade** | Alta |

**Pré-condições:** 10 SLOs ativos; 7 em compliance, 3 em violação.

**Passos:**
1. `GetSloComplianceSummary.Query(period: last30Days)`.

**Resultado Esperado:**
- `Compliant = 7`, `Violated = 3`, `ComplianceRate = 70%`.

**Critério de Aceite:** HTTP 200.

---

### TC-OI-014 — Escalar violação de SLA

| Campo | Valor |
|-------|-------|
| **Módulo** | OperationalIntelligence |
| **Feature** | EscalateSlaViolation |
| **Tipo** | Unitário |
| **Prioridade** | Alta |

**Pré-condições:**
- SLA com `MaxResponseTimeMinutes = 60`; incidente aberto há 75 min sem resolução.

**Passos:**
1. `EscalateSlaViolation.Command(slaId, incidentId, escalationReason: "SLA breach 75min")`.
2. Evento publicado no Outbox para notificação de gerência.

**Resultado Esperado:**
- `EscalatedAt` preenchido; nível de escalação incrementado.

**Critério de Aceite:** HTTP 200.

---

## Métricas DORA

### TC-OI-015 — Calcular métricas DORA

| Campo | Valor |
|-------|-------|
| **Módulo** | OperationalIntelligence |
| **Feature** | ComputeDoraMetrics |
| **Tipo** | Integração |
| **Prioridade** | Crítica |

**Pré-condições:**
- 20 deploys no último mês; 3 incidentes (2 rollbacks); MTTR médio 45 min.

**Passos:**
1. `ComputeDoraMetrics.Command(teamId, period: last30Days)`.
2. Handler agrega dados de releases, rollbacks e incidentes.

**Resultado Esperado:**
- `DeploymentFrequency ≈ 0.67/dia`; `ChangeFailureRate = 15%`; `MTTR = 45min`.
- Classificação: `Médio` (DORA band).

**Critério de Aceite:** HTTP 200 com 4 métricas.

---

### TC-OI-016 — Tendência de DORA ao longo do tempo

| Campo | Valor |
|-------|-------|
| **Módulo** | OperationalIntelligence |
| **Feature** | GetDoraMetricsTrend |
| **Tipo** | Integração |
| **Prioridade** | Alta |

**Pré-condições:** 3 meses de dados DORA.

**Passos:**
1. `GetDoraMetricsTrend.Query(teamId, periods: 3)`.

**Resultado Esperado:**
- Array de 3 pontos com métricas; tendência de melhora em `MTTR`.

**Critério de Aceite:** HTTP 200; `trend.length == 3`.

---

### TC-OI-017 — Maturidade de serviço

| Campo | Valor |
|-------|-------|
| **Módulo** | OperationalIntelligence |
| **Feature** | AssessServiceMaturity |
| **Tipo** | Unitário |
| **Prioridade** | Alta |

**Pré-condições:** Serviço com SLOs, runbooks, alertas e chaos tests configurados.

**Passos:**
1. `AssessServiceMaturity.Command(serviceId)`.
2. Handler pontua dimensões (observabilidade, resiliência, documentação, governança).

**Resultado Esperado:**
- `OverallScore > 70`; nível `Advanced`.

**Critério de Aceite:** HTTP 200 com breakdown por dimensão.

---

## Alertas

### TC-OI-018 — Criar regra de alerta

| Campo | Valor |
|-------|-------|
| **Módulo** | OperationalIntelligence |
| **Feature** | CreateAlertRule |
| **Tipo** | Unitário |
| **Prioridade** | Alta |

**Pré-condições:** Tenant autenticado.

**Passos:**
1. `CreateAlertRule.Command(name: "CPU > 90%", metric: "cpu_usage_percent", threshold: 90, operator: GreaterThan, severity: High, serviceId, notificationChannels: ["slack-incidents"])`.

**Resultado Esperado:**
- Regra criada com `IsActive = true`.

**Critério de Aceite:** HTTP 201.

---

### TC-OI-019 — Ativar/desativar regra de alerta

| Campo | Valor |
|-------|-------|
| **Módulo** | OperationalIntelligence |
| **Feature** | ToggleAlertRule |
| **Tipo** | Unitário |
| **Prioridade** | Média |

**Passos:**
1. `ToggleAlertRule.Command(ruleId, active: false)`.

**Resultado Esperado:**
- `IsActive = false`; avaliações futuras ignoram a regra.

**Critério de Aceite:** HTTP 200.

---

### TC-OI-020 — Alertas de topologia

| Campo | Valor |
|-------|-------|
| **Módulo** | OperationalIntelligence |
| **Feature** | GetTopologyAwareAlerts |
| **Tipo** | Integração |
| **Prioridade** | Alta |

**Pré-condições:**
- Serviço `payments-api` depende de `database-cluster`.
- Alerta crítico ativo em `database-cluster`.

**Passos:**
1. `GetTopologyAwareAlerts.Query(serviceId: "payments-api")`.

**Resultado Esperado:**
- Alerta de `database-cluster` propagado para `payments-api` com indicação de dependência.

**Critério de Aceite:** HTTP 200 com `propagatedAlerts`.

---

## Chaos Engineering

### TC-OI-021 — Criar experimento de chaos

| Campo | Valor |
|-------|-------|
| **Módulo** | OperationalIntelligence |
| **Feature** | CreateChaosExperiment |
| **Tipo** | Unitário |
| **Prioridade** | Alta |

**Pré-condições:** Tenant com capability `chaos_engineering`.

**Passos:**
1. `CreateChaosExperiment.Command(name: "Simular falha de rede", targetService: "payments-api", type: NetworkLatency, parameters: { delayMs: 500, percentage: 50 }, environment: "staging")`.
2. Validar que ambiente não é `production` sem flag explícito.

**Resultado Esperado:**
- Experimento criado com `Status = Planned`.

**Critério de Aceite:** HTTP 201.

---

### TC-OI-022 — Relatório de cobertura de chaos

| Campo | Valor |
|-------|-------|
| **Módulo** | OperationalIntelligence |
| **Feature** | GetChaosCoverageGapReport |
| **Tipo** | Integração |
| **Prioridade** | Alta |

**Pré-condições:** 20 serviços no catalog; 8 com experimentos de chaos executados.

**Passos:**
1. `GetChaosCoverageGapReport.Query`.

**Resultado Esperado:**
- `Coverage = 40%`; lista dos 12 serviços sem cobertura.

**Critério de Aceite:** HTTP 200.

---

## Observabilidade

### TC-OI-023 — Ingestão de métricas OTel

| Campo | Valor |
|-------|-------|
| **Módulo** | OperationalIntelligence |
| **Feature** | IngestOtelMetrics |
| **Tipo** | Integração |
| **Prioridade** | Crítica |

**Pré-condições:** Agente OpenTelemetry configurado.

**Passos:**
1. `IngestOtelMetrics.Command(payload: otelMetricsJson, serviceId, timestamp)`.
2. Handler valida formato OTLP; persiste série temporal.

**Resultado Esperado:**
- Métricas indexadas; `GetIngestionFreshness` retorna `LastIngested < 60s`.

**Critério de Aceite:** HTTP 202.

---

### TC-OI-024 — Estabelecer baseline de runtime

| Campo | Valor |
|-------|-------|
| **Módulo** | OperationalIntelligence |
| **Feature** | EstablishRuntimeBaseline |
| **Tipo** | Unitário |
| **Prioridade** | Alta |

**Pré-condições:** 7 dias de dados de runtime para `payments-api`.

**Passos:**
1. `EstablishRuntimeBaseline.Command(serviceId, window: 7d)`.
2. Handler calcula percentis P50/P95/P99 para CPU, memória, latência.

**Resultado Esperado:**
- Baseline salvo com percentis.

**Critério de Aceite:** HTTP 200; `GetRuntimeBaselineComparisonReport` usa baseline.

---

### TC-OI-025 — Detectar drift de runtime

| Campo | Valor |
|-------|-------|
| **Módulo** | OperationalIntelligence |
| **Feature** | DetectRuntimeDrift |
| **Tipo** | Unitário |
| **Prioridade** | Alta |

**Pré-condições:**
- Baseline estabelecido com P95 latência = 200ms.
- Última medição: P95 = 850ms.

**Passos:**
1. `DetectRuntimeDrift.Command(serviceId)`.
2. Handler compara métricas atuais vs baseline.

**Resultado Esperado:**
- `DriftDetected = true`; `DriftedMetrics = ["latency_p95"]`; desvio percentual calculado.

**Critério de Aceite:** HTTP 200.

---

### TC-OI-026 — Score de observabilidade

| Campo | Valor |
|-------|-------|
| **Módulo** | OperationalIntelligence |
| **Feature** | GetObservabilityScore |
| **Tipo** | Unitário |
| **Prioridade** | Alta |

**Pré-condições:** Serviço com métricas, logs, traces e alertas configurados.

**Passos:**
1. `GetObservabilityScore.Query(serviceId)`.
2. Handler pontua cobertura de pilares (metrics/logs/traces/alerting/SLOs).

**Resultado Esperado:**
- `OverallScore` entre 0 e 100; breakdown por pilar.

**Critério de Aceite:** HTTP 200.

---

## Mitigação e Cura Automática

### TC-OI-027 — Criar workflow de mitigação

| Campo | Valor |
|-------|-------|
| **Módulo** | OperationalIntelligence |
| **Feature** | CreateMitigationWorkflow |
| **Tipo** | Unitário |
| **Prioridade** | Alta |

**Passos:**
1. `CreateMitigationWorkflow.Command(name: "Rollback Automático", steps: [{action: "CreateJiraTicket"}, {action: "NotifyOnCall"}, {action: "TriggerRollback", params: {...}}])`.

**Resultado Esperado:**
- Workflow criado com `Steps.Count == 3`.

**Critério de Aceite:** HTTP 201.

---

### TC-OI-028 — Gerar recomendação de cura

| Campo | Valor |
|-------|-------|
| **Módulo** | OperationalIntelligence |
| **Feature** | GenerateHealingRecommendation |
| **Tipo** | Integração |
| **Prioridade** | Alta |

**Pré-condições:** Incidente com drift detectado e contexto de mudança.

**Passos:**
1. `GenerateHealingRecommendation.Command(incidentId)`.
2. Handler consulta IA e histórico de incidentes similares.

**Resultado Esperado:**
- Recomendação gerada com `Confidence > 0.7`; passos de ação.

**Critério de Aceite:** HTTP 200.

---

### TC-OI-029 — Aprovar ação de cura

| Campo | Valor |
|-------|-------|
| **Módulo** | OperationalIntelligence |
| **Feature** | ApproveHealingRecommendation |
| **Tipo** | Unitário |
| **Prioridade** | Alta |

**Pré-condições:** Recomendação `R1` pendente de aprovação.

**Passos:**
1. `ApproveHealingRecommendation.Command(recommendationId: R1.Id, approvedBy: "sre-lead@empresa.com")`.
2. `Status = Approved`; ação executada.

**Resultado Esperado:**
- `ApprovedAt` preenchido; ação disparada via Outbox.

**Critério de Aceite:** HTTP 200.

---

### TC-OI-030 — Selecionar playbook de mitigação por similaridade

| Campo | Valor |
|-------|-------|
| **Módulo** | OperationalIntelligence |
| **Feature** | SelectMitigationPlaybook |
| **Tipo** | Unitário |
| **Prioridade** | Alta |

**Pré-condições:**
- Biblioteca com 10 playbooks; incidente de timeout em database.

**Passos:**
1. `SelectMitigationPlaybook.Command(incidentId, context: "database timeout")`.

**Resultado Esperado:**
- Top-3 playbooks relevantes retornados com score de relevância.

**Critério de Aceite:** HTTP 200.

---

## Correlação com Legado

### TC-OI-031 — Correlacionar evento de mainframe

| Campo | Valor |
|-------|-------|
| **Módulo** | OperationalIntelligence |
| **Feature** | CorrelateMainframeEvent |
| **Tipo** | Integração |
| **Prioridade** | Alta |

**Pré-condições:** Evento de mainframe `JCL_ABEND_0C7` ingerido.

**Passos:**
1. `CorrelateMainframeEvent.Command(eventId, correlationWindow: 5min)`.
2. Handler mapeia para serviços modernos dependentes.

**Resultado Esperado:**
- Incidente relacionado criado ou vinculado; serviços impactados identificados.

**Critério de Aceite:** HTTP 200.

---

### TC-OI-032 — Auto-criar incidente a partir de falha de batch

| Campo | Valor |
|-------|-------|
| **Módulo** | OperationalIntelligence |
| **Feature** | AutoCreateIncidentFromBatchFailure |
| **Tipo** | Integração |
| **Prioridade** | Alta |

**Pré-condições:** Evento `BatchJobFailed` publicado com `jobName = "cob_prn_001"`, `exitCode = "JCL000"`.

**Passos:**
1. Handler `AutoCreateIncidentFromBatchFailure` processa evento do Outbox.
2. Cria incidente automaticamente com severidade mapeada do exit code.

**Resultado Esperado:**
- Incidente criado com `AutoGenerated = true`; `Source = "BatchJob"`.

**Critério de Aceite:** Incidente visível em `ListIncidents` com origem batch.

---

## Relatórios e Análises

### TC-OI-033 — Relatório MTTR

| Campo | Valor |
|-------|-------|
| **Módulo** | OperationalIntelligence |
| **Feature** | GetMttrTrendReport |
| **Tipo** | Integração |
| **Prioridade** | Alta |

**Pré-condições:** 60 dias de dados de incidentes.

**Passos:**
1. `GetMttrTrendReport.Query(serviceId, period: last60Days)`.

**Resultado Esperado:**
- Série temporal com MTTR por semana; tendência de melhora ou piora.

**Critério de Aceite:** HTTP 200 com array de pontos.

---

### TC-OI-034 — Relatório de risco de serviço

| Campo | Valor |
|-------|-------|
| **Módulo** | OperationalIntelligence |
| **Feature** | ComputeServiceRiskProfile |
| **Tipo** | Unitário |
| **Prioridade** | Alta |

**Pré-condições:** Serviço com histórico de incidentes e dependências críticas.

**Passos:**
1. `ComputeServiceRiskProfile.Command(serviceId)`.
2. Handler pondera: frequência de incidentes × severidade × blast radius.

**Resultado Esperado:**
- `RiskScore` calculado; `RiskLevel = High` se score > 70.

**Critério de Aceite:** HTTP 200.

---

### TC-OI-035 — Padrão de previsão de incidentes

| Campo | Valor |
|-------|-------|
| **Módulo** | OperationalIntelligence |
| **Feature** | GetIncidentPredictionPattern |
| **Tipo** | Integração |
| **Prioridade** | Média |

**Pré-condições:** Padrão identificado: incidentes às sextas-feiras após deploy.

**Passos:**
1. `GetIncidentPredictionPattern.Query(serviceId)`.

**Resultado Esperado:**
- Padrão `DayOfWeek: Friday + PostDeploy` com confiança > 80%.

**Critério de Aceite:** HTTP 200.

---

### TC-OI-036 — Relatório de previsão de risco de deploy

| Campo | Valor |
|-------|-------|
| **Módulo** | OperationalIntelligence |
| **Feature** | GetDeploymentRiskForecastReport |
| **Tipo** | Integração |
| **Prioridade** | Alta |

**Pré-condições:** Deploy agendado para produção amanhã.

**Passos:**
1. `GetDeploymentRiskForecastReport.Query(releaseId)`.

**Resultado Esperado:**
- `RiskScore` do deploy; fatores de risco listados; recomendação de janela alternativa.

**Critério de Aceite:** HTTP 200.

---

### TC-OI-037 — Relatório de frequência de incidentes por serviço

| Campo | Valor |
|-------|-------|
| **Módulo** | OperationalIntelligence |
| **Feature** | GetIncidentImpactScorecardReport |
| **Tipo** | Integração |
| **Prioridade** | Alta |

**Passos:**
1. `GetIncidentImpactScorecardReport.Query(period: last90Days)`.

**Resultado Esperado:**
- Scorecard com top-10 serviços mais afetados; frequência e MTTR médio.

**Critério de Aceite:** HTTP 200.

---

### TC-OI-038 — Isolamento de incidentes entre tenants

| Campo | Valor |
|-------|-------|
| **Módulo** | OperationalIntelligence |
| **Feature** | ListIncidents |
| **Tipo** | Segurança |
| **Prioridade** | Crítica |

**Pré-condições:** Tenant A com 5 incidentes; Tenant B com 10 incidentes.

**Passos:**
1. Tenant A lista incidentes.

**Resultado Esperado:**
- `result.Value.Count == 5`; nenhum incidente de Tenant B visível.

**Critério de Aceite:** RLS + filtro de repositório garantem isolamento.

---

### TC-OI-039 — Relatório de base de conhecimento de incidentes

| Campo | Valor |
|-------|-------|
| **Módulo** | OperationalIntelligence |
| **Feature** | GetIncidentKnowledgeBaseReport |
| **Tipo** | Integração |
| **Prioridade** | Média |

**Pré-condições:** 30 PIRs concluídos com action items.

**Passos:**
1. `GetIncidentKnowledgeBaseReport.Query`.

**Resultado Esperado:**
- Top causas raiz; padrões recorrentes; % de incidentes com PIR concluído.

**Critério de Aceite:** HTTP 200.

---

### TC-OI-040 — Playbook sugerido para incidente

| Campo | Valor |
|-------|-------|
| **Módulo** | OperationalIntelligence |
| **Feature** | SuggestRunbooksForIncident |
| **Tipo** | Unitário |
| **Prioridade** | Alta |

**Pré-condições:** Incidente de "alto consumo de CPU" em `api-gateway`.

**Passos:**
1. `SuggestRunbooksForIncident.Command(incidentId)`.

**Resultado Esperado:**
- Lista de runbooks relevantes com score > 0.5.

**Critério de Aceite:** HTTP 200; pelo menos 1 runbook sugerido.

---

### TC-OI-041 — Ranking de serviços por violação de SLO

| Campo | Valor |
|-------|-------|
| **Módulo** | OperationalIntelligence |
| **Feature** | GetSloServiceRankingReport |
| **Tipo** | Integração |
| **Prioridade** | Alta |

**Pré-condições:** 15 serviços com SLOs; 6 em violação.

**Passos:**
1. `GetSloServiceRankingReport.Query`.

**Resultado Esperado:**
- Ranking decrescente por severidade de violação.

**Critério de Aceite:** HTTP 200.

---

### TC-OI-042 — Avaliação de prontidão operacional

| Campo | Valor |
|-------|-------|
| **Módulo** | OperationalIntelligence |
| **Feature** | GetOperationalReadinessReport |
| **Tipo** | Integração |
| **Prioridade** | Alta |

**Pré-condições:** Serviço candidato a deploy em produção.

**Passos:**
1. `GetOperationalReadinessReport.Query(serviceId, targetEnvironment: "production")`.
2. Handler verifica: SLOs definidos, alertas configurados, runbooks existentes, chaos tests executados.

**Resultado Esperado:**
- `ReadinessScore`; lista de itens faltantes (gates vermelhos).

**Critério de Aceite:** HTTP 200.

---

### TC-OI-043 — Correlação de custo com mudança

| Campo | Valor |
|-------|-------|
| **Módulo** | OperationalIntelligence |
| **Feature** | CorrelateCloudCostWithChange |
| **Tipo** | Integração |
| **Prioridade** | Média |

**Pré-condições:**
- Deploy de nova versão que aumentou consumo de CPU em 40%.
- Custo cloud aumentou $500/dia após o deploy.

**Passos:**
1. `CorrelateCloudCostWithChange.Command(serviceId, changeDate, costIncrease: 500)`.

**Resultado Esperado:**
- Deploy identificado como provável causa; `CorrelationScore > 0.8`.

**Critério de Aceite:** HTTP 200.

---

### TC-OI-044 — Monitorar inteligência on-call

| Campo | Valor |
|-------|-------|
| **Módulo** | OperationalIntelligence |
| **Feature** | GetOnCallIntelligence |
| **Tipo** | Integração |
| **Prioridade** | Alta |

**Pré-condições:** Engenheiro on-call ativo; 3 alertas disparados nas últimas 2h.

**Passos:**
1. `GetOnCallIntelligence.Query(userId: engineer.Id)`.

**Resultado Esperado:**
- Resumo de alertas ativos; contexto de mudanças recentes; runbooks sugeridos.

**Critério de Aceite:** HTTP 200.

---

### TC-OI-045 — Registrar nota operacional

| Campo | Valor |
|-------|-------|
| **Módulo** | OperationalIntelligence |
| **Feature** | CreateOperationalNote |
| **Tipo** | Unitário |
| **Prioridade** | Média |

**Passos:**
1. `CreateOperationalNote.Command(serviceId, content: "Conexão com DB lenta entre 13h-15h; possível manutenção.", author: "eng1")`.

**Resultado Esperado:**
- Nota criada; visível em `GetServiceOperationalTimeline`.

**Critério de Aceite:** HTTP 201.

---

### TC-OI-046 — Relatório de estabilidade de ambiente

| Campo | Valor |
|-------|-------|
| **Módulo** | OperationalIntelligence |
| **Feature** | GetEnvironmentStabilityReport |
| **Tipo** | Integração |
| **Prioridade** | Alta |

**Pré-condições:** Ambiente `staging` com 30 dias de dados.

**Passos:**
1. `GetEnvironmentStabilityReport.Query(environment: "staging", period: last30Days)`.

**Resultado Esperado:**
- `UptimePercentage`; número de deploys; número de rollbacks; MTTR.

**Critério de Aceite:** HTTP 200.

---

### TC-OI-047 — Correlação de anomalia MQ

| Campo | Valor |
|-------|-------|
| **Módulo** | OperationalIntelligence |
| **Feature** | CorrelateMqEvent / AutoCreateIncidentFromMqAnomaly |
| **Tipo** | Integração |
| **Prioridade** | Alta |

**Pré-condições:** Anomalia em fila MQ detectada (mensagens represadas > threshold).

**Passos:**
1. `CorrelateMqEvent.Command(mqEventId)`.
2. Se threshold excedido, `AutoCreateIncidentFromMqAnomaly` dispara criação de incidente.

**Resultado Esperado:**
- Incidente criado com `Source = "MQAnomaly"`.

**Critério de Aceite:** HTTP 200; incidente listável.

---

### TC-OI-048 — Índice de maturidade SRE

| Campo | Valor |
|-------|-------|
| **Módulo** | OperationalIntelligence |
| **Feature** | GetSreMaturityIndexReport |
| **Tipo** | Integração |
| **Prioridade** | Média |

**Pré-condições:** Time com SLOs, DORA, postmortems e playbooks.

**Passos:**
1. `GetSreMaturityIndexReport.Query(teamId)`.

**Resultado Esperado:**
- `MaturityLevel` (1-5); dimensões avaliadas (incident management, SLO, automation, documentation).

**Critério de Aceite:** HTTP 200.

---

### TC-OI-049 — Acesso sem capability

| Campo | Valor |
|-------|-------|
| **Módulo** | OperationalIntelligence |
| **Feature** | CreateChaosExperiment |
| **Tipo** | Segurança |
| **Prioridade** | Crítica |

**Pré-condições:** Tenant no plano `Starter` sem `chaos_engineering`.

**Passos:**
1. `CreateChaosExperiment.Command(...)`.
2. Handler verifica `currentTenant.HasCapability("chaos_engineering")`.

**Resultado Esperado:**
- `result.IsFailure == true`; `ErrorType = Forbidden`.

**Critério de Aceite:** HTTP 403.

---

### TC-OI-050 — Tendência de violação de SLO

| Campo | Valor |
|-------|-------|
| **Módulo** | OperationalIntelligence |
| **Feature** | GetSloViolationTrend |
| **Tipo** | Integração |
| **Prioridade** | Alta |

**Pré-condições:** 90 dias de histórico de SLOs.

**Passos:**
1. `GetSloViolationTrend.Query(serviceId, granularity: weekly)`.

**Resultado Esperado:**
- Array de 13 semanas; `violationsCount` por semana; tendência de alta.

**Critério de Aceite:** HTTP 200 com array.

---
