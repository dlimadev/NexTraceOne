# Lista de Faltas — Prompts N1 a N10

> **Data:** 2026-03-25  
> **Fase:** Validação formal pré-N11  
> **Objectivo:** Listar explicitamente tudo que falta completar antes de avançar para N11

---

## A. Prompts Não Executados (3 prompts, 35 ficheiros)

### N4 — Environment Management (11 ficheiros)

Nenhum dos deliverables do N4 foi criado. O diretório `docs/11-review-modular/02-environment-management/` contém apenas ficheiros pré-existentes das auditorias anteriores (`module-consolidated-review.md`, `module-overview.md`, `README.md`) e subdiretórios de relatórios gerais.

**Ficheiros ausentes:**
1. `current-state-inventory.md`
2. `module-boundary-finalization.md`
3. `module-scope-finalization.md`
4. `domain-model-finalization.md`
5. `persistence-model-finalization.md`
6. `backend-functional-corrections.md`
7. `frontend-functional-corrections.md`
8. `security-and-permissions-review.md`
9. `module-dependency-map.md`
10. `documentation-and-onboarding-upgrade.md`
11. `module-remediation-plan.md`

**Impacto:** Alto — Environment Management é o módulo de isolamento por ambiente. Sem backend dedicado (está parcialmente em Identity), a consolidação é crítica para definir o caminho de extração.

---

### N7 — Change Governance (12 ficheiros)

Nenhum dos deliverables do N7 foi criado. O diretório `docs/11-review-modular/05-change-governance/` contém apenas os ficheiros pré-existentes `module-consolidated-review.md` (21,605 bytes) e `module-review.md` (7,020 bytes).

**Ficheiros ausentes:**
1. `module-role-finalization.md`
2. `module-scope-finalization.md`
3. `end-to-end-flow-validation.md`
4. `domain-model-finalization.md`
5. `persistence-model-finalization.md`
6. `backend-functional-corrections.md`
7. `frontend-functional-corrections.md`
8. `score-and-blast-radius-review.md`
9. `security-and-permissions-review.md`
10. `module-dependency-map.md`
11. `documentation-and-onboarding-upgrade.md`
12. `module-remediation-plan.md`

**Impacto:** Crítico — Change Governance é um pilar central do NexTraceOne (Production Change Confidence). A ausência de score/blast radius review e end-to-end flow validation é especialmente significativa.

---

### N8 — Notifications (12 ficheiros)

Nenhum dos deliverables do N8 foi criado. O diretório `docs/11-review-modular/11-notifications/` contém apenas `module-consolidated-review.md` (4,725 bytes) e `module-review.md` (4,134 bytes).

**Ficheiros ausentes:**
1. `module-role-finalization.md`
2. `module-scope-finalization.md`
3. `end-to-end-delivery-validation.md`
4. `domain-model-finalization.md`
5. `persistence-model-finalization.md`
6. `backend-functional-corrections.md`
7. `frontend-functional-corrections.md`
8. `templates-channels-retries-status-review.md`
9. `security-and-permissions-review.md`
10. `module-dependency-map.md`
11. `documentation-and-onboarding-upgrade.md`
12. `module-remediation-plan.md`

**Impacto:** Médio — O módulo Notifications tem 0 migrations e maturidade inferior. A consolidação é necessária mas é menos urgente que N7 e N10.

---

## B. Prompts Parcialmente Executados (2 prompts, 11 ficheiros em falta)

### N10 — Audit & Compliance (11 ficheiros em falta de 12 esperados)

Apenas `module-role-finalization.md` foi criado (6,002 bytes, 103 linhas). Este ficheiro define adequadamente o papel transversal do módulo, a hash chain SHA-256, e as responsabilidades. Porém, faltam 11 ficheiros essenciais para a consolidação completa.

**Ficheiros ausentes:**
1. `module-scope-finalization.md`
2. `end-to-end-audit-trail-validation.md`
3. `domain-model-finalization.md`
4. `persistence-model-finalization.md`
5. `backend-functional-corrections.md`
6. `frontend-functional-corrections.md`
7. `integrity-retention-and-evidence-review.md`
8. `security-and-permissions-review.md`
9. `module-dependency-map.md`
10. `documentation-and-onboarding-upgrade.md`
11. `module-remediation-plan.md`

