# Fase 1 — Fundação de Domínio: Modelo e Contratos-Base

**Data:** 2026-03-20  
**Status:** Completo  
**Fase:** 1 de N  
**ADR relacionada:** ADR-002

---

## 1. Objetivo

Estabelecer a fundação de domínio necessária para que o NexTraceOne evolua para um modelo verdadeiramente multi-tenant, environment-aware e AI-context-aware — sem quebrar o comportamento atual.

---

## 2. Entidades Criadas

### 2.1 Extensão da entidade `Environment`

**Módulo:** `NexTraceOne.IdentityAccess.Domain`  
**Arquivo:** `Entities/Environment.cs`  
**Status EF:** Novos campos ignorados por `EnvironmentConfiguration` (Fase 2 adicionará migration)

| Campo | Tipo | Descrição |
|---|---|---|
| `Profile` | `EnvironmentProfile` | Perfil operacional base (Development, Production, etc.) |
| `Code` | `string?` | Código curto livre do tenant (ex.: "PROD-BR", "QA-EU") |
| `Description` | `string?` | Descrição informativa do ambiente |
| `Criticality` | `EnvironmentCriticality` | Nível de proteção e rigor (Low → Critical) |
| `Region` | `string?` | Região geográfica ou datacenter |
| `IsProductionLike` | `bool` | Auto-inferido pelo perfil, pode ser sobrescrito |

**Novos métodos:**
- `Create(tenantId, name, slug, sortOrder, now, profile, criticality?, code?, description?, region?, isProductionLike?)` — sobrecarga com perfil
- `UpdateProfile(profile, criticality, isProductionLike?)` — atualiza perfil operacional
- `UpdateLocationInfo(code?, region?, description?)` — atualiza informações de localização

---

## 3. Enums Criados

### 3.1 `EnvironmentProfile`

**Módulo:** `NexTraceOne.IdentityAccess.Domain`  
**Arquivo:** `Enums/EnvironmentProfile.cs`

```
Development         = 1  (menor restrição, testes rápidos)
Validation          = 2  (QA, testes automatizados)
Staging             = 3  (comportamento próximo de produção)
Production          = 4  (máxima restrição, auditoria completa)
Sandbox             = 5  (experimentação isolada)
DisasterRecovery    = 6  (standby/failover de produção)
Training            = 7  (demonstração com dados fictícios)
UserAcceptanceTesting = 8 (validação pelo negócio)
PerformanceTesting  = 9  (testes de stress e carga)
```

**Regra:** `IsProductionLike` é inferido como `true` para `Production` e `DisasterRecovery`.

### 3.2 `EnvironmentCriticality`

**Módulo:** `NexTraceOne.IdentityAccess.Domain`  
**Arquivo:** `Enums/EnvironmentCriticality.cs`

```
Low      = 1  (desenvolvimento, sem impacto externo)
Medium   = 2  (validação, impacto limitado)
High     = 3  (staging/UAT com visibilidade externa)
Critical = 4  (produção, DR — impacto direto em clientes)
```

---

## 4. Value Objects Criados

### 4.1 `TenantEnvironmentContext`

**Módulo:** `NexTraceOne.IdentityAccess.Domain`  
**Arquivo:** `ValueObjects/TenantEnvironmentContext.cs`

Representa o contexto operacional resolvido: a combinação de `TenantId + EnvironmentId + Profile + Criticality + IsProductionLike + IsActive`.

**Métodos de negócio:**
- `From(environment)` — fábrica a partir de entidade resolvida
- `Create(...)` — fábrica a partir de valores explícitos (cache/deserialização)
- `RequiresProductionSafeguards()` — verdadeiro se `IsProductionLike || Criticality >= High`
- `AllowsDeepAiAnalysis()` — verdadeiro para ambientes não produtivos ativos
- `IsPreProductionCandidate()` — verdadeiro para Staging, UAT ou criticidade alta não-produtiva

**Igualdade:** baseada em `TenantId + EnvironmentId` (os mesmos dois ambientes são o mesmo contexto independente do estado).

### 4.2 `EnvironmentUiProfile`

**Módulo:** `NexTraceOne.IdentityAccess.Domain`  
**Arquivo:** `ValueObjects/EnvironmentUiProfile.cs`

