# NexTraceOne — Future Seams para Extração de Serviços

> **Classificação:** Documento de Arquitetura — Roadmap de Decomposição  
> **Audiência:** Arquitetos de solução, Tech Leads, Platform Engineers  
> **Última atualização:** Março 2026

---

## Visão Geral

O NexTraceOne opera hoje como **Modular Monolith** seguindo o Archon Pattern v2,
com 7 Bounded Contexts, 6 Building Blocks transversais e 3 Platform Services
(`ApiHost`, `BackgroundWorkers`, `Ingestion.Api`). Toda a comunicação entre módulos
é feita exclusivamente via **Integration Events** (Outbox Pattern) e **interfaces
definidas em Contracts** — nunca por acesso direto a `DbContext` de outro módulo.

Esta disciplina arquitetural não é acidental. Cada módulo foi desenhado com
**costuras internas de extração** (seams) que permitem a decomposição futura em
serviços independentes **sem refatoração de fronteiras**. As costuras já existem
no código: portas hexagonais no Domain, Integration Events como contratos de
comunicação e DbContexts isolados por subcontexto.

O objetivo deste documento é mapear explicitamente essas costuras, definir os
critérios de extração e documentar o passo a passo para quando a escala de equipa,
volume de dados ou requisitos regulatórios justifiquem a decomposição.

### Princípios de Decomposição

| Princípio | Descrição |
|-----------|-----------|
| **Seam-First** | A costura existe antes da necessidade de cortar — nunca adicionada sob pressão. |
| **Contract-Driven** | Integration Events e Hexagonal Ports são os contratos; implementações são substituíveis. |
| **Data Sovereignty** | Cada futuro serviço terá o seu próprio schema/DB — RLS garante isolamento desde o monolith. |
| **Incremental** | Extrai-se um serviço de cada vez; o monolith continua funcional durante a transição. |
| **Reversível** | Se a extração não trouxer benefício mensurável, o serviço pode ser reabsorvido. |

---

## Serviços Futuros Planejados

### 1. Runtime Agent

**Origem no monolith:** `OperationalIntelligence/Runtime`

O Runtime Agent é o candidato mais provável a primeira extração. Ele recebe
sinais operacionais (métricas, traces, logs, health checks) de serviços
monitorizados e correlaciona-os com releases conhecidas no NexTraceOne. Em
produção enterprise, o volume de sinais pode facilmente atingir milhões de
eventos por minuto — escala incompatível com o monolith.

| Aspecto | Detalhe |
|---------|---------|
| **Portas Hexagonais** | `IRuntimeSignalIngestionPort`, `IRuntimeCorrelationPort` |
| **Integration Events** | `RuntimeSignalReceivedEvent`, `RuntimeAnomalyDetectedEvent` |
| **DbContext de Origem** | `RuntimeDbContext` (schema `runtime`) |
| **Tecnologia Alvo** | Agente standalone (.NET AOT ou Go) com push para `Ingestion.Api` |

**Caminho de Extração:**

1. Criar agente standalone que recolhe sinais (OTLP, Prometheus, webhooks).
2. O agente publica eventos para `Ingestion.Api` via HTTP/gRPC.
3. `Ingestion.Api` converte em `RuntimeSignalReceivedEvent` e publica no message bus.
4. O módulo `OperationalIntelligence` no monolith consome o evento normalmente.
5. A porta `IRuntimeSignalIngestionPort` passa de implementação local para client HTTP.
6. O schema `runtime` migra para base de dados dedicada (time-series recomendado).

**Benefícios da Extração:**

- Escalabilidade horizontal independente do monolith.
- Deploy próximo aos serviços monitorizados (edge deployment).
- Possibilidade de usar tecnologia otimizada para ingestão (Kafka, ClickHouse).
- Ciclo de release independente para atualizações de coletores.

---

### 2. AI Gateway

**Origem no monolith:** `AIKnowledge/ExternalAI`

