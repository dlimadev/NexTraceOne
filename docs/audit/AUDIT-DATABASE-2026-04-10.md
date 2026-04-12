# NexTraceOne — Relatório de Auditoria: Base de Dados
**Data:** 2026-04-10  
**Escopo:** PostgreSQL 16 + EF Core 10 — 27 DbContexts, migrations, configurações de entidade  
**Ficheiros analisados:** ~30+ pastas de migrations, 100+ ficheiros de configuração

---

## Contexto Positivo

Antes dos problemas, vale registar o que está bem:

- Todos os 27 DbContexts têm pastas de migrations com conteúdo (sem migrations vazias)
- `ModelSnapshot` presente em todos os contextos
- Soft delete via `NexTraceDbContextBase` com filtro global
- Cascade rules configuradas correctamente
- Outbox pattern implementado em todos os módulos
- Encryption interceptor activo para campos `[EncryptedField]`
- **189 tabelas** com Row-Level Security (RLS) configurado no PostgreSQL

---

## CRÍTICO

### [C-01] Colisão de Tabela — Dois DbContexts Mapeiam para `chg_promotion_gates`
**Ficheiros:**
- `src/modules/changegovernance/NexTraceOne.ChangeGovernance.Infrastructure/ChangeIntelligence/Persistence/Configurations/PromotionGateConfiguration.cs` (linha 13)
- `src/modules/changegovernance/NexTraceOne.ChangeGovernance.Infrastructure/Promotion/Persistence/Configurations/PromotionGateConfiguration.cs` (linha 13)

**Problema:**  
Duas entidades completamente distintas — `ChangeIntelligence.PromotionGate` e `Promotion.PromotionGate` — de dois DbContexts separados (`ChangeIntelligenceDbContext` e `PromotionDbContext`) estão configuradas para usar a mesma tabela `chg_promotion_gates`.

O comentário na linha 10 do ficheiro Promotion indica que a tabela deveria ser `prm_promotion_gates`, mas o código usa `chg_promotion_gates`.

**Impacto:**
- Conflitos de migração ao executar `dotnet ef database update`
- Corrupção de dados — registos de um contexto sobrescrevem os do outro
- `ChangeTracker` do EF Core pode comportar-se de forma imprevisível
- RLS policies podem estar aplicadas à tabela errada

**Correcção:**

1. Actualizar a configuração do módulo Promotion:
```csharp
// FICHEIRO: Promotion/Persistence/Configurations/PromotionGateConfiguration.cs
// LINHA 13 - ALTERAR:
builder.ToTable("chg_promotion_gates", "changegovernance");
// PARA:
builder.ToTable("prm_promotion_gates", "changegovernance");
```

2. Criar migration no `PromotionDbContext`:
```bash
dotnet ef migrations add RenamePromotionGatesTable \
  --context PromotionDbContext \
  --project src/modules/changegovernance/NexTraceOne.ChangeGovernance.Infrastructure
```

3. Migration deve conter:
```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.RenameTable(
        name: "chg_promotion_gates",
        schema: "changegovernance",
        newName: "prm_promotion_gates",
        newSchema: "changegovernance");
}
```

4. Actualizar RLS policies no `infra/postgres/apply-rls.sql` para referenciar `prm_promotion_gates`

---

## ALTO

### [A-01] TenantId sem `.IsRequired()` em ~30 Configurações de Entidade
**Módulos afectados:** catalog, changegovernance, configuration, governance, identityaccess  

**Problema:**  
~30 ficheiros de configuração EF Core definem `TenantId` como propriedade mas sem `.IsRequired()`, deixando a coluna como `nullable` no schema real da base de dados:

```csharp
// PADRÃO INCORRECTO ENCONTRADO (exemplo):
builder.Property(x => x.TenantId).HasMaxLength(200);

// PADRÃO CORRECTO (referência):
builder.Property(x => x.TenantId).HasMaxLength(200).IsRequired();
```

**Impacto:**
- Colunas `TenantId` são `NULL` na base de dados
- RLS policies que filtram por `tenant_id IS NOT NULL AND tenant_id = current_setting(...)` podem deixar passar registos com `tenant_id = NULL`
- Dados podem ser inseridos sem tenant context — violação de isolamento multi-tenant

**Ficheiros a corrigir:**

