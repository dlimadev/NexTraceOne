# Cenários de Teste — Módulo: Configuration

> **Versão:** 1.0  
> **Data:** 2026-05-18  
> **Responsável:** QA Engineering  
> **Módulo:** Configuration  
> **Total de casos:** 27

---

## Sumário

| ID | Título | Prioridade |
|----|--------|-----------|
| TC-CFG-001 | Definir valor de configuração com sucesso | Crítica |
| TC-CFG-002 | Obter configuração efetiva com hierarquia tenant > environment > default | Crítica |
| TC-CFG-003 | Definir valor de configuração com chave inválida | Alta |
| TC-CFG-004 | Sobrescrever configuração de ambiente com valor de tenant | Alta |
| TC-CFG-005 | Obter configuração quando nenhum override existe (fallback ao default) | Alta |
| TC-CFG-006 | Obter saúde de configuração (GetConfigHealth) | Alta |
| TC-CFG-007 | Alternar configuração ativa/inativa (ToggleConfiguration) | Alta |
| TC-CFG-008 | Listar definições de configuração (GetDefinitions) | Média |
| TC-CFG-009 | Obter resumo de conformidade de parâmetros | Alta |
| TC-CFG-010 | Obter relatório de uso de parâmetros | Média |
| TC-CFG-011 | Obter relatório de drift de configuração | Alta |
| TC-CFG-012 | Obter relatório de comparação de comportamento entre ambientes | Alta |
| TC-CFG-013 | Obter feature flag efetiva sem override | Crítica |
| TC-CFG-014 | Definir override de feature flag para tenant específico | Crítica |
| TC-CFG-015 | Remover override de feature flag | Alta |
| TC-CFG-016 | Registrar estado de feature flag (RecordFeatureFlagState) | Média |
| TC-CFG-017 | Obter histórico de auditoria por prefixo | Alta |
| TC-CFG-018 | Obter histórico de auditoria completo paginado | Alta |
| TC-CFG-019 | Listar migrações pendentes (GetPendingMigrations) | Média |
| TC-CFG-020 | Acesso de leitura bloqueado para usuário sem permissão | Crítica |
| TC-CFG-021 | Acesso de escrita bloqueado para usuário sem role admin | Crítica |
| TC-CFG-022 | Definir valor de configuração com tipo incompatível | Alta |
| TC-CFG-023 | Drift detectado quando valor diverge do esperado | Alta |
| TC-CFG-024 | Override de feature flag em nível de serviço específico | Alta |
| TC-CFG-025 | Configuração efetiva retorna dado correto para ambiente de staging | Alta |
| TC-CFG-026 | Conformidade de parâmetros falha quando valor obrigatório ausente | Alta |
| TC-CFG-027 | Histórico de auditoria registra operações de escrita com usuário e timestamp | Crítica |

---

### TC-CFG-001 — Definir valor de configuração com sucesso

| Campo | Valor |
|-------|-------|
| **Módulo** | Configuration |
| **Feature** | SetConfigurationValue |
| **Tipo** | Integração |
| **Prioridade** | Crítica |

**Pré-condições:**
- Tenant autenticado com role `config:write`
- Chave `app.timeout.seconds` definida no schema de definições
- Ambiente `production` cadastrado

**Passos:**
1. Autenticar com JWT válido contendo claim de tenant e capability `configuration_management`
2. Enviar `POST /api/configuration/values` com body `{ "key": "app.timeout.seconds", "value": "30", "environment": "production" }`
3. Verificar resposta HTTP

**Resultado Esperado:**
- HTTP 201 Created
- Body contém `{ "key": "app.timeout.seconds", "environment": "production", "value": "30" }`
- Entrada criada no histórico de auditoria

**Critério de Aceite:** `result.IsSuccess == true` / HTTP 201

---

### TC-CFG-002 — Obter configuração efetiva com hierarquia tenant > environment > default

| Campo | Valor |
|-------|-------|
| **Módulo** | Configuration |
| **Feature** | GetEffectiveSettings |
| **Tipo** | Integração |
| **Prioridade** | Crítica |

**Pré-condições:**
- Valor default definido para a chave `feature.limit` = `100`
- Valor de ambiente `production` definido para a chave = `200`
- Valor de tenant definido para a mesma chave = `50`

**Passos:**
1. Autenticar com JWT do tenant que possui override específico
2. Enviar `GET /api/configuration/effective?key=feature.limit&environment=production`
3. Verificar a resolução hierárquica

