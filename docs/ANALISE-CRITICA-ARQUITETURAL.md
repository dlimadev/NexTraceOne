# ANÁLISE CRÍTICA ARQUITETURAL — NexTraceOne
## Principal Solution Architect Review

> **Nota:** Este documento é uma análise arquitetural original. Referências a Grafana/Jaeger/Tempo refletem a stack planeada inicialmente. A stack de observabilidade foi migrada para provider configurável (ClickHouse ou Elastic). Ver `docs/observability/` para a documentação atual.

> **Data:** Março 2026
> **Autor:** Análise crítica profunda — Arquiteto Principal de Solução
> **Scope:** Avaliação integral do projeto: arquitetura, código, maturidade, gaps, dívidas técnicas e plano de finalização
> **Método:** Inspeção direta de todo o codebase, documentação oficial, estado real de persistência, testes, segurança e alinhamento com a visão do produto
> **Princípio:** Apenas evidência de código conta. Documentação sem código não é prova de implementação.

---

## SUMÁRIO EXECUTIVO

O NexTraceOne é um projeto tecnicamente **ambicioso e bem fundamentado** na sua arquitetura base. A escolha de Modular Monolith com DDD, CQRS, Clean Architecture e Result Pattern é **correta e adequada** para o domínio de uma plataforma enterprise de governança operacional.

Após inspeção completa do codebase com **1.918 ficheiros** e análise de todos os documentos fundadores, a avaliação global é:

| Dimensão | Nota | Justificativa |
|----------|------|---------------|
| Arquitetura base | **9/10** | Sólida, consistente, bem estruturada |
| Foundation (Building Blocks) | **9/10** | Abstrações corretas, bem testadas |
| Módulos core (Identity, Catalog, Changes, Audit) | **9/10** | Persistência real, testes, handlers reais |
| Módulos secundários (OI, AIK, Governance) | **5/10** | Gaps significativos de persistência e implementação |
| Frontend | **7/10** | Arquitetura boa, mas muitas páginas sem estados reais |
| Segurança | **7/10** | Boa base, gaps identificados |
| Testes | **6/10** | Bons testes unitários, zero integration/E2E reais |
| Documentação | **8/10** | Extensa e bem estruturada, algum desalinhamento com código |
| Prontidão para produção | **5/10** | Módulos core prontos; módulos secundários bloqueados |

**Veredicto:** O produto tem uma **base sólida para os fluxos centrais de valor** (Source of Truth, Change Confidence, IdentityAccess, AuditCompliance). No entanto, **30% do codebase são dados mock ou stubs sem valor funcional real**. A plataforma não está pronta para produção como um todo — apenas os módulos core estão.

---

## PARTE 1 — O QUE ESTÁ BOM (Pontos Fortes)

### 1.1 Arquitetura e Estrutura

**Avaliação: EXCELENTE**

A escolha de **Modular Monolith** é a decisão arquitetural mais sensata para este estágio do produto. Evita a complexidade prematura de microserviços mantendo bounded contexts claros. A separação em:

```
Domain → Application → Infrastructure → API
```

é rigorosamente respeitada em **todos os 8 bounded contexts**. Verificado:
- Domain layers **não têm imports de infrastructure** (regra crítica de DDD respeitada)
- API layers são **finas** — handlers de 1-5 linhas delegando para MediatR
- CQRS com `ICommand<TResult>` e `IQuery<TResult>` é consistente

O `Program.cs` do `ApiHost` é **exemplar**: registration limpa por módulo, sem vazamentos, com todos os building blocks transversais devidamente separados. A adição de:
- Rate Limiting (FixedWindow por IP)
- Security Headers
- Assembly Integrity Check no boot
- Health/Ready/Live endpoints
- Response Compression

demonstra maturidade de engenharia acima da média para um produto neste estágio.

**Central Package Management** via `Directory.Packages.props` é uma decisão correta — evita drift de versões entre projetos.

### 1.2 Building Blocks

**Avaliação: MUITO BOM**

Os building blocks estão bem desenhados e abstrados:

| Building Block | Avaliação |
|----------------|-----------|
| `Core` (Entity, AggregateRoot, ValueObject, Result, StronglyTypedIds) | **Sólido** — Primitives corretos |
| `Application` (CQRS, Behaviors, Pagination, i18n) | **Sólido** — Pipeline MediatR completo |
| `Infrastructure` (OutboxEventBus, RepositoryBase, Interceptors) | **Bom** — Padrões corretos |
| `Security` (JWT, RBAC, ApiKey, AesGCM, TenantRLS) | **Bom** — Controles adequados |
| `Observability` (Serilog, OpenTelemetry, HealthChecks, Meters) | **Bom** — Stack observabilidade correta |

O `TenantRlsInterceptor` (Row-Level Security via EF Interceptors) é uma implementação inteligente para multi-tenancy sem vazamento cross-tenant.

O `OutboxEventBus` implementa o padrão Outbox correto para garantir delivery eventual de events — decisão arquitetural madura.

O `AesGcmEncryptor` + `EncryptedStringConverter` para dados sensíveis é correto, mas **ainda não aplicado a todos os campos que deveriam ser criptografados**.

### 1.3 Módulos Core de Alta Maturidade

#### IdentityAccess — **PRODUÇÃO** ✅
- JWT com refresh token, RBAC com permissões granulares
- Multi-tenancy com RLS
- JIT access, break glass, delegações, revisões de acesso
- 186 testes, 100% passam
- 12 repositórios EF Core implementados, 1 migration

#### Catalog (Source of Truth) — **PRODUÇÃO** ✅
- 3 DbContexts reais com 13 repositórios EF Core
- 4 migrações aplicadas
- Graph (27 features), Contracts (35 features) — 100% real persistence
- Developer Portal (22 features) — 68% real
- Contract lifecycle state machine completo (Draft→InReview→Approved→Locked→Deprecated→Retired)
- Diff semântico real, scoring de contratos, multi-protocol (REST/SOAP/Kafka/Background)
- 430 testes

