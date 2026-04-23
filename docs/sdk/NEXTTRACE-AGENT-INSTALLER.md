# NexTrace Agent — Instalador

Este documento descreve como instalar e configurar o NexTrace Agent em cada plataforma suportada, para os dois modelos de deployment: **SaaS** e **Self-Hosted/On-Prem**.

> O binário do agent é idêntico nos dois modelos. A distinção é feita exclusivamente pelas variáveis de ambiente fornecidas ao instalador.

---

## Pré-requisitos por plataforma

| Plataforma | Requisito |
|---|---|
| Linux (amd64 / arm64) | glibc ≥ 2.17; systemd; acesso root ou sudo |
| Windows Server 2019+ | PowerShell 5.1+; acesso Administrador; IIS instalado (para CLR Profiler) |
| Kubernetes | kubectl com acesso ao cluster; Helm 3.x |
| Docker Compose | Docker Engine ≥ 24; Docker Compose v2 |

---

## 1. Linux — `install.sh`

### Argumentos suportados

| Argumento | Obrigatório | Descrição |
|---|---|---|
| `--mode` | Não | `saas` (default) ou `self-hosted` |
| `--api-key <key>` | **Sim** | API Key do tenant (SaaS) ou do servidor local (Self-Hosted) |
| `--ingest-endpoint <url>` | Só em self-hosted | URL do endpoint de ingestão, ex.: `http://nextraceone.internal:4319` |
| `--control-endpoint <url>` | Não | URL do endpoint OpAMP; omitido = default do modo |
| `--version <tag>` | Não | Versão do agent, ex.: `v1.2.0`; omitido = latest |
| `--config-only` | Não | Só cria/actualiza `/etc/nexttrace-agent/agent.env`; não instala nem reinicia o serviço |

### Instalação SaaS (Linux)

```bash
# Instalação SaaS — uma linha
curl -fsSL https://get.nextraceone.io/agent/install.sh | sudo bash -s -- \
  --mode saas \
  --api-key nt_live_xxxxxxxxxxxxxxxxxxxxxxxxxx
```

O instalador executa os seguintes passos:

1. Descarrega o binário `nexttrace-agent-linux-amd64` para `/usr/local/bin/nexttrace-agent`
2. Cria o directório `/etc/nexttrace-agent/` e o ficheiro `/etc/nexttrace-agent/agent.env` com as variáveis fornecidas
3. Cria o directório de queue persistente `/var/lib/nexttrace-agent/queue`
4. Instala a systemd unit `/etc/systemd/system/nexttrace-agent.service`
5. Habilita e inicia o serviço
6. Aguarda 10 segundos e verifica o health check em `http://localhost:13133`

### Instalação Self-Hosted (Linux)

```bash
curl -fsSL https://get.nextraceone.io/agent/install.sh | sudo bash -s -- \
  --mode self-hosted \
  --api-key nt_local_xxxxxxxxxxxxxxxxxxxxxxxxxx \
  --ingest-endpoint http://nextraceone.internal:4319 \
  --control-endpoint ws://nextraceone.internal:4320/v1/opamp
```

### Actualizar apenas a configuração (sem reinstalar)

```bash
sudo nexttrace-agent-install.sh \
  --config-only \
  --api-key nt_local_yyyyyyyyyyyyyyyyyyyyyyy \
  --ingest-endpoint http://nextraceone-new.internal:4319

sudo systemctl restart nexttrace-agent
```

### Ficheiro `agent.env` resultante (self-hosted)

```dotenv
# Gerado automaticamente pelo install.sh — não editar manualmente
# Para alterações use: nexttrace-agent-install.sh --config-only
NEXTTRACE_DEPLOYMENT_MODE=self-hosted
NEXTTRACE_API_KEY=nt_local_xxxxxxxxxxxxxxxxxxxxxxxxxx
NEXTTRACE_INGEST_ENDPOINT=http://nextraceone.internal:4319
NEXTTRACE_CONTROL_ENDPOINT=ws://nextraceone.internal:4320/v1/opamp
```

### Systemd unit

```ini
# /etc/systemd/system/nexttrace-agent.service
[Unit]
Description=NexTrace Agent — OTel Collector Distribution
After=network-online.target
Wants=network-online.target

[Service]
Type=simple
EnvironmentFile=/etc/nexttrace-agent/agent.env
ExecStart=/usr/local/bin/nexttrace-agent --config /etc/nexttrace-agent/config.yaml
Restart=on-failure
RestartSec=5
LimitNOFILE=65536
StandardOutput=journal
StandardError=journal
SyslogIdentifier=nexttrace-agent
User=nexttrace-agent
Group=nexttrace-agent

[Install]
WantedBy=multi-user.target
```

