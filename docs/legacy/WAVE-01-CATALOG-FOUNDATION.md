# Onda 1 — Foundation de Domínio e Catálogo Legacy

> **Duração estimada:** 4-6 semanas
> **Dependências:** Onda 0
> **Risco:** Médio — novo DbContext pode requerer ajustes no outbox processing
> **Referência:** [LEGACY-MAINFRAME-WAVES.md](../LEGACY-MAINFRAME-WAVES.md)

---

## Objetivo

Modelar e persistir ativos legacy como cidadãos de primeira classe no catálogo do NexTraceOne. Após esta onda, será possível registar, consultar e gerir ativos mainframe da mesma forma que serviços modernos.

---

## Entregáveis

- [ ] Entidades de domínio: `MainframeSystem`, `CobolProgram`, `Copybook`, `CicsTransaction`, `ImsTransaction`, `Db2Artifact`, `ZosConnectBinding`
- [ ] Value Objects: `LparReference`, `CicsRegion`, `CopybookLayout`, `JobSchedule`
- [ ] Repository interfaces e implementações EF Core
- [ ] CRUD Commands/Queries/Handlers para todos os ativos legacy
- [ ] DTOs e FluentValidation Validators
- [ ] `LegacyAssetsDbContext` (sub-context dentro do módulo Catalog)
- [ ] Migrações EF para novas tabelas
- [ ] Endpoint na Ingestion API: `POST /api/v1/legacy/assets/sync`
- [ ] Página frontend Legacy Asset Catalog
- [ ] Chaves i18n para catálogo legacy (~150 keys adicionais)

---

## Impacto Backend

### Novo Sub-domínio: `LegacyAssets`

Localização: `src/modules/catalog/NexTraceOne.Catalog.Domain/LegacyAssets/`

#### Entidades

| Entidade | Tipo | Descrição |
|---|---|---|
| `MainframeSystem` | Aggregate Root | Sistema mainframe (LPAR, sysplex, região) |
| `CobolProgram` | Entity | Programa COBOL (nome, copybooks usados, data compilação) |
| `Copybook` | Entity | Definição de copybook (nome, layout, versão) |
| `CicsTransaction` | Entity | Transação CICS (TRANID, programa, região, commarea) |
| `ImsTransaction` | Entity | Transação IMS (código, PSB, DBD) |
| `Db2Artifact` | Entity | Objeto DB2 (nome, tipo, tablespace) |
| `ZosConnectBinding` | Entity | Binding z/OS Connect (service, operation, mapping) |

#### Value Objects

| Value Object | Propriedades |
|---|---|
| `LparReference` | SysplexName, LparName, RegionName |
| `CicsRegion` | RegionName, CicsVersion, Port |
| `CopybookLayout` | Fields (lista), TotalLength, RecordFormat |
| `JobSchedule` | ScheduleType, Frequency, Window, CronExpression |

#### Enums

| Enum | Valores |
|---|---|
| `MainframeAssetType` | System, Program, Copybook, Transaction, Job, Artifact, Binding |
| `CicsTransactionType` | Online, Conversational, Pseudo, Web, Channel |
| `ImsTransactionType` | MPP, BMP, FastPath, IFP |
| `Db2ArtifactType` | Table, View, StoredProcedure, Index, Tablespace, Package |

### Commands e Queries

| Feature | Tipo | Handler |
|---|---|---|
| `RegisterMainframeSystem` | Command | Cria novo sistema mainframe |
| `RegisterCobolProgram` | Command | Regista programa COBOL com referências |
| `RegisterCopybook` | Command | Regista copybook com campos |
| `RegisterCicsTransaction` | Command | Regista transação CICS |
| `RegisterImsTransaction` | Command | Regista transação IMS |
| `RegisterDb2Artifact` | Command | Regista artefacto DB2 |
| `RegisterZosConnectBinding` | Command | Regista binding z/OS Connect |
| `UpdateLegacyAsset` | Command | Atualiza qualquer ativo legacy |
| `ListLegacyAssets` | Query | Lista com filtros e paginação |
| `GetLegacyAssetDetail` | Query | Detalhe completo de um ativo |
| `SearchLegacyAssets` | Query | Pesquisa full-text em ativos |
| `SyncLegacyAssets` | Command | Sync bulk via API (Ingestion) |

