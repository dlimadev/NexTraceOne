# Auditoria Pericial — Estado Real do Projeto NexTraceOne

**Data:** 2026-05-31
**Escopo:** Backend (.NET 10 · 12 módulos · 3 hosts · building-blocks), Frontend (React 19 · 19 módulos), persistência, segurança, CI, contrato backend↔frontend e higiene de repositório.
**Método:** Análise **estática** exaustiva (`grep`/`find`/leitura de código). ⚠️ **`dotnet` não está disponível neste ambiente**, portanto não houve compilação nem execução de testes — todas as afirmações são derivadas de leitura de código e verificadas por inspeção direta de arquivos. `node_modules` do frontend também ausente (sem build/run).
**Postura:** Realista e pessimista, conforme solicitado. Onde a documentação do próprio projeto contradiz o código, o código prevalece.

> **Aviso de credibilidade:** Este repositório já contém 8+ documentos que se declaram "FINAL", "100% COMPLETO" ou "PRONTO PARA PRODUÇÃO" (`RELATORIO-FINAL-v1.0.0-COMPLETO.md`, `PLANO-FINAL-FECHAMENTO-PRODUTO.md`, etc.) e duas auditorias anteriores. **Nenhuma dessas declarações resiste à inspeção do código.** Esta auditoria foi feita do zero, ignorando os veredictos anteriores e verificando cada alegação na fonte.

---

## 1. Sumário Executivo

NexTraceOne é um **monólito modular tecnicamente ambicioso e, em sua infraestrutura, genuinamente bem construído** — Clean Architecture, CQRS-por-arquivo, Outbox com advisory lock, JWT/CORS/CSRF/rate-limiting estão corretos e consistentes. A persistência (camada CRUD) é real: **não há nenhum `IXxxRepository` falsificado com `Null` implementação** (a regra Honest-Null da Parte 21 é respeitada para repositórios de persistência).

**Porém, o produto que um usuário final consegue tocar é aproximadamente metade do que foi construído, e a garantia de segurança mais importante — isolamento de tenant — está efetivamente inoperante.**

Três conclusões quantificadas dominam o relatório:

1. **Funil de entrega (Handlers → Endpoints → UI):** dos **1.332 handlers** CQRS, **294 (22%) não têm endpoint REST** (código morto), e dos ~1.120 endpoints existentes, **~40% não têm nenhum consumidor no frontend**. Resultado: **apenas ~600 dos 1.332 handlers (~45%) chegam a um usuário real.** A camada de **analytics/relatórios — o diferencial de mercado anunciado — é majoritariamente fachada**: de 150 handlers `Get*Report`, **129 (86%) estão mortos**, e a maioria dos 21 expostos lê de `Null*Reader` que retornam coleções vazias em **todas** as configurações.

2. **Isolamento de tenant está quebrado (release-blocker).** As 189 políticas RLS existem, mas **não há um único `FORCE ROW LEVEL SECURITY`** e a aplicação conecta como **dono das tabelas** (`Username=nextraceone`), que é **isento de RLS no PostgreSQL**. O isolamento recai 100% sobre filtros `.Where(TenantId)` nos repositórios — que **faltam em repositórios reais** (vetor IDOR confirmado em `SemanticDiffResultRepository` e outros).

3. **A distância entre documentação e realidade é o problema mais profundo.** CLAUDE.md afirma "RLS ✅ Operacional" e "Módulos NUNCA acessam o DbContext uns dos outros" — **ambos violados no código**. Há credenciais reais (`ouro18`) commitadas, French (`fr.json`) anunciado mas inexistente, e o onboarding (primeiro contato do usuário) está quebrado por um cliente HTTP paralelo inseguro.

### Veredicto de prontidão

| Eixo | Estado | Nota |
|------|--------|------|
| Arquitetura / fundação | 🟢 Sólida e real | Outbox, CQRS, JWT, CORS, CSRF corretos |
| Persistência (CRUD) | 🟢 Real | 0 repositórios falsos |
| **Isolamento multi-tenant** | 🔴 **Inoperante** | RLS bypassado + filtros faltando = **release-blocker** |
| **Segurança de configuração** | 🔴 Senha real commitada | `ouro18` em 25 conn strings |
| Camada de analytics/relatórios | 🔴 Fachada | 86% dos relatórios mortos ou vazios |
| Cobertura frontend do backend | 🟠 ~15% dos endpoints | Produto largo mas raso |
| Onboarding (primeiro uso) | 🔴 Quebrado | Cliente HTTP paralelo inseguro |
| Disciplina de fronteira de módulos | 🔴 Violada | AIKnowledge acopla 4 módulos |
| CI/build | 🟢 Real, sem trapaça | "0 warnings" via supressão, porém |
| Higiene de repositório | 🟠 Ruim | 23 arquivos de rascunho commitados |
| Credibilidade da documentação | 🔴 Baixa | Docs "100% prontos" contradizem o código |

