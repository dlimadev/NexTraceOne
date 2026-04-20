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
- [ ] **CFG-01** `SystemHealthPage` (Platform Admin) listando providers opcionais e estado (configured / not-configured), com ligação para setup.
- [ ] **CFG-02** Padrão `IFeature.IsConfigured` + `NullXxxProvider` + `XxxConfigKeys` auditado em todos os DEG-01..DEG-15.
- [ ] **CFG-03** `docs/deployment/PRODUCTION-BOOTSTRAP.md` com checklist de "para remover todos os `simulatedNote` configure estes providers".

### Fase 4 — Gaps operacionais
- [ ] **OPS-01** Knowledge Hub: Changelog vivo + Operational Notes timeline por serviço.
- [ ] **OPS-02** `GenerateServerFromContract`: templates Java/Spring, Python/FastAPI, Node/Express sem TODOs quando possível.
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
