# NexTraceOne — Gaps de Banco de Dados e Testes

> **Parte da série de análise realista.** Ver [ESTADO-ATUAL-PRODUTO.md](./ESTADO-ATUAL-PRODUTO.md) para contexto.

---

## Parte 1: Banco de Dados

### 1.1 Connection Pool — Risco Crítico de Produção

**Configuração atual:**
```json
"ConnectionStrings": {
  "NexTraceOne": "...;Maximum Pool Size=20",
  "IdentityDatabase": "...;Maximum Pool Size=20",
  "CatalogDatabase": "...;Maximum Pool Size=20",
  ...
}
```

**O problema matemático:**
- 23 connection strings × max 20 conexões = **460 conexões máximas simultâneas**
- PostgreSQL padrão: `max_connections = 100`
- Kubernetes com 3 réplicas: 460 × 3 = **1380 conexões potenciais**

**Resultado em produção:** Com mais de 5-6 tenants ativos fazendo requests simultâneos, a aplicação lançará:
```
Npgsql.NpgsqlException: Sorry, too many clients already
```

**O que precisa ser feito:**

Opção A (Rápida): Reduzir `Maximum Pool Size` para 3-5 por connection string
```
23 × 5 × 3 réplicas = 345 conexões — ainda próximo do limite padrão
```

Opção B (Recomendada): Implementar PgBouncer em modo transaction pooling
```
Aplicação → PgBouncer → PostgreSQL
100 conexões do pool → sem limite client-side
```

Opção C: Aumentar `max_connections` no PostgreSQL (requer `shared_buffers` ajustado)
```sql
-- postgresql.conf
max_connections = 500
shared_buffers = 2GB  -- ~25% da RAM disponível
```

**Urgência:** Alta. Este é um bloqueador real para produção com múltiplos tenants.

---

### 1.2 pgvector — Sem Índice Configurado

**O problema:** `pgvector` está instalado (`pgvector:pg16` no docker-compose) e provavelmente tem embeddings armazenados, mas não há configuração de índice nos ficheiros de migration encontrados.

**Impacto:**
- Queries de similaridade semântica (RAG) fazem full table scan
- Com >10.000 vetores, performance degrada para segundos por query
- Em produção com base de conhecimento real, o AI Assistant tornará-se inutilavelmente lento

**Tipos de índice pgvector:**

```sql
-- HNSW (recomendado para balanceamento recall/performance)
CREATE INDEX ON ai_knowledge_embeddings USING hnsw (embedding vector_cosine_ops);

-- IVFFlat (mais rápido para criação, menos recall)
CREATE INDEX ON ai_knowledge_embeddings USING ivfflat (embedding vector_cosine_ops) WITH (lists = 100);
```

**O que fazer:** Adicionar uma migration com o índice HNSW após a migration inicial de qualquer tabela com coluna `vector`.

---

### 1.3 Sem Estratégia de Rollback de Migrations

**O problema:**
A documentação documenta apenas "forward migrations" — o `dotnet ef database update` vai para a frente.

**Cenários de risco:**
1. Deploy em produção com migration defeituosa
2. Migration que altera schema e dados (não é trivialmente reversível)
3. Migration que demora muito e bloqueia tabelas

**O que falta:**
- Procedimento documentado para `dotnet ef database update <MigrationName>` reverter
- Identificação de quais migrations são reversíveis vs. destrutivas
- Script de backup automático pré-migration
- Política sobre migrations com `DROP COLUMN` ou `ALTER COLUMN NOT NULL`

---

### 1.4 Elasticsearch Security Desativado

**Ficheiro:** `docker-compose.yml`
```yaml
XPACK_SECURITY_ENABLED: "false"
```

**O problema:** Mesmo que seja intencional para desenvolvimento, o risco está em:
1. Developers que usam este compose em staging por conveniência
2. O ficheiro `docker-compose.production.yml` que deve ter security ativado — não verificado se está bem configurado
3. Não há health check que valide que em produção o Elasticsearch tem autenticação ativa

**Recomendação:** Adicionar um preflight check `ElasticsearchAuthPreflightCheck` que falhe em `Production` se autenticação não estiver configurada.

---

### 1.5 Sem Read Replicas / Separação Read-Write

**Estado atual:** Todas as queries — leituras e escritas — vão para a mesma instância PostgreSQL.

**Impacto em produção:**
- Queries analíticas pesadas (governance reports, FinOps, observabilidade) competem com writes transacionais
- Sem separação read-write, um report lento pode bloquear ou atrasar writes críticos
- Não há configuração para dirigir `IQueryable<T>.AsNoTracking()` para replica

**O que falta:**
- Configuração de streaming replication no PostgreSQL
- Connection string separada para replica (`...ReadOnly...`)
- Convenção para handlers de Query usar a connection string de read replica

---

### 1.6 Sem Procedimento de Backup Testado

**Estado da documentação:** `docs/deployment/` não inclui um runbook de backup/restore.

**Risco:**
- Em falha catastrófica, a equipa não sabe executar restore
- Sem backup testado regularmente, o backup pode estar corrompido ou desatualizado
- Com 27 DbContexts e RLS, um restore parcial pode deixar dados inconsistentes

