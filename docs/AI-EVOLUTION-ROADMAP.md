# AI-EVOLUTION-ROADMAP.md

> **Data:** Abril 2026
> **Estado do módulo AI:** Funcional — base sólida, pronto para evolução enterprise
> **Autor:** Análise técnica + pesquisa de mercado (Abril 2026)

---

## Sumário Executivo

O módulo `AIKnowledge` do NexTraceOne possui uma arquitetura sólida com 14 agentes especializados, multi-provider (Ollama, OpenAI, Anthropic), sistema de guardrails, grounding cross-módulo e integração MCP. No entanto, os agentes são **estáticos**, os modelos locais são **insuficientes para produção**, e o sistema ainda **não aprende** com as interações.

Este roadmap define 5 fases de evolução para transformar o módulo de IA do NexTraceOne num sistema de inteligência **enterprise-grade**, capaz de gerar valor real para organizações, aprender continuamente e oferecer capacidades que nenhum concorrente disponibiliza hoje.

---

## Estado Atual — Diagnóstico

| Componente | Estado | Gap Identificado |
|---|---|---|
| Multi-provider (Ollama, OpenAI, Anthropic, LmStudio) | Implementado | Anthropic e LmStudio incompletos |
| 14 Agentes especializados | Definidos, estáticos | Não aprendem, não se adaptam |
| 6 Tools com JSON Schema | Implementado | Fallback text-based em modelos pequenos |
| 11 Guardrails (segurança, privacidade, compliance) | Implementado | Sem feedback loop de eficácia |
| Token quotas e budgets | Implementado | Checks por request sem cache |
| Grounding cross-módulo (Catalog, Changes, Incidents, Knowledge) | Implementado | Sem cache, DB calls separadas |
| MCP Integration (JSON-RPC 2.0) | Implementado | Base ideal para Skills |
| Modelos locais (deepseek-r1:1.5b, llama3.2:3b, codellama:7b) | Operacional | Inadequados para produção |
| Sistema de Skills dinâmicas | **Ausente** | Crítico para evolução |
| Aprendizagem contínua (RL) | **Ausente** | Crítico para melhoria autónoma |
| Memória organizacional | **Ausente** | Diferenciador único de mercado |

---

## Fase 0 — Quick Wins Imediatos (0–4 semanas)

Melhorias de alto impacto com baixo esforço que podem ser entregues imediatamente.

### 0.1 Completar Providers Existentes

- **Anthropic**: Finalizar implementação do `AnthropicProvider` — o `claude-3-5-sonnet` já está no catálogo mas o provider está incompleto
- **LmStudio**: Completar integração para suporte a modelos locais avançados
- **Objetivo**: Ter todos os providers configurados e operacionais

### 0.2 Cache de Token Quota

- Implementar cache Redis para verificações de quota por utilizador
- Reduz hit na base de dados de `N checks/request` para `1 check/minuto`
- **Impacto**: Redução de ~60% na latência de requests AI

### 0.3 Batch de Grounding Readers

- Unificar chamadas dos `GroundingReaders` em queries paralelas
- `CatalogGroundingReader` + `ChangeGroundingReader` + `IncidentGroundingReader` podem ser executados em paralelo
- **Impacto**: Redução de ~40% no tempo de construção de contexto

### 0.4 Upgrade de Modelos Locais

Adoptar **Qwen 2.5 Coder 32B** como modelo primário local (já documentado em `AI-MODELS-ANALYSIS.md`):

| Modelo | Parâmetros | Context Window | Tool Calling | Licença | VRAM |
|---|---|---|---|---|---|
| Qwen 2.5 Coder 32B | 32.5B | 131K tokens | ✅ Nativo | Apache 2.0 | 24 GB |
| Qwen 2.5 Coder 14B (fallback CPU) | 14B | 131K tokens | ✅ Nativo | Apache 2.0 | 64 GB RAM |
| Qwen 2.5 72B (reasoning avançado) | 72B | 131K tokens | ✅ Nativo | Apache 2.0 | 48 GB VRAM |

### 0.5 Completar Tool Calling Nativo

- Substituir fallback text-based por function calling nativo nos providers Ollama
- Garantir que todos os 14 agentes têm acesso consistente às 6 tools existentes

---

## Fase 1 — Sistema de Skills (4–10 semanas)

Implementação do sistema de **Agent Skills** baseado no padrão aberto da Anthropic (publicado como open standard em Dezembro de 2025).

**Objetivo**: Transformar os 14 agentes estáticos em capacidades dinâmicas, compostas e reutilizáveis.

### Entregas

- Entidade `AiSkill` com loader de `SKILL.md`
- `SkillRegistry` dinâmico (extensão do `InMemoryToolRegistry` existente)
- 12 Skills prioritárias criadas (ver `AI-SKILLS-SYSTEM.md` para detalhes completos)
- Interface de administração enterprise para provisionamento central de Skills
- Skills marketplace com permissões por tenant, team e utilizador

**Referência detalhada**: [AI-SKILLS-SYSTEM.md](./AI-SKILLS-SYSTEM.md)

---

## Fase 2 — Agent Lightning: Aprendizagem Contínua (10–20 semanas)

Integração do **Agent Lightning** (Microsoft Research, agosto 2025) para adicionar Reinforcement Learning aos agentes existentes sem reescrita de código.

**Objetivo**: Os agentes passam a melhorar automaticamente com cada interação real dentro do NexTraceOne.

### Entregas

- Endpoint de feedback por execução (`ExecutionId` + rating + comentário)
- Exportador de trajectórias de execução para Agent Lightning trainer
- Pipeline RL para modelos Ollama locais (fine-tuning)
- Prompt Optimization automática para agentes externos (OpenAI/Anthropic)
- Dashboard de melhoria por agente (accuracy over time, precision, recall)

