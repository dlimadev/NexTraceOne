# ADR-006 — Agent Runtime Foundation (Fase 3)

**Estado:** Aceite  
**Data:** 2026-03-21  
**Autores:** Equipa NexTraceOne  
**Pilares afetados:** AI-assisted Operations, Contract Governance, Source of Truth, AI Governance

---

## Contexto

A Fase 1 estabeleceu o domínio de infraestrutura de IA (providers, models, token budgets, access policies).
A Fase 2 introduziu a experiência de chat assistido (sessões, mensagens, model selector, agent selector básico).

Para cumprir a visão do NexTraceOne como plataforma de **AI-assisted Operations** e **Contract Governance**, é necessário um runtime de agents real: com agents governados, auditáveis, extensíveis e capazes de produzir artefactos operacionais verificáveis (contratos OpenAPI, schemas Kafka, cenários de teste, análises).

Este ADR documenta as decisões arquiteturais da **Fase 3 — Agent Runtime Foundation**.

---

## Decisões

### 1. Agent como entidade de primeira classe com ciclo de vida próprio

Os agents (`AiAgent`) são entidades de domínio com um ciclo de vida explícito:

```
Draft → PendingReview → Active → Published → Archived
                                            → Blocked
```

O estado `Blocked` é terminal e impede publicações futuras sem intervenção administrativa.  
O versionamento é automático a cada publicação (`Publish()` incrementa `Version`).

**Justificação:** Agents publicados em produção precisam de rastreabilidade, auditoria e controlo de qualidade. Um agent sem ciclo de vida gerido é um risco operacional.

---

### 2. Modelo de ownership com três camadas

```
AgentOwnershipType: System | Tenant | User
AgentVisibility:    Private | Team | Tenant
```

- **System agents** são imutáveis (criados via seed), visíveis a todos os tenants, e não podem ser modificados via API pública. `UpdateDefinition()` retorna erro para agents System.
- **Tenant agents** pertencem à organização, visíveis a todos os membros.
- **User agents** são pessoais, visíveis apenas ao dono ou à equipa (se `Visibility = Team`).

O acesso é resolvido por `IsAccessibleBy(userId, teamId)` na entidade, sem dependência de serviços externos.

**Justificação:** Simplicidade no modelo de segurança. O controlo de acesso está no domínio, não distribuído na infraestrutura.

---

### 3. Execução gravada e auditável (`AiAgentExecution`)

Cada execução de agent persiste:

- Input e output completos (JSON)
- Modelo e provider utilizados
- Tokens consumidos (prompt, completion, total)
- Duração em milissegundos
- Status (`Running → Completed | Failed | Cancelled`)
- CorrelationId para rastreabilidade entre sistemas
- Steps opcionais (JSON array de etapas intermédias)

O `CorrelationId` é gerado automaticamente se não fornecido, permitindo correlação com logs, traces e incidentes externos.

**Justificação:** Auditoria completa é obrigatória para qualquer sistema de IA governado. Execuções não rastreadas não satisfazem os requisitos regulatórios e de compliance.

---

### 4. Artefactos como output verificável (`AiAgentArtifact`)

Agents especializados produzem artefactos tipados:

| `AgentArtifactType`  | Descrição                          |
|---------------------|------------------------------------|
| `OpenApiDraft`      | Contrato OpenAPI gerado por IA     |
| `TestScenarios`     | Cenários de teste (JSON)           |
| `KafkaSchema`       | Schema de tópico Kafka             |
| `Documentation`     | Documentação técnica (Markdown)    |
| `Analysis`          | Análise de segurança/architecture  |
| `CodeReview`        | Revisão de código                  |
| `Checklist`         | Checklist operacional              |

O ciclo de vida do artefacto é:

```
Pending → Approved (por revisor humano)
        → Rejected (com notas obrigatórias)
        → Superseded (quando uma nova execução gera versão mais recente)
```

A aprovação humana é intencional: artefactos de IA não devem ser publicados automaticamente sem revisão.

**Justificação:** Artefactos de IA para contratos e produção exigem human-in-the-loop. Sem aprovação explícita, o NexTraceOne não pode ser Source of Truth de contratos.

---

### 5. Pipeline de execução centralizado em `AiAgentRuntimeService`

O serviço de runtime implementa um pipeline de 12 passos:

