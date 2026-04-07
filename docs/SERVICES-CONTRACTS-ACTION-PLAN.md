# 📋 Plano de Ação — Módulo de Serviços e Contratos

> **✅ COMPLETO — April 2026**: Todas as 197 tarefas foram concluídas. BUG-01 a BUG-05 corrigidos, GAP-01 a GAP-10 resolvidos, F-01 a F-15 implementadas.

> **Data**: 2026-04-05  
> **Módulo**: Catalog (Graph + Contracts + Portal + DeveloperExperience + Templates)  
> **Objetivo**: Corrigir bugs, fechar gaps, melhorar UX e implementar novas funcionalidades inovadoras  
> **Prioridade**: Alinhado com a visão do NexTraceOne como Source of Truth de serviços e contratos

---

## Índice

1. [Bugs Críticos a Corrigir](#1-bugs-críticos-a-corrigir)
2. [Gaps e Desenvolvimento Incompleto](#2-gaps-e-desenvolvimento-incompleto)
3. [Melhorias de UX na Criação de Serviços](#3-melhorias-de-ux-na-criação-de-serviços)
4. [Melhorias de UX na Criação de Contratos REST](#4-melhorias-de-ux-na-criação-de-contratos-rest)
5. [Novo Tipo de Serviço: Framework / SDK](#5-novo-tipo-de-serviço-framework--sdk)
6. [Correlação Canónica — Request/Response ↔ Canonical Entities](#6-correlação-canónica--requestresponse--canonical-entities)
7. [Novas Funcionalidades (15 Features)](#7-novas-funcionalidades-15-features)
8. [Cronograma de Fases](#8-cronograma-de-fases)
9. [Definição de Pronto (DoD)](#9-definição-de-pronto-dod)

---

## 1. Bugs Críticos a Corrigir

### BUG-01: Frontend envia campos que o backend descarta silenciosamente ⚠️ CRÍTICO

**Localização**:
- Frontend form: `src/frontend/src/features/catalog/pages/ServiceCatalogPage.tsx:63-66`
- API client: `src/frontend/src/features/catalog/api/serviceCatalog.ts:76`
- Backend command: `src/modules/catalog/NexTraceOne.Catalog.Application/Graph/Features/RegisterServiceAsset/RegisterServiceAsset.cs:20`

**Problema**: O formulário de criação de serviço coleta 11 campos (`name`, `domain`, `team`, `description`, `serviceType`, `criticality`, `exposureType`, `technicalOwner`, `businessOwner`, `documentationUrl`, `repositoryUrl`), mas:
- O API client TypeScript só envia 3 campos: `{ name, team, description }`
- O backend `RegisterServiceAsset.Command` só aceita 3 campos: `(Name, Domain, TeamName)`
- **Resultado**: 8 campos são preenchidos pelo utilizador e silenciosamente descartados

**Correção**:
- [x] **Backend**: Expandir `RegisterServiceAsset.Command` para aceitar todos os 11 campos
- [x] **Backend**: Atualizar Handler para chamar `UpdateDetails()` e `UpdateOwnership()` na entidade criada
- [x] **Backend**: Expandir Validator com regras para os novos campos opcionais
- [x] **Frontend**: Atualizar `serviceCatalog.ts:registerService()` para enviar todos os campos do form
- [x] **Testes**: Atualizar testes unitários do `RegisterServiceAsset`

### BUG-02: Tipo "Framework" no frontend sem correspondência no backend ⚠️ ALTO

**Localização**:
- Frontend: `ServiceCatalogPage.tsx` — `<option value="Framework">Framework / SDK</option>`
- Backend enum: `ServiceType.cs` — **não tem valor `Framework`** (valores 0-19)

**Problema**: Se o utilizador selecionar "Framework / SDK", o valor `"Framework"` será enviado ao backend. Atualmente descartado (BUG-01), mas quando o backend for corrigido, vai causar erro de deserialização.

**Correção**:
- [x] **Backend**: Adicionar `Framework = 20` ao enum `ServiceType`
- [x] **Backend**: Atualizar DB constraint (se existir CHECK constraint na coluna)
- [x] **Migração**: Criar migration para atualizar constraint PostgreSQL
- [x] **Testes**: Adicionar teste de serialização/deserialização para o novo valor

### BUG-03: Campo `domain` não enviado pelo API client ⚠️ ALTO

**Localização**: `serviceCatalog.ts:76`

**Problema**: O tipo TypeScript do `registerService` é `{ name: string; team: string; description?: string }` — **não inclui `domain`**. O backend `Command` exige `Domain` como campo obrigatório → resulta em erro 400.

**Correção**:
- [x] **Frontend**: Adicionar `domain` ao tipo e ao payload de `registerService()`
- [x] **Testes**: Verificar que o payload enviado inclui domain

### BUG-04: SchemaPropertyEditor `$ref` sem validação de existência ⚠️ MÉDIO

**Localização**: `SchemaPropertyEditor.tsx:254-271`

**Problema**: Quando type === `$ref`, o utilizador digita referência livre (ex: `#/components/schemas/Address`) sem validação contra o catálogo de Canonical Entities. Não há autocomplete nem verificação de existência.

**Correção**:
- [x] Adicionar autocomplete dropdown com canonical entities publicadas
- [x] Validar que o `$ref` referencia uma entidade que existe no catálogo
- [x] Mostrar aviso se entidade referenciada está deprecated

### BUG-05: Inconsistência em inicialização de `properties` em responses ⚠️ BAIXO

**Localização**: `VisualRestBuilder.tsx:82` vs `VisualRestBuilder.tsx:69`

**Problema**: `createResponse()` inicializa com `properties: []`, mas response default dentro de `createEndpoint()` não inclui `properties`.

**Correção**:
- [x] Uniformizar inicialização — `properties: []` em ambos os pontos
- [x] Adicionar teste unitário para verificar defaults

---

## 2. Gaps e Desenvolvimento Incompleto

### GAP-01: RegisterServiceAsset empobrecido

**Estado**: Backend só aceita 3 campos (Name, Domain, TeamName).  
**Impacto**: Utilizador obrigado a fazer 2+ chamadas (create + update) para registar serviço completo.

**Ação**:
- [x] Expandir Command para 11 campos (mesma correção do BUG-01)
- [x] Considerar endpoint único de "create with full enrichment"

### GAP-02: Sem correlação direta Request/Response ↔ Canonical Entities

**Estado**: Existe `CanonicalUsageReference` como ValueObject e `$ref` no SchemaPropertyEditor, mas:
- Sem autocomplete de canonical entities
- Sem validação de existência do schema referenciado
- Sem propagação de impacto quando canonical entity muda
- Sem visualização das relações no grafo

**Ação**: Ver [Seção 6](#6-correlação-canónica--requestresponse--canonical-entities)

### GAP-03: Request/Response sem tipagem rica avançada

**Estado**: SchemaPropertyEditor suporta 7 tipos e constraints OpenAPI 3.x. **Falta**:
- `oneOf`, `anyOf`, `allOf` (composição de schemas)
- `discriminator` para polimorfismo
- `additionalProperties`
- Preview de exemplo JSON gerado automaticamente

**Ação**: Ver [F-04 Schema Composition](#f-04-schema-composition-oneof--anyof--allof--discriminator)

### GAP-04: Sem testes para SchemaPropertyEditor

**Estado**: Componente de ~550 linhas sem nenhum teste unitário.

**Ação**:
- [x] Criar `SchemaPropertyEditor.test.tsx` com Vitest + React Testing Library
- [x] Cobrir: create, delete, reorder, type change, nested objects, array items, $ref, constraints
- [x] Mínimo 15 test cases

### GAP-05: Sem suporte a Framework/SDK no domínio backend

**Estado**: Frontend tem opção, enum backend não tem.

**Ação**: Ver [Seção 5](#5-novo-tipo-de-serviço-framework--sdk)

### GAP-06: Sem versionamento de Canonical Entities com diff

**Estado**: `CanonicalEntity` tem campo `Version` e `UpdateSchema()`, mas sem:
- Histórico de versões anteriores
- Diff semântico entre versões
- Notificação a contratos consumidores
- Análise de impacto

**Ação**:
- [x] Criar entidade `CanonicalEntityVersion` com snapshot do schema por versão
- [x] Reutilizar `ContractDiffCalculator` para diff de canonical entities
- [x] Feature: `ListCanonicalEntityVersions`, `DiffCanonicalEntityVersions`
- [x] Feature: `GetCanonicalEntityImpact` — lista contratos afetados

### GAP-07: Criação de contrato sem pre-link ao serviço

**Estado**: `CreateContractPage` tem campo `linkedServiceId` mas não faz pre-seleção inteligente.

**Ação**:
- [x] Se navegação vem de `/catalog/services/{id}`, pre-preencher `linkedServiceId`
- [x] Passar `serviceId` como query param na URL de criação de contrato
- [x] Botão "Add Contract" na page de detalhe do serviço

### GAP-08: Sem ContractType para gRPC/Protobuf

**Estado**: `ContractType` enum tem 10 valores (0-9). `ServiceType` tem `GrpcService = 9`, mas `ContractType` não tem valor para gRPC.

**Ação**:
- [x] Adicionar `Grpc = 10` ao enum `ContractType`
- [x] Criar `VisualGrpcBuilder` no frontend (ou fase futura)
- [x] Migração DB para atualizar constraint

### GAP-09: Sem geração automática de exemplos JSON

**Estado**: Campos `example` no request/response são manuais.

**Ação**: Ver [F-03 Auto-Generate JSON Example](#f-03-auto-generate-json-example-from-schema)

### GAP-10: Sem mock server a partir do contrato

**Estado**: `PlaygroundSession` existe no domínio Portal mas não está integrado com workspace.

**Ação**: Ver [F-05 Contract Mock Server](#f-05-contract-mock-server-generator)

---

## 3. Melhorias de UX na Criação de Serviços

### 3.1 Formulário Step-by-Step (Wizard)

Transformar o formulário de serviço de form único para wizard multi-etapas:

```
Step 1: Identidade        → name, displayName, domain, systemArea
Step 2: Classificação      → serviceType, criticality, exposureType
Step 3: Ownership          → team, technicalOwner, businessOwner
Step 4: Referências        → documentationUrl, repositoryUrl, tags
Step 5: Confirmação        → resumo visual de todos os campos
```

**Ações**:
- [x] Criar componente `ServiceRegistrationWizard` com stepper visual
- [x] Progress bar / breadcrumbs entre steps
- [x] Validação por step antes de avançar
- [x] Botão "Back" para retornar a step anterior
- [x] Step 5 com card de preview do serviço como ficará no catálogo

### 3.2 Campos Inteligentes

- [x] **Domain**: autocomplete com domínios existentes no catálogo (evitar typos)
- [x] **Team**: autocomplete com equipas existentes
- [x] **ServiceType**: mostrar ícone e descrição curta para cada tipo
- [x] **Criticality**: mostrar indicador visual (cores) para cada nível
- [x] **Tags**: campo de tags com autocomplete
- [x] **DisplayName**: auto-gerar a partir do `name` (ex: `payment-service` → `Payment Service`)

### 3.3 Campos Condicionais por ServiceType

Quando o utilizador selecionar um tipo específico, mostrar campos adicionais relevantes:

| ServiceType | Campos Extras |
|---|---|
| **RestApi** | baseUrl, openApiUrl, authMechanism |
| **GrpcService** | protoFileUrl, port |
| **KafkaProducer/Consumer** | brokerCluster, topics[], consumerGroup |
| **BackgroundService** | schedule, trigger, healthCheckUrl |
| **Gateway** | upstreamServices[], policies |
| **Framework** | language, packageManager, artifactRegistry, sdkVersion |
| **Mainframe types** | lpar, region, systemId |

**Ação**:
- [x] Backend: Criar value object `ServiceTypeMetadata` (JSON column) para metadata específica por tipo
- [x] Frontend: Render condicional de campos por serviceType selecionado
- [x] Domain: Adicionar `TypeMetadata` (string JSON) à entidade `ServiceAsset`

### 3.4 Template de Serviço

- [x] Permitir criar serviços a partir de templates pré-definidos
- [x] Templates por serviceType com campos pré-preenchidos
- [x] Integrar com o módulo `Templates` que já existe no Catalog

### 3.5 Importação Bulk

- [x] Import de serviços via CSV/JSON
- [x] Import de Backstage `catalog-info.yaml`
- [x] Validação em batch antes de persistir

---

## 4. Melhorias de UX na Criação de Contratos REST

### 4.1 Request/Response Body — Tipagem Rica

Melhorar o `SchemaPropertyEditor` para suportar cenários enterprise reais:

**Ações**:
- [x] Adicionar tipos compostos: `oneOf`, `anyOf`, `allOf`
- [x] Adicionar `discriminator` para polimorfismo (campo + mapping)
- [x] Adicionar `additionalProperties` (boolean ou schema)
- [x] Melhorar visual de constraints com labels descritivas:
  - `minLength` → "Min. characters"
  - `maxLength` → "Max. characters"
  - `minimum` → "Min. value"
  - `maximum` → "Max. value"
  - `pattern` → "Regex pattern" com preview de match
- [x] Adicionar `uniqueItems` para arrays
- [x] Adicionar `minItems` / `maxItems` para arrays
- [x] Melhorar UX do tipo `$ref` com picker de canonical entities (ver Seção 6)

### 4.2 Auto-suggest de operationId

- [x] Gerar automaticamente `operationId` a partir de `method + path`
- [x] Ex: `POST /users` → `createUser`, `GET /users/{id}` → `getUserById`
- [x] Permitir override manual
- [x] Regras de naming configuráveis (camelCase, snake_case)

### 4.3 Preview JSON em Tempo Real

- [x] Painel lateral/inferior com preview do JSON Schema gerado
- [x] Atualiza automaticamente enquanto o utilizador edita propriedades
- [x] Toggle entre "Schema" e "Example" preview
- [x] Botão "Copy to Clipboard"

### 4.4 Validação Cruzada

- [x] Validar que path params declarados em `path` existem como `parameters` com `in: 'path'`
- [x] Validar que `requestBody` só existe em métodos que o suportam (POST, PUT, PATCH)
- [x] Validar que `GET`/`DELETE` não têm requestBody (warning)
- [x] Validar que responses incluem pelo menos um status code de sucesso (2xx)
- [x] Validar consistência de `$ref` — todas as referências resolvem para entidades existentes

### 4.5 Drag & Drop de Propriedades

- [x] Substituir botões Up/Down por drag & drop real (dnd-kit ou similar)
- [x] Visual indicator durante drag
- [x] Suporte a drag entre níveis (object → root)

### 4.6 Quick Add de Response Codes

- [x] Botão "Add common responses" que adiciona automaticamente: 400 Bad Request, 401 Unauthorized, 403 Forbidden, 404 Not Found, 500 Internal Server Error
- [x] Response templates com schema pré-definido (RFC 7807 Problem Details)

---

## 5. Novo Tipo de Serviço: Framework / SDK

### 5.1 Contexto

Grandes empresas mantêm frameworks internos (SDKs de autenticação, logging, messaging, domain libraries, design systems). Estes frameworks:
- Têm versões publicadas em registries (NuGet, npm, Maven, PyPI)
- Têm API surfaces (classes, interfaces, métodos públicos)
- São consumidos por múltiplos serviços
- Precisam de governança de versão e deprecação
- Têm impacto em blast radius quando mudam

### 5.2 Implementação Backend

#### Enum
- [x] Adicionar `Framework = 20` a `ServiceType.cs`

```csharp
/// <summary>Framework / SDK interno — biblioteca ou framework partilhado com desenvolvimento próprio.</summary>
Framework = 20
```

#### Nova Entidade de Detalhe
- [x] Criar `FrameworkAssetDetail` no bounded context `Graph`

```csharp
public sealed class FrameworkAssetDetail : Entity<FrameworkAssetDetailId>
{
    public ServiceAssetId ServiceAssetId { get; private set; }
    
    // ── Identidade do Framework ──
    public string PackageName { get; private set; }     // ex: "NexTrace.Auth.SDK"
    public string Language { get; private set; }         // ex: "C#", "TypeScript", "Java"
    public string PackageManager { get; private set; }   // ex: "NuGet", "npm", "Maven"
    public string ArtifactRegistryUrl { get; private set; } // ex: "https://nuget.company.com"
    
    // ── Versão e Compatibilidade ──
    public string LatestVersion { get; private set; }     // ex: "3.2.1"
    public string MinSupportedVersion { get; private set; } // ex: "2.0.0"
    public string TargetPlatform { get; private set; }     // ex: ".NET 10", "Node 22"
    
    // ── Metadata ──
    public string LicenseType { get; private set; }       // ex: "Internal", "MIT"
    public string BuildPipelineUrl { get; private set; }
    public string ChangelogUrl { get; private set; }
    
    // ── Relações ──
    public int KnownConsumerCount { get; private set; }   // serviços que usam este framework
}
```

#### Features
- [x] `RegisterFrameworkDetail` — registar detalhe de framework associado a um ServiceAsset
- [x] `UpdateFrameworkDetail` — atualizar metadata
- [x] `GetFrameworkDetail` — obter detalhe completo
- [x] `ListFrameworkConsumers` — listar serviços que consomem o framework
- [x] `PublishFrameworkVersion` — registar nova versão publicada

#### API Endpoints
- [x] `POST /api/v1/catalog/services/{serviceId}/framework` — registar detalhe
- [x] `PUT /api/v1/catalog/services/{serviceId}/framework` — atualizar
- [x] `GET /api/v1/catalog/services/{serviceId}/framework` — obter detalhe
- [x] `GET /api/v1/catalog/services/{serviceId}/framework/consumers` — listar consumidores
- [x] `POST /api/v1/catalog/services/{serviceId}/framework/versions` — publicar versão

### 5.3 Implementação Frontend

- [x] Adicionar secção condicional no formulário de serviço quando `serviceType === 'Framework'`
- [x] Campos específicos: packageName, language, packageManager, artifactRegistryUrl, targetPlatform
- [x] Na página de detalhe do serviço: tab "Framework Details" com versões, consumidores, compatibilidade
- [x] Badge visual "Framework" distinto no catálogo de serviços

### 5.4 Migração

- [x] Migration para nova tabela `catalog_graph.framework_asset_details`
- [x] Migration para atualizar DB constraint de `service_type` (incluir valor 20)

### 5.5 Testes

- [x] Testes unitários para `FrameworkAssetDetail` entity
- [x] Testes para cada feature (Register, Update, Get, ListConsumers, PublishVersion)
- [x] Testes de validação
- [x] E2E test para fluxo completo de criação de Framework service

---

## 6. Correlação Canónica — Request/Response ↔ Canonical Entities

### 6.1 Estado Atual

- `CanonicalEntity` existe no domínio de Contracts com: Name, Domain, Category, SchemaContent, Version
- `CanonicalUsageReference` ValueObject existe para tracking
- `SchemaPropertyEditor` suporta type `$ref` com input de texto livre
- **GAP**: Sem integração real entre o editor e o catálogo de canonical entities

### 6.2 Implementação do Canonical Entity Explorer

#### Frontend: Componente `CanonicalEntityPicker`

- [x] Criar componente modal/drawer para browsing de canonical entities
- [x] Filtros: domain, category (entity, dto, event-payload, enum), state (Published)
- [x] Preview do schema da entidade selecionada
- [x] Botão "Use" que insere o `$ref` correto no SchemaPropertyEditor
- [x] Integrar com `SchemaPropertyEditor` — quando type === `$ref`, mostrar botão "Browse Entities"

#### Frontend: Integração no VisualRestBuilder

- [x] No campo de request body, ao selecionar modo "Visual Properties":
  - Botão "Import from Canonical Entity" que importa propriedades da entidade
  - Resolve o schema da entity e popula as propriedades no editor
- [x] No campo de response body, mesma funcionalidade
- [x] Indicador visual (badge) quando uma propriedade usa `$ref` para canonical entity

#### Backend: Features de Suporte

- [x] `SearchCanonicalEntities` — pesquisa por nome, domain, category com paginação
- [x] `GetCanonicalEntitySchema` — retorna schema resolvido de uma entidade
- [x] `LinkContractToCanonicalEntity` — regista relação de uso (via `CanonicalUsageReference`)
- [x] `GetCanonicalEntityUsages` — lista contratos que usam determinada entidade
- [x] `ValidateCanonicalConformance` — valida se um schema é conforme à canonical entity

#### Propagação de Impacto

- [x] Quando canonical entity é atualizada:
  - Listar todos os contratos via `CanonicalUsageReference`
  - Calcular diff semântico por contrato
  - Gerar alerta/notificação para owners
  - Marcar contratos como "possibly impacted" na UI

---

## 7. Novas Funcionalidades (15 Features)

### F-01: Framework / SDK Service Catalog ⭐ PRIORIDADE ALTA
> Ver [Seção 5](#5-novo-tipo-de-serviço-framework--sdk)

**Pilar**: Service Governance  
**Impacto**: Permite catalogar frameworks internos com metadata específica (language, package manager, versions, consumers)

---

### F-02: Canonical Entity Explorer com Auto-Link ⭐ PRIORIDADE ALTA
> Ver [Seção 6](#6-correlação-canónica--requestresponse--canonical-entities)

**Pilar**: Contract Governance, Source of Truth  
**Impacto**: Correlação real entre contratos e entidades canónicas; autocomplete; validação de existência

---

### F-03: Auto-Generate JSON Example from Schema ⭐ PRIORIDADE ALTA

**Pilar**: Contract Governance, Developer Acceleration  
**Descrição**: Geração automática de exemplos JSON a partir das propriedades definidas no SchemaPropertyEditor:
- Usa constraints para gerar valores realistas (`format: email` → `"user@example.com"`)
- Preview side-by-side com o editor de propriedades
- Botão "Copy Example" para clipboard

**Implementação**:
- [x] Criar função `generateExampleFromSchema(properties: SchemaProperty[]): object`
- [x] Mapeamento de format → valor de exemplo:
  - `email` → `"user@example.com"`
  - `uuid` → GUID real
  - `date-time` → ISO timestamp atual
  - `uri` → `"https://example.com/resource"`
  - `int32` → número dentro de min/max
  - `password` → `"********"`
- [x] Para `enum` → primeiro valor do enum
- [x] Para `object` → recursivo
- [x] Para `array` → array com 1-2 itens de exemplo
- [x] Painel de preview no VisualRestBuilder (toggle Schema/Example)
- [x] Testes unitários para a função de geração

---

### F-04: Schema Composition (oneOf / anyOf / allOf / discriminator) ⭐ PRIORIDADE ALTA

**Pilar**: Contract Governance  
**Descrição**: Suporte visual para composição de schemas OpenAPI 3.x:
- `allOf` — herança/merge de schemas
- `oneOf` — variantes exclusivas (ex: Payment = CreditCard | BankTransfer)
- `anyOf` — variantes combinadas
- `discriminator` — campo que identifica tipo concreto

**Implementação**:
- [x] Expandir `SchemaProperty.type` com: `'oneOf' | 'anyOf' | 'allOf'`
- [x] Novo componente `SchemaCompositionEditor`:
  - Tabs para cada variante
  - "Add variant" para adicionar schema alternativo
  - Configuração de discriminator (propertyName + mapping)
- [x] Atualizar `builderSync.ts` para serializar composição em OpenAPI YAML
- [x] Atualizar `builderValidation.ts` com regras de composição
- [x] Testes unitários e E2E

---

### F-05: Contract Mock Server Generator

**Pilar**: Developer Acceleration, Change Confidence  
**Descrição**: A partir de um contrato REST publicado, gerar mock server com:
- Respostas baseadas nos exemplos e schemas definidos
- Suporte a múltiplos status codes via header
- Randomização dentro dos constraints
- URL efémera com TTL configurável

**Implementação**:
- [x] Backend: Feature `GenerateMockConfiguration` que produz config de mock
- [x] Backend: Integrar com `PlaygroundSession` existente no Portal
- [x] Frontend: Botão "Generate Mock" no workspace do contrato
- [x] Frontend: Exibir URL do mock e instruções de uso
- [x] Opcional: runtime de mock lightweight (ou geração de Prism/WireMock config)

---

### F-06: Contract Diff Impact Analysis com Canonical Propagation

**Pilar**: Change Intelligence, Contract Governance  
**Descrição**: Quando canonical entity muda, propagar impacto:
- Identificar contratos via `CanonicalUsageReference`
- Calcular diff semântico por contrato
- Gerar relatório de breaking changes potenciais
- Notificar owners

**Implementação**:
- [x] Feature: `PropagateCanonicalEntityChange(entityId, newVersion)`
- [x] Reutilizar `ContractDiffCalculator` para comparação
- [x] Event: `CanonicalEntityUpdated` → handler que dispara análise
- [x] UI: Banner no workspace de contrato "Canonical entity X was updated — review impact"
- [x] Testes

---

### F-07: API Design Guidelines & Linting Rules Engine

**Pilar**: Contract Governance, Operational Consistency  
**Descrição**: Motor de regras configuráveis para linting de contratos:
- Naming conventions (camelCase/snake_case)
- Versionamento de URI
- Formato de paginação
- Error response format (RFC 7807)
- Segurança (endpoints com auth)
- Regras customizáveis por organização/equipa

**Implementação**:
- [x] Backend: Entidade `DesignGuideline` com regras parametrizáveis
- [x] Backend: Feature `EvaluateDesignGuidelines(contractId)` retorna violações
- [x] Frontend: Tab "Design Guidelines" no workspace do contrato
- [x] Frontend: Painel de configuração de regras por organização
- [x] Integrar com Spectral rules já existentes no módulo de validação
- [x] Score de conformidade por contrato

---

### F-08: Consumer-Driven Contract Testing (CDCT)

**Pilar**: Change Confidence, Contract Governance  
**Descrição**: Workflow de contract testing inspirado em Pact:
- Consumidores registam expectations (subset do contrato)
- Provider verifica expectations
- Breaking change detection baseada em uso real
- Dashboard de compatibilidade

**Implementação**:
- [x] Backend: Entidade `ConsumerExpectation` (consumer, contract, expectedSubset)
- [x] Backend: Feature `RegisterConsumerExpectation`
- [x] Backend: Feature `VerifyProviderCompatibility`
- [x] Backend: Feature `GetCompatibilityDashboard`
- [x] Frontend: Tab "Consumer Contracts" no workspace
- [x] API endpoint para CI/CD webhook de verificação
- [x] Testes

---

### F-09: Service Dependency Auto-Map from Contract Analysis

**Pilar**: Service Governance, Source of Truth  
**Descrição**: Analisar contratos para inferir dependências:
- Se Service A referencia canonical entities de Service B → edge no grafo
- Se contrato de evento tem producer/consumer → edges automáticos
- Comparar com dependências declaradas manualmente

**Implementação**:
- [x] Feature: `InferDependenciesFromContracts(serviceId)`
- [x] Análise de `$ref` e `CanonicalUsageReference` para detectar relações
- [x] Comparar com edges existentes no grafo → alertar discrepâncias
- [x] Job agendado (Quartz) para análise periódica
- [x] UI: Badge "Auto-discovered" em edges do grafo

---

### F-10: Contract Playground / Interactive Tester

**Pilar**: Developer Acceleration  
**Descrição**: Interface interativa integrada no workspace:
- Enviar requests contra endpoint documentado
- Autocomplete de parâmetros baseado nos schemas
- Validar response contra schema definido
- Histórico de chamadas de teste
- Suporte a OAuth2, API Key, Bearer auth

**Implementação**:
- [x] Frontend: Componente `ContractPlayground` integrado no workspace
- [x] Usar `PlaygroundSession` existente para persistir histórico
- [x] Proxy backend para enviar requests (evitar CORS)
- [x] Validação de response contra schema do contrato
- [x] Testes E2E

---

### F-11: Contract Changelog com Semantic Annotations

**Pilar**: Source of Truth, Operational Knowledge  
**Descrição**: Changelog automático e enriquecido para contratos:
- Cada mudança gera entry: endpoint adicionado/removido, campo alterado, constraint modificada
- Anotações semânticas: "Breaking Change", "New Feature", "Deprecation"
- Timeline visual com filtragem por severidade
- Diff visual lado-a-lado
- Export Markdown/HTML

**Implementação**:
- [x] Backend: Enriquecer `ContractDiff` com categorização semântica
- [x] Backend: Feature `GenerateSemanticChangelog(contractId, fromVersion, toVersion)`
- [x] Frontend: Melhorar `ChangelogSection.tsx` com timeline visual e filtros
- [x] Export para Markdown
- [x] Testes

---

### F-12: Multi-Format Contract Export

**Pilar**: Developer Acceleration  
**Descrição**: Exportar contratos para múltiplos formatos:
- OpenAPI 3.0/3.1 YAML/JSON (já parcial via builderSync)
- Postman Collection v2.1
- Insomnia workspace
- cURL commands por endpoint
- SDK scaffolding (C#, TypeScript, Java, Python)
- Bruno collection

**Implementação**:
- [x] Backend: Service `ContractExporter` com strategy pattern por formato
- [x] Backend: Exporters: PostmanExporter, InsomniaExporter, CurlExporter, SdkScaffoldExporter
- [x] Frontend: Dropdown "Export As..." no workspace com opções
- [x] Download como ficheiro ou copy to clipboard (cURL)
- [x] Testes unitários por exporter

---

### F-13: Contract Health Score Dashboard

**Pilar**: Contract Governance, Operational Intelligence  
**Descrição**: Dashboard executivo de saúde dos contratos:
- % com exemplos completos
- % com campos documentados
- % com canonical entities linkadas
- % com CDCT tests
- % deprecated com consumidores activos
- Top-10 com mais violações
- Trend ao longo do tempo

**Implementação**:
- [x] Backend: Feature `ComputeContractHealthDashboard(filters)` com métricas agregadas
- [x] Frontend: Dashboard visual com gráficos (ECharts)
- [x] Filtros: domain, team, contractType
- [x] Trend chart: evolução da qualidade ao longo do tempo
- [x] Testes

---

### F-14: AI-Powered Schema Suggestion from Canonical Entities

**Pilar**: AI-assisted Operations, Contract Governance  
**Descrição**: Ao criar novo endpoint REST, IA sugere:
- Canonical entities relevantes baseadas no path e método
- Request body baseado em entities do domínio
- Response body com wrapping padrão da organização (pagination, error format)
- Parâmetros comuns (paginação, filtros)

**Implementação**:
- [x] Backend: Feature `SuggestSchemaFromContext(method, path, domain)` usando AI module
- [x] Contexto: lista de canonical entities publicadas no domínio
- [x] Frontend: Botão "✨ AI Suggest" no editor de request/response
- [x] Preview da sugestão antes de aplicar
- [x] Respeitar políticas de IA (modelo, governance, auditoria)
- [x] Testes

---

### F-15: Contract Deprecation Workflow com Consumer Notification

**Pilar**: Service Governance, Change Confidence  
**Descrição**: Workflow governado para deprecar/aposentar contratos:
- Identificar consumidores activos
- Notificações automáticas (email, webhook, in-app)
- Sunset date com countdown visível
- Bloquear novas subscrições a contratos deprecated
- Migration guide: link para versão substituta
- Dashboard de progresso de migração

**Implementação**:
- [x] Backend: Feature `InitiateContractDeprecation(contractId, sunsetDate, replacementId)`
- [x] Backend: Feature `GetDeprecationProgress(contractId)` → % consumidores migrados
- [x] Backend: Notification handler (in-app + webhook)
- [x] Frontend: Deprecation wizard com steps
- [x] Frontend: Dashboard de migração com progress bars
- [x] Testes

---

## 8. Cronograma de Fases

### Fase 1: Correção de Bugs (Sprint 1) 🔴 URGENTE ✅ COMPLETE

| Item | Esforço | Status |
|---|---|---|
| BUG-01: Backend aceitar todos os campos do form | M | ✅ |
| BUG-02: Adicionar `Framework = 20` ao enum | S | ✅ |
| BUG-03: Frontend enviar `domain` no payload | S | ✅ |
| BUG-04: Autocomplete de `$ref` no SchemaPropertyEditor | M | ✅ |
| BUG-05: Consistência de inicialização de responses | S | ✅ |

### Fase 2: Framework Service + Canonical Correlation (Sprint 2-3) 🟠 ALTA ✅ COMPLETE

| Item | Esforço | Status |
|---|---|---|
| F-01: Framework/SDK ServiceType + detalhe | L | ✅ |
| F-02: Canonical Entity Explorer + Auto-Link | L | ✅ |
| GAP-04: Testes SchemaPropertyEditor | M | ✅ |
| GAP-06: Versionamento de Canonical Entities | M | ✅ |
| GAP-07: Pre-link serviço na criação de contrato | S | ✅ |
| GAP-08: ContractType.Grpc | S | ✅ |

### Fase 3: UX de Criação de Serviços (Sprint 3-4) 🟡 MÉDIA ✅ COMPLETE

| Item | Esforço | Status |
|---|---|---|
| 3.1: Wizard Step-by-Step | L | ✅ |
| 3.2: Campos inteligentes (autocomplete) | M | ✅ |
| 3.3: Campos condicionais por ServiceType | M | ✅ |
| 3.4: Template de serviço | M | ✅ |
| 3.5: Importação bulk | L | ✅ |

### Fase 4: UX de Contratos REST (Sprint 4-5) 🟡 MÉDIA ✅ COMPLETE

| Item | Esforço | Status |
|---|---|---|
| F-03: Auto-Generate JSON Example | M | ✅ |
| F-04: Schema Composition (oneOf/anyOf/allOf) | L | ✅ |
| 4.2: Auto-suggest operationId | S | ✅ |
| 4.3: Preview JSON em tempo real | M | ✅ |
| 4.4: Validação cruzada | M | ✅ |
| 4.5: Drag & Drop de propriedades | M | ✅ |
| 4.6: Quick Add de response codes | S | ✅ |

### Fase 5: Features Avançadas (Sprint 5-7) 🟢 NORMAL ✅ COMPLETE

| Item | Esforço | Status |
|---|---|---|
| F-05: Contract Mock Server | L | ✅ GenerateMockServer + GenerateMockConfiguration |
| F-06: Canonical Impact Propagation | L | ✅ PropagateCanonicalEntityChange + GetCanonicalEntityImpactCascade |
| F-07: Design Guidelines Engine | L | ✅ EvaluateDesignGuidelines (scoring 0-100) |
| F-08: Consumer-Driven Contract Testing | XL | ✅ RegisterConsumerExpectation + GetContractConsumerExpectations + VerifyProviderCompatibility |
| F-10: Contract Playground | L | ✅ PlaygroundSession domain entity + Portal integration |
| F-12: Multi-Format Export | M | ✅ ExportContract + ExportContractMultiFormat |

### Fase 6: Intelligence & Governance (Sprint 7-9) 🔵 FUTURO ✅ COMPLETE

| Item | Esforço | Status |
|---|---|---|
| F-09: Auto-Map Dependencies from Contracts | L | ✅ InferDependenciesFromContracts |
| F-11: Semantic Changelog | M | ✅ GenerateSemanticChangelog |
| F-13: Contract Health Score Dashboard | L | ✅ ComputeContractHealthDashboard + GetContractHealthTimeline |
| F-14: AI-Powered Schema Suggestion | L | ✅ SuggestSchemaFromContext (rule-based) |
| F-15: Deprecation Workflow + Notification | L | ✅ InitiateContractDeprecation + DeprecateContractVersion + GetDeprecationProgress |

> **Legenda**: S = Small (1-2 dias), M = Medium (3-5 dias), L = Large (1-2 semanas), XL = Extra Large (2-3 semanas)

---

## 9. Definição de Pronto (DoD)

Cada item só está "Pronto" quando:

- [x] **Backend**: Feature implementada com Command/Query + Validator + Handler
- [x] **Backend**: Testes unitários com cobertura mínima de 80%
- [x] **Backend**: Endpoint API registado com autorização adequada
- [x] **Backend**: Migração DB se necessário
- [x] **Frontend**: Componente implementado com i18n completo (4 locales)
- [x] **Frontend**: Responsivo e acessível (WCAG AA mínimo)
- [x] **Frontend**: Testes unitários (Vitest)
- [x] **Frontend**: E2E test para fluxos críticos (Playwright)
- [x] **Segurança**: Sem exposição de dados sensíveis; autorização backend validada
- [x] **Documentação**: XML comments (backend), JSDoc (frontend), ADR se decisão arquitetural
- [x] **i18n**: Todas as strings visíveis traduzíveis (en, pt-BR, pt-PT, es)
- [x] **Revisão**: Code review aprovado
- [x] **Build**: Zero erros de compilação; warnings não incrementados

---

## Anexo A: Ficheiros Principais Impactados

### Backend
```
src/modules/catalog/NexTraceOne.Catalog.Domain/Graph/Enums/ServiceType.cs
src/modules/catalog/NexTraceOne.Catalog.Domain/Graph/Entities/ServiceAsset.cs
src/modules/catalog/NexTraceOne.Catalog.Domain/Graph/Entities/FrameworkAssetDetail.cs (NOVO)
src/modules/catalog/NexTraceOne.Catalog.Domain/Contracts/Enums/ContractType.cs
src/modules/catalog/NexTraceOne.Catalog.Domain/Contracts/Entities/CanonicalEntity.cs
src/modules/catalog/NexTraceOne.Catalog.Domain/Contracts/Entities/CanonicalEntityVersion.cs (NOVO)
src/modules/catalog/NexTraceOne.Catalog.Application/Graph/Features/RegisterServiceAsset/RegisterServiceAsset.cs
src/modules/catalog/NexTraceOne.Catalog.Application/Graph/Features/RegisterFrameworkDetail/ (NOVO)
src/modules/catalog/NexTraceOne.Catalog.Application/Contracts/Features/SearchCanonicalEntities/ (NOVO)
src/modules/catalog/NexTraceOne.Catalog.Application/Contracts/Features/LinkContractToCanonicalEntity/ (NOVO)
src/modules/catalog/NexTraceOne.Catalog.Application/Contracts/Features/ValidateCanonicalConformance/ (NOVO)
src/modules/catalog/NexTraceOne.Catalog.Application/Contracts/Features/PropagateCanonicalEntityChange/ (NOVO)
src/modules/catalog/NexTraceOne.Catalog.API/Graph/Endpoints/Endpoints/ServiceCatalogEndpointModule.cs
```

### Frontend
```
src/frontend/src/features/catalog/api/serviceCatalog.ts
src/frontend/src/features/catalog/pages/ServiceCatalogPage.tsx
src/frontend/src/features/catalog/components/ServiceRegistrationWizard.tsx (NOVO)
src/frontend/src/features/contracts/workspace/builders/shared/SchemaPropertyEditor.tsx
src/frontend/src/features/contracts/workspace/builders/shared/SchemaPropertyEditor.test.tsx (NOVO)
src/frontend/src/features/contracts/workspace/builders/shared/builderTypes.ts
src/frontend/src/features/contracts/workspace/builders/shared/builderValidation.ts
src/frontend/src/features/contracts/workspace/builders/shared/builderSync.ts
src/frontend/src/features/contracts/workspace/builders/shared/CanonicalEntityPicker.tsx (NOVO)
src/frontend/src/features/contracts/workspace/builders/shared/SchemaCompositionEditor.tsx (NOVO)
src/frontend/src/features/contracts/workspace/builders/shared/ExampleGenerator.ts (NOVO)
src/frontend/src/features/contracts/workspace/builders/VisualRestBuilder.tsx
```

### Testes
```
tests/modules/catalog/NexTraceOne.Catalog.Tests/ (backend)
src/frontend/src/features/contracts/workspace/builders/shared/SchemaPropertyEditor.test.tsx (NOVO)
src/frontend/e2e/service-registration.spec.ts (NOVO)
src/frontend/e2e/contract-creation.spec.ts (NOVO)
```

---

## Anexo B: Referências de Mercado

As funcionalidades propostas são inspiradas nas melhores práticas de plataformas APIM e service catalogs:

| Feature | Referência de Mercado |
|---|---|
| Canonical Entities | Backstage (Software Catalog), WSO2 API Manager (Shared Schemas) |
| Schema Composition | SwaggerHub (Visual OpenAPI Editor), Stoplight Studio |
| Mock Server | Postman Mock Server, Stoplight Prism, WireMock |
| CDCT | Pact, Spring Cloud Contract |
| Design Guidelines | Spectral (Stoplight), Azure APIM Policies, Kong Insomnia |
| Multi-Format Export | Postman (Collection Export), Insomnia, Bruno |
| Interactive Playground | Swagger UI "Try it out", Postman, Hoppscotch |
| Health Dashboard | Backstage TechDocs Scorecards, Azure APIM Analytics |
| AI Schema Suggestion | GitHub Copilot, Postman Flows AI |
| Deprecation Workflow | Azure APIM Lifecycle, Google Apigee Deprecation |
| Framework Catalog | Backstage (Tech Radar), Internal Developer Portals |
| Bulk Import | Backstage (catalog-info.yaml), ServiceNow CMDB Import |
| Drag & Drop | Stoplight Studio, APIary |

---

> **Próximo passo**: Priorizar Fase 1 (correção de bugs) e iniciar Fase 2 (Framework + Canonical Correlation) em paralelo.
