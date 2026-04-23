# NexTrace Agent — Referência de Parametrização

Este documento é a referência completa de todas as variáveis de ambiente suportadas pelo NexTrace Agent, incluindo defaults por deployment mode, comportamento de TLS e instruções para obtenção da API Key.

---

## 1. Precedência de configuração

```
ENV do sistema operativo
    │  (maior prioridade)
    ▼
Ficheiro agent.env
    │  Linux:   /etc/nexttrace-agent/agent.env
    │  Windows: C:\ProgramData\NexTraceAgent\agent.env
    ▼
Variáveis no config YAML (${VAR} interpolation)
    │
    ▼
Defaults internos do binário
    │  (menor prioridade)
    ▼
```

---

## 2. Referência completa de variáveis

### 2.1 Variáveis de identidade e modo

| Variável | Obrigatório | Default (`saas`) | Default (`self-hosted`) | Descrição |
|---|---|---|---|---|
| `NEXTTRACE_DEPLOYMENT_MODE` | Não | `saas` | `self-hosted` | Modo de deployment. Controla defaults de endpoints, TLS e comportamento OpAMP. Valores aceites: `saas`, `self-hosted`. |
| `NEXTTRACE_API_KEY` | **Sim** | — | — | API Key de autenticação. Injectada como `Authorization: ApiKey <key>` em todos os requests para o endpoint de ingestão. Ver secção 3 para instruções de obtenção. |
| `NEXTTRACE_AGENT_VERSION` | Não | (versão do binário) | (versão do binário) | Sobrescreve o valor de `nextraceone.agent_version` injectado nos spans/métricas/logs. Útil em cenários de teste. |
| `NEXTTRACE_DEPLOYMENT_ID` | Não | `""` | `""` | Identificador lógico do deployment, ex.: `prod-eu-west-1`. Injectado como `nextraceone.deployment_id`. |

### 2.2 Endpoints

| Variável | Obrigatório | Default (`saas`) | Default (`self-hosted`) | Descrição |
|---|---|---|---|---|
| `NEXTTRACE_INGEST_ENDPOINT` | Não (tem default) | `https://ingest.nextraceone.io` | `http://<nextraceone-host>:4319` | URL completa do endpoint de ingestão OTLP. Em self-hosted, deve incluir protocolo, host e porta. |
| `NEXTTRACE_CONTROL_ENDPOINT` | Não (tem default) | `wss://control.nextraceone.io/v1/opamp` | `ws://<nextraceone-host>:4320/v1/opamp` | URL do endpoint OpAMP para configuração remota e reporting de health. |
| `NEXTTRACE_HEALTH_CHECK_PORT` | Não | `13133` | `13133` | Porta local do endpoint de health check (`GET http://localhost:<port>`). |
| `NEXTTRACE_OTLP_GRPC_PORT` | Não | `4317` | `4317` | Porta local onde o agent aceita dados OTLP via gRPC (das apps instrumentadas). |
| `NEXTTRACE_OTLP_HTTP_PORT` | Não | `4318` | `4318` | Porta local onde o agent aceita dados OTLP via HTTP (das apps instrumentadas). |

### 2.3 TLS

| Variável | Obrigatório | Default (`saas`) | Default (`self-hosted`) | Descrição |
|---|---|---|---|---|
| `NEXTTRACE_TLS_SKIP_VERIFY` | Não | `false` | `false` | Desabilita a verificação do certificado TLS do endpoint de ingestão. **Usar apenas em ambientes self-hosted com certificado self-signed.** Não usar em produção SaaS. |
| `NEXTTRACE_TLS_CA_CERT_PATH` | Não | `""` | `""` | Caminho para o ficheiro PEM com o CA certificate customizado. Alternativa mais segura a `NEXTTRACE_TLS_SKIP_VERIFY`. |
| `NEXTTRACE_TLS_CLIENT_CERT_PATH` | Não | `""` | `""` | Caminho para o certificado de cliente (mTLS). |
| `NEXTTRACE_TLS_CLIENT_KEY_PATH` | Não | `""` | `""` | Caminho para a chave privada do certificado de cliente (mTLS). |

### 2.4 Filas e retry

