# Revisão do Modelo de Dados — Catálogo de Serviços e Contratos

> Análise a nível de entidade e modelo relacional dos subdomínios **Graph** (registro de
> serviços) e **Contracts** (governança de contratos) do módulo `catalog`, com validação
> contra práticas de mercado (Backstage, Cortex/OpsLevel/Port, SwaggerHub/Postman, Pact,
> Confluent Schema Registry, Spectral, ODCS).
>
> Data da revisão: 2026-07-11. Base: `ServiceCatalogDbContextModelSnapshot` (migrations
> `20260605210320_InitialCreate` + `20260613110000_AddServiceOriginTemplate`).

---

## 1. Sumário executivo

O desenho de domínio (agregados, ciclo de vida, value objects) está alinhado — e em vários
pontos à frente — do estado da arte do mercado. Porém, a camada de **persistência não
acompanha o domínio**: a maior parte das 102 tabelas do módulo é mapeada apenas por
convenção, sem configuração explícita, e isso produz falhas objetivas de isolamento de
tenant, integridade, concorrência e performance.

| # | Severidade | Achado | Estado |
|---|-----------|--------|--------|
| 1 | **P0** | RLS inoperante no módulo: `infra/postgres/apply-rls.sql` referencia tabelas `ctr_*`/`cat_*` que não existem (EF gerou `ContractVersions`, `Drafts`, …) | **Corrigido neste PR** — convenção de nomes `ctr_`/`cat_`/`dx_` no DbContext, seções catalog do `apply-rls.sql` regeneradas a partir do modelo real, `seed-catalog.sql` atualizado |
| 2 | **P0** | 71 entidades sem `TenantId` (incl. `ContractVersion`, `ContractDraft`, `ApiAsset`) e repositórios de Contracts sem filtro de tenant | **Corrigido neste PR** — shadow property `TenantId` (Guid?) em toda entidade sem tenant, carimbada no `SaveChanges` a partir de `ICurrentTenant`, com filtro global de consulta combinado ao soft-delete; políticas RLS por tabela geradas |
| 3 | **P0** | `ServiceAsset.SearchVector` é `tsvector NOT NULL` sem coluna gerada/trigger e nunca preenchido — INSERT de serviço falha contra PostgreSQL real | **Corrigido neste PR** |
| 4 | **P0** | `ContractVersion.Sla`, `.Signature`, `.Provenance` eram ignorados pela convenção de VOs e **nunca persistidos** (perda silenciosa de SLA, assinatura digital e proveniência) | **Corrigido neste PR** |
| 5 | **P1** | `RowVersion` (xmin) declarado em 36 entidades mas mapeado como token de concorrência em apenas 2 — nas demais era coluna `bigint` inerte | **Corrigido neste PR** (convenção no DbContext) |
| 6 | **P1** | Nenhum índice em `TenantId` nas 24 entidades que o possuem; 81 entidades sem qualquer índice secundário | **Corrigido neste PR** (convenção + índices core) |
| 7 | **P1** | Invariantes documentados sem unique constraint: `(ApiAssetId, SemVer)`, `(TenantId, Name)` de serviço, `(TenantId, ServiceId, FlagKey)` de flags, `ContractId` de deprecação | **Corrigido neste PR** |
| 8 | **P2** | `FeatureFlagRecord.TenantId`/`SbomRecord.TenantId` são `string` — resto da plataforma usa `Guid` | Documentado |
| 9 | **P2** | Campos redundantes/livres em `ServiceAsset` (`RepositoryUrl` × `GitRepository`; `SloTarget`, `DataClassification`, `ChangeFrequency`, `HostingPlatform` como texto livre) | Documentado |
| 10 | **P2** | Referências soltas por `Guid` sem FK dentro do mesmo DbContext (`ContractVersion.ApiAssetId`, `ContractDraft.ServiceId`) | Documentado |

