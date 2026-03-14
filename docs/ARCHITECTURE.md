# NexTraceOne — Arquitetura Definitiva v3 (Archon Pattern — Consolidado)

> **Versão:** 3.1 — Consolidação completa: backend (7 BC × 3 layers) + frontend organizado por bounded context.
> **Última atualização:** Março 2026

---

## 1. Visão Geral

NexTraceOne segue o **Archon Pattern** — um Modular Monolith consolidado em **7 bounded contexts**
coesos, cada um pronto para extração futura como serviço independente sem refactoring de boundaries.

A consolidação de 14 módulos em 7 contextos eliminou fragmentação excessiva, reduziu comunicação
cross-módulo, e agrupou responsabilidades que compartilham invariantes de domínio sob o mesmo
Aggregate Root ou transação. A separação interna por namespaces (subcontextos) preserva as costuras
para extração futura caso a escala exija.

### Princípios Arquiteturais

- **Modular Monolith:** Tudo roda em um único processo hoje; amanhã cada contexto pode ser extraído
  como serviço independente — sem refactoring de boundaries.
- **Vertical Slice Architecture (VSA):** Features completas em arquivo único
  (Command/Query + Validator + Handler + Response).
- **DDD Tático:** Aggregates, Value Objects, Domain Events, Domain Services — aplicados onde há
  complexidade real de negócio.
- **CQRS:** Commands modificam estado, Queries leem. Separação explícita via MediatR.
- **REPR Pattern:** Request → Endpoint → Presenter → Response nas Minimal APIs.
- **Outbox Pattern:** Integration Events persistidos na mesma transação do comando,
  entregues de forma assíncrona pelo BackgroundWorkers.
- **Hexagonal Ports:** Interfaces de domínio (Ports) definem contratos para adaptadores
  de infraestrutura — inversão de dependência pura.

### Números da Plataforma

| Métrica                    | Valor  |
|----------------------------|--------|
| Bounded Contexts (módulos) | 7      |
| Building Blocks            | 6      |
| Platform Services          | 3      |
| DbContexts                 | 14     |
| Integration Events         | 18     |
| Hexagonal Ports            | 6      |
| Tabelas PostgreSQL         | 50+    |
| Projetos `.csproj`         | 32+    |

---

## 2. Building Blocks (Infraestrutura Transversal)

Cada building block é um projeto independente com responsabilidade única. Módulos referenciam
**apenas** os building blocks que precisam — sem dependência transitiva desnecessária.

### 2.1 BuildingBlocks.Domain

Primitivos DDD puros, sem qualquer dependência de infraestrutura.
Referências: `MediatR`, `Ardalis.GuardClauses`.

```
Primitives/
  Entity<TId>             — Igualdade por identidade, coleção de domain events
  AggregateRoot<TId>      — Emite Domain Events, mantém invariantes do aggregate
  ValueObject             — Igualdade estrutural por valor, imutável
  AuditableEntity<TId>    — CreatedAt/By, UpdatedAt/By, SoftDelete automáticos

StronglyTypedIds/
  ITypedId                — Marcador para IDs fortemente tipados
  TypedIdBase             — Record base: ReleaseId(Guid Value) : TypedIdBase(Value)

Events/
  IDomainEvent            — Intra-módulo, via MediatR INotification
  IIntegrationEvent       — Cross-módulo, via Outbox Pattern
  DomainEventBase         — Record abstrato com EventId + OccurredAt
  IntegrationEventBase    — Record abstrato base para Integration Events
                            Propriedades: EventId, OccurredAt, SourceModule
                            REGRA: Todos os Integration Events DEVEM herdar desta classe.

Results/
  Result<T>               — Sucesso ou falha sem exceção (pattern matching seguro)
  Error                   — Código i18n, mensagem técnica e ErrorType
  ErrorType               — NotFound | Validation | Conflict | Unauthorized
                            | Forbidden | Security | Business

Guards/
  NexTraceGuards          — Extensões Ardalis (SemVer, TenantId, Environment)

Enums/
  ChangeLevel             — Taxonomia de mudanças (Operational → Breaking → Publication)
  DiscoveryConfidence     — Modelo de confiança (Inferred → Confirmed)
```

### 2.2 BuildingBlocks.Application

CQRS, behaviors do pipeline MediatR e abstrações de serviço.
Referências: `BB.Domain`, `MediatR`, `FluentValidation`,
`Microsoft.Extensions.Localization`, `Microsoft.Extensions.Logging.Abstractions`.

```
Cqrs/
  ICommand / ICommand<T>      — Marcadores de command (modificam estado)
  ICommandHandler<T>          — Handler de command
  IQuery<T>                   — Marcador de query (somente leitura)
  IQueryHandler<TQ, TR>       — Handler de query
  IPagedQuery                 — Contrato de paginação padronizado

Behaviors/ (Pipeline MediatR — executam em ordem)
  1. ValidationBehavior       — FluentValidation antes do handler
  2. LoggingBehavior          — Log estruturado: entrada, saída, duração
  3. PerformanceBehavior      — Alerta >500ms (warn), >2000ms (error)
  4. TenantIsolationBehavior  — Garante tenant ativo (exceto IPublicRequest)
  5. TransactionBehavior      — Commit/rollback automático para Commands

Abstractions/
  ICurrentUser                — Usuário autenticado do HttpContext
  ICurrentTenant              — Tenant ativo (JWT → Header → Subdomínio)
  IUnitOfWork                 — CommitAsync() coordenado por DbContext
  IDateTimeProvider           — Abstração de DateTimeOffset.UtcNow (testável)
  IEventBus                   — Publicação de Integration Events via Outbox
  ILicenseCapabilityChecker   — Verificação de capacidades da licença ativa

Pagination/
  PagedList<T>                — Container padronizado para listagens paginadas

Localization/
  SharedMessages.resx         — Mensagens i18n compartilhadas (en)
  SharedMessages.pt-BR.resx   — Mensagens i18n compartilhadas (pt-BR)

Extensions/
  ResultExtensions            — ToHttpResult() converte Result<T> em IResult HTTP
```

