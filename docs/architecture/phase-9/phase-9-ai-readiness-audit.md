# Phase 9 — AI Readiness Audit

**Produto:** NexTraceOne  
**Fase:** 9 — Consolidation, Adherence Audit & 100% Validation  
**Data:** 2026-03-21

---

## Visão Geral

Este documento audita em profundidade a prontidão do sistema para operações de IA governadas, com foco em:
- Isolamento de contexto e tenant
- Capacidade de análise não-produtiva
- Comparação de ambientes
- Assessment de promoção
- Fail-safe e auditabilidade
- Governança e fronteiras do módulo AI

---

## 1. AIExecutionContext — Status

### Componentes Existentes

| Componente | Localização | Status |
|---|---|---|
| `AiExecutionContext` VO | `AIKnowledge.Domain/Orchestration/Context/AiExecutionContext.cs` | ✅ Completo |
| `AiUserContext` | Incluído em AiExecutionContext | ✅ Completo |
| `AiTimeWindow` | Incluído em AiExecutionContext | ✅ Completo |
| `AiReleaseContext` | Opcional em AiExecutionContext | ✅ Completo |
| `IAIContextBuilder` | `AIKnowledge.Application/Abstractions/IAIContextBuilder.cs` | ✅ Interface definida |
| `AIContextBuilder` (implementação) | `AIKnowledge.Infrastructure/Context/AIContextBuilder.cs` | ✅ Implementado |

### Análise do AIContextBuilder

O `AIContextBuilder` constrói `AiExecutionContext` com:
- `TenantId` resolvido de `ICurrentTenant`
- `EnvironmentId` resolvido de `IEnvironmentContextAccessor` (com fallback para `EnvironmentId.Empty` se não resolvido)
- `EnvironmentProfile` resolvido do accessor (com fallback para `Development`)
- `IsProductionLikeEnvironment` determinado pelo accessor
- `AllowedDataScopes` determinados programaticamente com base no perfil e nas permissões do utilizador
- Método `BuildForAsync(TenantId, EnvironmentId)` que usa `ITenantEnvironmentContextResolver` para resolver contexto a partir de IDs específicos

**Gap identificado:** O `AIContextBuilder` utiliza o perfil de `IEnvironmentContextAccessor` para determinar `IsProductionLike`. Se o campo `IsProductionLike` não estiver persistido na BD (ver F-01), o accessor retorna o valor default do código, não da BD.

---

## 2. Capacidade de Análise Não-Produtiva (AnalyzeNonProdEnvironment)

### Implementação

| Aspecto | Status | Detalhes |
|---|---|---|
| Handler existe | ✅ | `AnalyzeNonProdEnvironment.cs` — Command, Validator, Handler, Response |
| TenantId obrigatório | ✅ | `Guard.Against.NullOrWhiteSpace(request.TenantId)` + Validator `NotEmpty()` |
| EnvironmentId obrigatório | ✅ | `Guard.Against.NullOrWhiteSpace(request.EnvironmentId)` + Validator `NotEmpty()` |
| EnvironmentProfile passado como contexto | ✅ | Campo `EnvironmentProfile` no Command — incluído no grounding |
| TenantId incluído no grounding context | ✅ | `sb.AppendLine($"Tenant: {request.TenantId}")` |
| ServiceFilter opcional | ✅ | Permite análise de todos ou de serviços específicos |
| ObservationWindowDays validado (1-90) | ✅ | `InclusiveBetween(1, 90)` |
| Resposta inclui TenantId + EnvironmentId | ✅ | Response record inclui ambos para rastreabilidade |
| CorrelationId único por execução | ✅ | `Guid.NewGuid().ToString()` no início do Handle |
| Fail-safe se provider indisponível | ✅ | `Error.Business("AIKnowledge.Provider.Unavailable", ...)` |

### Gaps Críticos