#### ChangeGovernance — **PRODUÇÃO** ✅
- 4 DbContexts reais, 20+ repositórios EF Core, 4 migrações
- Advisory engine com 4 fatores ponderados (Evidence, BlastRadius, ChangeScore, RollbackReadiness)
- Workflow com stages, approval decisions, evidence packs, SLA policies — tudo real
- Blast radius, freeze windows, rollback assessments
- 179 testes

#### AuditCompliance — **PRODUÇÃO** ✅
- Hash chain SHA-256 para imutabilidade do audit trail
- RecordAuditEvent, VerifyChainIntegrity, SearchAuditLog — tudo real
- 1 DbContext, 2 repositórios, 1 migration

### 1.4 Frontend — Arquitetura

**Avaliação: BOA**

- Feature-based architecture alinhada com bounded contexts ✅
- Lazy loading com React.lazy/Suspense ✅
- Design system com tokens ✅
- API client centralizado (Axios + JWT + tenant headers) ✅
- React Query para state management ✅
- i18n maduro (4 locales, 1.650+ chaves, 41 namespaces) ✅
- Persona-aware UX com PersonaContext ✅
- Dark enterprise theme consistente ✅
- Zod + react-hook-form para validação ✅

### 1.5 Segurança

**Avaliação: BOA COM GAPS**

- `RequirePermission` enforced em todos os 22 endpoint modules ✅
- CORS restritivo ✅
- Rate limiting por IP ✅
- Assembly integrity check no boot ✅
- Refresh token apenas em memória (não persiste no browser) ✅
- CSP no frontend ✅
- ErrorBoundary sem expor stack traces ✅
- Source maps desativados em produção ✅
- Auditoria imutável com SHA-256 ✅

### 1.6 Observabilidade

- OpenTelemetry com 5 activity sources definidos ✅
- Serilog estruturado com enriquecimento ✅
- Health/Ready/Live endpoints ✅
- Docker Compose para stack OTEL (Loki, Tempo, Collector) ✅
- Background workers com Quartz (OutboxProcessor 5s, IdentityExpiration 60s) ✅

---

## PARTE 2 — O QUE ESTÁ EM FALTA (Gaps Críticos)

### 2.1 Governance Module — Maior Dívida Arquitetural

**Severidade: CRÍTICA**

O módulo `Governance` tem **74 features** definidas, todas retornando **dados hardcoded**. O `GovernanceInfrastructure` tem repositórios implementados, mas os **handlers da Application layer ignoram o repositório** e retornam arrays estáticos.

Porém — **há boa notícia não documentada**: o `Ingestion.Api` já usa `GovernanceInfrastructure` com persistência real para conectores, fontes de ingestão e execuções. Isto significa que a infra existe, mas **não foi ligada aos handlers do módulo principal**.

Áreas 100% mock no Governance:
- Governance Packs (5 packs hardcoded)
- Teams e Domains (retornam arrays fixos)
- FinOps (dados fictícios de custo)
- Product Analytics (métricas fictícias)
- Policy Catalog
- Evidence Packages
- Enterprise Controls
- Risk Center
- Compliance Reports

**Impacto:** Todo o módulo de Governance no frontend exibe dados que parecem reais mas são completamente fictícios. Sem disclosure claro ao utilizador.

### 2.2 AIKnowledge — Implementação Incompleta

**Severidade: ALTA**

| Sub-módulo | Estado Real |
|------------|-------------|
| AI Governance (28 features) | ✅ Funcional com repositórios |
| AI Orchestration | ⚠️ EndpointModule com TODO — handlers existem mas sem endpoints mapeados |
| ExternalAI (8 features) | ❌ TODO stubs — zero implementação |
| Migrações EF (3 DbContexts) | ❌ Não geradas — schema não existe no banco |

**O AI Assistant** (`AiAssistantPage`) usa `mockConversations` hardcoded — não chama o backend.

**Sem integração real com LLM** — `SendAssistantMessage` retorna respostas estruturadas mas não usa nenhum provider de IA (OpenAI, Anthropic, LLM local).

### 2.3 OperationalIntelligence — Parcialmente Funcional

**Severidade: ALTA**

| Sub-módulo | Estado Real |
|------------|-------------|
| Incidents (17 features) | ✅ EfIncidentStore real, migrations, seed data |
| Runtime Intelligence | ✅ DbContext real, repositórios EF — **sem migrations geradas** |
| Cost Intelligence | ✅ DbContext real, repositórios EF — **sem migrations geradas** |
| Automation (10 features) | ❌ 100% mock — catálogo estático, workflows não persistidos |
| Reliability (7 features) | ❌ 100% mock — 8 serviços hardcoded |
| Correlação incident↔change | ⚠️ Seed data estático — não dinâmico via eventos |

**Problemas específicos:**
- Sem `CreateIncident` endpoint — incidents só existem via seed SQL
- Sem formulário de criação de incident no frontend
- Correlação não se atualiza quando changes são criadas/atualizadas
- Runtime e Cost Intelligence: código existe, schema não está deployado

### 2.4 Testes — Cobertura Insuficiente

**Severidade: MÉDIA-ALTA**

| Tipo de Teste | Estado |
|---------------|--------|
| Unit tests backend | ✅ 1.243, 100% passam |
| Unit tests frontend | ✅ 266, 100% passam |
| Integration tests com DB real (Testcontainers) | ❌ Zero — não existe nenhum |
| E2E tests (Playwright) | ❌ Apenas placeholders vazios |
| Contract tests | ❌ Não existem |
| Performance tests | ❌ Não existem |

**Problema crítico:** Os 1.243 testes backend usam mocks para repositórios. **Nenhum teste verifica que as queries EF Core funcionam contra PostgreSQL real.** Bugs de SQL, índices em falta, problemas de serialização JSONB — nenhum destes seria detetado pelos testes atuais.

