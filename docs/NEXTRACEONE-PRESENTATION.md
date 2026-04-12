# NexTraceOne — Documento Formal de Apresentação

> **Versão:** 2.0 — Abril 2026
> **Tipo:** Documento institucional e comercial
> **Público-alvo:** Decisores, CTOs, VPs de Engenharia, Diretores de Operações, Auditores e Equipas Técnicas

---

## Sumário Executivo

O **NexTraceOne** é uma plataforma enterprise unificada de **governança de engenharia e confiança operacional**.

Num único produto, o NexTraceOne consolida catálogo de serviços, governança de contratos, inteligência de mudanças, confiabilidade operacional, inteligência artificial governada e gestão de conhecimento operacional — funcionando como a **fonte oficial de verdade (Source of Truth)** para toda a infraestrutura de serviços de uma organização.

> **Proposta central:** eliminar a fragmentação de informação e de decisão que existe quando serviços, contratos, mudanças, incidentes e conhecimento operacional estão espalhados por dezenas de ferramentas sem ligação entre si.

---

## Parte I — O Que É o NexTraceOne

### 1.1 Definição

O NexTraceOne é um **Engineering Governance Control Plane** — uma plataforma que centraliza e governa:

| Domínio | O que o NexTraceOne sabe responder |
|---------|-----------------------------------|
| **Serviços** | Que serviços existem? Quem é o owner? Qual é o estado de saúde? Quais as dependências? |
| **Contratos** | Que APIs, eventos, SOAP e schemas estão publicados? Que versão está ativa? Quem consome? |
| **Mudanças** | Que mudanças estão em curso? Qual o risco? Qual o blast radius? Quem aprovou? |
| **Operação** | Que incidentes ocorreram? Estão correlacionados com mudanças? Qual o SLO/SLA? |
| **IA** | Que modelos estão disponíveis? Quem pode usá-los? Quanto custa? Existe auditoria? |
| **Conhecimento** | Onde está o runbook? E a documentação? E as notas operacionais? |
| **Custo** | Quanto custa este serviço? Esta equipa? Este ambiente? Após esta mudança? |

### 1.2 Posicionamento

O NexTraceOne **não** é:

- ❌ um clone do Datadog, Dynatrace ou Grafana — não é apenas observabilidade
- ❌ um clone do Backstage ou SwaggerHub — não é apenas catálogo
- ❌ um clone do ServiceNow ou Jira — não é apenas gestão de tickets
- ❌ um chat genérico com LLM — não é apenas IA

O NexTraceOne **é**:

- ✅ a plataforma onde se consulta a verdade sobre serviços, contratos e mudanças
- ✅ o sistema que dá confiança para promover mudanças a produção
- ✅ o ponto onde operação, engenharia, governança e IA convergem com contexto
- ✅ uma plataforma self-hosted, soberana e auditável

---

## Parte II — Que Problemas o NexTraceOne Resolve

### 2.1 Problemas de engenharia

| Problema | Impacto | Como o NexTraceOne resolve |
|----------|---------|----------------------------|
| APIs crescem sem governança | Contratos quebrados, duplicação, incompatibilidade | **Contract Governance** — versionamento, diff semântico, compatibilidade automática, linting e approval workflows |
| Ownership obscuro | Ninguém sabe quem é responsável por quê | **Service Catalog** — ownership explícito, lifecycle management, topology de dependências |
| Mudanças sem análise de impacto | Deploys arriscados, falhas em cascata | **Change Intelligence** — blast radius, risk scoring, promotion gates, evidence packs |
| Documentação dispersa e desatualizada | Troubleshooting lento, onboarding demorado | **Knowledge Hub** — documentação centralizada, auto-documentation por serviço, relações de conhecimento |

### 2.2 Problemas operacionais

| Problema | Impacto | Como o NexTraceOne resolve |
|----------|---------|----------------------------|
| Incidentes sem causa clara | MTTR alto, recorrência | **Incident Correlation** — correlação automática incidente ↔ mudança ↔ contrato |
| Baixa confiança em deployments | Medo de promover a produção | **Production Change Confidence** — scoring, validação pós-change, comparação entre ambientes |
| Inconsistência operacional | Cada equipa opera de forma diferente | **Operational Consistency** — runbooks padronizados, automações com aprovação, SLO/SLA tracking |
| Custos sem contexto | Não se sabe onde se gasta | **FinOps contextual** — custo por serviço, equipa, ambiente e mudança |

