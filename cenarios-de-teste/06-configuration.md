# Cenários de Teste — Módulo: Configuration

**Versão:** 1.0  
**Data:** 2026-05-18  
**Responsável:** QA Engineering  
**Módulo:** Configuration  

---

## Visão Geral

Este documento cobre os cenários de teste funcionais do módulo **Configuration**, responsável por gerenciar valores de configuração hierárquicos (tenant > ambiente > padrão), feature flags, histórico de auditoria, compliance de parâmetros e detecção de drift de configuração.

---

### TC-CFG-001 — Definir valor de configuração no nível tenant

| Campo | Valor |
|-------|-------|
| **Módulo** | Configuration |
| **Feature** | SetConfigurationValue |
| **Tipo** | Integração |
| **Prioridade** | Crítica |

**Pré-condições:**
- Tenant autenticado com plano Professional ou superior
- Usuário com role `config:write`
- Chave de configuração válida conforme definições registradas

**Passos:**
1. Autenticar como administrador do tenant `tenant-alpha`
2. Enviar `POST /configuration/values` com payload `{ "key": "notifications.batchSize", "value": "200", "scope": "tenant" }`
3. Verificar resposta HTTP 200/201
4. Consultar o valor via `GET /configuration/settings/effective?key=notifications.batchSize`

**Resultado Esperado:**
- Valor `"200"` retornado para `notifications.batchSize` no escopo tenant
- Campo `source` na resposta igual a `"tenant"`
- `result.IsSuccess == true`

**Critério de Aceite:** `result.IsSuccess == true` / HTTP 200

---

### TC-CFG-002 — Hierarquia de resolução: tenant sobrepõe ambiente que sobrepõe padrão

| Campo | Valor |
|-------|-------|
| **Módulo** | Configuration |
| **Feature** | GetEffectiveSettings |
| **Tipo** | Integração |
| **Prioridade** | Crítica |

**Pré-condições:**
- Valor padrão definido globalmente: `notifications.batchSize = 50`
- Valor de ambiente definido para `production`: `notifications.batchSize = 100`
- Valor de tenant definido para `tenant-alpha`: `notifications.batchSize = 200`

**Passos:**
1. Autenticar como usuário do `tenant-alpha` no ambiente `production`
2. Consultar `GET /configuration/settings/effective?key=notifications.batchSize`
3. Verificar campo `value` e `source` na resposta

**Resultado Esperado:**
- `value = "200"` (nível tenant prevalece)
- `source = "tenant"`
- Sem acesso ao valor de outro tenant

**Critério de Aceite:** `result.IsSuccess == true` / HTTP 200

---

### TC-CFG-003 — Resolução por nível de ambiente quando tenant não define o valor

| Campo | Valor |
|-------|-------|
| **Módulo** | Configuration |
| **Feature** | GetEffectiveSettings |
| **Tipo** | Integração |
| **Prioridade** | Alta |

**Pré-condições:**
- Valor padrão global: `logs.level = "info"`
- Valor de ambiente `staging`: `logs.level = "debug"`
- Tenant `tenant-beta` sem override para esta chave

**Passos:**
1. Autenticar como usuário do `tenant-beta` no ambiente `staging`
2. Consultar `GET /configuration/settings/effective?key=logs.level`

**Resultado Esperado:**
- `value = "debug"`
- `source = "environment"`

**Critério de Aceite:** `result.IsSuccess == true` / HTTP 200

---

### TC-CFG-004 — Resolução por valor padrão quando nenhum override existe

| Campo | Valor |
|-------|-------|
| **Módulo** | Configuration |
| **Feature** | GetEffectiveSettings |
| **Tipo** | Integração |
| **Prioridade** | Alta |

**Pré-condições:**
- Valor padrão global: `retry.maxAttempts = 3`
- Nenhum override de ambiente ou tenant configurado

**Passos:**
1. Autenticar como qualquer usuário autenticado
2. Consultar `GET /configuration/settings/effective?key=retry.maxAttempts`

**Resultado Esperado:**
- `value = "3"`
- `source = "default"`

**Critério de Aceite:** `result.IsSuccess == true` / HTTP 200

---

### TC-CFG-005 — Rejeitar chave de configuração inválida/desconhecida

| Campo | Valor |
|-------|-------|
| **Módulo** | Configuration |
| **Feature** | SetConfigurationValue |
| **Tipo** | Unitário |
| **Prioridade** | Alta |

