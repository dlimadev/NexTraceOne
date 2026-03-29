# Onda 12 — Segurança, Readiness e Operação

> **Duração estimada:** 2-3 semanas
> **Dependências:** Todas as ondas anteriores
> **Risco:** Baixo — hardening final
> **Referência:** [LEGACY-MAINFRAME-WAVES.md](../LEGACY-MAINFRAME-WAVES.md)

---

## Objetivo

Segurança enterprise, hardening e readiness para produção. Garantir que a capability legacy está pronta para deployment em ambientes enterprise com requisitos reais de segurança, compliance e operação.

---

## Entregáveis

- [ ] RBAC permissions para legacy modules
- [ ] Data masking para logs mainframe
- [ ] Audit trail completo para legacy assets
- [ ] Health checks para legacy connectors
- [ ] Rate limiting para endpoints de ingestão legacy
- [ ] Sizing guide para legacy workloads
- [ ] Troubleshooting guide
- [ ] Performance testing com volumes enterprise
- [ ] Security hardening documentation
- [ ] Operational runbooks

---

## Impacto Backend

### RBAC — Novas Permissões

| Permissão | Descrição | Personas |
|---|---|---|
| `legacy:read` | Visualizar ativos legacy | Engineer, Tech Lead, Architect |
| `legacy:write` | Registar/editar ativos legacy | Tech Lead, Architect |
| `legacy:admin` | Configurar integrações legacy | Platform Admin |
| `batch:read` | Visualizar batch intelligence | Operations, Tech Lead |
| `batch:write` | Gerir batch definitions/SLAs | Operations, Platform Admin |
| `mq:read` | Visualizar messaging intelligence | Operations, Architect |
| `mq:write` | Gerir MQ definitions | Operations, Platform Admin |
| `legacy-change:read` | Ver análise de impacto legacy | Engineer, Architect, CAB |
| `legacy-change:write` | Submeter mudanças legacy | Engineer, Tech Lead |
| `legacy-change:approve` | Aprovar mudanças legacy (CAB) | Architect, CAB |

### Data Masking

Regras de mascaramento para dados sensíveis em telemetria legacy:

| Dado | Padrão | Substituição |
|---|---|---|
| Account number | `\b\d{10,12}\b` em contexto account | `****XXXX` |
| Credit card (PAN) | `\b\d{4}[-\s]?\d{4}[-\s]?\d{4}[-\s]?\d{4}\b` | `****-****-****-XXXX` |
| SSN / NIF | `\b\d{3}[-\s]?\d{2}[-\s]?\d{4}\b` | `***-**-XXXX` |
| IBAN | `\b[A-Z]{2}\d{2}[A-Z0-9]{4,30}\b` | `XX00****XXXX` |

**Implementação:**
- Redaction processor no OTel Collector (config templates)
- Redaction middleware na Ingestion API
- Masking em logs antes de persistência
- Configurável por tenant/ambiente

### Audit Trail

Extensão do `AuditEvent` existente com novos event types:

| Event Type | Trigger |
|---|---|
| `legacy.asset.created` | Registo de ativo legacy |
| `legacy.asset.updated` | Atualização de ativo legacy |
| `legacy.asset.deleted` | Remoção de ativo legacy |
| `legacy.copybook.imported` | Import de copybook |
| `legacy.copybook.versioned` | Nova versão de copybook |
| `legacy.change.submitted` | Mudança legacy submetida |
| `legacy.change.approved` | Mudança legacy aprovada |
| `legacy.change.rejected` | Mudança legacy rejeitada |
| `legacy.batch.sla.breached` | SLA de batch breached |
| `legacy.mq.anomaly.detected` | Anomalia MQ detectada |

### Health Checks

Novos health checks para componentes legacy:

| Check | Frequência | Descrição |
|---|---|---|
| `legacy-assets-db` | 30s | Conectividade com LegacyAssetsDbContext |
| `batch-intelligence-db` | 30s | Conectividade com BatchIntelligenceDbContext |
| `messaging-intelligence-db` | 30s | Conectividade com MessagingIntelligenceDbContext |
| `clickhouse-legacy-tables` | 60s | Verificação de tabelas ClickHouse legacy |
| `batch-sla-evaluator` | 5min | Job de SLA evaluation está ativo |
| `mq-anomaly-detector` | 5min | Job de anomaly detection está ativo |