**Resultado Esperado:**
- HTTP 200 OK
- Valor retornado = `50` (override de tenant prevalece sobre ambiente e default)
- Campo `source` indica `"tenant"` na resposta

**Critério de Aceite:** `result.Value.Source == "tenant"` e `result.Value.ResolvedValue == "50"`

---

### TC-CFG-003 — Definir valor de configuração com chave inválida

| Campo | Valor |
|-------|-------|
| **Módulo** | Configuration |
| **Feature** | SetConfigurationValue |
| **Tipo** | Unitário |
| **Prioridade** | Alta |

**Pré-condições:**
- Usuário autenticado com permissão de escrita
- Chave `chave.inexistente.xyz` não cadastrada no schema de definições

**Passos:**
1. Autenticar com JWT válido
2. Enviar `POST /api/configuration/values` com body `{ "key": "chave.inexistente.xyz", "value": "qualquer" }`
3. Verificar resposta de erro

**Resultado Esperado:**
- HTTP 422 Unprocessable Entity
- Body contém `{ "error": { "code": "ConfigKeyNotDefined", "type": "Validation" } }`
- Nenhuma entrada criada no banco de dados

**Critério de Aceite:** `result.IsSuccess == false` e `result.Error.Type == ErrorType.Validation`

---

### TC-CFG-004 — Sobrescrever configuração de ambiente com valor de tenant

| Campo | Valor |
|-------|-------|
| **Módulo** | Configuration |
| **Feature** | SetConfigurationValue |
| **Tipo** | Integração |
| **Prioridade** | Alta |

**Pré-condições:**
- Valor de ambiente `staging` para chave `log.level` = `"info"`
- Tenant autenticado sem override prévio para essa chave

**Passos:**
1. Autenticar como tenant específico com permissão de escrita
2. Enviar `POST /api/configuration/values` com `{ "key": "log.level", "value": "debug", "scope": "tenant" }`
3. Consultar configuração efetiva com `GET /api/configuration/effective?key=log.level&environment=staging`
4. Consultar como outro tenant (sem override)

**Resultado Esperado:**
- Primeira chamada retorna HTTP 201
- Segunda chamada retorna `value: "debug"` com `source: "tenant"`
- Quarta chamada retorna `value: "info"` com `source: "environment"` (isolamento correto)

**Critério de Aceite:** Override de tenant isolado corretamente por `TenantId`

---

### TC-CFG-005 — Obter configuração quando nenhum override existe (fallback ao default)

| Campo | Valor |
|-------|-------|
| **Módulo** | Configuration |
| **Feature** | GetEffectiveSettings |
| **Tipo** | Integração |
| **Prioridade** | Alta |

**Pré-condições:**
- Valor default definido para a chave `pagination.pageSize` = `25`
- Nenhum override de ambiente ou tenant cadastrado

**Passos:**
1. Autenticar com JWT de tenant sem overrides
2. Enviar `GET /api/configuration/effective?key=pagination.pageSize`

**Resultado Esperado:**
- HTTP 200 OK
- `value: "25"`, `source: "default"`

**Critério de Aceite:** `result.Value.Source == "default"`

---

### TC-CFG-006 — Obter saúde de configuração (GetConfigHealth)

| Campo | Valor |
|-------|-------|
| **Módulo** | Configuration |
| **Feature** | GetConfigHealth |
| **Tipo** | Integração |
| **Prioridade** | Alta |

**Pré-condições:**
- Módulo de configuração inicializado com definições carregadas

**Passos:**
1. Autenticar com JWT válido
2. Enviar `GET /api/configuration/health`

**Resultado Esperado:**
- HTTP 200 OK
- Body contém `{ "status": "Healthy", "definitionsLoaded": true, "totalKeys": N }`

**Critério de Aceite:** `status == "Healthy"` e `definitionsLoaded == true`

---

### TC-CFG-007 — Alternar configuração ativa/inativa (ToggleConfiguration)

| Campo | Valor |
|-------|-------|
| **Módulo** | Configuration |
| **Feature** | ToggleConfiguration |
| **Tipo** | Integração |
| **Prioridade** | Alta |

**Pré-condições:**
- Configuração `maintenance.mode` com valor `false` ativa
- Usuário com role `config:admin`

**Passos:**
1. Autenticar com JWT de admin
2. Enviar `PATCH /api/configuration/toggle` com body `{ "key": "maintenance.mode", "enabled": true }`
3. Consultar configuração efetiva
4. Enviar novamente para reverter: `{ "key": "maintenance.mode", "enabled": false }`
5. Verificar reversão

