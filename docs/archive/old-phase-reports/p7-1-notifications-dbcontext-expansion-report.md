# P7.1 — NotificationsDbContext Expansion Report

## Objetivo

Expandir o módulo Notifications do NexTraceOne para suportar templates persistidos, canais de entrega configuráveis e configuração SMTP persistida, resolvendo o gap identificado nos relatórios de auditoria: "3 entidades mínimas; sem templates e canais ricos".

---

## Estado Anterior do NotificationsDbContext

O `NotificationsDbContext` continha apenas 3 DbSets:

| DbSet | Entidade | Tabela |
|---|---|---|
| `Notifications` | `Notification` | `ntf_notifications` |
| `Deliveries` | `NotificationDelivery` | `ntf_deliveries` |
| `Preferences` | `NotificationPreference` | `ntf_preferences` |

**Gaps identificados:**
- Templates de notificação existiam apenas em memória (`NotificationTemplateResolver`) — não eram persistidos, logo não podiam ser geridos por administradores.
- Configuração de canais (Email, Teams) estava apenas em `appsettings` via `NotificationChannelOptions` — sem possibilidade de gestão em runtime.
- Configuração SMTP não tinha entidade persistida — a única forma de configurar era por ficheiro de configuração estático.

---

## Entidades/Modelos Novos Introduzidos

### 1. `NotificationTemplate` (Domínio)

**Ficheiro:** `src/modules/notifications/NexTraceOne.Notifications.Domain/Entities/NotificationTemplate.cs`

**Tabela:** `ntf_templates`

| Campo | Tipo | Descrição |
|---|---|---|
| `Id` | `NotificationTemplateId` (UUID) | PK — strongly typed ID |
| `TenantId` | `Guid` | Multi-tenancy |
| `EventType` | `string(300)` | Tipo de evento (ex.: "IncidentCreated") |
| `Name` | `string(500)` | Nome legível |
| `SubjectTemplate` | `string(1000)` | Assunto (email) ou título; suporta `{{Variável}}` |
| `BodyTemplate` | `text` | Corpo (HTML para email, markup para outros) |
| `PlainTextTemplate` | `text?` | Alternativa plain-text para email |
| `Channel` | `DeliveryChannel?` | Canal alvo (null = todos os canais) |
| `Locale` | `string(10)` | Idioma ("en", "pt") |
| `IsActive` | `bool` | Controlo de visibilidade |
| `IsBuiltIn` | `bool` | Templates de sistema vs. personalizados |
| `CreatedAt` | `timestamptz` | Auditoria |
| `UpdatedAt` | `timestamptz` | Auditoria |

**Métodos de domínio:** `Create()`, `CreateBuiltIn()`, `Update()`, `Activate()`, `Deactivate()`

**Índices:** `(TenantId, EventType, Channel, Locale)`, `TenantId`, `EventType`, `IsActive`

---

### 2. `DeliveryChannelConfiguration` (Domínio)

**Ficheiro:** `src/modules/notifications/NexTraceOne.Notifications.Domain/Entities/DeliveryChannelConfiguration.cs`

**Tabela:** `ntf_channel_configurations`

| Campo | Tipo | Descrição |
|---|---|---|
| `Id` | `DeliveryChannelConfigurationId` (UUID) | PK — strongly typed ID |
| `TenantId` | `Guid` | Multi-tenancy |
| `ChannelType` | `DeliveryChannel` | Email, MicrosoftTeams, InApp |
| `DisplayName` | `string(200)` | Nome de exibição |
| `IsEnabled` | `bool` | Estado do canal |
| `ConfigurationJson` | `jsonb?` | Configuração específica do canal (host, webhook URL, etc.) |
| `CreatedAt` | `timestamptz` | Auditoria |
| `UpdatedAt` | `timestamptz` | Auditoria |

**Unicidade:** `(TenantId, ChannelType)` — um canal por tenant.

**Métodos de domínio:** `Create()`, `Update()`, `Enable()`, `Disable()`

---

### 3. `SmtpConfiguration` (Domínio)

**Ficheiro:** `src/modules/notifications/NexTraceOne.Notifications.Domain/Entities/SmtpConfiguration.cs`

**Tabela:** `ntf_smtp_configurations`

| Campo | Tipo | Descrição |
|---|---|---|
| `Id` | `SmtpConfigurationId` (UUID) | PK — strongly typed ID |
| `TenantId` | `Guid` | Multi-tenancy |
| `Host` | `string(500)` | Hostname SMTP |
| `Port` | `int` | Porta (ex.: 587, 465, 25) |
| `UseSsl` | `bool` | SSL/TLS |
| `Username` | `string(500)?` | Utilizador de autenticação |
| `EncryptedPassword` | `string(2000)?` | Senha cifrada (cifra = responsabilidade da infra) |
| `FromAddress` | `string(500)` | Email remetente |
| `FromName` | `string(200)` | Nome remetente |
| `BaseUrl` | `string(2000)?` | URL base para deep links em emails |
| `IsEnabled` | `bool` | Estado da configuração |
| `CreatedAt` | `timestamptz` | Auditoria |
| `UpdatedAt` | `timestamptz` | Auditoria |

