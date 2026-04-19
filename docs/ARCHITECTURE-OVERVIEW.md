# ARCHITECTURE-OVERVIEW.md — NexTraceOne

> **Versão:** 2.0 — Março 2026
> **Escopo:** Visão completa da arquitetura técnica da plataforma NexTraceOne.
> **Audiência:** Engineers, Tech Leads, Architects.

---

## 1. Estilo Arquitetural

O NexTraceOne é um **Monólito Modular** (_Modular Monolith_) com os seguintes princípios fundacionais:

| Princípio | Aplicação |
|-----------|-----------|
| **Domain-Driven Design (DDD)** | Cada módulo é um Bounded Context com linguagem ubíqua própria |
| **Clean Architecture** | Dependências apontam para o interior: Domain ← Application ← Infrastructure ← API |
| **CQRS** | Commands (escrita) e Queries (leitura) são separados em handlers distintos via MediatR |
| **Result Pattern** | Nenhum handler lança exceção para fluxos de negócio — `Result<T>` é o contrato de retorno |
| **Strongly Typed IDs** | Cada entidade tem um ID próprio herdando de `TypedIdBase` — impede troca acidental entre IDs |
| **Multi-Tenancy** | `ICurrentTenant` injetado em toda a stack; `TenantRlsInterceptor` aplica RLS por query |
| **Outbox Pattern** | Eventos de domínio publicados via tabela de outbox para garantia de entrega |

O produto nasce como monólito modular deliberado, sem microserviços prematuros. A separação entre módulos via bounded contexts garante que a decomposição futura, se necessária, ocorra de forma cirúrgica sem reescritas.

---

## 2. Visão de Alto Nível

