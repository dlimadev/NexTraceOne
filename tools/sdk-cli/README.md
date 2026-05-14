# NexTraceOne CLI

Command-line interface (CLI) oficial para interação com a plataforma **NexTraceOne**.

## 🚀 Instalação

### Via .NET Tool (Recomendado)

```bash
# Instalar globalmente
dotnet tool install -g NexTraceOne.Cli

# Ou instalar localmente no projeto
dotnet tool install NexTraceOne.Cli

# Verificar instalação
ntrace --version
```

### Via Download Direto

```bash
# Linux/Mac
curl -L https://github.com/nextraceone/NexTraceOne/releases/latest/download/ntrace-linux -o /usr/local/bin/ntrace
chmod +x /usr/local/bin/ntrace

# Windows (PowerShell)
Invoke-WebRequest -Uri "https://github.com/nextraceone/NexTraceOne/releases/latest/download/ntrace-win.exe" -OutFile "C:\Program Files\ntrace.exe"
```

### Via Docker

```bash
docker run --rm -it nextraceone/cli:latest --help
```

## 📋 Comandos Disponíveis

### Autenticação

```bash
# Login
ntrace auth login --email user@example.com --password yourpassword

# Logout
ntrace auth logout

# Verificar status
ntrace auth status

# Renovar token
ntrace auth refresh
```

### Gestão de Contratos

```bash
# Listar contratos
ntrace contracts list --page 1 --pageSize 20

# Obter detalhes de um contrato
ntrace contracts get --id <contract-id>

# Criar contrato
ntrace contracts create \
  --name "My API Contract" \
  --version "1.0.0" \
  --spec ./openapi.yaml

# Atualizar contrato
ntrace contracts update --id <contract-id> --spec ./updated-spec.yaml

# Deletar contrato
ntrace contracts delete --id <contract-id>

# Exportar contrato
ntrace contracts export --id <contract-id> --format postman
```

### Gestão de Incidentes

```bash
# Listar incidentes
ntrace incidents list --status open --severity high

# Criar incidente
ntrace incidents create \
  --title "API Latency Spike" \
  --description "Response times increased by 300%" \
  --severity High \
  --environment production

# Obter detalhes
ntrace incidents get --id <incident-id>

# Atualizar status
ntrace incidents update --id <incident-id> --status resolved

# Adicionar comentário
ntrace incidents comment --id <incident-id> --text "Root cause identified"
```

### Notificações

```bash
# Listar notificações
ntrace notifications list --unread

# Marcar como lida
ntrace notifications read --id <notification-id>

# Enviar notificação teste
ntrace notifications test --message "Test notification"
```

### Health Checks

```bash
# Verificar saúde da plataforma
ntrace health check

# Verificar saúde de módulo específico
ntrace health module --name catalog

# Verificar dependências
ntrace health dependencies
```

### Configuração

```bash
# Definir endpoint da API
ntrace config set endpoint https://api.nextraceone.com

# Definir timeout
ntrace config set timeout 30

# Listar configurações
ntrace config list

# Resetar configurações
ntrace config reset
```

## ⚙️ Opções Globais

| Opção | Descrição | Exemplo |
|-------|-----------|---------|
| `--endpoint` | Override do endpoint da API | `--endpoint https://staging.api.com` |
| `--api-key` | API key para autenticação | `--api-key abc123` |
| `--output` | Formato de output (json/yaml/table) | `--output json` |
| `--verbose` | Modo verbose com logs detalhados | `--verbose` |
| `--no-color` | Desabilitar output colorido | `--no-color` |

## 🎨 Exemplos de Uso

### Workflow Completo

```bash
# 1. Login
ntrace auth login --email admin@company.com

# 2. Listar contratos ativos
ntrace contracts list --status active --output table

# 3. Criar novo incidente
ntrace incidents create \
  --title "Database Connection Pool Exhaustion" \
  --severity Critical \
  --environment production \
  --description "All DB connections in use"

# 4. Verificar saúde do sistema
ntrace health check

# 5. Exportar relatório
ntrace contracts export --all --format pdf --output ./report.pdf
```

