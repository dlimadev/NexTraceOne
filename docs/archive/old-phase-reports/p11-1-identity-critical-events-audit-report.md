# P11.1 — Identity & Access: reforço de eventos críticos e auditoria real

## Objetivo desta execução

Reforçar a geração consistente de eventos críticos de segurança no módulo Identity & Access, garantir trilha auditável ponta a ponta usando infraestrutura existente (SecurityEvent + SecurityEventAuditBehavior + SecurityAuditBridge + AuditModule) e expor consultas mínimas para investigação operacional.

## Ficheiros alterados

- `src/modules/identityaccess/NexTraceOne.IdentityAccess.Domain/Entities/SecurityEventType.cs`
- `src/modules/identityaccess/NexTraceOne.IdentityAccess.Application/NexTraceOne.IdentityAccess.Application.csproj`
- `src/modules/identityaccess/NexTraceOne.IdentityAccess.Application/Features/RequestBreakGlass/RequestBreakGlass.cs`
- `src/modules/identityaccess/NexTraceOne.IdentityAccess.Application/Features/RevokeBreakGlass/RevokeBreakGlass.cs`
- `src/modules/identityaccess/NexTraceOne.IdentityAccess.Application/Features/RequestJitAccess/RequestJitAccess.cs`
- `src/modules/identityaccess/NexTraceOne.IdentityAccess.Application/Features/DecideJitAccess/DecideJitAccess.cs`
- `src/modules/identityaccess/NexTraceOne.IdentityAccess.Application/Features/CreateDelegation/CreateDelegation.cs`
- `src/modules/identityaccess/NexTraceOne.IdentityAccess.Application/Features/RevokeDelegation/RevokeDelegation.cs`
- `src/modules/identityaccess/NexTraceOne.IdentityAccess.Application/Features/DecideAccessReviewItem/DecideAccessReviewItem.cs`
- `src/modules/identityaccess/NexTraceOne.IdentityAccess.Application/Features/StartAccessReviewCampaign/StartAccessReviewCampaign.cs`
- `src/modules/identityaccess/NexTraceOne.IdentityAccess.Application/Features/CreateUser/CreateUser.cs`
- `src/modules/identityaccess/NexTraceOne.IdentityAccess.Application/Features/ActivateUser/ActivateUser.cs`
- `src/modules/identityaccess/NexTraceOne.IdentityAccess.Application/Features/DeactivateUser/DeactivateUser.cs`
- `src/modules/identityaccess/NexTraceOne.IdentityAccess.Application/Features/Logout/Logout.cs`
- `src/modules/identityaccess/NexTraceOne.IdentityAccess.Application/Features/StartOidcLogin/StartOidcLogin.cs`
- `src/modules/identityaccess/NexTraceOne.IdentityAccess.Application/Features/SecurityAuditRecorder.cs`
- `src/modules/identityaccess/NexTraceOne.IdentityAccess.Application/Features/ListSecurityEvents/ListSecurityEvents.cs` (novo)
- `src/modules/identityaccess/NexTraceOne.IdentityAccess.Application/Features/ListBreakGlassRequests/ListBreakGlassRequests.cs`
- `src/modules/identityaccess/NexTraceOne.IdentityAccess.Application/Features/ListJitAccessRequests/ListJitAccessRequests.cs`
- `src/modules/identityaccess/NexTraceOne.IdentityAccess.Application/Features/ListDelegations/ListDelegations.cs`
- `src/modules/identityaccess/NexTraceOne.IdentityAccess.Application/Abstractions/IJitAccessRepository.cs`
- `src/modules/identityaccess/NexTraceOne.IdentityAccess.Application/Abstractions/IDelegationRepository.cs`
- `src/modules/identityaccess/NexTraceOne.IdentityAccess.Infrastructure/Persistence/Repositories/JitAccessRepository.cs`
- `src/modules/identityaccess/NexTraceOne.IdentityAccess.Infrastructure/Persistence/Repositories/DelegationRepository.cs`
- `src/modules/identityaccess/NexTraceOne.IdentityAccess.API/Endpoints/Endpoints/SecurityEventsEndpoints.cs` (novo)
- `src/modules/identityaccess/NexTraceOne.IdentityAccess.API/Endpoints/Endpoints/IdentityEndpointModule.cs`
- `src/modules/identityaccess/NexTraceOne.IdentityAccess.API/Endpoints/Endpoints/BreakGlassEndpoints.cs`
- `src/modules/identityaccess/NexTraceOne.IdentityAccess.API/Endpoints/Endpoints/JitAccessEndpoints.cs`
- `src/modules/identityaccess/NexTraceOne.IdentityAccess.API/Endpoints/Endpoints/DelegationEndpoints.cs`
- testes ajustados:
  - `tests/modules/identityaccess/NexTraceOne.IdentityAccess.Tests/Application/Features/CreateUserTests.cs`
  - `tests/modules/identityaccess/NexTraceOne.IdentityAccess.Tests/Application/Features/LogoutTests.cs`
  - `tests/modules/identityaccess/NexTraceOne.IdentityAccess.Tests/Application/Features/SecurityAuditRecorderTests.cs`

## Estado anterior (resumo)

- Parte dos fluxos críticos gerava `SecurityEvent`, mas sem consistência de cobertura.
- Em vários handlers críticos havia persistência de `SecurityEvent` sem `Track()`, o que reduzia a propagação para audit central via bridge.
- Faltavam eventos explícitos em operações críticas (ex.: decisão de JIT, revogações, tentativas negadas de self-action).
- Não havia endpoint dedicado para consulta de eventos críticos de segurança.
- Consultas de BreakGlass/JIT/Delegation eram focadas em ativos/pendentes, sem opção de histórico mínimo.

