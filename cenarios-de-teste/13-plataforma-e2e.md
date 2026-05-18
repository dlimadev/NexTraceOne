# Plataforma — Cenários de Teste E2E, Background Workers e Building Blocks

> Cobertura: Fluxos E2E multi-módulo, Outbox, Background Jobs, CLI, Load Testing, Inicialização

---

## Fluxos E2E Multi-Módulo

### TC-E2E-001 — Onboarding completo: criar tenant → provisionar → login → criar contrato

| Campo | Valor |
|-------|-------|
| **Módulo** | Plataforma (multi-módulo) |
| **Feature** | ProvisionTenant → LocalLogin → CreateDraft |
| **Tipo** | E2E |
| **Prioridade** | Crítica |

**Pré-condições:**
- Ambiente de teste com API, DB e migrations aplicadas.

**Passos:**
1. `POST /iam/tenants` — cria tenant `acme-corp` no plano `Professional`.
2. `ProvisionTenant` executa: semeia roles padrão, access policies, guardrails de IA.
3. `POST /iam/auth/login` com credenciais do admin — recebe JWT.
4. `POST /catalog/drafts` com JWT — cria rascunho de contrato REST.

**Resultado Esperado:**
- Tenant criado e provisionado; login bem-sucedido; contrato criado no contexto do tenant.

**Critério de Aceite:** Cada passo retorna 2xx; `tenantId` consistente em todos os recursos.

---

### TC-E2E-002 — Fluxo de release: ingerir commit → classificar mudança → aprovação → deploy

| Campo | Valor |
|-------|-------|
| **Módulo** | ChangeGovernance + Notifications |
| **Feature** | IngestCommit → ClassifyChangeLevel → RespondToApprovalRequest → UpdateDeploymentState |
| **Tipo** | E2E |
| **Prioridade** | Crítica |

**Passos:**
1. `IngestCommit` com payload de CI/CD.
2. `ClassifyChangeLevel` — retorna `Minor`.
3. Approval policy triggered → `ListPendingApprovals` mostra 1 item.
4. `RespondToApprovalRequest(decision: Approved)`.
5. `UpdateDeploymentState(status: Deployed)`.
6. Outbox publica `ReleaseDeployedEvent` → Notification handler cria notificação.

**Resultado Esperado:**
- Pipeline completo executado; notificação criada para usuários inscritos.

**Critério de Aceite:** `GetRelease` retorna `Status = Deployed`; `ListNotifications` mostra notificação.

---

### TC-E2E-003 — Incidente → PIR → Knowledge Document

| Campo | Valor |
|-------|-------|
| **Módulo** | OperationalIntelligence + Knowledge |
| **Feature** | CreateIncident → StartPostIncidentReview → CreateKnowledgeDocument |
| **Tipo** | E2E |
| **Prioridade** | Alta |

**Passos:**
1. `CreateIncident` com severidade `High`.
2. `ResolveIncident` após 2h.
3. `StartPostIncidentReview` — PIR agendado.
4. `ProgressPostIncidentReview` até `Completed`.
5. `CreateKnowledgeDocument` com análise de causa raiz do PIR.

**Resultado Esperado:**
- PIR concluído; documento de conhecimento criado e vinculado ao incidente.

**Critério de Aceite:** `GetKnowledgeDocumentById` retorna documento; `GetPostIncidentReview` retorna `Status = Completed`.

---

### TC-E2E-004 — Deploy bloqueado por freeze window

| Campo | Valor |
|-------|-------|
| **Módulo** | ChangeGovernance |
| **Feature** | CreateFreezeWindow → CheckFreezeConflict → CheckDeployReadiness |
| **Tipo** | E2E |
| **Prioridade** | Crítica |

**Passos:**
1. `CreateFreezeWindow(start: now, end: now+4h, scope: All)`.
2. `IngestCommit` durante janela de freeze.
3. `CheckFreezeConflict` — detecta conflito.
4. `CheckDeployReadiness` — retorna `ReadyToDeploy = false`.

