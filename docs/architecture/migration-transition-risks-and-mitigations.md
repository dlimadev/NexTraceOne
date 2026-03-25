# Riscos e Mitigação da Transição de Persistência

> **Status:** DRAFT
> **Data:** 2026-03-25
> **Fase:** N15 — Estratégia de Transição de Persistência

---

## Objetivo

Explicitar os riscos da transição antes da execução real, com descrição, impacto, probabilidade e mitigação.

---

## Matriz de Riscos

### R-01. Apagar migrations cedo demais

| Aspecto | Detalhe |
|---------|---------|
| **Descrição** | Remover migrations antes do modelo de domínio e persistência estarem finalizados, resultando em baseline incorreta |
| **Impacto** | 🔴 ALTO — retrabalho completo da baseline, perda de tempo |
| **Probabilidade** | MÉDIA — pressão para avançar pode levar a saltar pré-condições |
| **Mitigação** | Checklist formal de pré-condições (`migration-removal-prerequisites.md`). Só avançar quando 80%+ das pré-condições estiverem ✅. Tag do repositório antes de remover. |

---

### R-02. Gerar baseline antes de fechar módulos críticos

| Aspecto | Detalhe |
|---------|---------|
| **Descrição** | Criar baseline para módulos que ainda têm entidades ou mapeamentos incompletos |
| **Impacto** | 🔴 ALTO — baseline incorrecta precisa ser refeita |
| **Probabilidade** | MÉDIA — especialmente para módulos com maturidade < 50% (AI Knowledge, Product Analytics) |
| **Mitigação** | Ordem de ondas respeita maturidade. Módulos com maturidade < 50% vão para as últimas ondas. Readiness matrix consultada antes de cada onda. |

---

### R-03. Manter resíduos de Licensing na nova baseline

| Aspecto | Detalhe |
|---------|---------|
| **Descrição** | Permissões, seeds ou configurações de Licensing migram para a nova baseline |
| **Impacto** | 🟡 MÉDIO — poluição do modelo, confusão, possível exposição de features inexistentes |
| **Probabilidade** | ALTA — 17 permissões licensing identificadas em HasData() do Identity module |
| **Mitigação** | Limpeza obrigatória antes da Onda 1. `licensing-residue-cleanup-review.md` como checklist. Grep por `licensing:` nos seeds finais. |

---

### R-04. Perder fronteiras entre módulos

| Aspecto | Detalhe |
|---------|---------|
| **Descrição** | Módulos que partilham DbContext (Integrations/Product Analytics em Governance) podem ter tabelas mal atribuídas na nova baseline |
| **Impacto** | 🔴 ALTO — violação de bounded contexts, FK cross-module, acoplamento |
| **Probabilidade** | ALTA — 3 módulos (Contracts, Integrations, Product Analytics) estão dentro de módulos errados |
| **Mitigação** | Extracção obrigatória (Onda 0) antes de qualquer baseline. OI-01, OI-02, OI-03 resolvidos como pré-condição. |

---

### R-05. Misturar dados transacionais e analíticos

| Aspecto | Detalhe |
|---------|---------|
| **Descrição** | Dados que deveriam estar no ClickHouse ficam no PostgreSQL, ou vice-versa |
| **Impacto** | 🟡 MÉDIO — degradação de performance, schema errado, dificuldade de manutenção |
| **Probabilidade** | BAIXA — decisões de placement estão bem documentadas |
| **Mitigação** | `final-data-placement-matrix.md` como referência obrigatória. Revisão de schema antes de cada baseline. |

---

### R-06. Seeds inconsistentes entre módulos

| Aspecto | Detalhe |
|---------|---------|
| **Descrição** | Seeds de um módulo referenciam dados de outro módulo que ainda não foi seedado |
| **Impacto** | 🟡 MÉDIO — startup falha, dados de referência ausentes |
| **Probabilidade** | MÉDIA — ordem de seeds depende de ordem de ondas |
| **Mitigação** | Ordem de seeds definida em `module-seed-strategy.md`. Seeds idempotentes. Seeds de referência (IDs) não dependem de FK. |

---

### R-07. Dependências entre módulos mal ordenadas na baseline

| Aspecto | Detalhe |
|---------|---------|
| **Descrição** | Gerar baseline de um módulo que depende de tabelas de outro módulo que ainda não existe |
| **Impacto** | 🔴 ALTO — migrations falham, schema inconsistente |
| **Probabilidade** | BAIXA — ordem de ondas respeita dependências |
| **Mitigação** | Sem FK cross-module (regra). Ordem de ondas (`postgresql-baseline-execution-order.md`). Validação por onda. |

---

### R-08. ClickHouse mal introduzido

| Aspecto | Detalhe |
|---------|---------|
| **Descrição** | ClickHouse configurado incorrectamente, schema errado, ou pipeline de ingestão falha |
| **Impacto** | 🟡 MÉDIO — analytics não funciona, mas domínio transacional não é afectado |
| **Probabilidade** | MÉDIA — primeira vez que ClickHouse é usado em produção no produto |
| **Mitigação** | Introduzir apenas com Product Analytics (REQUIRED) primeiro. Pipeline simples. Fallback para buffer PostgreSQL se ClickHouse indisponível. |

