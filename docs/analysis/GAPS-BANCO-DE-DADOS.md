# Gaps — Banco de Dados
> Análise dos gaps na camada de persistência do NexTraceOne.

---

## 1. Row-Level Security (RLS) — Não Implementado

**Estado declarado:** PLANNED (mencionado em README e docs de segurança como direcção futura).

**Situação actual:** Isolamento multi-tenant é feito via **query filter no EF Core** (`HasQueryFilter`), não via RLS nativa do PostgreSQL.

**Risco real:**
- Uma query manual (ex: raw SQL, migration script, ferramenta de admin) pode aceder a dados de outro tenant sem filtro aplicado.
- Se um bug no EF Core ou no middleware de tenant bypassar o filtro, há vazamento de dados inter-tenant.
- RLS no PostgreSQL seria uma segunda camada de defesa independente da aplicação.

**Impacto:** Risco de conformidade GDPR/LGPD em ambiente multi-tenant real.

---

## 2. 27 DbContexts — Complexidade de Gestão

**Estado:** 27 DbContexts separados por bounded context. Arquitecturalmente correcto para isolamento.

**Gap operacional:**
- Cada migration tem de ser executada separadamente (ou via script de orquestração).
- Um erro numa migration de um contexto pode deixar o sistema em estado inconsistente.
- Não há evidência de migration orchestration automatizada com rollback coordenado.
- Connection strings: todas apontam para a mesma base de dados em dev — em produção multi-tenant real, a gestão de 27 connection strings por tenant é complexa.

**Risco:** Falha de deploy com migrations parcialmente aplicadas pode corromper estado.

---

## 3. Particionamento de Telemetria — Não Validado

**Estado:** A documentação menciona particionamento por data para dados de telemetria.

**Gap:** Não há evidência de que as migrações de telemetria implementem particionamento nativo do PostgreSQL (`PARTITION BY RANGE`). O particionamento pode estar a ser feito apenas logicamente pela aplicação.

**Impacto:** Sem particionamento real, queries de telemetria históricas vão degradar com o volume de dados.

---

## 4. Full-Text Search — Cobertura Parcial

**Estado:** FTS (Full-Text Search) com índice GIN implementado para `SpecContent` em contratos.

**Gap:** Não há evidência de FTS implementado para:
- Runbooks e documentação (Knowledge module)
- Notas operacionais
- Incidentes (histórico e timeline)
- Audit logs

**Impacto:** Pesquisa global (`/search`) pode ter performance degradada ou resultados incompletos para estes domínios.

---

## 5. Índices — Auditoria Necessária

**Estado:** Migrações adicionam índices pontualmente (ex: `AddContractVersionFtsIndex`).

**Gap:** Não há documento ou auditoria centralizada de índices por tabela e query crítica.

**Queries que provavelmente precisam de índices (não validados):**
- Filtro por `tenant_id` + `environment_id` em tabelas de alta frequência
- Listagem de contratos por serviço com ordenação por `created_at`
- Correlação de incidentes por janela temporal
- Releases por ambiente e estado

---

## 6. Connection Pool — Configuração Conservadora

**Estado actual:**
- Dev: `Maximum Pool Size=10`
- Override: `Maximum Pool Size=20`

**Gap:** 27 DbContexts × 20 conexões máximas = potencial de 540 conexões simultâneas por instância da aplicação. O PostgreSQL tem limite de conexões configurável (padrão: 100).

**Risco:** Em carga real com múltiplos DbContexts activos simultaneamente, pode esgotar o connection pool do PostgreSQL.

**Solução a avaliar:** PgBouncer como connection pooler externo.

---

## 7. Backup e Restore — Procedimento não Testado Automaticamente

**Estado:** Runbook de backup existe (`BACKUP-OPERATIONS-RUNBOOK.md`).

**Gap:** Não há evidência de testes automatizados de restore (backup drills). O CI/CD não testa o processo de restore.

**Risco:** Descobrir que o backup está corrompido ou incompleto apenas durante um incidente real.

---

## 8. Resumo de Prioridades Base de Dados

| Gap | Severidade | Esforço |
|-----|-----------|---------|
| RLS não implementado (multi-tenant) | P0 — Crítico | Alto |
| Migration orchestration sem rollback coordenado | P1 — Alto | Médio |
| Particionamento telemetria não validado | P1 — Alto | Médio |
| FTS incompleto (knowledge, incidents) | P1 — Alto | Médio |
| Auditoria de índices | P2 — Médio | Baixo |
| Connection pool vs. 27 DbContexts | P2 — Médio | Baixo |
| Backup drills automatizados | P2 — Médio | Médio |