### 2.5 Frontend — Estados Incompletos

**Severidade: MÉDIA**

| Problema | Scope |
|----------|-------|
| Páginas sem EmptyState pattern | 68/82 (83%) |
| Páginas sem error state handling | 79/82 (96%) |
| IncidentsPage com mock inline | 1 página crítica |
| Componentes muito grandes | ServiceCatalogPage (1.115 linhas), ContractsPage (1.053 linhas) |
| Developer Portal — frontend client para endpoints inexistentes | 7 stubs |
| 48 ficheiros .tsx com mock data | Sem indicação ao utilizador |

### 2.6 Dívidas de Código

**Severidade: BAIXA-MÉDIA**

| Dívida | Quantidade | Impacto |
|--------|-----------|---------|
| Warnings de compilação (CS8632 nullable) | 516 | Ruído CI, potencial bugs |
| Ficheiros com TODO/FIXME | ~41 | Implementações incompletas |
| AuditCompliance usa namespace `NexTraceOne.Audit.*` em vez de `NexTraceOne.AuditCompliance.*` | Cosmético | Confusão semântica |
| Google Fonts CDN em produto on-premise | 1 | Falha em ambiente isolado |
| Tokens em sessionStorage em vez de httpOnly cookies | Crítico por design | XSS pode exfiltrar token |
| Sem encryption at rest para dados sensíveis | Infra | Dados em texto claro no DB |

---

## PARTE 3 — O QUE PRECISA REFATORAR

### 3.1 Governance Module — Prioridade 1

**O que fazer:**
O `GovernanceInfrastructure` já tem `IGovernancePackRepository`, `IIntegrationConnectorRepository`, etc. implementados. O trabalho é:

1. Criar `GovernanceDbContext` com entidades de Governance Packs, Teams, Domains, Policies
2. Gerar migration EF Core
3. Substituir os arrays hardcoded nos handlers por `repository.ListAsync()`
4. Para FinOps/Analytics — criar queries cross-module (read-only) a partir de outros módulos

**Esforço estimado:** Alto (mas não reescrita — é ligação de infraestrutura existente a handlers)

### 3.2 AI Integration — Prioridade 1

**O que fazer:**
1. Implementar `IExternalAIRoutingPort` com pelo menos um provider (OpenAI ou mock realista com delay)
2. Ligar `SendAssistantMessage` ao provider via dependency injection
3. Ligar `AiAssistantPage` ao endpoint real de conversações
4. Gerar migrations EF para 3 DbContexts do AIKnowledge
5. Implementar `ExternalAI` handlers prioritários

### 3.3 Correlation Engine — Prioridade 1

**O que fazer:**
1. Substituir seed data estático por engine de correlação baseada em:
   - Intervalo temporal: change criada N horas antes do incident
   - Serviços afetados: intersection entre blast radius e services do incident
   - Score de correlação calculado em tempo real
2. Adicionar `CreateIncident` handler e endpoint
3. Adicionar formulário de criação de incident no frontend

### 3.4 Testes de Integração — Prioridade 2

**O que fazer:**
1. Criar projeto `NexTraceOne.IntegrationTests` com Testcontainers.PostgreSql
2. Testar os fluxos críticos contra DB real:
   - Catalog: RegisterService → GetService → ListServices
   - Contracts: CreateDraft → PublishDraft → ComputeSemanticDiff
   - ChangeGovernance: CreateRelease → GetAdvisory → ApproveDecision
   - IdentityAccess: Login → RefreshToken → Logout
3. Testar migrações EF Core contra PostgreSQL

### 3.5 Frontend — EmptyState e Error States — Prioridade 2

**O que fazer:**
1. Criar componente `PageStateDisplay` unificado (loading, empty, error)
2. Aplicar sistematicamente nas 68 páginas sem EmptyState
3. Padronizar o padrão de error handling com React Query
4. Dividir componentes grandes (>400 linhas) em sub-componentes

### 3.6 EF Migrations em Falta — Prioridade 1

**O que fazer:**
```bash
# RuntimeIntelligence
dotnet ef migrations add InitialRuntimeSchema --project NexTraceOne.OperationalIntelligence.Infrastructure

# CostIntelligence
dotnet ef migrations add InitialCostSchema --project NexTraceOne.OperationalIntelligence.Infrastructure

# AIKnowledge (3 DbContexts)
dotnet ef migrations add InitialAiGovernanceSchema
dotnet ef migrations add InitialExternalAiSchema
dotnet ef migrations add InitialAiOrchestrationSchema
```

### 3.7 Nullable Warnings — Prioridade 3

**O que fazer:**
1. Ativar `<Nullable>enable</Nullable>` em `Directory.Build.props` de forma gradual por projeto
2. Corrigir os 516 CS8632 warnings começando pelos módulos core
3. Adicionar análise de nullable ao CI para bloquear regressões

### 3.8 Segurança — httpOnly Cookies — Prioridade 2

**O que fazer:**
1. Migrar access token de `sessionStorage` para cookie `httpOnly; Secure; SameSite=Strict`
2. Backend: endpoint para emitir cookie
3. Frontend: remover leitura/escrita de sessionStorage para tokens
4. Adicionar CSRF token (obrigatório com cookies)

### 3.9 Google Fonts — On-Premise Fix — Prioridade 2

**O que fazer:**
1. Hospedar as fontes localmente no frontend (`public/fonts/`)
2. Remover dependência de CDN externo
3. Garantir funcionamento em ambiente on-premise isolado

---

## PARTE 4 — O QUE CRIAR DO ZERO

### 4.1 Correlation Engine (Incident ↔ Change) — ALTA PRIORIDADE

**Não existe nenhuma implementação dinâmica.** Precisa de:
- Domain service `IncidentCorrelationService` dentro de OperationalIntelligence
- Subscription a `DomainEvent` de `ReleaseCreated` e `ChangePublished` do ChangeGovernance
- Algoritmo de correlação por overlap temporal + serviços afetados
- Endpoint `PATCH /api/v1/incidents/{id}/correlation` para refresh manual

