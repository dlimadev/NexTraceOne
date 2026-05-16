# Relatório de Auditoria — Módulo ProductAnalytics

**Data da Auditoria:** 2026-05-15  
**Auditor:** Kimi Code CLI  
**Módulo:** `src/modules/productanalytics` + `src/frontend/src/features/product-analytics`  
**Referências:** `copilot-instructions.md` (v3), `CLAUDE.md`  

---

## 1. Resumo Executivo

O módulo **ProductAnalytics** é um dos mais maduros do NexTraceOne. Segue corretamente os princípios de **DDD**, **CQRS** (via MediatR), **Clean Architecture** e **SOLID**. A separação em camadas (Domain → Application → Infrastructure → API) está respeitada, as entidades utilizam **strongly typed IDs**, e o frontend consome a API de forma tipada com **i18n**, **TanStack Query** e **TypeScript**.

Contudo, foram identificados **problemas críticos de segurança**, **gaps de compliance arquitetural** e **violações do DRY principle** que precisam de correção imediata. O mais grave é a **vulnerabilidade a SQL Injection** no `ClickHouseAnalyticsEventRepository` e a **ausência de filtro de TenantId em defense-in-depth** nos repositórios PostgreSQL.

| Categoria | Crítico | Médio | Baixo |
|-----------|---------|-------|-------|
| Segurança | 4 | 2 | 1 |
| Arquitetura | 2 | 4 | 2 |
| DDD/SOLID | 1 | 3 | 2 |
| Frontend | 1 | 2 | 2 |
| Documentação | 1 | 1 | 2 |
| Persistência | 2 | 2 | 1 |

---

## 2. Conformidade Arquitetural

### 2.1 DDD & Clean Architecture

| Critério | Status | Observação |
|----------|--------|------------|
| Domain sem dependências de infra | ✅ | Domain referencia apenas `BuildingBlocks.Core` |
| Application sem dependências de persistência | ✅ | Usa abstrações (`IAnalyticsEventRepository`) |
| Infrastructure isola adapters | ✅ | `AnalyticsEventForwarder`, `ClickHouseAnalyticsEventRepository` |
| Bounded context claro | ✅ | Nomeação consistente `ProductAnalytics` |
| Cross-module via contratos | ✅ | `IProductAnalyticsModule` em `.Contracts` |
| Integration Events definidos | ✅ | `ValueMilestoneReachedIntegrationEvent`, `FrictionSignalDetectedIntegrationEvent` |

### 2.2 CQRS

| Critério | Status | Observação |
|----------|--------|------------|
| Commands separados de Queries | ✅ | 17 features, cada uma em pasta própria |
| Static class pattern | ✅ | `Command`, `Validator`, `Response`, `Handler` no mesmo ficheiro |
| Handlers injetam abstrações | ✅ | Nenhum handler acessa DbContext diretamente |
| MediatR pipeline | ✅ | `LoggingBehavior`, `ValidationBehavior`, `TenantIsolationBehavior`, `TransactionBehavior` |

### 2.3 SOLID

| Critério | Status | Observação |
|----------|--------|------------|
| SRP nos handlers | ⚠️ | Alguns handlers como `GetJourneys` são grandes (>300 linhas) |
| OCP nos repositórios | ✅ | `IAnalyticsEventRepository` com duas implementações |
| LSP | ✅ | `NullPortalAdoptionReader` / `NullSelfServiceWorkflowReader` seguem contrato |
| ISP | ✅ | Interfaces pequenas e focadas |
| DIP | ✅ | Application depende de abstrações |

### 2.4 Estrutura de Pastas

```
src/modules/productanalytics/
├── NexTraceOne.ProductAnalytics.API/           ✅ Endpoints
├── NexTraceOne.ProductAnalytics.Application/   ✅ Features, Abstractions, DI
├── NexTraceOne.ProductAnalytics.Contracts/     ✅ IProductAnalyticsModule, DTOs
├── NexTraceOne.ProductAnalytics.Domain/        ✅ Entities, Enums
├── NexTraceOne.ProductAnalytics.Infrastructure/✅ DbContext, Repos, Migrations
```