### 2.3 Problemas de governança e compliance

| Problema | Impacto | Como o NexTraceOne resolve |
|----------|---------|----------------------------|
| Uso descontrolado de IA | Vazamento de dados, custo descontrolado | **AI Governance** — model registry, access policies, token budgets, audit trail completo |
| Falta de rastreabilidade | Auditoria impossível | **Audit & Compliance** — hash chain SHA-256, trilha imutável, evidence export, assinatura digital |
| Sem visão executiva | Decisões baseadas em intuição | **Executive Views** — DORA Metrics, service scorecards, risk center, compliance dashboards |

---

## Parte III — Porque o NexTraceOne É Diferente

### 3.1 O problema das ferramentas fragmentadas

A maioria das organizações enterprise utiliza ferramentas separadas para cada preocupação:

```
┌─────────────┐  ┌──────────────┐  ┌───────────────┐  ┌────────────┐
│  Catálogo   │  │ Observabilid │  │   Change      │  │    IA      │
│  (Backstage │  │ (Datadog /   │  │   Management  │  │  (ChatGPT/ │
│   / Wiki)   │  │  Dynatrace)  │  │  (Jira/Snow)  │  │   Copilot) │
└──────┬──────┘  └──────┬───────┘  └──────┬────────┘  └─────┬──────┘
       │                │                  │                  │
       ▼                ▼                  ▼                  ▼
   SEM LIGAÇÃO      SEM LIGAÇÃO       SEM LIGAÇÃO       SEM LIGAÇÃO
   ENTRE ELES       ENTRE ELES        ENTRE ELES        ENTRE ELES
```

**Resultado:** quando ocorre um incidente, é preciso consultar 4-5 ferramentas para entender o que aconteceu. Quando se quer promover uma mudança a produção, não existe um local único que mostre o risco, as dependências, os contratos afetados e o histórico.

### 3.2 O NexTraceOne como plataforma unificada

```
┌──────────────────────────────────────────────────────────────────┐
│                          NexTraceOne                             │
│                                                                  │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────────────┐│
│  │ Serviços │──│Contratos │──│ Mudanças │──│  Incidentes      ││
│  │ & Owner  │  │ & Versões│  │ & Risco  │  │  & Mitigação     ││
│  └────┬─────┘  └────┬─────┘  └────┬─────┘  └────┬─────────────┘│
│       │              │              │              │              │
│       └──────────────┴──────────────┴──────────────┘              │
│                         CONTEXTO UNIFICADO                       │
│                              │                                    │
│       ┌──────────────────────┼──────────────────────┐            │
│       ▼                      ▼                      ▼            │
│  ┌──────────┐         ┌──────────┐         ┌──────────────┐     │
│  │    IA    │         │Knowledge │         │  Governance   │     │
│  │Governada │         │   Hub    │         │  & FinOps     │     │
│  └──────────┘         └──────────┘         └──────────────┘     │
└──────────────────────────────────────────────────────────────────┘
```

**O diferencial central: todos os domínios estão conectados.**

Quando se consulta um serviço, vê-se o contrato, o owner, as mudanças recentes, os incidentes correlacionados, o custo e a documentação — tudo no mesmo lugar.

### 3.3 Comparação com plataformas de mercado