```
┌─────────────────────────────────────────────────────────────────────────┐
│                        NexTraceOne Platform                              │
│                                                                          │
│  ┌──────────────────┐   ┌──────────────────────┐   ┌─────────────────┐  │
│  │  React SPA       │   │  NexTraceOne.ApiHost  │   │  Ingestion.Api  │  │
│  │  (Vite/React 19) │──▶│  ASP.NET Core 10     │   │  ASP.NET Core   │  │
│  │  Port 5173       │   │  Port 8080/8443       │   │  Port 8090      │  │
│  └──────────────────┘   └──────────┬────────────┘   └────────┬────────┘  │
│                                    │                          │           │
│                     ┌──────────────┴──────────────────────────┘           │
│                     ▼                                                     │
│  ┌─────────────────────────────────────────────────────────────────────┐ │
│  │                   Módulos (Bounded Contexts)                        │ │
│  │  IdentityAccess · Catalog · ChangeGovernance · OperationalIntel    │ │
│  │  AIKnowledge · AuditCompliance · Governance · Integrations         │ │
│  │  Knowledge · Notifications · Configuration · ProductAnalytics      │ │
│  └─────────────────────────────────────────────────────────────────────┘ │
│                     │                                                     │
│                     ▼                                                     │
│  ┌─────────────────────────────────────────────────────────────────────┐ │
│  │                      PostgreSQL (28 DbContexts)                     │ │
│  └─────────────────────────────────────────────────────────────────────┘ │
│                                                                          │
│  ┌────────────────────┐   ┌───────────┐   ┌──────────────────────────┐  │
│  │  BackgroundWorkers │   │  Ollama   │   │  Observability Stack     │  │
│  │  (Quartz.NET)      │   │  (LLM)    │   │  OTel Collector +        │  │
│  └────────────────────┘   └───────────┘   │  ClickHouse / Elastic    │  │
│                                            └──────────────────────────┘  │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## 3. Bounded Contexts (Módulos)

O NexTraceOne possui **12 bounded contexts** organizados em `src/modules/`. Cada módulo tem sua própria stack completa de camadas e DbContext(s) isolado(s).

### 3.1 Tabela de Módulos

| Módulo | Pasta | Responsabilidade Principal |
|--------|-------|---------------------------|
| **IdentityAccess** | `identityaccess/` | Autenticação, autorização, SSO (OIDC/SAML), sessões, RBAC, Break Glass, JIT access |
| **Catalog** | `catalog/` | Service Catalog, Contracts (REST/SOAP/Event), Developer Portal, Templates, Legacy Assets, Scorecard |
| **ChangeGovernance** | `changegovernance/` | Change Intelligence, Blast Radius, Approval Workflows, Promotion Governance, Release Calendar |
| **OperationalIntelligence** | `operationalintelligence/` | Incidents, Reliability (SLOs/SLAs), Automation, Runtime Intelligence, Telemetry Store, Cost Intelligence |
| **AIKnowledge** | `aiknowledge/` | AI Assistant, Agentes, Model Registry, Governance de IA, External AI, Orquestração |
| **AuditCompliance** | `auditcompliance/` | Trilhas de auditoria, compliance, evidências, campanhas de review |
| **Governance** | `governance/` | Reports, Risk Center, Policies, FinOps, Executive Views |
| **Integrations** | `integrations/` | ConnectorHub, IngestionSources, Webhooks, Legacy Telemetry, Integration Context |
| **Knowledge** | `knowledge/` | Knowledge Hub, Documentos, Notas Operacionais, Knowledge Graph, Runbooks |
| **Notifications** | `notifications/` | Canais de notificação, preferências, disparos por evento |
| **Configuration** | `configuration/` | Parametrização da plataforma, feature flags, políticas por tenant/ambiente |
| **ProductAnalytics** | `productanalytics/` | Métricas de adoção, journeys, friction indicators, Value Score, TTFV |

### 3.2 Sub-contextos com DbContexts próprios

Alguns módulos possuem múltiplos sub-contextos internos com DbContexts independentes. O módulo **Catalog** é o mais complexo:

| Sub-contexto | DbContext |
|---|---|
| Catalog Core | `CatalogDbContext` |
| Contracts | `ContractsDbContext` |
| Developer Portal | `DeveloperPortalDbContext` |
| Templates | `TemplatesDbContext` |
| Developer Experience | `DeveloperExperienceDbContext` |
| Legacy Assets | `LegacyAssetsDbContext` |
| Graph | `CatalogGraphDbContext` |
| Dependency Governance | `DependencyGovernanceDbContext` |

O módulo **ChangeGovernance** também possui múltiplos sub-contextos:

| Sub-contexto | DbContext |
|---|---|
| Change Intelligence | `ChangeIntelligenceDbContext` |
| Workflow | `WorkflowDbContext` |
| Promotion | `PromotionDbContext` |
| Ruleset Governance | `RulesetGovernanceDbContext` |

E o módulo **OperationalIntelligence**:

| Sub-contexto | DbContext |
|---|---|
| Incidents | `IncidentDbContext` |
| Reliability | `ReliabilityDbContext` |
| Automation | `AutomationDbContext` |
| Runtime Intelligence | `RuntimeIntelligenceDbContext` |
| Telemetry Store | `TelemetryStoreDbContext` |
| Cost Intelligence | `CostIntelligenceDbContext` |

O módulo **AIKnowledge** possui três sub-contextos:

| Sub-contexto | DbContext |
|---|---|
| Governance | `AiGovernanceDbContext` |
| External AI | `ExternalAiDbContext` |
| Orchestration | `AiOrchestrationDbContext` |

**Total: 28 DbContexts** distribuídos pelos 12 módulos.

---

## 4. Estrutura de Camadas por Módulo

Cada módulo segue rigorosamente esta estrutura de projetos (assemblies):

```
src/modules/{módulo}/
├── NexTraceOne.{Módulo}.Domain/          # Entidades, Value Objects, Domain Events
├── NexTraceOne.{Módulo}.Application/     # Commands, Queries, Handlers, Abstractions
├── NexTraceOne.{Módulo}.Infrastructure/  # DbContext, Repositories, Adapters externos
├── NexTraceOne.{Módulo}.API/             # Minimal API Endpoints (thin)
└── NexTraceOne.{Módulo}.Contracts/       # DTOs e contratos partilhados com outros módulos
```

### 4.1 Regras de Dependência entre Camadas

```
Domain          ← não depende de nada interno ao projeto
Application     ← depende de Domain + BuildingBlocks.Application
Infrastructure  ← depende de Application + BuildingBlocks.Infrastructure
API             ← depende de Application (via MediatR) + Contracts
Contracts       ← não depende de Domain (apenas tipos primitivos)
```

**A Infrastructure nunca é referenciada pela API diretamente** — a injeção de dependência do `Program.cs` (ApiHost) monta tudo via `Add{Módulo}Infrastructure()`.

### 4.2 Estrutura Interna do Application

```
NexTraceOne.{Módulo}.Application/
├── Abstractions/      # Interfaces de repositório e serviços usados pelos handlers
├── Features/          # Um diretório por feature (Command ou Query)
│   ├── CreateXxx/     # Command + Handler + Validator num único ficheiro estático
│   └── GetXxx/        # Query + Handler num único ficheiro estático
└── DependencyInjection.cs
```

---

## 5. Request Flow — Do HTTP ao Domínio

O fluxo completo de uma requisição HTTP percorre as seguintes camadas:

```
HTTP Request
    │
    ▼