O AI Gateway abstrai a comunicação com provedores LLM externos (OpenAI, Azure
OpenAI, Anthropic, modelos on-premise). A extração é motivada por requisitos de
segurança (isolamento de credenciais de LLM), controle de custos (rate limiting
centralizado) e flexibilidade tecnológica (proxy com circuit breaker dedicado).

| Aspecto | Detalhe |
|---------|---------|
| **Porta Hexagonal** | `IExternalAIRoutingPort` |
| **Integration Events** | `ExternalAIQueryRequestedEvent`, `ExternalAIResponseReceivedEvent` |
| **DbContext de Origem** | `ExternalAIDbContext` (schema `external_ai`) |
| **Tecnologia Alvo** | Serviço proxy ASP.NET Core ou FastAPI com cache semântico |

**Caminho de Extração:**

1. Criar serviço proxy que implementa `IExternalAIRoutingPort` via API HTTP.
2. Mover adaptadores de LLM (OpenAI, Azure) para o serviço proxy.
3. Substituir implementação local da porta por client HTTP no monolith.
4. `ExternalAIQueryRequestedEvent` torna-se mensagem no bus (RabbitMQ/Kafka).
5. O proxy responde com `ExternalAIResponseReceivedEvent` como reply.
6. Cache semântico, rate limiting e circuit breaker ficam no proxy.

**Benefícios da Extração:**

- Isolamento de credenciais de LLM do monolith principal.
- Rate limiting e controle de custos centralizados por provedor.
- Possibilidade de escalar horizontalmente para picos de consultas IA.
- Ciclo de release independente para suporte a novos modelos/provedores.
- Potencial reutilização como gateway IA para outros produtos da organização.

---

### 3. Audit Ledger

**Origem no monolith:** `AuditCompliance/Audit`

O Audit Ledger é o sistema de registo imutável com integridade criptográfica.
A extração é motivada primariamente por requisitos regulatórios: auditores e
compliance officers frequentemente exigem isolamento físico do sistema de
auditoria em relação ao sistema auditado. Além disso, a retenção de longo prazo
(anos) tem requisitos de armazenamento distintos do monolith operacional.

| Aspecto | Detalhe |
|---------|---------|
| **Porta Hexagonal** | `IAuditIntegrityPort` |
| **Integration Events** | `AuditEventRecordedEvent`, `AuditIntegrityCheckpointCreatedEvent` |
| **DbContext de Origem** | `AuditDbContext` (schema `audit`) |
| **Tecnologia Alvo** | Serviço append-only com hash chain (ASP.NET Core + PostgreSQL imutável ou ledger DB) |

**Caminho de Extração:**

1. Criar serviço append-only com API dedicada para escrita de eventos de auditoria.
2. Implementar hash chain criptográfica independente no novo serviço.
3. Monolith publica `AuditEventRecordedEvent` para o message bus.
4. Audit Ledger consome, persiste e gera `AuditIntegrityCheckpointCreatedEvent`.
5. A porta `IAuditIntegrityPort` passa de implementação local para client HTTP.
6. Schema `audit` migra para base de dados dedicada com política de retenção própria.
7. API de consulta de auditoria fica exclusiva no Audit Ledger (read model separado).

**Benefícios da Extração:**

- Isolamento regulatório: auditores acedem a um sistema fisicamente separado.
- Retenção independente: dados de auditoria com ciclo de vida distinto (anos vs. meses).
- Imutabilidade garantida: base de dados com permissões append-only.
- Performance: queries de auditoria não impactam o monolith operacional.
- Integridade: verificação criptográfica independente do sistema auditado.

---

### 4. Deployment Orchestrator

**Origem no monolith:** `ChangeGovernance/ChangeIntelligence`

O Deployment Orchestrator recebe webhooks de plataformas CI/CD (GitHub Actions,
Azure DevOps, GitLab CI, Jenkins), normaliza os eventos e orquestra decisões de
deployment baseadas em governance rules. A extração é motivada pela diversidade
de integrações e pela necessidade de alta disponibilidade no ponto de entrada
de eventos de deployment.

