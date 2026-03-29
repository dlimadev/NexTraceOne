# Onda 11 — Frontend Enterprise

> **Duração estimada:** 3-4 semanas
> **Dependências:** Todas as ondas anteriores
> **Risco:** Baixo — trabalho de polish e consolidação
> **Referência:** [LEGACY-MAINFRAME-WAVES.md](../LEGACY-MAINFRAME-WAVES.md)

---

## Objetivo

Polish de UX enterprise para todas as telas legacy. Garantir consistência visual, responsividade, acessibilidade, i18n completo e experiência de qualidade enterprise.

---

## Entregáveis

- [ ] Dashboard executivo com visão híbrida (moderno + legacy)
- [ ] Persona-aware landing pages com dados legacy
- [ ] Responsive design verificado em todas as telas legacy
- [ ] Loading/error/empty states completos em todas as páginas
- [ ] Keyboard navigation e acessibilidade básica
- [ ] i18n completo verificado (~500+ keys)
- [ ] Theme consistency com design system
- [ ] Cross-references entre páginas
- [ ] Command Palette com suporte a ativos legacy
- [ ] Export de dados (CSV)

---

## Impacto Frontend

### Dashboard Executivo — Extensão

**Página existente:** `/dashboard`
**Persona:** Executive, Architect

Novos widgets para visão híbrida:

| Widget | Descrição |
|---|---|
| `LegacyAssetsSummary` | Card: Total legacy assets por tipo |
| `BatchSlaOverviewWidget` | Mini-gráfico: SLA compliance rate |
| `MqHealthWidget` | Mini-gráfico: Queue health summary |
| `HybridChangeVelocity` | Gráfico: mudanças por semana (moderno vs legacy) |
| `CrossPlatformRiskWidget` | Indicador: mudanças com impacto cross-platform |

### Persona-Aware Landing

| Persona | Dados legacy prioritários |
|---|---|
| **Engineer** | Ativos legacy do meu team, copybooks recentes |
| **Tech Lead** | SLA compliance, batch failures, MQ anomalies do meu domínio |
| **Architect** | Hybrid graph, cross-platform dependencies, blast radius |
| **Operations** | Batch dashboard, MQ dashboard, incidents legacy |
| **Executive** | SLA trends, change velocity, risk overview |
| **CAB** | Pending approvals, evidence packs, impact summaries |
| **Auditor** | Audit trail de mudanças legacy, compliance |

### Loading / Error / Empty States

Verificar e implementar para todas as páginas legacy:

| Página | Loading | Error | Empty |
|---|---|---|---|
| Legacy Asset Catalog | ✅ Skeleton | ✅ Error card | ✅ "No legacy assets registered yet" |
| Mainframe System Detail | ✅ Skeleton | ✅ Error card | ✅ "System not found" |
| Copybook Viewer | ✅ Skeleton | ✅ Error card | ✅ "No copybooks yet" |
| Batch Intelligence | ✅ Skeleton | ✅ Error card | ✅ "No batch jobs configured" |
| Batch Job Detail | ✅ Skeleton | ✅ Error card | ✅ "No executions recorded" |
| MQ Intelligence | ✅ Skeleton | ✅ Error card | ✅ "No queue managers configured" |
| MQ Topology | ✅ Loading | ✅ Error card | ✅ "No topology data" |
| Legacy Change Impact | ✅ Skeleton | ✅ Error card | ✅ N/A |

### Command Palette — Extensão

O Command Palette (`Ctrl+K`) deve suportar:
- Search legacy assets por nome
- Quick navigate: "Go to batch job [name]"
- Quick action: "Check MQ queue health"
- Quick action: "Analyze copybook impact"
- Search across: services + legacy assets + contracts + releases

### Cross-References

Links entre páginas para navegação fluida:

| De | Para | Contexto |
|---|---|---|
| Service Detail | Legacy Assets | "View associated legacy assets" |
| Copybook Viewer | Programs | "Programs using this copybook" |
| Batch Job Detail | Chain View | "View in chain context" |
| MQ Queue Detail | Topology | "View in topology" |
| Release Detail | Legacy Impact | "View legacy impact analysis" |
| Incident Detail | Legacy Asset | "View affected legacy asset" |
| Dependency Graph | Legacy Detail | Click on legacy node → detail page |

### Export

- CSV export para: legacy asset list, batch execution history, MQ statistics
- Botão de export em todas as listas paginadas

---

## i18n — Verificação Completa

Verificar ~500+ keys em:

| Namespace | Keys estimadas |
|---|---|
| `legacy.*` | ~100 (base da Onda 0) |
| `batch.*` | ~80 (dashboard, detail, chain, SLA) |
| `messaging.*` | ~80 (dashboard, detail, topology, anomalies) |
| `legacyContracts.*` | ~60 (copybook viewer, diff, MQ contracts) |
| `legacyChange.*` | ~50 (impact analysis, risk, CAB summary) |
| `legacyAi.*` | ~30 (tools, quick actions) |
| Extensões em namespaces existentes | ~100 (releases, incidents, graph) |

---

## Testes

### Testes E2E (Playwright) (~30)
- Navigation flow: Dashboard → Legacy Assets → Detail
- Navigation flow: Batch Dashboard → Job Detail → Execution
- Navigation flow: MQ Dashboard → Topology → Queue Detail
- Copybook import → view → diff
- Command Palette search for legacy assets
- Responsive: mobile breakpoints
- Loading/error states

---

## Critérios de Aceite

1. ✅ Todas as telas legacy responsivas (desktop + tablet + mobile)
2. ✅ i18n 100% completo (nenhum texto hardcoded)
3. ✅ Loading/error/empty states em todas as telas
4. ✅ Navegação consistente com resto do produto
5. ✅ Command Palette encontra ativos legacy
6. ✅ Cross-references funcionais entre páginas
7. ✅ CSV export funcional em listas
8. ✅ Performance aceitável com volumes enterprise
9. ✅ Design system consistency verificada
10. ✅ Acessibilidade básica (keyboard navigation, ARIA labels)

---

## Stories

| ID | Story | Prioridade |
|---|---|---|
| W11-S01 | Dashboard executivo — widgets legacy | P1 |
| W11-S02 | Persona-aware landing pages | P2 |
| W11-S03 | Loading/error/empty states audit | P1 |
| W11-S04 | Responsive design verification | P1 |
| W11-S05 | i18n audit completo (~500+ keys) | P0 |
| W11-S06 | Command Palette — suporte legacy | P2 |
| W11-S07 | Cross-references entre páginas | P2 |
| W11-S08 | CSV export em listas | P2 |
| W11-S09 | Design system consistency audit | P1 |
| W11-S10 | Acessibilidade — keyboard + ARIA | P2 |
| W11-S11 | Testes E2E Playwright (~30) | P1 |