### 2.3 BuildingBlocks.Infrastructure

Persistência, interceptors, outbox e converters para EF Core + PostgreSQL.
Referências: `BB.Application`, `BB.Domain`, `BB.Security`,
`Microsoft.EntityFrameworkCore`, `Npgsql.EntityFrameworkCore.PostgreSQL`, `Mapster`.

```
Persistence/
  NexTraceDbContextBase       — Base com RLS, interceptors, SaveChangesAsync orquestrado
                                Publica Domain Events → Outbox automaticamente
  RepositoryBase<T,TId>       — CRUD genérico + Specification pattern

Outbox/
  OutboxMessage               — Entidade do padrão Outbox (serialização JSON)

Interceptors/
  TenantRlsInterceptor       — SET app.current_tenant_id no PostgreSQL (RLS)
  AuditInterceptor            — Preenche CreatedAt/By, UpdatedAt/By via reflection segura

Converters/
  EncryptedStringConverter    — AES-256-GCM via EF Core Value Converter
```

### 2.4 BuildingBlocks.EventBus

Barramento de eventos com dispatch in-process (monolith) e persistência via Outbox.
Referências: `BB.Application`, `BB.Domain`, `MediatR`,
`Microsoft.Extensions.Logging.Abstractions`.

```
Abstractions/
  IIntegrationEventHandler<T>    — Handler para eventos de outros módulos

InProcess/
  InProcessEventBus              — Dispatch via MediatR (monolith)
                                   Futuro: substituível por RabbitMQ/Kafka

Outbox/
  OutboxEventBus                 — Persiste no Outbox na mesma transação do command
                                   BackgroundWorkers processa e despacha
```

### 2.5 BuildingBlocks.Observability

Logging estruturado (Serilog), tracing distribuído (OpenTelemetry), métricas e health checks.
Referências: `BB.Domain`, `OpenTelemetry.*`, `Serilog.*`.

```
Logging/
  SerilogConfiguration           — Enrichers, sinks (Console, File), destructuring

Tracing/
  NexTraceActivitySources        — ActivitySources: Commands, Queries, Events, ExternalHttp

Metrics/
  NexTraceMeters                 — Métricas: DeploymentsNotified, WorkflowsInitiated,
                                   BlastRadiusDuration, etc.

HealthChecks/
  NexTraceHealthChecks           — PostgreSQL, Outbox backlog, License, Integrity
```

### 2.6 BuildingBlocks.Security

Autenticação dual (JWT + API Key), autorização granular por permissões, criptografia,
integridade de assemblies e multi-tenancy.
Referências: `BB.Application`, `BB.Domain`,
`Microsoft.AspNetCore.Authentication.JwtBearer`.

```
Authentication/
  JwtTokenService                — Access token (short-lived) + Refresh token (in-memory)
  ApiKeyAuthentication           — Autenticação por API Key para integrações M2M
  PolicyScheme "Smart"           — Auto-seleciona JWT ou API Key conforme o request

Authorization/
  PermissionPolicyProvider       — Políticas dinâmicas por permissão granular
  PermissionAuthorizationHandler — Avalia permissões do usuário autenticado
  EndpointAuthorizationExtensions — .RequirePermission() para Minimal APIs

Encryption/
  AesGcmEncryptor                — AES-256-GCM para campos sensíveis (PII, tokens)

Integrity/
  AssemblyIntegrityChecker       — SHA-256 de assemblies no boot (on-premise hardening)

Licensing/
  HardwareFingerprint            — SHA-256(CPU|Motherboard|MAC) para binding de licença

MultiTenancy/
  TenantResolutionMiddleware     — Resolução: JWT claim → Header → Subdomínio
```

---

## 3. Bounded Contexts (7 Módulos Consolidados)

A consolidação de 14 módulos granulares em 7 bounded contexts segue o princípio de
**coesão por domínio**: responsabilidades que compartilham invariantes, transações ou
ciclo de vida ficam no mesmo módulo. A separação interna por subcontextos (namespaces)
preserva as costuras para extração futura.

### 3.1 IdentityAccess

**Responsabilidade:** Autenticação, autorização, sessões, RBAC, SSO, multi-tenancy,
acesso privilegiado (JIT, Break Glass, Delegação) e access reviews.

| Aggregate Root | Entidades / VOs Principais                                |
|----------------|-----------------------------------------------------------|
| `User`         | Email, FullName, PasswordHash, RefreshToken, FederatedLogin |
| `Role`         | Permission, RolePermissionCatalog                          |
| `Session`      | LoginSession, SecurityEvent                                |

**DbContexts:** `IdentityDbContext`
**Tabelas:** `identity_users`, `identity_roles`, `identity_security_events`,
`identity_environments`, `identity_environment_accesses`,
`identity_jit_access_requests`, `identity_access_review_campaigns`,
`identity_access_review_items`
**Integration Events:** `UserCreatedIntegrationEvent`, `UserRoleChangedIntegrationEvent`

### 3.2 CommercialGovernance

**Responsabilidade:** Licenciamento da plataforma, capacidades por tier,
hardware binding, quotas de uso e ativações.

| Aggregate Root    | Entidades / VOs Principais                   |
|-------------------|----------------------------------------------|
| `License`         | Capability, UsageQuota, Activation           |
| `HardwareBinding` | HardwareFingerprint, BindingStatus           |

**DbContexts:** `LicensingDbContext`
**Tabelas:** `licensing_licenses`, `licensing_hardware_bindings`,
`licensing_capabilities`, `licensing_usage_quotas`, `licensing_activations`
**Integration Events:** `LicenseActivatedIntegrationEvent`,
`LicenseThresholdReachedIntegrationEvent`
**SharedContracts:** `ILicensingModule`, DTOs de capacidade

### 3.3 Catalog

**Responsabilidade:** Catálogo unificado de APIs, contratos OpenAPI e portal do
desenvolvedor. Consolida os antigos módulos EngineeringGraph, Contracts e DeveloperPortal.

