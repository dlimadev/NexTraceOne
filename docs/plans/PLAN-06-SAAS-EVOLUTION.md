# Plano 06 — SaaS Evolution

> **Prioridade:** Roadmap  
> **Esforço total:** 12–20 semanas  
> **Spec técnica:** [SAAS-ROADMAP.md](../SAAS-ROADMAP.md) · [SAAS-STRATEGY.md](../SAAS-STRATEGY.md) · [SAAS-LICENSING.md](../SAAS-LICENSING.md) · [NEXTTRACE-AGENT.md](../NEXTTRACE-AGENT.md)  
> **Posicionamento:** "A plataforma de governança operacional que o Dynatrace não é, com observabilidade que o Dynatrace tem."  
> **Nota:** Este grupo endereça o modelo SaaS. O core produto v1.0.0 é READY; estes itens são necessários para operação comercial multi-tenant gerida.
> **Estado (Maio 2026):** SaaS-01 IMPLEMENTADO | SaaS-02 A VERIFICAR | SaaS-03 IMPLEMENTADO | SaaS-04 IMPLEMENTADO | SaaS-05 A VERIFICAR | SaaS-06 NAO IMPLEMENTADO | SaaS-07 A VERIFICAR | SaaS-08 IMPLEMENTADO

---

## SaaS-01 — Fix HasCapability() Hook (P0 Bug)

**Problema:** `HasCapability()` nunca retorna capabilities populadas — todos os gates de licença são bypassados.  
**Impacto:** Clientes Starter acedem a features Premium sem controlo.

**Implementação:**
1. Identificar onde `HasCapability()` é chamado (grep em `src/`)
2. Popular `TenantCapabilities` no JWT durante login via `ICapabilityResolver`
3. `ICapabilityResolver` consulta plano do tenant (Starter/Professional/Enterprise) e retorna capabilities
4. Capabilities por plano (ver `SAAS-LICENSING.md`):
   - Starter: `["apm", "infra", "service_catalog", "change_governance_basic"]`
   - Professional: Starter + `["ai_governance", "contract_studio", "compliance_basic", "finops"]`
   - Enterprise: tudo + `["compliance_advanced", "multi_region", "air_gapped", "custom_agents"]`
5. Gates de feature: verificar capability antes de retornar dados ou executar commands

**Esforço:** 3–4 dias

---

## SaaS-02 — NexTrace Agent Binário Distribuível

**Referência:** [NEXTTRACE-AGENT.md](../NEXTTRACE-AGENT.md)  
**Objetivo:** Build de agente distribuível baseado em OTel Collector Builder (`ocb`).

### SaaS-02.1 — Builder Config

```yaml
# build/nexttrace-agent/builder-config.yaml
dist:
  name: nexttrace-agent
  description: NexTrace Observability Agent
  version: 1.0.0
  output_path: ./dist

exporters:
  - gomod: github.com/nextraceone/nextraceone-exporter v1.0.0

processors:
  - gomod: github.com/nextraceone/nextraceone-processor v1.0.0

extensions:
  - gomod: github.com/nextraceone/nextraceone-configurator v1.0.0
  # OpAMP extension
  - gomod: github.com/open-telemetry/opentelemetry-collector-contrib/extension/opampextension v0.120.0

receivers:
  - gomod: go.opentelemetry.io/collector/receiver/otlpreceiver v0.120.0
  - gomod: github.com/open-telemetry/opentelemetry-collector-contrib/receiver/hostmetricsreceiver v0.120.0
  - gomod: github.com/open-telemetry/opentelemetry-collector-contrib/receiver/k8sclusterreceiver v0.120.0
```

### SaaS-02.2 — Componentes Customizados (3 packages Go)

**`nextraceexporter`:**
- Envia OTLP para `NEXTTRACE_INGEST_ENDPOINT`
- Injeta `Authorization: ApiKey <key>` em todos os requests
- Disk-backed queue (1GB máximo) com retry exponential backoff
- Compressão gzip por padrão

**`nextraceprocessor`:**
- Enriquece com atributos: `nextraceone.agent_version`, `nextraceone.deployment_mode`, `nextraceone.host_unit_id`
- `host_unit_id`: UUID estável persistido em `~/.nexttrace/host_unit_id`