### Scripting/Automação

```bash
#!/bin/bash
# Script para monitoramento automatizado

# Verificar saúde
if ntrace health check --output json | jq '.status' | grep -q "healthy"; then
    echo "✅ System healthy"
else
    echo "❌ System unhealthy"
    # Criar incidente automaticamente
    ntrace incidents create \
      --title "Automated: System Health Check Failed" \
      --severity High
fi

# Listar incidentes críticos não resolvidos
ntrace incidents list \
  --severity Critical \
  --status open \
  --output json | jq '.[].title'
```

### Integração CI/CD

```yaml
# GitHub Actions example
- name: Validate API Contracts
  run: |
    ntrace auth login --api-key ${{ secrets.NEXTRACE_API_KEY }}
    ntrace contracts validate --spec ./api/openapi.yaml
    
- name: Report Deployment Incident
  if: failure()
  run: |
    ntrace incidents create \
      --title "Deployment Failed: ${{ github.workflow }}" \
      --severity High \
      --description "Commit: ${{ github.sha }}"
```

## 🔧 Configuração Avançada

### Arquivo de Configuração (~/.nextrace/config.json)

```json
{
  "endpoint": "https://api.nextraceone.com",
  "apiKey": "your-api-key-here",
  "timeout": 30,
  "retries": 3,
  "outputFormat": "table",
  "colors": true,
  "cache": {
    "enabled": true,
    "ttl": 300
  }
}
```

### Variáveis de Ambiente

```bash
export NEXTRACE_ENDPOINT=https://api.nextraceone.com
export NEXTRACE_API_KEY=your-api-key
export NEXTRACE_TIMEOUT=30
export NEXTRACE_OUTPUT=json
```

## 📊 Formatos de Output

### Table (Padrão)
```
┌─────────────────────┬──────────┬────────────┬──────────┐
│ ID                  │ Name     │ Version    │ Status   │
├─────────────────────┼──────────┼────────────┼──────────┤
│ abc-123             │ User API │ 1.0.0      │ Active   │
│ def-456             │ Order API│ 2.1.0      │ Draft    │
└─────────────────────┴──────────┴────────────┴──────────┘
```

### JSON
```json
{
  "contracts": [
    {
      "id": "abc-123",
      "name": "User API",
      "version": "1.0.0",
      "status": "Active"
    }
  ]
}
```

### YAML
```yaml
contracts:
  - id: abc-123
    name: User API
    version: 1.0.0
    status: Active
```

## 🐛 Troubleshooting

### Erro: "Authentication failed"
```bash
# Verificar credenciais
ntrace auth status

# Re-login
ntrace auth logout
ntrace auth login --email user@example.com
```

### Erro: "Connection timeout"
```bash
# Aumentar timeout
ntrace config set timeout 60

# Verificar endpoint
ntrace config list
```

### Erro: "Command not found"
```bash
# Verificar instalação
which ntrace

# Reinstalar
dotnet tool uninstall -g NexTraceOne.Cli
dotnet tool install -g NexTraceOne.Cli
```

## 🤝 Contribuição

Para desenvolver novos comandos:

1. Criar classe em `Commands/` herdando de `Command`
2. Implementar lógica em `ExecuteAsync()`
3. Registrar em `Program.cs`
4. Adicionar testes em `tests/`

## 📚 Recursos Adicionais

- [Documentação Oficial NexTraceOne](https://docs.nextraceone.com)
- [API Reference](https://api.nextraceone.com/swagger)
- [GitHub Repository](https://github.com/nextraceone/NexTraceOne)

---

**Versão:** 1.0.0  
**Última atualização:** 2026-05-12  
**Suporte:** support@nextraceone.com
