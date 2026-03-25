# Relatório de Remoção, Arquivo e Consolidação — NexTraceOne

**Data:** 25 de março de 2026

---

## 1. Objectivo

Identificar e classificar artefactos que devem ser removidos, arquivados ou consolidados para reduzir dívida técnica e manter o repositório limpo e coerente.

---

## 2. REMOVE_CANDIDATE — Remover com Segurança

### 2.1 Nenhum artefacto de código activo identificado para remoção imediata

Não foram encontrados ficheiros de código activo (C# ou TypeScript) que devam ser removidos de forma imediata. Os módulos incompletos (ExternalAI, Orchestration) são estratégicos e devem ser completados, não removidos.

---

## 3. ARCHIVE_CANDIDATE — Arquivar

### 3.1 Seeds Legados SQL

**Localização:** `docs/architecture/legacy-seeds/`

**Ficheiros:**
- `seed-identity.sql` — usa prefixo `identity_*` (incorrecto: deve ser `iam_*`)
- `seed-catalog.sql` — usa prefixos `eg_*`, `ct_*` (incorrectos)
- `seed-incidents.sql` — usa prefixo `oi_*` (incorrecto: deve ser `ops_inc_*`)
- `seed-audit.sql` — prefixos antigos
- `seed-governance.sql` — prefixos antigos
- `seed-aiknowledge.sql` — prefixos antigos
- `seed-changegovernance.sql` — prefixos antigos

**Justificativa:** Estes ficheiros foram criados antes da normalização de prefixos de tabela. Se executados actualmente, falhariam ou inseririam dados em tabelas inexistentes. Têm valor histórico como referência de dados de seed, mas não devem estar na pasta activa.

**Acção:** Mover para `docs/archive/legacy-seeds/`

**Risco de manter activo:** Alto — operador pode tentar executar e falhar

---

### 3.2 Documentação de Fases Antigas

**Localização:** `docs/architecture/phase-0/` a `docs/architecture/phase-3/`

**Justificativa:** Documentação de fases de evolução antigas (fase 0-3) não é mais relevante para decisões actuais. Tem valor histórico mas não deve estar no fluxo principal de documentação.

**Acção:** Mover para `docs/archive/architecture-phases/`

---

### 3.3 Relatório de Auditoria de Julho 2025 (IA)

**Localização:** `docs/11-review-modular/07-ai-knowledge/` (relatórios de julho 2025)

**Justificativa:** Relatório de julho 2025 indica "75-80% de maturidade" de IA, o que contradiz a auditoria de março 2026 e o estado real do código. Manter activo cria confusão.

**Acção:** Arquivar em `docs/archive/ai-audits/2025-07/` e substituir por relatório actualizado (`ai-agents-governance-report.md` desta auditoria)

---

## 4. CONSOLIDATE_CANDIDATE — Consolidar

### 4.1 GovernanceDbContext — Entidades Temporárias

**Localização:** `src/modules/governance/NexTraceOne.Governance.Infrastructure/Persistence/GovernanceDbContext.cs`

**Entidades a extrair:**
- `IntegrationConnector`
- `IngestionSource`
- `IngestionExecution`
- `AnalyticsEvent`

**Acção:** Criar módulo `integrations` e/ou `productanalytics` com os seus próprios DbContexts. Migrar estas entidades para os novos módulos.

**Timing:** Não é urgente — código funciona. Mas deve ser feito antes de escalar o módulo de integrações.

---

### 4.2 Documentação de Estado de IA

**Ficheiros:**
- `AI-LOCAL-IMPLEMENTATION-AUDIT.md` (raiz)
- Auditorias em `docs/11-review-modular/07-ai-knowledge/`
- `ai-agents-governance-report.md` (este auditoria)

**Problema:** 3 fontes diferentes com afirmações de maturidade contraditórias.

**Acção:** Consolidar num único documento `docs/state/ai-module-state.md` mantido actualizado. Arquivar os outros.

---

### 4.3 docs/11-review-modular/00-governance/ — 110+ ficheiros

**Localização:** `docs/11-review-modular/00-governance/`

**Problema:** 110+ ficheiros cobrem AI audits, backend structural/domain/application/persistence/security audits, database structural/migration audits, frontend structural/permission audits, documentation/onboarding audits, security/tenant isolation audits, consolidation reports — muito fragmentado.

**Acção:** Consolidar em ~10-15 documentos canónicos por tema. Arquivar os individuais.

---

### 4.4 Documentação de Tecnologias Removidas

**Verificar e remover referências a:**
- Redis (não usado — confirmar ausência)
- OpenSearch (não usado — confirmar ausência)
- Temporal (não usado — Quartz.NET em uso)

**Localização provável:** `docs/architecture/` e ADRs

**Acção:** Identificar documentos que referenciam estas tecnologias como activas e actualizá-los ou arquivá-los.

---

## 5. KEEP_AND_COMPLETE — Manter e Completar

Os seguintes artefactos incompletos são **estratégicos e devem ser mantidos**:

| Artefacto | Razão para manter |
|---------|------------------|
| `ExternalAiDbContext` (0 DbSets) | Subdomain estratégico — completar |
| `AiOrchestrationDbContext` | Subdomain estratégico — completar |
| 7 features TODO no ExternalAI | Capacidades críticas de AI governance |
| 8 features TODO no Orchestration | Capacidades de AI assistida (test gen, version suggest) |
| `AssistantPanel.tsx` com mock | Componente real — remover mock, não o componente |
| Seeds legados (valor histórico) | Arquivar, não eliminar |
| `NotificationsDbContext` (3 entidades) | Expandir, não remover |
| `ConfigurationDbContext` (3 entidades) | Expandir, não remover |
| `ReliabilityDbContext` (1 entidade) | Expandir significativamente |

---

## 6. Matriz de Decisão

| Artefacto | Decisão | Justificativa | Prioridade |
|---------|---------|---------------|------------|
| `docs/architecture/legacy-seeds/` | ARCHIVE | Prefixos antigos; risco se executado | P1 |
| `docs/architecture/phase-0-3/` | ARCHIVE | Histórico sem valor operacional | P3 |
| Auditoria IA julho 2025 | ARCHIVE | Contradiz estado actual | P2 |
| GovernanceDbContext 4 entidades temp | CONSOLIDATE | Módulo errado | P2 |
| docs/11-review-modular/00-governance/ | CONSOLIDATE | 110+ ficheiros fragmentados | P3 |
| Refs tecnologias removidas | REMOVE após identificar | Documentação obsoleta | P2 |
| ExternalAiDbContext | KEEP_AND_COMPLETE | Estratégico | P1 |
| AssistantPanel mock | KEEP (remover mock) | Componente estratégico | P0 |
| NotificationsDbContext | KEEP_AND_COMPLETE | Expandir | P2 |
| ConfigurationDbContext | KEEP_AND_COMPLETE | Expandir | P2 |

---

## 7. Recomendações

| Prioridade | Acção |
|-----------|-------|
| P0 | Remover mock response generator de AssistantPanel.tsx (não o componente) |
| P1 | Arquivar `docs/architecture/legacy-seeds/` → `docs/archive/legacy-seeds/` |
| P2 | Criar módulo Integrations e extrair entidades do GovernanceDbContext |
| P2 | Consolidar documentação de estado de IA num único ficheiro |
| P2 | Arquivar auditorias AI de julho 2025 |
| P3 | Arquivar docs de fases antigas (phase-0 a phase-3) |
| P3 | Consolidar docs/11-review-modular/00-governance/ de 110+ para ~15 ficheiros |
| P3 | Identificar e arquivar/remover referências a Redis, OpenSearch, Temporal |