**Conclusão: NÃO está pronto para produção como SaaS multi-tenant.** Não pela falta de funcionalidade — há muita —, mas porque a fronteira de tenant vaza e há credencial commitada. São 2-3 bloqueadores objetivos de release, seguidos de um grande trabalho de "ligar o que já foi construído" (wiring) e de remediar a fachada de analytics.

---

## 2. Metodologia, Confiança e Correções às Auditorias Anteriores

- Dados estruturais (contagem de endpoints/handlers/dead-handlers por módulo) gerados via `analyze_v5.py` (script já existente no repo) e **re-verificados** manualmente.
- Cada achado **crítico/alto foi confirmado por leitura direta do arquivo citado** (file:line).
- **Correções a `AUDITORIA-BACKEND-2026-05-31.md` (auditoria anterior, do mesmo dia):**
  - ❌ "11 Null*Repository = BUG" — **STALE.** Sobram apenas 3 classes `Null*Repository`, e as 3 são **provedores externos opcionais gated por configuração** (ClickHouse/Elasticsearch/Qdrant ausentes), não bugs de CRUD. A persistência está limpa.
  - ❌ "1 erro de build (TEST-01)" — **já corrigido** no commit `58182e0`; o construtor de `SendAssistantMessageLlmTests` já passa os 18 argumentos.
  - ✅ "294 dead handlers" — **confirmado** (com ressalva: o analisador tem falsos-positivos por `using`-aliases; a ordem de grandeza está correta).

---

## 3. Funil de Entrega — Quanto do Backend Chega ao Usuário

```
CAMADA 1 — Handlers que EXISTEM (CQRS/MediatR)           1.332   (100%)
                │  294 handlers sem nenhum endpoint REST          (-22%)
                ▼
CAMADA 2 — Handlers EXPOSTOS como endpoint REST          ~1.120  (~84%)
                │  ~40% dos endpoints sem nenhum consumidor de UI  (-)
                │  + toda a superfície GraphQL (catalog+changegov) 0% consumida
                ▼
CAMADA 3 — Endpoints realmente CONSUMIDOS pela UI        ~600    (~45% dos handlers)
```

**O frontend chama ~181 caminhos distintos de ~1.120 endpoints → ~15% da superfície do backend é alcançável pela UI.**

### Distribuição da dívida (dead handlers por módulo)

| Módulo | Handlers | Endpoints | Dead handlers | Consumo de UI |
|--------|---------:|----------:|--------------:|---------------|
| catalog | 326 | 226 | **104** | core ok; muitos sub-domínios de contracts + GraphQL mortos |
| changegovernance | 173 | 114 | **65** | changes/releases ok; subscriptions GraphQL 0% |
| operationalintelligence | 172 | 133 | **58** | `/cost`, `/predictions` mortos; FE aponta p/ `/operations/*` inexistente |
| governance | 190 | 207 | 20 | amplo; gates/monitors/artifact-signing mortos |
| aiknowledge | 204 | 192 | 18 | orchestration/eval/memory/guardrails 0% |
| identityaccess | 86 | 77 | 13 | bem consumido |
| knowledge | 23 | 15 | 10 | alta razão de morte |
| auditcompliance | 26 | 28 | 0 | só `/audit` root; compliance frameworks 0% |
| configuration | 66 | 66 | 0 | ok; analytics/taxonomies mortos |
| notifications | 22 | 23 | 2 | ok |
| integrations | 24 | 26 | 2 | parcial |
| productanalytics | 16 | 17 | 2 | ok |

> **catalog + changegovernance + operationalintelligence concentram 227 de 294 (77%) de todos os handlers mortos.**

---

## 4. BUGS CRÍTICOS (Release-Blockers)

