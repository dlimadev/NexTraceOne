# Integrations — Governance vs Integrations Boundary Deep Dive

> **Module:** Integrations (12) vs Governance (08)  
> **Date:** 2026-03-25  
> **Status:** Frontier finalized

---

## 1. Contexto do problema

O módulo Integrations tem o seu backend **fisicamente dentro do módulo Governance** (`src/modules/governance/`). Isto cria:

- Coupling arquitectural entre dois bounded contexts distintos
- Impossibilidade de criar `IntegrationsDbContext` próprio
- Prefixo `gov_` nas tabelas em vez de `int_`
- GovernanceDbContext sobrecarregado com entidades que não são governance
- Confusão conceptual entre governança de integrações e operação de integrações

---

## 2. Responsabilidades actuais de Governance relacionadas com integrações

| Componente | Localização | Tipo |
|-----------|-------------|------|
| `IntegrationConnector` (entity) | `Governance.Domain/Entities/` | Operacional |
| `IngestionSource` (entity) | `Governance.Domain/Entities/` | Operacional |
| `IngestionExecution` (entity) | `Governance.Domain/Entities/` | Operacional |
| 6 enums (ConnectorStatus, etc.) | `Governance.Domain/Enums/` | Operacional |
| `IntegrationHubEndpointModule` | `Governance.API/Endpoints/` | Operacional |
| 7 CQRS features (List/Get/Retry) | `Governance.Application/Features/` | Operacional |
| 3 entity configurations | `Governance.Infrastructure/Persistence/Configurations/` | Operacional |
| 3 repository interfaces | `Governance.Application/Abstractions/` | Operacional |
| 3 repository implementations | `Governance.Infrastructure/Repositories/` | Operacional |
| 3 DbSets in GovernanceDbContext | `Governance.Infrastructure/Persistence/` | Operacional |
| 3 migrations | `Governance.Infrastructure/Persistence/Migrations/` | Operacional |

**Conclusão:** Todas as responsabilidades de integração actualmente em Governance são **operacionais**, não de governança.

---

## 3. Responsabilidades actuais de Integrations

| Componente | Localização | Tipo |
|-----------|-------------|------|
| `IntegrationHubPage.tsx` | `frontend/features/integrations/pages/` | Frontend |
| `ConnectorDetailPage.tsx` | `frontend/features/integrations/pages/` | Frontend |
| `IngestionExecutionsPage.tsx` | `frontend/features/integrations/pages/` | Frontend |
| `IngestionFreshnessPage.tsx` | `frontend/features/integrations/pages/` | Frontend |
| `integrations.ts` (API client) | `frontend/features/integrations/api/` | Frontend |
| 60+ i18n keys | `frontend/locales/en.json`, `pt.json` | Frontend |

**Conclusão:** O frontend já trata Integrations como módulo independente.

---

## 4. O que é policy/compliance/governance de integração (fica em Governance)

| Responsabilidade | Descrição | Exemplo |
|-----------------|-----------|---------|
| **Políticas de integração permitida** | Regras que definem quais integrações são autorizadas | "Apenas GitHub e Datadog são aprovados para produção" |
| **Compliance de integrações** | Verificação de conformidade do uso de integrações | "Todos os conectores devem ter autenticação OAuth" |
| **Risk assessment** | Avaliação de risco de dependências externas | "PagerDuty tem SLA 99.9%, risco baixo" |
| **Executive reporting** | Dashboards executivos com métricas de integração | "Relatório mensal de saúde de integrações" |
| **Governance packs** | Packs de controlos que incluem integrações | "Pack de segurança inclui verificação de auth mode" |
| **Aprovação de novas integrações** | Workflow de aprovação antes de activar conector | "Novo conector requer aprovação de Tech Lead" |

**Mecanismo de acesso:** Governance consome dados de Integrations via **eventos ou read models**, não acede directamente às entidades.

---

## 5. O que é configuração/execução/monitorização operacional (fica em Integrations)

| Responsabilidade | Descrição | Entidade/Feature |
|-----------------|-----------|-----------------|
| **CRUD de conectores** | Criar, editar, activar, desactivar, eliminar conectores | `IntegrationConnector` |
| **Configuração de conectores** | Endpoint, auth mode, polling mode, teams | `IntegrationConnector.UpdateConfiguration()` |
| **Gestão de fontes de dados** | CRUD de ingestion sources por conector | `IngestionSource` |
| **Execução de ingestão** | Orquestrar e registar execuções de ingestão | `IngestionExecution` |
| **Health monitoring** | Monitorizar saúde de conectores em tempo real | `ConnectorHealth` enum + endpoints |
| **Freshness tracking** | Monitorizar frescura de dados ingeridos | `FreshnessStatus` + `UpdateFreshnessStatus()` |
| **Retry de conectores falhados** | Re-tentar conectores em estado Failed | `RetryConnector` feature |
| **Reprocessamento de execuções** | Reprocessar execuções falhadas | `ReprocessExecution` feature |
| **Trust level management** | Promover nível de confiança de fontes | `IngestionSource.PromoteTrustLevel()` |
| **Métricas operacionais** | TotalExecutions, SuccessfulExecutions, FailedExecutions | `IntegrationConnector` properties |
| **Webhook handling** | Recepção e processamento de webhooks | Futuro — endpoint dedicado |
| **Credential management** | Gestão segura de credenciais de conectores | Futuro — com encriptação |

---

## 6. Resumo da fronteira

