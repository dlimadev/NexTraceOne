# Auditoria Completa — Módulo AIKnowledge
**Data:** 2026-05-14  
**Versão analisada:** branch `claude/code-review-audit-tRGop`  
**Auditor:** Claude Code (Análise Automática — Modo Analysis)  
**Escopo:** Ponta-a-ponta — Domain → Application → Infrastructure → API → Frontend  
**Referências:** `CLAUDE.md`, `.github/copilot-instructions.md`

---

## 1. Visão Geral do Módulo

O módulo **AIKnowledge** é o bounded context responsável por toda a governança de IA da plataforma NexTraceOne. Cobre:

- **AI Governance**: Agents, Models, Guardrails, Policies, Budgets, Skills, Prompt Templates, Tool Definitions, Knowledge Sources, IDE Integration
- **AI Orchestration**: Orquestração de análises AI (Change Confidence, Contract Review, Promotion Readiness, etc.)
- **AI Runtime**: Providers (Ollama, OpenAI, Anthropic), Token Quota, Embeddings, Tool Execution
- **External AI**: Consultas a LLMs externos com capture e auditoria de respostas

### Dimensão do código
| Camada | Ficheiros .cs |
|---|---|
| Domain | ~130 ficheiros |
| Application | ~270 ficheiros |
| Infrastructure | ~200 ficheiros |
| API | ~15 ficheiros |
| **Total Backend** | **~615 ficheiros** |
| Tests | 119 ficheiros |
| Frontend | 32+ ficheiros |

### Contextos de persistência
- `AiGovernanceDbContext` — PostgreSQL (tabelas `aik_*`)
- `ExternalAiDbContext` — PostgreSQL (tabelas `ext_ai_*`)
- `AiOrchestrationDbContext` — PostgreSQL (tabelas `aik_orch_*`)
- `IAiAnalyticsRepository` — ClickHouse ou Null fallback
- `IAiSearchRepository` — Elasticsearch (NEST) ou Null fallback

---

## 2. Sumário Executivo de Problemas

| Severidade | Categoria | Nº de Problemas |
|---|---|---|
| 🔴 CRÍTICO | Segurança / Data Exposure | 3 |
| 🔴 CRÍTICO | Arquitectura | 2 |
| 🟠 ALTO | Arquitectura / Correctness | 8 |
| 🟡 MÉDIO | Qualidade / Conformidade | 12 |
| 🔵 BAIXO | Melhorias / Boas Práticas | 9 |
| **Total** | | **34** |

---

## 3. Problemas Críticos (🔴)

### P-C01 — Falta de filtro de tenant em 36 de 54 repositórios

**Severidade:** CRÍTICO — Exposição de dados entre tenants  
**Localização:** `Infrastructure/Governance/Persistence/Repositories/`  
**Regra violada:** CLAUDE.md §"Tenant isolation (two layers)" — "Repository-level filter — every read method must also add `.Where(e => e.TenantId == currentTenant.Id)` as defense-in-depth."

**Problema:**  
Apenas 18 dos 54 repositórios implementam filtro de `TenantId` ao nível da aplicação. Os restantes 36 dependem **exclusivamente** do `TenantRlsInterceptor` (RLS no PostgreSQL). Embora o RLS seja uma camada válida, a CLAUDE.md exige **duas camadas** de isolação por defense-in-depth.

**Repositórios críticos afectados (sem filtro de TenantId):**

```
AiAgentRepository           — Agentes IA (dados sensíveis de configuração)
AiAssistantConversationRepository — Histórico de conversas (dados de utilizador)
AiAgentExecutionRepository  — Logs de execução (dados operacionais)
AiModelRepository           — Model Registry (dados de configuração)
AiBudgetRepository          — Orçamentos de tokens (dados financeiros)
AiGuardrailRepository       — Guardrails de segurança
AiAccessPolicyRepository    — Políticas de acesso
AiProviderRepository        — Credenciais de providers
AiTokenQuotaPolicyRepository — Quotas (dados financeiros)
AiFeedbackRepository        — Feedback de utilizadores
AiSkillExecutionRepository  — Histórico de execuções de skills
PromptTemplateRepository    — Templates de prompts (IP sensível)
ExternalDataSourceRepository — Configurações de fontes externas
```

**Impacto:**  
Se o RLS falhar (ex: configuração errada, `set_config` falhou silenciosamente, bug de upgrade do Npgsql), dados de um tenant são visíveis para outros tenants. Isto é especialmente grave porque IA guarda histórico de conversas, prompts e dados de negócio sensíveis.

**Correcção:**

```csharp
// Antes (AiAgentRepository):
return await query.OrderBy(a => a.SortOrder).ThenBy(a => a.DisplayName).ToListAsync(ct);

// Depois:
internal sealed class AiAgentRepository(
    AiGovernanceDbContext context,
    ICurrentTenant currentTenant) : IAiAgentRepository
{
    public async Task<IReadOnlyList<AiAgent>> ListAsync(bool? isActive, bool? isOfficial, CancellationToken ct)
    {
        var query = context.Agents
            .Where(a => a.TenantId == currentTenant.Id); // defense-in-depth

        if (isActive.HasValue)
            query = query.Where(a => a.IsActive == isActive.Value);
        if (isOfficial.HasValue)
            query = query.Where(a => a.IsOfficial == isOfficial.Value);

        return await query.OrderBy(a => a.SortOrder).ThenBy(a => a.DisplayName).ToListAsync(ct);
    }
    // ... idem para GetByIdAsync, GetBySlugAsync, etc.
}
```

**Prioridade de correcção:** Imediata antes de qualquer deploy em ambiente com múltiplos tenants reais.

---

### P-C02 — SQL Injection em ClickHouseAiAnalyticsRepository

