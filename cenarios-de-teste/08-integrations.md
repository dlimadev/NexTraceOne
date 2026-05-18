# Cenários de Teste — Módulo: Integrations

> **Versão:** 1.0  
> **Data:** 2026-05-18  
> **Responsável:** QA Engineering  
> **Módulo:** Integrations  
> **Total de casos:** 27

---

## Sumário

| ID | Título | Prioridade |
|----|--------|-----------|
| TC-INT-001 | Obter conector de integração por ID | Alta |
| TC-INT-002 | Listar conectores de integração com filtro | Alta |
| TC-INT-003 | Verificar saúde dos conectores (GetIntegrationHealthReport) | Crítica |
| TC-INT-004 | Retentar conector com falha (RetryConnector) | Alta |
| TC-INT-005 | Obter opções de filtro de integração | Média |
| TC-INT-006 | Registrar assinatura de webhook | Crítica |
| TC-INT-007 | Listar assinaturas de webhook | Alta |
| TC-INT-008 | Obter tipos de eventos de webhook disponíveis | Média |
| TC-INT-009 | Criar template de webhook | Alta |
| TC-INT-010 | Listar templates de webhook | Média |
| TC-INT-011 | Alternar template de webhook (habilitar/desabilitar) | Alta |
| TC-INT-012 | Excluir template de webhook | Alta |
| TC-INT-013 | Sincronizar itens de trabalho do Jira (SyncJiraWorkItems) | Alta |
| TC-INT-014 | Importar lote de custos (ImportCostBatch) | Crítica |
| TC-INT-015 | Importar lote de custos com dados inválidos | Alta |
| TC-INT-016 | Listar lotes de importação de custo | Média |
| TC-INT-017 | Registrar marcador externo (RegisterExternalMarker) | Alta |
| TC-INT-018 | Testar conectividade de rede (TestNetworkConnectivity) | Alta |
| TC-INT-019 | Obter status de entrega de webhook | Alta |
| TC-INT-020 | Obter histórico de entregas de webhook | Alta |
| TC-INT-021 | Listar canais de entrega disponíveis | Média |
| TC-INT-022 | Criar ou atualizar canal de entrega (UpsertDeliveryChannel) | Alta |
| TC-INT-023 | Webhook entregue com sucesso e confirmado | Crítica |
| TC-INT-024 | Retry automático de webhook após falha de entrega | Crítica |
| TC-INT-025 | Isolamento de assinaturas de webhook por tenant | Crítica |
| TC-INT-026 | Conector retorna erro de autenticação (401) | Alta |
| TC-INT-027 | Lote de custo duplicado detectado e rejeitado | Alta |

---

### TC-INT-001 — Obter conector de integração por ID

| Campo | Valor |
|-------|-------|
| **Módulo** | Integrations |
| **Feature** | GetIntegrationConnector |
| **Tipo** | Integração |
| **Prioridade** | Alta |

**Pré-condições:**
- Conector `github-connector` registrado com ID conhecido

**Passos:**
1. Autenticar com JWT válido
2. Enviar `GET /api/integrations/connectors/{id}`

**Resultado Esperado:**
- HTTP 200 OK
- Body contém `id`, `name`, `type`, `status`, `lastSyncAt`, `configuration`

**Critério de Aceite:** Todos os campos do conector presentes na resposta

---

### TC-INT-002 — Listar conectores de integração com filtro

| Campo | Valor |
|-------|-------|
| **Módulo** | Integrations |
| **Feature** | ListIntegrationConnectors |
| **Tipo** | Integração |
| **Prioridade** | Alta |

**Pré-condições:**
- Múltiplos conectores registrados: GitHub, Jira, PagerDuty, Slack

**Passos:**
1. Autenticar com JWT válido
2. Enviar `GET /api/integrations/connectors?type=Jira`
3. Enviar `GET /api/integrations/connectors?status=Healthy`

**Resultado Esperado:**
- Primeira chamada: apenas conectores do tipo `Jira`
- Segunda chamada: apenas conectores com status `Healthy`

**Critério de Aceite:** Filtros funcionais; isolamento por tenant correto

