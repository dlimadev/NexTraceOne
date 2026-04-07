> **⚠️ ARCHIVED — April 2026**: Este documento foi gerado como análise pontual de gaps. Muitos dos gaps aqui listados já foram resolvidos. Para o estado atual, consultar `docs/CONSOLIDATED-GAP-ANALYSIS-AND-ACTION-PLAN.md` e `docs/IMPLEMENTATION-STATUS.md`.

# NexTraceOne — Seed Strategy Gaps
**Forensic Analysis | June 2026**

---

## 1. Estado Resumido

A estratégia de seed tem um problema **CRITICAL**: o orquestrador de seed referencia 7 ficheiros SQL mas apenas 1 existe no disco. A proteção por ambiente (`IsDevelopment()`) está correcta mas as falhas são silenciosas.

---

## 2. Infraestrutura de Seed Existente

### Ficheiros relevantes:
- `src/platform/NexTraceOne.ApiHost/DevelopmentSeedDataExtensions.cs` — orquestrador
- `src/platform/NexTraceOne.ApiHost/SeedData/seed-incidents.sql` — **ÚNICO EXISTENTE** (4.9KB)
- `src/modules/configuration/NexTraceOne.Configuration.Infrastructure/Persistence/ConfigurationDefinitionSeeder.cs` — seeder de feature flags
- `src/modules/operationalintelligence/NexTraceOne.OperationalIntelligence.Infrastructure/Incidents/Persistence/SeedData/IncidentSeedData.cs` — seed helper C#

### Proteção por ambiente:
- `DevelopmentSeedDataExtensions.cs` usa `app.Environment.IsDevelopment()` guard — **CORRECTO**
- Seed não executa em Staging ou Production — **CORRECTO**
- ConfigurationDefinitionSeeder não tem guard de ambiente explícito — **RISCO** (verificar se é chamado apenas em Development)

---

## 3. Gaps Críticos

### 3.1 6 Ficheiros SQL Referenciados Não Existem
- **Severidade:** CRITICAL
- **Classificação:** BROKEN
- **Ficheiros em falta:**
  - `src/platform/NexTraceOne.ApiHost/SeedData/seed-identity.sql` — **MISSING**
  - `src/platform/NexTraceOne.ApiHost/SeedData/seed-catalog.sql` — **MISSING**
  - `src/platform/NexTraceOne.ApiHost/SeedData/seed-changegovernance.sql` — **MISSING**
  - `src/platform/NexTraceOne.ApiHost/SeedData/seed-audit.sql` — **MISSING**
  - `src/platform/NexTraceOne.ApiHost/SeedData/seed-aiknowledge.sql` — **MISSING**
  - `src/platform/NexTraceOne.ApiHost/SeedData/seed-governance.sql` — **MISSING**
- **Comportamento actual:** O orquestrador loga warning e continua sem erro visível — falha **silenciosa**
- **Impacto:** Novos developers que façam setup local não terão dados de seed para 6 dos 7 módulos. Apenas incidents terá dados iniciais.
- **Evidência:** `DevelopmentSeedDataExtensions.cs` linhas 21-30 (array de targets), verificação de existência dos 7 ficheiros no disco

---

## 4. Gaps de Estratégia por Ambiente

### 4.1 Development
| Item | Estado |
|---|---|
| Guard `IsDevelopment()` | ✅ Presente |
| Seed SQL idempotente | ⚠ Apenas seed-incidents.sql verificável |
| Seed para todos os módulos core | ❌ 6 de 7 ficheiros SQL em falta |
| ConfigurationDefinitionSeeder | ⚠ Funciona mas guard de ambiente não explícito |
| Dados suficientes para desenvolver e testar | ❌ Insuficiente sem 6 seeds |

### 4.2 Staging / Pre-Production
| Item | Estado |
|---|---|
| Seed separado para staging | ❌ Não existe |
| Dados sintéticos para staging | ❌ Não existe |
| Anonimização de dados | ❌ Não existe |
| Guard contra demo data | ✅ `IsDevelopment()` impede seed SQL em staging |

### 4.3 Production
| Item | Estado |
|---|---|
| Seed proibido para dados de demo | ✅ Guard `IsDevelopment()` bloqueia |
| Bootstrap mínimo (admin, tenant, config) | ❌ Não documentado nem implementado |
| Seed de configuração mínima | ⚠ `ConfigurationDefinitionSeeder` pode correr, mas sem documentação de bootstrap |
| Catálogos mínimos (ex: severidades, categorias) | ❌ Não existe estratégia |
| Documentação de inicialização de produção | ❌ Não existe |
| Script de provisioning seguro | ❌ Não existe |

---

## 5. Gaps de Qualidade

### 5.1 Idempotência
- **Severidade:** MEDIUM
- **Classificação:** CONFIG_RISK
- `seed-incidents.sql` — verificar se usa `INSERT ... ON CONFLICT DO NOTHING` ou similar
- Os 6 seeds em falta tornam esta verificação impossível para os restantes módulos

### 5.2 Tenant Awareness
- **Severidade:** MEDIUM
- **Classificação:** INCOMPLETE
- Seeds devem associar dados ao tenant correcto. `seed-incidents.sql` precisa de verificação de tenant_id.
- Sem os outros 6 seeds, impossível auditar tenant awareness para identity, catalog, etc.

### 5.3 Falha Silenciosa
- **Severidade:** HIGH
- **Classificação:** WRONG_DESIGN
- Quando um ficheiro SQL não existe, o orquestrador loga warning mas **continua sem erro**. Num cenário de desenvolvimento, isto pode levar a cenários difíceis de diagnosticar onde o developer pensa que fez setup correcto mas metade dos dados de seed está em falta.

---

## 6. Ações Corretivas Obrigatórias

1. **CRITICAL:** Criar os 6 ficheiros SQL em falta OU remover as referências do array `SeedTargets` em `DevelopmentSeedDataExtensions.cs`
2. **HIGH:** Decidir se a falha silenciosa é aceitável ou se deve ser um erro fatal em Development
3. **HIGH:** Documentar estratégia de bootstrap para produção (admin inicial, tenant, configuração mínima)
4. **MEDIUM:** Verificar se `ConfigurationDefinitionSeeder` tem guard de ambiente
5. **MEDIUM:** Criar documentação de seed strategy por ambiente
6. **MEDIUM:** Verificar idempotência de `seed-incidents.sql`
