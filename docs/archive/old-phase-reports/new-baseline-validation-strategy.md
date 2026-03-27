# Estratégia de Validação da Nova Baseline

> **Status:** DRAFT
> **Data:** 2026-03-25
> **Fase:** N15 — Estratégia de Transição de Persistência

---

## Objetivo

Garantir que a recriação da persistência será validada com rigor, cobrindo PostgreSQL, ClickHouse, seeds, prefixos, startup e funcionalidades core.

---

## Princípios de Validação

1. **Cada onda tem validação própria** — uma onda só está completa após validação
2. **Validação automatizada preferida** — scripts > verificação manual
3. **Falha rápida** — se a baseline falha, o módulo não avança
4. **Zero-state boot** — a aplicação deve subir do zero sem dados pré-existentes

---

## Checklist de Validação por Categoria

### A. Criação Limpa do PostgreSQL

| # | Teste | Como Validar | Automatizável |
|---|-------|-------------|--------------|
| A-01 | Banco de dados pode ser criado do zero | `dotnet ef database update` para cada DbContext, num DB vazio | ✅ |
| A-02 | Todas as tabelas são criadas com prefixo correcto | Query `information_schema.tables` e validar naming | ✅ |
| A-03 | Nenhuma tabela orfã do schema antigo | Comparar tabelas no DB com lista esperada | ✅ |
| A-04 | `__EFMigrationsHistory` contém apenas 1 entry por DbContext | Query à tabela | ✅ |
| A-05 | Zero erros durante criação | Log de output do `database update` | ✅ |

### B. Criação Limpa do ClickHouse

| # | Teste | Como Validar | Automatizável |
|---|-------|-------------|--------------|
| B-01 | Schema ClickHouse pode ser criado do zero | DDL script execution | ✅ |
| B-02 | Tabelas ClickHouse com naming correcto (`pan_`, `ops_`, etc.) | Query `system.tables` | ✅ |
| B-03 | Engines correctos (MergeTree, SummingMergeTree, etc.) | Query `system.tables` engine column | ✅ |
| B-04 | Zero erros durante criação | Log de output | ✅ |

### C. Validação de Prefixos

| # | Teste | Como Validar | Automatizável |
|---|-------|-------------|--------------|
| C-01 | Todas as tabelas Identity começam com `iam_` | SQL query | ✅ |
| C-02 | Todas as tabelas Environment começam com `env_` | SQL query | ✅ |
| C-03 | Todas as tabelas Catalog começam com `cat_` ou `dp_` | SQL query | ✅ |
| C-04 | Todas as tabelas Contracts começam com `ctr_` | SQL query | ✅ |
| C-05 | Todas as tabelas Change Gov começam com `chg_` | SQL query | ✅ |
| C-06 | Todas as tabelas OpIntel começam com `ops_` | SQL query | ✅ |
| C-07 | Todas as tabelas AI Knowledge começam com `aik_` | SQL query | ✅ |
| C-08 | Todas as tabelas Governance começam com `gov_` | SQL query | ✅ |
| C-09 | Todas as tabelas Configuration começam com `cfg_` | SQL query | ✅ |
| C-10 | Todas as tabelas Audit começam com `aud_` | SQL query | ✅ |
| C-11 | Todas as tabelas Notifications começam com `ntf_` | SQL query | ✅ |
| C-12 | Todas as tabelas Integrations começam com `int_` | SQL query | ✅ |
| C-13 | Todas as tabelas Product Analytics começam com `pan_` | SQL query | ✅ |
| C-14 | Nenhuma tabela sem prefixo (excepto outbox e migrations history) | SQL query | ✅ |

### D. Validação de FKs e Índices

| # | Teste | Como Validar | Automatizável |
|---|-------|-------------|--------------|
| D-01 | FKs intra-módulo existem e são válidas | Query `information_schema.table_constraints` | ✅ |
| D-02 | Zero FKs cross-module | Query FK references e verificar prefixos | ✅ |
| D-03 | Índice em TenantId existe em todas as tabelas com tenant | Query `pg_indexes` | ✅ |
| D-04 | PKs são UUID em todas as tabelas | Query column types | ✅ |
| D-05 | xmin concurrency token configurado | Verificar EF configuration | ⚠️ Manual |

### E. Validação de Seeds

| # | Teste | Como Validar | Automatizável |
|---|-------|-------------|--------------|
| E-01 | Configuration seeds aplicados (~345 definitions) | Count query | ✅ |
| E-02 | Identity roles seedados (7 roles) | Count + name query | ✅ |
| E-03 | Identity permissions seedados (73+ excluindo licensing) | Count query | ✅ |
| E-04 | Default tenant criado | Exists query | ✅ |
| E-05 | Environments padrão criados (Dev, Staging, Prod) | Count + name query | ✅ |
| E-06 | Zero permissões licensing no seed | Query por `licensing:*` | ✅ |
| E-07 | Seeds idempotentes (re-executar não duplica) | Execute seeds 2x, verify count unchanged | ✅ |

### F. Validação de Startup da Aplicação