---

### TC-INT-003 — Verificar saúde dos conectores (GetIntegrationHealthReport)

| Campo | Valor |
|-------|-------|
| **Módulo** | Integrations |
| **Feature** | GetIntegrationHealthReport |
| **Tipo** | Integração |
| **Prioridade** | Crítica |

**Pré-condições:**
- Pelo menos 3 conectores ativos; 1 com falha de conexão

**Passos:**
1. Autenticar com JWT válido
2. Enviar `GET /api/integrations/health-report`

**Resultado Esperado:**
- HTTP 200 OK
- `healthyConnectors: 2`, `unhealthyConnectors: 1`, `totalConnectors: 3`
- Conector com falha listado com `lastError` e `lastAttemptAt`

**Critério de Aceite:** Relatório de saúde preciso com detalhe de falha

---

### TC-INT-004 — Retentar conector com falha (RetryConnector)

| Campo | Valor |
|-------|-------|
| **Módulo** | Integrations |
| **Feature** | RetryConnector |
| **Tipo** | Integração |
| **Prioridade** | Alta |

**Pré-condições:**
- Conector `jira-connector` com status `Failed` após 3 tentativas

**Passos:**
1. Autenticar com JWT de admin de integrações
2. Enviar `POST /api/integrations/connectors/{id}/retry`
3. Verificar status após retry

**Resultado Esperado:**
- HTTP 202 Accepted
- Tentativa de reconexão iniciada
- Status transitório `Retrying` e depois `Healthy` (se sucesso) ou `Failed` (se falha)

**Critério de Aceite:** Retry manual dispara nova tentativa; status atualizado corretamente

---

### TC-INT-005 — Obter opções de filtro de integração

| Campo | Valor |
|-------|-------|
| **Módulo** | Integrations |
| **Feature** | GetIntegrationFilterOptions |
| **Tipo** | Integração |
| **Prioridade** | Média |

**Pré-condições:**
- Conectores de múltiplos tipos registrados

**Passos:**
1. Autenticar com JWT válido
2. Enviar `GET /api/integrations/filter-options`

**Resultado Esperado:**
- HTTP 200 OK
- `connectorTypes: ["GitHub", "Jira", "PagerDuty", "Slack"]`
- `statusOptions: ["Healthy", "Failed", "Retrying", "Disabled"]`

**Critério de Aceite:** Opções de filtro refletem dados reais do tenant

---

### TC-INT-006 — Registrar assinatura de webhook

| Campo | Valor |
|-------|-------|
| **Módulo** | Integrations |
| **Feature** | RegisterWebhookSubscription |
| **Tipo** | Integração |
| **Prioridade** | Crítica |

**Pré-condições:**
- URL de endpoint HTTPS acessível configurada
- Usuário com role `integrations:webhooks`

**Passos:**
1. Autenticar com JWT adequado
2. Enviar `POST /api/integrations/webhooks/subscriptions` com body:
   ```json
   {
     "name": "Deploy Alerts",
     "url": "https://hooks.example.com/deploy",
     "events": ["deployment.completed", "deployment.failed"],
     "secret": "webhook-secret-token"
   }
   ```
3. Verificar ID gerado e status

**Resultado Esperado:**
- HTTP 201 Created
- `id`, `name`, `url`, `events`, `status: "Active"` na resposta
- Secret armazenado de forma criptografada (campo `[EncryptedField]`)

**Critério de Aceite:** Assinatura criada com secret criptografado; status `Active`

---

### TC-INT-007 — Listar assinaturas de webhook

| Campo | Valor |
|-------|-------|
| **Módulo** | Integrations |
| **Feature** | ListWebhookSubscriptions |
| **Tipo** | Integração |
| **Prioridade** | Alta |

**Pré-condições:**
- Pelo menos 3 assinaturas de webhook para o tenant atual

**Passos:**
1. Autenticar com JWT válido
2. Enviar `GET /api/integrations/webhooks/subscriptions`

**Resultado Esperado:**
- HTTP 200 OK
- Lista apenas das assinaturas do tenant atual (isolamento RLS)
- Secrets não retornados na listagem