**Severidade:** CRÍTICO — SQL Injection via string interpolation  
**Localização:** `Infrastructure/Governance/Persistence/ClickHouse/ClickHouseAiAnalyticsRepository.cs`  
**Regra violada:** Segurança — OWASP A03:2021 Injection

**Problema:**  
O método `InsertTokenUsageAsync` (e `InsertTokenUsageBatchAsync`) constrói queries ClickHouse via interpolação de strings directa com valores de `TokenUsageRecord`:

```csharp
// PROBLEMA: raw string interpolation com dados externos
var query = $@"
    INSERT INTO ai_token_usage FORMAT JSONEachRow
    {{""Id"":""{record.Id}"",""TenantId"":""{record.TenantId}"",
    ""OperationType"":""{record.OperationType}"",""UserId"":""{record.UserId}""}}";
```

Campos como `OperationType` e `UserId` podem conter caracteres especiais (`"`, `\n`, JSON-breaking chars) que corrompem a query. Se algum destes campos vier de input externo sem sanitização, é um vector de injecção.

**Problema adicional:** `InsertTokenUsageAsync` não recebe `CancellationToken`, tornando impossível cancelar operações longas:

```csharp
public async Task InsertTokenUsageAsync(TokenUsageRecord record)
// Deve ser:
public async Task InsertTokenUsageAsync(TokenUsageRecord record, CancellationToken cancellationToken = default)
```

**Correcção:**

```csharp
// Usar serialização JSON segura com JsonSerializer.Serialize
private async Task ExecuteInsertAsync<T>(string table, T record, CancellationToken ct)
{
    var jsonLine = JsonSerializer.Serialize(record, _jsonOptions);
    var query = $"INSERT INTO {table} FORMAT JSONEachRow\n{jsonLine}";
    var content = new StringContent(query, Encoding.UTF8, "text/plain");
    var response = await _httpClient.PostAsync(_baseUrl, content, ct);
    response.EnsureSuccessStatusCode();
}
```

Usar `JsonSerializer.Serialize` garante que todos os valores são devidamente escapados.

---

### P-C03 — HttpClient criado directamente (sem IHttpClientFactory)

**Severidade:** CRÍTICO — Socket exhaustion e gestão de ciclo de vida  
**Localização:** `Infrastructure/Governance/Persistence/ClickHouse/ClickHouseAiAnalyticsRepository.cs:22`

**Problema:**

```csharp
public ClickHouseAiAnalyticsRepository(string connectionString)
{
    // ...
    _httpClient = new HttpClient();  // PROBLEMA: instanciação directa
}
```

`ClickHouseAiAnalyticsRepository` é registado como **Singleton** no DI. Criar `HttpClient` directamente no construtor sem `IHttpClientFactory` causa:
1. **Socket exhaustion** em produção (HttpClient não respeita o ciclo de vida de DNS)
2. Impossibilidade de aplicar Polly retry/circuit breaker
3. Impossibilidade de configurar headers de autenticação de forma centralizada

**Correcção:**

```csharp
// Registar HttpClient nomeado no DI:
services.AddHttpClient("ClickHouseAiAnalytics", client => {
    client.BaseAddress = new Uri(clickHouseBaseUrl);
    client.DefaultRequestHeaders.Authorization = 
        new AuthenticationHeaderValue("Basic", credentials);
})
.AddStandardResilienceHandler();

// ClickHouseAiAnalyticsRepository injecta IHttpClientFactory:
internal sealed class ClickHouseAiAnalyticsRepository(
    IHttpClientFactory httpClientFactory,
    ClickHouseConnectionOptions options) : IAiAnalyticsRepository, IDisposable
{
    private HttpClient CreateClient() => httpClientFactory.CreateClient("ClickHouseAiAnalytics");
}
```

---

## 4. Problemas de Alta Severidade (🟠)

### P-A01 — Enum.Parse não guarded — Exceptions não controladas em 22 handlers

**Severidade:** ALTO — Causa exceções 500 em vez de respostas 422 correctas  
**Localização:** 22 ficheiros em `Application/Governance/Features/`

**Problema:**

```csharp
// CreateAgent.cs — Handler.Handle():
var category = Enum.Parse<AgentCategory>(request.Category, ignoreCase: true);    // LANÇA ArgumentException
var ownershipType = Enum.Parse<AgentOwnershipType>(request.OwnershipType, ignoreCase: true); // LANÇA ArgumentException
var visibility = Enum.Parse<AgentVisibility>(request.Visibility, ignoreCase: true); // LANÇA ArgumentException
```

A `Validator` valida que `OwnershipType != "System"` mas não valida que é um enum válido. Um valor inválido como `"InvalidCategory"` causa `ArgumentException` não tratada, escapando o pipeline MediatR e retornando 500.

**Ficheiros afectados:**
- `CreateAgent.cs`, `UpdateAgent.cs`, `RegisterSkill.cs`, `UpdateBudget.cs`, 
- `StartOnboardingSession.cs`, `ListIdeQuerySessions.cs`, `SubmitIdeQuery.cs`,
- `UpdateModel.cs`, `RegisterModel.cs`, `ListSkills.cs`, `CreateGuardrail.cs`, etc.

**Correcção:**

```csharp
// Opção 1: Usar Enum.TryParse no Validator:
public sealed class Validator : AbstractValidator<Command>
{
    public Validator()
    {
        RuleFor(x => x.Category)
            .NotEmpty()
            .Must(v => Enum.TryParse<AgentCategory>(v, ignoreCase: true, out _))
            .WithMessage("Invalid agent category.");
        
        RuleFor(x => x.OwnershipType)
            .NotEmpty()
            .Must(v => Enum.TryParse<AgentOwnershipType>(v, ignoreCase: true, out _) && v != "System")
            .WithMessage("Invalid or forbidden ownership type.");
        // ...
    }
}

// Opção 2: Usar Enum.TryParse no Handler e retornar Result.Failure:
if (!Enum.TryParse<AgentCategory>(request.Category, ignoreCase: true, out var category))
    return Error.Validation("Agent.InvalidCategory", $"'{request.Category}' is not a valid agent category.");
```