Contrato do backend para o frontend. Determina:
- `BadgeColor` — "green" (dev), "yellow" (validation), "orange" (staging/uat), "red" (prod/dr), "blue" (sandbox/training)
- `ShowProtectionWarning` — true para ambientes `IsProductionLike`
- `AllowDestructiveActions` — false para ambientes `IsProductionLike`
- `AiAssistanceAvailable` — true para ambientes ativos

---

## 5. Entidades de Política Criadas (sem persistência na Fase 1)

> Estas entidades existem no domínio mas não têm configuração EF ainda. Persistência na Fase 2.

### 5.1 `EnvironmentPolicy`

Política associada a um ambiente de um tenant.

**Campos:** `TenantId`, `EnvironmentId`, `PolicyType` (string), `Name`, `ConfigurationJson`, `IsActive`, `CreatedAt`, `UpdatedAt`

**Tipos conhecidos:** `promotion_approval`, `freeze_window`, `alert_escalation`, `deploy_quality_gate`

### 5.2 `EnvironmentIntegrationBinding`

Vínculo entre uma integração externa e um ambiente específico.

**Campos:** `TenantId`, `EnvironmentId`, `IntegrationType`, `ConnectorId` (Guid), `BindingConfigJson`, `IsActive`, `CreatedAt`

**Tipos conhecidos:** `observability`, `alerting`, `ci_cd`, `event_broker`, `incident_management`, `code_quality`

### 5.3 `EnvironmentTelemetryPolicy`

Política de telemetria para um ambiente. Usada pela IA para determinar disponibilidade e escopo de dados.

**Campos:** `TenantId`, `EnvironmentId`, `RetentionDays`, `VerbosityLevel`, `AllowCrossEnvironmentComparison`, `RequiresDataAnonymization`, `IsBaselineSource`, `CreatedAt`

**Defaults automáticos por perfil:** produção → retenção 90 dias, anonymization obrigatória, é fonte de baseline. Dev → verbosity debug, 30 dias.

---

## 6. Fundação de IA (AIKnowledge.Domain)

### 6.1 `AiExecutionContext`

Contexto de execução de toda operação de IA. Garante que a IA opere sempre com contexto explícito.

**Componentes:**
- `TenantId`, `EnvironmentId`, `EnvironmentProfile`, `IsProductionLikeEnvironment`
- `AiUserContext` (UserId, UserName, Persona, Roles)
- `AllowedDataScopes` — controlado pelo backend (não expansível pelo frontend)
- `ModuleContext` — módulo que acionou a IA
- `AiTimeWindow` (From, To) — janela de análise histórica
- `AiReleaseContext?` — contexto de release quando aplicável

**Escopos disponíveis (`AiDataScope`):** `telemetry`, `incidents`, `changes`, `contracts`, `topology`, `runbooks`, `cross_environment_comparison`, `promotion_analysis`

### 6.2 `PromotionRiskAnalysisContext`

Contexto para análise de risco antes de uma promoção entre ambientes.

**Campos-chave:** `SourceEnvironmentId + SourceProfile`, `TargetEnvironmentId + TargetProfile`, `ServiceName`, `Version`, `ReleaseId?`, `ObservationWindow`

**Validação:** source ≠ target.

### 6.3 `EnvironmentComparisonContext`

Contexto para comparação entre dois ambientes do mesmo tenant (ex.: QA vs PROD).

**Dimensões de comparação:** `Performance`, `ErrorRate`, `Availability`, `ContractCompatibility`, `Topology`, `IncidentPatterns`, `TestCoverage`

### 6.4 `RiskFinding`

Achado de risco rastreável com evidências.

**Campos:** `FindingId`, `Category`, `Severity`, `Title`, `Description`, `AffectedService?`, `EvidenceReferences`, `DetectedAt`, `SuggestedAction?`

**Categorias:** `PerformanceRegression`, `ErrorRateIncrease`, `ContractBreakingChange`, `DependencyRisk`, `DataAnomaly`, `SecurityConcern`, `InsufficientTestCoverage`, `ConfigurationGap`, `UnresolvedIncident`

**Severidades:** `Info`, `Warning`, `High`, `Critical`

### 6.5 `RegressionSignal`

Sinal de regressão mensurável: valor atual vs baseline.

**Campos:** `ServiceName`, `MetricName`, `CurrentValue`, `BaselineValue`, `DeltaPercent`, `Unit`, `IsDegradation`, `Intensity`, `DetectedAt`