**Critério de Aceite:** Isolamento por tenant correto; secrets omitidos na resposta

---

### TC-INT-008 — Obter tipos de eventos de webhook disponíveis

| Campo | Valor |
|-------|-------|
| **Módulo** | Integrations |
| **Feature** | GetWebhookEventTypes |
| **Tipo** | Integração |
| **Prioridade** | Média |

**Pré-condições:**
- Catálogo de eventos configurado na plataforma

**Passos:**
1. Autenticar com JWT válido
2. Enviar `GET /api/integrations/webhooks/event-types`

**Resultado Esperado:**
- Lista de tipos de eventos disponíveis agrupados por módulo
- Cada evento contém `name`, `description`, `payload-schema`

**Critério de Aceite:** Catálogo completo retornado com schema de payload por evento

---

### TC-INT-009 — Criar template de webhook

| Campo | Valor |
|-------|-------|
| **Módulo** | Integrations |
| **Feature** | CreateWebhookTemplate |
| **Tipo** | Integração |
| **Prioridade** | Alta |

**Pré-condições:**
- Usuário com role `integrations:webhooks`

**Passos:**
1. Autenticar com JWT adequado
2. Enviar `POST /api/integrations/webhooks/templates` com body:
   ```json
   {
     "name": "Slack Deploy Notification",
     "eventType": "deployment.completed",
     "bodyTemplate": "{\"text\": \"Deploy {{service}} concluído em {{environment}}\"}",
     "headers": { "Content-Type": "application/json" }
   }
   ```

**Resultado Esperado:**
- HTTP 201 Created
- Template criado com `status: "Active"` por padrão

**Critério de Aceite:** Template criado e disponível para uso em assinaturas

---

### TC-INT-010 — Listar templates de webhook

| Campo | Valor |
|-------|-------|
| **Módulo** | Integrations |
| **Feature** | ListWebhookTemplates |
| **Tipo** | Integração |
| **Prioridade** | Média |

**Pré-condições:**
- Templates criados para o tenant atual

**Passos:**
1. Autenticar com JWT válido
2. Enviar `GET /api/integrations/webhooks/templates`

**Resultado Esperado:**
- Lista de templates com `id`, `name`, `eventType`, `status`
- Somente templates do tenant atual retornados

**Critério de Aceite:** Listagem isolada por tenant

---

### TC-INT-011 — Alternar template de webhook (habilitar/desabilitar)

| Campo | Valor |
|-------|-------|
| **Módulo** | Integrations |
| **Feature** | ToggleWebhookTemplate |
| **Tipo** | Integração |
| **Prioridade** | Alta |

**Pré-condições:**
- Template `Slack Deploy Notification` com status `Active`

**Passos:**
1. Autenticar com JWT de admin
2. Enviar `PATCH /api/integrations/webhooks/templates/{id}/toggle` com `{ "enabled": false }`
3. Verificar status
4. Enviar novamente com `{ "enabled": true }`

**Resultado Esperado:**
- Após desabilitação: `status: "Inactive"`, notificações não enviadas
- Após reabilitação: `status: "Active"`

**Critério de Aceite:** Toggle funcional; webhooks com template inativo não disparados

---

### TC-INT-012 — Excluir template de webhook

| Campo | Valor |
|-------|-------|
| **Módulo** | Integrations |
| **Feature** | DeleteWebhookTemplate |
| **Tipo** | Integração |
| **Prioridade** | Alta |

**Pré-condições:**
- Template sem assinaturas ativas vinculadas

**Passos:**
1. Autenticar com JWT de admin
2. Enviar `DELETE /api/integrations/webhooks/templates/{id}`
3. Verificar que o template não aparece na listagem

**Resultado Esperado:**
- HTTP 204 No Content
- Template removido da listagem

**Critério de Aceite:** Template excluído; listagem não o retorna

---

### TC-INT-013 — Sincronizar itens de trabalho do Jira (SyncJiraWorkItems)

| Campo | Valor |
|-------|-------|
| **Módulo** | Integrations |
| **Feature** | SyncJiraWorkItems |
| **Tipo** | Integração |
| **Prioridade** | Alta |

