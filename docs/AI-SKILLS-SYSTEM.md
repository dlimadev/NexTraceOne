# AI-SKILLS-SYSTEM.md

> **Data:** Abril 2026
> **Fase:** 1 do AI Evolution Roadmap (4–10 semanas)
> **Padrão base:** Anthropic Agent Skills Open Standard (Dezembro 2025)

---

## O Que São Agent Skills

Agent Skills são diretórios de instruções, scripts e recursos que os agentes carregam **dinamicamente** para executar tarefas especializadas de forma repetível e consistente. Publicado como open standard pela Anthropic em Dezembro de 2025, o formato foi adoptado por Atlassian, Figma, Stripe, Notion e Zapier, e conta com mais de 20K stars no GitHub da comunidade.

Uma Skill é definida por um ficheiro `SKILL.md` que combina YAML de configuração com instruções Markdown. O sistema usa **progressive disclosure**: cada Skill ocupa apenas algumas dezenas de tokens quando resumida no contexto do agente, carregando os detalhes completos apenas quando a tarefa requerer.

### Estrutura de um SKILL.md

```markdown
---
name: incident-triage
version: 1.2.0
description: Triagem automática de incidentes com análise de root cause
author: system
tags: [incidents, operations, rca]
input_schema:
  incident_id: string
  service_name: string
  severity: enum[P0, P1, P2, P3]
output_schema:
  root_cause: string
  affected_services: array
  recommended_actions: array
  confidence_score: number
tools_required: [search_incidents, get_service_health, list_recent_changes]
models_preferred: [qwen2.5-coder-32b, gpt-4o, claude-3-5-sonnet]
---

## Instruções

1. Analisa os logs e métricas do serviço afectado nas últimas 2 horas
2. Correlaciona com mudanças recentes nos últimos 30 minutos
3. Verifica dependências upstream e downstream do serviço
4. Identifica padrões históricos de incidentes similares
5. Gera hipóteses ordenadas por probabilidade
6. Propõe acções correctivas específicas com passos detalhados

## Exemplos

Input: { incident_id: "INC-4521", service_name: "checkout", severity: "P0" }
Output: Root cause identificado em dependência de pagamento com 94% de confiança
```

---

## Arquitectura do Skills System no NexTraceOne

### Modelo de Domínio

O Skills System expande o domínio existente do `AIKnowledge` com uma nova entidade agregada:

```
NexTraceOne.AIKnowledge.Domain/
└── Skills/
    ├── AiSkill.cs                    ← Entidade agregada principal
    ├── AiSkillVersion.cs             ← Versionamento semântico de Skills
    ├── AiSkillExecution.cs           ← Log de cada execução
    ├── AiSkillFeedback.cs            ← Feedback para Agent Lightning
    ├── Enums/
    │   ├── SkillOwnershipType.cs     ← System | Tenant | User | Community
    │   ├── SkillVisibility.cs        ← Public | TeamOnly | Private
    │   └── SkillStatus.cs            ← Draft | Active | Deprecated
    └── DefaultSkillCatalog.cs        ← 12 Skills pré-instaladas
```

### Propriedades da Entidade `AiSkill`

```csharp
public sealed class AiSkill : AuditableEntity<AiSkillId>
{
    public string Name { get; }              // "incident-triage"
    public string DisplayName { get; }       // "Triagem de Incidentes"
    public string Description { get; }
    public string SkillContent { get; }      // Conteúdo do SKILL.md
    public string Version { get; }           // "1.2.0"
    public SkillOwnershipType OwnershipType { get; }
    public SkillVisibility Visibility { get; }
    public SkillStatus Status { get; }
    public string[] Tags { get; }
    public string[] RequiredTools { get; }
    public string[] PreferredModels { get; }
    public string InputSchema { get; }       // JSON Schema
    public string OutputSchema { get; }      // JSON Schema
    public int ExecutionCount { get; }
    public double AverageRating { get; }     // Calculado de AiSkillFeedback
    public string? ParentAgentId { get; }    // Skill acoplada a agente específico
    public bool IsComposable { get; }        // Pode ser chamada por outras Skills
}
```

