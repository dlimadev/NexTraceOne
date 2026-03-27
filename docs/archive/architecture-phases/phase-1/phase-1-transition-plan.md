# Fase 1 — Plano de Transição

**Data:** 2026-03-20  
**Fase:** 1 → 2  
**Relacionado com:** ADR-002, phase-1-domain-foundation.md

---

## 1. O que foi introduzido na Fase 1 (sem quebrar compatibilidade)

### 1.1 IdentityAccess.Domain

| Artefato | Tipo | Compatibilidade |
|---|---|---|
| `EnvironmentProfile` enum | Novo | ✅ Não afeta nada existente |
| `EnvironmentCriticality` enum | Novo | ✅ Não afeta nada existente |
| Campos em `Environment` entity | Extensão | ✅ EF ignora os novos campos; factory method original intocado |
| `TenantEnvironmentContext` | Novo value object | ✅ Não afeta nada existente |
| `EnvironmentUiProfile` | Novo value object | ✅ Não afeta nada existente |
| `EnvironmentPolicy` | Nova entidade (sem EF) | ✅ Não afeta nada existente |
| `EnvironmentIntegrationBinding` | Nova entidade (sem EF) | ✅ Não afeta nada existente |
| `EnvironmentTelemetryPolicy` | Nova entidade (sem EF) | ✅ Não afeta nada existente |

### 1.2 AIKnowledge.Domain

| Artefato | Tipo | Compatibilidade |
|---|---|---|
| `AiExecutionContext` | Novo value object | ✅ Não afeta nada existente |
| `PromotionRiskAnalysisContext` | Novo value object | ✅ Não afeta nada existente |
| `EnvironmentComparisonContext` | Novo value object | ✅ Não afeta nada existente |
| `RiskFinding` | Novo value object | ✅ Não afeta nada existente |
| `RegressionSignal` | Novo value object | ✅ Não afeta nada existente |
| `ReadinessAssessment` | Novo value object | ✅ Não afeta nada existente |
| Referência a IdentityAccess.Domain | Nova dependência | ✅ Sem ciclos — IA depende de domínio de identidade |

### 1.3 IdentityAccess.Application.Abstractions

| Artefato | Tipo | Compatibilidade |
|---|---|---|
| `IEnvironmentContextAccessor` | Nova interface | ✅ Apenas contrato, sem implementação |
| `ITenantEnvironmentContextResolver` | Nova interface | ✅ Apenas contrato, sem implementação |
| `IEnvironmentProfileResolver` | Nova interface | ✅ Apenas contrato, sem implementação |

### 1.4 AIKnowledge.Application.Abstractions

| Artefato | Tipo | Compatibilidade |
|---|---|---|
| `IAIContextBuilder` | Nova interface | ✅ Apenas contrato, sem implementação |
| `IPromotionRiskContextBuilder` | Nova interface | ✅ Apenas contrato, sem implementação |

### 1.5 Testes

| Módulo | Novos testes | Cobertura |
|---|---|---|
| IdentityAccess.Tests | +31 | Environment Phase 1 + TenantEnvironmentContext |
| AIKnowledge.Tests | +24 | AiExecutionContext, PromotionRisk, RiskFinding, RegressionSignal, ReadinessAssessment |

---

## 2. O que ainda será migrado na Fase 2+

### 2.1 Fase 2 — Persistência e Migração de Banco

**Prioridade alta (bloqueante para uso real dos novos campos):**

- [ ] Migration `AddEnvironmentProfileFields` — adicionar colunas em `identity_environments`:
  - `profile` (int, não nulo, default 1)
  - `code` (varchar(50), nulo)
  - `description` (varchar(500), nulo)
  - `criticality` (int, não nulo, default 1)
  - `region` (varchar(100), nulo)
  - `is_production_like` (boolean, não nulo, default false)
  - Remover `builder.Ignore()` de `EnvironmentConfiguration`

