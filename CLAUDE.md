# CLAUDE.md — NexTraceOne Platform

Guia completo para o Claude Code trabalhar neste repositório com máxima eficiência, qualidade e aderência aos padrões do projeto.

---

## Parte 1 — Diretrizes Comportamentais

> Estas regras têm precedência sobre qualquer comportamento padrão do LLM.

### 1.1 Pense Antes de Codificar

**Não assuma. Não esconda confusão. Exponha os trade-offs.**

Antes de implementar qualquer coisa:
- Declare explicitamente suas premissas. Se incerto, pergunte.
- Se existem múltiplas interpretações, apresente-as — não escolha silenciosamente.
- Se existe uma abordagem mais simples, diga. Questione quando justificado.
- Se algo não está claro, pare. Nomeie o que está confuso. Pergunte.
- Para tarefas de múltiplos passos, defina um plano curto com critérios de verificação:
  ```
  1. [Passo] → verificar: [critério]
  2. [Passo] → verificar: [critério]
  ```

### 1.2 Simplicidade Primeiro

**Código mínimo que resolve o problema. Nada especulativo.**

- Nenhuma feature além do que foi pedido.
- Nenhuma abstração para código de uso único.
- Nenhuma "flexibilidade" ou "configurabilidade" não solicitada.
- Nenhum tratamento de erros para cenários impossíveis.
- Se você escrever 200 linhas e pudesse ser 50, reescreva.

> Pergunta-teste: "Um engenheiro sênior diria que isso é complicado demais?" Se sim, simplifique.

### 1.3 Mudanças Cirúrgicas

**Toque apenas o que for necessário. Limpe apenas a própria bagunça.**

Ao editar código existente:
- Não "melhore" código adjacente, comentários ou formatação.
- Não refatore coisas que não estão quebradas.
- Combine o estilo existente, mesmo se faria diferente.
- Se notar código morto não relacionado, mencione — não apague.

Quando suas mudanças criam órfãos:
- Remova imports/variáveis/funções que SUAS mudanças tornaram inutilizáveis.
- Não remova código morto pré-existente a não ser que solicitado.

> Teste: cada linha alterada deve traçar diretamente à solicitação do usuário.

### 1.4 Execução Orientada a Metas

**Defina critérios de sucesso. Itere até verificar.**

Transforme tarefas em metas verificáveis:
- "Adicionar validação" → "Escrever testes para inputs inválidos, depois fazê-los passar"
- "Corrigir o bug" → "Escrever um teste que reproduza, depois fazê-lo passar"
- "Refatorar X" → "Garantir que os testes passam antes e depois"

Critérios de sucesso fortes permitem iterar de forma independente. Critérios fracos ("fazer funcionar") requerem clarificação constante.

---

## Parte 2 — Identidade e Visão do Projeto

### O que é NexTraceOne

NexTraceOne é uma **plataforma enterprise de Change Intelligence** — um monólito modular multi-tenant em SaaS que unifica:
- **Governança de mudanças** — scoring, blast radius, canary, promoção, rulesets
- **Catálogo de serviços** — registry, contratos, SBOM, portal do desenvolvedor, dependências
- **Inteligência operacional** — incidentes, SLO/SLA, rastreamento de custo, runtime telemetria
- **IA Governance** — registro de modelos, runtime Ollama/OpenAI, orquestração SemanticKernel
- **Auditoria e compliance** — trilha imutável, frameworks de conformidade, assinaturas digitais
- **Identidade e acesso** — JWT multi-tenant, OIDC/SAML, RBAC, Break Glass

### Padrão Arquitetural: "Archon Pattern"

**Monólito Modular + Building Blocks transversais**, com:
- `DDD` — Domain-Driven Design por bounded context
- `Clean Architecture` — Domain → Application → Infrastructure → API
- `CQRS` via MediatR — comandos e consultas separados em arquivos por feature
- `Outbox Pattern` — consistência eventual entre módulos via PostgreSQL
- `Row-Level Security` — isolamento de tenant no banco via PostgreSQL RLS
- `Result<T> Pattern` — sem exceções para fluxos de negócio

### Planos SaaS

| Plano | Capabilities |
|-------|-------------|
| **Starter** | Básico — sem AI, sem multi-region |
| **Professional** | Catálogo completo, governança, notificações |
| **Enterprise** | Tudo — AI, multi-region, air_gapped, contract_studio |
| **Trial** | Professional + 4 teasers Enterprise (não multi_region/air_gapped) |
| **Sem licença** | Fallback para Enterprise (todos os capabilities ativos — dev/CI) |

---

## Parte 3 — Stack de Tecnologia

### Backend (.NET 10)

| Camada | Tecnologia | Versão |
|--------|-----------|--------|
| Runtime | .NET / ASP.NET Core | **10.0.100** |
| Linguagem | C# | Latest (`LangVersion=latest`) |
| ORM | Entity Framework Core + Npgsql | 10.x |
| Micro-ORM | Dapper | para queries de reporting/leitura |
| Mediator | MediatR | pipeline CQRS |
| Validação | FluentValidation | Validators por Command |
| Mapeamento | Mapster | (não AutoMapper) |
| GraphQL | HotChocolate | code-first, subscriptions in-memory |
| OpenAPI | Scalar.AspNetCore | UI em `/scalar` |
| Scheduling | Quartz.NET | background jobs |
| Streaming | Confluent.Kafka | opcional (`NullKafka` padrão) |
| AI/LLM | Microsoft.SemanticKernel | orquestração LLM |
| Vetores | Qdrant.Client | busca semântica |
| Cache | StackExchangeRedis + Memory | `IDistributedCache` |
| Resiliência | Microsoft.Extensions.Http.Resilience | retry + circuit breaker em 14 HttpClients |
| Observabilidade | OpenTelemetry (traces, métricas, logs) | OTLP exporter |
| Logging | Serilog | structured logs |
| Analytics | Elasticsearch (NEST) + ClickHouse | providers configuráveis |
| Segurança | BCrypt.Net-Next, JwtBearer | hashing + autenticação |
| Guards | Ardalis.GuardClauses | guard assertions |
| Exportação | PdfSharpCore + ClosedXML | PDF + Excel |
| Token counting | Microsoft.ML.Tokenizers | contagem de tokens LLM |