**Resultado Esperado:**
- Após passo 2: HTTP 200, configuração ativa
- Após passo 3: `value: "true"`
- Após passo 4: HTTP 200, `value: "false"`

**Critério de Aceite:** Toggle idempotente e auditado corretamente

---

### TC-CFG-008 — Listar definições de configuração (GetDefinitions)

| Campo | Valor |
|-------|-------|
| **Módulo** | Configuration |
| **Feature** | GetDefinitions |
| **Tipo** | Integração |
| **Prioridade** | Média |

**Pré-condições:**
- Schema de definições carregado com pelo menos 10 chaves

**Passos:**
1. Autenticar com JWT válido
2. Enviar `GET /api/configuration/definitions`

**Resultado Esperado:**
- HTTP 200 OK
- Array com pelo menos 10 objetos de definição
- Cada objeto contém `key`, `type`, `defaultValue`, `description`, `required`

**Critério de Aceite:** Lista não vazia e estrutura de cada definição completa

---

### TC-CFG-009 — Obter resumo de conformidade de parâmetros (GetParameterComplianceSummary)

| Campo | Valor |
|-------|-------|
| **Módulo** | Configuration |
| **Feature** | GetParameterComplianceSummary |
| **Tipo** | Integração |
| **Prioridade** | Alta |

**Pré-condições:**
- Pelo menos 2 parâmetros obrigatórios definidos
- Um parâmetro obrigatório sem valor definido no ambiente atual

**Passos:**
1. Autenticar com JWT válido
2. Enviar `GET /api/configuration/compliance/summary?environment=production`

**Resultado Esperado:**
- HTTP 200 OK
- Body contém `totalRequired`, `compliant`, `nonCompliant`, lista de parâmetros não conformes
- Parâmetro sem valor aparece em `nonCompliant`

**Critério de Aceite:** `nonCompliant > 0` refletindo o parâmetro ausente

---

### TC-CFG-010 — Obter relatório de uso de parâmetros (GetParameterUsageReport)

| Campo | Valor |
|-------|-------|
| **Módulo** | Configuration |
| **Feature** | GetParameterUsageReport |
| **Tipo** | Integração |
| **Prioridade** | Média |

**Pré-condições:**
- Histórico de leituras de configuração registrado nos últimos 30 dias

**Passos:**
1. Autenticar com JWT válido com role de leitura de relatórios
2. Enviar `GET /api/configuration/reports/parameter-usage?from=2026-04-01&to=2026-05-18`

**Resultado Esperado:**
- HTTP 200 OK
- Lista de parâmetros com contagem de acessos, último acesso e módulo consumidor
- Parâmetros nunca utilizados identificados com `accessCount: 0`

**Critério de Aceite:** Relatório gerado sem erros com período solicitado coberto

---

### TC-CFG-011 — Obter relatório de drift de configuração (GetConfigurationDriftReport)

| Campo | Valor |
|-------|-------|
| **Módulo** | Configuration |
| **Feature** | GetConfigurationDriftReport |
| **Tipo** | Integração |
| **Prioridade** | Alta |

**Pré-condições:**
- Valor esperado (baseline) definido para chave `security.tls.version` = `"1.3"`
- Valor atual em produção = `"1.2"` (divergente)

**Passos:**
1. Autenticar com JWT de admin
2. Enviar `GET /api/configuration/reports/drift?environment=production`

**Resultado Esperado:**
- HTTP 200 OK
- Chave `security.tls.version` aparece no relatório como drift detectado
- Campos `expected`, `actual`, `environment`, `detectedAt` preenchidos

**Critério de Aceite:** `driftItems.Count > 0` e chave divergente presente

---

### TC-CFG-012 — Obter relatório de comparação de comportamento entre ambientes

| Campo | Valor |
|-------|-------|
| **Módulo** | Configuration |
| **Feature** | GetEnvironmentBehaviorComparisonReport |
| **Tipo** | Integração |
| **Prioridade** | Alta |

**Pré-condições:**
- Ambientes `staging` e `production` com valores divergentes em pelo menos 3 chaves

**Passos:**
1. Autenticar com JWT válido
2. Enviar `GET /api/configuration/reports/environment-comparison?env1=staging&env2=production`

**Resultado Esperado:**
- HTTP 200 OK
- Lista de divergências com chave, valor em env1 e valor em env2
- Chaves idênticas marcadas como `"match": true`