| # | Teste | Como Validar | Automatizável |
|---|-------|-------------|--------------|
| F-01 | Aplicação sobe sem erros | `dotnet run` + health check | ✅ |
| F-02 | Health endpoint responde 200 | HTTP GET /health | ✅ |
| F-03 | Todos os DbContexts conectam | Startup dependency injection validation | ✅ |
| F-04 | Nenhum EnsureCreated no startup | Code analysis (já confirmado: 0) | ✅ |
| F-05 | Zero warnings de schema mismatch no log | Log analysis | ⚠️ Manual |

### G. Validação de Login

| # | Teste | Como Validar | Automatizável |
|---|-------|-------------|--------------|
| G-01 | Login com admin user funciona | HTTP POST /api/v1/auth/login | ✅ |
| G-02 | JWT token retornado válido | Token decode + validation | ✅ |
| G-03 | Session criada no DB | Query `iam_sessions` | ✅ |
| G-04 | Permissions carregadas no token/context | Verify claims | ✅ |

### H. Validação dos Módulos Core

| # | Teste | Como Validar | Automatizável |
|---|-------|-------------|--------------|
| H-01 | Listar users funciona | GET /api/v1/users | ✅ |
| H-02 | Listar environments funciona | GET /api/v1/environments | ✅ |
| H-03 | Listar configurations funciona | GET /api/v1/configuration | ✅ |
| H-04 | Listar roles funciona | GET /api/v1/roles | ✅ |
| H-05 | CRUD básico funciona por módulo | Smoke test endpoints | ✅ |

### I. Validação de Tenant

| # | Teste | Como Validar | Automatizável |
|---|-------|-------------|--------------|
| I-01 | Dados filtrados por TenantId | Query com diferentes tenants | ✅ |
| I-02 | RLS activo e funcional | Tentativa de acesso cross-tenant | ✅ |
| I-03 | Tenant default existe | Query `iam_tenants` | ✅ |

### J. Validação de Ambiente

| # | Teste | Como Validar | Automatizável |
|---|-------|-------------|--------------|
| J-01 | Environment scoping funciona | Query filtrando por EnvironmentId | ✅ |
| J-02 | Ambientes padrão acessíveis | GET /api/v1/environments | ✅ |

### K. Validação de Auditoria

| # | Teste | Como Validar | Automatizável |
|---|-------|-------------|--------------|
| K-01 | Acções críticas geram audit events | Executar acção + query `aud_events` | ✅ |
| K-02 | Login gera security event | Login + query `iam_security_events` | ✅ |

### L. Validação de Eventos Analíticos (ClickHouse)

| # | Teste | Como Validar | Automatizável |
|---|-------|-------------|--------------|
| L-01 | Evento de uso pode ser inserido no ClickHouse | INSERT + SELECT | ✅ |
| L-02 | Aggregation tables funcionam | INSERT + SUM query | ✅ |
| L-03 | Retention policy configurada | Show CREATE TABLE | ✅ |

### M. Validação de Ausência de EnsureCreated

| # | Teste | Como Validar | Automatizável |
|---|-------|-------------|--------------|
| M-01 | Zero chamadas EnsureCreated no codebase | `grep -r "EnsureCreated" src/` | ✅ |
| M-02 | Zero chamadas EnsureCreated no startup path | Runtime trace | ⚠️ Manual |

---

## Script de Validação Sugerido

```bash
#!/bin/bash
# validate-baseline.sh

echo "=== A. PostgreSQL Clean Creation ==="
dotnet ef database update --context ConfigurationDbContext
dotnet ef database update --context IdentityDbContext
# ... repeat for all DbContexts

echo "=== C. Prefix Validation ==="
psql -c "SELECT table_name FROM information_schema.tables WHERE table_schema='public' AND table_name NOT LIKE 'iam_%' AND table_name NOT LIKE 'env_%' AND table_name NOT LIKE 'cat_%' AND table_name NOT LIKE 'cfg_%' AND table_name NOT LIKE 'aud_%' -- etc ORDER BY table_name;"

echo "=== E. Seed Validation ==="
psql -c "SELECT COUNT(*) FROM iam_roles;"           -- expect 7
psql -c "SELECT COUNT(*) FROM iam_permissions WHERE key NOT LIKE 'licensing:%';" -- expect 73+
psql -c "SELECT COUNT(*) FROM cfg_definitions;"     -- expect ~345

echo "=== F. Startup ==="
dotnet run &
sleep 10
curl -f http://localhost:5000/health

echo "=== G. Login ==="
curl -X POST http://localhost:5000/api/v1/auth/login -d '...'

echo "=== M. EnsureCreated Check ==="
grep -r "EnsureCreated" src/ | wc -l  # expect 0
```

---

## Critérios de Aprovação

| Categoria | Mínimo para Aprovar |
|-----------|-------------------|
| A. PostgreSQL | 100% pass |
| B. ClickHouse | 100% pass (quando aplicável) |
| C. Prefixos | 100% pass |
| D. FKs/Índices | 95% pass |
| E. Seeds | 100% pass |
| F. Startup | 100% pass |
| G. Login | 100% pass |
| H. Core | 90% pass |
| I-L. | 80% pass |
| M. | 100% pass |