| Capacidade | Backstage | Datadog | ServiceNow | NexTraceOne |
|-----------|-----------|---------|------------|-------------|
| Service Catalog | ✅ | ⚠️ Parcial | ⚠️ CMDB | ✅ Com ownership, lifecycle, topology |
| Contract Governance (REST/SOAP/Event) | ❌ | ❌ | ❌ | ✅ 10 tipos de contratos com studio visual |
| Semantic Diff de contratos | ❌ | ❌ | ❌ | ✅ Diff + compatibilidade automática |
| Change Intelligence (blast radius, risk) | ❌ | ⚠️ Básico | ⚠️ Manual | ✅ Scoring, blast radius, promotion gates |
| Promotion Governance entre ambientes | ❌ | ❌ | ⚠️ | ✅ Gates, avaliações, evidence packs |
| Incident ↔ Change correlation | ❌ | ⚠️ Parcial | ⚠️ Parcial | ✅ Correlação automática |
| AI com governança e auditoria | ❌ | ❌ | ❌ | ✅ Model registry, budgets, policies, audit |
| IA interna (self-hosted, sem cloud) | ❌ | ❌ | ❌ | ✅ Ollama / modelos locais como padrão |
| FinOps contextual por serviço/mudança | ❌ | ⚠️ | ❌ | ✅ Custo por serviço, equipa, ambiente |
| Audit trail imutável (SHA-256) | ❌ | ❌ | ⚠️ | ✅ Hash chain verificável |
| Self-hosted / on-premises | ✅ | ❌ Cloud-only | ⚠️ | ✅ Desenhado para on-prem |
| Multi-tenant com isolamento RLS | ❌ | N/A | ⚠️ | ✅ PostgreSQL Row-Level Security |
| Suporte a mainframe/CICS/Copybook | ❌ | ❌ | ❌ | ✅ Contratos legacy incluídos |

### 3.4 Os 6 diferenciais-chave do NexTraceOne

1. **Contexto unificado** — Serviço, contrato, mudança, incidente e custo ligados entre si, não em silos.

2. **Contract Governance real** — 10 tipos de contratos (REST, SOAP, Event, Background Service, Shared Schema, Webhook, Copybook, MQ Message, Fixed Layout, CICS Commarea) com studio visual, versionamento, diff semântico, compatibilidade e approval workflows.

3. **Change Intelligence orientado a decisão** — Blast radius, risk scoring, freeze windows, promotion gates, evidence packs e validação pós-change. Cada mudança tem identidade, risco e evidência.

4. **IA interna e governada** — Modelos internos (Ollama) como padrão, modelos externos como opção controlada. Model registry, access policies, token budgets e audit trail completo. Agentes especializados com grounding nos dados reais da plataforma.

5. **Self-hosted e soberano** — Desenhado para funcionar completamente on-premises, sem dependência de cloud. PostgreSQL, Docker Compose e IIS como opções de deploy. Sem telemetria enviada para fora.

6. **Auditoria e compliance por desenho** — Hash chain SHA-256, trilha imutável, evidence export com assinatura digital, RBAC granular, JIT Access e Break Glass Protocol.

---

## Parte IV — Porque Uma Empresa Deveria Contratar o NexTraceOne

### 4.1 Retorno sobre investimento (ROI)

| Área de impacto | Situação sem NexTraceOne | Com NexTraceOne | Benefício esperado |
|----------------|--------------------------|-----------------|---------------------|
| **Tempo de troubleshooting** | Consultar 4-5 ferramentas para entender um incidente | Um local com serviço, contrato, mudanças e incidentes correlacionados | Redução de MTTR em 30-60% |
| **Confiança em deploys** | "Vamos deployar e ver o que acontece" | Blast radius, risk score, validation gates | Redução de rollbacks em 40-70% |
| **Governança de contratos** | Contratos em Confluence/SharePoint sem versionamento | Versionamento, diff semântico, approval, linting automático | Eliminação de breaking changes não detetados |
| **Custo de IA** | Cada engenheiro usa ChatGPT/Copilot sem controlo | Budgets, quotas, auditoria, modelos internos | Redução de custo de IA em 50-80% |
| **Onboarding de engenheiros** | "Pergunta ao João, ele sabe" | Knowledge Hub, auto-documentation, Source of Truth | Redução de tempo de onboarding em 40-60% |
| **Compliance e auditoria** | Trilha manual, evidências em e-mails | Audit trail automático, evidence export, hash chain | Tempo de preparação de auditoria reduzido em 70% |

### 4.2 Para quem o NexTraceOne cria valor

