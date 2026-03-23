# Modelo de Preferências de Notificação

## Entidade: NotificationPreference

### Tabela: `ntf_preferences`

| Campo | Tipo | Obrigatório | Descrição |
|-------|------|-------------|-----------|
| Id | UUID (NotificationPreferenceId) | Sim | Identificador único |
| TenantId | UUID | Sim | Isolamento multi-tenant |
| UserId | UUID | Sim | Utilizador dono da preferência |
| Category | string (enum) | Sim | NotificationCategory |
| Channel | string (enum) | Sim | DeliveryChannel |
| Enabled | boolean | Sim | Se o canal está habilitado |
| UpdatedAt | timestamptz | Sim | Última atualização |

### Índices
- **Unique**: (TenantId, UserId, Category, Channel) — garante uma preferência por combinação
- **Index**: TenantId — queries por tenant
- **Index**: UserId — queries por utilizador

## Hierarquia de Preferências

```
1. Preferência explícita do utilizador (ntf_preferences)
        ↓ (se não existir)
2. Default do sistema (hardcoded no NotificationPreferenceService)
```

### Defaults do Sistema

| Canal | Default |
|-------|---------|
| InApp | Sempre habilitado |
| Email | Habilitado (routing engine decide por severidade) |
| Teams | Habilitado (routing engine decide por severidade) |

### Regras de Severidade (padrão)

| Severidade | InApp | Email | Teams |
|------------|-------|-------|-------|
| Info | ✅ | ❌ | ❌ |
| ActionRequired | ✅ | ✅ | ❌ |
| Warning | ✅ | ✅ | ✅ |
| Critical | ✅ | ✅ | ✅ |

## Override do Utilizador

O utilizador pode:
- Desativar Email para categorias não-obrigatórias
- Desativar Teams para categorias não-obrigatórias
- InApp não pode ser desativado

O utilizador NÃO pode:
- Desativar canais marcados como obrigatórios pela MandatoryNotificationPolicy
- Desativar InApp (sempre presente)

## Evolução Futura

### Preparado para (não implementado):
- Defaults por tenant (TenantId + Category + Channel sem UserId)
- Defaults por role (RoleName + Category + Channel)
- Defaults por equipa (TeamId + Category + Channel)
- PreferenceScope enum (System, Tenant, Role, Team, User)
- Source tracking (SystemDefault, TenantDefault, UserOverride)

### Modelo futuro sugerido:
```
NotificationPreference
  + Scope: PreferenceScope (System | Tenant | Role | Team | User)
  + ScopeId: string (null para System, TenantId, RoleName, TeamId, UserId)
  + Priority: int (System=0, Tenant=10, Role=20, Team=30, User=40)
```