### 4.2 Real LLM Provider — ALTA PRIORIDADE

**Não existe nenhuma integração com LLM real.** Precisa de:
- `IExternalAIRoutingPort` implementação (OpenAI client ou Azure OpenAI)
- Configuração via `appsettings.json` (endpoint, api-key, model)
- Grounding: context builder que consulta Catalog, Changes, Incidents antes de enviar ao LLM
- Retry/fallback policies com Polly

### 4.3 Integration Tests Suite — ALTA PRIORIDADE

**Nenhum teste verifica o banco real.** Precisa de:
- Projeto `NexTraceOne.Tests.Integration` com `Testcontainers.PostgreSQL`
- Test fixtures que aplicam migrations antes dos testes
- Respawn para reset de dados entre testes
- Testes para todos os fluxos críticos (B, C, D do WAVE-1)

### 4.4 E2E Tests Suite — MÉDIA PRIORIDADE

**Apenas placeholders existem.** Precisa de:
- Playwright tests reais para:
  1. Login → Navigate → Logout
  2. Service Registration → Contract Creation → Diff
  3. Change Submission → Advisory → Approval
  4. Incident View → Mitigation → Validation

### 4.5 CreateIncident Endpoint — MÉDIA PRIORIDADE

**Incidents só existem via seed SQL.** Precisa de:
- `CreateIncident` command + handler
- `POST /api/v1/incidents` endpoint
- Formulário no frontend `CreateIncidentForm`
- Integração com correlação engine

### 4.6 Contract Studio Advanced Editor — ROADMAP

**Backend está pronto, UX precisa de polish:**
- Wizard flow melhorado para criação de contratos
- Preview de diff ao editar
- AI-assisted contract generation (depende de LLM)
- Validation inline durante edição

### 4.7 Developer Portal Backend — MÉDIA PRIORIDADE

**Frontend tem 27 métodos API apontando para endpoints inexistentes:**
- `SearchCatalog`, `GetApiHealth`, `GetMyApis`, `GetApisIConsume`, `GetApiDetail`
- `GetAssetTimeline`, `RenderOpenApiContract`

---

## PARTE 5 — ANÁLISE DE ALINHAMENTO COM DOCUMENTAÇÃO

### 5.1 Documentos Fundadores

| Documento | Alinhamento Real |
|-----------|-----------------|
| `PRODUCT-VISION.md` | **90%** — Pilares definidos, implementação segue a visão |
| `ARCHITECTURE-OVERVIEW.md` | **95%** — Muito curto mas correto |
| `GUIDELINE.md` | **85%** — Dark theme e design system implementados, mas EmptyState/error states faltam |
| `DOMAIN-BOUNDARIES.md` | **90%** — Boundaries respeitados, comunicação via contratos |
| `BACKEND-MODULE-GUIDELINES.md` | **95%** — DDD/CQRS seguidos rigorosamente |
| `FRONTEND-ARCHITECTURE.md` | **90%** — Feature-based, i18n, persona-aware |
| `SECURITY-ARCHITECTURE.md` | **75%** — Zero Trust parcial; falta httpOnly cookies, encryption at rest |
| `I18N-STRATEGY.md` | **95%** — 4 locales, maturidade alta |
| `OBSERVABILITY-STRATEGY.md` | **60%** — Stack configurado mas exporter OTLP inativo por padrão |
| `REBASELINE.md` | **75%** — Alguns estados desatualizados (incidents melhoraram) |
| `SOLUTION-GAP-ANALYSIS.md` | **85%** — Boa análise, alguns itens precisam de update |

### 5.2 Problemas de Documentação

1. `REBASELINE.md` ainda diz "Incidents: 0% funcional" — agora está ~80% real
2. `SOLUTION-GAP-ANALYSIS.md` marca Ingestion.Api como "stubs" — agora tem persistência real
3. `OBSERVABILITY-STRATEGY.md` é muito vago (4 linhas) para uma plataforma de governança
4. `DEPLOYMENT-ARCHITECTURE.md` é insuficiente — não documenta Docker Compose, variáveis de ambiente, portas, health check paths
5. Vários docs fundadores são demasiado concisos (3-5 linhas) e não servem como guia prático

---

## PARTE 6 — AVALIAÇÃO DE RISCO

### 6.1 Riscos Arquiteturais

| # | Risco | Probabilidade | Impacto | Mitigação |
|---|-------|--------------|---------|-----------|
| R1 | Governance module sem persistência expõe dados falsos como reais | ALTA | ALTO | Implementar GovernanceDbContext + handlers reais |
| R2 | Zero integration tests — bugs EF/SQL só em produção | ALTA | ALTO | Testcontainers obrigatórios |
| R3 | AI sem LLM real — feature central do produto não funciona | ALTA | ALTO | Integrar pelo menos 1 provider |
| R4 | Tokens em sessionStorage vulneráveis a XSS | MÉDIA | ALTO | Migrar para httpOnly cookies |
| R5 | Incident correlation estática — informação errada em incidentes | ALTA | MÉDIO | Correlation engine dinâmica |
| R6 | Sem E2E tests — regressões invisíveis | ALTA | MÉDIO | Playwright tests core flows |
| R7 | 516 nullable warnings — potencial NullReferenceExceptions | BAIXA | MÉDIO | Ativar nullable gradualmente |
| R8 | Google Fonts CDN em on-premise | MÉDIA | BAIXO | Self-host das fontes |
| R9 | EF migrations em falta (Runtime, Cost, AI) | ALTA | MÉDIO | Gerar migrations imediatamente |
| R10 | Componentes grandes (>1000 linhas) difíceis de manter | BAIXA | BAIXO | Refactor incremental |

### 6.2 Riscos de Produto