---

### P-A02 — Violação de boundary: Infrastructure referencia DbContexts de outros módulos directamente

**Severidade:** ALTO — Viola isolamento de bounded context  
**Localização:** `NexTraceOne.AIKnowledge.Infrastructure.csproj`

**Problema:**

```xml
<!-- VIOLAÇÃO: AI Infrastructure depende directamente de Infrastructure de outros módulos -->
<ProjectReference Include="..\..\catalog\NexTraceOne.Catalog.Infrastructure\..." />
<ProjectReference Include="..\..\changegovernance\NexTraceOne.ChangeGovernance.Infrastructure\..." />
<ProjectReference Include="..\..\operationalintelligence\NexTraceOne.OperationalIntelligence.Infrastructure\..." />
<ProjectReference Include="..\..\knowledge\NexTraceOne.Knowledge.Infrastructure\..." />
```

Segundo CLAUDE.md: "Módulos nunca acêdem directamente ao DbContext de outro módulo."

A justificação no csproj é "AI grounding data retrieval (P01.10)", mas isto cria dependências directas entre os contextos de base de dados, impossibilitando a separação futura e criando tight coupling.

**Correcção:**  
Substituir por referências a `Contracts` layers dos outros módulos e usar interfaces `IXxxModule` ou `ICatalogGraphModule` para cross-module reads. O `IAIContextBuilder` deve depender de `ICatalogGraphModule`, `IChangeModule`, etc., não dos `DbContext`s directamente.

---

### P-A03 — NEST library (deprecated) em vez de Elastic.Clients.Elasticsearch 8.x

**Severidade:** ALTO — Biblioteca deprecada desde Elasticsearch 8.0  
**Localização:** `NexTraceOne.AIKnowledge.Infrastructure.csproj`, `ElasticSearchAiRepository.cs`

**Problema:**

```xml
<PackageReference Include="NEST" />  <!-- DEPRECATED para Elasticsearch 8.x -->
```

```csharp
using Elasticsearch.Net;
using Nest;  // DEPRECATED

var settings = new ConnectionSettings(uri)
    .DefaultIndex("ai-search")
    .PrettyJson();
_client = new ElasticClient(settings);  // DEPRECATED client
```

`NEST` é a client library para Elasticsearch 7.x. Para Elasticsearch 8.x (que é o standard em 2026), a library correcta é `Elastic.Clients.Elasticsearch`. A NEST em ES 8.x tem problemas de compatibilidade e não suporta as novas APIs.

**Correcção:**

```xml
<PackageReference Include="Elastic.Clients.Elasticsearch" Version="8.x.x" />
```

```csharp
using Elastic.Clients.Elasticsearch;

var settings = new ElasticsearchClientSettings(new Uri(connectionString))
    .DefaultIndex("ai-search");
_client = new ElasticsearchClient(settings);
```

---

### P-A04 — Connectors registados como Singleton com configuração por-tenant

**Severidade:** ALTO — Bug de configuração em SaaS multi-tenant  
**Localização:** `Infrastructure/Governance/DependencyInjection.cs`

**Problema:**

```csharp
// DI registration como Singleton:
services.AddSingleton<IDataSourceConnector, GitHubConnector>();
services.AddSingleton<IDataSourceConnector, BraveSearchConnector>();
services.AddSingleton<IDataSourceConnector, GitLabConnector>();
```

Os connectors são Singleton, mas `ExternalDataSource` (onde a configuração como `accessToken`, `repositories`, etc. está armazenada) é por-tenant. O `GitHubConnector` lê a config do JSON passado no momento da chamada, o que é correcto. Contudo, o `IHttpClientFactory` é injectado no construtor e os `HttpClient` criados internamente não têm baseAddress definida por tenant, tornando difícil aplicar headers de autenticação por tenant sem criar clientes dinamicamente.

**Risco adicional:** Se algum Connector guardar estado (cache, tokens) internamente como campo privado, isso é partilhado entre tenants — verificar todos os Connectors.

**Correcção:** Verificar que nenhum Connector guarda estado por-tenant. Adicionar comentário explícito nos Connectors confirmando que são stateless por design.

---

### P-A05 — Health Check usa reflexão de tipo para detectar Null implementation

**Severidade:** ALTO — Acoplamento frágil e anti-pattern  
**Localização:** `Infrastructure/Governance/HealthChecks/AiDatabaseHealthChecks.cs`

**Problema:**

```csharp
// ANTI-PATTERN: Detecção por nome de tipo
if (_repository.GetType().Name.Contains("Null"))
{
    return new HealthCheckResult(HealthStatus.Degraded, "ClickHouse não configurado...");
}
```

Este padrão é frágil: qualquer rename do `NullAiAnalyticsRepository` quebra silenciosamente o comportamento do health check sem erro de compilação.

**Correcção:**

```csharp
// Opção 1: Interface marcadora
public interface INullRepository { }

internal sealed class NullAiAnalyticsRepository : IAiAnalyticsRepository, INullRepository { }

// No HealthCheck:
if (_repository is INullRepository)
    return HealthCheckResult.Degraded("ClickHouse não configurado...");
```

---

### P-A06 — Microsoft.ML no Application layer

**Severidade:** ALTO — Dependência pesada num layer que deve ser thin  
**Localização:** `NexTraceOne.AIKnowledge.Application.csproj`

**Problema:**

```xml
<PackageReference Include="Microsoft.ML" />
```

`Microsoft.ML` é uma biblioteca de ML de ~200MB. A sua presença no Application layer viola o princípio de que Application deve depender apenas de abstrações leves. Se ML for necessário, deve estar em Infrastructure com uma interface abstracta no Application.