**`nextraceconfigurator` (OpAMP extension):**
- Conecta a `NEXTTRACE_CONTROL_ENDPOINT` via WebSocket
- Recebe config remota sem restart (via OpAMP)
- Reporta health e métricas do agente ao servidor

### SaaS-02.3 — Build Pipeline

```makefile
# Makefile
build-agent:
	cd build/nexttrace-agent && ocb --config=builder-config.yaml
	
package-agent-linux:
	tar -czf dist/nexttrace-agent-linux-amd64.tar.gz -C build/nexttrace-agent/dist/ nexttrace-agent

package-agent-windows:
	zip dist/nexttrace-agent-windows-amd64.zip build/nexttrace-agent/dist/nexttrace-agent.exe
```

**Esforço:** 3–4 semanas (incluindo testes de integração com servidor)

---

## SaaS-03 — Agent Heartbeat + Host Unit Tracking

**Objetivo:** Contar Host Units para billing.

**Implementação:**
1. Entidade `AgentRegistration` em `IdentityAccess.Domain`:
   - `HostUnitId` (UUID do agente), `HostName`, `CpuCores`, `RamGb`, `AgentVersion`
   - `HostUnits` calculado: `max(RamGb/8, CpuCores/4)` arredondado para 0.5 mais próximo
   - `LastHeartbeatAt`, `Status` (Active/Inactive/Decommissioned)
2. Endpoint `POST /api/v1/agent/heartbeat` (Ingestion API, auth via API Key):
   - Payload: `host_unit_id`, `hostname`, `cpu_cores`, `ram_gb`, `agent_version`, `deployment_mode`
   - Upsert de `AgentRegistration` por (`TenantId`, `HostUnitId`)
3. `HostUnitCountJob` (Quartz, horário): recalcula `TenantLicense.CurrentHostUnits` por tenant
4. Alerta de quota: notificação quando `CurrentHostUnits > Plan.IncludedHostUnits`
5. Migration: tabela `iam_agent_registrations`

**Esforço:** 1 semana

---

## SaaS-04 — Licensing Core

**Nota:** OOS-01 em `HONEST-GAPS.md` removeu licensing anti-tampering para v1.0.0. Este item implementa licensing **simplificado** para SaaS (sem anti-tampering local, sem hardware fingerprinting).

**Implementação:**
1. Entidade `TenantLicense` em `IdentityAccess.Domain`:
   - `Plan` (Starter/Professional/Enterprise), `IncludedHostUnits`, `CurrentHostUnits`
   - `ValidFrom`, `ValidUntil`, `Status` (Active/Suspended/Expired/Trial)
   - `BillingCycleStart`, `OverageHostUnits` (além do plano)
2. `LicenseRecalculationJob` (Quartz, horário): atualiza `CurrentHostUnits` a partir de `AgentRegistration`
3. `ILicenseService.GetCurrentPlan(tenantId)`: retorna plano e capabilities ativas
4. Integração com `HasCapability()` (SaaS-01)
5. Migration: tabela `iam_tenant_licenses`

**Esforço:** 1 semana

---

## SaaS-05 — Tenant Provisioning Automatizado

**Problema:** Onboarding manual não escala. Cada novo tenant requer intervenção de administrador.

**Implementação:**
1. `TenantProvisioningJob` command:
   - Cria tenant com slug único
   - Seed de configurações padrão via `ConfigurationDefinitionSeeder`
   - Cria utilizador admin inicial com convite por email (ou senha temporária)
   - Cria `TenantLicense` com plano selecionado
   - Cria `IngestApiKey` para uso do NexTrace Agent
2. `TenantProvisioningPage.tsx` (Platform Admin): wizard de criação de novo tenant
3. Webhook de provisioning: `POST /webhooks/tenant-provisioned` para integração com sistemas externos
4. Rollback: se qualquer step falhar, desfaz todos os anteriores (saga pattern)

**Esforço:** 1.5 semanas

---

## SaaS-06 — Onboarding Wizard (UI)

**Objetivo:** Wizard guiado para novos tenants instalarem o NexTrace Agent e registarem o primeiro serviço.