| # | Risco | Impacto | Mitigação |
|---|-------|---------|-----------|
| P1 | Utilizador percebe dados de Governance como reais | Confiança | Indicar claramente dados demo |
| P2 | AI Assistant inútil sem LLM — decepciona utilizador | Adoção | Priorizar integração LLM |
| P3 | Incidents sem correlação dinâmica — valor zero | Core flow | Correlation engine |
| P4 | 83% páginas sem EmptyState — experiência fraca | UX | EmptyState sistemático |

---

## PARTE 7 — PLANO DE AÇÃO E FINALIZAÇÃO

### Princípios do Plano

1. **Não reescrever — conectar.** A maioria dos gaps é de ligação (handlers mock → repositórios reais), não de reescrita.
2. **Fechar fluxos centrais primeiro.** Nenhuma nova feature antes de fechar os 4 fluxos.
3. **Testes acompanham implementação.** Cada feature nova tem integration test.
4. **Documentação reflete código.** Após cada fase, atualizar docs afetados.

---

### FASE 1 — Correções Imediatas (1-2 semanas)
**Objetivo:** Resolver gaps críticos que bloqueiam funcionamento básico

#### F1.1 — Gerar Migrations EF em Falta
```
Módulos: RuntimeIntelligence, CostIntelligence, AiGovernance, ExternalAI, AiOrchestration
Ação: dotnet ef migrations add Initial{Module}Schema para cada DbContext
Critério: dotnet ef database update aplicado com sucesso
```

#### F1.2 — Corrigir Ingestion API → ApiHost Integration
```
Ação: Adicionar endpoint proxy ou event para criar ChangeEvent no ChangeGovernance quando
      Ingestion receber deployment event
Critério: Deployment event na Ingestion API cria ChangeEvent no módulo ChangeGovernance
```

#### F1.3 — Ligar AiAssistantPage ao Backend Real
```
Ficheiro: src/frontend/src/features/ai-hub/pages/AiAssistantPage.tsx
Ação: Substituir mockConversations por useQuery(aiGovernanceApi.listConversations)
Critério: Page carrega conversações reais do backend (mesmo que vazio)
```

#### F1.4 — Corrigir IncidentsPage Mock
```
Ficheiro: src/frontend/src/features/operations/pages/IncidentsPage.tsx
Ação: Substituir mockIncidents inline por incidentsApi.getIncidents()
Critério: Page usa API real, trata loading/error/empty states
```

#### F1.5 — Adicionar Indicação de Demo Data
```
Para todas as páginas de Governance que retornam mock data do backend:
Adicionar banner "Demo data — não reflete dados reais"
Critério: Utilizador não confunde dados fictícios com dados reais
```

---

### FASE 2 — Fechar Fluxos Centrais (3-5 semanas)
**Objetivo:** 4 fluxos centrais 100% funcionais com dados reais

#### F2.1 — Governance Module Real Persistence
**Prioridade: P0**

```
1. Criar GovernanceDbContext com entidades:
   - GovernancePack (Id, Name, Category, Status, Rules[])
   - GovernanceScope (Id, PackId, ScopeType, ScopeId)
   - PolicyRule (Id, PackId, RuleCode, Severity, Condition)
   - GovernanceEvaluation (Id, ScopeId, Result, Evidence)

2. Gerar migration: dotnet ef migrations add InitialGovernanceSchema

3. Substituir handlers mock por real:
   - ListGovernancePacks → packRepository.ListAsync()
   - GetGovernancePack → packRepository.GetByIdAsync()
   - ListTeams → teamRepository.ListAsync()
   - GetDomainDetail → domainRepository.GetByIdAsync()

4. Para FinOps/Analytics: criar read-only cross-module queries:
   - FinOps: JOIN Cost Intelligence cost snapshots
   - Analytics: instrumentation events do ApiHost

Critério de sucesso:
- Governance Packs persistem e são consultáveis
- Teams e Domains refletem dados reais
- Sem dados hardcoded nos handlers
```

#### F2.2 — AI Integration com LLM Real
**Prioridade: P0**

```
1. Implementar IExternalAIRoutingPort:
   interface IExternalAIRoutingPort {
       Task<string> SendAsync(string systemPrompt, string userMessage, CancellationToken ct);
   }

2. Criar OpenAIRoutingPort (ou AzureOpenAIRoutingPort):
   - Configurável via appsettings.json
   - Retry policy com Polly
   - Fallback graceful quando sem configuração

3. Criar context builder para grounding:
   - IContextEnricher que consulta serviços relevantes
   - IContextEnricher que consulta contratos relevantes
   - IContextEnricher que consulta incidents recentes

4. Ligar SendAssistantMessage ao routing port

5. Implementar ExternalAI handlers prioritários:
   - AskAboutContract
   - AskAboutService
   - SuggestMitigationSteps

Critério de sucesso:
- AI Assistant responde com contexto de serviços/contratos reais
- Respostas são auditadas no AuditCompliance
- Fallback quando sem provider configurado
```

#### F2.3 — Correlation Engine Dinâmica
**Prioridade: P0**

```
1. Domain service: IncidentCorrelationService
   - Algoritmo: overlap temporal ± 2 horas do incident
   - Algoritmo: serviços do incident ∩ blast radius do change
   - Score calculado: temporal_weight * service_overlap_weight

2. Event handler:
   - Subscribir a ChangePublished do ChangeGovernance
   - Re-calcular correlações de incidents ativos quando nova change é publicada

3. Endpoint: PATCH /api/v1/incidents/{id}/correlation/refresh
   - Trigger manual de re-correlação

4. Criar CreateIncident handler e endpoint:
   POST /api/v1/incidents
   { serviceName, severity, title, description, detectedAt }

Critério de sucesso:
- Incident criado após deployment failure correlaciona automaticamente com a change
- Correlação atualiza quando blast radius muda
```

#### F2.4 — Developer Portal Backend Completo
**Prioridade: P1**

