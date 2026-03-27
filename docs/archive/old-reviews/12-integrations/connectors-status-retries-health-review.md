# Integrations — Connectors, Status, Retries and Health Review

> **Module:** Integrations (12)  
> **Date:** 2026-03-25  
> **Status:** Review completo

---

## 1. Conectores existentes

O módulo suporta conectores como **conceito genérico** via `IntegrationConnector`. Não existem implementações de conectores específicos (adapters) — o módulo é um **framework de gestão de conectores**, não uma colecção de adapters prontos.

### Tipos de conectores modelados (via `ConnectorType` string)

| ConnectorType | Providers esperados | Estado |
|--------------|--------------------|----|
| CI/CD | GitHub Actions, GitLab CI, Azure DevOps, Jenkins | 🟡 Modelado, sem adapter |
| Telemetry | Datadog, New Relic, Dynatrace, Prometheus | 🟡 Modelado, sem adapter |
| Incidents | PagerDuty, Opsgenie, VictorOps | 🟡 Modelado, sem adapter |
| Source Control | GitHub, GitLab, Bitbucket | 🟡 Modelado, sem adapter |
| ITSM | ServiceNow, Jira Service Management | 🟡 Modelado, sem adapter |
| Communication | Slack, Microsoft Teams | 🟡 Modelado, sem adapter |
| Monitoring | Grafana, Zabbix, Nagios | 🟡 Modelado, sem adapter |

**Nota:** Os conectores são dados de configuração, não código específico. O adapter pattern para executar integrações reais não está implementado.

---

## 2. Integrações reais existentes

| Integração real | Estado | Detalhe |
|----------------|--------|---------|
| Recepção de webhook HTTP genérico | ❌ Não implementado | Sem endpoint de webhook |
| Polling de API externa | ❌ Não implementado | Sem scheduler/worker |
| Push de dados para sistema externo | ❌ Não implementado | Sem outbound adapter |
| OAuth2 flow com provider externo | ❌ Não implementado | `AuthenticationMode` é string, não flow real |
| API key validation com provider | ❌ Não implementado | Sem verificação de credenciais |

**Conclusão:** O módulo gere **metadados** de integrações mas não **executa** integrações reais.

---

## 3. Parametrização dos conectores

| Parâmetro | Tipo | Editável via API | Validado |
|-----------|------|-----------------|----------|
| Name | `string(200)` | ❌ (sem PUT endpoint) | ✅ (max length) |
| ConnectorType | `string(100)` | ❌ | ⚠️ (sem enum, aceita qualquer string) |
| Provider | `string` | ❌ | ⚠️ (sem validação de provider conhecido) |
| Endpoint | `string?` | ❌ | ❌ (sem validação URL) |
| Environment | `string` | ❌ | ❌ (sem validação) |
| AuthenticationMode | `string` | ❌ | ❌ (sem validação) |
| PollingMode | `string` | ❌ | ❌ (sem validação) |
| AllowedTeams | `List<string>` (JSON) | ❌ | ⚠️ (sem validação de teams existentes) |

**Gap principal:** Nenhum parâmetro é validado contra valores conhecidos. ConnectorType, Provider, AuthenticationMode e PollingMode são strings livres.

---

## 4. Política de retry

### Estado actual

| Aspecto | Estado | Detalhe |
|---------|--------|---------|
| Endpoint de retry | ✅ Existe | POST `/connectors/{id}/retry` |
| RetryAttempt counter | ✅ Existe | Campo `RetryAttempt` na entity |
| Max retry attempts | ❌ Ausente | Sem limite configurável |
| Backoff strategy | ❌ Ausente | Sem exponential backoff |
| Circuit breaker | ❌ Ausente | Sem protecção contra falhas repetidas |
| Retry worker | ❌ Ausente | Endpoint marca retry mas ninguém processa |
| Retry delay | ❌ Ausente | Sem configuração de intervalo |
| Dead letter queue | ❌ Ausente | Sem tratamento de retries esgotados |

### Modelo target de retry policy

```
IntegrationConnector
├── MaxRetryAttempts: int (default: 3)
├── RetryBackoffSeconds: int (default: 60)
├── TimeoutSeconds: int (default: 300)
└── CircuitBreakerThreshold: int (default: 5 falhas consecutivas)

Retry flow:
1. Execução falha → marca Failed
2. Se RetryAttempt < MaxRetryAttempts → agenda retry com backoff
3. Backoff = RetryBackoffSeconds × 2^RetryAttempt
4. Se RetryAttempt >= MaxRetryAttempts → marca como Permanently Failed
5. Se falhas consecutivas >= CircuitBreakerThreshold → circuit breaker OPEN
6. Circuit breaker fecha após período de cooldown
```

---

## 5. Status de execução

### Estado actual ✅

O `ExecutionResult` enum cobre os estados necessários:

| Estado | Valor | Trigger |
|--------|-------|---------|
| Running | 0 | `IngestionExecution.Start()` |
| Success | 1 | `CompleteSuccess()` |
| PartialSuccess | 2 | `CompletePartialSuccess()` |
| Failed | 3 | `CompleteFailed()` |
| Cancelled | 4 | — (não há método de cancel) |
| TimedOut | 5 | — (não há detecção de timeout) |

### Gaps

