# NexTraceOne SaaS Strategy

> **Posicionamento**: "A plataforma de governança operacional que o Dynatrace não é, com observabilidade que o Dynatrace tem."

---

## 1. Proposta de Valor

O Dynatrace é excelente em observabilidade técnica mas fraco em governança operacional e AI Governance. O NexTraceOne preenche esse gap posicionando observabilidade como meio, não como fim.

| Dimensão | Dynatrace | Datadog | **NexTraceOne** |
|---|---|---|---|
| APM / Tracing | ✅ | ✅ | ✅ |
| Infraestrutura | ✅ | ✅ | ✅ (via NexTrace Agent) |
| AI Governance (Customer) | ❌ | Parcial | ✅ **Diferencial** |
| Service Contract Governance | ❌ | ❌ | ✅ **Diferencial** |
| Operational Intelligence | Parcial | Parcial | ✅ **Diferencial** |
| Multi-tenant Hierarchy | ❌ | ❌ | ✅ (Org/Holding/Subsidiary) |
| Self-hosted | ❌ | ❌ | ✅ |
| Preço | Alto | Alto | Competitivo |

---

## 2. Modelos de Distribuição

### 2.1 SaaS (Cloud-hosted)

- NexTraceOne opera e mantém toda a infraestrutura
- Cliente instala apenas o **NexTrace Agent** nos hosts monitorados
- Dados trafegam via HTTPS/OTLP para o endpoint gerenciado (`https://ingest.nextraceone.io`)
- Multi-tenancy por PostgreSQL RLS + tenant slug no JWT
- Faturamento automático baseado em **Host Units** (ver `SAAS-LICENSING.md`)

### 2.2 Self-hosted / On-Premise

- Cliente opera o NexTraceOne no ambiente próprio (Kubernetes, VMs, IIS)
- NexTrace Agent aponta para endpoint interno do cliente
- Licença perpétua ou por assinatura anual com suporte
- `DeploymentModel = SelfHosted | OnPremise` (ver `DeploymentModel.cs`)
- Todas as funcionalidades disponíveis, incluindo configuração de AI providers próprios

---

## 3. Feature Matrix: SaaS vs Self-hosted

### 3.1 Funcionalidades Expostas em Ambos os Modelos

| Feature | SaaS | Self-hosted |
|---|---|---|
| APM (traces, logs, métricas) | ✅ | ✅ |
| NexTrace Agent | ✅ | ✅ |
| Infraestrutura Monitoring | ✅ | ✅ |
| Operational Intelligence | ✅ | ✅ |
| Multi-tenant Hierarchy | ✅ | ✅ |
| Service Contract Governance | ✅ | ✅ |
| **Customer AI Governance** | ✅ Premium | ✅ |
| AI Budget (ExternalAI) | ✅ Premium | ✅ |
| AI Access Policy | ✅ Premium | ✅ |
| Alerting / SLOs | ✅ | ✅ |
| Integrações (Slack, PagerDuty) | ✅ | ✅ |
| Change Confidence | ✅ | ✅ |

### 3.2 Funcionalidades OCULTAS no SaaS (Infraestrutura Interna)

Estas funcionalidades **existem na plataforma** mas são geridas internamente pela equipa NexTraceOne — o cliente SaaS nunca as vê:

| Feature | Razão de Ocultar |
|---|---|
| **Model Registry** (`ModelRegistry`) | O cliente usa AI via abstração — não precisa saber qual model é usado internamente |
| **IAiProviderFactory** (configuração de providers) | Gerido pela NexTraceOne Cloud Operations |
| **IAiTokenQuotaService** (platform quota) | Quota interna da plataforma, não do cliente |
| **Configuração de OTel Collector** (infra) | Operado pela NexTraceOne, transparente ao cliente |
| **Elasticsearch / ClickHouse provisioning** | Backend de storage é decisão da NexTraceOne |
| **TenantRlsInterceptor** (database) | Isolamento automático, invisível ao cliente |

### 3.3 AI Governance: A Distinção Crítica

```
Platform AI (INTERNO - SaaS)          Customer AI Governance (EXPOSTO)
─────────────────────────────          ────────────────────────────────
IAiProviderFactory                     ExternalAiIntegration
  ↓ configura                            ↓ monitora
ModelRegistry                          AIBudget (por cliente/equipa)
  ↓ serve                                AIAccessPolicy (quem usa o quê)
IAiTokenQuotaService                   AiAgentGovernance (auditoria)
  ↓ controla custo interno               AIUsageReport (chargeback)

NUNCA exposto ao cliente SaaS          EXPOSTO como feature Premium
```

**Analogia**: O Dynatrace usa AI internamente (Davis AI) mas o cliente não configura o Davis — ele simplesmente usa os resultados. O NexTraceOne vai além: oferece ao cliente ferramentas de **governança** da AI que o cliente usa nos seus próprios sistemas.

---

## 4. Arquitetura Multi-tenant SaaS