```
Endpoints a implementar:
- SearchCatalog: full-text search cross-entity (serviços + contratos + changes)
- GetApiHealth: delegar ao RuntimeIntelligence
- GetMyApis: filtrar por TechnicalOwner do utilizador autenticado
- GetApisIConsume: lookup de subscriptions no DeveloperPortalDbContext
- GetApiDetail: enriquecer ServiceAsset com Contract + health + metrics

Critério de sucesso:
- Frontend Developer Portal funciona com dados reais
- SearchCatalog retorna resultados cruzados de múltiplas entidades
```

---

### FASE 3 — Hardening e Qualidade (2-4 semanas)
**Objetivo:** Tornar o produto deployável em produção com confiança

#### F3.1 — Integration Tests Suite
**Prioridade: P1**

```
Criar: src/tests/NexTraceOne.Tests.Integration/

Estrutura:
- Fixtures/PostgreSqlFixture.cs (Testcontainers)
- Fixtures/ApiTestBase.cs (WebApplicationFactory)
- Modules/Catalog/
  - ServiceRegistration.IntegrationTest.cs
  - ContractPublish.IntegrationTest.cs
  - SemanticDiff.IntegrationTest.cs
- Modules/ChangeGovernance/
  - ChangeAdvisory.IntegrationTest.cs
  - ApprovalDecision.IntegrationTest.cs
- Modules/IdentityAccess/
  - Authentication.IntegrationTest.cs
  - RBAC.IntegrationTest.cs
- Modules/Incidents/
  - CreateIncident.IntegrationTest.cs
  - CorrelateWithChange.IntegrationTest.cs

Meta: 50+ integration tests cobrindo os fluxos B, C, D

Critério de sucesso:
- Todos os testes passam contra PostgreSQL real (Testcontainers)
- CI executa integration tests em cada PR
```

#### F3.2 — E2E Tests com Playwright
**Prioridade: P1**

```
Criar testes reais em: src/frontend/e2e/

Fluxos obrigatórios:
1. auth.spec.ts: login → navigate → logout
2. service-catalog.spec.ts: register service → view detail → search
3. contract-governance.spec.ts: create draft → publish → view diff
4. change-confidence.spec.ts: submit change → view advisory → approve
5. incidents.spec.ts: create incident → view correlation → record mitigation

Critério de sucesso:
- 5 E2E tests core flows passam
- CI executa E2E em cada PR com ambiente de test
```

#### F3.3 — EmptyState e Error States Sistemáticos
**Prioridade: P1**

```
1. Criar componente unificado:
   <PageStateDisplay
     isLoading={isLoading}
     isError={isError}
     isEmpty={data?.length === 0}
     errorMessage={error?.message}
     emptyTitle="Nenhum item encontrado"
     emptyDescription="..."
   />

2. Aplicar nas 68 páginas sem EmptyState
3. Garantir loading skeleton em todas as páginas

Critério de sucesso:
- 0 páginas sem loading state
- 0 páginas sem error state
- 0 páginas sem empty state
```

#### F3.4 — Resolver Nullable Warnings
**Prioridade: P2**

```
1. Ativar nullable por projeto, começando pelos Building Blocks
2. Corrigir 516 CS8632 warnings incrementalmente
3. Adicionar ao CI: dotnet build --warnaserror CS8632

Critério de sucesso:
- 0 CS8632 warnings nos Building Blocks e módulos core
```

#### F3.5 — Migrar Tokens para httpOnly Cookies
**Prioridade: P1**

```
Backend:
1. Endpoint POST /auth/session → set-cookie: access_token httpOnly; Secure; SameSite=Strict
2. Endpoint DELETE /auth/session → clear cookie
3. CSRF token endpoint

Frontend:
1. Remover sessionStorage.setItem para tokens
2. Todas as requests automáticas com cookie (credentials: 'include')
3. CSRF header em mutations

Critério de sucesso:
- Access token não é acessível via JavaScript
- XSS não consegue exfiltrar token
```

#### F3.6 — Self-hosted Fonts
**Prioridade: P2**

```
1. Baixar Inter e JetBrains Mono para src/frontend/public/fonts/
2. Remover <link> para Google Fonts do index.html
3. @font-face declarations locais no CSS

Critério de sucesso:
- Produto funciona offline sem internet
```

#### F3.7 — Dividir Componentes Grandes
**Prioridade: P3**

```
ServiceCatalogPage.tsx (1.115 linhas):
→ ServiceList.tsx + ServiceFilters.tsx + ServiceCard.tsx + ServiceCatalogHeader.tsx

ContractsPage.tsx (1.053 linhas):
→ ContractList.tsx + ContractFilters.tsx + ContractCard.tsx + ContractStudioPanel.tsx

ChangeDetailPage.tsx (880 linhas):
→ ChangeAdvisory.tsx + BlastRadiusPanel.tsx + DecisionForm.tsx + DecisionHistory.tsx

Critério de sucesso:
- Nenhum componente > 400 linhas
- Cada componente tem responsabilidade única
```

---

### FASE 4 — Productização e Refinamento (2-3 semanas)
**Objetivo:** Produto utilizável e polido para utilizadores enterprise

#### F4.1 — Contract Studio Polish
```
- Wizard flow melhorado (step-by-step claro)
- Preview de diff em tempo real durante edição
- Validação inline de schema durante edição
- Assistente AI para sugestões de contrato (depende F2.2)
```

#### F4.2 — Dashboard e UX por Persona
```
- Dashboard Engineer: focado em serviços próprios + changes ativas
- Dashboard Tech Lead: visão de equipa + reliability + governance score
- Dashboard Architect: topologia + blast radius + contratos críticos
- Dashboard Executive: KPIs + SLA + riscos + tendências
- Dashboard Auditor: audit trail + compliance + evidências
```

#### F4.3 — Produto Analytics Real
```
- Event tracking backend: POST /api/v1/internal/analytics/events
- Frontend: hook useAnalytics() que envia eventos ao backend
- Métricas: page_view, feature_used, time_to_first_value
- Dashboard produto: métricas reais de adoção
```