### 🔴 C-1 — Isolamento de tenant inoperante: RLS bypassado + filtros de repositório faltando
**Severidade: CRÍTICA (bloqueador absoluto)**
**Evidência:**
- `infra/postgres/apply-rls.sql` — **189 `ENABLE ROW LEVEL SECURITY` + 189 `CREATE POLICY`, mas `FORCE ROW LEVEL SECURITY` aparece somente em comentário** (`apply-rls.sql:19`: `-- FORCE ROW LEVEL SECURITY; remove FORCE if the application runs as table owner`).
- `src/platform/NexTraceOne.ApiHost/appsettings.json` — a aplicação conecta como `Username=nextraceone`, que **é dono das tabelas** (roda as migrations). **No PostgreSQL, o dono da tabela é isento de RLS, a menos que `FORCE` esteja ativo.** Logo, **nenhuma política RLS se aplica às queries da própria aplicação.**
- `ApplyRlsPoliciesAsync` só é chamado **dentro de** `if (pendingContexts.Count > 0)` (`WebApplicationExtensions.cs:144-152`) — num deploy onde as migrations são aplicadas fora-de-banda (passo separado), as políticas **podem nunca ser aplicadas**.
- Filtros de repositório (a única camada que realmente isola): de **328 repositórios, 211 (64%) não referenciam `TenantId`**. Muitos são entidades-filhas legítimas, mas há buracos reais confirmados:
  - `catalog/.../Contracts/Persistence/Repositories/SemanticDiffResultRepository.cs` — **0 referências a `TenantId`**; `GetByIdAsync(Guid)` retorna o registro por Id sem escopo de tenant → **IDOR cross-tenant** (usuário do tenant A lê dado do tenant B sabendo um Guid).
  - Mesmo padrão em `BackgroundServiceContractDetailRepository`, `ImpactSimulationRepository`, `NegotiationCommentRepository` (amostrados).

**Impacto:** vazamento de dados entre tenants — fatal para um produto de *governança/auditoria*. Com RLS inerte, qualquer repositório sem filtro é um vetor de leitura (e possivelmente escrita) cross-tenant.
**Correção:** (1) `ALTER TABLE ... FORCE ROW LEVEL SECURITY` em todas as tabelas de tenant **e** rodar a aplicação como role **não-dono**; (2) desacoplar `ApplyRlsPoliciesAsync` do check de migrations pendentes; (3) auditar todos os repositórios de raiz-de-agregado garantindo `.Where(e => e.TenantId == currentTenant.Id)`; (4) adicionar teste de integração que prove isolamento.

### 🔴 C-2 — Senha real de banco (`ouro18`) commitada no `appsettings.json` base
**Severidade: CRÍTICA**
**Evidência:** `src/platform/NexTraceOne.ApiHost/appsettings.json` — **25 connection strings contêm `Password=ouro18`** (arquivo **rastreado no git**, carregado em **todos** os ambientes como camada base). O `appsettings.Development.json` usa placeholders corretamente (`CHANGE_ME`), mas o arquivo base anula isso.
**Impacto:** credencial vazada no histórico do git; senha fraca e compartilhada nos 28 bancos lógicos. Em produção, se cada conn string não for sobrescrita por env var, a app conecta com `ouro18`.
**Correção:** substituir por placeholder no base; **rotacionar a senha**; usar env/user-secrets (`NEXTRACEONE_DB_PASSWORD` já suportado). Reescrita de histórico do git recomendada.

### 🔴 C-3 — Onboarding do usuário quebrado via cliente HTTP paralelo inseguro
**Severidade: CRÍTICA (primeiro uso) + ALTA (segurança)**
**Evidência:** `src/frontend/src/lib/api-client.ts` é um **segundo** cliente axios (duplicado do canônico `src/api/client.ts`):
- `api-client.ts:4` → `baseURL = VITE_API_BASE_URL || 'http://localhost:5000'` (host hardcoded, **sem o prefixo `/api/v1`**).
- `api-client.ts:16` → `localStorage.getItem('auth_token')` — **viola a regra do CLAUDE.md** ("token em sessionStorage, nunca localStorage"). O cliente real grava em `sessionStorage` com outra chave → **este `getItem` é sempre `null`** → requisições saem **sem autenticação, sem `X-Tenant-Id`, sem CSRF, sem refresh**.
- Importado pela página **roteada** `OnboardingWizardPage.tsx` (rota `/onboarding`) → `GET http://localhost:5000/onboarding/status` (host errado, sem auth/tenant/csrf).

**Impacto:** o **wizard de onboarding de novo tenant é não-funcional em qualquer deploy real**. Violação direta do modelo de segurança documentado.
**Correção:** deletar `src/lib/api-client.ts`; migrar `OnboardingWizardPage` para o cliente canônico `@/api/client`; remover o dead code `operations/api/runtime-intelligence.ts`.

### 🔴 C-4 — Fronteira de bounded contexts violada (acoplamento direto de DbContext entre módulos)
**Severidade: CRÍTICA (arquitetural)**
**Evidência:** CLAUDE.md Parte 10: "Módulos **NUNCA** acessam o DbContext uns dos outros." Violado:
- `NexTraceOne.AIKnowledge.Infrastructure.csproj` faz `ProjectReference` para **`Catalog.Infrastructure`, `ChangeGovernance.Infrastructure`, `OperationalIntelligence.Infrastructure` e `Knowledge.Infrastructure`** e injeta seus DbContexts diretamente:
  - `CatalogGroundingReader.cs:12` injeta `CatalogGraphDbContext`
  - `ChangeGroundingReader` injeta `ChangeIntelligenceDbContext`
  - `IncidentGroundingReader` injeta `IncidentDbContext`
  - `KnowledgeDocumentGroundingReader` injeta `KnowledgeDbContext`
