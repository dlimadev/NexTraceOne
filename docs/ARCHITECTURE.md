# NexTraceOne — Arquitetura Definitiva v2 (Archon Pattern)

## 1. Visão Geral

NexTraceOne segue o **Archon Pattern** — um Modular Monolith onde cada módulo já nasce como um serviço potencial, pronto para extração futura sem refactoring de boundaries.

### Princípios Arquiteturais

- **Modular Monolith:** Hoje tudo roda em um processo; amanhã cada módulo pode virar serviço independente.
- **Vertical Slice Architecture (VSA):** Features completas em um único arquivo (Endpoint + Command + Handler + Validator + Response).
- **DDD Tático:** Aggregates, Value Objects, Domain Events onde há complexidade real de negócio.
- **CQRS:** Commands modificam estado, Queries leem. Separação clara via MediatR.
- **REPR Pattern:** Request → Endpoint → Presenter → Response nas Minimal APIs.
- **Outbox Pattern:** Integration Events persistidos na mesma transação, entregues de forma assíncrona.

---

## 2. Building Blocks (substituem SharedKernel monolítico)

Cada building block é um projeto independente. Módulos referenciam APENAS os building blocks que precisam.

### 2.1 BuildingBlocks.Domain

Primitivos DDD puros, sem dependência de infra.

```
Primitives/
  Entity<TId>           — Igualdade por identidade
  AggregateRoot<TId>    — Emite Domain Events, mantém invariantes
  ValueObject           — Igualdade por valor, imutável
  AuditableEntity<TId>  — CreatedAt/By, UpdatedAt/By, SoftDelete

StronglyTypedIds/
  ITypedId              — Marcador para Ids fortemente tipados
  TypedIdBase           — Record base (ex: ReleaseId(Guid Value) : TypedIdBase(Value))

Events/
  IDomainEvent          — Intra-módulo, via MediatR INotification
  IIntegrationEvent     — Cross-módulo, via Outbox Pattern
  DomainEventBase       — Record abstrato com EventId + OccurredAt
  IntegrationEventBase  — Record abstrato base para Integration Events (EventId, OccurredAt, SourceModule)
                          Todos os Integration Events de módulos DEVEM herdar desta classe.

Results/
  Result<T>             — Sucesso ou falha sem exceção
  Error                 — Código, mensagem e ErrorType
  ErrorType             — NotFound | Validation | Conflict | Unauthorized | Forbidden | Security | Business

Guards/
  NexTraceGuards        — Extensões Ardalis (SemVer, TenantId, Environment)

Enums/
  ChangeLevel           — Taxonomia de mudanças (Operational → Breaking → Publication)
  DiscoveryConfidence   — Modelo de confiança (Inferred → Confirmed)
```

### 2.2 BuildingBlocks.Application

CQRS, behaviors e abstrações de serviço.

```
Cqrs/
  ICommand / ICommand<T>    — Marcadores de command
  ICommandHandler           — Handler de command
  IQuery<T>                 — Marcador de query
  IQueryHandler             — Handler de query
  IPagedQuery               — Contrato de paginação

Behaviors/ (Pipeline MediatR — executam em ordem)
  1. ValidationBehavior     — FluentValidation antes do handler
  2. LoggingBehavior        — Log estruturado entrada/saída/duração
  3. PerformanceBehavior    — Alerta >500ms (warn), >2000ms (error)
  4. TenantIsolationBehavior — Garante tenant ativo (exceto IPublicRequest)
  5. TransactionBehavior    — Commit/rollback automático por Command

Abstractions/
  ICurrentUser              — Usuário autenticado do HttpContext
  ICurrentTenant            — Tenant ativo (JWT/Header/Subdomínio)
  IUnitOfWork               — CommitAsync() coordenado
  IDateTimeProvider         — Abstração de DateTimeOffset.UtcNow (testável)
  IEventBus                 — Publicação de Integration Events via Outbox

Pagination/
  PagedList<T>              — Container padronizado para listagens

Extensions/
  ResultExtensions          — ToHttpResult() converte Result<T> em IResult HTTP
```

