# Fase 2 — Fundação de IA Context-Aware

**Data:** 2026-03-20  
**Status:** Completo  
**Relacionado com:** ADR-002, phase-2-execution-context-foundation.md

---

## 1. Como a IA Passa a Depender de Contexto Resolvido

### Antes da Fase 2

As features de IA (AskCatalogQuestion, ClassifyChangeWithAI, etc.) operavam sem contexto explícito de tenant ou ambiente. A IA não sabia:
- De qual tenant eram os dados
- Em qual ambiente estava operando
- Se poderia comparar dados entre ambientes
- Quais fontes de dados eram permitidas

### Depois da Fase 2

Toda operação de IA pode agora receber um `AiExecutionContext` construído pelo `IAIContextBuilder`. O contexto carrega:
- `TenantId` validado (do CurrentTenant resolvido)
- `EnvironmentId` validado (do EnvironmentContextAccessor)
- `EnvironmentProfile` e `IsProductionLike`
- `AllowedDataScopes` — determinados pelo backend com base no perfil e permissões
- `AiUserContext` com persona e roles

---

## 2. Como Tenant/Environment Afetam a IA

### Escopos de dados por contexto

```
Ambiente não produtivo + permissão cross-environment:
  → AiDataScope.FullAnalysisScopes
  → Inclui: telemetry, incidents, changes, contracts, topology,
            runbooks, cross_environment_comparison, promotion_analysis

Ambiente não produtivo + sem permissão especial:
  → DefaultScopes + promotion_analysis
  → Inclui: telemetry, incidents, changes, contracts, topology, runbooks, promotion_analysis

Ambiente de produção (IsProductionLike):
  → DefaultScopes apenas
  → Sem cross_environment_comparison, sem promotion_analysis ativo
```

### Por que produção tem escopos mais restritos para IA?

A IA de análise de ambientes não produtivos existe para prevenir que problemas avancem para produção. Em produção:
- A comparação cross-environment poderia expor dados sensíveis de outros tenants (por bug futuro)
- A análise de promoção não faz sentido — já está em produção
- A IA deve ser usada para diagnóstico pontual, não análise cross-tenant especulativa

---

## 3. Como a Análise de Ambientes Não Produtivos Influencia a Fundação

### `IAIContextBuilder.BuildAsync(moduleContext)` — uso em requests HTTP

Constrói contexto a partir dos accessors ativos na requisição. Ideal para features de IA dentro do fluxo HTTP normal.

### `IAIContextBuilder.BuildForAsync(tenantId, environmentId, moduleContext)` — uso em background jobs

Constrói contexto para tenant/ambiente explícitos. Necessário quando não há contexto HTTP (scheduled jobs de análise de regressão).

### `IPromotionRiskContextBuilder.BuildAsync(...)` — análise de promoção

Constrói `PromotionRiskAnalysisContext` para análise de risco source → target:
1. Resolve o contexto do ambiente source (ex.: QA)
2. Resolve o contexto do ambiente target (ex.: PROD)
3. Valida que ambos pertencem ao mesmo tenant
4. Constrói o `AiExecutionContext` com escopos de promoção habilitados
5. Retorna o contexto pronto para a IA analisar

```
IA pode responder: "Qual o risco de promover payment-service 2.5.0 de QA para PROD?"
```

### `IPromotionRiskContextBuilder.BuildComparisonAsync(...)` — comparação ad-hoc

Constrói `EnvironmentComparisonContext` para comparar comportamento:
```
IA pode responder: "Como QA se compara com PROD nos últimos 7 dias?"
```

---

## 4. Riscos Evitados com Este Desenho

| Risco | Como o desenho evita |
|---|---|
| IA consulta dados de outro tenant | TenantId sempre validado antes de construir contexto |
| IA ignora ambiente — analisa dados globalmente | EnvironmentId obrigatório no AiExecutionContext |
| Frontend expande escopos de IA | AllowedDataScopes determinados pelo backend; frontend não pode sobrescrever |
| "IA de DEV" vs "IA de PROD" — duplicação | Uma IA, contexto diferente; comportamento por profile, não por instância |
| IA compara ambientes de tenants diferentes | PromotionRiskContextBuilder valida que source/target pertencem ao mesmo tenant |
| IA analisa produção com profundidade desnecessária | Produção recebe apenas DefaultScopes; sem promotion_analysis ou cross_environment |
| Background job usa tenant errado | BuildForAsync com parâmetros explícitos garante contexto correto |

---

## 5. Sequência de Uso para Análise de Pré-Produção

```
1. Usuário (Tech Lead/Architect) solicita análise de promoção para QA→PROD

2. Frontend envia:
   GET /api/v1/ai/promotion-risk
   Headers: Authorization: Bearer <jwt>
             X-Tenant-Id: <tenant-id>
             X-Environment-Id: <qa-env-id>  ← ambiente "de" (source)
   Body: { targetEnvironmentId: "<prod-env-id>", service: "payment", version: "2.5.0" }

3. Pipeline:
   TenantResolutionMiddleware → resolve tenant do JWT
   EnvironmentResolutionMiddleware → resolve QA (valida que pertence ao tenant)
   OperationalContextRequirement → exige tenant+env+usuário resolvidos

4. Handler:
   IPromotionRiskContextBuilder.BuildAsync(tenantId, qaEnvId, prodEnvId, "payment", "2.5.0")
   → Valida QA pertence ao tenant ✓
   → Valida PROD pertence ao tenant ✓
   → Constrói AiExecutionContext com promotion_analysis scope ✓
   → Constrói PromotionRiskAnalysisContext ✓

5. IA analisa dentro do contexto:
   - Dados de QA dentro do tenant
   - Dados de PROD dentro do tenant
   - Sem cruzar tenant boundary

6. Resposta:
   ReadinessAssessment { Score: 72, Recommendation: PromoteWithCaution, ... }
```

---

## 6. Próximos Passos para IA

### Fase 3
- [ ] Integrar `IAIContextBuilder` nos handlers de IA existentes (AskCatalogQuestion, ClassifyChangeWithAI)
- [ ] Adicionar validação de contexto antes de handlers de IA retornarem resultados
- [ ] Expor endpoint de análise de risco de promoção usando IPromotionRiskContextBuilder

### Fase 4+
- [ ] Implementar `RegressionSignal` com dados reais de telemetria
- [ ] Implementar `RiskFinding` a partir de incidentes e contratos
- [ ] Implementar `ReadinessAssessment` com cálculo de score real
- [ ] Persistir ReadinessAssessment para histórico e auditoria
