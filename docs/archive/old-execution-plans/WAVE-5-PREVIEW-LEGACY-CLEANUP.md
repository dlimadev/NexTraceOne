# Wave 5 — Preview & Legacy Cleanup

> **Data:** 2026-03-23
> **Referência:** Blocos C e D da Wave 5

---

## Objetivo

Eliminar restos de preview/polish e decidir formalmente sobre itens ambíguos/legados que ainda existiam no NexTraceOne após as ondas anteriores.

---

## Itens de Preview Removidos

### GAP-014 — GovernancePackDetail preview badge

| Campo | Valor |
|-------|-------|
| **Arquivo** | `src/frontend/src/features/governance/pages/GovernancePackDetailPage.tsx` |
| **Localização** | Tab "Simulation" (linhas 295-302) |
| **O que existia** | Badge `<Badge variant="warning">` com texto "Preview" e disclaimer sobre simulação |
| **Ação** | Removido completamente |
| **Justificativa** | A funcionalidade de simulação está operacional e integrada. O badge de preview era residual das ondas iniciais e enfraquecia a percepção de produto concluído. |
| **Testes afetados** | Nenhum — 5 testes de `GovernancePackDetailPage.test.tsx` continuam passando |

### GAP-013 — EvidencePackages preview badge

| Campo | Valor |
|-------|-------|
| **Verificação** | Nenhum preview badge encontrado em `EvidencePackagesPage.tsx` |
| **Ação** | Nenhuma necessária — já estava limpo |
| **Decisão** | Gap fechado sem ação |

---

## Itens Legados Decididos

### GAP-023 — ProductStore

| Campo | Valor |
|-------|-------|
| **Decisão** | **Descartado oficialmente** |
| **Contexto** | `IProductStore` aparecia em documentação de assessment e observabilidade como abstração de alto nível para armazenamento de métricas de produto |
| **Estado no código** | `ProductStoreOptions` existe como classe de configuração em `TelemetryStoreOptions.cs`. 7 testes unitários validam suas propriedades. Não existe implementação de interface `IProductStore`. |
| **Justificativa de descarte** | O ClickHouse já serve como store analítico oficial via provider configurável. A abstração `IProductStore` como interface separada seria redundante. A funcionalidade pretendida já é coberta pela arquitetura atual. |
| **Referências documentais atualizadas** | Documentação de assessment e gap classification atualizada para refletir o descarte |

### Grafana — Referências residuais em documentação

| Campo | Valor |
|-------|-------|
| **Decisão** | **Preservar com disclaimers** |
| **Contexto** | Documentos históricos (assessment, execution phases) mencionam Grafana como stack original |
| **Estado** | Todos os documentos que mencionam Grafana já possuem disclaimers/notas no topo indicando que a referência é histórica e que a stack foi migrada |
| **Justificativa** | Preservar o histórico de decisões arquiteturais é importante para auditoria. Os disclaimers são suficientes para evitar ambiguidade. |
| **Nenhuma ação adicional necessária** |

### Serilog.Sinks.Grafana.Loki — Dependência transitiva

| Campo | Valor |
|-------|-------|
| **Decisão** | **Aceite como dependência transitiva** |
| **Contexto** | O package aparece em ficheiros `obj/` gerados dos E2E tests |
| **Verificação** | Não é dependência direta em nenhum `.csproj` |
| **Justificativa** | É uma dependência transitiva que não afeta a arquitetura. Remoção forçada poderia causar problemas de resolução de pacotes sem benefício real. |

---

## Verificação Adicional: Sidebar Navigation

| Verificação | Resultado |
|-------------|-----------|
| Itens do sidebar com `preview: true` | **Zero** — nenhum item de navegação está marcado como preview |
| Todos os items em produção scope | **Sim** — `releaseScope.ts` tem 33 rotas incluídas e 0 excluídas |

---

## Rationale Consolidado

A estratégia adoptada na Wave 5 para preview/legacy foi:

1. **Se a funcionalidade está operacional** → remover indicação de preview
2. **Se a funcionalidade realmente não está pronta** → manter indicação e documentar como gap residual
3. **Se o conceito legado já foi substituído pela arquitectura actual** → descartar formalmente com justificativa
4. **Se a referência é histórica e tem disclaimer** → preservar para rastreabilidade

---

> Este documento formaliza todas as decisões de preview/legacy cleanup da Wave 5 do NexTraceOne.