### 2.3 BuildingBlocks.Infrastructure

Persistência, interceptors e converters.

```
Persistence/
  NexTraceDbContextBase     — Base com RLS, interceptors, SaveChangesAsync orquestrado
  RepositoryBase<T,TId>     — CRUD + Specification sem código repetido

Outbox/
  OutboxMessage             — Mensagem do padrão Outbox

Interceptors/
  TenantRlsInterceptor     — SET app.current_tenant_id (RLS PostgreSQL)
  AuditInterceptor          — Preenche CreatedAt/By, UpdatedAt/By automaticamente

Converters/
  EncryptedStringConverter  — AES-256-GCM via EF Core Value Converter
```

### 2.4 BuildingBlocks.EventBus

Abstração de barramento de eventos com duas implementações.

```
Abstractions/
  IIntegrationEventHandler<T>  — Handler para eventos de outros módulos

InProcess/
  InProcessEventBus            — MediatR (monolith) → futuro RabbitMQ/Kafka

Outbox/
  OutboxEventBus               — Persiste no Outbox na mesma transação
```

### 2.5 BuildingBlocks.Observability

Logging, tracing, metrics e health checks.

```
Logging/
  SerilogConfiguration         — Enrichers, sinks, destructuring

Tracing/
  NexTraceActivitySources      — Commands, Queries, Events, ExternalHttp

Metrics/
  NexTraceMeters               — DeploymentsNotified, WorkflowsInitiated, BlastRadiusDuration

HealthChecks/
  NexTraceHealthChecks         — PostgreSQL, Outbox backlog, License, Integrity
```

### 2.6 BuildingBlocks.Security

Autenticação, criptografia, integridade e multi-tenancy.

```
Encryption/
  AesGcmEncryptor              — AES-256-GCM para campos sensíveis

Integrity/
  AssemblyIntegrityChecker     — SHA-256 do binário no boot

Licensing/
  HardwareFingerprint          — SHA-256(CPU|Motherboard|MAC)

MultiTenancy/
  TenantResolutionMiddleware   — JWT → Header → Subdomínio

Authentication/
  JwtTokenService              — Access token + Refresh token
```

---

## 3. Estrutura Interna de Módulo (5 camadas)

Cada módulo gera 5 projetos independentes:

```
src/modules/{nome}/
├── NexTraceOne.{Nome}.Domain/
│   ├── Entities/          ← Aggregates Roots + Entidades filhas + StronglyTypedIds
│   ├── ValueObjects/      ← Value Objects do bounded context
│   ├── Events/            ← Domain Events (intra-módulo)
│   ├── Enums/             ← Enumerações do domínio
│   └── Errors/            ← Catálogo centralizado de erros do módulo
│
├── NexTraceOne.{Nome}.Application/
│   ├── Features/          ← CORAÇÃO: 1 pasta por use case (VSA)
│   │   └── {FeatureName}/
│   │       └── {FeatureName}.cs  ← Command + Handler + Validator + Response
│   ├── Abstractions/      ← Interfaces internas do módulo
│   └── Extensions/        ← Extension methods do módulo
│
├── NexTraceOne.{Nome}.Contracts/
│   ├── IntegrationEvents/ ← Eventos publicados para outros módulos
│   ├── ServiceInterfaces/ ← I{Nome}Module — interface pública
│   └── DTOs/              ← Objetos de transferência públicos
│
├── NexTraceOne.{Nome}.Infrastructure/
│   ├── Persistence/
│   │   ├── {Nome}DbContext.cs
│   │   ├── Configurations/ ← IEntityTypeConfiguration
│   │   ├── Repositories/   ← Implementações de repositório
│   │   └── Migrations/     ← EF Core migrations isoladas
│   ├── Adapters/           ← HTTP Clients para sistemas externos
│   └── Services/           ← Implementações de serviço
│
└── NexTraceOne.{Nome}.API/
    ├── Endpoints/          ← {Nome}EndpointModule — Minimal API endpoints
    ├── Middleware/          ← Middleware específico do módulo
    └── Extensions/         ← DI composition do módulo
```

