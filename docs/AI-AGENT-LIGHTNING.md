# AI-AGENT-LIGHTNING.md

> **Data:** Abril 2026
> **Fase:** 2 do AI Evolution Roadmap (10–20 semanas)
> **Framework base:** Agent Lightning — Microsoft Research (Agosto 2025)
> **Paper:** [arxiv.org/abs/2508.03680](https://arxiv.org/abs/2508.03680)

---

## O Que É o Agent Lightning

Agent Lightning é um framework open-source da Microsoft Research que adiciona **Reinforcement Learning (RL)** a qualquer agente de IA sem requerer reescrita de código. Publicado em Agosto de 2025, suporta qualquer framework de agentes (LangChain, OpenAI Agents SDK, AutoGen, CrewAI, Python puro) e funciona com qualquer modelo LLM.

O princípio central é a **separação entre execução do agente e treino do modelo**:

```
Sem Agent Lightning:                Com Agent Lightning:
Agente executa → resposta → fim     Agente executa → resposta
                                    → Utilizador avalia (feedback)
                                    → RL Trainer processa trajectória
                                    → Modelo melhora automaticamente
                                    → Próxima execução é mais precisa
```

### Algoritmos Suportados

| Algoritmo | Descrição | Melhor Para |
|---|---|---|
| **GRPO** | Group Relative Policy Optimization | Raciocínio e análise complexa |
| **PPO** | Proximal Policy Optimization | Tool use e function calling |
| **APO** | Automatic Prompt Optimization | Agentes com modelos externos (OpenAI/Anthropic) |
| **SFT** | Supervised Fine-tuning | Geração de código e scaffolding |

### Resultados Comprovados

Experimentos publicados no paper original mostram melhorias contínuas em:
- Text-to-SQL: +23% accuracy após 30 dias de RL
- RAG tasks: +18% relevance score
- Math tool-use: +31% acerto em multi-step reasoning

---

## Por Que o NexTraceOne Precisa de Agent Lightning

Hoje, o NexTraceOne regista `ExecutionCount` e guarda logs de uso para cada agente, mas essa informação **não gera nenhuma melhoria** nos agentes. Com Agent Lightning:

| Agente | O Que Aprende | Feedback Source | Impacto Esperado |
|---|---|---|---|
| `incident-responder` | Precisão de root cause analysis | Resolução real do incidente | MTTR -40% |
| `change-advisor` | Qualidade da análise de blast radius | Pós-deployment review | Falsos positivos -50% |
| `service-scaffold-agent` | Padrões arquiteturais preferidos | Code review feedback | Retrabalho -60% |
| `contract-designer` | Boas práticas de API do contexto | Revisões de contrato aprovadas | Iterações de review -45% |
| `security-reviewer` | Vulnerabilidades reais detectadas | Pentest / audit results | Gaps não detectados -70% |
| `test-generator` | Casos de teste que encontram bugs | CI/CD failure correlation | Coverage efectiva +35% |

---

## Arquitectura de Integração

### Visão Geral

```
NexTraceOne Backend (.NET 10)
│
├── AgentExecution (ExecuteAgent command)
│   └── Persiste AiMessage + AiAgentExecution com trajectória completa
│
├── FeedbackEndpoint (novo — POST /api/ai/executions/{id}/feedback)
│   └── Persiste AiSkillFeedback (rating + comentário + outcome)
│
└── TrajectoryExporter (novo — background service)
    └── Exporta trajectórias para Agent Lightning Trainer (Python service)

Agent Lightning Trainer (Python — novo serviço)
│
├── TrajectoryConsumer: consome trajectórias do NexTraceOne
├── RewardComputer: calcula reward baseado em feedback e outcomes
├── RLTrainer: treina modelos locais (Ollama/Qwen) via GRPO/PPO
└── PromptOptimizer: optimiza system prompts para modelos externos

Ollama (local)
└── Modelos actualizados com LoRA fine-tuning incremental

OpenAI / Anthropic (externos)
└── System prompts optimizados automaticamente via APO
```

### Estrutura de Dados — Trajectória de Execução

Uma trajectória captura o ciclo completo de uma execução de agente para RL:

```json
{
  "trajectory_id": "traj_01JXXXXXXX",
  "agent_id": "incident-responder",
  "model_id": "qwen2.5-coder-32b",
  "skill_id": "incident-triage",
  "timestamp_utc": "2026-04-19T10:30:00Z",
  "steps": [
    {
      "step": 1,
      "type": "tool_call",
      "tool": "search_incidents",
      "input": { "service": "checkout", "hours": 2 },
      "output": { "incidents": [...] },
      "latency_ms": 234
    },
    {
      "step": 2,
      "type": "tool_call",
      "tool": "list_recent_changes",
      "input": { "service": "checkout", "minutes": 30 },
      "output": { "changes": [...] },
      "latency_ms": 189
    },
    {
      "step": 3,
      "type": "reasoning",
      "content": "Correlacionando mudança de configuração de DB com spike de latência..."
    },
    {
      "step": 4,
      "type": "final_response",
      "content": "Root cause: mudança de pool size na conexão ao PostgreSQL às 10:15 UTC"
    }
  ],
  "feedback": {
    "rating": 5,
    "outcome": "resolved",
    "actual_root_cause": "PostgreSQL connection pool exhaustion",
    "was_correct": true,
    "time_to_resolve_minutes": 8
  }
}
```

### Componentes a Implementar no NexTraceOne

#### 1. Endpoint de Feedback (Backend .NET)

```
POST /api/ai/executions/{executionId}/feedback
{
  "rating": 5,                    // 1-5
  "outcome": "resolved|partial|incorrect",
  "comment": "Root cause estava correcto, acções resolveram o problema",
  "actual_outcome": "..."         // Opcional: o que realmente aconteceu
}
```

#### 2. Trajectory Exporter (Background Service .NET)

- Corre a cada N minutos
- Selecciona execuções com feedback confirmado
- Serializa para formato Agent Lightning
- Envia para Agent Lightning Trainer via HTTP ou file queue

#### 3. Agent Lightning Trainer (Python Service Standalone)

- Novo serviço Python (não no .NET monolith)
- Consome trajectórias via endpoint ou directório partilhado
- Executa ciclos de treino RL (GRPO para Qwen, APO para GPT-4o/Claude)
- Actualiza modelos Ollama via LoRA push
- Persiste métricas de melhoria por agente

#### 4. Feedback UI (Frontend React)

Após cada resposta de agente, interface discreta de feedback:

```
[  ★★★★★  ] [👍 Útil] [👎 Incorrecta] [💬 Comentar]
Acção tomada: [Resolvido ✓] [Parcial] [Ignorado]
```

---

## Reward Functions por Agente

O Agent Lightning usa funções de reward customizadas. Para o NexTraceOne:

### `incident-responder`

```python
def compute_reward(trajectory, feedback):
    base_reward = feedback.rating / 5.0          # 0.0 – 1.0
    if feedback.was_root_cause_correct:
        base_reward += 0.3
    if feedback.time_to_resolve_minutes < 15:    # MTTR < 15 min
        base_reward += 0.2
    if trajectory.steps_count < 5:               # Resposta eficiente
        base_reward += 0.1
    return min(base_reward, 1.0)
```

### `change-advisor`

```python
def compute_reward(trajectory, feedback):
    base_reward = feedback.rating / 5.0
    if feedback.outcome == "no_incident_after_deploy":
        base_reward += 0.4    # Deploy correu bem após análise positiva
    if feedback.outcome == "rollback_needed" and feedback.was_risk_flagged:
        base_reward += 0.3    # Risco foi correctamente identificado
    if feedback.outcome == "rollback_needed" and not feedback.was_risk_flagged:
        base_reward -= 0.5    # Falso negativo — penalizar
    return max(0.0, min(base_reward, 1.0))
```

---

## Estratégia de Treino por Tipo de Modelo

### Modelos Locais (Ollama — Qwen 2.5 Coder 32B)

- Algoritmo: GRPO (melhor para raciocínio multi-step)
- Técnica: LoRA fine-tuning incremental (não requer retreino completo)
- Frequência: Ciclo de treino a cada 500 trajectórias com feedback
- Custo: GPU local — sem custo adicional por request
- Rollback: Snapshots de LoRA weights por versão

### Modelos Externos (OpenAI GPT-4o, Anthropic Claude)

- Algoritmo: APO (Automatic Prompt Optimization)
- Técnica: Optimização automática do system prompt com base em trajectórias
- Frequência: Revisão de prompts a cada 200 trajectórias
- Custo: Sem fine-tuning billing — apenas optimização de prompts
- Rollback: Versionamento de prompts no `DefaultAgentCatalog`

---

## Dashboard de Melhoria de Agentes

Nova página no AI Hub (`/ai/agent-performance`):

```
┌─────────────────────────────────────────────────────────┐
│  Agent Performance Dashboard                            │
├──────────────────┬──────────────┬────────────┬──────────┤
│ Agente           │ Accuracy 30d │ Trend      │ RL Cycles│
├──────────────────┼──────────────┼────────────┼──────────┤
│ incident-responder│ 87% (+12%)  │ ↑ Melhora  │ 14       │
│ change-advisor   │ 79% (+8%)   │ ↑ Melhora  │ 9        │
│ service-scaffold │ 92% (+5%)   │ → Estável  │ 6        │
│ contract-designer│ 74% (+3%)   │ ↑ Melhora  │ 4        │
└──────────────────┴──────────────┴────────────┴──────────┘

Trajectórias colectadas: 2.847
Com feedback confirmado: 1.203 (42%)
Ciclos RL completados: 33
```

---

## Continual Learning — Evitar Catastrophic Forgetting

Um risco crítico em RL é o modelo "esquecer" capacidades anteriores ao aprender novas. A estratégia de mitigação:

1. **Experience Replay**: Manter buffer de trajectórias históricas (últimas 10K)
2. **EWC (Elastic Weight Consolidation)**: Proteger pesos importantes para tarefas antigas
3. **Validation Sets**: Avaliar performance em tasks anteriores antes de cada deploy de novo modelo
4. **Gradual Rollout**: Novo modelo começa com 10% do tráfego, cresce com confirmação de não-regressão

---

## Cronograma de Implementação

| Semana | Entrega |
|---|---|
| 1–2 | Endpoint de feedback + schema de trajectória |
| 3–4 | Trajectory Exporter (background service) |
| 5–6 | Agent Lightning Python service (setup + reward functions básicas) |
| 7–8 | Primeiro ciclo RL com `incident-responder` (modelo Ollama) |
| 9 | APO para agentes externos (GPT-4o/Claude) |
| 10 | Dashboard de performance + feedback UI no frontend |

---

## Referências

- [AI-EVOLUTION-ROADMAP.md](./AI-EVOLUTION-ROADMAP.md) — Roadmap geral
- [Agent Lightning Paper — arxiv.org/abs/2508.03680](https://arxiv.org/abs/2508.03680)
- [Agent Lightning GitHub — microsoft/agent-lightning](https://github.com/microsoft/agent-lightning)
- [Agent Lightning Blog — Microsoft Research](https://www.microsoft.com/en-us/research/blog/agent-lightning-adding-reinforcement-learning-to-ai-agents-without-code-rewrites/)
- [Reinforcement Learning Applications 2026](https://atxsoft.com/reinforcement-learning-applications-2026/)