**Configurações globais (Directory.Build.props):**
- `Nullable=enable` — nullable types explícitos obrigatórios
- `TreatWarningsAsErrors=true` — zero warnings em CI
- `ImplicitUsings=enable` — usings automáticos
- `GenerateDocumentationFile=true` — XML docs gerados
- `AnalysisLevel=latest-recommended` — Roslyn analyzers ativos
- `EnforceCodeStyleInBuild=true` — `.editorconfig` aplicado no build

### Frontend (React 19 / TypeScript 5.9)

| Camada | Tecnologia | Versão |
|--------|-----------|--------|
| Framework | React | **19.x** |
| Linguagem | TypeScript | **5.9.x** |
| Build | Vite | **7.3.x** |
| Roteamento | react-router-dom | **7.x** |
| Estado assíncrono | TanStack Query (React Query) | **5.90.x** |
| Formulários | React Hook Form + Zod | **7.x + 4.x** |
| Estilização | Tailwind CSS | **4.x** |
| Componentes UI | Radix UI + componentes customizados | — |
| Ícones | Lucide React | **0.577.x** |
| Gráficos | Apache ECharts + Recharts | **6.x + 3.x** |
| Editor de código | Monaco Editor | **0.52.x** |
| HTTP Client | Axios | **1.16.x** |
| Testes unitários | Vitest + Testing Library | **4.x** |
| Testes E2E | Playwright | **1.58.x** |
| Mocking | MSW (Mock Service Worker) | **2.x** |
| i18n | i18next + react-i18next | **25.x** (pt, en, es, fr) |
| Toasts | Sonner | **2.x** |

**Estrutura do frontend** (`src/frontend/src/`):
```
api/         ← axios client centralizado, interceptors de token/CSRF
auth/        ← AuthContext, guards de rota
components/  ← 40+ componentes UI reutilizáveis (DataTable, Alert, Badge…)
features/    ← 19 módulos de feature (espelham bounded contexts)
hooks/       ← hooks customizados (useDebounce, useLocalStorage…)
i18n.ts      ← configuração i18next (4 idiomas)
lib/         ← utilitários de domínio
locales/     ← arquivos de tradução JSON
routes/      ← definições de rota
types/       ← tipos globais TypeScript
utils/       ← tokenStorage, formatters, helpers
design/      ← tokens de design system, variáveis CSS
```

**19 módulos frontend** (`src/frontend/src/features/`):
`ai-hub`, `audit-compliance`, `catalog`, `change-governance`, `configuration`, `contracts`, `governance`, `identity-access`, `integrations`, `knowledge`, `legacy-assets`, `notifications`, `observability`, `operational-intelligence`, `operations`, `platform-admin`, `product-analytics`, `saas`, `shared`

**Segurança do frontend (`api/client.ts`):**
- Access token em `sessionStorage` (nunca `localStorage`)
- Refresh token em memória
- CSRF token em header `X-Csrf-Token` (cookie `nxt_csrf`)
- `X-Tenant-Id` header injetado — backend valida via JWT claim primário
- Interceptor de refresh automático antes de forçar re-login
- Evento `CustomEvent('auth:session-expired')` desacopla AuthContext do client HTTP

### Banco de Dados & Infraestrutura

| Componente | Tecnologia | Versão |
|-----------|-----------|--------|
| Banco principal | PostgreSQL + pgvector | **16** |
| Busca semântica | pgvector extension | em Postgres |
| Observabilidade | Elasticsearch **ou** ClickHouse | configurável |
| Cache | Redis (opcional) ou in-process Memory | — |
| LLM local | Ollama (qwen3.5:9b padrão) | — |
| Proxy reverso | Nginx | container |
| Orquestração | Docker Compose (dev) / Kubernetes + Helm (prod) | — |

---

## Parte 4 — Estrutura da Solução

### Hosts (3 entrypoints)

```
src/platform/
├── NexTraceOne.ApiHost/          ← REST + GraphQL (porta 5000)
│   ├── Program.cs                   # registro de serviços + pipeline
│   ├── appsettings*.json            # 24 connection strings (por DbContext)
│   └── Preflight/Checks/            # 10 verificações pré-startup
│       ├── PostgreSqlPreflightCheck
│       ├── JwtSecretPreflightCheck
│       ├── ConnectionStringsPreflightCheck
│       ├── DiskSpacePreflightCheck
│       ├── RamPreflightCheck
│       ├── PortsPreflightCheck
│       ├── OllamaPreflightCheck
│       ├── SmtpPreflightCheck
│       ├── OtelCollectorPreflightCheck
│       └── CorsOriginsPreflightCheck
│
├── NexTraceOne.BackgroundWorkers/ ← Quartz.NET jobs + processadores Outbox
└── NexTraceOne.Ingestion.Api/    ← endpoint de ingestão de telemetria
```

**Pipeline de startup (ApiHost):**
1. `AssemblyIntegrityChecker.VerifyOrThrow()` (skip via `NEXTRACE_SKIP_INTEGRITY=true`)
2. Serilog configurado (`ConfigureNexTraceSerilog`)
3. Building blocks: EventBus, DbContext base, Observabilidade, Analytics, Security, Infrastructure
4. AirGap enforcement em todos os HttpClients (`AirGapHttpMessageHandler`)
5. Distributed cache (Redis ou Memory)
6. Preflight checks executados antes do `app.Run()`
7. Middleware pipeline: Serilog → CORS → Rate Limiting → Auth → `TenantResolutionMiddleware` → CSRF