**Pré-condições:**
- Usuário com role `config:write`
- Chave `xyz.unknownKey` não registrada nas definições

**Passos:**
1. Enviar `POST /configuration/values` com `{ "key": "xyz.unknownKey", "value": "test" }`

**Resultado Esperado:**
- HTTP 422 Unprocessable Entity
- Mensagem de erro indicando que a chave não está definida no catálogo de configurações
- `result.IsSuccess == false`

**Critério de Aceite:** HTTP 422 / `ErrorType.Validation`

---

### TC-CFG-006 — Verificar saúde do serviço de configuração

| Campo | Valor |
|-------|-------|
| **Módulo** | Configuration |
| **Feature** | GetConfigHealth |
| **Tipo** | Integração |
| **Prioridade** | Média |

**Pré-condições:**
- Serviço Configuration em execução
- Banco de dados acessível

**Passos:**
1. Enviar `GET /configuration/health`
2. Verificar campos retornados: `status`, `latencyMs`, `lastChecked`

**Resultado Esperado:**
- `status = "healthy"`
- `latencyMs < 200`
- HTTP 200

**Critério de Aceite:** `result.IsSuccess == true` / HTTP 200

---

### TC-CFG-007 — Ativar e desativar toggle de configuração

| Campo | Valor |
|-------|-------|
| **Módulo** | Configuration |
| **Feature** | ToggleConfiguration |
| **Tipo** | Integração |
| **Prioridade** | Alta |

**Pré-condições:**
- Configuração booleana `maintenance.mode` existente com valor `false`
- Usuário com role `config:write`

**Passos:**
1. Enviar `POST /configuration/toggle` com `{ "key": "maintenance.mode", "enabled": true }`
2. Consultar `GET /configuration/settings/effective?key=maintenance.mode`
3. Enviar `POST /configuration/toggle` com `{ "key": "maintenance.mode", "enabled": false }`
4. Consultar novamente o valor

**Resultado Esperado:**
- Passo 2: `value = "true"`
- Passo 4: `value = "false"`
- Ambas as operações retornam HTTP 200

**Critério de Aceite:** `result.IsSuccess == true` / HTTP 200

---

### TC-CFG-008 — Listar definições de configuração disponíveis

| Campo | Valor |
|-------|-------|
| **Módulo** | Configuration |
| **Feature** | GetDefinitions |
| **Tipo** | Integração |
| **Prioridade** | Média |

**Pré-condições:**
- Definições registradas no catálogo do sistema

**Passos:**
1. Enviar `GET /configuration/definitions`
2. Verificar estrutura da resposta

**Resultado Esperado:**
- Lista com ao menos uma definição
- Cada item contém `key`, `type`, `defaultValue`, `description`, `scopes`
- HTTP 200

**Critério de Aceite:** `result.IsSuccess == true` / HTTP 200

---

### TC-CFG-009 — Usuário sem permissão de escrita não pode definir configuração

| Campo | Valor |
|-------|-------|
| **Módulo** | Configuration |
| **Feature** | SetConfigurationValue |
| **Tipo** | Integração |
| **Prioridade** | Crítica |

**Pré-condições:**
- Usuário autenticado com role somente leitura (`config:read`)
- Sem role `config:write`

**Passos:**
1. Tentar `POST /configuration/values` com payload válido

**Resultado Esperado:**
- HTTP 403 Forbidden
- Mensagem: "Permissão insuficiente para escrita de configuração"

**Critério de Aceite:** HTTP 403 / `ErrorType.Forbidden`

---

### TC-CFG-010 — Obter feature flag efetiva para contexto específico

| Campo | Valor |
|-------|-------|
| **Módulo** | Configuration |
| **Feature** | GetEffectiveFeatureFlag |
| **Tipo** | Integração |
| **Prioridade** | Crítica |

**Pré-condições:**
- Feature flag `new-dashboard-ui` registrada com valor padrão `false`
- Override configurado para `tenant-alpha`: `true`

**Passos:**
1. Autenticar como usuário do `tenant-alpha`
2. Consultar `GET /configuration/feature-flags/new-dashboard-ui/effective`

**Resultado Esperado:**
- `enabled = true`
- `source = "tenant-override"`
- HTTP 200

**Critério de Aceite:** `result.IsSuccess == true` / HTTP 200

---

### TC-CFG-011 — Definir override de feature flag por tenant

| Campo | Valor |
|-------|-------|
| **Módulo** | Configuration |
| **Feature** | SetFeatureFlagOverride |
| **Tipo** | Integração |
| **Prioridade** | Alta |

