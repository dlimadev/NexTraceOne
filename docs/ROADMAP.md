# ROADMAP.md — Plano de execução e evolução pós-PR-16

> **Última atualização:** Março 2026 (pós-finalização Onda 1)
>
> **Princípio diretor:** Parar a expansão conceitual, consolidar o que existe,
> fechar os fluxos centrais de valor, e só depois retomar evolução avançada de forma seletiva.
>
> Documento complementar: [REBASELINE.md](./REBASELINE.md) — inventário real do estado do produto.

---

## Direção estratégica

O foco deve voltar para os pilares centrais do NexTraceOne:

1. **Change Confidence** — ajudar a decidir se uma mudança deve seguir
2. **Source of Truth de serviços e contratos** — ser a fonte oficial
3. **Mitigação e troubleshooting operacional com IA** — reduzir tempo de diagnóstico
4. **Consistência operacional por equipa/domínio** — garantir padrões
5. **Governança real de contratos, serviços e mudanças** — enforcement, não só visualização
6. **IA útil, grounded e auditável** — respostas com contexto, fonte e explicação
7. **Produto utilizável no dia a dia** — não só arquitetura bonita

---

## Onda 0 — Rebaseline e consolidação (1-2 semanas)

**Objetivo:** Visão honesta do estado do produto. Backlog limpo. Prioridades reais.

### Entregáveis

- [x] Inventário real do que foi implementado até o PR-16 → ver [REBASELINE.md](./REBASELINE.md)
- [x] Mapa por módulo: pronto / parcial / inconsistente / técnico sem valor
- [x] Lista de dívidas de arquitetura (7 identificadas)
- [x] Lista de dívidas de frontend/UX (6 identificadas)
- [x] Lista de endpoints sem uso real (14+ endpoints mock)
- [x] Lista de telas sem fluxo real (15+ páginas)
- [x] Lista de modelos conceituais sem valor entregue (8 conceitos)
- [x] Revisão dos docs para refletirem estado real

### Fluxos revisados

- [x] Contrato e Source of Truth — 75% fechado
- [x] Change/advisory/approval — 95% fechado
- [x] Incident/correlation/mitigation — 0% funcional (todo mock)
- [x] AI Assistant com grounding — 30% funcional
- [x] Governance Packs (PR-16) — modelo definido, dados mock
- [x] Consistência backend ↔ frontend ↔ i18n — 89% conectado

---

## Onda 1 — Fechar os fluxos centrais do produto (4-6 semanas)

> **Estado:** Finalização transversal concluída (i18n, UX states, navegação, testes).
> Backend dos fluxos 1-2 estão completos com persistência real.
> Backend dos fluxos 3-4 estão com domain/application/API prontos, handlers ainda com dados mock.
> Frontend está conectado via API client em todos os 4 fluxos.

### Fluxo 1 — Contrato e Source of Truth

**Objetivo:** NexTraceOne é de fato a fonte de verdade de APIs/serviços/eventos.

| Item | Sprint | Estado atual |
|------|--------|-------------|
| Cadastro/importação REST, SOAP, Kafka, background | Sprint 1 | ✅ Real |
| Versionamento | Sprint 1 | ✅ Real |
| Diff semântico | Sprint 1 | ✅ Real |
| Compatibilidade | Sprint 1 | ✅ Real |
| Ownership | Sprint 1 | ✅ Real |
| Busca e navegação confiável | Sprint 1 | ✅ Explorer funcional via API |
| Documentação operacional mínima | Sprint 2 | ⚠️ Parcial |
| Visualização por serviço/contrato/tópico | Sprint 2 | ✅ Real |
| Contract Studio utilizável | Sprint 2 | ⚠️ Precisa polish |
| i18n — textos hardcoded removidos | Sprint W1 | ✅ Concluído |
| Error states — SourceOfTruthExplorer | Sprint W1 | ✅ Concluído |
| Navegação serviço ↔ contrato bidirecional | Sprint W1 | ✅ Concluído |
| Testes frontend (5 testes SoT Explorer) | Sprint W1 | ✅ Concluído |

**Critério de sucesso:**
- Um dev importa/edita/publica contrato sem sair do NexTraceOne
- Um tech lead revisa impacto de mudança de contrato
- Uma equipa consulta contratos e dependências sem ir para outra ferramenta

### Fluxo 2 — Change Confidence