### Building Blocks (5 bibliotecas transversais)

```
src/building-blocks/
├── BuildingBlocks.Core
│   └── AggregateRoot<T>, Entity<T>, AuditableEntity<T>, TypedIdBase,
│       Result<T>, Error, ErrorType, guard clauses
│
├── BuildingBlocks.Application
│   └── ICommand, IQuery, ICommandHandler, IQueryHandler,
│       ICurrentTenant, ICurrentUser, IDateTimeProvider,
│       IUnitOfWork, IEventBus, pipeline behaviors (5)
│
├── BuildingBlocks.Infrastructure
│   └── NexTraceDbContextBase, TenantRlsInterceptor, AuditInterceptor,
│       EncryptionInterceptor, OutboxMessage, ModuleOutboxProcessorJob<T>,
│       Dapper reporting, Elasticsearch writer, HTTP client configurável,
│       IDeadLetterRepository
│
├── BuildingBlocks.Observability
│   └── OpenTelemetry wiring (traces/métricas/logs), Serilog,
│       health checks, ClickHouse/Elasticsearch providers,
│       políticas de retenção (hot/warm/cold)
│
└── BuildingBlocks.Security
    └── TenantResolutionMiddleware, CurrentTenantAccessor,
        JWT generation, OIDC/SAML, RBAC, Rate Limiting,
        Break Glass, assembly integrity, CSRF/HSTS/CSP
```

---

## Parte 5 — Os 12 Bounded Contexts (Módulos)

Cada módulo tem exatamente **5 projetos** (exceto aiknowledge que tem 6):

```
NexTraceOne.<Module>.Domain
NexTraceOne.<Module>.Application
NexTraceOne.<Module>.Contracts
NexTraceOne.<Module>.Infrastructure
NexTraceOne.<Module>.API
```

| Módulo | Prefixo de tabela | Subdomínio | Destaque técnico |
|--------|------------------|-----------|-----------------|
| **identityaccess** | `iam_` | Identity & Access | OIDC/SAML, TOTP (RFC 6238), Break Glass, provisioning automático de tenant |
| **catalog** | `cat_` / `ctr_` | Service Catalog | GraphQL HotChocolate, SBOM (JSONB), data contracts, developer portal, feature flags |
| **changegovernance** | `chg_` | Change Intelligence | GraphQL subscriptions, blast radius scoring, canary, promoção, rulesets |
| **governance** | `gov_` | Policy & Risk | SLO/SLA, correlação de incidentes, RCA, AlertEvaluationJob |
| **operationalintelligence** | `opi_` | Runtime Observability | incidentes, custo, reliability, SLO tracking |
| **aiknowledge** | `aik_` | AI Governance | SemanticKernel, model registry, Ollama/OpenAI routing, Qdrant, 6 projetos |
| **auditcompliance** | `aud_` | Audit & Compliance | trilha imutável, frameworks de conformidade, PDF/Excel export, assinaturas digitais |
| **integrations** | `int_` | External Systems | CI/CD webhooks, multi-cluster, Jira, Slack, GitHub, Dead Letter Queue |
| **knowledge** | `knw_` | Operational Knowledge | runbooks, documentação, busca semântica (pgvector) |
| **notifications** | `ntf_` | Notifications | email, Slack channels, templates, preferências por tenant |
| **configuration** | `cfg_` | Platform Configuration | feature flags por tenant/serviço, settings por ambiente |
| **productanalytics** | `pdt_` | Product Analytics | adoção de features, telemetria de produto |

**AIKnowledge tem 6 projetos:**
```
NexTraceOne.AIKnowledge.Domain
NexTraceOne.AIKnowledge.Application
NexTraceOne.AIKnowledge.Contracts
NexTraceOne.AIKnowledge.Infrastructure
NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.Repositories  ← extra
NexTraceOne.AIKnowledge.API
```

---

## Parte 6 — Build & Test

```bash
# Build da solução completa
dotnet build NexTraceOne.sln

# Build de um módulo isolado (mais rápido para iteração)
dotnet build src/modules/identityaccess/NexTraceOne.IdentityAccess.API/NexTraceOne.IdentityAccess.API.csproj

# Todos os testes unitários (exclui E2E, Selenium, Integration que requerem infra)
dotnet test --filter "FullyQualifiedName!~E2E&FullyQualifiedName!~Selenium&FullyQualifiedName!~IntegrationTests"

# Testes de um módulo específico
dotnet test tests/modules/identityaccess/NexTraceOne.IdentityAccess.Tests/NexTraceOne.IdentityAccess.Tests.csproj

# Teste por nome exato
dotnet test --filter "FullyQualifiedName~CreateShould"

# Adicionar migration EF Core (executar da raiz do repo)
dotnet ef migrations add <MigrationName> \
  --project src/modules/identityaccess/NexTraceOne.IdentityAccess.Infrastructure \
  --startup-project src/platform/NexTraceOne.ApiHost \
  --context IdentityDbContext

# Aplicar migrations
dotnet ef database update \
  --project src/modules/identityaccess/NexTraceOne.IdentityAccess.Infrastructure \
  --startup-project src/platform/NexTraceOne.ApiHost \
  --context IdentityDbContext

# Contar DbContexts existentes
bash tools/count-dbcontexts.sh
```

**Frontend:**
```bash
cd src/frontend

npm install           # instalar dependências
npm run dev           # dev server Vite (porta 5173)
npm run build         # build de produção
npm run test          # Vitest (unit)
npm run test:e2e      # Playwright (mock backend)
npm run test:e2e-real # Playwright (backend real — requer ApiHost rodando)
npm run lint          # ESLint
```

