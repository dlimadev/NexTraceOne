# NexTraceOne — Honest Gaps

> **Última atualização:** Abril 2026
> **Propósito:** Registo único e honesto de toda a dívida declarada, degradação graciosa (`simulatedNote`), decisões de "out-of-scope" e providers opcionais cujo enforcement fica dependente de configuração externa.
>
> Este ficheiro é **fonte da verdade** para responder "isto está implementado?" quando o `IMPLEMENTATION-STATUS.md` classifica algo como `READY com notas`, `PARTIAL` ou `SIM`.

---

## Como ler este documento

Cada entrada segue o padrão:

- **ID** — referência estável para citação em issues/PRs.
- **Tipo** — uma de:
  - 🟢 **Por-design** — comportamento correto mas que parece "faltar" à primeira vista.
  - 🟡 **Degradação graciosa** — feature real, mas devolve dados simulados (`simulatedNote`) enquanto provider externo não está configurado.
  - 🔴 **Dívida aberta** — gap que deve ser fechado antes de `v1.0.0`.
  - ⚫ **Out-of-scope** — removido ou nunca será implementado; razão registada.
- **Como configurar / fechar** — passo concreto.

---

## ⚫ Out-of-scope (decisões de produto)

| ID | Item | Razão | Alternativa real |
|---|---|---|---|
| OOS-01 | **Product Licensing** (license online/offline, heartbeat, entitlements, anti-tampering) | Removido do produto. `Licensing` module não consta no código-base nem no `FUTURE-ROADMAP.md` (linha 249 do roadmap confirma). | Entitlements são geridos via contrato comercial + feature-flags em `Configuration`. |
| OOS-02 | **Convites in-app** (`POST /invitations/accept`, `GET /invitations/{token}`) | Produto é **SSO-first**. Provisionamento via IdP corporativo (OIDC/SAML + JIT/SCIM quando aplicável). Endpoints, página FE e i18n foram removidos em Abril/2026. | `StartOidcLogin` + `StartSamlLogin` já cobrem o onboarding de utilizadores. |
| OOS-03 | **TanStack Router** | Documentação antiga mencionava TanStack Router; o frontend usa **`react-router-dom` v7** desde sempre. `docs/FRONTEND-ARCHITECTURE.md` já está correto. | N/A — sem ação. |

## 🟢 Stubs controlados legítimos (comportamento por design)

| ID | Feature | Porque é stub | Porque está correto assim |
|---|---|---|---|
| DES-01 | `ResendMfaCode` handler | TOTP (RFC 6238) não tem noção de "reenvio" — o código é derivado do tempo. | Manter o endpoint devolve `success` vazio para UI previsível; qualquer tentativa de "reenviar" para TOTP seria incorreta. |
| DES-02 | `ResetPassword` / `ActivateAccount` | Produto é SSO-first (ver OOS-02). Senha local existe apenas como fallback. | Stubs retornam erro localizado indicando fluxo via SSO. Caminho real só será implementado se algum cliente exigir password local — até lá fica marcado como aqui. |

## 🟡 Degradação graciosa (providers opcionais)

Todos os itens abaixo retornam `simulatedNote` na UI enquanto o provider externo não está configurado, seguindo o padrão `NullXxxProvider` + `IConfigurationResolutionService` (convenção de configuração do repositório).

### Governance / Operations

| ID | Feature | Provider necessário | Chave de config | `Null*` impl |
|---|---|---|---|---|
| DEG-01 | Canary analysis (histogramas, p99, error rate) | Ingestão de métricas do canary | `canary.provider.*` (Argo Rollouts / Flagger / custom) | `NullCanaryProvider` |
| DEG-02 | Backup / Disaster Recovery posture | Ligação ao backup system | `backup.provider.*` (Velero / cloud-native) | `NullBackupProvider` |
| DEG-03 | Runtime module intelligence | Agent de runtime (CLR profiler) | `runtime.provider.*` | `NullRuntimeProvider` |
| DEG-04 | Chaos experiments execution | Chaos engine | `chaos.provider.*` (Litmus / Chaos Mesh) | `NullChaosProvider` |
| DEG-05 | mTLS certificate manager | Cert manager | `mtls.provider.*` (cert-manager / Vault PKI) | simulado em handler |
| DEG-06 | Multi-tenant schema planner | IaC executor | `schema.provider.*` | simulado em handler |
| DEG-07 | Capacity forecast (HPA-aware) | Métricas de infra | `capacity.provider.*` | simulado em handler |
| DEG-08 | Feature flags runtime (read-through de provedor externo) | LaunchDarkly/Unleash/custom | `featureflags.external.*` | BD local é fonte por defeito |