| Persona | Valor principal |
|---------|----------------|
| **CTO / VP Engenharia** | Visão consolidada de risco, maturidade e confiabilidade. Decisões baseadas em dados reais, não em intuição. |
| **Engenheiro** | Um local para ver os contratos, as dependências, pedir ajuda à IA e documentar. Menos context-switching. |
| **Tech Lead** | Blast radius antes de aprovar, promotion readiness, SLO/SLA tracking, reliability por equipa. |
| **Arquiteto** | Topology de serviços, padrões de contrato, compatibilidade, drift detection, impact cascade analysis. |
| **Product Manager** | Risco de release claro, change confidence score, calendário de deploys. |
| **Platform Admin** | Políticas centralizadas, AI governance, integrações, configuração por tenant e ambiente. |
| **Auditor** | Trilha imutável, evidence packs, compliance frameworks, export para auditoria. |
| **CISO / Segurança** | RBAC, RLS, JIT Access, Break Glass, encriptação AES-256-GCM, headers de segurança. |

### 4.3 Cenários de uso concretos

#### Cenário 1: "Uma mudança vai a produção — o que pode correr mal?"

**Sem NexTraceOne:** O engenheiro faz o deploy e espera. Se algo falhar, começa a procurar em logs, dashboards e Slack.

**Com NexTraceOne:**
1. O Change Intelligence mostra o **risk score** da mudança (0.0 a 1.0)
2. O **blast radius** identifica os 12 consumidores diretos e 34 transitivos que podem ser afetados
3. Os **promotion gates** verificam se os testes passaram no ambiente de staging
4. O **evidence pack** é gerado automaticamente com tudo o que aconteceu antes, durante e depois
5. Se houver incidente, a **correlação automática** liga o incidente à mudança em segundos

#### Cenário 2: "Alguém mudou a API e os consumidores não sabiam"

**Sem NexTraceOne:** Breaking change descoberta em produção. Incidente. Reunião de pós-mortem. Promessa de que "nunca mais acontece".

**Com NexTraceOne:**
1. O **Contract Studio** permite criar e versionar contratos com diff semântico
2. O **compatibility check** detecta breaking changes antes do deploy
3. O **approval workflow** obriga a aprovação antes de publicar uma nova versão
4. O **contract drift detection** compara o contrato publicado com o tráfego real (traces OTel) e identifica ghost endpoints e endpoints não declarados

#### Cenário 3: "A equipa usa ChatGPT sem controlo — como governar?"

**Sem NexTraceOne:** Dados sensíveis podem sair para modelos externos. Custo descontrolado. Sem auditoria.

**Com NexTraceOne:**
1. O **model registry** define que modelos estão disponíveis (internos e externos)
2. As **access policies** controlam quem pode usar qual modelo
3. Os **token budgets** limitam o gasto por equipa, utilizador ou período
4. O **audit trail** regista cada interação: prompt, contexto, resposta, modelo, custo
5. Os **security guardrails** bloqueiam prompt injection e vazamento de credenciais
6. O modelo interno (Ollama) funciona 100% on-premises — sem dados enviados para fora

#### Cenário 4: "Precisamos preparar uma auditoria de compliance"

**Sem NexTraceOne:** Semanas a recolher evidências de e-mails, tickets e logs manuais.

**Com NexTraceOne:**
1. O **audit trail** com hash chain SHA-256 garante integridade verificável
2. Os **evidence packs** são gerados automaticamente para cada mudança
3. O **compliance module** mapeia frameworks e avalia conformidade
4. O **evidence export** produz pacotes prontos para auditoria com assinatura digital
5. O **risk center** mostra o estado de risco atual por serviço, equipa e domínio

---

## Parte V — Capacidades Detalhadas

### 5.1 Service Catalog & Ownership

- Registo centralizado de serviços com lifecycle management (Planning → Development → Staging → Active → Deprecating → Deprecated → Retired)
- Ownership explícito por equipa e domínio
- Topology de dependências com graph interativo
- Import automático de fontes externas (Backstage, discovery)
- Health score e maturity benchmark por serviço

### 5.2 Contract Governance

