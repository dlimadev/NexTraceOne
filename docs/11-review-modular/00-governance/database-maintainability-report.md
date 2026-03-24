# Relatório de Manutenibilidade de Base de Dados — NexTraceOne

> **Data:** 2025-01-XX  
> **Escopo:** Organização de código, convenções, clareza de configuração, legibilidade de migrations, facilidade de onboarding e riscos de manutenção  
> **Método:** Análise estática de estrutura de ficheiros, padrões de código, naming conventions e modularidade  
> **Contexto:** 132 entity type configurations, 20 DbContexts, 29 migrations, 19 model snapshots

---

## 1. Resumo Executivo

| Dimensão | Avaliação | Nota (1-5) |
|---|---|---|
| Organização de código | ✅ Boa | 4/5 |
| Convenções de nomenclatura | ✅ Consistente | 4/5 |
| Clareza de configuração | ✅ Boa | 4/5 |
| Legibilidade de migrations | ⚠️ Parcial | 3/5 |
| Facilidade de onboarding | ⚠️ Moderada | 3/5 |
| Isolamento entre módulos | ✅ Forte | 4/5 |
| Áreas de alto risco | ⚠️ Existem | 3/5 |
| **Avaliação global** | **✅ Boa com pontos de atenção** | **3.6/5** |

---

## 2. Organização de Código

### 2.1 Estrutura de Diretórios

Cada módulo segue uma estrutura consistente:

```
src/modules/{module}/
├── Domain/
│   ├── Entities/
│   ├── ValueObjects/
│   ├── Events/
│   └── Enums/
├── Application/
│   ├── Commands/
│   ├── Queries/
│   └── DTOs/
└── Infrastructure/
    └── Persistence/
        ├── {Module}DbContext.cs
        ├── Configurations/
        │   ├── {Entity}Configuration.cs
        │   └── ...
        ├── Migrations/
        │   ├── {Timestamp}_{Name}.cs
        │   ├── {Timestamp}_{Name}.Designer.cs
        │   └── {DbContext}ModelSnapshot.cs
        └── Seeders/ (quando aplicável)
```

**Avaliação:** ✅ Estrutura clara e previsível. Um developer novo pode encontrar qualquer artefacto de persistência seguindo a convenção.

### 2.2 Distribuição de Ficheiros de Configuração

| Módulo | Ficheiros Config | DbContexts |
|---|---|---|
| `identityaccess` | ~20 | 1 |
| `auditcompliance` | ~8 | 1 |
| `catalog` | ~25 | 3 (Contracts, Graph, Portal) |
| `changegovernance` | ~28 | 4 (Change, Promotion, Workflow, Ruleset) |
| `governance` | ~15 | 1 |
| `notifications` | ~4 | 1 |
| `configuration` | ~4 | 1 |
| `aiknowledge` | ~25 | 3 (Governance, Orchestration, External) |
| `operationalintelligence` | ~20 | 5 (Cost, Runtime, Incident, Automation, Reliability) |
| **Total** | **~132** | **20** |

**Avaliação:** Proporção de ~6.6 configs/DbContext é saudável. Nenhum módulo tem ficheiros excessivos.

### 2.3 Separação por Namespace

Cada DbContext filtra as suas configurações via `ConfigurationsNamespace`:

```csharp
// Em NexTraceDbContextBase.OnModelCreating
modelBuilder.ApplyConfigurationsFromAssembly(
    assembly,
    type => type.Namespace?.StartsWith(ConfigurationsNamespace) == true
);
```

**Vantagem:** Garante que configs de um DbContext não são acidentalmente aplicadas a outro.  
**Risco:** Se o namespace estiver mal configurado, entidades ficam sem configuração (silenciosamente).

---

## 3. Convenções de Nomenclatura

### 3.1 DbContexts

| Padrão | Exemplo | Consistência |
|---|---|---|
| `{Domain}DbContext` | `IdentityDbContext`, `AuditDbContext` | ✅ Consistente |
| Exceções | `CatalogGraphDbContext` (sub-domínio) | ✅ Aceitável |

