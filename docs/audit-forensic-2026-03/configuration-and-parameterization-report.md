# Relatório de Configuração e Parametrização — NexTraceOne
**Auditoria Forense | 28 de Março de 2026**

---

## Objetivo da Área no Contexto do Produto

Configuração deve suportar deployment enterprise self-hosted e on-prem, sem segredos hardcoded, com parâmetros funcionais persistidos na base de dados e parâmetros técnicos em variáveis de ambiente.

---

## O que Existe

### Ficheiros de Configuração Identificados

| Ficheiro | Localização | Propósito |
|---|---|---|
| `appsettings.json` | `src/platform/NexTraceOne.ApiHost/` | Config base do API Host |
| `appsettings.Development.json` | `src/platform/NexTraceOne.ApiHost/` | Override desenvolvimento |
| `appsettings.json` | `src/platform/NexTraceOne.Ingestion.Api/` | Config do Ingestion service |
| `.env.example` | raiz | Template de variáveis de ambiente |
| `global.json` | raiz | Versão .NET |
| `Directory.Build.props` | raiz | MSBuild global |
| `Directory.Packages.props` | raiz | NuGet centralizado |
| `otel-collector.yaml` | `build/otel-collector/` | OpenTelemetry collector |
| `docker-compose.yml` | raiz | Compose de produção |
| `docker-compose.override.yml` | raiz | Override desenvolvimento |

---

## Análise do appsettings.json (ApiHost)

### Connection Strings
```json
"NexTraceOne": "Host=localhost;Port=5432;Database=nextraceone;Username=nextraceone;Password=REPLACE_VIA_ENV"
```

22 connection strings confirmadas. Todas usam `Password=REPLACE_VIA_ENV`.

**Avaliação:**
- ✅ Sem senhas hardcoded — padrão `REPLACE_VIA_ENV` correto
- ✅ Todas apontam para mesmo PostgreSQL (estratégia de single-DB multi-schema)
- ⚠️ Maximum Pool Size fixo em 10 para todos — pode precisar de tuning por ambiente

### JWT Configuration
```json
"Jwt": {
  "Issuer": "NexTraceOne",
  "Audience": "nextraceone-api",
  "AccessTokenExpirationMinutes": 60,
  "RefreshTokenExpirationDays": 7
}
```

**Avaliação:**
- ✅ Sem JwtSecret hardcoded — deve vir de variável de ambiente
- ⚠️ Expiração de 60 minutos e 7 dias — valores aceitáveis, mas auditáveis
- ⚠️ Confirmar que `JwtSecret` vem de variável de ambiente e não de appsettings.Development.json

### AI Runtime Configuration
```json
"AiRuntime": {
  "Ollama": {
    "BaseUrl": "http://localhost:11434",
    "TimeoutSeconds": 120,
    "DefaultChatModel": "qwen3.5:9b",
    "Enabled": true
  },
  "OpenAI": {
    "ApiKey": "",
    "DefaultChatModel": "gpt-4o-mini",
    "Enabled": false
  },
  "Routing": {
    "PreferredProvider": "ollama",
    "PreferredChatModel": "qwen3.5:9b"
  }
}
```

**Avaliação:**
- ✅ OpenAI desativado por default — correto para on-prem
- ✅ `ApiKey: ""` vazio — sem chave hardcoded
- ✅ Ollama como provider preferencial — alinhado com IA local
- ⚠️ `BaseUrl: "http://localhost:11434"` — hardcoded para localhost; em deployment self-hosted precisa ser override
- ⚠️ Model `qwen3.5:9b` hardcoded — deveria vir de parametrização persistida (ConfigurationDbContext)

### Auth / Cookie Session
```json
"Auth": {
  "CookieSession": {
    "AccessTokenCookieName": "nxt_at",
    "CsrfCookieName": "nxt_csrf",
    "RequireSecureCookies": true
  }
}
```

**Avaliação:**
- ✅ `RequireSecureCookies: true` — correto para produção
- ✅ Nomes de cookies configuráveis
- ⚠️ Confirmar que `RequireSecureCookies` é `false` apenas em `appsettings.Development.json`

### OIDC Providers
```json
"OidcProviders": {
  "azure": {
    "Authority": "https://login.microsoftonline.com/{tenant-id}/v2.0",
    "ClientId": "",
    "ClientSecret": ""
  }
}
```

**Avaliação:**
- ✅ `ClientId` e `ClientSecret` vazios — sem segredos hardcoded
- ✅ `{tenant-id}` como placeholder — correto
- ⚠️ Apenas Azure pré-configurado; Keycloak e outros IDPs requerem configuração manual

