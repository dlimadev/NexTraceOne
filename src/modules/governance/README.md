# NexTraceOne — Governance Module

## Visão Geral

O módulo Governance é responsável pela gestão de políticas, conformidade, exceções (waivers),
evidências de governança, reporting executivo e controlo organizacional dentro do NexTraceOne.

É a unidade funcional que permite definir, distribuir e auditar regras de governança
aplicáveis a serviços, contratos, mudanças e operações, organizadas por equipas e domínios.

## Escopo do Módulo

### O que PERTENCE ao Governance:
- Equipas e domínios organizacionais
- Pacotes de governança (packs) com versionamento e lifecycle
- Regras e bindings de governança
- Rollouts de pacotes
- Waivers (exceções de conformidade)
- Delegação de administração
- Políticas de conformidade
- Controlos empresariais
- Evidências de governança
- Reporting executivo
- FinOps contextualizado por governança
- Risk center e maturity scorecards

### O que NÃO PERTENCE ao Governance:
- **Integrations** (connectors, ingestion sources/executions) → Módulo próprio (OI-02)
- **Product Analytics** (analytics events, persona usage, journeys) → Módulo próprio (OI-03)
- **Change Governance** (workflow, promotion, releases) → Módulo separado
- **Audit & Compliance** (audit trail) → Módulo separado

> **Nota**: O GovernanceDbContext contém temporariamente DbSets de Integrations
> (IntegrationConnectors, IngestionSources, IngestionExecutions) e Product Analytics
> (AnalyticsEvents) que serão extraídos para módulos próprios em OI-02 e OI-03.

## Arquitetura

```
NexTraceOne.Governance.Domain/
├── Entities/           → 9 entidades próprias + 4 temporárias (para extração)
│   ├── Team.cs                      (Aggregate Root)
│   ├── GovernanceDomain.cs          (Aggregate Root)
│   ├── GovernancePack.cs            (Aggregate Root)
│   ├── GovernanceWaiver.cs          (Aggregate Root)
│   ├── DelegatedAdministration.cs   (Aggregate Root)
│   ├── TeamDomainLink.cs            (Child Entity)
│   ├── GovernancePackVersion.cs     (Child Entity)
│   ├── GovernanceRolloutRecord.cs   (Child Entity)
│   └── GovernanceRuleBinding.cs     (Value Object / Record)
└── Enums/              → 44 enums (31 próprios + 13 para extração)

NexTraceOne.Governance.Application/
└── Features/           → CQRS handlers (Commands + Queries)

NexTraceOne.Governance.Infrastructure/
└── Persistence/
    ├── GovernanceDbContext.cs
    └── Configurations/  → 12 EF configs (8 próprias + 4 para extração)

NexTraceOne.Governance.API/
└── Endpoints/           → 19 endpoint modules
```

## Aggregate Roots

| Entidade | Responsabilidade |
|----------|-----------------|
| `Team` | Equipa organizacional com status lifecycle (Active/Inactive/Archived) |
| `GovernanceDomain` | Domínio de negócio com criticidade (Low/Medium/High/Critical) |
| `GovernancePack` | Pacote de regras com lifecycle (Draft→Published→Deprecated→Archived) |
| `GovernanceWaiver` | Exceção de conformidade com fluxo de aprovação (Pending→Approved/Rejected→Revoked) |
| `DelegatedAdministration` | Delegação de permissões administrativas com expiração |

## Regras de Negócio

### GovernancePack Lifecycle
- `Publish()` — Apenas packs em Draft podem ser publicados
- `Deprecate()` — Apenas packs Published podem ser depreciados
- `Archive()` — Apenas packs Deprecated podem ser arquivados
- Transições inválidas lançam `InvalidOperationException`

### GovernanceWaiver Lifecycle
- `Approve()` — Apenas waivers Pending podem ser aprovados
- `Reject()` — Apenas waivers Pending podem ser rejeitados
- `Revoke()` — Apenas waivers Approved podem ser revogados

## Base de Dados

