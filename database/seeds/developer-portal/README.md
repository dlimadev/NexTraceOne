# Massa de Teste — Developer Portal

## Objetivo

Esta massa de teste fornece um conjunto completo de dados fictícios para o submódulo **Developer Portal** do bounded context **Catalog** do NexTraceOne. Permite validar funcionalmente os fluxos de subscrição, playground interativo, geração de código, analytics de utilização e pesquisas salvas sem dependências externas.

Os dados cobrem cenários enterprise reais:
- Subscrições de notificação com diferentes níveis e canais
- Sessões do playground com múltiplos métodos HTTP e códigos de resposta
- Geração de código em 5 linguagens com e sem inteligência artificial
- Eventos de analytics cobrindo todos os tipos de interação no portal
- Pesquisas salvas com filtros variados para reutilização

> ⚠️ **ATENÇÃO: Dados exclusivamente para desenvolvimento e testes locais. Nunca executar em produção.**

---

## Pré-requisitos

1. **PostgreSQL 16** em execução (local ou Docker)
2. **Schema de base de dados migrado** — as tabelas `dp_*` do Developer Portal devem existir
3. **Seeds do IdentityAccess executados** — os utilizadores referenciados (prefixo `u1`) devem existir
4. **Seeds do EngineeringGraph executados** — os ativos de API referenciados (prefixo `e2`) devem existir

```bash
# Aplicar migrações EF Core (cria as tabelas)
dotnet ef database update \
  --project src/modules/catalog/NexTraceOne.Catalog.Infrastructure \
  --startup-project src/platform/NexTraceOne.ApiHost
```

---

## Dependências Cross-Module

| Módulo | Entidade | Prefixo de ID | Utilização |
|--------|----------|---------------|------------|
| **IdentityAccess** | Users | `u1000000-...` | `SubscriberId`, `UserId`, `RequestedById` |
| **EngineeringGraph** | API Assets | `e2000000-...` | `ApiAssetId` em subscrições, playground, geração de código |

**Ordem de execução recomendada:**
1. `database/seeds/identity-access/` (tenants, utilizadores, roles)
2. `database/seeds/engineering-graph/` (serviços, APIs, consumidores)
3. `database/seeds/developer-portal/` (este módulo)

---

## Ordem de Execução

| # | Ficheiro | Descrição | Registos |
|---|---------|-----------|----------|
| 00 | `00-reset-developer-portal-test-data.sql` | **Reset completo** — elimina todos os dados de teste respeitando dependências | 0 (delete) |
| 01 | `01-seed-subscriptions.sql` | Subscrições de notificação para APIs com diferentes níveis e canais | 8 subscrições |
| 02 | `02-seed-playground-sessions.sql` | Sessões de execução sandbox com vários métodos HTTP e status codes | 10 sessões |
| 03 | `03-seed-code-generation.sql` | Registos de geração de código em 5 linguagens e 4 tipos de artefacto | 6 registos |
| 04 | `04-seed-analytics-events.sql` | Eventos de analytics cobrindo todos os tipos de interação | 15 eventos |
| 05 | `05-seed-saved-searches.sql` | Pesquisas salvas com critérios e filtros variados | 4 pesquisas |
| 06 | `06-seed-multi-protocol-analytics.sql` | Analytics multi-protocolo: SOAP/WSDL e Kafka/AsyncAPI | 10 eventos |

---

## IDs Determinísticos

| Prefixo | Entidade | Intervalo |
|---------|----------|-----------|
| `d1` | Subscriptions | `d1000000-...-000000000001` a `d1000000-...-000000000008` |
| `d2` | PlaygroundSessions | `d2000000-...-000000000001` a `d2000000-...-00000000000a` |
| `d3` | CodeGenerationRecords | `d3000000-...-000000000001` a `d3000000-...-000000000006` |
| `d4` | PortalAnalyticsEvents | `d4000000-...-000000000001` a `d4000000-...-000000000025` |
| `d5` | SavedSearches | `d5000000-...-000000000001` a `d5000000-...-000000000004` |

---

## Dados de Teste — Resumo

### Subscrições (dp_subscriptions)

