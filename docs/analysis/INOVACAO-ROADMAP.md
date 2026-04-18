# Inovação — Roadmap de Novas Funcionalidades
> Propostas baseadas no estado actual do produto e nos gaps identificados.
> Ordenadas por valor potencial e viabilidade de implementação.

---

## Princípio de Selecção

Cada proposta deve:
1. Reforçar o NexTraceOne como Source of Truth
2. Resolver uma dor real de ambiente enterprise
3. Ser implementável incrementalmente sobre a base existente
4. Diferenciar o produto de ferramentas genéricas (Backstage, Datadog, etc.)

---

## Tier 1 — Alto Valor, Base Existente Aproveitável

### 1.1 Contract Drift Detection entre Ambientes

**O que é:** Detectar automaticamente quando o contrato activo em produção diverge do contrato em staging ou dev.

**Dor que resolve:** Equipas promovem código sem perceber que o contrato de staging já divergiu do que produção espera.

**Como funciona:**
- Comparar `ContractVersion` activa por ambiente a cada ciclo de ingestão
- Gerar alerta quando versão em staging != versão em produção (sem promoção formal)
- Mostrar diff semântico automático entre versões de ambientes diferentes
- Integrar na view de Contract Detail com indicador visual por ambiente

**Base existente aproveitável:**
- `ContractDeployment` entity já existe
- Diff semântico já implementado
- EnvironmentContext no frontend já presente

**Esforço estimado:** Médio (2-3 semanas backend + 1 semana frontend)

---

### 1.2 Change-to-Contract Impact (Automático)

**O que é:** Quando um deploy é registado, o sistema analisa automaticamente quais contratos foram potencialmente afectados e notifica os consumidores.

**Dor que resolve:** Hoje, quando um serviço faz deploy, os consumidores dos seus contratos só descobrem breaking changes quando começam a falhar.

**Como funciona:**
- Ao ingerir um evento de deploy, correlacionar com `ContractVersion` do serviço
- Comparar com versão anterior usando diff semântico existente
- Se breaking change detectado: notificar consumidores registados
- Gerar "impact card" com lista de contratos afectados e nível de severidade

**Base existente aproveitável:**
- ChangeGovernance module com ingestão de eventos
- ContractDiff entity e lógica já existentes
- Notifications module já presente

**Esforço estimado:** Médio (3-4 semanas)

---

### 1.3 Promotion Readiness Score com Contexto Real

**O que é:** Score automático (0-100) para decidir se uma mudança está pronta para promoção, baseado em dados reais de runtime em não-produção.

**Dor que resolve:** Hoje o score é baseado em regras estáticas. Não há leitura real de "como o serviço se comportou em staging nas últimas 24h".

**Como funciona:**
- Agregar métricas de reliability (error rate, latência p99) do ambiente de staging
- Comparar com baseline histórico do mesmo serviço em produção
- Combinar com: testes passados, evidências, aprovações, blast radius
- Apresentar score composto com breakdown por categoria

**Base existente aproveitável:**
- IPromotionRiskSignalProvider (PLANNED — esta feature seria a sua implementação real)
- ReliabilityDbContext com métricas já presentes
- Promotion gates já existem no ChangeGovernance

**Esforço estimado:** Alto (4-6 semanas, inclui implementar IPromotionRiskSignalProvider)

---

### 1.4 Incident-to-Change Automatic Correlation

**O que é:** Quando um incidente é criado ou escalado, o sistema correlaciona automaticamente com deploys recentes e mostra os candidatos mais prováveis de causa.

**Dor que resolve:** Equipas de on-call perdem 20-40 minutos a identificar manualmente "o que mudou antes deste incidente".

**Como funciona:**
- Ao criar incidente, janela temporal de X horas antes
- Listar todos os deploys nessa janela para serviços da dependency topology
- Ordenar por proximidade temporal e blast radius
- Mostrar diff dos contratos que mudaram
- Acção directa: "Iniciar rollback" ou "Criar waiver"

**Base existente aproveitável:**
- IDistributedSignalCorrelationService (PLANNED — esta feature implementa-a)
- IncidentDbContext e ChangeIntelligenceDbContext já existem
- Dependency topology já no Catalog

**Esforço estimado:** Alto (5-7 semanas)

---

## Tier 2 — Valor Diferenciador, Esforço Médio-Alto

### 2.1 AI Contract Reviewer

**O que é:** O assistente IA analisa um contrato (REST, SOAP, Event) e devolve:
- Problemas de design (naming inconsistente, campos ambíguos)
- Breaking changes potenciais vs. versão anterior
- Sugestões de exemplos em falta
- Verificação de conformidade com políticas da organização

**Dor que resolve:** Revisões de contrato são lentas e subjectivas. Problemas só são detectados em code review ou em produção.

**Base existente aproveitável:**
- AIKnowledge module com agents e tool definitions
- ContractDiff e lint rules já existem
- Contract policies já no Catalog

**Esforço estimado:** Médio (3-4 semanas)

---

### 2.2 Service Health Heatmap por Domínio

**O que é:** Vista visual (heatmap) de saúde de todos os serviços agrupados por domínio/equipa, mostrando tendência das últimas 72h.

**Dor que resolve:** Tech Leads e Architects não têm vista consolidada de saúde sem abrir cada serviço individualmente.

