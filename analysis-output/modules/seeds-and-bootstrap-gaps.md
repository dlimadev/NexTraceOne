> **⚠️ ARCHIVED — April 2026**: Este documento foi gerado como análise pontual de gaps. Muitos dos gaps aqui listados já foram resolvidos. Para o estado atual, consultar `docs/CONSOLIDATED-GAP-ANALYSIS-AND-ACTION-PLAN.md` e `docs/IMPLEMENTATION-STATUS.md`.

# Seeds & Bootstrap — Gaps, Erros e Pendências

## 1. Estado resumido
Seed strategy tem 1 gap CRITICAL (6 ficheiros SQL em falta) e múltiplas lacunas de estratégia para produção. Detalhe completo em `00-seed-strategy-gaps.md`.

## 2. Gaps críticos

### 2.1 6 de 7 Seed SQL Files Missing
- **Severidade:** CRITICAL
- **Classificação:** BROKEN
- **Ficheiros em falta:**
  - `src/platform/NexTraceOne.ApiHost/SeedData/seed-identity.sql`
  - `src/platform/NexTraceOne.ApiHost/SeedData/seed-catalog.sql`
  - `src/platform/NexTraceOne.ApiHost/SeedData/seed-changegovernance.sql`
  - `src/platform/NexTraceOne.ApiHost/SeedData/seed-audit.sql`
  - `src/platform/NexTraceOne.ApiHost/SeedData/seed-aiknowledge.sql`
  - `src/platform/NexTraceOne.ApiHost/SeedData/seed-governance.sql`
- **Evidência:** `src/platform/NexTraceOne.ApiHost/DevelopmentSeedDataExtensions.cs` referencia 7 targets; apenas `seed-incidents.sql` existe

## 3. Gaps altos

### 3.1 Falha Silenciosa no Seed
- **Severidade:** HIGH
- **Classificação:** WRONG_DESIGN
- Orquestrador loga warning quando ficheiro SQL não existe mas continua sem erro. Developer não percebe que seed falhou.

### 3.2 Bootstrap de Produção Inexistente
- **Severidade:** HIGH
- **Classificação:** INCOMPLETE
- Sem estratégia documentada para: admin inicial, primeiro tenant, configuração mínima, catálogos base.

## 4. Gaps médios

### 4.1 Sem Seed Staging
- **Severidade:** MEDIUM
- **Classificação:** INCOMPLETE
- Sem dados sintéticos para ambientes de staging/pre-production.

### 4.2 ConfigurationDefinitionSeeder sem Guard Verificado
- **Severidade:** MEDIUM
- **Classificação:** CONFIG_RISK
- Verificar se `ConfigurationDefinitionSeeder` tem guard `IsDevelopment()`.

## 5. Itens mock / stub / placeholder
N/A.

## 6-12. Detalhes
Cobertos em `00-seed-strategy-gaps.md`.

## 13. Ações corretivas obrigatórias
1. **CRITICAL:** Criar 6 ficheiros SQL OU remover referências
2. **HIGH:** Documentar e implementar bootstrap de produção
3. **HIGH:** Decidir se falha de seed deve ser erro fatal em Development
4. **MEDIUM:** Criar seed strategy para staging
5. **MEDIUM:** Verificar ConfigurationDefinitionSeeder guard