| Aspecto | Detalhe |
|---------|---------|
| **Portas Hexagonais** | `IDeploymentEventPort`, `IDeploymentDecisionPort` |
| **Integration Events** | `DeploymentEventReceivedEvent`, `ReleasePublishedEvent` |
| **DbContext de Origem** | `ChangeIntelligenceDbContext` (schema `change_intelligence`) |
| **Tecnologia Alvo** | Serviço ASP.NET Core com webhook receivers e state machine |

**Caminho de Extração:**

1. Criar serviço com endpoints de webhook para cada plataforma CI/CD suportada.
2. Mover adaptadores de CI/CD (GitHub, Azure DevOps, GitLab) para o novo serviço.
3. O serviço normaliza eventos e publica `DeploymentEventReceivedEvent` no bus.
4. O monolith consome o evento e executa Blast Radius, Change Score, etc.
5. Decisões de governance são publicadas como `ReleasePublishedEvent`.
6. A porta `IDeploymentEventPort` passa de implementação local para client HTTP.
7. `Ingestion.Api` roteia webhooks de CI/CD para o Deployment Orchestrator.

**Benefícios da Extração:**

- Alta disponibilidade: webhook receivers sempre responsivos, independente do monolith.
- Extensibilidade: novos adaptadores CI/CD sem tocar no monolith.
- Escalabilidade: picos de deployment (deploy trains) não impactam outras funcionalidades.
- Isolamento de falhas: problema num adaptador não afeta o restante do sistema.

---

### 5. Runtime Stream Processor

**Origem no monolith:** `OperationalIntelligence/Runtime` + `OperationalIntelligence/Cost`

O Runtime Stream Processor é um serviço de processamento em tempo real que
correlaciona sinais de runtime, deteta anomalias e calcula impactos de custo.
A extração é motivada pela natureza fundamentalmente diferente do workload:
stream processing contínuo vs. request/response do monolith.

| Aspecto | Detalhe |
|---------|---------|
| **Integration Events Consumidos** | `RuntimeSignalReceivedEvent` |
| **Integration Events Produzidos** | `RuntimeAnomalyDetectedEvent`, `CostAnomalyDetectedEvent` |
| **Tecnologia Alvo** | Apache Kafka Streams, Apache Flink ou .NET com canal dedicado |

**Caminho de Extração:**

1. Criar processador de stream que consome `RuntimeSignalReceivedEvent` do bus.
2. Implementar correlação temporal e detecção de anomalias no processador.
3. Produzir `RuntimeAnomalyDetectedEvent` e `CostAnomalyDetectedEvent` no bus.
4. Monolith consome eventos de anomalia para dashboards e alertas.
5. Porta `IRuntimeCorrelationPort` passa de implementação local para consumer do bus.
6. Schema de dados temporais migra para store otimizado (InfluxDB, TimescaleDB).

**Benefícios da Extração:**

- Tecnologia adequada: stream processing nativo vs. polling em batch.
- Latência: detecção de anomalias em tempo real (segundos vs. minutos).
- Escalabilidade: particionamento por tenant/serviço no stream processor.
- Independência: evolução do modelo de detecção sem deploy do monolith.

---

## Portas Hexagonais (Contracts for Extraction)

As portas hexagonais são interfaces definidas na camada Domain de cada módulo.
Representam os contratos estáveis que o domínio exige da infraestrutura. Quando
um serviço é extraído, a implementação da porta muda de "adapter local" para
"adapter HTTP/gRPC" — o domínio permanece inalterado.

### 1. IRuntimeSignalIngestionPort

```
src/modules/operationalintelligence/
  NexTraceOne.OperationalIntelligence.Domain/Runtime/Ports/IRuntimeSignalIngestionPort.cs
```

**Responsabilidade:** Abstrai a ingestão de sinais operacionais (métricas, traces,
health checks) de serviços monitorizados. No monolith, a implementação recebe
dados via `Ingestion.Api`. Após extração, a implementação será um client HTTP/gRPC
que comunica com o Runtime Agent standalone.

### 2. IRuntimeCorrelationPort