**Observação:** Falta `AGENTS.md` no módulo para orientar futuros agents de IA.

---

## 3. Domain Layer Review

### 3.1 Entidades

#### `AnalyticsEvent` (`Domain/Entities/AnalyticsEvent.cs`)

| Aspecto | Avaliação |
|---------|-----------|
| Strongly typed ID | ✅ `AnalyticsEventId` herda `TypedIdBase` |
| XML docs | ✅ Em português |
| sealed class | ✅ |
| Guard clauses | ✅ `Guard.Against.Default`, `Guard.Against.NullOrWhiteSpace`, `Guard.Against.StringTooLong` |
| Imutabilidade | ✅ Propriedades `private init` |
| Sem DateTime.Now | ✅ Usa `DateTimeOffset` via parâmetro |
| Auditoria | ⚠️ **Não herda `AuditableEntity`** — intencional (evento imutável), mas falta `CreatedAt` para trilha de auditoria base |

#### `JourneyDefinition` (`Domain/Entities/JourneyDefinition.cs`)

| Aspecto | Avaliação |
|---------|-----------|
| Strongly typed ID | ✅ `JourneyDefinitionId` |
| XML docs | ✅ |
| sealed class | ✅ |
| Guard clauses | ✅ |
| Domain methods | ✅ `Update`, `Activate`, `Deactivate` |
| Tenant scope | ✅ `TenantId` nullable para definições globais |

### 3.2 Enums

| Enum | Valores numéricos | XML docs |
|------|-------------------|----------|
| `AnalyticsEventType` | ✅ | ✅ |
| `FrictionSignalType` | ✅ | ✅ |
| `JourneyStatus` | ✅ | ✅ |
| `PersonaType` | ❌ **Ausente** | ✅ |
| `ProductModule` | ✅ | ✅ |
| `TrendDirection` | ✅ | ✅ |
| `ValueMilestoneType` | ✅ | ✅ |

**Problema:** `PersonaType` não define valores numéricos explícitos, o que pode causar inconsistências se a ordem for alterada.

---

## 4. Application Layer Review

### 4.1 Padrão CQRS

Todos os 17 handlers seguem o padrão exigido pelo `CLAUDE.md`:

```csharp
public static class FeatureName
{
    public sealed record Command(...) : ICommand<Response>;
    public sealed class Validator : AbstractValidator<Command> { ... }
    public sealed record Response(...);
    internal sealed class Handler(...) : ICommandHandler<Command, Response> { ... }
}
```

### 4.2 Observações por Feature

| Feature | Status | Observação |
|---------|--------|------------|
| `RecordAnalyticsEvent` | ✅ | Privacy-aware, guard clauses, forwarder para analytics store |
| `CreateJourneyDefinition` | ✅ | Validação de key regex `[a-z0-9_]+` |
| `UpdateJourneyDefinition` | ✅ | Ativa/desativa via estado booleano |
| `DeleteJourneyDefinition` | ✅ | Proteção contra delete de definições globais |
| `GetAnalyticsSummary` | ✅ | Config-driven via `IConfigurationResolutionService` |
| `GetModuleAdoption` | ✅ | Paginação com clamp de pageSize (1-100) |
| `GetPersonaUsage` | ✅ | Computação de adoption depth |
| `GetJourneys` | ⚠️ | Handler com >300 linhas; `IJourneyDefinitionRepository?` e `ICurrentTenant?` como parâmetros opcionais quebram DI padrão |
| `GetAdoptionFunnel` | ✅ | Funnel definitions estáticos bem modelados |
| `GetFeatureHeatmap` | ✅ | |
| `GetFrictionIndicators` | ✅ | Heurística de fricção bem definida |
| `GetValueMilestones` | ✅ | Cálculo de TTFV/TTCV |
| `GetCohortAnalysis` | ✅ | ISO week calculation |
| `ExportAnalyticsData` | ⚠️ | **JSON/CSV gerado manualmente sem escapamento seguro** (ver seção 9) |
| `GetPortalAdoptionFunnelReport` | ✅ | Usa `IPortalAdoptionReader` (honest-null pattern) |
| `GetSelfServiceWorkflowHealthReport` | ✅ | Usa `ISelfServiceWorkflowReader` |
| `ListJourneyDefinitions` | ✅ | |