### Integrations

| ID | Feature | Provider necessário | Chave de config | `Null*` impl |
|---|---|---|---|---|
| DEG-09 | Kafka event producer (real) | Cluster Kafka | `integrations.kafka.bootstrap` | `NullKafkaEventProducer` (padrão) / `ConfluentKafkaEventProducer` quando config presente |
| DEG-10 | Cloud billing ingestion | AWS CUR / Azure CM / GCP BQ | `billing.provider.*` | `NullCloudBillingProvider` |
| DEG-11 | SAML SSO | IdP SAML 2.0 | `identity.saml.*` | fluxo responde gracefully com metadata vazia enquanto não configurado |
| DEG-12 | External AI models (OpenAI/Anthropic/…) | API keys de vendors | `ai.external.*` | chamadas degradam para modelos internos |

### Observability

| ID | Feature | Provider necessário | Chave de config | Fallback |
|---|---|---|---|---|
| DEG-13 | Elasticsearch queries (logs/traces/metrics) | Cluster ES | `telemetry.elastic.*` | PostgreSQL Product Store como fallback parcial |
| DEG-14 | ClickHouse analytics | Cluster CH | `telemetry.clickhouse.*` | Elastic/PostgreSQL |
| DEG-15 | OpenTelemetry Collector | Collector a 4317/4318 | `OTEL_EXPORTER_OTLP_ENDPOINT` | exporters in-memory / console |

---

## 🟡 Auditoria do padrão `IsConfigured + Null*Provider` — CFG-02

> **Auditado em:** Abril 2026. Estado real do padrão de configuração em cada entrada DEG-01..15.
> Dois níveis de maturidade são esperados:
>
> - **Nível A — Pattern completo:** interface `IXxxProvider` com `IsConfigured`, implementação `NullXxxProvider` registada por defeito, implementação real registada condicionalmente, handlers leem `IsConfigured` e emitem `simulatedNote` quando falso.
> - **Nível B — Simulated in handler:** handler decide internamente se o provider externo está ligado (ex.: consulta repositório, chama cliente HTTP e falha graciosamente), sem interface dedicada. Aceitável para features onde "configurar" significa "existir dados no banco" em vez de "apontar para sistema externo".

