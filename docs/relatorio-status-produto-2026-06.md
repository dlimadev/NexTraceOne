# Relatório de Status do Produto — NexTraceOne

**Data:** 12 de junho de 2026
**Escopo:** Auditoria técnica completa do código (5 áreas, verificação arquivo a arquivo) contra a estratégia de produto definida: *bundle mid-market de gestão do ciclo de vida de serviços* — catálogo + contratos + governança de mudanças (núcleo), incidentes-workflow, observabilidade via OpenTelemetry, flags como registro de governança, config por ambiente, funil comercial self-service.

---

## 1. Resumo Executivo

| Pilar da estratégia | Prontidão | Veredito |
|---|---|---|
| **Catálogo + Contratos** (núcleo) | ~85% | Mais maduro da plataforma. 13 de 16 áreas completas de ponta a ponta. |
| **Governança de Mudanças** (diferencial) | ~75% | Workflow de promoção/aprovação funciona, mas notificações nunca disparam (bug crítico de wiring). |
| **Incidentes** (PagerDuty-parcial) | ~55% | Criar/correlacionar funciona; **resolver incidente não tem endpoint** — ciclo de vida incompleto. |
| **Observabilidade** (OTel + ClickHouse) | ~50% | Cadeia existe de ponta a ponta, mas com **SQL injection** e **sem isolamento de tenant na telemetria**. |
| **Notificações** | ~70% | E-mail SMTP e Teams reais. **Slack não existe.** Webhook genérico não existe. |
| **Funil comercial** (signup → billing) | ~15% | **Showstopper.** Sem signup público, sem billing, enforcement de planos quase inexistente. |

**Conclusão geral:** a plataforma está em estágio **beta interno**. A jornada principal (registrar serviço → contrato → release → promoção → incidente correlacionado) existe e funciona em grande parte, mas é interrompida por lacunas de wiring (eventos não publicados, endpoints não mapeados) e está **inviável para receber um cliente externo** por ausência total do funil comercial e por 3 vulnerabilidades de segurança que precisam ser corrigidas antes de qualquer piloto.

**Saúde do frontend (verificado por execução):** `tsc` — 0 erros; ESLint — 0 erros, 149 warnings cosméticos; Vitest — 2.323 testes passando. A base de código é saudável; os problemas são funcionais (wiring e lacunas), não de qualidade de código.

---

## 2. Inventário: o que EXISTE e funciona de ponta a ponta

Verificado: handler + endpoint mapeado + repositório EF real + DI + tela conectada.

### Catálogo & Contratos (núcleo da estratégia)
- ✅ Registro/listagem/detalhe/lifecycle de serviços (wizard no frontend conectado)
- ✅ Contratos multiprotocolo (REST, eventos, SOAP, data contracts, background services) — 9 endpoint modules, 40+ rotas
- ✅ Scorecards e Evidence Packs (geração, assinatura digital, export PDF)
- ✅ Developer Portal (20+ endpoints: subscriptions, playground, API keys, codegen)
- ✅ Service Discovery (descoberta, matching, registro a partir da descoberta)
- ✅ Interfaces de serviço + contract bindings
- ✅ Source of Truth + busca global
- ✅ Legacy Assets (mainframe/COBOL/CICS/IMS/DB2/z/OS Connect)
- ✅ Dependency Governance com integrações externas reais (OSV.dev, NuGet.org)
- ✅ Templates de serviço, Developer Experience (surveys, IDE usage)
- ✅ 344 validators FluentValidation para ~138 commands — validação de input robusta

### Governança de Mudanças (diferencial da estratégia)
- ✅ Promotion requests com máquina de estados real (Pending → InEvaluation → Approved/Rejected/Blocked/Cancelled)
- ✅ Approval workflow com gates, override com justificativa, UI funcional (`PromotionPage`, `ReleaseApprovalGatewayPage`)
- ✅ Blast radius com cálculo real (consumidores diretos + transitivos) e recálculo do Change Intelligence Score
- ✅ Rulesets/linting (upload Spectral, execução, score) — backend completo
- ✅ SLA de workflow com escalação por violação
- ✅ Evidence packs com assinatura digital + viewer no frontend