**Referência detalhada**: [AI-AGENT-LIGHTNING.md](./AI-AGENT-LIGHTNING.md)

---

## Fase 3 — Capacidades Enterprise (20–36 semanas)

Conjunto de 8 capacidades de alto valor empresarial que elevam o NexTraceOne a um nível enterprise real.

### Capacidades

1. **Autonomous Incident War Room** — Sala de guerra autónoma em incidentes P0
2. **Regulatory Compliance Autopilot** — Mapeamento automático LGPD/GDPR/SOC2
3. **Technical Debt Business Quantifier** — Tech debt em custo de negócio real
4. **Natural Language Service Mesh** — Queries em linguagem natural sobre toda a infra
5. **Predictive Deployment Simulation** — Simulação animada de blast radius antes de deploy
6. **AI Change Confidence Score** — Score unificado 0-100 por deployment
7. **Self-Healing Pipeline** — Detecção + remediação autónoma de problemas
8. **AI-Driven SLA Intelligence** — SLAs baseados em dados históricos reais

**Referência detalhada**: [AI-ENTERPRISE-CAPABILITIES.md](./AI-ENTERPRISE-CAPABILITIES.md)

---

## Fase 4 — Inovações Sem Concorrência (36–52 semanas)

Três inovações que nenhum concorrente no espaço de platform engineering oferece hoje.

### Inovações

1. **Organizational Memory Engine (OME)** — Memória técnica organizacional temporal com grafo de conhecimento
2. **Multi-Tenant Federated Intelligence** — Aprendizagem colectiva entre tenants com privacidade preservada
3. **Proactive Architecture Guardian** — IA que detecta padrões de risco sem ser solicitada, antes de incidentes

**Referência detalhada**: [AI-INNOVATION-BLUEPRINT.md](./AI-INNOVATION-BLUEPRINT.md)

---

## Timeline Consolidado

```
2026 Q2 (Abr–Jun)
├── Fase 0: Quick Wins (4 semanas)
│   ├── Completar Anthropic + LmStudio providers
│   ├── Cache de token quota (Redis)
│   ├── Batch grounding readers
│   ├── Upgrade para Qwen 2.5 Coder 32B
│   └── Tool calling nativo em todos os providers

2026 Q2–Q3 (Mai–Jul)
└── Fase 1: Skills System (6 semanas)
    ├── AiSkill entity + SkillRegistry
    ├── SKILL.md loader + injecção de contexto
    ├── 12 Skills prioritárias
    └── Admin UI para gestão de Skills

2026 Q3–Q4 (Jul–Out)
└── Fase 2: Agent Lightning (10 semanas)
    ├── Feedback endpoint por execução
    ├── Trajectory exporter
    ├── RL pipeline para modelos locais
    └── Dashboard de melhoria de agentes

2026 Q4 — 2027 Q1 (Out–Jan)
└── Fase 3: Enterprise Capabilities (16 semanas)
    ├── Incident War Room + Compliance Autopilot
    ├── Tech Debt Quantifier + NL Service Mesh
    ├── Predictive Simulation + AI Confidence Score
    └── Self-Healing + SLA Intelligence

2027 Q1–Q2 (Jan–Jun)
└── Fase 4: Inovações Sem Concorrência (16 semanas)
    ├── Organizational Memory Engine
    ├── Federated Intelligence
    └── Proactive Architecture Guardian
```

---

## Métricas de Sucesso por Fase

| Fase | Métricas Chave |
|---|---|
| **Fase 0** | Latência AI < 800ms p95; 0 erros de tool calling |
| **Fase 1** | ≥ 12 Skills activas; NPS de utilização de agentes > 7/10 |
| **Fase 2** | Agents melhoram accuracy ≥ 15% após 30 dias de RL |
| **Fase 3** | MTTR reduzido ≥ 30%; compliance reports automáticos; deploy confidence score adoptado por ≥ 80% dos deploys |
| **Fase 4** | Retenção de contexto organizacional > 12 meses; 0 incidentes relacionados com padrões detectados pelo Guardian |

---

## Dependências Críticas

| Dependência | Necessária para | Proprietário |
|---|---|---|
| GPU ≥ 24 GB VRAM (RTX 4090 / A6000) | Qwen 2.5 Coder 32B | Infra |
| Redis cluster | Cache de quotas | Infra |
| Agent Lightning Python service | Fase 2 | AI Team |
| Feedback UI por execução | Fase 2 | Frontend |
| PostgreSQL para SkillRegistry | Fase 1 | Backend |
| Differential privacy library | Fase 4 (Federated) | AI Team |

---

## Referências

- [AI-SKILLS-SYSTEM.md](./AI-SKILLS-SYSTEM.md) — Arquitectura de Skills
- [AI-AGENT-LIGHTNING.md](./AI-AGENT-LIGHTNING.md) — Integração RL
- [AI-ENTERPRISE-CAPABILITIES.md](./AI-ENTERPRISE-CAPABILITIES.md) — Capacidades enterprise
- [AI-INNOVATION-BLUEPRINT.md](./AI-INNOVATION-BLUEPRINT.md) — Inovações únicas
- [AI-MODELS-ANALYSIS.md](./AI-MODELS-ANALYSIS.md) — Análise de modelos
- [AI-ARCHITECTURE.md](./AI-ARCHITECTURE.md) — Arquitectura actual
- [FUTURE-ROADMAP.md](./FUTURE-ROADMAP.md) — Roadmap geral do produto