#### F4.4 — OTLP Exporter Configurável
```
- appsettings.json: Otlp:Endpoint configurável
- Opcional por padrão — não falha se não configurado
- Documentar configuração para Grafana/Jaeger/Tempo
```

---

### FASE 5 — Evolução Seletiva (Pós-Fases 1-4)
**Condição:** Apenas após fechar as Fases 1-3 completamente

- PR-17 reinterpretado: Enterprise Rollout Templates
- PR-18 reinterpretado: Connector SDK
- PR-19: Change Advisory Intelligence avançada
- PR-20: Operational Knowledge Memory
- PR-21+: Apenas com evidência de valor real

---

## PARTE 8 — CRONOGRAMA E PRIORIDADES

### Matriz de Prioridades

| Item | Impacto | Esforço | Prioridade |
|------|---------|---------|------------|
| Migrations EF em falta | CRÍTICO | BAIXO | P0 Imediato |
| AiAssistantPage → backend real | MÉDIO | BAIXO | P0 Imediato |
| IncidentsPage → API real | ALTO | BAIXO | P0 Imediato |
| Demo data indicator (Governance) | MÉDIO | BAIXO | P0 Imediato |
| Governance Module persistência real | CRÍTICO | ALTO | P1 Fase 2 |
| LLM Provider integration | CRÍTICO | ALTO | P1 Fase 2 |
| Correlation Engine dinâmica | ALTO | MÉDIO | P1 Fase 2 |
| CreateIncident endpoint | MÉDIO | BAIXO | P1 Fase 2 |
| Integration Tests (Testcontainers) | ALTO | MÉDIO | P1 Fase 3 |
| E2E Tests (Playwright) | ALTO | MÉDIO | P1 Fase 3 |
| httpOnly Cookies | ALTO | MÉDIO | P1 Fase 3 |
| EmptyState/Error States | MÉDIO | MÉDIO | P1 Fase 3 |
| Self-hosted Fonts | BAIXO | BAIXO | P2 Fase 3 |
| Developer Portal Backend | MÉDIO | ALTO | P1 Fase 2 |
| Nullable Warnings | BAIXO | MÉDIO | P2 Fase 3 |
| Componentes grandes | BAIXO | MÉDIO | P3 Fase 3 |
| Contract Studio Polish | MÉDIO | MÉDIO | P2 Fase 4 |
| Product Analytics Real | MÉDIO | ALTO | P2 Fase 4 |

---

### Sprint Plan (Sugestão)

| Sprint | Duração | Foco | Entregáveis-chave |
|--------|---------|------|-------------------|
| **Sprint 0** | 3 dias | Correções imediatas (F1) | EF migrations, IncidentsPage fix, AiAssistantPage fix, demo indicators |
| **Sprint 1** | 2 semanas | Governance real (F2.1) | GovernanceDbContext, handlers reais, migration |
| **Sprint 2** | 2 semanas | AI + Correlation (F2.2, F2.3) | LLM provider, correlation engine, CreateIncident |
| **Sprint 3** | 1 semana | Developer Portal (F2.4) | SearchCatalog, GetApiHealth, GetMyApis |
| **Sprint 4** | 2 semanas | Integration Tests (F3.1) | 50+ integration tests com Testcontainers |
| **Sprint 5** | 1 semana | E2E Tests (F3.2) | 5 E2E flows com Playwright |
| **Sprint 6** | 1 semana | UX States (F3.3) | EmptyState/Error states em todas as páginas |
| **Sprint 7** | 1 semana | Security (F3.5) | httpOnly cookies, CSRF |
| **Sprint 8** | 1 semana | Polish (F3.6, F3.7) | Fonts self-hosted, componentes refatorados |
| **Sprint 9** | 2 semanas | Productização (F4) | Dashboard por persona, Contract Studio, Analytics |

**Total estimado:** ~12-14 semanas para produto completamente pronto para produção

---

## PARTE 9 — GATE GO/NO-GO ATUALIZADO

### Estado Real dos Gates de Qualidade

#### Gate G1 — Saída da Onda 1 (Current State)

| Critério | Estado | Notas |
|----------|--------|-------|
| Source of Truth / Contract Governance ponta a ponta | ✅ GO | 100% real, 466 testes |
| Change Confidence ponta a ponta | ✅ GO | 100% real, advisory engine |
| Incident Correlation ponta a ponta | ⚠️ PARCIAL | Persistência real mas correlação estática |
| AI Assistant grounded útil | ❌ NO-GO | Sem LLM real, mock conversations |
| i18n nas telas críticas | ✅ GO | 4 locales, 1650+ chaves |
| Navegação entre entidades | ✅ GO | Links bidirecionais implementados |
| Testes mínimos dos fluxos centrais | ⚠️ PARCIAL | Unitários sim, integration/E2E não |
| Docs refletem estado real | ⚠️ PARCIAL | Alguns desatualizados |

**Veredicto G1: NO-GO para próxima fase.** Critérios de AI e testes de integração bloqueiam.

#### O que falta para G1 GO:
1. ✅ Fechar AiAssistantPage (Fase 1 — rápido)
2. ✅ Integrar LLM provider mínimo (Fase 2 — prioritário)
3. ✅ Criar pelo menos 20 integration tests (Fase 3)
4. ✅ Correlation engine dinâmica (Fase 2)

---

## PARTE 10 — CONCLUSÃO DO ARQUITETO

### Avaliação Final

O **NexTraceOne tem um núcleo arquitetural sólido e bem executado**. As decisões de design são corretas, os padrões são bem aplicados, e os módulos core (IdentityAccess, Catalog, ChangeGovernance, AuditCompliance) estão prontos para produção.

**O principal problema do projeto não é arquitetura — é consistência de implementação.**