### Incidentes & Operação
- ✅ Criar incidente (UI + API + persistência EF)
- ✅ **Correlação release ↔ incidente é REAL** — `IncidentCorrelationService` cruza proximidade temporal (12h antes/2h depois), interseção de serviço e blast radius do changegovernance. É o coração da tese do produto e funciona.
- ✅ Timeline unificada (leitura)
- ✅ On-call intelligence (análise retrospectiva de fadiga/pico)
- ✅ E-mail real (SMTP via `SmtpClient`, retry com backoff, config criptografada em banco)
- ✅ Microsoft Teams real (Adaptive Cards via webhook)

### Plataforma & SaaS
- ✅ Login/refresh/TOTP/lockout/rate limiting — autenticação completa e bem feita (PBKDF2, RFC 6238)
- ✅ Ativação de conta por e-mail (com fallback dev)
- ✅ Provisioning de tenant (admin) com seed de roles/policies, trial 14 dias
- ✅ Feature flags por tenant/ambiente/usuário com persistência real (módulo configuration)
- ✅ Config por ambiente com campos criptografados (AES-256-GCM via `[EncryptedField]`)
- ✅ Dead Letter Queue de eventos
- ✅ IA real: `/api/v1/ai/chat` (+streaming) via Ollama, RAG com Qdrant (job de indexação a cada 30min), capability guards (`ai_enabled`/`ai_internal`/`ai_external`), quotas de token
- ✅ Telas de auth, licensing, onboarding wizard (5 passos), admin de tenants

---

## 3. Bugs e funcionalidades incompletas (achados da auditoria)

### 🔴 Críticos — corrigir antes de qualquer piloto

| # | Bug | Local | Impacto |
|---|---|---|---|
| 1 | **SQL injection no ClickHouse** — queries de traces/logs/métricas montadas por interpolação de string; `EscapeSqlString` faz apenas `Replace("'", "\\'")` (insuficiente) | `ClickHouseObservabilityProvider.cs` (~linhas 100-180, 297+), `ClickHouseOtelMetricRepository.cs:84-98` | Vulnerabilidade explorável por qualquer usuário autenticado. Nota: `ClickHouseRepository.cs` (runtime intelligence) usa Dapper parametrizado corretamente — usar como referência do fix. |
| 2 | **Telemetria OTel sem isolamento de tenant** — `OtelMetricRecord` não tem coluna `TenantId`; métricas de todos os tenants se misturam | `OtelMetricRepository.cs:129-142` | Quebra a promessa multi-tenant: um tenant pode ver dados de outro. Inaceitável em SaaS. |
| 3 | **Resolver incidente é impossível pela UI** — handler `ResolveIncident` + `MarkIncidentResolved()` existem, mas nenhum endpoint os expõe; frontend não tem botão | `IncidentEndpointModule.cs` (rota ausente) | Ciclo de vida do incidente incompleto: todo incidente fica "Open" para sempre. |
| 4 | **Eventos de integração nunca publicados** — `ApprovePromotion`/`BlockPromotion` não publicam `PromotionCompleted/Blocked`; criação/resolução de incidente não publica `IncidentCreated/Resolved`. Os handlers de notificação estão prontos e registrados, esperando eventos que nunca chegam | `ApprovePromotion.cs:38-82`, `BlockPromotion.cs`, handlers de incidente | Nenhuma notificação automática dispara na plataforma inteira (exceto custo). Aprovador não é avisado; dono do serviço não sabe de incidente. |
| 5 | **Perda silenciosa de dados na ingestão** — catch genérico retorna 0 sem distinguir falha parcial/total | `OtelMetricRepository.cs:58-64` | Telemetria do cliente some sem alerta. |
| 6 | **Aprovação sem papel de aprovador** — qualquer usuário com `promotion:requests:write` aprova qualquer promoção; `ApprovedBy` vem do cliente sem validação | `PromotionEndpointModule.cs:70-81` | Mina a credibilidade do produto de *governança* — aprovação sem controle de quem aprova. |

### 🟠 Altos — funcionalidade anunciada que não funciona para o usuário