### Rate Limiting

| Endpoint | Rate Limit | Burst |
|---|---|---|
| `POST /api/v1/batch/events` | 1000/min | 5000 |
| `POST /api/v1/mq/events` | 1000/min | 5000 |
| `POST /api/v1/mainframe/events` | 500/min | 2000 |
| `POST /api/v1/legacy/assets/sync` | 100/min | 500 |
| `POST /api/v1/legacy/dependencies` | 100/min | 500 |

---

## Documentação Operacional

### Sizing Guide

| Componente | Small (< 100 jobs) | Medium (100-1000 jobs) | Large (1000+ jobs) |
|---|---|---|---|
| PostgreSQL (legacy tables) | +500MB | +2GB | +10GB |
| ClickHouse (batch 1yr) | +1GB | +10GB | +100GB |
| ClickHouse (MQ 90d) | +500MB | +5GB | +50GB |
| ClickHouse (mainframe 30d) | +200MB | +2GB | +20GB |
| Background Worker memory | +100MB | +256MB | +512MB |

### Troubleshooting Guide

Documento com:
- Como diagnosticar falhas de ingestão
- Como verificar correlação de eventos
- Como troubleshoot anomaly detection
- Como verificar SLA evaluation
- Como verificar connectivity com fontes mainframe
- Common errors e soluções

### Operational Runbooks

- `RUNBOOK-LEGACY-INGESTION.md` — Procedimentos para ingestão legacy
- `RUNBOOK-BATCH-SLA.md` — Procedimentos para SLA breaches
- `RUNBOOK-MQ-ANOMALY.md` — Procedimentos para anomalias MQ
- `RUNBOOK-LEGACY-CONNECTIVITY.md` — Procedimentos para conectividade

---

## Performance Testing

### Cenários de Teste

| Cenário | Volume | Target |
|---|---|---|
| Batch event ingestion | 10K events/min | < 100ms P99 |
| MQ statistics ingestion | 5K events/min | < 100ms P99 |
| Mainframe event ingestion | 2K events/min | < 200ms P99 |
| Legacy asset catalog query | 10K assets | < 500ms P99 |
| Batch dashboard load | 1000 jobs | < 2s |
| MQ topology load | 100 managers, 5K queues | < 3s |
| Blast radius calculation | 1000 nodes graph | < 5s |
| Copybook parse | 500 fields | < 1s |

---

## Testes

### Testes de Segurança (~20)
- RBAC: verificar que cada endpoint respeita permissões
- Data masking: verificar que dados sensíveis são mascarados
- Audit trail: verificar que eventos são registados
- Rate limiting: verificar que limits são aplicados

### Performance Tests (~10)
- Load tests com volumes dos cenários acima
- Stress tests com 2x volume esperado
- Endurance tests (1h com volume constante)

---

## Critérios de Aceite

1. ✅ RBAC funcional para todos os recursos legacy
2. ✅ Dados sensíveis mascarados em logs e telemetria
3. ✅ Audit trail completo para todas as acções em ativos legacy
4. ✅ Health checks reportam estado de todos os componentes legacy
5. ✅ Rate limiting funcional nos endpoints de ingestão
6. ✅ Sizing guide publicado e verificado
7. ✅ Troubleshooting guide completo
8. ✅ Runbooks operacionais publicados
9. ✅ Performance dentro dos targets definidos
10. ✅ Zero vulnerabilidades de segurança em security review

---

## Stories

| ID | Story | Prioridade |
|---|---|---|
| W12-S01 | Implementar RBAC permissions para legacy modules | P0 |
| W12-S02 | Implementar data masking para logs mainframe | P0 |
| W12-S03 | Registar novos audit event types | P1 |
| W12-S04 | Implementar health checks para componentes legacy | P1 |
| W12-S05 | Implementar rate limiting nos endpoints legacy | P1 |
| W12-S06 | Criar sizing guide | P1 |
| W12-S07 | Criar troubleshooting guide | P1 |
| W12-S08 | Criar operational runbooks | P2 |
| W12-S09 | Executar performance tests | P1 |
| W12-S10 | Security review e fix | P0 |
| W12-S11 | Testes de segurança (~20) | P0 |
| W12-S12 | Performance tests (~10) | P1 |
| W12-S13 | Seed data para permissions legacy | P1 |
