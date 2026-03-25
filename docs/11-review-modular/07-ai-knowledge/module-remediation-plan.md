# PARTE 14 — Module Remediation Plan

> **Módulo:** AI & Knowledge (07)
> **Prefixo:** `aik_`
> **Data:** 2026-03-25
> **Prompt:** N13
> **Maturidade atual:** ~25% (backend 25%, frontend 70%, docs 35%, tests 10%)
> **Maturidade alvo:** 70%

---

## Sumário executivo

O módulo AI & Knowledge é o mais ambicioso e complexo do NexTraceOne, com 40+ entidades, 3 DbContexts, 68+ CQRS features e 54+ endpoints. A maturidade real é ~25%, com um gap radical entre frontend (70%) e backend (25%). Os problemas críticos são: (1) tools de agents não executam, (2) streaming de chat ausente, (3) retrieval possivelmente stub, (4) zero domain events publicados, (5) documentação otimista.

**Total de itens:** 55
**Esforço estimado:** ~325h (~40 dias de trabalho, ~8 sprints)

---

## A. Quick Wins (10 itens, ~20h)

| # | ID | Item | Prioridade | Esforço | Sprint |
|---|-----|------|------------|---------|--------|
| 1 | QW-01 | Remover mock response generator do AssistantPanel.tsx | P0 | 2h | S1 |
| 2 | QW-02 | Esconder IDE Integrations no menu (marcar "coming soon") | P1 | 1h | S1 |
| 3 | QW-03 | Reduzir menu AI de 9 para 6 items | P1 | 2h | S1 |
| 4 | QW-04 | Ativar botão de registo de modelo no frontend (ou remover) | P2 | 2h | S1 |
| 5 | QW-05 | Substituir JSON bruto por apresentação legível em Agent Detail | P2 | 2h | S1 |
| 6 | QW-06 | Substituir JSON bruto por apresentação legível em Routing page | P2 | 2h | S1 |
| 7 | QW-07 | Adicionar empty states com mensagens i18n em todas as páginas AI | P2 | 2h | S1 |
| 8 | QW-08 | Atualizar agents-README.md para refletir estado real | P2 | 1h | S1 |
| 9 | QW-09 | Atualizar ai-core-README.md para refletir estado real | P2 | 1h | S1 |
| 10 | QW-10 | Verificar encriptação de ApiKey em AiProvider | P0 | 3h | S1 |

---

## B. Correções Funcionais Obrigatórias (20 itens, ~170h)

| # | ID | Item | Prioridade | Esforço | Sprint | Ficheiro/Área |
|---|-----|------|------------|---------|--------|---------------|
| 11 | CF-01 | Implementar streaming SSE para chat | P1 | 16h | S2 | `ExecuteAiChat`, `AiRuntimeEndpointModule` |
| 12 | CF-02 | Implementar framework de tool calling (IToolRegistry, IToolDispatcher) | P1 | 24h | S2-S3 | Novo `Tools/` namespace |
| 13 | CF-03 | Implementar execução real de tools em ExecuteAgent | P1 | 16h | S3 | `ExecuteAgent.cs` |
| 14 | CF-04 | Validar e corrigir serviços de retrieval | P1 | 16h | S3 | `EnrichContext`, retrieval services |
| 15 | CF-05 | Implementar enforcement de quota de tokens | P1 | 8h | S3 | `AiTokenQuotaPolicy`, middleware |
| 16 | CF-06 | Implementar enforcement de orçamento antes de execução | P1 | 8h | S3 | `AIBudget`, pre-execution check |
| 17 | CF-07 | Adicionar campos ausentes a AiAgent (MaxTokens, Timeout, RequiresApproval) | P2 | 4h | S2 | `AiAgent.cs` |
| 18 | CF-08 | Completar health check de providers (teste de conectividade real) | P2 | 4h | S4 | `CheckAiProvidersHealth` |
| 19 | CF-09 | Implementar streaming UI no frontend para chat | P1 | 8h | S2 | `AiAssistantPage.tsx` |
| 20 | CF-10 | Validar 3+ features de Orchestration ponta a ponta | P2 | 16h | S4 | `Application/Orchestration/Features/` |
| 21 | CF-11 | Implementar scope check por UserId para histórico de conversas | P1 | 4h | S2 | Conversation query handlers |
| 22 | CF-12 | Garantir token usage registration em todos os fluxos (chat, agent, external) | P2 | 4h | S3 | Todos os handlers de execução |
| 23 | CF-13 | Validar External AI query ponta a ponta | P2 | 8h | S4 | `QueryExternalAISimple/Advanced` |
| 24 | CF-14 | Implementar IAiAgentAuthorizationService | P1 | 8h | S3 | Novo service |
| 25 | CF-15 | Melhorar UX de execução de agent (loading, resultado) | P2 | 4h | S4 | `AgentDetailPage.tsx` |
| 26 | CF-16 | Validar integração com API real em AiAnalysisPage | P2 | 4h | S4 | `AiAnalysisPage.tsx` |
| 27 | CF-17 | Validar todas as páginas para i18n completo | P2 | 4h | S3 | Todas as páginas AI |
| 28 | CF-18 | Revisar error handling em todas as chamadas API frontend | P3 | 4h | S5 | `aiGovernance.ts` |
| 29 | CF-19 | Implementar rate limiting por utilizador | P3 | 8h | S5 | Middleware |
| 30 | CF-20 | Implementar fallback de provider em caso de falha | P2 | 8h | S4 | Runtime execution |