| # | Gap | Impacto |
|---|-----|---------|
| R-01 | Sem método `Cancel()` em IngestionExecution | Cancelled nunca é usado |
| R-02 | Sem detecção automática de timeout | TimedOut nunca é setado automaticamente |
| R-03 | Sem transição de Running → Cancelled para cleanup | Execuções stuck em Running |
| R-04 | Sem "stuck execution detector" | Execuções Running sem CompletedAt > N minutos ficam orphaned |

---

## 6. Health e heartbeat

### Estado actual

| Aspecto | Estado | Detalhe |
|---------|--------|---------|
| ConnectorHealth enum | ✅ | Unknown, Healthy, Degraded, Unhealthy, Critical |
| Health transitions | ⚠️ Parcial | `RecordSuccess()` → Healthy, `RecordFailure()` → calcula, `MarkDegraded()` manual |
| Health endpoint | ✅ | GET `/integrations/health` retorna summary |
| Heartbeat/ping | ❌ | Sem verificação periódica de conectividade |
| Health history | ❌ | Sem registo de transições de health ao longo do tempo |
| Health-based alerting | ❌ | Sem publicação de evento quando health muda |

### Lógica de health actual (em `IntegrationConnector`)

```
RecordSuccess():
  Status → Active
  Health → Healthy
  LastSuccessAt → now

RecordFailure(msg):
  LastErrorAt → now
  LastErrorMessage → msg
  FailedExecutions++
  Se FailedExecutions > TotalExecutions/2 → Health = Critical
  Senão se FailedExecutions > 3 → Health = Unhealthy

UpdateFreshnessLag(lag):
  FreshnessLagMinutes → lag
  Se lag > 240 → Health = Degraded
```

### Gaps

| # | Gap | Impacto | Prioridade |
|---|-----|---------|-----------|
| R-05 | Sem heartbeat periódico | Health só muda em execuções, não detecta indisponibilidade | 🟡 P2_HIGH |
| R-06 | Sem health history | Impossível ver timeline de health | 🟢 P3_MEDIUM |
| R-07 | Threshold de freshness hardcoded (240 min) | Deveria ser configurável por conector | 🟡 P2_HIGH |
| R-08 | Sem health-based alerting | Degradation não notifica ninguém | 🟡 P2_HIGH |

---

## 7. Histórico operacional

### Estado actual

| Aspecto | Estado |
|---------|--------|
| Execuções registadas | ✅ `IngestionExecution` com pagination |
| Filtros de execução | ✅ Por connector, source, result, date range |
| Métricas no conector | ✅ TotalExecutions, SuccessfulExecutions, FailedExecutions |
| Freshness tracking | ✅ FreshnessStatus com cálculo automático |
| Trust level history | ❌ Sem registo de promoções de trust |
| Health transition history | ❌ Sem registo |
| Configuration change history | ❌ Sem registo de alterações de config |
| Retention policy | ❌ Sem política de retenção de execuções |

---

## 8. Distinção falha técnica vs falha funcional

| Tipo | Definição | Estado |
|------|-----------|--------|
| **Falha técnica** | Timeout, connection refused, SSL error, 500 | ⚠️ Capturada em `ErrorMessage` mas sem classificação |
| **Falha funcional** | Dados inválidos, schema mismatch, permission denied (403) | ⚠️ Capturada em `ErrorCode` mas sem classificação |

**Gap:** Sem classificação formal de tipo de falha. `ErrorCode` é string livre, não enum tipado.

**Recomendação:** Criar enum `ErrorCategory { Technical, Functional, Configuration, Authentication, RateLimit, Unknown }` e classificar erros automaticamente.

---

## 9. Backlog de correcções (conectores, status, retries, health)

| # | Item | Prioridade | Tipo | Esforço |
|---|------|-----------|------|---------|
| R-01 | Adicionar método `Cancel()` a IngestionExecution | 🟡 P2_HIGH | FUNCTIONAL_FIX | 1h |
| R-02 | Implementar stuck execution detector (background) | 🟡 P2_HIGH | FUNCTIONAL_FIX | 4h |
| R-03 | Adicionar retry policy fields a IntegrationConnector | 🔴 P1_CRITICAL | FUNCTIONAL_FIX | 2h |
| R-04 | Implementar retry worker com backoff | 🔴 P1_CRITICAL | FUNCTIONAL_FIX | 8h |
| R-05 | Implementar heartbeat/ping periódico | 🟡 P2_HIGH | FUNCTIONAL_FIX | 6h |
| R-06 | Registar health transitions como eventos | 🟢 P3_MEDIUM | FUNCTIONAL_FIX | 3h |
| R-07 | Tornar freshness threshold configurável por conector | 🟡 P2_HIGH | FUNCTIONAL_FIX | 2h |
| R-08 | Publicar health change events para Notifications | 🟡 P2_HIGH | FUNCTIONAL_FIX | 2h |
| R-09 | Classificar erros com ErrorCategory enum | 🟢 P3_MEDIUM | FUNCTIONAL_FIX | 3h |
| R-10 | Tipar ConnectorType como enum (ou validated string set) | 🟡 P2_HIGH | STRUCTURAL_FIX | 3h |
| R-11 | Implementar retention policy para execuções antigas | 🟢 P3_MEDIUM | STRUCTURAL_FIX | 4h |
| R-12 | Implementar circuit breaker pattern | 🟢 P3_MEDIUM | STRUCTURAL_FIX | 8h |

**Total estimado: ~46h**