[ASP.NET Core Middlewares]
    Compression → HTTPS Redirect → Rate Limiter → Security Headers
    → Exception Handler → Authentication → Tenant Resolution → Authorization
    │
    ▼
[Minimal API Endpoint]  ←── Controller thin: valida parâmetros básicos, cria Command/Query
    │
    ▼
[MediatR.Send(command)]
    │
    ▼ Pipeline Behaviors (ordenados)
    ├── LoggingBehavior          → loga entrada/saída do handler com correlationId
    ├── PerformanceBehavior      → mede tempo de execução, emite alerta se > threshold
    ├── TenantIsolationBehavior  → garante que ICurrentTenant está resolvido
    ├── ValidationBehavior       → executa FluentValidation; rejeita se houver erros
    └── TransactionBehavior      → abre transação antes do handler (apenas Commands)
    │
    ▼
[Command/Query Handler]
    │
    ├── Acede a IRepository (via abstração da Application layer)
    │       │
    │       ▼
    │   [Repository Implementation] (Infrastructure layer)
    │       │
    │       ▼
    │   [DbContext] (NexTraceDbContextBase)
    │       Interceptors: AuditInterceptor + TenantRlsInterceptor + EncryptionInterceptor
    │       │
    │       ▼
    │   [PostgreSQL]
    │
    ├── Manipula entidades de Domain (regras de negócio)
    ├── Registra Domain Events na entidade (via AddDomainEvent)
    └── Retorna Result<T>
    │
    ▼
[TransactionBehavior] → CommitAsync → publica Domain Events no Outbox
    │
    ▼
[Minimal API Endpoint] → mapeia Result<T> para IResult (200/400/404/409/500)
    │
    ▼
HTTP Response (JSON)
```

---

## 6. BuildingBlocks

Os BuildingBlocks são assemblies partilhados por todos os módulos. Não contêm lógica de negócio.

```
src/building-blocks/
├── NexTraceOne.BuildingBlocks.Core/            # Result<T>, Error, TypedIdBase, primitivas
├── NexTraceOne.BuildingBlocks.Application/     # ICommand, IQuery, Behaviors, ICurrentTenant
├── NexTraceOne.BuildingBlocks.Infrastructure/  # NexTraceDbContextBase, Interceptors, Outbox
├── NexTraceOne.BuildingBlocks.Security/        # JWT, CSRF, RBAC, Tenant Resolution
└── NexTraceOne.BuildingBlocks.Observability/   # OTel, Serilog, Activity Sources, Metrics
```

### 6.1 CQRS Abstractions

```csharp
// Query — somente leitura, sem efeitos colaterais
public interface IQuery<TResponse> : IRequest<Result<TResponse>> { }
public interface IQueryHandler<TQuery, TResponse>
    : IRequestHandler<TQuery, Result<TResponse>> where TQuery : IQuery<TResponse> { }

// Command sem resposta tipada
public interface ICommand : IRequest<Result<Unit>> { }
public interface ICommandHandler<TCommand>
    : IRequestHandler<TCommand, Result<Unit>> where TCommand : ICommand { }

// Command com resposta (ex.: retorna o ID criado)
public interface ICommand<TResponse> : IRequest<Result<TResponse>> { }
public interface ICommandHandler<TCommand, TResponse>
    : IRequestHandler<TCommand, Result<TResponse>> where TCommand : ICommand<TResponse> { }
```

### 6.2 MediatR Pipeline Behaviors

Os behaviors são registados globalmente e executam na seguinte ordem para cada request:

| Behavior | Tipo Aplicado | Função |
|---|---|---|
| `LoggingBehavior` | Commands + Queries | Loga entrada/saída, correlationId, usuário, tenant |
| `PerformanceBehavior` | Commands + Queries | Emite warning se > 500ms |
| `TenantIsolationBehavior` | Commands + Queries | Garante ICurrentTenant válido |
| `ValidationBehavior` | Commands + Queries | Executa FluentValidation; retorna `ErrorType.Validation` se falhar |
| `TransactionBehavior` | Commands apenas | Abre/fecha transação; commit apenas se `Result.IsSuccess` |

### 6.3 Result Pattern

```csharp
// Sucesso
return Result<Guid>.Success(entity.Id.Value);