| Subcontexto         | Aggregate Roots                              |
|---------------------|----------------------------------------------|
| **Graph**           | `ApiAsset`, `ServiceAsset`, `ConsumerRelationship` |
| **Contracts**       | `ContractVersion`, `ContractDiff`            |
| **Portal**          | `Subscription`, `PlaygroundSession`          |

**DbContexts:** `EngineeringGraphDbContext`, `ContractsDbContext`, `DeveloperPortalDbContext`
**Tabelas (Graph):** `eg_api_assets`, `eg_service_assets`, `eg_consumer_assets`,
`eg_consumer_relationships`, `eg_discovery_sources`, `graph_snapshots`,
`node_health_records`, `saved_graph_views`
**Tabelas (Contracts):** `ct_contract_versions`, `ct_contract_diffs`,
`ct_contract_rule_violations`, `ct_contract_artifacts`
**Tabelas (Portal):** `dp_subscriptions`, `dp_playground_sessions`,
`dp_portal_analytics_events`, `dp_code_generation_records`, `dp_saved_searches`
**SharedContracts:** `IEngineeringGraphModule`, `IContractsModule`, `IDeveloperPortalModule`

### 3.4 ChangeGovernance

**Responsabilidade:** O **núcleo da plataforma** — inteligência de mudança, blast radius,
workflows de aprovação, promoção entre ambientes e governança por rulesets. Consolida os
antigos módulos ChangeIntelligence, Workflow, Promotion e RulesetGovernance.

| Subcontexto            | Aggregate Roots                                  |
|------------------------|--------------------------------------------------|
| **ChangeIntelligence** | `Release`, `BlastRadiusReport`, `ChangeScore`    |
| **Workflow**           | `WorkflowTemplate`, `WorkflowInstance`, `EvidencePack` |
| **Promotion**          | `PromotionRequest`, `PromotionGate`              |
| **RulesetGovernance**  | `Ruleset`, `RulesetBinding`, `LintResult`        |

**DbContexts:** `ChangeIntelligenceDbContext`, `WorkflowDbContext`,
`PromotionDbContext`, `RulesetGovernanceDbContext`
**Tabelas (CI):** `ci_releases`, `ci_change_events`, `ci_change_scores`,
`ci_blast_radius_reports`
**Tabelas (Workflow):** `wf_workflow_templates`, `wf_workflow_instances`,
`wf_workflow_stages`, `wf_approval_decisions`, `wf_sla_policies`, `wf_evidence_packs`
**Tabelas (Promotion):** `prm_promotion_requests`, `prm_promotion_gates`,
`prm_deployment_environments`, `prm_gate_evaluations`
**Tabelas (Ruleset):** `rg_rulesets`, `rg_ruleset_bindings`, `rg_lint_results`
**Integration Events:** `DeploymentEventReceivedEvent`, `ReleasePublishedEvent`,
`WorkflowApprovedEvent`, `WorkflowRejectedEvent`, `PromotionRegisteredEvent`
**Hexagonal Ports:** `IDeploymentEventPort`, `IDeploymentDecisionPort`

### 3.5 OperationalIntelligence

**Responsabilidade:** Sinais de runtime, correlação de anomalias, drift detection,
inteligência de custos e atribuição de custos por API/consumer. Consolida os antigos
módulos RuntimeIntelligence e CostIntelligence.

| Subcontexto | Aggregate Roots                              |
|-------------|----------------------------------------------|
| **Runtime** | `RuntimeSnapshot`, `DriftFinding`            |
| **Cost**    | `CostSnapshot`, `CostAttribution`            |

**DbContexts:** `RuntimeIntelligenceDbContext`, `CostIntelligenceDbContext`
**Integration Events:** `RuntimeSignalReceivedEvent`, `RuntimeAnomalyDetectedEvent`,
`CostAnomalyDetectedEvent`
**Hexagonal Ports:** `IRuntimeSignalIngestionPort`, `IRuntimeCorrelationPort`

### 3.6 AIKnowledge

**Responsabilidade:** Orquestração de IA interna, routing para provedores de IA externos
(LLMs), knowledge capture e context engine. Consolida os antigos módulos AiOrchestration
e ExternalAi.

| Subcontexto       | Aggregate Roots                                    |
|-------------------|----------------------------------------------------|
| **ExternalAI**    | `ExternalAiConsultation`, `KnowledgeCapture`       |
| **Orchestration** | `AiConversation`, `GeneratedTestArtifact`          |

**DbContexts:** `ExternalAiDbContext`, `AiOrchestrationDbContext`
**Integration Events:** `ExternalAIQueryRequestedEvent`,
`ExternalAIResponseReceivedEvent`, `KnowledgeCandidateCreatedEvent`
**Hexagonal Ports:** `IExternalAIRoutingPort`

### 3.7 AuditCompliance

**Responsabilidade:** Trilha de auditoria imutável, integridade criptográfica via
hash chain, busca em audit trail, exportação de evidências e políticas de retenção.

| Aggregate Root   | Entidades / VOs Principais                       |
|------------------|--------------------------------------------------|
| `AuditEvent`     | EventType, ActorId, TenantId, Payload, Timestamp |
| `AuditChainLink` | PreviousHash, CurrentHash, BlockIndex            |

**DbContexts:** `AuditDbContext`
**Tabelas:** `aud_audit_events`, `aud_audit_chain_links`, `aud_retention_policies`
**Integration Events:** `AuditEventRecordedEvent`,
`AuditIntegrityCheckpointCreatedEvent`
**Hexagonal Ports:** `IAuditIntegrityPort`

---

## 4. Estrutura Interna de Módulo (3 Camadas)

Cada módulo possui **exatamente 3 projetos**. Os endpoints Minimal API ficam **dentro da
camada Infrastructure** (não há projeto API separado). O ApiHost compõe todos os módulos
via discovery automático de `*EndpointModule`.

