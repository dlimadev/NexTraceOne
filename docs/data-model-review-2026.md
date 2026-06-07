# Revisão de Modelo de Dados — NexTraceOne Platform
**Data:** 2026-06-07  
**Escopo:** 9 módulos, 12 bounded contexts, Building Blocks, camada analítica  
**Objetivo:** Gaps, erros, implementações incompletas, propostas de valor e decisão PostgreSQL vs ClickHouse

---

## Índice

1. [Executive Summary](#1-executive-summary)
2. [Módulo: IdentityAccess (IAM)](#2-módulo-identityaccess-iam)
3. [Módulo: ChangeGovernance](#3-módulo-changegovernance)
4. [Módulo: Catalog](#4-módulo-catalog)
5. [Módulo: Governance](#5-módulo-governance)
6. [Módulo: OperationalIntelligence](#6-módulo-operationalintelligence)
7. [Módulo: AIKnowledge](#7-módulo-aiknowledge)
8. [Módulo: Integrations](#8-módulo-integrations)
9. [Módulo: Notifications](#9-módulo-notifications)
10. [Módulo: Configuration](#10-módulo-configuration)
11. [Building Blocks — Primitivos de Domínio](#11-building-blocks--primitivos-de-domínio)
12. [Decisão PostgreSQL vs ClickHouse](#12-decisão-postgresql-vs-clickhouse)
13. [Remoção do Elasticsearch](#13-remoção-do-elasticsearch)
14. [Plano de Ação](#14-plano-de-ação)

---

## 1. Executive Summary

### Diagnóstico Geral

O modelo de dados do NexTraceOne está arquiteturalmente sólido: DDD com bounded contexts bem delimitados, tipagem forte, Result Pattern, Outbox, RLS multi-tenant e auditoria automática via interceptors. A base está correta.

Os problemas encontrados se distribuem em quatro categorias:

| Categoria | Severidade | Qtd Issues |
|-----------|-----------|-----------|
| Entidade no módulo errado | 🔴 Crítico | 2 |
| TenantId ausente (violação RLS) | 🔴 Crítico | 2 |
| Dado analítico no PostgreSQL (volume incorreto) | 🟠 Alto | 3 |
| Propriedades faltando que geram valor de produto | 🟡 Médio | 48 |
| Elasticsearch como padrão (NEST EOL, complexidade) | 🟠 Alto | 1 decisão |
| Inconsistência de base class (Entity vs AuditableEntity) | 🟡 Médio | 6 |

### Principais Riscos

1. **`AlertFiringRecord` no módulo `identityaccess`** — semanticamente pertence a `OperationalIntelligence`. Comentário no código admite a inconsistência ("iam_ owned by IdentityAccess por simplicidade").
2. **`IntegrationConnector` sem `TenantId`** — entidade multi-tenant sem isolamento RLS. Crítico.
3. **`AIUsageEntry` no PostgreSQL** — entidade imutável de auditoria de IA com volume potencial de milhões de registros/dia. Deve ir para ClickHouse.
4. **NEST 7.x (EOL desde Jan 2026)** — vulnerabilidade de segurança ativa. Único projeto usando NEST: `NexTraceOne.AIKnowledge.Infrastructure`.
5. **Default do provider analítico ainda é Elasticsearch** — `AnalyticsOptions.ConnectionString` default `http://elasticsearch:9200`; `Telemetry:ObservabilityProvider:Provider` default `"Elastic"`.

---

## 2. Módulo: IdentityAccess (IAM)

Prefixo de tabela: `iam_`

### 2.1 Tenant

**Base class:** `AggregateRoot<TenantId>` (correto — Tenant é o aggregate raiz do isolamento)

**Propriedades atuais:** `Name`, `Slug`, `IsActive`, `CreatedAt`, `UpdatedAt`, `ParentTenantId`, `TenantType`, `LegalName`, `TaxId`, `RowVersion`

#### Gaps Identificados

| Campo | Tipo | Severidade | Justificativa |
|-------|------|-----------|--------------|
| `Timezone` | `string?` | 🟡 Médio | Notificações em horário comercial do tenant; agendamento de relatórios |
| `DefaultLocale` | `string?` | 🟡 Médio | i18n padrão para e-mails gerados pelo sistema sem contexto de usuário |
| `CountryCode` | `string?` | 🟡 Médio | ISO 3166 — compliance (GDPR, LGPD), billing por região |
| `ContactEmail` | `string?` | 🟡 Médio | E-mail admin para alertas de licença e avisos de plataforma |
| `MaxUsers` | `int?` | 🟡 Médio | Enforcement de limite de usuários por plano |
| `LogoUrl` | `string?` | 🟢 Baixo | White-labeling por tenant (portal do dev, e-mails) |
| `SupportTier` | `enum` | 🟢 Baixo | Standard/Priority/Enterprise — roteamento de suporte |
| `MetadataJson` | `string?` | 🟢 Baixo | Extensibilidade sem alteração de schema para atributos customizados |
| `SubscriptionId` | `string?` | 🟡 Médio | Referência no sistema de billing externo (Stripe, etc.) |

**Proposta de valor:** `ContactEmail` + `Timezone` desbloqueia notificações de licença automáticas e relatórios agendados no fuso correto sem depender de usuário logado.

#### Inconsistência

`Tenant` não herda de `AuditableEntity<T>`, mantendo `CreatedAt` e `UpdatedAt` como campos manuais. Válido por ser provisioning especial, mas `UpdatedBy` está ausente — quem atualizou o nome do tenant não fica registrado.

**Proposta:** Adicionar `UpdatedBy` como campo manual (sem migrar para AuditableEntity, para não quebrar o fluxo de provisioning).

---

### 2.2 User

**Base class:** `AggregateRoot<UserId>` — sem `CreatedAt` automático (não herda AuditableEntity)

**Propriedades atuais:** `Email`, `FullName`, `PasswordHash`, `IsActive`, `LastLoginAt`, `FailedLoginAttempts`, `LockoutEnd`, `MfaEnabled`, `MfaMethod`, `MfaSecret`, `FederationProvider`, `ExternalId`, `RowVersion`

#### Gaps Identificados

| Campo | Tipo | Severidade | Justificativa |
|-------|------|-----------|--------------|
| `CreatedAt` | `DateTimeOffset` | 🔴 Crítico | Ausente! Não há registro de quando o usuário foi criado. Compliance. |
| `LastPasswordChangeAt` | `DateTimeOffset?` | 🟠 Alto | Compliance de rotação de senha; alertas de senha antiga |
| `MustChangePassword` | `bool` | 🟠 Alto | Primeiro login / reset forçado pelo admin |
| `PhoneNumber` | `string?` | 🟡 Médio | SMS MFA; valor de produto para enterprise |
| `Timezone` | `string?` | 🟡 Médio | Data/hora no fuso do usuário na UI |
| `Locale` | `string?` | 🟡 Médio | Preferência de idioma explícita (override do tenant default) |
| `Title` | `string?` | 🟢 Baixo | Cargo — exibido em perfil e relatórios de acesso |
| `Department` | `string?` | 🟢 Baixo | Departamento — segmentação de relatórios |
| `AvatarUrl` | `string?` | 🟢 Baixo | URL de avatar (ou hash Gravatar) |

**Proposta de valor:** `MustChangePassword` + `LastPasswordChangeAt` são requisitos de compliance (SOC 2, ISO 27001) que vários clientes enterprise vão exigir. `CreatedAt` é ausência crítica — corrigir imediatamente.

---

### 2.3 TenantLicense

**Base class:** `Entity<TenantLicenseId>` — **não herda `AuditableEntity`**

**Propriedades atuais:** `TenantId`, `Plan`, `Status`, `IncludedHostUnits`, `CurrentHostUnits`, `ValidFrom`, `ValidUntil`, `BillingCycleStart`, `CreatedAt`, `UpdatedAt`

#### Gaps Identificados

| Campo | Tipo | Severidade | Justificativa |
|-------|------|-----------|--------------|
| `UpdatedBy` | `string?` | 🟠 Alto | Quem fez upgrade/downgrade de plano (auditoria compliance) |
| `ExternalSubscriptionId` | `string?` | 🟠 Alto | ID no Stripe/billing; necessário para webhook reconciliation |
| `TrialStartedAt` | `DateTimeOffset?` | 🟡 Médio | Início explícito do trial (vs. ValidFrom que é genérico) |
| `MaxOverageHostUnits` | `int?` | 🟡 Médio | Cap de overage antes de bloquear; sem isso, overage ilimitado |
| `NotifyBeforeExpiryDays` | `int` | 🟡 Médio | Quantos dias antes de alertar sobre expiração |
| `PricePerHostUnit` | `decimal?` | 🟢 Baixo | Para geração de fatura estimada na UI |
| `Notes` | `string?` | 🟢 Baixo | Notas internas sobre a licença (negociação, discount) |

**Proposta de valor:** `ExternalSubscriptionId` + `MaxOverageHostUnits` são bloqueadores para billing automático. Sem eles, o time de CS precisa intervir manualmente em cada overage.

---

### 2.4 TenantMembership

**Base class:** `Entity<TenantMembershipId>`

**Propriedades atuais:** `UserId`, `TenantId`, `RoleId`, `JoinedAt`, `IsActive`, `RowVersion`

#### Gaps Identificados

| Campo | Tipo | Severidade | Justificativa |
|-------|------|-----------|--------------|
| `GrantedBy` | `string?` | 🟠 Alto | Quem concedeu o acesso — auditoria de RBAC |
| `ExpiresAt` | `DateTimeOffset?` | 🟠 Alto | Acesso temporário (contractors, JIT) sem precisar de JitAccessRequest |
| `InvitedAt` | `DateTimeOffset?` | 🟡 Médio | Quando o convite foi enviado (para reenvio, expiração de convite) |
| `Source` | `enum` | 🟡 Médio | Direct/OIDC/SAML/API — origin do vínculo para auditoria |
| `AcceptedAt` | `DateTimeOffset?` | 🟢 Baixo | Quando o convite foi aceito |

---

### 2.5 AgentRegistration

**Base class:** `Entity<AgentRegistrationId>` — sem `AuditableEntity`

**Propriedades atuais:** `TenantId`, `HostUnitId`, `HostName`, `AgentVersion`, `DeploymentMode`, `CpuCores`, `RamGb`, `HostUnits`, `Status`, `LastHeartbeatAt`, `RegisteredAt`

#### Gaps Identificados

| Campo | Tipo | Severidade | Justificativa |
|-------|------|-----------|--------------|
| `OsName` | `string?` | 🟡 Médio | Compatibilidade de agente por OS; suporte multi-plataforma |
| `OsVersion` | `string?` | 🟡 Médio | Relatório de versões de OS para fleet management |
| `Region` | `string?` | 🟡 Médio | Região geográfica — necessário para multi-region billing |
| `Tags` | `string?` | 🟡 Médio | Tags customizadas para filtrar fleet (ex: "production", "k8s") |
| `DeactivatedAt` | `DateTimeOffset?` | 🟢 Baixo | Quando foi desativado (para histórico de billing) |
| `IpAddress` | `string?` | 🟢 Baixo | Segurança — para auditoria de onde o agente está rodando |

---

### 2.6 🔴 AlertFiringRecord — Módulo ERRADO

**Situação:** `AlertFiringRecord` está no módulo `identityaccess`, tabela `iam_alert_firing_records`. O próprio comentário no código reconhece: *"Prefixo cfg_ — owned by IdentityAccess por simplicidade"*.

**Problema:** Esta entidade:
- É gerada pelo `AlertEvaluationJob` em Background Workers
- Representa alertas operacionais (LicenseUtilization, AgentHeartbeatMissed)
- Tem um ciclo de vida `Firing → Resolved` — puramente operacional
- Não tem relação semântica com Identity & Access Management

**Onde deveria estar:** Módulo `OperationalIntelligence` (prefixo `opi_`) — ou alternativamente um novo subdomínio `Alerting` dentro do módulo `governance`.

**Impacto do problema:** Viola o DDD — AlertFiringRecord não é uma invariante de IAM. Cria acoplamento implícito entre o Background Worker e o IAM DbContext.

---

## 3. Módulo: ChangeGovernance

Prefixo de tabela: `chg_`

### 3.1 Release

**Base class:** `AggregateRoot<ReleaseId>` — não herda `AuditableEntity`

**Propriedades atuais:** `ApiAssetId`, `ServiceName`, `Version`, `Environment`, `PipelineSource`, `CommitSha`, `ChangeLevel`, `Status`, `ChangeScore`, `WorkItemReference`, `RolledBackFromReleaseId`, `CreatedAt`, `ChangeType`, `ConfidenceStatus`, `ValidationStatus`, `TeamName`, `Domain`, `Description`, `SlsaProvenanceUri`, `ArtifactDigest`, `SbomUri`, `TenantId`, `EnvironmentId`, `ExternalReleaseId`, `ExternalSystem`, `ReleaseName`, `ApprovalStatus`, `HasBreakingChanges`, `ExternalValidationPassed`, `RowVersion`

A entidade está bem estruturada com suporte a SLSA Level 3. Gaps identificados:

| Campo | Tipo | Severidade | Justificativa |
|-------|------|-----------|--------------|
| `CreatedBy` | `string?` | 🟠 Alto | Quem registrou a release (usuário ou "system" para CI) |
| `UpdatedAt` | `DateTimeOffset?` | 🟠 Alto | Quando foi atualizada (Status changes, Score updates) |
| `DeploymentDurationMs` | `long?` | 🟡 Médio | Duração do deployment — KPI crítico para DORA metrics |
| `SucceededAt` | `DateTimeOffset?` | 🟡 Médio | Timestamp exato do sucesso (para DORA Lead Time) |
| `FailedAt` | `DateTimeOffset?` | 🟡 Médio | Timestamp exato da falha (para DORA MTTR) |
| `QualityGateScore` | `decimal?` | 🟡 Médio | Score do quality gate de CI (SonarQube, etc.) |
| `TestCoveragePercent` | `decimal?` | 🟡 Médio | Cobertura de testes no momento da release |
| `ChangelogUrl` | `string?` | 🟢 Baixo | Link para changelog no repositório |
| `TagNames` | `string?` | 🟢 Baixo | Tags da release (JSON array) para filtragem |

**Proposta de valor:** `DeploymentDurationMs` + `SucceededAt` + `FailedAt` são **bloqueadores para DORA metrics** (Lead Time for Changes, MTTR). Sem eles, os dashboards DORA precisam de estimativas imprecisas.

---

### 3.2 PromotionRequest

**Base class:** `AggregateRoot<PromotionRequestId>`

**Propriedades atuais:** `ReleaseId`, `SourceEnvironmentId`, `TargetEnvironmentId`, `RequestedBy`, `Status`, `Justification`, `RequestedAt`, `CompletedAt`, `RowVersion`

| Campo | Tipo | Severidade | Justificativa |
|-------|------|-----------|--------------|
| `ReviewedBy` | `string?` | 🟠 Alto | Quem aprovou/rejeitou — auditoria obrigatória |
| `ReviewNotes` | `string?` | 🟡 Médio | Notas de decisão (especialmente em rejeições) |
| `UpdatedAt` | `DateTimeOffset?` | 🟡 Médio | Rastreamento de quando o status mudou |
| `TenantId` | `Guid` | 🟠 Alto | Ausente! Sem TenantId explícito, RLS depende só do EnvironmentId |
| `PolicyVersion` | `string?` | 🟢 Baixo | Qual versão da política de promoção foi aplicada |

---

### 3.3 ChangeEvent

**Base class:** `AuditableEntity<ChangeEventId>` ✅ — correto

`OccurredAt` e `CreatedAt` coexistem com significados distintos (`OccurredAt` = timestamp no CI/CD externo, `CreatedAt` = quando foi registrado no NexTraceOne). Padrão válido e documentado.

**Gaps menores:**

| Campo | Tipo | Severidade | Justificativa |
|-------|------|-----------|--------------|
| `Metadata` | `string?` | 🟢 Baixo | JSON extensível para dados específicos por tipo de evento |
| `ExternalEventId` | `string?` | 🟢 Baixo | ID do evento no sistema externo para deduplicação |

---

### 3.4 RulesetBinding + Ruleset

Entidades de Rulesets existem mas precisam de revisão separada (não lidas neste ciclo). Flag para revisão futura.

---

## 4. Módulo: Catalog

Prefixo de tabela: `cat_` / `ctr_`

O módulo Catalog é o mais rico em entidades (24+ entidades documentadas pelo agente). Problemas identificados:

### 4.1 ContractVersion — Scorecard Faltando Histórico

`ContractHealthScore` e `ContractScorecard` calculam scores mas não mantêm histórico de tendência no PostgreSQL — o histórico vai para ClickHouse via `gov_compliance_trends`. Porém, a query "quais scores eu tinha há 3 meses?" depende de ClickHouse estar configurado.

**Gap:** Adicionar `PreviousOverallScore` e `ScoreChangedAt` em `ContractHealthScore` para comparação point-in-time sem precisar de ClickHouse.

### 4.2 ContractConsumerInventory — Alta Frequência

`ContractConsumerInventory` é atualizada por `ContractConsumerIngestionJob` a partir de OTel traces — potencialmente centenas de updates/hora por contrato popular.

**Problema:** Cada update recalcula no PostgreSQL com `UPDATE SET LastCalledAt, FrequencyPerDay`. Em alta escala, isso cria hot rows e lock contention.

**Proposta:** Separar em dois:
- PostgreSQL: `ContractConsumerInventory` como snapshot diário (1 update/dia)
- ClickHouse: `ctr_consumer_call_events` como log imutável de cada ingestão

### 4.3 DataContractSchema — PII sem Trilha

`PiiClassification` existe mas não há trilha de quem classificou e quando. Para compliance LGPD/GDPR, isso é obrigatório.

**Gap:** Adicionar `PiiClassifiedBy` (string?) e `PiiClassifiedAt` (DateTimeOffset?).

---

## 5. Módulo: Governance

Prefixo de tabela: `gov_`

### 5.1 CustomDashboard — DashboardUsageEvent no lugar errado

`DashboardUsageEvent` está em PostgreSQL (`gov_dashboard_usage_events`). É um evento de analytics de produto — append-only, alta frequência.

**Deve migrar para ClickHouse** como `nextraceone_analytics.gov_dashboard_usage` (já existe `IAnalyticsWriter.WriteProductEventAsync` que poderia absorver isso).

### 5.2 ServiceMaturityAssessment — Histórico Limitado

A entidade armazena `ReassessmentCount` e `LastReassessedAt` mas não mantém histórico de cada nível alcançado.

**Gap:** Criar `ServiceMaturityHistory` (tabela separada) com snapshot por reavaliação — para trending de maturidade ao longo do tempo.

### 5.3 PolicyAsCodeDefinition — Versioning Incompleto

`Version` existe mas não há rollback. Ao atualizar `DefinitionContent`, o conteúdo anterior se perde.

**Gap:** Criar `PolicyDefinitionVersion` com snapshots imutáveis, similar ao pattern `CanonicalEntityVersion` do Catalog.

---

## 6. Módulo: OperationalIntelligence

Prefixo de tabela: `opi_`

### 6.1 IncidentRecord

**Base class:** `AuditableEntity<IncidentRecordId>` ✅

**Propriedades atuais:** `ExternalRef`, `Title`, `Description`, `Type`, `Severity`, `Status`, `ServiceId`, `ServiceName`, `OwnerTeam`, `ImpactedDomain`, `Environment`, `DetectedAt`, `LastUpdatedAt`, `HasCorrelation`, `CorrelationConfidence`, `MitigationStatus`, `CorrelationAnalysis`, `EvidenceTelemetrySummary`, `EvidenceBusinessImpact`, `EvidenceAnalysis`

| Campo | Tipo | Severidade | Justificativa |
|-------|------|-----------|--------------|
| `ResolvedAt` | `DateTimeOffset?` | 🔴 Crítico | **Ausente!** Como calcular MTTR sem timestamp de resolução? |
| `AcknowledgedAt` | `DateTimeOffset?` | 🟠 Alto | MTTA (Mean Time To Acknowledge) — KPI importante |
| `AcknowledgedBy` | `string?` | 🟠 Alto | Quem reconheceu o incidente |
| `RootCause` | `string?` | 🟠 Alto | Causa raiz preenchida após PIR — hoje fica só na narrativa |
| `SlaBreached` | `bool` | 🟡 Médio | Flag de breach de SLA (calculado, mas útil persistido) |
| `EstimatedRevenueImpactUsd` | `decimal?` | 🟡 Médio | Para FinOps — impacto financeiro do incidente |
| `AffectedUsersCount` | `int?` | 🟡 Médio | Escala do impacto ao usuário final |
| `LinkedReleaseId` | `Guid?` | 🟡 Médio | Release correlacionada que causou o incidente |
| `Tags` | `string?` | 🟢 Baixo | Tags customizadas (JSON) para filtragem de dashboards |

**Proposta de valor:** `ResolvedAt` é crítico para MTTR. Sem ele, métricas DORA do OperationalIntelligence são estimativas. `AcknowledgedAt` adiciona MTTA.

---

### 6.2 🔴 AlertFiringRecord deveria estar aqui

Como discutido em 2.6, `AlertFiringRecord` pertence semanticamente ao domínio de `OperationalIntelligence`. A migração deve ser planejada.

---

## 7. Módulo: AIKnowledge

Prefixo de tabela: `aik_`

### 7.1 🟠 AIUsageEntry — PostgreSQL → ClickHouse

**Situação atual:** `AIUsageEntry` é imutável (`AuditableEntity<T>` sem métodos de update), append-only, com potencial de milhares de registros/hora em uso intenso.

**Propriedades:** `UserId`, `UserDisplayName`, `ModelId`, `ModelName`, `Provider`, `IsInternal`, `IsExternal`, `Timestamp`, `PromptTokens`, `CompletionTokens`, `TotalTokens`, `PolicyId`, `PolicyName`, `Result`, `ConversationId`, `ContextScope`, `ClientType`, `CorrelationId`

**Problema:** PostgreSQL não é adequado para logs de auditoria de alta frequência. Em produção com 100+ usuários de IA ativos, esperamos 10k-100k registros/dia. Isso cria:
- Tabela com crescimento linear sem particionamento nativo eficiente
- Queries lentas em janelas de tempo longas
- Pressure no WAL do PostgreSQL

**Decisão:** Mover para ClickHouse como `nextraceone_analytics.aik_ai_usage_log`.

**Gaps adicionais (para quando migrar):**

| Campo | Tipo | Justificativa |
|-------|------|--------------|
| `CostUsd` | `decimal?` | Custo estimado por interação (para AIBudget tracking) |
| `DurationMs` | `int?` | Latência da resposta do modelo |
| `SafetyFilterTriggered` | `bool` | Guardrail ativado — compliance |
| `ErrorCode` | `string?` | Código de erro quando Result != Allowed |
| `IsStreaming` | `bool` | Streaming vs batch — para analytics de UX |

---

### 7.2 ModelRoutingPolicy

**Base class:** `AuditableEntity<ModelRoutingPolicyId>` ✅

| Campo | Tipo | Severidade | Justificativa |
|-------|------|-----------|--------------|
| `Priority` | `int` | 🟡 Médio | Ordenação quando múltiplas políticas batem para a mesma intenção |
| `AllowedRolesJson` | `string?` | 🟡 Médio | RBAC no nível de roteamento de modelo |
| `ValidFrom` | `DateTimeOffset?` | 🟡 Médio | Política válida em janela de tempo (ex: só em horário comercial) |
| `ValidUntil` | `DateTimeOffset?` | 🟡 Médio | Idem |
| `RateLimitPerHour` | `int?` | 🟡 Médio | Throttling de uso por tenant/intenção |

---

### 7.3 AiAgentExecution — Candidato ClickHouse

`AiAgentExecution` é log de execução de agentes — append-only, alta frequência, natureza analítica. Candidato a ClickHouse no médio prazo, após `AIUsageEntry`.

---

## 8. Módulo: Integrations

Prefixo de tabela: `int_`

### 8.1 🔴 IntegrationConnector — TenantId Ausente

**Situação crítica:** `IntegrationConnector` herda de `Entity<IntegrationConnectorId>` e não tem campo `TenantId`.

```csharp
// Atual — sem TenantId
public sealed class IntegrationConnector : Entity<IntegrationConnectorId>
{
    public string Name { get; private init; } = string.Empty;
    public string ConnectorType { get; private set; } = string.Empty;
    // ...
    // AUSENTE: public Guid TenantId { get; private set; }
}
```

**Impacto:** 
- `TenantRlsInterceptor` não consegue filtrar connectors por tenant via RLS (não há coluna `app.current_tenant_id` aplicável)
- Sem `TenantId`, um connector poderia ser acessado cross-tenant via bypass de filtro de repositório
- `IntegrationConnector` é provavelmente global/platform-level, mas isso precisa ser documentado explicitamente

**Ação requerida:** Decidir se `IntegrationConnector` é:
1. **Platform-level** (global, sem tenant) → documentar explicitamente e adicionar flag `IsGlobal`, criar `TenantConnectorBinding` para vincular connectors a tenants
2. **Tenant-level** → adicionar `TenantId` e migrar dados existentes

**Gaps adicionais:**

| Campo | Tipo | Severidade | Justificativa |
|-------|------|-----------|--------------|
| `TenantId` / decisão de escopo | — | 🔴 Crítico | Ver acima |
| `CreatedBy` | `string?` | 🟠 Alto | Quem criou o conector |
| `EncryptedCredentials` | `[EncryptedField] string?` | 🟠 Alto | Credenciais do conector (hoje onde ficam?) |
| `LastConfiguredAt` | `DateTimeOffset?` | 🟡 Médio | Quando config foi atualizada (vs. criação) |
| `IsGlobal` | `bool` | 🟡 Médio | Platform-level vs tenant-level |

---

### 8.2 IngestionExecution — Candidato ClickHouse

`IngestionExecution` é log de execuções de conectores — append-only por definição. Em produção com conectores CI/CD ativos (GitHub, Jenkins), esperam-se centenas de execuções/hora.

O `IAnalyticsWriter.WriteIntegrationExecutionAsync` já existe e grava em ClickHouse! Porém, a entidade `IngestionExecution` ainda existe em PostgreSQL.

**Decisão:** `IngestionExecution` deve ser puramente ClickHouse. Manter em PostgreSQL apenas o status atual do conector (`IntegrationConnector.Health`, `LastSuccessAt`).

---

## 9. Módulo: Notifications

Prefixo de tabela: `ntf_`

### 9.1 Notification

**Base class:** `AggregateRoot<NotificationId>` ✅

**Propriedades atuais (parcial):** `TenantId`, `RecipientUserId`, `EventType`, `Category`, `Severity`, `Title`, `Message`, `SourceModule`, `SourceEntityType`, `SourceEntityId`, `EnvironmentId`, `ActionUrl`, `RequiresAction`, `PayloadJson`, `Status`, `CreatedAt`, `ExpiresAt`, `SourceEventId`

| Campo | Tipo | Severidade | Justificativa |
|-------|------|-----------|--------------|
| `ReadAt` | `DateTimeOffset?` | 🟡 Médio | Quando foi lida (vs. Status=Read que pode ser deduzido) |
| `DismissedAt` | `DateTimeOffset?` | 🟡 Médio | Quando foi descartada |
| `GroupKey` | `string?` | 🟡 Médio | Agrupamento de notificações similares (ex: "5 aprovações pendentes") |
| `Priority` | `int` | 🟡 Médio | Ordenação na central de notificações (0=baixo, 10=crítico) |
| `BatchId` | `Guid?` | 🟢 Baixo | Para rastrear notificações enviadas em batch (broadcast) |

---

## 10. Módulo: Configuration

Prefixo de tabela: `cfg_`

### 10.1 AutomationRule — Falta Auditoria de Execução

`AutomationRule` define regras de automação, mas não há entidade de `AutomationRuleExecution` no módulo Configuration — execuções são geradas em outro lugar.

**Gap:** Criar `AutomationRuleExecution` com: `RuleId`, `TriggerContext`, `Result`, `ExecutedAt`, `DurationMs`, `ErrorMessage`.

### 10.2 ConfigurationEntry — Sem Histórico de Valores

`ConfigurationEntry` armazena o valor atual de configurações, mas não há histórico de mudanças.

**Proposta:** `ConfigurationAuditEntry` já existe! Verificar se está sendo populada corretamente pelo AuditInterceptor.

### 10.3 FeatureFlagEntry vs FeatureFlagDefinition

Existe duplicação de conceitos:
- `FeatureFlagDefinition` (Configuration domain)  
- `FeatureFlagEntry` (Configuration domain)  
- `FeatureFlagRecord` / `IFeatureFlagRepository` (Catalog — tabela `ctr_feature_flag_records`)

**Problema:** Feature flags em dois módulos distintos com propósitos diferentes mas sobrepostos. Clarificar:
- Catalog `FeatureFlagRecord`: feature flags de serviços/APIs no catálogo
- Configuration `FeatureFlagDefinition`: configuração de features da plataforma

Isso **não é bug** se os propósitos são mesmo distintos, mas precisa de documentação clara para evitar uso incorreto.

---

## 11. Building Blocks — Primitivos de Domínio

### 11.1 AuditableEntity — Uso Inconsistente

Entidades que deveriam herdar `AuditableEntity<T>` mas herdam `Entity<T>`:

| Entidade | Módulo | Impacto |
|---------|--------|---------|
| `TenantLicense` | identityaccess | Sem `UpdatedBy` automático |
| `AgentRegistration` | identityaccess | Sem audit trail |
| `IntegrationConnector` | integrations | Sem audit trail |
| `TenantMembership` | identityaccess | Sem `UpdatedBy` |
| `AlertFiringRecord` | identityaccess | Sem audit trail |
| `ContractDeployment` | catalog | Imutável por design — OK |

**Regra de ouro:** Se a entidade pode ser modificada após criação, deve herdar `AuditableEntity`. Se é imutável (append-only log), `Entity` é aceitável mas deve ser documentado.

### 11.2 OutboxMessage — IdempotencyKey Sólido

O pattern de IdempotencyKey (`{EventType}:{SHA256}:{CreatedAt}`) está correto. Sem gaps.

### 11.3 TypedIdBase — TenantId em Dois Lugares

`TenantId` é declarado em `TenantMembership.cs` (módulo identityaccess). Mas `ChangeGovernance` usa `Guid TenantId` (não tipado) em `Release`. Inconsistência.

**Proposta:** `TenantId` como TypedId deve estar em `BuildingBlocks.Core.SharedKernel` e ser o mesmo tipo em todos os módulos. Hoje cada módulo declara o seu `Guid TenantId` — sem tipo forte.

---

## 12. Decisão PostgreSQL vs ClickHouse

### 12.1 Princípio de Decisão

```
PostgreSQL = estado operacional + domínio transacional + consistência ACID
ClickHouse = analytics + time-series + logs imutáveis + alta frequência append-only
```

| Critério | PostgreSQL | ClickHouse |
|---------|-----------|-----------|
| Dados de domínio (aggregates) | ✅ | ❌ |
| Consistência ACID | ✅ | ❌ |
| Queries de estado atual | ✅ | ❌ |
| Logs imutáveis de alta frequência | ❌ | ✅ |
| Time-series analytics | ❌ | ✅ |
| Agregações sobre bilhões de linhas | ❌ | ✅ |
| Joins complexos de domínio | ✅ | Limitado |
| Full-text search | Via `pgvector` / `pg_trgm` | Nativo |

### 12.2 Mapeamento Atual e Proposto

#### Tabelas que DEVEM permanecer no PostgreSQL ✅

| Tabela | Módulo | Justificativa |
|--------|--------|--------------|
| `iam_*` (todos) | identityaccess | Estado operacional de usuários/tenants |
| `chg_releases`, `chg_promotion_requests` | changegovernance | Estado mutable com ACID |
| `cat_*`, `ctr_*` | catalog | Contratos, versões, SBOM — domínio |
| `gov_*` (entities) | governance | Políticas, waivers, teams |
| `opi_incident_records` | operationalintelligence | Estado ativo de incidentes |
| `opi_mitigation_*` | operationalintelligence | Workflow ativo |
| `aik_model_routing_policies` | aiknowledge | Configuração, baixo volume |
| `ntf_notifications` | notifications | Estado de entrega |
| `cfg_*` | configuration | Configuração, baixo volume |
| `int_integration_connectors` | integrations | Estado atual do conector |
| outbox_messages | todos | Garantia de entrega |

#### Tabelas que DEVEM migrar para ClickHouse 🔴

| Tabela Atual (PostgreSQL) | Tabela Nova (ClickHouse) | Motivo |
|--------------------------|--------------------------|--------|
| `aik_ai_usage_entries` | `nextraceone_analytics.aik_ai_usage_log` | Imutável, alta frequência, log de auditoria |
| `gov_dashboard_usage_events` | `nextraceone_analytics.gov_dashboard_usage` | Analytics de produto |
| `int_ingestion_executions` | `nextraceone_analytics.int_execution_logs` | Log de execução (já existe no IAnalyticsWriter!) |

#### Tabelas que JÁ estão no ClickHouse ✅

Confirmado via `IAnalyticsWriter` e `ClickHouseAnalyticsWriter`:
- `nextraceone_analytics.pan_events` — product analytics events
- `nextraceone_analytics.ops_runtime_metrics` — métricas de runtime
- `nextraceone_analytics.ops_cost_entries` — custo por serviço
- `nextraceone_analytics.ops_incident_trends` — tendências de incidente
- `nextraceone_analytics.int_execution_logs` — logs de integração
- `nextraceone_analytics.int_health_history` — histórico de health de conector
- `nextraceone_analytics.gov_compliance_trends` — compliance ao longo do tempo
- `nextraceone_analytics.gov_finops_aggregates` — FinOps
- `nextraceone_analytics.chg_trace_release_mapping` — correlação trace↔release

---

## 13. Remoção do Elasticsearch

### 13.1 Escopo Atual

**153 arquivos** referenciam Elasticsearch. Principais touchpoints:

| Componente | Arquivo | Ação |
|-----------|---------|------|
| Analytics Writer | `ElasticAnalyticsWriter.cs` | Remover — ClickHouseAnalyticsWriter já implementado |
| Observability Provider | `ElasticObservabilityProvider.cs` | Remover — ClickHouseObservabilityProvider já implementado |
| Log Search Service | `ElasticsearchLogSearchService.cs` | Remover — ClickHouseLogSearchService já implementado |
| Background Job | `ElasticsearchIndexMaintenanceJob.cs` | Remover (sem equivalente ClickHouse necessário) |
| Index Manager | `IElasticsearchIndexManager.cs` + impl | Remover |
| AI Knowledge | `ElasticSearchAiRepository.cs` (usa NEST) | Substituir por PostgreSQL FTS ou ClickHouse FTS |
| Legacy Events | `ElasticLegacyEventWriter.cs` | Substituir por ClickHouse writer |
| Analytics Event Repo | `ElasticsearchAnalyticsEventRepository.cs` | Já tem ClickHouseAnalyticsEventRepository |
| Feature: GetElasticsearch | `GetElasticsearchManager.cs` | Remover feature inteira |
| Frontend | `ElasticsearchManagerPage.tsx` | Remover da UI |

### 13.2 🚨 NEST 7.x EOL — Vulnerabilidade Ativa

```xml
<!-- NexTraceOne.AIKnowledge.Infrastructure.csproj -->
<PackageReference Include="NEST" Version="7.17.5" />
<!-- SECURITY: NEST 7.x EOL desde Janeiro 2026 — vulnerabilidades sem correção -->
```

**NEST 7.x não receberá mais patches de segurança.** O único projeto que usa NEST diretamente é `NexTraceOne.AIKnowledge.Infrastructure` para full-text search em documentos de knowledge.

**Substituição:** `ElasticSearchAiRepository` deve ser reescrito usando `pg_trgm` ou `to_tsvector` do PostgreSQL (já disponível) para buscas de texto nos documentos do AIKnowledge.

### 13.3 Default Provider Errado

```csharp
// AnalyticsOptions.cs — ATUAL (incorreto)
public string ConnectionString { get; set; } = "http://elasticsearch:9200";

// TelemetryStoreOptions — ATUAL (incorreto)
// Provider default = "Elastic"
```

Ambos os defaults devem ser alterados para ClickHouse desde a primeira entrega.

### 13.4 Configuração Proposta Pós-Remoção

```json
{
  "Telemetry": {
    "ObservabilityProvider": {
      "Provider": "ClickHouse",
      "ClickHouse": {
        "Enabled": true,
        "ConnectionString": "Host=clickhouse;Port=8123;Database=nextraceone_obs",
        "LogsRetentionDays": 30,
        "TracesRetentionDays": 30,
        "MetricsRetentionDays": 90
      }
    }
  },
  "Analytics": {
    "Enabled": false,
    "ConnectionString": "http://clickhouse:8123",
    "WriteTimeoutSeconds": 10,
    "MaxBatchSize": 500,
    "SuppressWriteErrors": true
  }
}
```

---

## 14. Plano de Ação

### Priorização por Severidade e Valor

#### FASE 1 — Críticos (Sprint 1–2)

| # | Ação | Esforço | Impacto |
|---|------|---------|---------|
| 1.1 | Adicionar `ResolvedAt` + `AcknowledgedAt` + `AcknowledgedBy` em `IncidentRecord` | 2h | MTTR/MTTA desbloqueia KPIs DORA |
| 1.2 | Adicionar `CreatedAt` em `User` | 1h | Ausência crítica de compliance |
| 1.3 | Resolver `IntegrationConnector.TenantId` — decidir Global vs Tenant-scoped | 3h | Isolamento RLS |
| 1.4 | Trocar NEST 7.x por PostgreSQL FTS em `ElasticSearchAiRepository` | 6h | Vulnerabilidade de segurança ativa |
| 1.5 | Alterar default do provider de Elasticsearch para ClickHouse em `AnalyticsOptions` + `TelemetryStoreOptions` | 1h | Complexidade operacional desnecessária |
| 1.6 | Mover `AlertFiringRecord` para módulo `OperationalIntelligence` | 4h | DDD correto |

**Total Fase 1: ~17h**

---

#### FASE 2 — Alto Impacto (Sprint 3–4)

| # | Ação | Esforço | Impacto |
|---|------|---------|---------|
| 2.1 | Adicionar `MustChangePassword` + `LastPasswordChangeAt` em `User` | 2h | Compliance SOC 2 / ISO 27001 |
| 2.2 | Adicionar `ReviewedBy` + `TenantId` em `PromotionRequest` | 2h | Auditoria de promoção |
| 2.3 | Adicionar `DeploymentDurationMs` + `SucceededAt` + `FailedAt` em `Release` | 2h | DORA metrics (Lead Time) |
| 2.4 | Adicionar `ExternalSubscriptionId` + `MaxOverageHostUnits` em `TenantLicense` | 2h | Billing automation |
| 2.5 | Migrar `AIUsageEntry` de PostgreSQL para ClickHouse | 8h | Escalabilidade analítica AI |
| 2.6 | Adicionar `GrantedBy` + `ExpiresAt` em `TenantMembership` | 2h | JIT e compliance de acesso |
| 2.7 | Adicionar `ContactEmail` + `Timezone` em `Tenant` | 2h | Notificações automáticas |

**Total Fase 2: ~20h**

---

#### FASE 3 — Remoção Elasticsearch (Sprint 5–6)

| # | Ação | Esforço |
|---|------|---------|
| 3.1 | Remover `ElasticAnalyticsWriter` e atualizar DI para default ClickHouse | 2h |
| 3.2 | Remover `ElasticObservabilityProvider` e `ElasticServiceKindFilter` | 3h |
| 3.3 | Remover `ElasticsearchIndexMaintenanceJob` + `IElasticsearchIndexManager` | 2h |
| 3.4 | Remover `ElasticsearchLogSearchService` (ClickHouseLogSearchService já existe) | 1h |
| 3.5 | Remover `ElasticLegacyEventWriter` — criar `ClickHouseLegacyEventWriter` | 3h |
| 3.6 | Remover `ElasticsearchAnalyticsEventRepository` do Catalog | 1h |
| 3.7 | Remover feature `GetElasticsearchManager` (handler + API endpoint) | 1h |
| 3.8 | Remover `ElasticsearchManagerPage.tsx` do frontend | 1h |
| 3.9 | Remover `ElasticProviderOptions` e configurações de appsettings | 1h |
| 3.10 | Remover Elasticsearch de docker-compose e Kubernetes manifests | 2h |
| 3.11 | Atualizar CI/CD workflows (remover ES health checks) | 1h |
| 3.12 | Testes de regressão — validar todos os write paths em ClickHouse | 8h |

**Total Fase 3: ~26h**

---

#### FASE 4 — Melhorias de Modelo (Sprint 7–8)

| # | Ação | Esforço |
|---|------|---------|
| 4.1 | Migrar `DashboardUsageEvent` de PostgreSQL para ClickHouse | 4h |
| 4.2 | Migrar `IngestionExecution` de PostgreSQL para ClickHouse | 4h |
| 4.3 | Criar `ServiceMaturityHistory` para trending de maturidade | 3h |
| 4.4 | Criar `PolicyDefinitionVersion` para versionamento de PaC | 4h |
| 4.5 | Adicionar `Priority` + `GroupKey` em `Notification` | 2h |
| 4.6 | Resolver duplicação `FeatureFlagDefinition` vs `FeatureFlagRecord` | 3h |
| 4.7 | Adicionar `RootCause` + `SlaBreached` em `IncidentRecord` | 2h |
| 4.8 | Adicionar campos de AI budget (`CostUsd`, `DurationMs`) na estrutura ClickHouse | 3h |
| 4.9 | Padronizar `TenantId` como `TypedIdBase` compartilhado em BuildingBlocks | 6h |

**Total Fase 4: ~31h**

---

### Resumo Executivo do Plano

| Fase | Foco | Esforço | Risco |
|------|------|---------|-------|
| 1 — Críticos | Segurança, compliance, RLS | ~17h | 🔴 Alto se não feito |
| 2 — Alto Impacto | KPIs, billing, DORA | ~20h | 🟠 Médio |
| 3 — Remove Elasticsearch | Simplificação operacional | ~26h | 🟡 Médio (abstrações existem) |
| 4 — Melhorias | Escalabilidade, DX | ~31h | 🟢 Baixo |
| **Total** | | **~94h** | |

### Critérios de Verificação por Fase

**Fase 1 concluída quando:**
- `IncidentRecord` tem `ResolvedAt` com migration aplicada
- `User.CreatedAt` existe com default para registros existentes
- `IntegrationConnector` tem decisão de escopo documentada e aplicada
- NEST removido e `ElasticSearchAiRepository` funciona com PostgreSQL FTS
- Provider default é ClickHouse em todos os appsettings
- `AlertFiringRecord` com migration para `opi_alert_firing_records`

**Fase 3 concluída quando:**
- `docker-compose.yml` não tem serviço Elasticsearch
- Zero referências a NEST em qualquer `.csproj`
- `dotnet build` sem warnings de Elasticsearch
- Testes de integração passam com provider ClickHouse

---

*Relatório gerado em 2026-06-07. Próxima revisão recomendada: após conclusão da Fase 3.*

---

## Apêndice A — Correções e Complementos (pós análise completa)

### A.1 Notification — Campos já implementados

A análise inicial subestimou a entidade `Notification`. Os campos abaixo **já existem**:

| Campo | Status |
|-------|--------|
| `ReadAt` | ✅ Já existe |
| `AcknowledgedAt` + `AcknowledgedBy` | ✅ Já existe |
| `GroupId` + `CorrelationKey` | ✅ Já existe |
| `OccurrenceCount` + `LastOccurrenceAt` | ✅ Já existe |
| `SnoozedUntil` + `SnoozedBy` | ✅ Já existe |
| `IsSuppressed` + `SuppressionReason` | ✅ Já existe |
| `IsEscalated` + `EscalatedAt` | ✅ Já existe |

Seção 9.1 mantida apenas como referência histórica. **Não há gaps críticos em Notification**.

---

### A.2 IAM — UserRoleAssignment Já Endereça Multi-Role

`UserRoleAssignment` já tem `ValidFrom`/`ValidUntil` e `AssignedBy` — aborda os gaps de `TenantMembership` para casos de multi-role. `TenantMembership` é o modelo legado (single role). A estratégia correta é:

- Migrar novos assignments para `UserRoleAssignment`
- Manter `TenantMembership` como fallback de backward compatibility

**Portanto:** Os gaps de `ExpiresAt` e `GrantedBy` no `TenantMembership` são adrressados via `UserRoleAssignment`. Sem ação urgente.

---

### A.3 Novos Candidatos ClickHouse (AIKnowledge)

Além de `AIUsageEntry`, identificados dois candidatos adicionais a migração para ClickHouse:

| Entidade | Tabela atual | Motivo |
|---------|-------------|--------|
| `AiTokenUsageLedger` | `aik_ai_token_usage_ledger` | Imutável por design (`Record()` factory único), alta frequência, FinOps analytics |
| `AIRoutingDecision` | `aik_ai_routing_decisions` | Log de decisões de roteamento — append-only, analytics |
| `AiAgentExecution` | `aik_ai_agent_executions` | Execuções de agentes — log de performance, append-only |

Todos estes deveriam integrar o `IAnalyticsWriter` com novos métodos ou ir para `nextraceone_analytics` no ClickHouse.

---

### A.4 StorageBucket.BackendType — Remover Elasticsearch

`StorageBucket` em `integrations` tem enum `StorageBucketBackendType` que inclui `Elasticsearch`. Após remoção do Elasticsearch da plataforma, este enum deve ser atualizado:

```csharp
// Atual
public enum StorageBucketBackendType
{
    Elasticsearch,  // ← REMOVER na Fase 3
    ClickHouse,
    // ...
}
```

Ação: remover `Elasticsearch` do enum e migrar dados existentes de `StorageBucket` com `BackendType = Elasticsearch` para `ClickHouse`.

---

### A.5 Knowledge Module — Localização Correta

O módulo Knowledge **não é um módulo separado** — está dentro do Catalog. Tabelas com prefixo `knw_`:
- `knw_documents` — KnowledgeDocument
- `knw_relations` — KnowledgeRelation
- `knw_operational_notes` — OperationalNote
- `knw_proposed_runbooks` — ProposedRunbook
- `knw_graph_snapshots` — KnowledgeGraphSnapshot

Isso está correto arquiteturalmente (conhecimento operacional é parte do catálogo de serviços).

---

### A.6 IdentityAccess — Scope de Acesso via Ambiente

`EnvironmentAccess` já tem `ExpiresAt`, `GrantedBy` e `ChangeAccessLevel` — modelo de acesso temporário a ambiente está completo. `EnvironmentPolicy`, `EnvironmentTelemetryPolicy` e `EnvironmentIntegrationBinding` cobrem os casos de políticas por ambiente.

---

### A.7 ChangeGovernance — Entidades Não Lidas na Análise Inicial

O ChangeGovernance tem mais entidades do que cobertas na análise inicial. Entidades identificadas mas não analisadas em detalhe:

- `CanaryRollout` — rollout gradual
- `ObservationWindow` — janela de observação pós-release
- `PostReleaseReview` — revisão pós-release
- `ReleaseCalendarEntry` — calendário de releases
- `WorkItemAssociation` — link para work items externos
- `BlastRadiusReport` — análise detalhada de blast radius
- `ChangeIntelligenceScore` — score de risco calculado

Estas entidades parecem completas mas requerem revisão específica em ciclo futuro.

---

### A.8 OperationalIntelligence — Riqueza de Subdomínios

O módulo OI tem 5 subdomínios bem estruturados: Automation, Cost/FinOps, Incidents, Reliability e Runtime. O modelo é rico e bem dividido. Destaque:

- `CarbonScoreRecord` — rastreamento de pegada de carbono (GreenOps)
- `ServiceFailurePrediction` — predição de falhas (ML)
- `IncidentPredictionPattern` — padrões detectados por ML
- `ChaosExperiment` + `ResilienceReport` — chaos engineering integrado
- `CapacityForecast` — previsão de capacidade

**Gap confirmado:** `IncidentRecord.ResolvedAt` está ausente conforme identificado na seção 6.1. Crítico para MTTR.

---

### A.9 Configuration — Completude

A análise completa mostra que `ConfigurationAuditEntry` existe e é populado — sem gap de auditoria de configuração. `AutomationRule` tem validação de triggers válidos. `ContractCompliancePolicy` tem cobertura completa de ações de compliance.

`UserAlertRule` tem `IsEnabled` mas não tem `LastTriggeredAt` — gap menor para debugging de regras silenciosas.
