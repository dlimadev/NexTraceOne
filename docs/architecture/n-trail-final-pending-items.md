# Lista de Pendências Finais da Trilha N — NexTraceOne

> **Data:** 2026-03-25 | **Fase:** V2 — Auditoria de prontidão para iniciar a Trilha E

---

## Resumo

| Categoria | Quantidade |
|---|---|
| A. Prompts não executados | 0 |
| B. Prompts parcialmente executados | 0 |
| C. Ficheiros ausentes | 0 |
| D. Ficheiros fracos ou superficiais | 2 (não bloqueadores) |
| E. Pendências que bloqueiam a trilha E | 0 (absolutos) |
| F. Pendências resolvíveis dentro da trilha E | 35 itens (~93h) |

---

## A. Prompts Não Executados

**Nenhum.** Todos os 16 prompts (N1–N16) foram executados com resultados substantivos.

---

## B. Prompts Parcialmente Executados

**Nenhum.** Todos os prompts produziram a totalidade dos ficheiros esperados, muitos com ficheiros adicionais.

---

## C. Ficheiros Ausentes

**Nenhum ficheiro obrigatório está ausente.** Todas as 183+ especificações de ficheiros foram cumpridas, com 222+ ficheiros totais encontrados.

---

## D. Ficheiros Fracos ou Superficiais

| # | Ficheiro | Linhas | Motivo | Impacto |
|---|---|---|---|---|
| D-01 | `docs/architecture/migration-readiness-by-module.md` | 77 | Compacto — mas contém a matriz completa de readiness para 13 módulos com 8 colunas de avaliação | ❌ Não bloqueador |
| D-02 | `docs/11-review-modular/00-governance/prompt-n1-to-n10-summary-matrix.md` | 46 | Tabela resumida — mas cumpre o objetivo de vista rápida do estado de execução | ❌ Não bloqueador |

**Nota:** Ambos os ficheiros são concisos por natureza (matrizes/tabelas) e não por falta de conteúdo. Nenhum necessita de retrabalho.

---

## E. Pendências que Bloqueiam a Trilha E

**Nenhuma pendência bloqueia absolutamente o início da trilha E.**

Os seguintes itens são classificados como HIGH mas têm plano de resolução dentro da trilha E:

| # | Pendência | Severidade | Plano | Sprint E |
|---|---|---|---|---|
| E-01 | Extração OI-01 (Contracts de Catalog) | HIGH | Onda 0 | E-02 a E-05 |
| E-02 | Extração OI-02 (Integrations de Governance) | HIGH | Onda 0 | E-02 a E-05 |
| E-03 | Extração OI-03 (Product Analytics de Governance) | HIGH | Onda 0 | E-02 a E-05 |
| E-04 | Extração OI-04 (Environment de Identity) | MEDIUM | Onda 0 | E-02 a E-05 |
| E-05 | AI Tools nunca executam (CR-2) | HIGH | Onda 6 | E-último |
| E-06 | MFA modeled not enforced (P0 Identity) | HIGH | Remediação Identity | E-06+ |
| E-07 | API Key auth absent (P1 Identity) | HIGH | Remediação Identity | E-06+ |
| E-08 | InMemoryIncidentStore (mock production data) | MEDIUM | Remediação OpIntel | E-06+ |

---

## F. Pendências Resolvíveis Dentro da Trilha E

### F.1. Limpeza Imediata (Sprint E-00, ~12h)

| # | Item | Ficheiro/Referência | Horas |
|---|---|---|---|
| F-01 | Remover 17 licensing permissions | `RolePermissionCatalog.cs` | 1h |
| F-02 | Remover licensing:write das delegações | `CreateDelegation.cs` | 0.5h |
| F-03 | Remover mapeamento 'licensing' breadcrumbs | `Breadcrumbs.tsx` | 0.5h |
| F-04 | Remover mapeamento 'vendor' navigation | `navigation.ts` | 0.5h |
| F-05 | Remover "fake assistant response" i18n | `en.json`, `pt-BR.json` | 0.5h |
| F-06 | Adicionar notifications:* ao RolePermissionCatalog | `RolePermissionCatalog.cs` | 1h |
| F-07 | Condicionar AI pages a feature flags | `AiAssistantPage.tsx`, `AiAnalysisPage.tsx` | 4h |
| F-08 | Reescrever comentário MfaPolicy sem "licensing" | `MfaPolicy.cs` | 0.5h |
| F-09 | Reescrever guidanceAdmin sem "licensing" (3 locales) | i18n files | 0.5h |

**Subtotal: ~9h**

### F.2. Correções Obrigatórias (Sprint E-01, ~20h)

| # | Item | Módulo | Horas |
|---|---|---|---|
| F-10 | Adicionar DemoBanner a páginas FinOps com IsSimulated | Governance | 4h |
| F-11 | Documentar EnvironmentsPage como não acessível por sidebar | Environment Mgmt | 2h |
| F-12 | Adicionar warning labels a Product Analytics dashboards com dados mock | Product Analytics | 4h |
| F-13 | Remover HasData licensing seeds (quando recriar migrations) | Identity | 1h |
| F-14 | Corrigir prefixo `oi_` → `ops_` em OpIntel configs | OpIntel | 4h |
| F-15 | Corrigir prefixo `ct_` → `ctr_` em Contracts configs | Contracts | 3h |
| F-16 | Corrigir prefixos `gov_` → `int_`/`pan_` embeddings | Integrations/Product Analytics | 2h |

**Subtotal: ~20h**

### F.3. Remediação Incremental (Sprints E-02+, ~64h)

| # | Item | Módulo | Horas |
|---|---|---|---|
| F-17 | Substituir InMemoryIncidentStore por EF Core | OpIntel | 8h |
| F-18 | Substituir GenerateSimulatedEntries por dados reais | OpIntel | 4h |
| F-19 | Implementar DatabaseRetrievalService real | AI & Knowledge | 12h |
| F-20 | Implementar AI Tool Execution real | AI & Knowledge | 16h |
| F-21 | Implementar AI Streaming | AI & Knowledge | 8h |
| F-22 | Implementar GetPersonaUsage com dados reais | Product Analytics | 8h |
| F-23 | Implementar CatalogGraphModuleService | Catalog | 8h |

**Subtotal: ~64h**

---

## Resumo de Esforço

| Fase | Itens | Horas | Quando |
|---|---|---|---|
| Sprint E-00 (limpeza imediata) | 9 | ~9h | Antes do E-01 |
| Sprint E-01 (correções obrigatórias) | 7 | ~20h | Primeiro sprint E |
| Sprints E-02+ (incremental) | 7 | ~64h | Distribuído nas ondas |
| **Total pendências** | **23** | **~93h** | — |

Os restantes ~1782h do backlog total (~1875h) correspondem às remediações por módulo que constituem o corpo da trilha E e não são "pendências" — são o trabalho planeado.

---

## Conclusão

A trilha N não deixa pendências estruturais que impeçam o início da trilha E. As 23 pendências de limpeza (~93h) são conhecidas, priorizadas e executáveis nos primeiros sprints. O projeto pode avançar com confiança.

### ➡️ Recomendação: Iniciar trilha E imediatamente com E-00 (limpeza) + E-01 (correções) no primeiro sprint.