### Camada de Application

```
NexTraceOne.AIKnowledge.Application/
└── Skills/
    ├── RegisterSkill/         ← Cria nova Skill (system ou custom)
    ├── UpdateSkill/           ← Actualiza versão
    ├── LoadSkill/             ← Carrega SKILL.md e injeta no contexto
    ├── ExecuteSkill/          ← Executa Skill com agente e modelo seleccionado
    ├── ListSkills/            ← Lista Skills disponíveis com filtros
    ├── GetSkillDetails/       ← Detalhes + histórico de execuções
    ├── RateSkillExecution/    ← Feedback para RL (Agent Lightning)
    ├── PublishSkill/          ← Publica para marketplace interno
    └── DeprecateSkill/        ← Depreca versão anterior
```

### Camada de Infrastructure

```
NexTraceOne.AIKnowledge.Infrastructure/
└── Skills/
    ├── SkillLoader.cs            ← Lê SKILL.md do disco ou base de dados
    ├── SkillRegistry.cs          ← Registry dinâmico (extensão do InMemoryToolRegistry)
    ├── SkillContextInjector.cs   ← Injeta conteúdo da Skill no system prompt
    ├── SkillExecutionLogger.cs   ← Persiste resultados e métricas
    └── SkillMarketplaceSync.cs   ← Sync com repositório de Skills da comunidade
```

---

## As 12 Skills Prioritárias do NexTraceOne

### Grupo 1: Operações

#### `incident-triage`
- **Agente base**: `incident-responder`
- **Valor**: RCA automatizada em P0/P1, reduz MTTR em 40–60%
- **Tools**: `search_incidents`, `get_service_health`, `list_recent_changes`
- **Output**: Root cause + affected services + acções correctivas + confidence score

#### `change-blast-radius`
- **Agente base**: `change-advisor`
- **Valor**: Análise de impacto antes de cada deployment, calcula risco em segundos
- **Tools**: `list_recent_changes`, `get_service_health`, `list_services`
- **Output**: Blast radius map + serviços afectados + probabilidade de rollback

#### `service-health-diagnosis`
- **Agente base**: `incident-responder`
- **Valor**: Diagnóstico profundo de saúde com correlação de telemetria
- **Tools**: `get_service_health`, `search_incidents`, `get_contract_details`
- **Output**: Health score + anomalias + tendências + alertas preemptivos

### Grupo 2: Engenharia

#### `contract-lint`
- **Agente base**: `contract-designer`
- **Valor**: Review automático de contratos REST/AsyncAPI contra boas práticas
- **Tools**: `get_contract_details`, `list_services`
- **Output**: Violations list + severity + fix suggestions + breaking change detection

#### `service-scaffold`
- **Agente base**: `service-scaffold-agent`
- **Valor**: Geração completa de scaffold de serviço em JSON/código
- **Tools**: `list_services`, `get_contract_details`
- **Output**: Estrutura de projecto + configurações + CI/CD + testes base

#### `test-scenario-generator`
- **Agente base**: `test-generator`
- **Valor**: Robot Framework tests gerados a partir de contratos reais
- **Tools**: `get_contract_details`, `get_service_health`
- **Output**: Robot Framework suites + edge cases + mock data

#### `dependency-risk-scan`
- **Agente base**: `dependency-advisor`
- **Valor**: Scan de CVEs, licenças e compliance em SBOM
- **Tools**: `list_services`, `get_contract_details`
- **Output**: CVE list + severity scores + license flags + remediation steps

### Grupo 3: Arquitectura

#### `architecture-fitness`
- **Agente base**: `architecture-fitness-agent`
- **Valor**: Avaliação de bounded context, regras de dependência, naming
- **Tools**: `list_services`, `get_contract_details`
- **Output**: Fitness report JSON + score + violations + improvement plan