---

## C. Ajustes Estruturais (18 itens, ~105h)

### C.1 Persistência e prefixo aik_

| # | ID | Item | Prioridade | Esforço | Sprint |
|---|-----|------|------------|---------|--------|
| 31 | ST-01 | Definir nomes finais de todas as tabelas com prefixo aik_ | P1 | 2h | S2 |
| 32 | ST-02 | Consolidar AiSource + AIKnowledgeSource numa única entidade | P2 | 4h | S3 |
| 33 | ST-03 | Remover AiConversation duplicada de Orchestration | P2 | 2h | S3 |
| 34 | ST-04 | Consolidar KnowledgeCapture + KnowledgeCaptureEntry | P2 | 4h | S3 |
| 35 | ST-05 | Documentar exceção de 3 DbContexts (justificativa) | P2 | 1h | S2 |
| 36 | ST-06 | Validar RowVersion (xmin) em todas as entity configurations | P2 | 4h | S3 |
| 37 | ST-07 | Adicionar índices obrigatórios definidos no modelo final | P2 | 4h | S4 |

### C.2 Permissões e capabilities

| # | ID | Item | Prioridade | Esforço | Sprint |
|---|-----|------|------------|---------|--------|
| 38 | ST-08 | Adicionar ai:agents:read/execute/manage a RolePermissionCatalog | P1 | 4h | S2 |
| 39 | ST-09 | Adicionar ai:external:read/write a RolePermissionCatalog | P2 | 2h | S3 |
| 40 | ST-10 | Implementar tool permission validation no agent execution | P1 | 8h | S3 |
| 41 | ST-11 | Corrigir permissões em rotas do frontend (agents, IDE, analysis) | P2 | 2h | S2 |

### C.3 Domain events e integração

| # | ID | Item | Prioridade | Esforço | Sprint |
|---|-----|------|------------|---------|--------|
| 42 | ST-12 | Publicar domain events para Audit & Compliance | P2 | 8h | S4 |
| 43 | ST-13 | Publicar alertas de orçamento para Notifications | P2 | 4h | S5 |
| 44 | ST-14 | Publicar métricas de uso de IA para Product Analytics | P3 | 4h | S6 |

### C.4 ClickHouse preparation

| # | ID | Item | Prioridade | Esforço | Sprint |
|---|-----|------|------------|---------|--------|
| 45 | ST-15 | Definir schema de eventos ClickHouse para token usage | P3 | 4h | S5 |
| 46 | ST-16 | Preparar dual-write pattern para log entities | P3 | 8h | S6 |
| 47 | ST-17 | Definir schema de eventos ClickHouse para agent executions | P3 | 4h | S6 |

### C.5 Documentação

| # | ID | Item | Prioridade | Esforço | Sprint |
|---|-----|------|------------|---------|--------|
| 48 | ST-18 | Criar documento de arquitetura interna (4 subdomínios) | P1 | 4h | S2 |

---

## D. Pré-condições para Recriar Migrations (4 itens, ~8h)

