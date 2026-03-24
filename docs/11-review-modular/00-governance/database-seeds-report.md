# Relatório de Seed Data de Base de Dados — NexTraceOne

> **Data:** 2025-01-XX  
> **Escopo:** Todos os mecanismos de seed data (SQL scripts, C# seeders, extensions)  
> **Método:** Análise estática de ficheiros SQL, classes C# de seeding e orquestração  
> **Total:** 7 SQL scripts (2.258 linhas), 2 C# seeders, 1 orquestrador

---

## 1. Resumo Executivo

| Métrica | Valor |
|---|---|
| SQL seed scripts | 7 |
| Total de linhas SQL | 2.258 |
| C# seeders | 2 |
| Orquestrador | `DevelopmentSeedDataExtensions` |
| Ambiente de execução | **Development only** |
| Idempotência | ✅ Sim — `INSERT...ON CONFLICT DO NOTHING` |
| Seed de produção | ❌ **Não existe** |
| Áreas sem seed | Roles/Permissions, Governance packs, Notifications, Automation |

### Avaliação Global

| Aspeto | Estado |
|---|---|
| Cobertura de development | ⚠️ Parcial — cobre 7 de 12+ áreas |
| Qualidade dos scripts | ✅ Boa — idempotentes, bem estruturados |
| Seed de produção | ❌ Crítico — sem seed para roles, permissions ou governance packs |
| Consistência entre scripts | ✅ Boa — IDs referenciados são consistentes |

---

## 2. SQL Seed Scripts

### Localização

`src/platform/NexTraceOne.ApiHost/SeedData/`

### 2.1 `seed-identity.sql` — 83 linhas

| Conteúdo | Detalhe |
|---|---|
| Tenants | 2 tenants de desenvolvimento |
| Users | 10 utilizadores com roles diversos |
| Environments | Perfis de ambiente (dev, staging, production) |
| Teams | Equipas de exemplo |
| Roles | Roles base |

**Análise:**
- Cobre modelo de identidade completo
- 10 utilizadores permitem testar múltiplas personas
- Environments incluem `IsPrimaryProduction` para testar Change Confidence
- ❌ Sem permissions granulares — apenas roles base

**Exemplo de padrão:**
```sql
INSERT INTO tenants (id, name, slug, ...) 
VALUES ('...', 'Acme Corp', 'acme', ...)
ON CONFLICT DO NOTHING;
```

---

### 2.2 `seed-audit.sql` — 66 linhas

| Conteúdo | Detalhe |
|---|---|
| Audit events | 35 eventos de auditoria |
| Chain links | Links de cadeia de auditoria |

**Análise:**
- 35 eventos cobrem cenários variados (login, create, update, delete)
- Chain links demonstram funcionalidade blockchain-like
- Útil para testar Audit & Compliance views

---

### 2.3 `seed-catalog.sql` — 172 linhas

| Conteúdo | Detalhe |
|---|---|
| Services | 9 definições de serviço |
| APIs | 6 contratos de API |
| Contracts | Contratos diversos (REST, SOAP, Event) |
| Dependencies | Dependências entre serviços |
| Endpoints | Endpoints de serviço |

**Análise:**
- 9 serviços com dependências criam um grafo testável
- 6 APIs com contratos cobrem cenários de Contract Governance
- ✅ Dados suficientes para testar Service Catalog e Topology views

---

### 2.4 `seed-changegovernance.sql` — 266 linhas

| Conteúdo | Detalhe |
|---|---|
| Workflows | Templates e instâncias de workflow |
| Promotions | Pedidos de promoção entre ambientes |
| Rulesets | Conjuntos de regras e condições |
| Releases | Releases com change scores |
| Blast radius | Dados de impacto |

**Análise:**
- Script mais completo em termos de interligação entre entidades
- Cobre o pilar Change Confidence end-to-end
- Inclui dados para testar blast radius e change scores

---

### 2.5 `seed-governance.sql` — 420 linhas

| Conteúdo | Detalhe |
|---|---|
| Policies | Políticas de governança |
| Standards | Standards/padrões |
| Violations | Violações de exemplo |
| Metrics | Métricas de governança |

**Análise:**
- Segundo maior script (420 linhas)
- Cobre o modelo de governança extensivamente
- ❌ Faltam governance packs pré-definidos (SOC2, ISO27001)

---

### 2.6 `seed-incidents.sql` — 224 linhas

| Conteúdo | Detalhe |
|---|---|
| Incidents | 6 incidentes de exemplo |
| Runbooks | 3 runbooks operacionais |
| Timelines | Timelines de incidente |
| Correlations | Correlações incident↔change |

**Análise:**
- 6 incidentes com severidades variadas
- 3 runbooks com passos de execução
- Correlações com changes permitem testar Change-to-Incident views
- ❌ Sem execuções de runbook (RunbookExecution)

---

### 2.7 `seed-aiknowledge.sql` — 1.027 linhas

| Conteúdo | Detalhe |
|---|---|
| AI Agents | 10 agentes de IA |
| Models | Modelos de IA registados |
| Providers | Providers (OpenAI, Azure, local) |
| Conversations | Conversas de exemplo |
| Messages | Mensagens em conversas |
| Access policies | Políticas de acesso |
| Token budgets | Budgets de tokens |
| Knowledge sources | Fontes de conhecimento |
| Guardrails | Guardrails configurados |

**Análise:**
- Maior script de seed (1.027 linhas — 45% do total)
- 10 agentes de IA com capabilities e tools
- Cobertura muito completa do módulo de IA
- ✅ Dados suficientes para testar AI Governance, Orchestration e External AI views

---

## 3. C# Seeders

### 3.1 ConfigurationDefinitionSeeder

| Propriedade | Valor |
|---|---|
| Localização | `src/modules/configuration/` |
| Definitions | 345+ |
| Organização | 8 fases |
| Tipo | Programmatic (C#) |

**Fases de configuração:**

| Fase | Foco Estimado |
|---|---|
| Phase 1 | Configurações base do sistema |
| Phase 2 | Configurações de identidade e segurança |
| Phase 3 | Configurações de catálogo e contratos |
| Phase 4 | Configurações de change governance |
| Phase 5 | Configurações de operações |
| Phase 6 | Configurações de IA |
| Phase 7 | Configurações de governança |
| Phase 8 | Configurações avançadas |

**Análise:**
- 345+ definições é muito extenso — demonstra maturidade do modelo de configuração
- Organização em 8 fases facilita manutenção
- Executado via `ConfigurationDbContext` (sem migrations — usa `EnsureCreated`)
- ⚠️ Se o schema mudar, todas as configurações são recriadas

### 3.2 IncidentSeedData

| Propriedade | Valor |
|---|---|
| Localização | `src/modules/operationalintelligence/` |
| Conteúdo | 6 incidentes, 3 runbooks |
| Uso | Referenciado em migrations |
| Tipo | Programmatic (C#) |

**Análise:**
- Usado diretamente em InitialCreate migration do IncidentDbContext
- 6 incidentes com dados estruturados
- 3 runbooks com passos de execução
- Dupla fonte: SQL (`seed-incidents.sql`) + C# (`IncidentSeedData`) — potencial inconsistência

---

## 4. Orquestração

### 4.1 DevelopmentSeedDataExtensions

| Propriedade | Valor |
|---|---|
| Tipo | Extension method em `IApplicationBuilder` |
| Ambiente | **Development only** |
| Execução | No arranque da aplicação |
| Ordem | Sequencial — identity → audit → catalog → change → governance → incidents → ai |

**Fluxo:**

```
if (app.Environment.IsDevelopment())
{
    // 1. Executa SQL seeds por ordem
    seed-identity.sql
    seed-audit.sql
    seed-catalog.sql
    seed-changegovernance.sql
    seed-governance.sql
    seed-incidents.sql
    seed-aiknowledge.sql
    
    // 2. Executa C# seeders
    ConfigurationDefinitionSeeder.Seed()
}
```

**Análise:**
- Execução apenas em Development é segura
- Ordem sequencial respeita dependências (identity primeiro)
- `ON CONFLICT DO NOTHING` garante idempotência em re-execuções
- ❌ Não existe mecanismo equivalente para produção

---

## 5. Análise de Cobertura

### 5.1 Áreas com Seed Data

| Área | Script/Seeder | Linhas | Cobertura |
|---|---|---|---|
| Identity (users, roles, tenants) | `seed-identity.sql` | 83 | ✅ Boa |
| Audit & Compliance | `seed-audit.sql` | 66 | ✅ Adequada |
| Catalog (services, contracts) | `seed-catalog.sql` | 172 | ✅ Boa |
| Change Governance | `seed-changegovernance.sql` | 266 | ✅ Muito boa |
| Governance (policies, standards) | `seed-governance.sql` | 420 | ✅ Muito boa |
| Incidents & Runbooks | `seed-incidents.sql` + C# | 224 | ✅ Boa |
| AI (agents, models, conversations) | `seed-aiknowledge.sql` | 1.027 | ✅ Excelente |
| Configuration | C# Seeder | 345+ defs | ✅ Excelente |

### 5.2 Áreas SEM Seed Data

| Área | Impacto | Prioridade |
|---|---|---|
| **Roles & Permissions de produção** | ❌ Alto — sem roles base, produção não funciona | 🔴 Alta |
| **Governance packs (SOC2, ISO)** | ❌ Alto — onboarding requer configuração manual | 🔴 Alta |
| **Notifications (templates, channels)** | ❌ Médio — sem templates, notificações não funcionam | 🟡 Média |
| **Automation (rules, schedules)** | ❌ Baixo — funcionalidade secundária | 🟢 Baixa |
| **Reliability (SLOs)** | ❌ Baixo — apenas 1 entidade | 🟢 Baixa |
| **Runtime Intelligence** | ❌ Baixo — dados operacionais gerados em runtime | 🟢 Baixa |
| **Cost Intelligence** | ❌ Baixo — dados importados de fontes externas | 🟢 Baixa |
| **Developer Portal** | ⚠️ Médio — portal sem conteúdo inicial | 🟡 Média |

---

## 6. Análise de Qualidade

### 6.1 Idempotência

| Mecanismo | Análise |
|---|---|
| SQL: `ON CONFLICT DO NOTHING` | ✅ Idempotente — re-execução não causa duplicados |
| C# Seeder: verificação de existência | ✅ Assumido — seeder verifica antes de inserir |

### 6.2 Consistência de IDs

| Aspeto | Estado |
|---|---|
| IDs cross-reference entre scripts | ✅ Consistente — `seed-catalog.sql` referencia tenants de `seed-identity.sql` |
| IDs hard-coded (GUIDs) | ✅ Necessário para referências cruzadas |
| Potencial de conflito | Baixo — GUIDs são únicos |

### 6.3 Dados Realistas

| Script | Realismo |
|---|---|
| Identity | ✅ Nomes e emails plausíveis |
| Catalog | ✅ Serviços com nomes de domínio realistas |
| AI | ✅ Agentes com capabilities reais |
| Change | ✅ Releases com scores e blast radius |
| Governance | ⚠️ Parcial — políticas genéricas |

### 6.4 Duplicação

| Área | Problema |
|---|---|
| Incidents | Dados em SQL (`seed-incidents.sql`) E em C# (`IncidentSeedData`) |
| Risco | Possível inconsistência se um for atualizado e o outro não |
| Recomendação | Unificar em SQL ou C#, não ambos |

---

## 7. Seed de Produção — Análise de Gap

### 7.1 O que é Necessário para First Deploy em Produção

| Dados | Estado | Criticidade |
|---|---|---|
| Roles base (Admin, Engineer, Tech Lead, Architect, etc.) | ❌ Não existe | 🔴 Crítico |
| Permissions base | ❌ Não existe | 🔴 Crítico |
| Role↔Permission mappings | ❌ Não existe | 🔴 Crítico |
| Tenant admin user | ❌ Não existe | 🔴 Crítico |
| Governance pack base | ❌ Não existe | 🟡 Importante |
| Notification templates | ❌ Não existe | 🟡 Importante |
| Configuration defaults | ✅ Via C# seeder (dev) | ⚠️ Needs production mode |
| AI model registry defaults | ❌ Não existe | 🟡 Importante |

### 7.2 Estratégia Recomendada para Seed de Produção

```
src/platform/NexTraceOne.ApiHost/SeedData/
├── development/           (existente — 7 scripts SQL)
│   ├── seed-identity.sql
│   ├── seed-audit.sql
│   └── ...
└── production/            (a criar)
    ├── seed-roles.sql     — Roles e permissions base
    ├── seed-admin.sql     — Primeiro admin user
    ├── seed-governance-packs.sql  — SOC2, ISO27001
    ├── seed-notification-templates.sql
    └── seed-ai-defaults.sql
```

---

## 8. Recomendações

### 🔴 Prioridade Alta

| # | Ação | Justificação |
|---|---|---|
| 1 | Criar seed de produção para roles e permissions | Sem isto, produção não funciona |
| 2 | Criar seed de produção para tenant admin | Primeiro utilizador necessário |
| 3 | Adaptar ConfigurationDefinitionSeeder para produção | 345+ configs necessárias em prod |

### 🟡 Prioridade Média

| # | Ação | Justificação |
|---|---|---|
| 4 | Criar seed de governance packs (SOC2, ISO) | Onboarding de clientes |
| 5 | Criar seed de notification templates | Notificações não funcionam sem templates |
| 6 | Eliminar duplicação Incidents (SQL vs C#) | Evitar inconsistência |
| 7 | Criar seed de Developer Portal base | Portal sem conteúdo inicial |

### 🟢 Prioridade Baixa

| # | Ação | Justificação |
|---|---|---|
| 8 | Criar seed de AI model registry defaults | Configuração inicial de IA |
| 9 | Documentar ordem de execução de seeds | Clarificar dependências |
| 10 | Adicionar validation de seeds em CI/CD | Garantir que seeds executam sem erro |

---

## Referências

| Artefacto | Localização |
|---|---|
| SQL Seeds | `src/platform/NexTraceOne.ApiHost/SeedData/` |
| ConfigurationDefinitionSeeder | `src/modules/configuration/Infrastructure/Persistence/` |
| IncidentSeedData | `src/modules/operationalintelligence/Infrastructure/Persistence/` |
| DevelopmentSeedDataExtensions | `src/platform/NexTraceOne.ApiHost/Extensions/` |

---

*Relatório gerado como parte da auditoria modular de governança do NexTraceOne.*