**Unicidade:** `TenantId` — uma configuração SMTP por tenant.

**Métodos de domínio:** `Create()`, `UpdateServer()`, `UpdateCredentials()`, `UpdateSender()`, `Enable()`, `Disable()`

---

## Strongly Typed IDs Adicionados

| ID | Ficheiro |
|---|---|
| `NotificationTemplateId` | `Domain/StronglyTypedIds/NotificationTemplateId.cs` |
| `DeliveryChannelConfigurationId` | `Domain/StronglyTypedIds/DeliveryChannelConfigurationId.cs` |
| `SmtpConfigurationId` | `Domain/StronglyTypedIds/SmtpConfigurationId.cs` |

---

## Alterações no NotificationsDbContext

**Ficheiro:** `src/modules/notifications/NexTraceOne.Notifications.Infrastructure/Persistence/NotificationsDbContext.cs`

Adicionados 3 novos DbSets:

```csharp
public DbSet<NotificationTemplate> Templates => Set<NotificationTemplate>();
public DbSet<DeliveryChannelConfiguration> ChannelConfigurations => Set<DeliveryChannelConfiguration>();
public DbSet<SmtpConfiguration> SmtpConfigurations => Set<SmtpConfiguration>();
```

O DbContext passou de 3 para **6 entidades persistidas**.

---

## Mappings EF Core Adicionados

| Ficheiro | Entidade | Tabela |
|---|---|---|
| `NotificationTemplateConfiguration.cs` | `NotificationTemplate` | `ntf_templates` |
| `DeliveryChannelConfigurationEntityConfiguration.cs` | `DeliveryChannelConfiguration` | `ntf_channel_configurations` |
| `SmtpConfigurationEntityConfiguration.cs` | `SmtpConfiguration` | `ntf_smtp_configurations` |

---

## Abstrações de Application Adicionadas

| Interface | Ficheiro |
|---|---|
| `INotificationTemplateStore` | `Application/Abstractions/INotificationTemplateStore.cs` |
| `IDeliveryChannelConfigurationStore` | `Application/Abstractions/IDeliveryChannelConfigurationStore.cs` |
| `ISmtpConfigurationStore` | `Application/Abstractions/ISmtpConfigurationStore.cs` |

---

## Handlers CQRS Adicionados

| Feature | Ficheiro | Tipo |
|---|---|---|
| `ListNotificationTemplates` | `Features/ListNotificationTemplates/` | Query |
| `UpsertNotificationTemplate` | `Features/UpsertNotificationTemplate/` | Command |
| `ListDeliveryChannels` | `Features/ListDeliveryChannels/` | Query |
| `UpsertDeliveryChannel` | `Features/UpsertDeliveryChannel/` | Command |
| `GetSmtpConfiguration` | `Features/GetSmtpConfiguration/` | Query |
| `UpsertSmtpConfiguration` | `Features/UpsertSmtpConfiguration/` | Command |

---

## Repositórios Adicionados

| Repositório | Ficheiro |
|---|---|
| `NotificationTemplateRepository` | `Infrastructure/Persistence/Repositories/NotificationTemplateRepository.cs` |
| `DeliveryChannelConfigurationRepository` | `Infrastructure/Persistence/Repositories/DeliveryChannelConfigurationRepository.cs` |
| `SmtpConfigurationRepository` | `Infrastructure/Persistence/Repositories/SmtpConfigurationRepository.cs` |

---

## Endpoints API Adicionados

**Módulo:** `NotificationConfigurationEndpointModule.cs`

| Método | Endpoint | Permissão |
|---|---|---|
| `GET` | `/api/v1/notifications/configuration/templates` | `notifications:configuration:read` |
| `PUT` | `/api/v1/notifications/configuration/templates` | `notifications:configuration:write` |
| `GET` | `/api/v1/notifications/configuration/channels` | `notifications:configuration:read` |
| `PUT` | `/api/v1/notifications/configuration/channels` | `notifications:configuration:write` |
| `GET` | `/api/v1/notifications/configuration/smtp` | `notifications:configuration:read` |
| `PUT` | `/api/v1/notifications/configuration/smtp` | `notifications:configuration:write` |

---

## Migration EF Core

**Ficheiro:** `20260327082159_P7_1_NotificationsExpansion.cs`

Cria as 3 tabelas novas com todos os índices e constraints:
- `ntf_templates`
- `ntf_channel_configurations`
- `ntf_smtp_configurations`

---

## Alteração Necessária: Renaming de Registo Interno

Para resolver conflito de nomenclatura entre o registo em memória da camada Application e a nova entidade de domínio:

- `NotificationTemplate` (record em memória) → renomeado para **`ResolvedNotificationTemplate`**
- Ficheiros alterados: `INotificationTemplateResolver.cs`, `NotificationTemplateResolver.cs`, `NotificationOrchestrator.cs`
- Esta renomeação **não quebrou** nenhum comportamento existente.

---

## Impacto no Frontend

**Ficheiros alterados:**

