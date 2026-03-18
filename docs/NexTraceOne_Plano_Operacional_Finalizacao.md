# NexTraceOne — Plano Operacional de Finalização

## Objetivo geral

Concluir, estabilizar e homologar o núcleo funcional do produto, isolando o que ainda está mockado ou incompleto, para depois avançar com a evolução do produto.

---

## Visão macro das fases

- Fase 0 — Governança da execução
- Fase 1 — Saneamento técnico e estabilização da base
- Fase 2 — Fechamento do core de autenticação e navegação
- Fase 3 — Fechamento do núcleo de catálogo, contratos e mudanças
- Fase 4 — Fechamento de operações, auditoria e source of truth
- Fase 5 — Recorte dos módulos não homologáveis
- Fase 6 — Consolidação visual, UX e estados compartilhados
- Fase 7 — Preparação formal para teste de aceitação
- Fase 8 — Execução do teste de aceitação
- Fase 9 — Correções pós-aceite e baseline estável
- Fase 10 — Evolução do produto

---

## Fase 0 — Governança da execução

### Objetivo
Criar controlo de execução, prioridade e decisão de escopo.

### Entregáveis
- backlog de estabilização
- backlog de homologação
- backlog pós-homologação
- definição oficial de escopo homologável
- definição oficial de escopo excluído do aceite
- quadro de acompanhamento por módulo

### Atividades
- classificar todos os módulos em:
  - homologável agora
  - parcialmente homologável
  - fora do aceite
- nomear owner técnico por módulo
- definir ambiente de referência
- definir branch/estratégia de integração
- definir regra de bloqueio para regressão

### Critério de saída
- escopo aprovado
- responsáveis definidos
- ordem de execução acordada

---

## Fase 1 — Saneamento técnico e estabilização da base

### Objetivo
Eliminar os bloqueadores que impedem qualquer validação séria do produto.

### Prioridade
Máxima.

### Atividades principais

#### 1. Corrigir erros críticos de runtime
Fechar prioritariamente:
- `/identity/auth/me`
- `/catalog/graph`
- `/contracts/summary`
- `/changes/summary`
- `/incidents/summary`
- `/identity/users`
- `/identity/break-glass`

#### 2. Validar infraestrutura mínima
- migrations
- connection strings
- bootstrap do ApiHost
- DbContexts
- workers mínimos
- configuração de autenticação

#### 3. Revalidar seed data
- identity
- catalog
- contracts
- changegovernance
- incidents
- audit
- AI, quando aplicável

#### 4. Garantir ambiente executável
- frontend sobe sem crash
- backend sobe sem erro estrutural
- login responde
- APIs core respondem

### Entregáveis
- endpoints críticos corrigidos
- seed data compatível
- ambiente mínimo estável
- checklist de sanity concluída

### Critérios de aceite da fase
- sem 500/422 críticos nos fluxos core
- login possível
- dashboard deixa de quebrar por falha estrutural
- APIs core retornam dados mínimos

---

## Fase 2 — Fechamento do core de autenticação e navegação

### Objetivo
Deixar a entrada do sistema e a navegação global confiáveis.

### Módulos-alvo
- Identity & Access
- Shell / Navigation
- Login
- Tenant selection
- Session flow

### Atividades principais

#### 1. Auth flow end-to-end
- login válido
- login inválido
- seleção de tenant
- refresh token
- logout
- sessão expirada
- protected route
- unauthorized route

#### 2. Páginas administrativas essenciais de identity
- users
- break-glass
- JIT access
- delegations
- sessions
- access reviews, no que estiver realmente pronto

#### 3. Shell da aplicação
- sidebar
- topbar
- command palette
- breadcrumbs
- page transitions
- menu por permissão/persona

### Entregáveis
- auth flow estável
- navegação principal estável
- páginas essenciais de identity operacionais

### Critérios de aceite da fase
- utilizador entra e sai do sistema sem erro
- navega entre módulos sem crash
- rotas protegidas comportam-se corretamente
- identity admin básico está operacional

---

## Fase 3 — Fechamento do núcleo de catálogo, contratos e mudanças