```
┌─────────────────────────────────────────────────────────────────┐
│                        GOVERNANCE (08)                           │
│                                                                   │
│  ┌──────────────────────────────────────────────────────────┐    │
│  │  • Integration policies (which connectors are allowed)   │    │
│  │  • Compliance checks (auth mode, encryption)             │    │
│  │  • Risk assessment of integration dependencies           │    │
│  │  • Executive views and reports                           │    │
│  │  • Governance packs with integration controls            │    │
│  │  • Approval workflows for new integrations               │    │
│  └──────────────────────────────────────────────────────────┘    │
│                                                                   │
│  Consome dados de Integrations via: EVENTOS / READ MODELS        │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│                       INTEGRATIONS (12)                          │
│                                                                   │
│  ┌──────────────────────────────────────────────────────────┐    │
│  │  • IntegrationConnector CRUD + configuração              │    │
│  │  • IngestionSource CRUD + trust levels                   │    │
│  │  • IngestionExecution tracking                           │    │
│  │  • Health monitoring + freshness tracking                │    │
│  │  • Retry + reprocessamento                               │    │
│  │  • Webhooks + credential management                      │    │
│  │  • Métricas operacionais (executions, success, failure)  │    │
│  └──────────────────────────────────────────────────────────┘    │
│                                                                   │
│  Publica eventos para: Governance, Notifications, Audit          │
└─────────────────────────────────────────────────────────────────┘
```

---

## 7. O que deve ser extraído de Governance para Integrations

| Componente actual em Governance | Target em Integrations |
|-------------------------------|----------------------|
| `Governance.Domain/Entities/IntegrationConnector.cs` | `Integrations.Domain/Entities/IntegrationConnector.cs` |
| `Governance.Domain/Entities/IngestionSource.cs` | `Integrations.Domain/Entities/IngestionSource.cs` |
| `Governance.Domain/Entities/IngestionExecution.cs` | `Integrations.Domain/Entities/IngestionExecution.cs` |
| `Governance.Domain/Enums/ConnectorStatus.cs` | `Integrations.Domain/Enums/ConnectorStatus.cs` |
| `Governance.Domain/Enums/ConnectorHealth.cs` | `Integrations.Domain/Enums/ConnectorHealth.cs` |
| `Governance.Domain/Enums/SourceStatus.cs` | `Integrations.Domain/Enums/SourceStatus.cs` |
| `Governance.Domain/Enums/SourceTrustLevel.cs` | `Integrations.Domain/Enums/SourceTrustLevel.cs` |
| `Governance.Domain/Enums/FreshnessStatus.cs` | `Integrations.Domain/Enums/FreshnessStatus.cs` |
| `Governance.Domain/Enums/ExecutionResult.cs` | `Integrations.Domain/Enums/ExecutionResult.cs` |
| `Governance.API/Endpoints/IntegrationHubEndpointModule.cs` | `Integrations.API/Endpoints/IntegrationHubEndpointModule.cs` |
| 7 CQRS features (List/Get/Retry/Reprocess) | `Integrations.Application/Features/` |
| 3 entity configurations | `Integrations.Infrastructure/Persistence/Configurations/` |
| 3 repository interfaces | `Integrations.Application/Abstractions/` |
| 3 repository implementations | `Integrations.Infrastructure/Repositories/` |
| 3 DbSets | `IntegrationsDbContext` |

**Total:** ~30 ficheiros a mover/recriar.

---

## 8. O que fica em Governance

| Componente | Razão |
|-----------|-------|
| `GovernanceDomain` entity | Gestão de domínios de governance |
| `GovernancePack` entity | Packs de controlos (podem incluir regras de integração) |
| `ComplianceCheck` entity | Verificações de compliance (podem verificar integrações) |
| Políticas de integração | Regras de quais integrações são permitidas |
| Reports/Views | Dashboards que agregam dados de integração via eventos |

---

## 9. Exemplos concretos da separação

### Exemplo 1: Criar um novo conector GitHub
- **Integrations:** Recebe POST, cria `IntegrationConnector` com provider="GitHub", configura endpoint, auth mode → publica `ConnectorCreatedEvent`
- **Governance:** Consome `ConnectorCreatedEvent`, verifica se GitHub é provider aprovado, registra compliance check

### Exemplo 2: Conector falha
- **Integrations:** Marca conector como Failed, regista erro, incrementa FailedExecutions → publica `ConnectorFailedEvent`
- **Notifications:** Consome `ConnectorFailedEvent`, envia alerta ao team
- **Governance:** Consome `ConnectorFailedEvent`, avalia impacto no risk score

### Exemplo 3: Health check periódico
- **Integrations:** Avalia health de conectores, calcula freshness, actualiza status → publica `ConnectorHealthChangedEvent`
- **Operational Intelligence:** Consome métricas de health para dashboards operacionais
- **Governance:** Consome health data para compliance reporting

---

## 10. Impacto da extracção

| Dimensão | Impacto |
|----------|---------|
| GovernanceDbContext | Remove 3 DbSets, 3 configurations — fica mais leve |
| Governance migrations | Migrations existentes ficam orphaned — precisam ser recriadas |
| IntegrationsDbContext | Novo DbContext com prefixo `int_` |
| Frontend | Nenhuma mudança — já é independente |
| API routes | Mantêm-se — `/api/v1/integrations/` e `/api/v1/ingestion/` |
| Permissões | Mantêm-se — `integrations:read` e `integrations:write` |
