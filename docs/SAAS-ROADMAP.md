# NexTraceOne — SaaS Roadmap

> Objetivo: "A plataforma de governança operacional que o Dynatrace não é, com observabilidade que o Dynatrace tem."

Este roadmap cobre o caminho para tornar o NexTraceOne um produto SaaS competitivo e comercialmente viável, partindo da base atual.

---

## 1. Estado Atual (Baseline)

### O que já existe e funciona

| Área | Status | Notas |
|---|---|---|
| Multi-tenancy (PostgreSQL RLS) | ✅ Prod-ready | `TenantRlsInterceptor`, `ICurrentTenant` |
| APM via OTel (traces, logs, métricas) | ✅ Prod-ready | 6 ActivitySources, `tail_sampling`, `redaction` |
| Elasticsearch como backend | ✅ Prod-ready | |
| ClickHouse como backend alternativo | ✅ Implementado | |
| Operational Intelligence | ✅ Implementado | `RuntimeSnapshot`, `CapacityForecast` |
| Service Contract Governance | ✅ Implementado | |
| AI Governance (Customer) | ✅ Implementado | `ExternalAI`, `AIBudget`, `AIAccessPolicy` |
| Change Confidence | ✅ Implementado | |
| Ingestion API com API Key auth | ✅ Implementado | `IngestionApiSecurity.cs` |
| `HasCapability()` hook | ✅ Existe | **Capabilities nunca populadas** — bug crítico |
| OTel Collector config avançada | ✅ Prod-ready | tail_sampling, spanmetrics, redaction |
| Deployment multi-env | ✅ Implementado | `DeploymentModel` value object |

### O que está faltando (gaps críticos)

| Gap | Impacto | Prioridade |
|---|---|---|
| Capabilities nunca populadas no JWT | **Bloqueante** — todo gate de licença é bypass | 🔴 P0 |
| NexTrace Agent (binário distribuível) | **Bloqueante** — sem agent não há SaaS real | 🔴 P0 |
| Tenant provisioning automatizado | **Bloqueante** — onboarding manual não escala | 🔴 P0 |
| Módulo de Licensing (`TenantLicense`, HU calc) | **Bloqueante** — sem billing não há SaaS | 🔴 P0 |
| Agent heartbeat endpoint | Necessário para contagem de HU | 🔴 P0 |
| UI de onboarding (wizard) | Impacta conversão | 🟡 P1 |
| Topology Map (service dependencies) | Dynatrace killer feature | 🟡 P1 |
| Real User Monitoring (RUM) | Necessário para competir | 🟡 P1 |
| Log Search UI (Kibana-like) | Usuários esperam isto | 🟡 P1 |
| Alerting engine completo | Crítico para produção | 🟡 P1 |
| Dashboard builder (drag-and-drop) | Competitivo vs Grafana | 🟠 P2 |
| Mobile APM SDK | Expansão de mercado | 🟠 P2 |
| Synthetic Monitoring | Proativo vs reativo | 🟠 P2 |
| Session Replay (RUM avançado) | Premium feature | 🔵 P3 |
| AIOps / anomaly detection | Competir com Davis AI | 🔵 P3 |

---

## 2. Fases do Roadmap

### Fase 0 — Fundação SaaS (6 semanas)
*Pré-requisito para qualquer feature nova. Sem isto, não há SaaS.*

**Sprint 1-2: Licensing Core**
- [ ] Criar módulo `NexTraceOne.Licensing.Domain` com `AgentRegistration` e `TenantLicense`
- [ ] Criar `LicenseRecalculationJob` (Quartz.NET, horário)
- [ ] Criar endpoint `POST /v1/agent/heartbeat` na Ingestion API
- [ ] Definir capabilities por plano (ver `SAAS-LICENSING.md`)

**Sprint 3-4: JWT Capabilities**
- [ ] Modificar `JwtTokenGenerator` para incluir claim `capabilities`
- [ ] Modificar `TenantResolutionMiddleware` para ler e popular `CurrentTenantAccessor`
- [ ] Adicionar gates `HasCapability()` nas features premium (AI Governance, Service Contracts)
- [ ] Testes de integração: tenant Starter não acessa features Professional

**Sprint 5-6: Tenant Provisioning**
- [ ] Criar `TenantProvisioningService` (self-service signup)
- [ ] Workflow: signup → criar Tenant → criar TenantLicense (Starter) → gerar API Key → email com instruções
- [ ] Integração com Stripe (ou equivalente) para billing automático
- [ ] Página de billing no portal do cliente