| ID | API | Utilizador | Nível | Canal | Ativa |
|----|-----|-----------|-------|-------|-------|
| d1...01 | Payments API | dev@acme-corp.test | BreakingChangesOnly | Email | ✅ |
| d1...02 | Payments API | techlead@acme-corp.test | AllChanges | Webhook | ✅ |
| d1...03 | Refunds API | dev@acme-corp.test | DeprecationNotices | Email | ✅ |
| d1...04 | Processing API | multi@globex-inc.test | SecurityAdvisories | Webhook | ✅ |
| d1...05 | Settlements API | admin@acme-corp.test | AllChanges | Email | ✅ |
| d1...06 | Reconciliation API | viewer@acme-corp.test | DeprecationNotices | Email | ❌ |
| d1...07 | Payments API | devonly@globex-inc.test | BreakingChangesOnly | Webhook | ✅ |
| d1...08 | Processing API | security@acme-corp.test | SecurityAdvisories | Email | ✅ |

### Sessões do Playground (dp_playground_sessions)

| ID | API | Método | Path | Status | Duração (ms) |
|----|-----|--------|------|--------|-------------|
| d2...01 | Payments API | GET | /api/v2/payments?page=1&size=20 | 200 | 87 |
| d2...02 | Payments API | POST | /api/v2/payments | 201 | 234 |
| d2...03 | Payments API | GET | /api/v2/payments/pay-nonexistent | 404 | 45 |
| d2...04 | Refunds API | POST | /api/v1/refunds | 422 | 32 |
| d2...05 | Settlements API | GET | /api/v1/settlements?from=...&to=... | 200 | 1520 |
| d2...06 | Payments API | PUT | /api/v2/payments/pay-002 | 200 | 156 |
| d2...07 | Payments API | DELETE | /api/v2/payments/pay-002 | 401 | 18 |
| d2...08 | Reconciliation API | GET | /api/v1/reconciliation/batch/2025-02 | 504 | 30000 |
| d2...09 | Processing API | PATCH | /api/v3/processing/proc-001/status | 200 | 98 |
| d2...0a | Payments API | POST | /api/v2/payments (stress test) | 500 | 5200 |

### Geração de Código (dp_code_generation_records)

| ID | API | Linguagem | Tipo | IA |
|----|-----|-----------|------|-----|
| d3...01 | Payments API v2.1.0 | CSharp | SdkClient | ❌ |
| d3...02 | Payments API v2.1.0 | TypeScript | IntegrationExample | ✅ |
| d3...03 | Refunds API v1.0.0 | Python | ContractTest | ❌ |
| d3...04 | Processing API v3.0.0 | Java | DataModels | ✅ |
| d3...05 | Settlements API v1.2.0 | Go | SdkClient | ❌ |
| d3...06 | Reconciliation API v1.0.0 | CSharp | IntegrationExample | ✅ |

### Analytics (dp_portal_analytics_events)

| ID | Tipo | Utilizador | Detalhes |
|----|------|-----------|----------|
| d4...01 | Search | Developer | Pesquisa "payments" com resultados |
| d4...02 | Search | Developer | Pesquisa "blockchain transfer" — zero resultados |
| d4...03 | ApiView | Developer | Visualização Payments API |
| d4...04 | PlaygroundExecution | Developer | GET Payments API — 200 |
| d4...05 | PlaygroundExecution | Developer | POST Payments API — 201 |
| d4...06 | CodeGeneration | Developer | SDK C# para Payments API |
| d4...07 | SubscriptionCreated | Developer | Subscrição Breaking + Email |
| d4...08 | DocumentViewed | TechLead | Documentação Refunds API |
| d4...09 | OnboardingStarted | DevOnly | Início do fluxo de boas-vindas |
| d4...0a | OnboardingCompleted | DevOnly | Conclusão do onboarding (7.5 min) |
| d4...0b | Search | TechLead | Pesquisa "authentication oauth" |
| d4...0c | ApiView | Multi-tenant | Visualização Processing API |
| d4...0d | CodeGeneration | Multi-tenant | Data Models Java com IA |
| d4...0e | Search | Anónimo | Pesquisa sem login |
| d4...0f | DocumentViewed | Admin | Changelog Settlements API |

---

## Cenários de Teste Viabilizados

### Subscrições

| Cenário | Descrição |
|---------|-----------|
| Subscrição activa por e-mail | Verificar envio de notificação para subscritor com canal Email |
| Subscrição activa por webhook | Verificar envio para URL webhook configurada |
| Subscrição inactiva | Confirmar que notificações não são enviadas para subscrições desactivadas |
| Múltiplos subscritores por API | Payments API tem 3 subscritores — validar fan-out |
| Subscrição cross-tenant | Utilizadores de tenants diferentes subscritos à mesma API |
| Filtragem por nível | Verificar que BreakingChangesOnly ignora mudanças additive |

### Playground