**Critério de Aceite:** Pelo menos 3 divergências identificadas corretamente

---

### TC-CFG-013 — Obter feature flag efetiva sem override

| Campo | Valor |
|-------|-------|
| **Módulo** | Configuration |
| **Feature** | GetEffectiveFeatureFlag |
| **Tipo** | Integração |
| **Prioridade** | Crítica |

**Pré-condições:**
- Feature flag `new-dashboard-ui` definida com valor padrão `false`
- Tenant sem override específico para essa flag

**Passos:**
1. Autenticar com JWT de tenant sem override
2. Enviar `GET /api/configuration/feature-flags/effective?flag=new-dashboard-ui&serviceId=dashboard-svc`

**Resultado Esperado:**
- HTTP 200 OK
- `{ "flag": "new-dashboard-ui", "enabled": false, "source": "default" }`

**Critério de Aceite:** `enabled == false` e `source == "default"`

---

### TC-CFG-014 — Definir override de feature flag para tenant específico

| Campo | Valor |
|-------|-------|
| **Módulo** | Configuration |
| **Feature** | SetFeatureFlagOverride |
| **Tipo** | Integração |
| **Prioridade** | Crítica |

**Pré-condições:**
- Feature flag `new-dashboard-ui` com valor padrão `false`
- Tenant `tenant-beta` sem override
- Registro persistido via `IFeatureFlagRepository` (tabela `ctr_feature_flag_records`)

**Passos:**
1. Autenticar como `tenant-beta` com role `config:write`
2. Enviar `POST /api/configuration/feature-flags/override` com body `{ "flag": "new-dashboard-ui", "serviceId": "dashboard-svc", "enabled": true }`
3. Consultar flag efetiva como `tenant-beta`
4. Consultar flag efetiva como outro tenant sem override

**Resultado Esperado:**
- Passo 2: HTTP 201
- Passo 3: `enabled: true`, `source: "tenant"`
- Passo 4: `enabled: false`, `source: "default"` (isolamento de tenant correto)

**Critério de Aceite:** Override isolado por `TenantId`; registro persistido no banco com chave única `(TenantId, ServiceId, FlagKey)`

---

### TC-CFG-015 — Remover override de feature flag

| Campo | Valor |
|-------|-------|
| **Módulo** | Configuration |
| **Feature** | RemoveFeatureFlagOverride |
| **Tipo** | Integração |
| **Prioridade** | Alta |

**Pré-condições:**
- Override de feature flag `dark-mode` habilitado para tenant atual com `serviceId=ui-svc`

**Passos:**
1. Autenticar com JWT do tenant com override ativo
2. Enviar `DELETE /api/configuration/feature-flags/override?flag=dark-mode&serviceId=ui-svc`
3. Consultar flag efetiva

**Resultado Esperado:**
- HTTP 204 No Content
- Consulta posterior retorna `source: "default"` com valor padrão da flag

**Critério de Aceite:** Override removido; fallback ao default funcionando

---

### TC-CFG-016 — Registrar estado de feature flag (RecordFeatureFlagState)

| Campo | Valor |
|-------|-------|
| **Módulo** | Configuration |
| **Feature** | RecordFeatureFlagState |
| **Tipo** | Integração |
| **Prioridade** | Média |

**Pré-condições:**
- Feature flag `canary-release` em uso pelo serviço `deploy-svc`

**Passos:**
1. Autenticar com JWT do serviço com permissão de telemetria
2. Enviar `POST /api/configuration/feature-flags/state` com body `{ "flag": "canary-release", "serviceId": "deploy-svc", "state": true, "timestamp": "2026-05-18T10:00:00Z" }`

**Resultado Esperado:**
- HTTP 202 Accepted
- Estado registrado para análise de adoção de feature flags

**Critério de Aceite:** HTTP 202 e estado persistido corretamente

---

### TC-CFG-017 — Obter histórico de auditoria por prefixo

| Campo | Valor |
|-------|-------|
| **Módulo** | Configuration |
| **Feature** | GetAuditHistoryByPrefix |
| **Tipo** | Integração |
| **Prioridade** | Alta |

**Pré-condições:**
- Múltiplas alterações realizadas em chaves com prefixo `security.`

**Passos:**
1. Autenticar com JWT de admin com role `config:audit`
2. Enviar `GET /api/configuration/audit/history?prefix=security.`

