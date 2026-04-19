# NexTraceOne — Propostas de Inovação

> **Baseado no estado real do produto** — Ver [ESTADO-ATUAL-PRODUTO.md](./ESTADO-ATUAL-PRODUTO.md)  
> **Princípio:** Só propor inovações que o estado atual suporta. Cada proposta identifica a base existente que pode ser aproveitada.  
> **Nota:** Este documento complementa o [INOVACAO-ROADMAP.md](./INOVACAO-ROADMAP.md) existente, focando em inovações que resolvem gaps reais identificados na análise.

---

## Contexto: O Que o Produto Já Faz Bem

Antes de propor inovações, é importante reconhecer que o NexTraceOne já tem capacidades avançadas que muitos produtos enterprise não têm:

- AI com grounding contextual real (não chatbot genérico)
- Semantic diff de contratos (não só diff textual)
- Blast radius automático para mudanças
- Contract drift detection com traces OTel
- FinOps integrado no workflow de governança
- Outbox pattern para consistência eventual real

As propostas abaixo **partem desta base** e propõem como torná-la mais poderosa.

---

## Tier 1 — Fechar Gaps Existentes com Valor Real

### 1.1 Correlação Incident↔Change com ML Básico

**Gap que resolve:** Correlação incident↔change atual é 0% dinâmica (timestamp+service apenas)

**Proposta:**
Implementar correlação baseada em probabilidade usando dados históricos já armazenados:

```
P(incident causado por change) = 
  P(temporal_proximity) × P(service_overlap) × P(historical_pattern) × P(change_blast_radius)
```

**Componentes necessários:**
1. `CorrelationScoringEngine` — calcula score 0-100 para cada par (incident, change)
2. Janela temporal configurável: "considerar changes das últimas X horas"
3. Histórico de falsos positivos/negativos para treino incremental
4. Feedback loop: operator confirma/nega correlação → treina o modelo

**Base aproveitável:**
- `IIncidentCorrelationRepository` já existe
- `IChangeIntelligenceReader` já existe
- `BlastRadius` calculado por release já existe
- Blast radius pode ser usado como peso na correlação

**Diferenciação:** Backstage e Datadog não fazem correlação incident↔change automática. É um gap de mercado.

**Esforço:** 3-4 semanas (1 backend, 1 frontend, 1-2 tuning)

---

### 1.2 Push Real para VCS (GitHub/GitLab/Azure DevOps)

**Gap que resolve:** `PushToRepository` apenas gera comandos git para copy-paste

**Proposta:**
Integração real com APIs dos VCS providers usando os conectores de Integrations já existentes:

```csharp
public interface IVcsIntegrationPort
{
    Task<PushResult> PushFilesAsync(
        VcsProvider provider, 
        string repoUrl,
        string branch,
        IReadOnlyList<ScaffoldedFile> files,
        string commitMessage,
        CancellationToken ct);
    
    Task<string> CreatePullRequestAsync(
        VcsProvider provider,
        CreatePrRequest request,
        CancellationToken ct);
}
```

**Providers a implementar:**
- GitHub API v3 (via Octokit.NET ou HTTP direto)
- GitLab API v4
- Azure DevOps REST API

**Base aproveitável:**
- `IntegrationsDbContext` com `WebhookSubscription` já existe
- `GitProvider` enum já existe em `PushToRepository.cs`
- OAuth/API Key management já existe no módulo Integrations
- `GenerateServerFromContract` + `ScaffoldServiceFromTemplate` já geram os ficheiros

**Impacto do produto:** O Developer Portal passa de "gerador de instrução manual" para "criador de serviço em 1-click" com PR automático.

**Esforço:** 4-5 semanas (implementação dos 3 providers + UI de configuração de tokens)

---

### 1.3 Dead Letter Queue — Dashboard e Recovery

**Gap que resolve:** Mensagens de Outbox que falham são invisíveis para operators