### Comandos de gestão

```bash
# Estado
sudo systemctl status nexttrace-agent

# Logs em tempo real
sudo journalctl -u nexttrace-agent -f

# Reiniciar após alterar agent.env
sudo systemctl restart nexttrace-agent

# Desinstalar
sudo systemctl stop nexttrace-agent
sudo systemctl disable nexttrace-agent
sudo rm /usr/local/bin/nexttrace-agent \
        /etc/systemd/system/nexttrace-agent.service \
        /etc/nexttrace-agent/agent.env
sudo systemctl daemon-reload
```

---

## 2. Windows / IIS — `install.ps1`

### Parâmetros suportados

| Parâmetro | Obrigatório | Descrição |
|---|---|---|
| `-Mode` | Não | `Saas` (default) ou `SelfHosted` |
| `-ApiKey <string>` | **Sim** | API Key |
| `-IngestEndpoint <string>` | Só em SelfHosted | URL do endpoint de ingestão |
| `-ControlEndpoint <string>` | Não | URL OpAMP; omitido = default do modo |
| `-AppPoolName <string>` | Não | Nome do Application Pool IIS; `*` = todos (default) |
| `-Version <string>` | Não | Versão do agent; omitido = latest |
| `-ConfigOnly` | Não | Switch; só actualiza `agent.env`; não reinstala nem reinicia o serviço |

### Instalação SaaS (Windows)

```powershell
# Executar como Administrador
Set-ExecutionPolicy Bypass -Scope Process -Force
iex (irm https://get.nextraceone.io/agent/install.ps1) -ApiKey "nt_live_xxxxxxxxxxxxxxxxxxxxxxxxxx"
```

### Instalação Self-Hosted (Windows)

```powershell
# Executar como Administrador
Set-ExecutionPolicy Bypass -Scope Process -Force

& "C:\NexTraceAgentInstall\install.ps1" `
    -Mode SelfHosted `
    -ApiKey "nt_local_xxxxxxxxxxxxxxxxxxxxxxxxxx" `
    -IngestEndpoint "http://nextraceone.internal:4319" `
    -ControlEndpoint "ws://nextraceone.internal:4320/v1/opamp"
```

### Fluxo do instalador Windows

1. Descarrega `nexttrace-agent-windows-amd64.exe` para `C:\Program Files\NexTraceAgent\`
2. Cria `C:\ProgramData\NexTraceAgent\agent.env` com as variáveis fornecidas
3. Instala o OTel .NET Auto-Instrumentation (CLR Profiler) em `C:\Program Files\NexTraceAgent\otel-dotnet-auto\`
4. Configura as variáveis de ambiente do CLR Profiler nos Application Pools IIS especificados (usa `Set-WebConfigurationProperty`)
5. Instala o Windows Service `NexTraceAgent` via `sc.exe`
6. Inicia o serviço e aguarda health check em `http://localhost:13133`

### Snippet de configuração do CLR Profiler (parte do install.ps1)

```powershell
param(
    [string]$Mode = "Saas",
    [string]$ApiKey,
    [string]$IngestEndpoint = $(if ($Mode -eq "SelfHosted") { $null } else { "https://ingest.nextraceone.io" }),
    [string]$ControlEndpoint,
    [string]$AppPoolName = "*",
    [string]$Version = "latest",
    [switch]$ConfigOnly
)

$agentDir    = "C:\Program Files\NexTraceAgent"
$profilerDll = "$agentDir\otel-dotnet-auto\OpenTelemetry.AutoInstrumentation.Native.dll"
$managedPath = "$agentDir\otel-dotnet-auto"
$envFile     = "C:\ProgramData\NexTraceAgent\agent.env"

# Escrever agent.env
@"
NEXTTRACE_DEPLOYMENT_MODE=$(if ($Mode -eq 'SelfHosted') { 'self-hosted' } else { 'saas' })
NEXTTRACE_API_KEY=$ApiKey
NEXTTRACE_INGEST_ENDPOINT=$IngestEndpoint
$(if ($ControlEndpoint) { "NEXTTRACE_CONTROL_ENDPOINT=$ControlEndpoint" })
"@ | Set-Content -Path $envFile -Encoding UTF8

if ($ConfigOnly) { Write-Host "Config actualizado. Reinicie o serviço NexTraceAgent."; exit 0 }

# Configurar CLR Profiler nos App Pools IIS
$appPools = if ($AppPoolName -eq "*") {
    Get-WebConfiguration "system.applicationHost/applicationPools/add" | Select-Object -ExpandProperty name
} else { @($AppPoolName) }

foreach ($pool in $appPools) {
    $vars = @{
        CORECLR_ENABLE_PROFILING    = "1"
        CORECLR_PROFILER            = "{918728DD-259F-4A6A-AC2B-B85E1B658318}"
        CORECLR_PROFILER_PATH       = $profilerDll
        OTEL_DOTNET_AUTO_HOME       = $managedPath
        OTEL_EXPORTER_OTLP_ENDPOINT = "http://localhost:4318"  # Aponta sempre para o agent local
        OTEL_RESOURCE_ATTRIBUTES    = "nextraceone.api_key=$ApiKey"
    }
    foreach ($key in $vars.Keys) {
        Set-WebConfigurationProperty `
            -Filter "system.applicationHost/applicationPools/add[@name='$pool']/environmentVariables" `
            -Name "." `
            -Value @{ name = $key; value = $vars[$key] }
    }
    Restart-WebAppPool -Name $pool
    Write-Host "NexTrace Agent activado no App Pool: $pool"
}
```

### Comandos de gestão (Windows)

```powershell
# Estado do serviço
Get-Service NexTraceAgent

