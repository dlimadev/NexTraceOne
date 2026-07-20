# Plano — Remediação das FKs typed-id não mapeadas no ServiceCatalog

> **Para workers agênticos:** SUB-SKILL RECOMENDADA: `superpowers:subagent-driven-development` ou `executing-plans`.
> **PRÉ-REQUISITO OBRIGATÓRIO:** ambiente com **PostgreSQL real** para aplicar e verificar a migração (`AddColumn` em tabelas potencialmente populadas). Testes EF InMemory **não** validam mapeamento de colunas — não confiar neles para este trabalho.

**Goal:** Mapear as 31 FKs typed-id atualmente não persistidas do `ServiceCatalogDbContext`, restaurando a persistência (hoje há perda silenciosa de dados na escrita e `Where(x => x.Fk == …)` lança "member unmapped").

**Arquitetura:** 30 são scalar puros (sem navegação em nenhum lado, sem coluna shadow) → fix uniforme: `IEntityTypeConfiguration` que mapeia a propriedade + migração `AddColumn`. 1 é relacional (PackageDependency, navegação do lado do principal) → tratamento especial.

**Contexto que valida a abordagem:** o `ServiceCatalogDbContext` **NÃO tem drift** (probe confirmou: migração só toca a tabela alvo). Logo uma única migração captura todos os `AddColumn` de forma limpa. Ver [[reference-typed-id-fk-mapping-gap]].

## Global Constraints

- Configs em `src/modules/catalog/NexTraceOne.Catalog.Infrastructure/Persistence/Configurations/` (aplicadas via `ConfigurationsAssembly`; `ConfigurationsNamespace = null`), namespace `NexTraceOne.Catalog.Infrastructure.Persistence.Configurations`, `internal sealed`.
- Converter typed-id: `builder.Property(x => x.<Fk>).HasConversion(id => id.Value, value => new <Type>(value))` — o construtor primário existe sempre (não depender de `.From`).
- XML doc em português; código/identificadores em inglês.
- Migração via: `NEXTRACE_SKIP_INTEGRITY=true dotnet ef migrations add <Nome> --project src/modules/catalog/NexTraceOne.Catalog.Infrastructure --startup-project src/platform/NexTraceOne.ApiHost --context ServiceCatalogDbContext`.
- **Verificar SEMPRE a migração gerada** antes de aplicar. Para os scalar: só `AddColumn` (+`CreateIndex`), sem `DropColumn`. Sem navegação ⇒ EF **não** cria constraint FK ⇒ `AddColumn<Guid>` NOT NULL com `defaultValue` empty-guid é seguro (sem violação de integridade nas linhas existentes).
- Aplicar e verificar contra **Postgres real** (`dotnet ef database update … --context ServiceCatalogDbContext`), depois um teste de leitura `Where(x => x.Fk == id)` que hoje falha.

---

## Grupo A — Scalar puro (30 sites) — fix UNIFORME

Cada um: criar `<Entity>Configuration.cs` que mapeia a(s) FK(s). Todas as propriedades são `= null!`/`= default!` (required) ⇒ coluna NOT NULL (empty-guid default nas linhas existentes). Adicionar `HasIndex` em cada FK.

**Template (exemplo com uma FK):**
```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using <NAMESPACE_DA_ENTIDADE>;               // ver coluna "Namespace" abaixo
// + using do namespace do typed-id, se diferente (resolver ao criar)

namespace NexTraceOne.Catalog.Infrastructure.Persistence.Configurations;

/// <summary>Mapeia a FK typed-id não descoberta pela convenção (ver reference-typed-id-fk-mapping-gap).</summary>
internal sealed class <Entity>Configuration : IEntityTypeConfiguration<<Entity>>
{
    public void Configure(EntityTypeBuilder<<Entity>> builder)
    {
        builder.Property(x => x.<Fk>)
            .HasConversion(id => id.Value, value => new <Type>(value));
        builder.HasIndex(x => x.<Fk>);
    }
}
```
Entidades com múltiplas FKs (CopybookDiffRecord: 3; CopybookProgramUsage: 2) → mapear todas no mesmo Configure.

### A.1 — Subdomínio LegacyAssets / Mainframe (`SystemId` → `MainframeSystemId`)
Namespace de todas: `NexTraceOne.Catalog.Domain.LegacyAssets.Entities`.

| Entity | FK prop | Type |
|---|---|---|
| CicsTransaction | SystemId | MainframeSystemId |
| CobolProgram | SystemId | MainframeSystemId |
| Copybook | SystemId | MainframeSystemId |
| Db2Artifact | SystemId | MainframeSystemId |
| ImsTransaction | SystemId | MainframeSystemId |
| MqMessageContract | SystemId | MainframeSystemId |
| ZosConnectBinding | SystemId | MainframeSystemId |

> Nota: verificar `CTransaction.cs`/`CicsTransaction` — o detetor listou `CTransaction` (nome de ficheiro) para `SystemId`; confirmar o nome de classe real ao criar o config.

### A.2 — Subdomínio LegacyAssets / Copybook
Namespace: `NexTraceOne.Catalog.Domain.LegacyAssets.Entities`.

| Entity | FK prop | Type |
|---|---|---|
| CopybookContractMapping | CopybookId | CopybookId |
| CopybookField | CopybookId | CopybookId |
| CopybookVersion | CopybookId | CopybookId |
| CopybookProgramUsage | CopybookId | CopybookId |
| CopybookProgramUsage | ProgramId | CobolProgramId |
| CopybookDiffRecord | CopybookId | CopybookId |
| CopybookDiffRecord | BaseVersionId | CopybookVersionId |
| CopybookDiffRecord | TargetVersionId | CopybookVersionId |