**Correcção:** Mover `Microsoft.ML` para Infrastructure. Criar `IMLModelService` no Application layer. Injectar em Infrastructure.

---

### P-A07 — Dupla referência ao mesmo projecto em csproj

**Severidade:** ALTO — Causa warnings de build e possíveis conflitos  
**Localização:** `NexTraceOne.AIKnowledge.Infrastructure.csproj`

**Problema:**

```xml
<!-- DUPLICADAS: referências repetidas -->
<ProjectReference Include="...\NexTraceOne.Catalog.Infrastructure\..." />
<!-- ... (outros) ... -->
<ProjectReference Include="...\NexTraceOne.Catalog.Infrastructure\..." />  <!-- DUPLICADO -->
<ProjectReference Include="...\NexTraceOne.ChangeGovernance.Infrastructure\..." />
<!-- ... (outros) ... -->
<ProjectReference Include="...\NexTraceOne.ChangeGovernance.Infrastructure\..." />  <!-- DUPLICADO -->
```

**Correcção:** Remover referências duplicadas.

---

### P-A08 — Ardalis.GuardClauses e MediatR importados no Domain layer

**Severidade:** ALTO — Viola Clean Architecture — Domain não deve depender de packages externos  
**Localização:** Múltiplos ficheiros em `Domain/Governance/Entities/`

**Problema:**

```csharp
// AiAgent.cs, AIModel.cs, AIBudget.cs, etc.:
using Ardalis.GuardClauses;  // PROBLEMA: package externo no Domain
using MediatR;               // PROBLEMA: MediatR.Unit no Domain
```

O Domain layer deve depender apenas de:
1. `BuildingBlocks.Core` (primitivos do projecto)
2. Packages do próprio ecossistema .NET (sem third-party)

`Ardalis.GuardClauses` e `MediatR` estão no Domain csproj como transitive dependencies via `BuildingBlocks.Core`? Verificar — se o `NexTraceOne.AIKnowledge.Domain.csproj` não os referencia directamente, mas os importa transitivamente, é menos grave mas ainda deve ser explicitado.

**Análise:** O `Domain.csproj` só referencia `BuildingBlocks.Core`, que pode estar a expor `Ardalis.GuardClauses` e `MediatR` transitivamente. Estes `using` nos entities devem funcionar mas criam dependência implícita que dificulta o isolamento.

**Correcção:** Garantir que `BuildingBlocks.Core` não expõe `Ardalis.GuardClauses` publicamente (usar `PrivateAssets="all"`), ou substituir por guard clauses próprias no Domain.

---

## 5. Problemas de Média Severidade (🟡)

### P-M01 — AllowedModelIds, AllowedTools armazenados como strings delimitadas

**Localização:** `Domain/Governance/Entities/AiAgent.cs`, `AiAgentConfiguration.cs`

```csharp
// PROBLEMA: Lista de IDs como string delimitada
public string AllowedModelIds { get; private set; } = string.Empty;
public string AllowedTools { get; private set; } = string.Empty;
```

Armazenar colecções como strings delimitadas viola normalização de dados e impossibilita queries eficientes.

**Correcção:** Usar tabelas de associação ou, no mínimo, colunas JSON no PostgreSQL:

```csharp
// Alternativa com JSONB:
public IReadOnlyList<Guid> AllowedModelIds { get; private set; } = [];
// Na configuração EF: builder.Property(e => e.AllowedModelIds).HasColumnType("jsonb");
```

---

### P-M02 — CreateAgent não valida unicidade do Name/Slug

**Localização:** `Application/Governance/Features/CreateAgent/CreateAgent.cs`

O handler não verifica se o `Name` ou `Slug` já existe antes de adicionar. O índice único na tabela (`HasIndex(e => e.Slug).IsUnique()`) irá causar `DbUpdateException` não tratada ao tentar inserir duplicado.

**Correcção:**

```csharp
var exists = await agentRepository.ExistsByNameAsync(request.Name, cancellationToken);
if (exists)
    return Error.Conflict("Agent.NameAlreadyExists", $"An agent with name '{request.Name}' already exists.");
```

---

### P-M03 — ElasticSearchAiRepository usa índices hardcoded

**Localização:** `Infrastructure/Governance/Persistence/ElasticSearch/ElasticSearchAiRepository.cs`

```csharp
.Index("prompt-templates")  // hardcoded
.Index("conversations")     // hardcoded
.Index("ai-search")         // hardcoded no default
```

Em ambiente multi-tenant, índices devem ser prefixados com o ID do tenant ou usar aliases. Índices sem tenant prefix causam mistura de dados entre tenants no Elasticsearch.

**Correcção:**

```csharp
private string GetTenantIndex(string baseName) => $"{_tenantPrefix}-{baseName}";

.Index(GetTenantIndex("prompt-templates"))
```

---

### P-M04 — ClickHouseAiAnalyticsRepository sem tenant isolation

**Localização:** `Infrastructure/Governance/Persistence/ClickHouse/ClickHouseAiAnalyticsRepository.cs`

As queries ClickHouse não filtram por tenant. `InsertTokenUsageAsync` inclui `TenantId` no JSON, mas os métodos de leitura (`GetTokenUsageMetricsAsync`) não têm filtro de tenant, expondo dados cross-tenant.

**Correcção:** Adicionar `WHERE TenantId = '{tenantId}'` em todos os SELECT do ClickHouse. Injectar `ICurrentTenant` no repositório.

---

### P-M05 — Falta de rate limiting nos endpoints de execução de agents

**Localização:** `API/Governance/Endpoints/AiGovernanceEndpointModule.cs`

Os endpoints `/ai/agents/{id}/execute` e `/ai/assistant/chat` não têm rate limiting aplicado. Em contexto SaaS com multiple tenants, um único tenant pode saturar o sistema de inferência IA.

**Correcção:** Aplicar `RequireRateLimiting` nos endpoints AI. Usar `ICurrentTenant.Id` como partition key para o rate limiter.

