# BACKEND-MODULE-GUIDELINES.md — NexTraceOne

> **Versão:** 2.0 — Março 2026
> **Escopo:** Guia completo para implementação de módulos backend no NexTraceOne.
> **Audiência:** Engineers e Tech Leads trabalhando no backend .NET.

Este guia cobre tudo o que é necessário para criar, expandir e testar módulos no NexTraceOne. Todos os exemplos são baseados em código real do repositório.

---

## 1. Estrutura de Pastas Obrigatória

Cada módulo segue esta estrutura de assemblies:

```
src/modules/{modulo}/
├── NexTraceOne.{Modulo}.Domain/
│   ├── Entities/
│   │   └── {Aggregate}.cs
│   ├── Events/
│   │   └── {Aggregate}CreatedDomainEvent.cs
│   ├── Enums/
│   └── ValueObjects/
│
├── NexTraceOne.{Modulo}.Application/
│   ├── Abstractions/
│   │   ├── I{Aggregate}Repository.cs
│   │   └── IUnitOfWork.cs          # re-exportado do BuildingBlocks
│   ├── Features/
│   │   ├── Create{Aggregate}/
│   │   │   └── Create{Aggregate}.cs  # Command + Handler + Validator num único ficheiro
│   │   └── Get{Aggregate}ById/
│   │       └── Get{Aggregate}ById.cs # Query + Handler
│   └── DependencyInjection.cs
│
├── NexTraceOne.{Modulo}.Infrastructure/
│   ├── Persistence/
│   │   ├── {Modulo}DbContext.cs
│   │   ├── Configurations/
│   │   │   └── {Aggregate}Configuration.cs
│   │   └── Repositories/
│   │       └── {Aggregate}Repository.cs
│   ├── Migrations/
│   └── DependencyInjection.cs
│
├── NexTraceOne.{Modulo}.API/
│   ├── Endpoints/
│   │   ├── {Aggregate}Endpoints.cs
│   │   └── DependencyInjection.cs
│   └── Mappers/
│       └── {Aggregate}Mapper.cs     # opcional — se mapeamento for complexo
│
└── NexTraceOne.{Modulo}.Contracts/
    ├── {Aggregate}Dto.cs
    └── {Aggregate}Request.cs
```

---

## 2. Como Criar uma Entidade de Domínio com ID Tipado

Entidades de domínio herdam o padrão de `TypedIdBase` para IDs fortemente tipados. Isto previne erros como passar um `ServiceId` onde se espera um `ContractId`.

```csharp
// NexTraceOne.{Modulo}.Domain/Entities/IntegrationConnector.cs

namespace NexTraceOne.Integrations.Domain.Entities;

/// <summary>ID fortemente tipado para IntegrationConnector.</summary>
public sealed record IntegrationConnectorId(Guid Value) : TypedIdBase(Value);

/// <summary>
/// Representa um conector de integração com sistema externo (GitLab, Jenkins, etc).
/// Aggregate root do sub-contexto ConnectorHub.
/// </summary>
public sealed class IntegrationConnector
{
    public IntegrationConnectorId Id { get; private set; }
    public Guid TenantId { get; private set; }
    public string Name { get; private set; }
    public string ConnectorType { get; private set; }
    public ConnectorStatus Status { get; private set; }
    public ConnectorHealth Health { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private IntegrationConnector() { } // Construtor vazio para EF Core

    /// <summary>
    /// Factory method — única forma de criar um conector válido.
    /// Garante invariantes de domínio antes da criação.
    /// </summary>
    public static IntegrationConnector Create(
        string name,
        string connectorType,
        Guid tenantId,
        IDateTimeProvider clock)
    {
        Guard.AgainstNullOrWhiteSpace(name, nameof(name));
        Guard.AgainstNullOrWhiteSpace(connectorType, nameof(connectorType));

        return new IntegrationConnector
        {
            Id = new IntegrationConnectorId(Guid.NewGuid()),
            TenantId = tenantId,
            Name = name,
            ConnectorType = connectorType,
            Status = ConnectorStatus.Inactive,
            Health = ConnectorHealth.Unknown,
            CreatedAt = clock.UtcNow
        };
    }

    /// <summary>Activa o conector após validação de credenciais.</summary>
    public void Activate()
    {
        if (Status == ConnectorStatus.Active)
            return;

        Status = ConnectorStatus.Active;
    }
}
```

