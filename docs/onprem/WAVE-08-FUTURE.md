# Wave 8 — Evoluções Futuras & Roadmap On-Prem

> **Prioridade:** Roadmap (médio/longo prazo)
> **Esforço estimado:** XL
> **Referência:** [INDEX.md](./INDEX.md)

---

## Contexto

Esta wave consolida evoluções de maior prazo que requerem decisões de arquitectura
mais profundas, dependências externas ou maturidade dos módulos actuais.
Não são gaps bloqueantes — são diferenciadores estratégicos.

---

## W8-01 — Capacity Planning & Infrastructure Forecasting

### Problema
A equipa de infra não sabe quando vai precisar de mais hardware. A decisão é tomada
em modo reactivo (quando o disco enche ou a CPU está a 100%).

### Solução
Motor de previsão de capacidade baseado em tendências reais:

**Análise automática:**
```
Previsão de Capacidade — Próximos 90 dias
├── Disco Elasticsearch
│   Crescimento: +12 GB/semana (últimas 8 semanas)
│   Actual: 148 GB / 500 GB
│   Estimativa disco cheio: 29 de Julho de 2026
│   Recomendação: expandir capacidade antes de 15 de Julho
│
├── PostgreSQL
│   Crescimento: +2.1 GB/semana
│   Actual: 24.4 GB / 100 GB
│   Estimativa disco cheio: 6 de Outubro de 2026
│
└── RAM do processo (tendência)
    Crescimento: +50 MB/mês (memory leak suspeito?)
    Recomendação: investigar heap do ApiHost
```

**Inputs para previsão:**
- Série temporal de uso de disco, RAM, CPU (últimas 8 semanas)
- Número de serviços, mudanças e incidentes (crescimento do produto)
- Sazonalidade detectada (ex: pico às 2ª feiras pós-weekend)

### Critério de aceite
- [ ] Previsão disponível para disco, RAM e CPU
- [ ] Alerta proactivo quando data estimada < 60 dias
- [ ] Gráfico de tendência histórica + projecção futura
- [ ] Recomendações exportáveis para plano de aquisição de hardware

---

## W8-02 — Kubernetes Deployment (Helm Charts)

### Problema
O Docker Compose cobre POC e instalações simples. Para ambientes enterprise
com alta disponibilidade, o Kubernetes é o standard.

### Solução
Helm charts para deployment em Kubernetes on-prem (k3s, Rancher, OpenShift):

```
nextraceone/
├── Chart.yaml
├── values.yaml               ← configuração centralizada
├── templates/
│   ├── apihost-deployment.yaml
│   ├── workers-deployment.yaml
│   ├── ingestion-deployment.yaml
│   ├── postgres-statefulset.yaml  (opcional — pode usar BD externa)
│   ├── elasticsearch-statefulset.yaml
│   ├── ollama-deployment.yaml
│   ├── hpa.yaml               ← autoscaling horizontal para ApiHost
│   ├── pdb.yaml               ← pod disruption budget
│   └── ingress.yaml
└── README.md
```

**Funcionalidades:**
- HA para ApiHost (mínimo 2 réplicas)
- BackgroundWorkers com leader election (apenas 1 instância activa)
- Persistent Volumes para PostgreSQL e Elasticsearch
- Health probes configuradas (`/health`, `/ready`, `/live`)
- Resource requests e limits definidos

### Critério de aceite
- [ ] `helm install nextraceone nextraceone/nextraceone` funciona com valores padrão
- [ ] Suporte a k3s (lightweight Kubernetes para on-prem)
- [ ] PodDisruptionBudget garante disponibilidade durante upgrades
- [ ] Upgrade sem downtime: `helm upgrade nextraceone nextraceone/nextraceone`
- [ ] Testado em k3s + Rancher + vanilla Kubernetes

---

## W8-03 — Agentic SRE — Investigação Autónoma de Incidentes

### Problema
Os AI Agents existentes são descritivos — fornecem informação mas não executam
acções autonomamente. Em 2026, plataformas líderes (PagerDuty, Algomox) têm
agentes que investigam, propõem e executam remediações com aprovação humana.