**Pré-condições:**
- Feature flag `beta-ai-assist` existente
- Usuário com role `config:write`

**Passos:**
1. Enviar `POST /configuration/feature-flags/beta-ai-assist/override` com `{ "enabled": true, "serviceId": "svc-catalog" }`
2. Consultar a flag efetiva para o contexto `svc-catalog`

**Resultado Esperado:**
- Override salvo com sucesso
- Consulta retorna `enabled = true` com `source = "override"`

**Critério de Aceite:** `result.IsSuccess == true` / HTTP 200

---

### TC-CFG-012 — Remover override de feature flag

| Campo | Valor |
|-------|-------|
| **Módulo** | Configuration |
| **Feature** | RemoveFeatureFlagOverride |
| **Tipo** | Integração |
| **Prioridade** | Alta |

**Pré-condições:**
- Override da feature flag `beta-ai-assist` existente para `tenant-alpha`

**Passos:**
1. Enviar `DELETE /configuration/feature-flags/beta-ai-assist/override`
2. Consultar a flag efetiva

**Resultado Esperado:**
- Override removido com sucesso
- Flag retorna ao valor padrão ou de ambiente
- HTTP 200

**Critério de Aceite:** `result.IsSuccess == true` / HTTP 200

---

### TC-CFG-013 — Registrar estado de feature flag (auditoria)

| Campo | Valor |
|-------|-------|
| **Módulo** | Configuration |
| **Feature** | RecordFeatureFlagState |
| **Tipo** | Integração |
| **Prioridade** | Média |

**Pré-condições:**
- Feature flag `experimental-routing` com estado conhecido

**Passos:**
1. Enviar `POST /configuration/feature-flags/experimental-routing/record-state` com contexto de execução
2. Consultar histórico de auditoria da flag

**Resultado Esperado:**
- Estado registrado com timestamp, usuário e valor atual
- Aparece no histórico de auditoria
- HTTP 200/201

**Critério de Aceite:** `result.IsSuccess == true` / HTTP 201

---

### TC-CFG-014 — Consultar histórico de auditoria por prefixo de chave

| Campo | Valor |
|-------|-------|
| **Módulo** | Configuration |
| **Feature** | GetAuditHistoryByPrefix |
| **Tipo** | Integração |
| **Prioridade** | Alta |

**Pré-condições:**
- Múltiplas alterações realizadas em chaves com prefixo `notifications.*`

**Passos:**
1. Consultar `GET /configuration/audit?prefix=notifications`
2. Verificar lista retornada

**Resultado Esperado:**
- Apenas entradas com chaves iniciando em `notifications.`
- Cada entrada contém `key`, `oldValue`, `newValue`, `changedBy`, `changedAt`
- HTTP 200

**Critério de Aceite:** `result.IsSuccess == true` / HTTP 200

---

### TC-CFG-015 — Consultar histórico de auditoria completo

| Campo | Valor |
|-------|-------|
| **Módulo** | Configuration |
| **Feature** | GetAuditHistory |
| **Tipo** | Integração |
| **Prioridade** | Média |

**Pré-condições:**
- Histórico de alterações existente

**Passos:**
1. Consultar `GET /configuration/audit` com parâmetros de paginação `page=1&pageSize=20`
2. Verificar estrutura da resposta

**Resultado Esperado:**
- Lista paginada de alterações de configuração
- `totalCount` correto
- HTTP 200

**Critério de Aceite:** `result.IsSuccess == true` / HTTP 200

---

### TC-CFG-016 — Relatório de conformidade de parâmetros

| Campo | Valor |
|-------|-------|
| **Módulo** | Configuration |
| **Feature** | GetParameterComplianceSummary |
| **Tipo** | Integração |
| **Prioridade** | Alta |

**Pré-condições:**
- Parâmetros obrigatórios definidos no catálogo
- Alguns parâmetros ausentes ou fora dos valores permitidos

**Passos:**
1. Enviar `GET /configuration/compliance/summary`
2. Verificar campos `compliant`, `nonCompliant`, `missing`, `complianceRate`

**Resultado Esperado:**
- Resumo quantitativo de conformidade
- Lista de parâmetros não conformes identificados
- HTTP 200

**Critério de Aceite:** `result.IsSuccess == true` / HTTP 200

---

### TC-CFG-017 — Relatório de uso de parâmetros

| Campo | Valor |
|-------|-------|
| **Módulo** | Configuration |
| **Feature** | GetParameterUsageReport |
| **Tipo** | Integração |
| **Prioridade** | Média |