```
src/modules/{context}/
│
├── NexTraceOne.{Context}.Domain/
│   ├── {Subcontext}/Entities/       ← Aggregates, entidades filhas, StronglyTypedIds
│   ├── {Subcontext}/ValueObjects/   ← Value Objects com validação embutida
│   ├── {Subcontext}/Events/         ← Integration Events (herdam IntegrationEventBase)
│   ├── {Subcontext}/Ports/          ← Hexagonal Ports (interfaces de domínio)
│   ├── {Subcontext}/Enums/          ← Enumerações do domínio
│   ├── {Subcontext}/Errors/         ← Catálogo centralizado de erros (Error codes i18n)
│   └── SharedContracts/
│       ├── ServiceInterfaces/       ← I{Context}Module — contrato público cross-módulo
│       ├── IntegrationEvents/       ← Eventos publicados para outros módulos (quando no root)
│       └── DTOs/                    ← Objetos de transferência públicos
│
├── NexTraceOne.{Context}.Application/
│   ├── {Subcontext}/Features/       ← VSA: 1 arquivo por use case
│   │   └── {FeatureName}.cs         ← Command/Query + Validator + Handler + Response
│   ├── {Subcontext}/Abstractions/   ← Interfaces internas do subcontexto
│   ├── {Subcontext}/DependencyInjection.cs  ← Registro DI do subcontexto
│   └── DependencyInjection.cs       ← Registro DI raiz (quando não há subcontextos)
│
└── NexTraceOne.{Context}.Infrastructure/
    ├── {Subcontext}/Persistence/    ← DbContext, Configurations, Repositories, Migrations
    ├── {Subcontext}/Endpoints/      ← EndpointModule + arquivos de endpoint Minimal API
    ├── {Subcontext}/Services/       ← Implementações de adaptadores externos
    ├── {Subcontext}/DependencyInjection.cs  ← Registro DI do subcontexto
    └── DependencyInjection.cs       ← Registro DI raiz (quando não há subcontextos)
```

### Exemplo Concreto: Catalog

> **Nota:** A pasta `Endpoints/Endpoints/` é intencional — o primeiro nível contém
> `DependencyInjection.cs` do grupo de endpoints; o segundo nível contém os arquivos
> `*EndpointModule.cs` com os mapeamentos Minimal API.

```
src/modules/catalog/
├── NexTraceOne.Catalog.Domain/
│   ├── Graph/Entities/              ← ApiAsset, ServiceAsset, ConsumerRelationship
│   ├── Graph/SharedContracts/ServiceInterfaces/IEngineeringGraphModule.cs
│   ├── Contracts/Entities/          ← ContractVersion, ContractDiff
│   ├── Contracts/SharedContracts/ServiceInterfaces/IContractsModule.cs
│   ├── Portal/Entities/             ← Subscription, PlaygroundSession
│   └── Portal/SharedContracts/ServiceInterfaces/IDeveloperPortalModule.cs
│
├── NexTraceOne.Catalog.Application/
│   ├── Graph/Features/              ← RegisterApiAsset, SearchCatalog, etc.
│   ├── Graph/DependencyInjection.cs
│   ├── Contracts/Features/          ← ImportContract, ComputeSemanticDiff, etc.
│   ├── Contracts/DependencyInjection.cs
│   ├── Portal/Features/             ← CreateSubscription, SearchPortal, etc.
│   └── Portal/DependencyInjection.cs
│
└── NexTraceOne.Catalog.Infrastructure/
    ├── Graph/Persistence/           ← EngineeringGraphDbContext, Configurations, Repos
    ├── Graph/Endpoints/
    │   ├── DependencyInjection.cs   ← Registro DI dos endpoints do subcontexto
    │   └── Endpoints/               ← EngineeringGraphEndpointModule.cs
    ├── Contracts/Persistence/       ← ContractsDbContext, Configurations, Repos
    ├── Contracts/Endpoints/
    │   ├── DependencyInjection.cs
    │   └── Endpoints/               ← ContractsEndpointModule.cs
    ├── Portal/Persistence/          ← DeveloperPortalDbContext, Configurations
    └── Portal/Endpoints/
        ├── DependencyInjection.cs
        └── Endpoints/               ← DeveloperPortalEndpointModule.cs
```

### Padrão de Endpoint Module

Cada subcontexto expõe seus endpoints via uma classe estática com o método
`MapEndpoints(IEndpointRouteBuilder app)`. O ApiHost descobre e invoca automaticamente:

```csharp
public sealed class ContractsEndpointModule
{
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/contracts")
            .WithTags("Contracts");

        group.MapPost("/import", async (ImportContractCommand cmd, ISender sender) =>
            (await sender.Send(cmd)).ToHttpResult());

        group.MapGet("/versions/{id}/diff", async (Guid id, ISender sender) =>
            (await sender.Send(new ComputeSemanticDiffQuery(id))).ToHttpResult());
    }
}
```

---

## 5. Costuras Internas (Future Seams)

Cada módulo consolidado possui separações internas por namespace (subcontextos) que
funcionam como **costuras de extração futura**. Se a escala exigir, um subcontexto pode
ser promovido a módulo independente sem refactoring de domain boundaries.

| Módulo                    | Subcontextos Internos                                            |
|---------------------------|------------------------------------------------------------------|
| **Catalog**               | `Graph`, `Contracts`, `Portal`                                   |
| **ChangeGovernance**      | `ChangeIntelligence`, `Workflow`, `Promotion`, `RulesetGovernance` |
| **OperationalIntelligence** | `Runtime` (Core, Ingestion, Correlation), `Cost` (CostCorrelation) |
| **AIKnowledge**           | `ExternalAI` (ExternalAIRouting), `Orchestration` (ContextEngine, KnowledgeCapture) |
| **AuditCompliance**       | `Audit` (AuditSearch, AuditExports, AuditIntegrity)              |
| **IdentityAccess**        | Contexto único — sem subcontextos internos                       |
| **CommercialGovernance**  | Contexto único — sem subcontextos internos                       |

### Critérios para Extração de Subcontexto

Um subcontexto deve ser promovido a módulo independente quando:

1. **Volume de transações** justifica escala independente (ex: Runtime Ingestion)
2. **Ciclo de deploy** diverge do módulo pai (ex: AI Gateway com releases frequentes)
3. **Requisitos de latência** exigem infraestrutura dedicada (ex: Runtime Stream Processor)
4. **Equipe dedicada** assume ownership exclusivo do subcontexto

---

## 6. Portas Hexagonais (Ports)

Portas hexagonais definem contratos de domínio para operações que dependem de
infraestrutura externa. A inversão de dependência garante que o domínio nunca conhece
detalhes de implementação. Adaptadores na camada Infrastructure implementam as portas.

| Port                          | Módulo                  | Responsabilidade                                      |
|-------------------------------|-------------------------|-------------------------------------------------------|
| `IRuntimeSignalIngestionPort` | OperationalIntelligence | Ingestão de sinais de runtime (métricas, traces, logs) |
| `IRuntimeCorrelationPort`     | OperationalIntelligence | Correlação de anomalias e drift detection              |
| `IExternalAIRoutingPort`      | AIKnowledge             | Routing para provedores de IA externos (OpenAI, etc.)  |
| `IAuditIntegrityPort`         | AuditCompliance         | Verificação de integridade da hash chain de auditoria  |
| `IDeploymentEventPort`        | ChangeGovernance        | Recepção de webhooks de CI/CD (deployment notifications) |
| `IDeploymentDecisionPort`     | ChangeGovernance        | Decisão de deploy baseada em análise de risco          |

---

## 7. Integration Events

Integration Events são o mecanismo exclusivo de comunicação assíncrona entre módulos.
Todos herdam de `IntegrationEventBase` e são persistidos via Outbox Pattern na mesma
transação do command que os originou.

### Por Módulo

**IdentityAccess (2)**
- `UserCreatedIntegrationEvent` — Usuário criado, propaga para licenciamento e auditoria
- `UserRoleChangedIntegrationEvent` — Role alterada, propaga para auditoria e governança

**CommercialGovernance (2)**
- `LicenseActivatedIntegrationEvent` — Licença ativada, habilita capacidades da plataforma
- `LicenseThresholdReachedIntegrationEvent` — Threshold de uso atingido, alerta operacional

**ChangeGovernance (5)**
- `DeploymentEventReceivedEvent` — Webhook de CI/CD recebido, inicia análise
- `ReleasePublishedEvent` — Release publicada, dispara blast radius e workflows
- `WorkflowApprovedEvent` — Workflow aprovado, habilita promoção
- `WorkflowRejectedEvent` — Workflow rejeitado, bloqueia promoção
- `PromotionRegisteredEvent` — Promoção registrada, rastreia em auditoria

**OperationalIntelligence (3)**
- `RuntimeSignalReceivedEvent` — Sinal de runtime ingerido, inicia correlação
- `RuntimeAnomalyDetectedEvent` — Anomalia detectada, alerta operacional
- `CostAnomalyDetectedEvent` — Anomalia de custo detectada, alerta financeiro

**AIKnowledge (3)**
- `ExternalAIQueryRequestedEvent` — Query para IA externa solicitada
- `ExternalAIResponseReceivedEvent` — Resposta de IA externa recebida
- `KnowledgeCandidateCreatedEvent` — Candidato de knowledge capturado

**AuditCompliance (2)**
- `AuditEventRecordedEvent` — Evento de auditoria registrado na chain
- `AuditIntegrityCheckpointCreatedEvent` — Checkpoint de integridade criado

**Catalog (0)**
O módulo Catalog não publica Integration Events no momento — é consumidor de eventos
de outros módulos (ChangeGovernance, IdentityAccess).

---

## 8. Regras de Dependência (INEGOCIÁVEL)

### Dentro de um módulo

```
Domain  ←──  Application  ←──  Infrastructure
  │                                   │
  │  (Domain é puro, sem deps)        │  (Endpoints + Persistence + Adapters)
  │                                   │
  └─── SharedContracts/               └─── Endpoints/*EndpointModule
       ServiceInterfaces/                  (Minimal APIs dentro de Infrastructure)
```

- **Domain** NÃO referencia nenhum outro projeto do módulo.
- **Application** referencia apenas Domain.
- **Infrastructure** referencia Application e Domain.
- Endpoints ficam **dentro de Infrastructure**, não em projeto separado.
- SharedContracts (ServiceInterfaces, DTOs) ficam no **Domain** para serem acessíveis
  por outros módulos via referência ao Domain project.

### Entre módulos

- **PROIBIDO** referenciar projetos de outro módulo diretamente (exceto Domain para
  SharedContracts/ServiceInterfaces).
- Comunicação assíncrona **exclusivamente** via Integration Events (Outbox Pattern).
- Consultas síncronas via `ServiceInterfaces` definidas em `Domain/SharedContracts/`.
- O ApiHost compõe todos os módulos — é o único projeto que referencia tudo.

### Building Blocks → Módulos

```
Módulo.Domain           → BB.Domain
Módulo.Application      → BB.Application (inclui BB.Domain transitivamente)
Módulo.Infrastructure   → BB.Infrastructure, BB.EventBus, BB.Security
```

### Diagrama de Dependências

```
┌─────────────────────────────────────────────────────────────┐
│                        ApiHost                               │
│  (compõe todos os módulos + building blocks)                 │
└──────┬───────┬───────┬───────┬───────┬───────┬───────┬──────┘
       │       │       │       │       │       │       │
       ▼       ▼       ▼       ▼       ▼       ▼       ▼
   Identity  Commercial Catalog Change  Oper.  AI    Audit
   Access    Governance         Gov.    Intel. Know. Compliance
       │       │       │       │       │       │       │
       ▼       ▼       ▼       ▼       ▼       ▼       ▼
┌─────────────────────────────────────────────────────────────┐
│                    Building Blocks                            │
│  Domain │ Application │ Infrastructure │ EventBus │ Security │
│         │             │                │  Observability       │
└─────────────────────────────────────────────────────────────┘
       │
       ▼
┌─────────────────┐
│   PostgreSQL 16  │
└─────────────────┘
```

