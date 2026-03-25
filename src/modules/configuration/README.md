# NexTraceOne — Configuration Module

## Visão Geral

O módulo Configuration é o sistema centralizado de configuração hierárquica da plataforma NexTraceOne.
Gere definições, valores, herança, encriptação e auditoria de todas as configurações da plataforma.

## Arquitetura

```
NexTraceOne.Configuration.Domain        → Entidades, enums, regras de negócio
NexTraceOne.Configuration.Application   → Features CQRS (Commands + Queries)
NexTraceOne.Configuration.Contracts     → DTOs e Integration Events
NexTraceOne.Configuration.Infrastructure→ DbContext, Repositories, Services, Seed
NexTraceOne.Configuration.API           → Endpoints Minimal API
```

## Entidades

| Entidade                   | Papel                                                    |
|---------------------------|----------------------------------------------------------|
| `ConfigurationDefinition`  | Metadados/schema de uma chave de configuração            |
| `ConfigurationEntry`       | Valor concreto de configuração por âmbito                |
| `ConfigurationAuditEntry`  | Registo imutável de auditoria de alterações              |

## Hierarquia de Âmbitos (Scope Inheritance)

A resolução de configurações percorre a hierarquia do mais específico para o mais genérico:

```
User → Team → Role → Environment → Tenant → System
```

Se não existir valor no âmbito solicitado, o sistema herda do âmbito superior.
Se nenhum valor for encontrado, utiliza o `DefaultValue` da definição.

## Enums

- **ConfigurationScope**: System, Tenant, Environment, Role, Team, User
- **ConfigurationCategory**: Bootstrap, SensitiveOperational, Functional
- **ConfigurationValueType**: String, Integer, Decimal, Boolean, Json, StringList

## Endpoints REST

| Método | Rota                                      | Permissão            | Descrição                       |
|--------|-------------------------------------------|----------------------|---------------------------------|
| GET    | `/api/v1/configuration/definitions`       | `configuration:read` | Lista todas as definições       |
| GET    | `/api/v1/configuration/entries`           | `configuration:read` | Valores por âmbito              |
| GET    | `/api/v1/configuration/effective`         | `configuration:read` | Valores resolvidos por herança  |
| PUT    | `/api/v1/configuration/{key}`             | `configuration:write`| Define ou atualiza valor        |
| DELETE | `/api/v1/configuration/{key}/override`    | `configuration:write`| Remove override de âmbito       |
| POST   | `/api/v1/configuration/{key}/toggle`      | `configuration:write`| Ativa/desativa configuração     |
| GET    | `/api/v1/configuration/{key}/audit`       | `configuration:read` | Histórico de auditoria          |

## Segurança

- Valores sensíveis encriptados com AES-256-GCM em repouso
- Mascaramento automático de valores sensíveis em respostas API
- Permissões: `configuration:read`, `configuration:write`
- Auditoria completa de todas as alterações (utilizador, timestamp, valor anterior/novo, motivo)
- PostgreSQL RLS para multi-tenancy

## Seed Data

O módulo inclui um seeder idempotente (`ConfigurationDefinitionSeeder`) com ~345 definições
organizadas em 8 fases:

| Fase | Domínio                          | Prefixo das chaves                    |
|------|----------------------------------|---------------------------------------|
| 0-1  | Foundation                       | `instance.*`, `policies.*`, `feature.*`, `security.*` |
| 2    | Notifications                    | `notifications.*`                     |
| 3    | Workflow/Promotion               | `workflow.*`, `promotion.*`           |
| 4    | Governance                       | `governance.*`                        |
| 5    | Catalog/Contracts/Change         | `catalog.*`, `change.*`               |
| 6    | Operations/FinOps/Benchmarking   | `incidents.*`, `operations.*`, `finops.*` |
| 7    | AI/Integrations                  | `ai.*`, `integrations.*`              |

O seeder executa em **todos os ambientes** (não apenas Development).

## Base de Dados

- **DbContext**: `ConfigurationDbContext`
- **Prefixo de tabelas**: `cfg_`
- **Tabelas**: `cfg_definitions`, `cfg_entries`, `cfg_audit_entries`, `cfg_outbox_messages`
- **Concorrência otimista**: PostgreSQL xmin via RowVersion em Definition e Entry
- **FK**: Entry→Definition, AuditEntry→Entry (ambas com `DeleteBehavior.Restrict`)
- **Check constraints**: Category, ValueType, Scope, version≥1

## Frontend

- **ConfigurationAdminPage**: Gestão de configurações por âmbito
- **AdvancedConfigurationConsolePage**: Console avançado com 6 tabs (Explorer, Diff, Import/Export, Rollback, History, Health)

## Testes

251 testes unitários cobrindo:
- Entidades de domínio (criação, atualização, invariantes)
- Seed data (completude, idempotência, coerência por fase)

## Convenções

- Todas as tabelas usam prefixo `cfg_`
- Chaves de configuração usam formato dot-notation (ex: `notifications.email.enabled`)
- Valores sensíveis nunca são expostos em texto claro
- Auditoria é imutável — registos nunca são atualizados após criação