**O mínimo necessário:**
```bash
# Backup
pg_dump -h localhost -U nextraceone -d nextraceone \
  --format=custom --compress=9 \
  --file=backup-$(date +%Y%m%d-%H%M%S).dump

# Teste de restore (em ambiente separado)
pg_restore -h restore-test -U nextraceone -d nextraceone_restore backup-xxx.dump
```

---

### 1.7 Multi-Tenant com RLS — Limitações

**O modelo atual:** Um único banco de dados com RLS para isolamento de tenant.

**Limitações que não estão documentadas:**
1. **Backups são "all-or-nothing"** — não é possível fazer backup de um único tenant
2. **Purga de tenant é complexa** — `DELETE WHERE tenant_id = ?` em 50+ tabelas, precisa de coordenação
3. **Tenant com dados muito grandes** pode afetar performance de outros tenants (partilham o mesmo tablespace)
4. **Compliance individual** (GDPR "right to erasure") requer deleter para cada entidade do tenant

**Não é necessariamente um problema** para o modelo atual de negócio, mas precisa estar documentado para clientes enterprise que perguntam sobre isolamento de dados.

---

## Parte 2: Testes

### 2.1 Load Tests Nunca Executados em CI

**Localização:** `tests/load/` com cenários k6 completos

**O que existe:**
- `auth-load.js` — autenticação (1/10/25 VUs)
- `catalog-load.js` — catálogo (1/15/30 VUs)
- `contracts-load.js` — contratos (1/10/20 VUs)
- `governance-load.js` — governance (1/10 VUs)
- `mixed-load.js` — journey realista (20/40 VUs)

**O que falta:**
- **Nenhum** dos workflows GitHub Actions executa `k6 run`
- Não há baseline estabelecido (nunca correram em staging)
- Não há thresholds validados (p95 < 2000ms nunca verificado)
- Não há relatório de tendência de performance ao longo do tempo

**Impacto:** O produto nunca foi testado sob carga real. As queries EF Core, os 19+ outbox processors, e os 27 DbContexts nunca foram validados sob concorrência real.

**O que fazer:**
```yaml
# .github/workflows/load-test.yml
- name: Run load tests (smoke)
  run: k6 run --vus 1 --duration 30s tests/load/scenarios/mixed-load.js
  # Smoke test em cada PR — não carga total, só smoke
```

---

### 2.2 Contract Testing Cross-Module — Ausente

**Problema:** Com 15 interfaces cross-module (`IIncidentModule`, `ICatalogGraphModule`, `IReliabilityModule`, etc.), não há testes que garantam que o contrato entre módulos é preservado.

**Cenário de risco:**
1. Módulo A refatora `IContractsModule.GetContractSummary()` adicionando um parâmetro obrigatório
2. Módulo B que consome esta interface compila com erro
3. **Mas e se a refatoração for retrocompatível no contrato mas mudar o comportamento?** Isso passa silenciosamente

**O que existe hoje:** Testes unitários por módulo com mocks das interfaces cross-module — mas os mocks são configurados pelo próprio módulo que os escreve, sem validação de que o comportamento real do módulo implementador corresponde ao mock.

**O que falta:**
- Consumer-Driven Contract Tests (Pact.NET)
- Testes de integração que exercitem o path real cross-module sem mocks

---

### 2.3 Selenium — Autenticação Mockada

**Ficheiro:** `tests/platform/NexTraceOne.Selenium.Tests/Infrastructure/SeleniumTestBase.cs`

```csharp
protected void MockAuthSessionWithProfileIntercept()
{
    // Injeta cookie de sessão fake diretamente no browser
    // sem passar pelo flow real de login
}
```

**Impacto:**
- O flow de login JWT não é testado de forma E2E
- O refresh token automático (401 → refresh → retry) não é testado
- Expiração de sessão não é testada
- MFA flow não é testado de forma integrada

**O que deveria existir:**
```csharp
// Teste real de auth
[Fact]
public async Task Login_WithValidCredentials_Should_SetSession()
{
    await Driver.NavigateTo("/login");
    await Driver.FindElement(By.Id("email")).SendKeys("admin@acme.com");
    await Driver.FindElement(By.Id("password")).SendKeys("Admin123!");
    await Driver.FindElement(By.CssSelector("button[type=submit]")).Click();
    // Assert redirect to dashboard
}
```

---

### 2.4 Integration Tests — Cobertura Limitada a Migrations

**O que os testes de integração fazem bem:**
- Verificam que migrations foram aplicadas
- Verificam que tabelas existem
- Testam persistência e leitura de entidades individuais por módulo

**O que não testam:**
- Flows cross-module (e.g., criar release → aciona outbox → notification é enviada)
- RLS em ação (tenant A não consegue ver dados do tenant B)
- Rate limiting real (quantas requests por minuto bloqueiam?)
- Cenários de concorrência (dois requests simultâneos ao mesmo recurso)
- Comportamento do outbox quando a DB está lenta

---

### 2.5 Mutation Testing — Ausente