```
src/modules/operationalintelligence/
  NexTraceOne.OperationalIntelligence.Domain/Runtime/Ports/IRuntimeCorrelationPort.cs
```

**Responsabilidade:** Abstrai a correlação entre sinais de runtime e releases
conhecidas no NexTraceOne. Permite identificar qual release causou uma anomalia
ou degradação de performance. Após extração, a implementação será um consumer
de stream que correlaciona eventos em tempo real.

### 3. IExternalAIRoutingPort

```
src/modules/aiknowledge/
  NexTraceOne.AIKnowledge.Domain/ExternalAI/Ports/IExternalAIRoutingPort.cs
```

**Responsabilidade:** Abstrai o roteamento de consultas para provedores de IA
externos (OpenAI, Azure OpenAI, modelos on-premise). Encapsula seleção de
provedor, retry, fallback e cache semântico. Após extração, a implementação
será um client HTTP que comunica com o AI Gateway proxy.

### 4. IAuditIntegrityPort

```
src/modules/auditcompliance/
  NexTraceOne.AuditCompliance.Domain/Audit/Ports/IAuditIntegrityPort.cs
```

**Responsabilidade:** Abstrai a verificação de integridade criptográfica da
trilha de auditoria. Permite verificar hash chains, criar checkpoints de
integridade e detetar adulteração de registos. Após extração, a implementação
será um client HTTP que comunica com o Audit Ledger independente.

### 5. IDeploymentEventPort

```
src/modules/changegovernance/
  NexTraceOne.ChangeGovernance.Domain/ChangeIntelligence/Ports/IDeploymentEventPort.cs
```

**Responsabilidade:** Abstrai a receção de eventos de deployment de plataformas
CI/CD (GitHub Actions, Azure DevOps, GitLab CI, Jenkins). Normaliza o formato
proprietário de cada plataforma num modelo canónico interno. Após extração, a
implementação será um consumer do message bus.

### 6. IDeploymentDecisionPort

```
src/modules/changegovernance/
  NexTraceOne.ChangeGovernance.Domain/ChangeIntelligence/Ports/IDeploymentDecisionPort.cs
```

**Responsabilidade:** Abstrai a tomada de decisão sobre deployments com base em
regras de governance (Blast Radius, Change Score, aprovações pendentes). Permite
que o domínio solicite uma decisão sem conhecer a implementação (local, via API
ou via workflow engine). Após extração, a implementação será um client HTTP que
comunica com o Deployment Orchestrator.

---

## Integration Events como Contratos

Os Integration Events são o mecanismo de comunicação assíncrona entre módulos.
Hoje são publicados via `OutboxEventBus` (persistidos em tabela `outbox_messages`
e processados pelo `BackgroundWorkers`). Quando um módulo for extraído como
serviço independente, os mesmos eventos passam a ser publicados num message bus
externo (RabbitMQ, Kafka) — **sem alteração nos publishers ou subscribers**.

### IdentityAccess (2 eventos)

| Evento | Descrição |
|--------|-----------|
| `UserCreatedIntegrationEvent` | Publicado quando um novo utilizador é criado. Consumido por módulos que precisam de provisionar recursos per-user (Audit, Licensing). |
| `UserRoleChangedIntegrationEvent` | Publicado quando o papel de um utilizador muda. Consumido por módulos que ajustam permissões ou políticas baseadas em role. |

### CommercialGovernance (2 eventos)

| Evento | Descrição |
|--------|-----------|
| `LicenseActivatedIntegrationEvent` | Publicado quando uma licença é ativada com hardware binding. Consumido por Audit para registo e por Identity para ativar capabilities. |
| `LicenseThresholdReachedIntegrationEvent` | Publicado quando o uso de uma licença atinge limiar configurado. Consumido por notificações e dashboards operacionais. |

### Catalog (0 eventos)

O módulo Catalog não publica Integration Events atualmente. O `PortalAnalyticsEvent`
é uma entidade de domínio interna, não um evento de integração. Quando o Catalog
necessitar de notificar outros módulos (ex.: novo contrato publicado), será adicionado
um Integration Event seguindo o padrão existente.