### 3.2 Entidades

| Padrão | Exemplo | Consistência |
|---|---|---|
| PascalCase singular | `User`, `Release`, `AiAgent` | ✅ Consistente |
| Prefixo de módulo em IA | `AiAgent`, `AiModel`, `AiProvider` | ✅ Evita colisão |
| Value objects | `Email`, `FullName`, `HashedPassword` | ✅ Consistente |

### 3.3 Strongly-Typed IDs

| Padrão | Exemplo | Consistência |
|---|---|---|
| `{Entity}Id` | `UserId`, `TenantId`, `ServiceId` | ✅ Consistente |
| Tipo subjacente | `Guid` → `uuid` PostgreSQL | ✅ Padronizado |

### 3.4 Entity Configurations

| Padrão | Exemplo | Consistência |
|---|---|---|
| `{Entity}Configuration` | `UserConfiguration`, `ReleaseConfiguration` | ✅ Consistente |
| Interface | `IEntityTypeConfiguration<T>` | ✅ Padrão EF Core |

### 3.5 Migrations

| Padrão | Exemplo | Consistência |
|---|---|---|
| `{Timestamp}_{Name}` | `20250115_InitialCreate` | ✅ EF Core standard |
| Nomenclatura | Variada (InitialCreate, Add*, Phase*, Fix*) | ⚠️ Parcialmente consistente |

### 3.6 Enums

| Padrão | Exemplo | Consistência |
|---|---|---|
| Conversão para string | 91 conversões | ✅ Maioritário |
| Conversão para int | 12 conversões | ⚠️ Minoria — inconsistente |

**Recomendação:** Documentar critério de escolha entre string e int para enums, ou padronizar para string.

---

## 4. Clareza de Configuração

### 4.1 Padrão de EntityTypeConfiguration

```csharp
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new UserId(value));
        
        builder.Property(x => x.Email)
            .HasConversion(e => e.Value, v => Email.Create(v))
            .IsRequired()
            .HasMaxLength(256);
        
        builder.HasIndex(x => new { x.TenantId, x.Email })
            .IsUnique();
        
        // ... relationships
    }
}
```

**Avaliação:** ✅ Padrão fluent claro e legível. Cada config é auto-contida.

### 4.2 Pontos Fortes

| Aspeto | Detalhe |
|---|---|
| Conversões explícitas | Strongly-typed IDs e value objects com conversões claras |
| Índices declarativos | HasIndex com IsUnique, composite keys |
| MaxLength definido | Strings com limites explícitos |
| Relationships claras | HasOne/HasMany com FK explícita |
| Table naming | Snake_case para PostgreSQL (`"users"`, `"releases"`) |

### 4.3 Pontos de Atenção

| Aspeto | Risco |
|---|---|
| Sem comentários explicativos | Configs complexas não explicam "porquê" |
| Delete behaviors implícitos | Maioria não especifica `OnDelete` |
| Sem validação de completude | Nenhum teste verifica que todas as entidades têm config |
| Conversões enum inconsistentes | 91 string vs 12 int sem critério documentado |

---

## 5. Legibilidade de Migrations

### 5.1 InitialCreate Migrations

As 17 migrations `InitialCreate` são geradas automaticamente pelo EF Core e são tipicamente longas:

| Aspeto | Avaliação |
|---|---|
| Geração | Automática (EF Core) |
| Legibilidade | ⚠️ Baixa — ficheiros grandes com código gerado |
| Manutenção | ✅ Não precisam de edição manual |
| Compreensão | ⚠️ Difícil para developers novos |

### 5.2 Migrations Incrementais