### Validators (FluentValidation)

- `RegisterMainframeSystemValidator` — nome obrigatório, LPAR válido
- `RegisterCobolProgramValidator` — programa com nome e sistema associado
- `RegisterCopybookValidator` — nome e pelo menos um campo
- `RegisterCicsTransactionValidator` — TRANID (4 chars), programa obrigatório
- `RegisterImsTransactionValidator` — código de transação e PSB
- `SyncLegacyAssetsValidator` — validação de batch payload

### Ingestion API

Novo endpoint em `NexTraceOne.Ingestion.Api`:

```
POST /api/v1/legacy/assets/sync
```

Request DTO:
```csharp
public sealed record LegacyAssetSyncRequest(
    string Provider,                    // CMDB, manual, etc.
    string? CorrelationId,
    List<LegacyAssetItem> Assets
);

public sealed record LegacyAssetItem(
    string AssetType,                   // MainframeSystem, CobolProgram, etc.
    string Name,
    string? DisplayName,
    string? Description,
    string? SystemName,                 // Parent mainframe system
    string? TeamName,
    string? Domain,
    string? Criticality,
    Dictionary<string, string>? Metadata
);
```

---

## Impacto Frontend

### Nova Página: Legacy Asset Catalog

**Rota:** `/services/legacy`
**Persona:** Architect, Tech Lead

**Componentes:**
- `LegacyAssetCatalogPage` — página principal com lista e filtros
- `LegacyAssetCard` — card para cada ativo no grid/list
- `LegacyAssetDetailPanel` — painel lateral com detalhe
- `LegacyAssetFilters` — filtros por tipo, team, domain, criticality
- `LegacyAssetTypeIcon` — ícone diferenciado por tipo de ativo

**Filtros disponíveis:**
- Tipo de ativo (MainframeSystem, CobolProgram, etc.)
- Team
- Domain
- Criticality
- Lifecycle status
- Pesquisa por nome

### Nova Página: Mainframe System Detail

**Rota:** `/services/legacy/:systemId`
**Persona:** Architect

**Secções:**
- Informação geral (nome, LPAR, sysplex, team, domain)
- Programas COBOL associados
- Transações CICS/IMS
- Copybooks utilizados
- Artefactos DB2
- Bindings z/OS Connect
- Timeline de mudanças

### Extensão do Sidebar

Adicionar em **Services**:
```
Services
  ├── Service Catalog      (existente)
  ├── Dependency Graph     (existente)
  └── Legacy Assets        ← NOVO
```

---

## Impacto Base de Dados

### Novas Tabelas PostgreSQL (prefixo `cat_`)

| Tabela | Descrição |
|---|---|
| `cat_mainframe_systems` | Sistemas mainframe (aggregate root) |
| `cat_cobol_programs` | Programas COBOL |
| `cat_copybooks` | Definições de copybook |
| `cat_copybook_fields` | Campos parsed de copybook |
| `cat_cics_transactions` | Transações CICS |
| `cat_ims_transactions` | Transações IMS |
| `cat_db2_artifacts` | Artefactos DB2 |
| `cat_zos_connect_bindings` | Bindings z/OS Connect |
| `cat_legacy_dependencies` | Dependências entre ativos legacy |
| `cat_copybook_program_usages` | Relação programa ↔ copybook |

### Novo DbContext

`LegacyAssetsDbContext` dentro do módulo Catalog com:
- Configurações EF Core para todas as entidades
- Outbox table: `cat_legacy_outbox_messages`
- Índices para pesquisa por nome, tipo, team, domain