| Ficheiro | Alteração |
|---|---|
| `features/notifications/types.ts` | Adicionadas interfaces: `NotificationTemplateDto`, `DeliveryChannelDto`, `SmtpConfigurationDto`, `UpsertNotificationTemplateRequest`, `UpsertDeliveryChannelRequest`, `UpsertSmtpConfigurationRequest`, `UpsertResponse` |
| `features/notifications/api/notifications.ts` | Adicionadas chamadas: `listTemplates`, `upsertTemplate`, `listChannels`, `upsertChannel`, `getSmtpConfiguration`, `upsertSmtpConfiguration` |
| `features/notifications/hooks/useNotificationConfiguration.ts` | **Novo ficheiro:** Hooks TanStack Query para todas as operações de configuração |
| `features/notifications/index.ts` | Exportações públicas atualizadas |

---

## Registos DI Adicionados

No `DependencyInjection.cs` da Infrastructure:

```csharp
services.AddScoped<INotificationTemplateStore, NotificationTemplateRepository>();
services.AddScoped<IDeliveryChannelConfigurationStore, DeliveryChannelConfigurationRepository>();
services.AddScoped<ISmtpConfigurationStore, SmtpConfigurationRepository>();
```

---

## Testes

| Ficheiro | Testes | Tipo |
|---|---|---|
| `Domain/NotificationTemplateTests.cs` | 6 | Domínio |
| `Domain/SmtpConfigurationTests.cs` | 6 | Domínio |
| `Domain/DeliveryChannelConfigurationTests.cs` | 5 | Domínio |
| `Application/NotificationTemplateHandlerTests.cs` | 8 | Aplicação |
| `Application/SmtpConfigurationHandlerTests.cs` | 5 | Aplicação |

**Total:** 30 novos testes.

**Resultado:** 442 testes passam (412 anteriores + 30 novos).

---

## Validação de Compilação

- `dotnet build NexTraceOne.sln` → **Build succeeded. 0 Error(s)**
- `dotnet test NexTraceOne.Notifications.Tests` → **Passed! 442 tests**

---

## Ficheiros Alterados (Resumo)

### Novos ficheiros (27):
- `Domain/Entities/NotificationTemplate.cs`
- `Domain/Entities/DeliveryChannelConfiguration.cs`
- `Domain/Entities/SmtpConfiguration.cs`
- `Domain/StronglyTypedIds/NotificationTemplateId.cs`
- `Domain/StronglyTypedIds/DeliveryChannelConfigurationId.cs`
- `Domain/StronglyTypedIds/SmtpConfigurationId.cs`
- `Application/Abstractions/INotificationTemplateStore.cs`
- `Application/Abstractions/IDeliveryChannelConfigurationStore.cs`
- `Application/Abstractions/ISmtpConfigurationStore.cs`
- `Application/Features/ListNotificationTemplates/ListNotificationTemplates.cs`
- `Application/Features/UpsertNotificationTemplate/UpsertNotificationTemplate.cs`
- `Application/Features/ListDeliveryChannels/ListDeliveryChannels.cs`
- `Application/Features/UpsertDeliveryChannel/UpsertDeliveryChannel.cs`
- `Application/Features/GetSmtpConfiguration/GetSmtpConfiguration.cs`
- `Application/Features/UpsertSmtpConfiguration/UpsertSmtpConfiguration.cs`
- `Infrastructure/Persistence/Configurations/NotificationTemplateConfiguration.cs`
- `Infrastructure/Persistence/Configurations/DeliveryChannelConfigurationEntityConfiguration.cs`
- `Infrastructure/Persistence/Configurations/SmtpConfigurationEntityConfiguration.cs`
- `Infrastructure/Persistence/Repositories/NotificationTemplateRepository.cs`
- `Infrastructure/Persistence/Repositories/DeliveryChannelConfigurationRepository.cs`
- `Infrastructure/Persistence/Repositories/SmtpConfigurationRepository.cs`
- `Infrastructure/Persistence/Migrations/20260327082159_P7_1_NotificationsExpansion.cs`
- `API/Endpoints/NotificationConfigurationEndpointModule.cs`
- `frontend/hooks/useNotificationConfiguration.ts`
- `tests/Domain/NotificationTemplateTests.cs`
- `tests/Domain/SmtpConfigurationTests.cs`
- `tests/Domain/DeliveryChannelConfigurationTests.cs`
- `tests/Application/NotificationTemplateHandlerTests.cs`
- `tests/Application/SmtpConfigurationHandlerTests.cs`

### Ficheiros modificados (8):
- `Infrastructure/Persistence/NotificationsDbContext.cs` — 3 novos DbSets
- `Infrastructure/DependencyInjection.cs` — 3 novos serviços registados
- `Application/Abstractions/INotificationTemplateResolver.cs` — `NotificationTemplate` → `ResolvedNotificationTemplate`
- `Application/Engine/NotificationTemplateResolver.cs` — atualizado para `ResolvedNotificationTemplate`
- `Application/Engine/NotificationOrchestrator.cs` — atualizado para `ResolvedNotificationTemplate`
- `frontend/types.ts` — 7 novos tipos
- `frontend/api/notifications.ts` — 6 novas chamadas de API
- `frontend/index.ts` — exportações atualizadas