| # | ID | Item | Sprint |
|---|-----|------|--------|
| 49 | PM-01 | Todas as entidades consolidadas (ST-02, ST-03, ST-04) | S3 |
| 50 | PM-02 | Campos ausentes adicionados (CF-07) | S2 |
| 51 | PM-03 | Nomes de tabelas finais com aik_ definidos (ST-01) | S2 |
| 52 | PM-04 | Índices, FKs e constraints definidos (ST-06, ST-07) | S4 |

**Quando PM-01 a PM-04 estiverem completos:**
1. Apagar todas as 9 migrations existentes
2. Reconsolidar para 1-3 DbContexts (decisão documentada)
3. Gerar novas migrations baseline com prefixo aik_
4. Validar schema gerado vs modelo final

---

## E. Critérios de Aceite do Módulo (13 critérios)

| # | Critério | Bloqueador? |
|---|----------|------------|
| AC-01 | Chat funcional com streaming | ✅ Sim |
| AC-02 | Tools de agents executam realmente | ✅ Sim |
| AC-03 | Mock response generator removido | ✅ Sim |
| AC-04 | ApiKey encriptada em providers | ✅ Sim |
| AC-05 | Permissões granulares para agents (ai:agents:*) | ✅ Sim |
| AC-06 | Enforcement de quota/orçamento | ⚠️ Importante |
| AC-07 | Retrieval funcional com pelo menos 1 fonte real | ⚠️ Importante |
| AC-08 | Pelo menos 3 features de Orchestration E2E | ⚠️ Importante |
| AC-09 | Domain events publicados para Audit | ⚠️ Importante |
| AC-10 | Menu reduzido para 6 items | ⚠️ Desejável |
| AC-11 | i18n completo em todas as páginas | ⚠️ Desejável |
| AC-12 | Documentação de arquitetura interna | ⚠️ Importante |
| AC-13 | Modelo de persistência final com aik_ definido | ✅ Sim |

**Bloqueadores absolutos:** AC-01, AC-02, AC-03, AC-04, AC-05, AC-13

---

## F. Cronograma por sprint

| Sprint | Foco | Itens | Horas |
|--------|------|-------|-------|
| S1 | Quick wins + segurança imediata | QW-01..QW-10 | 20h |
| S2 | Streaming + tool framework + permissões | CF-01, CF-02 início, CF-07, CF-09, CF-11, ST-01, ST-05, ST-08, ST-11, ST-18 | 58h |
| S3 | Tool execution + retrieval + consolidação | CF-02 final, CF-03, CF-04, CF-05, CF-06, CF-12, CF-14, CF-17, ST-02, ST-03, ST-04, ST-06, ST-09, ST-10 | 80h |
| S4 | Orchestration E2E + health + events | CF-08, CF-10, CF-13, CF-15, CF-16, CF-20, ST-07, ST-12 | 56h |
| S5 | Rate limiting + notifications + ClickHouse prep | CF-18, CF-19, ST-13, ST-15 | 24h |
| S6 | ClickHouse + analytics + finalização | ST-14, ST-16, ST-17 | 16h |
| S7-S8 | Testes, documentação completa, polish | Testes + XML docs + onboarding | ~70h |

---

## G. Prioridade geral por categoria

| Categoria | Itens | Horas | % Total |
|-----------|-------|-------|---------|
| Quick wins | 10 | 20h | 6% |
| Correções funcionais | 20 | 170h | 52% |
| Ajustes estruturais | 18 | 105h | 32% |
| Pré-condições migrations | 4 | 8h | 3% |
| **Testes + docs (estimativa)** | — | ~22h | 7% |
| **TOTAL** | **55** | **~325h** | **100%** |

---

## H. Riscos

| # | Risco | Probabilidade | Impacto | Mitigação |
|---|-------|--------------|---------|-----------|
| R-01 | Tool calling framework complexo demais | 🟠 Média | 🔴 Alto | Começar com 2-3 tools simples, expandir incrementalmente |
| R-02 | Streaming complexo de implementar | 🟡 Baixa | 🟠 Médio | SSE é standard; usar pattern existente do mercado |
| R-03 | Retrieval depender de vector store | 🟠 Média | 🔴 Alto | Começar com retrieval simples (SQL queries), evoluir para embeddings |
| R-04 | 3 DbContexts dificultam migrations | 🟡 Baixa | 🟠 Médio | Documentar como exceção justificada |
| R-05 | Dependências cross-module instáveis | 🟠 Média | 🟠 Médio | Validar integrações E2E antes de depender delas |
