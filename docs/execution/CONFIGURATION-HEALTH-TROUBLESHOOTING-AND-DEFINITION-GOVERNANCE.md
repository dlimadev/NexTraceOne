# CONFIGURATION-HEALTH-TROUBLESHOOTING-AND-DEFINITION-GOVERNANCE

## Health da Plataforma de Configuração

### Health Checks Implementados

| Check | Descrição | Status |
|-------|-----------|--------|
| Definition Count | Verifica que existem definições carregadas | OK se > 0, Error se 0 |
| Effective Resolution | Verifica que configurações estão sendo resolvidas | OK se > 0, Warning se 0 |
| Sensitive Protection | Verifica que chaves sensíveis estão protegidas | OK com contagem |
| Orphan Check | Detecta entries sem definition correspondente | OK se 0, Warning se > 0 |
| Duplicate Check | Detecta chaves duplicadas no catálogo | OK se 0, Error se > 0 |

### Indicadores Visuais
- ✅ Verde = Saudável
- ⚠️ Amarelo = Warning (ação recomendada)
- ❌ Vermelho = Erro (ação necessária)

## Troubleshooting

A capability detecta e reporta:
- Definições inválidas ou malformadas
- Overrides órfãos (entry sem definition)
- Chaves duplicadas no catálogo
- Falhas de resolução de effective settings
- Inconsistências de cache

### Pistas Acionáveis
Cada problema identificado inclui informação suficiente para:
- Entender a natureza do problema
- Localizar a definição ou entry afetada
- Tomar ação corretiva

## Governança de Definitions

### Dashboard de Governança

| Métrica | Descrição |
|---------|-----------|
| Total Definitions | Número total de definições no catálogo |
| Sensitive | Definições marcadas como sensíveis (masking automático) |
| Editable | Definições que podem ser editadas por administradores |
| Mandatory (System-only) | Definições que não são herdáveis e só existem no escopo System |

### Domain Breakdown
Contagem de definições por domínio, permitindo ao administrador entender a distribuição:
- Instance / Tenant / Environment
- Notifications
- Workflows & Promotion
- Governance & Compliance
- Catalog & Contracts
- Operations & FinOps
- AI Governance
- Integrations

### Separation of Duties

A plataforma suporta separação de responsabilidades:
- **Platform Admin**: Acesso completo a todas as definições e valores
- **Tenant Admin**: Acesso a overrides no escopo Tenant
- **Domain Admin**: Acesso a definições de domínios específicos
- **Operator**: Acesso de leitura e visualização de effective settings

### Controles Administrativos
- Permissão `platform:admin:read` requerida para acesso à console avançada
- Valores sensíveis mascarados por padrão
- Auditoria completa de todas as operações administrativas
- Export não inclui valores sensíveis em claro
