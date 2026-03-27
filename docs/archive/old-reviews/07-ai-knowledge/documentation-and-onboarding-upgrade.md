# PARTE 13 — Documentation & Onboarding Upgrade

> **Módulo:** AI & Knowledge (07)
> **Data:** 2026-03-25
> **Prompt:** N13

---

## 1. Revisão de documentação existente

### 1.1 module-review.md

| Aspecto | Estado |
|---------|--------|
| Existe | ✅ 158 linhas |
| Conteúdo | ⚠️ Identifica problemas reais (backend 25%, tools cosméticos) |
| Prioridade documentada | P4 — Calibrate Expectations |
| Recomendações | ✅ Define ações concretas |
| Precisão | ⚠️ Algumas métricas discutíveis (frontend 98% — realidade ~70%) |

**Ação necessária:** Atualizar com resultados do N13. Corrigir métricas de frontend.

### 1.2 module-consolidated-review.md

| Aspecto | Estado |
|---------|--------|
| Existe | ✅ 200+ linhas |
| Conteúdo | ✅ Audit detalhado com CR-1, CR-2, CR-3 identificados |
| Score | 71% — BEM_ALINHADO |
| Breakdown | Backend 25%, Frontend 70%, Docs 65%, Tests 10% |
| Critical findings | ✅ Tools cosmético (CR-2), backend gap (CR-1), docs optimistic (CR-3) |

**Ação necessária:** Adicionar referência ao N13 como follow-up.

### 1.3 Outros documentos existentes

| Ficheiro | Estado | Ação |
|----------|--------|------|
| `agents-README.md` | ⚠️ Pode prometer mais do que existe | Calibrar expectativas |
| `ai-core-README.md` | ⚠️ Pode prometer mais do que existe | Calibrar expectativas |
| `agents-module-overview.md` | ⚠️ Overview pode estar desatualizado | Atualizar |
| `ai-core-module-overview.md` | ⚠️ Overview pode estar desatualizado | Atualizar |
| `backend/endpoints.md` | ⚠️ Verificar se lista todos os 54+ endpoints | Completar |
| `backend/authorization-rules.md` | ⚠️ Verificar se reflete gaps de SEC-01..SEC-06 | Atualizar |
| `backend/domain-rules.md` | ⚠️ Verificar regras de negócio | Atualizar |
| `backend/validation-rules.md` | ⚠️ Verificar | Atualizar |
| `database/schema-review.md` | ⚠️ Verificar vs modelo final aik_ | Atualizar |
| `database/migrations-review.md` | ⚠️ 9 migrations com debt | Documentar debt |
| `quality/bugs-and-gaps.md` | ⚠️ Verificar vs N13 findings | Completar |
| `quality/technical-debt.md` | ⚠️ Verificar vs N13 findings | Completar |
| `documentation/code-comments-review.md` | ⚠️ Verificar | Atualizar |
| `documentation/developer-onboarding-notes.md` | ⚠️ Verificar | Atualizar |

---

## 2. Documentação ausente

| # | Documento | Criticidade | Conteúdo esperado |
|---|----------|-------------|-------------------|
| DOC-01 | Arquitetura interna do módulo (4 subdomínios) | 🔴 ALTA | Diagrama de subdomínios, responsabilidades, dependências |
| DOC-02 | Guia de adição de novo model/provider | 🔴 ALTA | Passo a passo para configurar novo LLM |
| DOC-03 | Guia de criação de agent | 🔴 ALTA | Passo a passo, AllowedTools, SystemPrompt |
| DOC-04 | Fluxo E2E de chat (diagrama) | 🟠 ALTA | Diagrama de sequência do chat |
| DOC-05 | Fluxo E2E de agent execution (diagrama) | 🟠 ALTA | Diagrama de sequência da execução |
| DOC-06 | Modelo de dados completo (ER diagram) | 🟠 ALTA | Diagrama ER com todas as entidades |
| DOC-07 | Guia de tool calling (quando implementado) | 🟡 MÉDIA | Framework de tools, como adicionar tools |
| DOC-08 | Guia de segurança e capabilities | 🟠 ALTA | Permissões, políticas, capabilities |
| DOC-09 | Guia de integração para módulos consumidores | 🟠 ALTA | Como outros módulos usam a IA |
| DOC-10 | Roadmap de funcionalidades vs estado atual | 🟡 MÉDIA | O que está real vs o que é futuro |