### ChangeGovernance (5 eventos)

| Evento | Descrição |
|--------|-----------|
| `DeploymentEventReceivedEvent` | Publicado quando um webhook de CI/CD é recebido e normalizado. Inicia o fluxo de Change Intelligence. |
| `ReleasePublishedEvent` | Publicado quando uma release é promovida para um ambiente. Consumido por Audit, OperationalIntelligence e notificações. |
| `WorkflowApprovedEvent` | Publicado quando um workflow de aprovação é concluído com sucesso. Desbloqueia a promoção de release. |
| `WorkflowRejectedEvent` | Publicado quando um workflow de aprovação é rejeitado. Notifica o developer e regista em auditoria. |
| `PromotionRegisteredEvent` | Publicado quando uma promoção de ambiente é registada. Consumido por Audit e dashboards de governance. |

### OperationalIntelligence (3 eventos)

| Evento | Descrição |
|--------|-----------|
| `RuntimeSignalReceivedEvent` | Publicado quando um sinal operacional é ingerido. Consumido pelo processador de correlação e deteção de anomalias. |
| `RuntimeAnomalyDetectedEvent` | Publicado quando uma anomalia de runtime é detetada. Consumido por dashboards, alertas e Change Intelligence (correlação com releases). |
| `CostAnomalyDetectedEvent` | Publicado quando uma anomalia de custo é detetada. Consumido por dashboards financeiros e alertas operacionais. |

### AIKnowledge (3 eventos)

| Evento | Descrição |
|--------|-----------|
| `ExternalAIQueryRequestedEvent` | Publicado quando uma consulta a IA externa é solicitada. Permite rastreio de uso, custos e latência de LLM. |
| `ExternalAIResponseReceivedEvent` | Publicado quando a resposta de um provedor LLM é recebida. Consumido por Knowledge Capture e métricas de qualidade. |
| `KnowledgeCandidateCreatedEvent` | Publicado quando um candidato a conhecimento é identificado (ex.: padrão recorrente). Consumido pelo módulo de curadoria de conhecimento. |

### AuditCompliance (2 eventos)

| Evento | Descrição |
|--------|-----------|
| `AuditEventRecordedEvent` | Publicado quando um evento de auditoria é registado com sucesso. Pode ser consumido por sistemas SIEM externos. |
| `AuditIntegrityCheckpointCreatedEvent` | Publicado quando um checkpoint de integridade criptográfica é criado. Permite verificação distribuída da trilha de auditoria. |

**Total: 17 Integration Events** distribuídos por 6 dos 7 Bounded Contexts.

---

## Critérios de Extração

A extração de um módulo como serviço independente **não deve ser feita por
preferência técnica**. Deve ser justificada por necessidades concretas e
mensuráveis. Os critérios abaixo, ordenados por prioridade, orientam a decisão.

### 1. Escala de Equipa Requer Deploy Independente

Quando duas ou mais equipas precisam de fazer deploy de funcionalidades no mesmo
módulo com cadências diferentes, o acoplamento de deploy torna-se um bottleneck.
A extração permite ciclos de release independentes e ownership claro.

**Indicadores:**
- Conflitos frequentes de merge no mesmo módulo entre equipas distintas.
- Bloqueios de release porque uma equipa não está pronta para deploy.
- Necessidade de feature flags excessivos para isolar funcionalidades em progresso.

### 2. Escalabilidade Independente

Quando o volume de um workload específico cresce desproporcionalmente em relação
ao restante do sistema. O exemplo clássico é o Runtime Agent: ingestão de sinais
operacionais pode exigir dezenas de instâncias enquanto o monolith opera com duas.

**Indicadores:**
- CPU/memória do monolith dominado por um único módulo (> 60%).
- Latência do monolith degradada por carga de um módulo específico.
- Necessidade de escala para centenas de instâncias de um workload.

### 3. Requisitos Tecnológicos Distintos