**Secrets locais (primeira vez):**
```bash
dotnet user-secrets init --project src/platform/NexTraceOne.ApiHost
dotnet user-secrets set "Jwt:Secret" "your-local-dev-secret-minimum-32-chars" --project src/platform/NexTraceOne.ApiHost
dotnet user-secrets set "ConnectionStrings:NexTraceOne" "Host=localhost;Port=5432;Database=nextraceone;Username=nextraceone;Password=..." --project src/platform/NexTraceOne.ApiHost
```

**Variáveis de ambiente úteis:**
```bash
NEXTRACE_SKIP_INTEGRITY=true               # pular verificação de hash de assembly (dev/CI)
NEXTRACE_IGNORE_PENDING_MODEL_CHANGES=true # suprimir aviso de migrations pendentes (AIKnowledge)
NEXTRACEONE_CONNECTION_STRING=...          # override para design-time factories do EF
```

---

## Parte 7 — Padrão CQRS (Feature Pattern)

Cada handler vive em **um único arquivo** como classe estática com `Command`/`Query`, `Validator`, `Response` e `Handler` aninhados:

```csharp
// Caminho: src/modules/<m>/NexTraceOne.<M>.Application/<Subdomain>/Features/<Name>/<Name>.cs
public static class ActivateAccount
{
    // IPublicRequest → bypass do TenantIsolationBehavior (endpoints de auth/public)
    public sealed record Command(string Token, string Password) : ICommand<Response>, IPublicRequest;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Token).NotEmpty();
            RuleFor(x => x.Password).MinimumLength(8);
        }
    }

    public sealed record Response(bool Activated);

    internal sealed class Handler(
        IAccountActivationTokenRepository tokenRepository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider clock) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            // Result pattern — sem exceções para fluxos de negócio
            return new Response(true);            // implícito: Result.Success(...)
            // ou:
            return Error.NotFound("code", "msg"); // implícito: Result.Failure(...)
        }
    }
}
```

**Ordem do pipeline MediatR** (aplicada a cada request):
1. `LoggingBehavior` — logging estruturado de request/response
2. `PerformanceBehavior` — aviso se ultrapassar `NexTraceOne:PerformanceThresholdMs` (default 500ms)
3. `TenantIsolationBehavior` — rejeita sem tenant, a menos que `IPublicRequest`
4. `ValidationBehavior` — executa todos `IValidator<TRequest>` via FluentValidation
5. `TransactionBehavior` — comita `IUnitOfWork` após Commands bem-sucedidos (não Queries)

**Mapeamento Error → HTTP** (via `result.ToHttpResult(localizer)` nos endpoints):

| ErrorType | HTTP |
|-----------|------|
| `NotFound` | 404 |
| `Validation` / `Business` | 422 |
| `Conflict` | 409 |
| `Unauthorized` | 401 |
| `Forbidden` | 403 |
| `Security` | 500 |

---

## Parte 8 — Persistência e Isolamento de Tenant

### EF Core — DbContext Base

Todos os DbContexts de módulo estendem `NexTraceDbContextBase`. Esta base:
- Converte domain events de `AggregateRoot<T>` em linhas `OutboxMessage` no mesmo `SaveChanges`
- Aplica filtro global de soft-delete em `AuditableEntity<T>` (`IsDeleted == false`)
- Aplica `[EncryptedField]` convention (AES-256-GCM via `EncryptedStringConverter`)
- Tabelas com prefixo de módulo para evitar colisões (ex.: `iam_roles`, `aud_audit_events`)

### 24 Connection Strings

Cada DbContext tem sua própria connection string em `appsettings.json`. Em desenvolvimento local, todas apontam para o mesmo servidor PostgreSQL com diferentes pools. Em produção, podem ser bancos separados:

```
NexTraceOne (shared), IdentityDatabase, CatalogDatabase, ContractsDatabase,
DeveloperPortalDatabase, ChangeIntelligenceDatabase, WorkflowDatabase,
RulesetGovernanceDatabase, PromotionDatabase, IncidentDatabase,
CostIntelligenceDatabase, RuntimeIntelligenceDatabase, ReliabilityDatabase,
AuditDatabase, AiGovernanceDatabase, GovernanceDatabase, IntegrationsDatabase,
ProductAnalyticsDatabase, ExternalAiDatabase, AiOrchestrationDatabase,
AutomationDatabase, ConfigurationDatabase, KnowledgeDatabase, NotificationsDatabase
```

### Isolamento de Tenant (duas camadas)

1. **`TenantRlsInterceptor`** — antes de cada comando SQL: `SELECT set_config('app.current_tenant_id', @id, false)` → PostgreSQL RLS filtra as linhas automaticamente
2. **Repository-level filter** — todo método de leitura também adiciona `.Where(e => e.TenantId == currentTenant.Id)` como defesa em profundidade

> Background jobs (retention, recálculo de licenças) que rodam sem contexto de tenant ignoram intencionalmente o filtro de repositório.

### IDs Fortemente Tipados

```csharp
public sealed record MyEntityId(Guid Value) : TypedIdBase(Value)
{
    public static MyEntityId New() => new(Guid.NewGuid());
    public static MyEntityId From(Guid id) => new(id);
}

// SEMPRE use .Value ao comparar com Guid:
assets.Where(a => guids.Contains(a.Id.Value))   // ✓
assets.Where(a => guids.Contains(a.Id))          // ✗ erro de compilação
```

### Configuração de Entidade EF Core

```csharp
// Infrastructure/Persistence/Configurations/MyEntityConfiguration.cs
internal sealed class MyEntityConfiguration : IEntityTypeConfiguration<MyEntity>
{
    public void Configure(EntityTypeBuilder<MyEntity> builder)
    {
        builder.ToTable("mod_my_entities");   // prefixo de módulo OBRIGATÓRIO
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => MyEntityId.From(value));
    }
}
```