| Gap | Detalhe | Risco |
|---|---|---|
| **Sem validação server-side que ambiente é não-produtivo** | Handler aceita `EnvironmentProfile = "production"` sem rejeitar. Não há lista de profiles bloqueados nem validação de `IsProductionLike`. | Análise IA pode ser executada em ambiente produtivo, potencialmente expondo dados sensíveis ou produzindo recomendações que interferem com produção. Risco de segurança moderado a alto. |
| **EnvironmentProfile passado como string livre** | O `EnvironmentProfile` vem do cliente como string. Não é validado contra o enum `EnvironmentProfile` do domain. Strings arbitrárias são aceites. | Grounding context pode receber perfis inválidos, levando a análises com contexto errado. |

### Recomendação

```csharp
// No Validator de AnalyzeNonProdEnvironment.Command:
private static readonly HashSet<string> BlockedProfiles = 
    new(StringComparer.OrdinalIgnoreCase) { "production", "disasterrecovery" };

RuleFor(x => x.EnvironmentProfile)
    .NotEmpty()
    .Must(p => !BlockedProfiles.Contains(p))
    .WithMessage("Analysis of production or disaster recovery environments is not permitted via this endpoint.");
```

---

## 3. Capacidade de Comparação de Ambientes (CompareEnvironments)

### Implementação

| Aspecto | Status | Detalhes |
|---|---|---|
| Handler existe | ✅ | `CompareEnvironments.cs` — completo |
| TenantId obrigatório | ✅ | Guard + Validator |
| SubjectEnvironmentId obrigatório | ✅ | Validator NotEmpty |
| ReferenceEnvironmentId obrigatório | ✅ | Validator NotEmpty |
| Ambientes diferentes validados | ✅ | `SubjectEnvironmentId != ReferenceEnvironmentId` |
| Grounding include "same tenant" | ✅ | `"IMPORTANT: Both environments belong to the same tenant. Comparison is always intra-tenant."` |
| ComparisonDimensions configuráveis | ✅ | contracts, telemetry, incidents, topology, deployments |
| Resposta inclui PromotionRecommendation | ✅ | SAFE_TO_PROMOTE / REVIEW_REQUIRED / BLOCK_PROMOTION |
| TenantId na resposta | ✅ | Response record |

### Gaps

| Gap | Detalhe | Risco |
|---|---|---|
| **Sem DB lookup para confirmar tenant ownership dos environments** | Handler recebe TenantId + dois EnvironmentIds mas não verifica via BD que ambos os environments pertencem ao TenantId. | Actor malicioso pode passar EnvironmentId de outro tenant. O isolamento é apenas por contexto/grounding. Risco médio (requer TenantId válido + conhecimento de EnvironmentId alheio). |
| **Sem validação que um ambiente é base (prod-like)** | Pode comparar dois ambientes não-produtivos sem aviso. | Comparações irrelevantes podem ser feitas sem orientação contextual. Risco baixo. |

---

## 4. Assessment de Promoção (AssessPromotionReadiness)

### Implementação

| Aspecto | Status | Detalhes |
|---|---|---|
| Handler existe | ✅ | `AssessPromotionReadiness.cs` — completo |
| TenantId obrigatório | ✅ | Guard + Validator |
| SourceEnvironmentId + TargetEnvironmentId obrigatórios | ✅ | Validator NotEmpty |
| Source ≠ Target validado | ✅ | `SourceEnvironmentId != TargetEnvironmentId` |
| ServiceName + Version obrigatórios | ✅ | Validator NotEmpty |
| ReleaseId opcional | ✅ | Nullable string |
| ReadinessScore (0-100) | ✅ | `Math.Clamp(score, 0, 100)` |
| ReadinessLevel: NOT_READY / NEEDS_REVIEW / READY | ✅ | Parsing estruturado com fallback |
| Blockers e Warnings separados | ✅ | Parse separado para BLOCKER: e WARNING: |
| ShouldBlock boolean | ✅ | Parse de SHOULD_BLOCK: YES/NO |
| TenantId na resposta | ✅ | Response record |

### Gaps Críticos

| Gap | Detalhe | Risco |
|---|---|---|
| **Sem validação que source é non-prod e target é prod-like** | Validator apenas garante source ≠ target. Pode-se pedir assessment de promoção de prod→dev. | Análise IA inverte o fluxo, gerando recomendações de "readiness" sem sentido operacional. |
| **Sem DB lookup de tenant ownership** | Mesma limitação do CompareEnvironments. | Risco médio. |