*Módulo catalog:*
- `Contracts/Persistence/Configurations/SchemaEvolutionAdviceConfiguration.cs` (linha 57)
- `Contracts/Persistence/Configurations/SemanticDiffResultConfiguration.cs` (linha 42)
- `Contracts/Persistence/Configurations/ContractComplianceGateConfiguration.cs` (linhas 54–55)
- `Contracts/Persistence/Configurations/MarketplaceReviewConfiguration.cs` (linha 34)
- `Contracts/Persistence/Configurations/ContractComplianceResultConfiguration.cs` (linhas 47–48)
- `Contracts/Persistence/Configurations/ContractVerificationConfiguration.cs`
- `Contracts/Persistence/Configurations/ContractChangelogConfiguration.cs`
- `Contracts/Persistence/Configurations/ContractListingConfiguration.cs`
- `Contracts/Persistence/Configurations/ImpactSimulationConfiguration.cs`

*Módulo changegovernance:*
- `ChangeIntelligence/Persistence/Configurations/ReleaseConfiguration.cs` (linha 49)
- `ChangeIntelligence/Persistence/Configurations/ReleaseNotesConfiguration.cs`
- `ChangeIntelligence/Persistence/Configurations/PromotionGateEvaluationConfiguration.cs`
- `ChangeIntelligence/Persistence/Configurations/PromotionGateConfiguration.cs`

*Módulo configuration:*
- `Persistence/Configurations/ContractCompliancePolicyConfiguration.cs`

*Módulo governance:*
- `Persistence/Configurations/ChangeCostImpactConfiguration.cs`
- `Persistence/Configurations/CostAttributionConfiguration.cs`
- `Persistence/Configurations/CustomDashboardConfiguration.cs`
- `Persistence/Configurations/ExecutiveBriefingConfiguration.cs`
- `Persistence/Configurations/PolicyAsCodeDefinitionConfiguration.cs`
- `Persistence/Configurations/ServiceMaturityAssessmentConfiguration.cs`
- `Persistence/Configurations/TeamHealthSnapshotConfiguration.cs`
- `Persistence/Configurations/TechnicalDebtItemConfiguration.cs`
- `Persistence/Configurations/LicenseComplianceReportConfiguration.cs`

*Módulo identityaccess:*
- `Persistence/Configurations/AccessReviewCampaignConfiguration.cs`
- `Persistence/Configurations/DelegationConfiguration.cs`
- `Persistence/Configurations/EnvironmentAccessConfiguration.cs`
- `Persistence/Configurations/EnvironmentConfiguration.cs`
- `Persistence/Configurations/JitAccessRequestConfiguration.cs`
- `Persistence/Configurations/ModuleAccessPolicyConfiguration.cs`

**Correcção (aplicar em todos os ficheiros acima):**
```csharp
// ANTES:
builder.Property(x => x.TenantId).HasMaxLength(200);

// DEPOIS:
builder.Property(x => x.TenantId).HasMaxLength(200).IsRequired();
```

Após a correcção, gerar uma migration por DbContext afectado para tornar as colunas `NOT NULL`. Verificar se existem registos com `TenantId = NULL` antes de aplicar em produção — pode necessitar de script de backfill.

---

## MÉDIO

### [M-01] Comentário de Tabela Diverge do Código
**Ficheiro:** `src/modules/changegovernance/NexTraceOne.ChangeGovernance.Infrastructure/Promotion/Persistence/Configurations/PromotionGateConfiguration.cs` (linha 10)  

Comentário na linha 10 indica `prm_promotion_gates`, mas a configuração na linha 13 usa `chg_promotion_gates`. Esta discrepância é o indicador claro do problema C-01.

**Correcção:** Resolver C-01 primeiro; este issue desaparece como consequência.

---

## Verificações Adicionais Recomendadas

### Verificar RLS com TenantId NULL
Após correcção do A-01, executar no PostgreSQL de desenvolvimento:
```sql
SELECT table_name, COUNT(*) as null_tenant_count
FROM information_schema.columns c
JOIN (
  SELECT schemaname || '.' || tablename as table_name
  FROM pg_tables
  WHERE schemaname = 'changegovernance' OR schemaname = 'catalog'
) t ON true
WHERE c.column_name = 'tenant_id' AND c.is_nullable = 'YES'
GROUP BY table_name;
```

### Verificar Colisão de Tabela
```sql
SELECT table_schema, table_name, COUNT(*) 
FROM information_schema.tables 
WHERE table_name IN ('chg_promotion_gates', 'prm_promotion_gates')
GROUP BY table_schema, table_name;
```
