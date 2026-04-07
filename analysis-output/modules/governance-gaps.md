> **⚠️ ARCHIVED — April 2026**: Este documento foi gerado como análise pontual de gaps. Muitos dos gaps aqui listados já foram resolvidos. Para o estado atual, consultar `docs/CONSOLIDATED-GAP-ANALYSIS-AND-ACTION-PLAN.md` e `docs/IMPLEMENTATION-STATUS.md`.

# Governance — Gaps, Erros e Pendências

## 1. Estado resumido do módulo
144 .cs files, GovernanceDbContext com migrations. FinOps handlers usam `ICostIntelligenceModule` real (`IsSimulated: false`). O módulo é **REAL, não mock** — contrariando a auditoria de Março 2026.

## 2. Gaps críticos
Nenhum.

## 3. Gaps altos
Nenhum.

## 4. Gaps médios

### 4.1 XML doc comments desactualizados em FinOps DTOs
- **Severidade:** LOW
- **Classificação:** DOC_CONTRADICTION
- **Descrição:** Os XML doc comments em DTOs de FinOps ainda dizem `IsSimulated=true indica dados demonstrativos` quando o código real passa `IsSimulated: false` e `DataSource: "cost-intelligence"`.
- **Impacto:** Confusão para developers que leiam os XML comments sem verificar a implementação real.
- **Evidência:** 6 handlers em `src/modules/governance/` (GetFinOpsSummary, GetBenchmarking, GetDomainFinOps, GetFinOpsTrends, GetServiceFinOps, GetTeamFinOps) — todos passam `IsSimulated: false` mas XML docs mencionam o contrário.

## 5. Itens mock / stub / placeholder
Nenhum — todos os handlers de Governance usam `ICostIntelligenceModule.GetCostRecordsAsync()` real.

**CORRECÇÃO DE REGISTO:** A auditoria de Março 2026 e o `frontend-state-report.md` afirmam que 25 páginas de Governance estão "CONNECTED to mock backend" com `IsSimulated: true`. Isto é **FALSO**:
- Todos os 6 FinOps handlers passam `IsSimulated: false`
- A resposta inclui `DataSource: "cost-intelligence"` (real)
- O `DemoBanner` component existe mas **NÃO É USADO** por nenhuma página de feature

## 6. Erros de desenho / implementação incorreta
Nenhum.

## 7. Gaps de frontend ligados a este módulo
- **22 de 25 páginas sem error handling** (`isError`): CompliancePage, DelegatedAdminPage, DomainDetailPage, DomainsOverviewPage, EnterpriseControlsPage, GovernanceConfigurationPage, GovernancePackDetailPage, GovernancePacksOverviewPage, MaturityScorecardsPage, PolicyCatalogPage, ReportsPage, RiskCenterPage, RiskHeatmapPage, TeamDetailPage, TeamsOverviewPage, WaiversPage
- **Quase todas as páginas de Governance sem empty state pattern**: BenchmarkingPage, CompliancePage, DomainFinOpsPage, DomainsOverviewPage, EnterpriseControlsPage, ExecutiveDrillDownPage, ExecutiveFinOpsPage, ExecutiveOverviewPage, FinOpsPage, GovernancePacksOverviewPage, MaturityScorecardsPage, ReportsPage, RiskCenterPage, RiskHeatmapPage, ServiceFinOpsPage, TeamFinOpsPage, TeamsOverviewPage, WaiversPage

## 8. Gaps de backend ligados a este módulo
- XML doc comments desactualizados (LOW)

## 9. Gaps de banco/migração ligados a este módulo
Nenhum — GovernanceDbContext com migration confirmada.

## 10. Gaps de configuração ligados a este módulo
Nenhum.

## 11. Gaps de documentação ligados a este módulo
- `docs/IMPLEMENTATION-STATUS.md` §Governance afirma "SIM (por design)" — **FALSO**, handlers usam dados reais
- `docs/IMPLEMENTATION-STATUS.md` afirma "Apto para Produção? Não" — **INCORRETO** dado que handlers usam módulo real

## 12. Gaps de seed/bootstrap ligados a este módulo
- `seed-governance.sql` referenciado mas **NÃO EXISTE** no disco.

## 13. Ações corretivas obrigatórias
1. Corrigir XML doc comments nos 6 FinOps DTOs (remover menção a `IsSimulated=true`)
2. Adicionar error handling a 22 páginas frontend de Governance
3. Adicionar empty state patterns a páginas de Governance
4. Actualizar `docs/IMPLEMENTATION-STATUS.md` §Governance
5. Criar `seed-governance.sql` ou remover referência