**Proposta:**
1. Tabela `outbox_dead_letters` para mensagens que excederam max retries
2. API: `GET /api/v1/platform/outbox/dead-letters`
3. UI: Dashboard de mensagens bloqueadas com:
   - Tipo de evento, timestamp, número de tentativas, último erro
   - Botão "Retry" (reprocessar manualmente)
   - Botão "Discard" (marcar como resolvido sem reprocessar)
4. Alerta automático se dead letters acumularem (integrar com `UserAlertRule`)

**Base aproveitável:**
- Outbox pattern já implementado com 25 processors
- `ModuleOutboxProcessorJob` já tem lógica de retry
- `UserAlertRule` já existe para alertas configuráveis
- Dashboard infrastructure (PageContainer, DataTable, StatCard) no frontend

**Esforço:** 2-3 semanas

---

### 1.4 Contract Drift Budget

**Análogo ao Error Budget de SLO, mas para contratos.**

**Conceito:**
- Cada contrato tem um "drift budget" mensal: "máximo de horas com drift não resolvido"
- Quando o drift budget é consumido, o sistema bloqueia deploys automáticos que afetam esse contrato
- Equipa pode ver "quanto budget de drift resta este mês"

**Base aproveitável:**
- `ContractDriftDetection` já implementado e funcional
- `SLO/SLA` engine e burn rate já implementados em Reliability
- Gate evaluation para promotion já existe

**Diferenciação:** Nenhum produto no mercado tem "drift budget". É uma proposta genuinamente nova que combina os conceitos de SLO com contract governance.

**Esforço:** 2-3 semanas (principalmente domain logic + UI)

---

## Tier 2 — Novas Capacidades de Alto Valor

### 2.1 AI-Powered Root Cause Analysis (RCA) Automático

**Contexto:** O AI Assistant já faz grounding com incidents, changes, e services. Mas o utilizador tem de iniciar a conversa.

**Proposta:**
Quando um incidente é criado, o sistema automaticamente:
1. Agrega contexto relevante (changes recentes, traces, métricas, contratos afetados)
2. Invoca o LLM com este contexto para gerar uma hipótese de RCA inicial
3. Apresenta a hipótese no incident detail como "AI Suggestion" (não como verdade, mas como ponto de partida)
4. O engineer valida ou rejeita a hipótese — feedback vai para melhorar o modelo

```typescript
// UI de incidente com RCA sugerido
<AiRcaSuggestion
  suggestion={incident.aiRcaSuggestion}
  confidence={incident.aiRcaConfidence}
  sources={incident.aiRcaSources}
  onAccept={() => markRcaAsAccepted(incident.id)}
  onReject={() => openFeedbackDialog(incident.id)}
/>
```

**Base aproveitável:**
- `IIncidentModule` já existe com dados reais
- `IChangeIntelligenceReader` para correlação
- AI grounding pipeline já implementado (DocumentRetrievalService, etc.)
- `AiAgentRuntimeService` para execução de tool calls
- Guardrails de segurança AI já implementados

**Diferenciação real:** Jira, PagerDuty, e Opsgenie não têm RCA automático contextualizado com dados de contratos e changes. É um diferenciador único.

**Esforço:** 4-6 semanas

---

### 2.2 Service Health Score — Unificado e Temporal

**Contexto:** O produto tem SLO tracking, contract health timeline, maturity benchmark, e cost attribution. Mas não há um score único por serviço.

**Proposta:**
Um "Composite Health Score" por serviço, calculado como:
```
HealthScore = (
  SLO_Compliance × 0.30 +
  ContractCompliance × 0.25 +
  ChangeStability × 0.20 +
  SecurityGating × 0.15 +
  DeveloperExperience × 0.10
)
```

Com:
- Histórico temporal (últimos 30/60/90 dias)
- Comparação com benchmark do domínio
- Drill-down por dimensão
- Alertas quando score cai abaixo do threshold