### Objetivo
Concluir o núcleo central do valor de produto.

### Subfase 3.1 — Service Catalog e Source of Truth

#### Atividades
- estabilizar listagem de serviços
- estabilizar detalhe de serviço
- estabilizar grafo
- estabilizar search/global search
- validar rotas e filtros
- alinhar dados entre catálogo e source of truth

#### Entregáveis
- catálogo funcional
- detalhe funcional
- source of truth navegável

### Subfase 3.2 — Contracts / Service Studio

#### Objetivo
Fechar o escopo homologável do módulo mais estratégico.

#### Escopo homologável desta fase
- contract catalog
- create service / create draft
- draft studio
- contract workspace básico
- navegação entre secções
- save mínimo
- submit for review
- portal básico
- versioning/changelog básico visível
- approvals/compliance no que estiver de facto operacional

#### Atividades
- estabilizar catalog page
- estabilizar wizard de criação
- estabilizar navegação para draft studio
- estabilizar save/edit no draft studio
- estabilizar submit for review
- estabilizar contract workspace
- remover crashes de secções
- identificar e reduzir dependência de `studioMock.ts`
- explicitar na UI o que ainda é enrich/mock
- validar portal básico
- validar visual builders apenas no fluxo mínimo homologável
- validar source editor no nível atual suportado

#### Itens fora do aceite inicial
- sync bidirecional completo builder/source
- spectral avançado realtime, se não estiver íntegro
- canonical full governance, se ainda parcial
- partes avançadas de portal
- tudo que dependa fortemente de enriquecimento mock

#### Entregáveis
- fluxo create → edit → save → submit funcional
- workspace navegável
- catálogo operacional
- portal básico acessível
- backlog explícito do que ficou fora

### Subfase 3.3 — Change Governance

#### Atividades
- validar change catalog
- validar detalhe
- validar releases
- validar workflow
- validar promotion
- validar mutações
- corrigir inconsistências de status, filtros e loading

#### Entregáveis
- módulo operacional com dados reais

---

## Fase 4 — Fechamento de operações, auditoria e source of truth

### Objetivo
Fechar as áreas complementares que entram no aceite inicial.

### Módulos-alvo
- Operations — Incidents
- Audit
- ajustes finais em Source of Truth

### Atividades principais

#### Incidents
- listagem
- filtros
- detalhe
- timeline
- stats
- navegação
- loading/error/empty states

#### Audit
- listagem
- paginação/filtros, se existirem
- carga de dados
- estados

#### Source of Truth
- validar consistência final com catalog/contracts
- garantir que a navegação entre entidades não quebra

### Entregáveis
- incidents pronto
- audit pronto
- source of truth consolidado

---

## Fase 5 — Recorte dos módulos não homologáveis

### Objetivo
Proteger o aceite contra escopo falso-positivo.

### Módulos a recortar
- Governance Enterprise
- Integrations
- Product Analytics
- Reliability
- Automation
- AI admin mockado
- partes avançadas de portal/contracts não prontas

### Estratégias possíveis
- esconder no menu
- marcar como preview
- marcar como roadmap
- restringir por feature flag
- manter fora do plano de testes

### Entregáveis
- lista oficial de módulos fora do aceite
- estratégia de exposição por módulo
- UI sem aparência enganosa de “pronto”

---

## Fase 6 — Consolidação visual, UX e estados compartilhados

### Objetivo
Dar consistência final antes da homologação.

### Atividades principais
- padronizar loading states
- padronizar empty states
- padronizar error states
- revisar mensagens de erro
- revisar headers de página
- revisar quick actions
- revisar formulários inconsistentes
- remover campos técnicos indevidos ao utilizador
- revisar feedbacks de save/submit
- revisar responsividade mínima desktop/tablet

### Entregáveis
- biblioteca de states coerente
- UX mais uniforme
- menos ruído visual e funcional

---

## Fase 7 — Preparação formal para teste de aceitação

### Objetivo
Entrar em homologação com controlo.

### Entregáveis obrigatórios
1. Lista oficial do escopo homologável
2. Ambiente estável
3. Critérios de entrada
4. Plano de teste funcional

