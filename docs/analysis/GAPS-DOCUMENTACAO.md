# Gaps — Documentação
> Análise dos gaps na documentação técnica e operacional do NexTraceOne.

---

## 1. ARCHITECTURE-OVERVIEW.md — Inaceitável

**Ficheiro:** `docs/ARCHITECTURE-OVERVIEW.md`
**Tamanho actual:** 18 linhas

**Conteúdo actual:**
```
# ARCHITECTURE-OVERVIEW.md

## Architecture Style
Modular Monolith.

## Bounded Contexts
IdentityAccess Catalog ChangeGovernance OperationalIntelligence
AIKnowledge AuditCompliance

## Layers
Domain Application Infrastructure API

## Principles
DDD SOLID Clean Architecture CQRS Result pattern Strongly typed IDs
```

**O que está a faltar:**
- Diagrama de bounded contexts e suas fronteiras
- Como os módulos comunicam entre si (eventos, service interfaces)
- Decisão sobre 27 DbContexts (porquê, trade-offs)
- Fluxo de um request do browser até ao handler
- Explicação do outbox pattern e event bus
- Tenant isolation strategy em detalhe
- Regras de cross-module access
- Como adicionar um novo módulo (estrutura de pastas)

**Impacto:** Um desenvolvedor novo não consegue entender o sistema a partir deste documento.

---

## 2. BACKEND-MODULE-GUIDELINES.md — Inaceitável

**Ficheiro:** `docs/BACKEND-MODULE-GUIDELINES.md`
**Tamanho actual:** 18 linhas

**O que está a faltar:**
- Estrutura de pastas obrigatória de um módulo
- Como criar um Command/Query handler (exemplo completo)
- Como criar uma entidade de domínio com strongly typed ID
- Como fazer migration num DbContext específico
- Como registar o módulo no DI container
- Como adicionar endpoint no módulo
- Como fazer teste unitário de um handler
- Como fazer teste de integração com Testcontainers
- Regras de nomenclatura

**Impacto:** Cada desenvolvedor estrutura o código à sua maneira, quebrando consistência.

---

## 3. FRONTEND-ARCHITECTURE.md — Insuficiente

**Ficheiro:** `docs/FRONTEND-ARCHITECTURE.md`
**Tamanho actual:** 80 linhas

**O que está a faltar:**
- Como criar um novo feature module (estrutura de pastas)
- Como usar TanStack Query (padrão com queryKeys factory)
- Como adicionar nova rota
- Como adicionar nova chave i18n
- Como criar componente reutilizável
- Padrão de error handling (ErrorState vs EmptyState vs PageErrorState)
- Padrão de loading states (Skeleton vs Loader)
- Como usar TanStack Query com mutações
- Como aceder ao contexto de ambiente/persona

---

## 4. TESTING-STRATEGY.md — Não Existe

**Ficheiro esperado:** `docs/TESTING-STRATEGY.md`
**Estado:** **Não existe**

**O que devia conter:**
- Pirâmide de testes adoptada pelo projecto
- Quando escrever unit test vs integration test vs E2E
- Como mockar dependências no backend (NSubstitute)
- Como usar Testcontainers para testes de integração
- Como usar Bogus para geração de dados
- Como usar Respawn para limpeza de BD
- Como escrever teste Playwright (padrão de page objects)
- Metas de cobertura por tipo de módulo

---

## 5. Módulos sem README

**Módulos identificados sem README:**
| Módulo | Caminho |
|--------|---------|
| Integrations | `src/modules/integrations/` |
| Knowledge | `src/modules/knowledge/` (README mínimo) |
| ProductAnalytics | `src/modules/productanalytics/` |

**Impacto:** Desenvolvedor não sabe o propósito, fronteiras e features do módulo.

---

## 6. Documentação de APIs — Cobertura Desconhecida

**Estado:** Endpoints têm atributos OpenAPI (summaries, descriptions) via Minimal APIs.

**Gap:** Não há processo documentado para:
- Gerar documentação de API publicável (ex: Redoc, Scalar)
- Versionar documentação de API junto com código
- Validar que documentação OpenAPI está sincronizada com implementação

---

## 7. Decisões de Design não Documentadas (ADR Faltantes)

**ADRs existentes:** 6 (modular monolith, single-db-per-tenant, elasticsearch, local-ai, react-stack, graphql-roadmap)

**Decisões que deveriam ter ADR:**
- Porquê 27 DbContexts (vs. 1 por módulo ou 1 global)
- Porquê React Contexts em vez de Zustand
- Porquê sessionStorage em vez de localStorage para tokens
- Estratégia de multi-tenancy a longo prazo (schemas separados, databases separadas)
- Porquê HotChocolate GraphQL apenas no Catalog

---

## 8. Documentação de Onboarding — Ausente

**Estado:** Não há `docs/ONBOARDING.md` ou `docs/GETTING-STARTED-DEV.md`.

**LOCAL-SETUP.md existe** mas foca em configuração técnica, não em fluxo de trabalho diário.

**O que falta:**
- Como fazer a primeira feature de ponta a ponta
- Como executar só os testes de um módulo específico
- Como debugar um handler com breakpoint
- Como ver logs em desenvolvimento
- Como interagir com a UI em desenvolvimento

---

## 9. Resumo de Prioridades Documentação

| Gap | Severidade | Esforço |
|-----|-----------|---------|
| ARCHITECTURE-OVERVIEW.md (reescrever) | P0 — Crítico | Médio |
| BACKEND-MODULE-GUIDELINES.md (reescrever) | P0 — Crítico | Médio |
| TESTING-STRATEGY.md (criar) | P1 — Alto | Médio |
| FRONTEND-ARCHITECTURE.md (expandir) | P1 — Alto | Médio |
| README dos 3 módulos sem documentação | P1 — Alto | Baixo |
| ADRs em falta (5-6 decisões) | P2 — Médio | Baixo |
| Documentação de onboarding | P2 — Médio | Médio |
| Processo de documentação de API | P2 — Médio | Médio |