# Logs (Event Viewer)
Get-EventLog -LogName Application -Source "NexTraceAgent" -Newest 50

# Reiniciar
Restart-Service NexTraceAgent

# Desinstalar
Stop-Service NexTraceAgent
sc.exe delete NexTraceAgent
Remove-Item -Recurse "C:\Program Files\NexTraceAgent"
Remove-Item -Recurse "C:\ProgramData\NexTraceAgent"
```

---

## 3. Kubernetes — Helm Chart

### Instalação SaaS (Kubernetes)

```bash
helm repo add nextraceone https://charts.nextraceone.io
helm repo update

helm upgrade --install nexttrace-agent nextraceone/nexttrace-agent \
  --namespace nexttrace-system \
  --create-namespace \
  --set deploymentMode=saas \
  --set apiKey=nt_live_xxxxxxxxxxxxxxxxxxxxxxxxxx
```

### Instalação Self-Hosted (Kubernetes)

Quando o NexTraceOne corre no mesmo cluster, o endpoint de ingestão é o Service interno:

```bash
helm upgrade --install nexttrace-agent nextraceone/nexttrace-agent \
  --namespace nexttrace-system \
  --create-namespace \
  --values values-selfhosted.yaml
```

**`values-selfhosted.yaml`:**

```yaml
deploymentMode: "self-hosted"

# API Key gerada no painel do NexTraceOne local
# Recomendado: usar um Secret Kubernetes em vez de valor directo
apiKey: ""
existingSecret:
  name: nexttrace-credentials
  key: api-key

# Endpoint do NexTraceOne no mesmo cluster (Service interno)
ingestEndpoint: "http://nextraceone-api.nextraceone-system.svc.cluster.local:4319"
controlEndpoint: "ws://nextraceone-api.nextraceone-system.svc.cluster.local:4320/v1/opamp"

# TLS
tls:
  skipVerify: false         # true se usar certificado self-signed
  caCertSecret: ""          # nome do Secret com o CA cert customizado, se aplicável

# Recursos
resources:
  requests:
    cpu: "100m"
    memory: "128Mi"
  limits:
    cpu: "500m"
    memory: "512Mi"
```

**`values.yaml` (defaults — SaaS):**

```yaml
deploymentMode: "saas"
apiKey: ""
existingSecret:
  name: nexttrace-credentials
  key: api-key

# SaaS: endpoints geridos pela NexTraceOne — não alterar
ingestEndpoint: "https://ingest.nextraceone.io"
controlEndpoint: "wss://control.nextraceone.io/v1/opamp"

tls:
  skipVerify: false
  caCertSecret: ""

resources:
  requests:
    cpu: "100m"
    memory: "128Mi"
  limits:
    cpu: "500m"
    memory: "512Mi"

# DaemonSet corre em todos os nós por omissão
tolerations: []
nodeSelector: {}
```

### Secret Kubernetes para a API Key

```yaml
apiVersion: v1
kind: Secret
metadata:
  name: nexttrace-credentials
  namespace: nexttrace-system
type: Opaque
stringData:
  api-key: "nt_local_xxxxxxxxxxxxxxxxxxxxxxxxxx"