- `operationalintelligence.Infrastructure` referencia `ChangeGovernance.Infrastructure`; `EfChangeIntelligenceReader.cs:13` injeta `ChangeIntelligenceDbContext`.

**Impacto:** acoplamento em tempo de compilação entre contextos; uma mudança de schema em Catalog/ChangeGov quebra o build de AIKnowledge. Ironia: o padrão `IXxxReader`/Honest-Null (Parte 21) existe exatamente para evitar isto, e foi contornado.
**Correção:** rotear via `Contracts` (`IXxxModule`) ou expor readers através de interfaces no `Contracts` do módulo dono, removendo os `ProjectReference` de Infrastructure→Infrastructure.

---

## 5. BUGS / INCONSISTÊNCIAS — ALTO

### 🟠 A-1 — Camada de analytics/relatórios é fachada (86% morta ou vazia)
**Severidade: ALTA**
- De **150 handlers `Get*Report`, 129 (86%) estão mortos** (sem endpoint). Inclui relatórios de **compliance (SOC2, HIPAA, GDPR, FedRAMP, PCI-DSS, ISO27001, NIS2, CMMC)**, blast-radius, supply-chain risk, SRE maturity, FinOps.
- Os ~21 relatórios expostos leem majoritariamente de `Null*Reader` que retornam `[]` em **todas** as configurações (87 `Null*Reader` + 8 `Null*Provider`, cada um sendo a **única** implementação da interface).
- **Algoritmos reais, alimentação vazia:** `GetSreMaturityIndexReport.cs` é um modelo ponderado de 6 dimensões **genuíno**, mas sua fonte é `NullSreMaturityReader.ListByTenantAsync → []`. Com 0 linhas, retorna `TenantTier=Elite, Index=100` (linha 173: `rows.Count == 0 ? 100m`). **Um tenant novo é reportado como "Elite SRE" sem nenhum dado.**

**Impacto:** o diferencial competitivo anunciado (Change Intelligence, SRE maturity, compliance) é não-funcional e, pior, **enganoso** (defaults "Elite/100%" para ausência de dados).
**Correção:** ver C-1 do plano (ligar ingestão) + fazer endpoints retornarem status explícito `NoDataAvailable` em vez de defaults otimistas.

### 🟠 A-2 — Boundary de ingestão quebrado (causa-raiz da fachada)
**Severidade: ALTA**
**Evidência:** `IngestSloObservation` **não tem endpoint**, mas `RegisterSloDefinition` tem (`ReliabilityEndpointModule.cs:17`). Você define SLOs mas não consegue alimentar observações. O mesmo vale para `IngestServiceCostRecord`, `IngestSbomRecord`, `IngestModelPredictionSample`, `IngestProfilingSession`, `IngestAdvisoryReport`. Sem esses, toda a cadeia SLO→ErrorBudget→SreMaturity→Scorecard fica sem dados — **é por isso que os `Null*Reader` existem.**
**Impacto:** raiz estrutural da fachada de analytics.
**Correção:** expor os endpoints de ingestão (o código dos handlers já existe).

### 🟠 A-3 — Wiring frontend→backend para namespace inexistente (`/operations/*`, `/predictive/*`)
**Severidade: ALTA**
**Evidência:** widgets e páginas de `operations`/`governance` chamam endpoints que **não existem em nenhum módulo do backend**:
- `GET /operations/incidents`, `/operations/reliability`, `/operations/on-call`, `/operations/service-maturity`, `/operations/post-mortems`, `/operations/load-tests`, `/operations/slo/templates`, `POST /operations/drift/{id}/acknowledge`
- `GET /predictive/service-failure`, `/predictive/change-risk/{id}`
- `GET /api/runtime-intelligence/snapshots` (o backend usa `/api/v1/runtime`)

O backend real expõe esses dados sob `/api/v1/runtime`, `/api/v1/incidents`, `/api/v1/reliability`. **Dashboards inteiros de operations/SRE não funcionam contra a API real.**
**Correção:** reapontar as chamadas para os caminhos reais ou implementar uma fachada `/operations/*` no backend.

### 🟠 A-4 — Dados aleatórios renderizados como métricas reais
**Severidade: ALTA**
**Evidência:**
- `operations/pages/RuntimeIntelligenceDashboardPage.tsx:118-124` — botão "Ingest snapshot" **posta `Math.random()`** (latência/erro/CPU/memória) ao backend real → **polui telemetria de produção com números falsos**.
- `operations/pages/SreDashboardPage.tsx:360-367` — `buildMockTimeSeries()` (random) renderizado quando a API não retorna dados, como se fosse real.
- `governance/widgets/DeploymentFrequencyWidget.tsx:34-45` e `HeatmapCalendarWidget.tsx:43` — distribuição diária sintetizada com `Math.random()`.
- Backend: `governance/.../GetDemoSeedStatus.cs:60` — `Random.Shared.Next(50,200)` como contagem de entidades "semeadas".
**Impacto:** dashboards enganosos; corrupção ativa de dados de telemetria.
**Correção:** remover geradores aleatórios; usar empty-states honestos.

