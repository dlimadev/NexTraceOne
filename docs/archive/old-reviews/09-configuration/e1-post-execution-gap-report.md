# E1 — Configuration Module Post-Execution Gap Report

## Data
2026-03-25

## Resumo

Este relatório documenta o que foi resolvido, o que ainda ficou pendente,
e o que depende de outras fases após a execução do E1 para o módulo Configuration.

---

## ✅ Resolvido Nesta Fase

| Item | Categoria | Estado |
|------|-----------|--------|
| RowVersion (xmin) em ConfigurationDefinition e ConfigurationEntry | Domínio + Persistência | ✅ Concluído |
| IsDeprecated / DeprecatedMessage em ConfigurationDefinition | Domínio | ✅ Concluído |
| FK: cfg_entries → cfg_definitions | Persistência | ✅ Concluído |
| FK: cfg_audit_entries → cfg_entries | Persistência | ✅ Concluído |
| Check constraints: Category, ValueType, Scope, version >= 1 | Persistência | ✅ Concluído |
| Índices: SortOrder, DefinitionId, IsActive (filtered), ChangedBy | Persistência | ✅ Concluído |
| Validação de tipo de valor no SetConfigurationValue | Backend | ✅ Concluído |
| Rejeição de escritas em definições obsoletas | Backend | ✅ Concluído |
| ConfigurationDbContext na lista de migrações | Startup | ✅ Concluído |
| Seeder executável em todos os ambientes | Seeds | ✅ Concluído |
| ConfigurationDefinitionDto com IsDeprecated | Contratos/Frontend | ✅ Concluído |
| README do módulo | Documentação | ✅ Concluído |
| Verificação de EnsureCreated (confirmada ausência) | Persistência | ✅ N/A |
| Build: 0 erros | Validação | ✅ |
| Testes: 251/251 passam | Validação | ✅ |

---

## ⏳ Pendente — Depende de Outras Fases

| Item | Categoria | Bloqueador | Fase Esperada |
|------|-----------|------------|---------------|
| Gerar migration `InitialCreate` (baseline) | Persistência | Requer que TODOS os modelos estejam finais | E-Baseline (Wave 1) |
| Remover migrations antigas | Persistência | Não aplicável — Configuration não tem migrations existentes | N/A |
| Validação de `ValidationRules` (JSON schema enforcement) | Backend | Complexidade média, requer definição do formato de schema | E2 ou Sprint dedicado |
| Suporte a janela temporal (EffectiveFrom/EffectiveTo) no handler | Backend | Funcionalidade de prioridade média | E2 |
| Paginação em GetEntries, GetDefinitions, GetAuditHistory | Backend | Funcionalidade de prioridade baixa | E2 |
| Consistência de response shape em GetEffectiveSettings | Backend | Low priority | E2 |
| Decomposição de páginas grandes (38KB + 45KB) | Frontend | Refactoring de UX, não bloqueia | Futuro |
| Error boundaries no frontend | Frontend | Boa prática, não bloqueia | Futuro |
| Verificação completa de i18n (pt-BR, es) | Frontend | QA | E2 |
| Permissões domain-specific (ex: configuration:ai:write) | Segurança | Análise de viabilidade necessária | Futuro |
| Rate limiting por operação em writes | Segurança | Infra-estrutura global | Futuro |
| Change reason obrigatório para SensitiveOperational | Segurança | Decisão de produto | Futuro |
| Catálogo de 345 definições gerado automaticamente | Documentação | Tooling | E2 |
| Diagrama de herança documentado | Documentação | Esforço editorial | E2 |
| Concurrency conflict handling nos handlers de escrita | Backend | DbUpdateConcurrencyException catch | E2 |
| Integration events publishing verificado end-to-end | Backend | Depende de infra de eventos | E2 |
| Integração com ClickHouse | Persistência | ClickHouse: NOT_REQUIRED para Configuration | N/A |

---

## 🚫 Não Bloqueia Evolução para E2

Todos os itens pendentes são incrementais e **não bloqueiam** a próxima fase.
O módulo Configuration pode avançar para:

1. **Baseline generation** — assim que os modelos de TODOS os módulos do Wave 1 estiverem finalizados
2. **E2 corrections** — validação JSON schema, paginação, concurrency conflict handling
3. **E-Phase de outros módulos** — Configuration está estável e pode servir como módulo-modelo

---

## 📊 Métricas de Maturidade

| Dimensão | Antes do E1 | Após E1 | Target |
|----------|-------------|---------|--------|
| Backend | 95% | 97% | 100% |
| Frontend | 90% | 91% | 95% |
| Persistência | 80% | 95% | 100% |
| Documentação | 30% | 55% | 85% |
| Testes | 95% | 95% | 95% |
| **Global** | **77%** | **87%** | **95%** |

---

## Decisões Tomadas Durante E1

1. **xmin via IsRowVersion()**: Utilizada API `IsRowVersion()` do EF Core Npgsql em vez de `UseXminAsConcurrencyToken()` (deprecated na versão 10.x). A convenção Npgsql mapeia automaticamente `uint` + `IsRowVersion()` para a coluna `xmin` do PostgreSQL.

2. **Seeder em todos os ambientes**: O seeder foi integrado no startup com catch seguro — se a tabela ainda não existe (schema não criado), a falha é logada como warning e não bloqueia o arranque.

3. **IsDeprecated como soft-deprecation**: Definições obsoletas podem ser lidas mas não permitem novas escritas. Isto evita quebrar configurações existentes enquanto sinaliza que a chave está em fim de vida.

4. **Check constraints com valores literais**: Os check constraints usam os nomes string dos enums (não valores inteiros) porque as colunas são persistidas como `text` no PostgreSQL via `HasConversion<string>()`.