### Análise da TenantEnvironmentContext

O `TenantEnvironmentContext.IsPreProductionCandidate()` está implementado no domain:
```
Profile is Staging or UserAcceptanceTesting 
|| (Criticality >= High && !IsProductionLike)
```

Este método não é utilizado pelo handler `AssessPromotionReadiness`. Seria o local correcto para validar que o source é um candidato a promoção.

---

## 5. Isolamento de Tenant nas Respostas de IA

### Análise

| Mecanismo | Status | Detalhe |
|---|---|---|
| TenantId incluído no grounding context | ✅ | Todos os handlers incluem tenant-scoped grounding |
| TenantId incluído na response | ✅ | Todas as responses têm TenantId para rastreabilidade |
| Sem acesso cross-tenant via DB | ✅ | Handlers não fazem queries directas de dados de outros tenants |
| DB lookup para confirmar environment ownership | ❌ | Ausente em todos os handlers AI de análise |
| TenantId validado contra JWT/auth | ✅ | Middleware de tenant faz esta validação antes dos handlers |

**Análise de risco:** O risco de data leakage cross-tenant existe mas é mitigado pela autenticação de tenant no middleware. Um utilizador autenticado no TenantA não pode fazer pedidos com TenantB sem ter token JWT de TenantB. O risco residual é um utilizador que tem acesso a dois tenants e pode cruzar EnvironmentIds entre eles.

---

## 6. Comportamento Fail-Safe

### Análise

| Cenário | Comportamento | Status |
|---|---|---|
| Provider AI indisponível | `Error.Business("AIKnowledge.Provider.Unavailable")` | ✅ Correcto |
| Provider retorna fallback token `[FALLBACK_PROVIDER_UNAVAILABLE]` | `isFallback = true` na response | ✅ Correcto |
| Validator falha (TenantId vazio) | FluentValidation retorna erros, handler não é executado | ✅ Correcto |
| Guard falha (null) | `ArgumentException` do Ardalis.GuardClauses | ✅ Correcto |
| EnvironmentId não resolúvel pelo middleware | `IsResolved = false` — AIContextBuilder usa `EnvironmentId.Empty` + `Development` | ⚠️ Degradado mas sem crash |
| Context insuficiente (TenantId empty) | Guard.Against.NullOrWhiteSpace rejeita | ✅ Correcto |

**Nota:** Quando o ambiente não está resolvido, o `AIContextBuilder` usa defaults (EnvironmentId.Empty, profile Development). Isto não causa crash mas significa que a IA opera com contexto degradado. Idealmente os endpoints AI deveriam rejeitar pedidos sem contexto de ambiente resolvido.

---

## 7. Auditabilidade e Rastreabilidade

| Aspecto | Status | Detalhe |
|---|---|---|
| CorrelationId único por execução de IA | ✅ | `Guid.NewGuid()` no início de cada Handle |
| TenantId na resposta | ✅ | Incluído em todas as Response records |
| EnvironmentId na resposta | ✅ | Incluído em todas as Response records |
| RawAnalysis incluído na resposta | ✅ | Permite auditoria do output bruto do provider |
| IsFallback flag | ✅ | Indica se foi usado provider fallback |
| AnalyzedAt / AssessedAt / ComparedAt timestamps | ✅ | DateTimeOffset via IDateTimeProvider |
| Log structured com TenantId, EnvironmentId, CorrelationId | ✅ | `logger.LogInformation(...)` com structured logging |
| AIUsageEntry para billing/audit | ✅ | `AIUsageEntry` entity em Governance domain |
| AiExternalInferenceRecord | ✅ | Registo de inferências externas |
| Audit entries via ListAuditEntries feature | ✅ | Feature presente em Governance |

---

## 8. Fronteiras e Governança do Módulo AI

### Controlo de Modelos