---

## Testes

### Testes Unitários (~100)

- Entidades: criação, validação, transição de estado
- Value Objects: igualdade, validação
- Validators: validação de inputs
- Handlers: lógica de negócio de cada command/query

### Testes de Integração (~20)

- Repository tests com Testcontainers PostgreSQL
- Migração em base limpa e com dados existentes
- Sync bulk API funcional

---

## Critérios de Aceite

1. ✅ CRUD completo para todos os 7 tipos de ativos legacy
2. ✅ Busca e filtro funcionais no catálogo
3. ✅ API de sync bulk funcional via Ingestion API
4. ✅ UI mostra ativos legacy com tipo, ownership e criticality
5. ✅ i18n completo em todas as telas novas
6. ✅ Testes de regressão passam sem falhas
7. ✅ Outbox configurado e funcional para novo DbContext
8. ✅ Sidebar atualizado com nova entrada "Legacy Assets"

---

## Riscos

| Risco | Severidade | Mitigação |
|---|---|---|
| Novo DbContext requer configuração de outbox | Média | Seguir padrão existente dos outros módulos |
| Volume de ativos legacy pode ser grande | Média | Paginação obrigatória, índices adequados |
| Modelo de dados pode precisar ajustes | Baixa | Modelo extensível com `Metadata` dictionary |

---

## Stories

| ID | Story | Prioridade | Estado |
|---|---|---|---|
| W1-S01 | Criar entidades de domínio para ativos legacy | P0 | ✅ Implementado (10 entidades) |
| W1-S02 | Criar value objects (LparReference, CicsRegion, etc.) | P0 | ✅ Implementado (4 VOs) |
| W1-S03 | Criar enums legacy (MainframeAssetType, etc.) | P0 | ✅ Implementado (4 enums) |
| W1-S04 | Criar `LegacyAssetsDbContext` com configurações | P0 | ✅ Implementado (DbContext + 10 configs + 7 repos + DI) |
| W1-S05 | Criar migrações EF para novas tabelas | P0 | ✅ Implementado (11 tabelas) |
| W1-S06 | Implementar `RegisterMainframeSystem` command + handler | P1 | ✅ Implementado |
| W1-S07 | Implementar `RegisterCobolProgram` command + handler | P1 | ✅ Implementado |
| W1-S08 | Implementar `RegisterCopybook` command + handler | P1 | ✅ Implementado |
| W1-S09 | Implementar `RegisterCicsTransaction` command + handler | P1 | ✅ Implementado |
| W1-S10 | Implementar commands para IMS, DB2, z/OS Connect | P2 | ✅ Implementado |
| W1-S11 | Implementar `ListLegacyAssets` e `GetLegacyAssetDetail` queries | P1 | ✅ Implementado |
| W1-S12 | Implementar `SyncLegacyAssets` para Ingestion API | P1 | ✅ Implementado (POST /api/catalog/legacy/assets/sync) |
| W1-S13 | Criar FluentValidation validators | P1 | ✅ Implementado |
| W1-S14 | Criar endpoint modules na Catalog API | P1 | ✅ Implementado (10 endpoints) |
| W1-S15 | Criar `LegacyAssetCatalogPage` frontend | P1 | ✅ Implementado |
| W1-S16 | Criar `MainframeSystemDetailPage` frontend | P1 | ✅ Implementado |
| W1-S17 | Atualizar sidebar com "Legacy Assets" | P1 | ✅ Implementado |
| W1-S18 | Adicionar i18n keys (~150) | P1 | ✅ Implementado (132 keys × 4 locales) |
| W1-S19 | Criar testes unitários (~100) | P1 | ✅ Implementado (~88 testes, 741 total) |
| W1-S20 | Criar testes de integração (~20) | P2 | ⏳ Pendente |
| W1-S21 | Configurar outbox processing para `LegacyAssetsDbContext` | P1 | ✅ Implementado |