**Resultado Esperado:**
- Deploy bloqueado com mensagem de freeze ativo.

**Critério de Aceite:** `ReadyToDeploy = false`; `BlockedBy = "FreezeWindow"`.

---

### TC-E2E-005 — Promotion gate bloqueia por vulnerabilidade

| Campo | Valor |
|-------|-------|
| **Módulo** | Catalog + ChangeGovernance |
| **Feature** | IngestSbomRecord → EvaluateVulnerabilityPromotionGate → BlockPromotion |
| **Tipo** | E2E |
| **Prioridade** | Crítica |

**Passos:**
1. `IngestSbomRecord` com dependência com CVE crítica.
2. `EvaluateVulnerabilityPromotionGate` — gate falha.
3. `EvaluatePromotionGates(releaseId)` — resultado: `AllGatesPassed = false`.
4. `BlockPromotion(releaseId, reason: "CVE crítica em dependência")`.

**Resultado Esperado:**
- Promoção bloqueada; equipe notificada via Outbox.

**Critério de Aceite:** `GetPromotionGateStatus` retorna `Blocked`.

---

### TC-E2E-006 — Agente de IA revisa contrato e publica comentário

| Campo | Valor |
|-------|-------|
| **Módulo** | AIKnowledge + Catalog |
| **Feature** | ReviewContractDraft → SubmitContractReview |
| **Tipo** | E2E |
| **Prioridade** | Alta |

**Passos:**
1. Rascunho de contrato `D1` no status `PendingReview`.
2. `ReviewContractDraft.Command(draftId: D1.Id)` — agente IA analisa spec.
3. Resultado retornado com issues encontradas.
4. `SubmitContractReview` com feedback do agente.

**Resultado Esperado:**
- Revisão registrada em `ListDraftReviews`; contrato com status de revisão atualizado.

**Critério de Aceite:** HTTP 200; `ListDraftReviews` mostra 1 revisão do agente.

---

### TC-E2E-007 — Provisionar tenant semeia todos os defaults

| Campo | Valor |
|-------|-------|
| **Módulo** | IdentityAccess + AIKnowledge + Governance |
| **Feature** | ProvisionTenant |
| **Tipo** | Integração |
| **Prioridade** | Crítica |

**Pré-condições:** Tenant recém-criado sem nenhum dado.

**Passos:**
1. `ProvisionTenant.Command(tenantId)`.
2. Handler semeia: roles padrão, access policies, guardrails de IA, modelos de IA, skills, templates de prompt, tool definitions.

**Resultado Esperado:**
- `ListRoles` retorna roles padrão (Admin, Developer, Viewer).
- `ListGuardrails` retorna guardrails de segurança padrão.
- `SeedDefaultModels` executado.

**Critério de Aceite:** Todos os recursos padrão presentes após provisionamento.

---

## Outbox e Background Workers

### TC-E2E-008 — Processamento normal do Outbox

| Campo | Valor |
|-------|-------|
| **Módulo** | BuildingBlocks.Infrastructure |
| **Feature** | ModuleOutboxProcessorJob |
| **Tipo** | Integração |
| **Prioridade** | Crítica |

**Pré-condições:**
- 5 mensagens `OutboxMessage` com `ProcessedAt = null`.

**Passos:**
1. Job executa ciclo: `pg_try_advisory_lock` → lê 5 mensagens → deserializa → publica via `IEventBus` → salva `ProcessedAt`.

**Resultado Esperado:**
- Todas as 5 mensagens com `ProcessedAt` preenchido.
- Lock liberado no `finally`.

**Critério de Aceite:** `ProcessedAt != null` para todas; `RetryCount` não incrementado.

---

### TC-E2E-009 — Retry no Outbox após falha