**Objetivo:** NexTraceOne ajuda a decidir se uma change deve seguir.

| Item | Sprint | Estado atual |
|------|--------|-------------|
| Submissão de mudança | Sprint 3 | ✅ Real |
| Vínculo com serviços/contratos impactados | Sprint 3 | ✅ Real |
| Evidence pack mínimo | Sprint 3 | ✅ Real |
| Blast radius | Sprint 3 | ✅ Real |
| Advisory com rationale clara | Sprint 3 | ✅ Real |
| Approval/reject/conditional | Sprint 3 | ✅ Real |
| Readiness de rollout | Sprint 3 | ✅ Real |
| Trilha de decisão | Sprint 3 | ✅ Real |
| Error state — ChangeCatalogPage | Sprint W1 | ✅ Concluído |
| Navegação change → serviço específico | Sprint W1 | ✅ Corrigido (/services em vez de /catalog/services) |
| Navegação change → incidentes | Sprint W1 | ✅ Quick link adicionado |
| Testes frontend (16 testes existentes) | Sprint 3 | ✅ Concluído |

**Critério de sucesso:**
- Uma mudança sai de proposta até decisão com contexto completo
- O aprovador entende o risco sem abrir 10 lugares diferentes

### Fluxo 3 — Incident Correlation & Mitigation

**Objetivo:** Reduzir tempo de diagnóstico e resposta operacional.

| Item | Sprint | Estado atual |
|------|--------|-------------|
| Correlação incidente ↔ changes ↔ serviços | Sprint 4 | ⚠️ Domain pronto, handlers mock |
| Painel de troubleshooting | Sprint 4 | ⚠️ UI funcional via API, dados mock |
| Mitigação guiada | Sprint 4 | ⚠️ UI funcional via API, dados mock |
| Runbooks | Sprint 4 | ⚠️ UI funcional via API, dados mock |
| Validação pós-ação | Sprint 4 | ⚠️ UI funcional via API, dados mock |
| Histórico de mitigação | Sprint 4 | ⚠️ UI funcional via API, dados mock |
| Frontend conectado à API | Sprint 4 | ✅ Conectado (API retorna mock) |
| Navegação incident → serviço e contrato | Sprint W1 | ✅ Links diretos |
| Testes frontend (13 testes novos) | Sprint W1 | ✅ Concluído |

**Critério de sucesso:**
- Num incidente, a equipa chega mais rápido em hipótese, impacto e ação
- Dados persistidos e consultáveis, não hardcoded

> **Gap remanescente:** Handlers de incidentes ainda retornam dados mock.
> Persistência real requer EF migrations e stores — planeado para Onda 2.

### Fluxo 4 — AI Assistant útil de verdade

**Objetivo:** IA ajuda no trabalho real, não só conversa.

| Item | Sprint | Estado atual |
|------|--------|-------------|
| Grounding em contratos | Sprint 5 | ⚠️ API existe, grounding estrutural |
| Grounding em serviços | Sprint 5 | ⚠️ API existe, grounding estrutural |
| Grounding em incidents | Sprint 5 | ⚠️ Estrutura pronta, depende de dados reais |
| Grounding em runbooks | Sprint 5 | ⚠️ Estrutura pronta, depende de dados reais |
| Grounding em changes | Sprint 5 | ⚠️ Changes reais, grounding estrutural |
| Explicação de fontes | Sprint 5 | ✅ UI mostra fontes usadas |
| Prompts por persona | Sprint 5 | ✅ Estrutura funcional, 4 contextos |
| Troubleshooting assistido | Sprint 5 | ⚠️ UI pronta, depende de dados reais |
| AssistantPanel contextual (4 entidades) | Sprint W1 | ✅ Concluído |
| Testes frontend (5 testes novos) | Sprint W1 | ✅ Concluído |

**Critério de sucesso:**
- O assistant responde perguntas úteis sobre problema real de serviço
- A resposta tem contexto, fonte e explicação
- A IA não parece genérica

> **Gap remanescente:** Grounding profundo depende de integração com modelo real.
> UI e routing estão prontos. Validação E2E depende de modelo configurado.

---

## Onda 2 — Productização e adoção real (3-5 semanas)

> **Nota:** Alguns itens de product polish (empty states, error states, breadcrumbs, navegação
> cross-entity) foram antecipados e resolvidos na finalização da Onda 1.

