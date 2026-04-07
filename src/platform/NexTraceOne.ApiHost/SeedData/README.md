# NexTraceOne — Seed Data

## Visão Geral

Este diretório contém ficheiros SQL de dados de demonstração para o NexTraceOne.

## Ficheiros

| Ficheiro | Módulo | Conteúdo |
|----------|--------|----------|
| `seed-identity.sql` | IdentityAccess | Tenant demo, 7 utilizadores (admin, techlead, dev, viewer, auditor, secreview, approval), memberships |
| `seed-catalog.sql` | Catalog | Serviços de exemplo, contratos REST/SOAP/Event de demo |
| `seed-governance.sql` | Governance | Políticas, waivers, dashboards de exemplo |
| `seed-changegovernance.sql` | ChangeGovernance | Releases, blast radius, evidências de demo |
| `seed-audit.sql` | AuditCompliance | Registos de auditoria de demonstração |
| `seed-aiknowledge.sql` | AIKnowledge | Modelos AI, agentes, conversas de exemplo |
| `seed-incidents.sql` | OperationalIntelligence | Incidentes, mitigações, runbooks de exemplo |

## Estratégia por Ambiente

### Development (`ASPNETCORE_ENVIRONMENT=Development`)
- **ConfigurationDefinitionSeeder** executa automaticamente (458 parâmetros de configuração)
- **SQL seed files** são executados pelo mecanismo de bootstrap em Development
- Credenciais de demo: ver cabeçalho de cada ficheiro SQL
- Todos os dados são idempotentes (`ON CONFLICT DO NOTHING`)

### Staging (`ASPNETCORE_ENVIRONMENT=Staging`)
- **ConfigurationDefinitionSeeder** executa automaticamente
- **SQL seed files NÃO são executados** — dados devem ser importados ou criados via API
- Recomendação: popular via testes de integração ou importação controlada

### Production (`ASPNETCORE_ENVIRONMENT=Production`)
- **ConfigurationDefinitionSeeder** executa automaticamente (seguro — apenas parâmetros de configuração)
- **SQL seed files NÃO devem ser executados** — contêm dados de demo com credenciais conhecidas
- Bootstrap manual: ver [Production Bootstrap Guide](../../docs/deployment/PRODUCTION-BOOTSTRAP.md)

## Segurança

⚠️ **Os ficheiros SQL contêm credenciais de demonstração (passwords em hash PBKDF2).** Estas credenciais são públicas e destinam-se exclusivamente a desenvolvimento local. **Nunca executar em produção.**

## ConfigurationDefinitionSeeder

Além dos ficheiros SQL, o `ConfigurationDefinitionSeeder` (C#) é executado automaticamente em **todos os ambientes** e é seguro por design:
- Cria 458 parâmetros de configuração com valores default
- É idempotente (não duplica dados)
- Usa i18n keys (não texto hardcoded)
- Cria roles de sistema se não existirem

Localização: `src/modules/configuration/NexTraceOne.Configuration.Infrastructure/Seed/ConfigurationDefinitionSeeder.cs`