| Migration | Legibilidade | Notas |
|---|---|---|
| `AddIsPrimaryProductionToEnvironment` | ✅ Simples | 1 campo adicionado |
| `Phase3ComplianceDomain` | ⚠️ Média | Múltiplas tabelas novas |
| `Phase5Enrichment` | ⚠️ Média | Campos adicionados a tabelas existentes |
| `AddLastProcessedAt` | ✅ Simples | 1 campo adicionado |
| `AddCostImportPipeline` | ✅ Simples | 1 tabela nova |
| `ExpandProviderAndModelEntities` | ⚠️ Média | Múltiplos campos |
| `AddAiAgentEntity` | ⚠️ Média | Múltiplas tabelas |
| `AddAgentRuntimeFoundation` | ⚠️ Média | Múltiplas tabelas |
| `StandardizeTenantIdToGuid` | ❌ Complexa | Alteração de tipo de coluna |
| `FixTenantIdToUuid` | ❌ Complexa | Correção de tipo |
| `SeparateSharedEntityOwnership` | ⚠️ Média | Refactoring de relações |

### 5.3 Recomendação

- Adicionar comentários XML nas migrations incrementais explicando o "porquê"
- Considerar consolidação de migrations do AiGovernanceDbContext
- Manter migrations pequenas e focadas (uma responsabilidade por migration)

---

## 6. Facilidade de Onboarding

### 6.1 Para um Developer Novo

| Passo | Dificuldade | Obstáculo |
|---|---|---|
| Entender a estrutura de módulos | ✅ Fácil | Estrutura consistente |
| Encontrar um DbContext | ✅ Fácil | Convenção `{Domain}DbContext` |
| Entender uma entity config | ✅ Fácil | Padrão fluent standard |
| Criar uma nova entidade | ⚠️ Média | Precisa saber de strongly-typed IDs, AuditableEntity, conversões |
| Criar uma migration | ⚠️ Média | Sem DesignTimeDbContextFactory; precisa de context da app |
| Entender multi-tenancy | ⚠️ Média | RLS é transparente mas não óbvio |
| Entender outbox pattern | ⚠️ Média | Interceptor automático mas não documentado inline |
| Executar seeds | ✅ Fácil | `DevelopmentSeedDataExtensions` automático |

### 6.2 Documentação Necessária

| Documento | Estado | Prioridade |
|---|---|---|
| Guia de criação de entidades | ❌ Não existe | Alta |
| Guia de criação de migrations | ❌ Não existe | Alta |
| Mapa de DbContexts e bases de dados | Este relatório | Média |
| Explicação de interceptors | ❌ Não existe | Média |
| Checklist de nova entidade | ❌ Não existe | Alta |

### 6.3 Checklist Proposta para Nova Entidade

```
□ Entidade estende AuditableEntity ou AggregateRoot
□ ID é strongly-typed ({Entity}Id)
□ Value objects usam conversões explícitas
□ EntityTypeConfiguration criado no namespace correto
□ Índices definidos (TenantId + campos de busca)
□ Delete behaviors explícitos
□ DbSet adicionado ao DbContext correto
□ Migration gerada e testada
□ Seed data adicionado (se development)
```

---

## 7. Áreas de Alto Risco

### 7.1 Classificação de Risco

| Área | Risco | Justificação |
|---|---|---|
| `nextraceone_operations` (12 DbContexts) | 🔴 Alto | Sobrecarregada — colisões de outbox, migrations complexas |
| `AiGovernanceDbContext` (19+ entidades, 7 migrations) | 🔴 Alto | Overloaded + dívida técnica |
| Configuration/Notifications (sem migrations) | 🟡 Médio | Schema instável, não suporta evolução |
| Conversões enum inconsistentes (91 string + 12 int) | 🟡 Médio | Falta de padronização |
| Sem RowVersion em qualquer entidade | 🟡 Médio | Lost updates possíveis |
| Sem DesignTimeDbContextFactory | 🟡 Médio | Complica tooling de migrations |
| Delete behaviors implícitos | 🟢 Baixo | Comportamento surpresa possível |
| Apenas 2 filtered indexes | 🟢 Baixo | Performance com soft-delete |

### 7.2 Mapa de Calor por Módulo