**Pré-condições:**
- Parâmetros de configuração lidos por serviços durante pelo menos 24h

**Passos:**
1. Enviar `GET /configuration/reports/parameter-usage?period=7d`

**Resultado Esperado:**
- Lista de parâmetros com contagem de leituras
- Parâmetros nunca lidos identificados como potencialmente obsoletos
- HTTP 200

**Critério de Aceite:** `result.IsSuccess == true` / HTTP 200

---

### TC-CFG-018 — Detectar drift de configuração entre ambientes

| Campo | Valor |
|-------|-------|
| **Módulo** | Configuration |
| **Feature** | GetConfigurationDriftReport |
| **Tipo** | Integração |
| **Prioridade** | Alta |

**Pré-condições:**
- Ambientes `staging` e `production` com valores divergentes em chaves críticas
- Ex.: `cache.ttl = 300` em staging vs `cache.ttl = 600` em production

**Passos:**
1. Enviar `GET /configuration/reports/drift?baseEnvironment=production&compareEnvironment=staging`
2. Verificar lista de divergências

**Resultado Esperado:**
- Lista de chaves com valores diferentes entre os ambientes
- Cada divergência mostra `key`, `baseValue`, `compareValue`, `severity`
- HTTP 200

**Critério de Aceite:** `result.IsSuccess == true` / HTTP 200

---

### TC-CFG-019 — Relatório de comparação de comportamento entre ambientes

| Campo | Valor |
|-------|-------|
| **Módulo** | Configuration |
| **Feature** | GetEnvironmentBehaviorComparisonReport |
| **Tipo** | Integração |
| **Prioridade** | Média |

**Pré-condições:**
- Múltiplos ambientes configurados (development, staging, production)
- Feature flags com estados diferentes por ambiente

**Passos:**
1. Enviar `GET /configuration/reports/environment-behavior-comparison`

**Resultado Esperado:**
- Matriz comparativa de comportamentos por ambiente
- Feature flags e configurações críticas destacadas
- HTTP 200

**Critério de Aceite:** `result.IsSuccess == true` / HTTP 200

---

### TC-CFG-020 — Listagem de migrações pendentes de configuração

| Campo | Valor |
|-------|-------|
| **Módulo** | Configuration |
| **Feature** | GetPendingMigrations |
| **Tipo** | Integração |
| **Prioridade** | Alta |

**Pré-condições:**
- Nova versão do módulo com migrações de schema não aplicadas

**Passos:**
1. Enviar `GET /configuration/migrations/pending`

**Resultado Esperado:**
- Lista de migrações identificadas por nome e data de criação
- HTTP 200 (lista pode ser vazia se não houver pendentes)

**Critério de Aceite:** `result.IsSuccess == true` / HTTP 200

---

### TC-CFG-021 — Isolamento por tenant: tenant-B não acessa configurações do tenant-A

| Campo | Valor |
|-------|-------|
| **Módulo** | Configuration |
| **Feature** | GetEffectiveSettings |
| **Tipo** | Integração |
| **Prioridade** | Crítica |

**Pré-condições:**
- `tenant-alpha` com override `feature.limit = 100`
- `tenant-beta` sem override para `feature.limit`
- Valor padrão global: `feature.limit = 10`

**Passos:**
1. Autenticar como usuário do `tenant-beta`
2. Consultar `GET /configuration/settings/effective?key=feature.limit`

**Resultado Esperado:**
- Retorna `value = "10"` (padrão global), nunca `"100"` do tenant-alpha
- `source = "default"`

**Critério de Aceite:** `result.IsSuccess == true` / HTTP 200, sem cross-tenant leak

---

### TC-CFG-022 — Validação de tipo de valor na definição de configuração

| Campo | Valor |
|-------|-------|
| **Módulo** | Configuration |
| **Feature** | SetConfigurationValue |
| **Tipo** | Unitário |
| **Prioridade** | Alta |

**Pré-condições:**
- Chave `retry.maxAttempts` definida com tipo `integer`
- Usuário com role `config:write`

**Passos:**
1. Enviar `POST /configuration/values` com `{ "key": "retry.maxAttempts", "value": "abc" }`

**Resultado Esperado:**
- HTTP 422 Unprocessable Entity
- Mensagem indicando que o valor deve ser um inteiro

**Critério de Aceite:** HTTP 422 / `ErrorType.Validation`

---