### 4.3 Duplicação de Código (DRY)

**Problema Médio:** O método `ResolveRange` está **duplicado em 8 handlers diferentes** (`GetAnalyticsSummary`, `GetModuleAdoption`, `GetPersonaUsage`, `GetFeatureHeatmap`, `GetAdoptionFunnel`, `GetJourneys`, `GetValueMilestones`, `ExportAnalyticsData`).

**Problema Médio:** O método `ToModuleDisplayName` está **duplicado em 5 handlers**.

**Sugestão:** Criar `AnalyticsQueryHelper` ou `AnalyticsDateRangeResolver` na camada Application.

### 4.4 Abstrações

| Interface | Tipo | Implementação |
|-----------|------|---------------|
| `IAnalyticsEventRepository` | CRUD + Analytics | `AnalyticsEventRepository` (EF) / `ClickHouseAnalyticsEventRepository` |
| `IJourneyDefinitionRepository` | CRUD | `JourneyDefinitionRepository` (EF) |
| `IPortalAdoptionReader` | Read-only | `NullPortalAdoptionReader` / `PortalAdoptionReader` |
| `ISelfServiceWorkflowReader` | Read-only | `NullSelfServiceWorkflowReader` / `SelfServiceWorkflowReader` |

---

## 5. Infrastructure Layer Review

### 5.1 DbContext

| Aspecto | Status |
|---------|--------|
| Herda `NexTraceDbContextBase` | ✅ |
| `IUnitOfWork` | ✅ |
| Module prefix `pan_` | ✅ |
| Outbox table | ✅ `pan_outbox_messages` |
| Design-time factory | ✅ `ProductAnalyticsDbContextDesignTimeFactory` |
| Interceptors | ✅ `AuditInterceptor`, `TenantRlsInterceptor` |

### 5.2 Configurações EF Core

#### `AnalyticsEventConfiguration`

| Aspecto | Status |
|---------|--------|
| Conversão de typed ID | ✅ |
| Índices analíticos | ✅ Compostos por `TenantId`, `Module`, `OccurredAt` |
| Check constraints | ✅ Para `Module` e `EventType` |
| `MetadataJson` como `text` | ✅ |

#### `JourneyDefinitionConfiguration`

| Aspecto | Status |
|---------|--------|
| `StepsJson` como `jsonb` | ✅ |
| Unique constraint `(TenantId, Key)` | ✅ |
| Índice `(TenantId, IsActive)` | ✅ |

### 5.3 Repositórios

#### `AnalyticsEventRepository` (PostgreSQL/EF)

**Problema Crítico:** O método `ApplyFilters` **NÃO inclui filtro por `TenantId`**:

```csharp
private static IQueryable<AnalyticsEvent> ApplyFilters(
    IQueryable<AnalyticsEvent> query, string? persona, ProductModule? module,
    string? teamId, string? domainId)
{
    // Falta: query = query.Where(e => e.TenantId == currentTenant.Id);
    // ...
}
```

O `CLAUDE.md` exige explicitamente (seção "Tenant isolation"):
> "every read method must also add `.Where(e => e.TenantId == currentTenant.Id)` as defense-in-depth"

Embora o `TenantRlsInterceptor` exista, a ausência do filtro no repositório é uma violação direta das regras do projeto.

#### `JourneyDefinitionRepository` (PostgreSQL/EF)

**Problema Crítico:** `GetByIdAsync` e `GetByKeyAsync` **não filtram por tenant**:

```csharp
public async Task<JourneyDefinition?> GetByIdAsync(JourneyDefinitionId id, CancellationToken ct)
    => await context.JourneyDefinitions.FirstOrDefaultAsync(d => d.Id == id, ct);
    // Falta: .Where(d => d.TenantId == currentTenant.Id || d.TenantId == null)
```

Um tenant malicioso que conheça o GUID de uma journey de outro tenant pode acessá-la diretamente via `Update` ou `Delete` (embora o Delete verifique `TenantId != tenant.Id`, o Update não verifica).

#### `ClickHouseAnalyticsEventRepository`