**Intensidades:** `Negligible` (<5%), `Minor` (5-15%), `Moderate` (15-30%), `Significant` (30-50%), `Severe` (>50%)

**Suporte a métricas "higher is better"** (ex.: throughput).

### 6.6 `ReadinessAssessment`

Avaliação de prontidão para promoção. Resultado final da análise da IA.

**Campos:** `AssessmentId`, `ReadinessScore` (0-100), `Recommendation`, `RiskFindings`, `RegressionSignals`, `ExecutiveSummary`, `AssessedAt`

**Score:** calculado automaticamente. Penalidades: Critical=-40, High=-15, Warning=-5, Severe regression=-10, Significant=-5.

**Recomendação:** `Promote` (≥80), `PromoteWithCaution` (≥50), `Block` (<50 ou qualquer Critical)

---

## 7. Abstrações de Contexto (Application Layer)

### 7.1 Em `IdentityAccess.Application.Abstractions`

| Interface | Propósito |
|---|---|
| `IEnvironmentContextAccessor` | Acesso ao ambiente ativo na requisição (EnvironmentId, Profile, IsProductionLike) |
| `ITenantEnvironmentContextResolver` | Resolução do TenantEnvironmentContext a partir de TenantId + EnvironmentId |
| `IEnvironmentProfileResolver` | Resolução de EnvironmentProfile e IsProductionLike por ambiente |

### 7.2 Em `AIKnowledge.Application.Abstractions`

| Interface | Propósito |
|---|---|
| `IAIContextBuilder` | Construção do AiExecutionContext para a requisição atual ou para tenant/ambiente explícitos |
| `IPromotionRiskContextBuilder` | Construção do PromotionRiskAnalysisContext e EnvironmentComparisonContext |

---

## 8. Distinção: Capacidades Globais vs Operacionais

### Capacidades Globais (independentes de ambiente)
- Identidade e autenticação (`ICurrentTenant`, `ICurrentUser`)
- Catálogo de serviços e contratos (definições, não runtime)
- Modelo de governança e políticas (definições)
- Registry de modelos de IA e políticas de acesso
- Configuração de tenants e seus ambientes

### Capacidades Operacionais por Ambiente (requerem TenantId + EnvironmentId)
- Observabilidade e telemetria runtime
- Incidentes e mitigação
- Topologia e dependências runtime
- FinOps operacional
- Análise de readiness e regressão
- Remediação automatizada
- Execução de IA com escopo de dados

Esta distinção guia o design de APIs, autorização e roteamento de dados nas Fases 2+.

---

## 9. Como a IA se Encaixa no Domínio

A IA não é um módulo isolado — é uma capacidade transversal que opera sobre os dados de todos os módulos, dentro do contexto `{TenantId, EnvironmentId}`.

```
Usuário aciona IA
       ↓
IAIContextBuilder.BuildAsync(moduleContext)
       ↓
AiExecutionContext {TenantId, EnvironmentId, Profile, Scopes, ...}
       ↓
IA analisa dados dentro do escopo autorizado:
  - telemetria do ambiente
  - incidentes do ambiente
  - mudanças e deployments
  - contratos e versões
  - topologia de serviços
       ↓
Resultado contextualizado para o tenant/ambiente
```

Para análise de pré-produção:
```
IPromotionRiskContextBuilder.BuildAsync(tenantId, stagingEnvId, prodEnvId, ...)
       ↓
PromotionRiskAnalysisContext
       ↓
IA analisa: RegressionSignals + RiskFindings
       ↓
ReadinessAssessment { Score, Recommendation, Evidence }
```

---

## 10. Cobertura de Testes

| Módulo | Testes Antes | Testes Após | Novos |
|---|---|---|---|
| IdentityAccess.Tests | 186 | 217 | +31 |
| AIKnowledge.Tests | 180 | 204 | +24 |

**Novos testes cobrem:**
- Extensão da entidade Environment (perfil, criticidade, IsProductionLike por perfil)
- TenantEnvironmentContext (criação, igualdade, métodos de negócio)
- AiExecutionContext (escopos, comparação cross-environment)
- PromotionRiskAnalysisContext e EnvironmentComparisonContext (validações)
- RiskFinding e RegressionSignal (criação, cálculos)
- ReadinessAssessment (score, recomendação para todos os cenários)