| Aspecto | Status |
|---|---|
| Model Registry com AIModel entity | ✅ |
| RegisterModel / UpdateModel / ActivateModel features | ✅ |
| AIRoutingStrategy para selecção de modelo | ✅ |
| Budget por tenant com AIBudget entity | ✅ |
| Token quota via AiTokenQuotaPolicy | ✅ |
| AIAccessPolicy por utilizador/grupo | ✅ |

### External AI Governance

| Aspecto | Status |
|---|---|
| ExternalAiPolicy entity | ✅ |
| ConfigureExternalAIPolicy feature | ✅ |
| KnowledgeCapture com approval workflow | ✅ |
| ExternalAiConsultation com auditoria | ✅ |

### IDE Integration

| Aspecto | Status |
|---|---|
| AIIDECapabilityPolicy | ✅ |
| AIIDEClientRegistration | ✅ |
| RegisterIdeClient feature | ✅ |
| GetIdeCapabilities / GetIdeSummary | ✅ |

---

## 9. Superfícies AI no Frontend

| Componente | Localização | Status |
|---|---|---|
| AiAnalysisPage | `features/ai-hub/pages/AiAnalysisPage.tsx` | ✅ |
| Tab non-prod analysis | AiAnalysisPage | ✅ — verifica isProductionLike, bloqueia se prod |
| Tab compare environments | AiAnalysisPage | ✅ |
| Tab promotion readiness | AiAnalysisPage | ✅ |
| AssistantPanel | `features/ai-hub/components/AssistantPanel.tsx` | ✅ |
| AiGovernanceApi client | `features/ai-hub/api/aiGovernance.ts` | ✅ — inclui analyzeNonProdEnvironment, compareEnvironments, assessPromotionReadiness |
| Propagação de EnvironmentId nas chamadas AI | `src/api/client.ts` | ✅ — X-Environment-Id no interceptor |
| i18n em textos AI | AiAnalysisPage | ✅ — `t('aiAnalysis.*')` |
| **Ambiente real vs mock** | EnvironmentContext | ❌ — AiAnalysisPage usa ambientes mock |

---

## 10. Veredicto de AI Readiness

### Por Dimensão

| Dimensão | Score | Veredicto |
|---|---|---|
| Context building (TenantId+EnvironmentId) | 8/10 | ✅ Pronto — com gap de persistência de IsProductionLike |
| Non-prod analysis capability | 6/10 | ⚠️ Parcialmente Pronto — falta validação server-side de perfil |
| Cross-env comparison capability | 7/10 | ⚠️ Parcialmente Pronto — falta DB lookup de ownership |
| Promotion readiness assessment | 6/10 | ⚠️ Parcialmente Pronto — falta validação de source/target profiles |
| Tenant isolation | 7/10 | ⚠️ Parcialmente Pronto — grounding ok, DB lookup ausente |
| Fail-safe behavior | 9/10 | ✅ Pronto |
| Auditabilidade / rastreabilidade | 9/10 | ✅ Pronto |
| Frontend AI surfaces | 7/10 | ⚠️ Parcialmente Pronto — mock de ambientes |
| Governança de modelos e políticas | 9/10 | ✅ Pronto |
| Cobertura de testes AI | 7/10 | ⚠️ Parcial — falta teste de rejeição em prod |

### Score Global

**AI Readiness Score: 7.5 / 10**

### Veredicto

> **⚠️ PARCIALMENTE PRONTO PARA PRODUÇÃO**
>
> O módulo AI tem fundações sólidas e arquitectura correcta. As 3 features de análise ambiental estão implementadas com grounding correcto e fail-safe. Os mecanismos de auditabilidade e governança estão bem desenvolvidos.
>
> Para atingir 100% de AI Readiness são necessárias:
> 1. **Obrigatório (P0):** Validação server-side que AnalyzeNonProdEnvironment rejeita ambientes produtivos
> 2. **Obrigatório (P0):** Migração de EnvironmentProfile fields para BD (F-01)
> 3. **Obrigatório (P1):** Validação de source/target profiles em AssessPromotionReadiness
> 4. **Obrigatório (P1):** DB lookup para confirmar tenant ownership de environments nos handlers AI
> 5. **Obrigatório (P1):** Substituição do mock de ambientes por API real no frontend
