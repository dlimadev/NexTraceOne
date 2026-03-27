# P11.1 — Post-change gap report (Identity & Access critical events/audit)

## O que foi resolvido nesta fase

1. **Cobertura de eventos críticos reforçada** em Break Glass, JIT, Delegation, Access Review e ações sensíveis de identidade.
2. **Self-action prevention auditável**:
   - tentativa de self-approval JIT agora gera evento de negação;
   - tentativa de self-delegation agora gera evento de negação.
3. **Revogações críticas com trilha explícita**:
   - Break Glass revoke e Delegation revoke agora geram SecurityEvent dedicado.
4. **Fecho de trilha SecurityEvent → AuditEvent** reforçado ao garantir `Track()` em fluxos críticos para o pipeline/bridge existentes.
5. **Consulta operacional mínima** adicionada:
   - endpoint de `security-events`;
   - histórico opcional para Break Glass/JIT/Delegation.
6. **Wiring mínimo com Notifications**:
   - Break Glass activation com `SourceEventId` apontando para o SecurityEvent.

## Pendências que permanecem

1. **Propagação para Audit em jobs de expiração** (BackgroundWorkers):
   - os jobs persistem `SecurityEvent` (`Expired`) diretamente em `IdentityDbContext`;
   - nesta fase não foi implementado bridge/outbox específico para garantir cópia automática desses eventos no módulo Audit central.
2. **Notificação em tempo real mais abrangente**:
   - foi fechado wiring mínimo para Break Glass activation;
   - expansão para outros eventos críticos (ex.: revoke/expire JIT/Delegation) fica para fase seguinte.
3. **Correlação avançada e visualização consolidada**:
   - foi entregue consulta mínima;
   - não foi implementado dashboard/analytics avançado de segurança (fora de escopo).

## O que fica explicitamente para P11.2

1. Consolidação completa de trilha auditável para eventos críticos originados por background jobs (expirações).
2. Expansão controlada de notificações críticas para mais eventos privilegiados além de Break Glass activation.
3. Evolução de consultas para investigação com correlação mais rica entre:
   - SecurityEvent,
   - entidade de negócio,
   - AuditEvent/chain link.
4. Hardening complementar não tratado em P11.1 (sem ampliar para MFA/SAML/analytics avançada nesta fase).

## Limitações residuais após implementação

- A ponte para auditoria central depende do `SecurityEventTracker` no pipeline de request.
- Eventos críticos gerados fora desse pipeline (por jobs) permanecem persistidos e consultáveis no módulo Identity, porém sem garantia de espelho imediato no Audit central nesta etapa.
- Foi priorizada mudança cirúrgica e de baixo risco sobre redesign estrutural amplo, preservando arquitetura e contratos existentes.