- [ ] Migration `AddEnvironmentPolicies` — criar tabela `identity_environment_policies`
- [ ] Migration `AddEnvironmentIntegrationBindings` — criar tabela `identity_environment_integration_bindings`
- [ ] Migration `AddEnvironmentTelemetryPolicies` — criar tabela `identity_environment_telemetry_policies`

**Implementações requeridas:**
- [ ] `EnvironmentConfiguration` EF — adicionar mapeamento dos novos campos
- [ ] `EnvironmentPolicyConfiguration` EF — nova configuração
- [ ] `EnvironmentIntegrationBindingConfiguration` EF — nova configuração
- [ ] `EnvironmentTelemetryPolicyConfiguration` EF — nova configuração
- [ ] Repositories para as novas entidades
- [ ] Seed data para ambientes existentes (migrar `slug` para inferir `profile`)

### 2.2 Fase 2 — Implementação das Abstrações

- [ ] Implementar `IEnvironmentContextAccessor` — middleware ou accessor de contexto HTTP
- [ ] Implementar `ITenantEnvironmentContextResolver` — usando `IEnvironmentRepository`
- [ ] Implementar `IEnvironmentProfileResolver` — usando resolver acima
- [ ] Implementar `IAIContextBuilder` — usando `ICurrentTenant`, `ICurrentUser`, resolver de ambiente
- [ ] Implementar `IPromotionRiskContextBuilder` — usando `IAIContextBuilder` + resolução dos ambientes

### 2.3 Fase 2 — APIs de Gerenciamento de Ambientes

- [ ] Endpoint para criar ambiente com `EnvironmentProfile` (`POST /environments`)
- [ ] Endpoint para atualizar perfil de ambiente (`PATCH /environments/{id}/profile`)
- [ ] Endpoint para listar ambientes do tenant com contexto completo

### 2.4 Fase 3 — Refatoração dos Módulos Operacionais

Módulos que ainda usam `environment: string` e precisam migrar para `EnvironmentId`:

| Módulo | Entidade | Campo atual | Migração |
|---|---|---|---|
| ChangeGovernance | `Release` | `Environment: string` | Adicionar `EnvironmentId`, deprecar `Environment` |
| ChangeGovernance | `PromotionRequest` | sem TenantId e EnvironmentId | Adicionar ambos |
| ChangeGovernance | `DeploymentEnvironment` | Entidade duplicada sem TenantId | Unificar com IdentityAccess.Environment |
| OperationalIntelligence | `IncidentRecord` | `Environment: string` | Adicionar `EnvironmentId`, deprecar `Environment` |
| Catalog | `ApiAsset`, `ServiceAsset` | sem TenantId | Adicionar TenantId |
| Catalog | `ContractVersion`, `GraphSnapshot` | sem TenantId | Adicionar TenantId |

### 2.5 Fase 3 — IA Real com Contexto

- [ ] Integrar `IAIContextBuilder` nos handlers de IA existentes
- [ ] Implementar `IPromotionRiskContextBuilder` com lógica de resolução real
- [ ] Conectar `EnvironmentTelemetryPolicy.AllowCrossEnvironmentComparison` ao filtro de fontes de IA
- [ ] Implementar análise de `RegressionSignal` usando telemetria real
- [ ] Implementar geração de `RiskFinding` usando incidentes e contratos reais
- [ ] Implementar `ReadinessAssessment` com persistência e API de consulta

### 2.6 Fase 4 — Frontend Contextual

- [ ] API endpoint `GET /api/v1/environments/active-context` retornando `TenantEnvironmentContext`
- [ ] API endpoint `GET /api/v1/environments/ui-profile` retornando `EnvironmentUiProfile`
- [ ] Frontend: consumir `EnvironmentUiProfile` do backend (não hardcodar cores/avisos)
- [ ] Frontend: `WorkspaceSwitcher` orientado por ambientes reais do tenant
- [ ] Frontend: exibir badge de ambiente com cor semântica do backend
- [ ] Frontend: bloquear ações destrutivas em ambientes `IsProductionLike`

---

## 3. Pontos Sensíveis