// Falha controlada (sem exceção)
return Error.NotFound("service.not_found", "Service does not exist.");
return Error.Conflict("service.already_exists", "A service with this name already exists.");
return Error.Validation("name.required", "Name is required.");

// Conversões implícitas disponíveis
Result<Guid> result = entity.Id.Value;  // sucesso implícito
Result<Guid> result = Error.NotFound(…); // falha implícita
```

### 6.4 Strongly Typed IDs

```csharp
// Definição — um por aggregate root
public sealed record ServiceId(Guid Value) : TypedIdBase(Value);
public sealed record ContractId(Guid Value) : TypedIdBase(Value);

// Uso
var id = new ServiceId(Guid.NewGuid());
// ou TypedIdBase.NewId() para gerar novo Guid
```

---

## 7. Multi-Tenancy e Isolamento de Dados

### 7.1 ICurrentTenant

O `ICurrentTenant` é resolvido pelo `TenantResolutionMiddleware` a partir do JWT claim `tenant_id`. Está disponível em toda a stack via DI.

```csharp
public interface ICurrentTenant
{
    Guid Id { get; }
    string Slug { get; }
    string Name { get; }
    bool IsActive { get; }
    bool HasCapability(string capability);
}
```

### 7.2 TenantRlsInterceptor

O `TenantRlsInterceptor` executa `SET app.current_tenant_id = '{tenantId}'` antes de cada query no PostgreSQL. Todas as entidades com `TenantId` possuem filtros globais no EF Core que filtram automaticamente pelo tenant ativo.

**Regra absoluta:** Toda entidade de domínio que precise de isolamento multi-tenant deve:
1. Ter propriedade `TenantId` do tipo `Guid`
2. Ter query filter configurado na `IEntityTypeConfiguration<T>`
3. O `TenantRlsInterceptor` cuida do resto automaticamente

### 7.3 NexTraceDbContextBase

Todos os DbContexts herdam de `NexTraceDbContextBase` que configura automaticamente:

- **AuditInterceptor**: preenche `CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy`
- **TenantRlsInterceptor**: aplica RLS por tenant em cada query
- **EncryptionInterceptor**: cifra/decifra campos marcados com `[EncryptedField]` usando AES-256-GCM
- **OutboxInterceptor**: captura Domain Events e persiste no outbox antes do commit

```csharp
public sealed class KnowledgeDbContext(
    DbContextOptions<KnowledgeDbContext> options,
    ICurrentTenant tenant,
    ICurrentUser user,
    IDateTimeProvider clock)
    : NexTraceDbContextBase(options, tenant, user, clock)
{
    protected override Assembly ConfigurationsAssembly => typeof(KnowledgeDbContext).Assembly;
    protected override string OutboxTableName => "knowledge_outbox_messages";

    public DbSet<KnowledgeDocument> Documents { get; set; }
    public DbSet<OperationalNote> OperationalNotes { get; set; }
    // …
}
```

---

## 8. Comunicação entre Módulos

### 8.1 Regra fundamental

**Módulos nunca acedem diretamente ao DbContext de outro módulo.**

A comunicação entre módulos é feita exclusivamente via:

1. **Integration Events** publicados no event bus (Outbox Pattern)
2. **Contratos partilhados** (`*.Contracts` assembly) — apenas DTOs, sem lógica
3. **Interfaces de cross-module** definidas no Application como abstrações

### 8.2 Fluxo de Comunicação via Eventos

```
Módulo A (Publisher)                     Módulo B (Consumer)
─────────────────────                    ───────────────────
Command Handler executa
    └─ entity.AddDomainEvent(evt)
         │
TransactionBehavior.CommitAsync()
    └─ DomainEvent → OutboxMessage (DB)
         │
         ▼
OutboxProcessorJob (Quartz, 5s)
    └─ lê OutboxMessage
    └─ serializa para IntegrationEvent
    └─ publica no EventBus (in-process MVP)
                                              ▼
                                     IIntegrationEventHandler<TEvent>
                                          (no módulo B)
                                     atualiza seu próprio estado