Quando um módulo beneficiaria significativamente de uma tecnologia incompatível
com o monolith. O Runtime Stream Processor, por exemplo, beneficia de Kafka
Streams ou Apache Flink — tecnologias de stream processing que não fazem sentido
dentro de um monolith ASP.NET Core.

**Indicadores:**
- Workload de natureza fundamentalmente diferente (stream vs. request/response).
- Necessidade de base de dados especializada (time-series, graph, search engine).
- Requisitos de latência ultra-baixa incompatíveis com o overhead do monolith.

### 4. Isolamento Regulatório ou de Compliance

Quando auditores ou reguladores exigem separação física entre o sistema operacional
e o sistema de auditoria/compliance. O Audit Ledger é o candidato principal: a
prova de que registos de auditoria não foram adulterados é mais forte quando o
sistema de auditoria é fisicamente independente do sistema auditado.

**Indicadores:**
- Requisito regulatório explícito de separação física (SOC 2, ISO 27001, LGPD).
- Necessidade de demonstrar independência do sistema de auditoria em processos legais.
- Políticas de retenção de dados incompatíveis com o ciclo de vida do monolith.
- Requisito de acesso de auditores sem acesso ao sistema operacional.

---

## Passo a Passo da Extração

O processo abaixo aplica-se a qualquer um dos 5 serviços futuros planejados.
Cada passo é concebido para ser reversível e incremental.

### Passo 1 — Criar Novo ASP.NET Host

Criar um novo projeto `NexTraceOne.{Serviço}.Host` dentro de `src/platform/`.
Este Host compõe apenas a camada Infrastructure do módulo extraído, mantendo
as mesmas dependências de Building Blocks. Registar os mesmos services no DI.

```
src/platform/
├── NexTraceOne.ApiHost              ← monolith existente
├── NexTraceOne.BackgroundWorkers    ← jobs existentes
├── NexTraceOne.Ingestion.Api        ← ingestão existente
└── NexTraceOne.RuntimeAgent.Host    ← NOVO: serviço extraído
```

**Validação:** O novo Host compila e arranca sem erros. Health check responde OK.

### Passo 2 — Mover Implementações da Camada Infrastructure

Mover (não copiar) os adaptadores da camada Infrastructure do módulo extraído
para o novo Host. O monolith perde a implementação local das portas hexagonais
e ganha uma nova implementação: HTTP client que comunica com o novo serviço.

**Validação:** Testes de integração passam com o novo serviço. Testes unitários
do domínio continuam a passar inalterados (não dependem de infraestrutura).

### Passo 3 — Substituir InProcessEventBus por Message Bus

Substituir o `InProcessEventBus` por implementação de message bus (RabbitMQ ou
Kafka) para os Integration Events do módulo extraído. Os restantes módulos
continuam com `OutboxEventBus` + `InProcessEventBus` internamente.

**Configuração:**
- Publisher: novo serviço publica no message bus.
- Consumer: monolith consome do message bus.
- Fallback: manter `OutboxEventBus` como fallback durante transição.

**Validação:** Eventos publicados pelo novo serviço são consumidos pelo monolith.
Latência de entrega < 500ms em condições normais.

### Passo 4 — Adicionar Roteamento no API Gateway

Configurar o `Ingestion.Api` (ou reverse proxy externo como NGINX/Envoy) para
rotear requests destinados ao módulo extraído para o novo serviço. O monolith
deixa de receber esses requests diretamente.

**Regras de Roteamento:**
- Rotas do módulo extraído → novo serviço.
- Restantes rotas → monolith (`ApiHost`).
- Health checks → ambos (monolith + novo serviço).

**Validação:** Requests chegam ao serviço correto. Latência adicional do
roteamento < 5ms.

### Passo 5 — Separar Schema em Base de Dados Independente

Migrar o schema PostgreSQL do módulo extraído para uma base de dados dedicada.
O `DbContext` do novo serviço aponta para a nova connection string. O monolith
perde acesso direto àquele schema.

