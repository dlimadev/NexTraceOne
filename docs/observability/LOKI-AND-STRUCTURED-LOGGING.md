# Loki e Logging Estruturado

## Configuração do Sink

O Serilog envia logs ao Loki quando `Observability:Serilog:Loki:Endpoint` está configurado.

**Ficheiro de configuração:** `appsettings.json` (ApiHost e BackgroundWorkers)

```json
{
  "Observability": {
    "Serilog": {
      "Loki": {
        "Endpoint": "http://loki:3100"
      }
    }
  }
}
```

Em desenvolvimento, o endpoint está vazio (`""`) — o sink não é activado.
Em staging/produção, configurar via variável de ambiente ou secrets manager.

## Labels

| Label | Valor | Fonte |
|---|---|---|
| `application` | Nome do serviço OTel (`OpenTelemetry:ServiceName`) | Configuração |
| `environment` | Ambiente actual (`ASPNETCORE_ENVIRONMENT`) | Runtime |
| `module` | Módulo da aplicação | Propriedade de log |

### Cardinalidade

- `application`: valores fixos (ex: `nextraceone-apihost`, `nextraceone-backgroundworkers`)
- `environment`: valores fixos (dev, test, qa, uat, staging, production)
- `module`: valores controlados — não usar dados dinâmicos de utilizador como label

## Correlação com Traces

O sink Loki está configurado para incluir `TraceId` nos logs via `Enrich.FromLogContext()`.
O Grafana pode fazer drill-down do log para o trace correspondente via:
- Label derivado: `matcherRegex: '"TraceId":"([a-f0-9]{32})"'`
- Datasource linkado: Tempo (`nextraceone-tempo`)

## Boas Práticas e Limites

### Fazer
- Logar eventos operacionais com contexto estruturado (ServiceName, Environment, TenantId mascarado)
- Incluir CorrelationId e TraceId nos logs de request
- Usar `LogLevel.Information` para eventos de negócio normais
- Usar `LogLevel.Warning` para situações inesperadas recuperáveis
- Usar `LogLevel.Error` para falhas não recuperáveis

### Não Fazer
- **NUNCA** logar connection strings, tokens, passwords ou secrets
- **NUNCA** logar payloads completos de request/response com dados sensíveis
- **NUNCA** usar dados de utilizador como labels Loki (cardinalidade infinita)
- Não logar ao nível Debug em produção (custo e volume)

## Exemplo de Log Estruturado Correto

```csharp
_logger.LogInformation(
    "drift detected: {FindingsCount} findings for {ServiceName}/{Environment}",
    findings.Count, serviceName, environment);
```

Resultado em Loki:
```
{application="NexTraceOne", environment="production"} 
level=Information message="drift detected: 3 findings for payment-service/production"
ServiceName=payment-service Environment=production FindingsCount=3 TraceId=abc123...
```
