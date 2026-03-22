# E2E Go-Live Suite — NexTraceOne

> **Fase 8 — Go-Live Readiness**  
> Versão: 1.0 | Data: 2026-03-22

---

## Propósito

Este documento define a suíte mínima obrigatória de E2E para go-live do NexTraceOne.  
Estes cenários devem passar 100% antes de qualquer promoção para produção.

**Princípio**: a suíte deve ser pequena, confiável e valiosa — não exaustiva e frágil.

---

## Infraestrutura de Execução

### Backend E2E (API)
- **Projeto**: `tests/platform/NexTraceOne.E2E.Tests`
- **Fixture**: `ApiE2EFixture` — PostgreSQL 16 real via Testcontainers + `WebApplicationFactory<Program>`
- **Usuários seedados**: `e2e.admin@nextraceone.test` / `Admin@123`
- **Ambiente**: `ASPNETCORE_ENVIRONMENT=Development`, `NEXTRACE_SKIP_INTEGRITY=true`
- **Execução**: `dotnet test tests/platform/NexTraceOne.E2E.Tests/NexTraceOne.E2E.Tests.csproj`

### Frontend E2E (Playwright)
- **Diretórios**: `src/frontend/e2e/`, `src/frontend/e2e-real/`
- **Execução**: `cd src/frontend && npx playwright test`
- **Pipeline**: `e2e.yml` no GitHub Actions

---

## Suíte Mínima de Go-Live

### E2E-01 — Login e Sessão

**Arquivo**: `AuthApiFlowTests.cs`  
**Criticidade**: P0  
**Status**: ✅ Implementado

| Cenário | Expectativa | Status |
|---------|------------|--------|
| Login com credencial inválida | 400 ou 401 | ✅ |
| Login com email vazio | 400 ou 422 | ✅ |
| Login com senha curta | 400 ou 422 | ✅ |
| Login com usuário E2E válido | 200 + JWT | ✅ |
| Rota protegida sem token | 401 | ✅ |
| Rota protegida com token inválido | 401 | ✅ |
| Cliente autenticado acessa endpoint protegido | 200 | ✅ |

**Dados de teste**:
- Email: `e2e.admin@nextraceone.test`
- Senha: `Admin@123`
- Seedado automaticamente pelo `ApiE2EFixture`

---

### E2E-02 — Health/Readiness

**Arquivo**: `SystemHealthFlowTests.cs`  
**Criticidade**: P0  
**Status**: ✅ Implementado

| Cenário | Expectativa | Status |
|---------|------------|--------|
| `/health` retorna 200 | HTTP 200 | ✅ |
| `/ready` retorna 200 ou 503 | Nunca 404/500 | ✅ |
| `/live` retorna 200 | HTTP 200 | ✅ |
| `/health` retorna JSON com status | Content-Type JSON | ✅ |
| Endpoint protegido sem token | 401 | ✅ |
| POST /auth/login com body vazio | 400 ou 422 | ✅ |

---

### E2E-03 — Catálogo e Source of Truth

**Arquivo**: `ReleaseCandidateSmokeFlowTests.cs`  
**Criticidade**: P0  
**Status**: ✅ Implementado

| Cenário | Expectativa | Status |
|---------|------------|--------|
| GET /catalog/services retorna dados seedados | "Payments Service" presente | ✅ |
| Source of Truth search encontra serviço | "Payments Service" presente | ✅ |
| GET /contracts/summary retorna estrutura | campo `totalVersions` presente | ✅ |

**Dados de teste**:
- Serviço "Payments Service" seedado pela fixture
- Contrato "Payments API v2" seedado

---

### E2E-04 — Releases e Governance

**Arquivo**: `ReleaseCandidateSmokeFlowTests.cs`  
**Criticidade**: P0  
**Status**: ✅ Implementado

| Cenário | Expectativa | Status |
|---------|------------|--------|
| GET /releases retorna versão seedada | "1.3.0" presente | ✅ |
| GET /incidents retorna dados | HTTP 200 | ✅ |

---

### E2E-05 — Runtime e Reliability

**Arquivo**: `RealBusinessApiFlowTests.cs`  
**Criticidade**: P1  
**Status**: ✅ Implementado