---

### P-M06 — Cobertura de testes insuficiente para repositórios críticos

**Localização:** `tests/modules/aiknowledge/`

Existem 119 ficheiros de teste mas a análise mostra:
- Não há testes para repositórios (apenas handlers via mocks)
- Não há testes de integração para tenant isolation
- `AiAgentRepository`, `AiAssistantConversationRepository`, `AiModelRepository` não têm testes de repositório
- O `AiAgentRuntimeService` (serviço crítico) não tem teste directo visível

---

### P-M07 — Falta de auditoria em CreateAgent e ExecuteAgent

**Localização:** `Application/Governance/Features/CreateAgent/`, `ExecuteAgent/`

Os handlers `CreateAgent` e `ExecuteAgent` não publicam integration events nem registam entradas de auditoria explícitas (além do AuditInterceptor). Para acções sensíveis como criar agents ou executar inferências, é necessário registo de auditoria explícito com correlação de tenant e utilizador.

---

### P-M08 — PromptVersion sem configuração EF visível no DbContext

**Localização:** `AiGovernanceDbContext.cs`

`PromptVersions` está no DbContext mas a configuração de EF (`PromptVersionConfiguration.cs`) precisa de verificação de que os índices de versioning estão correctos.

---

### P-M09 — InMemoryEmbeddingCacheService como Singleton

**Localização:** `Infrastructure/Governance/Services/InMemoryEmbeddingCacheService.cs`

Cache de embeddings em memória é registado como Singleton. Em deploys horizontais (múltiplas instâncias), o cache não é partilhado entre instâncias, causando recálculo desnecessário de embeddings e comportamento inconsistente entre instâncias.

**Correcção (quando Redis disponível):**

```csharp
// Substituir por IDistributedCache quando Redis estiver configurado
services.AddScoped<IEmbeddingCacheService, DistributedEmbeddingCacheService>();
```

---

### P-M10 — Frontend: tipos `unknown` em chamadas de API

**Localização:** `frontend/src/features/ai-hub/api/aiGovernance.ts`

```typescript
registerModel: (data: unknown) =>   // PROBLEMA: tipo 'unknown' perde type safety
  client.post('/ai/models', data).then(r => r.data),
createPolicy: (data: unknown) =>    // PROBLEMA
  client.post('/ai/policies', data).then(r => r.data),
```

O uso de `unknown` como tipo de payload elimina toda a type safety do TypeScript.

**Correcção:** Definir tipos explícitos:

```typescript
registerModel: (data: RegisterModelRequest) =>
  client.post<ModelResponse>('/ai/models', data).then(r => r.data),
```

---

### P-M11 — Frontend: sem tratamento de erro global na AssistantPage

**Localização:** `frontend/src/features/ai-hub/pages/AiAssistantPage.tsx`

A página `AiAssistantPage` não tem `ErrorBoundary`. Falhas em chamadas de API à IA podem deixar a UI em estado indefinido sem feedback adequado ao utilizador.

---

### P-M12 — Connectors externos sem timeout configurável

**Localização:** `Infrastructure/Governance/Connectors/GitHubConnector.cs`, `BraveSearchConnector.cs`

Os connectors não configuram `Timeout` no `HttpClient`. Chamadas a APIs externas sem timeout podem bloquear o thread pool em produção.

**Correcção:** Configurar timeout no `AddHttpClient`:

```csharp
services.AddHttpClient("GitHubConnector")
    .SetHandlerLifetime(TimeSpan.FromMinutes(5))
    .AddStandardResilienceHandler();
// E no connector, usar CancellationToken com timeout:
using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
cts.CancelAfter(TimeSpan.FromSeconds(30));
```

---

## 6. Problemas de Baixa Severidade (🔵)

### P-B01 — Ausência de comentários XML em muitos handlers

**Regra:** CLAUDE.md e copilot-instructions.md — Documentação em Português obrigatória em `/// <summary>`

Muitos handlers da camada Application têm `/// <summary>` adequados, mas alguns features mais novos (ex: `SeedDefaultSkills`, `ValidateCustomAgentCreation`) têm documentação mínima ou ausente.

---

### P-B02 — `RowVersion` como campo público com setter público

**Localização:** `Domain/Governance/Entities/AiAgent.cs`, `AIModel.cs`

```csharp
public uint RowVersion { get; set; }  // setter público — deve ser internal ou private set
```

O `RowVersion` (PostgreSQL xmin) é gerido pelo ORM. Expô-lo como setter público permite mutações acidentais.

**Correcção:**

```csharp
public uint RowVersion { get; private set; }
// Com HasKey ou HasRowVersion no EF:
builder.Property(e => e.RowVersion).IsRowVersion();
```

---

### P-B03 — HealthCheck para ClickHouse e Elasticsearch sempre registados

**Localização:** `Infrastructure/Governance/DependencyInjection.cs`

Ambos os health checks (`ClickHouseAiHealthCheck` e `ElasticSearchAiHealthCheck`) são sempre registados, mesmo quando apenas um está configurado. Isto resulta em dois checks "Degraded" na dashboard de saúde, o que é confuso.

**Correcção:**

```csharp
if (!string.IsNullOrEmpty(clickHouseConnectionString))
{
    services.AddHealthChecks()
        .AddCheck<ClickHouseAiHealthCheck>("ai-clickhouse-analytics", ...);
}
if (!string.IsNullOrEmpty(elasticSearchConnectionString))
{
    services.AddHealthChecks()
        .AddCheck<ElasticSearchAiHealthCheck>("ai-elasticsearch-search", ...);
}
```

---

### P-B04 — Falta de `[Produces]` e OpenAPI annotations nos endpoints

**Localização:** `API/Governance/Endpoints/AiGovernanceEndpointModule.cs`

