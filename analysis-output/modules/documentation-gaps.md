# Documentação — Gaps, Erros e Pendências

## 1. Estado resumido
80+ ficheiros de documentação em `docs/`. Documentação extensiva mas com **contradições graves** entre documentos de Março 2026 e o estado real do código. Documentos activos contêm informação factualmente incorreta.

## 2. Gaps críticos

### 2.1 `docs/IMPLEMENTATION-STATUS.md` — Dramaticamente Desactualizado
- **Severidade:** CRITICAL
- **Classificação:** DOC_CONTRADICTION
- **Descrição:** Este ficheiro é referenciado como fonte de verdade de implementação mas contém **múltiplas afirmações factualmente incorretas**:
  - §CrossModule: 7 de 8 interfaces marcadas como PLAN — **pelo menos 11 de 14 estão IMPLEMENTED**
  - §OperationalIntelligence: "correlação quebrada, frontend mock" — **FALSO** (frontend usa API real, correlação funciona)
  - §AIKnowledge: "sem LLM real E2E" — **FALSO** (SendAssistantMessage usa IChatCompletionProvider real)
  - §Governance: "SIM (por design)" — **FALSO** (handlers usam ICostIntelligenceModule real)
  - §ProductAnalytics: "100% mock, Handlers mock" — **FALSO** (handlers usam repository real)
  - §Knowledge: "sem migrações confirmadas" — **FALSO** (migration exists)
  - §Integrations: "sem migrações confirmadas" — **FALSO** (migration exists)
  - §ProductAnalytics: "sem migrações confirmadas" — **FALSO** (migration exists)
  - §Outbox: "Apenas IdentityDbContext tem processamento ativo" — **FALSO** (21 DbContexts registados)
- **Impacto:** Qualquer decisão de roadmap baseada neste documento será incorrecta. Novos contribuidores serão enganados.
- **Evidência:** `docs/IMPLEMENTATION-STATUS.md` — última actualização declarada: "Março 2026"

### 2.2 `docs/CORE-FLOW-GAPS.md` — Factualmente Incorreto
- **Severidade:** CRITICAL
- **Classificação:** DOC_CONTRADICTION
- **Descrição:** Afirmações incorretas confirmadas:
  - §Flow 3: "State: 0% functional" — **FALSO**
  - §Flow 3: "Frontend not connected — IncidentsPage.tsx uses mockIncidents hardcoded inline" — **FALSO**
  - §Flow 3: "GetMitigationHistory returns fixed hardcoded data" — **FALSO**
  - §Flow 3: "RecordMitigationValidation discards data" — **FALSO**
  - §Flow 4: "SendAssistantMessage returns hardcoded responses — no real LLM invoked" — **FALSO**
  - §Flow 4: "AiAssistantPage.tsx uses mockConversations" — **FALSO**
  - §Flow 4: "IExternalAiModule = PLAN (empty interface)" — **FALSO** (IMPLEMENTED)
- **Impacto:** Mesmos impactos do 2.1.
- **Evidência:** `docs/CORE-FLOW-GAPS.md` — múltiplas secções

## 3. Gaps altos

### 3.1 `docs/archive/audit-forensic-2026-03/frontend-state-report.md` — Desactualizado mas referenciado
- **Severidade:** HIGH
- **Classificação:** DOC_CONTRADICTION
- **Descrição:** O relatório frontend de Março 2026 (fornecido como contexto) afirma:
  - "IncidentsPage 100% mock" — **FALSO**
  - "AiAssistantPage 100% mock conversations" — **FALSO**
  - "83% das páginas sem EmptyState padronizado" — parcialmente correcto (67% na verificação actual)
  - "96% das páginas sem error states por secção" — **FALSO** (72% têm error handling na verificação actual)
  - "Governance/FinOps 25 páginas com DemoBanner" — **FALSO** (DemoBanner não é importado por nenhuma feature page)
  - "9 páginas com mock inline" — **FALSO** (zero mock inline em produção)
- **Impacto:** Relatório está em `docs/archive/` mas é referenciado como fonte de verdade em documentos activos.
- **Evidência:** `docs/archive/audit-forensic-2026-03/frontend-state-report.md`

### 3.2 Documentação de Deployment Incompleta
- **Severidade:** HIGH
- **Classificação:** INCOMPLETE
- **Descrição:** Documentação de deployment para produção (`docs/deployment/`, `docs/runbooks/`) existe mas:
  - Bootstrap mínimo de produção não documentado
  - Lista completa de connection strings não documentada
  - Seed strategy por ambiente não documentada
  - OTEL endpoint obrigatório em produção não documentado explicitamente

## 4. Gaps médios

### 4.1 Documentação de Módulos Não Actualizada
- **Severidade:** MEDIUM
- **Classificação:** DOC_CONTRADICTION
- **Descrição:** Docs individuais de módulos (user guide, architecture) podem conter informação desactualizada se baseados nos relatórios de Março 2026.
- **Evidência:** `docs/user-guide/`, `docs/MODULES-AND-PAGES.md`

## 5. Itens mock / stub / placeholder
N/A — documentação não tem mocks.

## 6. Erros de desenho / implementação incorreta
- Documentação activa referencia documentos arquivados como fonte de verdade
- `IMPLEMENTATION-STATUS.md` deveria ser gerado automaticamente ou ter processo de actualização regular

## 7-12. N/A

## 13. Ações corretivas obrigatórias
1. **CRÍTICO:** Reescrever `docs/IMPLEMENTATION-STATUS.md` com estado real verificado
2. **CRÍTICO:** Reescrever `docs/CORE-FLOW-GAPS.md` com estado real verificado
3. **ALTO:** Documentar bootstrap mínimo de produção
4. **ALTO:** Documentar lista completa de connection strings
5. **MÉDIO:** Adicionar disclaimer "ARCHIVED — outdated" claro nos documentos de `docs/archive/`
6. **MÉDIO:** Criar processo regular de actualização de `IMPLEMENTATION-STATUS.md`