| Cenário | Expectativa | Status |
|---------|------------|--------|
| GET /runtime/services/health retorna 200 | HTTP 200 | ✅ |
| Endpoints de governance retornam dados | HTTP 200 | ✅ |

---

### E2E-06 — Catalog e Incidents (cross-module)

**Arquivo**: `CatalogAndIncidentApiFlowTests.cs`  
**Criticidade**: P1  
**Status**: ✅ Implementado

| Cenário | Expectativa | Status |
|---------|------------|--------|
| Catalog + incidents em sequência | Ambos 200 | ✅ |
| Dados cross-module coerentes | Sem conflito | ✅ |

---

### E2E-07 — AI Assistant (fluxo básico)

**Arquivo**: `RealBusinessApiFlowTests.cs`  
**Criticidade**: P1  
**Status**: ✅ Parcial (provider mockado em CI)

| Cenário | Expectativa | Status |
|---------|------------|--------|
| Endpoint AI assistant responde | 200 ou erro tratado | ✅ |
| Sem vazamento de tenant no response | TenantId correto no contexto | ✅ (via unit tests de isolamento) |

**Nota**: Provider LLM real não disponível em CI. O fluxo E2E valida que o endpoint existe e responde; a qualidade da resposta AI é validada em unit tests de isolamento.

---

## Cenários de Jornada Frontend (Playwright)

### Localizados em `src/frontend/e2e-real/`

| Cenário | Arquivo | Status |
|---------|---------|--------|
| Fluxo core real (health + auth + catalog) | `real-core-flows.spec.ts` | ✅ |
| Login e navegação autenticada | `app.spec.ts` | ✅ |
| Catálogo de serviços | `service-catalog.spec.ts` | ✅ |
| Gestão de contratos | `contracts.spec.ts` | ✅ |
| Incidents page | `incidents.spec.ts` | ✅ |
| Change confidence | `change-confidence.spec.ts` | ✅ |

---

## Estratégia de Dados de Teste

### Dados seedados automaticamente pela `ApiE2EFixture`

O `ApiE2EFixture` semeia dados de teste quando o ambiente é Development:

**Utilizadores**:
- `e2e.admin@nextraceone.test` / `Admin@123` — role admin
- `e2e.viewer@nextraceone.test` / `Viewer@123` — role viewer

**Serviços**:
- "Payments Service" — criticidade Critical, status Active
- "Orders Service" — criticidade High, status Active

**APIs**:
- "Payments API v2" — contrato REST, versão 2.0
- Release candidate "1.3.0"

**Incidentes**:
- Incidente crítico seedado para testes de correlação

---

## Estratégia de Execução

### Em CI (automático)
```bash
# Backend E2E
dotnet test tests/platform/NexTraceOne.E2E.Tests/ --logger "trx;LogFileName=e2e-results.trx"

# Frontend E2E (Playwright)
cd src/frontend && npx playwright test
```

### Manual (pré-deploy)
```bash
# Smoke mínimo via curl
APIHOST="http://localhost:8080"
curl -sf "${APIHOST}/live" | jq .
curl -sf "${APIHOST}/ready" | jq .
curl -X POST "${APIHOST}/api/v1/identity/auth/login" \
  -H "Content-Type: application/json" \
  -d '{"email":"e2e.admin@nextraceone.test","password":"Admin@123"}' | jq .accessToken
```

---

## Critérios de Gate de Go-Live

**Condição necessária**: todos os cenários marcados ✅ devem passar em staging antes do deploy de produção.

**Falha aceitável**: E2E-07 (AI com provider real) pode ser skipped em CI se provider indisponível, **desde que** os unit tests de isolamento passem.

**Falha bloqueante**: qualquer cenário E2E-01, E2E-02, E2E-03 ou E2E-04 em falha bloqueia go-live.

---

## Manutenção

- Atualizar este documento ao adicionar novos fluxos críticos
- Não adicionar E2E para funcionalidades fora do scope de release
- Manter suíte executando em < 10 minutos totais
- Cada E2E deve ter dados previsíveis (via seed) — nunca depender de dados criados por outro teste

---

*Documento mantido pelo Release Readiness Lead.*