---

## 9. Platform

### 9.1 ApiHost — Entry Point Principal

O entry point da aplicação Modular Monolith. Compõe todos os módulos, registra building
blocks transversais e configura o pipeline HTTP.

**Arquivo:** `src/platform/NexTraceOne.ApiHost/Program.cs`

**Responsabilidades:**
- Verificação de integridade de assemblies no boot (SHA-256)
- Logging estruturado via Serilog (`ConfigureNexTraceSerilog()`)
- Registro de Building Blocks: EventBus, Observability, Security
- Registro de todos os módulos (DI de cada subcontexto)
- OpenAPI com Scalar (tema BluePlanet)
- CORS configurável por ambiente
- Rate Limiting: 100 req/IP/60s (20 para IPs não resolvidos)
- Auto-migrations de todos os DbContexts no startup
- Seed data idempotente em desenvolvimento
- Health check anónimo em `/health`

**Pipeline de Middlewares (ordem de execução):**
1. HTTPS Redirect
2. Rate Limiter
3. Security Headers (CSP, HSTS, X-Frame-Options, etc.)
4. `TenantResolutionMiddleware` (JWT → Header → Subdomínio)
5. `GlobalExceptionHandler` (captura exceções, retorna ProblemDetails)
6. Authentication + Authorization

**Discovery de Endpoints:**
O `ModuleEndpointRouteBuilderExtensions.MapAllModuleEndpoints()` descobre
automaticamente todas as classes `*EndpointModule` via reflection e invoca
`MapEndpoints(IEndpointRouteBuilder)` em cada uma.

### 9.2 BackgroundWorkers — Outbox + Quartz.NET

Processa Outbox Messages com at-least-once delivery e executa jobs agendados.

**Arquivo:** `src/platform/NexTraceOne.BackgroundWorkers/`

**Jobs:**
- **OutboxProcessor** — Consome mensagens do Outbox e despacha Integration Events
- **SlaChecker** — Verifica SLAs de workflows pendentes
- **FingerprintCapture** — Captura hardware fingerprint para licensing
- **LicenseValidation** — Validação periódica da licença ativa

### 9.3 Ingestion.Api — Entry Point para Integrações Externas

Entry point dedicado para integrações de alta frequência que não passam pelo pipeline
completo do ApiHost (ex: webhooks de CI/CD, sinais de runtime).

**Arquivo:** `src/platform/NexTraceOne.Ingestion.Api/`

**Responsabilidades:**
- Recepção de webhooks de CI/CD (deployment notifications)
- Ingestão de sinais de runtime (métricas, traces, logs)
- Pipeline otimizado para throughput (menos middlewares)
- Validação mínima → enfileiramento para processamento assíncrono

### 9.4 CLI (`nex`) — Ferramenta de Linha de Comando

Ferramenta de linha de comando para interação com a plataforma.
Consome **apenas** ServiceInterfaces dos módulos (padrão de consumidor externo).

**Arquivo:** `tools/NexTraceOne.CLI/`
**Frameworks:** `System.CommandLine`, `Spectre.Console`

**Comandos planejados:**
- `nex validate` — Validar contrato OpenAPI com ruleset
- `nex release` — Gerenciar releases (status, health, history)
- `nex promotion` — Controlar promoção entre ambientes
- `nex approval` — Submeter/consultar workflows de aprovação
- `nex impact` — Analisar blast radius de mudanças
- `nex tests` — Gerar cenários de teste (Robot Framework)
- `nex catalog` — Consultar catálogo de APIs/serviços

---

## 10. Database — Isolamento por Contexto

Cada módulo (e subcontexto com complexidade suficiente) possui seu próprio DbContext
e tabelas com prefixo isolado. **PROIBIDO** acessar tabelas de outro módulo diretamente —
comunicação exclusivamente via Integration Events ou ServiceInterfaces.

### DbContexts por Módulo

| Módulo                  | Subcontexto        | DbContext                      | Prefixo de Tabelas |
|-------------------------|--------------------|--------------------------------|---------------------|
| IdentityAccess          | —                  | `IdentityDbContext`            | `identity_`         |
| CommercialGovernance    | —                  | `LicensingDbContext`           | `licensing_`        |
| Catalog                 | Graph              | `EngineeringGraphDbContext`    | `eg_`               |
| Catalog                 | Contracts          | `ContractsDbContext`           | `ct_`               |
| Catalog                 | Portal             | `DeveloperPortalDbContext`     | `dp_`               |
| ChangeGovernance        | ChangeIntelligence | `ChangeIntelligenceDbContext`  | `ci_`               |
| ChangeGovernance        | Workflow           | `WorkflowDbContext`            | `wf_`               |
| ChangeGovernance        | Promotion          | `PromotionDbContext`           | `prm_`              |
| ChangeGovernance        | RulesetGovernance  | `RulesetGovernanceDbContext`   | `rg_`               |
| OperationalIntelligence | Runtime            | `RuntimeIntelligenceDbContext` | (runtime tables)    |
| OperationalIntelligence | Cost               | `CostIntelligenceDbContext`    | (cost tables)       |
| AIKnowledge             | ExternalAI         | `ExternalAiDbContext`          | (external_ai tables)|
| AIKnowledge             | Orchestration      | `AiOrchestrationDbContext`     | (orchestration tables) |
| AuditCompliance         | —                  | `AuditDbContext`               | `aud_`              |

Todos os DbContexts herdam de `NexTraceDbContextBase`, que fornece:
- **Row-Level Security (RLS)** via `TenantRlsInterceptor`
- **Audit automático** via `AuditInterceptor` (CreatedAt/By, UpdatedAt/By)
- **Outbox integration** — Domain Events convertidos em OutboxMessages no SaveChanges
- **Encrypted fields** via `EncryptedStringConverter` (AES-256-GCM)

---

## 11. Caminho para Microserviços