---

### R-09. Documentação insuficiente para execução

| Aspecto | Detalhe |
|---------|---------|
| **Descrição** | Equipa de execução não consegue seguir o plano por falta de detalhes |
| **Impacto** | 🟡 MÉDIO — atrasos, interpretações incorrectas, retrabalho |
| **Probabilidade** | MÉDIA — documentação existe mas pode ter gaps operacionais |
| **Mitigação** | Runbook de execução por onda. Validação checklist por módulo. Pair programming para as primeiras ondas. |

---

### R-10. Perda de seed data implícito nas migrations

| Aspecto | Detalhe |
|---------|---------|
| **Descrição** | Ao remover migrations, `HasData()` seeds são perdidos sem serem extraídos primeiro |
| **Impacto** | 🔴 ALTO — aplicação sobe sem roles, permissions ou tenant default |
| **Probabilidade** | ALTA — 3 módulos têm seeds em HasData() (Identity, Governance, OpIntel) |
| **Mitigação** | Extrair seeds para seeders programáticos ANTES de remover migrations. Verificação obrigatória na pré-condição. |

---

### R-11. Rollback impossível após remoção

| Aspecto | Detalhe |
|---------|---------|
| **Descrição** | Após remover migrations e gerar nova baseline, não é possível voltar ao estado anterior |
| **Impacto** | 🟡 MÉDIO — risco mitigável com tag + backup |
| **Probabilidade** | BAIXA — se tag/backup forem feitos |
| **Mitigação** | Tag `pre-migration-reset-v1` obrigatória. Backup do schema DDL. Backup dos seeds. Teste em ambiente isolado antes de produção. |

---

### R-12. CI/CD pipelines quebram

| Aspecto | Detalhe |
|---------|---------|
| **Descrição** | Pipelines de CI/CD que aplicam migrations ou fazem database setup deixam de funcionar |
| **Impacto** | 🟡 MÉDIO — bloqueio temporário do pipeline |
| **Probabilidade** | ALTA — pipelines provavelmente referenciam migrations |
| **Mitigação** | Actualizar pipelines na mesma PR que remove migrations. Testar pipeline em branch antes de merge. |

---

### R-13. ModelSnapshot drift entre módulos

| Aspecto | Detalhe |
|---------|---------|
| **Descrição** | ModelSnapshots ficam dessincronizados entre módulos, causando conflitos |
| **Impacto** | 🟡 MÉDIO — migrations futuras podem gerar SQL incorrecta |
| **Probabilidade** | BAIXA — nova baseline gera snapshot fresco |
| **Mitigação** | Cada baseline gera novo ModelSnapshot. Não manter snapshots antigos. Validar compilação após remoção. |

---

## Resumo de Riscos

| Risco | Impacto | Probabilidade | Score |
|-------|---------|-------------|-------|
| R-04 Fronteiras entre módulos | 🔴 ALTO | ALTA | 🔴 CRÍTICO |
| R-10 Perda de seed data | 🔴 ALTO | ALTA | 🔴 CRÍTICO |
| R-03 Resíduos de Licensing | 🟡 MÉDIO | ALTA | 🟠 ALTO |
| R-12 CI/CD quebra | 🟡 MÉDIO | ALTA | 🟠 ALTO |
| R-01 Apagar cedo demais | 🔴 ALTO | MÉDIA | 🟠 ALTO |
| R-02 Baseline prematura | 🔴 ALTO | MÉDIA | 🟠 ALTO |
| R-07 Dependências mal ordenadas | 🔴 ALTO | BAIXA | 🟡 MÉDIO |
| R-06 Seeds inconsistentes | 🟡 MÉDIO | MÉDIA | 🟡 MÉDIO |
| R-08 ClickHouse mal introduzido | 🟡 MÉDIO | MÉDIA | 🟡 MÉDIO |
| R-09 Documentação insuficiente | 🟡 MÉDIO | MÉDIA | 🟡 MÉDIO |
| R-11 Rollback impossível | 🟡 MÉDIO | BAIXA | 🟢 BAIXO |
| R-05 Dados misturados PG/CH | 🟡 MÉDIO | BAIXA | 🟢 BAIXO |
| R-13 ModelSnapshot drift | 🟡 MÉDIO | BAIXA | 🟢 BAIXO |

---

## Top 5 Acções de Mitigação Prioritárias

1. **Resolver OI-01 a OI-04** — Extrair Contracts, Integrations, Product Analytics e Environment Management antes de qualquer baseline
2. **Extrair seeds de HasData()** — Criar seeders programáticos para Identity, Governance e OpIntel antes de remover migrations
3. **Limpar resíduos Licensing** — Remover 17 permissões e referências antes da baseline Identity
4. **Tag + backup** — Criar tag `pre-migration-reset-v1` e exportar DDL antes de iniciar
5. **Actualizar CI/CD** — Preparar pipelines para novo workflow de migrations na mesma PR