| Variável | Obrigatório | Default | Descrição |
|---|---|---|---|
| `NEXTTRACE_QUEUE_DIR` | Não | Linux: `/var/lib/nexttrace-agent/queue` / Windows: `C:\ProgramData\NexTraceAgent\queue` | Directório da disk-backed queue. Os dados são persistidos aqui quando o endpoint de ingestão está temporariamente inacessível. |
| `NEXTTRACE_QUEUE_SIZE` | Não | `10000` | Número máximo de batches na queue. |
| `NEXTTRACE_QUEUE_CONSUMERS` | Não | `4` | Número de goroutines paralelas a consumir a queue. |
| `NEXTTRACE_RETRY_INITIAL_INTERVAL` | Não | `5s` | Intervalo inicial entre retries (exponential backoff). |
| `NEXTTRACE_RETRY_MAX_INTERVAL` | Não | `30s` | Intervalo máximo entre retries. |
| `NEXTTRACE_RETRY_MAX_ELAPSED_TIME` | Não | `300s` | Tempo máximo total de retry antes de descartar o batch. |

### 2.5 Sampling e processamento

| Variável | Obrigatório | Default | Descrição |
|---|---|---|---|
| `NEXTTRACE_TAIL_SAMPLING_ENABLED` | Não | `true` | Habilita ou desabilita o tail sampling. |
| `NEXTTRACE_TAIL_SAMPLING_DECISION_WAIT` | Não | `10s` | Janela de tempo para decisão de tail sampling. |
| `NEXTTRACE_SAMPLING_PERCENTAGE` | Não | `10` | Percentagem de traces a amostrar (para traces sem erro ou alta latência). |
| `NEXTTRACE_SLOW_TRACE_THRESHOLD_MS` | Não | `2000` | Threshold em ms para considerar um trace "lento" e forçar inclusão no sample. |
| `NEXTTRACE_MEMORY_LIMIT_MIB` | Não | `400` | Limite de memória do agent em MiB. |

### 2.6 Proxy

| Variável | Obrigatório | Default | Descrição |
|---|---|---|---|
| `HTTP_PROXY` | Não | `""` | Proxy HTTP para requests de saída (padrão Go). |
| `HTTPS_PROXY` | Não | `""` | Proxy HTTPS para requests de saída. |
| `NO_PROXY` | Não | `""` | Hosts/CIDRs excluídos do proxy. |

### 2.7 Logging e diagnóstico

| Variável | Obrigatório | Default | Descrição |
|---|---|---|---|
| `NEXTTRACE_LOG_LEVEL` | Não | `info` | Nível de log do agent. Valores: `debug`, `info`, `warn`, `error`. |
| `NEXTTRACE_LOG_FORMAT` | Não | `json` | Formato dos logs do agent. Valores: `json`, `text`. |
| `NEXTTRACE_SELF_TELEMETRY_ENABLED` | Não | `true` | Habilita a ingestão de telemetria do próprio agent (métricas internas). |

---

## 3. Obtenção da API Key

### 3.1 Modo SaaS

1. Aceder ao painel NexTraceOne em `https://app.nextraceone.io`
2. Navegar para **Settings → Integrations → Agent API Keys**
3. Clicar em **New API Key**
4. Dar um nome descritivo (ex.: `prod-k8s-cluster-eu`) e seleccionar o tenant
5. Copiar o valor gerado — começa com `nt_live_`
6. **A chave só é mostrada uma vez.** Guardar num gestor de segredos (Vault, AWS Secrets Manager, Azure Key Vault, etc.)

> As API Keys SaaS têm escopo por tenant. Cada tenant tem as suas próprias chaves. Para revogar, aceder a **Settings → Integrations → Agent API Keys** e clicar em **Revoke**.

### 3.2 Modo Self-Hosted

1. Aceder ao painel NexTraceOne local (ex.: `http://nextraceone.internal:8080`)
2. Autenticar com uma conta com papel **Platform Admin**
3. Navegar para **Administration → Integrations → Agent API Keys**
4. Clicar em **New API Key**
5. Dar um nome descritivo (ex.: `docker-compose-datacenter-1`)
6. Copiar o valor — começa com `nt_local_`
7. Guardar num gestor de segredos ou num ficheiro com permissões restritas

> Em self-hosted, as API Keys são geridas localmente no servidor NexTraceOne. Não têm expiração automática — rotação deve ser feita manualmente ou via processo de rotação automatizado pelo Platform Admin.

---

## 4. Configuração de TLS

### 4.1 SaaS (TLS gerido automaticamente)