**Resultado Esperado:**
- HTTP 200 OK
- Apenas entradas com chaves prefixadas por `security.`
- Cada entrada contém `key`, `oldValue`, `newValue`, `changedBy`, `changedAt`

**Critério de Aceite:** Todas as entradas retornadas possuem prefixo `security.`

---

### TC-CFG-018 — Obter histórico de auditoria completo paginado

| Campo | Valor |
|-------|-------|
| **Módulo** | Configuration |
| **Feature** | GetAuditHistory |
| **Tipo** | Integração |
| **Prioridade** | Alta |

**Pré-condições:**
- Histórico com pelo menos 20 eventos de alteração

**Passos:**
1. Autenticar com JWT de admin
2. Enviar `GET /api/configuration/audit/history?page=1&pageSize=10`
3. Enviar `GET /api/configuration/audit/history?page=2&pageSize=10`

**Resultado Esperado:**
- HTTP 200 OK em ambas as chamadas
- Paginação com `totalCount >= 20`, página 1 com 10 itens distintos da página 2
- Ordenação por `changedAt` decrescente

**Critério de Aceite:** Paginação funcional, sem duplicatas entre páginas, ordenação correta

---

### TC-CFG-019 — Listar migrações pendentes (GetPendingMigrations)

| Campo | Valor |
|-------|-------|
| **Módulo** | Configuration |
| **Feature** | GetPendingMigrations |
| **Tipo** | Integração |
| **Prioridade** | Média |

**Pré-condições:**
- Ambiente de staging com uma migração não aplicada

**Passos:**
1. Autenticar com JWT de plataforma (admin de infra)
2. Enviar `GET /api/configuration/migrations/pending`

**Resultado Esperado:**
- HTTP 200 OK
- Lista de migrações pendentes com nome, contexto e timestamp de criação
- Migração não aplicada presente na lista

**Critério de Aceite:** Migração não aplicada aparece na lista com dados completos

---

### TC-CFG-020 — Acesso de leitura bloqueado para usuário sem permissão

| Campo | Valor |
|-------|-------|
| **Módulo** | Configuration |
| **Feature** | GetEffectiveSettings |
| **Tipo** | Integração |
| **Prioridade** | Crítica |

**Pré-condições:**
- Usuário autenticado sem capability `configuration_management`

**Passos:**
1. Autenticar com JWT sem a capability necessária
2. Enviar `GET /api/configuration/effective?key=app.timeout.seconds`

**Resultado Esperado:**
- HTTP 403 Forbidden
- Body `{ "error": { "code": "CapabilityRequired" } }`
- Nenhum dado retornado

**Critério de Aceite:** `result.Error.Type == ErrorType.Forbidden`

---

### TC-CFG-021 — Acesso de escrita bloqueado para usuário sem role admin

| Campo | Valor |
|-------|-------|
| **Módulo** | Configuration |
| **Feature** | SetConfigurationValue |
| **Tipo** | Integração |
| **Prioridade** | Crítica |

**Pré-condições:**
- Usuário autenticado com role `config:read` apenas (sem `config:write`)

**Passos:**
1. Autenticar com JWT com role somente leitura
2. Enviar `POST /api/configuration/values` com payload válido

**Resultado Esperado:**
- HTTP 403 Forbidden
- Nenhuma alteração realizada no banco

**Critério de Aceite:** Escrita bloqueada para roles insuficientes; erro de autorização retornado

---

### TC-CFG-022 — Definir valor de configuração com tipo incompatível

| Campo | Valor |
|-------|-------|
| **Módulo** | Configuration |
| **Feature** | SetConfigurationValue |
| **Tipo** | Unitário |
| **Prioridade** | Alta |

**Pré-condições:**
- Chave `server.port` definida como tipo `integer` no schema
- Usuário com permissão de escrita

**Passos:**
1. Autenticar com JWT válido com permissão de escrita
2. Enviar `POST /api/configuration/values` com body `{ "key": "server.port", "value": "nao-e-numero" }`

**Resultado Esperado:**
- HTTP 422 Unprocessable Entity
- Mensagem de erro indicando tipo inválido: `code: "ValueTypeMismatch"`
- Nenhum valor salvo

**Critério de Aceite:** `result.Error.Code == "ValueTypeMismatch"`

---

### TC-CFG-023 — Drift detectado quando valor diverge do esperado

| Campo | Valor |
|-------|-------|
| **Módulo** | Configuration |
| **Feature** | GetConfigurationDriftReport |
| **Tipo** | Integração |
| **Prioridade** | Alta |