A arquitetura foi desenhada para que a extração de módulos ou subcontextos seja
**incremental e de baixo risco**. As costuras já estão definidas — nenhum refactoring
de boundaries é necessário.

### Futuros Serviços Independentes (costuras já preparadas)

| Serviço Futuro              | Origem                                     | Motivação de Extração               |
|-----------------------------|--------------------------------------------|--------------------------------------|
| **Runtime Agent**           | OperationalIntelligence / Runtime           | Alto throughput de ingestão          |
| **AI Gateway**              | AIKnowledge / ExternalAI                    | Latência e billing independentes     |
| **Audit Ledger**            | AuditCompliance                             | Compliance e retenção independentes  |
| **Deployment Orchestrator** | ChangeGovernance / ChangeIntelligence       | Webhook processing dedicado          |
| **Runtime Stream Processor**| OperationalIntelligence / Runtime           | Processamento de stream em tempo real |

### Passos para Extração

1. **Cada subcontexto já tem seu próprio DbContext** com tabelas isoladas
2. **Cada subcontexto já tem seus próprios Endpoints** agrupados em EndpointModule
3. **Comunicação cross-módulo já é via Integration Events** (Outbox Pattern)
4. **Substituir `InProcessEventBus`** por RabbitMQ/Kafka no módulo extraído
5. **Promover o subcontexto** a host independente (cópia do ApiHost com menos módulos)
6. **Adicionar API Gateway** (Kong/YARP) na frente dos serviços extraídos
7. **Zero refactoring de domain boundaries** — as fronteiras já estão definidas

---

## 12. Frontend — Organizado por Bounded Context

O frontend React + TypeScript está organizado por **bounded context** na pasta `features/`,
espelhando a estrutura do backend. Cada feature agrupa páginas e chamadas de API relacionadas
a um contexto de domínio, facilitando a navegação, manutenção e futura extração modular.

```
src/frontend/
├── public/                                ← Assets estáticos
├── src/
│   ├── features/                          ← Organizado por bounded context
│   │   ├── identity-access/              ← Autenticação, utilizadores, sessões, SSO
│   │   │   ├── pages/                     (Login, Users, BreakGlass, JIT, Delegation, AccessReview)
│   │   │   ├── api/                       (identity.ts)
│   │   │   └── index.ts                   (barrel export)
│   │   ├── commercial-governance/        ← Licenciamento, capacidades
│   │   │   ├── pages/                     (LicensingPage)
│   │   │   ├── api/                       (licensing.ts)
│   │   │   └── index.ts
│   │   ├── catalog/                      ← Catálogo de APIs, contratos, portal
│   │   │   ├── pages/                     (Contracts, EngineeringGraph, DeveloperPortal)
│   │   │   ├── api/                       (contracts.ts, engineeringGraph.ts, developerPortal.ts)
│   │   │   └── index.ts
│   │   ├── change-governance/            ← Releases, workflows, promoção
│   │   │   ├── pages/                     (Releases, Workflow, Promotion)
│   │   │   ├── api/                       (changeIntelligence.ts, workflow.ts, promotion.ts)
│   │   │   └── index.ts
│   │   ├── audit-compliance/             ← Auditoria e compliance
│   │   │   ├── pages/                     (AuditPage)
│   │   │   ├── api/                       (audit.ts)
│   │   │   └── index.ts
│   │   └── shared/                       ← Páginas cross-context
│   │       ├── pages/                     (DashboardPage)
│   │       └── index.ts
│   ├── api/                               ← API client centralizado + barrel re-export
│   │   ├── client.ts                      (instância Axios configurada)
│   │   └── index.ts                       (re-exporta APIs de cada feature)
│   ├── auth/                              ← Autenticação, guards, token management
│   ├── components/                        ← Componentes compartilhados (UI library)
│   ├── contexts/                          ← React contexts (tema, tenant, sessão)
│   ├── hooks/                             ← Hooks compartilhados (usePermissions, etc.)
│   ├── locales/                           ← Catálogos i18n (pt-BR, pt-PT, en, es)
│   ├── types/                             ← TypeScript types compartilhados
│   ├── utils/                             ← Utilitários (sanitize, navigation, etc.)
│   └── __tests__/                         ← Testes unitários espelhando a estrutura
│       ├── components/
│       ├── auth/
│       ├── contexts/
│       ├── hooks/
│       ├── pages/
│       └── utils/
└── e2e/                                   ← Testes end-to-end (Playwright)
```

### Mapeamento Frontend → Backend (Bounded Contexts)

| Feature Frontend         | Bounded Context Backend       | Subdomínios                                        |
|--------------------------|-------------------------------|----------------------------------------------------|
| `identity-access`        | IdentityAccess                | Identity, Sessions, FederatedLogin, RBAC           |
| `commercial-governance`  | CommercialGovernance          | Licensing, Entitlements, HardwareBinding           |
| `catalog`                | Catalog                       | Contracts, EngineeringGraph, DeveloperPortal       |
| `change-governance`      | ChangeGovernance              | ChangeIntelligence, Workflow, RulesetGovernance    |
| `audit-compliance`       | AuditCompliance               | AuditTrail, Integrity, EvidencePack                |
| `shared`                 | Cross-context                 | Dashboard agregando dados de múltiplos contextos   |

### Regras do Frontend

- **i18n obrigatório** — todo texto visível ao usuário vem de catálogos de tradução.
- **Organização por bounded context** — cada feature agrupa pages + api do mesmo domínio.
- **API client centralizado** — nunca montar URLs concatenando strings.
- **Token security** — refresh token em memória (closure), access token em sessionStorage.
- **CSP-compatible** — sem `eval()`, `new Function()` ou `unsafe-inline`.
- **ErrorBoundary global** — captura erros sem expor detalhes técnicos em produção.
- **Barrel exports** — cada feature expõe páginas e APIs via `index.ts`.

---

## 13. Testes — Estrutura por Camada