| Campo | Valor |
|-------|-------|
| **Módulo** | BuildingBlocks.Infrastructure |
| **Feature** | ModuleOutboxProcessorJob |
| **Tipo** | Integração |
| **Prioridade** | Crítica |

**Pré-condições:** Mensagem `M1` com handler que lança exceção nas primeiras 4 tentativas.

**Passos:**
1. Ciclos 1-4: handler lança; `RetryCount` incrementado; `ProcessedAt = null`.
2. Ciclo 5 (último): handler lança novamente.
3. `RetryCount = 5` → mensagem movida para Dead Letter Queue via `IDeadLetterRepository`.

**Resultado Esperado:**
- `M1` na tabela DLQ com `FailedAt` preenchido.
- Sistema não trava; demais mensagens processadas.

**Critério de Aceite:** `RetryCount == 5`; registro na `int_event_consumer_dead_letters`.

---

### TC-E2E-010 — Advisory lock evita processamento duplo

| Campo | Valor |
|-------|-------|
| **Módulo** | BuildingBlocks.Infrastructure |
| **Feature** | ModuleOutboxProcessorJob — pg_try_advisory_lock |
| **Tipo** | Integração |
| **Prioridade** | Crítica |

**Pré-condições:**
- 2 instâncias do BackgroundWorkers rodando (multi-pod).

**Passos:**
1. Instância A adquire lock para o contexto `IdentityDbContext`.
2. Instância B tenta adquirir o mesmo lock — `pg_try_advisory_lock` retorna `false`.
3. Instância B pula o ciclo sem processar.

**Resultado Esperado:**
- Cada mensagem processada exatamente 1 vez.

**Critério de Aceite:** Nenhuma duplicação de eventos; `ProcessedAt` setado apenas uma vez por mensagem.

---

### TC-E2E-011 — Job LicenseRecalculationJob

| Campo | Valor |
|-------|-------|
| **Módulo** | BackgroundWorkers |
| **Feature** | LicenseRecalculationJob |
| **Tipo** | Integração |
| **Prioridade** | Alta |

**Pré-condições:**
- Tenant com 100 hosts registrados; licença com `IncludedHostUnits = 80`.

**Passos:**
1. Job executa a cada 15 min.
2. Conta hosts ativos por tenant.
3. Atualiza `CurrentHostUnits = 100`; `OverageUnits = 20`.

**Resultado Esperado:**
- `TenantLicense.CurrentHostUnits == 100`; alerta de overage gerado.

**Critério de Aceite:** `GetTenantLicense` retorna valores atualizados.

---

### TC-E2E-012 — AlertEvaluationJob — LicenseUtilization

| Campo | Valor |
|-------|-------|
| **Módulo** | BackgroundWorkers |
| **Feature** | AlertEvaluationJob |
| **Tipo** | Integração |
| **Prioridade** | Alta |

**Pré-condições:**
- `LicenseUtilization` alerta configurado com `ThresholdValue = 80%`.
- Tenant com utilização atual de 95%.

**Passos:**
1. Job avalia condição: `currentUtilization > threshold`.
2. Condição verdadeira → dispara alerta.

**Resultado Esperado:**
- Alerta `LicenseUtilization` gerado; notificação no Outbox.

**Critério de Aceite:** `GetAlertFiringHistory` mostra alerta disparado.

---

### TC-E2E-013 — AlertEvaluationJob — AgentHeartbeatMissed

| Campo | Valor |
|-------|-------|
| **Módulo** | BackgroundWorkers |
| **Feature** | AlertEvaluationJob |
| **Tipo** | Integração |
| **Prioridade** | Alta |

**Pré-condições:**
- Agente registrado com `LastHeartbeat = now - 10min`.
- `ThresholdValue = 5` (minutos).

**Passos:**
1. Job avalia: `(now - LastHeartbeat).TotalMinutes > ThresholdValue`.
2. Condição: `10 > 5` → verdadeiro.
3. Alerta `AgentHeartbeatMissed` disparado.

