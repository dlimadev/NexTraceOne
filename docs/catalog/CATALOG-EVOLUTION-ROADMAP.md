# NexTraceOne — Plano de Ação: Evolução do Módulo Catalog

> **Versão:** 1.0  
> **Data:** 2025-07-18  
> **Módulo:** Catalog (Graph + Contracts + Portal + SourceOfTruth + LegacyAssets)  
> **Estado atual:** Estável — 834 testes a passar, zero erros de compilação  
> **Pilares reforçados:** Service Governance · Contract Governance · Source of Truth · Change Intelligence · AI-assisted Operations

---

## Sumário Executivo

Este documento consolida todas as propostas de evolução do módulo Catalog identificadas durante a análise de funcionalidades, pesquisa de mercado (Backstage, Cortex, OpsLevel, Dynatrace, Compass) e avaliação de gaps internos. O plano está organizado em **8 fases incrementais**, ordenadas por valor estratégico e viabilidade técnica, respeitando a arquitetura modular monolítica, Clean Architecture, DDD e os princípios do NexTraceOne.

---

## Índice

1. [Fase 1 — Contract Studio Editor (Swagger Editor-like)](#fase-1--contract-studio-editor-swagger-editor-like)
2. [Fase 2 — Service Discovery Automático](#fase-2--service-discovery-automático)
3. [Fase 3 — Links e Documentação Enriquecidos](#fase-3--links-e-documentação-enriquecidos)
4. [Fase 4 — Entity Registry e Visual Builder](#fase-4--entity-registry-e-visual-builder)
5. [Fase 5 — IA Enriquecida para Contratos](#fase-5--ia-enriquecida-para-contratos)
6. [Fase 6 — Import/Export Avançado](#fase-6--importexport-avançado)
7. [Fase 7 — Integração com APIM (WSO2, Azure APIM)](#fase-7--integração-com-apim-wso2-azure-apim)
8. [Fase 8 — Scorecard de Maturidade e Governança Avançada](#fase-8--scorecard-de-maturidade-e-governança-avançada)

---

## Fase 1 — Contract Studio Editor (Swagger Editor-like)

### Objetivo de produto

Proporcionar uma experiência de autoria e visualização de contratos equivalente ao Swagger Editor (editor.swagger.io), com split-pane: **editor de código à esquerda + preview renderizado à direita**, diretamente no Contract Studio do NexTraceOne. Esta é a funcionalidade que mais diferencia a experiência de governança de contratos do produto.

### Estado atual encontrado

- `ContractSection.tsx` já possui **3 modos separados** (Visual / Source / Preview), mas **não** split-pane simultâneo.
- `SourceEditor` usa `<textarea>` com line numbers manuais — funcional mas limitado (sem syntax highlighting real, sem autocomplete, sem markers de erro inline).
- `PreviewPanel` renderiza o conteúdo como `<pre>` raw — não existe renderização semântica (endpoints, schemas, servers, security como no Swagger UI).
- `DraftStudioPage.tsx` reutiliza `ContractSection` para edição de drafts.
- Já existe infraestrutura de `CanonicalModelBuilder` no backend que parseia OpenAPI, Swagger, AsyncAPI e WSDL em modelo canónico (`ContractCanonicalModel`).

### Solução proposta

#### 1.1 Frontend — Split-Pane Editor

| Componente | Descrição |
|---|---|
| `ContractEditorSplitPane` | Layout split-pane resizable (editor esquerda + preview direita) |
| `MonacoEditorWrapper` | Integração com Monaco Editor (syntax highlighting YAML/JSON/XML, autocomplete OpenAPI, error markers inline, minimap) |
| `LivePreviewRenderer` | Preview renderizado em tempo real que interpreta o spec e mostra: info, servers, endpoints, schemas, security, examples — inspirado na estética do Swagger UI mas com design system NexTraceOne |
| `ProtocolPreviewAdapter` | Adapter pattern para preview por protocolo (REST/OpenAPI → endpoint cards, SOAP/WSDL → operations/messages, AsyncAPI → channels/messages, gRPC → services/methods) |

#### 1.2 Experiência alvo

```
┌───────────────────────────────────────────────────────────────────┐
│  Contract Studio — Payment API v2.1.0   [Visual] [Editor] [⬛Split] [Preview]  │
├─────────────────────────────┬─────────────────────────────────────┤
│ Monaco Editor               │ Live Preview                       │
│                             │                                    │
│ openapi: '3.1.0'           │ ┌─ Payment API v2.1.0 ───────────┐│
│ info:                       │ │ MIT License · JSON              ││
│   title: Payment API        │ │                                 ││
│   version: 2.1.0            │ │ Base URL: /api/v2/payments      ││
│   description: |            │ │                                 ││
│     Manages payments...     │ │ ── Endpoints ──────────────     ││
│ servers:                    │ │ POST /payments  Create payment  ││
│   - url: /api/v2/payments   │ │ GET  /payments/{id}  Get by ID  ││
│ paths:                      │ │ PUT  /payments/{id}  Update     ││
│   /payments:                │ │                                 ││
│     post:                   │ │ ── Schemas ───────────────      ││
│       summary: Create...    │ │ PaymentRequest { amount, ... }  ││
│       requestBody:          │ │ PaymentResponse { id, ... }     ││
│         content:            │ │                                 ││
│           application/json: │ │ ── Security ──────────────      ││
│             schema:         │ │ BearerAuth (JWT)                ││
│               $ref: '#/...' │ │                                 ││
│                             │ └─────────────────────────────────┘│
├─────────────────────────────┴─────────────────────────────────────┤
│ Status: ✓ Valid OpenAPI 3.1  │  Lines: 142  │  Warnings: 0       │
└───────────────────────────────────────────────────────────────────┘
```

#### 1.3 Funcionalidades do Editor (Monaco)

- Syntax highlighting para YAML, JSON, XML
- Autocomplete contextual (keywords OpenAPI 3.x, AsyncAPI 2.x/3.x)
- Validação inline com error/warning markers (squiggles)
- Minimap para navegação rápida
- Outline/breadcrumb para navegação por secções do spec
- Folding de secções
- Formatação automática (Shift+Alt+F)
- Temas dark/light alinhados com design system
- Diff view para comparar versões (opcional, fase evolutiva)

#### 1.4 Funcionalidades do Preview (Live)

- Renderização em tempo real com debounce (~300ms)
- Secção **Info**: título, versão, descrição, licença, contact
- Secção **Servers**: lista de servidores com badges de protocolo e ambiente
- Secção **Endpoints/Operations**: cards colapsáveis por path + method, com request/response schemas
- Secção **Schemas/Models**: visualização de schemas com tipos, exemplos, required markers
- Secção **Security**: esquemas de autenticação/autorização
- Secção **Examples**: exemplos de request/response quando disponíveis
- Secção **Tags**: agrupamento por tags com descrições
- Adaptação por protocolo:
  - **OpenAPI/Swagger**: endpoints REST clássicos
  - **AsyncAPI**: channels, messages, bindings
  - **WSDL/SOAP**: operations, messages, ports, bindings
  - **gRPC/Protobuf**: services, methods, messages
  - **WorkerService**: triggers, schedules, payloads

#### 1.5 Backend — Suporte

| Endpoint | Descrição |
|---|---|
| `POST /api/contracts/validate-spec` | Valida spec em tempo real, retorna errors/warnings estruturados com line/column |
| `POST /api/contracts/parse-preview` | Parseia spec e retorna `ContractCanonicalModel` para alimentar o preview (reutiliza `CanonicalModelBuilder`) |

#### 1.6 Bibliotecas frontend sugeridas

| Biblioteca | Propósito |
|---|---|
| `@monaco-editor/react` | Monaco Editor para React |
| `react-resizable-panels` | Split-pane resizable |
| `js-yaml` | Parse YAML no browser para preview |
| (custom) | Renderer de preview com componentes do design system |

#### 1.7 Impacto em ficheiros existentes

| Ficheiro | Ação |
|---|---|
| `ContractSection.tsx` | Adicionar modo `split` como 4º modo de editor |
| `DraftStudioPage.tsx` | Adaptar para usar split-pane como modo padrão para drafts |
| `ContractWorkspacePage.tsx` | Integrar split-pane no modo `contract` |
| `package.json` | Adicionar `@monaco-editor/react`, `react-resizable-panels` |
| (novo) `MonacoEditorWrapper.tsx` | Componente wrapper do Monaco |
| (novo) `LivePreviewRenderer.tsx` | Componente de preview renderizado |
| (novo) `ContractEditorSplitPane.tsx` | Layout split-pane |
| (novo) `ProtocolPreviewAdapter.tsx` | Adapter de preview por protocolo |
| (novo) `useSpecValidation.ts` | Hook de validação em tempo real |
| (novo) `useSpecPreview.ts` | Hook de parsing para preview |

#### 1.8 Prioridade: 🔴 CRÍTICA

Justificativa: Diferenciador principal de experiência. A experiência atual com `<textarea>` é funcional mas não atinge nível enterprise. O split-pane editor+preview é o padrão de mercado (Swagger Editor, Stoplight, Redocly) e é essencial para que o Contract Studio seja competitivo.

---

## Fase 2 — Service Discovery Automático

### Objetivo de produto

Descobrir automaticamente serviços presentes em ambientes non-prod e prod a partir de traces OpenTelemetry e logs estruturados, sugerindo registo no catálogo para serviços desconhecidos. Reforça o NexTraceOne como **Source of Truth** ao garantir que nenhum serviço opera "invisível".

### Estado atual encontrado

- `DiscoverySource` já existe como entidade com `SourceType`, `ExternalReference`, `ConfidenceScore`.
- `InferDependencyFromOtel` já extrai dependências de traces OTel.
- `IObservabilityProvider` tem `QueryTraces` e `QueryLogs`.
- `ITelemetryQueryService` tem queries orientadas a produto.
- Não existe ainda entidade `DiscoveredService` nem job de discovery automático.

### Solução proposta

#### 2.1 Backend — Domain

| Entidade/VO | Descrição |
|---|---|
| `DiscoveredService` | Serviço descoberto: `service.name`, `service.namespace`, `first_seen`, `last_seen`, `environment`, `trace_count`, `endpoint_count`, `status` (Pending/Matched/Ignored/Registered) |
| `DiscoveryRun` | Registo de cada execução do job: timestamp, source, services found, errors |
| `DiscoveryMatchRule` | Regras de matching automático (regex → ServiceAsset) |

#### 2.2 Backend — Application

| Feature | Tipo | Descrição |
|---|---|---|
| `RunServiceDiscovery` | Command | Executa discovery query nas fontes de telemetria |
| `ListDiscoveredServices` | Query | Lista serviços descobertos com filtros (status, environment, date range) |
| `MatchDiscoveredService` | Command | Associa serviço descoberto a ServiceAsset existente |
| `RegisterFromDiscovery` | Command | Cria ServiceAsset a partir de serviço descoberto |
| `IgnoreDiscoveredService` | Command | Marca como ignorado (não relevante para catálogo) |
| `GetDiscoveryDashboard` | Query | Stats: total discovered, matched, pending, new this week |

#### 2.3 Backend — Infrastructure

| Componente | Descrição |
|---|---|
| `ServiceDiscoveryJob` | Quartz.NET job periódico (configurável: hourly, daily) |
| `OtelServiceDiscoveryProvider` | Consulta ClickHouse para `DISTINCT service.name` com contagem |
| `EfDiscoveredServiceRepository` | Persistência PostgreSQL |

#### 2.4 Frontend

| Página/Componente | Descrição |
|---|---|
| `ServiceDiscoveryPage` | Dashboard de discovery: lista de serviços descobertos, filtros, bulk actions |
| `DiscoveryMatchDialog` | Modal para associar serviço descoberto a serviço existente |
| `DiscoveryRegisterWizard` | Wizard simplificado para registar novo serviço a partir de discovery |
| `DiscoveryInsightCard` | Card no ServiceCatalogPage mostrando "X serviços por registar" |

#### 2.5 Prioridade: 🔴 ALTA

Justificativa: Source of Truth obrigatória — serviços não registados são um dos maiores gaps em ambiente enterprise. Feature diferenciadora vs Backstage (manual-only discovery).

---

## Fase 3 — Links e Documentação Enriquecidos

### Objetivo de produto

Permitir associar múltiplos links categorizados a serviços e contratos (repositório, documentação, CI/CD, monitoring, wiki, Swagger UI, Backstage, ADRs). Enriquece o catálogo como **knowledge hub** operacional.

### Estado atual encontrado

- `ServiceAsset` tem apenas `DocumentationUrl` (string) e `RepositoryUrl` (string) — modelo limitado a 2 links fixos.
- Não existe modelo de links para contratos.

### Solução proposta

#### 3.1 Backend — Domain

| Entidade | Campos |
|---|---|
| `ServiceLink` | `Id`, `ServiceAssetId`, `Category` (enum: Repository, Documentation, CiCd, Monitoring, Wiki, SwaggerUi, Backstage, Adr, Runbook, Other), `Title`, `Url`, `Description?`, `IconHint?`, `SortOrder`, `CreatedAt` |
| `ContractLink` | `Id`, `ContractVersionId` ou `ApiAssetId`, `Category`, `Title`, `Url`, `Description?`, `SortOrder`, `CreatedAt` |
| `LinkCategory` enum | Repository, Documentation, CiCd, Monitoring, Wiki, SwaggerUi, ApiPortal, Backstage, Adr, Runbook, Changelog, Dashboard, Other |

#### 3.2 Backend — Features

| Feature | Descrição |
|---|---|
| `AddServiceLink` | Adiciona link a serviço |
| `RemoveServiceLink` | Remove link de serviço |
| `ListServiceLinks` | Lista links de um serviço |
| `AddContractLink` | Adiciona link a contrato |
| `RemoveContractLink` | Remove link de contrato |
| `ListContractLinks` | Lista links de um contrato |

#### 3.3 Frontend

| Componente | Descrição |
|---|---|
| `ServiceLinksSection` | Secção no ServiceDetailPage com lista categorizada de links |
| `ContractLinksSection` | Secção no ContractWorkspacePage |
| `LinkEditor` | Componente reutilizável para CRUD de links com ícone por categoria |

#### 3.4 Migração

- Migração que converte `DocumentationUrl` e `RepositoryUrl` existentes em registos `ServiceLink` e torna os campos antigos deprecated.

#### 3.5 Prioridade: 🟠 ALTA

Justificativa: Baixo esforço, alto valor. Transforma o catálogo em ponto central de referência operacional. Essencial para persona Engineer e Tech Lead.

---

## Fase 4 — Entity Registry e Visual Builder

### Objetivo de produto

Permitir a criação, gestão e reutilização de entidades, propriedades e tipos de dados canónicos (shared schemas) que possam ser referenciados na autoria de contratos. Garante **consistência** entre contratos e reduz duplicação de modelos.

### Estado atual encontrado

- `CanonicalEntity` existe com `Name`, `Domain`, `Category`, `SchemaContent` (JSON Schema/Avro/Protobuf), `Version`, `Aliases`, `Tags`, `ReusePolicy`.
- `ContractSchemaElement` é recursivo (Name, DataType, IsRequired, Format, DefaultValue, Children).
- Não existe ainda: `EntityProperty` decomposto, `DataTypeDefinition` custom, `EntityRelationship`, visual builder de entidades.

### Solução proposta

#### 4.1 Backend — Domain (evoluções)

| Entidade/VO | Descrição |
|---|---|
| `EntityProperty` | Propriedade individual: `Name`, `DataType` (ref a DataTypeDefinition ou primitivo), `IsRequired`, `Format`, `DefaultValue`, `Description`, `Constraints` (min, max, pattern, enum), `SortOrder` |
| `DataTypeDefinition` | Tipo custom reutilizável: `Name`, `BaseType`, `Format`, `Constraints`, `IsEnum`, `EnumValues`, `Domain` |
| `EntityRelationship` | Relação entre entidades: `SourceEntityId`, `TargetEntityId`, `RelationshipType` (OneToOne, OneToMany, ManyToMany, Composition, Reference), `Description` |

#### 4.2 Frontend

| Página/Componente | Descrição |
|---|---|
| `CanonicalEntityCatalogPage` (evolução) | Já existe — evoluir para incluir visual builder de propriedades |
| `EntityVisualBuilder` | Editor visual drag-and-drop para definir propriedades, tipos, relações |
| `EntityRelationshipDiagram` | Visualização de relações entre entidades (grafo simples) |
| `EntityPickerDialog` | Dialog para selecionar entidade canónica ao criar operações no contrato |
| `TypeDefinitionCatalog` | Catálogo de tipos de dados custom reutilizáveis |

#### 4.3 Integração com Contract Studio

- Ao definir request/response body num contrato (Visual Builder ou source), o utilizador pode importar entidades do Entity Registry.
- O `CanonicalModelBuilder` deve resolver `$ref` a entidades canónicas no preview.

#### 4.4 Prioridade: 🟠 MÉDIA-ALTA

Justificativa: Reduz duplicação de schemas entre contratos. Reforça consistência cross-domínio. Valor diferenciador face a Backstage/Stoplight que não têm entity registry nativo governado.

---

## Fase 5 — IA Enriquecida para Contratos

### Objetivo de produto

Melhorar a criação assistida por IA de contratos com contexto mais rico: entity awareness, geração de exemplos, detecção de duplicação, sugestão de melhorias e explicação de impacto de alterações.

### Estado atual encontrado

- `GenerateDraftFromAi` existe (Command → `IAiDraftGenerator` → `AiDraftGeneratorService` via `IChatCompletionProvider`).
- IA já gera drafts completos a partir de prompt de utilizador.
- Não existe: entity context no prompt, geração de examples, detecção de duplicação, sugestão de melhorias.

### Solução proposta

#### 5.1 Backend — Melhorias ao AiDraftGeneratorService

| Capacidade | Descrição |
|---|---|
| Entity-aware generation | Injectar lista de `CanonicalEntity` relevantes no prompt para que a IA use schemas existentes |
| Example generation | Agente que gera exemplos de request/response válidos para cada operação |
| Duplication detection | Antes de gerar, comparar com contratos existentes e alertar se há sobreposição |
| Improvement suggestions | Analisar draft existente e sugerir melhorias (naming, schemas, security, pagination, error handling) |
| Impact explanation | Dado um diff entre versões, explicar impacto de breaking changes em linguagem humana |

#### 5.2 Backend — Novos agentes/features

| Feature | Descrição |
|---|---|
| `GenerateContractExamples` | Command que gera examples para operações de um draft/version |
| `AnalyzeContractQuality` | Query que analisa qualidade do spec e retorna sugestões |
| `DetectContractDuplication` | Query que compara draft com contratos existentes |
| `ExplainContractDiff` | Query que explica diff semântico em linguagem natural |

#### 5.3 Frontend

| Componente | Descrição |
|---|---|
| `AiSuggestionsPanel` | Painel lateral no Contract Studio com sugestões de IA |
| `ExampleGeneratorButton` | Botão no editor que gera examples via IA |
| `DuplicationAlert` | Alert no draft indicando contratos similares existentes |
| `DiffExplanation` | Explicação em linguagem natural no ChangelogSection |

#### 5.4 Prioridade: 🟡 MÉDIA

Justificativa: Incrementa valor da IA existente. Depende parcialmente de Fase 4 (Entity Registry) para máximo valor. Reforça pilar AI-assisted Operations.

---

## Fase 6 — Import/Export Avançado

### Objetivo de produto

Fortalecer capacidades de importação e exportação de contratos com conversão entre formatos (JSON↔YAML), importação por URL, preview antes de importar, e exportação em bundle.

### Estado atual encontrado

- `ImportContract` suporta multi-protocolo (JSON/YAML/XML) com auto-detect.
- `ExportContract` e `ExportDraft` exportam spec raw.
- Não existe: conversão JSON↔YAML, importação por URL, preview antes de importar, bundle export.

### Solução proposta

#### 6.1 Backend — Novas features

| Feature | Descrição |
|---|---|
| `ConvertContractFormat` | Command que converte spec entre JSON↔YAML (e XML→JSON para WSDL quando possível) |
| `ImportContractFromUrl` | Command que fetcha spec a partir de URL (público ou com auth token), valida e importa |
| `PreviewImport` | Query que parseia spec sem persistir, retorna canonical model + warnings para o utilizador decidir |
| `ExportContractBundle` | Command que exporta spec + examples + schemas + metadata num ZIP ou tar.gz |
| `BulkExport` | Command que exporta múltiplos contratos de um serviço/domínio |

#### 6.2 Frontend

| Componente | Descrição |
|---|---|
| `ImportDialog` (evolução) | Adicionar tab "Import from URL" + preview step antes de confirmar |
| `FormatConverterButton` | Botão no editor para converter entre JSON↔YAML |
| `ExportOptionsMenu` | Menu com opções: Raw, Converted (outro formato), Bundle, Bulk |
| `ImportPreviewPanel` | Preview do que será importado antes de confirmar |

#### 6.3 Prioridade: 🟡 MÉDIA

Justificativa: Quality-of-life improvement. Importante para onboarding de organizações que já têm specs dispersos. URL import é particularmente útil para integração com repositórios Git.

---

## Fase 7 — Integração com APIM (WSO2, Azure APIM)

### Objetivo de produto

Permitir sincronização bidirecional entre o catálogo NexTraceOne e plataformas de API Management (WSO2, Azure APIM), reforçando o NexTraceOne como **source of truth** que alimenta os gateways.

### Estado atual encontrado

- Existem patterns de integração externa: `ImportFromBackstage`, `ImportFromKongGateway` (como referência).
- Não existe integração com WSO2 ou Azure APIM.
- `InteroperabilityProfile` e `SchemaRegistryBinding` demonstram preparação para cross-platform.

### Solução proposta

#### 7.1 Backend — Abstração

| Componente | Descrição |
|---|---|
| `IApimGateway` | Interface genérica: `ImportApis`, `ExportApi`, `SyncApi`, `ListApis`, `GetApiStatus` |
| `Wso2ApimGateway` | Implementação para WSO2 API Manager via REST API |
| `AzureApimGateway` | Implementação para Azure API Management via Management API |
| `ApimIntegrationConfiguration` | Configuração por tenant: URL, credenciais (vault ref), sync mode (manual/scheduled/webhook) |

#### 7.2 Backend — Features

| Feature | Descrição |
|---|---|
| `ImportFromApim` | Command que importa APIs de um gateway APIM para o catálogo |
| `PublishToApim` | Command que publica contrato do NexTraceOne num gateway APIM |
| `SyncApimStatus` | Query que compara estado no NexTraceOne com estado no gateway |
| `ApimSyncJob` | Quartz.NET job para sincronização periódica |

#### 7.3 Frontend

| Página/Componente | Descrição |
|---|---|
| `ApimIntegrationPage` | Configuração de integrações APIM |
| `ApimSyncDashboard` | Dashboard de estado de sincronização |
| `PublishToApimAction` | Ação no ContractWorkspacePage para publicar para gateway |
| `ApimStatusBadge` | Badge no contrato indicando estado no gateway (synced/outdated/not-published) |

#### 7.4 Prioridade: 🟡 MÉDIA

Justificativa: Valor enterprise elevado para organizações que já usam WSO2 ou Azure APIM. Reforça posicionamento como source of truth que governa os gateways, não apenas observa.

---

## Fase 8 — Scorecard de Maturidade e Governança Avançada

### Objetivo de produto

Adicionar scorecards de maturidade por serviço, detecção de serviços órfãos, análise de cobertura de contratos, compliance com standards e feed unificado de atividade. Reforça pilares de **Service Governance** e **Operational Reliability**.

### Estado atual encontrado

- Existe `ScorecardSection` no ContractWorkspace (scorecard por contrato).
- Existe `ServiceAsset` com ownership, criticality, lifecycle.
- Não existe: scorecard composto por serviço, orphan detection, contract coverage analysis, activity feed.

### Solução proposta

#### 8.1 Service Maturity Scorecard

| Dimensão | Critérios |
|---|---|
| Ownership | Tem owner team? Tem tech lead? Ownership verificado recentemente? |
| Documentation | Tem documentação? Links? Runbooks? |
| Contract Coverage | % de endpoints com contrato registado |
| Observability | Tem traces? Métricas? Alerts configurados? |
| Reliability | SLA definido? Incidentes recentes? MTTR? |
| Security | Security schemes definidos? Vulnerabilidades conhecidas? |
| Change Readiness | Deploy pipeline configurado? Promotion gates activos? |

Score composto A-F por serviço, visível no catálogo e no ServiceDetailPage.

#### 8.2 Ownership Audit / Orphan Detection

| Feature | Descrição |
|---|---|
| `DetectOrphanServices` | Query que identifica serviços sem owner, sem equipa, ou com owner inactivo |
| `OwnershipAuditReport` | Relatório periódico de qualidade de ownership |
| `OrphanAlertJob` | Quartz.NET job que gera notificações para serviços órfãos |

#### 8.3 Contract Coverage Analysis

| Feature | Descrição |
|---|---|
| `AnalyzeContractCoverage` | Compara endpoints descobertos via OTel com contratos registados |
| `CoverageReport` | Relatório de gaps: endpoints sem contrato, contratos sem tráfego |
| `CoverageDashboard` | Visualização no ServiceDetailPage |

#### 8.4 Continuous Dependency Inference

| Feature | Descrição |
|---|---|
| `RefreshDependencyGraph` | Job que actualiza grafo de dependências a partir de traces OTel recentes |
| `DependencyDriftAlert` | Detecção de novas dependências não registadas |
| `TopologyEvolution` | Timeline mostrando como a topologia mudou ao longo do tempo |

#### 8.5 Unified Service Activity Feed

| Feature | Descrição |
|---|---|
| `GetServiceActivityFeed` | Feed por serviço: deploys, changes, incidents, contract updates, ownership changes, alerts |
| `ServiceActivityTimeline` | Componente visual timeline no ServiceDetailPage |

#### 8.6 Service-Level Standards/Compliance

| Feature | Descrição |
|---|---|
| `ServiceStandard` | Entidade que define standards obrigatórios por domínio/tier (ex: "Tier 1 services must have SLA, 3 runbooks, contract coverage > 80%") |
| `EvaluateCompliance` | Query que avalia serviço contra standards aplicáveis |
| `ComplianceDashboard` | Dashboard executivo com visão de compliance por domínio |

#### 8.7 Prioridade: 🟢 NORMAL

Justificativa: Funcionalidades de governança avançada que constroem sobre tudo o que vem antes. Valor executivo e para persona Architect/Tech Lead. Dependem de dados de discovery (Fase 2) e entity registry (Fase 4) para máximo impacto.

---

## Matriz de Dependências entre Fases

```
Fase 1 (Contract Studio Editor)     → independente, pode começar imediatamente
Fase 2 (Service Discovery)          → independente, pode começar em paralelo com Fase 1
Fase 3 (Links Enriquecidos)         → independente, pode começar em paralelo
Fase 4 (Entity Registry)            → independente, pode começar em paralelo
Fase 5 (IA Enriquecida)             → beneficia de Fase 4 (entity context)
Fase 6 (Import/Export Avançado)     → beneficia de Fase 1 (preview no editor)
Fase 7 (APIM Integration)           → independente, mas beneficia de Fase 6 (export)
Fase 8 (Scorecard & Governança)     → beneficia de Fase 2 (discovery) + Fase 4 (entities)
```

### Paralelismo recomendado

| Sprint/Ciclo | Fases em paralelo |
|---|---|
| Ciclo 1 | **Fase 1** (Contract Studio Editor) + **Fase 3** (Links) |
| Ciclo 2 | **Fase 2** (Service Discovery) + **Fase 4** (Entity Registry) |
| Ciclo 3 | **Fase 5** (IA Enriquecida) + **Fase 6** (Import/Export) |
| Ciclo 4 | **Fase 7** (APIM Integration) + **Fase 8** (Scorecard & Governança) |

---

## Estimativa de Esforço por Fase

| Fase | Backend | Frontend | Complexidade | Esforço estimado |
|---|---|---|---|---|
| 1 — Contract Studio Editor | Baixo (2 endpoints) | Alto (Monaco, split-pane, preview renderer) | Alta | 3-4 semanas |
| 2 — Service Discovery | Médio (entities, job, queries) | Médio (dashboard, wizard) | Média-Alta | 2-3 semanas |
| 3 — Links Enriquecidos | Baixo (CRUD simples) | Baixo (componentes reutilizáveis) | Baixa | 1 semana |
| 4 — Entity Registry | Médio (domain evolution) | Alto (visual builder) | Média-Alta | 2-3 semanas |
| 5 — IA Enriquecida | Médio (novos agentes) | Médio (panels, buttons) | Média | 2 semanas |
| 6 — Import/Export | Médio (converters, URL fetch) | Baixo-Médio (dialogs) | Média | 1-2 semanas |
| 7 — APIM Integration | Alto (gateway adapters) | Médio (config, dashboard) | Alta | 3-4 semanas |
| 8 — Scorecard & Governança | Médio-Alto (múltiplas features) | Médio-Alto (dashboards) | Média-Alta | 3-4 semanas |

**Total estimado: 17-23 semanas** (parallelismo reduz para ~10-12 semanas com 2 work streams)

---

## Princípios Transversais a Todas as Fases

### Arquitectura

- Todas as features devem residir dentro do bounded context `Catalog` (sub-domains Graph, Contracts, SourceOfTruth).
- Seguir VSA (Vertical Slice Architecture): Command/Query + Validator + Handler + Response por feature.
- Nenhuma feature deve quebrar fronteiras de módulo nem aceder directamente a DbContexts de outros módulos.
- Comunicação entre módulos via contratos claros e eventos de integração quando necessário.

### Qualidade

- Testes unitários obrigatórios para cada feature.
- Testes de integração para jobs e integrações externas.
- i18n em todos os textos de UI.
- Logging estruturado com correlation.
- `CancellationToken` em todas as operações async.
- `sealed` em classes finais.
- Guard clauses no início de métodos.

### Segurança

- Autorização no backend para todas as operações (tenant-aware, environment-aware).
- Frontend reflete permissões mas não é autoridade.
- Auditoria de acções sensíveis (import, export, publish to APIM, discovery registration).
- URLs de import validadas (whitelist/blacklist configurable).

### UX

- Respeitar design system existente (Tailwind CSS, Radix UI patterns).
- Adaptar por persona quando aplicável.
- Estados de loading, erro e vazio em todas as telas novas.
- Responsividade real.
- Sem GUIDs expostos ao utilizador final em fluxos de negócio.

---

## Próximos Passos Recomendados

1. **Validar prioridades** — Confirmar com stakeholders a ordem das fases.
2. **Iniciar Fase 1** (Contract Studio Editor) — maior impacto visual e diferenciador competitivo.
3. **Iniciar Fase 3** (Links) em paralelo — quick win com alto valor.
4. **Preparar spikes técnicos** para Fase 2 (query ClickHouse para service.name) e Fase 7 (API WSO2).
5. **Documentar decisões** arquitecturais de cada fase como ADRs no repositório.

---

## Referências

- [Swagger Editor](https://editor.swagger.io/) — referência visual para Fase 1
- [Backstage Software Catalog](https://backstage.io/docs/features/software-catalog/) — referência para service maturity
- [Cortex Service Catalog](https://www.cortex.io/) — referência para scorecards e standards
- [OpsLevel Service Maturity](https://www.opslevel.com/) — referência para maturity model
- [WSO2 API Manager REST API](https://apim.docs.wso2.com/en/latest/reference/product-apis/overview/) — referência para Fase 7
- [Azure APIM Management API](https://learn.microsoft.com/en-us/rest/api/apimanagement/) — referência para Fase 7

---

*Este documento deve ser atualizado à medida que cada fase for concluída ou que prioridades mudem.*