---

## 4. Regras de Dependência (INEGOCIÁVEL)

### Dentro de um módulo:

```
Domain ← Application ← Infrastructure
             ↑               ↓
        Contracts ─────── API
```

- Domain NÃO referencia nenhum outro projeto do módulo
- Application referencia Domain e Contracts
- Infrastructure referencia Application e Domain
- API referencia Application e Contracts
- Contracts referencia APENAS BuildingBlocks.Domain (para ITypedId, IIntegrationEvent)

### Entre módulos:

- **PROIBIDO** referenciar projetos de outro módulo diretamente
- Comunicação via Integration Events (Outbox Pattern)
- Consultas via Contracts (ServiceInterfaces)
- O ApiHost compõe todos os módulos

### Building Blocks → Módulos:

```
Módulo.Domain          → BB.Domain
Módulo.Application     → BB.Application
Módulo.Infrastructure  → BB.Infrastructure, BB.EventBus
Módulo.API             → BB.Security
Módulo.Contracts       → BB.Domain
```

---

## 5. Platform

### ApiHost

- Entry point da aplicação (Modular Monolith)
- Compõe todos os módulos: referencia API + Infrastructure de cada um
- Registra Building Blocks transversais
- Configura middlewares, autenticação, OpenAPI, health checks
- Amanhã: cada módulo pode virar seu próprio host

### BackgroundWorkers

- Processa Outbox Messages (at-least-once delivery)
- Executa Quartz.NET jobs agendados
- Referencia apenas Infrastructure de cada módulo

### CLI (`nex`)

- Ferramenta de linha de comando
- Consome APENAS Contracts de cada módulo (consumidor externo)
- Comandos: validate, release, promotion, approval, impact, tests, catalog

---

## 6. Caminho para Microserviços (Futura Evolução)

A migração é incremental, módulo a módulo:

1. **Cada módulo já tem seu próprio API project** com endpoints isolados
2. **Cada módulo já tem seu próprio DbContext** com schema isolado
3. **O ApiHost compõe tudo hoje**; amanhã cada API pode virar seu próprio host
4. **Substituir InProcessEventBus** por RabbitMQ/Kafka
5. **Adicionar API Gateway** (Kong/YARP) na frente
6. **Zero refactoring de boundaries** — as fronteiras já estão definidas

---

## 7. Módulos do Sistema

| # | Módulo | Bounded Context | Aggregate Roots Principais |
|---|--------|----------------|---------------------------|
| 1 | Identity | Autenticação e autorização | User, Role, Session |
| 2 | Licensing | Licenciamento e capacidades | License, HardwareBinding |
| 3 | EngineeringGraph | Grafo de dependências | ApiAsset, ConsumerRelationship |
| 4 | DeveloperPortal | Portal do desenvolvedor | (Read-model, sem aggregates) |
| 5 | Contracts | Gestão de contratos OpenAPI | ContractVersion, ContractDiff |
| 6 | **ChangeIntelligence** | **CORE — Inteligência de mudança** | **Release, BlastRadiusReport** |
| 7 | RulesetGovernance | Regras e linting | Ruleset, LintExecution |
| 8 | Workflow | Fluxos de aprovação | WorkflowTemplate, WorkflowInstance |
| 9 | Promotion | Promoção entre ambientes | PromotionRequest, PromotionGate |
| 10 | RuntimeIntelligence | Observabilidade runtime | RuntimeSnapshot, DriftFinding |
| 11 | CostIntelligence | Inteligência de custos | CostSnapshot, CostAttribution |
| 12 | AiOrchestration | IA interna | AiConversation, GeneratedTestArtifact |
| 13 | ExternalAi | IA externa (LLMs) | ExternalAiConsultation, KnowledgeCapture |
| 14 | Audit | Auditoria e compliance | AuditEvent, AuditChainLink |
