# P-00-03 — Adicionar CancellationToken ao módulo Notifications

## Modo de operação

Refactor

## Objetivo

Adicionar `CancellationToken` a todos os métodos assíncronos do módulo Notifications (~46 métodos).
Este é o módulo com mais métodos afetados, incluindo dispatchers de email e Teams, serviços de
orquestração, deduplicação, agrupamento, digestão, auditoria, métricas e health checks.

## Problema atual

O módulo Notifications contém 46 métodos `async Task` sem `CancellationToken` — o maior volume
de todos os módulos. Os ficheiros afetados abrangem múltiplas camadas: handlers de compliance
(ComplianceNotificationHandler com 4 métodos HandleAsync), serviços de pipeline (NotificationSuppressionService,
NotificationGroupingService, NotificationDigestService, NotificationRoutingEngine), dispatchers
(TeamsNotificationDispatcher, EmailNotificationDispatcher com envio SMTP), serviços de entrega
externa (ExternalDeliveryService com 5 métodos), repositórios (NotificationTemplateRepository,
NotificationPreferenceStoreRepository, NotificationStoreRepository, DeliveryChannelConfigurationRepository,
SmtpConfigurationRepository, NotificationDeliveryStoreRepository), e serviços de observabilidade
(NotificationAuditService, NotificationMetricsService, NotificationHealthProvider, NotificationPreferenceService,
NotificationOrchestrator).

## Escopo permitido

- `src/modules/notifications/` — apenas este módulo
- Application/**/*.cs — handlers e serviços aplicacionais
- Infrastructure/Persistence/**/*.cs — repositórios
- Infrastructure/Services/**/*.cs — dispatchers e serviços

## Escopo proibido

- Outros módulos
- Ficheiros de migração existentes
- Configuração SMTP no appsettings
- Testes de outros módulos

## Ficheiros principais candidatos a alteração

- `Application/Handlers/ComplianceNotificationHandler.cs` (4× HandleAsync)
- `Application/Services/NotificationSuppressionService.cs` (EvaluateAsync)
- `Application/Services/NotificationGroupingService.cs` (ResolveGroupAsync)
- `Application/Services/NotificationDigestService.cs` (GenerateDigestAsync)
- `Application/Services/NotificationRoutingEngine.cs` (ResolveChannelsAsync)
- `Application/Services/NotificationDeduplicationService.cs` (IsDuplicateAsync)
- `Application/Services/NotificationAuditService.cs` (RecordAsync)
- `Application/Services/NotificationMetricsService.cs` (GetPlatformMetricsAsync, GetInteractionMetricsAsync, GetQualityMetricsAsync)
- `Application/Services/NotificationHealthProvider.cs` (GetHealthAsync, CheckStoreHealthAsync, CheckDeliveryPipelineAsync, CheckChannelHealthAsync)
- `Application/Services/NotificationPreferenceService.cs` (GetPreferencesAsync, IsChannelEnabledAsync, UpdatePreferenceAsync)
- `Application/Services/NotificationOrchestrator.cs` (ProcessAsync)
- `Infrastructure/Dispatchers/TeamsNotificationDispatcher.cs` (DispatchAsync)
- `Infrastructure/Dispatchers/EmailNotificationDispatcher.cs` (DispatchAsync, método privado de config SMTP)
- `Infrastructure/Services/ExternalDeliveryService.cs` (ProcessExternalDeliveryAsync, RetryDeliveryAsync, DispatchToChannelAsync, AttemptDispatchAsync, RecordDeliveryAuditAsync)
- `Infrastructure/Persistence/NotificationTemplateRepository.cs` (GetByIdAsync, ListAsync, ResolveAsync)
- `Infrastructure/Persistence/NotificationPreferenceStoreRepository.cs` (GetByUserIdAsync, GetAsync, AddAsync)
- `Infrastructure/Persistence/NotificationStoreRepository.cs` (ListAsync)
- `Infrastructure/Persistence/DeliveryChannelConfigurationRepository.cs` (GetByIdAsync, GetByChannelTypeAsync, ListAsync)
- `Infrastructure/Persistence/SmtpConfigurationRepository.cs` (GetByTenantAsync, GetByIdAsync)
- `Infrastructure/Persistence/NotificationDeliveryStoreRepository.cs` (GetByIdAsync, ListPendingForRetryAsync, ListScheduledForRetryAsync, ListByNotificationIdAsync, ListByTenantAsync)

## Responsabilidades permitidas

- Adicionar `CancellationToken cancellationToken = default` a cada método async
- Propagar token para EF Core, SmtpClient, HttpClient (Teams webhook), etc.
- Atualizar interfaces correspondentes

## Responsabilidades proibidas

- Alterar lógica de routing, deduplicação ou agrupamento
- Alterar templates de notificação
- Refatorar pipeline de notificações

## Critérios de aceite

1. Todos os 46 métodos async têm `CancellationToken`
2. Token propagado para SmtpClient.SendAsync, HttpClient (Teams), e todas as queries EF Core
3. Módulo compila sem erros
4. Testes existentes compilam e passam
5. Solução completa compila

## Validações obrigatórias

- `dotnet build src/modules/notifications/` — sem erros
- `dotnet build NexTraceOne.sln` — sem erros
- `grep -r "async Task" src/modules/notifications/ | grep -v CancellationToken` retorna zero

## Riscos e cuidados

- EmailNotificationDispatcher envia SMTP — garantir que o token é propagado para evitar envios pendurados
- TeamsNotificationDispatcher faz HTTP POST — propagar token ao HttpClient
- ExternalDeliveryService tem retry logic — garantir que o token é verificado entre retries
- NotificationOrchestrator coordena múltiplos serviços — propagar token a todos

## Dependências

- Nenhuma dependência hard de outros prompts
- Pode ser executado em paralelo com P-00-01 e P-00-02

## Próximos prompts sugeridos

- P-00-04 (CancellationToken no módulo AIKnowledge)
- P-00-06 (CancellationToken nos módulos restantes)