**Entregável**: Tenant consegue fazer signup, instalar o agent, e ser cobrado automaticamente.

---

### Fase 1 — NexTrace Agent v1 (8 semanas)
*Sem agent distribuível, o produto não compete com Dynatrace/Datadog.*

**Sprint 1-2: Core do Agent**
- [ ] Criar repositório `nexttrace-agent` com builder-config.yaml (ver `NEXTTRACE-AGENT.md`)
- [ ] Implementar `nextraceexporter` (disk-backed queue, API key auth, retry)
- [ ] Implementar `nextraceprocessor` (enriquecimento com host_unit_id, agent_version)
- [ ] Build pipeline (Linux amd64, Windows amd64, Docker)

**Sprint 3-4: Windows/IIS**
- [ ] `install.ps1` para configuração automática de IIS App Pools com CLR Profiler
- [ ] Integração com `iisreceiver`, `windowsperfcountersreceiver`, `windowseventlogreceiver`
- [ ] Config profile `nexttrace-agent-windows.yaml`
- [ ] Teste em ambiente IIS real (.NET Framework 4.8 + .NET 6+)

**Sprint 5-6: Kubernetes**
- [ ] Helm chart para DaemonSet do NexTrace Agent
- [ ] Integração com `k8sattributesprocessor` e `kubeletstatsreceiver`
- [ ] Suporte a OTel Operator (Instrumentation CRD) para auto-inject
- [ ] RBAC mínimo necessário para `k8sclusterreceiver`

**Sprint 7-8: Database + Messaging Receivers**
- [ ] Configuração de receivers para PostgreSQL, SQL Server, MySQL, MongoDB, Redis
- [ ] Solução Oracle via `sqlqueryreceiver` + documentação de setup do JDBC
- [ ] `kafkametricsreceiver` para consumer lag monitoring
- [ ] `rabbitmqreceiver` para queue depth monitoring
- [ ] `nextraceconfigurator` extension com OpAMP básico (remote config)

**Entregável**: NexTrace Agent instalável em Windows/IIS e Linux/K8s, enviando dados para SaaS.

---

### Fase 2 — Observabilidade Produção (10 semanas)
*Features que os usuários comparam com Dynatrace no primeiro dia.*

**Topology Map (Service Dependency)**
- [ ] Extrair relações span → serviço do Elasticsearch/ClickHouse
- [ ] Construir grafo de dependências em tempo real (atualização a cada 1 min)
- [ ] UI: mapa interativo com health overlay (cores por `HealthStatus`)
- [ ] Drill-down: clicar num serviço abre traces, métricas, logs daquele serviço

**Log Search UI**
- [ ] Interface tipo Kibana para busca full-text em logs
- [ ] Correlação log ↔ trace por `trace_id` (já existe nos logs via `NexTraceLogEnricher`)
- [ ] Filtros por tenant, environment, service, severity, time range
- [ ] Saved searches e alertas baseados em log pattern

**Alerting Engine**
- [ ] Definição de alerts como código YAML (similar ao Prometheus alertmanager)
- [ ] Triggers: threshold (>X), anomaly (>2σ), absence (sem dados por N min)
- [ ] Notificações: Slack, PagerDuty, email, webhook (integrações já existem parcialmente)
- [ ] SLO tracking: error budget burn rate

**Real User Monitoring (RUM) — Básico**
- [ ] JavaScript snippet para injeção em SPAs
- [ ] Coleta: page loads, Core Web Vitals, JS errors, user sessions
- [ ] Correlação com APM: trace_id propagado do browser para o backend

**Entregável**: Dashboard comparável ao Dynatrace em qualidade de observabilidade técnica.

---

### Fase 3 — Diferenciadores de Mercado (12 semanas)
*O que faz o cliente escolher NexTraceOne sobre Dynatrace.*

**AI Governance Dashboard (para clientes)**
- [ ] Dashboard consolidado: uso de AI por equipa, custo, violações de policy
- [ ] Chargeback automático por equipa/projeto
- [ ] Alertas de budget de AI em tempo real
- [ ] Auditoria completa: quem usou qual modelo, quando, com quais inputs/outputs (resumido)

