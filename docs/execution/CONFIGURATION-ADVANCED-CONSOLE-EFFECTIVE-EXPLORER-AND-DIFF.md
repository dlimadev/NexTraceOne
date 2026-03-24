# CONFIGURATION-ADVANCED-CONSOLE-EFFECTIVE-EXPLORER-AND-DIFF

## Console Administrativa Avançada

A console avançada da plataforma de parametrização centraliza todas as operações administrativas enterprise:

### Navegação por Domínio

| Domínio | Prefixos | Descrição |
|---------|----------|-----------|
| All | (todos) | Todas as definições |
| Instance | instance., tenant., environment., branding., featureFlags. | Configuração de instância, tenant e ambientes |
| Notifications | notifications. | Plataforma de notificações |
| Workflows | workflow., promotion. | Workflows e promotion governance |
| Governance | governance. | Governance, compliance e waivers |
| Catalog | catalog., change. | Catálogo, contratos e change governance |
| Operations | incidents., operations., finops., benchmarking. | Operações, FinOps e benchmarking |
| AI | ai. | AI governance, providers, modelos, budgets |
| Integrations | integrations. | Conectores, schedules, sync |

### Funcionalidades
- Pesquisa global por chave ou nome de definição
- Filtro por escopo (System/Tenant/Environment)
- Visualização de overrides ativos e configurações críticas
- Indicação de definições sensíveis, mandatórias e herdadas

## Effective Settings Explorer Avançado

O explorer mostra para cada definição:
- Valor efetivo final
- Escopo de resolução (onde o valor foi definido)
- Indicação de herança (Inherited badge)
- Indicação de valor default vs customizado
- Tipo, escopos permitidos, editor e herdabilidade
- Descrição da definição
- Valor default vs valor efetivo lado a lado
- Link para histórico de mudanças

### Expansão por Definição
Cada definição pode ser expandida para mostrar:
- Metadados completos (tipo, escopos, editor, herdabilidade)
- Valor default em bloco de código
- Valor efetivo com indicação do escopo de resolução e herança
- Ação para ver histórico

## Diff e Comparação

### Scopes Suportados
- System vs Tenant
- System vs Environment
- Tenant vs Environment

### Visualização
- Seletor de escopo esquerdo e direito
- Contagem de diferenças encontradas
- Para cada diferença:
  - Nome e chave da definição
  - Valor no escopo esquerdo (fundo vermelho)
  - Valor no escopo direito (fundo verde)
  - Masking automático para valores sensíveis

### Safeguards
- Valores sensíveis são mascarados na comparação
- Diff respeita permissões do administrador