```

---

## 4. Docker Compose — Self-Hosted

Para ambientes self-hosted com Docker Compose, o agent corre como serviço na mesma rede interna do stack NexTraceOne. O snippet abaixo deve ser adicionado ao `docker-compose.yml` principal do NexTraceOne.

```yaml
# docker-compose.yml — adicionar ao stack NexTraceOne self-hosted
services:
  nexttrace-agent:
    image: nextraceone/nexttrace-agent:latest
    restart: unless-stopped
    env_file:
      - ./nexttrace-agent.env
    environment:
      NEXTTRACE_DEPLOYMENT_MODE: "self-hosted"
      NEXTTRACE_INGEST_ENDPOINT: "http://nextraceone-api:4319"
      NEXTTRACE_CONTROL_ENDPOINT: "ws://nextraceone-api:4320/v1/opamp"
    volumes:
      - /var/log:/var/log:ro
      - /var/run/docker.sock:/var/run/docker.sock:ro
      - nexttrace-agent-queue:/var/lib/nexttrace-agent/queue
    ports:
      - "4317:4317"    # OTLP gRPC — apps instrumentadas enviam para cá
      - "4318:4318"    # OTLP HTTP
      - "13133:13133"  # Health check
    networks:
      - nextraceone-internal
    depends_on:
      nextraceone-api:
        condition: service_healthy
    healthcheck:
      test: ["CMD", "wget", "--quiet", "--tries=1", "--spider", "http://localhost:13133"]
      interval: 30s
      timeout: 5s
      retries: 3

volumes:
  nexttrace-agent-queue:

networks:
  nextraceone-internal:
    external: true
```

**`nexttrace-agent.env` (Docker Compose self-hosted):**

```dotenv
# Ficheiro de parametrização do NexTrace Agent — self-hosted
NEXTTRACE_DEPLOYMENT_MODE=self-hosted
NEXTTRACE_API_KEY=nt_local_xxxxxxxxxxxxxxxxxxxxxxxxxx
# Os endpoints abaixo são sobrescritos pelo environment: no compose,
# mas podem ser definidos aqui para uso noutros contextos.
NEXTTRACE_INGEST_ENDPOINT=http://nextraceone-api:4319
NEXTTRACE_CONTROL_ENDPOINT=ws://nextraceone-api:4320/v1/opamp
# Descomente se o servidor NexTraceOne usar certificado self-signed:
# NEXTTRACE_TLS_SKIP_VERIFY=true
```

### Verificar que o agent está saudável (Docker Compose)

```bash
# Health check
curl -s http://localhost:13133 | jq .

# Logs
docker compose logs nexttrace-agent --follow

# Reiniciar
docker compose restart nexttrace-agent
```

---

## 5. Verificação pós-instalação

Independentemente da plataforma, executar após a instalação:

```bash
# Verificar health check (Linux / Docker)
curl -s http://localhost:13133

# Validar configuração
nexttrace-agent config validate --config /etc/nexttrace-agent/config.yaml

# Teste de conectividade com o endpoint de ingestão
nexttrace-agent connectivity test
```

Output esperado de um agent saudável:

```json
{
  "status": "Server available",
  "uptime": "2m15s",
  "version": "v1.2.0",
  "deployment_mode": "self-hosted",
  "ingest_endpoint": "http://nextraceone.internal:4319",
  "queue_size": 0,
  "pipelines": {
    "traces": "running",
    "metrics": "running",
    "logs": "running"
  }
}
```

---

## 6. Fluxo de dados por deployment mode

### SaaS

```
Apps instrumentadas (OTLP)
    │
    ▼
NexTrace Agent (localhost:4317/4318)
    │ tail_sampling + redaction + batch
    │ nextraceprocessor (enrichment)
    ▼
https://ingest.nextraceone.io           ← endpoint gerido pela NexTraceOne
    │
    ▼
NexTraceOne SaaS Platform
```

### Self-Hosted / On-Prem

```
Apps instrumentadas (OTLP)
    │
    ▼
NexTrace Agent (localhost:4317/4318)
    │ tail_sampling + redaction + batch
    │ nextraceprocessor (enrichment)
    ▼
http://nextraceone.internal:4319        ← endpoint interno do cliente
    │
    ▼
NexTraceOne Self-Hosted Server
```

---

*Ver também: [`NEXTTRACE-AGENT.md`](../NEXTTRACE-AGENT.md), [`NEXTTRACE-AGENT-PARAMETRIZATION.md`](./NEXTTRACE-AGENT-PARAMETRIZATION.md), [`../onprem/WAVE-01-INSTALLATION.md`](../onprem/WAVE-01-INSTALLATION.md), [`../DEPLOYMENT-ARCHITECTURE.md`](../DEPLOYMENT-ARCHITECTURE.md)*