| ID | Nível | Interface | `Null*` impl | Registado em `/admin/system-health` | Notas |
|---|---|---|---|---|---|
| **DEG-01** Canary | **A** | `ICanaryProvider` | `NullCanaryProvider` | ✅ sim | `OptionalProviderNames.Canary`. Pattern completo. |
| **DEG-02** Backup | **A** | `IBackupProvider` | `NullBackupProvider` | ✅ sim | `OptionalProviderNames.Backup`. Pattern completo. |
| **DEG-03** Runtime | B | — | — | ❌ não | `GetRuntimeModuleMatrix` simula em handler. Promover para A exige `IRuntimeProvider` + agente CLR real. Fora de escopo imediato. |
| **DEG-04** Chaos | B | — | — | ❌ não | `SubmitChaosExperiment` aceita requests e retorna estado simulado. Para promover: `IChaosProvider` ligado a Litmus/Chaos Mesh. |
| **DEG-05** mTLS | B | — | — | ❌ não | `GetMtlsManager` retorna `simulatedNote` de texto fixo. Para promover: `ICertificateProvider` ligado a cert-manager / Vault PKI. |
| **DEG-06** Schema planner | B | — | — | ❌ não | Planner multi-tenant simulado. Promover exige executor IaC (Terraform/Pulumi) real. |
| **DEG-07** Capacity forecast | B | — | — | ❌ não | `GetCapacityForecast` deriva `simulatedNote` da ausência de snapshots de runtime em `aik_*` (legítimo como Nível B). |
| **DEG-08** Feature flags externo | B | — | — | ❌ não | Por design: BD local é a fonte primária. Read-through a LaunchDarkly/Unleash é feature futura opcional. |
| **DEG-09** Kafka | **A** | `IKafkaEventProducer` | `NullKafkaEventProducer` | ✅ sim | `OptionalProviderNames.Kafka`. Real impl: `ConfluentKafkaEventProducer`. |
| **DEG-10** Cloud billing | **A** | `ICloudBillingProvider` | `NullCloudBillingProvider` | ✅ sim | `OptionalProviderNames.CloudBilling`. |
| **DEG-11** SAML SSO | **A′** | `ISamlService` / `ISamlConfigProvider` | metadata vazia = "não configurado" | ❌ ainda não | Pattern completo mas com *shape* próprio de IdP (metadata XML). Não aparece em `/admin/system-health` porque não expõe `IsConfigured` — a verificação é feita no fluxo (`StartSamlLogin` retorna `SamlNotConfigured`). **Ação recomendada:** expor derivado via `ISamlConfigProvider.HasAnyActiveProvider` para incluir no dashboard. |
| **DEG-12** External AI | B | — | — | ❌ não | `ModelRegistry` + `AiAccessPolicy` decidem runtime-side (interno vs externo). Não cabe num único `IExternalAiProvider` — cada vendor tem driver próprio. |
| **DEG-13** Elasticsearch | B | — | — | ❌ não | `telemetry.elastic.*` via `IElasticQueryClient`. PostgreSQL Product Store é fallback parcial documentado. |
| **DEG-14** ClickHouse | B | — | — | ❌ não | `telemetry.clickhouse.*` análogo a DEG-13. |
| **DEG-15** OTel Collector | B | — | — | ❌ não | `OTEL_EXPORTER_OTLP_ENDPOINT` é infrastructure concern; exporters in-memory/console quando ausente. |

### Conclusões da auditoria

1. **Nível A confirmado em 4/15** (DEG-01, DEG-02, DEG-09, DEG-10). São exatamente os 4 providers expostos em `/admin/system-health` via `OptionalProviderNames`.
2. **Nível A′ em 1/15** (DEG-11 SAML). Tem a maquinaria de configuração mas não se integra ao dashboard — **próximo passo concreto**: adicionar `bool IsConfigured { get; }` em `ISamlConfigProvider` e expor como quinto provider em `OptionalProviderNames.Saml` + `GetOptionalProviders`.
3. **Nível B em 10/15**. São legítimos como degradação graciosa interna ao handler enquanto não há cliente real atrás deles. Promover para A só compensa quando alguém implementa o cliente externo correspondente.
4. **Nenhum DEG promovido a A deixa de aparecer em `/admin/system-health`** — a convenção é: se tem pattern completo, entra no dashboard (e no startup log via `OptionalProviderStartupLogger`).

### Config keys centralizados

Os 4 providers Nível A **não usam** `ConfigurationDefinitionSeeder + IConfigurationResolutionService` — consomem diretamente `appsettings`/env vars (`Kafka:BootstrapServers`, `FinOps:Billing:Provider`, …). Isto é coerente com a convenção interna: endpoints/credenciais de sistemas externos = infrastructure secrets → `appsettings`/env vars; parâmetros funcionais/operacionais (janelas, thresholds, budgets) → `IConfigurationResolutionService`.

Não é necessário criar `IntegrationsConfigKeys.cs` para estes casos.

---

## 🔴 Dívida aberta — backlog priorizado para fechar

> Estes são os itens que, quando fechados, permitem publicar `v1.0.0` com "zero gaps abertos".
> Cada linha é rastreável a um item do plano executivo (`ACTION-PLAN.md`).

### Fase 1 — Honestidade documental (esta iteração)
- [x] **D-01** Identity: declarar Invitation/Reset/Activation fora de escopo — ver OOS-02 e DES-02.
- [x] **D-02/D-05** Contagem de DbContexts deixa de ser citada em números fixos → `tools/count-dbcontexts.sh` como fonte da verdade.
- [x] **D-03** `react-router-dom` confirmado como stack real — `docs/FRONTEND-ARCHITECTURE.md` já está correto.
- [x] **D-04** Este ficheiro (`HONEST-GAPS.md`) consolida as degradações graciosas — critério de aceite 7 do plano.