### 3.1 `EnvironmentConfiguration` tem `Ignore()` provisório

Os 6 campos novos de `Environment` têm `builder.Ignore()` no `EnvironmentConfiguration`. Isso é intencional na Fase 1 — os campos existem no domínio mas não são persistidos. **Remover estes `Ignore()` antes de adicionar a migration causará erro de schema mismatch.**

**Ação na Fase 2:** Gerar migration, atualizar configuração, remover `Ignore()` de forma coordenada.

### 3.2 `DeploymentEnvironment` em ChangeGovernance é um duplicado

O módulo ChangeGovernance possui uma entidade `DeploymentEnvironment` que é conceitualmente um ambiente mas não está vinculada a `TenantId` nem ao `Environment` do IdentityAccess. **Esta é a maior dívida técnica de domínio identificada.**

**Estratégia recomendada:** na Fase 3, o `PromotionRequest` deve referenciar `EnvironmentId` do IdentityAccess. `DeploymentEnvironment` deve ser gradualmente substituído por lookups ao `Environment` correto.

### 3.3 Dependência AIKnowledge.Domain → IdentityAccess.Domain

Esta dependência foi introduzida na Fase 1 para que os contextos de IA possam referenciar `TenantId`, `EnvironmentId` e `EnvironmentProfile`. **É uma dependência válida** (IA precisa saber sobre ambientes), mas cria um coupling que deve ser monitorado.

**Alternativa futura:** criar tipos compartilhados em BuildingBlocks.Core (ex.: um DTO de contexto) para remover esta dependência direta. Avaliar na Fase 4.

### 3.4 Novos campos de `Environment` sem dados históricos

Ambientes já criados terão `Profile = Development` e `Criticality = Low` por default quando a migration for aplicada. **Será necessário um script de migração de dados** para inferir o perfil correto a partir do slug ou nome do ambiente existente.

---

## 4. Impactos Previstos por Área

### Backend
- **Fase 2:** Migrations seguras para os novos campos. Implementação das interfaces de contexto.
- **Fase 3:** Refatoração incremental dos módulos operacionais — adicionar campos, manter backward compat por um ciclo.

### Frontend
- **Fase 2:** Nenhuma mudança necessária — backend mantém mesma interface.
- **Fase 3-4:** Consumir `EnvironmentUiProfile` do backend. Remover hardcode de ambientes.
- **Compatibilidade:** O `WorkspaceSwitcher` atual pode continuar funcionando durante a transição.

### Banco de Dados
- **Fase 2:** 4 migrations independentes (uma por entidade nova).
- **Fase 3:** Adicionar colunas nullable `environment_id` nas tabelas operacionais; depois tornar obrigatórias.

### IA
- **Fase 2:** Implementar builders de contexto.
- **Fase 3:** Conectar à telemetria real para análise de regressão.
- **Sem impacto imediato:** Os handlers de IA existentes continuam funcionando como estão.

### Integrações
- **Fase 3:** `EnvironmentIntegrationBinding` permitirá que cada integração tenha config por ambiente.
- **Impacto:** Clientes de integração precisarão incluir `EnvironmentId` nas chamadas de ingestion.

### Telemetria
- **Fase 3:** `EnvironmentTelemetryPolicy` governará coleta e retenção por ambiente.
- **Impacto:** Fontes de telemetria precisarão incluir `TenantId + EnvironmentId` nos contextos de coleta.

---

## 5. Confirmação de Preservação de Comportamento

✅ Build: `0 errors`, `730 warnings` (warnings pré-existentes, nenhum novo)  
✅ IdentityAccess.Tests: 217 passed (186 originais + 31 novos)  
✅ AIKnowledge.Tests: 204 passed (180 originais + 24 novos)  
✅ Todos os outros módulos compilam sem erro  
✅ Nenhum handler existente foi alterado  
✅ Nenhuma API existente foi alterada  
✅ Nenhuma migration existente foi alterada  
✅ Nenhuma funcionalidade existente foi removida  