```
Cliente A (tenant-a.nextraceone.io)
    │
    ├── NexTrace Agent (host-1, host-2)  ──OTLP/HTTPS──►  Ingestion API
    │                                                           │
    └── Browser → Web App                                       │
                      │                                         ▼
                      └─── API Gateway ──► Modules         OTel Collector
                                              │             (NexTraceOne)
                                              │                 │
                                         PostgreSQL RLS      Elasticsearch
                                         (tenant isolation)  / ClickHouse

Cliente B (tenant-b.nextraceone.io)  ──────────────────────────┘
    Completamente isolado via RLS + JWT tenant_id
```

### Fluxo de Autenticação SaaS

1. Usuário autentica via SSO/OIDC → JWT com `tenant_id` + `capabilities` (plano)
2. `TenantResolutionMiddleware` lê JWT, chama `CurrentTenantAccessor.Set()` com capabilities
3. `HasCapability("ai-governance")` retorna `true` apenas para planos Professional+
4. `TenantRlsInterceptor` injeta `app.current_tenant_id` em toda query PostgreSQL

---

## 5. Plano de Monetização

### Planos SaaS

| Plano | Host Units incluídas | AI Governance | Price/Host Unit/mês |
|---|---|---|---|
| **Starter** | 5 HU | ❌ | €15 |
| **Professional** | 20 HU | ✅ | €22 |
| **Enterprise** | Ilimitado | ✅ + SLA | Negociado |

### Planos Self-hosted

| Tipo | Modelo | Inclui |
|---|---|---|
| **Team** | Anual por HU | Suporte email |
| **Business** | Anual por HU | Suporte 8x5 + SLA 4h |
| **Enterprise** | Perpétua + manutenção | Suporte 24x7 + SLA 1h + onboarding |

---

## 6. Estratégia de Go-to-Market

### Segmentos Prioritários

1. **Empresas Mid-market (200-2000 colaboradores)** que usam Dynatrace e acham caro
2. **Scale-ups tech** com IA própria que precisam de AI Governance
3. **Grupos empresariais** com subsidiárias que precisam de multi-tenant real
4. **Integradores/MSPs** que querem oferecer observabilidade como serviço (via hierarchy)

### Mensagem de Vendas por Segmento

**vs. Dynatrace**: "Pagou mais barato, tem observabilidade igual — e ainda ganha governança de contratos e AI que o Dynatrace não tem."

**vs. Datadog**: "Preço previsível por Host Unit (não por ingestão de logs), com AI Governance nativa."

**vs. Prometheus + Grafana**: "Stack completa sem 6 meses de configuração. Suporte. SLA. Governança."

---

## 7. Implementação: O que Mudar no Código

### Prioridade 1 — Capabilities no JWT (sem isso nada funciona)

```csharp
// JwtTokenGenerator.cs — adicionar capabilities ao token
var capabilities = await _tenantLicenseService.GetCapabilitiesAsync(tenantId);
claims.Add(new Claim("capabilities", JsonSerializer.Serialize(capabilities)));
```

```csharp
// TenantResolutionMiddleware.cs — ler capabilities do JWT
var capabilitiesClaim = principal.FindFirst("capabilities");
var capabilities = JsonSerializer.Deserialize<string[]>(capabilitiesClaim?.Value ?? "[]");
_currentTenantAccessor.Set(tenantId, slug, name, isActive, capabilities);
```

### Prioridade 2 — Gates nas Features Premium

```csharp
// Em qualquer CommandHandler ou Endpoint premium:
if (!_currentTenant.HasCapability("ai-governance"))
    throw new ForbiddenException("AI Governance requires Professional plan or higher.");
```

### Prioridade 3 — Endpoint de Ingestion Público

```
https://ingest.nextraceone.io/v1/traces   (OTLP HTTP)
https://ingest.nextraceone.io/v1/metrics  (OTLP HTTP)
https://ingest.nextraceone.io/v1/logs     (OTLP HTTP)
```

Auth via API Key (já implementado em `IngestionApiSecurity.cs`) com escopo `integrations:write`.

### Prioridade 4 — Tenant Onboarding Automatizado

```csharp
// TenantProvisioningService.cs (a criar)
public async Task ProvisionTenantAsync(CreateTenantSaaSCommand command)
{
    var tenant = Tenant.Create(command.Name, command.Slug, DeploymentModel.SaaS);
    var license = TenantLicense.CreateStarter(tenant.Id);
    var apiKey = ApiKey.Generate(tenant.Id, "default-ingestion");
    // Enviar email com: endpoint OTLP + API Key + link para docs do NexTrace Agent
}
```

---

## 8. Diferenciadores Defensáveis

1. **Multi-tenant Hierarchy real**: Holding → Subsidiária → Departamento em 3 níveis — Dynatrace não tem
2. **Service Contract Governance**: SLAs contratuais monitorados, não apenas técnicos
3. **Customer AI Governance**: Audit trail, budget e access policy para AI usada pelos clientes
4. **Self-hosted real**: Mesmo binário, mesmas features, sem feature degradation artificial
5. **Preço previsível**: Host Units fixos (não por GB de log ingerido como Datadog)
6. **NexTrace Agent open-source core**: Baseado em OTel Collector — cliente pode auditar e contribuir

---

*Ver também: `NEXTTRACE-AGENT.md`, `SAAS-LICENSING.md`, `SAAS-ROADMAP.md`*