### 🟠 A-5 — Features de segurança/assinatura simuladas
**Severidade: ALTA**
**Evidência:** `governance/.../Services/CosignArtifactSigner.cs:91` — `// Por enquanto, apenas simular a revogação`. `NullSamlProvider` é a única impl de SAML → **SSO SAML (selling point Enterprise) é no-op**.
**Impacto:** capability de segurança anunciada não existe de fato.

### 🟠 A-6 — Drift de migrations EF suprimido em 5 DbContexts
**Severidade: ALTA (operacional)**
**Evidência:** 5 DbContexts suprimem `PendingModelChangesWarning` via `NEXTRACE_IGNORE_PENDING_MODEL_CHANGES` (`catalog/Contracts`, `aiknowledge/Governance`, `aiknowledge/Orchestration`, `aiknowledge/ExternalAI`, `configuration`). O modelo EF drifou do último snapshot de migration.
**Impacto:** em runtime, tabelas podem faltar colunas que o código espera → falhas de query mascaradas em CI pela env var.
**Correção:** `dotnet ef migrations add` para cada um; remover a supressão.

### 🟠 A-7 — Locale inglês (fallback) ~9% incompleto; francês inexistente
**Severidade: ALTA**
**Evidência:** `src/frontend/src/locales/` contém apenas `en.json, es.json, pt-BR.json, pt-PT.json` — **não há `fr.json`** apesar de CLAUDE.md afirmar "4 idiomas: pt, en, es, fr ✅". `en.json` (12.971 chaves) é o menor, faltando ~1.280 chaves presentes em es/pt → usuários em inglês veem **chaves cruas** (ex.: `config.finops.waste.thresholds.description`).
**Correção:** completar `en.json`; criar `fr.json` ou corrigir a documentação.

### 🟠 A-8 — Módulo `observability` órfão e inseguro
**Severidade: ALTA**
**Evidência:** `src/features/observability/**` (7 arquivos) **não é referenciado por nenhuma rota**. `ObservabilityService.ts` usa `axios` cru contra `http://localhost:5000/...` sem auth/tenant/CSRF.
**Impacto:** capability (request metrics, error analytics, system health) construída mas 100% inalcançável; inseguro se ligado.
**Correção:** rotear via cliente canônico ou remover.

### 🟠 A-9 — `OutboxProcessorJob` ausente para `DeveloperExperienceDbContext`
**Severidade: ALTA**
**Evidência:** não há registro de `ModuleOutboxProcessorJob<DeveloperExperienceDbContext>` em `BackgroundWorkers/Program.cs`.
**Impacto:** domain events desse contexto **acumulam sem processamento**.

---

## 6. INCONSISTÊNCIAS — MÉDIO / BAIXO

| ID | Severidade | Achado | Evidência |
|----|-----------|--------|-----------|
| M-1 | Médio | "0 warnings" obtido por supressão: `NoWarn` com ~40 IDs + `WarningsNotAsErrors` para **`NU1903` (pacote vulnerável `Microsoft.Bcl.Memory` shippado conscientemente)** | `Directory.Build.props:42` |
| M-2 | Médio | Test projects **não herdam** `TreatWarningsAsErrors` (têm `Directory.Build.props` próprio sem `Import`) → ~2.457 warnings (CS8632) toleradas em testes | `tests/Directory.Build.props` |
| M-3 | Médio | UIs de contrato duplicadas/sobrepostas entre `catalog` e `contracts` (124 arquivos) — risco de fluxos divergentes | `catalog/pages/Contracts*` vs `contracts/**` |
| M-4 | Médio | GraphQL (HotChocolate) em catalog+changegovernance **100% não consumido** (sem cliente GraphQL no frontend) | `catalog/.../GraphQL/*` |
| M-5 | Médio | `build/clickhouse/*.sql` (11 arquivos) rastreados sob `build/`; SQL de schema fora do lugar | `build/` |
| B-1 | Baixo | `OpenAILLMProvider.cs:22` retorna `"[STUB] Response..."` — **mas sem registro de DI** (dead code, não fallback ativo) | `aiknowledge/.../OpenAILLMProvider.cs` |
| B-2 | Baixo | Log "All 27 DbContexts up-to-date" — existem **28** | `WebApplicationExtensions.cs:156` |
| B-3 | Baixo | `configurationApi.ts` com `return []` — verificar se é empty-state ou endpoint não-implementado | `configuration/api/configurationApi.ts` |
| B-4 | Baixo | `NullKafkaEventProducer` descarta eventos silenciosamente (aceitável pois Outbox é o caminho real, mas sem log de aviso) | infra |