`AuditableEntity<T>` — `CreatedAt/By`, `UpdatedAt/By` e `IsDeleted` preenchidos automaticamente pelo `AuditInterceptor`. **Nunca atribua manualmente.**

---

## Parte 9 — DI de Módulo

```csharp
// Infrastructure/DependencyInjection.cs — padrão exato de todos os módulos
public static IServiceCollection AddMyModuleInfrastructure(
    this IServiceCollection services, IConfiguration configuration)
{
    services.AddBuildingBlocksInfrastructure(configuration);

    var cs = configuration.GetRequiredConnectionString("MyModuleDatabase", "NexTraceOne");

    services.AddDbContext<MyDbContext>((sp, opts) =>
        opts.UseNpgsql(cs)
            .AddInterceptors(
                sp.GetRequiredService<AuditInterceptor>(),
                sp.GetRequiredService<TenantRlsInterceptor>()));

    services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<MyDbContext>());
    services.AddScoped<IMyModuleUnitOfWork>(sp => sp.GetRequiredService<MyDbContext>());
    services.AddScoped<IMyEntityRepository, MyEntityRepository>();
    services.AddScoped<IMyModule, MyModuleService>(); // contrato cross-module público
    return services;
}
```

Tanto `ApiHost/Program.cs` quanto `BackgroundWorkers/Program.cs` chamam esses extension methods. `ModuleOutboxProcessorJob<TContext>` de cada DbContext é registrado em `BackgroundWorkers/Program.cs`.

---

## Parte 10 — Comunicação Entre Módulos

Módulos **NUNCA** acessam o DbContext uns dos outros. Apenas `Contracts` são compartilhados.

### Opção A — Integration Events (assíncrono via Outbox)

```csharp
// Publicador — no handler, após mutação de domínio:
await eventBus.PublishAsync(new MyIntegrationEvent(entityId, tenantId), cancellationToken);
// O evento é escrito em OutboxMessage pelo NexTraceDbContextBase.SaveChanges.
// ModuleOutboxProcessorJob lê e entrega aos subscribers.

// Subscriber — no DI de Infrastructure do módulo consumidor:
services.AddScoped<IIntegrationEventHandler<MyIntegrationEvent>, MyEventHandler>();
```

### Opção B — IXxxModule interface (síncrono, in-process)

```csharp
// Definido em:     src/modules/catalog/NexTraceOne.Catalog.Contracts/.../ICatalogGraphModule.cs
// Implementado em: NexTraceOne.Catalog.Infrastructure/.../CatalogGraphModuleService.cs
// Registrado em:   DI do Catalog Infrastructure
// Consumido em:    outros módulos via injeção de ICatalogGraphModule (nunca o DbContext)
```

---

## Parte 11 — Outbox & Background Jobs

`ModuleOutboxProcessorJob<TContext>` (Quartz.NET) — ciclo por módulo DbContext:
1. Adquire `pg_try_advisory_lock(key)` — pula se outra instância segura (multi-instance safe)
2. Lê até 50 linhas `OutboxMessage` onde `ProcessedAt IS NULL AND RetryCount < 5`
3. Deserializa e publica cada uma via `IEventBus`
4. Salva `ProcessedAt` atomicamente por mensagem
5. Após 5 falhas → Dead Letter Queue via `IDeadLetterRepository` (tabela `int_event_consumer_dead_letters`)
6. Libera o advisory lock no `finally`

Novos jobs vão em `src/platform/NexTraceOne.BackgroundWorkers/Jobs/` e são registrados como `AddHostedService<MyJob>()`.

**Jobs existentes:**
- `LicenseRecalculationJob` — a cada 15min, soma host units ativos por tenant
- `AlertEvaluationJob` — avalia `LicenseUtilization` (% vs IncludedHostUnits) + `AgentHeartbeatMissed` (cutoff por ThresholdValue minutos)

---

## Parte 12 — Adicionando Handlers e Endpoints

### Novo Handler (checklist)

1. Crie `src/modules/<m>/NexTraceOne.<M>.Application/<Subdomain>/Features/<Name>/<Name>.cs` com o static class pattern
2. Adicione `Validator` se o command recebe input do usuário
3. Se precisar de novo método de repositório: adicione à interface `IXxxRepository` (Application) e implemente na Infrastructure
4. Registre o repositório em `Infrastructure/DependencyInjection.cs` se novo
5. Adicione o endpoint em `API/Endpoints/`
6. Execute `dotnet ef migrations add` se houver mudança de schema

### Novo Endpoint

```csharp
internal static class MyEndpoints
{
    internal static void Map(RouteGroupBuilder group)
    {
        var g = group.MapGroup("/my-resource");

        // Endpoint autenticado (tenant obrigatório)
        g.MapPost("/", async (MyFeature.Command cmd, ISender sender, IErrorLocalizer l, CancellationToken ct) =>
        {
            var result = await sender.Send(cmd, ct);
            return result.ToCreatedResult($"/my-resource/{result.Value?.Id}", l);
        }).RequireAuthorization();

        // Endpoint público (sem tenant — Command implementa IPublicRequest)
        g.MapGet("/public/{token}", async ([AsParameters] MyPublicQuery query, ISender sender, IErrorLocalizer l, CancellationToken ct) =>
        {
            var result = await sender.Send(query, ct);
            return result.ToHttpResult(l);
        }).AllowAnonymous();
    }
}
```

---

## Parte 13 — Testes

### Convenções

- **Framework:** xUnit + FluentAssertions + NSubstitute
- **Namespace:** `NSubstitute.ExceptionExtensions` para `.ThrowsAsync()`
- **EF Core em testes:** `UseInMemoryDatabase` para repositórios
- **Test doubles:** `TestCurrentTenant`, `TestDateTimeProvider` (em `tests/modules/identityaccess/TestDoubles/`)
- **GlobalUsings.cs** por projeto de teste — imports comuns (`System`, `FluentAssertions`, `NSubstitute`, `Xunit`)

