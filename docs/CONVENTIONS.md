# NexTraceOne — Convenções de Código e Padrões

## 1. Regras de Idioma (INEGOCIÁVEL)

| Artefato | Idioma |
|----------|--------|
| Código (classes, métodos, variáveis, enums, DTOs, endpoints) | **Inglês** |
| Logs (mensagens de log estruturado) | **Inglês** |
| Nomes de arquivos e pastas | **Inglês** |
| Comentários XML (`<summary>`, `<param>`, `<returns>`, `<remarks>`) | **Português** |
| Comentários inline explicando lógica de negócio | **Português** |
| Regras de negócio documentadas | **Português** |
| Mensagens de erro expostas ao usuário final | **Inglês** (i18n futuro) |

### Exemplo correto:

```csharp
/// <summary>
/// Calcula o raio de impacto (blast radius) de uma mudança,
/// identificando todos os consumidores diretos e transitivos afetados.
/// O score é normalizado de 0.0 (sem impacto) a 1.0 (impacto máximo).
/// </summary>
/// <param name="releaseId">Identificador da release a ser analisada.</param>
/// <param name="ct">Token de cancelamento da operação.</param>
/// <returns>Relatório de blast radius com lista de consumidores afetados e score.</returns>
public async Task<Result<BlastRadiusReport>> CalculateAsync(
    ReleaseId releaseId,
    CancellationToken ct)
{
    // Busca a release e valida que está em estado elegível para análise
    var release = await _releaseRepository.GetByIdOrThrowAsync(releaseId, ct);

    // Percorre o grafo de dependências para encontrar consumidores transitivos
    var affectedConsumers = await _graphService.GetTransitiveConsumersAsync(
        release.AssetId, ct);

    // Normaliza o score baseado no total de consumidores no grafo
    var score = ComputeNormalizedScore(affectedConsumers.Count, totalConsumers);

    return BlastRadiusReport.Create(release, affectedConsumers, score);
}
```

---

## 2. Estrutura de Feature (VSA — Vertical Slice Architecture)

Cada feature vive em um único arquivo dentro de `Application/Features/{FeatureName}/`:

```csharp
// NexTraceOne.ChangeIntelligence.Application/Features/NotifyDeployment/NotifyDeployment.cs

namespace NexTraceOne.ChangeIntelligence.Application.Features.NotifyDeployment;

/// <summary>
/// Feature: NotifyDeployment — recebe notificação de deploy de um pipeline CI/CD
/// e inicia o fluxo de Change Intelligence (classificação, blast radius, score).
/// </summary>
public static class NotifyDeployment
{
    // ── COMMAND ───────────────────────────────────────────────────────────
    /// <summary>Comando para notificar um novo deploy.</summary>
    public sealed record Command(
        string ServiceName,
        string ReleaseVersion,
        string Environment,
        string Status,
        string? GitCommitSha = null,
        string? PipelineId = null) : ICommand<Guid>;

    // ── VALIDATOR ─────────────────────────────────────────────────────────
    /// <summary>Validações de entrada do comando NotifyDeployment.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ServiceName).NotEmpty().MaximumLength(200);
            RuleFor(x => x.ReleaseVersion).NotEmpty().MaximumLength(50);
            RuleFor(x => x.Environment).NotEmpty().MaximumLength(100);
            RuleFor(x => x.Status).NotEmpty();
        }
    }

    // ── HANDLER ───────────────────────────────────────────────────────────
    /// <summary>Handler que processa a notificação de deploy.</summary>
    public sealed class Handler : ICommandHandler<Command, Guid>
    {
        // Implementação...
    }

    // ── RESPONSE (se necessário) ──────────────────────────────────────────
    // Neste caso, retorna apenas o Guid do Release criado.
}
```

---

## 3. Endpoint (REPR Pattern — Minimal API)

Endpoints ficam na camada API do módulo, em `API/Endpoints/`:

```csharp
// NexTraceOne.ChangeIntelligence.API/Endpoints/ChangeIntelligenceEndpointModule.cs

/// <summary>Registra endpoints do módulo ChangeIntelligence.</summary>
public sealed class ChangeIntelligenceEndpointModule
{
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/change-intelligence")
            .WithTags("ChangeIntelligence")
            .RequireAuthorization();

        group.MapPost("/deployments/notify", async (
            NotifyDeployment.Command command,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(command, ct);
            return result.ToCreatedResult("/api/v1/change-intelligence/releases/{0}");
        })
        .WithName("NotifyDeployment")
        .Produces<Guid>(StatusCodes.Status201Created)
        .ProducesValidationProblem();
    }
}
```

---

## 4. Aggregate Root (padrão)