- **10 tipos de contratos suportados:** REST (OpenAPI), SOAP (WSDL/XSD), Event (AsyncAPI), Background Service, Shared Schema, Webhook, Copybook, MQ Message, Fixed Layout, CICS Commarea
- **Contract Studio** com visual builders para cada tipo
- Versionamento semântico com diff
- Verificação de compatibilidade automática
- Approval workflows com políticas configuráveis
- Contract drift detection via traces OpenTelemetry
- Contract health score timeline com correlação de mudanças
- Criação assistida por IA

### 5.3 Change Intelligence & Production Change Confidence

- Risk scoring ponderado (breaking change weight + blast radius weight + environment weight)
- Blast radius com consumidores diretos e transitivos
- Freeze windows para proteção de períodos críticos
- Promotion governance com gates configuráveis por ambiente
- Evidence packs automáticos por mudança
- Rollback assessment com viabilidade calculada
- Observação pós-release com revisão automática
- Release calendar com janelas e restrições
- Correlação mudança ↔ incidente

### 5.4 Operational Reliability

- SLO/SLA tracking com burn rate e error budget
- Reliability snapshots por serviço, equipa e domínio
- Runtime intelligence com health monitoring, baseline e drift detection
- Comparação de ambientes (dev vs staging vs produção)
- Incident management com workflow de mitigação
- Runbooks operacionais integrados
- Automações com aprovação, auditoria e validação pós-execução

### 5.5 AI-Assisted Operations & AI Governance

- **IA interna como padrão** — Ollama com modelos locais (DeepSeek, Llama, CodeLlama)
- **IA externa como opção governada** — OpenAI, Anthropic com políticas de acesso
- Model registry com lifecycle management
- Access policies por utilizador, grupo, papel e contexto
- Token budgets com enforcement por período
- Routing inteligente — seleciona modelo por contexto e sensibilidade
- Audit trail completo de cada interação
- Security guardrails — deteção de prompt injection, vazamento de credenciais, PII
- **7 agentes especializados:** service-analyst, contract-designer, change-advisor, incident-responder, test-generator, docs-assistant, security-reviewer
- Grounding contextual com dados reais da plataforma (serviços, contratos, mudanças, incidentes, documentação)
- Extensões IDE para VS Code e Visual Studio

### 5.6 Knowledge Hub

- Documentação operacional centralizada
- Notas operacionais por serviço
- Auto-documentation gerada a partir do catálogo
- Knowledge graph com relações entre entidades
- Relações de conhecimento cross-module

### 5.7 Governance, Compliance & FinOps

- **Relatórios:** DORA Metrics, Service Scorecard, Technical Debt, Custom Dashboards
- **Risk Center:** visão consolidada de risco por serviço, equipa e domínio
- **Compliance:** frameworks de conformidade, avaliação contínua, remediação
- **FinOps contextual:** custo por serviço, equipa, ambiente, operação e mudança. Atribuição, tendências, budget forecasting e recomendações de eficiência
- **Executive Views:** visão consolidada para C-level com métricas cross-module

---

## Parte VI — Segurança e Privacidade

O NexTraceOne foi construído com o princípio **Security by Design**:

| Área | Capacidade |
|------|-----------|
| **Autenticação** | JWT Bearer, OIDC/SAML, API Keys, Cookie httpOnly (opt-in) |
| **Autorização** | RBAC granular com 22+ permission scopes, JIT Access, Break Glass Protocol, delegações com expiração |
| **Isolamento** | Multi-tenant com PostgreSQL Row-Level Security |
| **Encriptação** | AES-256-GCM para payloads sensíveis, PBKDF2 para passwords |
| **Rede** | CORS restritivo, rate limiting, security headers (HSTS, CSP, X-Frame-Options) |
| **Auditoria** | Hash chain SHA-256 imutável, evidence export com assinatura digital |
| **Integridade** | Assembly integrity verification no startup |
| **Frontend** | CSP, source maps desativados em produção, terser com drop_console |
| **Privacidade** | LGPD/GDPR como referência, PII detection nos guardrails de IA |
| **Compliance** | Break Glass com auditoria obrigatória, access reviews periódicos |

---

## Parte VII — Arquitetura e Tecnologia

