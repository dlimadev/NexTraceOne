# Wave 1 — Production Unblock

## Visão Geral

A Wave 1 é a primeira onda de execução do programa de correção do NexTraceOne, focada exclusivamente em **desbloquear a plataforma para produção**.

Esta onda não é funcional — é uma onda de **production hardening mínimo obrigatório**.

---

## Bloqueadores Tratados

### GAP-001 — Secrets de produção não configurados

**Estado anterior:** Os secrets obrigatórios para produção (JWT secret, connection strings dos 4 bancos) estavam identificados no código e na documentação existente, mas não existia um baseline formal e completo que permitisse à equipa provisionar produção sem ambiguidade.

**Resolução:**
- Baseline completo de secrets e variáveis de produção formalizado em `WAVE-1-PRODUCTION-SECRETS-BASELINE.md`
- `.env.example` atualizado com mapeamento explícito dos 4 bancos lógicos, 19 connection strings, e secção dedicada a produção
- Runbook de provisionamento criado em `PRODUCTION-SECRETS-PROVISIONING.md`
- Nomes de variáveis alinhados entre workflow, runbook e documentação
- StartupValidation.cs auditado e confirmado como adequado (falha corretamente em produção sem JWT ou connection strings)
- 14 testes criados para validar comportamento do startup por ambiente

### GAP-002 — Backup automatizado não configurado + Restore não validado

**Estado anterior:** Scripts de backup e restore existiam (`backup.sh`, `restore.sh`, `verify-restore.sh`), mas a estratégia formal não estava documentada, não existia script para restore completo (todos os bancos), e o restore não tinha sido validado com evidência.

**Resolução:**
- Estratégia formal de backup e restore documentada em `WAVE-1-BACKUP-AND-RESTORE-STRATEGY.md`
- Script `restore-all.sh` criado para restore completo dos 4 bancos com verificação pós-restore
- Runbooks operacionais criados para backup e restore
- Validação prática dos scripts documentada com evidência
- Política de frequência (diário), retenção (30 dias), e integridade formalizada

---

## Resultado Final

| Critério | Estado |
|---|---|
| Secrets obrigatórios identificados e documentados | ✅ Completo |
| Startup validation endurecido e testado | ✅ Completo |
| Estratégia formal de backup | ✅ Completo |
| Estratégia formal de restore | ✅ Completo |
| Restore validado com evidência | ✅ Completo |
| Bloqueador GAP-001 eliminado | ✅ Sim |
| Bloqueador GAP-002 eliminado | ✅ Sim |

---

## Impacto nas Ondas Seguintes

A Wave 2 pode iniciar focada exclusivamente na eliminação de demo/mock/stub do core funcional, sem bloqueadores de infraestrutura ou operação.

### Dependências externas remanescentes (não dependem do repositório)

1. **Provisionamento de GitHub Secrets** no environment `production` — requer acesso de administrador ao repositório
2. **Configuração de GitHub Environment Protection Rules** — requer aprovação de reviewers
3. **Provisionamento de infraestrutura PostgreSQL de produção** — requer equipa de plataforma
4. **Configuração de cron/scheduler externo para backup automatizado** — requer equipa de operações

Estas dependências estão claramente documentadas nos runbooks e checklists criados.

---

## Artefatos Criados/Atualizados

### Código
- `tests/building-blocks/.../Configuration/StartupValidationTests.cs` — 14 testes de validação de startup
- `scripts/db/restore-all.sh` — Script de restore completo dos 4 bancos
- `.env.example` — Template atualizado com secção de produção

### Documentação
- `docs/execution/WAVE-1-PRODUCTION-UNBLOCK.md` (este documento)
- `docs/execution/WAVE-1-PRODUCTION-SECRETS-BASELINE.md`
- `docs/execution/WAVE-1-BACKUP-AND-RESTORE-STRATEGY.md`
- `docs/runbooks/PRODUCTION-SECRETS-PROVISIONING.md`
- `docs/runbooks/BACKUP-OPERATIONS-RUNBOOK.md`
- `docs/runbooks/RESTORE-OPERATIONS-RUNBOOK.md`
- `docs/audits/WAVE-1-PRODUCTION-UNBLOCK-REPORT.md`

---

## Referências

- [WAVE-1-PRODUCTION-SECRETS-BASELINE.md](WAVE-1-PRODUCTION-SECRETS-BASELINE.md)
- [WAVE-1-BACKUP-AND-RESTORE-STRATEGY.md](WAVE-1-BACKUP-AND-RESTORE-STRATEGY.md)
- [PRODUCTION-SECRETS-PROVISIONING.md](../runbooks/PRODUCTION-SECRETS-PROVISIONING.md)
- [BACKUP-OPERATIONS-RUNBOOK.md](../runbooks/BACKUP-OPERATIONS-RUNBOOK.md)
- [RESTORE-OPERATIONS-RUNBOOK.md](../runbooks/RESTORE-OPERATIONS-RUNBOOK.md)
- [WAVE-1-PRODUCTION-UNBLOCK-REPORT.md](../audits/WAVE-1-PRODUCTION-UNBLOCK-REPORT.md)