```csharp
/// <summary>
/// Release representa uma versão específica de um serviço/API
/// que foi ou será promovida entre ambientes.
/// É o Aggregate Root central do módulo ChangeIntelligence.
/// </summary>
public sealed class Release : AggregateRoot<ReleaseId>
{
    // Construtor privado — criação apenas via factory method
    private Release() { }

    // Propriedades com setters privados — invariantes protegidas
    public string ServiceName { get; private set; } = string.Empty;
    public SemanticVersion Version { get; private set; } = null!;

    // Factory method — único ponto de criação
    /// <summary>Cria uma nova Release a partir de uma notificação de deploy.</summary>
    public static Release CreateFromDeployment(
        string serviceName,
        SemanticVersion version,
        EnvironmentName environment)
    {
        var release = new Release
        {
            Id = ReleaseId.New(),
            ServiceName = Guard.Against.NullOrWhiteSpace(serviceName),
            Version = Guard.Against.Null(version),
        };

        // Emite Domain Event — será coletado pelo DbContext no commit
        release.RaiseDomainEvent(new ReleaseCreatedDomainEvent(release.Id));

        return release;
    }
}

/// <summary>Identificador fortemente tipado de Release.</summary>
public sealed record ReleaseId(Guid Value) : TypedIdBase(Value)
{
    public static ReleaseId New() => new(Guid.NewGuid());
    public static ReleaseId From(Guid id) => new(id);
}
```

---

## 5. Naming Conventions

| Artefato | Padrão | Exemplo |
|----------|--------|---------|
| Aggregate Root | PascalCase, substantivo | `Release`, `WorkflowInstance` |
| Value Object | PascalCase, substantivo composto | `SemanticVersion`, `GitContext` |
| Domain Event | PascalCase, passado + DomainEvent | `ReleaseCreatedDomainEvent` |
| Integration Event | PascalCase, passado + IntegrationEvent | `ReleaseApprovedIntegrationEvent` |
| Command | PascalCase, verbo imperativo | `NotifyDeployment.Command` |
| Query | PascalCase, Get/List/Search + substantivo | `GetRelease.Query`, `ListReleases.Query` |
| Handler | PascalCase, Handler | `NotifyDeployment.Handler` |
| Validator | PascalCase, Validator | `NotifyDeployment.Validator` |
| Repository interface | I + Aggregate + Repository | `IReleaseRepository` |
| DbContext | Module + DbContext | `ChangeIntelligenceDbContext` |
| Endpoint module | Module + EndpointModule | `ChangeIntelligenceEndpointModule` |
| StronglyTypedId | Aggregate + Id | `ReleaseId`, `AssetId` |
| Erros do módulo | Module + Errors (estático) | `ChangeIntelligenceErrors` |
| Campos privados | _camelCase | `_releaseRepository` |
| Constantes | PascalCase | `MaxPageSize` |

---

## 6. Regras de Qualidade

### SEMPRE fazer:

- Toda classe pública com `<summary>` XML em português
- Todo método público com `<summary>` XML em português
- Comentários inline explicando o **porquê**, não o **o quê**
- Result Pattern para operações que podem falhar
- Guard clauses no início de métodos (fail fast)
- Construtores privados em Aggregates (criação via factory methods)
- Setters privados em propriedades de domínio
- `sealed` em classes que não serão herdadas
- `readonly` em campos que não mudam após construção
- `CancellationToken` em toda operação assíncrona

### NUNCA fazer:

- `DateTime.Now` ou `DateTimeOffset.UtcNow` direto — usar `IDateTimeProvider`
- Exceções para controle de fluxo — usar `Result<T>`
- Acessar DbContext de outro módulo
- Publicar Integration Events fora do Outbox
- Lógica de negócio em Controllers/Endpoints
- `string` para IDs de entidade — usar StronglyTypedIds
- Campos públicos com setter público em Aggregates
- `new()` público em Aggregates — usar factory methods
- Referência direta entre módulos (exceto via Contracts)
- Simplificar sem avisar — profundidade máxima sempre

---

## 7. Testes

### Estrutura de teste por módulo:

```
tests/modules/{nome}/NexTraceOne.{Nome}.Tests/
├── Domain/
│   ├── Entities/          ← Testes de invariantes do Aggregate
│   └── ValueObjects/      ← Testes de igualdade e validação
├── Application/
│   └── Features/          ← Testes de handlers com mocks
└── Infrastructure/
    └── Persistence/       ← Testes de integração com Testcontainers
```

### Convenções:

- Framework: xUnit
- Assertions: FluentAssertions
- Mocking: NSubstitute
- Data generation: Bogus
- Containers: Testcontainers.PostgreSql
- Naming: `{Método}_Should_{Resultado}_When_{Condição}`
- Arrange-Act-Assert (AAA)

```csharp
[Fact]
public void CreateFromDeployment_Should_RaiseDomainEvent_When_ValidInput()
{
    // Arrange
    var serviceName = "order-api";
    var version = SemanticVersion.Parse("2.4.0");

    // Act
    var release = Release.CreateFromDeployment(serviceName, version, env);

    // Assert
    release.DomainEvents.Should().ContainSingle()
        .Which.Should().BeOfType<ReleaseCreatedDomainEvent>();
}
```