### Critérios de entrada
- sem bloqueadores P0 abertos
- sem endpoints core quebrados
- navegação principal íntegra
- massa mínima carregada

### Critérios de saída
- ambiente pronto
- plano pronto
- escopo congelado para aceite

---

## Fase 8 — Execução do teste de aceitação

### Objetivo
Validar com confiança o escopo homologável.

### Ordem recomendada
1. Login / tenant / auth
2. Shell / navegação
3. Dashboard
4. Services / source of truth
5. Contracts / draft studio / workspace
6. Change governance
7. Incidents
8. Audit

### Saídas esperadas
- bugs classificados
- gaps reais de produto
- ajustes de UX
- backlog pós-aceite
- decisão de pronto ou não por módulo

---

## Fase 9 — Correções pós-aceite e baseline estável

### Objetivo
Fechar os problemas encontrados no aceite e consolidar uma versão estável.

### Atividades
- corrigir bugs P0
- corrigir bugs P1 que impedem operação
- revalidar regressão mínima
- atualizar documentação de escopo
- congelar baseline funcional

### Entregáveis
- versão estável candidata
- relatório de aceite consolidado
- backlog pós-baseline

---

## Fase 10 — Evolução do produto

### Objetivo
Retomar crescimento do produto sobre base estabilizada.

### Trilha 1 — Contracts avançado
- remover mock enrichment restante
- fortalecer source editor
- builder/source round-trip
- spectral realtime real
- canonical entities completas

### Trilha 2 — Governance real
- persistência
- API real
- integração do frontend mockado
- evidence, controls, packs, waivers, finops, risk

### Trilha 3 — Integrations real
- ingestion API real
- connectors
- executions
- freshness

### Trilha 4 — Product Analytics real
- eventos
- dashboards
- journeys
- value tracking

### Trilha 5 — AI Hub real
- models
- policies
- routing
- integrations IDE
- backend real de operações AI

---

## Checklist operacional por fase

### Fase 1
- [ ] corrigir 500/422 críticos
- [ ] validar migrations
- [ ] validar seeds
- [ ] validar ambiente
- [ ] validar dashboard mínimo

### Fase 2
- [ ] login funcional
- [ ] tenant selection funcional
- [ ] logout funcional
- [ ] routes protegidas funcionais
- [ ] shell íntegro
- [ ] admin pages essenciais de identity estáveis

### Fase 3
- [ ] service catalog funcional
- [ ] source of truth funcional
- [ ] contract catalog funcional
- [ ] create draft funcional
- [ ] draft studio funcional
- [ ] workspace estável
- [ ] change governance funcional

### Fase 4
- [ ] incidents funcional
- [ ] audit funcional
- [ ] source of truth consolidado

### Fase 5
- [ ] módulos mockados fora do aceite
- [ ] preview/feature flag definidos
- [ ] menu/rotas ajustados

### Fase 6
- [ ] loading states padronizados
- [ ] empty states padronizados
- [ ] error states padronizados
- [ ] formulários revisados
- [ ] UX core consistente

### Fase 7
- [ ] escopo homologável fechado
- [ ] ambiente homologável pronto
- [ ] massa de teste pronta
- [ ] plano funcional fechado

### Fase 8
- [ ] smoke tests executados
- [ ] P0/P1 executados
- [ ] evidências recolhidas
- [ ] decisão por módulo registada

### Fase 9
- [ ] bugs críticos corrigidos
- [ ] regressão executada
- [ ] baseline funcional congelada

### Fase 10
- [ ] roadmap pós-baseline priorizado
- [ ] trilhas de evolução aprovadas

---

## Critério final de sucesso

O plano será bem executado quando:
- o projeto tiver um núcleo claramente homologável
- os módulos mockados estiverem isolados ou explicitamente fora do aceite
- os fluxos core funcionarem de ponta a ponta
- o teste de aceitação puder ser executado sem ambiguidade de escopo
- a equipa tenha uma baseline estável
- a evolução futura aconteça sobre uma base sólida