```

### 8.3 Event Bus

No MVP1, o event bus é **in-process** (publicação síncrona no mesmo processo). A arquitetura de Outbox garante que, quando evoluir para mensageria externa (RabbitMQ, Kafka), a mudança seja transparente para os handlers.

### 8.4 Contratos Partilhados

```
NexTraceOne.{Módulo}.Contracts/
└── {NomeContrato}Dto.cs    # Apenas records com tipos primitivos
```

Outro módulo pode referenciar `*.Contracts` de outro módulo para consumir seus DTOs — mas **nunca** pode referenciar `*.Domain`, `*.Application` ou `*.Infrastructure` de outro módulo.

### 8.5 Interfaces de Módulo Cross-Module

Para casos onde um módulo precisa consultar funcionalidade de outro sem evento:

```csharp
// Definido em Knowledge.Contracts
public interface IKnowledgeModule
{
    Task<int> GetDocumentCountForServiceAsync(Guid serviceId, CancellationToken ct);
}

// Implementado em Knowledge.Infrastructure
// Registado no DI pela Knowledge.Infrastructure.DependencyInjection
// Consumido pelo Governance sem saber nada de KnowledgeDbContext
```

---

## 9. Segurança

### 9.1 Autenticação

- JWT Bearer com httpOnly cookies para o access token
- Refresh token em memória no frontend (não persistido)
- OIDC/SAML suportados para login federado
- API Keys para integrações externas (Ingestion.Api)

### 9.2 CSRF

- Double-submit cookie pattern implementado no `CsrfTokenValidator`
- Todas as mutations (POST, PUT, PATCH, DELETE) validam o header `X-CSRF-Token`
- O frontend injeta o token via Axios interceptor

### 9.3 Autorização

- RBAC baseado em permissões (`permission:resource:action`)
- `IPermissionAuthorizationHandler` valida permissões por módulo/ação
- Authorization é sempre no backend — frontend reflete, nunca garante

### 9.4 Multi-Tenancy Security

- `TenantResolutionMiddleware` resolve o tenant antes de qualquer handler
- Tenants não podem aceder dados de outros tenants — garantido por RLS
- Break Glass Access e JIT Access auditados via AuditCompliance

---

## 10. Outbox Pattern e Garantia de Entrega

O `OutboxProcessorJob` (Quartz.NET, intervalo de 5 segundos) processa mensagens pendentes:

1. Lê mensagens com `ProcessedAt IS NULL` ordenadas por `CreatedAt`
2. Publica no event bus em ordem
3. Marca como processada com `ProcessedAt = now()`
4. Em caso de falha, registra `LastError` e tenta novamente na próxima execução

Cada DbContext tem sua própria tabela de outbox (configurada via `OutboxTableName`) para evitar contenção entre módulos que partilham o mesmo schema PostgreSQL.

---

## 11. Como Adicionar um Novo Bounded Context

Siga estes passos para criar um novo módulo do zero:

### Passo 1 — Criar estrutura de projetos

```bash
mkdir -p src/modules/meumodulo

# Criar os 5 assemblies
dotnet new classlib -n NexTraceOne.MeuModulo.Domain        -o src/modules/meumodulo/NexTraceOne.MeuModulo.Domain
dotnet new classlib -n NexTraceOne.MeuModulo.Application   -o src/modules/meumodulo/NexTraceOne.MeuModulo.Application
dotnet new classlib -n NexTraceOne.MeuModulo.Infrastructure -o src/modules/meumodulo/NexTraceOne.MeuModulo.Infrastructure
dotnet new classlib -n NexTraceOne.MeuModulo.API           -o src/modules/meumodulo/NexTraceOne.MeuModulo.API
dotnet new classlib -n NexTraceOne.MeuModulo.Contracts     -o src/modules/meumodulo/NexTraceOne.MeuModulo.Contracts
```

### Passo 2 — Referenciar BuildingBlocks

```xml
<!-- Domain.csproj: nenhuma referência de BuildingBlocks obrigatória -->
<!-- Application.csproj -->
<PackageReference Include="NexTraceOne.BuildingBlocks.Application" />
<ProjectReference Include="..\..\..\..\building-blocks\NexTraceOne.BuildingBlocks.Application\" />

<!-- Infrastructure.csproj -->
<PackageReference Include="NexTraceOne.BuildingBlocks.Infrastructure" />
```

### Passo 3 — Criar entidade de domínio com ID tipado

```csharp
// Domain/Entities/MeuAggregate.cs
public sealed record MeuAggregateId(Guid Value) : TypedIdBase(Value);

public sealed class MeuAggregate : AggregateRoot
{
    public MeuAggregateId Id { get; private set; }
    public Guid TenantId { get; private set; }
    public string Name { get; private set; }

    private MeuAggregate() { } // EF Core