### 7.1 Princípios arquiteturais

- **Modular Monolith** — 12 bounded contexts com separação clara, sem microserviços prematuros
- **DDD + Clean Architecture + CQRS** — domínio isolado, sem dependência de infraestrutura
- **27 DbContexts** — cada subdomínio com o seu próprio contexto EF Core
- **15 interfaces cross-module** — comunicação entre módulos via contratos claros
- **Outbox Pattern** — 25 processadores para mensageria assíncrona confiável

### 7.2 Stack tecnológica

| Camada | Tecnologia |
|--------|-----------|
| **Backend** | .NET 10, ASP.NET Core 10, EF Core 10, HotChocolate (GraphQL) |
| **Base de dados** | PostgreSQL 16 com RLS |
| **Frontend** | React 19, TypeScript, Vite, TanStack Query, Tailwind CSS, Radix UI, Apache ECharts |
| **Observabilidade** | OpenTelemetry, Elasticsearch e/ou ClickHouse |
| **IA** | Ollama (local), OpenAI/Anthropic (externo, governado) |
| **Testes** | 4.000+ testes unitários, Playwright E2E, Vitest |
| **Deploy** | Docker Compose, IIS, Linux, Windows |
| **Internacionalização** | 4 idiomas (EN, PT-BR, PT-PT, ES) com 7.200+ chaves i18n |

### 7.3 Modelo de deploy

```
                  [Proxy Reverso: nginx/Traefik]
                          |
           ┌──────────────┼──────────────┐
           ▼              ▼              ▼
    [ApiHost:8080]  [Ingestion:8090]  [Frontend: static]
           |              |
           └──────────────┤
                          ▼
                   [PostgreSQL:5432]
                          │
              ┌───────────┼────────────┐
              ▼           ▼            ▼
        [Ollama]   [OTel Collector]  [BackgroundWorkers]
                          │
                   [Elastic/ClickHouse]
```

- **Self-hosted:** funciona completamente on-premises
- **Sem dependências cloud obrigatórias:** toda a observabilidade e IA podem operar localmente
- **Escalável:** stateless API host — safe to scale horizontally
- **Kubernetes-ready:** health checks implementados (`/health`, `/ready`, `/live`)

---

## Parte VIII — Modelo de Licenciamento

O NexTraceOne é um produto **proprietário** com modelo de licenciamento enterprise:

- Licenciamento por capacidade e tenant
- Suporte a licenças online e offline
- Trial/freemium disponível para avaliação
- Enforcement de licença no backend
- Operação 100% self-hosted — os dados nunca saem da infraestrutura do cliente

---

## Parte IX — Porque Agora

O mercado enterprise enfrenta uma convergência de pressões:

1. **Complexidade crescente** — mais serviços, mais APIs, mais equipas, mais ambientes
2. **Exigência de compliance** — reguladores pedem rastreabilidade e evidência
3. **Proliferação de IA** — necessidade urgente de governança sobre uso de modelos
4. **Pressão sobre custos** — FinOps deixou de ser opcional
5. **Expectativa de velocidade** — as equipas precisam de promover mudanças com confiança, não com medo

O NexTraceOne responde a estas 5 pressões numa única plataforma, em vez de obrigar a organização a adquirir e integrar 5-7 ferramentas separadas.

---

## Parte X — Próximos Passos

### Para avaliação

1. **POC (Proof of Concept)** — Instalação local via Docker Compose em menos de 30 minutos
2. **Piloto** — Integrar 3-5 serviços reais, testar governança de contratos e change intelligence
3. **Rollout** — Expansão gradual por equipa e domínio

### Para decisão

Solicitar:
- Demo personalizada com cenário da organização
- Proposta comercial com modelo de licenciamento
- Plano de onboarding e implementação

---

## Contacto

Para mais informação sobre o NexTraceOne, solicitar uma demonstração ou obter uma proposta comercial, contactar a equipa responsável pelo produto.

---

> **NexTraceOne** — *A fonte de verdade para serviços, contratos, mudanças, operação e conhecimento operacional.*
>
> *Governance-first. Context-driven. Enterprise-ready.*