**IMPORTANTE — migration pendente:** as correções deste PR alteram o modelo EF.
É necessário gerar a migration localmente (o ambiente desta sessão não possui o SDK .NET):

```bash
dotnet ef migrations add CatalogDataModelHardening \
  --project src/modules/catalog/NexTraceOne.Catalog.Infrastructure \
  --startup-project src/platform/NexTraceOne.ApiHost \
  --context ServiceCatalogDbContext
```

A migration resultante deve: renomear ~90 tabelas para os nomes `ctr_`/`cat_`/`dx_`,
criar as colunas jsonb (`sla_json`, `signature_json`, `provenance_json`), adicionar as
colunas `TenantId uuid NULL` (shadow) nas entidades sem tenant, converter `SearchVector`
em coluna gerada + índice GIN, remover as colunas `RowVersion` bigint inertes e criar os
índices/uniques. Constraints únicas exigem dados sem duplicatas — validar antes em
ambientes com dados.

**Pós-migração:**
1. Reaplicar `infra/postgres/apply-rls.sql` (seções catalog regeneradas neste PR).
2. **Backfill de tenant** nas tabelas de Contracts a partir da cadeia
   `ApiAsset → ServiceAsset.TenantId` (linhas antigas ficam com `TenantId NULL` e
   permanecem visíveis durante a transição — tanto o filtro EF quanto as políticas RLS
   das colunas shadow permitem `TenantId IS NULL` intencionalmente).
3. Após o backfill, endurecer: remover a cláusula `"TenantId" IS NULL` das políticas e
   do filtro global, e opcionalmente tornar a coluna NOT NULL.

---

## 2. Análise por entidade (falta × excesso de informação)

### 2.1 `ServiceAsset` (registro de serviço)

**Pontos fortes:** identidade completa (nome, domínio, subdomínio, capability), ownership
em 3 eixos com criptografia de PII (`[EncryptedField]`), máquina de estados de lifecycle
validada em domínio, tier operacional, proveniência de template (golden path), revisão de
ownership com detecção de drift. Cobre e excede o modelo Component do Backstage.

**Falta:**
- Índice único `(TenantId, Name)` — sem ele o "nome único do serviço" documentado na
  entidade era apenas convenção (corrigido neste PR).
- Índice GIN + geração automática do `SearchVector` (corrigido neste PR — config `simple`,
  casando com `PlainToTsQuery("simple", …)` do repositório).
- Vocabulário controlado: `DataClassification`, `RegulatoryScope`, `ChangeFrequency`,
  `SloTarget`, `HostingPlatform`, `InfrastructureProvider` e `RuntimeLanguage` são texto
  livre. O mercado (Cortex/OpsLevel scorecards) trata esses campos como enums — texto
  livre degrada relatórios de maturidade e compliance. Recomendação: converter para enums
  de domínio (há precedente: `Criticality`, `ExposureType`, `ServiceTierType`).
- Tags/labels flexíveis: Backstage e Port suportam `labels`/`annotations` arbitrárias.
  Hoje qualquer metadado novo vira coluna. Recomendação: coluna `jsonb` de labels.

**Excesso / redundância:**
- `RepositoryUrl` × `GitRepository` — dois campos para o mesmo conceito; consolidar em um
  e migrar dados (o formulário de onboarding usa ambos de forma inconsistente).
- `UpdateExtendedMetadata` com 15 parâmetros string posicionais é frágil (troca silenciosa
  de argumentos da mesma família de tipos); considerar um value object de metadados.

### 2.2 `ContractVersion` (agregado central de contratos)

**Pontos fortes:** lifecycle de 7 estados, lock com autoria, semver, multi-protocolo,
limite de 1MB por spec, diffs/violações/artefatos como coleções filhas com FK reais.