### Fase 2 — ACTs pendentes (25)
- [ ] **ACT-019..021** (classificação pendente — próximo ciclo).
- [ ] **ACT-022** E2E SAML SSO com Playwright + mock IdP.
- [ ] **ACT-023** Testes de integração `ExportAnalyticsData` (CSV/JSON, paginação, authz).
- [ ] **ACT-024** OpenAPI como artefacto de build (`swagger.json` no `ci.yml`).
- [ ] **ACT-025** Elasticsearch com `xpack.security.enabled=true` em `docker-compose.staging.yml`.

### Fase 3 — Coerência dos `simulatedNote`
- [x] **CFG-01** `SystemHealthPage` (Platform Admin) listando providers opcionais e estado (configured / not-configured), com ligação para setup.
- [x] **CFG-02** Padrão `IFeature.IsConfigured` + `NullXxxProvider` + `XxxConfigKeys` auditado em todos os DEG-01..DEG-15 — ver secção *Auditoria do padrão `IsConfigured + Null*Provider`* acima. Próximo passo concreto identificado: promover DEG-11 (SAML) a Nível A expondo `ISamlConfigProvider.IsConfigured` no dashboard.
- [x] **CFG-03** `docs/deployment/PRODUCTION-BOOTSTRAP.md` com checklist de "para remover todos os `simulatedNote` configure estes providers".

### Fase 4 — Gaps operacionais
- [x] **OPS-01** Knowledge Hub: `GetServiceOperationalTimeline` feature + endpoint `GET /knowledge/services/{serviceId}/operational-timeline` + `ServiceTimelinePage` frontend + rota `/knowledge/services/:serviceId/timeline` + i18n 4 línguas + 11 testes.
- [x] **OPS-02** `GenerateServerFromContract`: templates .NET/Java/Spring/Python/FastAPI/Node/Express/Go sem TODOs — cada linguagem inclui agora 3+ ficheiros (controller, interface de serviço, ficheiro de projeto: .csproj, pom.xml, package.json, pyproject.toml, go.mod).
- [ ] **OPS-03** Validar NullKafkaEventProducer com warning de arranque claro.

### Fase 5 — Qualidade transversal
- [ ] **QLT-01** `parallel_validation` sem findings de alto/médio impacto.
- [ ] **QLT-02** `npm run validate:i18n` / `typecheck` / `lint` — **pass** (hoje validate:i18n falha com chaves em falta pré-existentes; não relacionadas a este ficheiro).
- [ ] **QLT-03** Backend com treat-warnings-as-errors onde falta.
- [ ] **QLT-04** Cobertura: cada `Application/Features/*` novo com ≥ 1 teste unitário.

### Fase 6 — Publicação
- [ ] **DOC-01** `IMPLEMENTATION-STATUS.md` rescrito sem afirmações não verificáveis.
- [ ] **DOC-02** Este ficheiro passa a "Zero gaps abertos" na v1.0.0.
- [ ] **DOC-03** `FUTURE-ROADMAP.md` sem itens já entregues.
- [ ] **DOC-04** Tag `v1.0.0` + CHANGELOG consolidado.

---

## Critérios de aceite "100% fechado"

Mantidos aqui para visibilidade (fonte única):

1. Backend: 0 warnings, 0 errors.
2. Frontend: `typecheck` + `lint` + `validate:i18n` **pass**.
3. `dotnet test` em todos os módulos: **pass**.
4. Zero `TODO/FIXME/HACK` em código de produto (exceto templates code-gen marcados).
5. Zero `Stub controlado:` em `Application/Features/` exceto DES-01 e DES-02 (justificados).
6. Sidebar FE sem entradas que abram páginas com `simulatedNote` sem explicação — CFG-01 resolve.
7. D-01..D-08 resolvidas.
8. 25 ACTs fechados (✅ Resolvido / ❌ Descartado com razão / 🔀 Movido para `FUTURE-ROADMAP.md`).
9. `parallel_validation` sem findings abertos.