### Padrão de Teste de Handler

```csharp
[Fact]
public async Task Handle_ValidToken_ReturnsActivated()
{
    // Arrange
    var repo = Substitute.For<IMyRepository>();
    var unitOfWork = Substitute.For<IUnitOfWork>();
    var clock = new TestDateTimeProvider();
    var handler = new MyFeature.Handler(repo, unitOfWork, clock);

    repo.FindByTokenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
        .Returns(MyEntity.Create("valid-token"));

    // Act
    var result = await handler.Handle(new MyFeature.Command("valid-token", "Pass@123"), CancellationToken.None);

    // Assert
    result.IsSuccess.Should().BeTrue();
    result.Value.Should().NotBeNull();
    result.Value!.Activated.Should().BeTrue();
}
```

### Localização dos Testes

```
tests/
├── building-blocks/              ← BuildingBlocks.*.Tests (5 projetos)
├── modules/
│   ├── aiknowledge/              NexTraceOne.AIKnowledge.Tests
│   ├── auditcompliance/          NexTraceOne.AuditCompliance.Tests
│   ├── catalog/                  NexTraceOne.Catalog.Tests
│   ├── changegovernance/         NexTraceOne.ChangeGovernance.Tests
│   ├── configuration/            NexTraceOne.Configuration.Tests
│   ├── governance/               NexTraceOne.Governance.Tests
│   ├── identityaccess/           NexTraceOne.IdentityAccess.Tests
│   ├── integrations/             NexTraceOne.Integrations.Tests
│   ├── knowledge/                NexTraceOne.Knowledge.Tests
│   ├── notifications/            NexTraceOne.Notifications.Tests
│   ├── operationalintelligence/  NexTraceOne.OperationalIntelligence.Tests
│   └── productanalytics/         NexTraceOne.ProductAnalytics.Tests
└── platform/
    ├── NexTraceOne.IntegrationTests   ← requer infra (Postgres, etc.)
    ├── NexTraceOne.E2E.Tests
    ├── NexTraceOne.BackgroundWorkers.Tests
    └── NexTraceOne.CLI.Tests
```

---

## Parte 14 — SaaS / Licenciamento / Capabilities

```csharp
// Verificar capability no handler:
if (!currentTenant.HasCapability("contract_studio"))
    return Error.Forbidden("CapabilityRequired", "This feature requires the Contract Studio plan.");

// Verificar plano:
if (currentTenant.Plan == TenantPlan.Starter)
    return Error.Forbidden("PlanRequired", "Upgrade to Professional.");
```

Capabilities disponíveis em `TenantCapabilities.ForPlan(TenantPlan.X)`.
Tenant sem licença → fallback para Enterprise (todos os capabilities ativos — ideal para dev/CI).

---

## Parte 15 — IA & ML

### Runtime de LLM (appsettings.json)

```json
"AiRuntime": {
  "Ollama": {
    "BaseUrl": "http://localhost:11434",
    "DefaultChatModel": "qwen3.5:9b",
    "TimeoutSeconds": 120,
    "MaxRetries": 2,
    "Enabled": true
  },
  "OpenAI": {
    "ApiKey": "",
    "DefaultChatModel": "gpt-4o-mini",
    "DefaultTemperature": 0.3,
    "DefaultMaxTokens": 2048,
    "Enabled": false
  },
  "Routing": {
    "PreferredProvider": "ollama",
    "EnableDeterministicFallback": true,
    "FallbackPrefix": "[FALLBACK_PROVIDER_UNAVAILABLE]"
  }
}
```

### Stack AI no módulo aiknowledge

- **Microsoft.SemanticKernel** — orquestração de LLM, plugins, prompts
- **Microsoft.ML.Tokenizers** — contagem de tokens (cl100k_base)
- **Qdrant.Client** — busca semântica por embeddings
- **pgvector** — alternativa de vetores no PostgreSQL 16
- Model routing policy → tabela `aik_model_routing_policies`
- Agent execution plans → tabela `aik_agent_execution_plans` (steps em JSONB)
- Model prediction samples → tabela `aik_model_prediction_samples` (features em JSONB)

---

## Parte 16 — Observabilidade

```json
"Telemetry": {
  "ObservabilityProvider": "Elastic",
  "CollectionMode": "OpenTelemetryCollector",
  "Retention": { "hot": 7, "warm": 30, "cold": 365 }
}
```

**Stack:**
- **OpenTelemetry** — traces, métricas, logs (OTLP exporter)
- **Serilog** — logging estruturado (correlação por `TraceId`/`SpanId`)
- **Elasticsearch** — provider padrão (NEST client)
- **ClickHouse** — provider alternativo de analytics (opcional)
- **OpenTelemetry Collector** — coleta centralizada (docker-compose)
- **Health checks** — endpoint `/health` agregado

---

## Parte 17 — Segurança

- **JWT** — `Issuer: NexTraceOne`, `Audience: nextraceone-api`, access 60min / refresh 7 dias
- **OIDC** — Azure AD configurável via `OidcProviders.azure`
- **TOTP** — RFC 6238 (usa HMAC-SHA1 por especificação — não é bug, CA5350 suprimido)
- **CSRF** — cookie `nxt_csrf` + header `X-Csrf-Token` (validação no middleware)
- **Rate Limiting** — 6 políticas: Global, Auth, AuthSensitive, Ai, DataIntensive, Operations
- **PostgreSQL RLS** — `TenantRlsInterceptor` + políticas de RLS no banco
- **Assembly Integrity** — SHA-256 hash check no startup (skip via `NEXTRACE_SKIP_INTEGRITY=true`)
- **Break Glass** — acesso emergencial auditado via `BuildingBlocks.Security`
- **AirGap** — `AirGapHttpMessageHandler` aplicado a todos os HttpClients
- **CSP/HSTS/CORS** — configurado no middleware do ApiHost