### Solução
**Agente de Investigação de Incidentes:**

```
Incidente criado: "payment-api — latência > 2s"

Agente (automaticamente, em 30s):
1. Correlacionar com mudanças recentes
   → Encontrado: deploy de payment-api v2.3.1 há 45 minutos
2. Analisar telemetria no período
   → CPU: +180% após deploy | DB queries: +340% | Error rate: 2.3%
3. Verificar diff de código (se integração CI/CD disponível)
   → PR #892: adicionada query N+1 em OrderService
4. Consultar runbooks relacionados
   → Runbook "High DB Load — payment-api" encontrado
5. Propor acção:
   → "Rollback para v2.3.0 resolve o problema com 95% de confiança"
   → [Aprovar rollback] [Ver evidências] [Ignorar sugestão]
```

**Guardrails obrigatórios:**
- Toda acção executável requer aprovação humana explícita
- Acções auditadas com rastreabilidade completa
- Agente nunca acede a sistemas externos sem autorização configurada
- Escopo do agente limitado por políticas do AI Governance existente

### Critério de aceite
- [ ] Agente activa automaticamente quando incidente de severidade ≥ High é criado
- [ ] Investigação completa em < 60 segundos
- [ ] Proposta de acção com justificação e evidências
- [ ] Human-in-the-loop obrigatório para acções destrutivas
- [ ] Auditado no módulo AI Audit existente

---

## W8-04 — SAML 2.0 SSO

### Problema
Grandes empresas usam ADFS, Okta, Ping Identity ou Azure AD para SSO corporativo.
O suporte a SAML 2.0 é obrigatório em muitos processos de procurement enterprise.
As entidades de domínio já existem — faltam os protocol handlers.

### Solução
Implementação de SAML 2.0 SP (Service Provider):

```
Fluxos suportados:
├── SP-initiated SSO (utilizador vai ao NexTraceOne, redirecciona para IdP)
├── IdP-initiated SSO (utilizador já autenticado no portal corporativo)
└── SLO (Single Logout — terminar sessão em todos os SPs)

Atributos mapeáveis:
├── email → User.Email
├── name → User.DisplayName
├── groups → Roles (configurável por admin)
└── department → Team (opcional)
```

### Critério de aceite
- [ ] Funciona com ADFS 2019+, Okta, Ping Identity, Azure AD
- [ ] Mapeamento de grupos SAML para roles NexTraceOne configurável via UI
- [ ] Deep-link preservation após SSO (redirecção para página original)
- [ ] Fallback para login local quando IdP está indisponível

---

## W8-05 — SDK Externo & CLI Avançado

### Problema
A base do CLI já existe em `/tools/NexTraceOne.CLI`. Falta completar os comandos
e publicar um SDK para integração com scripts e pipelines externos.

### Solução
CLI completo + SDK .NET + SDK Python:

```bash
# CLI
ntrace service list --env production
ntrace service register --name payment-api --owner platform-team
ntrace change submit --service payment-api --env staging --type Deploy
ntrace blast-radius --change CHG-2024-001
ntrace contract validate --file openapi.yaml

# SDK .NET
var client = new NexTraceClient("https://nextraceone.acme.com", apiKey);
await client.Changes.SubmitAsync(new ChangeRequest { ... });

# SDK Python
client = NexTraceClient("https://nextraceone.acme.com", api_key=key)
client.changes.submit(service_id="payment-api", environment="staging")
```

### Critério de aceite
- [ ] CLI disponível como binário standalone para Linux e Windows
- [ ] Autenticação via API Key (sem browser)
- [ ] SDK .NET publicado como NuGet package
- [ ] Todos os comandos cobertos por testes de integração

---

## W8-06 — Compliance Packs (SOC2, LGPD/GDPR, ISO 27001)

### Problema
Em 2026, compliance é uma preocupação central em enterprise. O módulo de
Governance existe mas não tem packs de compliance prontos a usar.

### Solução
Compliance Packs configuráveis por tenant:

```
Pack SOC2 Type II — Controls
├── CC6.1: Logical access security (verificar RBAC configurado)
├── CC6.7: Transmission encryption (verificar HTTPS activo)
├── CC7.2: System monitoring (verificar alertas configurados)
├── CC8.1: Change management (verificar approval workflow activo)
└── A1.2: Availability monitoring (verificar SLOs definidos)

Estado: 4/5 controls passam | 1 control em falta
  → CC7.2: nenhum alerta configurado para serviço payment-api
  → Acção: configurar SLO alert para payment-api
```

**Evidence Collector Automático:**
- Para cada control, recolher evidência automaticamente dos módulos existentes
- Exportar Evidence Pack em formato auditável (PDF/ZIP)
- Integrar com o Evidence Pack do ChangeGovernance existente

### Critério de aceite
- [ ] Packs SOC2, LGPD/GDPR e ISO 27001 incluídos por padrão
- [ ] Controls mapeados a módulos do NexTraceOne com verificação automática
- [ ] Evidence collection automática por período
- [ ] Export em formato aceite por auditores (PDF com hash SHA-256)
- [ ] Pack Builder para controles customizados

---

## Sumário de Priorização Global

```
╔══════════════════════════════════════════════════════════════════╗
║           ROADMAP COMPLETO ON-PREM — PRIORIZAÇÃO                ║
╠══════════════════════════════════════════════════════════════════╣
║  CRÍTICO (implementar já)                                        ║
║  W1-01 Preflight Check Engine                                    ║
║  W1-02 Setup Wizard First-Run                                    ║
║  W1-03 Configuration Validator                                   ║
╠══════════════════════════════════════════════════════════════════╣
║  ALTA (próxima iteração)                                         ║
║  W2-01 Admin Health Dashboard                                    ║
║  W2-04 Support Bundle Generator                                  ║
║  W3-01 Migration Preview API                                     ║
║  W3-02 Offline Release Bundle                                    ║
║  W3-03 Backup Coordinator                                        ║
║  W4-01 Model Manager UI                                          ║
║  W4-02 LLM Hardware Advisor                                      ║
║  W5-01 Network Isolation Mode (Air-Gap)                          ║
║  W5-02 Proxy Corporativo & Internal CA                           ║
╠══════════════════════════════════════════════════════════════════╣
║  MÉDIA (segunda wave)                                            ║
║  W2-02 Startup Report                                            ║
║  W2-03 Auto-Diagnóstico Proactivo                                ║
║  W3-05 Graceful Shutdown                                         ║
║  W4-03 AI Resource Governor                                      ║
║  W4-04 AI Governance / Hallucination Detection                   ║
║  W5-03 Audit de Chamadas Externas                                ║
║  W5-05 Fine-Grained Authorization por Ambiente                   ║
║  W6-01 Waste Detection Engine                                    ║
║  W6-03 Resource Budget por Tenant                                ║
║  W7-01 Elasticsearch Index Manager                               ║
║  W7-03 PostgreSQL Health Dashboard                               ║
║  W7-04 DORA Metrics Dashboard                                    ║
╠══════════════════════════════════════════════════════════════════╣
║  ROADMAP (futuro)                                                ║
║  W6-04 GreenOps / Carbon Score                                   ║
║  W7-02 Lightweight Mode                                          ║
║  W8-01 Capacity Planning & Forecasting                           ║
║  W8-02 Kubernetes Helm Charts                                    ║
║  W8-03 Agentic SRE Autónomo                                      ║
║  W8-04 SAML 2.0 SSO                                              ║
║  W8-05 SDK Externo & CLI Avançado                                ║
║  W8-06 Compliance Packs (SOC2/LGPD/ISO27001)                    ║
╚══════════════════════════════════════════════════════════════════╝
```

---

## Referências de Mercado

- PagerDuty Agentic SRE (2026): investigação + remediação autónoma
- Replicated: distribuição self-hosted com Helm + air-gap support
- k3s (Rancher): Kubernetes lightweight para on-prem sem cloud
- Drata / Vanta: compliance automation com evidence collection
- DORA Research Program (2026): métricas de elite engineering teams
- Green Software Foundation SOFT Framework (Nov 2025)