### OpenTelemetry
```json
"Telemetry": {
  "OtlpEndpoint": "http://localhost:4317"
}
```

**Avaliação:**
- ⚠️ `localhost:4317` hardcoded em config base — requer override obrigatório em produção
- ⚠️ Sem configuração por ambiente visível no appsettings base

---

## O que Está Hardcoded e Não Deveria Estar

| Item | Localização | Problema |
|---|---|---|
| `DefaultChatModel: "qwen3.5:9b"` | `appsettings.json` | Deveria vir de `ConfigurationDbContext` (model registry) |
| `OtlpEndpoint: "http://localhost:4317"` | `appsettings.json` | Deveria ser override por ambiente |
| `MaxPoolSize: 10` | Todas connection strings | Deveria ser parametrizável |
| Runbooks hardcoded | `src/modules/operationalintelligence/` handlers | Deveria vir de `RunbookRecord` no DB |
| 8 serviços hardcoded em Reliability | handlers de reliability | Deveria vir de `ReliabilityDbContext` |
| `IsSimulated: true` em 22+ handlers | `src/modules/governance/` | Dados fabricados — sem parametrização de dados reais |

---

## O que Deve Permanecer em Configuração Técnica

✅ Correto permanecer em `appsettings.json`:
- Connection strings (com senha via ENV)
- JWT issuer/audience
- OidcProviders (com segredos via ENV)
- Cookie names e flags de segurança
- AI provider base URLs e timeouts
- SMTP/notification endpoints
- OpenTelemetry endpoint (com override por ambiente)

---

## O que Deve Ser Parametrização Persistida (ConfigurationDbContext)

| Parâmetro | Motivo |
|---|---|
| Modelo de IA padrão por tenant/role | Admin funcional precisa mudar sem redeploy |
| Janelas de deploy por ambiente | Operação muda conforme calendário |
| Critérios de aprovação de mudanças | Risco e governance evoluem |
| Thresholds de blast radius | Produto-específico, não técnico |
| Severidades e classificações de incidente | Operação configura |
| Políticas de retenção de auditoria | Admin funcional |
| Quotas de tokens AI por grupo/persona | Governance de IA |
| Configuração de conectores de integração | Admin de integrações |

O módulo `Configuration` com `ConfigurationDbContext` e 13 migrações já existe — é o lugar correto para estes parâmetros.

---

## Segredos e Exposição

| Item | Estado |
|---|---|
| Passwords em appsettings.json | ✅ `REPLACE_VIA_ENV` — sem exposição |
| JWT Secret | ✅ Não encontrado hardcoded |
| OpenAI ApiKey | ✅ `""` vazio em config base |
| OIDC Client secrets | ✅ `""` vazios |
| Encryption keys | ✅ Não hardcoded (AES-256-GCM key via ENV) |
| `.env.example` | ✅ Template sem valores reais |

**Avaliação de segurança de configuração:** Boa — sem segredos hardcoded identificados.

---

## Feature Flags

O módulo `Configuration` tem feature flags database-driven com:
- Override por tenant
- `ConfigurationDefinitionSeeder` para valores padrão
- API de gestão via `ConfigurationDbContext`

**Avaliação:** ✅ Feature flags funcionais e DB-driven.

---

## Consistência entre Ambientes

| Config | Desenvolvimento | Produção | Alinhamento |
|---|---|---|---|
| `RequireSecureCookies` | Presumivelmente `false` | `true` | ✅ Correto |
| `OtlpEndpoint` | `localhost:4317` | Precisa de override | ⚠️ Requer atenção |
| AI provider URL | `localhost:11434` | Precisa de override | ⚠️ Requer atenção |
| Log level | Provavelmente Debug/Verbose | Information/Warning | ⚠️ Confirmar |

---

## Obsoletos / Remover

Nenhum ficheiro de configuração identificado como obsoleto. `appsettings.Development.json` deve ser inspecionado para confirmar que não contém segredos em desenvolvimento.

---

## Recomendações

1. **Alta:** Override obrigatório de `OtlpEndpoint` por ambiente em deployment docs
2. **Alta:** Migrar `DefaultChatModel` para `ConfigurationDbContext` (model registry já existe)
3. **Média:** Documentar todos os overrides obrigatórios para self-hosted deployment
4. **Média:** Adicionar validação de startup para variáveis de ambiente obrigatórias ausentes
5. **Baixa:** Tornar `MaxPoolSize` parametrizável por ambiente

---

*Data: 28 de Março de 2026*
