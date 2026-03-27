# Relatório de Remoção, Arquivamento e Consolidação — NexTraceOne
**Auditoria Forense | Março 2026**

---

## 1. Critérios Aplicados

- **REMOVE_CANDIDATE**: Sem uso real, tecnologia abandonada, duplicação desnecessária, documento contraditório sem valor, artefato fora da visão
- **ARCHIVE_CANDIDATE**: Valor histórico/técnico mas não deve permanecer ativo
- **CONSOLIDATE_CANDIDATE**: Dois ou mais artefatos com o mesmo papel
- **KEEP_AND_COMPLETE**: Estratégico, incompleto, deve ser preservado e concluído

---

## 2. REMOVE_CANDIDATE

### Código

| Artefato | Motivo | Evidência |
|---|---|---|
| `InMemoryIncidentStore` (se ainda existir) | Substituído por `EfIncidentStore`; REBASELINE confirma EF como atual | `docs/REBASELINE.md` §Operational Intelligence |
| Handlers de mock em Governance com `IsSimulated: true` | Substituir por implementação real quando persistence layer for adicionado; não remover antes | Remover apenas após substituição — ver §4 |
| Referências a Commercial Governance em docs | Módulo removido no PR-17 | `docs/REBASELINE.md` §Removido |

### Documentação

| Documento | Motivo | Evidência |
|---|---|---|
| Docs que referenciam Commercial Governance como ativo | Módulo removido | PR-17 |
| Docs que descrevem stack tecnológico abandonado (ex: se houver refs a Redis/Temporal) | Tecnologia não adotada | Verificar cada caso |
| Relatórios de execução histórica (e14, e15, e16, e17, e18) redundantes com REBASELINE | Histórico de processo, não estado atual | `docs/architecture/e14-* a e18-*` |
| Relatórios p0-*, p1-* de segurança historicos | Histórico de hardening | `docs/architecture/p0-* a p1-*` |

---

## 3. ARCHIVE_CANDIDATE

### Documentação Histórica de Evolução

| Grupo de Documentos | Motivo para Arquivar |
|---|---|
| `docs/architecture/e14-legacy-migrations-removal-report.md` até `e18-final-technical-closure-report.md` | Histórico de execução de fases; não é estado atual |
| `docs/architecture/p0-1-*` até `p1-5-*` (relatórios de hardening de segurança) | Histórico de hardening implementado; já consolidado no estado atual |
| `docs/architecture/n-trail-*` (execução de auditoria anterior) | Histórico de processo |
| `docs/archive/` (conteúdo já em subdiretório de arquivo) | Já identificado como arquivo |
| `docs/11-review-modular/` como conjunto completo | Histórico de revisão modular; substituído por REBASELINE.md como fonte atual |

**Ação recomendada:** Mover para `docs/archive/historical-execution/` mantendo estrutura interna.

---

## 4. KEEP_AND_COMPLETE

### Código — Estratégico, incompleto, preservar e concluir

| Artefato | Justificativa | Ação |
|---|---|---|
| `EfIncidentStore` + `IncidentDbContext` | Real, 5 DbSets, migração; falta engine de correlação dinâmica | Completar com correlação |
| `RuntimeIntelligenceDbContext` | DbContext real; migração não confirmada; IRuntimeIntelligenceModule PLAN | Gerar migração; implementar interface |
| `CostIntelligenceDbContext` | DbContext real; ICostIntelligenceModule PLAN | Gerar migração; implementar interface |
| `AiOrchestrationDbContext` + `ExternalAiDbContext` | DbContexts existem; migrações não confirmadas | Gerar migrações; implementar handlers ExternalAI |
| `IExternalAIRoutingPort` | Abstração correta para routing de providers | Conectar ao handler SendAssistantMessage |
| `GovernanceDbContext` | Existe; módulo 100% mock por design; persistence layer necessária | Adicionar persistence layer completo |
| `KnowledgeDbContext` | DbContext existe; sem migrações | Gerar migração; completar Knowledge Hub |
| `IntegrationsDbContext` | DbContext existe; conectores são stubs | Gerar migração; implementar ao menos 1 conector |
| 7 stubs no Developer Portal | Intencionais; aguardam cross-module | Implementar via IContractsModule quando disponível |
| `IAiOrchestrationModule` | Interface vazia mas arquiteturalmente correta | Adicionar métodos e implementação |
| `IContractsModule` | PLAN; crítico para cross-module | Implementar |
| `IChangeIntelligenceModule` | PLAN; crítico para Governance e IA | Implementar |

---

## 5. CONSOLIDATE_CANDIDATE

### Documentação com Papel Duplicado

| Conjunto | Consolidação Proposta |
|---|---|
| `IMPLEMENTATION-STATUS.md` + `REBASELINE.md` | REBASELINE.md é mais atual e honesto; consolidar em `CURRENT-STATE.md` |
| `ROADMAP.md` + `POST-PR16-EVOLUTION-ROADMAP.md` + `PRODUCT-REFOUNDATION-PLAN.md` | Consolidar em único `ROADMAP.md` atual |
| `docs/11-review-modular/` (100+ docs por módulo) | Criar `CURRENT-STATE.md` por módulo como sumário; 11-review-modular como histórico |
| ADRs duplicados em `docs/architecture/adr/` e `docs/architecture/ADR-*.md` | Verificar sobreposição e consolidar em estrutura única |
| Múltiplos relatórios de validação (WAVE-1-*, EXECUTION-BASELINE-*) | Consolidar em REBASELINE.md |

---

## 6. Resumo de Ações por Categoria

| Categoria | Quantidade Estimada | Impacto |
|---|---|---|
| REMOVE_CANDIDATE | ~10-15 artefatos | Baixo risco; reduz ruído |
| ARCHIVE_CANDIDATE | ~100+ documentos históricos | Reduz confusão sobre estado atual |
| CONSOLIDATE_CANDIDATE | ~5 grupos de docs | Cria fonte única de verdade |
| KEEP_AND_COMPLETE | ~12 artefatos estratégicos | Alta prioridade para produto |

---

## 7. O que NÃO Remover

| Artefato | Justificativa para Preservar |
|---|---|
| Módulos com `IsSimulated: true` | São o estado atual intencional; remover sem substituição quebra o produto |
| 7 stubs do Developer Portal | Intencionais; referenciam capacidades reais futuras |
| DbContexts sem migrações | Estratégicos; precisam de migrações geradas, não de remoção |
| REBASELINE.md, CORE-FLOW-GAPS.md | Fontes de verdade do estado atual |
| Building Blocks completos | Fundação do produto; não remover |
| ADRs | Decisões arquiteturais registadas |
