# NexTraceOne — Audit & Compliance Module

## Visão Geral

O módulo Audit & Compliance é a base transversal de confiança do NexTraceOne.
Responsável pela trilha auditável, integridade, evidências, compliance e retenção.

## Subdomínios

| Subdomínio | Responsabilidade |
|------------|-----------------|
| **Audit Events** | Eventos auditáveis imutáveis com correlação por utilizador, tenant e ambiente |
| **Hash Chain** | Cadeia de integridade SHA-256 estilo blockchain |
| **Compliance** | Políticas, campanhas e resultados de avaliação de compliance |
| **Retention** | Políticas de retenção configuráveis |

## Arquitetura

```
NexTraceOne.AuditCompliance.Domain/
├── Entities/   → AuditEvent (AR), AuditChainLink, AuditCampaign, CompliancePolicy, ComplianceResult, RetentionPolicy
├── Enums/      → CampaignStatus, ComplianceSeverity, ComplianceOutcome
├── Events/     → AuditEventRecordedEvent, AuditIntegrityCheckpointCreatedEvent
├── Errors/     → AuditErrors (catálogo centralizado com i18n)
└── Ports/      → IAuditIntegrityPort

NexTraceOne.AuditCompliance.Infrastructure/
└── Persistence/
    ├── AuditDbContext.cs (6 DbSets + 1 outbox)
    └── Configurations/ (6 EF Core configs)

NexTraceOne.AuditCompliance.API/
└── Endpoints/
    └── AuditEndpointModule.cs (13 endpoints)
```

## Entidades

| Entidade | Tipo | Tabela | RowVersion |
|----------|------|--------|------------|
| `AuditEvent` | Aggregate Root (imutável) | `aud_audit_events` | ❌ (imutável) |
| `AuditChainLink` | Entity (imutável) | `aud_audit_chain_links` | ❌ (imutável) |
| `AuditCampaign` | Entity (mutável) | `aud_campaigns` | ✅ xmin |
| `CompliancePolicy` | Entity (mutável) | `aud_compliance_policies` | ✅ xmin |
| `ComplianceResult` | Entity (imutável) | `aud_compliance_results` | ❌ (imutável) |
| `RetentionPolicy` | Entity (mutável) | `aud_retention_policies` | ✅ xmin |

## Concorrência Otimista

PostgreSQL xmin via `RowVersion` nas 3 entidades mutáveis (AuditCampaign, CompliancePolicy, RetentionPolicy).
Entidades imutáveis (AuditEvent, AuditChainLink, ComplianceResult) não necessitam de RowVersion.

## Check Constraints

- `CK_aud_campaigns_status`: Status ∈ ('Planned','InProgress','Completed','Cancelled')
- `CK_aud_compliance_policies_severity`: Severity ∈ ('Low','Medium','High','Critical')
- `CK_aud_compliance_results_outcome`: Outcome ∈ ('Compliant','NonCompliant','PartiallyCompliant','NotApplicable')

## Integridade (Hash Chain)

O módulo implementa cadeia de integridade SHA-256:
- Cada AuditEvent pode ser ligado a um AuditChainLink
- Cada link contém: SequenceNumber, CurrentHash, PreviousHash
- Verificação de integridade via endpoint `GET /api/v1/audit/verify-chain`

## Endpoints (16)

| Método | Rota | Permissão |
|--------|------|-----------|
| POST | `/api/v1/audit/events` | `audit:events:write` |
| GET | `/api/v1/audit/trail` | `audit:trail:read` |
| GET | `/api/v1/audit/search` | `audit:trail:read` |
| GET | `/api/v1/audit/verify-chain` | `audit:trail:read` |
| GET | `/api/v1/audit/report` | `audit:reports:read` |
| GET | `/api/v1/audit/compliance` | `audit:compliance:read` |
| POST | `/api/v1/audit/retention/policies` | `audit:compliance:write` |
| GET | `/api/v1/audit/retention/policies` | `audit:compliance:read` |
| POST | `/api/v1/audit/retention/apply` | `audit:compliance:write` |
| POST | `/api/v1/audit/compliance/policies` | `audit:compliance:write` |
| GET | `/api/v1/audit/compliance/policies` | `audit:compliance:read` |
| GET | `/api/v1/audit/compliance/policies/{id}` | `audit:compliance:read` |
| POST | `/api/v1/audit/campaigns` | `audit:compliance:write` |
| GET | `/api/v1/audit/campaigns` | `audit:compliance:read` |
| GET | `/api/v1/audit/campaigns/{id}` | `audit:compliance:read` |
| POST | `/api/v1/audit/compliance/results` | `audit:compliance:write` |

## Permissões

| Permissão | Escopo |
|-----------|--------|
| `audit:events:write` | Registar eventos auditáveis |
| `audit:trail:read` | Consultar trilha auditável, pesquisa e integridade |
| `audit:reports:read` | Exportar relatórios de auditoria |
| `audit:compliance:read` | Consultar políticas, campanhas e resultados |
| `audit:compliance:write` | Criar/atualizar políticas, campanhas e resultados |

## Testes

113 testes cobrindo: Domain entities, Application features, Chain integrity, Compliance.