**Pré-condições:**
- Conector Jira ativo com credenciais válidas
- Projeto Jira com 5 issues criadas

**Passos:**
1. Autenticar com JWT de integração
2. Enviar `POST /api/integrations/jira/sync` com body `{ "projectKey": "NXT", "since": "2026-05-01" }`
3. Verificar itens sincronizados

**Resultado Esperado:**
- HTTP 202 Accepted
- Sincronização assíncrona iniciada
- Após conclusão: 5 itens importados com `jiraId`, `title`, `status`, `assignee`

**Critério de Aceite:** Itens Jira importados corretamente; sincronização incremental por data

---

### TC-INT-014 — Importar lote de custos (ImportCostBatch)

| Campo | Valor |
|-------|-------|
| **Módulo** | Integrations |
| **Feature** | ImportCostBatch |
| **Tipo** | Integração |
| **Prioridade** | Crítica |

**Pré-condições:**
- Arquivo CSV válido com registros de custo por serviço e período
- Usuário com role `integrations:cost-import`

**Passos:**
1. Autenticar com JWT adequado
2. Enviar `POST /api/integrations/cost-import/batches` com `multipart/form-data` contendo o CSV
3. Verificar lote criado

**Resultado Esperado:**
- HTTP 202 Accepted
- `batchId` retornado
- Processamento assíncrono: status `Processing` → `Completed`
- Registros disponíveis no módulo de FinOps

**Critério de Aceite:** Lote importado com sucesso; dados disponíveis para análise de custo

---

### TC-INT-015 — Importar lote de custos com dados inválidos

| Campo | Valor |
|-------|-------|
| **Módulo** | Integrations |
| **Feature** | ImportCostBatch |
| **Tipo** | Integração |
| **Prioridade** | Alta |

**Pré-condições:**
- CSV com coluna `amount` contendo valores não numéricos

**Passos:**
1. Autenticar com JWT adequado
2. Enviar `POST /api/integrations/cost-import/batches` com CSV inválido
3. Verificar resposta de erro

**Resultado Esperado:**
- HTTP 422 Unprocessable Entity ou lote criado com status `Failed`
- Lista de erros de validação com linha e coluna específicas

**Critério de Aceite:** Dados inválidos rejeitados com erros detalhados por linha

---

### TC-INT-016 — Listar lotes de importação de custo

| Campo | Valor |
|-------|-------|
| **Módulo** | Integrations |
| **Feature** | ListCostImportBatches |
| **Tipo** | Integração |
| **Prioridade** | Média |

**Pré-condições:**
- Múltiplos lotes importados com diferentes status

**Passos:**
1. Autenticar com JWT válido
2. Enviar `GET /api/integrations/cost-import/batches`

**Resultado Esperado:**
- Lista com `batchId`, `status`, `recordCount`, `importedAt`, `importedBy`
- Ordenação por `importedAt` decrescente

**Critério de Aceite:** Listagem isolada por tenant com todos os campos

---

### TC-INT-017 — Registrar marcador externo (RegisterExternalMarker)

| Campo | Valor |
|-------|-------|
| **Módulo** | Integrations |
| **Feature** | RegisterExternalMarker |
| **Tipo** | Integração |
| **Prioridade** | Alta |

**Pré-condições:**
- Serviço externo (CI/CD) com token de API válido

**Passos:**
1. Autenticar com token de API de integração
2. Enviar `POST /api/integrations/markers` com body:
   ```json
   {
     "type": "deployment",
     "serviceId": "payment-svc",
     "version": "v2.3.1",
     "environment": "production",
     "timestamp": "2026-05-18T14:30:00Z",
     "metadata": { "commit": "abc123", "pipeline": "github-actions" }
   }
   ```

**Resultado Esperado:**
- HTTP 201 Created
- Marcador registrado e disponível para correlação em OperationalIntelligence

**Critério de Aceite:** Marcador persistido com todos os metadados; disponível para correlação

---

### TC-INT-018 — Testar conectividade de rede (TestNetworkConnectivity)

