# ADR-009: AI Evaluation Harness Interno

## Status

Proposed

## Data

2026-04-20

## Contexto

O NexTraceOne adotou como decisão estratégica (ADR-004) **IA local/interna como default**, com IA externa opcional e governada. O módulo `AIKnowledge` já implementa:

- Model Registry (`IAiModelCatalogService`).
- Políticas de acesso e budgets por tenant/persona/ambiente.
- Guardrails (prompt injection, credential leak, PII warn).
- Agentes especializados (Contract Review, Skills System, Agent Lightning).
- Audit trail completo de prompts, contexto, resposta e custo.

### Problema observado

À medida que o produto passa a oferecer **múltiplos modelos** (locais + externos) e **múltiplos casos de uso** (contract generation, incident summary, change impact analysis, runbook generation, diff explanation), surge um problema concreto:

1. **Não é possível trocar um modelo com confiança** — o feedback é implícito e subjetivo, não métrico.
2. **Não existe regression testing de prompts/modelos** — uma mudança no prompt pode degradar silenciosamente a qualidade do output.
3. **Auditoria qualitativa em falta** — um auditor não tem como provar que o modelo escolhido para X caso de uso tem qualidade superior a Y em métricas objetivas.
4. **Custo vs. qualidade sem trade-off explícito** — não há forma de decidir "modelo A é 3× mais barato que B e degrada apenas 8% em accuracy — vale a pena".
5. **Agentes especializados sem sanity check** — `Skills` e `AgentLightning` capturam trajetórias e feedback, mas falta um harness estruturado que traduza trajetórias em métricas reproduzíveis.

## Decisão

Introduzir **AI Evaluation Harness** como capacidade interna de **`AIKnowledge`** — sem novo módulo, reutilizando infraestrutura existente (Skills, AgentLightning, Audit) e adicionando domínio + CRUD + runner orquestrado.

### Objetivo

Permitir a Platform Admins e Tech Leads **definir, versionar, executar e comparar** conjuntos de avaliação (eval sets) para cada caso de uso de IA suportado pelo NexTraceOne, com métricas objetivas e auditáveis.

### Componentes

1. **`EvaluationSuite`** (agregado) — coleção nomeada e versionada de casos de teste para um dado caso de uso. Exemplos: `contract-review-suite-v1`, `incident-summary-suite-v1`, `change-impact-suite-v1`.
2. **`EvaluationCase`** — entrada concreta: prompt, contexto (grounding data), input esperado, critérios de avaliação.
3. **`EvaluationRun`** — execução de uma suite contra um modelo + versão de prompt + política de grounding + cenário de guardrails; persiste outputs completos e métricas.
4. **`EvaluationMetric`** — 0..N por caso, por run. Tipos iniciais suportados:
   - `ExactMatch` (string/JSON match)
   - `JsonSchemaValidity`
   - `ContainsKeyword` / `AbsenceOfKeyword`
   - `Latency` (ms, p50/p95/p99 por run)
   - `TokenCost`
   - `GuardrailTriggers` (contagem e severidade)
   - `LlmAsJudgeScore` — rubric evaluation via outro modelo, com rubric persistido e auditado
   - `HumanReviewScore` — quando ativado, fica em estado `AwaitingHumanReview` e agrega manualmente
5. **`EvaluationDataset`** — fonte de casos partilhada entre suites (reutilizável). Pode ser:
   - curated (casos estáticos validados),
   - generated (capturados via `AgentLightning` a partir de trajetórias reais marcadas como canonical),
   - synthetic (gerados por outro modelo, explicitamente identificado).
6. **`EvaluationPolicy`** — política por tenant definindo:
   - Limiares mínimos aceitáveis por caso de uso (ex.: `contract-review` requer >=0.75 em LlmAsJudgeScore).
   - Modelos autorizados a serem promovidos para *default* num caso de uso sem passar por eval run recente (default: nenhum).
   - Janela máxima sem re-eval (default: 90 dias).

### Governance

- **Quem define suites**: papel `ai:evaluation:author` (Tech Lead, Architect, Platform Admin).
- **Quem aprova promoção de modelo**: papel `ai:evaluation:approve` (Platform Admin + auditor on request).
- **Audit trail**: todas as runs, promoções e alterações de policy ficam em `AuditCompliance` (hash chain existente).
- **Execução**: agendada via Quartz (`EvaluationRunnerJob`) — pode ser on-demand, periódica, ou disparada por `ModelRegistry` publicar nova versão.
- **Custo**: cada run consome budget de tokens como qualquer outra chamada de IA — agrega em `ExternalAi` + `CostIntelligence` cruzado por `Model × UseCase`.

### Integração cross-module (sem quebrar bounded contexts)

- `ModelRegistry` consulta `IEvaluationReader` antes de marcar um modelo como `Default` para um caso de uso; se não houver run recente passando os thresholds definidos em `EvaluationPolicy`, bloqueia a promoção.
- `CostIntelligence` recebe eventos de custo por run via outbox existente.
- `AgentLightning` pode marcar trajetórias reais como *candidate* para `EvaluationDataset` (Human-in-the-Loop).
- `AiOrchestration` expõe `/api/v1/aiorchestration/evaluation/suites` e `/runs` para consumo do frontend.