### A.3 — Subdomínio Contracts
Namespace: `NexTraceOne.Catalog.Domain.Contracts.Entities`.

| Entity | FK prop | Type |
|---|---|---|
| BackgroundServiceDraftMetadata | ContractDraftId | ContractDraftId |
| CanonicalEntityVersion | CanonicalEntityId | CanonicalEntityId |
| ContractDeployment | ContractVersionId | ContractVersionId |
| ContractEvidencePack | ContractVersionId | ContractVersionId |
| ContractReview | DraftId | ContractDraftId |
| EventContractDetail | ContractVersionId | ContractVersionId |
| EventDraftMetadata | ContractDraftId | ContractDraftId |
| MarketplaceReview | ListingId | ContractListingId |
| SoapContractDetail | ContractVersionId | ContractVersionId |
| SoapDraftMetadata | ContractDraftId | ContractDraftId |

### A.4 — Subdomínio Graph (serviço/asset/interface)
Namespace: `NexTraceOne.Catalog.Domain.Graph.Entities`.

| Entity | FK prop | Type |
|---|---|---|
| AssetDeploymentState | ServiceAssetId | ServiceAssetId |
| FrameworkAssetDetail | ServiceAssetId | ServiceAssetId |
| ServiceInterface | ServiceAssetId | ServiceAssetId |
| ConsumerRelationship | ConsumerAssetId | ConsumerAssetId |
| ContractBinding | ServiceInterfaceId | ServiceInterfaceId |

> Nota `ContractBinding`: já tem `ContractVersionId` como **Guid simples** (mapeado por convenção); mapear só a FK typed `ServiceInterfaceId`.

---

## Grupo B — Relacional especial (1 site): `PackageDependency.ProfileId`

Namespace: `NexTraceOne.Catalog.Domain.DependencyGovernance.Entities`. Type: `ServiceDependencyProfileId`.

**Porque é diferente:** o principal `ServiceDependencyProfile` tem uma **coleção-navegação** cuja FK usa a coluna shadow `ServiceDependencyProfileId` (nullable). A propriedade typed `ProfileId` é um duplicado não ligado à relação. Mapear `ProfileId` à coluna `ServiceDependencyProfileId` de forma ingénua **desloca** a FK da navegação para uma nova shadow `ServiceDependencyProfileId1` (verificado na probe).

**Fix correto (requer inspeção da relação):**
1. Localizar onde a relação `ServiceDependencyProfile` → `PackageDependency` é declarada (coleção no principal; procurar `HasMany`/`List<PackageDependency>` e o nome da navegação).
2. Configurar a relação a usar a propriedade typed como FK, reutilizando a coluna existente:
   ```csharp
   builder.Property(x => x.ProfileId)
       .HasConversion(id => id.Value, value => new ServiceDependencyProfileId(value))
       .HasColumnName("ServiceDependencyProfileId");
   builder.HasOne<ServiceDependencyProfile>()          // ou HasOne(x => x.<nav>) se existir no dependente
       .WithMany(/* nav-coleção do principal, se houver */)
       .HasForeignKey(x => x.ProfileId)
       .IsRequired();                                   // ProfileId é null! (required)
   ```
3. Gerar a migração e **confirmar**: NÃO deve criar `ServiceDependencyProfileId1`. Espera-se `AlterColumn` nullable→NOT NULL na coluna existente (empty-guid default) + recriação da FK na mesma coluna. Se surgir `...Id1`, a relação não ficou unificada — rever o `WithMany`.

---

## Execução (ordem)

1. Criar os 30 configs do Grupo A (um por entidade; entidades com N FKs mapeiam as N). **Verificar:** `dotnet build` do módulo Infrastructure compila.
2. Gerar UMA migração `MapCatalogTypedIdFks` para `ServiceCatalogDbContext`. **Verificar:** o `Up()` contém apenas `AddColumn` (+`CreateIndex`) para as tabelas do Grupo A — nenhum `DropColumn`/drift. (ServiceCatalog não tem drift; se aparecer, PARAR e reportar.)
3. Tratar o Grupo B (PackageDependency) — pode ir na mesma migração ou numa própria; confirmar ausência de `...Id1`.
4. Aplicar contra Postgres real: `dotnet ef database update --context ServiceCatalogDbContext …`.
5. **Verificação de sucesso (por grupo, amostragem):** escrever um teste de integração (Postgres) que persiste uma entidade com a FK preenchida, relê por `Where(x => x.Fk == id)` e confirma o valor (hoje: exceção "member unmapped" / valor perdido). Rodar a suíte `NexTraceOne.Catalog.Tests` — 0 regressões.
6. Atualizar [[reference-typed-id-fk-mapping-gap]] marcando os 31 como resolvidos.

## Critérios de verificação

- [ ] 30 configs Grupo A criados; build 0 erros.
- [ ] Migração só com `AddColumn`/`CreateIndex` (Grupo A) — inspecionada, sem `DropColumn`/drift.
- [ ] PackageDependency: migração sem `ServiceDependencyProfileId1`.
- [ ] `database update` aplica em Postgres real sem erro.
- [ ] Teste de leitura `Where(x => x.Fk == id)` passa para ≥1 entidade por subgrupo (A.1–A.4 + B).
- [ ] Suíte Catalog verde.

## Notas de risco

- `AddColumn` NOT NULL com empty-guid: as linhas existentes ficam com FK `00000000-…` (dangling, mas sem constraint pois não há navegação nos scalar). Se o negócio exigir integridade, considerar uma etapa de backfill separada — **fora do âmbito** deste plano (o objetivo é parar a perda de dados a partir de agora).
- Se algum "scalar" afinal tiver navegação do lado do principal (como PackageDependency), a probe mostrará `...Id1` — nesse caso mover essa entidade para o tratamento do Grupo B.