**Problema:** Com 737 testes no backend, a questão não é "temos testes suficientes?" mas "os testes verificam o que pensamos que verificam?"

**Exemplo de teste que passa mas não garante nada:**
```csharp
[Fact]
public async Task CreateService_Should_Succeed()
{
    var result = await handler.Handle(command, CancellationToken.None);
    result.IsSuccess.Should().BeTrue();
    // Não verifica: dados persistidos, eventos de domínio disparados, audit trail criado
}
```

**Mutation testing** (Stryker.NET) introduziria mutações no código (e.g., troca `>` por `>=`) e verificaria que algum teste falha. Se nenhum teste falhar para uma mutação, o teste é ineficaz.

**Custo:** Stryker.NET é pesado para CI frequente, mas pode correr semanalmente ou em PRs para módulos críticos.

---

### 2.6 Chaos Engineering — Não Integrado

**O que existe:** Entidade `ChaosExperiment` em `RuntimeIntelligenceDbContext`, 20+ features de chaos engineering no domínio.

**O que não existe:** Integração com qualquer runner de chaos (LitmusChaos, Chaos Monkey, ou mesmo testes de chaos simples em CI).

**Gap real:** O produto tem a UI para definir chaos experiments, mas nunca são executados. É como ter um checklist de segurança incêndio mas nunca fazer um simulacro.

---

### 2.7 Testes de Security — Sem Penetration Testing

**Estado:** Scanners estáticos (GitHub `security.yml` workflow) estão configurados.

**O que falta:**
- OWASP ZAP ou Burp Suite scan automatizado contra a API
- Teste de SQL injection contra endpoints com parâmetros livres
- Teste de CSRF bypass
- Teste de rate limiting bypass
- Verificação de que JWT secret rotation não quebra sessões ativas

---

## Parte 3: CI/CD — Gaps

### 3.1 Load Tests Ausentes do Pipeline

Ver secção 2.1.

### 3.2 Sem Smoke Test em Produção Pós-Deploy

**Estado:** Os workflows CI/CD fazem build e deploy, mas não há um health check pós-deploy que valide que o sistema está funcional após o deploy.

**Risco:** Um deploy pode ser "verde" no CI mas a aplicação pode não estar a responder corretamente em produção.

**O mínimo necessário:**
```yaml
- name: Post-deploy smoke test
  run: |
    curl -f https://api.nextraceone.io/health || exit 1
    curl -f https://api.nextraceone.io/api/v1/catalog/services \
      -H "Authorization: Bearer ${{ secrets.SMOKE_TEST_TOKEN }}" || exit 1
```

### 3.3 Sem Rollback Automático

**Estado:** Os workflows de deploy não têm passo de rollback automático se o smoke test falhar pós-deploy.

---

## Parte 4: Documentação Técnica

### 4.1 OpenAPI Annotations Incompletas

**Estado:** Endpoints minimal API têm anotações básicas (`.WithSummary()`, `.WithDescription()`) mas:
- Schemas de request/response não estão todos documentados com exemplos
- Códigos de erro possíveis não estão documentados
- Paginação e filtros não têm descrições dos parâmetros
- Autenticação necessária não é sempre indicada por endpoint

**Impacto:** A documentação Scalar (`/api/v1/scalar`) existe mas é incompleta para integração por terceiros ou outras equipas.

### 4.2 Runbooks Genéricos

**Localização:** `docs/runbooks/`

**Problema:** Os runbooks documentam procedimentos genéricos mas não incluem:
- Comandos específicos do NexTraceOne para diagnosticar problemas
- Queries SQL para verificar estado do banco
- Como interpretar os health checks específicos
- Thresholds de alerta e ações correspondentes

### 4.3 Changelog Ausente

**Estado:** Não há `CHANGELOG.md` nem formato de release notes estabelecido.

**Impacto:** Impossível para clientes perceberem o que mudou entre versões. Impossível para a equipa rastrear quando um bug foi introduzido.

---

## Resumo de Prioridades

### Crítico (Bloqueador de Produção)

1. **Connection pool sizing** — Implementar PgBouncer ou reduzir pool size
2. **Backup/restore** — Documentar e testar procedimento antes de produção
3. **Smoke test pós-deploy** — Adicionar ao pipeline CI/CD

### Alto (Antes do Primeiro Cliente Real)

4. **Load tests em CI** — Smoke load test em cada PR
5. **pgvector índice** — Adicionar migration com HNSW index
6. **Selenium auth real** — Corrigir para testar flow de login real

### Médio (Próximo Quarter)

7. **Contract testing cross-module** — Pact.NET para interfaces críticas
8. **Elasticsearch security em prod** — Preflight check para ambientes de produção
9. **Read replica** — Configuração e connection string separada
10. **OpenAPI annotations** — Completar para endpoints públicos principais

### Baixo (Backlog)

11. **Mutation testing** — Stryker.NET para módulos críticos (semanal)
12. **Rollback strategy** — Documentar migrações reversíveis vs. destrutivas
13. **Chaos engineering** — Integrar com scheduler real
14. **Changelog** — Formato e processo estabelecido