---

## Parte 18 — Deployment & Infraestrutura

### Docker Compose (desenvolvimento local)

```bash
# Stack completa (Postgres, Elasticsearch, Ollama, Nginx, apps)
docker-compose -f docker-compose.yml -f docker-compose.override.yml up

# Apenas infraestrutura
docker-compose up postgres elasticsearch
```

**Variantes:**
- `docker-compose.yml` — base
- `docker-compose.override.yml` — overrides locais
- `docker-compose.staging.yml` — staging
- `docker-compose.production.yml` — produção (hardening, resource limits, Elasticsearch security)

**5 Dockerfiles:**
- `Dockerfile.apihost` — API principal
- `Dockerfile.workers` — background jobs
- `Dockerfile.ingestion` — ingestão de telemetria
- `Dockerfile.frontend` — React SPA + Nginx
- `Dockerfile.kubernetes` — multi-stage otimizado para K8s

### Kubernetes

```bash
helm install nextraceone deploy/kubernetes/helm/nextraceone/ \
  --set image.tag=latest \
  --namespace nextraceone
```

Ver `deploy/MIGRATION-GUIDE.md` para guia completo de deploy K8s.

### CI/CD (GitHub Actions — `.github/workflows/`)

| Workflow | Trigger | Objetivo |
|----------|---------|---------|
| `ci.yml` | push (main/develop/release), PR | Build + test backend & frontend |
| `security.yml` | push | SAST, dependency checks |
| `e2e.yml` | merge para main | Playwright E2E |
| `staging.yml` | push para develop | Deploy staging |
| `production.yml` | tag `release/*` | Deploy produção |
| `kubernetes-deploy.yml` | manual | Deploy K8s |
| `release-bundle.yml` | push main | GitHub Release artifact |
| `artifact-signing.yml` | push main | Assinaturas SHA-256 |

---

## Parte 19 — Ferramentas

```
tools/
├── NexTrace.Sdk/          ← cliente .NET para consumidores da API
├── NexTraceOne.CLI/       ← CLI para administração da plataforma
├── sdk-cli/               ← implementação CLI alternativa
└── ide-extensions/
    └── visualstudio/      ← extensão Visual Studio
```

Documentação da plataforma em `docs/` (71 arquivos, 20k+ linhas), incluindo:
- `docs/adr/` — Architecture Decision Records
- `docs/runbooks/` — runbooks operacionais
- `docs/sdk/` — referência do SDK
- `docs/observability/` — padrões de observabilidade

---

## Parte 20 — Convenção de Linguagem

| Contexto | Idioma |
|----------|--------|
| Código, identificadores, logs, exceções | **Inglês** |
| XML doc comments (`/// <summary>`) | **Português** |
| Comentários inline no código | **Português** |
| Texto de UI (labels, mensagens, tooltips) | **chaves i18n** — nunca strings hardcoded |
| Documentação (`docs/`) | **Português** |

---

## Parte 21 — Padrão Honest-Null

`IXxxReader` interfaces (para read models analíticos alimentados por dados cross-module) são **intencionalmente** registradas com implementações `NullXxxReader` que retornam coleções vazias. São **placeholders phase-gated** — não os trate como bugs.

**Regra de ouro:**
- `IXxxRepository` (CRUD/persistência) → sempre precisa de implementação real EF Core. Se ver `NullXxxRepository`, é um **bug**.
- `IXxxReader` (analytics/reporting) → `NullXxxReader` é válido enquanto a bridge cross-module não está construída.

---

## Parte 22 — Princípios de Design (2026)

### SOLID no contexto do projeto

| Princípio | Como se aplica |
|-----------|---------------|
| **SRP** | Cada handler faz uma coisa; cada Preflight check é uma classe; cada endpoint group é isolado |
| **OCP** | Novos behaviors do pipeline sem modificar o core; novos módulos sem alterar Building Blocks |
| **LSP** | `AuditableEntity<T>` substituível em qualquer lugar que aceite `Entity<T>` |
| **ISP** | `ICurrentTenant`, `ICurrentUser`, `IUnitOfWork` — interfaces pequenas e focadas |
| **DIP** | Handlers dependem de `IXxxRepository` (abstração), nunca de `DbContext` diretamente |

### Clean Architecture — regras de dependência

```
Domain        ← zero dependências externas (apenas primitivos C# e BuildingBlocks.Core)
Application   ← depende de Domain; define interfaces (IRepository, IUnitOfWork)
Infrastructure← implementa interfaces de Application; depende de EF Core, Npgsql, etc.
API           ← depende de Application (ISender MediatR); NUNCA de Infrastructure diretamente
```

> **Violação crítica a evitar:** API layer importando Infrastructure ou Domain.

### DDD — invariantes de domínio

- **Agregados** protegem suas invariantes: validações de negócio ficam em métodos de domínio, não em handlers
- **Value Objects** são imutáveis: use `sealed record`
- **Domain Events** são raised no agregado: `this.RaiseDomainEvent(new MyDomainEvent(...))`
- **Repositórios** operam na fronteira de agregado: um repositório por agregado raiz
- **Domain Services** para lógica que não pertence a um agregado específico

### Result Pattern — nunca lance exceções para fluxos de negócio

