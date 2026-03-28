# Relatório de Remoção, Arquivamento e Consolidação — NexTraceOne
**Auditoria Forense | 28 de Março de 2026**

---

## Objetivo

Identificar artefatos do repositório que devem ser removidos, arquivados ou consolidados para reduzir confusão, eliminar resíduos técnicos e manter o repositório limpo e alinhado com a visão do produto.

---

## Princípio de Avaliação

> Não remover funcionalidade relevante apenas porque está incompleta.
> Remover apenas quando: ausência total de uso real, duplicação desnecessária, tecnologia abandonada, ou contradição com a visão oficial sem valor histórico.

---

## REMOVE_CANDIDATE — Remoção Segura Recomendada

### 1. Scripts Mortos ou Substituídos

| Ficheiro | Motivo | Evidência |
|---|---|---|
| `scripts/root-level/fix-pagination-defaults.ps1` | Script pontual — objetivo já alcançado | Não referenciado em pipelines |

**Ação:** Verificar se ainda é necessário; remover se o fix já foi aplicado.

---

## ARCHIVE_CANDIDATE — Arquivar para Referência Histórica

### 1. Relatórios de Fases e Waves Anteriores

**Localização:** `docs/audits/`

**Ficheiros (30+):**
- `PHASE-0-*`, `PHASE-1-*` a `PHASE-9-*` (10 reports)
- `WAVE-1-*` a `WAVE-FINAL-*` (5 reports)
- `NOTIFICATIONS-PHASE-*` (6 reports)
- `CONFIGURATION-PHASE-*` (8 reports)
- `NEXTRACEONE-*` (6 reports)

**Motivo:** Estes relatórios têm valor histórico de auditoria mas não refletem o estado atual. Mantê-los em `docs/audits/` pode confundir quem consulta estado atual. A referência definitiva é `docs/audit-forensic-2026-03/`.

**Ação recomendada:** Mover para `docs/archive/audits/` com um `README.md` que explica que estes são relatórios históricos de fases passadas.

**Não apagar:** Mantêm rastreabilidade de decisões e evolução do produto.

### 2. Execution Prompts Gerados

**Localização:** `docs/execution-prompts/generated/`

**Motivo:** Prompts gerados para execução de tarefas específicas. Valor operacional baixo após a tarefa concluída.

**Ação recomendada:** Mover para `docs/archive/execution-prompts/` ou remover se sem valor documental.

### 3. Protótipos

**Localização:** `docs/prototype/pdfs/`

**Motivo:** PDFs de protótipo visual não são código e têm baixo valor em repositório de código.

**Ação recomendada:** Avaliar se devem ficar em repositório de design separado ou `docs/archive/prototype/`.

---

## CONSOLIDATE_CANDIDATE — Consolidar Fontes Duplicadas

### 1. Múltiplas Fontes de "Estado Atual"

**Fontes existentes:**
- `docs/audit-forensic-2026-03/final-project-state-assessment.md` (principal)
- `docs/IMPLEMENTATION-STATUS.md` (atualizado)
- `docs/current-state/*.md` (10 ficheiros por módulo)
- `docs/ROADMAP.md` (atualizado)
- `docs/audits/NEXTRACEONE-CURRENT-STATE-AND-100-PERCENT-GAP-REPORT.md` (desatualizado)

**Ação:** Manter `audit-forensic-2026-03/` como fonte definitiva. `IMPLEMENTATION-STATUS.md` e `ROADMAP.md` mantêm-se como documentos de referência rápida. `docs/audits/` → `docs/archive/audits/`. `docs/current-state/` mantém-se para detalhe por módulo.

### 2. AI Architecture Documents

**Documentos relacionados:**
- `docs/AI-GOVERNANCE.md`
- `docs/AI-ARCHITECTURE.md`
- `docs/AI-ASSISTED-OPERATIONS.md`
- `docs/AI-DEVELOPER-EXPERIENCE.md`
- `docs/aiknowledge/` (subdiretório)

**Ação:** Consolidar em estrutura hierárquica clara com distinção explícita entre "estado atual" e "visão/roadmap".

---

## KEEP_AND_COMPLETE — Manter e Concluir

### 1. Módulo Operations (`src/modules/operationalintelligence/`)

**Motivo:** EfIncidentStore (678 linhas), schema real, 21 migrações. Não remover — completar conectando frontend e engine de correlação.

### 2. Módulo Governance (`src/modules/governance/`)

**Motivo:** GovernanceDbContext existe, 3 migrações. Não remover — substituir handlers mock por implementação real. Valor estratégico alto.

### 3. Knowledge Hub (`src/modules/knowledge/`)

**Motivo:** Pequeno mas alinhado à visão. Completar migrações e conectar ao AI context.

### 4. Módulo Integrations (`src/modules/integrations/`)

**Motivo:** Estrutura correta. Completar com implementação real de conectores CI/CD.

### 5. `docs/audits/` (fase 0-9 e waves)

**Motivo:** Valor histórico. Arquivar em vez de remover.

### 6. `e2e-real/` (frontend)

**Motivo:** 5 specs de ambiente real são valiosos — configuração separada intencional. Manter e integrar no CI quando ambientes de teste estiverem prontos.

### 7. `DemoBanner.tsx` (`src/components/`)

**Motivo:** Componente explícito de marcação de áreas de demonstração. Manter para uso enquanto existirem áreas mock; remover quando todo o produto for real.

---

## Resíduos a Eliminar (sem criação de novas features)

### 1. `IsSimulated: true` nos handlers de Governance
**Não é remoção de ficheiro** — é substituição de comportamento. Os ficheiros mantêm-se; o conteúdo muda.

### 2. `mockIncidents` em IncidentsPage.tsx
**Não é remoção de ficheiro** — é substituição da fonte de dados.

### 3. `mockConversations` em AssistantPanel.tsx
**Não é remoção de ficheiro** — é substituição pela API real.

---

## Sumário de Ações

| Ação | Artefatos | Prioridade |
|---|---|---|
| ARCHIVE | `docs/audits/` (30+ relatórios de fases) | Baixa |
| ARCHIVE | `docs/execution-prompts/generated/` | Baixa |
| ARCHIVE | `docs/prototype/pdfs/` | Baixa |
| REMOVE (verificar) | `scripts/root-level/fix-pagination-defaults.ps1` | Baixa |
| CONSOLIDATE | Documentação AI (4 docs) | Média |
| KEEP_AND_COMPLETE | `src/modules/operationalintelligence/` | Crítica |
| KEEP_AND_COMPLETE | `src/modules/governance/` | Alta |
| KEEP_AND_COMPLETE | `src/modules/knowledge/` | Alta |
| KEEP_AND_COMPLETE | `src/modules/integrations/` | Alta |
| KEEP_AND_COMPLETE | `DemoBanner.tsx` (temporariamente) | Baixa |
| KEEP_AND_COMPLETE | `src/frontend/e2e-real/` | Média |

---

*Data: 28 de Março de 2026*