**Multi-tenant Hierarchy UI**
- [ ] Vista de holding → subsidiária → departamento
- [ ] Rollup de métricas cross-tenant (observar a empresa toda, não apenas um projeto)
- [ ] Policies de AI Governance propagadas do parent para subsidiárias
- [ ] Billing consolidado ao nível da Holding

**Service Contract Governance (completo)**
- [ ] Comparação automática: SLA contratual vs latência real medida
- [ ] Relatório de breach: "o fornecedor X violou o SLA 3 vezes este mês"
- [ ] Integração com contratos PDF (extração de SLA via AI)
- [ ] Notificação automática ao fornecedor em caso de breach

**Change Confidence (aprimorado)**
- [ ] Comparação automática pre/post deploy (métricas + error rate)
- [ ] Score de confiança: "este deploy reduziu latência em 12%, erro rate estável"
- [ ] Rollback sugerido se degradação detectada em < 10 min

**Entregável**: Features que o Dynatrace não tem, comunicadas claramente em marketing.

---

### Fase 4 — Escala e Enterprise (ongoing)
*Para conquistar contas Enterprise e crescimento de LTV.*

- [ ] **AIOps / Anomaly Detection**: baseline automático por serviço, alertas preditivos
- [ ] **Synthetic Monitoring**: scripts de teste executados de múltiplas regiões
- [ ] **Session Replay**: gravação de sessão de usuário para debugging UX
- [ ] **Mobile SDK**: iOS e Android com OTel trace propagation
- [ ] **SSO Custom Domain**: tenant.empresa.com com certificado próprio
- [ ] **Data Residency**: escolha de região para armazenamento dos dados
- [ ] **Audit Export API**: exportação de logs de auditoria para SIEM do cliente
- [ ] **SCIM Provisioning**: sync automático de usuários via Azure AD / Okta

---

## 3. Gaps vs Dynatrace (Análise Honesta)

| Feature | Dynatrace | NexTraceOne hoje | Plano |
|---|---|---|---|
| Auto-discovery de serviços | ✅ OneAgent | 🟡 Parcial (k8s_observer) | Fase 1 |
| Topology Map | ✅ Smartscape | ❌ Não existe | Fase 2 |
| Davis AI (anomaly detection) | ✅ | ❌ | Fase 4 |
| RUM (Real User Monitoring) | ✅ | ❌ | Fase 2 |
| Synthetic Monitoring | ✅ | ❌ | Fase 4 |
| Session Replay | ✅ | ❌ | Fase 4 |
| Mobile SDK | ✅ | ❌ | Fase 4 |
| Log Management | ✅ | 🟡 Backend OK, UI básica | Fase 2 |
| Infrastructure Monitoring | ✅ | 🟡 Config OK, UI básica | Fase 2 |
| Service Contract Governance | ❌ | ✅ **Diferencial** | — |
| AI Governance (Customer) | ❌ | ✅ **Diferencial** | — |
| Multi-tenant Hierarchy | ❌ | ✅ **Diferencial** | — |
| Self-hosted real | ❌ | ✅ | — |
| Preço previsível (não por GB) | 🟡 Por HU | ✅ Por HU | — |
| Open-source core | ❌ | ✅ (OTel) | — |

---

## 4. Métricas de Sucesso por Fase

| Fase | Métrica | Target |
|---|---|---|
| 0 | Tenants self-service onboarded | 10 beta customers |
| 1 | Agents ativos em produção | 50 hosts monitorados |
| 2 | Churn < 5%/mês | NPS > 40 |
| 3 | Feature adotada pelos clientes (AI Gov) | 30% dos tenants Professional |
| 4 | ARR | €500K |

---

## 5. Decisões Técnicas Pendentes

| Decisão | Opções | Impacto |
|---|---|---|
| Billing provider | Stripe vs Lago vs manual | Fase 0 |
| Agent auto-update | OpAMP pull vs S3 presigned vs GitHub Releases | Fase 1 |
| Topology storage | Neo4j vs PostgreSQL graph queries vs Redis | Fase 2 |
| Anomaly detection | Custom ML vs OpenAI vs Amazon Lookout | Fase 4 |
| Multi-region data | Single region agora vs multi-region desde o início | Fase 3 |
| Log retention pricing | Incluído no HU vs tier separado por GB | Fase 0 |

---

*Ver também: `SAAS-STRATEGY.md`, `NEXTTRACE-AGENT.md`, `SAAS-LICENSING.md`, `HONEST-GAPS.md`*