### Fora de escopo (explícito)

- **Fine-tuning** de modelos — o harness **mede**, não treina. Fine-tuning/LoRA fica para ADR futuro.
- **Leaderboard cross-tenant** — opt-in apenas, fora do escopo v1 deste ADR. Encaixa em Wave D.2 do roadmap.
- **Benchmark público de modelos externos** — só benchmarks internos sobre casos de uso reais do NexTraceOne.

### Persistência

- Novo `AiEvaluationDbContext` dentro de `AIKnowledge.Infrastructure` (segue padrão de sub-contextos já usado em `Governance`/`Orchestration`/`ExternalAi`).
- Tabelas: `aik_evaluation_suites`, `aik_evaluation_cases`, `aik_evaluation_runs`, `aik_evaluation_metrics`, `aik_evaluation_datasets`, `aik_evaluation_policies`.
- RLS por tenant obrigatório.
- Retenção de outputs brutos controlável por tenant (default: 180 dias; agregados mantidos indefinidamente).

### Configuração parametrizável via `ConfigurationDefinitionSeeder`

- `ai.evaluation.defaults.latencyBudgetMs`
- `ai.evaluation.defaults.llmJudge.model`
- `ai.evaluation.defaults.llmJudge.rubric`
- `ai.evaluation.runs.retentionDays`
- `ai.evaluation.runs.maxConcurrency`
- `ai.evaluation.humanReview.slaHours`

## Consequências

### Positivas

- Reforça diretamente o pilar **AI Governance** (capítulo 7.7 das Copilot Instructions).
- Destranca mercado regulado — auditores passam a ter evidência objetiva de qualidade por modelo × caso de uso.
- Habilita trade-off custo × qualidade explícito (modelos internos vs. externos).
- Reduz risco de regressão silenciosa ao trocar modelos.
- Fecha o loop com `AgentLightning` — trajetórias reais viram casos de teste.
- Reutiliza audit, budget, guardrails, RLS, configuração — nenhum módulo novo.

### Negativas

- Custo de execução pode ser relevante (LlmAsJudge consome tokens adicionais) — mitigar com amostragem configurável e runs periódicos, não em cada inferência.
- Curva de aprendizagem para autores de suites — documentar padrões no `docs/user-guide/`.
- UI nova para definição de suites e visualização de runs — esforço frontend não trivial.

### Neutras

- Não altera contratos existentes de `AiGovernance` / `AiOrchestration` — adiciona endpoints novos sob `/evaluation/*`.
- Modelos locais e externos são avaliados com a mesma rubrica, preservando a neutralidade do registry.

## Roadmap de implementação

| Fase | Escopo | Depende de |
|------|--------|-----------|
| Fase 1 | Domínio + DbContext + Create/List/Get de Suites/Cases/Datasets | — |
| Fase 2 | `EvaluationRunnerJob` (Quartz) + métricas determinísticas (ExactMatch, Latency, TokenCost, Guardrails) | Fase 1 |
| Fase 3 | `LlmAsJudge` metric + rubric registry + auditoria | Fase 2 |
| Fase 4 | `EvaluationPolicy` + bloqueio de promoção no `ModelRegistry` | Fase 2 |
| Fase 5 | `HumanReviewScore` + integração com `AgentLightning` para dataset promotion | Fase 2 |
| Fase 6 | Frontend (Suite editor, Run viewer, Model comparison) + i18n | Fase 2 |

## Critérios de aceite

- [ ] `AiEvaluationDbContext` registado, RLS aplicado.
- [ ] Runs persistem outputs completos, métricas agregadas e citações para o artefacto do modelo usado.
- [ ] Cost attribution integrada com `CostIntelligence` via outbox.
- [ ] Policy bloqueia promoção de modelo sem eval run válido dentro da janela.
- [ ] Configuração 100% via `IConfigurationResolutionService` — **nunca** `appsettings`.
- [ ] `CancellationToken` em todas as operações assíncronas; `Result<T>` para falhas.
- [ ] i18n em pt-PT, pt-BR, en, es.
- [ ] Auditoria via `AuditCompliance` de todas as mutações e runs.
- [ ] Testes unitários de agregação de métricas, redistribuição, policy enforcement.
- [ ] Documentação no `docs/user-guide/` com pattern recomendado para autoria de suites.

## Referências

- [ADR-004: Local AI First](./004-local-ai-first.md)
- [AI-GOVERNANCE.md](../AI-GOVERNANCE.md)
- [AI-AGENT-LIGHTNING.md](../AI-AGENT-LIGHTNING.md)
- [AI-SKILLS-SYSTEM.md](../AI-SKILLS-SYSTEM.md)
- [FUTURE-ROADMAP — Wave A.4](../FUTURE-ROADMAP.md)