    public static MeuAggregate Create(string name, Guid tenantId)
    {
        Guard.AgainstNullOrWhiteSpace(name, nameof(name));
        var entity = new MeuAggregate
        {
            Id = new MeuAggregateId(Guid.NewGuid()),
            TenantId = tenantId,
            Name = name
        };
        entity.AddDomainEvent(new MeuAggregateCreatedDomainEvent(entity.Id.Value));
        return entity;
    }
}
```

### Passo 4 — Criar DbContext

```csharp
// Infrastructure/Persistence/MeuModuloDbContext.cs
public sealed class MeuModuloDbContext(
    DbContextOptions<MeuModuloDbContext> options,
    ICurrentTenant tenant, ICurrentUser user, IDateTimeProvider clock)
    : NexTraceDbContextBase(options, tenant, user, clock)
{
    protected override Assembly ConfigurationsAssembly => typeof(MeuModuloDbContext).Assembly;
    protected override string OutboxTableName => "meumodulo_outbox_messages";

    public DbSet<MeuAggregate> MeusAggregates { get; set; }
}
```

### Passo 5 — Criar DependencyInjection

```csharp
// Infrastructure/DependencyInjection.cs
public static class DependencyInjection
{
    public static IServiceCollection AddMeuModuloInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetRequiredConnectionString("MeuModuloDatabase", "NexTraceOne");

        services.AddDbContext<MeuModuloDbContext>((sp, opts) =>
            opts.UseNpgsql(connectionString)
                .AddInterceptors(
                    sp.GetRequiredService<AuditInterceptor>(),
                    sp.GetRequiredService<TenantRlsInterceptor>()));

        services.AddScoped<IMeuAggregateRepository, MeuAggregateRepository>();
        return services;
    }
}
```

### Passo 6 — Registar no ApiHost

```csharp
// src/ApiHost/Program.cs
builder.Services.AddMeuModuloInfrastructure(builder.Configuration);
```

### Passo 7 — Criar migration

```bash
dotnet ef migrations add Initial \
  --project src/modules/meumodulo/NexTraceOne.MeuModulo.Infrastructure \
  --startup-project src/ApiHost/NexTraceOne.ApiHost \
  --context MeuModuloDbContext
```

---

## 12. Regras Cross-Module — O que NUNCA fazer

| ❌ Proibido | ✅ Correto |
|---|---|
| Injetar `CatalogDbContext` no módulo ChangeGovernance | Usar Integration Event ou `ICatalogModule` interface |
| Importar `NexTraceOne.Catalog.Domain` no módulo Knowledge | Usar `NexTraceOne.Catalog.Contracts` apenas |
| Query direta na tabela de outro módulo via SQL cru | Publicar/consumir Integration Event |
| Colocar lógica de negócio no Controller/Endpoint | Usar Command/Query Handler via MediatR |
| Retornar exceção para validação de negócio | Usar `Result<T>` com `Error.Validation(…)` |
| Usar `DateTime.Now` | Usar `IDateTimeProvider.UtcNow` |

---

## 13. Observabilidade Interna

O NexTraceOne instrumenta sua própria stack via OpenTelemetry:

```
Activity Sources registados:
  - "NexTraceOne.Commands"    → span por Command executado
  - "NexTraceOne.Queries"     → span por Query executada
  - "NexTraceOne.Events"      → span por evento publicado/consumido
  - "NexTraceOne.ExternalHttp"→ span por chamada HTTP externa
  - "NexTraceOne.TelemetryPipeline" → pipeline de ingestão
```

Traces fluem via OTLP para o provider configurado (`ClickHouse` por padrão, `Elastic` como alternativa).

---

## 14. Referências Cruzadas

| Tópico | Documento |
|---|---|
| Boundaries por módulo | `docs/ARCHITECTURE-OVERVIEW.md` |
| Guia de implementação de módulos | `docs/BACKEND-MODULE-GUIDELINES.md` |
| Estratégia de testes | `docs/TESTING-STRATEGY.md` |
| Arquitetura de dados | `docs/DATA-ARCHITECTURE.md` |
| Deploy e operação | `docs/DEPLOYMENT-ARCHITECTURE.md` |
| Frontend | `docs/FRONTEND-ARCHITECTURE.md` |
| IA e governança | `docs/AI-ARCHITECTURE.md` |
| Segurança | `docs/SECURITY-ARCHITECTURE.md` |

---

*Última atualização: Março 2026.*
*Este documento substitui a versão anterior de 18 linhas e reflete a arquitetura real do produto.*
