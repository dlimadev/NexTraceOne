# IIS + CLR Profiler — Coleta de Telemetria

> **Módulo:** Observability › Collection  
> **Modo de coleta:** `ClrProfiler`  
> **Ambiente alvo:** IIS / Windows Server  
> **Estratégia:** `ClrProfilerStrategy` (`ICollectionModeStrategy`)

---

## Índice

1. [Objetivo](#objetivo)
2. [Quando usar](#quando-usar)
3. [Quando não usar](#quando-não-usar)
4. [Pré-requisitos](#pré-requisitos)
5. [Arquitetura resumida](#arquitetura-resumida)
6. [Instalação](#instalação)
7. [Configuração](#configuração)
8. [Ativação no IIS](#ativação-no-iis)
9. [Coleta de traces](#coleta-de-traces)
10. [Coleta de logs IIS](#coleta-de-logs-iis)
11. [Validação](#validação)
12. [Limitações](#limitações)
13. [Troubleshooting](#troubleshooting)

---

## Objetivo

Coletar traces, métricas e logs de aplicações .NET hospedadas em IIS/Windows Server
**sem modificar o código da aplicação**. O CLR Profiler realiza auto-instrumentação
ao nível do runtime .NET, capturando automaticamente chamadas HTTP, acesso a base de
dados, invocações externas e exceções não tratadas.

Este modo é implementado pela classe `ClrProfilerStrategy`, registada como
`ICollectionModeStrategy` quando `ActiveMode` é `"ClrProfiler"` na secção
`Telemetry:CollectionMode` da configuração.

---

## Quando usar

| Cenário | Recomendação |
|---------|--------------|
| Aplicações .NET hospedadas em IIS | ✅ Recomendado |
| Windows Server sem contentor | ✅ Recomendado |
| Ambientes onde a intrusão no código deve ser mínima | ✅ Recomendado |
| Aplicações .NET Framework 4.6.2+ | ✅ Suportado |
| Aplicações .NET 6/7/8+ em IIS | ✅ Suportado |
| Modo `SelfHosted` (Kestrel / Windows Service) | ✅ Suportado (configurar `Mode: "SelfHosted"`) |

---

## Quando não usar

| Cenário | Alternativa |
|---------|-------------|
| Ambientes Kubernetes / contentor | Usar **OpenTelemetry Collector** (`ActiveMode: "OpenTelemetryCollector"`) |
| Aplicações não-.NET (Java, Python, Go, Node.js) | Usar OTel SDK nativo da linguagem + Collector |
| Ambientes Linux sem IIS | Usar OTel Collector com auto-instrumentação OTLP |
| Cenários com requisitos rígidos de overhead zero | Avaliar instrumentação manual seletiva |

---

## Pré-requisitos

1. **Sistema operativo:** Windows Server 2016 ou superior
2. **Runtime .NET:** .NET Framework 4.6.2+ **ou** .NET 6+
3. **IIS:** Instalado e em execução (com Application Pool configurado)
4. **Agente de auto-instrumentação .NET:** OpenTelemetry .NET Auto-Instrumentation Agent instalado
5. **Rede:** Conectividade entre o servidor IIS e o OTel Collector (porta 4317 gRPC ou 4318 HTTP)
6. **Permissões:** Acesso administrativo para configurar variáveis de ambiente no Application Pool

---

## Arquitetura resumida

```
┌─────────────────────────────────────────────────────┐
│  Windows Server / IIS                               │
│                                                     │
│  ┌─────────────────┐    ┌──────────────────────┐   │
│  │  .NET App (IIS)  │    │  CLR Profiler Agent   │   │
│  │  w3wp.exe        │◄───│  Auto-Instrumentation │   │
│  └────────┬─────────┘    └──────────┬───────────┘   │
│           │                         │                │
│           │  traces, logs, métricas │                │
│           └─────────┬───────────────┘                │
│                     │ OTLP (gRPC/HTTP)               │
└─────────────────────┼───────────────────────────────┘
                      │
                      ▼
        ┌─────────────────────────┐
        │  OTel Collector         │
        │  (porta 4317 gRPC)      │
        │  memory_limiter → batch │
        │  → redaction → export   │
        └────────────┬────────────┘
                     │
                     ▼
        ┌─────────────────────────┐
        │  ClickHouse / Elastic   │
        │  (Provider configurado) │
        └─────────────────────────┘
```

**Fluxo:**

1. O CLR Profiler injeta-se no runtime .NET através das APIs de profiling do CLR
2. Captura automaticamente operações (HTTP, SQL, chamadas externas, exceções)
3. Exporta os sinais via OTLP para o OTel Collector (ou diretamente para o provider)
4. O Collector processa (normalização, redação de PII, sampling) e exporta para o provider

---

## Instalação

### 1. Instalar o agente de auto-instrumentação .NET

```powershell
# Descarregar a versão mais recente do agente
$agentUrl = "https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/releases/latest"

# Instalar via script oficial
Invoke-WebRequest -Uri "https://raw.githubusercontent.com/open-telemetry/opentelemetry-dotnet-instrumentation/main/script/Install-OpenTelemetryDotNetAutoInstrumentation.ps1" -OutFile "Install-Agent.ps1"
.\Install-Agent.ps1
```

### 2. Verificar a instalação

```powershell
# O agente instala-se tipicamente em:
# C:\Program Files\OpenTelemetry .NET AutoInstrumentation\

Get-ChildItem "C:\Program Files\OpenTelemetry .NET AutoInstrumentation\"
```

### 3. Confirmar que o OTel Collector está acessível

```powershell
# Testar conectividade com o Collector
Test-NetConnection -ComputerName otel-collector -Port 4317
```

---

## Configuração

### Configuração da aplicação (`appsettings.json`)

A secção `Telemetry:CollectionMode` controla o modo de coleta. Para ativar o CLR
Profiler, definir `ActiveMode` como `"ClrProfiler"`:

```json
{
  "Telemetry": {
    "CollectionMode": {
      "ActiveMode": "ClrProfiler",
      "ClrProfiler": {
        "Enabled": true,
        "Mode": "IIS",
        "ProfilerType": "AutoInstrumentation",
        "ExportTarget": "Collector",
        "OtlpEndpoint": "http://otel-collector:4317"
      }
    },
    "ObservabilityProvider": {
      "Provider": "ClickHouse",
      "ClickHouse": {
        "Enabled": true,
        "ConnectionString": "Host=clickhouse;Port=8123;Database=nextraceone_obs;Username=default;Password=",
        "Database": "nextraceone_obs",
        "LogsRetentionDays": 30,
        "TracesRetentionDays": 30,
        "MetricsRetentionDays": 90
      }
    }
  }
}
```

#### Parâmetros da secção `ClrProfiler`

| Parâmetro | Tipo | Default | Descrição |
|-----------|------|---------|-----------|
| `Enabled` | `bool` | `false` | Ativa o modo CLR Profiler |
| `Mode` | `string` | — | `"IIS"` para Application Pools ou `"SelfHosted"` para Kestrel/Windows Service |
| `ProfilerType` | `string` | — | `"AutoInstrumentation"` (automático) ou `"Manual"` (instrumentação seletiva) |
| `ExportTarget` | `string` | — | `"Collector"` (via OTel Collector) ou `"Direct"` (direto ao provider) |
| `OtlpEndpoint` | `string` | `"http://localhost:4317"` | Endpoint OTLP do Collector ou provider |

### Variáveis de ambiente para IIS

Configurar as seguintes variáveis de ambiente no Application Pool do IIS:

```ini
# --- Ativar o CLR Profiler ---
COR_ENABLE_PROFILING=1
COR_PROFILER={918728DD-259F-4A6A-AC2B-B85E1B658318}
COR_PROFILER_PATH=C:\Program Files\OpenTelemetry .NET AutoInstrumentation\win-x64\OpenTelemetry.AutoInstrumentation.Native.dll

# --- Para .NET Core/.NET 6+ ---
CORECLR_ENABLE_PROFILING=1
CORECLR_PROFILER={918728DD-259F-4A6A-AC2B-B85E1B658318}
CORECLR_PROFILER_PATH=C:\Program Files\OpenTelemetry .NET AutoInstrumentation\win-x64\OpenTelemetry.AutoInstrumentation.Native.dll

# --- Configuração OTLP ---
OTEL_EXPORTER_OTLP_ENDPOINT=http://otel-collector:4317
OTEL_EXPORTER_OTLP_PROTOCOL=grpc
OTEL_SERVICE_NAME=minha-aplicacao-iis
OTEL_RESOURCE_ATTRIBUTES=deployment.environment=production,service.namespace=nextraceone

# --- Instrumentação automática ---
OTEL_DOTNET_AUTO_HOME=C:\Program Files\OpenTelemetry .NET AutoInstrumentation
OTEL_TRACES_EXPORTER=otlp
OTEL_METRICS_EXPORTER=otlp
OTEL_LOGS_EXPORTER=otlp
```

### Configurar variáveis via PowerShell no IIS

```powershell
Import-Module WebAdministration

$appPoolName = "MeuAppPool"
$envVars = @{
    "COR_ENABLE_PROFILING"    = "1"
    "COR_PROFILER"            = "{918728DD-259F-4A6A-AC2B-B85E1B658318}"
    "COR_PROFILER_PATH"       = "C:\Program Files\OpenTelemetry .NET AutoInstrumentation\win-x64\OpenTelemetry.AutoInstrumentation.Native.dll"
    "CORECLR_ENABLE_PROFILING" = "1"
    "CORECLR_PROFILER"        = "{918728DD-259F-4A6A-AC2B-B85E1B658318}"
    "CORECLR_PROFILER_PATH"   = "C:\Program Files\OpenTelemetry .NET AutoInstrumentation\win-x64\OpenTelemetry.AutoInstrumentation.Native.dll"
    "OTEL_EXPORTER_OTLP_ENDPOINT" = "http://otel-collector:4317"
    "OTEL_EXPORTER_OTLP_PROTOCOL" = "grpc"
    "OTEL_SERVICE_NAME"       = "minha-aplicacao-iis"
    "OTEL_DOTNET_AUTO_HOME"   = "C:\Program Files\OpenTelemetry .NET AutoInstrumentation"
    "OTEL_TRACES_EXPORTER"    = "otlp"
    "OTEL_METRICS_EXPORTER"   = "otlp"
    "OTEL_LOGS_EXPORTER"      = "otlp"
}

foreach ($key in $envVars.Keys) {
    $path = "IIS:\AppPools\$appPoolName"
    $existing = Get-ItemProperty -Path $path -Name "environmentVariables" -ErrorAction SilentlyContinue
    Set-ItemProperty -Path $path -Name "environmentVariables" -Value @{
        Name = $key; Value = $envVars[$key]
    }
}

# Reiniciar o Application Pool
Restart-WebAppPool -Name $appPoolName
```

---

## Ativação no IIS

### Passo 1 — Definir variáveis no Application Pool

Utilizar o script PowerShell acima ou configurar manualmente via IIS Manager:

1. Abrir **IIS Manager**
2. Selecionar o **Application Pool** da aplicação
3. Clicar em **Advanced Settings**
4. Localizar **Environment Variables** (disponível em IIS 10+)
5. Adicionar todas as variáveis listadas na secção anterior

### Passo 2 — Reiniciar o Application Pool

```powershell
Restart-WebAppPool -Name "MeuAppPool"

# Ou reiniciar o IIS inteiro (afeta todos os sites)
iisreset /restart
```

### Passo 3 — Verificar que o profiler foi carregado

```powershell
# Procurar eventos do profiler no Event Viewer
Get-WinEvent -LogName "Application" -MaxEvents 50 |
    Where-Object { $_.Message -like "*OpenTelemetry*" -or $_.Message -like "*Profiler*" } |
    Format-Table TimeCreated, Message -AutoSize
```

### Modo SelfHosted (Kestrel / Windows Service)

Para aplicações que não correm em IIS (por exemplo, Kestrel como Windows Service):

```json
{
  "Telemetry": {
    "CollectionMode": {
      "ActiveMode": "ClrProfiler",
      "ClrProfiler": {
        "Enabled": true,
        "Mode": "SelfHosted",
        "ProfilerType": "AutoInstrumentation",
        "ExportTarget": "Collector",
        "OtlpEndpoint": "http://otel-collector:4317"
      }
    }
  }
}
```

As variáveis de ambiente são definidas diretamente no serviço Windows:

```powershell
[System.Environment]::SetEnvironmentVariable("CORECLR_ENABLE_PROFILING", "1", "Machine")
[System.Environment]::SetEnvironmentVariable("CORECLR_PROFILER", "{918728DD-259F-4A6A-AC2B-B85E1B658318}", "Machine")
# ... restantes variáveis ...

Restart-Service -Name "MeuServicoWindows"
```

---

## Coleta de traces

O CLR Profiler em modo `AutoInstrumentation` captura automaticamente:

### Instrumentações automáticas

| Tipo | O que captura | Exemplo |
|------|---------------|---------|
| **HTTP Server** | Pedidos HTTP recebidos pelo IIS/Kestrel | `GET /api/orders` → span `HTTP GET /api/orders` |
| **HTTP Client** | Chamadas HTTP externas (`HttpClient`) | `POST https://api.partner.com/v1/notify` |
| **SQL Client** | Queries SQL Server, PostgreSQL | `SELECT * FROM Orders WHERE Id = @id` |
| **Entity Framework** | Operações EF Core | `SaveChangesAsync` → span com query SQL |
| **gRPC Client** | Chamadas gRPC | `grpc.health.v1.Health/Check` |
| **Redis** | Operações StackExchange.Redis | `GET cache:orders:123` |
| **Message Queues** | Operações com RabbitMQ, Azure Service Bus | `SEND queue:order-events` |

### Atributos automáticos em cada span

- `service.name` — nome do serviço (de `OTEL_SERVICE_NAME`)
- `deployment.environment` — ambiente (de `OTEL_RESOURCE_ATTRIBUTES`)
- `http.method`, `http.status_code`, `http.route` — para spans HTTP
- `db.system`, `db.statement` — para spans de base de dados
- `exception.type`, `exception.message` — quando ocorrem exceções

### Exemplo de trace capturado

```
Trace: abc123def456
├── [HTTP Server] GET /api/orders/42           (12ms)
│   ├── [SQL Client] SELECT * FROM Orders...   (3ms)
│   ├── [HTTP Client] GET /api/inventory/42    (6ms)
│   │   └── [SQL Client] SELECT stock FROM...  (2ms)
│   └── [Redis] GET cache:order-status:42      (1ms)
```

---

## Coleta de logs IIS

### Logs do IIS (W3C)

O IIS gera logs em formato W3C que podem ser coletados e enviados ao pipeline:

#### Localização padrão dos logs

```
C:\inetpub\logs\LogFiles\W3SVC<site-id>\
```

#### Formato W3C típico

```
#Fields: date time s-ip cs-method cs-uri-stem cs-uri-query s-port cs-username c-ip cs(User-Agent) cs(Referer) sc-status sc-substatus sc-win32-status time-taken
2024-01-15 10:30:45 192.168.1.100 GET /api/orders - 443 - 10.0.0.50 Mozilla/5.0 - 200 0 0 125
```

#### Configurar coleta de logs IIS via Filelog Receiver

No OTel Collector, adicionar um receiver `filelog` para os logs IIS:

```yaml
receivers:
  filelog/iis:
    include:
      - 'C:\inetpub\logs\LogFiles\W3SVC*\*.log'
    exclude:
      - 'C:\inetpub\logs\LogFiles\W3SVC*\*.old'
    start_at: end
    operators:
      - type: regex_parser
        regex: '^(?P<date>\d{4}-\d{2}-\d{2}) (?P<time>\d{2}:\d{2}:\d{2}) (?P<server_ip>\S+) (?P<method>\S+) (?P<uri_stem>\S+) (?P<uri_query>\S+) (?P<port>\d+) (?P<username>\S+) (?P<client_ip>\S+) (?P<user_agent>\S+) (?P<referer>\S+) (?P<status>\d+) (?P<substatus>\d+) (?P<win32_status>\d+) (?P<time_taken>\d+)'
      - type: severity_parser
        parse_from: attributes.status
        mapping:
          error: ['500', '502', '503']
          warn: ['400', '401', '403', '404']
          info: ['200', '201', '204', '301', '302']
```

### Logs da aplicação (.NET)

Logs estruturados via `ILogger` são automaticamente capturados pelo profiler e
exportados via OTLP quando `OTEL_LOGS_EXPORTER=otlp` está configurado.

```csharp
// Estes logs são automaticamente enviados ao Collector
logger.LogInformation("Pedido {OrderId} processado com sucesso", orderId);
logger.LogError(ex, "Falha ao processar pedido {OrderId}", orderId);
```

---

## Validação

### 1. Verificar que o profiler está ativo

```powershell
# Verificar variáveis de ambiente do processo w3wp.exe
$process = Get-Process w3wp | Select-Object -First 1
[System.Diagnostics.Process]::GetProcessById($process.Id).StartInfo.EnvironmentVariables["COR_ENABLE_PROFILING"]
```

### 2. Verificar conectividade com o Collector

```powershell
# Health check do Collector
Invoke-RestMethod -Uri "http://otel-collector:13133/health/status"
# Resposta esperada: {"status":"Server available","..."}
```

### 3. Verificar traces no Collector (modo debug)

Se o exporter `debug` estiver ativo no Collector, os traces aparecem nos logs:

```bash
docker logs otel-collector 2>&1 | grep "TracesExporter"
```

### 4. Verificar dados no ClickHouse

```sql
-- Verificar traces recentes
SELECT ServiceName, OperationName, Duration, StatusCode
FROM nextraceone_obs.otel_traces
WHERE ServiceName = 'minha-aplicacao-iis'
ORDER BY Timestamp DESC
LIMIT 10;

-- Verificar logs recentes
SELECT Timestamp, SeverityText, Body
FROM nextraceone_obs.otel_logs
WHERE ServiceName = 'minha-aplicacao-iis'
ORDER BY Timestamp DESC
LIMIT 10;
```

### 5. Validação programática (`ClrProfilerStrategy.IsHealthyAsync`)

A estratégia CLR Profiler verifica automaticamente:

- Se `Enabled` é `true`
- Se o `OtlpEndpoint` está configurado e acessível
- Se o `ExportTarget` é válido (`"Collector"` ou `"Direct"`)

---

## Limitações

| Limitação | Impacto | Mitigação |
|-----------|---------|-----------|
| **Apenas Windows** | Não funciona em Linux/macOS | Usar OTel Collector para ambientes não-Windows |
| **.NET Framework 4.6.2+** | Versões mais antigas não suportadas | Atualizar o runtime ou usar instrumentação manual |
| **.NET 6+** | Requer CoreCLR profiler path | Configurar `CORECLR_PROFILER_PATH` corretamente |
| **Overhead de CPU** | 2-5% adicional no processo `w3wp.exe` | Aceitável para a maioria dos cenários; monitorizar |
| **Overhead de memória** | ~50-100 MB adicionais por processo | Configurar memory limits no Collector |
| **Application Pool recycling** | Pode perder spans em trânsito | O Collector com retry mitiga parcialmente |
| **Instrumentação limitada** | Nem todas as bibliotecas são suportadas | Complementar com instrumentação manual se necessário |
| **Debug/profiling conflitos** | Não compatível com outros profilers em simultâneo | Desativar outros profilers (ex.: Application Insights Profiler) |

---

## Troubleshooting

### O profiler não carrega

**Sintomas:** Nenhum trace aparece, Event Viewer sem eventos do OpenTelemetry.

**Diagnóstico:**

```powershell
# Verificar se as variáveis estão definidas
Get-WebConfigurationProperty -Filter "/system.applicationHost/applicationPools/add[@name='MeuAppPool']/environmentVariables" -Name "."

# Verificar se o DLL do profiler existe
Test-Path "C:\Program Files\OpenTelemetry .NET AutoInstrumentation\win-x64\OpenTelemetry.AutoInstrumentation.Native.dll"

# Verificar se o Application Pool está a correr em 64-bit
Get-ItemProperty "IIS:\AppPools\MeuAppPool" -Name "enable32BitAppOnWin64"
# Deve ser False para usar o profiler x64
```

**Soluções comuns:**

1. Garantir que o caminho do profiler corresponde à arquitetura (x64 vs x86)
2. Verificar que `COR_ENABLE_PROFILING=1` está definido
3. Reiniciar o Application Pool após configurar variáveis
4. Verificar permissões de leitura no diretório do agente

### Traces não aparecem no provider

**Sintomas:** Profiler carregou, mas dados não chegam ao ClickHouse/Elastic.

**Diagnóstico:**

```powershell
# Verificar se o Collector está a receber dados
Invoke-RestMethod -Uri "http://otel-collector:8888/metrics" |
    Select-String "otelcol_receiver_accepted_spans"

# Verificar logs do Collector para erros
docker logs otel-collector 2>&1 | Select-String "error|failed|rejected"
```

**Soluções comuns:**

1. Verificar `OTEL_EXPORTER_OTLP_ENDPOINT` — deve ser acessível a partir do servidor IIS
2. Verificar firewall entre o servidor IIS e o Collector (porta 4317)
3. Verificar que o protocolo corresponde (`grpc` na variável e no Collector)
4. Se `ExportTarget: "Direct"`, verificar conectividade direta com o provider

### Erros de permissão

**Sintomas:** Eventos de erro no Event Viewer relacionados com acesso negado.

**Soluções:**

1. O Application Pool identity deve ter permissão de leitura no diretório do agente
2. Em Windows Server com políticas restritivas, adicionar exceção para o DLL do profiler
3. Verificar que a conta do Application Pool pode aceder à rede (para envio OTLP)

### Alta utilização de CPU após ativação

**Diagnóstico:**

```powershell
# Monitorizar CPU do processo w3wp
Get-Process w3wp | Select-Object Id, CPU, WorkingSet64
```

**Soluções:**

1. Verificar se não existem outros profilers ativos em simultâneo
2. Reduzir a taxa de sampling no Collector (`tail_sampling`)
3. Excluir instrumentações desnecessárias via variáveis de ambiente:
   ```ini
   OTEL_DOTNET_AUTO_TRACES_DISABLED_INSTRUMENTATIONS=MongoDB,ElasticSearch
   ```

---

## Referências internas

- **Estratégia:** [`ClrProfilerStrategy.cs`](../../../src/building-blocks/NexTraceOne.BuildingBlocks.Observability/Observability/Collection/ClrProfiler/ClrProfilerStrategy.cs)
- **Interface:** [`ICollectionModeStrategy`](../../../src/building-blocks/NexTraceOne.BuildingBlocks.Observability/Observability/Abstractions/IObservabilityProvider.cs)
- **Configuração:** [`TelemetryStoreOptions.cs`](../../../src/building-blocks/NexTraceOne.BuildingBlocks.Observability/Telemetry/Configuration/TelemetryStoreOptions.cs) — secção `ClrProfilerModeOptions`
- **Registo DI:** [`DependencyInjection.cs`](../../../src/building-blocks/NexTraceOne.BuildingBlocks.Observability/DependencyInjection.cs) — seleção por `ActiveMode`
- **Alternativa K8s:** [Kubernetes + OTel Collector](./kubernetes-otel-collector.md)
