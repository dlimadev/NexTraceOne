> **⚠️ ARCHIVED — April 2026**: Este documento foi gerado como análise pontual de gaps. Muitos dos gaps aqui listados já foram resolvidos. Para o estado atual, consultar `docs/CONSOLIDATED-GAP-ANALYSIS-AND-ACTION-PLAN.md` e `docs/IMPLEMENTATION-STATUS.md`.

# Change Governance — Gaps, Erros e Pendências

## 1. Estado resumido do módulo
246 .cs files, 4 DbContexts, 100% features reais. Módulo flagship do produto. Gaps limitados a 2 interfaces cross-module sem implementação e seed em falta.

## 2. Gaps críticos
Nenhum gap crítico.

## 3. Gaps altos
Nenhum gap alto.

## 4. Gaps médios

### 4.1 `IPromotionModule` sem implementação
- **Severidade:** MEDIUM
- **Classificação:** INCOMPLETE
- **Descrição:** Interface define `IsPromotionApprovedAsync` e `GetPromotionStatusAsync`. Comentário: `IMPLEMENTATION STATUS: Planned — no implementation exists, no consumers.`
- **Impacto:** Nenhum módulo externo pode consultar estado de promoção.
- **Evidência:** `src/modules/changegovernance/NexTraceOne.ChangeGovernance.Contracts/Promotion/ServiceInterfaces/IPromotionModule.cs`

### 4.2 `IRulesetGovernanceModule` sem implementação
- **Severidade:** MEDIUM
- **Classificação:** INCOMPLETE
- **Descrição:** Interface define `GetRulesetScoreAsync` e `IsReleaseCompliantAsync`. Comentário: `IMPLEMENTATION STATUS: Planned — no implementation exists, no consumers.`
- **Impacto:** Scoring de conformidade de releases não disponível cross-module.
- **Evidência:** `src/modules/changegovernance/NexTraceOne.ChangeGovernance.Contracts/RulesetGovernance/ServiceInterfaces/IRulesetGovernanceModule.cs`

## 5. Itens mock / stub / placeholder
Nenhum — todos os handlers são reais.

## 6. Erros de desenho / implementação incorreta
Nenhum.

## 7. Gaps de frontend ligados a este módulo
- 5 de 6 páginas sem empty state pattern: `ChangeCatalogPage`, `ChangeDetailPage`, `PromotionPage`, `ReleasesPage`, `WorkflowPage`
- `WorkflowConfigurationPage` sem error handling (isError)

## 8. Gaps de backend ligados a este módulo
- `IPromotionModule` e `IRulesetGovernanceModule` sem `*ModuleService` implementation

## 9. Gaps de banco/migração ligados a este módulo
Nenhum — 4 DbContexts com migrations confirmadas.

## 10. Gaps de configuração ligados a este módulo
Nenhum.

## 11. Gaps de documentação ligados a este módulo
- `docs/IMPLEMENTATION-STATUS.md` afirma `IChangeIntelligenceModule = PLAN` — **FALSO**, implementado por `ChangeIntelligenceModule.cs`
- `docs/CORE-FLOW-GAPS.md` §Flow 2 afirma "IChangeIntelligenceModule cross-module interface = PLAN" — **FALSO**

## 12. Gaps de seed/bootstrap ligados a este módulo
- `seed-changegovernance.sql` referenciado mas **NÃO EXISTE** no disco.

## 13. Ações corretivas obrigatórias
1. Implementar `IPromotionModule` como `PromotionModuleService.cs` quando houver consumer
2. Implementar `IRulesetGovernanceModule` como `RulesetGovernanceModuleService.cs` quando houver consumer
3. Criar `seed-changegovernance.sql` ou remover referência
4. Actualizar documentação cross-module