**Falta (corrigido neste PR):**
- `Sla`, `Signature`, `Provenance` não eram persistidos — a convenção
  `ApplyValueObjectConventions` do `NexTraceDbContextBase` ignora tipos sem chave, e sem
  `OwnsOne` explícito esses três VOs eram silenciosamente descartados em cada
  `SaveChanges`. Na prática, **assinatura digital e proveniência de contratos nunca
  chegavam ao banco**. Agora mapeados como jsonb (`sla_json`, `signature_json`,
  `provenance_json`).
- Unique `(ApiAssetId, SemVer)` — o repositório usa `SingleOrDefaultAsync` nessa dupla,
  assumindo unicidade que o banco não garantia (corrigido, filtrado por soft-delete).
- Índice em `LifecycleState` (listagens de governança filtram por estado).

**Falta (documentado):**
- `TenantId` — ver §3. A âncora de tenant hoje é indireta
  (`ApiAssetId → ApiAsset → ServiceAsset.TenantId`) e nenhum repositório a percorre.
- FK real para `ApiAssets` — mesma DbContext, integridade referencial gratuita.

### 2.3 `ContractDraft` (Contract Studio)

**Pontos fortes:** fluxo de estados de edição/revisão, rastreio de geração por IA
(`IsAiGenerated` + prompt — em linha com as exigências emergentes de AI governance),
vínculo obrigatório a serviço.

**Falta:** índices `(ServiceId, Status)` e `Status` (corrigidos); `TenantId` (documentado);
`Format` ("json"/"yaml"/"xml") como enum em vez de string livre — `ContractProtocol` e
`ContractType` já são enums, `Format` destoa.

**Observação de modelagem:** o contrato tem três âncoras possíveis — `ContractDraft`
aponta para `ServiceId` (+ `ServiceInterfaceId` opcional), `ContractVersion` aponta para
`ApiAssetId`, e `ContractBinding` liga contrato a `ServiceInterface`. Documentar a âncora
canônica (recomendação: `ServiceInterface`) evita a degradação típica de catálogos.

### 2.4 Entidades satélite de Contracts

- `FeatureFlagRecord`: o XML doc promete "upsert idempotente por (TenantId, ServiceId,
  FlagKey)" e "Tabela: ctr_feature_flag_records" — nem o unique existia nem a tabela tem
  esse nome (é `FeatureFlagRecords`). Unique criado neste PR; renomeação em §5.
  `TenantId` é `string` (inconsistência de tipo com a plataforma).
- `DeprecationScheduleRecord`: upsert por `ContractId` documentado sem unique — criado
  neste PR.
- `SbomRecord`, `DataContractRecord`, snapshots GraphQL/Protobuf, `ContractChangelog`,
  `ContractVerification`, `SemanticDiffResult`, etc.: têm `TenantId` mas nenhum índice —
  cobertos pela convenção de índice de tenant adicionada ao DbContext.
- `DataContractRecord`: recomendável suportar import/export do Open Data Contract
  Standard (Bitol/Linux Foundation), pela mesma razão que motivou o suporte a Backstage.

### 2.5 Convenções transversais

- **RowVersion/xmin:** 36 entidades documentam "token de concorrência otimista
  (PostgreSQL xmin)", mas só `KnowledgeDocument` e `OperationalNote` (com configuração
  explícita) tinham o mapeamento. Nas outras 34, `RowVersion` era uma coluna `bigint`
  comum, sempre 0 — concorrência otimista inexistente. A convenção adicionada ao
  `ServiceCatalogDbContext` corrige todas de uma vez; a migration removerá as colunas
  inertes.
- **Colunas `text` ilimitadas:** 558 colunas `text` sem `HasMaxLength`. Os validators
  FluentValidation limitam a entrada via API, mas escritas por outros caminhos (jobs,
  eventos, imports) não passam por eles. Este PR adiciona limites nas entidades core;
  recomenda-se progressivamente nas demais.

---

## 3. Isolamento de tenant (P0 — requer fase dedicada)

