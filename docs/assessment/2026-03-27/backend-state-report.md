# Relatório de Estado do Backend — NexTraceOne

**Data:** 2026-03-27
**Âmbito:** Análise completa dos 12 módulos backend, building blocks, plataforma e estado de maturidade
**Classificação:** Documento interno de engenharia

---

## Índice

1. [Resumo Executivo](#1-resumo-executivo)
2. [Metodologia de Classificação](#2-metodologia-de-classificação)
3. [Módulo 1 — AIKnowledge (aik)](#3-módulo-1--aiknowledge-aik)
4. [Módulo 2 — AuditCompliance (aud)](#4-módulo-2--auditcompliance-aud)
5. [Módulo 3 — Catalog (cat)](#5-módulo-3--catalog-cat)
6. [Módulo 4 — ChangeGovernance (chg)](#6-módulo-4--changegovernance-chg)
7. [Módulo 5 — Configuration (cfg)](#7-módulo-5--configuration-cfg)
8. [Módulo 6 — Governance (gov)](#8-módulo-6--governance-gov)
9. [Módulo 7 — IdentityAccess (iam)](#9-módulo-7--identityaccess-iam)
10. [Módulo 8 — Integrations (int)](#10-módulo-8--integrations-int)
11. [Módulo 9 — Knowledge (knw)](#11-módulo-9--knowledge-knw)
12. [Módulo 10 — Notifications (ntf)](#12-módulo-10--notifications-ntf)
13. [Módulo 11 — OperationalIntelligence (ops)](#13-módulo-11--operationalintelligence-ops)
14. [Módulo 12 — ProductAnalytics (pan)](#14-módulo-12--productanalytics-pan)
15. [Building Blocks](#15-building-blocks)
16. [Plataforma](#16-plataforma)
17. [Análise Transversal — CancellationToken](#17-análise-transversal--cancellationtoken)
18. [Matriz de Maturidade Consolidada](#18-matriz-de-maturidade-consolidada)
19. [Recomendações Prioritárias](#19-recomendações-prioritárias)
20. [Conclusão](#20-conclusão)

---

## 1. Resumo Executivo

O backend do NexTraceOne é composto por **12 módulos de domínio**, **5 projetos de building blocks** e **3 projetos de plataforma**. A análise revela um ecossistema modular bem estruturado, seguindo os princípios de Clean Architecture e DDD estabelecidos na visão do produto.

### Classificação global

| Classificação | Módulos | Percentagem |
|---|---|---|
| **READY** | Catalog, Configuration, IdentityAccess, Notifications | 33% |
| **PARTIAL** | AIKnowledge, AuditCompliance, ChangeGovernance, Governance, OperationalIntelligence | 42% |
| **INCOMPLETE** | Integrations, Knowledge, ProductAnalytics | 25% |

### Métricas de alto nível

- **Total de ficheiros backend:** ~1.726 (módulos) + 115 (building blocks) + plataforma
- **Total de DbContexts:** 18 distribuídos por módulos
- **Total de métodos async sem CancellationToken:** 247
- **Módulo mais maduro:** IdentityAccess (185 ficheiros, ciclo de vida completo)
- **Módulo mais volumoso:** Catalog (310 ficheiros, 3 bounded contexts)
- **Módulo com maior défice de CancellationToken:** Notifications (46 métodos)

---

## 2. Metodologia de Classificação

Cada módulo é avaliado segundo cinco dimensões:

| Dimensão | Descrição |
|---|---|
| **Modelo de Domínio** | Riqueza das entidades, enums, value objects e regras de negócio |
| **Camada de Aplicação** | Handlers, validações, contratos de comando/query |
| **Infraestrutura** | Repositórios, DbContexts, migrações, adapters |
| **Exposição API** | Endpoints, contratos HTTP, documentação |
| **Operacionalidade** | Capacidade de funcionar em produção com dados reais |

### Escala de classificação

- **READY** — Funcional, testável, pronto para uso real com ajustes menores
- **PARTIAL** — Estrutura sólida, domínio modelado, mas requer completude em fluxos críticos ou dados reais
- **INCOMPLETE** — Estrutura inicial existe, mas faltam peças fundamentais para operação

---

## 3. Módulo 1 — AIKnowledge (aik)

### Visão geral

| Atributo | Valor |
|---|---|
| **Prefixo** | `aik` |
| **Ficheiros** | 279 |
| **DbContexts** | 3 (ExternalAI, Governance, Orchestration) |
| **Enums** | 34 |
| **Ficheiros de domínio** | 72 |
| **Ficheiros de aplicação** | 120 |
| **Classificação** | **PARTIAL** |

### Descrição funcional

O módulo AIKnowledge é responsável por toda a camada de inteligência artificial do NexTraceOne. Cobre o registo de modelos, políticas de acesso, governança de orçamento/tokens, runtime de agentes, execução de ferramentas e capacidades de streaming.

### Estrutura de bounded contexts

#### ExternalAI
- Gestão de modelos externos (Ollama, OpenAI)
- Configuração de providers e endpoints
- Mapeamento de capabilities por modelo

#### Governance
- Políticas de acesso a modelos por tenant, ambiente e persona
- Orçamentos e quotas de tokens
- Registo de auditoria de uso de IA
- Controlo de quais dados podem ser enviados para modelos externos

#### Orchestration
- Runtime de agentes especializados
- Registo e execução de ferramentas (tools)
- Gestão de sessões de conversação
- Streaming SSE para respostas em tempo real
- Grounding/RAG para contextualização de respostas

### Entidades e domínio relevantes

- Modelos de IA registados com metadata (provider, capabilities, limites)
- Políticas granulares por tenant, ambiente, papel e grupo
- Agentes com configurações específicas de modelo e ferramentas
- Sessões de conversação com histórico e contexto
- Resultados de execução de ferramentas com rastreabilidade

### Providers implementados

| Provider | Estado | Capacidades |
|---|---|---|
| **Ollama** | Funcional | Modelos locais, streaming, inferência |
| **OpenAI** | Funcional | Streaming SSE, completion, chat |

### Tool execution

O sistema de ferramentas segue o padrão `IToolRegistry` / `IToolExecutor`:

| Ferramenta | Descrição | Estado |
|---|---|---|
| `list_services` | Lista serviços do catálogo | Funcional |
| `get_service_health` | Obtém estado de saúde de um serviço | Funcional |
| `list_recent_changes` | Lista mudanças recentes | Funcional |

### Grounding/RAG

O sistema de grounding permite contextualizar respostas da IA com dados do NexTraceOne. Atualmente:

- **Implementado:** Pesquisa na base de dados de modelos de IA (`AIModels`)
- **Não implementado:** Pesquisa vetorial/semântica
- **Não implementado:** Indexação de documentação, contratos, runbooks ou knowledge base
- **Não implementado:** Integração com fontes de dados de telemetria ou incidentes

### Caminhos de evidência

```
src/Modules/AIKnowledge/
├── NexTraceOne.Modules.AIKnowledge.Domain/
│   ├── ExternalAI/          # Modelos, providers, capabilities
│   ├── Governance/          # Políticas, budgets, tokens
│   └── Orchestration/       # Agentes, tools, sessões
├── NexTraceOne.Modules.AIKnowledge.Application/
│   ├── ExternalAI/          # CRUD de modelos e providers
│   ├── Governance/          # Gestão de políticas e orçamentos
│   └── Orchestration/       # Runtime, streaming, grounding
└── NexTraceOne.Modules.AIKnowledge.Infrastructure/
    ├── ExternalAI/          # DbContext, repos, adapters
    ├── Governance/          # DbContext, repos
    └── Orchestration/       # DbContext, repos, provider adapters
```

### Problemas identificados

1. **43 métodos async sem CancellationToken** — risco de operações pendentes em cenários de cancelamento
2. **Grounding limitado** — pesquisa apenas na tabela `AIModels`, sem cobertura de contratos, serviços, mudanças ou documentação
3. **Sem pesquisa vetorial/semântica** — RAG depende de correspondência textual básica em base de dados
4. **Falta de ferramentas especializadas** — apenas 3 tools implementadas; faltam ferramentas para contratos, incidentes, runbooks, topologia
5. **Sem validação de saída** — respostas da IA não são filtradas por política de dados sensíveis

### Recomendações

1. **Prioridade alta:** Adicionar `CancellationToken` em todos os métodos async
2. **Prioridade alta:** Expandir grounding para cobrir serviços, contratos, mudanças e knowledge base
3. **Prioridade média:** Implementar mais ferramentas especializadas (contratos, incidentes, blast radius)
4. **Prioridade média:** Avaliar pesquisa vetorial com pgvector ou solução dedicada
5. **Prioridade baixa:** Implementar filtragem de dados sensíveis nas respostas

---

## 4. Módulo 2 — AuditCompliance (aud)

### Visão geral

| Atributo | Valor |
|---|---|
| **Prefixo** | `aud` |
| **Ficheiros** | 56 |
| **Entidades** | 6 |
| **Handlers** | 17 |
| **Repositórios** | 6 |
| **Endpoints** | 2 módulos |
| **Classificação** | **PARTIAL** |

### Descrição funcional

Responsável pela auditoria de ações sensíveis, integridade de cadeia de eventos, políticas de compliance e gestão de retenção. Fornece a espinha dorsal de rastreabilidade que atravessa todo o produto.

### Entidades principais

| Entidade | Função |
|---|---|
| `AuditEvent` | Registo unitário de ação auditável com contexto, actor, recurso e resultado |
| `AuditChainLink` | Elo de cadeia de integridade — garante que eventos não podem ser adulterados sem detecção |
| `AuditCampaign` | Campanha de auditoria formal com escopo, período e critérios de avaliação |
| `CompliancePolicy` | Política de compliance definida com regras, severidade e escopo de aplicação |
| `ComplianceResult` | Resultado de avaliação de uma política contra um recurso ou escopo |
| `RetentionPolicy` | Política de retenção de dados com período, escopo e ações de expiração |

### Integridade de cadeia

O mecanismo `AuditChainLink` implementa integridade encadeada tipo blockchain simplificado. Cada evento de auditoria é ligado ao anterior por hash, permitindo deteção de adulteração.

### Caminhos de evidência

```
src/Modules/AuditCompliance/
├── NexTraceOne.Modules.AuditCompliance.Domain/
│   ├── Entities/            # 6 entidades de domínio
│   └── Enums/               # Tipos de evento, severidades
├── NexTraceOne.Modules.AuditCompliance.Application/
│   ├── Commands/            # Criação de eventos, políticas, campanhas
│   └── Queries/             # Consulta de trilha de auditoria, compliance
└── NexTraceOne.Modules.AuditCompliance.Infrastructure/
    ├── Repositories/        # 6 repositórios
    └── Endpoints/           # 2 módulos de API
```

### Problemas identificados

1. **4 métodos async sem CancellationToken**
2. **Campanhas de compliance necessitam de workflow mais robusto** — criação e avaliação existem, mas falta ciclo de vida completo (agendamento, revisão, fecho, relatório)
3. **Falta integração nativa com scheduler** — campanhas periódicas dependem de trigger externo
4. **Relatórios de compliance básicos** — falta consolidação por domínio, equipa e ambiente

### Recomendações

1. **Prioridade média:** Completar ciclo de vida de campanhas de auditoria com agendamento via Quartz.NET
2. **Prioridade média:** Adicionar relatórios consolidados de compliance por tenant e ambiente
3. **Prioridade baixa:** Adicionar `CancellationToken` nos 4 métodos pendentes
4. **Prioridade baixa:** Implementar exportação de trilha de auditoria em formato regulatório

---

## 5. Módulo 3 — Catalog (cat)

### Visão geral

| Atributo | Valor |
|---|---|
| **Prefixo** | `cat` |
| **Ficheiros** | 310 |
| **DbContexts** | 3 (Graph, Contracts, Portal) |
| **Enums** | 25 |
| **Repositórios** | 22 implementações |
| **Endpoints** | 12 módulos de API |
| **Classificação** | **READY** |

### Descrição funcional

O módulo Catalog é o **coração do NexTraceOne como Source of Truth**. Alberga o catálogo de serviços, a topologia de dependências baseada em grafo, a governança de contratos (REST, SOAP, Event, Background Service) e o portal de desenvolvedor.

### Bounded contexts

#### Graph
- `ServiceAsset` — entidade central representando um serviço no catálogo
- `ApiAsset` — representação de API como nó do grafo
- `ConsumerAsset` — representação de consumidor
- `NodeHealth` — estado de saúde agregado por nó
- `GraphSnapshot` — snapshot pontual da topologia para análise histórica
- Dependências e relações entre nós do grafo

#### Contracts
- Contratos REST (OpenAPI)
- Contratos SOAP (WSDL)
- Contratos de Eventos (AsyncAPI/Kafka)
- Contratos de Background Services
- Versionamento e compatibilidade
- Diff semântico entre versões
- Publicação e workflow de aprovação
- Políticas e linting (rulesets spectrais)

#### Portal
- Portal de desenvolvedor
- Subscrições a APIs
- Playground para teste de contratos
- Documentação viva por contrato

### Contract Studio

O Contract Studio permite criação e edição assistida de contratos:

| Capacidade | Estado |
|---|---|
| `DraftStudio` — edição draft | Funcional |
| Spectral rulesets — linting | Funcional |
| Review workflow — aprovação | Funcional |
| Importação de OpenAPI/WSDL | Funcional |
| Diff semântico | Funcional |
| Geração assistida por IA | Parcial (depende do módulo AIKnowledge) |

### Source of Truth — Pesquisa global

O catálogo implementa pesquisa global usando **PostgreSQL Full-Text Search**, permitindo localizar serviços, contratos, APIs e dependências a partir de termos livres.

### Caminhos de evidência

```
src/Modules/Catalog/
├── NexTraceOne.Modules.Catalog.Domain/
│   ├── Graph/               # ServiceAsset, ApiAsset, ConsumerAsset, NodeHealth, GraphSnapshot
│   ├── Contracts/           # REST, SOAP, Event, Background contracts, versions, diffs
│   └── Portal/              # Subscriptions, playground
├── NexTraceOne.Modules.Catalog.Application/
│   ├── Graph/               # Gestão de topologia, snapshots, saúde
│   ├── Contracts/           # CRUD, versionamento, linting, publicação
│   ├── Portal/              # Subscrições, playground
│   └── Search/              # Pesquisa FTS global
└── NexTraceOne.Modules.Catalog.Infrastructure/
    ├── Graph/               # DbContext, repos, adapters de grafo
    ├── Contracts/           # DbContext, repos, spectral integration
    └── Portal/              # DbContext, repos
```

### Problemas identificados

1. **26 métodos async sem CancellationToken** — significativo dado o volume de operações
2. **Snapshots de grafo podem crescer rapidamente** — necessária política de retenção
3. **Diff semântico é computacionalmente pesado** — pode necessitar de cache ou processamento assíncrono
4. **Portal de desenvolvedor depende de contratos publicados** — playground sem contrato é limitado

### Recomendações

1. **Prioridade alta:** Adicionar `CancellationToken` nos 26 métodos pendentes
2. **Prioridade média:** Implementar política de retenção para `GraphSnapshot`
3. **Prioridade média:** Considerar cache de resultados de diff semântico
4. **Prioridade baixa:** Melhorar pesquisa FTS com pesos por tipo de recurso

---

## 6. Módulo 4 — ChangeGovernance (chg)

### Visão geral

| Atributo | Valor |
|---|---|
| **Prefixo** | `chg` |
| **Ficheiros** | 238 |
| **DbContexts** | 4 (ChangeIntelligence, Workflow, RulesetGovernance, Promotion) |
| **Enums** | 12 |
| **Repositórios** | 22+ |
| **Endpoints** | 19 módulos de API |
| **Classificação** | **PARTIAL** |

### Descrição funcional

O módulo de Change Governance é fundamental para o pilar de **Change Intelligence & Production Change Confidence**. Gere o ciclo de vida completo de mudanças, desde a ingestão de eventos de deploy até à validação pós-change e decisão de rollback.

### Bounded contexts

#### ChangeIntelligence
- `ChangeEvent` — evento de mudança ingerido de fontes externas
- `ChangeScore` — scoring de confiança baseado em múltiplas dimensões
- `BlastRadius` — análise de impacto potencial de uma mudança
- `EvidencePack` — pacote de evidências associadas a uma mudança
- `RollbackAssessment` — avaliação de viabilidade de rollback

#### Workflow
- `WorkflowInstance` — instância de workflow de aprovação
- Definições de passos, aprovadores e condições
- Histórico de execução

#### RulesetGovernance
- Regras e políticas que governam mudanças
- Critérios de aprovação por ambiente e tipo de mudança
- Validações automatizadas

#### Promotion
- `PromotionRequest` — pedido de promoção entre ambientes
- `FreezeWindow` — janelas de congelamento de mudanças
- `Release` — identidade de release com metadados
- Gates de promoção por ambiente
- Calendário de releases

### Entidades centrais

| Entidade | Função |
|---|---|
| `Release` | Identidade de release com versão, serviço, ambiente e metadados |
| `ChangeEvent` | Evento de mudança com origem, tipo, escopo e timestamp |
| `ChangeScore` | Score multidimensional de confiança (risco, impacto, validação) |
| `BlastRadius` | Mapa de impacto: serviços, contratos e consumidores potencialmente afetados |
| `EvidencePack` | Colecção de evidências: testes, aprovações, análises, screenshots |
| `RollbackAssessment` | Avaliação: é possível reverter? Qual o custo? Quais os riscos? |
| `PromotionRequest` | Pedido formal de promoção de ambiente não produtivo para produção |
| `WorkflowInstance` | Execução concreta de workflow de aprovação com estado e histórico |
| `FreezeWindow` | Janela temporal onde mudanças são bloqueadas ou restritas |

### Caminhos de evidência

```
src/Modules/ChangeGovernance/
├── NexTraceOne.Modules.ChangeGovernance.Domain/
│   ├── ChangeIntelligence/  # ChangeEvent, ChangeScore, BlastRadius, EvidencePack, Rollback
│   ├── Workflow/            # WorkflowInstance, steps, aprovadores
│   ├── RulesetGovernance/   # Regras, políticas, validações
│   └── Promotion/           # PromotionRequest, FreezeWindow, Release
├── NexTraceOne.Modules.ChangeGovernance.Application/
│   ├── ChangeIntelligence/  # Scoring, blast radius, evidence management
│   ├── Workflow/            # Gestão de workflows
│   ├── RulesetGovernance/   # Avaliação de regras
│   └── Promotion/           # Promoção, freeze, calendário
└── NexTraceOne.Modules.ChangeGovernance.Infrastructure/
    ├── ChangeIntelligence/  # DbContext, repos
    ├── Workflow/            # DbContext, repos
    ├── RulesetGovernance/   # DbContext, repos
    └── Promotion/           # DbContext, repos
```

### Problemas identificados

1. **11 métodos async sem CancellationToken**
2. **Scoring de confiança depende de dados de telemetria** — sem ingestão real de métricas, o score é baseado apenas em metadados estáticos
3. **Blast radius limitado sem topologia em tempo real** — depende de `GraphSnapshot` do Catalog
4. **Rollback assessment conceptual** — avaliação existe como estrutura, mas necessita de validação com dados operacionais reais
5. **Falta correlação change-to-incident** — estrutura preparada, mas sem pipeline de dados implementado

### Recomendações

1. **Prioridade alta:** Implementar pipeline de ingestão de dados de deploy para alimentar scoring real
2. **Prioridade alta:** Integrar blast radius com topologia do Catalog (GraphSnapshot)
3. **Prioridade média:** Implementar correlação change-to-incident com módulo OperationalIntelligence
4. **Prioridade média:** Adicionar `CancellationToken` nos 11 métodos pendentes
5. **Prioridade média:** Completar validação pós-change com dados de telemetria

---

## 7. Módulo 5 — Configuration (cfg)

### Visão geral

| Atributo | Valor |
|---|---|
| **Prefixo** | `cfg` |
| **Ficheiros** | 57 |
| **Entidades** | 6 |
| **Handlers** | 11 |
| **Repositórios** | 8 |
| **Classificação** | **READY** |

### Descrição funcional

Módulo de gestão centralizada de configuração com resolução hierárquica e feature flags. Permite parametrização sem redeploy, alinhado com a secção 33 das instruções do produto.

### Resolução hierárquica

O sistema implementa resolução de configuração com precedência clara:

```
Instance → Tenant → Environment → Module
```

Isto significa que:
1. Configuração ao nível da **instância** é o valor por defeito global
2. **Tenant** sobrepõe a instância para o inquilino específico
3. **Ambiente** sobrepõe o tenant para o ambiente específico (Dev, Pre-Prod, Prod)
4. **Módulo** sobrepõe o ambiente para o módulo específico

### Entidades principais

| Entidade | Função |
|---|---|
| Configuração base | Par chave-valor com escopo hierárquico |
| Feature flag definition | Definição de flag com nome, descrição e estado por defeito |
| Feature flag override | Sobreposição de flag por tenant, ambiente ou módulo |
| Configuração de auditoria | Trilha de alterações de configuração |

### Caminhos de evidência

```
src/Modules/Configuration/
├── NexTraceOne.Modules.Configuration.Domain/
│   └── Entities/            # 6 entidades de configuração
├── NexTraceOne.Modules.Configuration.Application/
│   ├── Commands/            # Criação e atualização de configuração
│   └── Queries/             # Resolução hierárquica, listagem
└── NexTraceOne.Modules.Configuration.Infrastructure/
    └── Repositories/        # 8 repositórios
```

### Problemas identificados

1. **19 métodos async sem CancellationToken** — proporcionalmente elevado para um módulo de 57 ficheiros
2. **Falta cache de resolução** — cada consulta pode percorrer toda a hierarquia

### Recomendações

1. **Prioridade média:** Adicionar `CancellationToken` nos 19 métodos pendentes
2. **Prioridade média:** Implementar cache de resolução hierárquica com invalidação por escopo
3. **Prioridade baixa:** Considerar notificação de alteração de configuração via eventos de domínio

---

## 8. Módulo 6 — Governance (gov)

### Visão geral

| Atributo | Valor |
|---|---|
| **Prefixo** | `gov` |
| **Ficheiros** | 138 |
| **Entidades** | 9 |
| **Handlers** | 56 |
| **Endpoints** | 17 módulos |
| **Enums** | 36 |
| **Classificação** | **PARTIAL** |

### Descrição funcional

O módulo Governance é responsável pela governança organizacional: equipas, domínios, packs de governança, waivers, administração delegada e relatórios executivos. Fornece a visão consolidada para personas como Executive, Architect e Auditor.

### Entidades principais

| Entidade | Função |
|---|---|
| Team | Equipa com membros, ownership e escopo |
| Domain | Domínio organizacional de negócio |
| GovernancePack | Pacote de regras, políticas e critérios de governança |
| Waiver | Exceção temporária a uma política com justificação e expiração |
| DelegatedAdministration | Delegação de capacidades administrativas com escopo e tempo |
| Relatórios | Executive overview, compliance, risk, FinOps, benchmarking, maturity scorecards |

### Relatórios disponíveis

| Relatório | Estado | Observação |
|---|---|---|
| Executive overview | Funcional | Computa a partir de dados disponíveis |
| Compliance | Funcional | Depende de políticas definidas no AuditCompliance |
| Risk | Funcional | Baseado em dados estruturais, não em telemetria |
| FinOps | Parcial | Necessita dados reais de custo |
| Benchmarking | Parcial | Comparação entre equipas/domínios com dados limitados |
| Maturity scorecards | Funcional | Pontuação multidimensional por equipa e domínio |

### Enums (36 no total)

Cobrem dimensões de:
- Compliance (tipos, severidades, estados)
- Risco (categorias, impacto, probabilidade)
- Maturidade (níveis, dimensões, critérios)
- Custo (tipos, fontes, categorias de desperdício)

### Caminhos de evidência

```
src/Modules/Governance/
├── NexTraceOne.Modules.Governance.Domain/
│   ├── Entities/            # 9 entidades
│   └── Enums/               # 36 enums
├── NexTraceOne.Modules.Governance.Application/
│   ├── Commands/            # Gestão de equipas, domínios, packs, waivers
│   ├── Queries/             # Relatórios, listagens, scorecards
│   └── Reports/             # Lógica de computação de relatórios
└── NexTraceOne.Modules.Governance.Infrastructure/
    ├── Repositories/        # Repos
    └── Endpoints/           # 17 módulos de API
```

### Problemas identificados

1. **9 métodos async sem CancellationToken**
2. **FinOps depende de dados reais de custo** — sem integração com fontes de custo cloud/infra
3. **Relatórios de risco baseados em dados estruturais** — sem correlação com telemetria ou incidentes reais
4. **56 handlers é um número elevado** — avaliar se há duplicação ou possibilidade de consolidação

### Recomendações

1. **Prioridade alta:** Implementar ingestão de dados de custo para FinOps contextual
2. **Prioridade média:** Enriquecer relatórios de risco com dados do OperationalIntelligence
3. **Prioridade média:** Adicionar `CancellationToken` nos 9 métodos pendentes
4. **Prioridade baixa:** Rever handlers para identificar possíveis consolidações

---

## 9. Módulo 7 — IdentityAccess (iam)

### Visão geral

| Atributo | Valor |
|---|---|
| **Prefixo** | `iam` |
| **Ficheiros** | 185 |
| **Entidades** | 21 |
| **Handlers** | 40 |
| **Endpoints** | 14 módulos |
| **Repositórios** | 27 |
| **Classificação** | **READY** |

### Descrição funcional

O módulo mais maduro do sistema. Gere o ciclo de vida completo de identidade: utilizadores, papéis, permissões, sessões, tenants, ambientes, e mecanismos avançados de segurança como Break Glass, JIT Access e delegações.

### Capacidades implementadas

| Capacidade | Estado |
|---|---|
| Gestão de utilizadores | ✅ Completa |
| Gestão de papéis e permissões | ✅ Completa |
| Gestão de sessões | ✅ Completa |
| Multi-tenancy | ✅ Completa |
| Gestão de ambientes | ✅ Completa |
| Break Glass Access Protocol | ✅ Completa |
| Just-In-Time Access | ✅ Completa |
| Delegações com expiração | ✅ Completa |
| Access Reviews | ✅ Completa |
| SSO/OIDC | ✅ Completa |
| Eventos de segurança | ✅ Completa |
| MFA | ✅ Completa |

### Entidades (21)

Incluem utilizadores, papéis, permissões, sessões, tokens, tenants, ambientes, organizações, convites, políticas de password, configurações de SSO, logs de segurança, Break Glass requests, JIT requests, delegações, access reviews, entre outros.

### Caminhos de evidência

```
src/Modules/IdentityAccess/
├── NexTraceOne.Modules.IdentityAccess.Domain/
│   ├── Entities/            # 21 entidades
│   ├── ValueObjects/        # IDs tipados, políticas
│   └── Events/              # Eventos de domínio de segurança
├── NexTraceOne.Modules.IdentityAccess.Application/
│   ├── Commands/            # Gestão completa de identidade
│   ├── Queries/             # Consultas de utilizadores, papéis, sessões
│   └── Behaviors/           # Validações específicas de identidade
└── NexTraceOne.Modules.IdentityAccess.Infrastructure/
    ├── Repositories/        # 27 repositórios
    ├── Endpoints/           # 14 módulos de API
    └── Security/            # JWT, OIDC, hashing, MFA
```

### Problemas identificados

1. **24 métodos async sem CancellationToken** — relevante dado que operações de identidade podem ter latência elevada (SSO, OIDC)
2. **Expiração de Break Glass, JIT e delegações depende de jobs background** — implementados no BackgroundWorkers

### Recomendações

1. **Prioridade alta:** Adicionar `CancellationToken` nos 24 métodos pendentes — especialmente nos fluxos SSO/OIDC
2. **Prioridade baixa:** Considerar rate limiting específico para endpoints de autenticação (para além do rate limiting global do ApiHost)

---

## 10. Módulo 8 — Integrations (int)

### Visão geral

| Atributo | Valor |
|---|---|
| **Prefixo** | `int` |
| **Ficheiros** | 32 |
| **Entidades** | 3 |
| **Handlers** | 8 |
| **Repositórios** | 3 |
| **Classificação** | **INCOMPLETE** |

### Descrição funcional

O módulo de Integrações define a estrutura para ligação do NexTraceOne a fontes externas de dados e serviços. Atualmente fornece apenas a espinha dorsal (entidades, handlers, repos), sem adapters concretos para qualquer provider.

### Entidades

| Entidade | Função |
|---|---|
| `IntegrationConnector` | Configuração de uma integração: tipo, endpoint, credenciais, estado |
| `IngestionSource` | Fonte de ingestão de dados com mapeamento e frequência |
| `IngestionExecution` | Registo de execução de ingestão com resultado, duração e erros |

### Adapters ausentes

| Provider | Estado | Impacto |
|---|---|---|
| GitLab | ❌ Não implementado | Deploy events, pipeline data |
| Jenkins | ❌ Não implementado | Build/deploy events |
| GitHub | ❌ Não implementado | Commits, PRs, deploy events |
| Azure DevOps | ❌ Não implementado | Pipeline, releases, work items |
| Fontes de telemetria | ❌ Não implementado | Métricas, logs, traces |
| Identity providers | ❌ Não implementado | Sincronização de utilizadores/grupos |

### Caminhos de evidência

```
src/Modules/Integrations/
├── NexTraceOne.Modules.Integrations.Domain/
│   └── Entities/            # 3 entidades base
├── NexTraceOne.Modules.Integrations.Application/
│   ├── Commands/            # CRUD de conectores e fontes
│   └── Queries/             # Listagem, estado de execuções
└── NexTraceOne.Modules.Integrations.Infrastructure/
    └── Repositories/        # 3 repositórios base
```

### Problemas identificados

1. **9 métodos async sem CancellationToken**
2. **Nenhum adapter real implementado** — módulo puramente estrutural
3. **Sem gestão de credenciais segura** — credenciais de integração precisam de encriptação
4. **Sem health check de conectores** — impossível verificar estado de uma integração
5. **Sem retry/circuit breaker** — ingestão não tem resiliência

### Recomendações

1. **Prioridade crítica:** Implementar pelo menos um adapter real (GitHub ou GitLab) para validar o modelo
2. **Prioridade alta:** Implementar gestão segura de credenciais de integração (usar `EncryptedStringConverter` dos building blocks)
3. **Prioridade alta:** Adicionar health check e retry para execuções de ingestão
4. **Prioridade média:** Implementar adapter para fontes de deploy events (alimenta ChangeGovernance)
5. **Prioridade média:** Adicionar `CancellationToken` nos 9 métodos pendentes

---

## 11. Módulo 9 — Knowledge (knw)

### Visão geral

| Atributo | Valor |
|---|---|
| **Prefixo** | `knw` |
| **Ficheiros** | 30 |
| **Entidades** | 3 |
| **Handlers** | 5 |
| **Repositórios** | 3 |
| **Classificação** | **INCOMPLETE** |

### Descrição funcional

O módulo Knowledge materializa o pilar de **Source of Truth & Operational Knowledge**. Permite documentação técnica e operacional governada, notas operacionais e relações entre conhecimento e entidades do sistema.

### Entidades

| Entidade | Função |
|---|---|
| `KnowledgeDocument` | Documento de conhecimento com título, corpo, tipo, tags e ownership |
| `OperationalNote` | Nota operacional associada a um contexto (serviço, incidente, mudança) |
| `KnowledgeRelation` | Relação tipada entre documento e entidade do sistema |

### Relações suportadas

`KnowledgeRelation` pode ligar documentos a:
- Serviços (via ServiceId)
- Contratos (via ContractId)
- Mudanças (via ChangeId)
- Incidentes (via IncidentId)

### Caminhos de evidência

```
src/Modules/Knowledge/
├── NexTraceOne.Modules.Knowledge.Domain/
│   └── Entities/            # 3 entidades
├── NexTraceOne.Modules.Knowledge.Application/
│   ├── Commands/            # CRUD de documentos, notas, relações
│   └── Queries/             # Listagem, consulta
└── NexTraceOne.Modules.Knowledge.Infrastructure/
    └── Repositories/        # 3 repositórios
```

### Problemas identificados

1. **2 métodos async sem CancellationToken**
2. **Sem migração EF** — entidades definidas mas sem schema materializado na base de dados
3. **Sem pesquisa FTS** — não é possível pesquisar documentos por texto livre
4. **Módulo recentemente criado (P10)** — funcionalidade CRUD básica apenas
5. **Sem versionamento de documentos** — alterações sobrescrevem sem histórico
6. **Sem integração com grounding de IA** — documentos não alimentam o RAG do AIKnowledge

### Recomendações

1. **Prioridade crítica:** Criar migração EF para materializar o schema
2. **Prioridade alta:** Implementar pesquisa FTS com PostgreSQL para documentos e notas
3. **Prioridade alta:** Integrar com grounding do AIKnowledge — documentos devem ser fonte de RAG
4. **Prioridade média:** Implementar versionamento de documentos
5. **Prioridade média:** Adicionar `CancellationToken` nos 2 métodos pendentes
6. **Prioridade baixa:** Considerar suporte a Markdown rendering e templates

---

## 12. Módulo 10 — Notifications (ntf)

### Visão geral

| Atributo | Valor |
|---|---|
| **Prefixo** | `ntf` |
| **Ficheiros** | 124 |
| **Entidades** | 6 |
| **Handlers** | 16 |
| **Interfaces de repositório** | 22 |
| **Implementações de repositório** | 6 |
| **Classificação** | **READY** |

### Descrição funcional

Módulo completo de notificações cobrindo todo o ciclo: templates, preferências de utilizador, canais de entrega (SMTP, in-app), horas silenciosas e despacho automático via eventos de integração do sistema.

### Entidades principais

| Entidade | Função |
|---|---|
| Template de notificação | Template com variáveis, canal e tipo |
| Preferências de utilizador | Configuração de canais preferidos, frequência e opt-out |
| Entrega de notificação | Registo de entrega com estado, tentativas e resultado |
| Canal de notificação | Definição de canal (SMTP, in-app, webhook futuro) |
| Horas silenciosas | Configuração de períodos sem notificações |
| Histórico | Registo completo de notificações enviadas |

### Integration Event Handlers

O módulo destaca-se pela quantidade de **40+ integration event handlers** que escutam eventos de todo o sistema:

- Eventos de identidade (login, falha, MFA, convite)
- Eventos de catálogo (novo serviço, contrato publicado, breaking change)
- Eventos de mudança (nova release, promoção, freeze window)
- Eventos de incidente (novo incidente, escalação, resolução)
- Eventos de IA (execução de agente, quota atingida)
- Eventos de governança (waiver, compliance, risco)
- Eventos de auditoria (eventos sensíveis)

### Caminhos de evidência

```
src/Modules/Notifications/
├── NexTraceOne.Modules.Notifications.Domain/
│   └── Entities/            # 6 entidades
├── NexTraceOne.Modules.Notifications.Application/
│   ├── Commands/            # Envio, gestão de templates e preferências
│   ├── Queries/             # Listagem, histórico
│   └── IntegrationEvents/   # 40+ handlers de eventos do sistema
└── NexTraceOne.Modules.Notifications.Infrastructure/
    ├── Repositories/        # 6 implementações (22 interfaces)
    ├── Channels/            # SMTP adapter
    └── Endpoints/           # API de notificações
```

### Problemas identificados

1. **46 métodos async sem CancellationToken** — **o pior de todos os módulos**
2. **Discrepância entre interfaces (22) e implementações (6)** — possível dívida técnica ou interfaces futuras
3. **Apenas SMTP como canal de entrega externo** — falta webhook, Slack, Teams
4. **40+ event handlers podem criar carga significativa** — avaliar processamento em background

### Recomendações

1. **Prioridade alta:** Adicionar `CancellationToken` em **todos** os 46 métodos — prioridade máxima neste módulo
2. **Prioridade alta:** Rever a discrepância entre 22 interfaces e 6 implementações — remover interfaces órfãs ou implementar
3. **Prioridade média:** Mover processamento de event handlers para background job quando o volume justificar
4. **Prioridade baixa:** Adicionar canais de entrega adicionais (webhook, Slack)

---

## 13. Módulo 11 — OperationalIntelligence (ops)

### Visão geral

| Atributo | Valor |
|---|---|
| **Prefixo** | `ops` |
| **Ficheiros** | 254 |
| **DbContexts** | 5 (Runtime, Cost, Automation, Incidents, Reliability) |
| **Enums** | 25 |
| **Repositórios** | 15 |
| **Classificação** | **PARTIAL** |

### Descrição funcional

O módulo mais ambicioso em termos de scope. Cobre observabilidade operacional, SLO/SLA, gestão de incidentes, automação, custo e confiabilidade. É o motor de inteligência operacional do NexTraceOne.

### Bounded contexts

#### Runtime
- Métricas de runtime de serviços
- Estado operacional em tempo real
- Telemetria contextualizada

#### Cost
- Atribuição de custo por serviço, equipa e ambiente
- Deteção de desperdício
- Drift findings (desvios de custo)

#### Automation
- Workflows de automação operacional
- Aprovações e pré-condições
- Execução automatizada de ações

#### Incidents
- Incidentes com severidade, impacto e escopo
- Mitigação e resolução
- Correlação com mudanças e serviços
- Post-mortem

#### Reliability
- Definições de SLO/SLA
- Burn rate e error budgets
- Health scoring por serviço e equipa

### Capacidades modeladas

| Capacidade | Estado | Dependência |
|---|---|---|
| SLO/SLA definitions | Estrutura pronta | Necessita dados de telemetria |
| Burn rate | Estrutura pronta | Necessita métricas contínuas |
| Error budgets | Estrutura pronta | Necessita dados de telemetria |
| Cost attribution | Estrutura pronta | Necessita dados reais de custo |
| Drift findings | Estrutura pronta | Necessita baseline de custo |
| Incidents | Funcional | CRUD completo |
| Mitigation | Funcional | Ligado a incidentes |
| Correlation | Estrutura pronta | Necessita pipeline de dados |
| Automation workflows | Estrutura pronta | Necessita executors |

### Caminhos de evidência

```
src/Modules/OperationalIntelligence/
├── NexTraceOne.Modules.OperationalIntelligence.Domain/
│   ├── Runtime/             # Métricas, telemetria
│   ├── Cost/                # Atribuição, desperdício, drift
│   ├── Automation/          # Workflows, aprovações
│   ├── Incidents/           # Incidentes, mitigação, correlação
│   └── Reliability/         # SLO, SLA, burn rate, error budget
├── NexTraceOne.Modules.OperationalIntelligence.Application/
│   ├── Runtime/             # Handlers de métricas
│   ├── Cost/                # Handlers de custo
│   ├── Automation/          # Handlers de automação
│   ├── Incidents/           # Handlers de incidentes
│   └── Reliability/         # Handlers de SLO/SLA
└── NexTraceOne.Modules.OperationalIntelligence.Infrastructure/
    ├── Runtime/             # DbContext, repos
    ├── Cost/                # DbContext, repos
    ├── Automation/          # DbContext, repos
    ├── Incidents/           # DbContext, repos
    └── Reliability/         # DbContext, repos
```

### Problemas identificados

1. **36 métodos async sem CancellationToken** — segundo pior módulo
2. **Modelo riquíssimo mas sem pipeline de dados real** — maioria das capacidades depende de ingestão externa que não existe
3. **SLO/SLA sem dados de telemetria** — burn rate e error budget são calculáveis apenas com métricas reais
4. **Custo sem integração** — atribuição de custo é conceptual sem dados de cloud/infra
5. **5 DbContexts podem criar complexidade de gestão** — avaliar se todos são necessários

### Recomendações

1. **Prioridade crítica:** Implementar pipeline de ingestão de telemetria (métricas mínimas para SLO)
2. **Prioridade alta:** Adicionar `CancellationToken` nos 36 métodos pendentes
3. **Prioridade alta:** Implementar ingestão de dados de incidentes de fontes externas
4. **Prioridade média:** Implementar correlação incidente-mudança com ChangeGovernance
5. **Prioridade média:** Avaliar consolidação de DbContexts se dois ou mais partilham forte coesão
6. **Prioridade baixa:** Implementar ingestão de dados de custo para FinOps real

---

## 14. Módulo 12 — ProductAnalytics (pan)

### Visão geral

| Atributo | Valor |
|---|---|
| **Prefixo** | `pan` |
| **Ficheiros** | 23 |
| **Entidades** | 1 (`AnalyticsEvent`) |
| **Handlers** | 7 |
| **Enums** | 6 |
| **Classificação** | **INCOMPLETE** |

### Descrição funcional

Módulo interno para análise de uso do próprio produto NexTraceOne. Regista eventos de interação, uso por persona, jornadas de utilizador e adoção por módulo.

### Entidade única

`AnalyticsEvent` — registo de evento de analytics com:
- Tipo de evento
- Persona do utilizador
- Módulo envolvido
- Contexto (serviço, ambiente, etc.)
- Timestamp
- Metadados adicionais

### Enums (6)

- Tipos de eventos de analytics
- Categorias de persona
- Fases de jornada
- Métricas de adoção
- Fontes de evento
- Classificações de engagement

### Caminhos de evidência

```
src/Modules/ProductAnalytics/
├── NexTraceOne.Modules.ProductAnalytics.Domain/
│   ├── Entities/            # AnalyticsEvent
│   └── Enums/               # 6 enums
├── NexTraceOne.Modules.ProductAnalytics.Application/
│   ├── Commands/            # Registo de eventos
│   └── Queries/             # Consultas de analytics
└── NexTraceOne.Modules.ProductAnalytics.Infrastructure/
    └── Repositories/        # Repos
```

### Problemas identificados

1. **8 métodos async sem CancellationToken**
2. **Queries analíticas retornam dados mínimos** — sem agregações ou funnels reais
3. **Uma única entidade é limitante** — falta separação entre eventos brutos e métricas agregadas
4. **Sem dashboard ou visualização** — dados registados mas não apresentados
5. **Sem retenção ou rollup** — eventos brutos podem crescer indefinidamente

### Recomendações

1. **Prioridade média:** Implementar queries de agregação (uso por módulo, por persona, por período)
2. **Prioridade média:** Definir política de retenção/rollup para eventos brutos
3. **Prioridade baixa:** Adicionar `CancellationToken` nos 8 métodos pendentes
4. **Prioridade baixa:** Considerar separação entre eventos brutos e métricas pré-computadas
5. **Prioridade baixa:** Este módulo é secundário face aos pilares principais do produto

---

## 15. Building Blocks

Os building blocks são a fundação transversal que sustenta todos os módulos. Distribuídos por 5 projetos com 115 ficheiros no total.

### 15.1 Core (16 ficheiros)

Fundação do modelo de domínio DDD:

| Componente | Função |
|---|---|
| `AggregateRoot` | Base para agregados com gestão de eventos de domínio |
| `Entity` | Base para entidades com identidade |
| `ValueObject` | Base para value objects imutáveis |
| `Result<T>` | Tipo monádico para falhas controladas sem excepções |
| `Guards` | Guard clauses para validação de pré-condições |
| `DomainEvent` | Base para eventos de domínio |
| `StronglyTypedId` | IDs tipados para evitar confusão entre identificadores |

**Avaliação:** Sólido e alinhado com as boas práticas DDD definidas na visão do produto. O `Result<T>` é particularmente importante para a regra de não usar excepções para fluxos de controlo.

### 15.2 Application (31 ficheiros)

Infraestrutura da camada de aplicação:

| Componente | Função |
|---|---|
| CQRS | Separação comando/query com MediatR |
| Behaviors | Pipeline behaviors (validação, logging, transação) |
| TenantIsolation | Isolamento automático de dados por tenant |
| Validation | Integração com FluentValidation |
| Logging | Logging estruturado com correlação |
| Correlation | CorrelationId para rastreabilidade cross-module |

**Avaliação:** Completo e consistente. O `TenantIsolation` behavior é especialmente relevante para o contexto enterprise multi-tenant.

### 15.3 Infrastructure (12 ficheiros)

Suporte de infraestrutura:

| Componente | Função |
|---|---|
| `DbContextBase` | Base para todos os DbContexts dos módulos |
| Outbox | Outbox pattern para eventos de domínio confiáveis |
| TenantRLS | Row Level Security automático por tenant no PostgreSQL |
| `AuditInterceptor` | Interceptor EF Core para auditoria automática de alterações |
| `EncryptedStringConverter` | Converter para campos encriptados em base de dados |

**Avaliação:** Bem desenhado. O `TenantRLS` e `AuditInterceptor` são essenciais para os requisitos de segurança e auditoria.

### 15.4 Observability (39 ficheiros)

Camada de observabilidade:

| Componente | Função |
|---|---|
| Alerting | Definições de alertas e regras |
| Analytics/ClickHouse | Preparação para stack analítica com ClickHouse |
| HealthChecks | Health checks configuráveis por módulo |
| Telemetry | OpenTelemetry integration |
| Metrics | Métricas de aplicação |

**Avaliação:** A preparação para ClickHouse é positiva e alinhada com a direção arquitetural documentada. O volume de 39 ficheiros sugere investimento significativo nesta área.

### 15.5 Security (17 ficheiros)

Segurança transversal:

| Componente | Função |
|---|---|
| JWT | Geração e validação de JWT tokens |
| ApiKey | Autenticação por API Key |
| CSRF | Proteção contra CSRF |
| AES-GCM | Encriptação simétrica forte |
| TenantResolution | Resolução do tenant corrente |
| PermissionAuthorization | Autorização baseada em permissões |

**Avaliação:** Cobertura adequada. A combinação JWT + ApiKey + CSRF fornece múltiplas opções de autenticação. O AES-GCM é uma escolha moderna e segura.

### Caminhos de evidência

```
src/BuildingBlocks/
├── NexTraceOne.BuildingBlocks.Core/             # 16 ficheiros — DDD foundations
├── NexTraceOne.BuildingBlocks.Application/      # 31 ficheiros — CQRS, behaviors, tenant
├── NexTraceOne.BuildingBlocks.Infrastructure/   # 12 ficheiros — EF, outbox, RLS, audit
├── NexTraceOne.BuildingBlocks.Observability/    # 39 ficheiros — alerting, ClickHouse, telemetry
└── NexTraceOne.BuildingBlocks.Security/         # 17 ficheiros — JWT, ApiKey, CSRF, AES-GCM
```

---

## 16. Plataforma

Os projetos de plataforma são os entry points do sistema.

### 16.1 ApiHost

**Função:** Composition root do backend. Ponto de entrada HTTP principal.

| Capacidade | Detalhe |
|---|---|
| Rate limiting | 6 políticas configuradas (global, por tenant, por IP, por API key, por utilizador, burst) |
| Health checks | Endpoints de health com checks por módulo e dependência |
| OpenAPI/Scalar | Documentação automática da API com Scalar UI |
| Auto-migrations | Execução automática de migrações EF Core no startup |
| Composition root | Registo de todos os módulos, building blocks e dependências |

**Avaliação:** Bem organizado como composition root. O rate limiting com 6 políticas é robusto. As auto-migrations são convenientes para desenvolvimento mas devem ser opcionais em produção.

### 16.2 BackgroundWorkers

**Função:** Processamento assíncrono e jobs agendados.

| Capacidade | Detalhe |
|---|---|
| Quartz.NET | Scheduler para jobs recorrentes |
| BreakGlass expiration | Job de expiração de acessos de emergência |
| JIT expiration | Job de expiração de acessos just-in-time |
| Delegation expiration | Job de expiração de delegações |

**Avaliação:** Alinhado com a decisão de usar Quartz.NET (secção 14.5 da visão). Os jobs de expiração são críticos para a segurança do IdentityAccess.

### 16.3 Ingestion.Api

**Função:** Ponto de entrada para ingestão de dados externos.

| Capacidade | Detalhe |
|---|---|
| Entry point | API dedicada para receber dados de fontes externas |
| Separação | Isolamento do tráfego de ingestão do tráfego operacional |

**Avaliação:** A separação do ponto de ingestão é uma decisão arquitetural positiva. Permite escalar independentemente e aplicar políticas específicas.

### Caminhos de evidência

```
src/Platform/
├── NexTraceOne.ApiHost/                         # Composition root, rate limiting, health, OpenAPI
├── NexTraceOne.BackgroundWorkers/               # Quartz.NET, expiration jobs
└── NexTraceOne.Ingestion.Api/                   # Entry point de ingestão
```

---

## 17. Análise Transversal — CancellationToken

### Distribuição por módulo

| Módulo | Métodos sem CancellationToken | Classificação de risco |
|---|---|---|
| Notifications | 46 | 🔴 Crítico |
| AIKnowledge | 43 | 🔴 Crítico |
| OperationalIntelligence | 36 | 🔴 Crítico |
| Catalog | 26 | 🟡 Elevado |
| IdentityAccess | 24 | 🟡 Elevado |
| Configuration | 19 | 🟡 Elevado |
| ChangeGovernance | 11 | 🟡 Moderado |
| Governance | 9 | 🟢 Baixo |
| Integrations | 9 | 🟢 Baixo |
| ProductAnalytics | 8 | 🟢 Baixo |
| AuditCompliance | 4 | 🟢 Baixo |
| Knowledge | 2 | 🟢 Baixo |
| **Total** | **247** | — |

### Impacto

A ausência de `CancellationToken` em métodos async tem consequências:

1. **Operações não podem ser canceladas** quando o pedido HTTP é abortado
2. **Recursos ficam retidos** durante operações longas desnecessárias
3. **Graceful shutdown é comprometido** — processos podem ficar pendurados
4. **Degradação de performance** sob carga elevada com muitos pedidos cancelados

### Recomendação transversal

Implementar um **analyzer Roslyn** ou regra de linting que detete métodos async sem `CancellationToken` e impeça regressões futuras. Corrigir os 247 métodos existentes em sprints dedicados, começando pelos módulos críticos (Notifications, AIKnowledge, OperationalIntelligence).

---

## 18. Matriz de Maturidade Consolidada

### Por dimensão

| Módulo | Domínio | Aplicação | Infraestrutura | API | Operacionalidade | Global |
|---|---|---|---|---|---|---|
| **IdentityAccess** | ⬛⬛⬛⬛⬛ | ⬛⬛⬛⬛⬛ | ⬛⬛⬛⬛⬛ | ⬛⬛⬛⬛⬛ | ⬛⬛⬛⬛⬜ | **READY** |
| **Catalog** | ⬛⬛⬛⬛⬛ | ⬛⬛⬛⬛⬛ | ⬛⬛⬛⬛⬛ | ⬛⬛⬛⬛⬛ | ⬛⬛⬛⬛⬜ | **READY** |
| **Notifications** | ⬛⬛⬛⬛⬜ | ⬛⬛⬛⬛⬛ | ⬛⬛⬛⬛⬜ | ⬛⬛⬛⬛⬜ | ⬛⬛⬛⬛⬜ | **READY** |
| **Configuration** | ⬛⬛⬛⬛⬜ | ⬛⬛⬛⬛⬜ | ⬛⬛⬛⬛⬜ | ⬛⬛⬛⬜⬜ | ⬛⬛⬛⬛⬜ | **READY** |
| **ChangeGovernance** | ⬛⬛⬛⬛⬛ | ⬛⬛⬛⬛⬜ | ⬛⬛⬛⬛⬜ | ⬛⬛⬛⬛⬜ | ⬛⬛⬜⬜⬜ | **PARTIAL** |
| **AIKnowledge** | ⬛⬛⬛⬛⬜ | ⬛⬛⬛⬛⬜ | ⬛⬛⬛⬜⬜ | ⬛⬛⬛⬜⬜ | ⬛⬛⬜⬜⬜ | **PARTIAL** |
| **OperationalIntelligence** | ⬛⬛⬛⬛⬛ | ⬛⬛⬛⬛⬜ | ⬛⬛⬛⬜⬜ | ⬛⬛⬛⬜⬜ | ⬛⬜⬜⬜⬜ | **PARTIAL** |
| **Governance** | ⬛⬛⬛⬛⬜ | ⬛⬛⬛⬛⬜ | ⬛⬛⬛⬜⬜ | ⬛⬛⬛⬛⬜ | ⬛⬛⬛⬜⬜ | **PARTIAL** |
| **AuditCompliance** | ⬛⬛⬛⬛⬜ | ⬛⬛⬛⬜⬜ | ⬛⬛⬛⬜⬜ | ⬛⬛⬜⬜⬜ | ⬛⬛⬛⬜⬜ | **PARTIAL** |
| **Integrations** | ⬛⬛⬜⬜⬜ | ⬛⬛⬜⬜⬜ | ⬛⬜⬜⬜⬜ | ⬛⬛⬜⬜⬜ | ⬜⬜⬜⬜⬜ | **INCOMPLETE** |
| **Knowledge** | ⬛⬛⬜⬜⬜ | ⬛⬛⬜⬜⬜ | ⬛⬜⬜⬜⬜ | ⬛⬜⬜⬜⬜ | ⬜⬜⬜⬜⬜ | **INCOMPLETE** |
| **ProductAnalytics** | ⬛⬜⬜⬜⬜ | ⬛⬛⬜⬜⬜ | ⬛⬜⬜⬜⬜ | ⬛⬜⬜⬜⬜ | ⬜⬜⬜⬜⬜ | **INCOMPLETE** |

### Legenda

- ⬛⬛⬛⬛⬛ = Completo
- ⬛⬛⬛⬛⬜ = Quase completo
- ⬛⬛⬛⬜⬜ = Substancial
- ⬛⬛⬜⬜⬜ = Básico
- ⬛⬜⬜⬜⬜ = Inicial
- ⬜⬜⬜⬜⬜ = Inexistente

---

## 19. Recomendações Prioritárias

### Prioridade 1 — Crítica (bloqueia valor de produção)

| # | Recomendação | Módulos impactados |
|---|---|---|
| 1.1 | Implementar pipeline de ingestão de dados de deploy/change | Integrations → ChangeGovernance |
| 1.2 | Criar migração EF para Knowledge | Knowledge |
| 1.3 | Implementar pelo menos um adapter real de integração (GitHub/GitLab) | Integrations |
| 1.4 | Resolver CancellationToken nos 3 módulos críticos (ntf: 46, aik: 43, ops: 36) | Notifications, AIKnowledge, OperationalIntelligence |

### Prioridade 2 — Alta (necessário para MVP funcional completo)

| # | Recomendação | Módulos impactados |
|---|---|---|
| 2.1 | Expandir grounding/RAG para cobrir serviços, contratos e knowledge | AIKnowledge, Catalog, Knowledge |
| 2.2 | Implementar pipeline de telemetria mínimo para SLO | OperationalIntelligence |
| 2.3 | Integrar blast radius com topologia do Catalog | ChangeGovernance, Catalog |
| 2.4 | Implementar pesquisa FTS no Knowledge | Knowledge |
| 2.5 | Implementar correlação change-to-incident | ChangeGovernance, OperationalIntelligence |
| 2.6 | Completar CancellationToken nos módulos elevados (cat: 26, iam: 24, cfg: 19) | Catalog, IdentityAccess, Configuration |

### Prioridade 3 — Média (melhora qualidade e completude)

| # | Recomendação | Módulos impactados |
|---|---|---|
| 3.1 | Implementar ingestão de dados de custo para FinOps real | Governance, OperationalIntelligence |
| 3.2 | Completar ciclo de vida de campanhas de auditoria | AuditCompliance |
| 3.3 | Implementar cache de resolução hierárquica de configuração | Configuration |
| 3.4 | Adicionar mais ferramentas de IA (contratos, incidentes, blast radius) | AIKnowledge |
| 3.5 | Implementar versionamento de documentos | Knowledge |
| 3.6 | Rever discrepância interfaces/implementações em Notifications | Notifications |
| 3.7 | Implementar Roslyn analyzer para detetar async sem CancellationToken | Transversal |

### Prioridade 4 — Baixa (refinamento e evolução)

| # | Recomendação | Módulos impactados |
|---|---|---|
| 4.1 | Avaliar pesquisa vetorial com pgvector para RAG | AIKnowledge |
| 4.2 | Adicionar canais de notificação adicionais | Notifications |
| 4.3 | Implementar queries de agregação de analytics | ProductAnalytics |
| 4.4 | Considerar consolidação de DbContexts no OperationalIntelligence | OperationalIntelligence |
| 4.5 | Avaliar auto-migrations como opcionais em produção | ApiHost |

---

## 20. Conclusão

### Estado geral

O backend do NexTraceOne demonstra uma **base arquitetural sólida e bem pensada**. A aderência aos princípios de Clean Architecture, DDD e modular monolith é evidente na estrutura de todos os 12 módulos.

### Pontos fortes

1. **Modelo de domínio rico** — entidades, enums e value objects bem definidos em todos os bounded contexts
2. **Building blocks robustos** — fundação transversal consistente com DDD, CQRS, tenant isolation e segurança
3. **Separação clara** — cada módulo respeita fronteiras de bounded context
4. **Preparação para evolução** — ClickHouse, outbox pattern e event-driven design preparados
5. **Segurança por desenho** — JWT, ApiKey, RLS, AES-GCM, Break Glass, JIT no core

### Gaps principais

1. **247 métodos async sem CancellationToken** — risco transversal de operações não canceláveis
2. **Módulos dependentes de dados reais não ingeridos** — ChangeGovernance e OperationalIntelligence modelados mas sem pipeline de dados
3. **Knowledge sem migração EF** — módulo recente sem materialização na base de dados
4. **Integrations sem adapters** — impossibilidade de ingerir dados de fontes externas
5. **Grounding/RAG limitado** — IA sem acesso real aos dados do produto para contextualização

### Direção recomendada

A prioridade deve ser completar o **ciclo de ingestão de dados** (Integrations → ChangeGovernance / OperationalIntelligence) e resolver a **dívida técnica do CancellationToken**. Isto desbloqueará o valor real dos módulos que já possuem modelagem de domínio rica mas que operam sem dados de produção.

O NexTraceOne está bem posicionado para cumprir a sua visão como **fonte de verdade para serviços, contratos, mudanças, operação e conhecimento operacional**. Os alicerces existem; o caminho para produção requer completude nas ligações entre módulos e na ingestão de dados reais.

---

**Fim do relatório.**

*Gerado em 2026-03-27 | NexTraceOne Backend State Assessment*