**Como funciona:**
- Grid de serviços × domínio
- Cor baseada em: error rate, latência, mudanças recentes, incidentes activos
- Drill-down ao clicar
- Persona-aware: Executive vê por domínio; Engineer vê por serviço

**Base existente aproveitável:**
- ReliabilityDbContext com métricas por serviço
- Dependency topology no Catalog
- Dashboard persona-aware já implementado

**Esforço estimado:** Médio (2-3 semanas)

---

### 2.3 Contract SLA Monitor

**O que é:** Definir SLAs por contrato (ex: "este endpoint deve responder em < 200ms com error rate < 0.1%") e alertar quando viola.

**Dor que resolve:** Consumidores de contratos não têm forma de saber se o produtor está a cumprir os acordos de nível de serviço definidos.

**Base existente aproveitável:**
- ContractHealthScore entity já existe
- RuntimeIntelligenceDbContext com telemetria
- Notifications module para alertas

**Esforço estimado:** Médio-Alto (4-5 semanas)

---

### 2.4 Automated Runbook Generation (AI)

**O que é:** A partir de incidentes anteriores e da topology do serviço, o sistema gera um runbook inicial automático para tipos de falha recorrentes.

**Dor que resolve:** Runbooks são raramente escritos proactivamente. Equipas escrevem documentação de incidente apenas quando a dor é grande.

**Como funciona:**
- Detectar padrões em incidentes do mesmo serviço (mesmo erro, mesma causa)
- Propor runbook draft com: descrição, passos de diagnóstico, mitigação, rollback
- Tech Lead revisa e publica com um clique
- Fica ligado ao serviço e ao tipo de incidente

**Base existente aproveitável:**
- Knowledge module com runbooks já existente
- AIKnowledge com agents
- Incident timeline já estruturada

**Esforço estimado:** Alto (5-6 semanas)

---

## Tier 3 — Diferenciação Estratégica, Esforço Alto

### 3.1 Developer Portal Público por Tenant

**O que é:** Portal público (ou privado por token) onde consumidores externos podem descobrir, explorar e subscrever contratos publicados.

**Dor que resolve:** Hoje, consumidores externos precisam de acesso ao sistema interno para ver contratos. Falta uma superfície de developer experience externa.

**Funcionalidades:**
- Navegação de contratos publicados
- Geração de SDK client (já existe PREVIEW)
- Subscrição a notificações de breaking changes
- Playground interactivo de API

**Base existente aproveitável:**
- Publication Center já existe
- DeveloperPortalDbContext já presente
- Contract examples e schemas já implementados

---

### 3.2 FinOps por Release

**O que é:** Correlacionar custo incremental de infra com cada release. Mostrar "este deploy de v2.3 aumentou custo mensal em X%".

**Dor que resolve:** Equipas de engenharia não percebem o impacto financeiro das suas decisões técnicas (ex: N+1 queries, payloads maiores, mais chamadas externas).

**Base existente aproveitável:**
- CostIntelligenceDbContext já existe
- ChangeGovernance com release tracking
- FinOps module no Governance

---

### 3.3 Contract Test Coverage por Consumidor

**O que é:** Rastrear quais consumidores de um contrato têm testes de contrato (consumer-driven contract tests) e quais não têm.

**Dor que resolve:** Em ambientes com muitos consumidores, é impossível saber quem está a testar activamente o contrato que produz.

**Base existente aproveitável:**
- Consumer-driven contracts já existe no frontend
- Contract versioning e consumer registration

---

### 3.4 Compliance Audit Trail com Exportação Forense

**O que é:** Exportar trilha de auditoria num formato imutável e verificável (ex: PDF assinado, JSON com hash) para auditorias externas ou regulatórias.

**Dor que resolve:** Auditores externos precisam de evidências em formatos que não podem ser alterados após geração.

**Base existente aproveitável:**
- AuditCompliance module robusto
- Evidence Pack já existe
- AuditDbContext com retenção por política

---

## Priorização Sugerida

| # | Feature | Tier | Valor | Esforço | Depende de |
|---|---------|------|-------|---------|------------|
| 1 | Contract Drift Detection | 1 | Alto | Médio | — |
| 2 | Change-to-Contract Impact | 1 | Alto | Médio | — |
| 3 | Incident-to-Change Correlation | 1 | Alto | Alto | IDistributedSignalCorrelationService |
| 4 | Promotion Readiness Score real | 1 | Alto | Alto | IPromotionRiskSignalProvider |
| 5 | AI Contract Reviewer | 2 | Médio-Alto | Médio | — |
| 6 | Service Health Heatmap | 2 | Médio | Médio | — |
| 7 | Contract SLA Monitor | 2 | Médio | Médio-Alto | — |
| 8 | AI Runbook Generation | 2 | Médio | Alto | — |
| 9 | Developer Portal Público | 3 | Alto | Alto | Publication Center |
| 10 | FinOps por Release | 3 | Médio | Alto | Cost Intelligence |

---

## Nota sobre Implementação

Nenhuma destas features deve ser implementada antes de fechar os gaps P0 identificados em GAPS-BACKEND.md, GAPS-FRONTEND.md e GAPS-TESTES.md.

Adicionar features sobre uma base com stubs críticos não implementados e 4% de cobertura de testes apenas aumenta a dívida técnica.