---

## 7. Higiene de Repositório — POBRE (Médio)

`git ls-files` confirma todos rastreados, nenhum no `.gitignore`:

- **23 arquivos de rascunho na raiz:** `analysis_output.txt` (202 KB), `analysis_v2-v6.txt` (6), `analyze_*.py` (5), `chg_*.sql` (6), `ntf_*.sql` (3), `test_oidc*.py` (3).
- **6 documentos de estratégia sobrepostos e conflitantes** na raiz, todos declarando conclusão: `PLANO-FINAL-FECHAMENTO-PRODUTO.md`, `PRODUCTION-ACTION-PLAN.md`, `RELATORIO-FINAL-v1.0.0-COMPLETO.md`, `ROADMAP-COMPLETO-ESTRATEGICO.md`, `UNIFIED-FINAL-DELIVERY-PLAN.md` + 2 auditorias.
- **`nextraceone.frontend/`** — diretório placeholder com só um `README.md` (o frontend real é `src/frontend/`).
- **`.gitignore` gap:** `appsettings.Development.json` está no `.gitignore:39` mas **2 já estão rastreados**; nenhum padrão ignora `analysis_*`/`analyze_*`/`*_test.sql`/`test_oidc*.py`.
- Existe um guardrail `scripts/quality/check-no-demo-artifacts.sh` no CI — **que não cobre os artefatos de rascunho da raiz**.

---

## 8. Documentação vs. Realidade (Credibilidade)

| Alegação (docs do projeto) | Realidade no código | Veredicto |
|---|---|---|
| "PRODUTO 100% PRONTO PARA PRODUÇÃO" (RELATORIO-FINAL) | Isolamento de tenant inoperante + senha commitada | ❌ Falso |
| "Build limpo: 0 errors, 0 warnings" (PLANO-FINAL) | 0 warnings por supressão (40 NoWarn + NU1903); ~2.457 warnings em testes | ⚠️ Enganoso |
| "Zero TODOs em produção / 0 NotImplementedException" | 0 `NotImplementedException` (verdade), mas 159 comentários "simplified/placeholder/por enquanto" + features simuladas | ⚠️ Enganoso |
| "RLS via TenantRlsInterceptor ✅ Operacional" (CLAUDE.md P23) | RLS bypassado (sem FORCE, app é dono) | ❌ Falso |
| "Módulos NUNCA acessam DbContext uns dos outros" (P10) | AIKnowledge acopla 4 módulos; OPI acopla ChangeGov | ❌ Violado |
| "4 idiomas: pt, en, es, fr ✅" | Sem `fr.json`; `en.json` ~9% incompleto | ❌ Falso |
| "2000+ testes" / "99+ endpoints" | ~10.053 `[Fact]`/`[Theory]`; ~1.120 endpoints | ⚠️ Subcontagem (na verdade há mais) |
| Outbox + advisory lock ✅ | Genuinamente correto e completo | ✅ Verdadeiro |
| JWT / CORS / CSRF / rate-limiting ✅ | Genuinamente endurecidos | ✅ Verdadeiro |

**O time sabe onde estão os gaps** (`docs/HONEST-GAPS.md`, marcadores `[STUB]`/TODO), mas os documentos de topo e a tabela de Feature Status **super-vendem**.

---

## 9. O Que Está Genuinamente Bom (para não ser injusto)

- **Outbox processor** (`ModuleOutboxProcessorJob`): advisory lock não-bloqueante, release no `finally`, batch 50, retry<5, DLQ na exaustão — corretíssimo.
- **JWT** (`JwtTokenService`): **lança** se não há chave — sem fallback/secret de dev.
- **CORS**: rejeita wildcard com credentials, exige origins explícitos fora de Dev.
- **CSRF + rate limiting** (6 políticas) presentes e wired.
- **Persistência CRUD**: 0 repositórios falsos; IDs fortemente tipados; soft-delete e auditoria via interceptors.
- **`ComputeServiceScorecard`** (catalog): lógica real, cross-module, com fallbacks honestos e TODOs explícitos — o padrão-ouro que o resto deveria seguir.
- **10.053 métodos de teste** distribuídos por todos os 12 módulos.
- **CI real**: build+test+integration+E2E com gate agregado, sem `continue-on-error` trapaceiro.

---

## 10. PLANO DE AÇÃO

Organizado por fases com critério de verificação por item (conforme Parte 1.4 do CLAUDE.md). Estimativas em dias-engenheiro (DE) são grosseiras.

