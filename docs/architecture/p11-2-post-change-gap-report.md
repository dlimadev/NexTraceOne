# P11.2 — Post-change gap report (privileged flows enforcement)

## O que foi resolvido nesta fase

1. **Self-approval prevention em JIT Access** — verificada e comprovada por testes de handler:
   - retorna `Identity.JitAccess.SelfApprovalNotAllowed` (403 Forbidden)
   - emite `SecurityEvent.JitSelfApprovalDenied` (riskScore 65) com trilha auditável
   - dupla proteção: handler + domínio.

2. **Self-delegation prevention em Delegations** — verificada e comprovada por testes de handler:
   - retorna `Identity.Delegation.SelfNotAllowed` (400 Validation)
   - emite `SecurityEvent.DelegationToSelfDenied` (riskScore 55) com trilha auditável
   - dupla proteção: handler + domínio.

3. **Background jobs de expiração automática** — confirmados funcionais para Break Glass, JIT Access e Delegation:
   - `BreakGlassExpirationHandler` — expira solicitações `Active` cujo `ExpiresAt <= now`
   - `JitAccessExpirationHandler` — expira solicitações `Pending` sem aprovação no prazo e solicitações `Approved` cujo `GrantedUntil <= now`
   - `DelegationExpirationHandler` — expira delegações `Active` cujo `ValidUntil <= now`
   - Todos orquestrados em `IdentityExpirationJob` (execução a cada 60 segundos)

4. **Enforcement proativo e não apenas oportunista** — estados privilegiados expiram de forma automática e previsível por job, sem dependência do próximo acesso do utilizador.

5. **Trilha auditável mínima** — todos os eventos de expiração geram `SecurityEvent` persistido com metadata de correlação.

6. **Testes de handler adicionados** (7 novos):
   - `DecideJitAccessTests`: self-approval bloqueado + aprovação legítima + rejeição + not found
   - `CreateDelegationTests`: self-delegation bloqueado + permissão não delegável + criação legítima

## O que ainda ficou pendente

1. **Propagação de SecurityEvent de jobs para Audit central**: eventos de expiração são gravados diretamente em `IdentityDbContext.SecurityEvents` pelo job. Não há bridge para `IAuditModule` a partir do contexto do job (sem pipeline de request, sem `ISecurityEventTracker`). Este é o mesmo gap documentado em P11.1.

2. **Testes de handler nível-EF para os expiration handlers**: não foram criados testes unitários com EF Core InMemory para os expiration handlers (`BreakGlassExpirationHandler`, `JitAccessExpirationHandler`, `DelegationExpirationHandler`). Os handlers são cobertos indiretamente pelos testes de domínio das entidades (`BreakGlassRequestTests`, `JitAccessRequestTests`, `DelegationTests`).

3. **Notificações mínimas para expirações**: expiração automática de JIT/Delegation não envia notificações atualmente. Break Glass já tem notificação na activação (P11.1). Expirações de Break Glass/JIT/Delegation poderiam gerar notificações mínimas para o utilizador afetado.

## O que fica explicitamente para P11.3

1. **Ponte de auditoria para eventos de jobs**: implementar mecanismo simples para que os eventos criados diretamente por jobs de expiração também alcancem o módulo Audit central (ex.: via outbox na identidade ou polling de SecurityEvent não auditados).

2. **Notificações mínimas para expirações privilegiadas**: wiring mínimo de notificação quando JIT ou Delegation expiram automaticamente (permitindo que o utilizador saiba que o acesso terminou).

3. **Revisão pós-mortem automática para Break Glass**: actualmente o post-mortem é manual. Poderia ser solicitado automaticamente ao utilizador via notificação após expiração do Break Glass.

4. **Testes de integração dos expiration handlers**: com EF Core InMemory ou SQLite para validar o fluxo completo do job sem dependência de PostgreSQL.

## Limitações residuais após implementação

- A trilha de auditoria central depende do mecanismo de request pipeline. Eventos gerados fora desse pipeline (jobs) permanecem consultáveis em `IdentityDbContext.SecurityEvents` mas não se propagam automaticamente para `AuditDbContext` nesta fase.
- Esta limitação é aceitável para o estado atual do módulo e não compromete a segurança operacional, pois os eventos existem e são consultáveis via `GET /api/v1/identity/security-events`.