#### `security-owasp-review`
- **Agente base**: `security-reviewer`
- **Valor**: Review automatizado OWASP Top 10 por contrato/serviço
- **Tools**: `get_contract_details`, `list_services`, `search_incidents`
- **Output**: OWASP checklist + gaps + severity + remediation guide

#### `event-schema-design`
- **Agente base**: `event-designer`
- **Valor**: Design e validação de schemas AsyncAPI/Kafka
- **Tools**: `get_contract_details`, `list_services`
- **Output**: AsyncAPI spec + Kafka topics + event schema + compatibility check

### Grupo 4: Gestão e Compliance

#### `tech-debt-quantifier`
- **Agente base**: `architecture-fitness-agent`
- **Valor**: Converte dívida técnica em custo de negócio mensurável
- **Tools**: `list_services`, `search_incidents`, `get_token_usage_summary`
- **Output**: Debt items + business cost estimate + incident probability + prioritization

#### `compliance-mapper`
- **Agente base**: `security-reviewer`
- **Valor**: Mapeia serviços para requisitos LGPD/GDPR/SOC2 automaticamente
- **Tools**: `list_services`, `get_contract_details`, `search_incidents`
- **Output**: Compliance map + gaps + evidence package + audit checklist

---

## Progressive Disclosure — Como Funciona

O sistema carrega Skills de forma inteligente para não sobrecarregar o contexto do modelo:

```
1. SkillRegistry lista todas as Skills disponíveis (resumos ~30 tokens cada)
2. Agente identifica qual(is) Skills são relevantes para a tarefa
3. SkillLoader carrega o SKILL.md completo apenas da(s) Skill(s) seleccionadas
4. SkillContextInjector injeta no system prompt antes da execução
5. Agente executa com contexto completo e especializado
6. SkillExecutionLogger persiste resultado + métricas + feedback
```

---

## Enterprise Administration

O Skills System inclui gestão centralizada para administradores enterprise:

### Provisionamento Central

- Admins definem quais Skills estão disponíveis por tenant, team ou role
- Skills obrigatórias podem ser configuradas (ex: `security-owasp-review` sempre activa para architects)
- Versões de Skills podem ser fixadas por ambiente (prod usa v1.2.0, staging usa v2.0.0-beta)

### Políticas de Acesso

```
TenantAdmin → Pode criar e publicar Skills para o tenant
TeamLead    → Pode criar Skills privadas e partilhar com a sua equipa
Engineer    → Pode usar Skills disponíveis e criar Skills pessoais
ReadOnly    → Só pode listar e executar Skills públicas
```

### Métricas e Observabilidade

- Execuções por Skill (total, por agente, por utilizador, por tenant)
- Ratings médios e distribuição de feedback
- Latência média e erros por Skill
- Skills mais usadas e com melhor performance
- Custo de tokens por Skill (integrado com AiTokenUsageLedger)

---

## Integração com Skills Open Standard da Anthropic

O NexTraceOne pode importar Skills do repositório público da Anthropic (`github.com/anthropics/skills`) e partilhar Skills do NexTraceOne com a comunidade:

```
SkillMarketplaceSync:
  ├── Import: Anthropic Skills Hub → NexTraceOne SkillRegistry
  ├── Export: NexTraceOne custom Skills → Anthropic Skills Hub (opt-in)
  └── Sync: Actualizações automáticas de versão com aprovação admin
```

Skills da comunidade disponíveis para importação imediata:
- PowerPoint/Excel/Word/PDF processing (Anthropic official)
- Atlassian Jira integration
- Figma design analysis
- Stripe payment flow documentation
- Notion knowledge base integration

---

## Referências

- [AI-EVOLUTION-ROADMAP.md](./AI-EVOLUTION-ROADMAP.md) — Roadmap geral
- [Anthropic Agent Skills Engineering Blog](https://www.anthropic.com/engineering/equipping-agents-for-the-real-world-with-agent-skills)
- [Anthropic Skills GitHub](https://github.com/anthropics/skills)
- [Agent Skills Open Standard — The New Stack](https://thenewstack.io/agent-skills-anthropics-next-bid-to-define-ai-standards/)