| # | Problema | Local | Impacto |
|---|---|---|---|
| 7 | **SBOM sem endpoints** — `IngestSbomRecord` e `GetSbomCoverageReport` prontos (handler+repo+DI), zero rotas HTTP | `Catalog.Application/Contracts/Features/IngestSbomRecord/`, `GetSbomCoverageReport/` | Feature morta. |
| 8 | **Feature Flags Registry sem endpoints** — `IngestFeatureFlagState`, inventário e risk report prontos, zero rotas | `Catalog.Application/Contracts/Features/IngestFeatureFlagState/` etc. | Exatamente a peça "flags como governança" da estratégia — está pronta e inacessível. |
| 9 | **Agendamento de deprecação sem endpoint** — só "deprecar agora" é exposto | `ScheduleContractDeprecation/` | Parcial. |
| 10 | **Dashboard de observabilidade com números hardcoded** ("1.2M requests", "145ms", "0.8%") + componentes que engolem erros de API silenciosamente (`catch(() => {})`) | `ObservabilityDashboardPage.tsx:67-90`, `RequestMetricsDashboard.tsx:21`, `ErrorAnalyticsDashboard.tsx:21` | Usuário vê dados falsos ou tela vazia sem saber que houve erro. |
| 11 | **PostIncidentPage usa dados de fallback** — backend de PIR funciona, frontend mostra mock constante | `PostIncidentPage.tsx:46-51` | PIRs criados não aparecem. |
| 12 | **Slack não existe** — só Teams implementado; também não há dispatcher de webhook genérico | módulo notifications | Estratégia previa Slack/e-mail/webhook; só e-mail/Teams existem. |
| 13 | **Webhooks inbound de CI/CD ausentes** — subscriptions outbound OK, mas nenhum endpoint recebe push do GitHub/Jira/Jenkins | módulo integrations | A plataforma não enxerga deploys automaticamente — alimentação manual. |
| 14 | **OpenAI provider é stub** — `// TODO: Implementar chamada real à API da OpenAI`; só Ollama funciona. NLP routing também é TODO | `OpenAILLMProvider.cs`, `PromptRouter.cs` | Fallback de IA anunciado não existe. |

### 🟡 Médios

- **GraphQL muito limitado**: catalog expõe só 3 queries; **GraphQL subscriptions do changegovernance não existem** (CLAUDE.md anuncia, código não tem) — UI depende de refresh manual.
- **Canary**: entidade/repos/recording existem, mas sem lógica de decisão (promover/rollback), sem métricas de validação e sem UI.
- **Rulesets e SLA**: backend completo, **sem UI** (upload de ruleset, config de SLA).
- **SLO tracking**: framework pronto, alimentado por `NullIncidentKnowledgeReader` com dados fake datados de 2025-10 — telas mostram dados que parecem reais mas não são.
- **`NullMultiDimensionalPromotionConfidenceReader` retorna score 100 fixo** — UI apresenta "confiança perfeita" enganosa; trocar por estado "sem dados".
- **Status page pública: zero implementação** (nenhum endpoint anônimo, nenhuma tela).
- **Retenção hot/warm/cold**: implementada e funcional no Postgres (7/90/180 dias, consolidação minuto→hora) — OK, mas verificar equivalente no ClickHouse.
- Ollama com modelo hardcoded `llama3.2:3b` em `OllamaCompletionClient.cs:25` (divergente do `qwen3.5:9b` documentado).

### Funil comercial (a maior lacuna vs estratégia)

| Item | Status |
|---|---|
| Signup self-service público (criar conta+tenant sem admin) | ❌ Não existe — `CreateUser`/`ProvisionTenant` exigem permissão admin; nenhuma tela pública de registro |
| Billing/pagamento (Stripe ou gateway) | ❌ Zero — `ExternalSubscriptionId`, `BillingCycleStart` existem como campos e nunca são usados |
| Enforcement de planos | ⚠️ Apenas **2 handlers** checam `HasCapability` (ambos de IA). Starter acessa features Enterprise via API direta |
| Upgrade/downgrade de plano com cobrança | ❌ Não existe (UI de licensing existe, sem cobrança) |
| Documentação de cliente | ❌ Docs são internas de engenharia |
| Free tier / trial | ⚠️ Trial 14 dias existe no provisioning (admin-only) |