**Pré-condições:**
- Baseline de produção estabelecido com `cache.ttl` = `3600`
- Valor atual alterado manualmente para `1800`

**Passos:**
1. Autenticar com JWT de admin
2. Executar `GET /api/configuration/reports/drift?environment=production`
3. Verificar se `cache.ttl` aparece como drift

**Resultado Esperado:**
- Chave `cache.ttl` listada com `expected: "3600"` e `actual: "1800"`
- Severidade calculada com base na variação
- Campo `detectedAt` preenchido com timestamp da detecção

**Critério de Aceite:** Drift identificado e reportado com valores corretos

---

### TC-CFG-024 — Override de feature flag em nível de serviço específico

| Campo | Valor |
|-------|-------|
| **Módulo** | Configuration |
| **Feature** | SetFeatureFlagOverride |
| **Tipo** | Integração |
| **Prioridade** | Alta |

**Pré-condições:**
- Feature flag `experimental-alerts` com default `false`
- Dois serviços: `alert-svc` e `report-svc` para o mesmo tenant

**Passos:**
1. Definir override apenas para `serviceId=alert-svc`: `{ "flag": "experimental-alerts", "serviceId": "alert-svc", "enabled": true }`
2. Consultar flag efetiva para `alert-svc`
3. Consultar flag efetiva para `report-svc`

**Resultado Esperado:**
- `alert-svc`: `enabled: true`, `source: "service-override"`
- `report-svc`: `enabled: false`, `source: "default"`

**Critério de Aceite:** Isolamento por serviço respeitado via chave única `(TenantId, ServiceId, FlagKey)` na tabela `ctr_feature_flag_records`

---

### TC-CFG-025 — Configuração efetiva retorna dado correto para ambiente de staging

| Campo | Valor |
|-------|-------|
| **Módulo** | Configuration |
| **Feature** | GetEffectiveSettings |
| **Tipo** | Integração |
| **Prioridade** | Alta |

**Pré-condições:**
- `log.retention.days` com valor default `30`, override de ambiente staging = `7`
- Tenant sem override próprio para essa chave

**Passos:**
1. Autenticar com JWT sem override de tenant para essa chave
2. Enviar `GET /api/configuration/effective?key=log.retention.days&environment=staging`

**Resultado Esperado:**
- `value: "7"`, `source: "environment"`

**Critério de Aceite:** Override de ambiente prevalece sobre default quando não há override de tenant

---

### TC-CFG-026 — Conformidade de parâmetros falha quando valor obrigatório ausente

| Campo | Valor |
|-------|-------|
| **Módulo** | Configuration |
| **Feature** | GetParameterComplianceSummary |
| **Tipo** | Integração |
| **Prioridade** | Alta |

**Pré-condições:**
- Parâmetro `smtp.host` marcado como obrigatório no schema
- Nenhum valor definido para `smtp.host` no ambiente `production`

**Passos:**
1. Autenticar com JWT de admin
2. Enviar `GET /api/configuration/compliance/summary?environment=production`

**Resultado Esperado:**
- `nonCompliant` inclui `smtp.host` com motivo `"RequiredValueMissing"`
- Score de conformidade < 100%
- Campo `compliancePercentage` calculado corretamente

**Critério de Aceite:** Parâmetro obrigatório ausente refletido no resumo de conformidade

---

### TC-CFG-027 — Histórico de auditoria registra operações de escrita com usuário e timestamp

| Campo | Valor |
|-------|-------|
| **Módulo** | Configuration |
| **Feature** | GetAuditHistory |
| **Tipo** | Integração |
| **Prioridade** | Crítica |

**Pré-condições:**
- Usuário `admin@nextraceone.io` identificado no JWT
- Chave `api.rate.limit` com valor atual `100`

**Passos:**
1. Realizar `POST /api/configuration/values` como `admin@nextraceone.io` alterando `api.rate.limit` para `200`
2. Consultar `GET /api/configuration/audit/history?key=api.rate.limit`

**Resultado Esperado:**
- Entrada de auditoria contém `changedBy: "admin@nextraceone.io"`
- `oldValue: "100"`, `newValue: "200"`
- `changedAt` com timestamp ISO 8601 correto
- `AuditInterceptor` populou os campos automaticamente via `ICurrentUser`

**Critério de Aceite:** Trilha de auditoria completa e imutável para todas as operações de escrita