| Campo | Valor |
|-------|-------|
| **Módulo** | Integrations |
| **Feature** | TestNetworkConnectivity |
| **Tipo** | Integração |
| **Prioridade** | Alta |

**Pré-condições:**
- Endpoint de destino `https://api.jira.company.com` configurado

**Passos:**
1. Autenticar com JWT de admin
2. Enviar `POST /api/integrations/network/test` com body `{ "host": "api.jira.company.com", "port": 443, "protocol": "HTTPS" }`

**Resultado Esperado:**
- HTTP 200 OK
- `{ "reachable": true, "latencyMs": 45, "tlsValid": true }`

**Critério de Aceite:** Teste de conectividade executado e resultado retornado em menos de 5 segundos

---

### TC-INT-019 — Obter status de entrega de webhook

| Campo | Valor |
|-------|-------|
| **Módulo** | Integrations |
| **Feature** | GetDeliveryStatus |
| **Tipo** | Integração |
| **Prioridade** | Alta |

**Pré-condições:**
- Webhook disparado com `deliveryId` conhecido

**Passos:**
1. Autenticar com JWT válido
2. Enviar `GET /api/integrations/deliveries/{deliveryId}/status`

**Resultado Esperado:**
- HTTP 200 OK
- `{ "deliveryId": "...", "status": "Delivered", "httpStatus": 200, "deliveredAt": "..." }`

**Critério de Aceite:** Status de entrega preciso com código HTTP de resposta do destino

---

### TC-INT-020 — Obter histórico de entregas de webhook

| Campo | Valor |
|-------|-------|
| **Módulo** | Integrations |
| **Feature** | GetDeliveryHistory |
| **Tipo** | Integração |
| **Prioridade** | Alta |

**Pré-condições:**
- Assinatura de webhook com múltiplas entregas registradas

**Passos:**
1. Autenticar com JWT válido
2. Enviar `GET /api/integrations/webhooks/subscriptions/{subscriptionId}/delivery-history`

**Resultado Esperado:**
- Lista de entregas com `deliveryId`, `eventType`, `status`, `attempts`, `lastAttemptAt`
- Entregas com falha mostram `lastError`

**Critério de Aceite:** Histórico completo com status e detalhes de erro para falhas

---

### TC-INT-021 — Listar canais de entrega disponíveis

| Campo | Valor |
|-------|-------|
| **Módulo** | Integrations |
| **Feature** | ListDeliveryChannels |
| **Tipo** | Integração |
| **Prioridade** | Média |

**Pré-condições:**
- Canais Slack, Email e PagerDuty configurados

**Passos:**
1. Autenticar com JWT válido
2. Enviar `GET /api/integrations/delivery-channels`

**Resultado Esperado:**
- Lista de canais com `id`, `type`, `name`, `status`, `lastTestedAt`

**Critério de Aceite:** Listagem completa por tenant; status dos canais atualizado

---

### TC-INT-022 — Criar ou atualizar canal de entrega (UpsertDeliveryChannel)

| Campo | Valor |
|-------|-------|
| **Módulo** | Integrations |
| **Feature** | UpsertDeliveryChannel |
| **Tipo** | Integração |
| **Prioridade** | Alta |

**Pré-condições:**
- Nenhum canal Slack configurado para o tenant

**Passos:**
1. Autenticar com JWT de admin
2. Enviar `PUT /api/integrations/delivery-channels/slack` com body `{ "webhookUrl": "https://hooks.slack.com/...", "channel": "#alerts", "enabled": true }`
3. Repetir para atualizar a URL
4. Verificar idempotência

**Resultado Esperado:**
- Primeira chamada: HTTP 201 Created
- Segunda chamada: HTTP 200 OK (atualização)
- Canal configurado e testável

**Critério de Aceite:** Upsert idempotente; canal disponível para entrega

---

### TC-INT-023 — Webhook entregue com sucesso e confirmado

| Campo | Valor |
|-------|-------|
| **Módulo** | Integrations |
| **Feature** | RegisterWebhookSubscription / GetDeliveryStatus |
| **Tipo** | E2E |
| **Prioridade** | Crítica |