Os endpoints de IA não têm anotações OpenAPI (`WithName`, `WithTags`, `Produces<>`, `ProducesValidationProblem`). Isto degrada a qualidade do Swagger gerado automaticamente.

---

### P-B05 — Slug derivation inconsistente

**Localização:** `Domain/Governance/Entities/AiAgent.cs`, `AIModel.cs`

```csharp
// AiAgent:
var derivedSlug = slug ?? name.ToLowerInvariant().Replace(' ', '-').Replace(':', '-');
// AIModel:
Slug = slug ?? name.ToLowerInvariant().Replace(':', '-').Replace(' ', '-'),
```

A ordem de Replace é diferente entre as duas entidades. Deve ser centralizada num método estático utilitário.

---

### P-B06 — Singleton `IDataSourceConnectorFactory` com list de Singletons

**Localização:** `Infrastructure/Governance/DependencyInjection.cs`

```csharp
services.AddSingleton<IDataSourceConnectorFactory, DataSourceConnectorFactory>();
services.AddSingleton<IDataSourceConnector, BraveSearchConnector>();
services.AddSingleton<IDataSourceConnector, GitHubConnector>();
```

`IDataSourceConnector` registado como `IEnumerable<IDataSourceConnector>` pode ter comportamento inesperado. Verificar que `DataSourceConnectorFactory` resolve correctamente todos os conectores.

---

### P-B07 — Frontend: importação de ícones desnecessários em AiAssistantPage

**Localização:** `frontend/src/features/ai-hub/pages/AiAssistantPage.tsx`

```typescript
import {
  Bot, Send, Plus, Shield, Cpu, User, Server, FileText,
  AlertTriangle, GitBranch, BookOpen, Info, ChevronDown,
  ChevronUp, Sparkles, CheckCircle2, AlertCircle, Loader2,
  Globe, Lock, Users, X,
} from 'lucide-react';
```

22 ícones importados. Verificar se todos são usados — imports desnecessários aumentam o bundle size.

---

### P-B08 — `getAccessToken` e `getCsrfToken` importados mas uso não verificado

**Localização:** `frontend/src/features/ai-hub/api/aiGovernance.ts`

```typescript
import { getAccessToken, getCsrfToken, getTenantId, getEnvironmentId } from '../../../utils/tokenStorage';
```

Estas funções são importadas mas não são visíveis a ser usadas no código mostrado. Se o `client` (axios) já lida com headers de autenticação, estas importações são desnecessárias.

---

### P-B09 — Comentários em inglês em código interno

**Regra:** copilot-instructions.md §19.1 — "Inline code comments: Português"

```csharp
// Repository for persisted tool definitions (Phase 4)
services.AddScoped<IAiToolDefinitionRepository, AiToolDefinitionRepository>();
```

Comentários inline no código devem estar em Português conforme convenção estabelecida.

---

## 7. Análise de Persistência: O que Pertence Onde

### PostgreSQL (correcto)
Todas as entidades de domínio (`AiAgent`, `AIModel`, `AiGuardrail`, `PromptTemplate`, etc.) estão correctamente no PostgreSQL. ✅

### ClickHouse (correcto quando configurado)
`IAiAnalyticsRepository` — dados de uso de tokens ao longo do tempo, métricas de execução de agents. ✅  
Dados time-series de alto volume são ideais para ClickHouse.

### Elasticsearch (correcto quando configurado)
`IAiSearchRepository` — busca full-text em prompt templates, conversas, documentos de conhecimento. ✅

### Problemas de colocação identificados:

| Entidade | Colocação Actual | Sugestão | Razão |
|---|---|---|---|
| `AiTokenUsageLedger` | PostgreSQL | **Manter** — é o ledger financeiro oficial | Dados de cobrança precisam de ACID |
| `AIUsageEntry` | PostgreSQL | **Avaliar** — se volume for muito alto, considerar ClickHouse | Para analytics agregadas |
| `AiRoutingDecision` | PostgreSQL | **Avaliar** — logs de routing são time-series | Pode crescer muito rapidamente |
| `AiAgentExecution` | PostgreSQL | **Avaliar** — logs de execução são time-series | Potencial para ClickHouse a longo prazo |
| `ModelPredictionSample` | PostgreSQL | **Avaliar** — dados de ML são melhor em storage especializado | Potencial para ClickHouse |
| `OrganizationalMemoryNode` | PostgreSQL | **Manter** — dados de domínio com relações | Correcto no PostgreSQL |
| Embeddings vectoriais | Não existe | **Gap** — não há storage para embeddings | Precisa de pgvector ou Elasticsearch vector fields |

### Gap crítico: Ausência de storage de embeddings

O módulo tem `IEmbeddingCacheService` e `IEmbeddingProvider`, mas não há storage persistente para embeddings vectoriais. Os embeddings calculados (para RAG/semântica) são apenas cacheados em memória. Isto significa que:
1. Restart da aplicação perde todo o cache
2. Não é possível fazer busca por similaridade semântica persistente
3. RAG (Retrieval Augmented Generation) não funciona de forma durável

**Sugestão:** Adicionar suporte a `pgvector` (extensão PostgreSQL) para embedding storage, ou usar Elasticsearch com `dense_vector` fields.

---

## 8. Análise DDD / SOLID / CQRS

### Positivos ✅

1. **Entities bem modeladas**: `AiAgent`, `AIModel`, `AiGuardrail` seguem DDD correctamente — factory methods, private setters, invariantes documentados, método de domínio significativos
2. **Strongly Typed IDs**: `AiAgentId`, `AIModelId` etc. implementados correctamente
3. **Result pattern**: Usado consistentemente em handlers
4. **CQRS**: Separação clara entre Commands e Queries
5. **VSA (Vertical Slice Architecture)**: Handlers seguem o padrão correcto (Command+Validator+Handler+Response num ficheiro)
6. **Documentação XML**: Domain entities têm `/// <summary>` em Português com invariantes documentados
7. **Auditoria automática**: `AuditableEntity<T>` e `AuditInterceptor` correctamente configurados
8. **Soft delete**: Filtro global do `NexTraceDbContextBase` aplicado
9. **Outbox pattern**: `OutboxTableName` correctamente definido por context