**Base aproveitável:**
- `ServiceMaturityBenchmark` já existe (PA-28)
- `ContractHealthTimeline` já existe (PA-27)
- `ReliabilityDbContext` com SLO data existe
- `SecurityGate` existe no módulo Governance
- `DeveloperExperienceScorePage` existe no frontend

**O gap atual:** Estes dados existem mas estão em silos separados — não há score unificado.

**Esforço:** 3-4 semanas

---

### 2.3 GraphQL Federation Completa

**Gap que resolve:** GraphQL cobre apenas 2 de 12 módulos

**Proposta:**
Completar a federação GraphQL para todos os módulos, seguindo o padrão já estabelecido:

```graphql
# Novos resolvers a adicionar
type Query {
  # Operational Intelligence
  incidentsSummary(teamName: String, environment: String): IncidentsSummary
  reliabilityOverview(serviceId: ID): ReliabilityOverview
  
  # Governance
  complianceSummary(environment: String): ComplianceSummary
  riskCenterOverview: RiskCenterOverview
  
  # AI Knowledge  
  aiUsageSummary(tenantId: ID): AiUsageSummary
  
  # Knowledge
  knowledgeCoverage(serviceId: ID): KnowledgeCoverage
  
  # + 6 módulos restantes
}
```

**Base aproveitável:**
- HotChocolate 14.3.0 já configurado
- `[ExtendObjectType("Query")]` pattern já estabelecido
- Subscriptions WebSocket já funcionais para 2 eventos

**Valor:** Clientes enterprise preferem GraphQL para dashboards e portais customizados. Com federation completa, o produto torna-se muito mais integrável.

**Esforço:** 3-4 semanas (um resolver por módulo, ~1-2 dias cada)

---

### 2.4 Multi-Tenancy Visual — Tenant Health Dashboard

**Contexto:** O produto suporta multi-tenancy mas não tem uma visão do operador da plataforma que mostra a saúde de todos os tenants.

**Proposta:**
Um dashboard "Platform Admin" que mostra:
- Todos os tenants ativos e seu estado de saúde
- Uso de recursos por tenant (requests, storage, AI tokens)
- Alertas de tenants com problemas (rate limit atingido, erros elevados)
- Quick actions: suspender tenant, aumentar limits, inspecionar logs por tenant

**Base aproveitável:**
- RLS já implementado com `tenant_id` em todas as tabelas
- `ProductAnalyticsDbContext` com `AnalyticsEvent` por tenant
- `UserAlertRule` para alertas configuráveis
- Role de `PlatformAdmin` provavelmente existe no RBAC

**Esforço:** 3 semanas

---

### 2.5 Live Contract Validation — Integração com Traffic Mirroring

**Contexto:** O drift detection detecta desvios com OTel traces históricos. Mas e se fosse possível validar contratos em tempo real com tráfego real?

**Proposta:**
Uma integração com Envoy/Nginx/service mesh para:
1. Capturar uma amostra de requests/responses de produção
2. Validar automaticamente contra o contrato publicado
3. Marcar divergências em tempo real no Contract Detail
4. Gerar score de conformidade contínuo (não snapshot)

**Base aproveitável:**
- Ingestion API já aceita payloads de telemetria externa
- `VerifyContractCompliance` handler já existe
- Contract schema parsing para OpenAPI/REST já implementado

**Diferenciação:** Nenhum produto OSS ou comercial oferece validação de contrato com tráfego real de produção. É inovação real.

**Esforço:** 6-8 semanas (requer integração com service mesh)

---

## Tier 3 — Inovações a Médio Prazo

### 3.1 Developer Experience AI Coach

**Proposta:**
Um modo "AI Coach" que analisa:
- Padrões de deploy do developer (frequência, blast radius médio, rollbacks)
- Contratos que frequentemente têm drift nos seus serviços
- Incidentes onde o developer estava envolvido

E sugere proativamente:
- "O teu service X tem blast radius médio de 8 — considera isolar os consumidores"
- "Os teus deploys às sextas têm 3× mais incidentes — considera freeze window automática"
- "O contrato do service Y tem drift há 2 semanas sem resolução"