### TC-CFG-023 — Valor de configuração dentro de intervalo permitido

| Campo | Valor |
|-------|-------|
| **Módulo** | Configuration |
| **Feature** | SetConfigurationValue |
| **Tipo** | Unitário |
| **Prioridade** | Alta |

**Pré-condições:**
- Chave `cache.ttl` definida com tipo `integer`, min=60, max=3600

**Passos:**
1. Enviar `POST /configuration/values` com `{ "key": "cache.ttl", "value": "10" }` (abaixo do mínimo)

**Resultado Esperado:**
- HTTP 422 Unprocessable Entity
- Mensagem indicando violação de intervalo permitido

**Critério de Aceite:** HTTP 422 / `ErrorType.Validation`

---

### TC-CFG-024 — Feature flag com override por serviço específico (multi-dimensão)

| Campo | Valor |
|-------|-------|
| **Módulo** | Configuration |
| **Feature** | SetFeatureFlagOverride / GetEffectiveFeatureFlag |
| **Tipo** | Integração |
| **Prioridade** | Alta |

**Pré-condições:**
- Feature flag `dark-mode` ativada globalmente para o tenant
- Override específico para `serviceId = "svc-reports"`: desativada

**Passos:**
1. Consultar flag efetiva para `serviceId = "svc-catalog"`
2. Consultar flag efetiva para `serviceId = "svc-reports"`

**Resultado Esperado:**
- Passo 1: `enabled = true`
- Passo 2: `enabled = false` (override de serviço prevalece)

**Critério de Aceite:** `result.IsSuccess == true` / HTTP 200

---

### TC-CFG-025 — Auditoria registra autor e timestamp da alteração

| Campo | Valor |
|-------|-------|
| **Módulo** | Configuration |
| **Feature** | SetConfigurationValue / GetAuditHistory |
| **Tipo** | Integração |
| **Prioridade** | Alta |

**Pré-condições:**
- Usuário `admin@tenant-alpha.com` autenticado

**Passos:**
1. Definir valor `POST /configuration/values` com `{ "key": "logs.level", "value": "warn" }`
2. Consultar `GET /configuration/audit?key=logs.level`

**Resultado Esperado:**
- Entrada de auditoria com `changedBy = "admin@tenant-alpha.com"`
- `changedAt` dentro dos últimos 5 segundos
- `newValue = "warn"`

**Critério de Aceite:** `result.IsSuccess == true` / HTTP 200

---

### TC-CFG-026 — Configuração somente-leitura não pode ser alterada via API

| Campo | Valor |
|-------|-------|
| **Módulo** | Configuration |
| **Feature** | SetConfigurationValue |
| **Tipo** | Integração |
| **Prioridade** | Alta |

**Pré-condições:**
- Chave `system.version` marcada como `readOnly = true` nas definições

**Passos:**
1. Usuário admin tenta `POST /configuration/values` com `{ "key": "system.version", "value": "9.9.9" }`

**Resultado Esperado:**
- HTTP 422 ou 403
- Mensagem indicando que a chave é somente-leitura

**Critério de Aceite:** HTTP 422 / `ErrorType.Validation`

---

### TC-CFG-027 — Relatório de drift identifica chave crítica ausente em produção

| Campo | Valor |
|-------|-------|
| **Módulo** | Configuration |
| **Feature** | GetConfigurationDriftReport |
| **Tipo** | Integração |
| **Prioridade** | Alta |

**Pré-condições:**
- Chave `security.enforceHttps` definida em `staging` com `true`
- Chave ausente em `production`

**Passos:**
1. Enviar `GET /configuration/reports/drift?baseEnvironment=staging&compareEnvironment=production`

**Resultado Esperado:**
- `security.enforceHttps` aparece como chave ausente em `production`
- Severity marcada como `critical`

**Critério de Aceite:** `result.IsSuccess == true` / HTTP 200

---

### TC-CFG-028 — Usuário sem autenticação não acessa endpoints de configuração

| Campo | Valor |
|-------|-------|
| **Módulo** | Configuration |
| **Feature** | GetEffectiveSettings |
| **Tipo** | Integração |
| **Prioridade** | Crítica |

**Pré-condições:**
- Nenhum token JWT na requisição

**Passos:**
1. Enviar `GET /configuration/settings/effective?key=logs.level` sem header Authorization

**Resultado Esperado:**
- HTTP 401 Unauthorized
- Corpo da resposta indica ausência de autenticação

**Critério de Aceite:** HTTP 401

---