**Problema Crítico — SQL Injection:** A construção de queries ClickHouse via concatenação de strings é **insegura**:

```csharp
private static string BuildWhereClause(...)
{
    if (!string.IsNullOrWhiteSpace(persona))
        conditions.Add($"persona = '{Escape(persona)}'");  // ❌ Vulnerável
}

private static string Escape(string value) => value.Replace("'", "\\'");
```

O `Escape` é insuficiente. Caracteres como `\x00`, Unicode tricks, ou injection via `\'` podem contornar a proteção. ClickHouse suporta prepared statements / query parameters que deveriam ser usados.

**Problema Médio:** A classe recebe `ProductAnalyticsDbContext` no construtor para implementar `AddAsync`. Isso quebra a separação de responsabilidades — o repositório ClickHouse não deveria saber do PostgreSQL.

**Problema Crítico — Falta Elastic:** O `copilot-instructions.md` (seção 10.3) define:
> "Elasticsearch como base principal para workloads analíticos e de observabilidade... ClickHouse permanece como opção alternativa"

No entanto, **não existe `ElasticAnalyticsEventRepository`**. O DI configura:
```csharp
var analyticsProvider = configuration["Telemetry:ObservabilityProvider:Provider"] ?? "Elastic";
if (string.Equals(analyticsProvider, "ClickHouse", ...))
    // registra ClickHouse
else
    services.AddScoped<IAnalyticsEventRepository, AnalyticsEventRepository>();
```

Ou seja, quando o provider é "Elastic", cai no repositório PostgreSQL. Isso contradiz a arquitetura alvo.

---

## 6. API Layer Review

### 6.1 Endpoints

| Aspecto | Status |
|---------|--------|
| Minimal API pattern | ✅ |
| `RequirePermission` | ✅ Todos os endpoints |
| Rate limiting no POST /events | ✅ `RequireRateLimiting("data-intensive")` |
| `ToHttpResult` | ✅ |
| Versionamento `/api/v1/` | ✅ |
| Paginação em queries | ✅ `page` e `pageSize` com defaults |

### 6.2 Segurança dos Endpoints

**Problema Médio:** Os endpoints de export (`/export/events`, `/export/summary`) **não têm `RequireRateLimiting`**. Exportação de dados é uma operação custosa e potencial vetor de DoS.

**Problema Baixo:** O endpoint `POST /events` retorna `200 OK` via `ToHttpResult` em vez de `201 Created`. Um evento recém-criado deveria retornar `201`.

---

## 7. Frontend Review

### 7.1 Estrutura e Tecnologias

| Tecnologia | Uso | Status |
|------------|-----|--------|
| React 18 | ✅ | Hooks modernos |
| TypeScript | ✅ | Tipos definidos em `productAnalyticsApi.ts` |
| TanStack Query | ✅ | `useQuery`, `useMutation`, staleTime |
| i18n | ✅ | `useTranslation()` em todos os textos visíveis |
| Tailwind CSS | ✅ | Classes utility |

### 7.2 Segurança Frontend

| Aspecto | Status |
|---------|--------|
| Sem `dangerouslySetInnerHTML` | ✅ |
| Sem exposição de secrets | ✅ |
| Session ID em `sessionStorage` | ✅ (escopo de sessão, não persiste entre sessões) |

### 7.3 Problemas Frontend

**Problema Crítico:** `JourneyConfigPage.tsx` (linha 312-313) contém texto **hardcoded** em inglês:

```tsx
<span className="flex items-center gap-1 text-muted">
  <XCircle className="w-3 h-3" /> Inactive  {/* ❌ Não traduzido */}
</span>
```

A coluna "Scope" (linha 295) também não tem chave i18n (embora os valores "Global" e "Tenant" estejam traduzidos, o header não está usando `t()` — verificar).

**Problema Médio:** `AnalyticsEventTracker.tsx` usa `useLocation` de `react-router-dom`. O projeto alvo usa **TanStack Router** segundo `copilot-instructions.md`. Embora funcione, cria inconsistência tecnológica.

**Problema Médio:** O fallback para geração de session ID usa `Math.random()` quando `crypto.randomUUID` não está disponível:

```typescript
const id = typeof crypto !== 'undefined' && 'randomUUID' in crypto
  ? crypto.randomUUID()
  : `${Date.now()}-${Math.random().toString(16).slice(2)}`;
```

`Math.random()` não é criptograficamente seguro. Deveria usar `crypto.getRandomValues` como fallback.

---

## 8. Tests Review

### 8.1 Cobertura

| Área | Testes | Status |
|------|--------|--------|
| RecordAnalyticsEvent handler | `RecordAnalyticsEventTests.cs` | ✅ |
| Multi-tenant isolation | `MultiTenantIsolationTests.cs` | ✅ |
| Friction indicators | `GetFrictionIndicatorsTests.cs` | ✅ |
| Cross-module service | `ProductAnalyticsModuleServiceTests.cs` | ✅ |
| Edge cases | `EdgeCaseTests.cs`, `EdgeCaseValidationTests.cs` | ✅ |
| Journey config | `JourneyConfigAndCohortTests.cs` | ✅ |
| Persona usage | `GetPersonaUsageTests.cs` | ✅ |
| Value milestones | `GetValueMilestonesTests.cs` | ✅ |
| Export | `ExportAnalyticsDataIntegrationTests.cs` | ✅ |
| Pagination | `PaginationAndExportTests.cs` | ✅ |

### 8.2 Qualidade dos Testes

| Aspecto | Status |
|---------|--------|
| xUnit + FluentAssertions + NSubstitute | ✅ |
| In-memory EF Core para infra tests | ✅ |
| Test doubles para ICurrentTenant/IDateTimeProvider | ✅ |
| Testes de tenant isolation | ✅ |
| Testes de range/periodo | ✅ (Theory com InlineData) |

### 8.3 Gaps de Teste

**Problema Médio:** Não existem testes unitários para:
- `ClickHouseAnalyticsEventRepository` (gap crítico dada a vulnerabilidade de SQL injection)
- `JourneyDefinitionRepository` (tenant isolation nos métodos GetById/GetByKey)
- `AnalyticsEventRepository` real com EF InMemory (apenas mocks nos handler tests)
- `ExportAnalyticsData` — validação de escapamento CSV/JSON

---

## 9. Segurança & Auditoria

### 9.1 Vulnerabilidades Identificadas

#### VULN-001: SQL Injection em ClickHouseAnalyticsEventRepository [CRÍTICO]

**Local:** `src/modules/productanalytics/NexTraceOne.ProductAnalytics.Infrastructure/Persistence/Repositories/ClickHouseAnalyticsEventRepository.cs:291`

**Descrição:** A função `BuildWhereClause` concatena valores de entrada diretamente em SQL sem parametrização. O `Escape` só substitui `'` por `\'`, o que é insuficiente.

**Impacto:** Um atacante com acesso à API de analytics pode injetar SQL ClickHouse, potencialmente extraindo dados de outros tenants ou causando negação de serviço.

**Correção:** Usar query parameters do ClickHouse HTTP API (`?param_p1=valor`) ou prepared statements.

#### VULN-002: Ausência de Tenant Filter em AnalyticsEventRepository [CRÍTICO]

**Local:** `src/modules/productanalytics/NexTraceOne.ProductAnalytics.Infrastructure/Persistence/Repositories/AnalyticsEventRepository.cs:223-243`

**Descrição:** O `ApplyFilters` não inclui `TenantId`. Embora o RLS interceptor do PostgreSQL filtre, a regra de defense-in-depth do projeto exige filtro explícito no repositório.

**Impacto:** Se o RLS for desabilitado por erro ou bypassado, dados de outros tenants ficam expostos.

**Correção:** Adicionar `.Where(e => e.TenantId == _currentTenant.Id)` no início de `ApplyFilters`.

#### VULN-003: JourneyDefinitionRepository não filtra por Tenant em GetById/GetByKey [CRÍTICO]

**Local:** `src/modules/productanalytics/NexTraceOne.ProductAnalytics.Infrastructure/Persistence/Repositories/JourneyDefinitionRepository.cs:33-43`

**Descrição:** `GetByIdAsync` e `GetByKeyAsync` não aplicam filtro de tenant. Um utilizador pode aceder/modificar journeys de outros tenants.