---

## 4. O que falta implementar para fechar o escopo da estratégia

### Fase 1 — Tornar honesto e seguro o que já existe (pré-requisito para piloto)
1. Corrigir SQL injection no ClickHouse (parametrizar como em `ClickHouseRepository.cs`) — bugs #1
2. Adicionar `TenantId` à telemetria OTel + filtro em todas as queries — bug #2
3. Mapear endpoint + botão de **resolver incidente** (e transições de status) — bug #3
4. Publicar os integration events nos handlers de promoção e incidente (ligar o fio das notificações) — bug #4
5. Papel/permissão de aprovador (`promotion:approve`) + validar `ApprovedBy` no servidor — bug #6
6. Remover números hardcoded e catches silenciosos do dashboard de observabilidade; conectar `PostIncidentPage` ao backend — bugs #10, #11
7. Expor endpoints de SBOM, Feature Flags Registry e agendamento de deprecação (handlers já prontos — trabalho pequeno, valor alto) — bugs #7-9

### Fase 2 — Fechar o escopo funcional do bundle
8. **Slack dispatcher + webhook genérico** no notifications (base do Teams serve de modelo)
9. **Webhooks inbound de CI/CD** (GitHub Actions/GitLab no mínimo) — alimenta releases automaticamente; é o que faz a correlação release↔incidente funcionar sem digitação manual
10. **Status page pública** (endpoint anônimo + página) — feature de alto valor percebido no mid-market e esforço moderado
11. UI para rulesets (upload/bind/findings) e config de SLA
12. Substituir `NullIncidentKnowledgeReader`/confidence reader por implementações reais ou estados explícitos de "sem dados"
13. On-call: escala/plantão mínimos (cadastro de escala + roteamento de notificação) — sem telefonia, conforme estratégia

### Fase 3 — Funil comercial (sem isso não há produto vendável)
14. **Signup self-service**: endpoint público `AllowAnonymous` que cria tenant+usuário+trial e dispara ativação por e-mail (reaproveitar `ProvisionTenant` + `RequestAccountActivation`); tela pública de registro
15. **Billing**: integração Stripe (checkout, webhook `payment_intent.succeeded`/`subscription.*`, preencher `ExternalSubscriptionId`), upgrade/downgrade
16. **Enforcement de capabilities** em todos os handlers de feature paga (behavior do pipeline MediatR, não checagem manual handler a handler)
17. Documentação de cliente (getting started, API, instalação do collector OTel)

### Explicitamente FORA do escopo (estratégia acordada)
Cofre de segredos próprio · runtime de feature flags (SDKs/edge) · agentes de instrumentação próprios · telefonia/SMS de on-call · paridade GraphQL completa.

---

## 5. Verificação executável (frontend)

- **TypeScript (`tsc -b`)**: ✅ 0 erros
- **ESLint**: ✅ 0 erros, 149 warnings (imports/variáveis não usados — cosmético)
- **Vitest**: ✅ 311 arquivos de teste, **2.323 testes, todos passando** (238s)

*Backend não pôde ser compilado neste ambiente (sem SDK .NET); a auditoria backend foi estática, arquivo a arquivo.*

---

## 6. Leitura estratégica final

A tese central do produto — **correlação serviço → contrato → release → incidente — já funciona no código**, e é exatamente a parte que os concorrentes do bundle barato não têm. O catálogo/contratos está em nível demonstrável hoje.

O padrão dominante de problema **não é falta de código, é falta de fio**: handlers prontos sem endpoint, eventos definidos sem publicação, telas prontas lendo de readers nulos. São dezenas de horas de wiring, não meses de desenvolvimento — com as exceções do funil comercial (Fase 3), que é desenvolvimento novo de verdade, e dos 2 bugs de segurança, que são inegociáveis antes de qualquer usuário externo.

Ordem recomendada: **Fase 1 inteira → itens 8-10 da Fase 2 → Fase 3 → restante da Fase 2.** Critério de saída: um piloto externo conseguindo se cadastrar sozinho, conectar um collector OTel, registrar um serviço com contrato, promover uma release com aprovação notificada, e ver um incidente correlacionado — sem nenhum toque de admin.