### Regras para Entidades

- Construtor privado vazio obrigatório para EF Core
- Factory method `Create(…)` como único ponto de construção pública
- Propriedades com `private set` — apenas métodos da entidade alteram estado
- Guard clauses no factory method para validar invariantes
- `TenantId` obrigatório em toda entidade que precisa de isolamento multi-tenant
- Nunca usar `DateTime.Now` — sempre `IDateTimeProvider.UtcNow`

---

## 3. Como Criar um Command Handler

Commands representam intenções de mudar o estado do sistema. Seguem o padrão de classe estática com records aninhados.

```csharp
// NexTraceOne.{Modulo}.Application/Features/CreateIntegrationConnector/CreateIntegrationConnector.cs

using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Integrations.Application.Abstractions;
using NexTraceOne.Integrations.Domain.Entities;

namespace NexTraceOne.Integrations.Application.Features.CreateIntegrationConnector;

/// <summary>
/// Cria um novo conector de integração para o tenant ativo.
/// Valida unicidade do nome antes da criação.
/// </summary>
public static class CreateIntegrationConnector
{
    /// <summary>Command com os dados do novo conector.</summary>
    public sealed record Command(
        string Name,
        string ConnectorType,
        string? BaseUrl) : ICommand<Guid>;

    /// <summary>Handler que executa a criação.</summary>
    public sealed class Handler(
        IIntegrationConnectorRepository repository,
        ICurrentTenant tenant,
        IDateTimeProvider clock) : ICommandHandler<Command, Guid>
    {
        public async Task<Result<Guid>> Handle(Command request, CancellationToken cancellationToken)
        {
            // Guard clause: verificar unicidade
            var exists = await repository.ExistsByNameAsync(
                request.Name, tenant.Id, cancellationToken);

            if (exists)
                return Error.Conflict(
                    "integration.connector.name_conflict",
                    $"A connector named '{request.Name}' already exists for this tenant.");

            // Criar via factory method do domínio
            var connector = IntegrationConnector.Create(
                request.Name,
                request.ConnectorType,
                tenant.Id,
                clock);

            await repository.AddAsync(connector, cancellationToken);

            // Commit é feito pelo TransactionBehavior após o handler retornar
            return Result<Guid>.Success(connector.Id.Value);
        }
    }

    /// <summary>Validação de FluentValidation executada pelo ValidationBehavior antes do handler.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name is required.")
                .MaximumLength(200).WithMessage("Name must not exceed 200 characters.");

            RuleFor(x => x.ConnectorType)
                .NotEmpty().WithMessage("ConnectorType is required.");
        }
    }
}
```

### Regras para Command Handlers