```
tests/
├── building-blocks/                     ← Testes unitários dos Building Blocks
│   ├── NexTraceOne.BuildingBlocks.Application.Tests/
│   ├── NexTraceOne.BuildingBlocks.Domain.Tests/
│   ├── NexTraceOne.BuildingBlocks.EventBus.Tests/
│   ├── NexTraceOne.BuildingBlocks.Infrastructure.Tests/
│   ├── NexTraceOne.BuildingBlocks.Observability.Tests/
│   └── NexTraceOne.BuildingBlocks.Security.Tests/
│
├── modules/                             ← Testes por módulo (unit + integration)
│   ├── aiknowledge/NexTraceOne.AIKnowledge.Tests/
│   ├── auditcompliance/NexTraceOne.AuditCompliance.Tests/
│   ├── catalog/NexTraceOne.Catalog.Tests/
│   ├── changegovernance/NexTraceOne.ChangeGovernance.Tests/
│   ├── commercialgovernance/NexTraceOne.CommercialGovernance.Tests/
│   ├── identityaccess/NexTraceOne.IdentityAccess.Tests/
│   └── operationalintelligence/NexTraceOne.OperationalIntelligence.Tests/
│
└── platform/                            ← Testes de plataforma
    ├── NexTraceOne.IntegrationTests/    ← Cross-module integration scenarios
    └── NexTraceOne.E2E.Tests/           ← End-to-end API testing
```

### Convenções de Teste

- Cada módulo possui **um único projeto de teste** cobrindo Domain, Application e
  Infrastructure.
- Testes unitários usam `xUnit` + `FluentAssertions` + `NSubstitute`.
- Testes de integração usam `Testcontainers` para PostgreSQL real.
- Testes E2E usam `Playwright` para frontend e `WebApplicationFactory` para backend.

---

## 14. Estrutura Física Completa

```
NexTraceOne/
├── src/
│   ├── building-blocks/                 ← 6 projetos transversais
│   │   ├── NexTraceOne.BuildingBlocks.Domain/
│   │   ├── NexTraceOne.BuildingBlocks.Application/
│   │   ├── NexTraceOne.BuildingBlocks.Infrastructure/
│   │   ├── NexTraceOne.BuildingBlocks.EventBus/
│   │   ├── NexTraceOne.BuildingBlocks.Observability/
│   │   └── NexTraceOne.BuildingBlocks.Security/
│   │
│   ├── modules/                         ← 7 bounded contexts × 3 camadas
│   │   ├── identityaccess/
│   │   │   ├── NexTraceOne.IdentityAccess.Domain/
│   │   │   ├── NexTraceOne.IdentityAccess.Application/
│   │   │   └── NexTraceOne.IdentityAccess.Infrastructure/
│   │   ├── commercialgovernance/
│   │   │   ├── NexTraceOne.CommercialGovernance.Domain/
│   │   │   ├── NexTraceOne.CommercialGovernance.Application/
│   │   │   └── NexTraceOne.CommercialGovernance.Infrastructure/
│   │   ├── catalog/
│   │   │   ├── NexTraceOne.Catalog.Domain/
│   │   │   ├── NexTraceOne.Catalog.Application/
│   │   │   └── NexTraceOne.Catalog.Infrastructure/
│   │   ├── changegovernance/
│   │   │   ├── NexTraceOne.ChangeGovernance.Domain/
│   │   │   ├── NexTraceOne.ChangeGovernance.Application/
│   │   │   └── NexTraceOne.ChangeGovernance.Infrastructure/
│   │   ├── operationalintelligence/
│   │   │   ├── NexTraceOne.OperationalIntelligence.Domain/
│   │   │   ├── NexTraceOne.OperationalIntelligence.Application/
│   │   │   └── NexTraceOne.OperationalIntelligence.Infrastructure/
│   │   ├── aiknowledge/
│   │   │   ├── NexTraceOne.AIKnowledge.Domain/
│   │   │   ├── NexTraceOne.AIKnowledge.Application/
│   │   │   └── NexTraceOne.AIKnowledge.Infrastructure/
│   │   └── auditcompliance/
│   │       ├── NexTraceOne.AuditCompliance.Domain/
│   │       ├── NexTraceOne.AuditCompliance.Application/
│   │       └── NexTraceOne.AuditCompliance.Infrastructure/
│   │
│   ├── platform/
│   │   ├── NexTraceOne.ApiHost/         ← Entry point principal (Modular Monolith)
│   │   ├── NexTraceOne.BackgroundWorkers/ ← Outbox processor + Quartz.NET jobs
│   │   └── NexTraceOne.Ingestion.Api/   ← Entry point para integrações externas
│   │
│   └── frontend/                        ← React + TypeScript + Vite
│       ├── src/
│       │   ├── features/                ← Organizado por bounded context
│       │   │   ├── identity-access/     (pages + api)
│       │   │   ├── commercial-governance/
│       │   │   ├── catalog/
│       │   │   ├── change-governance/
│       │   │   ├── audit-compliance/
│       │   │   └── shared/
│       │   ├── api/                     ← Client centralizado + barrel re-export
│       │   ├── components/              ← Componentes shared
│       │   ├── contexts/                ← React contexts
│       │   ├── hooks/                   ← Hooks compartilhados
│       │   ├── locales/                 ← i18n (pt-BR, pt-PT, en, es)
│       │   ├── types/                   ← TypeScript types
│       │   └── utils/                   ← Utilitários
│       └── e2e/
│
├── tools/
│   └── NexTraceOne.CLI/                 ← CLI 'nex' (System.CommandLine + Spectre.Console)
│
├── tests/
│   ├── building-blocks/                 ← 6 projetos de teste
│   ├── modules/                         ← 7 projetos de teste (1 por módulo)
│   └── platform/                        ← Integration + E2E tests
│
├── docs/                                ← Documentação técnica
├── database/                            ← Scripts SQL auxiliares
├── scripts/                             ← Scripts de automação
├── build/                               ← Configurações de build
│
├── NexTraceOne.sln                      ← Solution raiz
├── Directory.Build.props                ← Propriedades globais de build
├── Directory.Packages.props             ← Central Package Management
└── global.json                          ← SDK version pinning
```
