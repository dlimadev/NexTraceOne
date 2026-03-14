# Seeds de Teste — CommercialGovernance (Licensing)

## Objetivo

Scripts SQL para popular o banco de dados com massa de teste realista do módulo de Licenciamento.
Permitem testar o backend e o frontend (Tenant Licensing e Vendor Operations) em ambiente local.

## ⚠️ ATENÇÃO

Estes scripts devem ser usados **APENAS em ambiente de desenvolvimento/debug**.
**NÃO executar em produção.**

## Scripts

| # | Arquivo | Descrição |
|---|---------|-----------|
| 00 | `00-reset-commercial-governance-test-data.sql` | Limpa toda massa de teste do módulo |
| 01 | `01-seed-licenses.sql` | Cria 10 licenças cobrindo todos os cenários |
| 02 | `02-seed-capabilities.sql` | Associa capabilities às licenças por edição |
| 03 | `03-seed-quotas.sql` | Cria quotas de uso com consumo variado |
| 04 | `04-seed-activations.sql` | Registra ativações e hardware bindings |
| 05 | `05-seed-telemetry-consent.sql` | Registra consentimentos de telemetria por licença |
| 06 | `06-seed-plans.sql` | Cria 5 planos comerciais cobrindo todos os modelos |
| 07 | `07-seed-feature-packs.sql` | Cria 3 feature packs com 14 items de capability |
| 08 | `08-seed-plan-featurepack-mappings.sql` | Associa planos aos seus feature packs |

## Ordem de Execução

```bash
# 1. Limpar dados anteriores (opcional)
psql -d nextraceone -f database/seeds/commercial-governance/00-reset-commercial-governance-test-data.sql

# 2. Criar licenças
psql -d nextraceone -f database/seeds/commercial-governance/01-seed-licenses.sql

# 3. Criar capabilities
psql -d nextraceone -f database/seeds/commercial-governance/02-seed-capabilities.sql

# 4. Criar quotas de uso
psql -d nextraceone -f database/seeds/commercial-governance/03-seed-quotas.sql

# 5. Criar ativações
psql -d nextraceone -f database/seeds/commercial-governance/04-seed-activations.sql

# 6. Criar consentimentos de telemetria
psql -d nextraceone -f database/seeds/commercial-governance/05-seed-telemetry-consent.sql

# 7. Criar planos comerciais
psql -d nextraceone -f database/seeds/commercial-governance/06-seed-plans.sql

# 8. Criar feature packs e items
psql -d nextraceone -f database/seeds/commercial-governance/07-seed-feature-packs.sql

# 9. Criar mapeamentos plano → feature pack
psql -d nextraceone -f database/seeds/commercial-governance/08-seed-plan-featurepack-mappings.sql
```

## Cenários de Teste Cobertos

### Tenant Licensing (experiência do cliente)

| Cenário | Licença | LicenseKey |
|---------|---------|------------|
| SaaS Enterprise ativo | Banco Nacional SA | `LIC-SAAS-ENT-001` |
| SaaS Professional ativo | Seguros Confiança | `LIC-SAAS-PRO-002` |
| On-Premise Enterprise | Ministério da Defesa | `LIC-ONPREM-ENT-003` |
| Self-Hosted Professional | TelecomBR SA | `LIC-SELFHOST-PRO-004` |
| Trial ativo (20 dias) | Startup Inovadora | `TRIAL-STARTUP-005` |
| Trial expirado | Tech Antiga SA | `TRIAL-EXPIRED-006` |
| Grace period | Empresa em Renovação | `LIC-GRACE-007` |
| Licença revogada | Ex-Cliente Corp | `LIC-REVOKED-008` |
| Trial convertido | Empresa Convertida | `LIC-CONVERTED-009` |
| Community (free) | Projeto Open Source | `LIC-COMMUNITY-010` |

### Vendor Operations (backoffice NexTraceOne)

- **Listagem completa**: 10 licenças com status variados
- **Limites próximos do threshold**: Seguros Confiança (85% de APIs, 88% de users)
- **Community com limites apertados**: Projeto Open Source (80% APIs, 100% ambientes)
- **On-Premise offline**: Ministério da Defesa (ActivationMode=Offline)
- **Revogação**: Ex-Cliente Corp (já revogada para teste de UI)
- **Trial management**: Startup Inovadora (trial ativo) + Tech Antiga (expirado)

### Deployment Models

| Modelo | Licenças |
|--------|----------|
| SaaS | #1, #2, #5, #6, #7, #8, #9, #10 |
| Self-Hosted | #4 |
| On-Premise | #3 |

### Cenários de Warning/Threshold

| Licença | Métrica | Uso | Limite | % |
|---------|---------|-----|--------|---|
| Seguros Confiança | api.count | 85 | 100 | 85% ⚠️ |
| Seguros Confiança | user.count | 22 | 25 | 88% ⚠️ |
| Community | api.count | 8 | 10 | 80% ⚠️ |
| Community | environment.count | 1 | 1 | 100% 🔴 |

### CommercialCatalog — Planos Comerciais (06-seed-plans.sql)

| Plano | Code | Modelo Comercial | Deployment | Trial | Ativações |
|-------|------|-----------------|------------|-------|-----------|
| SaaS Professional | `saas-professional` | Subscription | SaaS | 30 dias | 5 |
| SaaS Enterprise | `saas-enterprise` | Subscription | SaaS | 30 dias | 10 |
| Self-Hosted Professional | `selfhosted-professional` | Subscription | Self-Hosted | 14 dias | 3 |
| On-Premise Enterprise | `onprem-enterprise` | Perpetual | On-Premise | — | 2 |
| Trial Starter | `trial-starter` | Trial | SaaS | 30 dias | 1 |

### CommercialCatalog — Feature Packs (07-seed-feature-packs.sql)

| Feature Pack | Code | Capabilities |
|-------------|------|-------------|
| API Governance Pack | `api-governance-pack` | api_catalog, contract_management (limit: 100), semantic_diff, developer_portal, multi_protocol |
| Change Intelligence Pack | `change-intelligence-pack` | blast_radius, workflow_engine, change_score, approval_workflow, promotion_gates (limit: 5) |
| Admin Operations Pack | `admin-operations-pack` | audit_trail, evidence_pack, compliance_reporting, advanced_rbac |

### CommercialCatalog — Mapeamentos Plano → Feature Pack (08-seed-plan-featurepack-mappings.sql)

| Plano | Feature Packs |
|-------|--------------|
| SaaS Professional | API Governance, Change Intelligence |
| SaaS Enterprise | API Governance, Change Intelligence, Admin Operations |
| Self-Hosted Professional | API Governance, Change Intelligence |
| On-Premise Enterprise | API Governance, Change Intelligence, Admin Operations |
| Trial Starter | API Governance |