## Matriz mínima de eventos críticos adotada

| Fluxo | SecurityEvent | AuditEvent (via bridge) | Estado |
|---|---|---|---|
| Break Glass activation | `BreakGlassActivated` | Sim (`Track`) | Reforçado |
| Break Glass revoke | `BreakGlassRevoked` | Sim (`Track`) | Novo |
| Break Glass expiration | `BreakGlassExpired` | Parcial (persistido no job) | Mantido |
| JIT request | `JitAccessRequested` | Sim (`Track`) | Reforçado |
| JIT approve | `JitAccessApproved` | Sim (`Track`) | Novo |
| JIT reject | `JitAccessRejected` | Sim (`Track`) | Novo |
| JIT self-approval denied | `JitSelfApprovalDenied` | Sim (`Track`) | Novo |
| JIT expiration | `JitAccessExpired` | Parcial (persistido no job) | Mantido |
| Delegation create | `DelegationCreated` | Sim (`Track`) | Reforçado |
| Delegation revoke | `DelegationRevoked` | Sim (`Track`) | Novo |
| Delegation self-denied | `DelegationToSelfDenied` | Sim (`Track`) | Novo |
| Delegation expiration | `DelegationExpired` | Parcial (persistido no job) | Mantido |
| Access Review start | `AccessReviewStarted` | Sim (`Track`) | Reforçado |
| Access Review item approved/revoked | `AccessReviewItemApproved` / `AccessReviewItemRevoked` | Sim (`Track`) | Reforçado |
| Role/permission sensitive changes | `RoleAssigned` (já existente) + metadata | Sim (`Track`) | Mantido/Reforçado |
| User lifecycle sensitive actions | `UserCreated`, `UserActivated`, `UserDeactivated` | Sim (`Track`) | Reforçado |

## Ajustes de handlers/services/endpoints/queries

### 1) Eventos críticos e self-action prevention auditável

- Novos tipos em `SecurityEventType`:
  - `security.privileged.delegation_revoked`
  - `security.privileged.break_glass_revoked`
  - `security.privileged.jit_approved`
  - `security.privileged.jit_rejected`
  - `security.privileged.jit_self_approval_denied`
  - `security.privileged.delegation_self_denied`

- `DecideJitAccess`:
  - gera evento para aprovação e rejeição;
  - registra tentativa negada de self-approval com evento dedicado.

- `CreateDelegation`:
  - registra tentativa negada de self-delegation (`DelegationToSelfDenied`);
  - enriquecimento de metadata no evento de criação.

- `RevokeBreakGlass` e `RevokeDelegation`:
  - agora geram eventos explícitos de revogação com metadata de correlação.

### 2) Fecho de auditoria ponta a ponta (sem mecanismo paralelo)

- Reforço do padrão `securityEventRepository.Add(...) + securityEventTracker.Track(...)` nos fluxos críticos.
- Ajustes feitos também em fluxos já existentes que não rastreavam para bridge:
  - `CreateUser`, `ActivateUser`, `DeactivateUser`, `Logout`, `StartOidcLogin`, `StartAccessReviewCampaign`, `RequestJitAccess`, `DecideAccessReviewItem`, `SecurityAuditRecorder`.
- A propagação central mantém o mecanismo existente:
  - `SecurityEventAuditBehavior` → `ISecurityAuditBridge` → `IAuditModule.RecordEventAsync`.

### 3) Consultas mínimas e navegação investigativa

- Novo endpoint:
  - `GET /api/v1/identity/security-events?eventType=&page=&pageSize=`
- Novo handler:
  - `ListSecurityEvents` (com paginação + `UnreviewedCount`).
- Consultas mínimas com histórico opcional:
  - `GET /api/v1/identity/break-glass?includeInactive=true`
  - `GET /api/v1/identity/jit-access/pending?includeHistory=true`
  - `GET /api/v1/identity/delegations?includeHistory=true`

### 4) Integração mínima com Notifications (Fase 7)

- `RequestBreakGlass` agora chama `INotificationModule.SubmitAsync(...)` com:
  - severidade crítica,
  - contexto do request,
  - `SourceEventId = SecurityEvent.Id`.
- Isto fecha o wiring mínimo Identity → Notifications com correlação auditável (`SourceEventId`) sem criar mecanismo paralelo.

## Uso explícito da infraestrutura solicitada

- `SecurityEvent`: expandido e reforçado em handlers críticos.
- `SecurityAuditBridge` / `SecurityEventAuditBehavior`: mantidos como caminho oficial de envio para audit central.
- `AuditDbContext`, `AuditEvent`, `AuditChainLink`: reutilizados indiretamente via `IAuditModule` (sem acoplamento direto, sem bypass).

## Impacto em APIs

- Adicionado:
  - `GET /api/v1/identity/security-events`
- Ajustados:
  - Break Glass list: novo parâmetro `includeInactive`
  - JIT list: novo parâmetro `includeHistory`
  - Delegation list: novo parâmetro `includeHistory`

## Validação executada

- Build baseline e build final da solução (`dotnet build NexTraceOne.sln --configuration Release --no-restore`).
- Build dos projetos Identity Application/API.
- Testes do módulo Identity:
  - `dotnet test tests/modules/identityaccess/NexTraceOne.IdentityAccess.Tests/NexTraceOne.IdentityAccess.Tests.csproj --configuration Release --no-restore`
  - Resultado: **Passed 290 / Failed 0**.