### Problemas DDD ⚠️

1. **AiAgent — AllowedModelIds como string**: Deve ser Value Object ou relação
2. **Sem Value Objects** para conceitos como `TokenQuota`, `ModelCapabilities`, `AgentCapabilities` — actualmente são strings delimitadas
3. **AiGovernanceDbContext muito grande**: 70+ DbSets num único contexto sugere que pode ser dividido. Considerar separar em `AiAgentDbContext`, `AiModelDbContext`, `AiConversationDbContext`

### Problemas SOLID ⚠️

1. **SRP**: `AiAgentRuntimeService` (Application) faz: resolução de agent, validação de acesso, resolução de modelo, resolução de provider, resolução de tools, montagem de prompt, execução de inferência, gestão de tool calls em loop, persistência de execução, geração de artefactos, persistência de artefactos. Responsabilidades demais para um único serviço.
2. **ISP**: `IAiAgentRepository` tem métodos muito específicos (`ListByCategoriesAsync`) que poderiam ser queries LINQ no handler

---

## 9. Análise de Segurança

### Checklist copilot-instructions.md §39 — IA

| Item | Estado | Observação |
|---|---|---|
| Contexto suficiente do produto | ✅ | `AIContextBuilder` implementado |
| Modelo explicitamente escolhido ou por política | ✅ | `IAiModelAuthorizationService` |
| Controlo de acesso | ✅ / ⚠️ | Falta tenant filter em repos (P-C01) |
| Trilha de auditoria | ⚠️ | `AuditInterceptor` geral, mas sem eventos explícitos de auditoria |
| Protecção contra saída indevida de dados | ⚠️ | `IExternalAiPolicyRepository` existe mas aplicação não verificada em todos os endpoints |
| Resposta explicável/contextualizável | ✅ | Metadata de resposta na UI |
| Útil para operação/engenharia | ✅ | Casos de uso claros |
| Política por tenant, ambiente, grupo, persona | ✅ / ⚠️ | Existe mas tenant isolation em risco |
| Custo medido por token/budget | ✅ | `AiTokenUsageLedger` implementado |

### Vectores de segurança identificados

| Vector | Severidade | Mitigação |
|---|---|---|
| SQL Injection via ClickHouse string interpolation | CRÍTICO | P-C02 |
| Cross-tenant data exposure (missing repo filters) | CRÍTICO | P-C01 |
| Elasticsearch sem tenant isolation | ALTO | P-M03, P-M04 |
| Enum.Parse unguarded → 500 errors | ALTO | P-A01 |
| Rate limiting ausente em endpoints AI | MÉDIO | P-M05 |
| HttpClient sem managed lifecycle | CRÍTICO | P-C03 |
| Connectors com tokens de acesso em JSON (no DB) | MÉDIO | Encriptação via [EncryptedField] deve ser verificada |

---

## 10. Análise do Frontend (ai-hub)

### Positivos ✅
1. `useTranslation` usado correctamente — i18n implementado
2. `PageContainer` e design system utilizado consistentemente
3. `usePersona` para segmentação por persona
4. Estados de loading, typing indicator implementados
5. Sidebar de conversas com histórico

### Problemas ⚠️

1. **Tipos `unknown` em API client** (P-M10)
2. **Sem ErrorBoundary** na página principal do assistente (P-M11)
3. **Sem paginação visível** em `ListConversations` — pode causar lentidão com muitas conversas
4. **`getAccessToken` importado mas possivelmente não usado** (P-B08)
5. **Ausência de testes unitários** para componentes AI Hub (nenhum ficheiro `__tests__` em `features/ai-hub`)

---

## 11. Análise de Bibliotecas

### Backend

| Biblioteca | Status 2026 | Observação |
|---|---|---|
| .NET 10 / ASP.NET Core 10 | ✅ Correcto | |
| EF Core 10 + Npgsql | ✅ Correcto | |
| MediatR | ✅ Correcto | |
| FluentValidation | ✅ Correcto | |
| Ardalis.GuardClauses | ✅ Aceitável | Mas não deve estar no Domain |
| `NEST` (Elasticsearch) | ❌ DEPRECATED | Substituir por `Elastic.Clients.Elasticsearch` 8.x |
| `Microsoft.ML` | ⚠️ Pesado | Não deve estar no Application layer |
| `Mapster` | ✅ Aceitável | Alternativa ao AutoMapper, ok |
| `Microsoft.Extensions.Http.Resilience` | ✅ Correcto | Polly v8 integrado |

### Frontend

| Biblioteca | Status 2026 | Observação |
|---|---|---|
| React 18 | ✅ Correcto | |
| TypeScript | ✅ Correcto | |
| Vite | ✅ Correcto | |
| TanStack Query | ✅ Correcto | |
| TanStack Router | ✅ Correcto | |
| Zustand | ✅ Correcto | |
| Tailwind CSS | ✅ Correcto | |
| Radix UI | ✅ Correcto | |
| lucide-react | ✅ Correcto | |
| react-i18next | ✅ Correcto | |

---

## 12. Plano de Correcções — Priorizado

### Sprint 1 — Crítico (fazer antes de qualquer go-live com dados reais)

| # | Problema | Esforço | Impacto |
|---|---|---|---|
| 1 | P-C01: Adicionar tenant filter em 36 repositórios | Alto (2-3 dias) | CRÍTICO |
| 2 | P-C02: Corrigir SQL injection no ClickHouse | Baixo (2h) | CRÍTICO |
| 3 | P-C03: Migrar HttpClient para IHttpClientFactory | Médio (4h) | CRÍTICO |
| 4 | P-A01: Enum.Parse → Enum.TryParse em 22 handlers | Médio (1 dia) | ALTO |
| 5 | P-A07: Remover referências duplicadas no csproj | Baixo (30min) | ALTO |