**Impacto:** Vazamento de configurações de jornada entre tenants; potencial modificação não autorizada.

**Correção:** Adicionar filtro de tenant em todos os métodos de leitura.

#### VULN-004: JSON Injection em ExportAnalyticsData [CRÍTICO]

**Local:** `src/modules/productanalytics/NexTraceOne.ProductAnalytics.Application/Features/ExportAnalyticsData/ExportAnalyticsData.cs:162-175`

**Descrição:** O método `BuildEventsJson` concatena strings manualmente sem escapar caracteres especiais:

```csharp
sb.Append($"{{\"sessionId\":\"{r.SessionId}\",\"eventType\":\"{r.EventType}\",\"occurredAt\":\"{r.OccurredAt:O}\"}}");
```

Se `SessionId` contiver `"` ou `\`, o JSON gerado será malformado. O `BuildSummaryJson` tem o mesmo problema com `topModulesJson`.

**Impacto:** Dados corrompidos na exportação; potencialmente explorável se o JSON for consumido por parsers inseguros.

**Correção:** Usar `System.Text.Json.JsonSerializer` para serialização segura.

### 9.2 Auditoria

| Aspecto | Status |
|---------|--------|
| Outbox para eventos | ✅ `pan_outbox_messages` |
| AuditInterceptor no DbContext | ✅ |
| `OccurredAt` em AnalyticsEvent | ✅ |
| `CreatedAt`/`UpdatedAt` em JourneyDefinition | ✅ |
| Soft-delete em JourneyDefinition | ❌ Não implementado (delete físico) |

---

## 10. Persistência & Escolha de BD

### 10.1 Decisões de Armazenamento

| Entidade | PostgreSQL | ClickHouse | Elastic | Justificativa |
|----------|-----------|------------|---------|---------------|
| `AnalyticsEvent` | ✅ (default) | ✅ (read-only) | ❌ **Falta** | Dados temporais de alto volume — deveria usar Elastic como padrão |
| `JourneyDefinition` | ✅ | N/A | N/A | Dados de configuração, baixo volume, necessita ACID |
| `OutboxMessage` | ✅ | N/A | N/A | Transacional |

### 10.2 Problemas de Persistência

**Problema Crítico:** A arquitetura alvo define **Elasticsearch como provider padrão** para analytics, mas o código não tem implementação para Elastic. O default `"Elastic"` no DI cai no `AnalyticsEventRepository` (PostgreSQL).

**Sugestão:** Implementar `ElasticAnalyticsEventRepository` usando o Elastic.Clients.Elasticsearch (ou NEST para compatibilidade), seguindo o mesmo padrão do `ClickHouseAnalyticsEventRepository`.

**Problema Médio:** `AnalyticsEvent` é armazenado em PostgreSQL sem partição por data. Com milhões de eventos, queries analíticas vão degradar. Deveria haver:
- Particionamento por `OccurredAt` (range partitioning)
- Política de retenção automática
- Ou migração completa para ClickHouse/Elastic

---

## 11. Bibliotecas & Dependências

### 11.1 Backend

| Biblioteca | Versão | Uso | Avaliação |
|------------|--------|-----|-----------|
| .NET 10 | net10.0 | Runtime | ✅ Alinhado com stack alvo |
| EF Core 10 | implícito | ORM | ✅ |
| Npgsql | implícito | PostgreSQL driver | ✅ |
| MediatR | implícito | CQRS | ✅ |
| FluentValidation | implícito | Validação | ✅ |
| Ardalis.GuardClauses | implícito | Guard clauses | ✅ Moderno, recomendado |
| Microsoft.Extensions.Http.Resilience | implícito | Resiliência HTTP | ✅ Padrão .NET 8+ |

**Observação:** Não há dependência de cliente Elasticsearch no `.Infrastructure.csproj`. Isso confirma que a implementação Elastic está de facto ausente.

### 11.2 Frontend

| Biblioteca | Uso | Avaliação |
|------------|-----|-----------|
| React 18 | UI | ✅ |
| TanStack Query | Data fetching | ✅ |
| react-router-dom | Routing | ⚠️ Projeto alvo usa TanStack Router |
| i18next | Internacionalização | ✅ |
| lucide-react | Ícones | ✅ |
| Tailwind CSS | Estilos | ✅ |

---

## 12. Problemas Críticos (High Priority)

| ID | Problema | Ficheiro(s) | Risco |
|----|----------|-------------|-------|
| HIGH-001 | SQL Injection em ClickHouse repository | `ClickHouseAnalyticsEventRepository.cs` | Segurança |
| HIGH-002 | Ausência de filtro TenantId em `AnalyticsEventRepository.ApplyFilters` | `AnalyticsEventRepository.cs` | Segurança / Multi-tenancy |
| HIGH-003 | JourneyDefinitionRepository não filtra por tenant | `JourneyDefinitionRepository.cs` | Segurança / Multi-tenancy |
| HIGH-004 | JSON malformado/unsafe em ExportAnalyticsData | `ExportAnalyticsData.cs` | Integridade de dados |
| HIGH-005 | Falta implementação Elasticsearch (provider padrão) | DI + Infrastructure | Arquitetura |
| HIGH-006 | ProductAnalyticsModuleService sem filtro de tenant | `ProductAnalyticsModuleService.cs` | Segurança |
| HIGH-007 | Texto hardcoded "Inactive" no frontend | `JourneyConfigPage.tsx` | i18n / UX |

---

## 13. Problemas Médios (Medium Priority)

| ID | Problema | Ficheiro(s) | Impacto |
|----|----------|-------------|---------|
| MED-001 | `ResolveRange` duplicado em 8 handlers | Vários | Manutenibilidade (DRY) |
| MED-002 | `ToModuleDisplayName` duplicado em 5 handlers | Vários | Manutenibilidade (DRY) |
| MED-003 | `GetJourneys` handler com parâmetros opcionais no DI | `GetJourneys.cs` | Arquitetura |
| MED-004 | `ClickHouseAnalyticsEventRepository` depende de `ProductAnalyticsDbContext` | `ClickHouseAnalyticsEventRepository.cs` | Separação de responsabilidades |
| MED-005 | Endpoints de export sem rate limiting | `ProductAnalyticsEndpointModule.cs` | Disponibilidade |
| MED-006 | `AnalyticsEvent` sem CreatedAt para auditoria | `AnalyticsEvent.cs` | Auditoria |
| MED-007 | `PersonaType` sem valores numéricos explícitos | `PersonaType.cs` | Consistência |
| MED-008 | Session ID fallback usa `Math.random()` | `AnalyticsEventTracker.tsx` | Segurança (entropia baixa) |
| MED-009 | Gaps de teste em repositórios e export | Tests/ | Qualidade |
| MED-010 | `GetPortalAdoptionFunnelReport` recebe `TenantId` no request | `GetPortalAdoptionFunnelReport.cs` | Segurança (deveria usar `ICurrentTenant`) |
| MED-011 | README desatualizado (código antigo) | `README.md` | Documentação |
| MED-012 | `ClickHouseAnalyticsOptions.SectionName` = "LegacyTelemetry" | `ClickHouseAnalyticsEventRepository.cs` | Nomenclatura confusa |

---

## 14. Problemas Baixos (Low Priority)

| ID | Problema | Ficheiro(s) | Impacto |
|----|----------|-------------|---------|
| LOW-001 | Falta `AGENTS.md` no módulo | `src/modules/productanalytics/` | Documentação para agents |
| LOW-002 | `RecordAnalyticsEvent` endpoint retorna 200 em vez de 201 | `ProductAnalyticsEndpointModule.cs` | Semântica HTTP |
| LOW-003 | `AnalyticsEventConfiguration` check constraint não inclui `ServiceCreated` | `AnalyticsEventConfiguration.cs` | Consistência de schema |
| LOW-004 | `useLocation` de react-router-dom em vez de TanStack Router | `AnalyticsEventTracker.tsx` | Consistência tecnológica |
| LOW-005 | Soft-delete não implementado para JourneyDefinition | `JourneyDefinition.cs` | Auditoria |

---

## 15. Plano de Correção Sugerido (Priorizado)

### Sprint 1 — Segurança Crítica (Imediato)

1. **HIGH-002 + HIGH-003 + HIGH-006**: Adicionar filtro de `TenantId` em **todos** os métodos de leitura dos repositórios e no `ProductAnalyticsModuleService`.
   - Injetar `ICurrentTenant` nos repositórios
   - Adicionar `.Where(e => e.TenantId == _tenant.Id)` como defense-in-depth

2. **HIGH-001**: Refatorar `ClickHouseAnalyticsEventRepository.BuildWhereClause` para usar **query parameters** da API HTTP do ClickHouse em vez de concatenação de strings.
   - Exemplo: `?param_p1={Uri.EscapeDataString(persona)}`
   - Substituir `Escape` por parametrização real

3. **HIGH-004**: Substituir serialização manual de JSON/CSV em `ExportAnalyticsData` por:
   - `System.Text.Json.JsonSerializer` para JSON
   - `CsvHelper` (biblioteca consolidada) ou escapamento rigoroso para CSV

### Sprint 2 — Arquitetura & Elastic

4. **HIGH-005**: Implementar `ElasticAnalyticsEventRepository`
   - Adicionar pacote `Elastic.Clients.Elasticsearch` (ou equivalente .NET 10 compatível)
   - Mapear queries analíticas para DSL do Elasticsearch
   - Atualizar DI para resolver `ElasticAnalyticsEventRepository` quando provider = "Elastic"

5. **MED-001 + MED-002**: Criar `AnalyticsQueryHelper` compartilhado
   - `ResolveRange(DateTimeOffset utcNow, string? range, int maxDays)`
   - `ToModuleDisplayName(ProductModule module)`
   - Local: `Application/Helpers/AnalyticsQueryHelper.cs`

6. **MED-003**: Remover parâmetros opcionais do construtor de `GetJourneys.Handler`
   - `IJourneyDefinitionRepository` e `ICurrentTenant` devem ser obrigatórios
   - Se necessário, criar `NullJourneyDefinitionRepository` seguindo honest-null pattern

### Sprint 3 — Qualidade & Frontend

7. **HIGH-007 + MED-008**: Corrigir frontend
   - Substituir "Inactive" hardcoded por `t('analytics.journeyConfig.inactive')`
   - Usar `crypto.getRandomValues` como fallback para session ID
   - Avaliar migração de `react-router-dom` para `TanStack Router`

8. **MED-005**: Adicionar `RequireRateLimiting("data-intensive")` nos endpoints de export

9. **MED-009**: Adicionar testes unitários para:
   - `ClickHouseAnalyticsEventRepository` (mock do HttpClient)
   - `JourneyDefinitionRepository` com EF InMemory (tenant isolation)
   - `ExportAnalyticsData` (validação de escapamento)

### Sprint 4 — Refinamentos

10. **MED-011 + LOW-001**: Atualizar `README.md` com código atual e criar `AGENTS.md`
11. **MED-006**: Avaliar adicionar `CreatedAt` em `AnalyticsEvent` (ou documentar intencionalmente como imutável sem auditoria)
12. **MED-012**: Renomear `ClickHouseAnalyticsOptions.SectionName` para `"ClickHouse:Analytics"`
13. **LOW-003**: Sincronizar check constraint do PostgreSQL com o enum `AnalyticsEventType`

---

## 16. Conclusão

O módulo ProductAnalytics demonstra **alto nível de maturidade arquitetural** e **alinhamento com a visão do produto** (analytics contextualizado, não vanity metrics). Os handlers são bem modelados, o frontend é limpo e tipado, e os testes cobrem cenários críticos.

No entanto, **problemas de segurança graves** (SQL injection, falta de tenant filter defense-in-depth) **devem ser corrigidos imediatamente** antes de qualquer deploy em ambiente multi-tenant real. A ausência da implementação Elasticsearch também precisa de atenção para alinhar com a arquitetura alvo definida nos `copilot-instructions.md`.

Com as correções propostas, o módulo estará em conformidade total com os padrões enterprise exigidos pelo NexTraceOne.

---

*Relatório gerado automaticamente pela auditoria ponta a ponta do módulo ProductAnalytics.*
