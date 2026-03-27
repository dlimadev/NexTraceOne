# P10.2 — Post-Change Gap Report

> **Status:** COMPLETED  
> **Date:** 2026-03-27  
> **Phase:** P10.2 — Cross-module search com PostgreSQL FTS

---

## O que foi resolvido

1. **Backend real de search cross-module mantido e consolidado**
   - `GlobalSearch` continuou como endpoint unificado para Command Palette.
   - Integração Knowledge ↔ Catalog permaneceu ativa via `IKnowledgeSearchProvider`.

2. **PostgreSQL FTS implementado no fluxo inicial**
   - Repositórios de Search migrados de `LIKE/ILIKE` para FTS com:
     - `PlainToTsQuery`
     - `ToTsVector(...).Matches(...)`
     - `Rank(...)`

3. **Participação mínima de Knowledge Hub garantida**
   - `KnowledgeDocument` e `OperationalNote` entram no resultado global.

4. **Participação mínima de Source of Truth / Catalog / Contracts garantida**
   - `ServiceAsset`, `LinkedReference` e `ContractVersion` entram no resultado global.

5. **Command Palette continua consumindo resultados reais**
   - Sem quebra no contrato frontend (`globalSearchApi`).

6. **Validação técnica executada**
   - Restore, builds e testes dos módulos impactados passaram.

---

## O que ainda ficou pendente

1. **Otimização de FTS por schema**
   - Sem colunas `tsvector` materializadas.
   - Sem índices GIN dedicados.
   - Nesta fase, FTS roda em expressão de query.

2. **Ranking avançado**
   - Ranking ainda simples (`Rank` direto).
   - Sem boosting por módulo/persona/contexto operacional.

3. **Facetas e filtros enterprise**
   - Sem facetas avançadas por ambiente, equipa, domínio, criticidade, tenant etc.

4. **Cobertura de mais módulos**
   - `changes` e `incidents` no `GlobalSearch` continuam como placeholders de faceta (0), sem providers dedicados de pesquisa.

5. **Migrations específicas para FTS**
   - Não houve migração de BD nesta fase porque a implementação foi intencionalmente mínima e sem alteração de schema.

---

## O que fica explicitamente para P10.3

1. **FTS hardening de produção**
   - colunas `tsvector` persistidas
   - índices GIN
   - tuning de dicionário/language config por tenant/idioma

2. **Expansão cross-module**
   - fontes de `changes` e `incidents` no resultado global
   - mais superfícies de Source of Truth conforme prioridade funcional

3. **Search governance avançada**
   - filtros por ambiente/tenant/persona
   - política de escopo por permissão no nível de resultado

4. **Evoluções de relevância**
   - peso por tipo de entidade
   - sinais operacionais (recência, estado, criticidade)

5. **(Fora de escopo desta linha)**
   - vector DB, embeddings, semantic search avançada (continuam fora de P10.2)

---

## Limitações residuais após implementação

1. O backend agora usa PostgreSQL FTS, mas ainda sem otimizações de índice dedicadas.
2. `GlobalSearch` mantém placeholders para domínios que ainda não têm provider de pesquisa.
3. Não houve expansão de UX de pesquisa (intencionalmente mantido mínimo para esta fase).
4. Warnings pré-existentes de build/test permanecem no repositório (sem regressão funcional introduzida).

---

## Conclusão

P10.2 cumpriu o objetivo de mover a pesquisa cross-module para um backend real com **PostgreSQL FTS** no fluxo inicial e manter a Command Palette operando sobre resultados reais de múltiplos módulos (Knowledge + Source of Truth/Catalog/Contracts), com escopo controlado e sem over-engineering.