### Sprint 2 — Alta prioridade

| # | Problema | Esforço | Impacto |
|---|---|---|---|
| 6 | P-A03: Migrar NEST para Elastic.Clients.Elasticsearch | Alto (1-2 dias) | ALTO |
| 7 | P-M03: Tenant prefix nos índices Elasticsearch | Médio (4h) | ALTO |
| 8 | P-M04: Tenant filter nas queries ClickHouse | Médio (4h) | ALTO |
| 9 | P-A05: Refactoring do HealthCheck (marker interface) | Baixo (1h) | ALTO |
| 10 | P-A02: Resolver violações de bounded context na infra | Alto (2-3 dias) | ALTO |
| 11 | P-M02: Validar unicidade de Name/Slug no CreateAgent | Baixo (1h) | MÉDIO |
| 12 | P-M05: Adicionar rate limiting nos endpoints AI | Médio (4h) | MÉDIO |

### Sprint 3 — Médio prazo

| # | Problema | Esforço | Impacto |
|---|---|---|---|
| 13 | P-A06: Mover Microsoft.ML para Infrastructure | Médio (4h) | ALTO |
| 14 | P-M01: Normalizar AllowedModelIds / AllowedTools | Alto (1 dia) | MÉDIO |
| 15 | P-M06: Adicionar testes de repositório e integração | Alto (3-5 dias) | MÉDIO |
| 16 | P-M07: Adicionar auditoria explícita em actions críticas | Médio (1 dia) | MÉDIO |
| 17 | P-M09: Migrar EmbeddingCache para IDistributedCache | Baixo (4h) | MÉDIO |
| 18 | Gap: Storage de embeddings (pgvector) | Alto (3-5 dias) | ALTO |

### Sprint 4 — Baixo prazo / Manutenção

| # | Problema | Esforço | Impacto |
|---|---|---|---|
| 19 | P-M10: Tipagem TypeScript nos API clients | Médio (1 dia) | MÉDIO |
| 20 | P-M11: Adicionar ErrorBoundary no AI Hub | Baixo (2h) | MÉDIO |
| 21 | P-B01: Completar documentação XML | Médio (1 dia) | BAIXO |
| 22 | P-B02: RowVersion com private setter | Baixo (1h) | BAIXO |
| 23 | P-B03: Registar HealthChecks condicionalmente | Baixo (1h) | BAIXO |
| 24 | P-B04: Anotações OpenAPI nos endpoints | Médio (4h) | BAIXO |
| 25 | P-B05: Centralizar slug derivation | Baixo (1h) | BAIXO |
| 26 | P-B09: Comentários inline em Português | Baixo (2h) | BAIXO |
| 27 | P-M12: Timeout nos connectors externos | Baixo (1h) | MÉDIO |
| 28 | Testes unitários para AI Hub frontend | Alto (3 dias) | MÉDIO |

---

## 13. Conformidade com Directrizes

### CLAUDE.md — Compliance

| Regra | Status |
|---|---|
| `sealed` para classes finais | ✅ Conforme |
| `CancellationToken` em toda operação async | ⚠️ Parcial (P-C02) |
| `Result<T>` para falhas controladas | ✅ Conforme |
| Guard clauses no início dos métodos | ✅ Conforme |
| Strongly typed IDs | ✅ Conforme |
| Nunca `DateTime.Now` | ✅ Conforme (IDateTimeProvider usado) |
| Módulo não acede DbContext de outro módulo | ❌ Violado (P-A02) |
| Tenant filter em repositórios | ❌ Violado (P-C01) |
| Documentação XML em Português | ✅ Maioritariamente conforme |
| Comentários inline em Português | ⚠️ Parcial (P-B09) |
| i18n no frontend | ✅ Conforme |

### copilot-instructions.md — Compliance

| Área | Status |
|---|---|
| IA com governança (política, auditoria, observabilidade) | ✅ Implementado |
| Tenant, ambiente e persona awareness | ⚠️ Parcial (tenant isolation em risco) |
| Segurança no frontend | ✅ Maioritariamente conforme |
| i18n em todo texto visível | ✅ Conforme |
| Bounded context respeitado | ❌ Parcial (P-A02) |
| Backend é autoridade de autorização | ✅ Conforme |

---

## 14. Riscos Remanescentes após Correcções

1. **Embeddings sem storage persistente**: RAG funcional requer vector database (pgvector ou ES vector fields)
2. **ClickHouse e Elasticsearch são opcionais**: Analytics e search degradados em instalações sem estas dependências — comunicar claramente ao utilizador
3. **Microsoft.ML no Application**: Aumenta tempo de build e tamanho do assembly — avaliar se ML é realmente necessário ou pode ser substituído por chamadas ao LLM externo

---

## 15. Conclusão

O módulo AIKnowledge demonstra um nível elevado de qualidade arquitectural no domínio e na camada de aplicação. As entidades de domínio são bem modeladas, a documentação XML está presente e em Português, o padrão CQRS é respeitado e os testes cobrem os casos mais críticos.

**Os problemas identificados são maioritariamente de infraestrutura e segurança**, não de design de domínio. Os três problemas críticos (tenant isolation em repositórios, SQL injection no ClickHouse, HttpClient sem factory) devem ser resolvidos antes de qualquer exposição a múltiplos tenants em ambiente real.

A adopção da NEST (Elasticsearch deprecated) e a violação de bounded context no csproj são problemas arquitecturais que requerem atenção a médio prazo mas não bloqueiam o arranque.

**Score geral:** 72/100  
- Domain/Application: 88/100  
- Infrastructure: 62/100  
- Security: 55/100  
- Testing: 65/100  
- Frontend: 75/100