**Impacto:** Crítico — Audit & Compliance é transversal. Sem a validação de integridade/retenção/evidências e sem o backlog de correções, o módulo não pode ser considerado pronto.

---

### N9 — Operational Intelligence (0 ficheiros ausentes, 1 possivelmente insuficiente)

Todos os 13 ficheiros existem com conteúdo substantivo. Porém:

**Ficheiro potencialmente insuficiente:**
- `module-remediation-plan.md` — 112 linhas / 7,011 bytes

Comparação com outros módulos:
| Módulo | Linhas | Bytes |
|--------|--------|-------|
| Configuration (N2) | 161 | 9,791 |
| Contracts (N3) | 162 | 8,268 |
| Governance (N5) | 230 | 12,846 |
| Catalog (N6) | 163 | 9,591 |
| **Operational Intelligence (N9)** | **112** | **7,011** |

**Recomendação:** Verificar se as 5 secções obrigatórias (A. Quick Wins, B. Correções Funcionais, C. Ajustes Estruturais, D. Pré-condições para Migrations, E. Critérios de Aceite) estão todas presentes e com profundidade adequada.

---

## C. Ficheiros Ausentes — Resumo por Tipo

| Tipo de Ficheiro | N4 | N7 | N8 | N10 | Total Ausente |
|-----------------|----|----|----|----|---------------|
| `module-role-finalization.md` | — | ❌ | ❌ | ✅ | 2 |
| `module-scope-finalization.md` | ❌ | ❌ | ❌ | ❌ | 4 |
| `domain-model-finalization.md` | ❌ | ❌ | ❌ | ❌ | 4 |
| `persistence-model-finalization.md` | ❌ | ❌ | ❌ | ❌ | 4 |
| `backend-functional-corrections.md` | ❌ | ❌ | ❌ | ❌ | 4 |
| `frontend-functional-corrections.md` | ❌ | ❌ | ❌ | ❌ | 4 |
| `security-and-permissions-review.md` | ❌ | ❌ | ❌ | ❌ | 4 |
| `documentation-and-onboarding-upgrade.md` | ❌ | ❌ | ❌ | ❌ | 4 |
| `module-remediation-plan.md` | ❌ | ❌ | ❌ | ❌ | 4 |
| `module-dependency-map.md` | ❌ | ❌ | ❌ | ❌ | 4 |
| Ficheiro específico do módulo | ❌¹ | ❌² | ❌³ | ❌⁴ | 4 |
| Ficheiro específico do módulo (2º) | ❌⁵ | ❌⁶ | — | — | 2 |

¹ `current-state-inventory.md` ² `end-to-end-flow-validation.md` ³ `end-to-end-delivery-validation.md` ⁴ `end-to-end-audit-trail-validation.md` ⁵ `module-boundary-finalization.md` ⁶ `score-and-blast-radius-review.md`

Ficheiros específicos adicionais: `integrity-retention-and-evidence-review.md` (N10), `templates-channels-retries-status-review.md` (N8)

---

## D. Ficheiros Vazios ou Só com Template

Nenhum ficheiro vazio ou com apenas template foi encontrado. Todos os ficheiros existentes contêm conteúdo substantivo.

---

## E. Ficheiros com Conteúdo Potencialmente Fraco

| Ficheiro | Prompt | Bytes | Linhas | Observação |
|----------|--------|-------|--------|------------|
| `06-operational-intelligence/module-remediation-plan.md` | N9 | 7,011 | 112 | Menor que equivalentes (média ~10K bytes). Verificar profundidade das 5 secções |

---

## Acções Recomendadas Antes de N11

| Prioridade | Acção | Esforço Estimado |
|-----------|-------|-----------------|
| 🔴 P0 | Executar N7 (Change Governance) completo — 12 ficheiros | Alto |
| 🔴 P0 | Executar N10 (Audit & Compliance) — 11 ficheiros restantes | Alto |
| 🟠 P1 | Executar N4 (Environment Management) completo — 11 ficheiros | Alto |
| 🟡 P2 | Executar N8 (Notifications) completo — 12 ficheiros | Médio |
| 🟢 P3 | Enriquecer N9 remediation plan se necessário | Baixo |