| Módulo | Complexidade | Dívida Técnica | Risco de Mudança | Risco Global |
|---|---|---|---|---|
| Identity | Média | Baixa | Baixo | 🟢 |
| Audit | Baixa | Baixa | Baixo | 🟢 |
| Catalog (Contracts) | Média | Baixa | Baixo | 🟢 |
| Catalog (Graph) | Média | Baixa | Baixo | 🟢 |
| Catalog (Portal) | Baixa | Baixa | Baixo | 🟢 |
| Change Intelligence | Média | Baixa | Baixo | 🟢 |
| Workflow | Média | Baixa | Baixo | 🟢 |
| Promotion | Baixa | Baixa | Baixo | 🟢 |
| Ruleset | Baixa | Baixa | Baixo | 🟢 |
| **Configuration** | Baixa | **Média** | **Médio** | 🟡 |
| Governance | Média | Baixa | Baixo | 🟢 |
| **Notifications** | Baixa | **Média** | **Médio** | 🟡 |
| **AI Governance** | **Alta** | **Alta** | **Alto** | 🔴 |
| AI Orchestration | Baixa | Baixa | Baixo | 🟢 |
| External AI | Baixa | Baixa | Baixo | 🟢 |
| Cost Intelligence | Média | Baixa | Baixo | 🟢 |
| Runtime Intelligence | Baixa | Baixa | Baixo | 🟢 |
| Incidents | Média | Baixa | Baixo | 🟢 |
| Automation | Baixa | Baixa | Baixo | 🟢 |
| Reliability | Baixa | Baixa | Baixo | 🟢 |

---

## 8. Isolamento entre Módulos

### 8.1 Mecanismos de Isolamento

| Mecanismo | Estado |
|---|---|
| DbContext por bounded context | ✅ Cada módulo tem o(s) seu(s) DbContext(s) |
| ConfigurationsNamespace filtering | ✅ Configs não "vazam" entre contextos |
| Outbox pattern para comunicação | ✅ Domain events → messages (não direct DB queries) |
| Bases de dados lógicas separadas | ✅ 4 BDs lógicas |
| Foreign keys cross-module | ⚠️ Referências por ID (não FK real) |

### 8.2 Avaliação de Isolamento

| Fronteira | Estado | Detalhe |
|---|---|---|
| Identity ↔ Catalog | ✅ Isolado | Referência via TenantId/UserId (valor, não FK) |
| Catalog ↔ Change | ✅ Isolado | Referência via ServiceId (valor, não FK) |
| Change ↔ Incidents | ✅ Isolado | ChangeCorrelation referencia IncidentId por valor |
| AI ↔ Identity | ✅ Isolado | Referência via UserId/TenantId (valor) |
| AI ↔ Catalog | ✅ Isolado | Knowledge sources referenciam ServiceId por valor |
| Governance ↔ Audit | ⚠️ Parcial | ComplianceReport duplicado em ambos |

### 8.3 Risco de Acoplamento

| Par de Módulos | Risco | Causa |
|---|---|---|
| AI Governance ↔ AI Orchestration | Baixo | Na mesma BD, schemas separados |
| 12 módulos em nextraceone_operations | **Médio** | Partilham BD, risco de colisão de outbox |
| Governance ↔ Audit (ComplianceReport) | **Médio** | Duplicação de conceito |

---

## 9. Avaliação dos 132 Ficheiros de Configuração

### 9.1 Métricas

| Métrica | Valor |
|---|---|
| Total de ficheiros | 132 |
| Média por DbContext | 6.6 |
| Ficheiro mais complexo (estimado) | AiAgent/ContractVersion configurations |
| Padrão predominante | Fluent API (IEntityTypeConfiguration<T>) |
| Data annotations | Não utilizados (apenas Fluent API) |

### 9.2 Qualidade por Critério

| Critério | Avaliação |
|---|---|
| Um ficheiro por entidade | ✅ Sim — 1:1 |
| Conversões explícitas | ✅ Todas as conversões são declarativas |
| Índices definidos | ✅ 353 HasIndex distribuídos |
| MaxLength em strings | ✅ Maioritariamente definido |
| Required/Optional explícito | ⚠️ Nem sempre — usa conventions |
| Table naming | ✅ Snake_case consistente |
| Delete behavior explícito | ❌ Maioritariamente implícito |