**Processo:**
1. Criar nova base de dados PostgreSQL.
2. Executar migrations do módulo no novo banco.
3. Migrar dados históricos (pg_dump/pg_restore do schema específico).
4. Atualizar connection string no novo serviço.
5. Remover schema do banco do monolith.

**Validação:** Dados acessíveis exclusivamente pelo novo serviço. Monolith sem
acesso ao schema removido. Queries de cross-module funcionam via Integration
Events (eventual consistency).

### Passo 6 — Atualizar Ingestion.Api e Monitorização

Atualizar o `Ingestion.Api` para rotear dados de ingestão para o novo serviço
quando aplicável. Atualizar dashboards de observabilidade (OpenTelemetry) para
incluir o novo serviço como componente independente.

**Checklist Final:**
- [ ] Health checks do novo serviço em monitoring.
- [ ] Traces distribuídos com correlation ID entre monolith e novo serviço.
- [ ] Métricas de latência, throughput e error rate do novo serviço.
- [ ] Alertas configurados para o novo serviço.
- [ ] Runbook operacional documentado.
- [ ] Rollback plan testado (reverter para implementação local no monolith).

---

## Diagrama de Dependências para Extração

```
┌─────────────────────────────────────────────────────────────┐
│                      API Gateway / Ingestion.Api            │
│                    (roteamento por path/header)              │
└──────┬──────────────┬───────────────┬───────────────┬───────┘
       │              │               │               │
       ▼              ▼               ▼               ▼
┌────────────┐ ┌────────────┐ ┌────────────┐ ┌────────────────┐
│  Monolith  │ │  Runtime   │ │    AI      │ │    Audit       │
│  (ApiHost) │ │   Agent    │ │  Gateway   │ │    Ledger      │
│            │ │            │ │            │ │                │
│ Identity   │ │ Signal     │ │ LLM Proxy  │ │ Append-Only    │
│ Catalog    │ │ Ingestion  │ │ Rate Limit │ │ Hash Chain     │
│ Change Gov │ │ Correlation│ │ Cache      │ │ Retention      │
│ Commercial │ │            │ │            │ │                │
└─────┬──────┘ └─────┬──────┘ └─────┬──────┘ └───────┬────────┘
      │               │              │                │
      └───────────────┴──────────────┴────────────────┘
                              │
                    ┌─────────▼──────────┐
                    │   Message Bus      │
                    │ (RabbitMQ / Kafka)  │
                    │                    │
                    │ Integration Events │
                    │ como contratos     │
                    └─────────┬──────────┘
                              │
                    ┌─────────▼──────────┐
                    │    PostgreSQL(s)    │
                    │  Schema por serviço │
                    └────────────────────┘
```

---

## Riscos e Mitigações

| Risco | Probabilidade | Impacto | Mitigação |
|-------|:------------:|:-------:|-----------|
| Consistência eventual entre serviços | Alta | Médio | Outbox Pattern + idempotência em consumers. Monitorizar lag do bus. |
| Complexidade operacional aumentada | Alta | Alto | Extrair apenas quando critérios forem claramente atingidos. Manter monolith como default. |
| Latência de rede entre serviços | Média | Médio | Colocar serviços na mesma rede/zona. gRPC para chamadas síncronas críticas. |
| Debugging distribuído | Média | Médio | OpenTelemetry com trace propagation. Correlation ID obrigatório em todos os eventos. |
| Migração de dados durante extração | Baixa | Alto | Migração offline com downtime planejado. Testes de integridade pós-migração. |

---

## Referências

- [ARCHITECTURE.md](./ARCHITECTURE.md) — Arquitetura completa do Archon Pattern v2.
- [CONVENTIONS.md](./CONVENTIONS.md) — Convenções de código, naming e documentação.
- [ROADMAP.md](./ROADMAP.md) — Estado atual e fases de desenvolvimento.
- [DOMAIN.md](./DOMAIN.md) — Taxonomia de mudanças e domínio de negócio.
- [SECURITY.md](./SECURITY.md) — Pilares de segurança, RLS, encryption e integrity.