---

## 3. Classes e fluxos que precisam de explicação

| # | Classe/Fluxo | Motivo |
|---|-------------|--------|
| CL-01 | `SendAssistantMessage` handler | Fluxo principal de chat — complexo |
| CL-02 | `ExecuteAgent` handler | Fluxo principal de agents — crítico |
| CL-03 | `IAiModelAuthorizationService` | Mecanismo de autorização de modelos |
| CL-04 | `EnrichContext` handler | Montagem de contexto para retrieval |
| CL-05 | `AiGovernanceDbContext` + 19 DbSets | DbContext mais complexo do produto |
| CL-06 | Classes de routing (`AIRoutingStrategy`, `AIRoutingDecision`) | Lógica de seleção de modelo |
| CL-07 | External AI query flow | Fluxo de consulta a AI externa |
| CL-08 | Budget/quota enforcement | Mecanismo de controlo de custos |

---

## 4. XML docs necessárias

| Área | Prioridade |
|------|------------|
| Todas as entidades de domínio | P1 — 30+ entidades sem doc |
| Todos os enums | P2 — 23 enums, valores precisam de explicação |
| Handlers de features críticas | P1 — SendAssistantMessage, ExecuteAgent |
| Interfaces de serviço | P1 — IAiModelAuthorizationService, etc. |
| Endpoint modules | P2 — 5 módulos de endpoints |

---

## 5. Notas de onboarding necessárias

| # | Nota | Público |
|---|------|---------|
| ON-01 | Como o módulo se organiza (4 subdomínios) | Developers novos |
| ON-02 | Quais funcionalidades são reais vs parciais | Developers novos |
| ON-03 | Como configurar um provider local para desenvolvimento | Developers novos |
| ON-04 | Como testar o chat localmente | Developers novos |
| ON-05 | Como criar um agent de teste | Developers novos |
| ON-06 | Quais permissões são necessárias para cada ação | Developers + Ops |
| ON-07 | Estado real dos tools (CR-2) — o que funciona e o que não funciona | Todos |

---

## 6. Documentação mínima do módulo

### P0 — Obrigatório imediato
1. [ ] Atualizar `agents-README.md` e `ai-core-README.md` para refletir estado real
2. [ ] Criar DOC-01 (arquitetura interna)
3. [ ] Criar DOC-10 (roadmap vs estado atual)

### P1 — Obrigatório antes da próxima release
4. [ ] Criar DOC-02 (guia de model/provider)
5. [ ] Criar DOC-03 (guia de agent)
6. [ ] Criar DOC-04 (fluxo E2E chat)
7. [ ] Criar DOC-05 (fluxo E2E agent)
8. [ ] Criar DOC-08 (guia de segurança)
9. [ ] Adicionar XML docs nas 30+ entidades

### P2 — Importante
10. [ ] Criar DOC-06 (modelo de dados ER)
11. [ ] Criar DOC-09 (guia de integração)
12. [ ] Atualizar `backend/endpoints.md` para 54+ endpoints
13. [ ] Atualizar `quality/bugs-and-gaps.md` com N13 findings
14. [ ] Atualizar `quality/technical-debt.md` com N13 findings

---

## 7. Resumo

| Dimensão | Estado |
|----------|--------|
| Documentação existente | ⚠️ 24 ficheiros — maioria desatualizada ou otimista |
| Documentação ausente | 🔴 10 documentos críticos em falta |
| XML docs | 🔴 Ausentes em quase todas as classes |
| Onboarding | 🔴 7 notas de onboarding necessárias |
| **Maturidade documental** | **🔴 ~35%** |