**Base aproveitável:**
- `ProductAnalytics` com eventos de uso já existe
- AI grounding pipeline já existe
- `DeveloperExperienceScorePage` existe como ponto de entrada

---

### 3.2 Self-Healing Automation

**Proposta:**
Quando um drift é detetado, o sistema propõe ou executa automaticamente uma remediação:
- Contract drift → criar automaticamente uma PromoçãoRequest com o contrato correto
- SLO breach → escalar para on-call automaticamente via `AutomationRule`
- Outbox dead letter → retry automático com backoff adaptativo

**Base aproveitável:**
- `AutomationRule` com 10/10 handlers reais
- `ChaosExperiment` entity para validação pós-remediation
- Outbox pattern para coordinação

---

### 3.3 Continuous Architecture Fitness Functions

**Inspirado em "Building Evolutionary Architectures" de Ford/Parsons.**

**Proposta:**
Permitir que a equipa defina "fitness functions" arquiteturais verificadas continuamente:
- "Nenhum serviço core pode ter mais de 3 dependências críticas"
- "Todos os contratos de produção devem ter SLO definido"
- "Serviços com blast radius > 10 requerem aprovação de arquiteto"

Com score de fitness ao longo do tempo e alertas quando as funções são violadas.

**Base aproveitável:**
- `RulesetGovernance` com Spectral lint já implementado
- Blast radius calculado automaticamente
- Gate evaluation no módulo promotion

---

### 3.4 Contract Marketplace / Sharing

**Proposta:**
Um marketplace onde:
- Equipas publicam contratos reutilizáveis (schemas comuns, eventos padrão)
- Outros serviços podem "subscrever" um contrato do marketplace
- Quando o contrato do marketplace é atualizado, todos os subscritores são notificados

**Diferenciação:** Transforma o NexTraceOne de "repositório de contratos" para "plataforma de contratos" — muito mais defensável como produto.

**Base aproveitável:**
- `ContractTemplate` já existe no Configuration module
- `Subscription` management já existe no Developer Portal
- Notification engine para subscriber notification

---

## Resumo de Roadmap Sugerido

### Q2 2026 (Fechar Gaps Críticos)

| Iniciativa | Tipo | Semanas | Impacto |
|---|---|---|---|
| Correlação Incident↔Change com ML básico | Gap Fix + Valor | 4 | Alto |
| Dead Letter Queue Dashboard | Gap Fix | 2 | Alto |
| Push real para GitHub/GitLab | Gap Fix + Feature | 5 | Alto |
| Contract Drift Budget | Inovação | 3 | Diferenciador |

### Q3 2026 (Capacidades de Plataforma)

| Iniciativa | Tipo | Semanas | Impacto |
|---|---|---|---|
| AI-Powered RCA Automático | Inovação | 5 | Alto |
| Composite Service Health Score | Consolidação | 4 | Alto |
| GraphQL Federation Completa | Gap Fix | 4 | Médio |
| Multi-Tenancy Admin Dashboard | Feature | 3 | Médio |

### Q4 2026 (Diferenciação)

| Iniciativa | Tipo | Semanas | Impacto |
|---|---|---|---|
| Live Contract Validation (Traffic) | Inovação | 8 | Muito Alto |
| Developer Experience AI Coach | Inovação | 6 | Alto |
| Self-Healing Automation | Inovação | 5 | Alto |
| Contract Marketplace | Expansão | 8 | Estratégico |

---

## Critérios de Priorização

Para cada proposta antes de implementar, validar:

1. **Dor real:** Um utilizador em produção real manifestou esta dor?
2. **Base aproveitável:** Pode ser construído sobre código existente (não reescrever)?
3. **Testabilidade:** Pode ser testado com os frameworks existentes?
4. **Reversibilidade:** Se correr mal, pode ser desativado com feature flag?
5. **Differenciação:** Backstage, Datadog, ou Confluent fazem isto? Se sim, qual é a vantagem?
