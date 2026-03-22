# Playbook — Resposta a Incidentes

> **NexTraceOne — Operação**  
> Versão: 1.0 | Data: 2026-03-22

---

## Classificação de Severidade

| Severidade | Critério | Tempo de resposta |
|-----------|---------|------------------|
| **SEV-1** | Produção totalmente indisponível; perda de dados em progresso | Imediato (< 5 min) |
| **SEV-2** | Funcionalidade crítica degradada; múltiplos utilizadores afetados | < 15 min |
| **SEV-3** | Funcionalidade não-crítica afetada; utilizadores específicos | < 1h |
| **SEV-4** | Bug cosmético; sem impacto funcional | Próxima sprint |

---

## Processo de Resposta

### Fase 1 — Detecção (0–5 min)

1. **Identificar sintomas**:
   - Alerta de health check?
   - Aumento de erros 5xx?
   - Reclamação de utilizador?
   - Pipeline falhou?

2. **Declarar incidente** no canal de operações:
   ```
   🚨 INCIDENTE DECLARADO
   Severidade: SEV-X
   Sintoma: [descrever brevemente]
   Impacto: [quantos utilizadores? quais funcionalidades?]
   Executor: [nome]
   Início: [timestamp]
   ```

3. **Verificar health imediatamente**:
   ```bash
   APIHOST="https://<prod-url>"
   curl -sf "${APIHOST}/live"
   curl -sf "${APIHOST}/ready"
   curl -sf "${APIHOST}/health" | jq .
   ```

---

### Fase 2 — Diagnóstico (5–15 min)

#### Verificar logs do ApiHost

```bash
# Últimas 200 linhas
docker logs nextraceone-apihost --tail 200

# Filtrar por erros
docker logs nextraceone-apihost --tail 500 2>&1 | grep -E "(ERROR|FATAL|Exception|Error)"
```

#### Verificar logs dos workers

```bash
docker logs nextraceone-workers --tail 100 2>&1 | grep -E "(ERROR|FATAL|fail)"
```

#### Verificar conectividade com bancos de dados

```bash
# Testar conexão com cada banco
psql "${PROD_CONN_IDENTITY}" -c "SELECT 1;" 2>&1
psql "${PROD_CONN_CATALOG}" -c "SELECT 1;" 2>&1
psql "${PROD_CONN_OPERATIONS}" -c "SELECT 1;" 2>&1
psql "${PROD_CONN_AI}" -c "SELECT 1;" 2>&1
```

#### Verificar último deploy

```bash
# Qual imagem está rodando?
docker inspect nextraceone-apihost --format '{{.Config.Image}}'

# Quando foi iniciado?
docker inspect nextraceone-apihost --format '{{.State.StartedAt}}'
```

#### Verificar migrations

```bash
# Conferir se migrations foram aplicadas
psql "${PROD_CONN_OPERATIONS}" -c "SELECT COUNT(*) FROM \"__EFMigrationsHistory\";"
```

---

### Fase 3 — Mitigação

#### Opção A — Rollback de imagem (mais rápido)

Se o problema apareceu após deploy recente:

```bash
# Ver histórico de imagens
# https://github.com/dlimadev/NexTraceOne/pkgs/container/nextraceone-apihost

export IMAGE_TAG=<tag-anterior-estavel>
IMAGE_TAG=${IMAGE_TAG} docker compose up -d --no-build apihost workers frontend

# Validar
sleep 30
curl -sf "https://<prod-url>/live"
```

Ver `docs/runbooks/ROLLBACK-RUNBOOK.md` para detalhes.

#### Opção B — Reiniciar serviços (problema de estado)

```bash
docker compose restart apihost workers
sleep 30
curl -sf "https://<prod-url>/ready"
```

#### Opção C — Scale down/up (problema de memory leak)

```bash
docker compose stop apihost
docker compose up -d apihost
```

#### Opção D — Isolamento de componente

Se o problema é no AI provider:
- Ver `docs/runbooks/AI-PROVIDER-DEGRADATION-RUNBOOK.md`

Se o problema é em migrations:
- Ver `docs/runbooks/MIGRATION-FAILURE-RUNBOOK.md`

---

### Fase 4 — Resolução e Post-mortem

1. **Confirmar resolução**:
   ```bash
   curl -sf "https://<prod-url>/health" | jq .
   # Esperado: status: Healthy para todos os checks
   ```

2. **Anunciar resolução**:
   ```
   ✅ INCIDENTE RESOLVIDO
   Duração: [X minutos]
   Causa raiz: [descrever]
   Ação tomada: [rollback / hotfix / restart]
   Próximos passos: [PIR agendado? fix definitivo?]
   ```

3. **Registrar Post-Incident Review (PIR)**:
   - O que aconteceu?
   - Por que não foi detectado antes?
   - Como prevenir recorrência?
   - Runbook precisa ser atualizado?

---

## Cenários Comuns

### Sistema não responde a /live

**Sintomas**: `curl: (7) Failed to connect`

**Diagnóstico**:
```bash
docker ps | grep apihost
# Se não está rodando:
docker logs nextraceone-apihost --tail 50
```

**Ações**:
1. Container parado → `docker compose up -d apihost`
2. Container em crash loop → verificar logs para causa; rollback se necessário
3. Porta não exposta → verificar docker-compose.yml e firewall

---

### /ready retorna Unhealthy

**Sintomas**: HTTP 503 com JSON indicando check falhando

**Diagnóstico**:
```bash
curl -sf "https://<prod-url>/health" | jq .checks
# Verificar qual check está falhando: database? external service?
```

**Ações**:
- Check de DB falhando → verificar conectividade e credentials
- Check de AI provider falhando → não crítico; ver runbook AI
- Check de worker falhando → reiniciar workers

---

### Aumento de erros 5xx nas APIs

**Sintomas**: erros 500/502/503 em endpoints de negócio

**Diagnóstico**:
```bash
docker logs nextraceone-apihost --tail 500 2>&1 | grep "500\|Exception" | head -20
```

**Ações**:
1. Erro de serialização JSON → investigar payload malformado
2. Erro de DB constraint → investigar migration pendente
3. Erro de configuração → verificar appsettings e environment variables
4. Erro desconhecido → rollback se impacto alto; hotfix se isolado

---

### Workers pararam de processar outbox

**Sintomas**: eventos não processados; integrações atrasadas

**Diagnóstico**:
```bash
docker logs nextraceone-workers --tail 200 2>&1 | grep -E "(outbox|ERROR|Exception)"

# Verificar backlog no banco
psql "${PROD_CONN_OPERATIONS}" -c "SELECT COUNT(*) FROM outbox_messages WHERE processed_at IS NULL;"
```

**Ações**:
1. Reiniciar workers: `docker compose restart workers`
2. Se backlog muito alto: verificar logs para deadlocks ou erros de processamento
3. Se persistir: escalar para hotfix

---

*Playbook mantido pelo Tech Lead. Revisar após cada incidente SEV-1 ou SEV-2.*