### 9.3 Padrões Identificados

| Padrão | Frequência | Exemplo |
|---|---|---|
| Strongly-typed ID conversion | 100+ | `HasConversion(id => id.Value, v => new UserId(v))` |
| Enum→string conversion | 91 | `HasConversion<string>()` |
| Value object conversion | 50+ | `HasConversion(e => e.Value, v => Email.Create(v))` |
| Owned entity | 2+ | `builder.OwnsOne(x => x.Signature)` |
| Composite index | 46 | `HasIndex(x => new { x.TenantId, x.Name })` |
| Unique index | 42 | `.IsUnique()` |

---

## 10. Recomendações de Manutenibilidade

### 🔴 Prioridade Alta

| # | Ação | Justificação |
|---|---|---|
| 1 | Criar guia de onboarding para persistência | Sem documentação, onboarding demora |
| 2 | Criar checklist de nova entidade | Garantir consistência em novas entidades |
| 3 | Resolver dívida técnica do AiGovernanceDbContext | 7 migrations + 19+ entidades = alto risco |
| 4 | Avaliar split de nextraceone_operations | 12 DbContexts é excessivo para 1 BD |

### 🟡 Prioridade Média

| # | Ação | Justificação |
|---|---|---|
| 5 | Padronizar conversões enum (string vs int) | Inconsistência afeta previsibilidade |
| 6 | Tornar delete behaviors explícitos em todas as relações | Evitar comportamento surpresa |
| 7 | Implementar DesignTimeDbContextFactory | Facilitar tooling de migrations |
| 8 | Adicionar testes de integridade de schema | Verificar que todas as entidades têm config |

### 🟢 Prioridade Baixa

| # | Ação | Justificação |
|---|---|---|
| 9 | Adicionar comentários XML em configs complexas | Explicar decisões não óbvias |
| 10 | Documentar estratégia de outbox table naming | Prevenir colisões futuras |
| 11 | Criar diagrama ER por base de dados lógica | Visualização para onboarding |
| 12 | Implementar archetype/template para nova entidade | Acelerar desenvolvimento |

---

## 11. Métricas de Manutenibilidade

### 11.1 Métricas Positivas

| Métrica | Valor | Benchmark |
|---|---|---|
| Consistência de padrões | ~90% | > 80% é bom |
| Isolamento entre módulos | ~95% | > 90% é excelente |
| Cobertura de índices | 353/~130 entidades = 2.7 avg | > 2.0 é adequado |
| Cobertura de strongly-typed IDs | ~100% | 100% é ideal |

### 11.2 Métricas de Atenção

| Métrica | Valor | Benchmark |
|---|---|---|
| Filtered indexes | 2/353 = 0.6% | > 10% seria ideal |
| Check constraints | 0 | > 0 para campos críticos |
| Migrations com dívida | 2/29 = 7% | 0% ideal |
| DbContexts sem migrations | 2/20 = 10% | 0% ideal |
| Delete behaviors explícitos | ~14/total = ~10% | > 50% seria ideal |

---

## Referências

| Artefacto | Localização |
|---|---|
| Estrutura de módulos | `src/modules/` |
| Base class | `src/platform/NexTraceOne.SharedKernel/Persistence/NexTraceDbContextBase.cs` |
| Interceptors | `src/platform/NexTraceOne.SharedKernel/Persistence/Interceptors/` |
| Entity configurations (exemplo) | `src/modules/identityaccess/Infrastructure/Persistence/Configurations/` |
| Migrations (exemplo) | `src/modules/identityaccess/Infrastructure/Persistence/Migrations/` |
| Seeds | `src/platform/NexTraceOne.ApiHost/SeedData/` |

---

*Relatório gerado como parte da auditoria modular de governança do NexTraceOne.*