### FASE 0 — Release-Blockers de Segurança (1–2 semanas) — **obrigatória antes de qualquer produção**

| # | Ação | Critério de verificação | Esforço |
|---|------|-------------------------|---------|
| 0.1 | **Forçar RLS:** `ALTER TABLE ... FORCE ROW LEVEL SECURITY` em todas as tabelas de tenant; criar role de aplicação **não-dono** (`nextraceone_app`) com `GRANT` mínimo; app conecta com ela; migrations rodam com role separada (`nextraceone_owner`). | Teste de integração: usuário tenant A **não** lê linha de tenant B mesmo com Guid conhecido, com RLS ativo. | 4–6 DE |
| 0.2 | Desacoplar `ApplyRlsPoliciesAsync` do `if (pendingContexts > 0)`; aplicar/idempotente sempre no startup. | Deploy com DB já migrado fora-de-banda ainda aplica políticas (log confirma). | 1 DE |
| 0.3 | Auditar **todos** os repositórios de raiz-de-agregado p/ filtro `.Where(TenantId == currentTenant.Id)`; corrigir `SemanticDiffResultRepository`, `BackgroundServiceContractDetailRepository`, `ImpactSimulationRepository`, `NegotiationCommentRepository` e demais sem escopo. | Script de verificação lista 0 repositórios de agregado sem filtro; testes IDOR por módulo. | 5–8 DE |
| 0.4 | Remover `ouro18` do `appsettings.json` base (placeholder); **rotacionar senha**; documentar env vars; considerar reescrita de histórico (`git filter-repo`). | `grep -r ouro18 src/` retorna 0; app sobe só com env/secret. | 1 DE |
| 0.5 | Atualizar `NU1903` (`Microsoft.Bcl.Memory`) p/ versão não-vulnerável; remover supressão. | `dotnet list package --vulnerable` limpo; remover `WarningsNotAsErrors` NU1903. | 1–2 DE |

### FASE 1 — Corrigir o Que Está "Desenvolvido Errado" (2–3 semanas)

| # | Ação | Critério | Esforço |
|---|------|----------|---------|
| 1.1 | Deletar `src/lib/api-client.ts`; migrar `OnboardingWizardPage` p/ `@/api/client`; remover `operations/api/runtime-intelligence.ts`. | Onboarding funciona contra API real com auth/tenant/CSRF; `grep localStorage src/frontend/src` sem token. | 2 DE |
| 1.2 | Reapontar chamadas FE `/operations/*` e `/predictive/*` p/ caminhos reais (`/api/v1/runtime`, `/incidents`, `/reliability`) **ou** criar fachada backend. | Dashboards de operations/SRE carregam dados reais (sem 404). | 4–6 DE |
| 1.3 | Remover geração de dados aleatórios da UI (RuntimeIntelligence ingest, `buildMockTimeSeries`, widgets DORA/heatmap); empty-states honestos. | `grep Math.random src/frontend/src/features` só em geração de id legítima. | 2 DE |
| 1.4 | Desacoplar fronteiras de módulo (C-4): remover `ProjectReference` Infra→Infra; rotear grounding readers via `Contracts`/`IXxxReader`. | Nenhum `.csproj` de Infrastructure referencia outro módulo Infrastructure. | 6–10 DE |
| 1.5 | Corrigir defaults enganosos (`rows.Count==0 ? Elite/100`) p/ status `NoDataAvailable`. | Relatório sem dados retorna estado explícito, não "Elite". | 2 DE |
| 1.6 | Registrar `ModuleOutboxProcessorJob<DeveloperExperienceDbContext>`. | Eventos do contexto processados; teste. | 0.5 DE |
| 1.7 | Implementar `NullSamlProvider` real **ou** rebaixar SAML de "Operacional" na doc; idem revogação Cosign. | Capability anunciada corresponde ao código. | variável |

### FASE 2 — Ligar o Que Já Foi Construído (Wiring) (3–5 semanas)

| # | Ação | Critério | Esforço |
|---|------|----------|---------|
| 2.1 | **Expor endpoints de ingestão** (`IngestSloObservation`, `IngestServiceCostRecord`, `IngestSbomRecord`, `IngestModelPredictionSample`, `IngestProfilingSession`, `IngestAdvisoryReport`). | Cadeia SLO→ErrorBudget→Maturity recebe dados reais. | 4–6 DE |
| 2.2 | Substituir `Null*Reader` por readers EF/Dapper reais à medida que a ingestão alimenta dados (priorizar os já expostos por endpoint). | Relatório retorna dados reais com dados semeados; remover binding `Null*`. | 10–20 DE (incremental) |
| 2.3 | **Triagem dos 294 handlers mortos:** decisão explícita por handler — (a) expor endpoint (a maioria só falta `EndpointModule`), ou (b) deletar. Priorizar compliance (SOC2/HIPAA/GDPR/...), policy/promotion CRUD, blast-radius. | Planilha com decisão por handler; nº de handlers mortos cai para ~0 (expostos ou removidos). | 10–15 DE |
| 2.4 | Construir UI para áreas de alto valor sem frontend: compliance frameworks, FinOps cost (`/cost`), AI governance (eval/guardrails/memory), policy gates. | Cada capability anunciada tem ao menos uma página consumindo a API. | grande (por capability) |
| 2.5 | Decidir sobre GraphQL: consumir no frontend (cliente urql/apollo) ou remover a superfície HotChocolate não usada. | GraphQL consumido ou removido — sem código morto. | variável |