```csharp
// ✓ CORRETO — erro de negócio via Result
public async Task<Result<Response>> Handle(Command cmd, CancellationToken ct)
{
    var entity = await repository.FindByIdAsync(cmd.Id, ct);
    if (entity is null)
        return Error.NotFound("Entity.NotFound", $"Entity {cmd.Id} not found.");

    var domainResult = entity.DoSomething(cmd.Value);
    if (domainResult.IsFailure)
        return domainResult.Error;

    await unitOfWork.CommitAsync(ct);
    return new Response(entity.Id);
}

// ✗ ERRADO — throw para fluxo de negócio (reservado para falhas de infraestrutura)
throw new NotFoundException("Entity not found");
```

### Clean Code — regras práticas

- Nomes revelam intenção: `GetActiveTenantsAsync` > `GetTenants`
- Sem "magic numbers" — use constantes nomeadas ou valores de configuração
- Handler com mais de ~80 linhas → extraia um Domain Service ou Value Object
- Um arquivo de teste por handler: `{FeatureName}Tests.cs`
- Sem dependência circular entre módulos — apenas `Contracts` são compartilhados
- Nenhum campo público mutável em entidades de domínio — use propriedades com `private set`

### Padrões de Frontend (React 19 / TypeScript 5.9)

```tsx
// ✓ Componentes com tipos explícitos e Zod para validação
const schema = z.object({
  name: z.string().min(1),
  planId: z.string().uuid(),
});
type FormValues = z.infer<typeof schema>;

// ✓ TanStack Query para estado assíncrono (não useState + useEffect para dados remotos)
const { data, isLoading } = useQuery({
  queryKey: ['tenants', filters],
  queryFn: () => tenantsApi.list(filters),
});

// ✓ i18n — nunca strings hardcoded em UI
const { t } = useTranslation('catalog');
return <Button>{t('catalog.service.create')}</Button>;

// ✓ Zod + React Hook Form para formulários
const form = useForm<FormValues>({
  resolver: zodResolver(schema),
});
```

---

## Parte 23 — Feature Status

| Feature | Status |
|---------|--------|
| 24 PostgreSQL DbContexts (um por contexto) | ✅ Operacional |
| Outbox + pg_advisory_lock | ✅ Operacional |
| Row-Level Security via TenantRlsInterceptor | ✅ Operacional |
| JWT multi-tenant auth + capability claims | ✅ Operacional |
| Elasticsearch (observabilidade/analytics) | ✅ Configurável |
| ClickHouse (analytics alternativo) | ⚠️ Opcional — desabilitado por padrão |
| Hot Chocolate GraphQL (catalog + change) | ✅ Operacional (code-first + subscriptions) |
| Kafka | ⚠️ Opcional — `NullKafkaEventProducer` por padrão |
| Redis / distributed cache | ✅ Redis se `ConnectionStrings:Redis` configurado; senão memory |
| HTTP resilience (retry + circuit breaker) | ✅ `AddStandardResilienceHandler()` em 14 HttpClients |
| `LicenseRecalculationJob` | ✅ A cada 15min — soma host units ativos por tenant |
| `AlertEvaluationJob` | ✅ LicenseUtilization + AgentHeartbeatMissed |
| Tenant provisioning automation | ✅ `ProvisionTenant` semeia roles + access policies |
| Trial plan capabilities | ✅ Professional + 4 teasers Enterprise (não multi_region/air_gapped) |
| Ollama (LLM local, qwen3.5:9b) | ✅ Habilitado por padrão |
| OpenAI | ⚠️ Opcional — `Enabled: false` por padrão |
| SemanticKernel + Qdrant | ✅ Operacional no módulo aiknowledge |
| pgvector (busca semântica) | ✅ PostgreSQL 16 + pgvector extension |
| `IModelRoutingPolicyRepository` | ✅ EF Core — tabela `aik_model_routing_policies` |
| `IAgentExecutionPlanRepository` | ✅ EF Core — tabela `aik_agent_execution_plans`, steps em JSONB |
| `IModelPredictionRepository` | ✅ EF Core — tabela `aik_model_prediction_samples`, features em JSONB |
| `ISbomRepository` | ✅ EF Core — tabela `ctr_sbom_records`, components em JSONB |
| `IDataContractRepository` | ✅ EF Core — tabela `ctr_data_contract_records` |
| `IDeprecationScheduleRepository` | ✅ EF Core — tabela `ctr_deprecation_schedules`, upsert por ContractId |
| `IFeatureFlagRepository` | ✅ EF Core — tabela `ctr_feature_flag_records`, unique (TenantId, ServiceId, FlagKey) |
| `IIDEUsageRepository` | ✅ EF Core — tabela `dx_ide_usage_records` |
| `IEventConsumerDeadLetterRepository` | ✅ EF Core — tabela `int_event_consumer_dead_letters`, Scoped |
| Assembly integrity check | ✅ SHA-256 no startup (skip via `NEXTRACE_SKIP_INTEGRITY=true`) |
| Preflight checks (10 verificações) | ✅ Postgres, JWT, RAM, disk, ports, Ollama, SMTP, OTel, CORS |
| Break Glass Access | ✅ BuildingBlocks.Security |
| CSRF protection | ✅ Cookie `nxt_csrf` + header `X-Csrf-Token` |
| Rate limiting (6 políticas) | ✅ Global, Auth, AuthSensitive, Ai, DataIntensive, Operations |
| AirGap enforcement | ✅ `AirGapHttpMessageHandler` em todos os HttpClients |
| React 19 SPA (1.048 arquivos TS/TSX) | ✅ Operacional |
| i18n (4 idiomas: pt, en, es, fr) | ✅ Operacional (i18next 25) |
| E2E tests (Playwright) | ✅ Mock backend + real backend |
| NexTrace SDK (cliente .NET) | ✅ `tools/NexTrace.Sdk/` |
| NexTraceOne CLI | ✅ `tools/NexTraceOne.CLI/` |
| Visual Studio IDE Extension | ✅ `tools/ide-extensions/visualstudio/` |
| Schema-per-tenant (`TenantSchemaManager`) | ❌ Existe mas não utilizado |