Em modo `saas`, o TLS é gerido pela plataforma. Não é necessária nenhuma configuração adicional. Qualquer tentativa de desabilitar TLS em modo SaaS é ignorada.

### 4.2 Self-Hosted com TLS válido

O servidor NexTraceOne deve ter um certificado válido (emitido por CA pública ou CA interna conhecida pelo OS do agente). Nenhuma configuração adicional é necessária além do endpoint:

```dotenv
NEXTTRACE_DEPLOYMENT_MODE=self-hosted
NEXTTRACE_API_KEY=nt_local_xxx
NEXTTRACE_INGEST_ENDPOINT=https://nextraceone.internal:4319
NEXTTRACE_CONTROL_ENDPOINT=wss://nextraceone.internal:4320/v1/opamp
```

### 4.3 Self-Hosted com certificado self-signed

**Opção A — Desabilitar verificação (não recomendado em produção):**

```dotenv
NEXTTRACE_DEPLOYMENT_MODE=self-hosted
NEXTTRACE_API_KEY=nt_local_xxx
NEXTTRACE_INGEST_ENDPOINT=https://nextraceone.internal:4319
NEXTTRACE_TLS_SKIP_VERIFY=true
```

**Opção B — Fornecer o CA certificate (recomendado):**

```dotenv
NEXTTRACE_DEPLOYMENT_MODE=self-hosted
NEXTTRACE_API_KEY=nt_local_xxx
NEXTTRACE_INGEST_ENDPOINT=https://nextraceone.internal:4319
NEXTTRACE_TLS_CA_CERT_PATH=/etc/nexttrace-agent/certs/nextraceone-ca.pem
```

Copiar o CA cert para o agent:
```bash
sudo mkdir -p /etc/nexttrace-agent/certs
sudo cp nextraceone-ca.pem /etc/nexttrace-agent/certs/
sudo chmod 600 /etc/nexttrace-agent/certs/nextraceone-ca.pem
sudo systemctl restart nexttrace-agent
```

### 4.4 mTLS (mutual TLS)

Para ambientes self-hosted com autenticação mútua:

```dotenv
NEXTTRACE_TLS_CA_CERT_PATH=/etc/nexttrace-agent/certs/ca.pem
NEXTTRACE_TLS_CLIENT_CERT_PATH=/etc/nexttrace-agent/certs/agent.crt
NEXTTRACE_TLS_CLIENT_KEY_PATH=/etc/nexttrace-agent/certs/agent.key
```

---

## 5. Validar configuração antes de iniciar

O agent suporta o subcomando `config validate` para verificar se a configuração está correcta antes de iniciar o serviço:

```bash
# Validar config YAML e variáveis de ambiente
nexttrace-agent config validate --config /etc/nexttrace-agent/config.yaml

# Output esperado (configuração válida):
# ✅  NEXTTRACE_API_KEY          — presente
# ✅  NEXTTRACE_INGEST_ENDPOINT  — http://nextraceone.internal:4319 (acessível)
# ✅  NEXTTRACE_CONTROL_ENDPOINT — ws://nextraceone.internal:4320/v1/opamp (acessível)
# ✅  NEXTTRACE_DEPLOYMENT_MODE  — self-hosted
# ✅  Queue directory             — /var/lib/nexttrace-agent/queue (writable)
# ✅  TLS                        — CA cert carregado de /etc/nexttrace-agent/certs/ca.pem
# ⚠️  NEXTTRACE_TLS_SKIP_VERIFY  — false (correcto para produção)
# ✅  Config YAML                — válido

# Testar conectividade com o endpoint de ingestão
nexttrace-agent connectivity test
# Output: Connected to http://nextraceone.internal:4319 — 12ms latency
```

Em caso de erro:

```bash
# Exemplo de output com API Key inválida:
# ❌  NEXTTRACE_API_KEY — resposta 401 Unauthorized do endpoint de ingestão
#     → Verificar que a chave foi copiada correctamente e não expirou
#     → SaaS: regenerar em Settings → Integrations → Agent API Keys
#     → Self-Hosted: regenerar em Administration → Integrations → Agent API Keys
```

---

## 6. Exemplos completos de `agent.env`

### SaaS — produção

```dotenv
# NexTrace Agent — SaaS Production
NEXTTRACE_DEPLOYMENT_MODE=saas
NEXTTRACE_API_KEY=nt_live_xxxxxxxxxxxxxxxxxxxxxxxxxx
NEXTTRACE_DEPLOYMENT_ID=prod-k8s-eu-west-1
NEXTTRACE_LOG_LEVEL=info
```