### FASE 3 — Schema, Qualidade e Higiene (1–2 semanas, paralelizável)

| # | Ação | Critério | Esforço |
|---|------|----------|---------|
| 3.1 | Regenerar migrations dos 5 DbContexts com drift; remover supressão `PendingModelChangesWarning`. | `dotnet ef migrations has-pending-model-changes` limpo nos 5; env var removida. | 3–5 DE |
| 3.2 | `git rm` dos 23 arquivos de rascunho da raiz + `nextraceone.frontend/`; mover `build/clickhouse/*.sql` p/ `db/`. | Raiz limpa; guardrail anti-demo cobre raiz. | 1 DE |
| 3.3 | Consolidar os 6+ docs de estratégia em **um** `STATUS.md` honesto; alinhar CLAUDE.md Parte 23 à realidade (RLS, módulos, idiomas). | Existe 1 fonte de verdade; sem alegações falsas. | 1–2 DE |
| 3.4 | Completar `en.json` (~1.280 chaves); criar `fr.json` ou remover francês da doc. | `en` e `fr` íntegros vs `es/pt`; teste de paridade de chaves no CI. | 2–3 DE |
| 3.5 | Limpar warnings reais em vez de suprimir (reduzir `NoWarn`); fazer test projects herdarem `TreatWarningsAsErrors`. | `NoWarn` reduzido; testes sem CS8632. | 5–10 DE |
| 3.6 | Deletar dead code (`OpenAILLMProvider` stub); corrigir log "27 DbContexts" → 28; trocar `Random` do demo-seed por contagem real. | Sem stubs/dead code; logs corretos. | 1 DE |

### FASE 4 — Endurecimento e Cobertura (contínuo)

- Testes de integração de **isolamento de tenant** por módulo (Testcontainers) — prova viva de C-1.
- Reforçar testes em `integrations` (DLQ, webhooks) e `knowledge` (pgvector), hoje os mais finos.
- Observabilidade de produção dos `Null*Reader` restantes (métrica "feature retornou vazio") para evitar fachada silenciosa.

### Ordem de execução recomendada

```
FASE 0 (bloqueia release) ─► FASE 1 (corrige o errado) ─► FASE 2 (liga o construído) ─► FASE 3/4 (qualidade, contínuo)
        │                                                          ▲
        └──────────────── 3.1 (migrations) pode iniciar em paralelo┘
```

**Esforço total grosseiro até "produção honesta":** Fases 0+1 ≈ 6–9 semanas-engenheiro de bloqueadores e correções; Fase 2 (entregar o valor já construído) é o maior bloco e define o quão "completo" o produto realmente fica.

---

## 11. Conclusão Pericial

NexTraceOne **não é um mock nem um MVP de fachada** — é uma plataforma real, ampla, com fundação de engenharia sólida onde foi terminada. Mas **também não é o produto "100% pronto" que sua própria documentação declara**. A leitura honesta é:

- **~45% do backend construído chega ao usuário**; o resto está morto (22%) ou é fachada sobre readers vazios.
- **O isolamento multi-tenant — a promessa central — está inoperante**, e há **credencial real commitada**. São bloqueadores objetivos.
- **A camada de "inteligência"** (analytics, compliance, SRE, FinOps, AI governance) — o que diferencia o produto — é onde está concentrada a fachada.
- **A maior dívida não é técnica, é de credibilidade:** o projeto foi repetidamente declarado "pronto" sem a correção de fronteira que o sustentaria.

A boa notícia: muita coisa só precisa ser **ligada** (wiring), não reescrita. A fundação aguenta. O caminho para produção é claro e está no plano acima — começando, inevitavelmente, por fechar a fronteira de tenant.

---

*Auditoria gerada por análise estática (sem compilação/execução disponíveis no ambiente). Todos os achados críticos/altos foram verificados por leitura direta de arquivo. Onde a verificação dinâmica (build/test/runtime) for necessária para confirmar (ex.: drift de migrations, flutuação de testes de integração), está explicitamente assinalado.*