| Cenário | Descrição |
|---------|-----------|
| Execução com sucesso (2xx) | GET 200, POST 201, PUT 200, PATCH 200 |
| Erro do cliente (4xx) | 401 Unauthorized, 404 Not Found, 422 Validation Error |
| Erro do servidor (5xx) | 500 Internal Error, 504 Gateway Timeout |
| Resposta lenta | Sessão com DurationMs > 1000ms para alertas de performance |
| Vários métodos HTTP | GET, POST, PUT, DELETE, PATCH cobertos |
| Trilha de auditoria | Cada execução preserva request/response completos |

### Geração de Código

| Cenário | Descrição |
|---------|-----------|
| Geração por template | Artefactos com TemplateId preenchido |
| Geração por IA | Artefactos com IsAiGenerated = true e sem TemplateId |
| Múltiplas linguagens | CSharp, TypeScript, Python, Java, Go |
| Múltiplos tipos | SdkClient, IntegrationExample, ContractTest, DataModels |
| Auditoria de geração | Verificar rastreabilidade completa (quem, quando, API, versão) |

### Analytics

| Cenário | Descrição |
|---------|-----------|
| Pesquisa com resultados | Validar contagem e filtros aplicados |
| Pesquisa sem resultados | Identificar lacunas no catálogo (ZeroResults = true) |
| Funil de adoção | Search → ApiView → Playground → CodeGeneration → Subscribe |
| Onboarding completo | OnboardingStarted seguido de OnboardingCompleted |
| Evento anónimo | Evento sem UserId (portal público) |
| Métricas por utilizador | Agrupar eventos por UserId para dashboards |
| Métricas por API | Agrupar eventos por EntityId para popularidade |

### Pesquisas Salvas

| Cenário | Descrição |
|---------|-----------|
| Reutilização de pesquisa | LastUsedAt mais recente que CreatedAt |
| Pesquisa com filtros JSON | Filtros complexos serializados correctamente |
| Pesquisas por utilizador | Listar pesquisas de um utilizador específico |
| Actualização de query | Modificar critérios de pesquisa existente |

---

## Como Executar

### Execução completa (reset + seed)

```bash
cd database/seeds/developer-portal/

psql -h localhost -U nextraceone -d nextraceone_dev \
  -f 00-reset-developer-portal-test-data.sql \
  -f 01-seed-subscriptions.sql \
  -f 02-seed-playground-sessions.sql \
  -f 03-seed-code-generation.sql \
  -f 04-seed-analytics-events.sql \
  -f 05-seed-saved-searches.sql
```

### Execução com Docker

```bash
docker exec -i nextraceone-db psql -U nextraceone -d nextraceone_dev < 00-reset-developer-portal-test-data.sql
docker exec -i nextraceone-db psql -U nextraceone -d nextraceone_dev < 01-seed-subscriptions.sql
docker exec -i nextraceone-db psql -U nextraceone -d nextraceone_dev < 02-seed-playground-sessions.sql
docker exec -i nextraceone-db psql -U nextraceone -d nextraceone_dev < 03-seed-code-generation.sql
docker exec -i nextraceone-db psql -U nextraceone -d nextraceone_dev < 04-seed-analytics-events.sql
docker exec -i nextraceone-db psql -U nextraceone -d nextraceone_dev < 05-seed-saved-searches.sql
```

### Execução selectiva (apenas reset)

```bash
psql -h localhost -U nextraceone -d nextraceone_dev \
  -f 00-reset-developer-portal-test-data.sql
```

### Script de conveniência (todos de uma vez)

```bash
cat database/seeds/developer-portal/*.sql | \
  psql -h localhost -U nextraceone -d nextraceone_dev
```

> **Nota:** Os scripts usam `ON CONFLICT ("Id") DO NOTHING` para idempotência — é seguro re-executar sem o reset prévio, embora o reset garanta um estado limpo.

---

## Aviso de Segurança

> ⚠️ **ATENÇÃO: Dados exclusivamente para desenvolvimento e testes locais.**

- Os IDs dos registos são **determinísticos e previsíveis** (ex: `d1000000-...-01`) — nunca usar este padrão em produção.
- Os e-mails, URLs de webhook e dados são **completamente fictícios**.
- O código gerado nos seeds é **exemplificativo** — não representa artefactos reais de produção.
- O script `00-reset-developer-portal-test-data.sql` **elimina dados irrecuperáveis** — confirmar sempre o ambiente antes de executar.
- **Nunca executar estes scripts em ambientes de produção, staging ou qualquer ambiente com dados reais.**