### SaaS — staging

```dotenv
# NexTrace Agent — SaaS Staging
NEXTTRACE_DEPLOYMENT_MODE=saas
NEXTTRACE_API_KEY=nt_live_yyyyyyyyyyyyyyyyyyyyyyyyyy
NEXTTRACE_DEPLOYMENT_ID=staging-k8s-eu-west-1
NEXTTRACE_SAMPLING_PERCENTAGE=100
NEXTTRACE_LOG_LEVEL=debug
```

### Self-Hosted — produção com TLS válido

```dotenv
# NexTrace Agent — Self-Hosted Production
NEXTTRACE_DEPLOYMENT_MODE=self-hosted
NEXTTRACE_API_KEY=nt_local_xxxxxxxxxxxxxxxxxxxxxxxxxx
NEXTTRACE_INGEST_ENDPOINT=https://nextraceone.acme.internal:4319
NEXTTRACE_CONTROL_ENDPOINT=wss://nextraceone.acme.internal:4320/v1/opamp
NEXTTRACE_DEPLOYMENT_ID=prod-datacenter-lisbon
NEXTTRACE_TLS_CA_CERT_PATH=/etc/nexttrace-agent/certs/acme-ca.pem
NEXTTRACE_LOG_LEVEL=info
```

### Self-Hosted — Docker Compose (rede interna)

```dotenv
# NexTrace Agent — Self-Hosted Docker Compose
NEXTTRACE_DEPLOYMENT_MODE=self-hosted
NEXTTRACE_API_KEY=nt_local_xxxxxxxxxxxxxxxxxxxxxxxxxx
NEXTTRACE_INGEST_ENDPOINT=http://nextraceone-api:4319
NEXTTRACE_CONTROL_ENDPOINT=ws://nextraceone-api:4320/v1/opamp
NEXTTRACE_DEPLOYMENT_ID=local-compose
NEXTTRACE_LOG_LEVEL=info
# HTTP dentro da rede interna do compose — TLS não necessário
```

### Self-Hosted — Windows IIS (on-prem corporativo)

```dotenv
# NexTrace Agent — Self-Hosted Windows IIS On-Prem
NEXTTRACE_DEPLOYMENT_MODE=self-hosted
NEXTTRACE_API_KEY=nt_local_xxxxxxxxxxxxxxxxxxxxxxxxxx
NEXTTRACE_INGEST_ENDPOINT=https://nextraceone.corp.local:4319
NEXTTRACE_CONTROL_ENDPOINT=wss://nextraceone.corp.local:4320/v1/opamp
NEXTTRACE_TLS_CA_CERT_PATH=C:\ProgramData\NexTraceAgent\certs\corp-ca.pem
NEXTTRACE_DEPLOYMENT_ID=prod-iis-datacenter-porto
NEXTTRACE_LOG_LEVEL=info
```

---

## 7. Resumo rápido por cenário

| Cenário | `DEPLOYMENT_MODE` | `INGEST_ENDPOINT` | `TLS_SKIP_VERIFY` |
|---|---|---|---|
| SaaS Cloud | `saas` | (default automático) | `false` |
| Self-Hosted K8s mesmo cluster | `self-hosted` | `http://<service>.<ns>.svc.cluster.local:4319` | `false` |
| Self-Hosted Docker Compose | `self-hosted` | `http://<service-name>:4319` | `false` |
| Self-Hosted VM / Bare-metal (TLS válido) | `self-hosted` | `https://<host>:4319` | `false` |
| Self-Hosted VM (cert self-signed, temporário) | `self-hosted` | `https://<host>:4319` | `true` |
| Self-Hosted Windows IIS (corp PKI) | `self-hosted` | `https://<host>:4319` | `false` + `TLS_CA_CERT_PATH` |

---

*Ver também: [`NEXTTRACE-AGENT.md`](../NEXTTRACE-AGENT.md), [`NEXTTRACE-AGENT-INSTALLER.md`](./NEXTTRACE-AGENT-INSTALLER.md), [`../SAAS-STRATEGY.md`](../SAAS-STRATEGY.md), [`../onprem/WAVE-01-INSTALLATION.md`](../onprem/WAVE-01-INSTALLATION.md)*
