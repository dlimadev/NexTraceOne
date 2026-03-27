# Fase 4 — Preferências e Roteamento Avançado de Notificações

## Objetivo

Evoluir a plataforma de notificações do NexTraceOne de regras rígidas de roteamento para um modelo enterprise com:

- Preferências reais por utilizador
- Roteamento baseado em contexto (severidade, categoria, preferências)
- Política de notificações obrigatórias (non-opt-out)
- Fallback seguro de destinatários
- Decisão de canal auditável e explicável

## Capabilities Entregues

### 1. Persistência de Preferências
- Entidade `NotificationPreference` com persistência em `ntf_preferences`
- Índice único por (TenantId, UserId, Category, Channel)
- Multi-tenant by design
- CRUD via `INotificationPreferenceStore`

### 2. Serviço de Preferências com Hierarquia
- `NotificationPreferenceService` com fallback para system defaults
- Hierarquia: preferência explícita do utilizador → default do sistema
- Preparado para evolução com defaults por tenant/role/equipa

### 3. Política de Notificações Obrigatórias
- `MandatoryNotificationPolicy` define eventos non-opt-out
- BreakGlass → todos os canais
- Incidente Crítico → todos os canais
- Aprovação Pendente → InApp + Email
- Compliance Failed → InApp + Email
- Severidade Critical → InApp + Email (mínimo)

### 4. Recipient Resolver
- `NotificationRecipientResolver` centralizado
- Resolução por IDs explícitos
- Scaffolding para resolução por roles e equipas (Fase 5+)
- Fallback com warning quando sem destinatários

### 5. Routing Engine Avançado
- `NotificationRoutingEngine` agora combina:
  - Canais obrigatórios (sempre incluídos)
  - Regras de severidade (padrão da plataforma)
  - Preferências do utilizador (para canais não obrigatórios)
  - Disponibilidade de infraestrutura

### 6. API de Preferências
- `GET /api/v1/notifications/preferences` — lista completa de preferências
- `PUT /api/v1/notifications/preferences` — atualiza preferência individual
- Validação: rejeita desativação de canais obrigatórios
- Proteção por permissão: `notifications:preferences:read/write`

### 7. Frontend — Página de Preferências
- Matriz categoria × canal com toggles
- Indicação visual de preferências obrigatórias (ícone de cadeado)
- Tooltip explicativo para preferências non-opt-out
- Feedback de sucesso/erro inline
- i18n completo (EN, PT-PT, PT-BR, ES)

## Impacto na Maturidade da Plataforma

| Antes (Fase 3) | Depois (Fase 4) |
|---|---|
| Roteamento fixo por severidade | Roteamento contextual com preferências |
| Sem preferências de utilizador | Preferências persistidas e editáveis |
| Todas as notificações opt-out implícito | Política explícita mandatory/optional |
| Sem UI de gestão | Página completa de preferências |
| Decisão opaca | Decisão auditável e logada |

## Arquitetura

```
NotificationRequest → RecipientResolver → Orchestrator
                                              ↓
                                      RoutingEngine
                                    ↙     ↓      ↘
                          MandatoryPolicy  PreferenceService  ChannelOptions
                                    ↘     ↓      ↙
                                   Channel Decision
                                        ↓
                              ExternalDeliveryService
```

## Limitações desta Fase

- Resolução de roles e equipas scaffolded, não implementada
- Sem defaults por tenant ou role (apenas system defaults + user override)
- Sem quiet hours ou digest
- Sem escalation automática
- Canais limitados a InApp, Email, Teams

## Próximos Passos (Fase 5)

- Eventos de alto valor por domínio
- Resolução de roles e equipas
- Defaults por tenant
- Quiet hours básicas
- Digest diário