### UX por persona

Fechar experiência distinta para: Engineer, Tech Lead, Architect, Product, Executive, Platform Admin, Auditor.

Cada persona precisa ter: home útil, quick actions, foco correto, linguagem correta, menos ruído.

### Product polish

- Empty states úteis
- Loading e error states
- Breadcrumbs
- Navegação entre entidades (serviço → contrato → change → incident)
- Consistência visual e de títulos/labels/ações

### Product analytics validado

Validar se as métricas de adoção, fricção, jornada, time-to-first-value estão a funcionar.

### Integration Hub pragmático

Garantir que conectores prioritários estão estáveis, freshness/health claros, falhas visíveis.

---

## Onda 3 — Hardening e operação (3-4 semanas)

- Performance de endpoints críticos, busca, diff, listagens
- Revisão de jobs e background processing
- Health/readiness endpoints
- Logs e observabilidade da própria plataforma
- Secrets/config/environment
- Packaging/deployment
- Revisão de permissões e escopos
- Testes E2E dos fluxos centrais

**Critério de sucesso:** plataforma estável, fluxos críticos testados E2E, operação previsível.

---

## Onda 4 — Evolução seletiva (após ondas anteriores)

Só retomar evolução avançada após fechar ondas 0-3. Com filtro rigoroso.

### Evoluções aprovadas (por ordem)

1. **Change Advisory Intelligence pragmático** — melhorar recommendations, evidence readiness, rollout suggestions
2. **Operational Knowledge Memory** — reaproveitar incidentes, mitigação e learnings
3. **Connector SDK** — somente depois dos conectores principais estarem realmente bons
4. **Previsão seletiva** — apenas com dados confiáveis suficientes

### Congelados por agora

Trilhas abstratas congeladas até fechar o núcleo:
- Governance fabrics avançados
- Digital twins de serviço
- Orchestrators meta
- Compliance packs avançados
- Qualquer feature que não entregue valor direto ao utilizador

---

## Plano tático por sprint

| Sprint | Foco | Entregáveis-chave |
|--------|------|-------------------|
| **Sprint 0** | Rebaseline | Inventário, mapa de módulos, backlog consolidado, critérios de aceite |
| **Sprint 1** | Source of Truth core | Contratos, versões, diff, ownership, busca |
| **Sprint 2** | Contract Studio + navegação | Edição, publicação, visualização por serviço, dependências |
| **Sprint 3** | Change workflow core | Submit, evidence, blast radius, advisory, approval flow |
| **Sprint 4** | Incident & mitigation core | Correlation, incident detail, mitigation guiada, validação pós-ação |
| **Sprint 5** | AI grounded core | Assistant em contract/change/incident/runbook, resposta explicável |
| **Sprint 6** | UX e productização | Home por persona, quick actions, empty states, consistência visual |
| **Sprint 7** | Integration + reliability polish | Health/freshness, reliability por equipa, conectores prioritários |
| **Sprint 8** | Hardening | Performance, health/readiness, jobs, deployment, E2E |

---

## Critério de prioridade

### Prioridade máxima

- Melhora change confidence
- Melhora source of truth
- Melhora troubleshooting/mitigation
- Melhora governança real de contratos/serviços
- Melhora assistant grounded
- Melhora usabilidade diária do produto

### Prioridade média

- Melhora escala
- Melhora adoção
- Melhora integração
- Melhora relatórios e analytics

### Prioridade baixa por agora

- Abstrações meta
- Camadas institucionais sofisticadas
- "Fabrics", "graphs", "twins" e "orchestrators" sem valor direto

---

## Métricas de evolução

### Valor de produto

- Tempo para importar primeiro contrato
- Tempo para gerar primeiro diff útil
- Tempo para analisar uma change
- Tempo para chegar em hipótese de incidente
- Tempo para obter resposta útil do assistant

### Qualidade operacional

- Taxa de mudanças com evidence suficiente
- Taxa de approvals com rationale clara
- Taxa de incidentes correlacionados com changes
- Taxa de uso dos runbooks
- Taxa de uso real do assistant grounded

### Adoção

- Quantos serviços têm owner
- Quantos contratos estão versionados
- Quantos serviços têm documentação operacional
- Quantas equipas usam change workflow
- Quantas equipas usam source of truth como consulta real