O contrato arquitetural do projeto é RLS + filtro de repositório. No módulo catalog,
**nenhuma das duas camadas funciona para o subdomínio Contracts**:

1. `apply-rls.sql` referencia `ctr_contract_versions`, `cat_discovered_services`,
   `cat_contract_health_scores`, etc. — nomes que não existem no schema real (EF criou
   `ContractVersions`, `DiscoveredServices`, …). As políticas nunca são aplicadas.
2. 71 entidades não têm coluna de tenant; logo, mesmo com nomes corretos, não haveria
   política possível.
3. Os repositórios de Contracts (`ContractVersionRepository`, etc.) não aplicam
   `Where(TenantId == …)` — diferentemente de `ServiceAssetRepository`, que aplica.

Consequência: um tenant que obtenha o Guid de uma versão de contrato de outro tenant pode
lê-la.

**Resolução aplicada neste PR:** as duas camadas foram restauradas sem alterar domínio ou
handlers — (a) shadow property `TenantId Guid?` + filtro global de consulta no
`ServiceCatalogDbContext` (camada de aplicação) e (b) políticas RLS regeneradas para os
nomes reais de tabela (camada de banco). Linhas com tenant nulo (legado/sistema)
permanecem visíveis até o backfill — ver instruções pós-migração no topo do documento.

---

## 4. Nomes de tabela × convenção do projeto

O padrão do projeto (CLAUDE.md, Parte 8) exige prefixo de módulo (`cat_`/`ctr_`). Das 102
tabelas do DbContext, só as de Knowledge (`knw_`), ProductAnalytics (`pan_`) e o outbox
seguem a convenção — as demais usam o nome default do DbSet (`ContractVersions`, `Drafts`,
`Reviews`, `Examples`, …), incluindo nomes genéricos com alto risco de colisão. A
documentação (CLAUDE.md "Feature Status", XML docs) afirma nomes `ctr_*` que não existem.

---

## 5. Plano de remediação (fases)

1. **Fase A — ✅ aplicada neste PR:** persistência dos VOs de `ContractVersion`, tsvector
   gerado + GIN, convenções xmin e índice de tenant, uniques de invariantes core.
2. **Fase B — ✅ aplicada neste PR:** convenção de nomes `ctr_`/`cat_`/`dx_` no
   `ServiceCatalogDbContext` (configurações explícitas `knw_`/`pan_`/outbox prevalecem),
   regeneração das seções catalog do `apply-rls.sql` a partir do modelo real (37 blocos
   obsoletos removidos, 95 tabelas cobertas) e atualização do `seed-catalog.sql`.
3. **Fase C — ✅ aplicada neste PR (variante shadow):** em vez de alterar domínio e
   handlers, toda entidade sem `TenantId` ganhou shadow property `Guid?` carimbada no
   `SaveChanges` via `ICurrentTenant` + filtro global de consulta (combinado ao
   soft-delete). Pendências pós-migração: backfill e endurecimento (ver topo do doc).
   Evolução futura opcional: promover o tenant a conceito de domínio nos agregados core.
4. **Fase D — pendente (PR próprio):** enums para os campos livres de `ServiceAsset`,
   consolidação `RepositoryUrl`/`GitRepository`, `Format` como enum, FKs internas ao
   DbContext, `TenantId` `string→Guid` em `FeatureFlagRecord`/`SbomRecord` — mudam o
   contrato da API (frontend/ingestão precisam acompanhar).
5. **Fase E — pendente:** comando `nex contract validate|diff|can-i-deploy` para CI
   (APIOps shift-left) e query de decisão de deploy combinando `ContractDeployment` +
   `ContractVerification` + `ConsumerExpectation` (equivalente ao can-i-deploy do Pact
   Broker); import/export ODCS para data contracts.

> Observação fora do escopo do módulo: restaram 35 políticas `USING (true)` (no-op) em
> seções de outros módulos do `apply-rls.sql` — merecem a mesma revisão.