**Resultado Esperado:**
- Alerta gerado; agente marcado como `Unhealthy`.

**Critério de Aceite:** `GetAlertFiringHistory` mostra alerta do agente.

---

### TC-E2E-014 — Outbox com falha de serialização

| Campo | Valor |
|-------|-------|
| **Módulo** | BuildingBlocks.Infrastructure |
| **Feature** | ModuleOutboxProcessorJob |
| **Tipo** | Integração |
| **Prioridade** | Alta |

**Pré-condições:** Mensagem `M1` com `Payload` JSON corrompido.

**Passos:**
1. Job tenta deserializar → `JsonException` lançada.
2. `RetryCount` incrementado.
3. Após 5 falhas, vai para DLQ.

**Resultado Esperado:**
- Mensagem na DLQ; log de erro registrado; demais mensagens não afetadas.

**Critério de Aceite:** DLQ contém `M1` com `ErrorDetails` descrevendo a falha.

---

## CLI

### TC-E2E-015 — Comando CLI: status da plataforma

| Campo | Valor |
|-------|-------|
| **Módulo** | NexTraceOne.CLI |
| **Feature** | GetPlatformHealth via CLI |
| **Tipo** | Integração |
| **Prioridade** | Alta |

**Passos:**
1. Executar `nexttrace platform status --tenant acme-corp`.

**Resultado Esperado:**
- Saída formatada com módulos e status (healthy/unhealthy).

**Critério de Aceite:** Exit code 0; saída contém `status: healthy`.

---

### TC-E2E-016 — Comando CLI: migrar banco

| Campo | Valor |
|-------|-------|
| **Módulo** | NexTraceOne.CLI |
| **Feature** | Database migration via CLI |
| **Tipo** | Integração |
| **Prioridade** | Alta |

**Passos:**
1. Executar `nexttrace db migrate --context IdentityDbContext`.

**Resultado Esperado:**
- Migrations aplicadas; saída lista migrations aplicadas.

**Critério de Aceite:** Exit code 0.

---

### TC-E2E-017 — CLI com credenciais inválidas

| Campo | Valor |
|-------|-------|
| **Módulo** | NexTraceOne.CLI |
| **Feature** | Autenticação CLI |
| **Tipo** | Segurança |
| **Prioridade** | Alta |

**Passos:**
1. Executar comando CLI com API token inválido.

**Resultado Esperado:**
- Exit code não-zero; mensagem de erro de autenticação.

**Critério de Aceite:** Nenhuma operação executada com credenciais inválidas.

---

## Startup e Integridade

### TC-E2E-018 — Verificação de integridade de assembly (habilitada)

| Campo | Valor |
|-------|-------|
| **Módulo** | ApiHost |
| **Feature** | Assembly Integrity Check |
| **Tipo** | Integração |
| **Prioridade** | Alta |

**Pré-condições:**
- `NEXTRACE_SKIP_INTEGRITY` não definida.

**Passos:**
1. Iniciar `NexTraceOne.ApiHost`.
2. Startup verifica hash dos assemblies.

**Resultado Esperado:**
- Startup normal se hashes válidos.

**Critério de Aceite:** Host inicia sem exceção.

---

### TC-E2E-019 — Verificação de integridade com assembly adulterado

| Campo | Valor |
|-------|-------|
| **Módulo** | ApiHost |
| **Feature** | Assembly Integrity Check |
| **Tipo** | Segurança |
| **Prioridade** | Crítica |

**Pré-condições:** Assembly com hash divergente do esperado.

**Passos:**
1. Iniciar host sem `NEXTRACE_SKIP_INTEGRITY`.

**Resultado Esperado:**
- `InvalidOperationException` lançada; host não sobe.

**Critério de Aceite:** Processo termina com código de erro; log de segurança registrado.

---

### TC-E2E-020 — Pular verificação de integridade em dev/CI