30% do codebase são dados mock que criam a ilusão de funcionalidade. Isto é compreensível num contexto de desenvolvimento iterativo, mas precisa ser resolvido antes de qualquer claim de "pronto para produção" em toda a plataforma.

### Os 3 Riscos Maiores

1. **Governance 100% mock** — O módulo com mais visibilidade para stakeholders é completamente fictício. Isto é o gap de maior impacto para a credibilidade do produto.

2. **Sem LLM real** — Um produto que se posiciona como "AI-powered" sem integração AI funcional é um risco de produto significativo.

3. **Sem integration tests** — 1.243 testes unitários não garantem que o produto funciona contra PostgreSQL real. Uma regressão de SQL silenciosa pode chegar a produção.

### Os 3 Maiores Pontos Fortes

1. **Change Confidence é genuinamente bom** — O fluxo mais maduro do produto. Advisory engine, blast radius, workflow, evidence pack — tudo real e bem testado.

2. **Source of Truth é sólido** — Contract lifecycle, versioning, diff semântico — fundamentação técnica adequada para o posicionamento do produto.

3. **Arquitetura extensível** — A separação de módulos e building blocks torna a adição de funcionalidades uma questão de implementar handlers, não de refatorar estrutura.

### Recomendação Final

**Executar as Fases 1-3 deste plano antes de qualquer nova feature ou PR de evolução.**

Nenhum novo conceito deve entrar no produto enquanto os fluxos centrais tiverem gaps de persistência, sem integration tests, e com AI sem LLM real. A tentação de expandir horizontalmente deve ser resistida até que o núcleo seja genuinamente sólido de ponta a ponta.

O produto tem 70% do trabalho feito. As Fases 1-3 fecham os 30% restantes críticos. Após isso, a plataforma estará numa posição real para evoluir para os Horizontes 2 e 3 do roadmap com base sólida.

---

## APÊNDICE A — Inventário de Ficheiros Críticos

### Backend — Ficheiros mais importantes

| Ficheiro | Importância | Notas |
|----------|-------------|-------|
| `src/platform/NexTraceOne.ApiHost/Program.cs` | CRÍTICO | Composition root — bem estruturado |
| `src/building-blocks/NexTraceOne.BuildingBlocks.Core/Primitives/AggregateRoot.cs` | CORE | Base do DDD |
| `src/building-blocks/NexTraceOne.BuildingBlocks.Infrastructure/Persistence/NexTraceDbContextBase.cs` | CORE | Base de todos os DbContexts |
| `src/modules/catalog/NexTraceOne.Catalog.Domain/Contracts/Entities/ContractVersion.cs` | ALTO | Entidade mais complexa (lifecycle state machine) |
| `src/modules/changegovernance/.../GetChangeAdvisory/GetChangeAdvisory.cs` | ALTO | Advisory engine central |
| `src/modules/operationalintelligence/.../EfIncidentStore.cs` | ALTO | 678 linhas, store principal de incidents |

### Frontend — Ficheiros mais importantes

| Ficheiro | Importância | Notas |
|----------|-------------|-------|
| `src/frontend/src/components/shell/Sidebar.tsx` | CRÍTICO | Navegação principal |
| `src/frontend/src/auth/PersonaContext.tsx` | ALTO | Contexto de persona para UX |
| `src/frontend/src/shared/api/client.ts` | ALTO | API client centralizado |
| `src/frontend/src/features/change-governance/pages/ChangeDetailPage.tsx` | ALTO | 880 linhas, página mais complexa |
| `src/frontend/src/features/operations/pages/IncidentsPage.tsx` | MÉDIO | 206 linhas, ainda com mock inline |

---

## APÊNDICE B — Checklist de Prontidão para Produção

### Infraestrutura ✅/❌

- ✅ Health/Ready/Live endpoints implementados
- ✅ Structured logging com Serilog
- ✅ OpenTelemetry configurado (exporter inativo por padrão)
- ✅ Rate limiting por IP
- ✅ Security headers (CSP, HSTS, X-Frame-Options)
- ✅ CORS restritivo
- ✅ Assembly integrity check no boot
- ✅ Graceful shutdown
- ✅ Background workers (Outbox, Identity Expiration)
- ❌ OTLP exporter não configurado por padrão
- ❌ Sem documentação de variáveis de ambiente obrigatórias
- ❌ Sem Helm chart ou Kubernetes manifests

### Segurança ✅/❌

- ✅ JWT com RBAC granular em todos os endpoints
- ✅ RequirePermission em 22 endpoint modules
- ✅ Multi-tenancy com RLS
- ✅ Audit trail imutável SHA-256
- ✅ Tokens refresh em memória (não persistido no browser)
- ✅ Source maps desativados em produção
- ✅ Build hardening (terser drop_console)
- ❌ Tokens em sessionStorage (vulnerável a XSS na mesma origem)
- ❌ Sem httpOnly cookies
- ❌ Sem encryption at rest para campos sensíveis
- ❌ Sem signing de artefatos

### Módulos ✅/❌

- ✅ IdentityAccess — Produção ready
- ✅ Catalog (Source of Truth) — Produção ready
- ✅ ChangeGovernance — Produção ready
- ✅ AuditCompliance — Produção ready
- ⚠️ OperationalIntelligence — Parcialmente ready (incidents OK, resto mock)
- ❌ Governance — Não ready (100% mock)
- ❌ AIKnowledge — Não ready (sem LLM, sem migrations)
- ⚠️ BackgroundWorkers — Ready mas sem OTLP

### Testes ✅/❌

- ✅ 1.243 unit tests backend (100% passam)
- ✅ 266 unit tests frontend (100% passam)
- ❌ Zero integration tests com DB real
- ❌ Zero E2E tests reais

---

*Análise gerada em Março 2026 por inspeção direta do codebase NexTraceOne.*
*Baseada em 1.918 ficheiros analisados, todos os documentos de referência lidos, e código-fonte inspecionado modulo a módulo.*