**Pré-condições:**
- Servidor de destino operacional retornando HTTP 200
- Assinatura para evento `deployment.completed` registrada

**Passos:**
1. Registrar assinatura de webhook
2. Disparar evento `deployment.completed` via deploy de serviço
3. Aguardar processamento pelo Outbox
4. Verificar status da entrega

**Resultado Esperado:**
- Evento capturado; HTTP POST enviado ao endpoint destino
- `status: "Delivered"`, `httpStatus: 200`, `deliveredAt` preenchido

**Critério de Aceite:** Entrega confirmada end-to-end via Outbox e callback HTTP

---

### TC-INT-024 — Retry automático de webhook após falha de entrega

| Campo | Valor |
|-------|-------|
| **Módulo** | Integrations |
| **Feature** | GetDeliveryHistory |
| **Tipo** | E2E |
| **Prioridade** | Crítica |

**Pré-condições:**
- Endpoint de destino configurado para retornar HTTP 500 nas 2 primeiras tentativas
- Assinatura ativa para o evento

**Passos:**
1. Disparar evento que aciona o webhook
2. Verificar histórico de entrega após falhas
3. Configurar endpoint para retornar 200
4. Aguardar retry automático

**Resultado Esperado:**
- `attempts: 2` com `status: "Failed"` nas primeiras
- Terceira tentativa bem-sucedida: `status: "Delivered"`
- Política de backoff exponencial aplicada entre tentativas

**Critério de Aceite:** Retry automático funcional com backoff; entrega confirmada na 3ª tentativa

---

### TC-INT-025 — Isolamento de assinaturas de webhook por tenant

| Campo | Valor |
|-------|-------|
| **Módulo** | Integrations |
| **Feature** | ListWebhookSubscriptions |
| **Tipo** | Integração |
| **Prioridade** | Crítica |

**Pré-condições:**
- Tenant A com 3 assinaturas; Tenant B com 2 assinaturas

**Passos:**
1. Autenticar como Tenant A
2. Enviar `GET /api/integrations/webhooks/subscriptions`
3. Autenticar como Tenant B
4. Enviar `GET /api/integrations/webhooks/subscriptions`

**Resultado Esperado:**
- Tenant A vê exatamente 3 assinaturas (suas)
- Tenant B vê exatamente 2 assinaturas (suas)
- Nenhuma sobreposição entre os conjuntos

**Critério de Aceite:** Isolamento RLS completo; `TenantRlsInterceptor` e filtro de repositório operacionais

---

### TC-INT-026 — Conector retorna erro de autenticação (401)

| Campo | Valor |
|-------|-------|
| **Módulo** | Integrations |
| **Feature** | GetIntegrationHealthReport |
| **Tipo** | Integração |
| **Prioridade** | Alta |

**Pré-condições:**
- Token de API do conector GitHub expirado

**Passos:**
1. Configurar conector GitHub com token expirado
2. Aguardar próxima verificação de saúde
3. Enviar `GET /api/integrations/health-report`

**Resultado Esperado:**
- Conector GitHub com `status: "AuthenticationFailed"`, `lastError: "401 Unauthorized"`
- Alerta gerado para o tenant (via canal configurado)

**Critério de Aceite:** Erro de autenticação detectado e reportado; alerta disparado

---

### TC-INT-027 — Lote de custo duplicado detectado e rejeitado

| Campo | Valor |
|-------|-------|
| **Módulo** | Integrations |
| **Feature** | ImportCostBatch |
| **Tipo** | Integração |
| **Prioridade** | Alta |

**Pré-condições:**
- Lote de custo com `batchRef: "aws-may-2026"` já importado com sucesso

**Passos:**
1. Autenticar com JWT adequado
2. Enviar novamente `POST /api/integrations/cost-import/batches` com mesmo `batchRef`

**Resultado Esperado:**
- HTTP 409 Conflict
- Body contém `{ "error": { "code": "DuplicateBatchRef", "existingBatchId": "..." } }`
- Nenhum dado duplicado inserido

**Critério de Aceite:** `result.Error.Type == ErrorType.Conflict`; deduplicação por `batchRef` funcionando
