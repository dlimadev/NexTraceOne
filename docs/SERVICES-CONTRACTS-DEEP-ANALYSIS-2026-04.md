> **⚠️ ARCHIVED — April 2026**: Este documento foi supersedido por `docs/SERVICES-CONTRACTS-ACTION-PLAN.md` (plano completo) e `docs/CONSOLIDATED-GAP-ANALYSIS-AND-ACTION-PLAN.md` (estado global). Mantido como referência histórica.

# Análise Detalhada: Módulo de Serviços e Contratos
## NexTraceOne — Abril 2026

> **Modo de Operação**: Analysis + Implementation Planning
> **Escopo**: Módulos Catalog (Graph, Contracts, Portal, LegacyAssets, Templates) — todas as camadas
> **Data**: 2026-04-06

---

## Índice

1. [Sumário Executivo](#1-sumário-executivo)
2. [Pontos Positivos](#2-pontos-positivos)
3. [Backend — Camada de Domínio](#3-backend--camada-de-domínio)
4. [Backend — Camada de Aplicação](#4-backend--camada-de-aplicação)
5. [Backend — Camada de Infraestrutura](#5-backend--camada-de-infraestrutura)
6. [Backend — Camada de API](#6-backend--camada-de-api)
7. [Base de Dados e Migrações](#7-base-de-dados-e-migrações)
8. [Frontend — Componentes e Layout](#8-frontend--componentes-e-layout)
9. [Frontend — Estado, Navegação e i18n](#9-frontend--estado-navegação-e-i18n)
10. [Testes Unitários — Backend](#10-testes-unitários--backend)
11. [Testes Unitários — Frontend](#11-testes-unitários--frontend)
12. [Testes E2E (Playwright)](#12-testes-e2e-playwright)
13. [Documentação](#13-documentação)
14. [Gaps Transversais e Riscos](#14-gaps-transversais-e-riscos)
15. [Sugestões de Inovação](#15-sugestões-de-inovação)
16. [Plano de Ação](#16-plano-de-ação)

---

## 1. Sumário Executivo

### Volumetria Geral

| Camada | Métrica | Valor |
|--------|---------|-------|
| **Application** | Features (Contract + Graph) | 115 features (67 + 48) |
| **Domain** | Entidades + Value Objects + Enums | 35 entidades, 22 VOs, 24 enums |
| **Infrastructure** | Repositórios | 28 repositórios (13 + 15) |
| **Infrastructure** | DbContexts Catalog | 7 DbContexts independentes |
| **Infrastructure** | Migrações Catalog | 22 migrações |
| **Infrastructure** | Índices na BD | 694 definições de índices |
| **API** | Endpoints de Contratos | 30+ endpoints |
| **API** | Endpoints de Serviços (Graph) | 45+ endpoints |
| **Testes Backend** | Contracts + Graph | 882 + 224 = 1.106 testes |
| **Testes E2E** | Playwright (services + contracts) | 34 testes dedicados |
| **Frontend** | Arquivos de features | 131 ficheiros (~14.000 LOC) |
| **i18n** | Chaves no locale inglês | 6.038 chaves |
| **Docs XML** | Entidades + Handlers documentados | 2.836+ itens |

### Classificação Global por Camada

| Camada | Avaliação | Justificativa |
|--------|-----------|---------------|
| **Domínio** | 🟢 Excelente | Modelo rico, VOs, tipagem forte, enums completos |
| **Aplicação** | 🟢 Excelente | VSA 100%, Result<T>, CancellationToken, validators |
| **Infraestrutura** | 🟡 Boa com gaps | Sem AsNoTracking() explícito nos reads, sem lazy-load |
| **API** | 🟢 Boa | Contratos estáveis, autorização granular, OpenAPI |
| **Base de Dados** | 🟢 Excelente | Audit fields, soft-delete, RLS, índices adequados |
| **Frontend** | 🟡 Boa com gaps | i18n incompleto (47–57 chaves em falta), acessibilidade |
| **Testes Backend** | 🟡 Bom com gaps | 1.106 testes, mas poucos parametrizados e infra mínima |
| **Testes Frontend** | 🔴 Insuficiente | ContractHealthDashboardPage sem testes; builders sem testes |
| **Testes E2E** | 🟡 Parcial | Fluxos básicos cobertos; versioning, approval, health sem cobertura |
| **Documentação** | 🟢 Excelente | 5+ ficheiros de docs, ADRs, 2.836+ comentários XML |

---

## 2. Pontos Positivos

### 2.1 Arquitetura e Domínio
- ✅ **VSA 100%**: Todos os 115 features seguem rigorosamente o padrão `static class → Command/Query + Validator + Handler + Response` num único ficheiro.
- ✅ **Result<T> Padrão**: Todos os handlers retornam `Task<Result<Response>>` — zero exceptions de negócio não tratadas.
- ✅ **IDs Fortemente Tipados**: 20+ tipos de IDs (`ContractVersionId`, `ServiceAssetId`, etc.) eliminando confusão de primitivos.
- ✅ **DateTime Abstração**: `IDateTimeProvider` usado em 100% dos casos — zero `DateTime.Now` no código.
- ✅ **CancellationToken Consistente**: 72+ usos confirmados em handlers assíncronos.
- ✅ **FluentValidation Coverage**: 111/115 features com validadores explícitos (96.5%).
- ✅ **Domain Events / Outbox**: Padrão Outbox implementado via `NexTraceDbContextBase`.
- ✅ **Bounded Context Resolvido**: Documentação explicita que outros módulos NUNCA referenciam `ContractsDbContext` diretamente.

### 2.2 Base de Dados
- ✅ **Audit Fields Automáticos**: `CreatedAt/By`, `UpdatedAt/By` populados via `AuditInterceptor` em todas as entidades.
- ✅ **Soft-Delete Global**: Filtro global de query aplicado automaticamente em 99+ entidades.
- ✅ **Row-Level Security**: RLS aplicado via tenant/user injection no `NexTraceDbContextBase`.
- ✅ **694 Índices**: Cobertura extensiva em campos frequentemente consultados (name, protocol, status, team, domain).
- ✅ **Migrações Organizadas**: 22 migrações com nomenclatura descritiva (ex: `Phase01_AddContractSlaAndWebhookType`).

### 2.3 Frontend
- ✅ **Sem dangerouslySetInnerHTML**: Zero ocorrências em todo o módulo.
- ✅ **Sem GUIDs expostos ao utilizador**: Nenhum campo técnico desnecessário nos formulários de negócio.
- ✅ **TanStack Query**: Padrão correto de server state em uso (loading, error states na maioria das páginas).
- ✅ **4 Locales Presentes**: en, pt-BR, pt-PT, es com 6.038 chaves no inglês.
- ✅ **TypeScript Rigoroso**: Mínimo uso de `any` (apenas 2 instâncias identificadas).
- ✅ **Roteamento Completo**: 28+ rotas de catálogo, 10+ rotas de contratos bem organizadas.
- ✅ **Empty States**: Implementados na maioria das páginas de listagem.

### 2.4 Testes
- ✅ **Nomenclatura de Testes**: Padrão `Method_Should_X_When_Y` seguido em 139+ testes backend.
- ✅ **Cobertura de Erros**: 107+ asserções `IsFailure` nos testes — testes cobrem caminhos de falha.
- ✅ **Sem Testes Skipped**: Zero testes marcados com `Skip` em backend ou E2E.
- ✅ **E2E Funcionais**: 34 testes E2E dedicados a serviços (11) e contratos (12 + 11 wizard).

### 2.5 Documentação
- ✅ **2.836+ comentários XML**: Documentação inline em português (conforme convenção do projeto).
- ✅ **5 ADRs**: Decisões arquiteturais documentadas e aceitadas.
- ✅ **Docs Detalhadas**: `SERVICE-CONTRACT-GOVERNANCE.md`, `CONTRACT-STUDIO-VISION.md`, `SERVICES-CONTRACTS-ACTION-PLAN.md` existentes.
- ✅ **OpenAPI Configurado**: Todos os endpoints com `.WithName()` e Scalar UI ativo.

---

## 3. Backend — Camada de Domínio

### 3.1 Pontos Positivos
- **Modelo de Domínio Rico**: 22 entidades de contratos + 13 de serviços, cada uma com comportamento próprio (métodos de negócio).
- **Value Objects Representativos**: `ContractSignature`, `ContractSla`, `SemanticVersion`, `CompatibilityAssessment`, `ContractProvenance` — regras de negócio encapsuladas.
- **Suporte Multi-Protocolo**: OpenAPI, Swagger, WSDL, AsyncAPI, Background Service — modelados corretamente no domínio.
- **Enum Lifecycle State**: `ContractLifecycleState` com transições claras (Draft → InReview → Approved → Locked → Deprecated → Sunset → Retired).

### 3.2 Gaps e Problemas

| # | Severidade | Descrição | Ficheiro |
|---|-----------|-----------|---------|
| D1 | 🔴 Alta | **GraphQL não implementado** — suportado no enum `ContractProtocol` e mencionado na visão, mas sem entidade `GraphQLContractDetail`, parser, diff calculator ou validador. | `CONTRACT-STUDIO-VISION.md` vs entidades |
| D2 | 🔴 Alta | **Protobuf não implementado** — idem ao GraphQL; presente no enum mas sem implementação. | `CONTRACT-STUDIO-VISION.md` vs entidades |
| D3 | 🟡 Média | **Framework/SDK ausente do enum `ServiceType`** (BUG-01 do ACTION-PLAN) — frontend envia este valor, backend descarta silenciosamente. | `src/modules/catalog/NexTraceOne.Catalog.Domain/Graph/` |
| D4 | 🟡 Média | **ContractVersionTests.cs tem apenas 4 testes** para a entidade raiz mais complexa do domínio. | `tests/…/Contracts/Domain/Entities/ContractVersionTests.cs` |
| D5 | 🟡 Média | **Sem domain event explícito** para mudanças de estado do ciclo de vida do contrato (ex: `ContractPublishedDomainEvent`, `ContractDeprecatedDomainEvent`). O Outbox existe mas os eventos de domínio específicos não estão mapeados. | Domain layer |

### 3.3 Sugestões
- Implementar `GraphQLContractDetail` e `ProtobufContractDetail` seguindo o padrão já existente para SOAP/Event/Background.
- Adicionar `Framework` e `Library` ao enum `ServiceType` e atualizar constraint da BD.
- Definir domain events explícitos para transições de lifecycle, integráveis via Outbox para notificação de módulos consumidores (OI, Changes, AI).

---

## 4. Backend — Camada de Aplicação

### 4.1 Pontos Positivos
- **VSA rigoroso**: 115/115 features seguem o padrão.
- **Validators presentes**: 111/115 com FluentValidation.
- **Handlers concisos**: Lógica de orquestração delegada ao domínio.

### 4.2 Gaps e Problemas

| # | Severidade | Descrição | Ficheiro |
|---|-----------|-----------|---------|
| A1 | 🔴 Alta | **TODOs em `GenerateServerFromContract.cs`** — 6 comentários `// TODO:` indicam geração de stubs incompleta. Endpoint existe na API mas retorna código placeholder. | `src/modules/catalog/…/Portal/Features/GenerateServerFromContract/` |
| A2 | 🔴 Alta | **`ContractGovernanceApplicationTests.cs` com apenas 5 testes** para um módulo de governança crítico — sem cobertura de cenários de rejeição, re-submissão, escalada. | `tests/…/Application/ContractGovernanceApplicationTests.cs` |
| A3 | 🟡 Média | **Sem MaximumLength em alguns campos de string** — campos como `SpecContent` e `Author` nas validações têm apenas `NotEmpty()` sem limite superior, podendo aceitar payloads de vários MB. | Validators de CreateDraft, UpdateDraftContent |
| A4 | 🟡 Média | **`GetTemporalDiff` com 3 testes apenas** — feature complexa de comparação temporal do grafo com cobertura insuficiente. | `tests/…/GetTemporalDiffTests.cs` |
| A5 | 🟡 Média | **`GetImpactPropagation` com 4 testes** — feature de análise de impacto crítica para change confidence, pouco testada. | `tests/…/GetImpactPropagationTests.cs` |
| A6 | 🟡 Média | **`DecommissionAsset` com 3 testes** — fluxo irreversível com cobertura mínima. | `tests/…/DecommissionAssetTests.cs` |
| A7 | 🟡 Média | **Ausência de testes parametrizados (Theory)** — apenas 2 testes com `[Theory]` em todo o módulo; cenários multi-protocolo (OpenAPI vs WSDL vs AsyncAPI) não explorados sistematicamente. | Toda suite de testes |
| A8 | 🟢 Baixa | **2 features sem validator** — leituras simples que provavelmente não necessitam, mas deve ser documentado explicitamente. | Features de query |

### 4.3 Sugestões
- Concluir `GenerateServerFromContract.cs` resolvendo os 6 TODOs para tornar a feature production-ready.
- Expandir `ContractGovernanceApplicationTests.cs` com cenários: rejeição com comentários, re-submissão após rejeição, aprovação parcial, timeout de revisão.
- Adicionar `MaximumLength(5_242_880)` (5MB) ao `SpecContent` como limite razoável para specs OpenAPI/WSDL.
- Converter testes de domain services para `[Theory]` com InlineData para múltiplos protocolos.

---

## 5. Backend — Camada de Infraestrutura

### 5.1 Pontos Positivos
- **28 repositórios** com interfaces bem definidas em `Application/Contracts/Abstractions/`.
- **Soft-delete e audit automáticos** via `NexTraceDbContextBase`.
- **RLS implementado** via interceptors.

### 5.2 Gaps e Problemas

| # | Severidade | Descrição | Ficheiro |
|---|-----------|-----------|---------|
| I1 | 🔴 Alta | **Zero chamadas `.AsNoTracking()`** nas queries de leitura de contratos e serviços — todos os reads rastreiam entidades desnecessariamente, aumentando pressão no Change Tracker do EF. | Todos os repositórios de Contracts + Graph |
| I2 | 🟡 Média | **Navigation properties carregadas in-memory** sem `.Include()` — ex: `GetContractVersionDetail` acede a `apiAsset?.OwnerService` e `apiAsset?.ConsumerRelationships` sem eager loading, criando risco de N+1. | `GetContractVersionDetail.cs` linhas 48-77 |
| I3 | 🟡 Média | **Apenas 1 teste de infraestrutura** por módulo (`ContractsModuleServiceTests.cs` e `CatalogGraphModuleServiceTests.cs`) — ausência de testes de repositório com BD real. | Infrastructure tests |
| I4 | 🟡 Média | **Ausência de índice full-text** para pesquisa por `SpecContent` — `SearchContracts` funciona mas sem índice FTS do PostgreSQL, performance degradará com volume. | Migrations de Contracts |
| I5 | 🟢 Baixa | **Método único `GetByIdAsync`** nos repositórios — sem sobrecarga `GetByIdWithRelationsAsync()` para queries que precisam de navegação. | Repository implementations |

### 5.3 Sugestões
- **Adicionar `.AsNoTracking()` a todos os `IQueryHandler`** — criar método `IReadRepository<T>` com AsNoTracking por padrão.
- **Criar métodos especializados** nos repositórios: `GetDetailAsync()` com Include statements predefinidos para os cenários de detalhe.
- **Adicionar índice GIN para FTS** em `ContractVersion.SpecContent` e `ServiceAsset.Name + Description`.
- **Implementar testes de integração** com TestContainers (PostgreSQL) para repositórios críticos.

---

## 6. Backend — Camada de API

### 6.1 Pontos Positivos
- **Endpoints com nomes OpenAPI**: Todos os 75+ endpoints têm `.WithName()`.
- **Autorização Granular**: `contracts:read`, `contracts:write`, `contracts:import` — segmentado por operação.
- **Scalar UI**: Documentação interativa ativa.
- **Minimal API**: Controladores finos sem lógica de negócio.

### 6.2 Gaps e Problemas

| # | Severidade | Descrição | Ficheiro |
|---|-----------|-----------|---------|
| P1 | 🟡 Média | **Rate limiting aplicado genericamente** ("data-intensive") — sem distinção entre endpoints de leitura simples vs. diff semântico (computacionalmente caro). | `ServiceCatalogEndpointModule.cs` |
| P2 | 🟡 Média | **Ausência de endpoint de saúde contextual** para serviços — ex: `GET /api/v1/catalog/{serviceId}/health` que consolide métricas, incidentes e mudanças recentes. | Graph endpoints |
| P3 | 🟡 Média | **Sem endpoint de relatório comparativo** entre ambientes não produtivos e produção para contratos — ausência esta impacta o pilar de "Production Change Confidence". | Contracts endpoints |
| P4 | 🟢 Baixa | **`/api/v1/catalog` e `/api/v1/contracts` como prefixos separados** — consideração de unificar em `/api/v1/catalog/services` e `/api/v1/catalog/contracts` para consistência semântica. | Routing |

---

## 7. Base de Dados e Migrações

### 7.1 Pontos Positivos
- **22 migrações** bem nomeadas e organizadas por fase.
- **694 índices** cobrindo campos de filtro, ordenação e pesquisa.
- **Soft-delete global** com filtro de query automático.
- **Audit fields automáticos** em todas as entidades.
- **7 DbContexts** no catálogo, todos registados no wave strategy correto.

### 7.2 Gaps e Problemas

| # | Severidade | Descrição | Localização |
|---|-----------|-----------|------------|
| DB1 | 🔴 Alta | **Ausência de índice GIN (Full-Text Search)** para pesquisa de contratos por conteúdo (`SpecContent`, `Title`, `Description`). | Migrations Contracts |
| DB2 | 🟡 Média | **`Framework` não existe como `ServiceType`** na constraint da BD (BUG-01) — a migração `W00_ExtendServiceTypeConstraintForLegacySystems` adicionou legacy types mas não Framework. | `CatalogGraphDbContext` migrations |
| DB3 | 🟡 Média | **Sem índice composto `(TenantId, Status)` em `ContractDraft`** — queries de "all my pending reviews" sem optimização de tenant. | ContractDraft config |
| DB4 | 🟡 Média | **Sem índice em `ContractVersion.CreatedAt` para timeline** — queries de auditoria temporal podem ser lentas em high-volume. | ContractVersion config |
| DB5 | 🟢 Baixa | **Migração de GraphQL/Protobuf não planeada** — quando implementado, exigirá migrações adicionais. | Contracts schema |

### 7.3 Sugestões
- Criar migração `AddFullTextSearchIndexes` com índices GIN em PostgreSQL para `ContractVersion.Title + SpecContent`.
- Criar migração `FixFrameworkServiceType` para adicionar `'Framework'` e `'Library'` ao check constraint de `ServiceType`.
- Adicionar índice composto `(TenantId, Status, CreatedAt DESC)` em `ContractDraft` para dashboard de revisões.

---

## 8. Frontend — Componentes e Layout

### 8.1 Pontos Positivos
- **Zero `dangerouslySetInnerHTML`**: Segurança garantida.
- **Componentes reutilizáveis**: `PageLoadingState`, `PageErrorState`, `EmptyState` usados consistentemente.
- **VisualRestBuilder**: Validação abrangente com 57 regras de negócio.
- **TanStack Query**: Gestão de estado servidor correta na maioria dos componentes.

### 8.2 Gaps e Problemas de Layout/Componentes

| # | Severidade | Descrição | Ficheiro |
|---|-----------|-----------|---------|
| FE1 | 🔴 Alta | **ContractHealthDashboardPage.tsx sem testes** — página de governance crítica completamente sem cobertura. | `src/frontend/src/features/contracts/governance/ContractHealthDashboardPage.tsx` |
| FE2 | 🔴 Alta | **ContractWorkspacePage sem loading/error state** — `detailQuery.isLoading` e `detailQuery.isError` não verificados na renderização inicial. | `src/frontend/src/features/contracts/workspace/ContractWorkspacePage.tsx` L54-62 |
| FE3 | 🟡 Média | **Inconsistência de espaçamento em DeveloperPortalPage.tsx** — mistura `gap-1`, `gap-2`, `gap-3`, `gap-4`; `px-3 py-2` vs `py-2 px-3` (ordem invertida em células de tabela). | `src/frontend/src/features/catalog/pages/DeveloperPortalPage.tsx` L88, L151-152, L170 |
| FE4 | 🟡 Média | **Ausência de aria-labels em botões de ícone** — 0 aria-label attributes em `CreateContractPage.tsx`; 2 em `ServiceCatalogPage.tsx`; botões e links com apenas ícone sem descrição. | Múltiplos componentes |
| FE5 | 🟡 Média | **SchemasSection.tsx sem empty state** — quando não há schemas, não há mensagem de "nenhum schema registado". | `src/frontend/src/features/contracts/workspace/sections/SchemasSection.tsx` |
| FE6 | 🟡 Média | **Builders sem testes** — `VisualRestBuilder.tsx`, `VisualSoapBuilder.tsx`, `VisualEventBuilder.tsx`, `VisualWebhookBuilder.tsx`, `VisualWorkserviceBuilder.tsx` sem coverage. | `src/frontend/src/features/contracts/workspace/builders/` |
| FE7 | 🟡 Média | **Seções de detalhe de contrato sem testes** — `ContractHeader.tsx`, `ComplianceSection.tsx`, `SecuritySection.tsx`, `DependenciesSection.tsx`, `ScorecardSection.tsx`, `ChangelogSection.tsx` sem coverage. | `src/frontend/src/features/contracts/workspace/sections/` |
| FE8 | 🟡 Média | **ServiceRegistrationWizard.tsx sem testes** — wizard multi-step crítico sem cobertura de validação de campos e interdependências. | `src/frontend/src/features/catalog/` |
| FE9 | 🟢 Baixa | **Imports relativos nos builders** — `./shared/BuilderFormPrimitives` em vez de alias de path configurado. | `VisualRestBuilder.tsx`, `VisualSoapBuilder.tsx` |
| FE10 | 🟢 Baixa | **Queries não invalidadas após mutações** em `ContractWorkspacePage` — possível exibição de dados stale após operações de escrita. | `ContractWorkspacePage.tsx` |

---

## 9. Frontend — Estado, Navegação e i18n

### 9.1 Gaps de i18n (CRÍTICO)

| # | Severidade | Descrição | Impacto |
|---|-----------|-----------|---------|
| i1 | 🔴 Alta | **57 chaves de validação do builder não existem nos ficheiros de locale** — utilizadores em locales não-ingleses vêem mensagens de erro em inglês nos construtores visuais. | `builderValidation.ts` L45-215; todas as locales |
| i2 | 🔴 Alta | **47 chaves em falta em pt-BR.json e es.json** — cobertura de tradução de 99.2% mas com gaps em funcionalidades novas. | `src/frontend/src/locales/pt-BR.json`, `es.json` |
| i3 | 🔴 Alta | **50 chaves em falta em pt-PT.json** — maior gap entre os locales. | `src/frontend/src/locales/pt-PT.json` |
| i4 | 🟡 Média | **Options de `<select>` hardcoded** em `ServiceCatalogPage.tsx` L280, L285 (`"API Gateway"`, `"Framework / SDK"`) e `TemplateEditorPage.tsx` L257, L270, L271 (`"gRPC"`, `".NET"`, `"Node.js"`). | ServiceCatalogPage.tsx, TemplateEditorPage.tsx |

**Chaves de validação em falta nos locales** (amostra):
```
contracts.builder.validation.titleRequired
contracts.builder.validation.basePathRequired
contracts.builder.validation.basePathSlash
contracts.builder.validation.pathRequired
contracts.builder.validation.pathSyntaxInvalid
contracts.builder.validation.responseRequired
contracts.builder.validation.duplicateStatusCode
contracts.builder.validation.duplicateOperationId
contracts.builder.validation.pathParamNotDeclared
contracts.builder.validation.pathParamMustBeRequired
contracts.builder.validation.paramNameRequired
contracts.builder.validation.deprecationNoteRequired
contracts.builder.validation.serverUrlInvalid
... (+ 44 chaves adicionais)
```

### 9.2 Navegação e Roteamento
- ✅ Sidebar completa com todos os módulos de serviços e contratos.
- ✅ 28+ rotas de catálogo + 10+ rotas de contratos bem organizadas.
- ⚠️ `ContractHealthDashboardPage` existe na sidebar mas sem testes de navegação E2E.

---

## 10. Testes Unitários — Backend

### 10.1 Distribuição de Testes

| Módulo | Testes | Avaliação |
|--------|--------|-----------|
| Contracts Domain (VOs + Entities) | 130 | 🟢 Bom |
| Contracts Domain Services | 168 | 🟢 Excelente |
| Contracts Application | 158 | 🟢 Bom |
| Contracts Infrastructure | 1 | 🔴 Insuficiente |
| Graph Application | 151 | 🟡 Adequado |
| Graph Domain | ~30 | 🟡 Adequado |
| Graph Infrastructure | 1 | 🔴 Insuficiente |
| **TOTAL** | **~1.106** | 🟡 Bom |

### 10.2 Gaps Críticos de Cobertura

| # | Severidade | Feature | Testes Actuais | Testes Recomendados |
|---|-----------|---------|---------------|-------------------|
| T1 | 🔴 Alta | `ContractGovernanceApplicationTests` | 5 | 20+ (rejeição, re-submissão, escalada, timeout) |
| T2 | 🔴 Alta | `ContractVersionTests` (entidade raiz) | 4 | 15+ (sign, lock, deprecate, lifecycle completo) |
| T3 | 🟡 Média | `GetTemporalDiff` | 3 | 10+ (intervalos, snapshot comparison, delta types) |
| T4 | 🟡 Média | `GetImpactPropagation` | 4 | 12+ (profundidade n, ciclos, thresholds de risco) |
| T5 | 🟡 Média | `DecommissionAsset` | 3 | 8+ (com contratos ativos, com consumers, sem permissão) |
| T6 | 🟡 Média | `GetSubgraph` | 4 | 8+ (depth, filtros de tipo, subgrafo vazio) |
| T7 | 🟡 Média | Testes parametrizados multi-protocolo | 2 | 15+ (via `[Theory]` para OpenAPI/WSDL/AsyncAPI) |

### 10.3 Qualidade dos Testes Existentes
- ✅ Nomenclatura `Method_Should_X_When_Y` seguida rigorosamente.
- ✅ Cobertura de paths de erro (107+ asserções `IsFailure`).
- ✅ NSubstitute usado corretamente para mocks.
- ❌ Ausência de testes de integração com BD real (TestContainers/WebApplicationFactory).
- ❌ Ausência de testes de concorrência (ex: dois aprovadores simultâneos).

---

## 11. Testes Unitários — Frontend

### 11.1 Estado Actual

| Componente | Testes | Estado |
|-----------|--------|--------|
| ServiceCatalogPage.tsx | ✅ ~152 linhas | Adequado |
| ServiceCatalogListPage.tsx | ✅ Presente | Adequado |
| ServiceDetailPage.tsx | ✅ Presente | Adequado |
| ContractDetailPage.tsx | ✅ 211 linhas | Bom |
| ContractListPage.tsx | ✅ 100 linhas | Adequado |
| ContractGovernancePage.tsx | ✅ 79 linhas | Mínimo |
| ContractCatalogPage.tsx | ✅ 94 linhas | Adequado |
| ContractHealthDashboardPage.tsx | ❌ **ZERO** | 🔴 Crítico |
| VisualRestBuilder.tsx | ❌ **ZERO** | 🔴 Crítico |
| VisualSoapBuilder.tsx | ❌ **ZERO** | 🔴 Crítico |
| VisualEventBuilder.tsx | ❌ **ZERO** | 🔴 Crítico |
| ServiceRegistrationWizard.tsx | ❌ **ZERO** | 🔴 Crítico |
| SchemaPropertyEditor.tsx | ⚠️ Mínimo | Baixo |
| ContractHeader.tsx | ❌ **ZERO** | 🟡 Médio |
| ScorecardSection.tsx | ❌ **ZERO** | 🟡 Médio |
| CanonicalEntityPicker.tsx | ❌ **ZERO** | 🟡 Médio |

### 11.2 Sugestões
- Criar `ContractHealthDashboardPage.test.tsx` com 10+ testes: renderização de métricas, lista de violações, estado vazio, estado de erro.
- Criar `VisualRestBuilder.test.tsx` com testes de: adição de endpoint, validação de path, geração de schema, preview de código.
- Criar `ServiceRegistrationWizard.test.tsx` cobrindo wizard multi-step: navegação, validação por step, submissão.

---

## 12. Testes E2E (Playwright)

### 12.1 Cobertura Actual

| Fluxo | Ficheiro | Testes | Estado |
|-------|---------|--------|--------|
| Service Catalog listing | `service-catalog.spec.ts` | 11 | ✅ Coberto |
| Service detail view | `service-catalog.spec.ts` | 3 | ✅ Coberto |
| Contract listing | `contracts.spec.ts` | 12 | ✅ Coberto |
| Contract creation wizard | `contract-wizard-flows.spec.ts` | 11 | ✅ Coberto |
| Contract versioning | — | 0 | 🔴 Não coberto |
| Contract approval workflow | — | 0 | 🔴 Não coberto |
| Contract health dashboard | — | 0 | 🔴 Não coberto |
| Breaking change detection | — | 0 | 🔴 Não coberto |
| Service creation | — | 0 | 🔴 Não coberto |
| Canonical entity management | — | 0 | 🔴 Não coberto |
| Contract deprecation | — | 0 | 🔴 Não coberto |
| Consumer expectations (CDCT) | — | 0 | 🔴 Não coberto |
| Semantic diff viewer | — | 0 | 🔴 Não coberto |
| Contract signing | — | 0 | 🔴 Não coberto |
| Service discovery | — | 0 | 🔴 Não coberto |

### 12.2 Qualidade dos Testes Existentes
- ✅ Mock da API bem estruturado em `e2e/helpers/auth.ts`.
- ✅ Testes em português — alinhado com convenção do projeto.
- ✅ Playwright com `page.locator('#password')` conforme convenção documentada.
- ❌ `e2e-real/real-core-flows.spec.ts` existe mas tem apenas 1 ficheiro básico — testes reais sem backend não existem.
- ❌ Sem testes de formulário de criação de serviço.

### 12.3 Prioridade de Novos Testes E2E

**P1 — Crítico** (adicionar imediatamente):
1. `contract-health-dashboard.spec.ts` — métricas de saúde, violações, navegação
2. `service-creation.spec.ts` — wizard de criação, campos obrigatórios, submissão
3. `contract-versioning.spec.ts` — criar versão, ver histórico, comparar versões

**P2 — Importante** (próxima sprint):
4. `contract-approval-flow.spec.ts` — submeter para revisão, aprovar, rejeitar
5. `contract-deprecation.spec.ts` — iniciar deprecação, timeline, consumers afectados
6. `semantic-diff.spec.ts` — comparar versões, breaking changes highlight

**P3 — Enriquecimento**:
7. `canonical-entities.spec.ts` — pesquisa, detalhes, impacto
8. `service-discovery.spec.ts` — descoberta, matching, confirmação
9. `cdct-consumer-expectations.spec.ts` — CDCT workflow completo

---

## 13. Documentação

### 13.1 Estado Actual
- ✅ `SERVICES-CONTRACTS-ACTION-PLAN.md` — 866 linhas, atualizado, cobre bugs, gaps, 15 features e roadmap por fases.
- ✅ `SERVICE-CONTRACT-GOVERNANCE.md` — visão de governança.
- ✅ `CONTRACT-STUDIO-VISION.md` — visão do estúdio de contratos.
- ✅ `CATALOG-EVOLUTION-ROADMAP.md` — roadmap do catálogo.
- ✅ `CONTRACT-GOVERNANCE-INNOVATIONS.md` — inovações planeadas.
- ✅ ADRs: 5 decisões documentadas (modular monolith, BD, Elasticsearch, AI local, React).
- ✅ Comentários XML em português conforme convenção — 2.836+ itens.
- ✅ OpenAPI/Scalar: documentação interativa da API activa.

### 13.2 Gaps de Documentação

| # | Severidade | Descrição |
|---|-----------|-----------|
| DOC1 | 🟡 Média | **Ausência de runbook operacional** para o módulo de contratos — ex: "Como lidar com um contrato locked que precisa de correção urgente". |
| DOC2 | 🟡 Média | **Sem guia de onboarding para o estúdio de contratos** — documentação de utilizador sobre como criar um primeiro contrato end-to-end. |
| DOC3 | 🟡 Média | **ADRs não cobrem decisões de protocolo** — ausência de ADR para "Por que não GraphQL nativo no MVP1". |
| DOC4 | 🟢 Baixa | **`IMPLEMENTATION-STATUS.md` pode estar desatualizado** — verificar se reflecte o estado actual da implementação. |
| DOC5 | 🟢 Baixa | **Sem changelog automático de features** — o `GenerateSemanticChangelog` existe no backend mas sem integração com documentação do produto. |

---

## 14. Gaps Transversais e Riscos

### 14.1 Riscos de Produto

| # | Risco | Probabilidade | Impacto | Mitigação |
|---|------|--------------|---------|-----------|
| R1 | **GenerateServerFromContract incompleto** em produção pode gerar código quebrado | Alta | Alto | Resolver TODOs ou desactivar o endpoint via feature flag |
| R2 | **N+1 queries em GetContractVersionDetail** podem degradar performance em high-volume | Média | Alto | Adicionar `.AsNoTracking()` + eager loading imediato |
| R3 | **GraphQL/Protobuf ausentes** quando utilizadores importarem contratos nestes formatos | Média | Médio | Devolver erro claro "protocolo não suportado" até implementar |
| R4 | **Framework ServiceType** descartado silenciosamente pode criar registos corrompidos | Alta | Médio | Fix urgente BUG-01: adicionar enum + migração |
| R5 | **Falta de testes E2E para approval workflow** — regressões não detectadas em fluxo crítico | Alta | Alto | Adicionar `contract-approval-flow.spec.ts` |
| R6 | **47–57 chaves de i18n em falta** — utilizadores PT/ES vêem mensagens em inglês | Alta | Médio | Sincronização de locales como tarefa recorrente |

### 14.2 Dívida Técnica Identificada

| # | Área | Dívida | Esforço Estimado |
|---|------|--------|-----------------|
| DT1 | Infraestrutura | Adicionar `.AsNoTracking()` a todos os read handlers | 4h |
| DT2 | Infraestrutura | Criar métodos `GetDetailAsync()` com eager loading | 8h |
| DT3 | Infraestrutura | Índice GIN para FTS em contratos | 2h (migração) |
| DT4 | Domínio | Adicionar `Framework` ao `ServiceType` enum + migração | 3h |
| DT5 | Frontend | Adicionar 57 chaves i18n aos 4 locales | 4h |
| DT6 | Frontend | Corrigir loading/error state em ContractWorkspacePage | 2h |
| DT7 | Frontend | Adicionar aria-labels a botões de ícone | 3h |
| DT8 | Aplicação | Resolver 6 TODOs em GenerateServerFromContract | 12h |
| DT9 | Testes | Criar suite E2E para approval workflow + health dashboard | 8h |
| DT10 | Testes | Criar testes unitários frontend para builders | 12h |

**Total estimado**: ~58 horas de dívida técnica identificada

---

## 15. Sugestões de Inovação

Alinhadas com a visão do NexTraceOne como Source of Truth e plataforma de Change Intelligence:

### 15.1 Contract Dependency Graph Visual
**Pilar**: Contract Governance + Source of Truth
- Visualização interativa das dependências entre contratos (quem consume qual versão de qual contrato).
- Heatmap de "contrato mais crítico" baseado em número de consumers e criticidade dos serviços.
- Drill-down de impacto: "se deprecar este contrato, estes 12 serviços serão afectados".

### 15.2 Contract Drift Detection
**Pilar**: Operational Consistency + Change Intelligence
- Comparação automática entre o contrato publicado (source of truth) e o comportamento real capturado por traces OpenTelemetry.
- Alert quando o comportamento real diverge do contrato: "endpoint `/users/{id}` retorna campo não documentado `lastLoginIp`".
- Score de "Contract Adherence" por serviço.

### 15.3 AI Contract Reviewer
**Pilar**: AI-assisted Operations + Contract Governance
- Agente especializado que revê automaticamente rascunhos de contratos antes da submissão humana.
- Verifica: naming conventions, RESTfulness, segurança, exemplos, documentação de erros.
- Sugere melhorias inline no estúdio de contratos.
- Governado por política de acesso (respeita `contracts:ai:review` permission).

### 15.4 Contract Health Score Timeline
**Pilar**: Operational Intelligence + Source of Truth
- Evolução temporal do health score por contrato — gráfico de linha (Apache ECharts).
- "Este contrato estava a 87/100 há 30 dias, hoje está a 72/100 — 3 novas violações".
- Integração com changes: correlação automática entre mudança de score e último deploy.

### 15.5 Service Maturity Benchmark Comparativo
**Pilar**: Service Governance + Executive Views
- Comparação de maturidade entre serviços do mesmo domínio ou equipa.
- "Os serviços da equipa Payments têm maturidade média de 73% vs. 91% da equipa Identity".
- Drill-down por dimensão: contratos, testes, documentação, observabilidade.

### 15.6 Contract Promotion Gate
**Pilar**: Production Change Confidence + Change Intelligence
- Gate automático no pipeline de promoção de ambiente: "Este contrato em staging passa nos critérios para produção?".
- Verifica: health score > threshold, sem violações críticas, consumers em staging validados, approval completo.
- Integração com `PromotionGovernance` do módulo Changes.

### 15.7 Canonical Entity Impact Cascade Visualization
**Pilar**: Contract Governance + AI-assisted Operations
- Quando uma canonical entity muda, visualizar em tempo real a cascata de impacto: quais contratos referem, quais serviços consomem, qual o risco de breaking change.
- Fluxo de aprovação automático para mudanças de alta criticidade.

### 15.8 Contract Expiry & Sunset Calendar
**Pilar**: Operational Consistency + Governance
- Vista calendário integrada com `ReleaseCalendar` (módulo Changes) para contratos com sunset planeado.
- Alertas automáticos para consumers 90, 60 e 30 dias antes do sunset.
- Dashboard "Contratos a expirar nos próximos 90 dias" para Tech Leads.

---

## 16. Plano de Ação

### Fase 1 — Correções Críticas (Sprint 1, ~1 semana)

| ID | Tarefa | Camada | Esforço | Prioridade |
|----|--------|--------|---------|-----------|
| PA-01 | **Fix BUG-01**: Adicionar `Framework` e `Library` ao enum `ServiceType` no domínio + migração `FixFrameworkServiceType` | Domínio + BD | 3h | 🔴 P1 |
| PA-02 | **Adicionar 57 chaves de validação** aos ficheiros `en.json`, `pt-BR.json`, `pt-PT.json`, `es.json` | Frontend i18n | 4h | 🔴 P1 |
| PA-03 | **Corrigir select options hardcoded** em `ServiceCatalogPage.tsx` e `TemplateEditorPage.tsx` com `t()` | Frontend | 2h | 🔴 P1 |
| PA-04 | **Adicionar loading/error state** em `ContractWorkspacePage.tsx` e nas sections sem coverage | Frontend | 2h | 🔴 P1 |
| PA-05 | **Adicionar `.AsNoTracking()`** a todos os QueryHandlers de Contracts e Graph | Infra Backend | 4h | 🔴 P1 |
| PA-06 | **Feature flag ou erro claro** para `GenerateServerFromContract` até TODOs serem resolvidos | Backend API | 1h | 🔴 P1 |

### Fase 2 — Gaps de Qualidade (Sprint 2, ~2 semanas)

| ID | Tarefa | Camada | Esforço | Prioridade |
|----|--------|--------|---------|-----------|
| PA-07 | **Resolver 6 TODOs** em `GenerateServerFromContract.cs` — implementação real de geração de server stubs | Backend App | 12h | 🟡 P2 |
| PA-08 | **Criar `ContractHealthDashboardPage.test.tsx`** com 10+ testes (métricas, violações, empty state, erro) | Frontend Tests | 6h | 🟡 P2 |
| PA-09 | **Criar `VisualRestBuilder.test.tsx`** — endpoint, validação, schema, preview | Frontend Tests | 8h | 🟡 P2 |
| PA-10 | **Criar `ServiceRegistrationWizard.test.tsx`** — wizard steps, validação, submissão | Frontend Tests | 6h | 🟡 P2 |
| PA-11 | **Criar `contract-health-dashboard.spec.ts`** E2E — health metrics, violation list, navigação | E2E Tests | 4h | 🟡 P2 |
| PA-12 | **Criar `service-creation.spec.ts`** E2E — wizard de criação end-to-end | E2E Tests | 4h | 🟡 P2 |
| PA-13 | **Criar `contract-versioning.spec.ts`** E2E — criar versão, ver histórico, comparar | E2E Tests | 4h | 🟡 P2 |
| PA-14 | **Migração de índice FTS** para `ContractVersion.Title` e `SpecContent` (PostgreSQL GIN) | BD | 2h | 🟡 P2 |
| PA-15 | **Expandir `ContractGovernanceApplicationTests`** para 20+ testes (rejeição, re-submissão, escalada) | Backend Tests | 6h | 🟡 P2 |

### Fase 3 — Enriquecimento (Sprint 3-4)

| ID | Tarefa | Camada | Esforço | Prioridade |
|----|--------|--------|---------|-----------|
| PA-16 | **Criar `contract-approval-flow.spec.ts`** E2E completo (submit, approve, reject) | E2E Tests | 6h | 🟢 P3 |
| PA-17 | **Criar `contract-deprecation.spec.ts`** E2E — deprecation workflow + consumers afectados | E2E Tests | 4h | 🟢 P3 |
| PA-18 | **Criar métodos `GetDetailAsync()`** nos repositórios com Include statements predefinidos | Infra Backend | 8h | 🟢 P3 |
| PA-19 | **Adicionar aria-labels** a todos os botões de ícone em components de contratos e serviços | Frontend | 3h | 🟢 P3 |
| PA-20 | **Padronizar espaçamento** em `DeveloperPortalPage.tsx` — criar tokens de spacing consistentes | Frontend | 2h | 🟢 P3 |
| PA-21 | **Expandir `ContractVersionTests.cs`** para 15+ testes (sign, lock, deprecate, lifecycle completo) | Backend Tests | 4h | 🟢 P3 |
| PA-22 | **Converter testes de domain services para `[Theory]`** multi-protocolo | Backend Tests | 4h | 🟢 P3 |
| PA-23 | **Criar runbook operacional** para cenários críticos de contratos (locked contract correction, emergency rollback) | Docs | 3h | 🟢 P3 |
| PA-24 | **ADR-006: GraphQL/Protobuf no roadmap** — documentar decisão de não incluir no MVP1 | Docs | 1h | 🟢 P3 |

### Fase 4 — Inovação (Sprint 5-8)

| ID | Tarefa | Pilar | Esforço |
|----|--------|-------|---------|
| PA-25 | **Contract Drift Detection** — comparar contrato publicado vs. traces OTel reais | OI + Contracts | 3 semanas |
| PA-26 | **AI Contract Reviewer** — agente de revisão automática de rascunhos | AI + Contracts | 2 semanas |
| PA-27 | **Contract Health Score Timeline** — evolução temporal com correlação de changes | Analytics + Contracts | 1 semana |
| PA-28 | **Service Maturity Benchmark** — comparação entre equipas e domínios | Governance | 1 semana |
| PA-29 | **Contract Promotion Gate** — integração com PromotionGovernance do módulo Changes | Changes + Contracts | 2 semanas |
| PA-30 | **Canonical Entity Impact Cascade Visual** — visualização de impacto em tempo real | Contracts + AI | 2 semanas |

---

### Resumo do Plano por Prioridade

```
FASE 1 — Crítico (1 semana)
 6 tarefas | ~16 horas | 0 regressões esperadas

FASE 2 — Qualidade (2 semanas)
 9 tarefas | ~52 horas | Cobertura de testes aumenta significativamente

FASE 3 — Enriquecimento (2 semanas)
 9 tarefas | ~35 horas | Acessibilidade, performance, docs, robustez

FASE 4 — Inovação (4+ semanas)
 6 iniciativas | ~9 semanas | Diferenciação de produto

DÍVIDA TÉCNICA TOTAL IDENTIFICADA: ~58 horas (fases 1-3)
```

---

### Definition of Done para cada tarefa

- [ ] Implementação completa (sem TODOs ou stubs)
- [ ] Testes unitários adicionados ou actualizados
- [ ] i18n completo para as 4 locales
- [ ] Build passa sem erros (`dotnet build` + `npm run build`)
- [ ] Testes passam (`dotnet test` + `npm run test`)
- [ ] Documentação inline actualizada (XML comments em português)
- [ ] Sem novas vulnerabilidades de segurança
- [ ] Code review aprovado

---

*Análise gerada automaticamente com base na inspeção de todas as camadas do módulo de Serviços e Contratos do NexTraceOne em 2026-04-06.*
*Para questões sobre esta análise, consultar o `docs/SERVICES-CONTRACTS-ACTION-PLAN.md` e `docs/CONTRACT-STUDIO-VISION.md`.*
