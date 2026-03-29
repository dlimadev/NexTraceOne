# Onda 10 — Workflow, Aprovação e Políticas Legacy

> **Duração estimada:** 2-3 semanas
> **Dependências:** Onda 6
> **Risco:** Baixo — reutiliza infraestrutura existente
> **Referência:** [LEGACY-MAINFRAME-WAVES.md](../LEGACY-MAINFRAME-WAVES.md)

---

## Objetivo

Workflows de aprovação e políticas específicas para mudanças em core systems. Inclui templates para CAB, freeze windows para mainframe, e governance rules para contratos legacy.

---

## Entregáveis

- [ ] Workflow templates para mudanças legacy (copybook change, batch change, MQ change)
- [ ] Políticas de freeze para mainframe (janelas operacionais)
- [ ] Approval gates específicos para CAB mainframe
- [ ] Evidence pack generation para mudanças legacy
- [ ] Promotion gates para promoção de mudanças em ambientes mainframe
- [ ] Regras de governança para contratos legacy

---

## Impacto Backend

### Workflow Templates para Legacy

Templates pré-configurados que podem ser customizados:

#### Template: "Copybook Change Approval"

```
Stages:
1. Technical Review (Engineer/Tech Lead)
   - Automaticamente gera diff semântico
   - Verifica se é breaking change
   - Anexa lista de programas afetados

2. Impact Assessment (Architect)
   - Blast radius review
   - Cross-platform impact check
   - SLA impact evaluation

3. CAB Approval (Change Advisory Board)
   - CAB summary auto-gerado
   - Evidence pack completo
   - Votação multi-approver

4. Schedule Confirmation (Operations)
   - Validação de janela operacional
   - Confirmação de rollback plan
```

#### Template: "Batch Job Change"

```
Stages:
1. Technical Review
   - JCL review
   - Dependency check (chains afetadas)

2. SLA Impact Assessment
   - Verificação de SLA de jobs afetados
   - Baseline comparison

3. Operations Approval
   - Janela operacional confirmada
   - Rollback plan verificado
```

#### Template: "MQ Configuration Change"

```
Stages:
1. Technical Review
   - Queue/channel configuration review
   - Consumer/producer impact check

2. Architecture Review
   - Topology impact assessment
   - Performance impact evaluation

3. Operations Approval
   - Scheduling confirmation
```

### Freeze Windows para Mainframe

Extensão do `FreezeWindow` existente com:

| Scope | Descrição |
|---|---|
| `MainframeAll` | Freeze em todos os ativos mainframe |
| `BatchWindow` | Freeze durante janela de batch (e.g., 22h-06h) |
| `EodProcessing` | Freeze durante end-of-day processing |
| `MonthEnd` | Freeze durante month-end processing |
| `YearEnd` | Freeze durante year-end processing |
| `SpecificSystem` | Freeze num LPAR/sysplex específico |

### Governance Rules para Contratos Legacy

Regras automáticas:

| Regra | Condição | Acção |
|---|---|---|
| `copybook-breaking-requires-cab` | Copybook change classificado como Breaking | Workflow CAB obrigatório |
| `batch-sla-critical-requires-approval` | Mudança em batch com SLA Critical | Workflow de aprovação |
| `mq-config-requires-arch-review` | Mudança em configuração MQ | Architecture review obrigatório |
| `freeze-blocks-production` | Dentro de freeze window | Block mudança |
| `cross-platform-requires-impact` | Mudança com `CrossesPlatformBoundary` | Impact assessment obrigatório |

### Promotion Gates para Ambientes Mainframe

Extensão dos `PromotionGate` existentes:

| Gate | Condição |
|---|---|
| `legacy-tests-passed` | Testes unitários COBOL passaram |
| `copybook-compatibility-check` | Diff de copybook sem breaking changes não autorizadas |
| `batch-sla-verified` | SLA de batch jobs impactados verificado |
| `freeze-window-clear` | Nenhum freeze window ativo |
| `cab-approved` | Aprovação CAB obtida |

---

## Impacto Frontend

### Configuração de Workflow Templates

**Extensão de:** `/workflow`
- Lista de templates inclui templates legacy
- Configuração de stages com approvers

### Freeze Window Management

**Extensão de:** Configuração de freeze windows
- Novos scopes para mainframe
- Calendar view com freeze windows marcadas
- Presets para batch window, EOD, month-end

### Evidence Pack Viewer

**Extensão de:** `/changes/legacy-impact/:changeId`
- Botão "Download Evidence Pack"
- Preview inline do evidence pack
- Secções: Impact Analysis, Blast Radius, Risk Score, Approvals

---

## Impacto Base de Dados

Extensões em tabelas existentes:

| Tabela | Alteração |
|---|---|
| `chg_workflow_templates` | Novos templates (seed data) |
| `chg_freeze_windows` | Novos `FreezeScope` values para mainframe |
| `chg_promotion_gates` | Novos gate types para legacy |
| `chg_ruleset_bindings` | Novas rules para contratos legacy |

---

## Testes

### Testes Unitários (~20)
- Workflow template instantiation com stages legacy
- Freeze window evaluation com scopes mainframe
- Gate evaluation para condições legacy
- Rule matching para contratos legacy

### Testes de Integração (~10)
- Workflow completo: copybook change → review → CAB → approve
- Freeze window blocks mudança em produção
- Promotion gate verifica condições legacy

---

## Critérios de Aceite

1. ✅ Workflow completo funcional para aprovação de mudança em copybook
2. ✅ Freeze window bloqueia mudanças em janela proibida
3. ✅ Evidence pack inclui impacto, programas afetados, blast radius
4. ✅ Promotion gates verificam SLA, testes, aprovações
5. ✅ CAB summary auto-gerado com linguagem executiva
6. ✅ Governance rules automáticas (breaking copybook → CAB required)
7. ✅ Templates customizáveis por tenant

---

## Riscos

| Risco | Severidade | Mitigação |
|---|---|---|
| Templates genéricos podem não servir todos os clientes | Baixa | Templates customizáveis. Exemplos como baseline |
| Freeze window scheduling complexa | Baixa | Presets + custom scheduling |

---

## Stories

| ID | Story | Prioridade |
|---|---|---|
| W10-S01 | Criar template "Copybook Change Approval" | P1 |
| W10-S02 | Criar template "Batch Job Change" | P1 |
| W10-S03 | Criar template "MQ Configuration Change" | P2 |
| W10-S04 | Implementar novos FreezeScope values para mainframe | P1 |
| W10-S05 | Implementar governance rules para contratos legacy | P1 |
| W10-S06 | Implementar promotion gates para legacy | P1 |
| W10-S07 | Criar evidence pack generator para mudanças legacy | P1 |
| W10-S08 | Extensão do frontend com templates e freeze config | P2 |
| W10-S09 | Seed data para templates default | P1 |
| W10-S10 | Testes unitários (~20) | P1 |
| W10-S11 | Testes de integração (~10) | P2 |