- Classe `public static` com records `Command`, `Handler` e opcionalmente `Validator` aninhados
- `Command` implementa `ICommand<TResponse>` (com resposta) ou `ICommand` (sem resposta)
- `Handler` recebe dependências via constructor injection (primary constructors do C# 12)
- Guard clauses no início do `Handle` para verificações de negócio
- Nunca fazer leitura de dados para retornar ao cliente dentro de um Command — use Query
- `CancellationToken` sempre passado para chamadas async
- Commit é automático via `TransactionBehavior` — não chamar `SaveChangesAsync` manualmente

---

## 4. Como Criar um Query Handler

Queries são somente-leitura. O handler acede ao repositório via a interface de leitura e retorna um DTO.

```csharp
// NexTraceOne.{Modulo}.Application/Features/GetAnalyticsSummary/GetAnalyticsSummary.cs
// (Exemplo real do módulo ProductAnalytics)

public static class GetAnalyticsSummary
{
    /// <summary>Query com filtros opcionais para o resumo de analytics.</summary>
    public sealed record Query(
        string? Persona,
        string? Module,
        string? TeamId,
        string? DomainId,
        string? Range) : IQuery<Response>;

    /// <summary>Handler que compila e retorna o resumo de analytics.</summary>
    public sealed class Handler(
        IAnalyticsEventRepository repository,
        IDateTimeProvider clock) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var (from, to, periodLabel) = ResolveRange(clock.UtcNow, request.Range);

            var totalEvents = await repository.CountAsync(
                persona: request.Persona,
                from: from,
                to: to,
                cancellationToken: cancellationToken);

            var uniqueUsers = await repository.CountUniqueUsersAsync(
                from: from, to: to, cancellationToken: cancellationToken);

            return new Response(
                TotalEvents: totalEvents,
                UniqueUsers: uniqueUsers,
                PeriodLabel: periodLabel);
        }

        private static (DateTimeOffset From, DateTimeOffset To, string Label) ResolveRange(
            DateTimeOffset utcNow, string? range)
        {
            var label = string.IsNullOrWhiteSpace(range) ? "last_30d" : range;
            var days = label switch { "last_7d" => 7, "last_1d" => 1, "last_90d" => 90, _ => 30 };
            return (utcNow.AddDays(-days), utcNow, label);
        }
    }

    /// <summary>Resposta da query com resumo de analytics.</summary>
    public sealed record Response(
        long TotalEvents,
        int UniqueUsers,
        string PeriodLabel);
}
```

### Regras para Query Handlers

- Queries **nunca** chamam repositórios de escrita nem disparam Domain Events
- Podem usar `AsNoTracking()` nos repositórios (performance)
- O `TransactionBehavior` não abre transação para Queries — comportamento por design
- Retornam `Result<Response>` — nunca lançam exceção

---

## 5. Como Criar Configuração EF Core

Cada entidade tem uma classe de configuração separada implementando `IEntityTypeConfiguration<T>`.

```csharp
// NexTraceOne.{Modulo}.Infrastructure/Persistence/Configurations/IntegrationConnectorConfiguration.cs

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.Integrations.Domain.Entities;

namespace NexTraceOne.Integrations.Infrastructure.Persistence.Configurations;

/// <summary>Configuração EF Core para IntegrationConnector.</summary>
public sealed class IntegrationConnectorConfiguration : IEntityTypeConfiguration<IntegrationConnector>
{
    public void Configure(EntityTypeBuilder<IntegrationConnector> builder)
    {
        builder.ToTable("integration_connectors");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new IntegrationConnectorId(value))
            .IsRequired();

        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ConnectorType).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(x => x.Health).HasConversion<string>().HasMaxLength(50).IsRequired();

        // Query filter para multi-tenancy — filtrado automaticamente pelo TenantRlsInterceptor
        builder.HasQueryFilter(x => x.TenantId == EF.Property<Guid>(x, "_currentTenantId"));

        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => new { x.TenantId, x.Name }).IsUnique();
    }
}
```

### Regras de Configuração EF Core

- Tabela com prefixo do módulo em snake_case (ex.: `integration_connectors`)
- Conversão de `TypedId` obrigatória: `HasConversion(id => id.Value, v => new XxxId(v))`
- Enum persistido como string (`HasConversion<string>()`) para legibilidade no PostgreSQL
- `HasQueryFilter` para TenantId em entidades multi-tenant
- Índices em `TenantId` e colunas de busca frequente
- Índice único em `(TenantId, Name)` quando nome é único por tenant

---

## 6. Como Criar um DbContext

```csharp
// NexTraceOne.{Modulo}.Infrastructure/Persistence/{Modulo}DbContext.cs

using System.Reflection;
using Microsoft.EntityFrameworkCore;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Infrastructure.Persistence;
using NexTraceOne.Integrations.Domain.Entities;

namespace NexTraceOne.Integrations.Infrastructure.Persistence;

/// <summary>
/// DbContext do módulo Integrations.
/// Herda NexTraceDbContextBase para obter AuditInterceptor,
/// TenantRlsInterceptor, EncryptionInterceptor e OutboxInterceptor.
/// </summary>
public sealed class IntegrationsDbContext(
    DbContextOptions<IntegrationsDbContext> options,
    ICurrentTenant tenant,
    ICurrentUser user,
    IDateTimeProvider clock)
    : NexTraceDbContextBase(options, tenant, user, clock)
{
    /// <summary>Assembly onde estão as IEntityTypeConfiguration deste DbContext.</summary>
    protected override Assembly ConfigurationsAssembly => typeof(IntegrationsDbContext).Assembly;

    /// <summary>Nome único da tabela de outbox para evitar colisões entre DbContexts.</summary>
    protected override string OutboxTableName => "integrations_outbox_messages";

    public DbSet<IntegrationConnector> IntegrationConnectors { get; set; }
    public DbSet<IngestionSource> IngestionSources { get; set; }
    public DbSet<IngestionExecution> IngestionExecutions { get; set; }
    public DbSet<WebhookSubscription> WebhookSubscriptions { get; set; }
}
```

### Checklist para um novo DbContext

- [ ] Herda `NexTraceDbContextBase` (não `DbContext` directamente)
- [ ] Recebe `ICurrentTenant`, `ICurrentUser`, `IDateTimeProvider` no constructor
- [ ] `ConfigurationsAssembly` aponta para o assembly correto
- [ ] `OutboxTableName` tem prefixo único do módulo
- [ ] `DbSet<T>` para cada aggregate root gerido por este contexto

---

## 7. Como Registar um Módulo no DI

```csharp
// NexTraceOne.{Modulo}.Infrastructure/DependencyInjection.cs

public static class DependencyInjection
{
    /// <summary>Adiciona os serviços de infraestrutura do módulo Integrations.</summary>
    public static IServiceCollection AddIntegrationsInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // 1. Connection string — usa helper GetRequiredConnectionString para falhar cedo
        var connectionString = configuration
            .GetRequiredConnectionString("IntegrationsDatabase", "NexTraceOne");

        // 2. DbContext com interceptors obrigatórios
        services.AddDbContext<IntegrationsDbContext>((serviceProvider, options) =>
            options.UseNpgsql(connectionString)
                .AddInterceptors(
                    serviceProvider.GetRequiredService<AuditInterceptor>(),
                    serviceProvider.GetRequiredService<TenantRlsInterceptor>()));

        // 3. Repositórios
        services.AddScoped<IIntegrationConnectorRepository, IntegrationConnectorRepository>();
        services.AddScoped<IIngestionSourceRepository, IngestionSourceRepository>();
        services.AddScoped<IWebhookSubscriptionRepository, WebhookSubscriptionRepository>();

        // 4. Serviços de domínio ou infra específicos
        services.AddScoped<IIntegrationContextResolver, IntegrationContextResolver>();

        // 5. Integration Event Handlers (Domain Events → Integration Events downstream)
        services.AddScoped<
            IIntegrationEventHandler<IngestionPayloadProcessedDomainEvent>,
            IngestionPayloadProcessedDomainEventHandler>();

        return services;
    }
}
```

E no `Program.cs` do ApiHost:

```csharp
builder.Services.AddIntegrationsInfrastructure(builder.Configuration);
```

O DI da camada Application (MediatR handlers, validators) é registado automaticamente via assembly scanning no BuildingBlocks — basta que os handlers estejam no assembly correto.

---

## 8. Como Criar e Executar Migrations

### Criar migration para um DbContext específico

```bash
# Executar a partir da raiz do repositório
dotnet ef migrations add {NomeDaMigration} \
  --project src/modules/{modulo}/NexTraceOne.{Modulo}.Infrastructure \
  --startup-project src/ApiHost/NexTraceOne.ApiHost \
  --context {Modulo}DbContext \
  --output-dir Persistence/Migrations
```

### Exemplos reais

```bash
# Migration para o módulo Integrations
dotnet ef migrations add AddWebhookSubscriptions \
  --project src/modules/integrations/NexTraceOne.Integrations.Infrastructure \
  --startup-project src/ApiHost/NexTraceOne.ApiHost \
  --context IntegrationsDbContext

# Migration para o sub-contexto ChangeIntelligence
dotnet ef migrations add AddBlastRadiusScore \
  --project src/modules/changegovernance/NexTraceOne.ChangeGovernance.Infrastructure \
  --startup-project src/ApiHost/NexTraceOne.ApiHost \
  --context ChangeIntelligenceDbContext
```

### Aplicar migrations manualmente

```bash
# Aplicar todas as migrations pendentes
dotnet ef database update \
  --project src/modules/{modulo}/NexTraceOne.{Modulo}.Infrastructure \
  --startup-project src/ApiHost/NexTraceOne.ApiHost \
  --context {Modulo}DbContext
```

> **Nota:** Em ambiente de desenvolvimento, as migrations são aplicadas automaticamente no startup do ApiHost via `ApplyDatabaseMigrationsAsync()`. Em produção, recomenda-se aplicar manualmente antes do deploy.

---

## 9. Como Criar Endpoints Minimal API

```csharp
// NexTraceOne.{Modulo}.API/Endpoints/{Aggregate}Endpoints.cs

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using MediatR;
using NexTraceOne.BuildingBlocks.Application.Extensions;

namespace NexTraceOne.Integrations.API.Endpoints;

/// <summary>Endpoints para gestão de IntegrationConnectors.</summary>
public static class IntegrationConnectorEndpoints
{
    public static IEndpointRouteBuilder MapIntegrationConnectorEndpoints(
        this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/integrations/connectors")
            .WithTags("Integrations")
            .RequireAuthorization("integrations:connectors:read");

        group.MapGet("/", async (IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new ListIntegrationConnectors.Query(), ct);
            return result.ToHttpResult();
        })
        .WithName("ListIntegrationConnectors");

        group.MapPost("/", async (
            CreateIntegrationConnector.Command command,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            return result.ToHttpResult(StatusCodes.Status201Created);
        })
        .RequireAuthorization("integrations:connectors:write")
        .WithName("CreateIntegrationConnector");

        return routes;
    }
}
```

### Regras para Endpoints

- Controladores/Endpoints devem ser **thin** — nenhuma lógica de negócio aqui
- Usar `result.ToHttpResult()` para mapeamento automático de `Result<T>` para `IResult`
- Autorização declarada via `RequireAuthorization("permissao:recurso:acao")`
- `CancellationToken` sempre passado para `mediator.Send`

---

## 10. Convenções de Nomenclatura

### Handlers

| Tipo | Convenção | Exemplo |
|---|---|---|
| Command de criação | `Create{Aggregate}` | `CreateIntegrationConnector` |
| Command de atualização | `Update{Aggregate}` | `UpdateConnectorCredentials` |
| Command de deleção | `Delete{Aggregate}` | `DeleteIntegrationConnector` |
| Command de ação de negócio | `{Verbo}{Contexto}` | `ActivateConnector`, `PublishContract` |
| Query de lista | `List{Aggregates}` | `ListIntegrationConnectors` |
| Query de detalhe por ID | `Get{Aggregate}ById` | `GetIntegrationConnectorById` |
| Query de sumário | `Get{Modulo}Summary` | `GetAnalyticsSummary` |

### Repositórios

| Interface | Implementação |
|---|---|
| `I{Aggregate}Repository` | `{Aggregate}Repository` |
| `IUnitOfWork` | resolvido para o DbContext do módulo |

### Domain Events

| Convenção | Exemplo |
|---|---|
| `{Aggregate}{Passado}DomainEvent` | `IntegrationConnectorActivatedDomainEvent` |

### Integration Events (cross-module)

| Convenção | Exemplo |
|---|---|
| `{Aggregate}{Passado}IntegrationEvent` | `IngestionPayloadProcessedIntegrationEvent` |

### DTOs (Contracts)

| Tipo | Convenção | Exemplo |
|---|---|---|
| Resposta de leitura | `{Aggregate}Dto` | `IntegrationConnectorDto` |
| Request de escrita | `{Aggregate}Request` | `CreateConnectorRequest` |
| Sumário / lista | `{Aggregate}SummaryDto` | `ConnectorSummaryDto` |

---

## 11. Regras de Segurança Obrigatórias

### CancellationToken

Toda operação async deve receber e propagar `CancellationToken`:

```csharp
// ✅ Correto
public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
{
    var items = await repository.ListAsync(cancellationToken);
    // …
}

// ❌ Errado — ignora cancelamento
public async Task<Result<Response>> Handle(Query request, CancellationToken _)
{
    var items = await repository.ListAsync(CancellationToken.None); // não fazer
}
```

### Result<T> Pattern

```csharp
// ✅ Correto — fluxo de negócio usa Result, não exceções
if (await repository.ExistsAsync(name, ct))
    return Error.Conflict("connector.exists", "Connector already exists.");

// ❌ Errado — não lançar exceções para fluxos esperados
if (await repository.ExistsAsync(name, ct))
    throw new InvalidOperationException("Connector already exists.");
```

### Guard Clauses

```csharp
// ✅ Correto — guard clauses no início do método
public static IntegrationConnector Create(string name, string type, ...)
{
    Guard.AgainstNullOrWhiteSpace(name, nameof(name));
    Guard.AgainstNullOrWhiteSpace(type, nameof(type));
    // lógica de negócio aqui
}
```

### DateTime — Nunca DateTime.Now

```csharp
// ✅ Correto
CreatedAt = clock.UtcNow  // IDateTimeProvider injetado

// ❌ Errado
CreatedAt = DateTime.Now
CreatedAt = DateTime.UtcNow  // mesmo UTC — sempre usar a abstração
```

### sealed em classes finais

```csharp
// ✅ Correto — classes sem herança pretendida são sealed
public sealed class CreateIntegrationConnector.Handler(…) : ICommandHandler<…>
public sealed record IntegrationConnectorId(Guid Value) : TypedIdBase(Value);

// ❌ Desnecessário deixar aberto quando não há plano de herança
public class Handler(…) : ICommandHandler<…>
```

---

## 12. Como Escrever Testes Unitários para Handlers

Ferramentas: **xUnit** + **NSubstitute** (mocks) + **Bogus** (dados falsos) + **FluentAssertions**.

```csharp
// tests/modules/integrations/NexTraceOne.Integrations.Application.Tests/
// Features/CreateIntegrationConnector/CreateIntegrationConnectorHandlerTests.cs

using Bogus;
using FluentAssertions;
using NSubstitute;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Integrations.Application.Features.CreateIntegrationConnector;

namespace NexTraceOne.Integrations.Application.Tests.Features;

public sealed class CreateIntegrationConnectorHandlerTests
{
    private readonly IIntegrationConnectorRepository _repository;
    private readonly ICurrentTenant _tenant;
    private readonly IDateTimeProvider _clock;
    private readonly CreateIntegrationConnector.Handler _handler;
    private readonly Faker _faker = new();

    public CreateIntegrationConnectorHandlerTests()
    {
        _repository = Substitute.For<IIntegrationConnectorRepository>();
        _tenant = Substitute.For<ICurrentTenant>();
        _clock = Substitute.For<IDateTimeProvider>();

        _tenant.Id.Returns(Guid.NewGuid());
        _clock.UtcNow.Returns(DateTimeOffset.UtcNow);

        _handler = new CreateIntegrationConnector.Handler(_repository, _tenant, _clock);
    }

    [Fact]
    public async Task Handle_WhenNameIsUnique_ShouldCreateConnectorAndReturnId()
    {
        // Arrange
        var command = new CreateIntegrationConnector.Command(
            Name: _faker.Commerce.ProductName(),
            ConnectorType: "GitLab",
            BaseUrl: "https://gitlab.example.com");

        _repository
            .ExistsByNameAsync(command.Name, _tenant.Id, Arg.Any<CancellationToken>())
            .Returns(false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
        await _repository.Received(1).AddAsync(Arg.Any<IntegrationConnector>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenNameAlreadyExists_ShouldReturnConflictError()
    {
        // Arrange
        var command = new CreateIntegrationConnector.Command(
            Name: "Existing Connector",
            ConnectorType: "Jenkins",
            BaseUrl: null);

        _repository
            .ExistsByNameAsync(command.Name, _tenant.Id, Arg.Any<CancellationToken>())
            .Returns(true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Conflict);
        await _repository.DidNotReceive().AddAsync(Arg.Any<IntegrationConnector>(), Arg.Any<CancellationToken>());
    }
}
```

---

## 13. Como Escrever Testes de Integração com Testcontainers

Testes de integração levantam um PostgreSQL real via Docker.

```csharp
// tests/modules/integrations/NexTraceOne.Integrations.Integration.Tests/
// IntegrationConnectorIntegrationTests.cs

using DotNet.Testcontainers.Builders;
using Microsoft.EntityFrameworkCore;
using NexTraceOne.Integrations.Infrastructure.Persistence;
using Respawn;
using Testcontainers.PostgreSql;

public sealed class IntegrationConnectorIntegrationTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("integrations_test")
        .Build();

    private IntegrationsDbContext _context = null!;
    private Respawner _respawner = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        var options = new DbContextOptionsBuilder<IntegrationsDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        var tenant = Substitute.For<ICurrentTenant>();
        tenant.Id.Returns(Guid.NewGuid());

        _context = new IntegrationsDbContext(options, tenant, ...);
        await _context.Database.MigrateAsync();

        _respawner = await Respawner.CreateAsync(
            _postgres.GetConnectionString(),
            new RespawnerOptions { DbAdapter = DbAdapter.Postgres });
    }

    public async Task DisposeAsync()
    {
        await _postgres.StopAsync();
        await _context.DisposeAsync();
    }

    [Fact]
    public async Task CreateConnector_ShouldPersistAndRetrieve()
    {
        // Arrange
        var connector = IntegrationConnector.Create("GitLab CI", "GitLab", tenantId, clock);

        // Act
        _context.IntegrationConnectors.Add(connector);
        await _context.SaveChangesAsync();

        // Assert
        var persisted = await _context.IntegrationConnectors
            .FirstOrDefaultAsync(x => x.Id == connector.Id);

        persisted.Should().NotBeNull();
        persisted!.Name.Should().Be("GitLab CI");
    }

    // Limpar DB entre testes para isolamento
    private async Task ResetDatabaseAsync() => await _respawner.ResetAsync(_postgres.GetConnectionString());
}
```

---

## 14. Checklist para Revisão de Módulo Backend

Antes de fazer merge de um módulo novo ou alterado:

### Domínio
- [ ] Entidades têm IDs fortemente tipados (TypedIdBase)
- [ ] Factory methods com guard clauses
- [ ] `TenantId` em entidades multi-tenant
- [ ] Domain Events para operações de negócio relevantes
- [ ] Sem dependências de Infrastructure no Domain

### Application
- [ ] Command e Query separados (CQRS)
- [ ] Handlers recebem apenas interfaces (abstractions), nunca Infrastructure concreta
- [ ] `CancellationToken` em todas as operações async
- [ ] `Result<T>` em vez de exceções para fluxos esperados
- [ ] `Validator` com FluentValidation para Commands com inputs externos

### Infrastructure
- [ ] DbContext herda `NexTraceDbContextBase`
- [ ] Configurações EF Core em classes `IEntityTypeConfiguration<T>` separadas
- [ ] Conversão de TypedId em toda propriedade de ID
- [ ] `HasQueryFilter` para TenantId
- [ ] `DependencyInjection.cs` completo com todos os repositórios e serviços

### API
- [ ] Endpoints thin — sem lógica de negócio
- [ ] `RequireAuthorization` em todos os endpoints
- [ ] `CancellationToken` passado para `mediator.Send`
- [ ] Mapeamento correto via `result.ToHttpResult()`

### Testes
- [ ] Testes unitários para handlers críticos
- [ ] Casos de erro testados (not found, conflict, validation)
- [ ] Sem testes que dependem de estado externo não mockado

---

*Última atualização: Março 2026.*