| Campo | Valor |
|-------|-------|
| **Módulo** | ApiHost |
| **Feature** | Assembly Integrity Check |
| **Tipo** | Integração |
| **Prioridade** | Média |

**Passos:**
1. Definir `NEXTRACE_SKIP_INTEGRITY=true`.
2. Iniciar host.

**Resultado Esperado:**
- Host inicia sem verificação de hash.

**Critério de Aceite:** Log indica "integrity check skipped".

---

### TC-E2E-021 — Migrations EF pendentes (aviso)

| Campo | Valor |
|-------|-------|
| **Módulo** | ApiHost / BackgroundWorkers |
| **Feature** | Pending Migrations Warning |
| **Tipo** | Integração |
| **Prioridade** | Média |

**Pré-condições:**
- `NEXTRACE_IGNORE_PENDING_MODEL_CHANGES` não definida.
- Migration pendente no `AIKnowledgeDbContext`.

**Passos:**
1. Iniciar host.

**Resultado Esperado:**
- Aviso de migration pendente no log.
- Host **não** aborta — apenas avisa.

**Critério de Aceite:** Log contém "pending model changes detected".

---

## Load Testing

### TC-E2E-022 — Carga: autenticação simultânea

| Campo | Valor |
|-------|-------|
| **Módulo** | IdentityAccess |
| **Feature** | LocalLogin — Load Test |
| **Tipo** | Carga |
| **Prioridade** | Alta |

**Pré-condições:** k6 configurado; 500 usuários virtuais.

**Passos:**
1. Executar `k6 run tests/load/scenarios/auth.js --vus 500 --duration 60s`.

**Resultado Esperado:**
- P95 de latência < 300ms.
- Taxa de erro < 1%.
- Throughput > 1.000 req/s.

**Critério de Aceite:** Relatório k6 dentro dos thresholds.

---

### TC-E2E-023 — Carga: leitura de releases

| Campo | Valor |
|-------|-------|
| **Módulo** | ChangeGovernance |
| **Feature** | ListReleases — Load Test |
| **Tipo** | Carga |
| **Prioridade** | Alta |

**Passos:**
1. `k6 run tests/load/scenarios/releases.js --vus 200 --duration 120s`.

**Resultado Esperado:**
- P99 < 500ms; zero erros 5xx.

**Critério de Aceite:** k6 thresholds passam.

---

### TC-E2E-024 — Carga: contratos (listagem + busca)

| Campo | Valor |
|-------|-------|
| **Módulo** | Catalog |
| **Feature** | ListContracts + SearchContracts — Load Test |
| **Tipo** | Carga |
| **Prioridade** | Alta |

**Passos:**
1. `k6 run tests/load/scenarios/contracts.js --vus 100 --duration 60s`.
2. Mix: 70% listagem, 30% busca full-text.

**Resultado Esperado:**
- P95 listagem < 200ms; P95 busca < 500ms.

**Critério de Aceite:** k6 thresholds passam.

---

### TC-E2E-025 — Fluxo completo de revisão de acesso (E2E)

| Campo | Valor |
|-------|-------|
| **Módulo** | IdentityAccess |
| **Feature** | StartAccessReviewCampaign → DecideAccessReviewItem → EscalateOverdueAccessReviews |
| **Tipo** | E2E |
| **Prioridade** | Alta |

**Passos:**
1. Admin inicia campanha: `StartAccessReviewCampaign(scope: AllUsers, deadline: now+7d)`.
2. Revisores decidem itens: `DecideAccessReviewItem(decision: Revoke)` para usuários inativos.
3. Após prazo: `EscalateOverdueAccessReviews` notifica items pendentes.
4. Relatório `GetAccessReviewCampaign` mostra progresso.

**Resultado Esperado:**
- Acessos revogados aplicados; escalação para itens pendentes; relatório reflete estado real.

**Critério de Aceite:** Usuários com acesso revogado não conseguem login; `ListActiveSessions` não mostra sessões para usuários revogados.

---