1. Resolver agent por ID
2. Validar estado ativo
3. Validar acesso do utilizador (`IsAccessibleBy`)
4. Resolver modelo (override → preferido → default `"chat"`)
5. Validar modelo permitido para o agent (`IsModelAllowed`)
6. Resolver provider via `IAiProviderFactory`
7. Iniciar execução (`AiAgentExecution.Start`)
8. Construir system prompt (SystemPrompt + Objective + OutputSchema)
9. Executar inferência via `IChatCompletionProvider.CompleteAsync`
10. Registar execução como `Completed` ou `Failed`
11. Incrementar `ExecutionCount` no agent
12. Gerar artefactos tipados com base na categoria do agent

Este pipeline é síncrono e transacional — toda a execução ocorre dentro da mesma unidade de trabalho.

**Justificação:** Um pipeline explícito e auditável é mais seguro que uma abordagem baseada em eventos para execuções síncronas. Eventos assíncronos são candidatos a uma Fase futura.

---

### 6. Agents especializados como agents oficiais (seed)

Três agents oficiais adicionados para o domínio de contratos:

| Slug                             | Categoria        | Artefacto gerado    |
|---------------------------------|------------------|---------------------|
| `api-contract-author`           | `ApiDesign`      | `OpenApiDraft`      |
| `api-test-scenario-generator`   | `TestGeneration` | `TestScenarios`     |
| `kafka-schema-contract-designer`| `EventDesign`    | `KafkaSchema`       |

Estes agents são do tipo `System`, imutáveis via API, e visíveis a todos os tenants.

**Justificação:** Agents de contrato são centrais ao papel do NexTraceOne como plataforma de Contract Governance. Precisam de garantias de imutabilidade e disponibilidade universal.

---

### 7. Restrições de governança por agent

Cada agent pode restringir:

- **Modelos permitidos** (`AllowedModelIds` — lista CSV de GUIDs). Lista vazia = todos os modelos.
- **Tools permitidas** (`AllowedTools` — lista CSV). Reservado para fases futuras.
- **Override de modelo pelo utilizador** (`AllowModelOverride`). Agents System bloqueiam override por omissão.

**Justificação:** Agents para contratos de produção não devem permitir substituição de modelo ad hoc. A qualidade e reprodutibilidade dependem do modelo fixo.

---

## Alternativas não escolhidas

### Pipeline assíncrono baseado em eventos
Rejeitado para esta fase. A complexidade de rastreamento, compensação e rollback aumenta significativamente. Adequado para execuções de longa duração (Fase futura).

### Agents como simples PromptTemplates
Rejeitado explicitamente. Agents sem ciclo de vida, artefactos, auditoria e governança de modelo são apenas configurações de sistema, não entidades de negócio.

### Aprovação automática de artefactos
Rejeitado. Artefactos de IA para contratos de produção exigem human-in-the-loop. A aprovação automática viola o princípio de Source of Truth controlado.

---

## Consequências

### Positivas
- Agents são entidades de domínio ricas, governáveis e auditáveis
- Execuções são rastreáveis end-to-end com correlação de telemetria
- Artefactos de IA têm ciclo de vida explícito e passam por aprovação humana
- O NexTraceOne pode servir como Source of Truth para contratos gerados por IA
- Governança de modelos por agent garante consistência e reprodutibilidade

### Negativas / Trade-offs
- Overhead de persistência por execução (mitigado por índices seletivos)
- Human-in-the-loop obrigatório pode atrasar pipelines de contract generation
- Pipeline síncrono limita execuções de longa duração (endereçar em Fase futura com execução assíncrona)

---

## Impacto em módulos

| Módulo           | Impacto                                               |
|-----------------|-------------------------------------------------------|
| AI Governance    | Novos DbSets, 6 handlers, 7 endpoints, 3 configs EF  |
| Contract Governance | Artefactos OpenAPI/Kafka produzidos por agents    |
| Change Intelligence | CorrelationId permite correlação futura           |
| Auditing         | Todas as execuções e reviews têm rastreabilidade completa |

---

## Referências

- [ADR-005 — AI Runtime Foundation](./ADR-005-ai-runtime-foundation.md)
- [AI Architecture](../AI-ARCHITECTURE.md)
- [Contract Governance Vision](../CONTRACT-STUDIO-VISION.md)
- `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Domain/Governance/`
- `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Application/Governance/Services/`
- Migration: `20260321183633_AddAgentRuntimeFoundation`