### Tabelas (prefixo gov_)
| Tabela | Entidade |
|--------|---------|
| `gov_teams` | Team |
| `gov_domains` | GovernanceDomain |
| `gov_packs` | GovernancePack |
| `gov_pack_versions` | GovernancePackVersion |
| `gov_rollout_records` | GovernanceRolloutRecord |
| `gov_team_domain_links` | TeamDomainLink |
| `gov_waivers` | GovernanceWaiver |
| `gov_delegated_administrations` | DelegatedAdministration |
| `gov_outbox_messages` | OutboxMessage (infra) |

### Concorrência Otimista
PostgreSQL xmin via `RowVersion` em: Team, GovernanceDomain, GovernancePack, GovernanceWaiver.

### Check Constraints
- `CK_gov_teams_status`: Status IN ('Active', 'Inactive', 'Archived')
- `CK_gov_domains_criticality`: Criticality IN ('Low', 'Medium', 'High', 'Critical')
- `CK_gov_packs_status`: Status IN ('Draft', 'Published', 'Deprecated', 'Archived')
- `CK_gov_waivers_status`: Status IN ('Pending', 'Approved', 'Rejected', 'Revoked', 'Expired')

### Foreign Keys
- `GovernanceWaiver.PackId` → `GovernancePack.Id` (Restrict)

## Permissões

| Permissão | Escopo | Roles |
|-----------|--------|-------|
| `governance:teams:read` | Equipas | PlatformAdmin, TechLead, Developer, Auditor, SecurityReview |
| `governance:teams:write` | Equipas | PlatformAdmin, TechLead |
| `governance:domains:read` | Domínios | PlatformAdmin, TechLead, Developer, Viewer, Auditor, SecurityReview |
| `governance:domains:write` | Domínios | PlatformAdmin |
| `governance:packs:read` | Packs | PlatformAdmin, TechLead |
| `governance:packs:write` | Packs | PlatformAdmin |
| `governance:waivers:read` | Waivers | PlatformAdmin, TechLead |
| `governance:waivers:write` | Waivers | PlatformAdmin |
| `governance:policies:read` | Políticas | PlatformAdmin, TechLead, SecurityReview |
| `governance:controls:read` | Controlos | PlatformAdmin |
| `governance:compliance:read` | Conformidade | PlatformAdmin, TechLead, Auditor, SecurityReview |
| `governance:risk:read` | Risco | PlatformAdmin, SecurityReview |
| `governance:evidence:read` | Evidências | PlatformAdmin, Auditor, SecurityReview |
| `governance:reports:read` | Relatórios | PlatformAdmin, TechLead, Viewer, Auditor |
| `governance:finops:read` | FinOps | PlatformAdmin |
| `governance:admin:read` | Delegações (ler) | PlatformAdmin |
| `governance:admin:write` | Delegações (criar) | PlatformAdmin |

## Frontend

### Páginas Sidebar
| Página | Rota | Permissão |
|--------|------|-----------|
| Executive Overview | `/governance/executive` | governance:reports:read |
| Reports | `/governance/reports` | governance:reports:read |
| Compliance | `/governance/compliance` | governance:compliance:read |
| Risk Center | `/governance/risk` | governance:risk:read |
| FinOps | `/governance/finops` | governance:finops:read |
| Policies | `/governance/policies` | governance:policies:read |
| Packs | `/governance/packs` | governance:packs:read |
| Waivers | `/governance/waivers` | governance:waivers:read |
| Controls | `/governance/controls` | governance:controls:read |
| Evidence | `/governance/evidence` | governance:evidence:read |
| Teams | `/governance/teams` | governance:teams:read |
| Domains | `/governance/domains` | governance:domains:read |

## Módulos Consumidores

| Módulo | Relação com Governance |
|--------|----------------------|
| Change Governance | Consome políticas e packs para validar mudanças |
| Operational Intelligence | Consome métricas de conformidade |
| Audit & Compliance | Consome eventos de governança para trilha de auditoria |
| Configuration | Configurações podem ser escopadas por domínio |