**Passos do Wizard (5 steps):**
1. **Install Agent**: instruções de instalação por plataforma (Linux/Windows/K8s/Docker) com cópia de configuração + API key
2. **First Signal**: aguarda primeiro heartbeat do agente (polling 30s, timeout 10min)
3. **Register Service**: formulário simplificado para registar primeiro serviço
4. **Add Contract**: opção de importar contrato OpenAPI ou criar manualmente
5. **Setup SLO**: definir SLO básico (availability ≥ 99.9%)

**Implementação:**
- `OnboardingWizardPage.tsx` (`/onboarding`)
- `OnboardingProgressTracker` entity: persiste progresso do wizard por tenant
- Pode ser ignorado/retomado a qualquer momento

**Esforço:** 1.5 semanas

---

## SaaS-07 — Log Search UI (Kibana-like)

**Objetivo:** Interface de pesquisa de logs sem sair do NexTraceOne.

**Implementação:**
1. `LogSearchPage.tsx` (`/observability/logs`):
   - Query bar com syntax highlighting (Lucene/KQL simplificado)
   - Time picker (last 15min/1h/6h/24h/7d/custom)
   - Log stream em tempo real via SignalR
   - Filtros por `service.name`, `severity`, `environment`
   - Expandir log entry para ver todos os atributos
2. Backend: `SearchLogs` query usa `IElasticQueryClient` ou `IClickHouseAnalyticsReader`
3. Export: download de resultados como CSV/JSON

**Esforço:** 2 semanas

---

## SaaS-08 — Alerting Engine Completo

**Estado atual:** `UserAlertRule` entity existe; falta motor de avaliação contínuo.

**Implementação:**
1. `AlertEvaluationJob` (Quartz, a cada 1min): avalia todas as `UserAlertRule` ativas por tenant
2. `AlertConditionEvaluator`: avalia condição (threshold, anomaly, trend) contra dados reais
3. Canais de notificação: email, Slack (via webhook), PagerDuty, in-app
4. `AlertFiringRecord` entity: registo de quando alerta disparou/resolveu
5. Silencing: suprimir alerta por período (ex: durante maintenance window)
6. `AlertsPage.tsx`: gestão de regras + histórico de disparos

**Esforço:** 2 semanas

---

## Matriz de Prioridades SaaS

| ID | Item | Prioridade | Esforço | Dependência |
|----|------|------------|---------|-------------|
| SaaS-01 | Fix HasCapability() | P0 | 3–4 dias | — |
| SaaS-03 | Agent Heartbeat | P0 | 1 semana | NexTrace Agent |
| SaaS-04 | Licensing Core | P0 | 1 semana | SaaS-03 |
| SaaS-02 | NexTrace Agent Binário | P0 | 3–4 semanas | — |
| SaaS-05 | Tenant Provisioning | P1 | 1.5 semanas | SaaS-04 |
| SaaS-06 | Onboarding Wizard | P1 | 1.5 semanas | SaaS-02, SaaS-05 |
| SaaS-07 | Log Search UI | P1 | 2 semanas | Fase 2 Infra (CH) |
| SaaS-08 | Alerting Engine | P1 | 2 semanas | — |

## Critérios de Aceite (estado Maio 2026)

- [x] `HasCapability("ai_governance")` retorna `false` para tenant Starter — implementado via `ICapabilityResolver` e JWT claims *(SaaS-01 implementado)*
- [ ] NexTrace Agent instalável em Linux x86_64 com 1 comando e a enviar dados em 5min *(SaaS-02 — a verificar)*
- [ ] Novo tenant provisionado automaticamente (0 intervenção manual) em < 2min *(SaaS-05 — a verificar)*
- [ ] Log search retorna resultados em < 2s para queries simples *(SaaS-07 — a verificar)*
- [x] Alerta de SLO breach dispara em < 2min após evento — `AlertEvaluationJob` implementado *(SaaS-08 implementado)*

**Itens confirmados implementados:** `AgentRegistration` entity + heartbeat endpoint (SaaS-03),
`TenantLicense` entity + `LicenseRecalculationJob` (SaaS-04), `AlertEvaluationJob` (SaaS-08).
**Pendente:** `OnboardingWizard` SaaS-06 não implementado.
